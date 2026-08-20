using Microsoft.Win32;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PCUserDetection
{
    internal enum ThemeMode
    {
        /// <summary>Follow the app theme Windows is set to.</summary>
        System,
        Light,
        Dark
    }

    /// <summary>
    /// The single place the app's colours and fonts are defined. Every form and
    /// control reads from here, so the whole look can be changed by editing this
    /// file rather than by hunting through the designer code.
    /// </summary>
    /// <remarks>
    /// The palette is chosen once, when the app starts, from the Windows app
    /// theme. Changing the Windows setting while the app is open does not
    /// repaint it; restarting the app picks the new palette up.
    /// </remarks>
    internal static class Theme
    {
        /// <summary>Set this to Light or Dark to ignore what Windows is set to.</summary>
        private const ThemeMode Preferred = ThemeMode.Light;

        /// <summary>True when the dark palette is the one in use.</summary>
        public static readonly bool IsDark;

        // surfaces, from the back of the window to the front
        public static readonly Color Background;
        public static readonly Color Surface;
        public static readonly Color SurfaceHover;
        public static readonly Color Canvas; // behind the camera image
        public static readonly Color Border;

        // text
        public static readonly Color Text;
        public static readonly Color TextMuted;

        // meaning
        public static readonly Color Accent;
        public static readonly Color AccentHover;
        public static readonly Color OnAccent; // text on top of the accent
        public static readonly Color Success;
        public static readonly Color Danger;

        static Theme()
        {
            IsDark = Preferred == ThemeMode.Dark ||
                     (Preferred == ThemeMode.System && WindowsPrefersDarkApps());

            if (IsDark)
            {
                Background = Color.FromArgb(22, 24, 29);
                Surface = Color.FromArgb(29, 32, 38);
                SurfaceHover = Color.FromArgb(38, 42, 50);
                Canvas = Color.FromArgb(14, 15, 18);
                Border = Color.FromArgb(46, 51, 61);

                Text = Color.FromArgb(231, 233, 238);
                TextMuted = Color.FromArgb(138, 147, 163);

                Accent = Color.FromArgb(79, 124, 247);
                AccentHover = Color.FromArgb(106, 144, 255);
                OnAccent = Color.White;
                Success = Color.FromArgb(63, 185, 128);
                Danger = Color.FromArgb(229, 72, 77);
            }
            else
            {
                Background = Color.FromArgb(244, 246, 249);
                Surface = Color.FromArgb(255, 255, 255);
                SurfaceHover = Color.FromArgb(234, 238, 244);
                Canvas = Color.FromArgb(226, 230, 237);
                Border = Color.FromArgb(214, 219, 227);

                Text = Color.FromArgb(24, 28, 35);
                TextMuted = Color.FromArgb(103, 112, 128);

                // darker than the dark palette's accent, so white text on it and
                // the accent itself both stay readable against white
                Accent = Color.FromArgb(43, 92, 226);
                AccentHover = Color.FromArgb(30, 74, 199);
                OnAccent = Color.White;
                Success = Color.FromArgb(13, 122, 79);
                Danger = Color.FromArgb(191, 44, 48);
            }
        }

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
            button.ForeColor = OnAccent;
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
        /// Puts the title bar in the same light or dark mode as the palette. This
        /// is supported from Windows 10 20H1 onwards and is ignored, without
        /// failing, on anything older.
        /// </summary>
        public static void ApplyTitleBar(IntPtr handle)
        {
            int dark = IsDark ? 1 : 0;
            try
            {
                DwmSetWindowAttribute(handle, UseImmersiveDarkMode, ref dark, sizeof(int));
            }
            catch (DllNotFoundException)
            {
            }
            catch (EntryPointNotFoundException)
            {
            }
        }

        /// <summary>
        /// Reads the "Choose your default app mode" setting. A missing value means
        /// a Windows old enough not to have the setting, which was light only.
        /// </summary>
        private static bool WindowsPrefersDarkApps()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    if (key == null) return false;

                    return (key.GetValue("AppsUseLightTheme") as int?) == 0;
                }
            }
            catch (Exception)
            {
                return false;
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
