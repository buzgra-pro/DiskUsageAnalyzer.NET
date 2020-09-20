using System;
using System.ComponentModel;
using System.Windows.Forms;
using System.IO;
using System.Configuration;

namespace DiskUsageAnalyzer
{
	public partial class DiskUsageAnalyzerForm : Form
	{
		FolderAnalyzer analyzer;
		FolderToDb sendToDB;
		string scannedpath = "";
		string selectedpath = "";

		public string SelectedPath { get { return selectedpath; } set { selectedpath = value; } }
			
		public DiskUsageAnalyzerForm()
		{
			InitializeComponent();
		}

		private void toolScanFolder_Click(object sender, EventArgs e)
		{
			if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
				ScanFolder(folderBrowserDialog.SelectedPath);
		}

		private void toolRefresh_Click(object sender, EventArgs e)
		{
			ScanFolder(scannedpath);
		}

		private void ScanFolder(string path)
		{
			scannedpath = path;
			toolBar.Enabled = false;
			toolStripStatusLabel.Text = "Analyzing...";
			backgroundWorker.RunWorkerAsync();
		}


		private void ScanFolderToDB(string path)
		{
			scannedpath = path;
			toolBar.Enabled = false;
			toolStripStatusLabel.Text = "Analyzing...";
			backgroundWorkerDB.RunWorkerAsync();
			
		}



		private void toolZoomIn_Click(object sender, EventArgs e)
		{
			circleMap.MapDepth--;
			Invalidate(true);
		}

		private void toolZoomOut_Click(object sender, EventArgs e)
		{
			circleMap.MapDepth++;
			Invalidate(true);
		}

		private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
		{
			analyzer = new FolderAnalyzer(scannedpath, backgroundWorker);
			FolderItem root = analyzer.AnalyzeFolder();
			e.Result = root;
		}
		
		private void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			toolStripStatusLabel.Text = "Preparing disk...";
			toolStripStatusLabel.Invalidate();

			if (e.Error == null)
			{
				FolderItem result = (FolderItem)e.Result;
				foreach (FolderItem item in result.Children)
				{
					treeView.Nodes.Clear();
					treeView.Nodes.Add(result.FolderNode);
				}
				circleMap.RootItem = result;
				circleMap.MapDepth = analyzer.MapDepth;
				toolStripStatusLabel.Text = "Finnished analyzing";
			}
			else
				toolStripStatusLabel.Text = "Unexpected error during disk analysis";

			toolBar.Enabled = true;
			treeView.Nodes[0].Toggle();
			
		}

		private void backgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			string currentfolder = (string)e.UserState;
			toolStripStatusLabel.Text = "Scanning folder: " + currentfolder;
			toolStripStatusLabel.Invalidate();
		}

		private void DiskUsageAnalyzerForm_Load(object sender, EventArgs e)
		{
			txDatasource.Text= ConfigurationManager.AppSettings.Get("ConnStr");

			treeView.Nodes.Clear();
			circleMap.DiskUsageAnalyzerForm = this;
		}

		private void treeView_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
		{
			treeView.SelectedNode = e.Node;
			if (e.Button == MouseButtons.Right)
			{
				rightClickMenu.Show(treeView, e.X, e.Y);
				selectedpath = e.Node.FullPath;
			}
		}

		private void viewThisItemAsTheRootToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ViewSelectedItemAsRoot();
		}

		private void openFolderToolStripMenuItem_Click(object sender, EventArgs e)
		{
			OpenSelectedFolder();
		}

		public void ViewSelectedItemAsRoot()
		{
			FolderItem root = (FolderItem)treeView.SelectedNode.Tag;
			root.PercentageOfParent = 1f;
			root.StartAngle = 0f;
			root.SweepAngle = 1f;
			root.Level = 1;
			analyzer.CalculateDisk(root);
			circleMap.RootItem = root;
			circleMap.Invalidate(true);
		}

		public void OpenSelectedFolder()
		{
			FolderItem item = (FolderItem)treeView.SelectedNode.Tag;
			System.Diagnostics.Process.Start(item.FolderName);
		}

        private void toolScanSpecialFolder_ButtonClick(object sender, EventArgs e)
        {
            if(!toolScanSpecialFolder.DropDown.Visible)
                toolScanSpecialFolder.ShowDropDown();
        }

        private void scanDocumentsTool_Click(object sender, EventArgs e)
        {
            ScanFolder(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
        }

        private void scanPicturesTool_Click(object sender, EventArgs e)
        {
            ScanFolder(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures));
        }

        private void scanMusicTool_Click(object sender, EventArgs e)
        {
            ScanFolder(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic));
        }

        private void scanDesktopTool_Click(object sender, EventArgs e)
        {
            ScanFolder(Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
        }

        private void scanProgramsTool_Click(object sender, EventArgs e)
        {
            ScanFolder(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));
        }

        private void toolAbout_Click(object sender, EventArgs e)
        {
            FormAbout aboutbox = new FormAbout();
            aboutbox.CreateControl();
            aboutbox.ShowDialog();
        }

        private void treeView_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            ViewSelectedItemAsRoot();
        }

		private void toolBar_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
		{

		}

		private void recordToDBToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
				ScanFolderToDB(folderBrowserDialog.SelectedPath);
		}

	
	
		private void backgroundWorkerDB_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			toolStripStatusLabel.Text = "Preparing disk...";
			toolStripStatusLabel.Invalidate();

			if (e.Error == null)
			{
				FolderItem result = (FolderItem)e.Result;
				foreach (FolderItem item in result.Children)
				{
					treeView.Nodes.Clear();
					treeView.Nodes.Add(result.FolderNode);
				}
				toolStripStatusLabel.Text = "Finnished analyzing";
			}
			else
				toolStripStatusLabel.Text = "Unexpected error during disk analysis";

			toolBar.Enabled = true;
			treeView.Nodes[0].Toggle();


		}

		private void backgroundWorkerDB_DoWork_1(object sender, DoWorkEventArgs e)
		{
						
			sendToDB = new FolderToDb(scannedpath, backgroundWorkerDB,txDatasource.Text);
			//sendToDB.ConStr = txDatasource.Text;

			FolderItem root = sendToDB.AnalyzeFolder();
			e.Result = root;
		}

		private void backgroundWorkerDB_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			string currentfolder = (string)e.UserState;
			toolStripStatusLabel.Text = "Scanning folder: " + currentfolder;
			toolStripStatusLabel.Invalidate();
		}

		private void backgroundWorkerDB_RunWorkerCompleted_1(object sender, RunWorkerCompletedEventArgs e)
		{
			toolStripStatusLabel.Text = "Preparing disk...";
			toolStripStatusLabel.Invalidate();

			if (e.Error == null)
			{
				FolderItem result = (FolderItem)e.Result;
				foreach (FolderItem item in result.Children)
				{
					treeView.Nodes.Clear();
					treeView.Nodes.Add(result.FolderNode);
				}
				FolderToDb.DoBulkInsert();
				FolderToDb.SqlCon.Close();
				toolStripStatusLabel.Text = "Finnished analyzing";
				
			}
			else
				toolStripStatusLabel.Text = "Unexpected error during disk analysis";

			toolBar.Enabled = true;
			treeView.Nodes[0].Toggle();
			


		}

		private void SetDB_Click(object sender, EventArgs e)
		{

		}
	}
}
