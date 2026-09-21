using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace UnityPackageExtractor
{
    /// <summary>Rounded, bordered surface container (Fluent "Card"). Children are laid out by the
    /// caller inside a padded client area; the corners are painted in the surrounding surface color
    /// so the card reads as an elevated, rounded panel.</summary>
    public class CardPanel : Panel
    {
        public int Radius { get; set; } = Theme.RadiusCard;
        public Color FillColor { get; set; } = Theme.Card;
        public Color BorderColor { get; set; } = Theme.CardBorder;
        public Color CornerColor { get; set; } = Theme.Window;
        public bool ShowBorder { get; set; } = true;

        private bool _clipChildren;
        /// <summary>When true the control (and therefore its children) is clipped to the rounded
        /// rectangle, so a docked square child such as a TreeView still reads as a rounded card.</summary>
        public bool ClipChildren
        {
            get { return _clipChildren; }
            set { _clipChildren = value; UpdateRegion(); }
        }

        public CardPanel()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer
                     | ControlStyles.AllPaintingInWmPaint
                     | ControlStyles.UserPaint
                     | ControlStyles.ResizeRedraw, true);
        }

        public void ApplyTheme()
        {
            FillColor = Theme.Card;
            BorderColor = Theme.CardBorder;
            CornerColor = Theme.Window;
            Invalidate();
        }

        private void UpdateRegion()
        {
            if (!IsHandleCreated) return;
            Region old = Region;
            if (_clipChildren && Width > 0 && Height > 0)
            {
                using (var path = Theme.RoundRect(new Rectangle(0, 0, Width, Height), Theme.S(Radius)))
                    Region = new Region(path);
            }
            else
            {
                Region = null;
            }
            if (old != null) old.Dispose();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            UpdateRegion();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            UpdateRegion();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Theme.EnableSmoothing(g);
            RectangleF r = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);

            if (!_clipChildren)
                using (var b = new SolidBrush(CornerColor)) g.FillRectangle(b, ClientRectangle);

            using (var path = Theme.RoundRect(r, Theme.S(Radius)))
            using (var fill = new SolidBrush(FillColor))
            {
                g.FillPath(fill, path);
                if (ShowBorder)
                {
                    var pen = new Pen(BorderColor, Math.Max(1f, Theme.Sf(1f)));
                    g.DrawPath(pen, path);
                    pen.Dispose();
                }
            }
        }
    }

    /// <summary>Standard control variant of a card that keeps a fixed, DPI-scaled height.</summary>
    public enum ButtonStyle
    {
        Standard,
        Primary
    }

    /// <summary>Owner-drawn Fluent push button: rounded, accent "Primary" style, and distinct
    /// hover / pressed / focused / disabled states so every action has visible feedback.</summary>
    public class ModernButton : Button
    {
        public ButtonStyle Style { get; set; } = ButtonStyle.Standard;
        public int Radius { get; set; } = Theme.RadiusControl;
        public int IconGap { get; set; } = Theme.Gap;
        public int ContentPadding { get; set; } = 14;

        private bool _hover;
        private bool _pressed;
        private bool _focus;

        public ModernButton()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer
                     | ControlStyles.AllPaintingInWmPaint
                     | ControlStyles.UserPaint
                     | ControlStyles.ResizeRedraw, true);
            FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Font = Theme.Button;
            Cursor = Cursors.Hand;
            TabStop = true;
            UseMnemonic = false;
            TextImageRelation = TextImageRelation.ImageBeforeText;
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; _pressed = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { _pressed = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { _pressed = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnGotFocus(EventArgs e) { _focus = true; Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { _focus = false; Invalidate(); base.OnLostFocus(e); }
        protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Theme.EnableSmoothing(g);
            Theme.FillSurround(this, g);

            Color fill, fore, border;
            if (!Enabled)
            {
                fill = Theme.DisabledFill;
                fore = Theme.DisabledText;
                border = Theme.DisabledBorder;
            }
            else if (Style == ButtonStyle.Primary)
            {
                fill = _pressed ? Blend(Theme.Accent, Color.Black, 0.20f)
                     : _hover ? Blend(Theme.Accent, Color.White, 0.12f)
                     : Theme.Accent;
                fore = Theme.OnAccent;
                border = fill;
            }
            else
            {
                fill = _pressed ? Theme.ButtonFillPressed
                     : _hover ? Theme.ButtonFillHover
                     : Theme.ButtonFill;
                fore = Theme.TextPrimary;
                border = Theme.CardBorder;
            }

            RectangleF r = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            using (var path = Theme.RoundRect(r, Theme.S(Radius)))
            using (var b = new SolidBrush(fill))
            {
                g.FillPath(b, path);
                using (var p = new Pen(border, Math.Max(1f, Theme.Sf(1f))))
                    g.DrawPath(p, path);
            }

            if (_focus && ShowFocusCues)
            {
                RectangleF fr = new RectangleF(2.5f, 2.5f, Width - 6f, Height - 6f);
                using (var path = Theme.RoundRect(fr, Theme.S(Radius)))
                using (var p = new Pen(Theme.IsDark ? Color.White : Theme.TextPrimary, Math.Max(1f, Theme.Sf(1.4f))))
                {
                    p.DashStyle = DashStyle.Solid;
                    g.DrawPath(p, path);
                }
            }

            DrawContent(g, fore);
        }

        private void DrawContent(Graphics g, Color fore)
        {
            int pad = Theme.S(ContentPadding);
            int iconSize = Image != null ? Theme.S(16) : 0;
            int gap = Image != null ? Theme.S(IconGap) : 0;
            Size textSize = TextRenderer.MeasureText(g, Text, Font, new Size(int.MaxValue, int.MaxValue),
                TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);

            int contentW = iconSize + gap + textSize.Width;
            int cx = (Width - contentW) / 2;
            int cy = (Height - textSize.Height) / 2;

            Rectangle iconRect = new Rectangle(cx, (Height - iconSize) / 2, iconSize, iconSize);
            if (Image != null)
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(Image, iconRect);
            }

            Rectangle textRect = new Rectangle(cx + iconSize + gap, cy, textSize.Width + 4, textSize.Height);
            TextRenderer.DrawText(g, Text, Font, textRect, Enabled ? fore : Theme.DisabledText,
                TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
        }

        private static Color Blend(Color a, Color b, float t)
        {
            return Color.FromArgb(
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));
        }
    }

    /// <summary>Rounded thumbnail surface for asset previews, with a checkerboard backdrop so
    /// transparent previews stay readable in both themes, and a friendly empty state.</summary>
    public class ThumbnailBox : Control
    {
        public Image Image { get; set; }
        public AssetKind Placeholder { get; set; } = AssetKind.Other;
        public int Radius { get; set; } = Theme.RadiusCard;

        public ThumbnailBox()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer
                     | ControlStyles.AllPaintingInWmPaint
                     | ControlStyles.UserPaint
                     | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            RectangleF r = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            using (var path = Theme.RoundRect(r, Theme.S(Radius)))
            {
                Theme.FillSurround(this, g);
                g.SetClip(path);
                using (var b = new SolidBrush(Theme.Card)) g.FillRectangle(b, ClientRectangle);

                if (Image != null)
                {
                    DrawChecker(g);
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    Size fit = FitInside(Image.Size, new Size(Width - Theme.S(16), Height - Theme.S(16)));
                    var dest = new Rectangle((Width - fit.Width) / 2, (Height - fit.Height) / 2, fit.Width, fit.Height);
                    using (var wrap = new ImageAttributes())
                    {
                        wrap.SetWrapMode(WrapMode.TileFlipXY);
                        g.DrawImage(Image, dest, 0, 0, Image.Width, Image.Height, GraphicsUnit.Pixel, wrap);
                    }
                }
                else
                {
                    DrawEmptyState(g);
                }
                g.ResetClip();

                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var p = new Pen(Theme.CardBorder, Math.Max(1f, Theme.Sf(1f))))
                    g.DrawPath(p, path);
            }
        }

        private void DrawChecker(Graphics g)
        {
            int cell = Theme.S(10);
            Color a = Theme.IsDark ? Color.FromArgb(0x30, 0x30, 0x30) : Color.FromArgb(0xF3, 0xF3, 0xF3);
            Color b = Theme.IsDark ? Color.FromArgb(0x27, 0x27, 0x27) : Color.FromArgb(0xFA, 0xFA, 0xFA);
            using (var ba = new SolidBrush(a))
            using (var bb = new SolidBrush(b))
            {
                for (int y = 0; y < Height; y += cell)
                    for (int x = 0; x < Width; x += cell)
                        g.FillRectangle((((x / cell) + (y / cell)) & 1) == 0 ? ba : bb, x, y, cell, cell);
            }
        }

        private void DrawEmptyState(Graphics g)
        {
            int size = Math.Min(Theme.S(56), Math.Min(Width, Height) - Theme.S(24));
            if (size < Theme.S(20)) return;
            using (var icon = Icons.Render(Placeholder, size))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(icon, (Width - size) / 2, (Height - size) / 2, size, size);
            }
        }

        private static Size FitInside(Size image, Size max)
        {
            if (image.Width <= 0 || image.Height <= 0) return new Size(0, 0);
            float scale = Math.Min((float)max.Width / image.Width, (float)max.Height / image.Height);
            scale = Math.Min(scale, 1f);
            return new Size(Math.Max(1, (int)(image.Width * scale)), Math.Max(1, (int)(image.Height * scale)));
        }
    }

    /// <summary>Theme-aware Fluent progress bar (the native ProgressBar ignores dark mode and the
    /// accent color). Supports an indeterminate marquee used while a package is being read.</summary>
    public class FluentProgressBar : Control
    {
        private int _minimum, _maximum = 100, _value;
        private bool _marquee;
        private int _marqueePos;
        private readonly Timer _timer;

        public FluentProgressBar()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint
                     | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            Height = Theme.S(6);
            _timer = new Timer { Interval = 16 };
            _timer.Tick += (s, e) =>
            {
                _marqueePos += Math.Max(2, Theme.S(3));
                if (_marqueePos > Width + Theme.S(60)) _marqueePos = -Theme.S(60);
                Invalidate();
            };
        }

        public int Minimum { get { return _minimum; } set { _minimum = value; Invalidate(); } }
        public int Maximum { get { return _maximum; } set { _maximum = value; Invalidate(); } }
        public int Value
        {
            get { return _value; }
            set { _value = Math.Max(_minimum, Math.Min(_maximum, value)); Invalidate(); }
        }

        public bool Marquee
        {
            get { return _marquee; }
            set
            {
                if (_marquee == value) return;
                _marquee = value;
                if (_marquee) { _marqueePos = -Theme.S(60); _timer.Start(); }
                else { _timer.Stop(); }
                Invalidate();
            }
        }

        protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); Invalidate(); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Theme.EnableSmoothing(g);
            Theme.FillSurround(this, g);
            int h = Height, w = Width;
            int rad = Math.Min(h / 2, Theme.S(3));

            using (var track = Theme.RoundRect(new Rectangle(0, 0, w - 1, h - 1), rad))
            using (var tb = new SolidBrush(Theme.ProgressTrack))
                g.FillPath(tb, track);

            Rectangle fillRect;
            if (_marquee)
            {
                int seg = Theme.S(60);
                fillRect = new Rectangle(_marqueePos, 0, seg, h);
            }
            else
            {
                int span = Math.Max(0, _maximum - _minimum);
                float frac = span == 0 ? 0 : (_value - _minimum) / (float)span;
                fillRect = new Rectangle(0, 0, (int)(w * frac), h);
            }
            if (fillRect.Width > 0)
            {
                using (var clip = Theme.RoundRect(new Rectangle(0, 0, w - 1, h - 1), rad))
                {
                    g.SetClip(clip);
                    using (var b = new SolidBrush(Enabled ? Theme.Accent : Theme.DisabledFill))
                        g.FillRectangle(b, fillRect);
                    g.ResetClip();
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _timer.Dispose();
            base.Dispose(disposing);
        }
    }
}
