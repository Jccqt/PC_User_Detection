using MailKit.Security;
using MimeKit;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
// MailKit and System.Net.Mail both have an SmtpClient, and the built-in one is
// obsolete for new code; the alias keeps it obvious which one is in use here.
using MailKitSmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace PCUserDetection
{
    /// <summary>What an alert says, before it is turned into a mail message.</summary>
    internal class EmailMessage
    {
        public string Subject { get; set; }
        public string Body { get; set; }

        /// <summary>The photo to attach, or null to send the message on its own.</summary>
        public string AttachmentPath { get; set; }
    }

    /// <summary>
    /// Delivers a composed message. The transport sits behind this interface so
    /// the screen and the alert policy never mention SMTP: a provider API could
    /// be added later as another implementation without either of them changing.
    /// </summary>
    internal interface IEmailSender
    {
        Task SendAsync(EmailMessage message, CancellationToken cancellationToken);

        /// <summary>Where the message went, phrased for the status line.</summary>
        string Destination { get; }
    }

    /// <summary>Chooses the sender the settings ask for.</summary>
    internal static class EmailSenderFactory
    {
        public static IEmailSender Create(EmailSettings settings)
        {
            if (settings.Delivery == EmailDelivery.FileDrop) return new FileDropEmailSender(settings);
            return new SmtpEmailSender(settings);
        }
    }

    /// <summary>
    /// Sends through any SMTP server. Nothing here is tied to a particular mail
    /// provider: the host, port and credentials all come from the settings, so
    /// Gmail, a company relay or a local test server are the same code path.
    /// </summary>
    internal class SmtpEmailSender : IEmailSender
    {
        // long enough for a slow server, short enough that a wrong host does not
        // leave the person watching a stuck status line for MailKit's two minutes
        private const int TimeoutMilliseconds = 30000;

        private readonly EmailSettings settings;

        public SmtpEmailSender(EmailSettings settings)
        {
            this.settings = settings;
        }

        public string Destination
        {
            get { return settings.To; }
        }

        public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
        {
            MimeMessage mime = EmailComposer.Compose(settings, message);

            using (var client = new MailKitSmtpClient())
            {
                client.Timeout = TimeoutMilliseconds;

                await client.ConnectAsync(settings.Host, settings.Port, SocketOptions(), cancellationToken);

                // a relay on the same machine usually wants no credentials, and
                // offering them to one that does not expect them fails the send
                if (!string.IsNullOrWhiteSpace(settings.Username))
                {
                    await client.AuthenticateAsync(settings.Username, settings.Password, cancellationToken);
                }

                await client.SendAsync(mime, cancellationToken);
                await client.DisconnectAsync(true, cancellationToken);
            }
        }

        private SecureSocketOptions SocketOptions()
        {
            switch (settings.Security)
            {
                case EmailSecurity.SslOnConnect: return SecureSocketOptions.SslOnConnect;
                case EmailSecurity.None: return SecureSocketOptions.None;
                default: return SecureSocketOptions.StartTls;
            }
        }
    }

    /// <summary>
    /// Writes the message to <see cref="AppPaths.EmailDrops"/> as an .eml file
    /// rather than sending it.
    /// </summary>
    /// <remarks>
    /// This exists so the whole path — the failed check, the cooldown, the
    /// attachment, the composed message — can be exercised by somebody who has
    /// just cloned the repository and has no mail account to point it at. The
    /// files it writes open in any mail client.
    /// </remarks>
    internal class FileDropEmailSender : IEmailSender
    {
        private readonly EmailSettings settings;

        public FileDropEmailSender(EmailSettings settings)
        {
            this.settings = settings;
        }

        public string Destination
        {
            get { return AppPaths.EmailDrops; }
        }

        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
        {
            MimeMessage mime = EmailComposer.Compose(settings, message);

            string filename = string.Format("Alert_{0:yyyyMMdd_HHmmss_fff}.eml", DateTime.Now);
            string path = Path.Combine(AppPaths.EmailDrops, filename);

            using (var stream = File.Create(path))
            {
                mime.WriteTo(stream, cancellationToken);
            }

            return Task.CompletedTask;
        }
    }

    /// <summary>Turns an <see cref="EmailMessage"/> into the message that is sent.</summary>
    internal static class EmailComposer
    {
        public static MimeMessage Compose(EmailSettings settings, EmailMessage message)
        {
            var mime = new MimeMessage();
            mime.From.Add(MailboxAddress.Parse(settings.From));

            // several recipients can be given, separated by commas, so an alert
            // can reach more than one person without any more configuration
            foreach (string recipient in settings.To.Split(','))
            {
                if (!string.IsNullOrWhiteSpace(recipient))
                {
                    mime.To.Add(MailboxAddress.Parse(recipient.Trim()));
                }
            }

            mime.Subject = message.Subject;

            var builder = new BodyBuilder { TextBody = message.Body };

            if (!string.IsNullOrEmpty(message.AttachmentPath) && File.Exists(message.AttachmentPath))
            {
                builder.Attachments.Add(message.AttachmentPath);
            }

            mime.Body = builder.ToMessageBody();
            return mime;
        }
    }
}
