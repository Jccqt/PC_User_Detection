using FaceAiSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.IO;
using System.Linq;
// ImageSharp and System.Drawing both have an Image type, so the ImageSharp one
// is aliased to keep the calls below unambiguous.
using ImageSharpImage = SixLabors.ImageSharp.Image;

namespace PCUserDetection
{
    /// <summary>
    /// Compares the captured "anonymous" frame against the registered user images.
    /// This used to live in a separate FaceDetection console app that the form
    /// launched as a child process and parsed stdout from; it now runs in-process.
    /// </summary>
    public class FaceRecognizer
    {
        // two faces count as the same person at or above this cosine similarity
        private const double MatchThreshold = 0.42;

        private static FaceRecognizer faceRecognizerInstance;

        // the ONNX models behind these are expensive to load, so they are created
        // once and reused for every detection
        private readonly IFaceDetectorWithLandmarks faceDetector;
        private readonly IFaceEmbeddingsGenerator embeddingsGenerator;

        private FaceRecognizer()
        {
            faceDetector = FaceAiSharpBundleFactory.CreateFaceDetectorWithLandmarks();
            embeddingsGenerator = FaceAiSharpBundleFactory.CreateFaceEmbeddingsGenerator();
        }

        public static FaceRecognizer GetFaceRecognizerInstance()
        {
            if (faceRecognizerInstance == null)
            {
                faceRecognizerInstance = new FaceRecognizer();
            }
            return faceRecognizerInstance;
        }

        /// <summary>
        /// Returns true when the face in <paramref name="anonymousImagePath"/> matches
        /// any of the registered images in <paramref name="capturedImagesDirectory"/>.
        /// </summary>
        public bool IsUserVerified(string anonymousImagePath, string capturedImagesDirectory)
        {
            float[] anonymousEmbedding = GenerateEmbedding(anonymousImagePath);

            // no face in the captured frame, so there is nobody to verify
            if (anonymousEmbedding == null) return false;

            string[] imageFiles = Directory.GetFiles(capturedImagesDirectory, "*.*").
                Where(file => file.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)).ToArray();

            foreach (string userImage in imageFiles)
            {
                float[] userEmbedding = GenerateEmbedding(userImage);

                // a registered image without a detectable face is skipped rather than
                // aborting the comparison against the remaining images
                if (userEmbedding == null) continue;

                if (FaceAiSharp.Extensions.GeometryExtensions.Dot(anonymousEmbedding, userEmbedding) >= MatchThreshold)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns the face embedding for the first face found in the image,
        /// or null when the image contains no detectable face.
        /// </summary>
        private float[] GenerateEmbedding(string imagePath)
        {
            using (var image = ImageSharpImage.Load<Rgb24>(imagePath))
            {
                var faces = faceDetector.DetectFaces(image).ToList();

                if (faces.Count == 0 || faces[0].Landmarks == null) return null;

                var face = faces[0];

                embeddingsGenerator.AlignFaceUsingLandmarks(image, face.Landmarks);
                return embeddingsGenerator.GenerateEmbedding(image);
            }
        }
    }
}
