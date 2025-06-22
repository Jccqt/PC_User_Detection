namespace PCUserDetection
{
    partial class UserFaceDetector
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.cbCamera = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.pbCamera = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.pbCamera)).BeginInit();
            this.SuspendLayout();
            // 
            // cbCamera
            // 
            this.cbCamera.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbCamera.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.cbCamera.Font = new System.Drawing.Font("Poppins", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cbCamera.FormattingEnabled = true;
            this.cbCamera.Location = new System.Drawing.Point(160, 23);
            this.cbCamera.Name = "cbCamera";
            this.cbCamera.Size = new System.Drawing.Size(251, 36);
            this.cbCamera.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Poppins", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(70, 23);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(99, 34);
            this.label1.TabIndex = 1;
            this.label1.Text = "Camera:";
            // 
            // pbCamera
            // 
            this.pbCamera.Location = new System.Drawing.Point(76, 113);
            this.pbCamera.Name = "pbCamera";
            this.pbCamera.Size = new System.Drawing.Size(543, 350);
            this.pbCamera.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pbCamera.TabIndex = 2;
            this.pbCamera.TabStop = false;
            // 
            // UserFaceDetector
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(847, 511);
            this.Controls.Add(this.pbCamera);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.cbCamera);
            this.Name = "UserFaceDetector";
            this.Text = "PC User Face Detector";
            this.Load += new System.EventHandler(this.UserFaceDetector_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pbCamera)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox cbCamera;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.PictureBox pbCamera;
    }
}

