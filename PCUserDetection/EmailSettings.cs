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
        /// </summary>
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

        public static EmailSettings Load()
        {
            try
            {
                string path = AppPaths.EmailSetting;

                if (File.Exists(path))
                {
                    var loaded = JsonSerializer.Deserialize<EmailSettings>(File.ReadAllText(path), Options);
                    if (loaded != null) return loaded;
                }
            }
            catch (Exception)
            {
                // an unreadable or hand-edited settings file should leave the app
                // working with alerts off, not stop it from starting
            }

            return new EmailSettings();
        }

        public void Save()
        {
            File.WriteAllText(AppPaths.EmailSetting, JsonSerializer.Serialize(this, Options));
        }

        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            WriteIndented = true, // the file is meant to be readable if somebody opens it
            Converters = { new JsonStringEnumConverter() }
        };

        private static string Protect(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return string.Empty;

            try
            {
                return Convert.ToBase64String(ProtectedData.Protect(
                    Encoding.UTF8.GetBytes(plainText), null, DataProtectionScope.CurrentUser));
            }
            catch (CryptographicException)
            {
                // refusing to fall back to plain text: an alert that cannot be
                // sent is a smaller problem than a password on disk in the clear
                return string.Empty;
            }
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
