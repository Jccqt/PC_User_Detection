using System;
using System.Drawing;
using System.Windows.Forms;

namespace PCUserDetection
{
    /// <summary>
    /// One row of the navigation rail: the name of a screen, a full-height
    /// accent bar down its left edge while it is the screen on show, and an
    /// optional count on its right.
    /// </summary>
    /// <remarks>
    /// The row is drawn rather than left to the button, which insets its text by
    /// a few pixels of its own and would put the wording out of line with the
    /// app mark above it. Everything is read from the palette at paint time, so
    /// a theme change is a repaint.
    /// </remarks>
    internal class RailButton : Button
    {
        /// <summary>The width of the bar marking the row in force.</summary>
        private const int BarWidth = 3;

        /// <summary>Where the wording starts, matching the app mark above it.</summary>
        private const int TextLeft = 16;

        /// <summary>How far the count is held off the right edge.</summary>
        private const int CountRight = 14;

        private bool hovered;

        public RailButton(string text, object tag)
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer, true);

            Text = text;
            Tag = tag;
            Margin = new Padding(0);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Cursor = Cursors.Hand;
            UseVisualStyleBackColor = false;
        }

        /// <summary>True while this is the screen the content area is showing.</summary>
        public bool Selected { get; set; }

        /// <summary>
        /// A number shown on the right of the row, or null for a row that does
        /// not count anything.
        /// </summary>
        public string Count { get; set; }

        protected override void OnPaint(PaintEventArgs e)
        {
            using (var fill = new SolidBrush(Selected || hovered ? Theme.SurfaceHover : Theme.Surface))
            {
                e.Graphics.FillRectangle(fill, ClientRectangle);
            }

            if (Selected)
            {
                using (var bar = new SolidBrush(Theme.Accent))
                {
                    e.Graphics.FillRectangle(bar, 0, 0, BarWidth, Height);
                }
            }

            Color ink = Selected ? Theme.Text : Theme.TextMuted;

            const TextFormatFlags flags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                                          TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis;

            TextRenderer.DrawText(e.Graphics, Text, Theme.Nav,
                new Rectangle(TextLeft, 0, Width - TextLeft - CountRight, Height), ink, flags);

            if (Count != null)
            {
                TextRenderer.DrawText(e.Graphics, Count, Theme.Small,
                    new Rectangle(0, 0, Width - CountRight, Height), ink,
                    TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }

            if (Focused && ShowFocusCues)
            {
                ControlPaint.DrawFocusRectangle(e.Graphics,
                    new Rectangle(TextLeft - 2, 4, Width - TextLeft - 8, Height - 8));
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            hovered = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hovered = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnGotFocus(EventArgs e)
        {
            Invalidate();
            base.OnGotFocus(e);
        }

        protected override void OnLostFocus(EventArgs e)
        {
            Invalidate();
            base.OnLostFocus(e);
        }
    }
}
