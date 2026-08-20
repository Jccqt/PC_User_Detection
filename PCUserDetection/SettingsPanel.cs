using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PCUserDetection
{
    /// <summary>
    /// The Settings screen: where the email alert is turned on and pointed at a
    /// mail server. It reads and writes <see cref="EmailSettings"/> itself, and
    /// reports what happened on its own status line rather than through the
    /// window's, since the camera footer is not on screen here.
    /// </summary>
    internal class SettingsPanel : Panel
    {
        /// <summary>
        /// Stands in for a password that is already saved. The stored password is
        /// encrypted and is never put on screen; leaving this untouched keeps it,
        /// and typing over it replaces it.
        /// </summary>
        private const string PasswordPlaceholder = "••••••••";

        private const int LabelColumn = 140;
        private const int FieldWidth = 660;

        /// <summary>What a status line means, so its colour survives a theme change.</summary>
        private enum Tone { Muted, Good, Bad }

        private readonly TableLayoutPanel rows;

        private readonly ChoiceStrip<bool> enabled = new ChoiceStrip<bool>();
        private readonly ChoiceStrip<bool> attachPhoto = new ChoiceStrip<bool>();
        private readonly ChoiceStrip<EmailDelivery> delivery = new ChoiceStrip<EmailDelivery>();
        private readonly ChoiceStrip<EmailSecurity> security = new ChoiceStrip<EmailSecurity>();

        private readonly TextBox cooldown = new TextBox();
        private readonly TextBox from = new TextBox();
        private readonly TextBox to = new TextBox();
        private readonly TextBox host = new TextBox();
        private readonly TextBox port = new TextBox();
        private readonly TextBox username = new TextBox();
        private readonly TextBox password = new TextBox();

        private readonly Label deliveryHint = new Label();
        private readonly Label status = new Label();
        private readonly Button save = new Button();
        private readonly Button test = new Button();

        private EmailSettings settings = new EmailSettings();
        private Tone statusTone = Tone.Muted;

        public SettingsPanel()
        {
            AutoScroll = true;

            rows = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                // wide fields on a maximised window would be a stripe of empty
                // white, so the form stops growing at a readable width
                MaximumSize = new Size(FieldWidth, 0)
            };
            rows.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, LabelColumn));
            rows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            enabled.Add("On", true).Add("Off", false);
            attachPhoto.Add("Yes", true).Add("No", false);

            delivery.Add("SMTP server", EmailDelivery.Smtp).Add("Folder", EmailDelivery.FileDrop);
            delivery.ValueChanged += (s, e) => ShowDeliveryFields();

            security.Add("STARTTLS", EmailSecurity.StartTls)
                    .Add("SSL", EmailSecurity.SslOnConnect)
                    .Add("None", EmailSecurity.None);

            password.UseSystemPasswordChar = true;

            AddHeading("Alerts");
            AddRow("Email an alert", enabled);
            AddRow("Attach the photo", attachPhoto);
            AddRow("Cooldown", Frame(cooldown), "Minutes to wait before another alert may be sent.");

            AddHeading("Message");
            AddRow("From", Frame(from), "The mailbox the alert is sent from.");
            AddRow("To", Frame(to), "Who receives it. Separate several addresses with commas.");

            AddHeading("Delivery");
            AddRow("Send using", delivery);
            AddHint(deliveryHint);
            AddRow("Server", Frame(host));
            AddRow("Port", Frame(port));
            AddRow("Security", security);
            AddRow("Username", Frame(username), "Leave empty for a relay that does not ask.");
            AddRow("Password", Frame(password));

            AddActions();

            Controls.Add(rows);

            Reload();
        }

        /// <summary>
        /// Reads the saved settings back onto the screen. Called every time the
        /// screen is opened, so a file edited by hand shows up without a restart.
        /// </summary>
        public void Reload()
        {
            settings = EmailSettings.Load();

            enabled.Value = settings.Enabled;
            attachPhoto.Value = settings.AttachPhoto;
            delivery.Value = settings.Delivery;
            security.Value = settings.Security;

            cooldown.Text = settings.CooldownMinutes.ToString();
            from.Text = settings.From;
            to.Text = settings.To;
            host.Text = settings.Host;
            port.Text = settings.Port.ToString();
            username.Text = settings.Username;
            password.Text = string.IsNullOrEmpty(settings.ProtectedPassword) ? string.Empty : PasswordPlaceholder;

            ShowDeliveryFields();
            SetStatus(settings.Enabled ? DescribeState() : "Alerts are off.", Tone.Muted);
        }

        /// <summary>Re-reads the palette. Called by the window after the theme changes.</summary>
        public void ApplyTheme()
        {
            BackColor = Theme.Background;
            rows.BackColor = Theme.Background;

            ApplyTheme(rows);

            enabled.ApplyTheme();
            attachPhoto.ApplyTheme();
            delivery.ApplyTheme();
            security.ApplyTheme();

            Theme.StylePrimary(save);
            Theme.StyleGhost(test);

            status.ForeColor = StatusColor();

            Invalidate(true);
        }

        /// <summary>
        /// Colours the labels and fields the rows are built from. They are walked
        /// rather than kept in a list, so a row added later is themed too.
        /// </summary>
        private void ApplyTheme(Control parent)
        {
            foreach (Control control in parent.Controls)
            {
                var box = control as TextBox;

                if (box != null)
                {
                    box.BackColor = Theme.Surface;
                    box.ForeColor = Theme.Text;
                    continue;
                }

                var label = control as Label;

                if (label != null)
                {
                    // headings are marked when they are built, so they can stay
                    // at full strength while the rest of the text is muted
                    label.ForeColor = IsHeading(label) ? Theme.Text : Theme.TextMuted;
                    continue;
                }

                var panel = control as Panel;

                if (panel != null)
                {
                    // only the frame around a text box is a raised surface; the
                    // panels that group buttons have to disappear into the page
                    panel.BackColor = IsField(panel) ? Theme.Surface : Theme.Background;
                    ApplyTheme(panel);
                }
            }
        }

        #region Rows

        private void AddHeading(string text)
        {
            var heading = new Label
            {
                Text = text,
                Tag = HeadingTag,
                AutoSize = true,
                Font = Theme.Nav,
                Margin = new Padding(0, rows.RowCount == 0 ? 4 : 22, 0, 6)
            };

            rows.Controls.Add(heading, 0, rows.RowCount);
            rows.SetColumnSpan(heading, 2);
            rows.RowCount++;
        }

        private void AddRow(string text, Control field)
        {
            AddRow(text, field, null);
        }

        private void AddRow(string text, Control field, string hint)
        {
            var label = new Label
            {
                Text = text,
                AutoSize = true,
                Font = Theme.Body,
                Margin = new Padding(0, 11, 12, 0)
            };

            rows.Controls.Add(label, 0, rows.RowCount);
            rows.Controls.Add(field, 1, rows.RowCount);
            rows.RowCount++;

            if (hint == null) return;

            var note = new Label
            {
                Text = hint,
                AutoSize = true,
                Font = Theme.Small,
                Margin = new Padding(2, 0, 0, 8)
            };

            rows.Controls.Add(note, 1, rows.RowCount);
            rows.RowCount++;
        }

        /// <summary>Adds a line of explanation on its own, under the row above it.</summary>
        private void AddHint(Label hint)
        {
            hint.AutoSize = true;
            hint.Font = Theme.Small;
            hint.Margin = new Padding(2, 0, 0, 8);
            hint.MaximumSize = new Size(FieldWidth - LabelColumn, 0);

            rows.Controls.Add(hint, 1, rows.RowCount);
            rows.RowCount++;
        }

        private void AddActions()
        {
            save.Text = "Save";
            save.Width = 120;
            save.Margin = new Padding(0, 22, 8, 0);
            save.Click += Save_Click;

            test.Text = "Send test email";
            test.Width = 150;
            test.Margin = new Padding(0, 22, 0, 0);
            test.Click += Test_Click;

            var actions = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false,
                Margin = new Padding(0)
            };
            actions.Controls.Add(save);
            actions.Controls.Add(test);

            rows.Controls.Add(actions, 1, rows.RowCount);
            rows.RowCount++;

            status.AutoSize = true;
            status.Font = Theme.Small;
            status.Margin = new Padding(2, 12, 0, 12);
            status.MaximumSize = new Size(FieldWidth - LabelColumn, 0);

            rows.Controls.Add(status, 1, rows.RowCount);
            rows.RowCount++;
        }

        /// <summary>
        /// Puts a text box in a panel that paints the border. A text box draws its
        /// own in a system colour that no property changes, so it is given none
        /// and the panel around it draws one from the palette instead.
        /// </summary>
        private static Panel Frame(TextBox box)
        {
            box.BorderStyle = BorderStyle.None;
            box.Font = Theme.Body;
            box.Dock = DockStyle.Fill;

            var frame = new Panel
            {
                Tag = FieldTag,
                Height = 32,
                Dock = DockStyle.Top,
                Margin = new Padding(0, 4, 0, 4),
                // the text box keeps its own height, so the top padding is what
                // centres it rather than the dock doing it
                Padding = new Padding(9, 7, 9, 0)
            };
            frame.Paint += Frame_Paint;
            frame.Controls.Add(box);

            return frame;
        }

        private static void Frame_Paint(object sender, PaintEventArgs e)
        {
            var frame = (Panel)sender;

            using (var pen = new Pen(Theme.Border))
            {
                e.Graphics.DrawRectangle(pen, 0, 0, frame.Width - 1, frame.Height - 1);
            }
        }

        // marks put on controls as they are built, so the theme walk can tell a
        // heading from a hint and a field frame from a plain grouping panel
        private const string HeadingTag = "heading";
        private const string FieldTag = "field";

        private static bool IsHeading(Label label)
        {
            return HeadingTag.Equals(label.Tag as string);
        }

        private static bool IsField(Panel panel)
        {
            return FieldTag.Equals(panel.Tag as string);
        }

        #endregion

        #region Saving

        private void Save_Click(object sender, EventArgs e)
        {
            settings = Collect();

            try
            {
                settings.Save();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                SetStatus("The settings could not be saved. " + ex.Message, Tone.Bad);
                return;
            }

            // the saved password is back to being a blob, so the box goes back to
            // standing in for it rather than holding what was typed
            password.Text = string.IsNullOrEmpty(settings.ProtectedPassword) ? string.Empty : PasswordPlaceholder;

            string problem = settings.Enabled ? settings.Describe() : null;

            if (problem != null) SetStatus("Saved, but alerts will not send yet. " + problem, Tone.Bad);
            else SetStatus("Saved. " + DescribeState(), Tone.Good);
        }

        private async void Test_Click(object sender, EventArgs e)
        {
            EmailSettings entered = Collect();

            SetStatus("Sending...", Tone.Muted);
            test.Enabled = false;
            save.Enabled = false;

            try
            {
                EmailAlertResult result = await EmailAlert.SendTestAsync(entered);
                SetStatus(result.Detail, result.Outcome == EmailAlertOutcome.Sent ? Tone.Good : Tone.Bad);
            }
            finally
            {
                test.Enabled = true;
                save.Enabled = true;
            }
        }

        /// <summary>Reads the screen into settings, without saving them.</summary>
        private EmailSettings Collect()
        {
            var collected = new EmailSettings
            {
                Enabled = enabled.Value,
                AttachPhoto = attachPhoto.Value,
                Delivery = delivery.Value,
                Security = security.Value,
                CooldownMinutes = ParseNumber(cooldown, settings.CooldownMinutes),
                From = from.Text.Trim(),
                To = to.Text.Trim(),
                Host = host.Text.Trim(),
                Port = ParseNumber(port, settings.Port),
                Username = username.Text.Trim(),
                // the encrypted blob is carried across untouched unless the person
                // typed over the placeholder, so saving does not need the password
                // to have been on screen in the clear
                ProtectedPassword = settings.ProtectedPassword
            };

            if (password.Text != PasswordPlaceholder) collected.Password = password.Text;

            return collected;
        }

        private static int ParseNumber(TextBox box, int fallback)
        {
            int parsed;
            return int.TryParse(box.Text.Trim(), out parsed) ? parsed : fallback;
        }

        #endregion

        #region Status

        /// <summary>Turns the settings into the sentence shown when nothing has just happened.</summary>
        private string DescribeState()
        {
            string problem = settings.Describe();

            if (problem != null) return "Alerts are on, but " + LowerFirst(problem);

            if (settings.Delivery == EmailDelivery.FileDrop)
            {
                return "Alerts are on, written to " + AppPaths.EmailDrops + ".";
            }

            return "Alerts are on, sent to " + settings.To + ".";
        }

        private static string LowerFirst(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return char.ToLower(text[0]) + text.Substring(1);
        }

        private void ShowDeliveryFields()
        {
            bool smtp = delivery.Value == EmailDelivery.Smtp;

            host.Enabled = smtp;
            port.Enabled = smtp;
            username.Enabled = smtp;
            password.Enabled = smtp;

            foreach (Control control in security.Controls) control.Enabled = smtp;

            deliveryHint.Text = smtp
                ? "The alert is sent through the server below."
                : "The alert is written to " + AppPaths.EmailDrops +
                  " as an .eml file instead of being sent. Useful for trying the alert out without a mail account.";
        }

        private void SetStatus(string text, Tone tone)
        {
            statusTone = tone;
            status.Text = text;
            status.ForeColor = StatusColor();
        }

        /// <summary>
        /// Resolved on demand rather than stored, so a status already on screen
        /// takes the new palette when the theme changes.
        /// </summary>
        private Color StatusColor()
        {
            if (statusTone == Tone.Good) return Theme.Success;
            if (statusTone == Tone.Bad) return Theme.Danger;
            return Theme.TextMuted;
        }

        #endregion
    }
}
