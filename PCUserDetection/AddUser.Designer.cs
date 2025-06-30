namespace PCUserDetection
{
    partial class AddUser
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
            this.label1 = new System.Windows.Forms.Label();
            this.cbCamera = new System.Windows.Forms.ComboBox();
            this.btnRestart = new System.Windows.Forms.Button();
            this.btnAdd = new System.Windows.Forms.Button();
            this.pbCamera = new System.Windows.Forms.PictureBox();
            this.lblImageFileDir = new System.Windows.Forms.Label();
            this.btnReturn = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.pbCamera)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Poppins", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(149, 12);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(99, 34);
            this.label1.TabIndex = 3;
            this.label1.Text = "Camera:";
            // 
            // cbCamera
            // 
            this.cbCamera.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbCamera.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.cbCamera.Font = new System.Drawing.Font("Poppins", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cbCamera.FormattingEnabled = true;
            this.cbCamera.Location = new System.Drawing.Point(254, 12);
            this.cbCamera.Name = "cbCamera";
            this.cbCamera.Size = new System.Drawing.Size(236, 36);
            this.cbCamera.TabIndex = 2;
            this.cbCamera.SelectedIndexChanged += new System.EventHandler(this.cbCamera_SelectedIndexChanged);
            // 
            // btnRestart
            // 
            this.btnRestart.Font = new System.Drawing.Font("Poppins", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnRestart.Location = new System.Drawing.Point(135, 522);
            this.btnRestart.Name = "btnRestart";
            this.btnRestart.Size = new System.Drawing.Size(113, 36);
            this.btnRestart.TabIndex = 7;
            this.btnRestart.Text = "Restart";
            this.btnRestart.UseVisualStyleBackColor = true;
            this.btnRestart.Click += new System.EventHandler(this.btnRestart_Click);
            // 
            // btnAdd
            // 
            this.btnAdd.Font = new System.Drawing.Font("Poppins", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnAdd.Location = new System.Drawing.Point(390, 522);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(113, 36);
            this.btnAdd.TabIndex = 6;
            this.btnAdd.Text = "Add";
            this.btnAdd.UseVisualStyleBackColor = true;
            this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);
            // 
            // pbCamera
            // 
            this.pbCamera.Location = new System.Drawing.Point(57, 76);
            this.pbCamera.Name = "pbCamera";
            this.pbCamera.Size = new System.Drawing.Size(543, 350);
            this.pbCamera.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pbCamera.TabIndex = 5;
            this.pbCamera.TabStop = false;
            // 
            // lblImageFileDir
            // 
            this.lblImageFileDir.AutoSize = true;
            this.lblImageFileDir.Font = new System.Drawing.Font("Poppins", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblImageFileDir.Location = new System.Drawing.Point(53, 437);
            this.lblImageFileDir.MaximumSize = new System.Drawing.Size(500, 0);
            this.lblImageFileDir.Name = "lblImageFileDir";
            this.lblImageFileDir.Size = new System.Drawing.Size(113, 22);
            this.lblImageFileDir.TabIndex = 8;
            this.lblImageFileDir.Text = "<Image file label>";
            this.lblImageFileDir.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblImageFileDir.Visible = false;
            // 
            // btnReturn
            // 
            this.btnReturn.Font = new System.Drawing.Font("Poppins", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnReturn.Location = new System.Drawing.Point(12, 11);
            this.btnReturn.Name = "btnReturn";
            this.btnReturn.Size = new System.Drawing.Size(85, 37);
            this.btnReturn.TabIndex = 9;
            this.btnReturn.Text = "Return";
            this.btnReturn.UseVisualStyleBackColor = true;
            this.btnReturn.Click += new System.EventHandler(this.btnReturn_Click);
            // 
            // AddUser
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(674, 570);
            this.Controls.Add(this.btnReturn);
            this.Controls.Add(this.lblImageFileDir);
            this.Controls.Add(this.btnRestart);
            this.Controls.Add(this.btnAdd);
            this.Controls.Add(this.pbCamera);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.cbCamera);
            this.Name = "AddUser";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Add User";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.AddUser_FormClosing);
            this.Load += new System.EventHandler(this.AddUser_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pbCamera)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox cbCamera;
        private System.Windows.Forms.Button btnRestart;
        private System.Windows.Forms.PictureBox pbCamera;
        private System.Windows.Forms.Label lblImageFileDir;
        private System.Windows.Forms.Button btnReturn;
        private System.Windows.Forms.Button btnAdd;
    }
}