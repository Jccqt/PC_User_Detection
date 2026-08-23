using System;
using System.IO;
using PCUserDetection;
using Xunit;

namespace PCUserDetection.Tests
{
    /// <summary>
    /// Covers reading the settings back off disk and writing them to it, which
    /// the seam in AppPaths now points at a folder of the test's own.
    /// </summary>
    /// <remarks>
    /// The distinction these turn on is between a file that is not there and a
    /// file that cannot be read. The first is a fresh install and is answered
    /// with defaults; the second is answered with defaults and a line saying so,
    /// because presenting a guess as somebody's settings is how alerts go
    /// quietly missing.
    /// </remarks>
    public class EmailSettingsStoreTests : IDisposable
    {
        private readonly Sandbox sandbox = new Sandbox();

        public void Dispose()
        {
            sandbox.Dispose();
        }

        [Fact]
        public void No_file_at_all_is_a_fresh_install_rather_than_a_problem()
        {
            string problem;
            EmailSettings settings = EmailSettings.Load(out problem);

            Assert.Null(problem);
            Assert.False(settings.Enabled);
        }

        [Fact]
        public void Reading_a_file_back_gives_what_was_written()
        {
            var saved = new EmailSettings
            {
                Enabled = true,
                Delivery = EmailDelivery.Smtp,
                Host = "smtp.example.com",
                Port = 465,
                Security = EmailSecurity.SslOnConnect,
                Username = "watcher@example.com",
                From = "watcher@example.com",
                To = "owner@example.com",
                AttachPhoto = false,
                CooldownMinutes = 12
            };

            saved.Save();

            string problem;
            EmailSettings loaded = EmailSettings.Load(out problem);

            Assert.Null(problem);
            Assert.True(loaded.Enabled);
            Assert.Equal(EmailDelivery.Smtp, loaded.Delivery);
            Assert.Equal("smtp.example.com", loaded.Host);
            Assert.Equal(465, loaded.Port);
            Assert.Equal(EmailSecurity.SslOnConnect, loaded.Security);
            Assert.Equal("watcher@example.com", loaded.Username);
            Assert.Equal("owner@example.com", loaded.To);
            Assert.False(loaded.AttachPhoto);
            Assert.Equal(12, loaded.CooldownMinutes);
        }

        [Fact]
        public void A_password_survives_the_round_trip_without_being_written_in_the_clear()
        {
            var saved = new EmailSettings
            {
                From = "watcher@example.com",
                To = "owner@example.com",
                Password = "an-app-password"
            };

            saved.Save();

            string onDisk = File.ReadAllText(AppPaths.EmailSettingPath);
            Assert.DoesNotContain("an-app-password", onDisk);

            string problem;
            EmailSettings loaded = EmailSettings.Load(out problem);

            Assert.Null(problem);
            Assert.Equal("an-app-password", loaded.Password);
        }

        [Fact]
        public void A_file_that_does_not_parse_is_reported_and_named()
        {
            // what a hand-edit that went wrong leaves behind
            sandbox.WriteSettingsFile("{ \"Enabled\": true, ");

            string problem;
            EmailSettings settings = EmailSettings.Load(out problem);

            Assert.NotNull(problem);
            Assert.Contains(AppPaths.EmailSettingPath, problem);
            Assert.Contains("could not be read", problem);

            // the defaults come back alongside it, and must not be turned on
            Assert.False(settings.Enabled);
        }

        [Fact]
        public void A_file_holding_nothing_is_reported_as_empty()
        {
            // "null" is valid JSON and deserialises to nothing at all
            sandbox.WriteSettingsFile("null");

            string problem;
            EmailSettings settings = EmailSettings.Load(out problem);

            Assert.NotNull(problem);
            Assert.Contains("are empty", problem);
            Assert.False(settings.Enabled);
        }

        [Fact]
        public void A_delivery_the_app_does_not_have_is_refused_by_number()
        {
            // a bare number gets past the converter, and would then read as SMTP
            // at one switch and as neither delivery at the next
            sandbox.WriteSettingsFile("{ \"Enabled\": true, \"Delivery\": 7 }");

            string problem;
            EmailSettings settings = EmailSettings.Load(out problem);

            Assert.NotNull(problem);
            Assert.Contains("Delivery is 7", problem);
            Assert.False(settings.Enabled);
        }

        [Fact]
        public void A_delivery_the_app_does_not_have_is_refused_by_name()
        {
            // the two ways of writing down a choice that does not exist have to
            // fail the same way, or only one of them is ever noticed
            sandbox.WriteSettingsFile("{ \"Enabled\": true, \"Delivery\": \"Post\" }");

            string problem;
            EmailSettings settings = EmailSettings.Load(out problem);

            Assert.NotNull(problem);
            Assert.Contains("could not be read", problem);
            Assert.False(settings.Enabled);
        }

        [Fact]
        public void A_security_setting_the_app_does_not_have_is_refused()
        {
            sandbox.WriteSettingsFile("{ \"Enabled\": true, \"Security\": 9 }");

            string problem;
            EmailSettings settings = EmailSettings.Load(out problem);

            Assert.NotNull(problem);
            Assert.Contains("Security is 9", problem);
            Assert.False(settings.Enabled);
        }

        [Fact]
        public void Saving_over_working_settings_leaves_no_half_written_file()
        {
            // the file is written beside the real one and moved over it, so that
            // a machine going down mid-write cannot lose settings that worked
            var first = new EmailSettings
            {
                Enabled = true,
                From = "watcher@example.com",
                To = "owner@example.com",
                Host = "smtp.example.com"
            };

            first.Save();

            var second = new EmailSettings
            {
                Enabled = true,
                From = "watcher@example.com",
                To = "someone.else@example.com",
                Host = "smtp.example.com"
            };

            second.Save();

            string problem;
            EmailSettings loaded = EmailSettings.Load(out problem);

            Assert.Null(problem);
            Assert.Equal("someone.else@example.com", loaded.To);
            Assert.False(File.Exists(AppPaths.EmailSettingPath + ".tmp"));
        }

        [Fact]
        public void The_file_is_left_readable_for_anyone_who_opens_it()
        {
            new EmailSettings { From = "watcher@example.com", To = "owner@example.com" }.Save();

            string onDisk = File.ReadAllText(AppPaths.EmailSettingPath);

            // written indented and with the choices spelled out, not as numbers
            Assert.Contains(Environment.NewLine, onDisk);
            Assert.Contains("\"Delivery\": \"Smtp\"", onDisk);
        }
    }
}
