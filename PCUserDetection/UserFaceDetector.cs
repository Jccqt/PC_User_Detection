using AForge.Video.DirectShow;
using System;
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
        private StatusKind statusKind = StatusKind.Neutral;

        public UserFaceDetector()
        {
            InitializeComponent();

            // The designer file does not carry the icon, so the window is given
            // it here. The title bar, the taskbar button and the Alt+Tab entry
            // all follow from this one assignment.
            Icon = AppIcon.Value;

            BuildNavigation();
            BuildThemeButtons();
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

            lblBrand.ForeColor = Theme.Text;
            lblAppearance.ForeColor = Theme.TextMuted;
            lblCount.ForeColor = Theme.TextMuted;
            lblScreenTitle.ForeColor = Theme.Text;
            lblScreenHint.ForeColor = Theme.TextMuted;
            lblStatus.ForeColor = StatusColor();

            pnlCameraFrame.BackColor = Theme.Surface;
            pnlCameraClip.BackColor = Theme.Surface;
            cbCamera.BackColor = Theme.Surface;
            cbCamera.ForeColor = Theme.Text;

            cameraView.BackColor = Theme.Canvas;
            pnlSettings.ApplyTheme();

            Theme.StylePrimary(btnPrimary);
            Theme.StyleGhost(btnSecondary);

            foreach (Control button in flpNav.Controls) StyleNavButton((Button)button);
            foreach (Control button in flpModes.Controls) StyleModeButton((Button)button);

            if (IsHandleCreated) Theme.ApplyTitleBar(Handle);

            Invalidate(true);
        }

        private void BuildThemeButtons()
        {
            flpModes.Controls.Add(CreateModeButton("Light", ThemeMode.Light));
            flpModes.Controls.Add(CreateModeButton("Dark", ThemeMode.Dark));
            flpModes.Controls.Add(CreateModeButton("Auto", ThemeMode.System));
        }

        private Button CreateModeButton(string text, ThemeMode mode)
        {
            var button = new Button
            {
                Text = text,
                Tag = mode,
                Size = new Size(54, 30),
                Margin = new Padding(0, 0, 4, 0),
                Font = Theme.Small,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            button.Click += (s, e) => SwitchTheme(mode);
            return button;
        }

        private void SwitchTheme(ThemeMode mode)
        {
            Theme.Apply(mode);
            ApplyTheme();

            // the cards read the palette as they are built, so they are rebuilt
            RefreshGallery();
        }

        /// <summary>Marks the mode in force. Auto tracks whatever Windows is set to.</summary>
        private void StyleModeButton(Button button)
        {
            Theme.StyleChoice(button, (ThemeMode)button.Tag == Theme.Mode);
        }

        private void StyleNavButton(Button button)
        {
            bool selected = (Screen)button.Tag == currentScreen;

            button.BackColor = selected ? Theme.SurfaceHover : Theme.Surface;
            button.ForeColor = selected ? Theme.Text : Theme.TextMuted;
            button.FlatAppearance.MouseOverBackColor = Theme.SurfaceHover;
            button.FlatAppearance.MouseDownBackColor = Theme.SurfaceHover;
            button.Invalidate();
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
                cbCamera.Items.Add(cameraListFailed ? "Camera unavailable" : "No camera found");
                cbCamera.SelectedIndex = 0;
                cbCamera.Enabled = false;
                cameraView.Placeholder = cameraListFailed
                    ? "The cameras on this PC could not be listed."
                    : "No camera was found. Connect one and restart the app.";
                btnPrimary.Enabled = false;
            }
            else
            {
                foreach (FilterInfo camera in cameras) cbCamera.Items.Add(camera.Name);
                cbCamera.SelectedIndex = 0; // starts the feed through SelectedIndexChanged
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

        private Button CreateNavButton(string text, Screen screen)
        {
            var button = new Button
            {
                Text = text,
                Tag = screen,
                Size = new Size(184, 42),
                Margin = new Padding(0, 0, 0, 4),
                Font = Theme.Nav,
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(18, 0, 0, 0),
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = Theme.SurfaceHover;
            button.FlatAppearance.MouseDownBackColor = Theme.SurfaceHover;
            button.Paint += NavButton_Paint;
            button.Click += (s, e) => ShowScreen(screen);
            return button;
        }

        /// <summary>Separates the rail from the content with a hairline down its right edge.</summary>
        private void pnlNav_Paint(object sender, PaintEventArgs e)
        {
            using (var pen = new Pen(Theme.Border))
            {
                e.Graphics.DrawLine(pen, pnlNav.Width - 1, 0, pnlNav.Width - 1, pnlNav.Height);
            }
        }

        /// <summary>Marks the screen the rail is on with an accent bar down its left edge.</summary>
        private void NavButton_Paint(object sender, PaintEventArgs e)
        {
            var button = (Button)sender;
            if ((Screen)button.Tag != currentScreen) return;

            using (var brush = new SolidBrush(Theme.Accent))
            {
                e.Graphics.FillRectangle(brush, 0, 8, 3, button.Height - 16);
            }
        }

        private void ShowScreen(Screen screen)
        {
            currentScreen = screen;

            foreach (Control control in flpNav.Controls) StyleNavButton((Button)control);

            bool usesCamera = screen == Screen.Detect || screen == Screen.AddUser;
            cameraView.Visible = usesCamera;
            pnlGallery.Visible = screen == Screen.Images;
            pnlSettings.Visible = screen == Screen.Settings;
            pnlFooter.Visible = usesCamera;
            pnlCameraSlot.Visible = usesCamera && cameras != null && cameras.Count > 0;

            switch (screen)
            {
                case Screen.Detect:
                    lblScreenTitle.Text = "Detect";
                    lblScreenHint.Text = "Check whether the person at the PC is a registered user.";
                    btnPrimary.Text = "Capture";
                    break;
                case Screen.AddUser:
                    lblScreenTitle.Text = "Add user";
                    lblScreenHint.Text = "Save a photo of the person at the PC as a registered user.";
                    btnPrimary.Text = "Save photo";
                    break;
                case Screen.Images:
                    lblScreenTitle.Text = "Images";
                    lblScreenHint.Text = "Every photo the detection is compared against.";
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

        private void cbCamera_SelectedIndexChanged(object sender, EventArgs e)
        {
            StartSelectedCamera();
            ResetStatus();
        }

        private void StartSelectedCamera()
        {
            if (cameras == null || cameras.Count == 0 || cbCamera.SelectedIndex < 0) return;

            // a failure belongs to the attempt that reported it; this is a fresh
            // one, and the screen says so until the camera says otherwise
            cameraFailure = null;
            cameraView.Placeholder = "The camera feed will appear here.";

            // Start is a no-op when this device is already the one running, so
            // moving between Detect and Add user does not restart the feed.
            cameraView.Start(cameras[cbCamera.SelectedIndex].MonikerString);

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

        /// <summary>Draws the border and the chevron the clipped combo box cannot.</summary>
        private void pnlCameraFrame_Paint(object sender, PaintEventArgs e)
        {
            using (var border = new Pen(Theme.Border))
            {
                e.Graphics.DrawRectangle(border, 0, 0, pnlCameraFrame.Width - 1, pnlCameraFrame.Height - 1);
            }

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            int x = pnlCameraFrame.Width - 15;
            int y = pnlCameraFrame.Height / 2 - 2;
            var chevron = new[] { new Point(x - 4, y), new Point(x, y + 4), new Point(x + 4, y) };

            using (var pen = new Pen(Theme.TextMuted, 1.6F))
            {
                e.Graphics.DrawLines(pen, chevron);
            }
        }

        private void pnlCameraFrame_Click(object sender, EventArgs e)
        {
            if (cbCamera.Enabled) cbCamera.DroppedDown = true;
        }

        /// <summary>The combo box is owner drawn so its list matches the theme.</summary>
        private void cbCamera_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            bool highlighted = (e.State & DrawItemState.Selected) == DrawItemState.Selected;

            using (var background = new SolidBrush(highlighted ? Theme.SurfaceHover : Theme.Surface))
            {
                e.Graphics.FillRectangle(background, e.Bounds);
            }

            var text = new Rectangle(e.Bounds.X + 8, e.Bounds.Y, e.Bounds.Width - 12, e.Bounds.Height);
            TextRenderer.DrawText(e.Graphics, cbCamera.Items[e.Index].ToString(), cbCamera.Font, text,
                cbCamera.Enabled ? Theme.Text : Theme.TextMuted,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
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

                SetStatus("Saved as " + filename, StatusKind.Good);
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

        private void SetStatus(string text, StatusKind kind)
        {
            statusKind = kind;
            lblStatus.Text = text;
            lblStatus.ForeColor = StatusColor();
            pnlStatusDot.Invalidate();
        }

        /// <summary>
        /// Resolved on demand rather than stored, so a status already on screen
        /// takes the new palette when the theme changes.
        /// </summary>
        private Color StatusColor()
        {
            if (statusKind == StatusKind.Good) return Theme.Success;
            if (statusKind == StatusKind.Bad) return Theme.Danger;
            return Theme.TextMuted;
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

        private void pnlStatusDot_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var brush = new SolidBrush(StatusColor()))
            {
                e.Graphics.FillEllipse(brush, 0, 0, pnlStatusDot.Width - 1, pnlStatusDot.Height - 1);
            }
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
                lblCount.Text = "Images unavailable";
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

            lblCount.Text = imageFiles.Length == 1
                ? "1 registered image"
                : imageFiles.Length + " registered images";
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
