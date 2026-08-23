using FaceAiSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
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
        /// Returns true when every face in <paramref name="anonymousImagePath"/> matches
        /// one of the registered images in <paramref name="capturedImagesDirectory"/>.
        /// </summary>
        public bool IsUserVerified(string anonymousImagePath, string capturedImagesDirectory)
        {
            List<float[]> anonymousEmbeddings = GenerateEmbeddings(anonymousImagePath, false);

            // no face in the captured frame, so there is nobody to verify
            if (anonymousEmbeddings.Count == 0) return false;

            string[] imageFiles = Directory.GetFiles(capturedImagesDirectory, "*.*").
                Where(file => file.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)).ToArray();

            var registeredEmbeddings = new List<float[]>();

            foreach (string userImage in imageFiles)
            {
                float[] userEmbedding = ReadRegisteredEmbedding(userImage);
                if (userEmbedding != null) registeredEmbeddings.Add(userEmbedding);
            }

            // everyone in the frame has to be someone registered: a stranger standing
            // next to a registered user is still a stranger in front of the machine
            return anonymousEmbeddings.All(anonymousEmbedding => registeredEmbeddings.Any(
                userEmbedding => FaceAiSharp.Extensions.GeometryExtensions.Dot(
                    anonymousEmbedding, userEmbedding) >= MatchThreshold));
        }

        /// <summary>
        /// Returns the embedding of the face in a registered image, or null when there
        /// is none to read. A registered image is a portrait of one person, so only its
        /// largest face counts.
        /// </summary>
        private float[] ReadRegisteredEmbedding(string imagePath)
        {
            try
            {
                List<float[]> embeddings = GenerateEmbeddings(imagePath, true);
                return embeddings.Count > 0 ? embeddings[0] : null;
            }
            catch (Exception ex)
            {
                // the file is empty, truncated or not an image at all; it is skipped
                // for the same reason one without a detectable face is, rather than
                // aborting the comparison against the remaining images
                Console.WriteLine(ex);
                return null;
            }
        }

        /// <summary>
        /// Returns an embedding for every face in the image, or only for the largest
        /// one when <paramref name="largestFaceOnly"/> is set. The list comes back
        /// empty when the image contains no detectable face.
        /// </summary>
        private List<float[]> GenerateEmbeddings(string imagePath, bool largestFaceOnly)
        {
            var embeddings = new List<float[]>();

            using (var image = ImageSharpImage.Load<Rgb24>(imagePath))
            {
                // the detector hands the faces back in no particular order, so they are
                // sorted by box area: taking the largest is then a deliberate choice
                // rather than whichever face happened to come out first
                IEnumerable<FaceDetectorResult> faces = faceDetector.DetectFaces(image)
                    .Where(face => face.Landmarks != null)
                    .OrderByDescending(face => face.Box.Width * face.Box.Height);

                if (largestFaceOnly) faces = faces.Take(1);

                foreach (var face in faces)
                {
                    // aligning mutates the image it is handed, so each face is aligned
                    // on its own copy and the frame itself is left intact for the next
                    using (var alignedFace = image.Clone())
                    {
                        embeddingsGenerator.AlignFaceUsingLandmarks(alignedFace, face.Landmarks);
                        embeddings.Add(embeddingsGenerator.GenerateEmbedding(alignedFace));
                    }
                }
            }

            return embeddings;
        }
    }
}
