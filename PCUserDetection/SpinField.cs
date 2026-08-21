using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace PCUserDetection
{
    /// <summary>
    /// A text field with a step column on its right, for the two settings that
    /// are a plain whole number: the cooldown and the port.
    /// </summary>
    /// <remarks>
    /// The number is still typed into a text box rather than into a
    /// NumericUpDown, which paints its own frame and quietly rewrites anything
    /// it does not like. What is typed is left exactly as typed, and saving is
    /// what refuses a number that cannot be used.
    /// </remarks>
    internal class SpinField : TextField
    {
        /// <summary>The width of the column holding the two arrows.</summary>
        private const int StepColumn = 18;

        private readonly int least;
        private readonly int most;

        public SpinField(int least, int most)
        {
            this.least = least;
            this.most = most;

            Padding = new Padding(9, 7, StepColumn + 5, 0);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            int divider = Width - 1 - StepColumn;
            Color ink = Enabled ? Theme.TextMuted : Theme.Border;

            using (var pen = new Pen(Theme.Border))
            {
                e.Graphics.DrawLine(pen, divider, 1, divider, Height - 2);
            }

            var up = new Rectangle(divider, 1, StepColumn, Height / 2 - 1);
            var down = new Rectangle(divider, Height / 2, StepColumn, Height / 2 - 1);

            TextRenderer.DrawText(e.Graphics, "▲", Theme.Small, up, ink,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.Bottom);
            TextRenderer.DrawText(e.Graphics, "▼", Theme.Small, down, ink,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.Top);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (!Enabled || e.Button != MouseButtons.Left) return;
            if (e.X < Width - 1 - StepColumn) return;

            Step(e.Y < Height / 2 ? 1 : -1);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            Cursor = e.X >= Width - 1 - StepColumn ? Cursors.Hand : Cursors.Default;
        }

        /// <summary>
        /// Moves the number by one, within the range this field was built for.
        /// Text that is not a number is left alone rather than replaced: what
        /// was typed is the thing to correct, and saving says so.
        /// </summary>
        private void Step(int by)
        {
            int value;

            if (!int.TryParse(Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
            {
                Highlight();
                return;
            }

            value += by;

            if (value < least) value = least;
            if (value > most) value = most;

            Text = value.ToString(CultureInfo.InvariantCulture);
            Box.SelectionStart = Text.Length;
        }
    }
}
