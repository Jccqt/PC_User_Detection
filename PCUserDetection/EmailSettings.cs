using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PCUserDetection
{
    /// <summary>Where an alert goes once it has been composed.</summary>
    internal enum EmailDelivery
    {
        /// <summary>Hand the message to the configured SMTP server.</summary>
        Smtp,

        /// <summary>
        /// Write the message to a folder as an .eml file instead of sending it.
        /// The alert can then be exercised end to end without a mail account,
        /// which is what anyone building the project from a fresh clone has.
        /// </summary>
        FileDrop
    }

    /// <summary>How the connection to the SMTP server is secured.</summary>
    internal enum EmailSecurity
    {
        /// <summary>Connect in the clear and upgrade to TLS. The usual choice, on port 587.</summary>
        StartTls,

        /// <summary>Connect with TLS already established. The older scheme, on port 465.</summary>
        SslOnConnect,

        /// <summary>No encryption at all. Only sensible for a relay on the same machine.</summary>
        None
    }

    /// <summary>
    /// The email alert configuration, as it is written to
    /// <see cref="AppPaths.EmailSetting"/>.
    /// </summary>
    /// <remarks>
    /// Nothing here is ever committed: the file lives under AppData, and the
    /// password is not stored in the clear. It is encrypted with DPAPI under the
    /// Windows account that entered it, so the file is useless if it is copied
    /// to another machine or read by another user, and there is no key for the
    /// app to keep anywhere.
    /// </remarks>
    internal class EmailSettings
    {
        /// <summary>Alerts are off until somebody turns them on, so a fresh clone sends nothing.</summary>
        public bool Enabled { get; set; }

        public EmailDelivery Delivery { get; set; } = EmailDelivery.Smtp;

        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public EmailSecurity Security { get; set; } = EmailSecurity.StartTls;

        /// <summary>Left empty for a relay that does not ask who is connecting.</summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// The DPAPI blob, base64 encoded. Read and written through
        /// <see cref="Password"/> rather than directly.
        /// </summary>
        [JsonPropertyName("PasswordProtected")]
        public string ProtectedPassword { get; set; } = string.Empty;

        public string From { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;

        /// <summary>Attaches the frame that failed the check to the alert.</summary>
        public bool AttachPhoto { get; set; } = true;

        /// <summary>
        /// How long to wait before a second alert may be sent. Without this, a
        /// stranger sitting in front of the camera is a mailbox full of alerts.
        /// </summary>
        public int CooldownMinutes { get; set; } = 5;

        /// <summary>
        /// The password in the clear. Reading it decrypts the stored blob and
        /// writing it encrypts what is given; the plain text is never persisted.
        /// A blob that cannot be decrypted here reads as no password rather than
        /// throwing: it belongs to another user or another machine, and the way
        /// out of that is to type the password again.
        /// </summary>
        /// <exception cref="CryptographicException">
        /// From the setter, when Windows will not encrypt what it was given. See
        /// <see cref="Protect"/> for why that is not quietly swallowed.
        /// </exception>
        [JsonIgnore]
        public string Password
        {
            get { return Unprotect(ProtectedPassword); }
            set { ProtectedPassword = Protect(value); }
        }

        /// <summary>True when there is enough here to attempt a send.</summary>
        [JsonIgnore]
        public bool IsComplete
        {
            get { return Describe() == null; }
        }

        /// <summary>
        /// Returns what is missing or wrong, or null when the settings are usable.
        /// The settings screen shows this, so it is written for a person to read.
        /// </summary>
        public string Describe()
        {
            // a negative wait is not a shorter one: anything at or below zero is
            // treated as no cooldown at all, so it would alert on every frame
            // rather than the once every few minutes that was being asked for
            if (CooldownMinutes < 0) return "The cooldown cannot be negative.";

            if (string.IsNullOrWhiteSpace(From)) return "Enter the address the alert is sent from.";
            if (string.IsNullOrWhiteSpace(To)) return "Enter the address the alert is sent to.";

            if (Delivery == EmailDelivery.FileDrop) return null;

            if (string.IsNullOrWhiteSpace(Host)) return "Enter the SMTP server to send through.";
            if (Port <= 0 || Port > 65535) return "The port has to be between 1 and 65535.";

            // an empty password with a username set is the mistake people make
            // after typing a Gmail address and stopping there
            if (!string.IsNullOrWhiteSpace(Username) && string.IsNullOrEmpty(Password))
            {
                return "Enter the password for " + Username + ".";
            }

            return null;
        }

        /// <summary>Reads the saved settings back off disk.</summary>
        /// <param name="problem">
        /// Null when what comes back is what is on disk, which includes there
        /// being no file yet: a fresh install has nothing to lose and the
        /// defaults are the truth. Otherwise a line written for a person to
        /// read, saying the file is there but could not be read. The defaults
        /// returned alongside it are then a guess rather than anybody's choice,
        /// and the caller has to say so rather than present them as settings.
        /// </param>
        public static EmailSettings Load(out string problem)
        {
            problem = null;

            string path = AppPaths.EmailSetting;

            try
            {
                if (!File.Exists(path)) return new EmailSettings();

                var loaded = JsonSerializer.Deserialize<EmailSettings>(File.ReadAllText(path), Options);
                if (loaded != null) return loaded;

                // "null" is valid JSON and deserialises to nothing at all, which
                // is no more a usable set of settings than a truncated file is
                problem = "The saved settings in " + path + " are empty.";
            }
            catch (Exception ex)
            {
                // an unreadable or hand-edited settings file should leave the app
                // working rather than stop it from starting, but it is not the
                // same thing as having turned alerts off and must not read as it
                Console.WriteLine(ex);
                problem = "The saved settings in " + path + " could not be read. " + ex.Message;
            }

            return new EmailSettings();
        }

        /// <summary>Writes the settings to disk, all of them or none of them.</summary>
        /// <remarks>
        /// The file is written beside the real one and then moved over it, which
        /// the file system does in a single step. Writing over the file in place
        /// would leave a half-written one behind if the machine went down during
        /// the write, and a half-written file does not parse: the next start
        /// would come up with alerts off and the password gone, having said
        /// nothing about it. Losing the change being saved is recoverable;
        /// quietly losing the settings that were already working is not.
        /// </remarks>
        public void Save()
        {
            string path = AppPaths.EmailSetting;
            string temporary = path + ".tmp";

            try
            {
                using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    JsonSerializer.Serialize(stream, this, Options);

                    // the move only carries across what has actually reached the
                    // disk, so the bytes are pushed out of the cache before it
                    stream.Flush(true);
                }

                if (File.Exists(path)) File.Replace(temporary, path, null);
                else File.Move(temporary, path);
            }
            catch (Exception)
            {
                Discard(temporary);
                throw;
            }
        }

        /// <summary>Clears away a temporary file that never made it into place.</summary>
        private static void Discard(string path)
        {
            try
            {
                File.Delete(path);
            }
            catch (Exception ex)
            {
                // the failure that brought us here is the one worth reporting,
                // and a stray .tmp is written over by the next save anyway
                Console.WriteLine(ex);
            }
        }

        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            WriteIndented = true, // the file is meant to be readable if somebody opens it
            Converters = { new JsonStringEnumConverter() }
        };

        /// <summary>Encrypts a password for storage, or fails loudly trying.</summary>
        /// <remarks>
        /// There is no fall back here, in either direction. Writing the password
        /// out in the clear is worse than an alert that cannot be sent, and
        /// storing nothing while reporting success is worse still: the password
        /// is just as lost, and the first anybody hears of it is an alert that
        /// does not arrive. So the failure is left to reach the caller, which
        /// can say a password was not saved before anyone relies on it.
        /// </remarks>
        /// <exception cref="CryptographicException">Windows would not encrypt it.</exception>
        private static string Protect(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return string.Empty;

            return Convert.ToBase64String(ProtectedData.Protect(
                Encoding.UTF8.GetBytes(plainText), null, DataProtectionScope.CurrentUser));
        }

        private static string Unprotect(string protectedText)
        {
            if (string.IsNullOrEmpty(protectedText)) return string.Empty;

            try
            {
                return Encoding.UTF8.GetString(ProtectedData.Unprotect(
                    Convert.FromBase64String(protectedText), null, DataProtectionScope.CurrentUser));
            }
            catch (Exception)
            {
                // the blob belongs to another user or another machine, so it
                // cannot be read here and the password has to be entered again
                return string.Empty;
            }
        }
    }
}
