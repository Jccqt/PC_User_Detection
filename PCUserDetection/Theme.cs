using Microsoft.Win32;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PCUserDetection
{
    internal enum ThemeMode
    {
        Light,
        Dark,
        /// <summary>Follow the app theme Windows is set to.</summary>
        System
    }

    /// <summary>
    /// The single place the app's colours and fonts are defined. Every form and
    /// control reads from here, so the whole look can be changed by editing this
    /// file rather than by hunting through the designer code.
    /// </summary>
    /// <remarks>
    /// The palette can change while the app is running, so nothing should hold on
    /// to a colour it read earlier. Read <see cref="Text"/> and the rest at the
    /// moment they are needed, which for painted controls means inside their
    /// Paint handler.
    /// </remarks>
    internal static class Theme
    {
        /// <summary>Used until the person picks a theme for the first time.</summary>
        private const ThemeMode DefaultMode = ThemeMode.Light;

        /// <summary>The setting in force, which is what the rail shows as selected.</summary>
        public static ThemeMode Mode { get; private set; }

        /// <summary>True when the dark palette is the one in use.</summary>
        public static bool IsDark { get; private set; }

        // surfaces, from the back of the window to the front
        public static Color Background { get; private set; }
        public static Color Surface { get; private set; }
        public static Color SurfaceHover { get; private set; }
        public static Color Canvas { get; private set; } // behind the camera image
        public static Color Border { get; private set; }

        // text
        public static Color Text { get; private set; }
        public static Color TextMuted { get; private set; }

        // meaning
        public static Color Accent { get; private set; }
        public static Color AccentHover { get; private set; }
        public static Color OnAccent { get; private set; } // text on top of the accent
        public static Color Success { get; private set; }
        public static Color Danger { get; private set; }

        static Theme()
        {
            Select(LoadMode());
        }

        /// <summary>
        /// Switches the palette and remembers the choice for the next run. The
        /// window still has to re-apply the colours to its controls afterwards.
        /// </summary>
        public static void Apply(ThemeMode mode)
        {
            Select(mode);
            SaveMode(mode);
        }

        private static void Select(ThemeMode mode)
        {
            Mode = mode;
            IsDark = mode == ThemeMode.Dark || (mode == ThemeMode.System && WindowsPrefersDarkApps());

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

        /// <summary>
        /// The heavier weight is a family of its own on Windows rather than a
        /// style of Segoe UI, so it has to be asked for by name. It stands in
        /// everywhere the chrome would otherwise reach for bold, which is a
        /// step heavier than this window wants.
        /// </summary>
        private const string SemiboldFamily = "Segoe UI Semibold";

        /// <summary>The app name at the top of the rail.</summary>
        public static readonly Font Brand = new Font(SemiboldFamily, 10F);

        /// <summary>The screen title at the top of the content area.</summary>
        public static readonly Font Heading = new Font(SemiboldFamily, 14F);

        /// <summary>A section heading on the settings form, and primary button text.</summary>
        public static readonly Font Section = new Font(SemiboldFamily, 9.5F);

        /// <summary>The word a status line opens with, ahead of its detail.</summary>
        public static readonly Font Status = new Font(SemiboldFamily, 9.75F);

        /// <summary>Body text: row labels, sentence-long hints, status detail.</summary>
        public static readonly Font Body = new Font(Family, 9.75F);

        /// <summary>The text inside a field, a combo or a button.</summary>
        public static readonly Font Control = new Font(Family, 9.5F);

        /// <summary>Captions, counts and the shorter hints.</summary>
        public static readonly Font Small = new Font(Family, 8.5F);

        /// <summary>The rows of the navigation rail.</summary>
        public static readonly Font Nav = new Font(Family, 10F);

        /// <summary>
        /// Filenames and folder paths. They are read character by character
        /// rather than as words, which is what a fixed pitch is for.
        /// </summary>
        public static readonly Font Mono = new Font("Consolas", 9F);

        /// <summary>
        /// A filled, accent coloured button for the main action on a screen.
        /// Accent is spent on this and on the rail's selection bar, and on
        /// nothing else in the window.
        /// </summary>
        /// <remarks>
        /// The button paints itself from the palette in force, so this only has
        /// to say which of the two treatments it wears; a theme change is a
        /// repaint rather than another call to this.
        /// </remarks>
        public static void StylePrimary(FlatButton button)
        {
            button.Kind = ButtonKind.Primary;
            button.Invalidate();
        }

        /// <summary>An outlined button for secondary actions next to a primary one.</summary>
        public static void StyleGhost(FlatButton button)
        {
            button.Kind = ButtonKind.Ghost;
            button.Invalidate();
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

        private static ThemeMode LoadMode()
        {
            try
            {
                string path = AppPaths.ThemeSetting;

                if (File.Exists(path))
                {
                    ThemeMode saved;
                    if (Enum.TryParse(File.ReadAllText(path).Trim(), true, out saved)) return saved;
                }
            }
            catch (Exception)
            {
                // an unreadable setting is not worth failing the app over
            }

            return DefaultMode;
        }

        private static void SaveMode(ThemeMode mode)
        {
            try
            {
                File.WriteAllText(AppPaths.ThemeSetting, mode.ToString());
            }
            catch (Exception)
            {
                // the theme still changed for this run, it just will not be remembered
            }
        }

        private const int UseImmersiveDarkMode = 20; // DWMWA_USE_IMMERSIVE_DARK_MODE

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr handle, int attribute, ref int value, int size);
    }

    /// <summary>
    /// The rounded rectangle every control in the window is drawn as. A button
    /// and a text box cannot round themselves, so each one is painted over with
    /// this instead.
    /// </summary>
    /// <remarks>
    /// The radius is 3 pixels on a control and 0 on a surface; nothing in the
    /// window is rounder than that. Pass the control's own client rectangle
    /// deflated by 1, so the stroke has a pixel to sit in and is not clipped by
    /// the edge it is drawn against.
    /// </remarks>
    internal static class Rounded
    {
        public static GraphicsPath Path(Rectangle bounds, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d - 1, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d - 1, bounds.Bottom - d - 1, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d - 1, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        /// <summary>
        /// Fills the rectangle, and outlines it when a border colour is given.
        /// A null border is a fill with nothing drawn around it.
        /// </summary>
        public static void Fill(Graphics g, Rectangle bounds, int radius, Color fill, Color? border)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (GraphicsPath path = Path(bounds, radius))
            using (var brush = new SolidBrush(fill))
            {
                g.FillPath(brush, path);

                if (border == null) return;

                using (var pen = new Pen(border.Value)) g.DrawPath(pen, path);
            }
        }
    }
}
