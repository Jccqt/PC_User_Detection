using System;
using System.Drawing;
using System.Windows.Forms;

namespace PCUserDetection
{
    /// <summary>
    /// A single line of text to type in, drawn from the palette: a 30 pixel
    /// field with 3 pixel corners and a hairline border that turns accent while
    /// it holds the focus.
    /// </summary>
    /// <remarks>
    /// A text box draws its own border in a system colour that no property
    /// changes, so it is given none and this panel draws one around it instead.
    /// The box keeps its own height, which is what the top padding centres.
    /// </remarks>
    internal class TextField : Panel
    {
        private const int Radius = 3;

        /// <summary>The height of the field, matching a combo and a button row.</summary>
        public const int FieldHeight = 30;

        private readonly TextBox box = new TextBox();

        public TextField()
        {
            Height = FieldHeight;

            box.BorderStyle = BorderStyle.None;
            box.Font = Theme.Control;
            box.Dock = DockStyle.Fill;
            box.GotFocus += (s, e) => Invalidate();
            box.LostFocus += (s, e) => Invalidate();

            Padding = new Padding(9, 7, 9, 0);
            Controls.Add(box);
        }

        /// <summary>The box itself, for a field that builds on this one.</summary>
        protected TextBox Box
        {
            get { return box; }
        }

        public override string Text
        {
            get { return box.Text; }
            set { box.Text = value; }
        }

        /// <summary>Hides what is typed, for a password.</summary>
        public bool UseSystemPasswordChar
        {
            get { return box.UseSystemPasswordChar; }
            set { box.UseSystemPasswordChar = value; }
        }

        /// <summary>
        /// Puts the caret in this field and selects what is in it, for pointing
        /// at the one value that could not be used.
        /// </summary>
        public void Highlight()
        {
            box.Focus();
            box.SelectAll();
        }

        /// <summary>Re-reads the palette. Called after the theme changes.</summary>
        public virtual void ApplyTheme()
        {
            box.BackColor = Enabled ? Theme.Surface : Theme.Background;
            box.ForeColor = Enabled ? Theme.Text : Theme.TextMuted;
            Invalidate();
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            ApplyTheme();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Rectangle bounds = ClientRectangle;
            bounds.Width -= 1;
            bounds.Height -= 1;

            Rounded.Fill(e.Graphics, bounds, Radius,
                Enabled ? Theme.Surface : Theme.Background,
                box.Focused ? Theme.Accent : Theme.Border);
        }
    }
}
