using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PCUserDetection
{
    /// <summary>
    /// A drop-down list drawn in the colours of the theme: a 30 pixel field with
    /// 3 pixel corners, a hairline border and a chevron.
    /// </summary>
    /// <remarks>
    /// A combo box always paints its own light border and drop arrow, and no
    /// property turns them off. So it is put in a panel smaller than itself,
    /// which clips both away, and this frame paints the border and the chevron
    /// from the palette instead. The window needs the trick four times over —
    /// Appearance, Camera, Send using and Security — which is why it lives here
    /// rather than in the screen that first wanted it.
    /// </remarks>
    internal class ComboField : Panel
    {
        private const int Radius = 3;

        /// <summary>How much of the right-hand side is kept clear for the chevron.</summary>
        private const int ChevronColumn = 26;

        /// <summary>The height of the field, and of the combo box hidden behind it.</summary>
        public const int FieldHeight = 30;

        /// <summary>How much shorter than the field the clip is, top and bottom.</summary>
        private const int ClipInset = 4;

        private readonly Panel clip;
        private readonly ComboBox combo;

        /// <summary>What each row stands for, in the order the rows were added.</summary>
        private readonly List<object> values = new List<object>();

        private bool quiet;

        public ComboField()
        {
            Height = FieldHeight;
            Cursor = Cursors.Hand;

            combo = new ComboBox
            {
                DrawMode = DrawMode.OwnerDrawFixed,
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                Font = Theme.Control,
                ItemHeight = FieldHeight - 2
            };
            combo.DrawItem += Combo_DrawItem;
            combo.SelectedIndexChanged += Combo_SelectedIndexChanged;
            combo.GotFocus += (s, e) => Invalidate();
            combo.LostFocus += (s, e) => Invalidate();

            // sized rather than docked, because a panel clips its children to
            // its own bounds and not to its padding
            clip = new Panel { Cursor = Cursors.Hand };
            clip.Click += Field_Click;

            clip.Controls.Add(combo);
            Controls.Add(clip);

            Click += Field_Click;
            Arrange();
        }

        /// <summary>Raised when a different row becomes the one in force.</summary>
        public event EventHandler ValueChanged;

        /// <summary>The row in force, counted from the top. -1 when there is none.</summary>
        public int SelectedIndex
        {
            get { return combo.SelectedIndex; }
            set { combo.SelectedIndex = value; }
        }

        /// <summary>
        /// What the row in force stands for. Setting it to something no row
        /// carries leaves the list where it was.
        /// </summary>
        public object Value
        {
            get { return combo.SelectedIndex < 0 ? null : values[combo.SelectedIndex]; }
            set
            {
                int index = values.FindIndex(candidate => Equals(candidate, value));
                if (index >= 0) combo.SelectedIndex = index;
            }
        }

        /// <summary>Adds a row, and what choosing it means.</summary>
        public ComboField Add(string text, object value)
        {
            combo.Items.Add(text);
            values.Add(value);
            return this;
        }

        /// <summary>
        /// Puts a row in force without raising <see cref="ValueChanged"/>, for
        /// loading saved settings onto the screen. A reload is not a choice
        /// somebody made, and the handlers hang off choices.
        /// </summary>
        public void SetValueQuietly(object value)
        {
            quiet = true;
            try
            {
                Value = value;
            }
            finally
            {
                quiet = false;
            }
        }

        /// <summary>Re-reads the palette. Called after the theme changes.</summary>
        public void ApplyTheme()
        {
            Color surface = Enabled ? Theme.Surface : Theme.Background;

            clip.BackColor = surface;
            combo.BackColor = surface;
            combo.ForeColor = Theme.Text;
            Invalidate();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Arrange();
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            ApplyTheme();
        }

        /// <summary>
        /// Hangs the combo box off the clip so that its own border and arrow
        /// both fall outside what the clip shows, leaving only its text on view.
        /// </summary>
        private void Arrange()
        {
            // setting the height in the constructor resizes the field before
            // there is anything inside it to hang
            if (clip == null) return;
            if (Width <= ChevronColumn + 2) return;

            clip.Bounds = new Rectangle(1, ClipInset, Width - ChevronColumn, Height - ClipInset * 2);

            // a combo box keeps a height of its own whatever it is given, so it
            // is placed and then centred again against the height it took; a
            // pixel to the left of the clip and taller than it, so that its own
            // frame falls outside on every side and only its text is on view
            combo.Bounds = new Rectangle(-1, 0, Width - 1, Height);
            combo.Top = (Height - combo.Height) / 2 - ClipInset;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Rectangle bounds = ClientRectangle;
            bounds.Width -= 1;
            bounds.Height -= 1;

            Rounded.Fill(e.Graphics, bounds, Radius,
                Enabled ? Theme.Surface : Theme.Background,
                combo.Focused ? Theme.Accent : Theme.Border);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            int x = Width - 15;
            int y = Height / 2 - 2;
            var chevron = new[] { new Point(x - 4, y), new Point(x, y + 4), new Point(x + 4, y) };

            using (var pen = new Pen(Theme.TextMuted, 1.6F))
            {
                e.Graphics.DrawLines(pen, chevron);
            }
        }

        private void Field_Click(object sender, EventArgs e)
        {
            if (combo.Enabled) combo.DroppedDown = true;
        }

        private void Combo_SelectedIndexChanged(object sender, EventArgs e)
        {
            Invalidate();

            if (quiet) return;

            var handler = ValueChanged;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        /// <summary>The list is owner drawn so that it matches the theme too.</summary>
        private void Combo_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            bool onTheField = (e.State & DrawItemState.ComboBoxEdit) == DrawItemState.ComboBoxEdit;
            bool highlighted = !onTheField && (e.State & DrawItemState.Selected) == DrawItemState.Selected;

            using (var background = new SolidBrush(
                highlighted ? Theme.SurfaceHover : Enabled ? Theme.Surface : Theme.Background))
            {
                e.Graphics.FillRectangle(background, e.Bounds);
            }

            // on the field itself the text is placed against the frame's own
            // padding, which the combo box's internal inset knows nothing about
            int left = onTheField ? 9 : e.Bounds.X + 8;

            var text = new Rectangle(left, e.Bounds.Y, e.Bounds.Right - left - 4, e.Bounds.Height);

            TextRenderer.DrawText(e.Graphics, combo.Items[e.Index].ToString(), Theme.Control, text,
                Enabled ? Theme.Text : Theme.TextMuted,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
    }
}
