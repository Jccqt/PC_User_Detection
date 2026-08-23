using System;
using System.Threading.Tasks;
using MimeKit;
using PCUserDetection;
using Xunit;

namespace PCUserDetection.Tests
{
    /// <summary>
    /// Covers what becomes of a failed check: whether it turns into an email at
    /// all, and what the screen is told either way.
    /// </summary>
    /// <remarks>
    /// Every case here delivers to a folder, so the whole path runs end to end
    /// with no mail account and no network: the settings are read off disk, the
    /// decision is made, the message is composed, and the .eml that was written
    /// is read back and asserted on.
    /// </remarks>
    public class EmailAlertTests : IDisposable
    {
        private readonly Sandbox sandbox = new Sandbox();

        public void Dispose()
        {
            sandbox.Dispose();
        }

        /// <summary>Records whether the screen was told a message was going out.</summary>
        private class Screen
        {
            public int Told;

            public Action Sending
            {
                get { return () => Told++; }
            }
        }

        [Fact]
        public async Task Settings_that_cannot_be_read_are_reported_rather_than_read_as_off()
        {
            // the one wrong answer that says nothing: a stranger was at the
            // machine and nobody is ever told the alert did not go out
            sandbox.WriteSettingsFile("{ not json");

            var screen = new Screen();
            EmailAlertResult result = await EmailAlert.SendAnonymousAsync(
                sandbox.GivenAPhoto(), screen.Sending);

            Assert.Equal(EmailAlertOutcome.Failed, result.Outcome);
            Assert.Contains("could not be read", result.Detail);
            Assert.Equal(0, screen.Told);
        }

        [Fact]
        public async Task Alerts_that_are_off_say_nothing()
        {
            EmailSettings settings = Sandbox.Deliverable();
            settings.Enabled = false;
            sandbox.Save(settings);

            var screen = new Screen();
            EmailAlertResult result = await EmailAlert.SendAnonymousAsync(
                sandbox.GivenAPhoto(), screen.Sending);

            Assert.Equal(EmailAlertOutcome.Disabled, result.Outcome);
            Assert.Equal(string.Empty, result.Detail);
            Assert.Empty(sandbox.Drops());
            Assert.Equal(0, screen.Told);
        }

        [Fact]
        public async Task A_fresh_install_sends_nothing()
        {
            // no settings file at all, which is what a clone starts with
            var screen = new Screen();
            EmailAlertResult result = await EmailAlert.SendAnonymousAsync(
                sandbox.GivenAPhoto(), screen.Sending);

            Assert.Equal(EmailAlertOutcome.Disabled, result.Outcome);
            Assert.Equal(0, screen.Told);
        }

        [Fact]
        public async Task Turned_on_but_unfinished_is_reported()
        {
            EmailSettings settings = Sandbox.Deliverable();
            settings.To = string.Empty;
            sandbox.Save(settings);

            var screen = new Screen();
            EmailAlertResult result = await EmailAlert.SendAnonymousAsync(
                sandbox.GivenAPhoto(), screen.Sending);

            Assert.Equal(EmailAlertOutcome.Incomplete, result.Outcome);
            Assert.Equal("Alerts are on, but the email settings are not finished.", result.Detail);
            Assert.Empty(sandbox.Drops());
            Assert.Equal(0, screen.Told);
        }

        [Fact]
        public async Task A_complete_alert_is_written_and_the_screen_is_told()
        {
            sandbox.Save(Sandbox.Deliverable());

            var screen = new Screen();
            EmailAlertResult result = await EmailAlert.SendAnonymousAsync(
                sandbox.GivenAPhoto(), screen.Sending);

            Assert.Equal(EmailAlertOutcome.Sent, result.Outcome);
            Assert.Contains(AppPaths.EmailDropsPath, result.Detail);
            Assert.Single(sandbox.Drops());
            Assert.Equal(1, screen.Told);
        }

        [Fact]
        public async Task The_message_names_the_machine_and_says_what_happened()
        {
            sandbox.Save(Sandbox.Deliverable());

            await EmailAlert.SendAnonymousAsync(sandbox.GivenAPhoto(), null);

            MimeMessage message = sandbox.TheOnlyDrop();

            Assert.Equal("Unrecognised person at " + Environment.MachineName, message.Subject);
            Assert.Contains("did not match any registered image", message.TextBody);
            Assert.Contains(Environment.MachineName, message.TextBody);
            Assert.Contains(Environment.UserName, message.TextBody);
        }

