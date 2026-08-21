using System;
using System.Drawing;
using System.IO;
using System.Reflection;

namespace PCUserDetection
{
    /// <summary>
    /// The app's icon, read from the copy embedded in the assembly.
    /// </summary>
    /// <remarks>
    /// The same file is set as the project's ApplicationIcon, which is what
    /// Explorer and a shortcut show. That copy lives in the executable's Win32
    /// resources, though, and reaching it from managed code only yields a
    /// single size; a window that is given one 32 pixel icon gets a shrunken
    /// version of it in the title bar. Loading the .ico itself keeps every size
    /// in it, so Windows can choose the one drawn for each place it appears.
    /// </remarks>
    internal static class AppIcon
    {
        /// <summary>The name the .ico is embedded under; see the project file.</summary>
        private const string ResourceName = "PCUserDetection.Assets.PCUserDetection.ico";

        /// <summary>
        /// Loaded once. The icon does not change while the app is running, and
        /// a failed load is remembered as null rather than retried.
        /// </summary>
        private static readonly Icon Loaded = Load();

        /// <summary>
        /// The window icon, or null when it could not be read. Null is what
        /// <see cref="System.Windows.Forms.Form.Icon"/> takes to mean the
        /// default icon, so a caller does not have to check.
        /// </summary>
        public static Icon Value
        {
            get { return Loaded; }
        }

        private static Icon Load()
        {
            try
            {
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName))
                {
                    if (stream == null)
                    {
                        return null;
                    }

                    return new Icon(stream);
                }
            }
            catch (Exception)
            {
                // An icon that cannot be read is worth losing silently: the
                // window still opens, wearing the default one, and every other
                // thing the app does is unaffected.
                return null;
            }
        }
    }
}
