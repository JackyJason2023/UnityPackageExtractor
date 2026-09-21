
namespace UnityPackageExtractor
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (backgroundWorkerExtractPackage != null) backgroundWorkerExtractPackage.Dispose();
                if (openFileDialogUnityPackage != null) openFileDialogUnityPackage.Dispose();
                if (saveFileDialog != null) saveFileDialog.Dispose();
                if (components != null) components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        // The visible UI is assembled in code (Form1.BuildModernLayout) so it can honour the
        // runtime theme and per-monitor DPI. This designer block only configures the window and
        // the non-visual components.
        private void InitializeComponent()
        {
            this.openFileDialogUnityPackage = new System.Windows.Forms.OpenFileDialog();
            this.saveFileDialog = new System.Windows.Forms.SaveFileDialog();
            this.backgroundWorkerExtractPackage = new System.ComponentModel.BackgroundWorker();
            this.SuspendLayout();
            //
            // openFileDialogUnityPackage
            //
            this.openFileDialogUnityPackage.Filter = "Unity packages (*.unitypackage)|*.unitypackage";
            this.openFileDialogUnityPackage.Title = "Open a Unity package";
            //
            // saveFileDialog
            //
            this.saveFileDialog.Title = "Select location to extract assets to";
            //
            // backgroundWorkerExtractPackage
            //
            this.backgroundWorkerExtractPackage.WorkerReportsProgress = true;
            this.backgroundWorkerExtractPackage.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorkerExtractPackage_DoWork);
            this.backgroundWorkerExtractPackage.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.backgroundWorkerExtractPackage_ProgressChanged);
            this.backgroundWorkerExtractPackage.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.backgroundWorkerExtractPackage_RunWorkerCompleted);
            //
            // Form1
            //
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(1000, 660);
            this.MinimumSize = new System.Drawing.Size(780, 520);
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Unity Package Extractor";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.Form1_FormClosed);
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.Form1_DragDrop);
            this.DragOver += new System.Windows.Forms.DragEventHandler(this.Form1_DragOver);
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.OpenFileDialog openFileDialogUnityPackage;
        private System.Windows.Forms.SaveFileDialog saveFileDialog;
        private System.ComponentModel.BackgroundWorker backgroundWorkerExtractPackage;
    }
}
