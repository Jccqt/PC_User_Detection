using FaceAiSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace FaceDetection
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string fullPath = Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\..\PCUserDetection\CapturedImages\"));
            string anonymousPath = Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\..\PCUserDetection\AnonymousImages\"));

            string[] imageFiles = Directory.GetFiles(fullPath, "*.*").
                Where(file => file.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)).ToArray();

            Console.WriteLine(checkImages(imageFiles, fullPath, anonymousPath));
        }

        private static bool checkImages(string[] imageFiles, string fullPath, string anonymousPath)
        {
            foreach (string userImage in imageFiles)
            {
                var img1 = Image.Load<Rgb24>(userImage);
                var img2 = Image.Load<Rgb24>(anonymousPath + "Anonymous.jpeg");

                var det = FaceAiSharpBundleFactory.CreateFaceDetectorWithLandmarks();
                var rec = FaceAiSharpBundleFactory.CreateFaceEmbeddingsGenerator();

                var firstFace = det.DetectFaces(img1).First();
                var secondFace = det.DetectFaces(img2).First();

                rec.AlignFaceUsingLandmarks(img1, firstFace.Landmarks!);
                rec.AlignFaceUsingLandmarks(img2, secondFace.Landmarks!);

                var embedding1 = rec.GenerateEmbedding(img1);
                var embedding2 = rec.GenerateEmbedding(img2);

                var dot = FaceAiSharp.Extensions.GeometryExtensions.Dot(embedding1, embedding2);

                if (dot >= 0.42)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
