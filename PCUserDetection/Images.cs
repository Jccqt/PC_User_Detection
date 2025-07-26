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
    public partial class Images : Form
    {
        UserFaceDetector userFaceDetector = UserFaceDetector.GetUserFaceDetectorInstance();
        private static Images imagesInstance;

        public Images()
        {
            InitializeComponent();
        }

        public static Images GetImagesInstance()
        {
            if(imagesInstance == null)
            {
                imagesInstance = new Images();
            }
            return imagesInstance;
        }

        private void Images_Load(object sender, EventArgs e)
        {
            RefreshImages();
        }

        public void RefreshImages()
        {
            flpImages.Controls.Clear();

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string fullPath = Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\PCUserDetection\CapturedImages\"));

            string[] imageFiles = Directory.GetFiles(fullPath, "*.*").
                Where(file => file.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)).ToArray();

            foreach (string imageFile in imageFiles)
            {
                var image = new Image();

                image.btnDelete.Tag = imageFile;
                image.pbImage.ImageLocation = imageFile;
                image.pbImage.SizeMode = PictureBoxSizeMode.StretchImage;

                flpImages.Controls.Add(image);
            }
        }

        private void btnReturn_Click(object sender, EventArgs e)
        {

            userFaceDetector.Show();
            this.Hide();
        }
    }
}
