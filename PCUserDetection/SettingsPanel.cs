using System;
using System.Drawing;
using System.Globalization;
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

        private const int LabelColumn = 150;
        private const int FormWidth = 620;

        /// <summary>The gap under a row, and the extra a section leaves after its last one.</summary>
        private const int RowGap = 10;
        private const int SectionGap = 4;

        /// <summary>What a status line means, so its colour survives a theme change.</summary>
        private enum Tone { Muted, Good, Bad }

        private readonly Panel form;
        private readonly TableLayoutPanel rows;
        private readonly Panel actions;

        private readonly CheckField enabled =
            new CheckField("Email an alert when a face is not recognised");
        private readonly CheckField attachPhoto =
            new CheckField("Attach the photo that failed the check");

        private readonly ComboField delivery = new ComboField();
        private readonly ComboField security = new ComboField();

        private readonly SpinField cooldown = new SpinField(0, int.MaxValue);
        private readonly TextField from = new TextField();
        private readonly TextField to = new TextField();
        private readonly TextField host = new TextField();
        private readonly SpinField port = new SpinField(1, 65535);
        private readonly TextField username = new TextField();
        private readonly TextField password = new TextField();

        private readonly Label deliveryHint = new Label();
        private readonly StatusLine status = new StatusLine();
        private readonly ToolTip statusTip = new ToolTip();
        private readonly FlatButton save = new FlatButton();
        private readonly FlatButton test = new FlatButton();

        private EmailSettings settings = new EmailSettings();

        public SettingsPanel()
        {
            rows = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                // wide fields on a maximised window would be a stripe of empty
                // white, so the form stops growing at a readable width
                MaximumSize = new Size(FormWidth, 0)
            };
            rows.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, LabelColumn));
            rows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            // the form scrolls and the action bar does not, so the status line and
            // Save are on screen wherever the form has been scrolled to
            form = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(24, 0, 24, 0)
            };

            delivery.Add("SMTP server", EmailDelivery.Smtp).Add("Folder", EmailDelivery.FileDrop);
            delivery.ValueChanged += (s, e) => ShowDeliveryFields();

            security.Add("STARTTLS", EmailSecurity.StartTls)
                    .Add("SSL", EmailSecurity.SslOnConnect)
                    .Add("None", EmailSecurity.None);

            password.UseSystemPasswordChar = true;

            AddHeading("Alerts");
            AddWideRow(enabled);
            AddWideRow(attachPhoto);
            AddRow("Cooldown", WithCaption(cooldown, 76, "minutes between alerts"));
            EndSection();

            AddHeading("Message");
            AddRow("From", Stretch(from));
            AddRow("To", Stretch(to));
            AddHint("Separate several addresses with commas.");
            EndSection();

            AddHeading("Delivery");
            AddRow("Send using", Fixed(delivery, 220));
            AddHint(deliveryHint);
            AddRow("Server", Pair(host, port, 88));
            AddRow("Security", Fixed(security, 220));
            AddRow("Sign in", Pair(username, password, 0));
            AddHint("Leave the username empty for a relay that does not ask.");
            EndSection();

            actions = BuildActionBar();

            form.Controls.Add(rows);
            Controls.Add(form);
            Controls.Add(actions);

            Reload();
        }

        /// <summary>
        /// Reads the saved settings back onto the screen. Called every time the
        /// screen is opened, so a file edited by hand shows up without a restart.
        /// </summary>
        public void Reload()
        {
            string problem;
            settings = EmailSettings.Load(out problem);

            enabled.Checked = settings.Enabled;
            attachPhoto.Checked = settings.AttachPhoto;
            delivery.SetValueQuietly(settings.Delivery);
            security.SetValueQuietly(settings.Security);

            cooldown.Text = settings.CooldownMinutes.ToString();
            from.Text = settings.From;
            to.Text = settings.To;
            host.Text = settings.Host;
            port.Text = settings.Port.ToString();
            username.Text = settings.Username;
            password.Text = string.IsNullOrEmpty(settings.ProtectedPassword) ? string.Empty : PasswordPlaceholder;

            ShowDeliveryFields();

            if (problem != null)
            {
                // the fields above are the defaults rather than anything that was
                // saved, and "Alerts are off." over a file that could not be read
                // would report a choice nobody made
                SetStatus(problem + " The screen is showing the defaults instead; saving replaces the file with them.",
                    Tone.Bad);
            }
            else
            {
                SetStatus(settings.Enabled ? DescribeState() : "Alerts are off.", Tone.Muted);
            }
        }

        /// <summary>
        /// What the Send using list is showing. A settings file naming a delivery
        /// the app does not have is turned away by <see cref="EmailSettings.Load"/>
        /// before it reaches the list, so the fallback here is for a delivery
        /// added to the enum without a row being added alongside it: the list
        /// then reads as the one it was built to show first rather than throwing
        /// in a constructor, where there is no screen yet to say so on.
        /// </summary>
        private EmailDelivery SelectedDelivery
        {
            get { return delivery.ValueOr(EmailDelivery.Smtp); }
        }

        /// <summary>What the Security list is showing. See <see cref="SelectedDelivery"/>.</summary>
        private EmailSecurity SelectedSecurity
        {
            get { return security.ValueOr(EmailSecurity.StartTls); }
        }

        /// <summary>Re-reads the palette. Called by the window after the theme changes.</summary>
        public void ApplyTheme()
        {
            BackColor = Theme.Background;
            form.BackColor = Theme.Background;
            rows.BackColor = Theme.Background;
            actions.BackColor = Theme.Surface;

            ApplyTheme(rows);

            Theme.StylePrimary(save);
            Theme.StyleGhost(test);

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
                // a spin box is a text field with a step column, so this covers both
                var field = control as TextField;

                if (field != null)
                {
                    field.ApplyTheme();
                    continue;
                }

                var combo = control as ComboField;

                if (combo != null)
                {
                    combo.ApplyTheme();
                    continue;
                }

                var check = control as CheckField;

                if (check != null)
                {
                    check.BackColor = Theme.Background;
                    check.Invalidate();
                    continue;
                }

                var label = control as Label;

                if (label != null)
                {
                    // a hint is marked as one when it is built, so it can stay
                    // muted while the headings and row labels are at full strength
                    label.ForeColor = IsHint(label) ? Theme.TextMuted : Theme.Text;
                    label.Invalidate();
                    continue;
                }

                ApplyTheme(control);
            }
        }

        #region Rows

        /// <summary>
        /// A section title with a hairline under it. The rule is what separates
        /// one part of the form from the next, in place of the card panels the
        /// rows used to sit in.
        /// </summary>
        private void AddHeading(string text)
        {
            var heading = new Label
            {
                Text = text,
                AutoSize = false,
                Dock = DockStyle.Fill,
                Font = Theme.Section,
                Height = 23,
                TextAlign = ContentAlignment.TopLeft,
                // the air over a heading is left by the section above it, in
                // EndSection; what is set here is the gap under the rule
                Margin = new Padding(0, 0, 0, 12)
            };
            heading.Paint += Heading_Paint;

            rows.Controls.Add(heading, 0, rows.RowCount);
            rows.SetColumnSpan(heading, 2);
            rows.RowCount++;
        }

        private static void Heading_Paint(object sender, PaintEventArgs e)
        {
            var heading = (Label)sender;

            using (var pen = new Pen(Theme.Border))
            {
                e.Graphics.DrawLine(pen, 0, heading.Height - 1, heading.Width, heading.Height - 1);
            }
        }

        /// <summary>A row with a label in the left column and its control beside it.</summary>
        private void AddRow(string text, Control field)
        {
            var label = new Label
            {
                Text = text,
                AutoSize = true,
                Font = Theme.Body,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 0, 12, RowGap)
            };

            rows.Controls.Add(label, 0, rows.RowCount);
            rows.Controls.Add(field, 1, rows.RowCount);
            rows.RowCount++;
        }

        /// <summary>
        /// A control that stands in the field column with nothing labelling it,
        /// which is what a check box does: its wording is its own.
        /// </summary>
        private void AddWideRow(Control field)
        {
            field.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            field.Margin = new Padding(0, 0, 0, RowGap);

            rows.Controls.Add(field, 1, rows.RowCount);
            rows.RowCount++;
        }

        private void AddHint(string text)
        {
            AddHint(new Label { Text = text });
        }

        /// <summary>Adds a line of explanation on its own, under the row above it.</summary>
        private void AddHint(Label hint)
        {
            hint.Tag = HintTag;
            hint.AutoSize = true;
            hint.Font = Theme.Small;
            hint.Margin = new Padding(0, 0, 0, RowGap);
            hint.MaximumSize = new Size(FormWidth - LabelColumn, 0);

            rows.Controls.Add(hint, 1, rows.RowCount);
            rows.RowCount++;
        }

        /// <summary>
        /// Puts the extra air a section leaves under its last row. Sections are
        /// closed by hand rather than by the next heading, so that the last one
        /// is not left sitting against the action bar.
        /// </summary>
        private void EndSection()
        {
            Control last = rows.GetControlFromPosition(1, rows.RowCount - 1);
            if (last == null) return;

            Padding margin = last.Margin;
            last.Margin = new Padding(margin.Left, margin.Top, margin.Right, margin.Bottom + SectionGap);
        }

        /// <summary>A field that fills the width the form leaves it.</summary>
        private static Control Stretch(Control field)
        {
            field.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            field.Margin = new Padding(0, 0, 0, RowGap);
            return field;
        }

        /// <summary>
        /// A field of a set width, for a list that does not need the room. It is
        /// held in a row that fills the column, because the table would otherwise
        /// size a field that is not stretched to fit its own idea of it.
        /// </summary>
        private static Control Fixed(Control field, int width)
        {
            field.Dock = DockStyle.Left;
            field.Width = width;

            var row = new Panel
            {
                Height = field.Height,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Margin = new Padding(0, 0, 0, RowGap)
            };
            row.Controls.Add(field);

            return row;
        }

        /// <summary>
        /// Two fields side by side, eight pixels apart. A width of zero shares
        /// the room evenly between them; anything else pins the second one.
        /// </summary>
        private static Control Pair(Control first, Control second, int secondWidth)
        {
            var pair = new TableLayoutPanel
            {
                ColumnCount = 2,
                RowCount = 1,
                Height = TextField.FieldHeight,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Margin = new Padding(0, 0, 0, RowGap)
            };

            if (secondWidth == 0)
            {
                pair.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                pair.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                first.Margin = new Padding(0, 0, 4, 0);
                second.Margin = new Padding(4, 0, 0, 0);
            }
            else
            {
                pair.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                pair.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, secondWidth + 8));
                first.Margin = new Padding(0);
                second.Margin = new Padding(8, 0, 0, 0);
            }

            first.Dock = DockStyle.Fill;
            second.Dock = DockStyle.Fill;

            pair.Controls.Add(first, 0, 0);
            pair.Controls.Add(second, 1, 0);

            return pair;
        }

        /// <summary>A narrow field with a few words of unit after it.</summary>
        private static Control WithCaption(Control field, int width, string caption)
        {
            var note = new Label
            {
                Text = caption,
                Tag = HintTag,
                Dock = DockStyle.Fill,
                Font = Theme.Control,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0)
            };

            field.Dock = DockStyle.Left;
            field.Width = width;

            var row = new Panel
            {
                Height = TextField.FieldHeight,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Margin = new Padding(0, 0, 0, RowGap)
            };
            row.Controls.Add(note);
            row.Controls.Add(field);

            return row;
        }

        /// <summary>
        /// The bar along the foot of the screen: what happened on the left, and
        /// what can be done about it on the right.
        /// </summary>
        private Panel BuildActionBar()
        {
            save.Text = "&Save";
            save.Size = new Size(104, 32);
            save.Margin = new Padding(0);
            save.Click += Save_Click;

            test.Text = "Send test email";
            test.Size = new Size(132, 32);
            test.Margin = new Padding(0, 0, 8, 0);
            test.Click += Test_Click;

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Size = new Size(244, 32)
            };
            buttons.Controls.Add(save);
            buttons.Controls.Add(test);

            status.Dock = DockStyle.Fill;

            var bar = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                Padding = new Padding(24, 14, 24, 14)
            };
            bar.Paint += ActionBar_Paint;
            bar.Controls.Add(status);
            bar.Controls.Add(buttons);

            return bar;
        }

        /// <summary>Separates the action bar from the form above it.</summary>
        private void ActionBar_Paint(object sender, PaintEventArgs e)
        {
            var bar = (Panel)sender;

            using (var pen = new Pen(Theme.Border))
            {
                e.Graphics.DrawLine(pen, 0, 0, bar.Width, 0);
            }
        }

        // the mark put on a label as it is built, so the theme walk can tell a
        // hint, which is muted, from a heading or a row label, which are not
        private const string HintTag = "hint";

        private static bool IsHint(Label label)
        {
            return HintTag.Equals(label.Tag as string);
        }

        #endregion

        #region Saving

        private void Save_Click(object sender, EventArgs e)
        {
            EmailSettings entered;

            if (!TryCollect(out entered)) return;

            settings = entered;

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
            EmailSettings entered;

            if (!TryCollect(out entered)) return;

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

        /// <summary>
        /// Reads the screen into settings, without saving them. Returns false,
        /// having said why on the status line, when something on screen cannot
        /// be used: a number that will not parse, or a password Windows will not
        /// encrypt. What was typed is left alone to be corrected rather than
        /// being quietly swapped back for the value it was replacing, or worse,
        /// dropped on the way to the file.
        /// </summary>
        private bool TryCollect(out EmailSettings collected)
        {
            collected = null;

            int cooldownMinutes;

            if (!TryReadNumber(cooldown, 0, int.MaxValue,
                    "The cooldown has to be a whole number of minutes, and cannot be negative.",
                    out cooldownMinutes))
            {
                return false;
            }

            // the port is only read when it is in play; a folder drop leaves the
            // box disabled, and refusing to save over something that cannot be
            // reached to be corrected would be a dead end
            int portNumber = settings.Port;

            if (SelectedDelivery == EmailDelivery.Smtp &&
                !TryReadNumber(port, 1, 65535,
                    "The port has to be a whole number between 1 and 65535.", out portNumber))
            {
                return false;
            }

            collected = new EmailSettings
            {
                Enabled = enabled.Checked,
                AttachPhoto = attachPhoto.Checked,
                Delivery = SelectedDelivery,
                Security = SelectedSecurity,
                CooldownMinutes = cooldownMinutes,
                From = from.Text.Trim(),
                To = to.Text.Trim(),
                Host = host.Text.Trim(),
                Port = portNumber,
                Username = username.Text.Trim(),
                // the encrypted blob is carried across untouched unless the person
                // typed over the placeholder, so saving does not need the password
                // to have been on screen in the clear
                ProtectedPassword = settings.ProtectedPassword
            };

            if (password.Text != PasswordPlaceholder)
            {
                try
                {
                    collected.Password = password.Text;
                }
                catch (Exception ex)
                {
                    // Windows would not encrypt it, and keeping it in the clear
                    // is not on offer. Going ahead would save a set of settings
                    // with no password in them while the box on screen still
                    // showed one, so nothing is saved and the person is told.
                    Console.WriteLine(ex);
                    SetStatus("The password could not be encrypted, so nothing was saved. " + ex.Message,
                        Tone.Bad);
                    password.Highlight();

                    collected = null;
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Reads a whole number in the given range out of a box. Anything else
        /// puts <paramref name="problem"/> on the status line and leaves the
        /// caret in the box that has to be fixed.
        /// </summary>
        private bool TryReadNumber(TextField box, int least, int most, string problem, out int value)
        {
            // the invariant culture, so a thousands separator or a stray space is
            // a mistake to point at rather than something a machine abroad accepts
            bool read = int.TryParse(box.Text.Trim(), NumberStyles.Integer,
                CultureInfo.InvariantCulture, out value);

            if (read && value >= least && value <= most) return true;

            SetStatus(problem, Tone.Bad);
            box.Highlight();

            return false;
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
                return "Alerts are on, written to " + AppPaths.EmailDropsPath + ".";
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
            bool smtp = SelectedDelivery == EmailDelivery.Smtp;

            host.Enabled = smtp;
            port.Enabled = smtp;
            username.Enabled = smtp;
            password.Enabled = smtp;
            security.Enabled = smtp;

            deliveryHint.Text = smtp
                ? "The alert is sent through the server below."
                : "The alert is written to " + AppPaths.EmailDropsPath +
                  " as an .eml file instead of being sent. Useful for trying the alert out without a mail account.";
        }

        private void SetStatus(string text, Tone tone)
        {
            // the line resolves its own colour while painting, so a status
            // already on screen takes the new palette when the theme changes
            status.Set(text, ToneOf(tone));

            // the bar is one row and an error can be longer than the room the
            // buttons leave it, so the whole of it stays reachable
            statusTip.SetToolTip(status, status.FullText);
        }

        private static StatusTone ToneOf(Tone tone)
        {
            if (tone == Tone.Good) return StatusTone.Good;
            if (tone == Tone.Bad) return StatusTone.Bad;
            return StatusTone.Neutral;
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing) statusTip.Dispose();
            base.Dispose(disposing);
        }
    }
}
