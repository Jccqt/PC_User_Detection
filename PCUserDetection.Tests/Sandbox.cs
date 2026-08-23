using System;
using System.IO;
using System.Linq;
using MimeKit;
using PCUserDetection;

namespace PCUserDetection.Tests
{
    /// <summary>
    /// A folder of the test's own, standing in for the two the app writes to on
    /// a real machine, with the handful of things a test needs to put into it
    /// and read back out.
    /// </summary>
    /// <remarks>
    /// Every test that reaches the settings or the alert takes one of these and
    /// disposes it, so no test ever sees a file another test wrote, and none of
    /// them ever touch the settings or the sent messages actually in use.
    /// </remarks>
    internal class Sandbox : IDisposable
    {
        private readonly string root;

        public Sandbox()
        {
            root = Path.Combine(Path.GetTempPath(),
                "PCUserDetection.Tests", Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(root);

            AppPaths.RedirectToSandbox(root);

            // the cooldown outlives the test that started it, so a test that
            // sends would otherwise decide the outcome of the one after it
            EmailAlert.ForgetLastSent();
        }

        public void Dispose()
        {
            AppPaths.ClearSandbox();
            EmailAlert.ForgetLastSent();

            try
            {
                Directory.Delete(root, true);
            }
            catch (Exception)
            {
                // a folder under temp that will not go is Windows' to clear up;
                // failing the test over it would report the wrong thing
            }
        }

        /// <summary>Settings that deliver to a folder, so a send needs no account.</summary>
        public static EmailSettings Deliverable()
        {
            return new EmailSettings
            {
                Enabled = true,
                Delivery = EmailDelivery.FileDrop,
                From = "watcher@example.com",
                To = "owner@example.com",
                AttachPhoto = false,
                CooldownMinutes = 0
            };
        }

        /// <summary>Saves settings the way the settings screen would.</summary>
        public void Save(EmailSettings settings)
        {
            settings.Save();
        }

        /// <summary>Writes the settings file by hand, for the cases a person edits it into.</summary>
        public void WriteSettingsFile(string contents)
        {
            string path = AppPaths.EmailSetting;
            File.WriteAllText(path, contents);
        }

        /// <summary>Writes something standing in for a captured frame.</summary>
        public string GivenAPhoto()
        {
            string path = Path.Combine(root, "Anonymous.jpeg");
            File.WriteAllText(path, "not really a jpeg");
            return path;
        }

        /// <summary>Names a photo that is not there.</summary>
        public string GivenNoPhoto()
        {
            return Path.Combine(root, "gone.jpeg");
        }

        /// <summary>
        /// Puts a file where the drop folder has to go, so that writing a
        /// message fails without needing a mail server to be unreachable.
        /// </summary>
        public void BlockTheDropFolder()
        {
            string path = AppPaths.EmailDropsPath;

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, "in the way");
        }

        /// <summary>Lets a blocked drop folder be written to again.</summary>
        public void UnblockTheDropFolder()
        {
            File.Delete(AppPaths.EmailDropsPath);
        }

        /// <summary>The messages folder delivery has written, oldest first.</summary>
        public string[] Drops()
        {
            if (!Directory.Exists(AppPaths.EmailDropsPath)) return new string[0];

            string[] drops = Directory.GetFiles(AppPaths.EmailDropsPath, "Alert_*.eml");
            Array.Sort(drops, StringComparer.OrdinalIgnoreCase);
            return drops;
        }

        /// <summary>Reads back the one message that was written.</summary>
        public MimeMessage TheOnlyDrop()
        {
            string[] drops = Drops();

            if (drops.Length != 1)
            {
                throw new InvalidOperationException(
                    "Expected exactly one message, found " + drops.Length + ".");
            }

            return MimeMessage.Load(drops[0]);
        }

        /// <summary>The copies taken for sending that are still sitting in temp.</summary>
        public string[] AttachmentCopies()
        {
            string folder = AppPaths.AttachmentCopies;

            return Directory.GetFiles(folder, "Anonymous_*.jpeg")
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToArray();
        }
    }
}
