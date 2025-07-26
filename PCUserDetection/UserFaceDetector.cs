using AForge.Video;
using AForge.Video.DirectShow;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Http;
using Newtonsoft.Json;
using Google.Apis.Gmail.v1;
using Google.Apis.Auth.OAuth2;
using System.Threading;
using Google.Apis.Util.Store;
using Google.Apis.Services;
using MimeKit;

namespace PCUserDetection
{
    public partial class UserFaceDetector : Form
    {
        // Objects from AForge Framework
        FilterInfoCollection filterInfoCollection; // will store the available camera devices
        VideoCaptureDevice videoCaptureDevice; // will capture video from the webcam
        Bitmap currentFrame; // current frame from webcam
        AddUser addUser;
        Email email;
        Images images;
        private static UserFaceDetector userFaceDetectorInstance;

        static string[] Scopes = { GmailService.Scope.GmailSend };
        static string ApplicationName = "PCUserDetection";

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
            email = Email.GetEmailInstance();
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

        private async void btnDetect_Click(object sender, EventArgs e)
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
                    string deviceLocation = await getLocation();
                    var service = AuthenticateGmail();
                    SendEmail(service, "me", Properties.Settings.Default.UserEmail, "Anonymous user", deviceLocation);
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

        private async Task<string> getLocation()
        {
            string token = Environment.GetEnvironmentVariable("IPINFO_TOKEN");
            
            using (HttpClient client = new HttpClient())
            {
                string url = $"https://ipinfo.io?token={token}";
                string json = await client.GetStringAsync(url);
                IpInfoResponse location = JsonConvert.DeserializeObject<IpInfoResponse>(json);

                // for debugging purposes
                Console.WriteLine($"IP: {location.ip}");
                Console.WriteLine($"City: {location.city}");
                Console.WriteLine($"Region: {location.region}");
                Console.WriteLine($"Country: {location.country}");
                Console.WriteLine($"Coordinates: {location.loc}");
                Console.WriteLine($"Postal: {location.postal}");
                Console.WriteLine($"TimeZone: {location.timezone}");
                Console.WriteLine($"ISP: {location.org}");

                // will return the email message
                return $@"
                    Your device was being accessed by an anonymous user!
                    
                    Here's the IP and Location details:

                    IP: {location.ip}
                    City: {location.city}
                    Region: {location.region}
                    Country: {location.country}
                    Coordinates: {location.loc}
                    Postal: {location.postal}
                    TimeZone: {location.timezone}
                    ISP: {location.org}";
            }
        }

        private void btnSetEmail_Click(object sender, EventArgs e)
        {
            if (cbCamera.SelectedIndex > 0)
            {
                // will stop image capture on main page when going to Email page
                videoCaptureDevice.SignalToStop();
                videoCaptureDevice.WaitForStop();
                videoCaptureDevice.NewFrame -= FinalFrame_NewFrame;
            }
            email.Show();
            this.Hide();
        }

        private GmailService AuthenticateGmail()
        {
            string credentialsPath = Environment.GetEnvironmentVariable("GMAIL_CREDENTIALS_PATH");
            string tokenPath = Environment.GetEnvironmentVariable("GMAIL_TOKEN_PATH");

            UserCredential credential;

            using (var stream = new FileStream(credentialsPath, FileMode.Open, FileAccess.Read))
            {
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    new[] { GmailService.Scope.GmailSend },
                    "user",
                    CancellationToken.None,
                    new FileDataStore(tokenPath, true)).Result;
            }

            return new GmailService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });
        }

        private void SendEmail(GmailService service, string userID, string to, string subject, string bodyText)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("PCUserDection", "jcbcalayag@gmail.com"));
            message.To.Add(new MailboxAddress("", to));
            message.Subject = subject;
            message.Body = new TextPart("plain") { Text = bodyText };

            using (var memoryStream = new MemoryStream())
            {
                message.WriteTo(memoryStream);
                var rawMessage = Convert.ToBase64String(memoryStream.ToArray())
                    .Replace("+", "-")
                    .Replace("/", "_")
                    .Replace("=", "");

                var gmailMessage = new Google.Apis.Gmail.v1.Data.Message { Raw = rawMessage };
                service.Users.Messages.Send(gmailMessage, userID).Execute();

                Console.WriteLine("Email sent!");
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
