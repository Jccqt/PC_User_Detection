using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PCUserDetection
{
    /// <summary>What a status line means, so its colour survives a theme change.</summary>
    internal enum StatusTone
    {
        Neutral,
        Good,
        Bad
    }

    /// <summary>
    /// The line that says what just happened: a filled dot, the state in a word,
    /// and the detail behind it.
    /// </summary>
    /// <remarks>
    /// The two halves are drawn rather than laid out as labels because they are
    /// set in different faces — the state semibold, the detail plain, and a
    /// filename or a path in Consolas — and they have to sit against each other
    /// as one sentence whatever their widths come to. The colour is resolved
    /// while painting, so a status already on screen takes the new palette when
    /// the theme changes.
    /// </remarks>
    internal class StatusLine : Control
    {
        private const int DotSize = 8;

        /// <summary>The gap after the dot, and between the state and its detail.</summary>
        private const int Gap = 10;

        private const TextFormatFlags Flags =
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
            TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis;

        private string state = string.Empty;
        private string detail = string.Empty;
        private bool detailIsAPath;
        private StatusTone tone;

        public StatusLine()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
        }

        /// <summary>The whole line as one string, for a tooltip when it will not fit.</summary>
        public string FullText
        {
            get { return detail.Length == 0 ? state : state + " " + detail; }
        }

        /// <summary>
        /// Says what happened, given as one sentence and split at the first full
        /// stop into the state and the detail behind it. Everything that reports
        /// a status says it as a sentence, so the split happens here rather than
        /// at each of the places that has something to report.
        /// </summary>
        public void Set(string sentence, StatusTone tone)
        {
            int stop = sentence.IndexOf(". ", StringComparison.Ordinal);

            if (stop < 0) Set(sentence, string.Empty, tone, false);
            else Set(sentence.Substring(0, stop + 1), sentence.Substring(stop + 2), tone, false);
        }

        /// <summary>
        /// Says what happened, in the two halves already. <paramref name="detail"/>
        /// may be empty, which leaves the state standing on its own.
        /// </summary>
        /// <param name="detailIsAPath">
        /// True for a filename or a folder, which is set in a fixed pitch
        /// because it is read character by character.
        /// </param>
        public void Set(string state, string detail, StatusTone tone, bool detailIsAPath)
        {
            this.state = state ?? string.Empty;
            this.detail = detail ?? string.Empty;
            this.tone = tone;
            this.detailIsAPath = detailIsAPath;

            Invalidate();
        }

        /// <summary>
        /// Resolved on demand rather than stored, so a status already on screen
        /// takes the new palette when the theme changes.
        /// </summary>
        private Color Ink
        {
            get
            {
                if (tone == StatusTone.Good) return Theme.Success;
                if (tone == StatusTone.Bad) return Theme.Danger;
                return Theme.TextMuted;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Parent == null ? Theme.Background : Parent.BackColor);

            Color ink = Ink;

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using (var brush = new SolidBrush(ink))
            {
                e.Graphics.FillEllipse(brush, 0, (Height - DotSize) / 2, DotSize, DotSize);
            }

            int x = DotSize + Gap;

            if (state.Length > 0)
            {
                var bounds = new Rectangle(x, 0, Width - x, Height);
                TextRenderer.DrawText(e.Graphics, state, Theme.Status, bounds, ink, Flags);
                x += TextRenderer.MeasureText(e.Graphics, state, Theme.Status,
                    new Size(int.MaxValue, Height), Flags).Width + Gap;
            }

            if (detail.Length == 0 || x >= Width) return;

            TextRenderer.DrawText(e.Graphics, detail, detailIsAPath ? Theme.Mono : Theme.Body,
                new Rectangle(x, 0, Width - x, Height), ink, Flags);
        }
    }
}
