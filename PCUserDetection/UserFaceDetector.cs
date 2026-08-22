using AForge.Video.DirectShow;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PCUserDetection
{
    /// <summary>
    /// The only window in the app. The navigation rail swaps the content area
    /// between four screens; Detect and Add user share one camera view, so the
    /// webcam is never opened twice.
    /// </summary>
    public partial class UserFaceDetector : Form
    {
        private enum Screen { Detect, AddUser, Images, Settings }

        /// <summary>What a status line means, so its colour survives a theme change.</summary>
        private enum StatusKind { Neutral, Good, Bad }

        private FilterInfoCollection cameras;

        /// <summary>True when the camera list could not be read at all, as opposed to being empty.</summary>
        private bool cameraListFailed;

        /// <summary>The detail behind that failure, until it has been reported.</summary>
        private string cameraListError;

        /// <summary>
        /// What to say about a camera that has failed, until it is started
        /// again. Null while there is nothing wrong with it.
        /// </summary>
        private string cameraFailure;

        /// <summary>True between starting the camera and its first frame arriving.</summary>
        private bool cameraStarting;
        private Screen currentScreen = Screen.Detect;

        /// <summary>
        /// How many registered images the gallery last found, or null when they
        /// could not be listed. The rail draws it on the Images row and the
        /// folder line under the gallery repeats it, so it is held here rather
        /// than written into either of them.
        /// </summary>
        private int? imageCount;

        public UserFaceDetector()
        {
            InitializeComponent();

            // The designer file does not carry the icon, so the window is given
            // it here. The title bar, the taskbar button and the Alt+Tab entry
            // all follow from this one assignment.
            Icon = AppIcon.Value;

            BuildNavigation();
            BuildAppearanceChoices();
            ApplyTheme();
        }

        #region Theme

        /// <summary>
        /// Paints every control from the palette in force. The designer only lays
        /// the window out; the colours all come from here, so switching the theme
        /// is a matter of choosing a palette and calling this again.
        /// </summary>
        /// <remarks>
        /// BackColor and ForeColor are inherited by any child that has not been
        /// given one of its own, so setting the rail and the content area covers
        /// the panels inside them.
        /// </remarks>
        private void ApplyTheme()
        {
            BackColor = Theme.Background;
            ForeColor = Theme.Text;

            pnlNav.BackColor = Theme.Surface;
            pnlMain.BackColor = Theme.Background;

            lblAppearance.ForeColor = Theme.TextMuted;
            lblCamera.ForeColor = Theme.TextMuted;
            lblScreenTitle.ForeColor = Theme.Text;
            lblScreenHint.ForeColor = Theme.TextMuted;
            lnkOpenFolder.ForeColor = Theme.Accent;

            cboCamera.ApplyTheme();
            cboAppearance.ApplyTheme();

            cameraView.BackColor = Theme.Canvas;
            pnlSettings.ApplyTheme();

            Theme.StylePrimary(btnPrimary);
            Theme.StyleGhost(btnSecondary);

            // the rail rows read the palette while painting, so marking the one
            // in force again is all they need
            MarkSelectedScreen();

            if (IsHandleCreated) Theme.ApplyTitleBar(Handle);

            Invalidate(true);
        }

        /// <summary>
        /// Fills the Appearance list at the foot of the rail. Auto is spelled
        /// out as what it does, since a list has the room a button did not.
        /// </summary>
        private void BuildAppearanceChoices()
        {
            cboAppearance.Add("Light", ThemeMode.Light)
                         .Add("Dark", ThemeMode.Dark)
                         .Add("Follow Windows", ThemeMode.System);

            // the mode in force is not a choice somebody just made, so it is put
            // in place without the handler that would re-apply the whole palette
            cboAppearance.SetValueQuietly(Theme.Mode);
            cboAppearance.ValueChanged += cboAppearance_ValueChanged;
        }

        private void cboAppearance_ValueChanged(object sender, EventArgs e)
        {
            SwitchTheme((ThemeMode)cboAppearance.Value);
        }

        private void SwitchTheme(ThemeMode mode)
        {
            Theme.Apply(mode);
            ApplyTheme();

            // the cards read the palette as they are built, so they are rebuilt
            RefreshGallery();
        }

        /// <summary>Marks the row for the screen on show, and unmarks the rest.</summary>
        private void MarkSelectedScreen()
        {
            foreach (RailButton row in flpNav.Controls)
            {
                row.Selected = (Screen)row.Tag == currentScreen;
                row.Invalidate();
            }
        }

        #endregion

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Theme.ApplyTitleBar(Handle);
        }

        private void UserFaceDetector_Load(object sender, EventArgs e)
        {
            // A damaged DirectShow registration or a misbehaving virtual-camera
            // driver throws while the device list is being built. That leaves the
            // app with no camera to offer, which it already copes with, so it is
            // reported rather than allowed to take the window down on startup.
            try
            {
                cameras = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            }
            catch (Exception ex)
            {
                cameras = null;
                cameraListFailed = true;
                cameraListError = ex.Message;
            }

            if (cameraListFailed || cameras.Count == 0)
            {
                string only = cameraListFailed ? "Camera unavailable" : "No camera found";
                cboCamera.Add(only, only);
                cboCamera.SelectedIndex = 0;
                cboCamera.Enabled = false;
                cameraView.Placeholder = cameraListFailed
                    ? "The cameras on this PC could not be listed."
                    : "No camera was found. Connect one and restart the app.";
                btnPrimary.Enabled = false;
            }
            else
            {
                foreach (FilterInfo camera in cameras) cboCamera.Add(camera.Name, camera.MonikerString);
                cboCamera.SelectedIndex = 0; // starts the feed through ValueChanged
            }

            ShowScreen(Screen.Detect);
            RefreshGallery();
        }

        /// <summary>
        /// Reports a camera list that could not be read. It waits for Shown rather
        /// than Load, because during Load the window is not on screen yet and the
        /// dialog would come up on its own with nothing behind it.
        /// </summary>
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            if (cameraListError == null) return;

            string detail = cameraListError;
            cameraListError = null; // said once, not again on a later Shown

            MessageBox.Show(this,
                "The cameras on this PC could not be listed, so the camera is unavailable.\n\n" + detail,
                "PC User Detection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void UserFaceDetector_FormClosing(object sender, FormClosingEventArgs e)
        {
            cameraView.Stop();
        }

        #region Navigation

        private void BuildNavigation()
        {
            flpNav.Controls.Add(CreateNavButton("Detect", Screen.Detect));
            flpNav.Controls.Add(CreateNavButton("Add user", Screen.AddUser));
            flpNav.Controls.Add(CreateNavButton("Images", Screen.Images));
            flpNav.Controls.Add(CreateNavButton("Settings", Screen.Settings));
        }

        private RailButton CreateNavButton(string text, Screen screen)
        {
            // the full width of the rail, less the hairline down its right edge,
            // so the selection bar can sit against the window edge
            var row = new RailButton(text, screen) { Size = new Size(207, 36) };
            row.Click += (s, e) => ShowScreen(screen);
            return row;
        }

        /// <summary>Separates the rail from the content with a hairline down its right edge.</summary>
        private void pnlNav_Paint(object sender, PaintEventArgs e)
        {
            using (var pen = new Pen(Theme.Border))
            {
                e.Graphics.DrawLine(pen, pnlNav.Width - 1, 0, pnlNav.Width - 1, pnlNav.Height);
            }
        }

        /// <summary>
        /// Draws the app mark and the name beside it at the top of the rail.
        /// The wording is drawn rather than left to a label, so that it lines up
        /// with the rows below it to the pixel.
        /// </summary>
        private void pnlBrand_Paint(object sender, PaintEventArgs e)
        {
            const int Left = 16;
            const int MarkSize = 20;
            const int Gap = 10;

            Icon icon = AppIcon.Value;

            if (icon != null)
            {
                // the .ico carries a 20 pixel drawing of its own, so asking for
                // that size gets it rather than a shrunken copy of a larger one
                using (var sized = new Icon(icon, new Size(MarkSize, MarkSize)))
                using (Bitmap mark = sized.ToBitmap())
                {
                    e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    e.Graphics.DrawImage(mark,
                        new Rectangle(Left, (pnlBrand.Height - MarkSize) / 2, MarkSize, MarkSize));
                }
            }

            int x = Left + MarkSize + Gap;

            TextRenderer.DrawText(e.Graphics, "User Detection", Theme.Brand,
                new Rectangle(x, 0, pnlBrand.Width - x - Left, pnlBrand.Height), Theme.Text,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
        }

        /// <summary>Separates the Appearance block from the rows above it.</summary>
        private void pnlAppearance_Paint(object sender, PaintEventArgs e)
        {
            using (var pen = new Pen(Theme.Border))
            {
                e.Graphics.DrawLine(pen, 0, 0, pnlAppearance.Width, 0);
            }
        }

        private void ShowScreen(Screen screen)
        {
            currentScreen = screen;

            MarkSelectedScreen();

            bool usesCamera = screen == Screen.Detect || screen == Screen.AddUser;
            pnlCameraArea.Visible = usesCamera;
            pnlGallery.Visible = screen == Screen.Images;
            pnlSettings.Visible = screen == Screen.Settings;
            pnlFooter.Visible = usesCamera;
            pnlCameraSlot.Visible = usesCamera && cameras != null && cameras.Count > 0;

            // the camera column is taller than the title and its hint, so the
            // band it stands in is the taller of the two and the title drops to
            // sit on the same baseline
            int top = pnlCameraSlot.Visible ? 24 : 20;

            pnlHeader.Height = pnlCameraSlot.Visible ? 85 : 80;
            lblScreenTitle.Top = top;
            lblScreenHint.Top = top + 28;

            switch (screen)
            {
                case Screen.Detect:
                    lblScreenTitle.Text = "Detect";
                    lblScreenHint.Text = "Check whether the person at the PC is a registered user.";
                    btnPrimary.Text = "&Capture";
                    break;
                case Screen.AddUser:
                    lblScreenTitle.Text = "Add user";
                    lblScreenHint.Text = "Save a photo of the person at the PC as a registered user.";
                    btnPrimary.Text = "&Save photo";
                    break;
                case Screen.Images:
                    lblScreenTitle.Text = "Images";
                    lblScreenHint.Text = "Every photo the detection is compared against. Newest first.";
                    break;
                case Screen.Settings:
                    lblScreenTitle.Text = "Settings";
                    lblScreenHint.Text = "Email an alert when the person at the PC is not recognised.";
                    break;
            }

            if (usesCamera)
            {
                StartSelectedCamera();
                btnSecondary.Enabled = cameraView.IsFrozen;
                ResetStatus();
                return;
            }

            cameraView.Stop();

            // both screens read from disk as they are shown, so a photo saved or
            // a setting changed on another screen is never stale here
            if (screen == Screen.Images) RefreshGallery();
            else pnlSettings.Reload();
        }

        #endregion

        #region Camera

        private void cboCamera_ValueChanged(object sender, EventArgs e)
        {
            StartSelectedCamera();
            ResetStatus();
        }

        private void StartSelectedCamera()
        {
            if (cameras == null || cameras.Count == 0 || cboCamera.SelectedIndex < 0) return;

            // a failure belongs to the attempt that reported it; this is a fresh
            // one, and the screen says so until the camera says otherwise
            cameraFailure = null;
            cameraView.Placeholder = "The camera feed will appear here.";

            // Start is a no-op when this device is already the one running, so
            // moving between Detect and Add user does not restart the feed.
            cameraView.Start(cameras[cboCamera.SelectedIndex].MonikerString);

            // a feed that is already delivering is ready now; a fresh one has
            // nothing behind Capture until its first frame arrives, and saying
            // "Ready" before then is the whole of what went wrong here
            cameraStarting = !cameraView.HasFrame;
            btnPrimary.Enabled = cameraView.HasFrame;
        }

        /// <summary>
        /// The feed failed, which usually means another app is holding the
        /// camera. There is no frame behind Capture until a later attempt
        /// works, so it says so rather than leaving "Ready" standing.
        /// </summary>
        private void cameraView_Failed(object sender, CameraFailedEventArgs e)
        {
            cameraStarting = false;

            // a camera that was working and stopped is a different thing to one
            // that never started, and the likely cause differs with it
            cameraFailure = e.WasDelivering
                ? "The camera stopped responding."
                : "The camera is unavailable. Another app may be using it.";

            cameraView.Placeholder = e.WasDelivering
                ? "The camera stopped responding. Reconnect it and pick it again."
                : "The camera could not be started. It may already be in use by another app.";

            btnPrimary.Enabled = false;
            btnSecondary.Enabled = false;

            // the device's own wording is too technical for the status line, but
            // it is the only clue when the cause is something else entirely
            Console.WriteLine(e.Detail);

            ResetStatus();
        }

        /// <summary>
        /// The first frame has arrived, so there is something behind Capture at
        /// last and the screen can say so.
        /// </summary>
        private void cameraView_Ready(object sender, EventArgs e)
        {
            cameraStarting = false;
            btnPrimary.Enabled = true;
            ResetStatus();
        }

        /// <summary>Separates the footer from the camera above it.</summary>
        private void pnlFooter_Paint(object sender, PaintEventArgs e)
        {
            using (var pen = new Pen(Theme.Border))
            {
                e.Graphics.DrawLine(pen, pnlFooter.Padding.Left, 0,
                    pnlFooter.Width - pnlFooter.Padding.Right, 0);
            }
        }

        #endregion

        #region Capture

        private async void btnPrimary_Click(object sender, EventArgs e)
        {
            // hold the view before taking the copy, so a frame arriving in
            // between cannot leave the screen showing one frame while another
            // is the one verified and emailed
            cameraView.Freeze();

            Bitmap frame = cameraView.CaptureFrame();

            if (frame == null)
            {
                cameraView.Resume();
                SetStatus("There is no camera frame to capture yet.", StatusKind.Neutral);
                return;
            }

            btnSecondary.Enabled = true;

            using (frame)
            {
                if (currentScreen == Screen.Detect) await VerifyAsync(frame);
                else SaveRegisteredImage(frame);
            }
        }

        private void btnSecondary_Click(object sender, EventArgs e)
        {
            cameraView.Resume();
            btnSecondary.Enabled = false;
            ResetStatus();
        }

        private async Task VerifyAsync(Bitmap frame)
        {
            SetStatus("Checking the face...", StatusKind.Neutral);
            btnPrimary.Enabled = false;

            try
            {
                string filepath = AppPaths.AnonymousImage;
                frame.Save(filepath, System.Drawing.Imaging.ImageFormat.Jpeg);

                // the ONNX models take a moment, so the recognition runs off the UI
                // thread and the window keeps repainting while it does
                bool verified = await Task.Run(() =>
                    FaceRecognizer.GetFaceRecognizerInstance().IsUserVerified(filepath, AppPaths.CapturedImages));

                if (verified) SetStatus("Verified. This is a registered user.", StatusKind.Good);
                else await ReportAnonymousAsync(filepath);
            }
            catch (Exception ex)
            {
                // both the frame save and the recognition can throw, and neither
                // should take the window down
                SetStatus("The check could not be completed.", StatusKind.Bad);
                Console.WriteLine(ex);
                MessageBox.Show("The detection could not be completed.\n\n" + ex.Message,
                    "PC User Detection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                // the camera can fail while the check is running, and Capture
                // has nothing to offer once it has
                btnPrimary.Enabled = cameraFailure == null;
            }
        }

        /// <summary>
        /// Says the face was not recognised, and emails the frame if the settings
        /// ask for it. The alert never throws, so whatever became of it is only
        /// ever a line on the status bar.
        /// </summary>
        private async Task ReportAnonymousAsync(string photoPath)
        {
            const string anonymous = "Anonymous. ";

            SetStatus(anonymous + "No registered image matched.", StatusKind.Bad);

            // Retake is held for the send as well as Capture, so it cannot put
            // "Ready" on the status line a moment before the alert overwrites it
            btnSecondary.Enabled = false;

            try
            {
                EmailAlertResult alert = await EmailAlert.SendAnonymousAsync(photoPath,
                    () => SetStatus(anonymous + "Sending an alert...", StatusKind.Bad));

                // with alerts off there is nothing to add, and the line above stands
                if (alert.Outcome == EmailAlertOutcome.Disabled) return;

                SetStatus(anonymous + alert.Detail, StatusKind.Bad);
            }
            finally
            {
                btnSecondary.Enabled = cameraView.IsFrozen;
            }
        }

        private void SaveRegisteredImage(Bitmap frame)
        {
            try
            {
                string filename = string.Format("Image_{0:yyyyMMdd_HHmmss}.jpeg", DateTime.Now);
                frame.Save(Path.Combine(AppPaths.CapturedImages, filename),
                    System.Drawing.Imaging.ImageFormat.Jpeg);

                SetStatus("Saved.", filename, StatusKind.Good, true);
                RefreshGallery();
            }
            catch (Exception ex)
            {
                SetStatus("The photo could not be saved.", StatusKind.Bad);
                Console.WriteLine(ex);
                MessageBox.Show("The photo could not be saved.\n\n" + ex.Message,
                    "PC User Detection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        #endregion

        #region Status

        /// <summary>
        /// Puts a whole sentence on the status line, split at the first full
        /// stop into the state and the detail behind it. Everything that reports
        /// a status says it as one sentence, so the split happens here rather
        /// than at each of the places that has something to report.
        /// </summary>
        private void SetStatus(string text, StatusKind kind)
        {
            lblStatus.Set(text, ToneOf(kind));
            statusTip.SetToolTip(lblStatus, lblStatus.FullText);
        }

        /// <summary>
        /// Puts a status on the line in its two halves already, for the one
        /// report whose detail is not a sentence.
        /// </summary>
        /// <param name="detailIsAPath">
        /// True when the detail is a filename or a folder, which is set in a
        /// fixed pitch rather than read as words.
        /// </param>
        private void SetStatus(string state, string detail, StatusKind kind, bool detailIsAPath)
        {
            lblStatus.Set(state, detail, ToneOf(kind), detailIsAPath);

            // the line is one row and an error can be longer than the room the
            // buttons leave it, so the whole of it stays reachable
            statusTip.SetToolTip(lblStatus, lblStatus.FullText);
        }

        private static StatusTone ToneOf(StatusKind kind)
        {
            if (kind == StatusKind.Good) return StatusTone.Good;
            if (kind == StatusKind.Bad) return StatusTone.Bad;
            return StatusTone.Neutral;
        }

        private void ResetStatus()
        {
            if (cameraListFailed)
            {
                SetStatus("The cameras on this PC could not be listed.", StatusKind.Bad);
                return;
            }

            if (cameras != null && cameras.Count == 0)
            {
                SetStatus("No camera was found.", StatusKind.Bad);
                return;
            }

            if (cameraFailure != null)
            {
                SetStatus(cameraFailure, StatusKind.Bad);
                return;
            }

            if (cameraStarting)
            {
                SetStatus("Starting the camera...", StatusKind.Neutral);
                return;
            }

            SetStatus(currentScreen == Screen.Detect
                ? "Ready. Capture a frame to check it."
                : "Ready. Save a photo to register this person.", StatusKind.Neutral);
        }

        #endregion

        #region Gallery

        private void RefreshGallery()
        {
            foreach (Control card in flpImages.Controls.Cast<Control>().ToList()) card.Dispose();
            flpImages.Controls.Clear();

            string[] imageFiles;

            try
            {
                imageFiles = Directory.GetFiles(AppPaths.CapturedImages, "*.*")
                    .Where(file => file.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(file => file)
                    .ToArray();
            }
            catch (Exception ex)
            {
                flpImages.Controls.Add(GalleryMessage(
                    "The registered images could not be listed.\n\n" + ex.Message));
                ShowImageCount(null);
                return;
            }

            foreach (string imageFile in imageFiles)
            {
                var card = new ImageCard(imageFile);
                card.DeleteRequested += ImageCard_DeleteRequested;
                flpImages.Controls.Add(card);
            }

            if (imageFiles.Length == 0)
            {
                flpImages.Controls.Add(GalleryMessage(
                    "No images yet. Use Add user to register the person at the PC."));
            }

            ShowImageCount(imageFiles.Length);
        }

        /// <summary>
        /// Hands the count to the two places that show it. Null is a listing
        /// that failed, which is not the same as none and must not read as one.
        /// </summary>
        private void ShowImageCount(int? count)
        {
            imageCount = count;
            pnlFolder.Invalidate();

            foreach (RailButton row in flpNav.Controls)
            {
                if ((Screen)row.Tag != Screen.Images) continue;

                row.Count = count == null ? null : count.Value.ToString();
                row.Invalidate();
            }
        }

        /// <summary>Says how many images there are and where they are kept.</summary>
        private void pnlFolder_Paint(object sender, PaintEventArgs e)
        {
            using (var pen = new Pen(Theme.Border))
            {
                e.Graphics.DrawLine(pen, 0, 16, pnlFolder.Width, 16);
            }

            const TextFormatFlags flags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                                          TextFormatFlags.NoPadding;

            int top = pnlFolder.Padding.Top;
            int height = pnlFolder.Height - top - pnlFolder.Padding.Bottom;
            int x = 0;

            if (imageCount != null)
            {
                string lead = imageCount == 1 ? "1 image in " : imageCount.Value + " images in ";

                TextRenderer.DrawText(e.Graphics, lead, Theme.Small,
                    new Rectangle(x, top, pnlFolder.Width, height), Theme.TextMuted, flags);

                x += TextRenderer.MeasureText(e.Graphics, lead, Theme.Small,
                    new Size(int.MaxValue, height), flags).Width;
            }

            // the folder is not a hardcoded string: it is the project folder in a
            // development build and one under AppData in a published one. The
            // path is asked for by name rather than by the property that creates
            // it, since a repaint has no business touching the disk.
            int room = pnlFolder.Width - x - lnkOpenFolder.Width - 8;
            if (room <= 0) return;

            TextRenderer.DrawText(e.Graphics, AppPaths.CapturedImagesPath, Theme.Mono,
                new Rectangle(x, top, room, height), Theme.TextMuted, flags | TextFormatFlags.EndEllipsis);
        }

        private void lnkOpenFolder_Click(object sender, EventArgs e)
        {
            try
            {
                // this is the one place worth creating the folder from: asking for it
                // brings back one that has gone missing, and a folder that cannot
                // be created is reported below rather than thrown
                Process.Start(new ProcessStartInfo(AppPaths.CapturedImages) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                // an Explorer that will not open is not worth taking the app down
                Console.WriteLine(ex);
                MessageBox.Show("The folder could not be opened.\n\n" + ex.Message,
                    "PC User Detection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>Text shown in place of the cards when there is nothing to show.</summary>
        private static Label GalleryMessage(string text)
        {
            return new Label
            {
                AutoSize = true,
                Font = Theme.Body,
                ForeColor = Theme.TextMuted,
                Text = text
            };
        }

        private void ImageCard_DeleteRequested(object sender, EventArgs e)
        {
            var card = (ImageCard)sender;

            DialogResult answer = MessageBox.Show("Delete this registered image?", "PC User Detection",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);

            if (answer != DialogResult.Yes) return;

            try
            {
                File.Delete(card.FilePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("The image could not be deleted.\n\n" + ex.Message,
                    "PC User Detection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            RefreshGallery();
        }

        #endregion
    }
}
