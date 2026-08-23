using System;
using System.IO;
using System.Linq;
using MimeKit;
using PCUserDetection;
using Xunit;

namespace PCUserDetection.Tests
{
    /// <summary>
    /// Covers the step that turns the settings and an alert into the message
    /// that goes out. It builds a message and returns it without sending it, so
    /// everything here runs with no server and no account.
    /// </summary>
    public class EmailComposerTests : IDisposable
    {
        /// <summary>A folder of this test's own, for the attachment cases.</summary>
        private readonly string workspace;

        public EmailComposerTests()
        {
            workspace = Path.Combine(Path.GetTempPath(),
                "PCUserDetection.Tests", Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(workspace);
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(workspace, true);
            }
            catch (Exception)
            {
                // a folder under temp that will not go is Windows' to clear up;
                // failing the test over it would report the wrong thing
            }
        }

        private static EmailSettings Settings(string from, string to)
        {
            return new EmailSettings { From = from, To = to };
        }

        private static EmailMessage Alert()
        {
            return new EmailMessage
            {
                Subject = "Unrecognised person at DESKTOP-1",
                Body = "Somebody who is not you was at the machine."
            };
        }

        /// <summary>Writes a file to attach, and returns the path to it.</summary>
        private string GivenAFile(string name, string content)
        {
            string path = Path.Combine(workspace, name);
            File.WriteAllText(path, content);
            return path;
        }

        [Fact]
        public void Sender_and_recipient_come_from_the_settings()
        {
            MimeMessage mime = EmailComposer.Compose(
                Settings("watcher@example.com", "owner@example.com"), Alert());

            Assert.Equal("watcher@example.com", Assert.Single(mime.From.Mailboxes).Address);
            Assert.Equal("owner@example.com", Assert.Single(mime.To.Mailboxes).Address);
        }

        [Fact]
        public void Subject_and_body_come_from_the_alert()
        {
            MimeMessage mime = EmailComposer.Compose(
                Settings("watcher@example.com", "owner@example.com"), Alert());

            Assert.Equal("Unrecognised person at DESKTOP-1", mime.Subject);
            Assert.Equal("Somebody who is not you was at the machine.", mime.TextBody);
        }

        [Fact]
        public void Several_recipients_can_be_separated_by_commas()
        {
            MimeMessage mime = EmailComposer.Compose(
                Settings("watcher@example.com", "owner@example.com,phone@example.com"), Alert());

            Assert.Equal(
                new[] { "owner@example.com", "phone@example.com" },
                mime.To.Mailboxes.Select(mailbox => mailbox.Address).ToArray());
        }

        [Fact]
        public void Space_around_a_recipient_is_ignored()
        {
            // the address list is typed into a text box, and people space it out.
            // MimeKit tolerates the space too, so this pins the behaviour rather
            // than the Trim that Compose does before handing it over
            MimeMessage mime = EmailComposer.Compose(
                Settings("watcher@example.com", " owner@example.com , phone@example.com "), Alert());

            Assert.Equal(
                new[] { "owner@example.com", "phone@example.com" },
                mime.To.Mailboxes.Select(mailbox => mailbox.Address).ToArray());
        }

        [Fact]
        public void A_stray_comma_does_not_add_an_empty_recipient()
        {
            // a trailing comma is what a half-finished edit leaves behind
            MimeMessage mime = EmailComposer.Compose(
                Settings("watcher@example.com", "owner@example.com,,"), Alert());

            Assert.Equal("owner@example.com", Assert.Single(mime.To.Mailboxes).Address);
        }

        [Fact]
        public void The_photo_is_attached_when_there_is_one()
        {
            string photo = GivenAFile("Anonymous.jpeg", "not really a jpeg");

            EmailMessage alert = Alert();
            alert.AttachmentPath = photo;

            MimeMessage mime = EmailComposer.Compose(
                Settings("watcher@example.com", "owner@example.com"), alert);

            MimeEntity attachment = Assert.Single(mime.Attachments);
            Assert.Equal("Anonymous.jpeg", attachment.ContentDisposition.FileName);
        }

        [Fact]
        public void No_attachment_path_means_no_attachment()
        {
            MimeMessage mime = EmailComposer.Compose(
                Settings("watcher@example.com", "owner@example.com"), Alert());

            Assert.Empty(mime.Attachments);
            Assert.Equal("Somebody who is not you was at the machine.", mime.TextBody);
        }

        [Fact]
        public void A_photo_that_is_no_longer_there_still_sends_the_alert()
        {
            // the copy is taken before the send and cleared away after it, so a
            // path that has gone is a race worth surviving: the alert saying
            // somebody was there matters more than the picture of them
            EmailMessage alert = Alert();
            alert.AttachmentPath = Path.Combine(workspace, "gone.jpeg");

            MimeMessage mime = EmailComposer.Compose(
                Settings("watcher@example.com", "owner@example.com"), alert);

            Assert.Empty(mime.Attachments);
            Assert.Equal("Somebody who is not you was at the machine.", mime.TextBody);
        }
    }
}
