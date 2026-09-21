using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace UnityPackageExtractor
{
    /// <summary>Modeless window that renders the real asset file rather than Unity's 128px
    /// preview.png: images at native resolution with zoom and pan, source for text assets,
    /// and a typed fallback for formats GDI+ cannot decode.</summary>
    public class PreviewWindow : Form
    {
        const long MaxImageBytes = 128L * 1024 * 1024;
        const long MaxImagePixels = 64L * 1024 * 1024;
        const int MaxTextChars = 200000;

        Label titleLabel, kindLabel, metaLabel;
        CardPanel bodyCard;
        Panel bodySurface, infoPanel;
        ImageStage stage;
        TextBox textView;
        Font mono;
        PictureBox infoIcon;
        Label infoTitle, infoBody;
        FlowLayoutPanel zoomBar;
        ModernButton btnZoomOut, btnFit, btnActual, btnZoomIn;

        bool ownsStageBitmap;

        public PreviewWindow()
        {
            AutoScaleMode = AutoScaleMode.None;
            BackColor = Theme.Window;
            Font = Theme.Body;
            Text = "Asset preview";
            FormBorderStyle = FormBorderStyle.Sizable;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(Theme.S(420), Theme.S(320));
            Size = new Size(Theme.S(900), Theme.S(680));

            BuildLayout();
            ApplyTheme();
        }

        private static int S(int v) { return Theme.S(v); }

        private void BuildLayout()
        {
            TableLayoutPanel root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(S(16), S(14), S(16), S(14))
            };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, S(44)));

            titleLabel = new Label { AutoSize = true, Text = "—", Font = Theme.Section, ForeColor = Theme.TextPrimary, Margin = new Padding(0) };
            kindLabel = new Label { AutoSize = true, Text = " ", Font = Theme.Meta, ForeColor = Theme.TextSecondary, Margin = new Padding(0, S(3), 0, 0) };

            TableLayoutPanel head = new TableLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 1, RowCount = 2, Margin = new Padding(0, 0, 0, S(12)) };
            head.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            head.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            head.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            head.Controls.Add(titleLabel, 0, 0);
            head.Controls.Add(kindLabel, 0, 1);

            bodyCard = new CardPanel { Dock = DockStyle.Fill, FillColor = Theme.Card, ClipChildren = true };
            bodySurface = new Panel { Dock = DockStyle.Fill, Padding = new Padding(S(1)), BackColor = Theme.Card };

            stage = new ImageStage { Dock = DockStyle.Fill, Visible = false };
            stage.ViewChanged += delegate { RefreshMeta(); };

            mono = new Font("Consolas", Theme.Body.SizeInPoints, FontStyle.Regular, GraphicsUnit.Point);
            textView = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                WordWrap = false,
                ScrollBars = ScrollBars.Both,
                BorderStyle = BorderStyle.None,
                BackColor = Theme.Card,
                ForeColor = Theme.TextPrimary,
                Font = mono,
                TabStop = false,
                HideSelection = false,
                Visible = false
            };

            infoPanel = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Card, Visible = false };
            // Spacer rows above and below keep the block centred in both axes; Anchor None centres
            // each row inside the percent-width column, which a GrowAndShrink panel does not.
            TableLayoutPanel it = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                Padding = new Padding(S(24), 0, S(24), 0)
            };
            it.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            it.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            for (int i = 0; i < 3; i++) it.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            it.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            infoIcon = new PictureBox { Size = new Size(S(72), S(72)), SizeMode = PictureBoxSizeMode.Zoom, Margin = new Padding(0, 0, 0, S(14)), Anchor = AnchorStyles.None };
            infoTitle = new Label { AutoSize = true, Font = Theme.Value, ForeColor = Theme.TextPrimary, Margin = new Padding(0, 0, 0, S(6)), Anchor = AnchorStyles.None };
            infoBody = new Label { AutoSize = true, MaximumSize = new Size(S(420), 0), Font = Theme.Meta, ForeColor = Theme.TextSecondary, TextAlign = ContentAlignment.TopCenter, Margin = new Padding(0), Anchor = AnchorStyles.None };
            it.Controls.Add(infoIcon, 0, 1);
            it.Controls.Add(infoTitle, 0, 2);
            it.Controls.Add(infoBody, 0, 3);
            infoPanel.Controls.Add(it);

            bodySurface.Controls.Add(stage);
            bodySurface.Controls.Add(textView);
            bodySurface.Controls.Add(infoPanel);
            bodyCard.Controls.Add(bodySurface);

            metaLabel = new Label { AutoSize = true, Text = " ", Font = Theme.Meta, ForeColor = Theme.TextTertiary, Anchor = AnchorStyles.Left | AnchorStyles.Bottom, Margin = new Padding(0, 0, 0, S(10)) };

            zoomBar = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Anchor = AnchorStyles.Right | AnchorStyles.Bottom, Margin = new Padding(0, 0, 0, S(4)) };
            btnZoomOut = MakeFootButton("−", delegate { stage.ZoomBy(1f / 1.25f); });
            btnFit = MakeFootButton("Fit", delegate { stage.FitToWindow(); });
            btnActual = MakeFootButton("100%", delegate { stage.ActualSize(); });
            btnZoomIn = MakeFootButton("+", delegate { stage.ZoomBy(1.25f); });
            zoomBar.Controls.Add(btnZoomOut);
            zoomBar.Controls.Add(btnFit);
            zoomBar.Controls.Add(btnActual);
            zoomBar.Controls.Add(btnZoomIn);

            TableLayoutPanel foot = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            foot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            foot.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            foot.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            foot.Controls.Add(metaLabel, 0, 0);
            foot.Controls.Add(zoomBar, 1, 0);

            root.Controls.Add(head, 0, 0);
            root.Controls.Add(bodyCard, 0, 1);
            root.Controls.Add(foot, 0, 2);
            Controls.Add(root);
        }

        private ModernButton MakeFootButton(string text, EventHandler onClick)
        {
            ModernButton b = new ModernButton
            {
                Text = text,
                Style = ButtonStyle.Standard,
                Height = S(30),
                Margin = new Padding(0, 0, S(6), 0),
                TabStop = false
            };
            b.Width = S(26) + TextRenderer.MeasureText(text, b.Font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine).Width;
            b.Click += onClick;
            return b;
        }

        public void ApplyTheme()
        {
            BackColor = Theme.Window;
            bodyCard.FillColor = Theme.Card;
            bodyCard.BorderColor = Theme.CardBorder;
            bodyCard.CornerColor = Theme.Window;
            bodySurface.BackColor = Theme.Card;
            infoPanel.BackColor = Theme.Card;
            titleLabel.ForeColor = Theme.TextPrimary;
            kindLabel.ForeColor = Theme.TextSecondary;
            metaLabel.ForeColor = Theme.TextTertiary;
            infoTitle.ForeColor = Theme.TextPrimary;
            infoBody.ForeColor = Theme.TextSecondary;
            textView.BackColor = Theme.Card;
            textView.ForeColor = Theme.TextPrimary;
            stage.Invalidate();
            bodyCard.Invalidate();
            Theme.ApplyTitleBar(this);
        }

        //====================================================================================================================================================================
        //loading
        //====================================================================================================================================================================
        /// <summary>Render one asset. <paramref name="packageThumb"/> is the preview.png Unity stored
        /// in the package; it is copied, never owned, because Form1 disposes the originals.</summary>
        public void LoadAsset(string title, string path, bool isFolder, Bitmap packageThumb)
        {
            titleLabel.Text = title;
            Text = title + "  —  Asset preview";
            ReleaseStage();

            if (isFolder) { ShowFolder(path); return; }

            AssetKind kind = Icons.Classify(Path.GetExtension(path));
            string kindName = Icons.FriendlyName(kind);

            Bitmap real = TryLoadImage(path);
            if (real != null)
            {
                ownsStageBitmap = true;
                ShowImage(real, kindName + "  •  original file", path);
                return;
            }

            string content; int lines; bool truncated;
            if (TryLoadText(path, out content, out lines, out truncated))
            {
                kindLabel.Text = kindName + "  •  source";
                ShowView(ViewKind.Text);
                textView.Text = content;
                textView.SelectionStart = 0;
                textView.SelectionLength = 0;
                metaLabel.Text = lines + " lines  •  " + Form1.FormatBytes(FileSize(path))
                    + (truncated ? "  •  first " + MaxTextChars.ToString("N0") + " characters" : "");
                return;
            }

            Bitmap thumbCopy = packageThumb != null ? CopyOf(packageThumb) : null;
            if (thumbCopy != null)
            {
                ownsStageBitmap = true;
                ShowImage(thumbCopy, kindName + "  •  package thumbnail", path);
                return;
            }

            kindLabel.Text = kindName + "  •  not previewable";
            ShowView(ViewKind.Info);
            infoIcon.Image = Icons.Render(kind, S(72));
            infoTitle.Text = Path.GetFileName(path);
            infoBody.Text = "This format cannot be rendered inline. Extract the asset to open it with the tool that created it.";
            metaLabel.Text = Form1.FormatBytes(FileSize(path)) + "  •  " + path;
        }

        private void ShowFolder(string path)
        {
            long bytes = 0; int files = 0;
            try
            {
                foreach (string f in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                {
                    try { bytes += new FileInfo(f).Length; files++; } catch { }
                }
            }
            catch { }

            kindLabel.Text = "Folder";
            ShowView(ViewKind.Info);
            infoIcon.Image = Icons.Render(AssetKind.Folder, S(72));
            infoTitle.Text = files + (files == 1 ? " asset" : " assets") + "  •  " + Form1.FormatBytes(bytes);
            infoBody.Text = "Select a file in the list to preview its contents.";
            metaLabel.Text = path;
        }

        private void ShowImage(Bitmap bmp, string subtitle, string path)
        {
            kindLabel.Text = subtitle;
            ShowView(ViewKind.Image);
            stage.SetImage(bmp, path);
        }

        private void RefreshMeta()
        {
            Bitmap b = stage.Image;
            if (b == null) { metaLabel.Text = " "; return; }
            metaLabel.Text = b.Width + " × " + b.Height + " px  •  "
                + Form1.FormatBytes(FileSize(stage.ImagePath)) + "  •  "
                + (int)Math.Round(stage.Zoom * 100f) + "%";
        }

        enum ViewKind { Image, Text, Info }

        private void ShowView(ViewKind view)
        {
            stage.Visible = view == ViewKind.Image;
            textView.Visible = view == ViewKind.Text;
            infoPanel.Visible = view == ViewKind.Info;
            zoomBar.Visible = view == ViewKind.Image;
        }

        private void ReleaseStage()
        {
            Bitmap b = stage.Image;
            stage.SetImage(null, null);
            if (b != null && ownsStageBitmap) b.Dispose();
            ownsStageBitmap = false;
        }

        private static Bitmap CopyOf(Bitmap src)
        {
            try { return new Bitmap(src); }
            catch { return null; }
        }

        private static long FileSize(string path)
        {
            try { return new FileInfo(path).Length; }
            catch { return 0; }
        }

        //====================================================================================================================================================================
        //decoders
        //====================================================================================================================================================================
        /// <summary>Decodes through a stream copy so the file never stays locked, and probes a pixel
        /// because Image.FromStream is lazy enough to "accept" some binary formats.</summary>
        private static Bitmap TryLoadImage(string path)
        {
            try
            {
                FileInfo fi = new FileInfo(path);
                if (!fi.Exists || fi.Length == 0 || fi.Length > MaxImageBytes) return null;

                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (Image tmp = Image.FromStream(fs, false, false))
                {
                    int w = tmp.Width, h = tmp.Height;
                    if (w < 1 || h < 1 || (long)w * h > MaxImagePixels) return null;
                    Bitmap copy = new Bitmap(tmp);
                    try { copy.GetPixel(0, 0); }
                    catch { copy.Dispose(); return null; }
                    return copy;
                }
            }
            catch { return null; }
        }

        /// <summary>Any NUL byte in the first 8 KB means binary. UTF-8 is validated strictly so a
        /// legacy-encoded file can be retried with the machine's ANSI code page.</summary>
        private static bool TryLoadText(string path, out string content, out int lines, out bool truncated)
        {
            content = null; lines = 0; truncated = false;
            try
            {
                if (!File.Exists(path)) return false;

                byte[] head = new byte[8192];
                int read;
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    read = fs.Read(head, 0, head.Length);
                if (read <= 0) { content = ""; return true; }
                for (int i = 0; i < read; i++) if (head[i] == 0) return false;

                char[] buf = new char[MaxTextChars];
                int n = 0, got;
                bool fallback = false;
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var sr = new StreamReader(fs, new UTF8Encoding(false, true), true))
                {
                    try { while (n < buf.Length && (got = sr.Read(buf, n, buf.Length - n)) > 0) n += got; }
                    catch (DecoderFallbackException) { fallback = true; }
                    catch (ArgumentException) { fallback = true; }
                    truncated = !fallback && n >= buf.Length;
                }

                if (fallback)
                {
                    n = 0;
                    using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var sr = new StreamReader(fs, Encoding.Default, true))
                    {
                        while (n < buf.Length && (got = sr.Read(buf, n, buf.Length - n)) > 0) n += got;
                        truncated = n >= buf.Length;
                    }
                }

                content = NormalizeNewlines(new string(buf, 0, n));
                lines = 1;
                for (int i = 0; i < n; i++) if (buf[i] == '\n') lines++;
                return true;
            }
            catch { return false; }
        }

        /// <summary>A multiline TextBox only breaks on CRLF, and assets shipped from macOS or Linux
        /// carry bare LF, which would otherwise render the whole file as one long line.</summary>
        private static string NormalizeNewlines(string s)
        {
            if (s.IndexOf('\r') < 0) return s.Replace("\n", "\r\n");
            return s.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\r\n");
        }

        //====================================================================================================================================================================
        //window plumbing
        //====================================================================================================================================================================
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Theme.ApplyTitleBar(this);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape) { Close(); return true; }
            if (stage != null && stage.Visible)
            {
                if (keyData == (Keys.Control | Keys.Oemplus) || keyData == (Keys.Control | Keys.Add)) { stage.ZoomBy(1.25f); return true; }
                if (keyData == (Keys.Control | Keys.OemMinus) || keyData == (Keys.Control | Keys.Subtract)) { stage.ZoomBy(1f / 1.25f); return true; }
                if (keyData == (Keys.Control | Keys.D0) || keyData == (Keys.Control | Keys.NumPad0)) { stage.ActualSize(); return true; }
                if (keyData == (Keys.Control | Keys.B)) { stage.FitToWindow(); return true; }
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ReleaseStage();
                if (infoIcon != null && infoIcon.Image != null) { Image i = infoIcon.Image; infoIcon.Image = null; i.Dispose(); }
                if (mono != null) { mono.Dispose(); mono = null; }
            }
            base.Dispose(disposing);
        }

        //====================================================================================================================================================================
        //image stage
        //====================================================================================================================================================================
        /// <summary>Zoomable, pannable image surface. The wheel zooms around the cursor, dragging pans,
        /// double-click toggles fit/100%, and alpha formats sit on a checkerboard.</summary>
        public class ImageStage : Control
        {
            private Bitmap _img;
            private float _zoom = 1f;
            private bool _fit = true;
            private Point _pan;
            private bool _dragging;
            private Point _grab;
            private Point _panAtGrab;

            public string ImagePath { get; private set; }
            public event EventHandler ViewChanged;

            public ImageStage()
            {
                SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer
                         | ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw
                         | ControlStyles.Selectable, true);
            }

            public Bitmap Image { get { return _img; } }
            public float Zoom { get { return _zoom; } }

            public void SetImage(Bitmap bmp, string path)
            {
                _img = bmp;
                ImagePath = path;
                _fit = true;
                LayoutImage();
                Invalidate();
                Raise();
            }

            public void FitToWindow()
            {
                if (_img == null) return;
                _fit = true;
                LayoutImage();
                Invalidate();
                Raise();
            }

            public void ActualSize()
            {
                if (_img == null) return;
                _fit = false;
                _zoom = 1f;
                LayoutImage();
                Invalidate();
                Raise();
            }

            public void ZoomBy(float factor) { ZoomAt(new Point(Width / 2, Height / 2), factor); }

            private void ZoomAt(Point anchor, float factor)
            {
                if (_img == null) return;
                float target = Math.Max(0.05f, Math.Min(32f, _zoom * factor));
                if (Math.Abs(target - _zoom) < 1e-6f) return;
                float k = target / _zoom;
                _pan = new Point((int)Math.Round(anchor.X - (anchor.X - _pan.X) * k),
                                 (int)Math.Round(anchor.Y - (anchor.Y - _pan.Y) * k));
                _zoom = target;
                _fit = false;
                Invalidate();
                Raise();
            }

            private void LayoutImage()
            {
                if (_img == null) return;
                if (_fit)
                {
                    int m = Theme.S(20);
                    int aw = Math.Max(1, Width - m * 2), ah = Math.Max(1, Height - m * 2);
                    _zoom = Math.Max(0.05f, Math.Min(32f, Math.Min((float)aw / _img.Width, (float)ah / _img.Height)));
                }
                _pan = new Point((Width - (int)Math.Round(_img.Width * _zoom)) / 2,
                                 (Height - (int)Math.Round(_img.Height * _zoom)) / 2);
            }

            private Rectangle ImageRect
            {
                get
                {
                    if (_img == null) return Rectangle.Empty;
                    return new Rectangle(_pan.X, _pan.Y,
                        Math.Max(1, (int)Math.Round(_img.Width * _zoom)),
                        Math.Max(1, (int)Math.Round(_img.Height * _zoom)));
                }
            }

            protected override void OnResize(EventArgs e)
            {
                base.OnResize(e);
                if (_fit && _img != null) { LayoutImage(); Invalidate(); }
            }

            protected override void OnMouseWheel(MouseEventArgs e)
            {
                ZoomAt(e.Location, e.Delta > 0 ? 1.15f : 1f / 1.15f);
                base.OnMouseWheel(e);
            }

            protected override void OnMouseDown(MouseEventArgs e)
            {
                base.OnMouseDown(e);
                if (e.Button == MouseButtons.Left && _img != null)
                {
                    _dragging = true;
                    _grab = e.Location;
                    _panAtGrab = _pan;
                    Cursor = Cursors.SizeAll;
                    Focus();
                }
            }

            protected override void OnMouseMove(MouseEventArgs e)
            {
                if (_dragging)
                {
                    _pan = new Point(_panAtGrab.X + (e.X - _grab.X), _panAtGrab.Y + (e.Y - _grab.Y));
                    Invalidate();
                }
                else if (_img != null)
                {
                    Rectangle r = ImageRect;
                    Cursor = (r.Width > Width || r.Height > Height) ? Cursors.SizeAll : Cursors.Default;
                }
                base.OnMouseMove(e);
            }

            protected override void OnMouseUp(MouseEventArgs e)
            {
                if (_dragging) { _dragging = false; Cursor = Cursors.Default; }
                base.OnMouseUp(e);
            }

            protected override void OnDoubleClick(EventArgs e)
            {
                base.OnDoubleClick(e);
                if (_img == null) return;
                if (_fit) ActualSize(); else FitToWindow();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                g.Clear(Theme.IsDark ? Color.FromArgb(0x1F, 0x1F, 0x1F) : Color.FromArgb(0xF6, 0xF6, 0xF6));
                if (_img == null) return;

                Theme.EnableSmoothing(g);
                Rectangle r = ImageRect;

                if ((_img.PixelFormat & PixelFormat.Alpha) != 0) DrawChecker(g, r);

                g.InterpolationMode = _zoom >= 2f ? InterpolationMode.NearestNeighbor : InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.Half;
                // Bicubic sampling reads just past the source edge, where GDI+ hands back
                // transparent black, so a scaled bitmap gains a dark fringe along its own border.
                // Mirroring the edge into the missing texels removes it.
                using (var wrap = new ImageAttributes())
                {
                    wrap.SetWrapMode(WrapMode.TileFlipXY);
                    g.DrawImage(_img, r, 0, 0, _img.Width, _img.Height, GraphicsUnit.Pixel, wrap);
                }

                using (var p = new Pen(Theme.IsDark ? Color.FromArgb(0x3A, 0x3A, 0x3A) : Color.FromArgb(0xDD, 0xDD, 0xDD), Math.Max(1f, Theme.Sf(1f))))
                    g.DrawRectangle(p, r.X - 0.5f, r.Y - 0.5f, r.Width + 1f, r.Height + 1f);
            }

            private void DrawChecker(Graphics g, Rectangle r)
            {
                int cell = Theme.S(12);
                Color a = Theme.IsDark ? Color.FromArgb(0x2A, 0x2A, 0x2A) : Color.FromArgb(0xEC, 0xEC, 0xEC);
                Color b = Theme.IsDark ? Color.FromArgb(0x24, 0x24, 0x24) : Color.FromArgb(0xF5, 0xF5, 0xF5);
                using (var ba = new SolidBrush(a))
                using (var bb = new SolidBrush(b))
                {
                    int x0 = r.Left - (((r.Left % cell) + cell) % cell);
                    int y0 = r.Top - (((r.Top % cell) + cell) % cell);
                    for (int y = y0; y < r.Bottom; y += cell)
                        for (int x = x0; x < r.Right; x += cell)
                        {
                            Rectangle c = Rectangle.Intersect(r, new Rectangle(x, y, cell, cell));
                            if (c.Width > 0 && c.Height > 0)
                                g.FillRectangle((((x / cell) - (y / cell)) & 1) == 0 ? ba : bb, c);
                        }
                }
            }

            private void Raise()
            {
                EventHandler h = ViewChanged;
                if (h != null) h(this, EventArgs.Empty);
            }
        }
    }
}
