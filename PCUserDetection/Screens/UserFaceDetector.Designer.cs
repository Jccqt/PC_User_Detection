namespace PCUserDetection
{
    partial class UserFaceDetector
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Builds the shell: a navigation rail on the left and a content area on
        /// the right that swaps between the camera and the image gallery.
        /// Everything is docked, so the window can be resized freely.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.statusTip = new System.Windows.Forms.ToolTip(this.components);
            this.pnlNav = new System.Windows.Forms.Panel();
            this.pnlBrand = new System.Windows.Forms.Panel();
            this.flpNav = new System.Windows.Forms.FlowLayoutPanel();
            this.pnlAppearance = new System.Windows.Forms.Panel();
            this.lblAppearance = new System.Windows.Forms.Label();
            this.cboAppearance = new PCUserDetection.ComboField();
            this.pnlMain = new System.Windows.Forms.Panel();
            this.pnlHeader = new System.Windows.Forms.Panel();
            this.lblScreenTitle = new System.Windows.Forms.Label();
            this.lblScreenHint = new System.Windows.Forms.Label();
            this.pnlCameraSlot = new System.Windows.Forms.Panel();
            this.lblCamera = new System.Windows.Forms.Label();
            this.cboCamera = new PCUserDetection.ComboField();
            this.pnlFooter = new System.Windows.Forms.Panel();
            this.lblStatus = new PCUserDetection.StatusLine();
            this.pnlActions = new System.Windows.Forms.FlowLayoutPanel();
            this.btnPrimary = new PCUserDetection.FlatButton();
            this.btnSecondary = new PCUserDetection.FlatButton();
            this.pnlCameraArea = new System.Windows.Forms.Panel();
            this.cameraView = new PCUserDetection.CameraView();
            this.pnlGallery = new System.Windows.Forms.Panel();
            this.flpImages = new System.Windows.Forms.FlowLayoutPanel();
            this.pnlFolder = new System.Windows.Forms.Panel();
            this.lnkOpenFolder = new System.Windows.Forms.Label();
            this.pnlSettings = new PCUserDetection.SettingsPanel();
            this.pnlNav.SuspendLayout();
            this.pnlAppearance.SuspendLayout();
            this.pnlMain.SuspendLayout();
            this.pnlHeader.SuspendLayout();
            this.pnlCameraSlot.SuspendLayout();
            this.pnlFooter.SuspendLayout();
            this.pnlActions.SuspendLayout();
            this.pnlCameraArea.SuspendLayout();
            this.pnlGallery.SuspendLayout();
            this.pnlFolder.SuspendLayout();
            this.SuspendLayout();
            //
            // pnlNav
            //
            // The rows span the full width of the rail, so that the selection bar
            // can sit against the window edge; the one pixel of right padding is
            // the hairline the rail is separated from the content by, which a
            // docked child would otherwise cover.
            this.pnlNav.Dock = System.Windows.Forms.DockStyle.Left;
            this.pnlNav.Name = "pnlNav";
            this.pnlNav.Padding = new System.Windows.Forms.Padding(0, 0, 1, 0);
            this.pnlNav.Size = new System.Drawing.Size(208, 640);
            this.pnlNav.Paint += new System.Windows.Forms.PaintEventHandler(this.pnlNav_Paint);
            //
            // pnlBrand
            //
            // the app mark and the name beside it are both drawn; see pnlBrand_Paint
            this.pnlBrand.AccessibleName = "User Detection";
            this.pnlBrand.AccessibleRole = System.Windows.Forms.AccessibleRole.StaticText;
            this.pnlBrand.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlBrand.Height = 56;
            this.pnlBrand.Name = "pnlBrand";
            this.pnlBrand.Paint += new System.Windows.Forms.PaintEventHandler(this.pnlBrand_Paint);
            //
            // flpNav
            //
            this.flpNav.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flpNav.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpNav.Name = "flpNav";
            this.flpNav.WrapContents = false;
            //
            // pnlAppearance
            //
            this.pnlAppearance.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlAppearance.Height = 79;
            this.pnlAppearance.Name = "pnlAppearance";
            this.pnlAppearance.Padding = new System.Windows.Forms.Padding(16, 12, 16, 16);
            this.pnlAppearance.Paint += new System.Windows.Forms.PaintEventHandler(this.pnlAppearance_Paint);
            //
            // lblAppearance
            //
            this.lblAppearance.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblAppearance.Font = Theme.Small;
            this.lblAppearance.Height = 21;
            this.lblAppearance.Name = "lblAppearance";
            this.lblAppearance.Padding = new System.Windows.Forms.Padding(0, 0, 0, 6);
            this.lblAppearance.Text = "Appearance";
            this.lblAppearance.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // cboAppearance
            //
            // the rows are added in BuildAppearanceChoices, beside the theme code
            this.cboAppearance.Dock = System.Windows.Forms.DockStyle.Top;
            this.cboAppearance.Name = "cboAppearance";
            this.cboAppearance.TabIndex = 3;
            //
            // pnlMain
            //
            // No padding of its own: the header, the camera area and the footer
            // each carry theirs, so that the settings action bar can reach the
            // window edges while the screens above it stay inset.
            this.pnlMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlMain.Name = "pnlMain";
            //
            // pnlHeader
            //
            this.pnlHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlHeader.Height = 85;
            this.pnlHeader.Name = "pnlHeader";
            this.pnlHeader.Padding = new System.Windows.Forms.Padding(24, 20, 24, 16);
            //
            // lblScreenTitle
            //
            // placed rather than docked, so the title and the hint under it stay
            // on the same baseline as the camera field beside them
            this.lblScreenTitle.AutoSize = true;
            this.lblScreenTitle.Font = Theme.Heading;
            this.lblScreenTitle.Location = new System.Drawing.Point(24, 24);
            this.lblScreenTitle.Name = "lblScreenTitle";
            //
            // lblScreenHint
            //
            this.lblScreenHint.AutoSize = true;
            this.lblScreenHint.Font = Theme.Body;
            this.lblScreenHint.Location = new System.Drawing.Point(24, 52);
            this.lblScreenHint.Name = "lblScreenHint";
            //
            // pnlCameraSlot
            //
            this.pnlCameraSlot.Dock = System.Windows.Forms.DockStyle.Right;
            this.pnlCameraSlot.Name = "pnlCameraSlot";
            this.pnlCameraSlot.Width = 240;
            //
            // lblCamera
            //
            this.lblCamera.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblCamera.Font = Theme.Small;
            this.lblCamera.Height = 19;
            this.lblCamera.Name = "lblCamera";
            this.lblCamera.Padding = new System.Windows.Forms.Padding(0, 0, 0, 4);
            this.lblCamera.Text = "Camera";
            this.lblCamera.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // cboCamera
            //
            this.cboCamera.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.cboCamera.Name = "cboCamera";
            this.cboCamera.TabIndex = 0;
            this.cboCamera.ValueChanged += new System.EventHandler(this.cboCamera_ValueChanged);
            //
            // pnlFooter
            //
            // The top padding is the hairline the footer is separated by plus the
            // 16 pixels of air under it; the camera area above supplies the 16
            // pixels over it.
            this.pnlFooter.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlFooter.Height = 69;
            this.pnlFooter.Name = "pnlFooter";
            this.pnlFooter.Padding = new System.Windows.Forms.Padding(24, 17, 24, 20);
            this.pnlFooter.Paint += new System.Windows.Forms.PaintEventHandler(this.pnlFooter_Paint);
            //
            // lblStatus
            //
            this.lblStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStatus.Name = "lblStatus";
            //
            // pnlActions
            //
            this.pnlActions.Dock = System.Windows.Forms.DockStyle.Right;
            this.pnlActions.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.pnlActions.Name = "pnlActions";
            this.pnlActions.Size = new System.Drawing.Size(232, 32);
            this.pnlActions.WrapContents = false;
            //
            // btnPrimary
            //
            this.btnPrimary.Margin = new System.Windows.Forms.Padding(0);
            this.btnPrimary.Name = "btnPrimary";
            this.btnPrimary.Size = new System.Drawing.Size(120, 32);
            this.btnPrimary.TabIndex = 1;
            this.btnPrimary.Click += new System.EventHandler(this.btnPrimary_Click);
            //
            // btnSecondary
            //
            this.btnSecondary.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
            this.btnSecondary.Name = "btnSecondary";
            this.btnSecondary.Size = new System.Drawing.Size(104, 32);
            this.btnSecondary.TabIndex = 2;
            this.btnSecondary.Text = "Retake";
            this.btnSecondary.Click += new System.EventHandler(this.btnSecondary_Click);
            //
            // pnlCameraArea
            //
            // holds the camera view off the footer rule and in from the window edge
            this.pnlCameraArea.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlCameraArea.Name = "pnlCameraArea";
            this.pnlCameraArea.Padding = new System.Windows.Forms.Padding(24, 0, 24, 16);
            //
            // cameraView
            //
            this.cameraView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cameraView.Name = "cameraView";
            this.cameraView.Failed += new System.EventHandler<PCUserDetection.CameraFailedEventArgs>(this.cameraView_Failed);
            this.cameraView.Ready += new System.EventHandler(this.cameraView_Ready);
            //
            // pnlGallery
            //
            this.pnlGallery.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlGallery.Name = "pnlGallery";
            this.pnlGallery.Padding = new System.Windows.Forms.Padding(24, 0, 24, 0);
            this.pnlGallery.Visible = false;
            //
            // flpImages
            //
            this.flpImages.AutoScroll = true;
            this.flpImages.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flpImages.Name = "flpImages";
            //
            // pnlFolder
            //
            // The top padding is the 16 pixels of air over the rule, the rule
            // itself, and the 14 under it; see pnlFolder_Paint.
            this.pnlFolder.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlFolder.Height = 66;
            this.pnlFolder.Name = "pnlFolder";
            this.pnlFolder.Padding = new System.Windows.Forms.Padding(0, 31, 0, 20);
            this.pnlFolder.Paint += new System.Windows.Forms.PaintEventHandler(this.pnlFolder_Paint);
            //
            // lnkOpenFolder
            //
            this.lnkOpenFolder.Cursor = System.Windows.Forms.Cursors.Hand;
            this.lnkOpenFolder.Dock = System.Windows.Forms.DockStyle.Right;
            this.lnkOpenFolder.Font = Theme.Small;
            this.lnkOpenFolder.Name = "lnkOpenFolder";
            this.lnkOpenFolder.Text = "Open folder";
            this.lnkOpenFolder.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.lnkOpenFolder.Width = 80;
            this.lnkOpenFolder.Click += new System.EventHandler(this.lnkOpenFolder_Click);
            //
            // pnlSettings
            //
            // the fields inside it are built by the control itself, the same way
            // the gallery cards are
            this.pnlSettings.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlSettings.Name = "pnlSettings";
            this.pnlSettings.Visible = false;
            //
            // UserFaceDetector
            //
            // Segoe UI at 9.75pt, which is what Theme.Body sets the window to,
            // measures 7 by 17 at 96 DPI. Saying 15 here told WinForms the font
            // had grown by a ninth and left it stretching every height in this
            // file to match, so nothing landed at the size it was given.
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1024, 640);
            this.Font = Theme.Body;
            this.MinimumSize = new System.Drawing.Size(900, 600);
            this.Name = "UserFaceDetector";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "PC User Detection";

            // A docked control claims its edge in the order it is added, so the
            // Fill control goes in first and the edges are layered on top of it.
            this.pnlNav.Controls.Add(this.flpNav);
            this.pnlNav.Controls.Add(this.pnlBrand);
            this.pnlAppearance.Controls.Add(this.cboAppearance);
            this.pnlAppearance.Controls.Add(this.lblAppearance);

            this.pnlNav.Controls.Add(this.pnlAppearance);

            this.pnlCameraSlot.Controls.Add(this.lblCamera);
            this.pnlCameraSlot.Controls.Add(this.cboCamera);

            this.pnlHeader.Controls.Add(this.lblScreenTitle);
            this.pnlHeader.Controls.Add(this.lblScreenHint);
            this.pnlHeader.Controls.Add(this.pnlCameraSlot);

            this.pnlActions.Controls.Add(this.btnPrimary);
            this.pnlActions.Controls.Add(this.btnSecondary);

            this.pnlFooter.Controls.Add(this.lblStatus);
            this.pnlFooter.Controls.Add(this.pnlActions);

            this.pnlCameraArea.Controls.Add(this.cameraView);

            this.pnlFolder.Controls.Add(this.lnkOpenFolder);
            this.pnlGallery.Controls.Add(this.flpImages);
            this.pnlGallery.Controls.Add(this.pnlFolder);

            this.pnlMain.Controls.Add(this.pnlCameraArea);
            this.pnlMain.Controls.Add(this.pnlGallery);
            this.pnlMain.Controls.Add(this.pnlSettings);
            this.pnlMain.Controls.Add(this.pnlHeader);
            this.pnlMain.Controls.Add(this.pnlFooter);

            this.Controls.Add(this.pnlMain);
            this.Controls.Add(this.pnlNav);

            this.Load += new System.EventHandler(this.UserFaceDetector_Load);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.UserFaceDetector_FormClosing);
            this.pnlAppearance.ResumeLayout(false);
            this.pnlNav.ResumeLayout(false);
            this.pnlCameraSlot.ResumeLayout(false);
            this.pnlHeader.ResumeLayout(false);
            this.pnlHeader.PerformLayout();
            this.pnlActions.ResumeLayout(false);
            this.pnlFooter.ResumeLayout(false);
            this.pnlCameraArea.ResumeLayout(false);
            this.pnlFolder.ResumeLayout(false);
            this.pnlGallery.ResumeLayout(false);
            this.pnlMain.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        /// <summary>Carries a status line that is longer than the room it has.</summary>
        private System.Windows.Forms.ToolTip statusTip;

        private System.Windows.Forms.Panel pnlNav;
        private System.Windows.Forms.Panel pnlBrand;
        private System.Windows.Forms.FlowLayoutPanel flpNav;
        private System.Windows.Forms.Panel pnlAppearance;
        private System.Windows.Forms.Label lblAppearance;
        private ComboField cboAppearance;
        private System.Windows.Forms.Panel pnlMain;
        private System.Windows.Forms.Panel pnlHeader;
        private System.Windows.Forms.Label lblScreenTitle;
        private System.Windows.Forms.Label lblScreenHint;
        private System.Windows.Forms.Panel pnlCameraSlot;
        private System.Windows.Forms.Label lblCamera;
        private ComboField cboCamera;
        private System.Windows.Forms.Panel pnlFooter;
        private StatusLine lblStatus;
        private System.Windows.Forms.FlowLayoutPanel pnlActions;
        private FlatButton btnPrimary;
        private FlatButton btnSecondary;
        private System.Windows.Forms.Panel pnlCameraArea;
        private CameraView cameraView;
        private System.Windows.Forms.Panel pnlGallery;
        private System.Windows.Forms.FlowLayoutPanel flpImages;
        private System.Windows.Forms.Panel pnlFolder;
        private System.Windows.Forms.Label lnkOpenFolder;
        private SettingsPanel pnlSettings;
    }
}
