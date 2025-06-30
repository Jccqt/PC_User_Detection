using AForge.Video;
using AForge.Video.DirectShow;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace PCUserDetection
{
    public partial class UserFaceDetector : Form
    {
        // Objects from AForge Framework
        FilterInfoCollection filterInfoCollection; // will store the available camera devices
        VideoCaptureDevice videoCaptureDevice; // will capture video from the webcam
        Bitmap currentFrame; // current frame from webcam
        AddUser addUser;

        public UserFaceDetector()
        {
            InitializeComponent();
        }

        private void UserFaceDetector_Load(object sender, EventArgs e)
        {
            filterInfoCollection = new FilterInfoCollection(FilterCategory.VideoInputDevice); // will get all camera devices

            if(filterInfoCollection.Count == 0)
            {
                MessageBox.Show("No camera devices found.", "Alert", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            } 
            else
            {
                // will insert all camera devices to cbCamera combobox
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
                    getLocation();
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
            videoCaptureDevice.NewFrame -= FinalFrame_NewFrame;
            videoCaptureDevice.Stop();

            if (addUser == null)
            {
                addUser = new AddUser();
            }
            addUser.Show();
            this.Hide();
        }

        private void cbCamera_SelectedIndexChanged(object sender, EventArgs e)
        {
            // will restart the image capture if the camera being used was changed
            if(videoCaptureDevice != null)
            {
                videoCaptureDevice.Stop();
                videoCaptureDevice = null;
                pbCamera.Image = null;
                videoCaptureDevice = new VideoCaptureDevice(filterInfoCollection[cbCamera.SelectedIndex].MonikerString);
                videoCaptureDevice.NewFrame += FinalFrame_NewFrame;
                videoCaptureDevice.Start();
            }
        }

        private async Task getLocation()
        {
            string token = Environment.GetEnvironmentVariable("IPINFO_TOKEN");
            
            using (HttpClient client = new HttpClient())
            {
                string url = $"https://ipinfo.io?token={token}";
                string json = await client.GetStringAsync(url);
                IpInfoResponse location = JsonConvert.DeserializeObject<IpInfoResponse>(json);

                Console.WriteLine($"IP: {location.ip}");
                Console.WriteLine($"City: {location.city}");
                Console.WriteLine($"Region: {location.region}");
                Console.WriteLine($"Country: {location.country}");
                Console.WriteLine($"Coordinates: {location.loc}");
                Console.WriteLine($"Postal: {location.postal}");
                Console.WriteLine($"TimeZone: {location.timezone}");
                Console.WriteLine($"ISP: {location.org}");
            }
        }
    }

    public class IpInfoResponse
    {
        public string ip { get; set; }
        public string city { get; set; }
        public string region { get; set; }
        public string country { get; set; }
        public string loc { get; set; } // latitude and longitude
        public string org { get; set; } // ISP
        public string postal { get; set; }
        public string timezone { get; set; }
    }
}
