using AForge.Video;
using AForge.Video.DirectShow;
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
    public partial class UserFaceDetector : Form
    {
        FilterInfoCollection filterInfoCollection;
        VideoCaptureDevice videoCaptureDevice;

        public UserFaceDetector()
        {
            InitializeComponent();
        }

        private void UserFaceDetector_Load(object sender, EventArgs e)
        {
            filterInfoCollection = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            foreach (FilterInfo Device in filterInfoCollection)
                cbCamera.Items.Add(Device.Name);
            cbCamera.SelectedIndex = 0;
            videoCaptureDevice = new VideoCaptureDevice(filterInfoCollection[cbCamera.SelectedIndex].MonikerString);
            videoCaptureDevice.NewFrame += FinalFrame_NewFrame;
            videoCaptureDevice.Start();
        }

        private void FinalFrame_NewFrame(object sender, NewFrameEventArgs e)
        {
            pbCamera.Image = (Bitmap)e.Frame.Clone();
        }
    }
}
