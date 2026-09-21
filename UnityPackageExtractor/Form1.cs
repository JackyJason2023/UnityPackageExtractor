using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;

namespace UnityPackageExtractor
{
    public partial class Form1 : Form
    {
        static readonly string extract_folder = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "extract"; //raw contents of the unity package
        static readonly string build_folder = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "build";       //recreated asset tree

        Dictionary<string, Bitmap> asset_thumbnails = new Dictionary<string, Bitmap>();
        string package_name;
        int package_asset_count = 0;

        // ---- modern UI controls (assembled in code, see BuildModernLayout) ----
        TableLayoutPanel rootLayout;
        CardPanel headerCard; PictureBox headerIcon; Label titleLabel, subtitleLabel;
        Panel headerSurface, previewSurface, footerSurface;
        Panel toolbarPanel; ModernButton btnOpen, btnExtractSelected, btnExtractAll;
        ToolTip sharedTips;
        SplitContainer split;
        CardPanel treeCard; TreeView treeView; ImageList treeImages; Label treeEmptyHint;
        CardPanel previewCard; ThumbnailBox thumb; Label previewTitle, previewKind;
        ModernButton btnPreview;
        PreviewWindow previewWin;
        Label lblTypeValue, lblSizeValue, lblItemsValue, lblModifiedValue, lblPathValue;
        CardPanel footerCard; Label statusLabel, countLabel; FluentProgressBar progressBar;

        private bool lastIsDark;
        private Color lastAccent;

        public Form1(string path)
        {
            InitializeComponent();

            InitScale();
            BuildModernLayout();
            ApplyTheme();

            treeView.PathSeparator = Path.DirectorySeparatorChar.ToString();

            lastIsDark = Theme.IsDark;
            lastAccent = Theme.Accent;

            if (path != null)
            {
                if (Path.GetExtension(path) == ".unitypackage") extractUnityPackage(path);
                else MessageBox.Show("The file \"" + Path.GetFileName(path) + "\" is not a Unity package.", "Open Unity Package", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            }
        }

        private int S(int v) { return Theme.S(v); }
        private float Sf(float v) { return Theme.Sf(v); }

        private void InitScale()
        {
            try { using (Graphics g = Graphics.FromHwnd(IntPtr.Zero)) Theme.Scale = Math.Max(1f, g.DpiX / 96f); }
            catch { Theme.Scale = 1f; }
        }

        //====================================================================================================================================================================
        //layout
        //====================================================================================================================================================================
        private void BuildModernLayout()
        {
            AutoScaleMode = AutoScaleMode.None;
            BackColor = Theme.Window;
            Font = Theme.Body;
            ClientSize = new Size(S(1000), S(660));
            MinimumSize = new Size(S(780), S(520));

            rootLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(S(14)),
                BackColor = Theme.Window
            };
            rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Controls.Add(rootLayout);

            BuildHeader();
            BuildToolbar();
            BuildContent();
            BuildFooter();
        }

        private void BuildHeader()
        {
            headerCard = new CardPanel { Dock = DockStyle.Fill, Height = S(66), FillColor = Theme.Layer, Margin = new Padding(0, 0, 0, S(10)) };

            TableLayoutPanel tl = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(S(16), S(14), S(16), S(14)) };
            tl.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            headerIcon = new PictureBox { Size = new Size(S(36), S(36)), SizeMode = PictureBoxSizeMode.Zoom, Margin = new Padding(0, 0, S(14), 0), Anchor = AnchorStyles.Left };

            TableLayoutPanel vt = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right };
            vt.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            vt.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            titleLabel = new Label { AutoSize = true, Text = "Unity Package Extractor", Font = Theme.Title, ForeColor = Theme.TextPrimary, Margin = new Padding(0) };
            subtitleLabel = new Label { AutoSize = true, Text = "Browse and unpack a .unitypackage without opening the Unity Editor", Font = Theme.Meta, ForeColor = Theme.TextSecondary, Margin = new Padding(0, S(2), 0, 0) };
            vt.Controls.Add(titleLabel, 0, 0);
            vt.Controls.Add(subtitleLabel, 0, 1);

