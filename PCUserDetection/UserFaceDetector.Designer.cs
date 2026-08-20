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
            this.pnlNav = new System.Windows.Forms.Panel();
            this.lblBrand = new System.Windows.Forms.Label();
            this.flpNav = new System.Windows.Forms.FlowLayoutPanel();
            this.pnlAppearance = new System.Windows.Forms.Panel();
            this.lblAppearance = new System.Windows.Forms.Label();
            this.flpModes = new System.Windows.Forms.FlowLayoutPanel();
            this.lblCount = new System.Windows.Forms.Label();
            this.pnlMain = new System.Windows.Forms.Panel();
            this.pnlHeader = new System.Windows.Forms.Panel();
            this.lblScreenTitle = new System.Windows.Forms.Label();
            this.lblScreenHint = new System.Windows.Forms.Label();
            this.pnlCameraSlot = new System.Windows.Forms.Panel();
            this.pnlCameraFrame = new System.Windows.Forms.Panel();
            this.pnlCameraClip = new System.Windows.Forms.Panel();
            this.cbCamera = new System.Windows.Forms.ComboBox();
            this.pnlFooter = new System.Windows.Forms.Panel();
            this.pnlStatus = new System.Windows.Forms.Panel();
            this.pnlStatusDot = new System.Windows.Forms.Panel();
            this.lblStatus = new System.Windows.Forms.Label();
            this.pnlActions = new System.Windows.Forms.FlowLayoutPanel();
            this.btnPrimary = new System.Windows.Forms.Button();
            this.btnSecondary = new System.Windows.Forms.Button();
            this.cameraView = new PCUserDetection.CameraView();
            this.pnlGallery = new System.Windows.Forms.Panel();
            this.flpImages = new System.Windows.Forms.FlowLayoutPanel();
            this.pnlSettings = new PCUserDetection.SettingsPanel();
            this.pnlNav.SuspendLayout();
            this.pnlAppearance.SuspendLayout();
            this.pnlMain.SuspendLayout();
            this.pnlHeader.SuspendLayout();
            this.pnlCameraSlot.SuspendLayout();
            this.pnlCameraFrame.SuspendLayout();
            this.pnlFooter.SuspendLayout();
            this.pnlStatus.SuspendLayout();
            this.pnlActions.SuspendLayout();
            this.pnlGallery.SuspendLayout();
            this.SuspendLayout();
            //
            // pnlNav
            //
            this.pnlNav.Dock = System.Windows.Forms.DockStyle.Left;
            this.pnlNav.Name = "pnlNav";
            this.pnlNav.Padding = new System.Windows.Forms.Padding(12, 0, 12, 0);
            this.pnlNav.Size = new System.Drawing.Size(208, 640);
            this.pnlNav.Paint += new System.Windows.Forms.PaintEventHandler(this.pnlNav_Paint);
            //
            // lblBrand
            //
            this.lblBrand.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblBrand.Font = Theme.Brand;
            this.lblBrand.Height = 80;
            this.lblBrand.Name = "lblBrand";
            this.lblBrand.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblBrand.Text = "User Detection";
            this.lblBrand.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
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
            this.pnlAppearance.Height = 62;
            this.pnlAppearance.Name = "pnlAppearance";
            //
            // lblAppearance
            //
            this.lblAppearance.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblAppearance.Font = Theme.Small;
            this.lblAppearance.Height = 24;
            this.lblAppearance.Name = "lblAppearance";
            this.lblAppearance.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblAppearance.Text = "Appearance";
            this.lblAppearance.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // flpModes
            //
            // the three buttons themselves are built in BuildThemeButtons, next to
            // the navigation buttons they are styled like
            this.flpModes.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flpModes.Name = "flpModes";
            this.flpModes.Padding = new System.Windows.Forms.Padding(4, 0, 0, 0);
            this.flpModes.WrapContents = false;
            //
            // lblCount
            //
            this.lblCount.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblCount.Font = Theme.Small;
            this.lblCount.Height = 46;
            this.lblCount.Name = "lblCount";
            this.lblCount.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblCount.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // pnlMain
            //
            this.pnlMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlMain.Name = "pnlMain";
            this.pnlMain.Padding = new System.Windows.Forms.Padding(28, 20, 28, 20);
            //
            // pnlHeader
            //
            this.pnlHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlHeader.Height = 72;
            this.pnlHeader.Name = "pnlHeader";
            //
            // lblScreenTitle
            //
            this.lblScreenTitle.AutoSize = true;
            this.lblScreenTitle.Font = Theme.Heading;
            this.lblScreenTitle.Location = new System.Drawing.Point(0, 2);
            this.lblScreenTitle.Name = "lblScreenTitle";
            //
            // lblScreenHint
            //
            this.lblScreenHint.AutoSize = true;
            this.lblScreenHint.Font = Theme.Body;
            this.lblScreenHint.Location = new System.Drawing.Point(0, 33);
            this.lblScreenHint.Name = "lblScreenHint";
            //
            // pnlCameraSlot
            //
            this.pnlCameraSlot.Dock = System.Windows.Forms.DockStyle.Right;
            this.pnlCameraSlot.Name = "pnlCameraSlot";
            this.pnlCameraSlot.Padding = new System.Windows.Forms.Padding(0, 8, 0, 0);
            this.pnlCameraSlot.Width = 264;
            //
            // pnlCameraFrame
            //
            // A combo box always paints its own light border and drop arrow, and no
            // property turns them off. It is put in a panel smaller than itself so
            // both are clipped away, and the frame paints the border and the
            // chevron in the colours of the theme instead.
            this.pnlCameraFrame.Cursor = System.Windows.Forms.Cursors.Hand;
            this.pnlCameraFrame.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlCameraFrame.Height = 34;
            this.pnlCameraFrame.Name = "pnlCameraFrame";
            this.pnlCameraFrame.Padding = new System.Windows.Forms.Padding(1, 1, 24, 1);
            this.pnlCameraFrame.Size = new System.Drawing.Size(264, 34);
            this.pnlCameraFrame.Paint += new System.Windows.Forms.PaintEventHandler(this.pnlCameraFrame_Paint);
            this.pnlCameraFrame.Click += new System.EventHandler(this.pnlCameraFrame_Click);
            //
            // pnlCameraClip
            //
            // sized rather than docked, because a panel clips its children to its
            // own bounds and not to its padding
            this.pnlCameraClip.Location = new System.Drawing.Point(1, 4);
            this.pnlCameraClip.Name = "pnlCameraClip";
            this.pnlCameraClip.Size = new System.Drawing.Size(239, 26);
            //
            // cbCamera
            //
            this.cbCamera.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.cbCamera.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbCamera.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cbCamera.Font = Theme.Body;
            this.cbCamera.ItemHeight = 32;
            this.cbCamera.Location = new System.Drawing.Point(-1, -5);
            this.cbCamera.Name = "cbCamera";
            this.cbCamera.Size = new System.Drawing.Size(264, 34);
            this.cbCamera.TabIndex = 0;
            this.cbCamera.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.cbCamera_DrawItem);
            this.cbCamera.SelectedIndexChanged += new System.EventHandler(this.cbCamera_SelectedIndexChanged);
            //
            // pnlFooter
            //
            this.pnlFooter.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlFooter.Height = 76;
            this.pnlFooter.Name = "pnlFooter";
            this.pnlFooter.Padding = new System.Windows.Forms.Padding(0, 20, 0, 0);
            //
            // pnlStatus
            //
            this.pnlStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlStatus.Name = "pnlStatus";
            //
            // pnlStatusDot
            //
            this.pnlStatusDot.Location = new System.Drawing.Point(2, 15);
            this.pnlStatusDot.Name = "pnlStatusDot";
            this.pnlStatusDot.Size = new System.Drawing.Size(10, 10);
            this.pnlStatusDot.Paint += new System.Windows.Forms.PaintEventHandler(this.pnlStatusDot_Paint);
            //
            // lblStatus
            //
            this.lblStatus.AutoSize = true;
            this.lblStatus.Font = Theme.Status;
            this.lblStatus.Location = new System.Drawing.Point(22, 10);
            this.lblStatus.Name = "lblStatus";
            //
            // pnlActions
            //
            this.pnlActions.Dock = System.Windows.Forms.DockStyle.Right;
            this.pnlActions.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.pnlActions.Name = "pnlActions";
            this.pnlActions.Size = new System.Drawing.Size(300, 56);
            this.pnlActions.WrapContents = false;
            //
            // btnPrimary
            //
            this.btnPrimary.Name = "btnPrimary";
            this.btnPrimary.Size = new System.Drawing.Size(132, 40);
            this.btnPrimary.TabIndex = 1;
            this.btnPrimary.Click += new System.EventHandler(this.btnPrimary_Click);
            //
            // btnSecondary
            //
            this.btnSecondary.Margin = new System.Windows.Forms.Padding(3, 3, 10, 3);
            this.btnSecondary.Name = "btnSecondary";
            this.btnSecondary.Size = new System.Drawing.Size(110, 40);
            this.btnSecondary.TabIndex = 2;
            this.btnSecondary.Text = "Retake";
            this.btnSecondary.Click += new System.EventHandler(this.btnSecondary_Click);
            //
            // cameraView
            //
            this.cameraView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cameraView.Name = "cameraView";
            //
            // pnlGallery
            //
            this.pnlGallery.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlGallery.Name = "pnlGallery";
            this.pnlGallery.Visible = false;
            //
            // flpImages
            //
            this.flpImages.AutoScroll = true;
            this.flpImages.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flpImages.Name = "flpImages";
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
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
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
            this.pnlNav.Controls.Add(this.lblBrand);
            this.pnlAppearance.Controls.Add(this.flpModes);
            this.pnlAppearance.Controls.Add(this.lblAppearance);

            this.pnlNav.Controls.Add(this.pnlAppearance);
            this.pnlNav.Controls.Add(this.lblCount);

            this.pnlCameraClip.Controls.Add(this.cbCamera);
            this.pnlCameraFrame.Controls.Add(this.pnlCameraClip);
            this.pnlCameraSlot.Controls.Add(this.pnlCameraFrame);

            this.pnlHeader.Controls.Add(this.lblScreenTitle);
            this.pnlHeader.Controls.Add(this.lblScreenHint);
            this.pnlHeader.Controls.Add(this.pnlCameraSlot);

            this.pnlStatus.Controls.Add(this.pnlStatusDot);
            this.pnlStatus.Controls.Add(this.lblStatus);

            this.pnlActions.Controls.Add(this.btnPrimary);
            this.pnlActions.Controls.Add(this.btnSecondary);

            this.pnlFooter.Controls.Add(this.pnlStatus);
            this.pnlFooter.Controls.Add(this.pnlActions);

            this.pnlGallery.Controls.Add(this.flpImages);

            this.pnlMain.Controls.Add(this.cameraView);
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
            this.pnlCameraFrame.ResumeLayout(false);
            this.pnlCameraSlot.ResumeLayout(false);
            this.pnlHeader.ResumeLayout(false);
            this.pnlHeader.PerformLayout();
            this.pnlStatus.ResumeLayout(false);
            this.pnlStatus.PerformLayout();
            this.pnlActions.ResumeLayout(false);
            this.pnlFooter.ResumeLayout(false);
            this.pnlGallery.ResumeLayout(false);
            this.pnlMain.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel pnlNav;
        private System.Windows.Forms.Label lblBrand;
        private System.Windows.Forms.FlowLayoutPanel flpNav;
        private System.Windows.Forms.Panel pnlAppearance;
        private System.Windows.Forms.Label lblAppearance;
        private System.Windows.Forms.FlowLayoutPanel flpModes;
        private System.Windows.Forms.Label lblCount;
        private System.Windows.Forms.Panel pnlMain;
        private System.Windows.Forms.Panel pnlHeader;
        private System.Windows.Forms.Label lblScreenTitle;
        private System.Windows.Forms.Label lblScreenHint;
        private System.Windows.Forms.Panel pnlCameraSlot;
        private System.Windows.Forms.Panel pnlCameraFrame;
        private System.Windows.Forms.Panel pnlCameraClip;
        private System.Windows.Forms.ComboBox cbCamera;
        private System.Windows.Forms.Panel pnlFooter;
        private System.Windows.Forms.Panel pnlStatus;
        private System.Windows.Forms.Panel pnlStatusDot;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.FlowLayoutPanel pnlActions;
        private System.Windows.Forms.Button btnPrimary;
        private System.Windows.Forms.Button btnSecondary;
        private CameraView cameraView;
        private System.Windows.Forms.Panel pnlGallery;
        private System.Windows.Forms.FlowLayoutPanel flpImages;
        private SettingsPanel pnlSettings;
    }
}
