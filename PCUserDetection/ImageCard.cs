using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace PCUserDetection
{
    /// <summary>
    /// One registered image in the gallery: a thumbnail, the name of the file it
    /// came from, and a Remove button. Deleting is left to the owning screen,
    /// which raises the confirmation and reloads the gallery.
    /// </summary>
    internal class ImageCard : Panel
    {
        private const int CardWidth = 210;
        private const int CardHeight = 176;
        private const int FooterHeight = 34;

        private readonly PictureBox thumbnail;

        public ImageCard(string filePath)
        {
            FilePath = filePath;

            Size = new Size(CardWidth, CardHeight);
            Margin = new Padding(0, 0, 16, 16);
            BackColor = Theme.Surface;
            Padding = new Padding(1);

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

            var name = new Label
            {
                Dock = DockStyle.Fill,
                Font = Theme.Small,
                ForeColor = Theme.TextMuted,
                Text = DescribeCapture(filePath),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0),
                AutoEllipsis = true
            };

            var remove = new Button
            {
                Dock = DockStyle.Right,
                Width = 64,
                Text = "Remove",
                Font = Theme.Small,
                ForeColor = Theme.TextMuted,
                BackColor = Theme.Surface,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            remove.FlatAppearance.BorderSize = 0;
            remove.FlatAppearance.MouseOverBackColor = Theme.SurfaceHover;
            remove.MouseEnter += (s, e) => remove.ForeColor = Theme.Danger;
            remove.MouseLeave += (s, e) => remove.ForeColor = Theme.TextMuted;
            remove.Click += (s, e) =>
            {
                var handler = DeleteRequested;
                if (handler != null) handler(this, EventArgs.Empty);
            };

            var footer = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = FooterHeight,
                BackColor = Theme.Surface
            };
            footer.Controls.Add(name);
            footer.Controls.Add(remove);

            Controls.Add(thumbnail);
            Controls.Add(footer);
        }

        /// <summary>The registered image this card was built from.</summary>
        public string FilePath { get; private set; }

        /// <summary>Raised when Remove is clicked. The card does not delete anything itself.</summary>
        public event EventHandler DeleteRequested;

        /// <summary>Turns Image_20250726_140742 into a date a person can read.</summary>
        private static string DescribeCapture(string filePath)
        {
            string name = Path.GetFileNameWithoutExtension(filePath);
            DateTime taken;

            if (name.StartsWith("Image_") && DateTime.TryParseExact(name.Substring(6), "yyyyMMdd_HHmmss",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out taken))
            {
                return taken.ToString("d MMM yyyy, HH:mm");
            }

            return name;
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
            using (var pen = new Pen(Theme.Border))
            {
                e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
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
