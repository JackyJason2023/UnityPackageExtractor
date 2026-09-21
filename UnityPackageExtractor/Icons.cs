using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace UnityPackageExtractor
{
    /// <summary>
    /// Programmatic vector-style icons (no bitmaps, no emoji) for the asset tree and the toolbar.
    /// Every file kind gets a distinct silhouette + colour so type is not conveyed by colour alone.
    /// </summary>
    public enum AssetKind
    {
        Folder, Texture, Model, Material, Scene, Script, Audio, Animation, Font, Text, Other
    }

    public static class Icons
    {
        private static readonly Dictionary<string, AssetKind> ExtMap =
            new Dictionary<string, AssetKind>(StringComparer.OrdinalIgnoreCase)
        {
            { ".png", AssetKind.Texture }, { ".jpg", AssetKind.Texture }, { ".jpeg", AssetKind.Texture },
            { ".tga", AssetKind.Texture }, { ".psd", AssetKind.Texture }, { ".bmp", AssetKind.Texture },
            { ".gif", AssetKind.Texture }, { ".exr", AssetKind.Texture }, { ".hdr", AssetKind.Texture },
            { ".tif", AssetKind.Texture }, { ".tiff", AssetKind.Texture }, { ".raw", AssetKind.Texture },
            { ".cin", AssetKind.Texture }, { ".dds", AssetKind.Texture }, { ".pic", AssetKind.Texture },

            { ".fbx", AssetKind.Model }, { ".obj", AssetKind.Model }, { ".blend", AssetKind.Model },
            { ".max", AssetKind.Model }, { ".ma", AssetKind.Model }, { ".mb", AssetKind.Model },
            { ".dae", AssetKind.Model }, { ".3ds", AssetKind.Model }, { ".dxj", AssetKind.Model },
            { ".blend1", AssetKind.Model }, { ".gltf", AssetKind.Model }, { ".glb", AssetKind.Model },

            { ".mat", AssetKind.Material }, { ".shader", AssetKind.Material }, { ".compute", AssetKind.Material },
            { ".cginc", AssetKind.Material }, { ".hlsl", AssetKind.Material }, { ".glsl", AssetKind.Material },
            { ".cinc", AssetKind.Material }, { ".shadergraph", AssetKind.Material },

            { ".unity", AssetKind.Scene }, { ".prefab", AssetKind.Scene }, { ".asset", AssetKind.Scene },
            { ".unitypackage", AssetKind.Scene },

            { ".cs", AssetKind.Script }, { ".js", AssetKind.Script }, { ".boo", AssetKind.Script },

            { ".wav", AssetKind.Audio }, { ".mp3", AssetKind.Audio }, { ".ogg", AssetKind.Audio },
            { ".aif", AssetKind.Audio }, { ".aiff", AssetKind.Audio }, { ".mod", AssetKind.Audio },
            { ".it", AssetKind.Audio }, { ".xm", AssetKind.Audio },

            { ".anim", AssetKind.Animation }, { ".controller", AssetKind.Animation },
            { ".overridecontroller", AssetKind.Animation }, { ".mask", AssetKind.Animation },

            { ".ttf", AssetKind.Font }, { ".otf", AssetKind.Font }, { ".fontsettings", AssetKind.Font },

            { ".txt", AssetKind.Text }, { ".md", AssetKind.Text }, { ".xml", AssetKind.Text },
            { ".json", AssetKind.Text },
        };

        public static AssetKind Classify(string ext)
        {
            AssetKind k;
            if (!string.IsNullOrEmpty(ext) && ExtMap.TryGetValue(ext, out k)) return k;
            return AssetKind.Other;
        }

        public static string FriendlyName(AssetKind kind)
        {
            switch (kind)
            {
                case AssetKind.Folder: return "Folder";
                case AssetKind.Texture: return "Texture / image";
                case AssetKind.Model: return "3D model";
                case AssetKind.Material: return "Material / shader";
                case AssetKind.Scene: return "Scene / prefab";
                case AssetKind.Script: return "C# script";
                case AssetKind.Audio: return "Audio clip";
                case AssetKind.Animation: return "Animation";
                case AssetKind.Font: return "Font";
                case AssetKind.Text: return "Text file";
                default: return "File";
            }
        }

        private static Color ColorFor(AssetKind kind)
        {
            switch (kind)
            {
                case AssetKind.Folder: return Color.FromArgb(0xE8, 0xB3, 0x3C);
                case AssetKind.Texture: return Color.FromArgb(0x7A, 0x5A, 0xF8);
                case AssetKind.Model: return Color.FromArgb(0x0E, 0xA5, 0xE9);
                case AssetKind.Material: return Color.FromArgb(0xEC, 0x48, 0x99);
                case AssetKind.Scene: return Color.FromArgb(0x16, 0xA3, 0x4A);
                case AssetKind.Script: return Color.FromArgb(0x25, 0x63, 0xEB);
                case AssetKind.Audio: return Color.FromArgb(0xF9, 0x73, 0x16);
                case AssetKind.Animation: return Color.FromArgb(0x14, 0xB8, 0xA6);
                case AssetKind.Font: return Color.FromArgb(0x6B, 0x72, 0x80);
                case AssetKind.Text: return Color.FromArgb(0x64, 0x74, 0x8B);
                default: return Color.FromArgb(0x9C, 0xA3, 0xAF);
            }
        }

        private static readonly AssetKind[] Order =
        {
            AssetKind.Folder, AssetKind.Texture, AssetKind.Model, AssetKind.Material, AssetKind.Scene,
            AssetKind.Script, AssetKind.Audio, AssetKind.Animation, AssetKind.Font, AssetKind.Text, AssetKind.Other
        };

        /// <summary>Build a DPI-aware ImageList in a fixed order; <see cref="IndexOf"/> returns the
        /// matching index for a given <see cref="AssetKind"/>.</summary>
        public static ImageList BuildImageList()
        {
            int size = Theme.S(18);
            var list = new ImageList { ImageSize = new Size(size, size), ColorDepth = ColorDepth.Depth32Bit };
            foreach (AssetKind kind in Order)
                list.Images.Add(kind.ToString(), Render(kind, size));
            return list;
        }

        public static int IndexOf(AssetKind kind)
        {
            for (int i = 0; i < Order.Length; i++) if (Order[i] == kind) return i;
            return (int)AssetKind.Other;
        }

        // ---- drawing ------------------------------------------------------------------------
        public static Bitmap Render(AssetKind kind, int size)
        {
            var bmp = new Bitmap(size, size);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.Clear(Color.Transparent);
                if (kind == AssetKind.Folder) DrawFolder(g, size);
                else DrawPage(g, size, ColorFor(kind), kind);
            }
            return bmp;
        }

        private static void DrawFolder(Graphics g, int s)
        {
            float m = s * 0.10f;
            float w = s - m * 2;
            Color baseColor = ColorFor(AssetKind.Folder);
            Color tabColor = Control(baseColor, -0.12f);

            float tabH = s * 0.16f;
            var tab = new GraphicsPath();
            var tabRect = new RectangleF(m, m + s * 0.06f, w * 0.52f, tabH + s * 0.10f);
            AddRounded(tab, tabRect, s * 0.10f);
            using (var b = new SolidBrush(tabColor)) g.FillPath(b, tab);

            var body = new RectangleF(m, m + tabH + s * 0.10f, w, s - m * 2 - tabH - s * 0.10f);
            var bodyPath = new GraphicsPath();
            AddRounded(bodyPath, body, s * 0.10f);
            using (var b = new SolidBrush(baseColor)) g.FillPath(b, bodyPath);
            using (var p = new Pen(Control(baseColor, -0.25f), Math.Max(1f, s * 0.02f))) g.DrawPath(p, bodyPath);
        }

        private static void DrawPage(Graphics g, int s, Color color, AssetKind kind)
        {
            float m = s * 0.14f;
            float x0 = m, y0 = m;
            float x1 = s - m, y1 = s - m;
            float fold = s * 0.28f;

            var body = new GraphicsPath();
            body.AddLine(x0, y0, x1 - fold, y0);
            body.AddLine(x1 - fold, y0, x1, y0 + fold);
            body.AddLine(x1, y0 + fold, x1, y1);
            body.AddLine(x1, y1, x0, y1);
            body.CloseFigure();

            using (var b = new SolidBrush(color)) g.FillPath(b, body);
            using (var p = new Pen(Control(color, -0.22f), Math.Max(1f, s * 0.02f))) g.DrawPath(p, body);

            // folded corner
            var corner = new[]
            {
                new PointF(x1 - fold, y0), new PointF(x1 - fold, y0 + fold), new PointF(x1, y0 + fold)
            };
            using (var b = new SolidBrush(Control(color, 0.35f))) g.FillPolygon(b, corner);

            // tiny per-kind glyph in ink so type survives even in greyscale
            DrawGlyph(g, kind, new RectangleF(x0 + s * 0.04f, y0 + fold + s * 0.06f, (x1 - x0) - s * 0.10f, (y1 - y0) - fold - s * 0.10f),
                Color.White);
        }

        private static void DrawGlyph(Graphics g, AssetKind kind, RectangleF r, Color ink)
        {
            using (var p = new Pen(ink, Math.Max(1f, r.Height * 0.10f)))
            using (var b = new SolidBrush(ink))
            {
                p.StartCap = LineCap.Round; p.EndCap = LineCap.Round;
                switch (kind)
                {
                    case AssetKind.Texture:
                        g.DrawEllipse(p, r.X + r.Width * 0.12f, r.Y + r.Height * 0.10f, r.Width * 0.24f, r.Height * 0.24f);
                        g.DrawLine(p, r.X, r.Bottom, r.X + r.Width * 0.40f, r.Y + r.Height * 0.55f);
                        g.DrawLine(p, r.X + r.Width * 0.40f, r.Y + r.Height * 0.55f, r.Right, r.Bottom);
                        break;
                    case AssetKind.Model:
                        g.DrawPolygon(p, new[]
                        {
                            new PointF(r.X + r.Width * 0.5f, r.Y), new PointF(r.Right, r.Y + r.Height * 0.30f),
                            new PointF(r.X + r.Width * 0.5f, r.Y + r.Height * 0.60f), new PointF(r.X, r.Y + r.Height * 0.30f)
                        });
                        break;
                    case AssetKind.Material:
                        g.DrawEllipse(p, r.X + r.Width * 0.1f, r.Y, r.Width * 0.8f, r.Height * 0.8f);
                        break;
                    case AssetKind.Script:
                        g.DrawLines(p, new[] { new PointF(r.X + r.Width * 0.4f, r.Y), new PointF(r.X + r.Width * 0.1f, r.Y + r.Height * 0.4f), new PointF(r.X + r.Width * 0.4f, r.Y + r.Height * 0.8f) });
                        g.DrawLines(p, new[] { new PointF(r.X + r.Width * 0.6f, r.Y), new PointF(r.X + r.Width * 0.9f, r.Y + r.Height * 0.4f), new PointF(r.X + r.Width * 0.6f, r.Y + r.Height * 0.8f) });
                        break;
                    case AssetKind.Audio:
                        g.FillEllipse(b, r.X + r.Width * 0.12f, r.Bottom - r.Height * 0.34f, r.Width * 0.3f, r.Height * 0.3f);
                        g.DrawLine(p, r.Right - r.Width * 0.18f, r.Bottom - r.Height * 0.30f, r.Right - r.Width * 0.18f, r.Y);
                        break;
                    case AssetKind.Animation:
                        g.FillPolygon(b, new[] { new PointF(r.X + r.Width * 0.2f, r.Y), new PointF(r.Right, r.Y + r.Height * 0.4f), new PointF(r.X + r.Width * 0.2f, r.Y + r.Height * 0.8f) });
                        break;
                    case AssetKind.Scene:
                        g.DrawEllipse(p, r.X + r.Width * 0.32f, r.Y, r.Width * 0.36f, r.Width * 0.36f);
                        g.DrawLine(p, r.X + r.Width * 0.5f, r.Y + r.Width * 0.36f, r.X + r.Width * 0.5f, r.Bottom);
                        break;
                    case AssetKind.Font:
                        using (var fp = new Font("Segoe UI", Math.Max(4f, r.Height * 0.7f), FontStyle.Bold))
                            g.DrawString("A", fp, b, r.X + r.Width * 0.22f, r.Y - r.Height * 0.06f);
                        break;
                    default: // Text / Other: content lines
                        for (int i = 0; i < 3; i++)
                        {
                            float ly = r.Y + r.Height * (0.18f + i * 0.28f);
                            float lw = (i == 2 ? 0.62f : 0.9f) * r.Width;
                            g.DrawLine(p, r.X, ly, r.X + lw, ly);
                        }
                        break;
                }
            }
        }

        // ---- toolbar / header glyphs --------------------------------------------------------
        public static Bitmap Glyph(AssetKind kind, int size)
        {
            return Render(kind, size);
        }

        public static Bitmap OpenIcon(float scale, Color color)
        {
            return StrokeIcon(scale, color, (g, s) =>
            {
                using (var p = new Pen(color, Math.Max(1.4f, s * 0.075f)))
                {
                    p.LineJoin = LineJoin.Round;
                    float m = s * 0.14f;
                    var back = new[]
                    {
                        new PointF(m, s * 0.30f), new PointF(s * 0.34f, s * 0.30f), new PointF(s * 0.44f, s * 0.42f),
                        new PointF(s - m, s * 0.42f), new PointF(s - m, s * 0.50f)
                    };
                    g.DrawLines(p, back);
                    var front = new GraphicsPath();
                    AddRounded(front, new RectangleF(m, s * 0.42f, s - m * 2, s * 0.40f), s * 0.06f);
                    g.DrawPath(p, front);
                }
            });
        }

        public static Bitmap PreviewIcon(float scale, Color color)
        {
            return StrokeIcon(scale, color, (g, s) =>
            {
                using (var p = new Pen(color, Math.Max(1.4f, s * 0.075f)))
                {
                    p.LineJoin = LineJoin.Round;
                    using (var eye = new GraphicsPath())
                    {
                        PointF l = new PointF(s * 0.12f, s * 0.50f);
                        PointF r = new PointF(s * 0.88f, s * 0.50f);
                        eye.AddBezier(l, new PointF(s * 0.28f, s * 0.24f), new PointF(s * 0.72f, s * 0.24f), r);
                        eye.AddBezier(r, new PointF(s * 0.72f, s * 0.76f), new PointF(s * 0.28f, s * 0.76f), l);
                        g.DrawPath(p, eye);
                    }
                    float d = s * 0.26f;
                    g.DrawEllipse(p, (s - d) / 2f, (s - d) / 2f, d, d);
                }
            });
        }

        public static Bitmap ExtractIcon(float scale, Color color)
        {
            return StrokeIcon(scale, color, (g, s) =>
            {
                using (var p = new Pen(color, Math.Max(1.4f, s * 0.075f)))
                {
                    p.StartCap = LineCap.Round; p.EndCap = LineCap.Round; p.LineJoin = LineJoin.Round;
                    float cx = s / 2f;
                    g.DrawLine(p, cx, s * 0.16f, cx, s * 0.60f);
                    g.DrawLines(p, new[] { new PointF(cx - s * 0.16f, s * 0.44f), new PointF(cx, s * 0.62f), new PointF(cx + s * 0.16f, s * 0.44f) });
                    g.DrawLines(p, new[] { new PointF(s * 0.18f, s * 0.72f), new PointF(s * 0.18f, s * 0.84f), new PointF(s * 0.82f, s * 0.84f), new PointF(s * 0.82f, s * 0.72f) });
                }
            });
        }

        public static Bitmap ExtractAllIcon(float scale, Color color)
        {
            return StrokeIcon(scale, color, (g, s) =>
            {
                using (var p = new Pen(color, Math.Max(1.4f, s * 0.075f)))
                {
                    p.StartCap = LineCap.Round; p.EndCap = LineCap.Round; p.LineJoin = LineJoin.Round;
                    float cx = s / 2f;
                    g.DrawLine(p, cx, s * 0.14f, cx, s * 0.48f);
                    g.DrawLines(p, new[] { new PointF(cx - s * 0.15f, s * 0.34f), new PointF(cx, s * 0.50f), new PointF(cx + s * 0.15f, s * 0.34f) });
                    var tray = new GraphicsPath();
                    AddRounded(tray, new RectangleF(s * 0.16f, s * 0.60f, s - s * 0.32f, s * 0.26f), s * 0.05f);
                    g.DrawPath(p, tray);
                }
            });
        }

        private static Bitmap StrokeIcon(float scale, Color color, Action<Graphics, float> draw)
        {
            int size = Math.Max(1, (int)(18 * scale));
            var bmp = new Bitmap(size, size);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                draw(g, size);
            }
            return bmp;
        }

        // ---- geometry helpers ---------------------------------------------------------------
        private static void AddRounded(GraphicsPath path, RectangleF r, float radius)
        {
            float d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
            path.Reset();
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
        }

        private static Color Control(Color c, float t)
        {
            Color target = t < 0 ? Color.Black : Color.White;
            float a = Math.Abs(t);
            return Color.FromArgb(c.A,
                (int)(c.R + (target.R - c.R) * a),
                (int)(c.G + (target.G - c.G) * a),
                (int)(c.B + (target.B - c.B) * a));
        }
    }
}
