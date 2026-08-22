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
    /// executable; <see cref="ResolveImageRoot"/> covers where they land in a
    /// development tree and in a published one. Settings live under AppData in
    /// either case; see <see cref="UserFolder"/>.
    /// </remarks>
    internal static class AppPaths
    {
        /// <summary>The project file that marks a development build's tree.</summary>
        private const string ProjectFile = "PCUserDetection.csproj";

        /// <summary>The name the app's own folders take, whichever root they sit under.</summary>
        private const string FolderName = "PCUserDetection";

        /// <summary>
        /// The folder the two image folders sit in. Resolved once, since the
        /// executable does not move while the app is running.
        /// </summary>
        private static readonly string ImageRoot = ResolveImageRoot();

        /// <summary>Holds the most recent frame captured for verification.</summary>
        public static string AnonymousImages
        {
            get { return EnsureDirectory(Path.Combine(ImageRoot, "AnonymousImages")); }
        }

        /// <summary>Holds the registered user images to compare against.</summary>
        public static string CapturedImages
        {
            get { return EnsureDirectory(CapturedImagesPath); }
        }

        /// <summary>
        /// Names the same folder as <see cref="CapturedImages"/> without creating
        /// it, for showing the path and for anything that must not raise when the
        /// folder cannot be created. Reading it does not touch the disk in either
        /// build layout, since the root it is built on is resolved by name too.
        /// </summary>
        public static string CapturedImagesPath
        {
            get { return Path.Combine(ImageRoot, "CapturedImages"); }
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
        /// Names the same file as <see cref="EmailSetting"/> without creating the
        /// folder it sits in, for reading the settings back, which must not raise
        /// when the folder cannot be made. A folder that is not there holds no
        /// file, which is what a fresh install looks like and is answered with
        /// the defaults rather than with a failure.
        /// </summary>
        public static string EmailSettingPath
        {
            get { return Path.Combine(UserFolderPath, "email.json"); }
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
        /// Names the same folder as <see cref="EmailDrops"/> without creating it,
        /// for the screens that only say where the alerts would be written. A
        /// folder that cannot be made is worth reporting when a message is
        /// actually being written to it, not while a sentence about it is drawn.
        /// </summary>
        public static string EmailDropsPath
        {
            get { return Path.Combine(UserFolderPath, "SentMail"); }
        }

        /// <summary>
        /// Holds the copy of a captured frame that an alert has attached, for
        /// as long as the message is being sent. Why a copy is made rather than
        /// the frame itself attached is at the one place that asks for this,
        /// EmailAlert.
        /// </summary>
        /// <remarks>
        /// Under the temp folder rather than beside the images. A copy here is
        /// finished with the moment the message is away and is deleted then, and
        /// temp is somewhere Windows already clears up, which is the right end
        /// for one that a crash left behind. Nothing shows this path or reads it
        /// back, so it has no name-only twin the way the folders that reach the
        /// screen do.
        /// </remarks>
        public static string AttachmentCopies
        {
            get { return EnsureDirectory(Path.Combine(Path.GetTempPath(), FolderName)); }
        }

        /// <summary>
        /// The per-person folder under AppData that the settings above live in.
        /// What is kept there belongs to the person rather than to the build, so
        /// a rebuild does not lose it.
        /// </summary>
        private static string UserFolder
        {
            get { return EnsureDirectory(UserFolderPath); }
        }

        /// <summary>
        /// Names <see cref="UserFolder"/> without creating it, so that resolving
        /// where things live is separate from making the folder they live in.
        /// </summary>
        private static string UserFolderPath
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    FolderName);
            }
        }

        /// <summary>The frame captured by the Capture button.</summary>
        public static string AnonymousImage
        {
            get { return Path.Combine(AnonymousImages, "Anonymous.jpeg"); }
        }

        /// <summary>
        /// The project folder during development, and the folder the settings
        /// already live in once the app has been published.
        /// </summary>
        /// <remarks>
        /// A development build sits in bin\&lt;Config&gt;\, below the project
        /// folder, and keeping the images there means a rebuild does not lose
        /// the ones a developer registered. A published build has no project
        /// folder above it: counting folders upwards from, say, C:\Apps\PCUD
        /// lands on C:\, which is the wrong place to write images to and on many
        /// machines cannot be written to at all. So the project file is looked
        /// for rather than assumed, and when it is not there the images go
        /// beside the settings, under AppData, where writing always succeeds.
        /// </remarks>
        private static string ResolveImageRoot()
        {
            DirectoryInfo directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null)
            {
                if (File.Exists(Path.Combine(directory.FullName, ProjectFile)))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            // named rather than created: this runs from a static field
            // initializer, where a throw would come back as a
            // TypeInitializationException from whatever first touched the class
            // rather than as something a caller can report. The folder itself is
            // made by whichever property is asked for a path to write to.
            return UserFolderPath;
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