        [Fact]
        public async Task The_photo_is_attached_when_it_was_asked_for()
        {
            EmailSettings settings = Sandbox.Deliverable();
            settings.AttachPhoto = true;
            sandbox.Save(settings);

            await EmailAlert.SendAnonymousAsync(sandbox.GivenAPhoto(), null);

            MimeMessage message = sandbox.TheOnlyDrop();

            Assert.Single(message.Attachments);
            Assert.Contains("The captured frame is attached.", message.TextBody);
        }

        [Fact]
        public async Task The_photo_is_left_off_when_it_was_not_asked_for()
        {
            EmailSettings settings = Sandbox.Deliverable();
            settings.AttachPhoto = false;
            sandbox.Save(settings);

            await EmailAlert.SendAnonymousAsync(sandbox.GivenAPhoto(), null);

            MimeMessage message = sandbox.TheOnlyDrop();

            Assert.Empty(message.Attachments);
            Assert.DoesNotContain("attached", message.TextBody);
        }

        [Fact]
        public async Task The_copy_taken_for_sending_is_cleared_away_afterwards()
        {
            // the frame is captured over the same file every time, so a copy is
            // taken to send it; leaving those behind is what fills a temp folder
            EmailSettings settings = Sandbox.Deliverable();
            settings.AttachPhoto = true;
            sandbox.Save(settings);

            await EmailAlert.SendAnonymousAsync(sandbox.GivenAPhoto(), null);

            Assert.Single(sandbox.TheOnlyDrop().Attachments);
            Assert.Empty(sandbox.AttachmentCopies());
        }

        [Fact]
        public async Task A_photo_that_is_no_longer_there_still_sends_the_alert()
        {
            EmailSettings settings = Sandbox.Deliverable();
            settings.AttachPhoto = true;
            sandbox.Save(settings);

            EmailAlertResult result = await EmailAlert.SendAnonymousAsync(
                sandbox.GivenNoPhoto(), null);

            Assert.Equal(EmailAlertOutcome.Sent, result.Outcome);
            Assert.Empty(sandbox.TheOnlyDrop().Attachments);
        }

        [Fact]
        public async Task A_second_alert_inside_the_cooldown_is_held_back()
        {
            EmailSettings settings = Sandbox.Deliverable();
            settings.CooldownMinutes = 5;
            sandbox.Save(settings);

            await EmailAlert.SendAnonymousAsync(sandbox.GivenAPhoto(), null);

            var screen = new Screen();
            EmailAlertResult held = await EmailAlert.SendAnonymousAsync(
                sandbox.GivenAPhoto(), screen.Sending);

            Assert.Equal(EmailAlertOutcome.Cooling, held.Outcome);
            Assert.Equal("No alert sent; the next one can go out in 5 minutes.", held.Detail);
            Assert.Single(sandbox.Drops());
            Assert.Equal(0, screen.Told);
        }

        [Fact]
        public async Task A_cooldown_of_a_minute_is_worded_as_under_a_minute()
        {
            EmailSettings settings = Sandbox.Deliverable();
            settings.CooldownMinutes = 1;
            sandbox.Save(settings);

            await EmailAlert.SendAnonymousAsync(sandbox.GivenAPhoto(), null);

            EmailAlertResult held = await EmailAlert.SendAnonymousAsync(
                sandbox.GivenAPhoto(), null);

            Assert.Equal("No alert sent; the next one can go out in under a minute.", held.Detail);
        }

        [Fact]
        public async Task No_cooldown_lets_every_alert_through()
        {
            EmailSettings settings = Sandbox.Deliverable();
            settings.CooldownMinutes = 0;
            sandbox.Save(settings);

            await EmailAlert.SendAnonymousAsync(sandbox.GivenAPhoto(), null);
            EmailAlertResult second = await EmailAlert.SendAnonymousAsync(
                sandbox.GivenAPhoto(), null);

            Assert.Equal(EmailAlertOutcome.Sent, second.Outcome);
            Assert.Equal(2, sandbox.Drops().Length);
        }

