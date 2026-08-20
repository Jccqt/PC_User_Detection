using AForge.Video;
using AForge.Video.DirectShow;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace PCUserDetection
{
    public partial class UserFaceDetector : Form
    {
        // Objects from AForge Framework
        FilterInfoCollection filterInfoCollection; // will store the available camera devices
        VideoCaptureDevice videoCaptureDevice; // will capture video from the webcam
        Bitmap currentFrame; // current frame from webcam
        AddUser addUser;
        Images images;
        private static UserFaceDetector userFaceDetectorInstance;

        public UserFaceDetector()
        {
            InitializeComponent();
            
        }

        public static UserFaceDetector GetUserFaceDetectorInstance()
        {
            if(userFaceDetectorInstance == null)
            {
                userFaceDetectorInstance = new UserFaceDetector();
            }
            return userFaceDetectorInstance;
        }

        private void UserFaceDetector_Load(object sender, EventArgs e)
        {
            // Initializes singleton for these classes when UserFaceDetector page loads
            addUser = AddUser.GetAddUserInstance();
            images = Images.GetImagesInstance();

            filterInfoCollection = new FilterInfoCollection(FilterCategory.VideoInputDevice); // will get all camera devices

            if(filterInfoCollection.Count == 0)
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

            if(pbCamera.InvokeRequired)
            {
                pbCamera.Invoke(new Action(() => {
                    if(pbCamera.Image != null) pbCamera.Image.Dispose();
                    pbCamera.Image = currentFrame; // will display camera feed on the screen using picture box
                }));
            } 
            else
            {
                if (pbCamera.Image != null) pbCamera.Image.Dispose();
                pbCamera.Image = currentFrame; // will display camera feed on the screen using picture box
            }    
        }

        private void btnDetect_Click(object sender, EventArgs e)
        {
            if(currentFrame != null)
            {
                // the frame save and the FaceDetection child process can both
                // throw, so the whole body has to be guarded here.
                try
                {
                    string filename = "Anonymous.jpeg";
                    string directory = Directory.GetParent(System.Environment.CurrentDirectory).Parent.FullName + @"\AnonymousImages";
                    string filepath = System.IO.Path.Combine(directory, filename);
                    currentFrame.Save(filepath, System.Drawing.Imaging.ImageFormat.Jpeg);
                    lblAlert.Visible = true;

                    if (RunFaceAiSharpConsole())
                    {
                        lblAlert.Text = "The user was verified";
                        lblAlert.ForeColor = System.Drawing.Color.Green;
                    }
                    else
                    {
                        lblAlert.Text = "The user was anonymous";
                        lblAlert.ForeColor = System.Drawing.Color.Red;
                    }
                }
                catch (Exception ex)
                {
                    lblAlert.Visible = true;
                    lblAlert.Text = "The detection failed";
                    lblAlert.ForeColor = System.Drawing.Color.Red;

                    // for debugging purposes
                    Console.WriteLine(ex);

                    MessageBox.Show("The detection could not be completed.\n\n" + ex.Message,
                        "Alert", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private bool RunFaceAiSharpConsole()
        {
            bool result = false;
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = Directory.GetParent(System.Environment.CurrentDirectory).Parent.FullName + @"..\..\FaceDetection\bin\Debug\net8.0\FaceDetection.exe",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = Process.Start(psi))
            {
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                if(bool.TryParse(output.Trim(), out bool res))
                {
                    result = res;
                }
            }
            return result;
        }

        private void btnRestart_Click(object sender, EventArgs e)
        {
            lblAlert.Visible = false;
            videoCaptureDevice.NewFrame -= FinalFrame_NewFrame;
            videoCaptureDevice.NewFrame += FinalFrame_NewFrame;
        }

        private void UserFaceDetector_FormClosing(object sender, FormClosingEventArgs e)
        {
            Environment.Exit(0);
        }

        private void btnAddUser_Click(object sender, EventArgs e)
        {
            if(cbCamera.SelectedIndex > 0)
            {
                // will stop image capture on main page when going to Add User page
                videoCaptureDevice.SignalToStop();
                videoCaptureDevice.WaitForStop();
                videoCaptureDevice.NewFrame -= FinalFrame_NewFrame;
            }
            addUser.Show();
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

        private void btnImages_Click(object sender, EventArgs e)
        {
            if (cbCamera.SelectedIndex > 0)
            {
                // will stop image capture on main page when going to Images page
                videoCaptureDevice.SignalToStop();
                videoCaptureDevice.WaitForStop();
                videoCaptureDevice.NewFrame -= FinalFrame_NewFrame;
            }

            images.Show();
            this.Hide();
        }
    }
}
