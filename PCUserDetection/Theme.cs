using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PCUserDetection
{
    /// <summary>
    /// The single place the app's colours and fonts are defined. Every form and
    /// control reads from here, so the whole look can be changed by editing this
    /// file rather than by hunting through the designer code.
    /// </summary>
    internal static class Theme
    {
        // surfaces, from the back of the window to the front
        public static readonly Color Background = Color.FromArgb(22, 24, 29);
        public static readonly Color Surface = Color.FromArgb(29, 32, 38);
        public static readonly Color SurfaceHover = Color.FromArgb(38, 42, 50);
        public static readonly Color Canvas = Color.FromArgb(14, 15, 18); // behind the camera image
        public static readonly Color Border = Color.FromArgb(46, 51, 61);

        // text
        public static readonly Color Text = Color.FromArgb(231, 233, 238);
        public static readonly Color TextMuted = Color.FromArgb(138, 147, 163);

        // meaning
        public static readonly Color Accent = Color.FromArgb(79, 124, 247);
        public static readonly Color AccentHover = Color.FromArgb(106, 144, 255);
        public static readonly Color Success = Color.FromArgb(63, 185, 128);
        public static readonly Color Danger = Color.FromArgb(229, 72, 77);

        // Segoe UI ships with Windows, so it always renders as intended.
        private const string Family = "Segoe UI";

        public static readonly Font Brand = new Font(Family, 12.5F, FontStyle.Bold);
        public static readonly Font Heading = new Font(Family, 14F, FontStyle.Bold);
        public static readonly Font Body = new Font(Family, 9.75F);
        public static readonly Font Small = new Font(Family, 8.5F);
        public static readonly Font Nav = new Font(Family, 10F);
        public static readonly Font Status = new Font(Family, 10.5F, FontStyle.Bold);

        /// <summary>A filled, accent coloured button for the main action on a screen.</summary>
        public static void StylePrimary(Button button)
        {
            StyleButton(button);
            button.BackColor = Accent;
            button.ForeColor = Color.White;
            button.FlatAppearance.MouseOverBackColor = AccentHover;
            button.FlatAppearance.MouseDownBackColor = Accent;
        }

        /// <summary>An outlined button for secondary actions next to a primary one.</summary>
        public static void StyleGhost(Button button)
        {
            StyleButton(button);
            button.BackColor = Surface;
            button.ForeColor = Text;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = Border;
            button.FlatAppearance.MouseOverBackColor = SurfaceHover;
            button.FlatAppearance.MouseDownBackColor = Surface;
        }

        /// <summary>
        /// Asks Windows for a dark title bar so the frame matches the window.
        /// This is supported from Windows 10 20H1 onwards and is ignored, without
        /// failing, on anything older.
        /// </summary>
        public static void ApplyDarkTitleBar(IntPtr handle)
        {
            int enabled = 1;
            try
            {
                DwmSetWindowAttribute(handle, UseImmersiveDarkMode, ref enabled, sizeof(int));
            }
            catch (DllNotFoundException)
            {
            }
            catch (EntryPointNotFoundException)
            {
            }
        }

        private const int UseImmersiveDarkMode = 20; // DWMWA_USE_IMMERSIVE_DARK_MODE

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr handle, int attribute, ref int value, int size);

        private static void StyleButton(Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.Font = Body;
            button.Height = 40;
            button.Cursor = Cursors.Hand;
            button.UseVisualStyleBackColor = false;
        }
    }
}