            tl.Controls.Add(headerIcon, 0, 0);
            tl.Controls.Add(vt, 1, 0);
            headerSurface = tl;
            headerCard.ClipChildren = true;
            headerCard.Controls.Add(tl);
            rootLayout.Controls.Add(headerCard, 0, 0);

            int textHeight = titleLabel.PreferredSize.Height + subtitleLabel.PreferredSize.Height + S(2);
            headerCard.Height = Math.Max(S(64), tl.Padding.Vertical + Math.Max(textHeight, S(36)) + S(4));
        }

        private void BuildToolbar()
        {
            toolbarPanel = new Panel { Dock = DockStyle.Fill, Height = S(40), BackColor = Theme.Window, Margin = new Padding(0, 0, 0, S(10)) };
            FlowLayoutPanel flow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, AutoSize = false };

            btnOpen = new ModernButton { Text = "Open Package", Style = ButtonStyle.Primary, Height = S(36), Margin = new Padding(0, 0, S(8), 0) };
            btnExtractSelected = new ModernButton { Text = "Extract Selected", Style = ButtonStyle.Standard, Enabled = false, Height = S(36), Margin = new Padding(0, 0, S(8), 0) };
            btnExtractAll = new ModernButton { Text = "Extract All", Style = ButtonStyle.Standard, Enabled = false, Height = S(36), Margin = new Padding(0) };

            ToolTip tips = new ToolTip { AutoPopDelay = 6000, InitialDelay = 350, ReshowDelay = 100 };
            tips.SetToolTip(btnOpen, "Select a Unity package to open");
            tips.SetToolTip(btnExtractSelected, "Extract just the selected asset or folder");
            tips.SetToolTip(btnExtractAll, "Extract every asset to a folder");
            this.sharedTips = tips;

            btnOpen.Click += toolStripButtonOpenPackage_Click;
            btnExtractSelected.Click += toolStripButtonExtractSelected_Click;
            btnExtractAll.Click += toolStripButtonExtractAll_Click;

            flow.Controls.Add(btnOpen);
            flow.Controls.Add(btnExtractSelected);
            flow.Controls.Add(btnExtractAll);
            toolbarPanel.Controls.Add(flow);
            rootLayout.Controls.Add(toolbarPanel, 0, 1);
        }

        private void BuildContent()
        {
            split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterWidth = S(10),
                BackColor = Theme.Window
            };
            split.Panel1.BackColor = Theme.Window;
            split.Panel2.BackColor = Theme.Window;
            rootLayout.Controls.Add(split, 0, 2);

            // ---- tree ----
            treeCard = new CardPanel { Dock = DockStyle.Fill, FillColor = Theme.Card, ClipChildren = true };
            treeImages = Icons.BuildImageList();
            treeView = new TreeView
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                ShowLines = false,
                HideSelection = false,
                FullRowSelect = true,
                HotTracking = true,
                ShowNodeToolTips = true,
                Indent = S(20),
                ItemHeight = S(28),
                BackColor = Theme.Card,
                ForeColor = Theme.TextPrimary,
                LineColor = Theme.Divider,
                ImageList = treeImages
            };
            treeView.AfterSelect += treeView_AfterSelect;
            treeView.NodeMouseDoubleClick += treeView_NodeMouseDoubleClick;
            treeView.ItemDrag += treeView_ItemDrag;
            treeCard.Controls.Add(treeView);

            treeEmptyHint = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = Theme.Meta,
                ForeColor = Theme.TextTertiary,
                BackColor = Theme.Card,
                Text = "No package open\r\nUse Open Package, or drop a .unitypackage file here",
                Visible = false
            };
            treeCard.Controls.Add(treeEmptyHint);
            treeEmptyHint.BringToFront();

            split.Panel1.Controls.Add(treeCard);

            // ---- preview ----
            BuildPreview();
        }

        private void BuildPreview()
        {
            previewCard = new CardPanel { Dock = DockStyle.Fill, FillColor = Theme.Card };

            TableLayoutPanel pt = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(S(20)) };
            pt.RowStyles.Add(new RowStyle(SizeType.AutoSize));   // thumbnail + title
            pt.RowStyles.Add(new RowStyle(SizeType.AutoSize));   // divider
            pt.RowStyles.Add(new RowStyle(SizeType.AutoSize));   // details header
            pt.RowStyles.Add(new RowStyle(SizeType.AutoSize));   // details grid

            TableLayoutPanel head = new TableLayoutPanel { ColumnCount = 3, RowCount = 1, Dock = DockStyle.Fill, AutoSize = true };
            head.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            head.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            head.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            head.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            thumb = new ThumbnailBox { Size = new Size(S(104), S(104)), Margin = new Padding(0, 0, S(18), 0), Anchor = AnchorStyles.Left };

            TableLayoutPanel titleStack = new TableLayoutPanel { ColumnCount = 1, RowCount = 3, Dock = DockStyle.Fill, Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right, AutoSize = true };
            titleStack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            titleStack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            titleStack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            previewTitle = new Label { AutoSize = true, Text = "No asset selected", Font = Theme.Section, ForeColor = Theme.TextPrimary, Margin = new Padding(0) };
            previewKind = new Label { AutoSize = true, Text = "Select an item to see its details", Font = Theme.Meta, ForeColor = Theme.TextSecondary, Margin = new Padding(0, S(3), 0, 0) };
            titleStack.Controls.Add(previewTitle, 0, 0);
            titleStack.Controls.Add(previewKind, 0, 1);
            head.Controls.Add(thumb, 0, 0);
            head.Controls.Add(titleStack, 1, 0);

            btnPreview = new ModernButton
            {
                Text = "Preview",
                Style = ButtonStyle.Standard,
                Height = S(32),
                Enabled = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Margin = new Padding(0)
            };
            btnPreview.Click += delegate { OpenPreviewFor(treeView.SelectedNode, true); };
            sharedTips.SetToolTip(btnPreview, "Open this asset in a preview window, or double-click it in the list");
            head.Controls.Add(btnPreview, 2, 0);

            Panel divider = new Panel { Dock = DockStyle.Fill, Height = S(1), BackColor = Theme.Divider, Margin = new Padding(0, S(18), 0, S(14)) };

            Label detailsHeader = new Label { AutoSize = true, Text = "Details", Font = Theme.Value, ForeColor = Theme.TextSecondary, Margin = new Padding(0, 0, 0, S(8)) };

            // Anchored rather than docked: a docked panel gets stretched by the cell and TableLayoutPanel
            // dumps the surplus into its last row, which visibly detaches the Location value.
            TableLayoutPanel grid = new TableLayoutPanel
            {
                ColumnCount = 2,
                RowCount = 5,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Height = S(30) * 5
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, S(96)));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            for (int i = 0; i < 5; i++) grid.RowStyles.Add(new RowStyle(SizeType.Absolute, S(30)));
            grid.Controls.Add(MakeCaption("Type"), 0, 0); lblTypeValue = AddDetailRow(grid, 0, "—");
            grid.Controls.Add(MakeCaption("Size"), 0, 1); lblSizeValue = AddDetailRow(grid, 1, "—");
            grid.Controls.Add(MakeCaption("Items"), 0, 2); lblItemsValue = AddDetailRow(grid, 2, "—");
            grid.Controls.Add(MakeCaption("Modified"), 0, 3); lblModifiedValue = AddDetailRow(grid, 3, "—");
            grid.Controls.Add(MakeCaption("Location"), 0, 4); lblPathValue = AddDetailRow(grid, 4, "—");

            pt.Controls.Add(head, 0, 0);
            pt.Controls.Add(divider, 0, 1);
            pt.Controls.Add(detailsHeader, 0, 2);
            pt.Controls.Add(grid, 0, 3);
            previewSurface = pt;
            previewCard.ClipChildren = true;
            previewCard.Controls.Add(pt);
            split.Panel2.Controls.Add(previewCard);
        }

        private Label MakeCaption(string text)
        {
            return new Label { AutoSize = true, Text = text, Font = Theme.Meta, ForeColor = Theme.TextTertiary, Anchor = AnchorStyles.Left, Margin = new Padding(0, 0, S(12), 0) };
        }
        private Label AddDetailRow(TableLayoutPanel grid, int row, string value)
        {
            Label v = new Label
            {
                Dock = DockStyle.Fill,
                Text = value,
                Font = Theme.Body,
                ForeColor = Theme.TextPrimary,
                AutoEllipsis = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 0, S(8), 0)
            };
            grid.Controls.Add(v, 1, row);
            return v;
        }

        private void BuildFooter()
        {
            footerCard = new CardPanel { Dock = DockStyle.Fill, Height = S(46), FillColor = Theme.Layer, Margin = new Padding(0, S(10), 0, 0) };

            TableLayoutPanel ft = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Padding = new Padding(S(16), 0, S(16), 0) };
            ft.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            ft.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            ft.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            ft.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            statusLabel = new Label { Dock = DockStyle.Fill, AutoEllipsis = true, TextAlign = ContentAlignment.MiddleLeft, Font = Theme.Body, ForeColor = Theme.TextSecondary, Text = "Drag a Unity package onto this window, or click \"Open Package\"." };
            countLabel = new Label { AutoSize = true, Anchor = AnchorStyles.None, Font = Theme.Meta, ForeColor = Theme.TextTertiary, Text = "", Margin = new Padding(0, 0, S(14), 0) };
            progressBar = new FluentProgressBar { Width = S(180), Height = S(6), Anchor = AnchorStyles.None, Visible = false, Margin = new Padding(0) };

            ft.Controls.Add(statusLabel, 0, 0);
            ft.Controls.Add(countLabel, 1, 0);
            ft.Controls.Add(progressBar, 2, 0);
            footerSurface = ft;
            footerCard.ClipChildren = true;
            footerCard.Controls.Add(ft);
            rootLayout.Controls.Add(footerCard, 0, 3);
        }

        //====================================================================================================================================================================
        //theming
        //====================================================================================================================================================================
        private void ApplyTheme()
        {
            Theme.Refresh();

            BackColor = Theme.Window;
            if (rootLayout != null) rootLayout.BackColor = Theme.Window;
            toolbarPanel.BackColor = Theme.Window;
            split.BackColor = Theme.Window;
            split.Panel1.BackColor = Theme.Window;
            split.Panel2.BackColor = Theme.Window;

            headerCard.FillColor = Theme.Layer; headerCard.CornerColor = Theme.Window; headerCard.BorderColor = Theme.CardBorder;
            footerCard.FillColor = Theme.Layer; footerCard.CornerColor = Theme.Window; footerCard.BorderColor = Theme.CardBorder;
            treeCard.FillColor = Theme.Card; treeCard.CornerColor = Theme.Window; treeCard.BorderColor = Theme.CardBorder;
            previewCard.FillColor = Theme.Card; previewCard.CornerColor = Theme.Window; previewCard.BorderColor = Theme.CardBorder;
            if (headerSurface != null) headerSurface.BackColor = Theme.Layer;
            if (footerSurface != null) footerSurface.BackColor = Theme.Layer;
            if (previewSurface != null) previewSurface.BackColor = Theme.Card;
            if (treeEmptyHint != null) { treeEmptyHint.BackColor = Theme.Card; treeEmptyHint.ForeColor = Theme.TextTertiary; }
            headerCard.Invalidate(); footerCard.Invalidate(); treeCard.Invalidate(); previewCard.Invalidate();

            titleLabel.ForeColor = Theme.TextPrimary;
            subtitleLabel.ForeColor = Theme.TextSecondary;
            previewTitle.ForeColor = Theme.TextPrimary;
            previewKind.ForeColor = Theme.TextSecondary;
            statusLabel.ForeColor = Theme.TextSecondary;
            countLabel.ForeColor = Theme.TextTertiary;

            treeView.BackColor = Theme.Card;
            treeView.ForeColor = Theme.TextPrimary;
            treeView.LineColor = Theme.Divider;

            headerIcon.Image = LoadAppIcon();

            btnOpen.Image = Icons.OpenIcon(Theme.Scale, Theme.OnAccent);
            btnExtractSelected.Image = Icons.ExtractIcon(Theme.Scale, Theme.TextPrimary);
            btnExtractAll.Image = Icons.ExtractAllIcon(Theme.Scale, Theme.TextPrimary);
            btnPreview.Image = Icons.PreviewIcon(Theme.Scale, Theme.TextPrimary);
            btnOpen.ImageAlign = ContentAlignment.MiddleLeft;
            btnExtractSelected.ImageAlign = ContentAlignment.MiddleLeft;
            btnExtractAll.ImageAlign = ContentAlignment.MiddleLeft;
            btnPreview.ImageAlign = ContentAlignment.MiddleLeft;
            SizeButton(btnOpen); SizeButton(btnExtractSelected); SizeButton(btnExtractAll);
            SizeButton(btnPreview, S(96));

            if (PreviewIsOpen()) previewWin.ApplyTheme();

            progressBar.Invalidate();
            thumb.Invalidate();
            Theme.ApplyTitleBar(this);
        }

        private Image LoadAppIcon()
        {
            try
            {
                Icon ic = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                if (ic != null) return ic.ToBitmap();
            }
            catch { }
            try { return SystemIcons.Application.ToBitmap(); } catch { return null; }
        }

        private void SizeButton(ModernButton b, int minWidth = 0)
        {
            if (minWidth <= 0) minWidth = S(112);
            using (Graphics g = CreateGraphics())
            {
                Size t = TextRenderer.MeasureText(g, b.Text, b.Font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
                int w = t.Width + S(20) * 2 + (b.Image != null ? S(16 + 8) : 0);
                b.Width = Math.Max(minWidth, w);
            }
        }

        //====================================================================================================================================================================
        //package extraction
        //====================================================================================================================================================================
        void extractUnityPackage(string package_path)
        {
            package_name = Path.GetFileNameWithoutExtension(package_path);
            package_asset_count = 0;

            ClosePreview();   //the temp asset tree it points at is about to be rebuilt
            treeView.Nodes.Clear();
            UpdateTreeEmptyState();
            DisposeThumbnails();

            SetBusy(true);
            ShowEmptyPreview("Loading " + package_name + "…");
            backgroundWorkerExtractPackage.RunWorkerAsync(package_path);
        }

        private void backgroundWorkerExtractPackage_DoWork(object sender, DoWorkEventArgs e)
        {
            string package_path = (string)e.Argument;
            Dictionary<string, Bitmap> assets = new Dictionary<string, Bitmap>();

            clearDirectory(extract_folder);
            clearDirectory(build_folder);

            Stream in_stream = File.OpenRead(package_path);
            Stream gzip_stream = new GZipInputStream(in_stream);
            TarArchive tar = TarArchive.CreateInputTarArchive(gzip_stream, Encoding.Default);
            tar.ExtractContents(extract_folder);
            tar.Close();
            gzip_stream.Close();
            in_stream.Close();

            //each package entry lives in a random-named folder holding "asset", "pathname", "preview.png" and "asset.meta"
            string[] folders = Directory.GetDirectories(extract_folder);
            for (int i = 0; i < folders.Length; i++)
            {
                backgroundWorkerExtractPackage.ReportProgress((int)((float)(i + 1) / folders.Length * 100));

                string folder = folders[i];
                string asset_current_path = folder + Path.DirectorySeparatorChar + "asset";
                string asset_pathname_path = folder + Path.DirectorySeparatorChar + "pathname";
                string asset_thumbnail_path = folder + Path.DirectorySeparatorChar + "preview.png";

                if (File.Exists(asset_current_path))
                {
                    //only the first non-empty line of "pathname" is the path (a GUID may follow)
                    string asset_new_path = File.ReadLines(asset_pathname_path)
                        .Select(line => line.Trim())
                        .FirstOrDefault(line => !string.IsNullOrWhiteSpace(line));

                    if (string.IsNullOrEmpty(asset_new_path)) continue;

                    asset_new_path = asset_new_path.Replace('/', Path.DirectorySeparatorChar);

                    string asset_new_path_absolute = Path.Combine(build_folder, asset_new_path);
                    Directory.CreateDirectory(Path.GetDirectoryName(asset_new_path_absolute));
                    File.Move(asset_current_path, asset_new_path_absolute);

                    //Unity only writes preview.png for assets it had an import preview for.
                    Bitmap thumbnail = null;
                    if (File.Exists(asset_thumbnail_path))
                    {
                        try { using (var raw = new Bitmap(asset_thumbnail_path)) thumbnail = new Bitmap(raw); }
                        catch { thumbnail = null; }
                    }
                    assets.Add(asset_new_path, thumbnail);
                }
            }

            e.Result = assets;
        }

        private void backgroundWorkerExtractPackage_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar.Visible = true;
            progressBar.Marquee = false;
            progressBar.Value = e.ProgressPercentage;
            statusLabel.Text = "Reading files… " + e.ProgressPercentage + "%";
        }

        private void backgroundWorkerExtractPackage_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            SetBusy(false);
            progressBar.Visible = false;

            if (e.Error != null)
            {
                MessageBox.Show("The Unity package file \"" + package_name + "\" could not be extracted.\n\n" + e.Error.Message, "Open Unity Package", MessageBoxButtons.OK, MessageBoxIcon.Error);
                statusLabel.Text = "Could not extract \"" + package_name + "\".";
                btnOpen.Enabled = true;
                return;
            }

            asset_thumbnails = (Dictionary<string, Bitmap>)e.Result;

            treeView.BeginUpdate();
            foreach (KeyValuePair<string, Bitmap> kvp in asset_thumbnails) addTreeViewItem(kvp.Key);
            treeView.ExpandAll();
            treeView.EndUpdate();

            package_asset_count = asset_thumbnails.Count;
            subtitleLabel.Text = package_name + "   •   " + package_asset_count + (package_asset_count == 1 ? " asset" : " assets");
            countLabel.Text = package_asset_count + (package_asset_count == 1 ? " asset" : " assets");
            btnExtractAll.Enabled = true;
            statusLabel.Text = "Ready — select an asset to preview it, or extract it below.";

            if (treeView.Nodes.Count > 0)
            {
                treeView.SelectedNode = treeView.Nodes[0];
                treeView.Focus();
            }
            else
            {
                ShowEmptyPreview("This package does not contain any assets.");
            }
            UpdateTreeEmptyState();
        }

        private void UpdateTreeEmptyState()
        {
            if (treeEmptyHint != null) treeEmptyHint.Visible = (treeView.Nodes.Count == 0);
        }

        void addTreeViewItem(string path)
        {
            string[] path_nodes = path.Split(new[] { Path.DirectorySeparatorChar });
            TreeNodeCollection node_collection = treeView.Nodes;
            TreeNode created = null;
            for (int i = 0; i < path_nodes.Length; i++)
            {
                string node_name = path_nodes[i];
                TreeNode found = null;
                foreach (TreeNode node in node_collection) if (node.Text == node_name) { found = node; break; }

                bool leaf = i == path_nodes.Length - 1;
                if (found == null)
                {
                    found = node_collection.Add(node_name);
                    found.ImageIndex = found.SelectedImageIndex = leaf
                        ? Icons.IndexOf(Icons.Classify(Path.GetExtension(node_name)))
                        : Icons.IndexOf(AssetKind.Folder);
                    found.ToolTipText = path.Replace(Path.DirectorySeparatorChar, '/');
                }
                created = found;
                node_collection = found.Nodes;
            }
        }

        bool isTreeViewNodeAFolder(TreeNode node) { return node != null && node.Nodes.Count > 0; }

        //====================================================================================================================================================================
        //preview
        //====================================================================================================================================================================
        private void treeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            btnExtractSelected.Enabled = true;
            PopulateDetails(e.Node);
            if (PreviewIsOpen()) OpenPreviewFor(e.Node, false);
        }

        private void treeView_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            OpenPreviewFor(e.Node, true);
        }

        private bool PreviewIsOpen()
        {
            return previewWin != null && !previewWin.IsDisposed && previewWin.Visible;
        }

        /// <summary>Reuse the single preview window; <paramref name="activate"/> only when the user
        /// asked for it, so following the tree selection never steals focus.</summary>
        private void OpenPreviewFor(TreeNode node, bool activate)
        {
            if (node == null) return;
            if (isTreeViewNodeAFolder(node) && !PreviewIsOpen()) return;

            bool folder = isTreeViewNodeAFolder(node);
            string rel = node.FullPath;
            Bitmap thumb;
            asset_thumbnails.TryGetValue(rel, out thumb);

            if (previewWin == null || previewWin.IsDisposed)
            {
                previewWin = new PreviewWindow();
                previewWin.FormClosed += delegate { previewWin = null; };
                previewWin.Show(this);
            }
            previewWin.LoadAsset(node.Text, Path.Combine(build_folder, rel), folder, thumb);
            if (activate) previewWin.BringToFront();
        }

        private void ClosePreview()
        {
            if (previewWin != null && !previewWin.IsDisposed) previewWin.Close();
            previewWin = null;
        }

        private void PopulateDetails(TreeNode node)
        {
            if (node == null) { ShowEmptyPreview("Select an item to see its details"); return; }

            string rel = node.FullPath;
            string abs = Path.Combine(build_folder, rel);
            bool folder = isTreeViewNodeAFolder(node);

            previewTitle.Text = node.Text;

            if (folder)
            {
                previewKind.Text = "Folder  •  " + (rel.Contains(Path.DirectorySeparatorChar.ToString()) ? "container" : "top-level folder");

                long bytes = 0; int files = 0;
                try
                {
                    foreach (string f in Directory.EnumerateFiles(abs, "*", SearchOption.AllDirectories))
                    {
                        try { bytes += new FileInfo(f).Length; files++; } catch { }
                    }
                }
                catch { }

                lblTypeValue.Text = "Folder";
                lblSizeValue.Text = FormatBytes(bytes);
                lblItemsValue.Text = files + (files == 1 ? " asset" : " assets");
                try { lblModifiedValue.Text = Directory.GetLastWriteTime(abs).ToString("yyyy-MM-dd HH:mm"); }
                catch { lblModifiedValue.Text = "—"; }
                thumb.Image = null;
                thumb.Placeholder = AssetKind.Folder;
                btnPreview.Enabled = false;
            }
            else
            {
                string ext = Path.GetExtension(rel);
                AssetKind kind = Icons.Classify(ext);

                Bitmap t;
                asset_thumbnails.TryGetValue(rel, out t);
                if (t != null && (t.Width < 2 || t.Height < 2)) t = null;
                thumb.Image = t;
                thumb.Placeholder = kind;
                previewKind.Text = Icons.FriendlyName(kind) + (t == null ? "  •  no preview in package" : "");

                lblTypeValue.Text = string.IsNullOrEmpty(ext) ? Icons.FriendlyName(kind) : Icons.FriendlyName(kind) + " (" + ext + ")";
                try
                {
                    FileInfo fi = new FileInfo(abs);
                    lblSizeValue.Text = FormatBytes(fi.Length);
                    lblModifiedValue.Text = fi.LastWriteTime.ToString("yyyy-MM-dd HH:mm");
                }
                catch { lblSizeValue.Text = "—"; lblModifiedValue.Text = "—"; }
                lblItemsValue.Text = "—";
                btnPreview.Enabled = true;
            }

            string display = rel.Replace(Path.DirectorySeparatorChar, '/');
            lblPathValue.Text = display;
            sharedTips.SetToolTip(lblPathValue, display);
            thumb.Invalidate();
        }

        private void ShowEmptyPreview(string hint)
        {
            previewTitle.Text = "No asset selected";
            previewKind.Text = hint;
            lblTypeValue.Text = "—";
            lblSizeValue.Text = "—";
            lblItemsValue.Text = "—";
            lblModifiedValue.Text = "—";
            lblPathValue.Text = "—";
            thumb.Image = null;
            thumb.Invalidate();
            btnPreview.Enabled = false;
        }

        //====================================================================================================================================================================
        //UI interactions
        //====================================================================================================================================================================
        private void SetBusy(bool busy)
        {
            btnOpen.Enabled = !busy;
            btnExtractSelected.Enabled = false;
            btnExtractAll.Enabled = false;
            btnPreview.Enabled = false;
            treeView.Enabled = !busy;
            if (busy)
            {
                progressBar.Visible = true;
                progressBar.Marquee = true;
                progressBar.Value = 0;
                statusLabel.Text = "Extracting package…";
            }
        }

        private void toolStripButtonOpenPackage_Click(object sender, EventArgs e)
        {
            if (openFileDialogUnityPackage.ShowDialog() == DialogResult.OK)
                extractUnityPackage(openFileDialogUnityPackage.FileName);
        }

        private void toolStripButtonExtractSelected_Click(object sender, EventArgs e)
        {
            if (treeView.SelectedNode == null) return;

            if (isTreeViewNodeAFolder(treeView.SelectedNode)) saveFileDialog.Filter = "Folder|folder";
            else
            {
                string extension = Path.GetExtension(treeView.SelectedNode.FullPath);
                saveFileDialog.Filter = extension + " file|" + extension;
            }
            saveFileDialog.FileName = treeView.SelectedNode.Text;
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
                copyFile(build_folder + Path.DirectorySeparatorChar + treeView.SelectedNode.FullPath, saveFileDialog.FileName);
        }

        private void toolStripButtonExtractAll_Click(object sender, EventArgs e)
        {
            saveFileDialog.Filter = "Folder|folder";
            saveFileDialog.FileName = package_name;
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
                copyFile(build_folder, saveFileDialog.FileName);
        }

        private void Form1_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Link : DragDropEffects.None;
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] paths = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (paths.Length == 1)
                {
                    if (Path.GetExtension(paths[0]) == ".unitypackage") extractUnityPackage(paths[0]);
                    else MessageBox.Show("The file \"" + Path.GetFileName(paths[0]) + "\" is not a Unity package.", "Open Unity Package", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                }
            }
        }

        private void treeView_ItemDrag(object sender, ItemDragEventArgs e)
        {
            treeView.SelectedNode = (TreeNode)e.Item;
            string[] filename_to_drag = { build_folder + Path.DirectorySeparatorChar + treeView.SelectedNode.FullPath };
            treeView.DoDragDrop(new DataObject(DataFormats.FileDrop, filename_to_drag), DragDropEffects.Copy);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            ConfigureSplit();
            ShowEmptyPreview("Select an item to see its details");
            UpdateTreeEmptyState();
        }

        private void ConfigureSplit()
        {
            if (split == null || split.Width <= 0) return;
            try
            {
                split.Panel1MinSize = S(220);
                split.Panel2MinSize = S(280);
                int lo = split.Panel1MinSize;
                int hi = split.Width - split.Panel2MinSize - split.SplitterWidth;
                if (hi > lo)
                {
                    int target = (int)(split.Width * 0.44f);
                    split.SplitterDistance = Math.Max(lo, Math.Min(hi, target));
                }
            }
            catch { }
        }

        //Live light/dark + accent following, matching the system theme switch.
        protected override void WndProc(ref Message m)
        {
            const int WM_SETTINGCHANGE = 0x001A;
            base.WndProc(ref m);
            if (m.Msg == WM_SETTINGCHANGE)
            {
                Theme.Refresh();
                if (Theme.IsDark != lastIsDark || Theme.Accent != lastAccent)
                {
                    lastIsDark = Theme.IsDark;
                    lastAccent = Theme.Accent;
                    ApplyTheme();
                    Invalidate(true);
                }
            }
        }

        //====================================================================================================================================================================
        //other file operations
        //====================================================================================================================================================================
        void clearDirectory(string path)
        {
            if (Directory.Exists(path)) Directory.Delete(path, true);
            Directory.CreateDirectory(path);
        }

        void copyFile(string source_path, string dest_path)
        {
            if (File.Exists(source_path))
            {
                File.Copy(source_path, dest_path, true);
            }
            else if (Directory.Exists(source_path))
            {
                Directory.CreateDirectory(dest_path);
                foreach (string subfolder in Directory.GetDirectories(source_path))
                    copyFile(subfolder, dest_path + Path.DirectorySeparatorChar + Path.GetFileName(subfolder));
                foreach (string subfile in Directory.GetFiles(source_path))
                    copyFile(subfile, dest_path + Path.DirectorySeparatorChar + Path.GetFileName(subfile));
            }
        }

        internal static string FormatBytes(long bytes)
        {
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            double v = bytes;
            int u = 0;
            while (v >= 1024 && u < units.Length - 1) { v /= 1024; u++; }
            return (u == 0 ? v.ToString("0") : v.ToString("0.0")) + " " + units[u];
        }

        private void DisposeThumbnails()
        {
            thumb.Image = null;
            foreach (Bitmap b in asset_thumbnails.Values) if (b != null) b.Dispose();
            asset_thumbnails.Clear();
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            DisposeThumbnails();
            if (Directory.Exists(extract_folder)) Directory.Delete(extract_folder, true);
            if (Directory.Exists(build_folder)) Directory.Delete(build_folder, true);
        }
    }
}
