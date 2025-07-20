using AForge.Video;
using AForge.Video.DirectShow;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PCUserDetection
{
    public partial class AddUser : Form
    {
        // Objects from AForge Framework
        FilterInfoCollection filterInfoCollection; // will store the available camera devices
        VideoCaptureDevice videoCaptureDevice; // will capture video from the webcam
        Bitmap currentFrame; // current frame from webcam
        UserFaceDetector userFaceDetector;
        public AddUser()
        {
            InitializeComponent();
        }

        private void AddUser_Load(object sender, EventArgs e)
        {
            filterInfoCollection = new FilterInfoCollection(FilterCategory.VideoInputDevice); // will get all camera devices

            if (filterInfoCollection.Count == 0)
            {
                MessageBox.Show("No camera devices found.", "Alert", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                // will insert all camera devices to cbCamera combobox
                cbCamera.Items.Add("Select Camera"); // Default selection
                foreach (FilterInfo Device in filterInfoCollection)
                    cbCamera.Items.Add(Device.Name);
                cbCamera.SelectedIndex = 0;

                videoCaptureDevice = new VideoCaptureDevice(filterInfoCollection[cbCamera.SelectedIndex].MonikerString);
                videoCaptureDevice.NewFrame += FinalFrame_NewFrame;
                videoCaptureDevice.Start();
            }
        }

        private void FinalFrame_NewFrame(object sender, NewFrameEventArgs e)
        {
            currentFrame = (Bitmap)e.Frame.Clone();

            if (pbCamera.InvokeRequired)
            {
                pbCamera.Invoke(new Action(() =>
                {
                    if (pbCamera.Image != null) pbCamera.Image.Dispose();
                    pbCamera.Image = currentFrame; // will display camera feed on the screen using picture box
                }));
            }
            else
            {
                if (pbCamera.Image != null) pbCamera.Image.Dispose();
                pbCamera.Image = currentFrame; // will display camera feed on the screen using picture box
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (currentFrame != null)
            {
                videoCaptureDevice.NewFrame -= FinalFrame_NewFrame;
                string filename = $"Image_{DateTime.Now:yyyyMMdd_HHmmss}.jpeg";
                string directory = Directory.GetParent(System.Environment.CurrentDirectory).Parent.FullName + @"\CapturedImages";
                string filepath = System.IO.Path.Combine(directory, filename);
                currentFrame.Save(filepath, System.Drawing.Imaging.ImageFormat.Jpeg);
                lblImageFileDir.Text = "Image has been captured and saved on " + directory + "\\" + filename;
                lblImageFileDir.Visible = true;
            }
        }

        private void btnRestart_Click(object sender, EventArgs e)
        {
            lblImageFileDir.Visible = false;
            videoCaptureDevice.NewFrame -= FinalFrame_NewFrame;
            videoCaptureDevice.NewFrame += FinalFrame_NewFrame;
        }

        private void AddUser_FormClosing(object sender, FormClosingEventArgs e)
        {
            Environment.Exit(0);
        }

        private void btnReturn_Click(object sender, EventArgs e)
        {
            if(cbCamera.SelectedIndex > 0)
            {
                // will stop image capture on Add User page when returning to main page
                videoCaptureDevice.SignalToStop();
                videoCaptureDevice.WaitForStop();
                videoCaptureDevice.NewFrame -= FinalFrame_NewFrame;
            }

            if(userFaceDetector == null)
            {
                userFaceDetector = new UserFaceDetector();
            }
            userFaceDetector.Show();
            this.Hide();
        }

        private void cbCamera_SelectedIndexChanged(object sender, EventArgs e)
        {
            // will stop the image capture if the camera being used was changed
            // and will only stop if the videoCaptureDevice is not null and is running
            if (videoCaptureDevice != null && videoCaptureDevice.IsRunning)
            {
                // will stop the image capture from getting input
                videoCaptureDevice.SignalToStop();
                videoCaptureDevice.WaitForStop();
                videoCaptureDevice.NewFrame -= FinalFrame_NewFrame;
                videoCaptureDevice = null;
                pbCamera.Image = null;
            }

            // will only restart if there is a selected camera
            if (cbCamera.Text != "Select Camera")
            {
                videoCaptureDevice = new VideoCaptureDevice(filterInfoCollection[cbCamera.SelectedIndex - 1].MonikerString);
                videoCaptureDevice.NewFrame += FinalFrame_NewFrame;
                videoCaptureDevice.Start();
            }
        }
    }
}
