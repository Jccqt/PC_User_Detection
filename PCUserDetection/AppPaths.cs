using System;
using System.IO;

namespace PCUserDetection
{
    /// <summary>
    /// The folders the app reads and writes images from, and the files it
    /// remembers a person's settings in.
    /// </summary>
    /// <remarks>
    /// The image folders are resolved from the folder the executable lives in,
    /// not from the working directory, so the app behaves the same whether it is
    /// started by Visual Studio, by "dotnet run", or by double-clicking the
    /// executable. Settings instead live under AppData; see <see cref="UserFolder"/>.
    /// </remarks>
    internal static class AppPaths
    {
        /// <summary>The project folder, two levels above bin\&lt;Config&gt;\.</summary>
        private static string ProjectDirectory
        {
            get { return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..")); }
        }

        /// <summary>Holds the most recent frame captured for verification.</summary>
        public static string AnonymousImages
        {
            get { return EnsureDirectory(Path.Combine(ProjectDirectory, "AnonymousImages")); }
        }

        /// <summary>Holds the registered user images to compare against.</summary>
        public static string CapturedImages
        {
            get { return EnsureDirectory(Path.Combine(ProjectDirectory, "CapturedImages")); }
        }

        /// <summary>Where the chosen theme is remembered.</summary>
        public static string ThemeSetting
        {
            get { return Path.Combine(UserFolder, "theme.txt"); }
        }

        /// <summary>
        /// Where the email alert settings are remembered. Keeping this under
        /// AppData rather than beside the executable means a clone of the
        /// repository never carries someone else's mail server or password.
        /// </summary>
        public static string EmailSetting
        {
            get { return Path.Combine(UserFolder, "email.json"); }
        }

        /// <summary>
        /// Where the file drop delivery mode writes messages instead of sending
        /// them, so the alert can be exercised without any mail account at all.
        /// </summary>
        public static string EmailDrops
        {
            get { return EnsureDirectory(Path.Combine(UserFolder, "SentMail")); }
        }

        /// <summary>
        /// The per-person folder under AppData that the settings above live in.
        /// What is kept there belongs to the person rather than to the build, so
        /// a rebuild does not lose it.
        /// </summary>
        private static string UserFolder
        {
            get
            {
                return EnsureDirectory(Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "PCUserDetection"));
            }
        }

        /// <summary>The frame captured by the Capture button.</summary>
        public static string AnonymousImage
        {
            get { return Path.Combine(AnonymousImages, "Anonymous.jpeg"); }
        }

        // a fresh clone or a cleaned build can be missing these, and creating
        // them here is cheaper than failing on the first capture
        private static string EnsureDirectory(string path)
        {
            Directory.CreateDirectory(path);
            return path;
        }
    }
}
