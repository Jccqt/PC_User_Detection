using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PCUserDetection
{
    /// <summary>What became of an alert. Everything but <see cref="Failed"/> is normal.</summary>
    internal enum EmailAlertOutcome
    {
        /// <summary>Alerts are turned off, which is how the app ships.</summary>
        Disabled,

        /// <summary>Turned on, but the settings are not filled in yet.</summary>
        Incomplete,

        /// <summary>An alert went out recently enough that this one was held back.</summary>
        Cooling,

        Sent,
        Failed
    }

    /// <summary>The outcome of an alert, and a line about it for the status bar.</summary>
    internal class EmailAlertResult
    {
        public EmailAlertResult(EmailAlertOutcome outcome, string detail)
        {
            Outcome = outcome;
            Detail = detail;
        }

        public EmailAlertOutcome Outcome { get; private set; }

        /// <summary>Written for a person to read, or empty when there is nothing to say.</summary>
        public string Detail { get; private set; }
    }

    /// <summary>
    /// Decides whether a failed check should become an email, and composes it.
    /// The transport is left to <see cref="IEmailSender"/>; this is only the
    /// policy around it.
    /// </summary>
    internal static class EmailAlert
    {
        /// <summary>
        /// When the last alert went out, so the cooldown can be applied. Held in
        /// memory rather than on disk on purpose: restarting the app is a
        /// deliberate act, and it should be able to alert again straight away.
        /// </summary>
        private static DateTime lastSentUtc = DateTime.MinValue;

        /// <summary>
        /// Emails the frame that failed the check, unless the settings say not to
        /// or an alert has already gone out inside the cooldown.
        /// </summary>
        /// <param name="sending">
        /// Called once it is settled that a message is going out, so the screen
        /// can say so before the wait for the server begins. It is not called
        /// when the alert is skipped, which keeps a disabled alert silent.
        /// </param>
        /// <remarks>
        /// This never throws. A mail server that is unreachable is a bad moment
        /// for the alert, not a reason to take the window down, so the failure
        /// comes back as a result to put on the status line.
        /// </remarks>
        public static async Task<EmailAlertResult> SendAnonymousAsync(string photoPath, Action sending)
        {
            string problem;
            EmailSettings settings = EmailSettings.Load(out problem);

            // calling an unreadable settings file "disabled" would be the one
            // wrong answer that says nothing: a stranger was at the machine and
            // nobody would ever be told the alert did not go out
            if (problem != null) return new EmailAlertResult(EmailAlertOutcome.Failed, problem);

            if (!settings.Enabled) return new EmailAlertResult(EmailAlertOutcome.Disabled, string.Empty);

            if (!settings.IsComplete)
            {
                return new EmailAlertResult(EmailAlertOutcome.Incomplete,
                    "Alerts are on, but the email settings are not finished.");
            }

            TimeSpan remaining = CooldownRemaining(settings);

            if (remaining > TimeSpan.Zero)
            {
                return new EmailAlertResult(EmailAlertOutcome.Cooling,
                    string.Format("No alert sent; the next one can go out in {0}.", Describe(remaining)));
            }

            if (sending != null) sending();

            // The frame is captured over the same file every time, so an alert
            // sending in the background would race the next capture. Copying it
            // first means the message carries the face that failed this check.
            string attachment = settings.AttachPhoto ? CopyForSending(photoPath) : null;

            try
            {
                var sender = EmailSenderFactory.Create(settings);

                await sender.SendAsync(new EmailMessage
                {
                    Subject = "Unrecognised person at " + Environment.MachineName,
                    Body = AnonymousBody(settings),
                    AttachmentPath = attachment
                }, CancellationToken.None);

                lastSentUtc = DateTime.UtcNow;

                return new EmailAlertResult(EmailAlertOutcome.Sent,
                    "Alert sent to " + sender.Destination + ".");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);

                // the cooldown is deliberately not started here, so a server that
                // is briefly down does not swallow the next alert as well
                return new EmailAlertResult(EmailAlertOutcome.Failed,
                    "The alert email could not be sent. " + Summarise(ex));
            }
            finally
            {
                Discard(attachment);
            }
        }

        /// <summary>
        /// Sends a message to prove the settings work, using what is on the
        /// settings screen rather than what was last saved. The cooldown does not
        /// apply: the person is standing there asking for it.
        /// </summary>
        public static async Task<EmailAlertResult> SendTestAsync(EmailSettings settings)
        {
            string problem = settings.Describe();

            if (problem != null) return new EmailAlertResult(EmailAlertOutcome.Incomplete, problem);

            try
            {
                var sender = EmailSenderFactory.Create(settings);

                await sender.SendAsync(new EmailMessage
                {
                    Subject = "PC User Detection test message",
                    Body = "This is a test from PC User Detection on " + Environment.MachineName +
                           "." + Environment.NewLine + Environment.NewLine +
                           "If it arrived, an alert will too.",
                    AttachmentPath = null
                }, CancellationToken.None);

                return new EmailAlertResult(EmailAlertOutcome.Sent,
                    "Test message sent to " + sender.Destination + ".");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return new EmailAlertResult(EmailAlertOutcome.Failed,
                    "The test message could not be sent. " + Summarise(ex));
            }
        }

        private static string AnonymousBody(EmailSettings settings)
        {
            string body =
                "A face was captured that did not match any registered image." + Environment.NewLine +
                Environment.NewLine +
                "Computer:  " + Environment.MachineName + Environment.NewLine +
                "Signed in: " + Environment.UserName + Environment.NewLine +
                "Time:      " + DateTime.Now.ToString("d MMM yyyy, HH:mm:ss") + Environment.NewLine;

            if (settings.AttachPhoto) body += Environment.NewLine + "The captured frame is attached.";

            return body;
        }

        private static TimeSpan CooldownRemaining(EmailSettings settings)
        {
            if (settings.CooldownMinutes <= 0) return TimeSpan.Zero;

            TimeSpan since = DateTime.UtcNow - lastSentUtc;
            TimeSpan cooldown = TimeSpan.FromMinutes(settings.CooldownMinutes);

            return since >= cooldown ? TimeSpan.Zero : cooldown - since;
        }

        private static string Describe(TimeSpan remaining)
        {
            int minutes = (int)Math.Ceiling(remaining.TotalMinutes);

            if (minutes <= 1) return "under a minute";
            return minutes + " minutes";
        }

        /// <summary>
        /// The first line of the exception. The full trace goes to the console;
        /// the status line only has room for what went wrong.
        /// </summary>
        private static string Summarise(Exception ex)
        {
            string message = ex.Message ?? string.Empty;
            int newline = message.IndexOf('\n');

            return newline < 0 ? message : message.Substring(0, newline).Trim();
        }

        private static string CopyForSending(string photoPath)
        {
            try
            {
                if (!File.Exists(photoPath)) return null;

                string copy = Path.Combine(AppPaths.AttachmentCopies,
                    string.Format("Anonymous_{0:yyyyMMdd_HHmmss_fff}.jpeg", DateTime.Now));

                File.Copy(photoPath, copy, true);
                return copy;
            }
            catch (Exception ex)
            {
                // an alert with no photo still tells somebody what happened, so
                // this is not worth failing the send over
                Console.WriteLine(ex);
                return null;
            }
        }

        private static void Discard(string path)
        {
            if (path == null) return;

            try
            {
                File.Delete(path);
            }
            catch (Exception)
            {
                // a stray file in the temp folder is Windows' problem to clean up
            }
        }
    }
}
