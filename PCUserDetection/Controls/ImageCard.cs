using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace PCUserDetection
{
    /// <summary>
    /// One registered image in the gallery: a thumbnail, when it was taken, and
    /// a × to remove it. Deleting is left to the owning screen, which raises the
    /// confirmation and reloads the gallery.
    /// </summary>
    internal class ImageCard : Panel
    {
        private const int CardWidth = 170;
        private const int ThumbnailHeight = 111;
        private const int FooterHeight = 32;
        private const int Radius = 3;

        /// <summary>The one pixel border the card is drawn with, top and bottom.</summary>
        private const int CardHeight = ThumbnailHeight + FooterHeight + 2;

        private readonly PictureBox thumbnail;
        private readonly Panel footer;
        private readonly Label taken;
        private readonly Button remove;

        public ImageCard(string filePath)
        {
            FilePath = filePath;

            Size = new Size(CardWidth, CardHeight);
            Margin = new Padding(0, 0, 12, 12);
            BackColor = Theme.Surface;
            Padding = new Padding(1);

            // The card has 3 pixel corners and its children are square, so the
            // whole card is clipped to the shape its border is drawn in.
            // This rectangle is not deflated the way the border's is: the border
            // is measured for a stroke, and the region is measured for a fill.
            using (GraphicsPath shape = Rounded.Path(new Rectangle(0, 0, CardWidth, CardHeight), Radius))
            {
                Region = new Region(shape);
            }

            thumbnail = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Theme.Canvas,
                // read the file into memory rather than binding the picture box to
                // the path, which would hold the file open and block Remove
                Image = LoadWithoutLocking(filePath)
            };

            if (thumbnail.Image == null)
            {
                // the file is empty, truncated or not an image at all; the card is
                // still listed so the file can be seen and removed
                thumbnail.Controls.Add(new Label
                {
                    Dock = DockStyle.Fill,
                    Font = Theme.Small,
                    ForeColor = Theme.TextMuted,
                    Text = "Preview unavailable",
                    TextAlign = ContentAlignment.MiddleCenter
                });
            }

            taken = new Label
            {
                Dock = DockStyle.Fill,
                Font = ParsesAsACapture(filePath) ? Theme.Small : Theme.Mono,
                ForeColor = ParsesAsACapture(filePath) ? Theme.Text : Theme.TextMuted,
                Text = DescribeCapture(filePath),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            };

            remove = new Button
            {
                Dock = DockStyle.Right,
                Width = 20,
                Text = "✕",
                Font = Theme.Control,
                ForeColor = Theme.TextMuted,
                BackColor = Theme.Surface,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false,
                TabStop = false
            };
            remove.FlatAppearance.BorderSize = 0;
            remove.FlatAppearance.MouseOverBackColor = Theme.SurfaceHover;
            remove.FlatAppearance.MouseDownBackColor = Theme.SurfaceHover;
            remove.Click += (s, e) =>
            {
                var handler = DeleteRequested;
                if (handler != null) handler(this, EventArgs.Empty);
            };

            footer = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = FooterHeight,
                BackColor = Theme.Surface,
                Padding = new Padding(10, 0, 5, 0)
            };
            footer.Paint += Footer_Paint;
            footer.Controls.Add(taken);
            footer.Controls.Add(remove);

            // a child swallows the mouse events of the panel under it, so every
            // part of the footer has to be asked where the pointer really is
            footer.MouseEnter += Hover_Changed;
            footer.MouseLeave += Hover_Changed;
            taken.MouseEnter += Hover_Changed;
            taken.MouseLeave += Hover_Changed;
            remove.MouseEnter += Hover_Changed;
            remove.MouseLeave += Hover_Changed;

            Controls.Add(thumbnail);
            Controls.Add(footer);
        }

        /// <summary>The registered image this card was built from.</summary>
        public string FilePath { get; private set; }

        /// <summary>Raised when × is clicked. The card does not delete anything itself.</summary>
        public event EventHandler DeleteRequested;

        /// <summary>
        /// Lights the footer while the pointer is anywhere over it, and turns
        /// the × the colour of what it does.
        /// </summary>
        private void Hover_Changed(object sender, EventArgs e)
        {
            bool over = footer.ClientRectangle.Contains(footer.PointToClient(Cursor.Position));

            footer.BackColor = over ? Theme.SurfaceHover : Theme.Surface;
            remove.BackColor = footer.BackColor;
            remove.ForeColor = over ? Theme.Danger : Theme.TextMuted;
            taken.BackColor = footer.BackColor;
        }

        /// <summary>Separates the footer from the thumbnail above it.</summary>
        private void Footer_Paint(object sender, PaintEventArgs e)
        {
            using (var pen = new Pen(Theme.Border))
            {
                e.Graphics.DrawLine(pen, 0, 0, footer.Width, 0);
            }
        }

        /// <summary>Turns Image_20250726_140742 into a date a person can read.</summary>
        private static string DescribeCapture(string filePath)
        {
            string name = Path.GetFileNameWithoutExtension(filePath);
            DateTime taken;

            if (TryReadCapture(name, out taken)) return taken.ToString("d MMM, HH:mm");

            return name;
        }

        /// <summary>
        /// True when the filename carries the moment the photo was taken. A name
        /// that does not is shown as it is, in the face filenames are set in.
        /// </summary>
        private static bool ParsesAsACapture(string filePath)
        {
            DateTime taken;
            return TryReadCapture(Path.GetFileNameWithoutExtension(filePath), out taken);
        }

        private static bool TryReadCapture(string name, out DateTime taken)
        {
            const string prefix = "Image_";
            const int momentLength = 15; // yyyyMMdd_HHmmss

            taken = DateTime.MinValue;

            if (!name.StartsWith(prefix) || name.Length < prefix.Length + momentLength) return false;

            // a photo registered in a second that already had one carries a count
            // after the moment, which is no part of when it was taken
            string count = name.Substring(prefix.Length + momentLength);
            if (count.Length > 0 && count[0] != '_') return false;

            return DateTime.TryParseExact(name.Substring(prefix.Length, momentLength), "yyyyMMdd_HHmmss",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out taken);
        }

        /// <summary>
        /// Reads the image into memory, or returns null when it cannot be decoded.
        /// A file that was never finished writing must not stop the gallery, and
        /// with it the whole app, from opening.
        /// </summary>
        private static System.Drawing.Image LoadWithoutLocking(string filePath)
        {
            try
            {
                using (var stream = File.OpenRead(filePath))
                using (var loaded = System.Drawing.Image.FromStream(stream))
                {
                    return new Bitmap(loaded);
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Rectangle bounds = ClientRectangle;
            bounds.Width -= 1;
            bounds.Height -= 1;

            using (GraphicsPath shape = Rounded.Path(bounds, Radius))
            using (var pen = new Pen(Theme.Border))
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.DrawPath(pen, shape);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && thumbnail != null && thumbnail.Image != null)
            {
                thumbnail.Image.Dispose();
                thumbnail.Image = null;
            }
            base.Dispose(disposing);
        }
    }
}
