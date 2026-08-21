using System;
using System.Drawing;
using System.Windows.Forms;

namespace PCUserDetection
{
    /// <summary>Which of the two button treatments a button wears.</summary>
    internal enum ButtonKind
    {
        /// <summary>Filled with the accent. One per screen, on the main action.</summary>
        Primary,

        /// <summary>Outlined, for the action standing next to a primary one.</summary>
        Ghost
    }

    /// <summary>
    /// A button with 3 pixel corners, painted from the palette rather than
    /// coloured by properties.
    /// </summary>
    /// <remarks>
    /// A WinForms button cannot round itself, and a flat one draws its disabled
    /// text in a system grey that ignores the palette, so it is drawn here
    /// instead. Everything is read at paint time, which means a theme change is
    /// a repaint and nothing has to be re-applied to each button.
    /// </remarks>
    internal class FlatButton : Button
    {
        private const int Radius = 3;

        private bool hovered;
        private bool pressed;

        public FlatButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer, true);

            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Height = 32;
            Cursor = Cursors.Hand;
            UseVisualStyleBackColor = false;
        }

        /// <summary>The treatment this button wears. See <see cref="Theme.StylePrimary"/>.</summary>
        public ButtonKind Kind { get; set; }

        protected override void OnPaint(PaintEventArgs e)
        {
            // the corners are rounded, so whatever is behind the button has to
            // be laid down first or the cut corners keep the last thing drawn there
            e.Graphics.Clear(Parent == null ? Theme.Background : Parent.BackColor);

            Rectangle bounds = ClientRectangle;
            bounds.Width -= 1;
            bounds.Height -= 1;

            Color fill;
            Color text;
            Color? border = null;

            if (Kind == ButtonKind.Primary)
            {
                fill = !Enabled ? Theme.SurfaceHover : pressed ? Theme.Accent : hovered ? Theme.AccentHover : Theme.Accent;
                text = Enabled ? Theme.OnAccent : Theme.TextMuted;
            }
            else
            {
                fill = Enabled && hovered && !pressed ? Theme.SurfaceHover : Theme.Surface;
                text = Enabled ? Theme.Text : Theme.TextMuted;
                border = Theme.Border;
            }

            Rounded.Fill(e.Graphics, bounds, Radius, fill, border);

            // the focus ring is the only thing a person tabbing through the
            // window has to go on, and the fill alone does not show it
            if (Focused && ShowFocusCues && Enabled)
            {
                Rectangle inner = Rectangle.Inflate(bounds, -2, -2);
                using (var path = Rounded.Path(inner, Radius))
                using (var pen = new Pen(Kind == ButtonKind.Primary ? Theme.OnAccent : Theme.Accent))
                {
                    e.Graphics.DrawPath(pen, path);
                }
            }

            TextFormatFlags flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                                    TextFormatFlags.EndEllipsis;

            // Windows underlines the Alt mnemonic only once Alt has been held,
            // and the button has to honour that itself now that it draws its own text
            if (!ShowKeyboardCues) flags |= TextFormatFlags.HidePrefix;

            TextRenderer.DrawText(e.Graphics, Text,
                Kind == ButtonKind.Primary ? Theme.Section : Theme.Control,
                ClientRectangle, text, flags);
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
            pressed = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            pressed = e.Button == MouseButtons.Left;
            Invalidate();
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            pressed = false;
            Invalidate();
            base.OnMouseUp(e);
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            // the cursor is left over the button when it is disabled mid-click,
            // and a hover it can no longer answer is a lie
            if (!Enabled)
            {
                hovered = false;
                pressed = false;
            }

            base.OnEnabledChanged(e);
        }
    }
}
