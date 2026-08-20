using System;
using System.IO;

namespace PCUserDetection
{
    /// <summary>
    /// The folders the app reads and writes images from.
    /// </summary>
    /// <remarks>
    /// These are resolved from the folder the executable lives in, not from the
    /// working directory, so the app behaves the same whether it is started by
    /// Visual Studio, by "dotnet run", or by double-clicking the executable.
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

        /// <summary>
        /// Where the chosen theme is remembered. This one lives under AppData
        /// rather than next to the executable, so the setting belongs to the
        /// person rather than to the build, and a rebuild does not lose it.
        /// </summary>
        public static string ThemeSetting
        {
            get
            {
                string folder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "PCUserDetection");
                return Path.Combine(EnsureDirectory(folder), "theme.txt");
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
