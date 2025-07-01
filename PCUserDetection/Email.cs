using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PCUserDetection
{
    public partial class Email : Form
    {
        UserFaceDetector userFaceDetector;
        public Email()
        {
            InitializeComponent();
        }

        private void btnSetEmail_Click(object sender, EventArgs e)
        {
            DialogResult setEmailDiag = MessageBox.Show("Are you sure you want to set this are your email?", "Email Alert", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);

            if(setEmailDiag == DialogResult.Yes)
            {
                if (txtEmail.Text == "" || !txtEmail.Text.Contains("@") || !txtEmail.Text.Contains("."))
                {
                    MessageBox.Show("Invalid email address!\n" +
                        "Please enter a valid email address.", "Email Alert", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    Properties.Settings.Default.UserEmail = txtEmail.Text;
                    Properties.Settings.Default.Save();
                    lblEmail.Text = txtEmail.Text;
                }
            }
        }

        private void Email_Load(object sender, EventArgs e)
        {
            lblEmail.Text = Properties.Settings.Default.UserEmail;
        }

        private void btnReturn_Click(object sender, EventArgs e)
        {
            if (userFaceDetector == null)
            {
                userFaceDetector = new UserFaceDetector();
            }
            userFaceDetector.Show();
            this.Hide();
        }
    }
}
