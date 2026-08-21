using System;
using System.Drawing;
using System.Windows.Forms;

namespace PCUserDetection
{
    /// <summary>
    /// A check box drawn from the palette: a 16 pixel box with 2 pixel corners,
    /// filled with the accent and ticked when it is on, and the wording beside it.
    /// </summary>
    /// <remarks>
    /// A system check box paints its glyph in system colours that no property
    /// changes, which is what the On / Off button strips were working around.
    /// Drawing the box here gets the native shape and the palette at once, and
    /// keeps the keyboard and screen reader behaviour a real check box has.
    /// </remarks>
    internal class CheckField : CheckBox
    {
        private const int BoxSize = 16;
        private const int BoxRadius = 2;

        /// <summary>The gap between the box and the wording.</summary>
        private const int Gap = 8;

        public CheckField(string text)
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer, true);

            Text = text;
            AutoSize = false;
            // the wording is what sets the height: a 16 pixel box in a row any
            // shorter than the line it labels would clip the descenders
            Height = TextRenderer.MeasureText("Hg", Theme.Body).Height;
            FlatStyle = FlatStyle.Flat;
            Cursor = Cursors.Hand;
            TextAlign = ContentAlignment.MiddleLeft;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Parent == null ? Theme.Background : Parent.BackColor);

            var box = new Rectangle(0, (Height - BoxSize) / 2, BoxSize - 1, BoxSize - 1);

            if (Checked)
            {
                Rounded.Fill(e.Graphics, box, BoxRadius, Enabled ? Theme.Accent : Theme.SurfaceHover, null);

                TextRenderer.DrawText(e.Graphics, "✓", Theme.Small,
                    new Rectangle(box.X, box.Y, box.Width + 1, box.Height + 1),
                    Enabled ? Theme.OnAccent : Theme.TextMuted,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
            else
            {
                Rounded.Fill(e.Graphics, box, BoxRadius,
                    Enabled ? Theme.Surface : Theme.Background, Theme.Border);
            }

            var caption = new Rectangle(BoxSize + Gap, 0, Width - BoxSize - Gap, Height);

            TextFormatFlags flags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                                    TextFormatFlags.EndEllipsis;

            if (!ShowKeyboardCues) flags |= TextFormatFlags.HidePrefix;

            TextRenderer.DrawText(e.Graphics, Text, Theme.Body, caption,
                Enabled ? Theme.Text : Theme.TextMuted, flags);

            // the only cue somebody tabbing through the form has, since the box
            // itself looks the same whether or not it holds the focus
            if (Focused && ShowFocusCues)
            {
                ControlPaint.DrawFocusRectangle(e.Graphics,
                    new Rectangle(caption.X - 2, 2, TextWidth() + 4, Height - 4));
            }
        }

        private int TextWidth()
        {
            return TextRenderer.MeasureText(Text, Theme.Body).Width;
        }

        protected override void OnCheckedChanged(EventArgs e)
        {
            Invalidate();
            base.OnCheckedChanged(e);
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            Invalidate();
            base.OnEnabledChanged(e);
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
