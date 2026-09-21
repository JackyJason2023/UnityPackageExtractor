using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace UnityPackageExtractor
{
    /// <summary>
    /// Central Fluent design system for the app: runtime light/dark detection, system accent
    /// color, semantic color tokens, typography, geometry and the window-level effects
    /// (immersive dark title bar, rounded window corners) that make a WinForms app feel like a
    /// native Windows 10/11 application.
    /// </summary>
    public static class Theme
    {
        // ---- geometry (96 DPI base; multiply by Scale for device pixels) --------------------
        public const int RadiusCard = 8;
        public const int RadiusControl = 4;
        public const int Gap = 8;
        public const int Pad = 12;

        private const string DefaultFontFamily = "Segoe UI Variable Text, Segoe UI";
        private const string DisplayFontFamily = "Segoe UI Variable Display, Segoe UI";

        // ---- runtime state -----------------------------------------------------------------
        public static bool IsDark { get; private set; }
        public static float Scale = 1f;              // device-independent-unit -> pixel factor

        // ---- semantic color tokens ---------------------------------------------------------
        public static Color Window;
        public static Color Layer;
        public static Color Card;
        public static Color CardBorder;
        public static Color TextPrimary;
        public static Color TextSecondary;
        public static Color TextTertiary;
        public static Color Divider;
        public static Color Accent;
        public static Color OnAccent;
        public static Color ButtonFill;
        public static Color ButtonFillHover;
        public static Color ButtonFillPressed;
        public static Color CardHover;
        public static Color CardPressed;
        public static Color Selection;
        public static Color TextOnAccent;
        public static Color TreeHover;
        public static Color ProgressTrack;
        public static Color DisabledText;
        public static Color DisabledFill;
        public static Color DisabledBorder;
        public static Color SubtleFill;

        // ---- cached fonts (point sizes auto-scale with system DPI) -------------------------
        public static Font Body { get; private set; }
        public static Font Meta { get; private set; }
        public static Font Section { get; private set; }
        public static Font Title { get; private set; }
        public static Font Button { get; private set; }
        public static Font Value { get; private set; }

        // ---- native interop ----------------------------------------------------------------
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        [DllImport("dwmapi.dll")]
        private static extern int DwmGetColorizationColor(out int colorization, out bool opaqueBlend);

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;      // Win10 2004+ / Win11
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_OLD = 19;  // early Win10 builds
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWCP_ROUND = 2;

        static Theme()
        {
            BuildFonts();
            Refresh();
        }

        private static void BuildFonts()
        {
            Body = new Font(DefaultFontFamily, 9.5f, FontStyle.Regular, GraphicsUnit.Point);
            Meta = new Font(DefaultFontFamily, 9f, FontStyle.Regular, GraphicsUnit.Point);
            Section = new Font(DisplayFontFamily, 12f, FontStyle.Bold, GraphicsUnit.Point);
            Title = new Font(DisplayFontFamily, 16f, FontStyle.Bold, GraphicsUnit.Point);
            Button = new Font(DefaultFontFamily, 10f, FontStyle.Regular, GraphicsUnit.Point);
            Value = new Font(DefaultFontFamily, 10f, FontStyle.Bold, GraphicsUnit.Point);
        }

        /// <summary>Re-detect system theme + accent and rebuild every color token.</summary>
        public static void Refresh()
        {
            IsDark = DetectIsDark();
            Accent = SanitizeAccent(DetectAccent());
            OnAccent = TextOnAccent = ContrastText(Accent);
            if (IsDark) SetDark(); else SetLight();
        }

        // The OS accent can come back near-black/near-white on some setups, which would make the
        // primary button unreadable; fall back to the Windows default blue in those cases.
        private static Color SanitizeAccent(Color c)
        {
            int mx = Math.Max(c.R, Math.Max(c.G, c.B));
            double lum = 0.2126 * srgb(c.R) + 0.7152 * srgb(c.G) + 0.0722 * srgb(c.B);
            if (mx < 60 || lum < 0.12 || lum > 0.92) return Color.FromArgb(0, 120, 215);
            return c;
        }

        private static void SetLight()
        {
            Window = Hex(0xF3F3F3);
            Layer = Hex(0xFBFBFB);
            Card = Hex(0xFFFFFF);
            CardBorder = Hex(0xE5E5E5);
            TextPrimary = Hex(0x1B1B1B);
            TextSecondary = Hex(0x5F5F5F);
            TextTertiary = Hex(0x8A8A8A);
            Divider = Hex(0xE5E5E5);
            ButtonFill = Hex(0xFBFBFB);
            ButtonFillHover = Hex(0xF0F0F0);
            ButtonFillPressed = Hex(0xE6E6E6);
            CardHover = Hex(0xF6F6F6);
            CardPressed = Hex(0xEAEAEA);
            Selection = Accent;
            TreeHover = Hex(0xF2F2F2);
            ProgressTrack = Hex(0xE2E2E2);
            DisabledText = Hex(0xA0A0A0);
            DisabledFill = Hex(0xF5F5F5);
            DisabledBorder = Hex(0xE0E0E0);
            SubtleFill = Blend(Window, TextPrimary, 0.04f);
        }

        private static void SetDark()
        {
            Window = Hex(0x202020);
            Layer = Hex(0x272727);
            Card = Hex(0x2B2B2B);
            CardBorder = Hex(0x383838);
            TextPrimary = Hex(0xFFFFFF);
            TextSecondary = Hex(0xCFCFCF);
            TextTertiary = Hex(0x9A9A9A);
            Divider = Hex(0x383838);
            ButtonFill = Hex(0x373737);
            ButtonFillHover = Hex(0x3D3D3D);
            ButtonFillPressed = Hex(0x303030);
            CardHover = Hex(0x333333);
            CardPressed = Hex(0x2A2A2A);
            Selection = Accent;
            TreeHover = Hex(0x303030);
            ProgressTrack = Hex(0x3A3A3A);
            DisabledText = Hex(0x6E6E6E);
            DisabledFill = Hex(0x2D2D2D);
            DisabledBorder = Hex(0x363636);
            SubtleFill = Blend(Window, TextPrimary, 0.06f);
        }

        // ---- detection ---------------------------------------------------------------------
        private static bool DetectIsDark()
        {
            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    if (k != null)
                    {
                        object v = k.GetValue("AppsUseLightTheme");
                        if (v is int) return (int)v == 0;
                    }
                }
            }
            catch { }
            return false;
        }

        private static Color DetectAccent()
        {
            try
            {
                int argb;
                bool opaque;
                if (DwmGetColorizationColor(out argb, out opaque) == 0)
                {
                    Color c = Color.FromArgb(argb & 0x00FFFFFF);
                    if (c.R + c.G + c.B > 24) return c; // ignore black / empty
                }
            }
            catch { }

            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\DWM"))
                {
                    object v = k != null ? k.GetValue("AccentColor") : null;
                    if (v is int)
                    {
                        int abgr = (int)v; // stored as 0x00BBGGRR
                        return Color.FromArgb(255, abgr & 0xFF, (abgr >> 8) & 0xFF, (abgr >> 16) & 0xFF);
                    }
                }
            }
            catch { }

            return Color.FromArgb(0, 120, 215); // Windows default blue
        }

        // ---- helpers -------------------------------------------------------------------------
        public static int S(int v) { return (int)Math.Round(v * Scale); }
        public static float Sf(float v) { return v * Scale; }

        public static Color Hex(int rgb) { return Color.FromArgb(255, (rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF); }

        /// <summary>Choose dark or light text for the given fill (WCAG relative-luminance test).</summary>
        public static Color ContrastText(Color fill)
        {
            double lum = 0.2126 * srgb(fill.R) + 0.7152 * srgb(fill.G) + 0.0722 * srgb(fill.B);
            return lum > 0.5 ? Color.FromArgb(0x1A, 0x1A, 0x1A) : Color.White;
        }
        private static double srgb(byte channel)
        {
            double c = channel / 255.0;
            return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }

        public static Color Blend(Color a, Color b, float t)
        {
            return Color.FromArgb(
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));
        }

        public static GraphicsPath RoundRect(Rectangle r, int radius)
        {
            return RoundRect(new RectangleF(r.X, r.Y, r.Width, r.Height), radius);
        }

        /// <summary>Pass a rect inset by half the pen width so a 1px stroke lands on whole
        /// pixels instead of straddling two columns, which reads as a ragged edge.</summary>
        public static GraphicsPath RoundRect(RectangleF r, float radius)
        {
            var path = new GraphicsPath();
            float d = radius * 2;
            if (d <= 0 || r.Width <= 0 || r.Height <= 0)
            {
                path.AddRectangle(r);
                path.CloseFigure();
                return path;
            }
            d = Math.Min(d, Math.Min(r.Width, r.Height));
            path.AddArc(new RectangleF(r.X, r.Y, d, d), 180, 90);
            path.AddArc(new RectangleF(r.Right - d, r.Y, d, d), 270, 90);
            path.AddArc(new RectangleF(r.Right - d, r.Bottom - d, d, d), 0, 90);
            path.AddArc(new RectangleF(r.X, r.Bottom - d, d, d), 90, 90);
            path.CloseFigure();
            return path;
        }

        public static void EnableSmoothing(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        }

        /// <summary>Paint the surface a rounded, owner-drawn control sits on before drawing its path.
        /// The double-buffered back buffer arrives uncleared and an OnPaintBackground override does
        /// not reach it here, so without this the four corner wedges outside the rounded path stay
        /// black and the anti-aliased arc blends into them.</summary>
        public static void FillSurround(Control owner, Graphics g)
        {
            using (var b = new SolidBrush(SurroundColor(owner)))
                g.FillRectangle(b, owner.ClientRectangle);
        }

        private static Color SurroundColor(Control owner)
        {
            for (Control p = owner == null ? null : owner.Parent; p != null; p = p.Parent)
            {
                CardPanel card = p as CardPanel;
                if (card != null) return card.FillColor;
                if (p.BackColor.A > 0) return p.BackColor;
            }
            return Window;
        }

        // ---- window effects ------------------------------------------------------------------
        public static void ApplyTitleBar(Form form)
        {
            try
            {
                int on = IsDark ? 1 : 0;
                if (DwmSetWindowAttribute(form.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref on, 4) != 0)
                    DwmSetWindowAttribute(form.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE_OLD, ref on, 4);

                int round = DWMWCP_ROUND;
                DwmSetWindowAttribute(form.Handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref round, 4);
            }
            catch { }
        }
    }
}
