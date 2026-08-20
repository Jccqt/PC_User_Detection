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
    /// between three screens; Detect and Add user share one camera view, so the
    /// webcam is never opened twice.
    /// </summary>
    public partial class UserFaceDetector : Form
    {
        private enum Screen { Detect, AddUser, Images }

        /// <summary>What a status line means, so its colour survives a theme change.</summary>
        private enum StatusKind { Neutral, Good, Bad }

        private FilterInfoCollection cameras;
        private Screen currentScreen = Screen.Detect;
        private StatusKind statusKind = StatusKind.Neutral;

        public UserFaceDetector()
        {
            InitializeComponent();
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
            bool selected = (ThemeMode)button.Tag == Theme.Mode;

            button.BackColor = selected ? Theme.Accent : Theme.Surface;
            button.ForeColor = selected ? Theme.OnAccent : Theme.TextMuted;
            button.FlatAppearance.BorderSize = selected ? 0 : 1;
            button.FlatAppearance.BorderColor = Theme.Border;
            button.FlatAppearance.MouseOverBackColor = selected ? Theme.AccentHover : Theme.SurfaceHover;
            button.FlatAppearance.MouseDownBackColor = button.BackColor;
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
            cameras = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            if (cameras.Count == 0)
            {
                cbCamera.Items.Add("No camera found");
                cbCamera.SelectedIndex = 0;
                cbCamera.Enabled = false;
                cameraView.Placeholder = "No camera was found. Connect one and restart the app.";
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

            bool usesCamera = screen != Screen.Images;
            cameraView.Visible = usesCamera;
            pnlGallery.Visible = !usesCamera;
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
            }

            if (usesCamera)
            {
                StartSelectedCamera();
                btnSecondary.Enabled = cameraView.IsFrozen;
                ResetStatus();
            }
            else
            {
                cameraView.Stop();
                RefreshGallery();
            }
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

            // Start is a no-op when this device is already the one running, so
            // moving between Detect and Add user does not restart the feed.
            cameraView.Start(cameras[cbCamera.SelectedIndex].MonikerString);
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
            Bitmap frame = cameraView.CaptureFrame();

            if (frame == null)
            {
                SetStatus("There is no camera frame to capture yet.", StatusKind.Neutral);
                return;
            }

            // hold the view on what was captured until Retake is pressed, so the
            // result on screen belongs to the frame that is on screen
            cameraView.Freeze();
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
                else SetStatus("Anonymous. No registered image matched.", StatusKind.Bad);
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
                btnPrimary.Enabled = true;
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
            if (cameras != null && cameras.Count == 0)
            {
                SetStatus("No camera was found.", StatusKind.Bad);
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

            string[] imageFiles = Directory.GetFiles(AppPaths.CapturedImages, "*.*")
                .Where(file => file.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(file => file)
                .ToArray();

            foreach (string imageFile in imageFiles)
            {
                var card = new ImageCard(imageFile);
                card.DeleteRequested += ImageCard_DeleteRequested;
                flpImages.Controls.Add(card);
            }

            if (imageFiles.Length == 0)
            {
                flpImages.Controls.Add(new Label
                {
                    AutoSize = true,
                    Font = Theme.Body,
                    ForeColor = Theme.TextMuted,
                    Text = "No images yet. Use Add user to register the person at the PC."
                });
            }

            lblCount.Text = imageFiles.Length == 1
                ? "1 registered image"
                : imageFiles.Length + " registered images";
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