        [Fact]
        public async Task A_send_that_fails_is_reported_rather_than_thrown()
        {
            // a mail server that is unreachable is a bad moment for the alert,
            // not a reason to take the window down
            sandbox.Save(Sandbox.Deliverable());
            sandbox.BlockTheDropFolder();

            var screen = new Screen();
            EmailAlertResult result = await EmailAlert.SendAnonymousAsync(
                sandbox.GivenAPhoto(), screen.Sending);

            Assert.Equal(EmailAlertOutcome.Failed, result.Outcome);
            Assert.Contains("could not be sent", result.Detail);

            // the screen was already told, since it was settled that a message
            // was going out before the attempt to write it
            Assert.Equal(1, screen.Told);
        }

        [Fact]
        public async Task A_failed_send_does_not_start_the_cooldown()
        {
            // a server that is briefly down must not swallow the next alert too
            EmailSettings settings = Sandbox.Deliverable();
            settings.CooldownMinutes = 5;
            sandbox.Save(settings);

            sandbox.BlockTheDropFolder();

            EmailAlertResult failed = await EmailAlert.SendAnonymousAsync(
                sandbox.GivenAPhoto(), null);

            Assert.Equal(EmailAlertOutcome.Failed, failed.Outcome);

            sandbox.UnblockTheDropFolder();

            EmailAlertResult next = await EmailAlert.SendAnonymousAsync(
                sandbox.GivenAPhoto(), null);

            Assert.Equal(EmailAlertOutcome.Sent, next.Outcome);
            Assert.Single(sandbox.Drops());
        }

        [Fact]
        public async Task A_failed_send_leaves_no_copy_behind()
        {
            EmailSettings settings = Sandbox.Deliverable();
            settings.AttachPhoto = true;
            sandbox.Save(settings);

            sandbox.BlockTheDropFolder();

            await EmailAlert.SendAnonymousAsync(sandbox.GivenAPhoto(), null);

            Assert.Empty(sandbox.AttachmentCopies());
        }

        [Fact]
        public async Task An_alert_survives_having_nobody_to_tell()
        {
            // the callback is optional, and a null one must not be a failed alert
            sandbox.Save(Sandbox.Deliverable());

            EmailAlertResult result = await EmailAlert.SendAnonymousAsync(
                sandbox.GivenAPhoto(), null);

            Assert.Equal(EmailAlertOutcome.Sent, result.Outcome);
        }

        [Fact]
        public async Task A_test_message_on_unfinished_settings_is_refused_with_the_reason()
        {
            // the settings screen shows this line, so it has to be the one that
            // names what is still missing rather than a general refusal
            EmailSettings settings = Sandbox.Deliverable();
            settings.From = string.Empty;

            EmailAlertResult result = await EmailAlert.SendTestAsync(settings);

            Assert.Equal(EmailAlertOutcome.Incomplete, result.Outcome);
            Assert.Equal("Enter the address the alert is sent from.", result.Detail);
            Assert.Empty(sandbox.Drops());
        }

        [Fact]
        public async Task A_test_message_says_it_is_a_test()
        {
            EmailAlertResult result = await EmailAlert.SendTestAsync(Sandbox.Deliverable());

            Assert.Equal(EmailAlertOutcome.Sent, result.Outcome);

            MimeMessage message = sandbox.TheOnlyDrop();

            Assert.Equal("PC User Detection test message", message.Subject);
            Assert.Empty(message.Attachments);
        }

        [Fact]
        public async Task A_test_message_uses_what_is_on_the_screen_not_what_was_saved()
        {
            // the point of the button is proving settings before saving them
            EmailSettings saved = Sandbox.Deliverable();
            saved.Enabled = false;
            sandbox.Save(saved);

            EmailAlertResult result = await EmailAlert.SendTestAsync(Sandbox.Deliverable());

            Assert.Equal(EmailAlertOutcome.Sent, result.Outcome);
            Assert.Single(sandbox.Drops());
        }

        [Fact]
        public async Task A_test_message_ignores_the_cooldown_and_does_not_start_one()
        {
            // the person is standing there asking for it
            EmailSettings settings = Sandbox.Deliverable();
            settings.CooldownMinutes = 5;
            sandbox.Save(settings);

            await EmailAlert.SendTestAsync(settings);
            EmailAlertResult second = await EmailAlert.SendTestAsync(settings);

            Assert.Equal(EmailAlertOutcome.Sent, second.Outcome);

            // and a real alert straight after is still free to go out
            EmailAlertResult alert = await EmailAlert.SendAnonymousAsync(
                sandbox.GivenAPhoto(), null);

            Assert.Equal(EmailAlertOutcome.Sent, alert.Outcome);
        }
    }
}
