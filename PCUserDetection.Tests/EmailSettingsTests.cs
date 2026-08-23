using PCUserDetection;
using Xunit;

namespace PCUserDetection.Tests
{
    /// <summary>
    /// Covers the check that decides whether an alert can be attempted at all,
    /// which is the one piece of the email path that runs before anything
    /// touches the disk or the network.
    /// </summary>
    /// <remarks>
    /// <see cref="EmailSettings.IsComplete"/> is <see cref="EmailSettings.Describe"/>
    /// having found nothing, so the two are tested together: asserting on the
    /// message says both that the settings were refused and which of the
    /// several reasons it was refused for.
    /// </remarks>
    public class EmailSettingsTests
    {
        /// <summary>Settings that would send, as a starting point to break one thing in.</summary>
        private static EmailSettings Usable()
        {
            return new EmailSettings
            {
                Enabled = true,
                Delivery = EmailDelivery.Smtp,
                Host = "smtp.example.com",
                Port = 587,
                Security = EmailSecurity.StartTls,
                From = "watcher@example.com",
                To = "owner@example.com"
            };
        }

        [Fact]
        public void Usable_settings_are_complete()
        {
            Assert.Null(Usable().Describe());
            Assert.True(Usable().IsComplete);
        }

        [Fact]
        public void Fresh_settings_are_not_complete()
        {
            // a fresh clone has nowhere to send to, and must not read as ready
            var settings = new EmailSettings();

            Assert.False(settings.IsComplete);
            Assert.Equal("Enter the address the alert is sent from.", settings.Describe());
        }

        [Fact]
        public void Missing_recipient_is_reported()
        {
            var settings = Usable();
            settings.To = "   ";

            Assert.Equal("Enter the address the alert is sent to.", settings.Describe());
        }

        [Fact]
        public void Folder_delivery_needs_no_server()
        {
            // the point of folder delivery is exercising the alert without a mail
            // account, so it must not ask for the things an account brings
            var settings = new EmailSettings
            {
                Delivery = EmailDelivery.FileDrop,
                Host = string.Empty,
                Port = 0,
                From = "watcher@example.com",
                To = "owner@example.com"
            };

            Assert.Null(settings.Describe());
        }

        [Fact]
        public void Folder_delivery_still_needs_addresses()
        {
            // the .eml it writes is a message, and a message with no From is not
            // one any mail client will open
            var settings = new EmailSettings { Delivery = EmailDelivery.FileDrop, To = "owner@example.com" };

            Assert.Equal("Enter the address the alert is sent from.", settings.Describe());
        }

        [Fact]
        public void Smtp_without_a_host_is_reported()
        {
            var settings = Usable();
            settings.Host = string.Empty;

            Assert.Equal("Enter the SMTP server to send through.", settings.Describe());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(65536)]
        public void Port_outside_the_range_is_reported(int port)
        {
            var settings = Usable();
            settings.Port = port;

            Assert.Equal("The port has to be between 1 and 65535.", settings.Describe());
        }

        [Theory]
        [InlineData(1)]
        [InlineData(587)]
        [InlineData(65535)]
        public void Port_at_the_edges_of_the_range_is_accepted(int port)
        {
            var settings = Usable();
            settings.Port = port;

            Assert.Null(settings.Describe());
        }

        [Fact]
        public void Username_without_a_password_is_reported_by_name()
        {
            // the mistake is typing the address and stopping, so the message has
            // to name the account it is still waiting on
            var settings = Usable();
            settings.Username = "watcher@gmail.com";

            Assert.Equal("Enter the password for watcher@gmail.com.", settings.Describe());
        }

        [Fact]
        public void Username_with_a_password_is_complete()
        {
            var settings = Usable();
            settings.Username = "watcher@gmail.com";
            settings.Password = "an-app-password";

            Assert.Null(settings.Describe());
        }

        [Fact]
        public void No_username_needs_no_password()
        {
            // a relay on the same machine takes no credentials at all
            var settings = Usable();
            settings.Username = string.Empty;
            settings.Password = string.Empty;

            Assert.Null(settings.Describe());
        }

        [Fact]
        public void Negative_cooldown_is_reported()
        {
            var settings = Usable();
            settings.CooldownMinutes = -1;

            Assert.Equal("The cooldown cannot be negative.", settings.Describe());
        }

        [Fact]
        public void Cooldown_of_zero_is_accepted()
        {
            // zero is a deliberate choice: alert every time, no waiting
            var settings = Usable();
            settings.CooldownMinutes = 0;

            Assert.Null(settings.Describe());
        }

        [Fact]
        public void Cooldown_is_reported_before_anything_else()
        {
            // the ordering matters on the settings screen, which shows one line
            // at a time: a value that was typed beats fields never filled in
            var settings = new EmailSettings { CooldownMinutes = -5 };

            Assert.Equal("The cooldown cannot be negative.", settings.Describe());
        }

        [Fact]
        public void Password_is_not_stored_in_the_clear()
        {
            var settings = Usable();
            settings.Password = "an-app-password";

            Assert.Equal("an-app-password", settings.Password);
            Assert.NotEqual("an-app-password", settings.ProtectedPassword);
            Assert.DoesNotContain("an-app-password", settings.ProtectedPassword);
        }

        [Fact]
        public void Empty_password_stays_empty_rather_than_encrypted()
        {
            var settings = Usable();
            settings.Password = string.Empty;

            Assert.Equal(string.Empty, settings.ProtectedPassword);
            Assert.Equal(string.Empty, settings.Password);
        }
    }
}
