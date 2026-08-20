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
    public partial class Image : UserControl
    {
        public Image()
        {
            InitializeComponent();
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            DialogResult deleteImgDiag = MessageBox.Show("Are you sure you want to delete this image?", "Image Alert", 
                MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);

            if(deleteImgDiag == DialogResult.Yes)
            {
                // Tag holds the full path of the image this row was built from
                File.Delete(btnDelete.Tag.ToString());

                MessageBox.Show($"Image {btnDelete.Tag.ToString()} has been successfully deleted.", "Image Alert", MessageBoxButtons.OK, MessageBoxIcon.Information);

                Images.GetImagesInstance().RefreshImages();
            }
            Console.WriteLine(btnDelete.Tag.ToString());
        }
    }
}
