using System;
using System.Text;
using System.IO;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.Data.SqlClient;
using System.Runtime.CompilerServices;

namespace DiskUsageAnalyzer
{


	public class FolderToDb
	{
		int mapdepth = 0;
		private string path = "";
		private BackgroundWorker DBbackgroundWorker;
		public string ConStr;
		public static SqlConnection SqlCon;
		public static DataTable table = new DataTable();
		public SqlCommand Cmd = new SqlCommand();
		public static long totRecs;
		public void Connectdb(string ConStr)
		{
			//    Dim DL As MSDASC.DataLinks

			//  DL = New MSDASC.DataLinks
			if (SqlCon == null)
				SqlCon = new SqlConnection();
			// DL.PromptEdit(AdoCon)
			// Debug.Print NavCon.ConnectionString
			//AdoCon.ConnectionString = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + txDB.Text
			//AdoCon.ConnectionString = "Provider=SQLNCLI11.1;Data Source=IT-2014-W8\SQL2014;Initial Catalog=Files;Integrated Security=True"
			SqlCon.ConnectionString =  "Data Source=DESKTOP-BO2EKPI\\SQL2014;Initial Catalog=FileAnalysis;Integrated Security=True";
			SqlCon.Open();

			Cmd.Connection= SqlCon;
			Cmd.CommandType = CommandType.StoredProcedure;
			Cmd.CommandText = "CreateFileList";
			Cmd.ExecuteNonQuery();

			// Create four typed columns in the DataTable.
			table.Columns.Add("RecordNo", typeof(long));
			table.Columns.Add("FileName", typeof(string));
			table.Columns.Add("FilePath", typeof(string));
			table.Columns.Add("Size", typeof(long));
			table.Columns.Add("CreationDate", typeof(DateTime));
			table.Columns.Add("ModifiedDate", typeof(DateTime));
			table.Columns.Add("ComputerName", typeof(string));




		}


		public static void DoBulkInsert()
		{
			SqlBulkCopy bulkCopy = new SqlBulkCopy(SqlCon);
			bulkCopy.DestinationTableName = "dbo.FileList";
			bulkCopy.WriteToServer(table);
		}



		public static void DoInsertToDataTable(DataTable table, string Folder, string Fln, Int64 nSize,DateTime CreationDate, DateTime LastWriteTime, string ComputerName)
		{
			long recsDone = table.Rows.Count;
			long isAstep = recsDone % 100000;
			totRecs++;
			if ((isAstep == 0) && (recsDone >0) )
			{ 
				DoBulkInsert();
				table.Clear();

			}

			table.Rows.Add(totRecs, Fln, Folder, nSize,CreationDate, LastWriteTime, ComputerName);
		
		}


		public FolderToDb(string scannedpath, BackgroundWorker backgroundWorker,string ConStr)
		{
			this.path = scannedpath;
			this.DBbackgroundWorker = backgroundWorker;
			Connectdb(ConStr);

		}

		//Analyzes the folder given when the object was created and calculate disk.
		public FolderItem AnalyzeFolder()
		{
			TreeNode rootTreeNode = new TreeNode(ShortFolderName(path));
			FolderItem root = new FolderItem(path, rootTreeNode);
			rootTreeNode.Tag = root;
			TraverseFolder(root);
			CalculateDisk(root);
			return root;
		}



		//Recursively travese the folder structure.
		public void TraverseFolder(FolderItem root)
		{
			try
			{
				DirectoryInfo dirinfo = new DirectoryInfo(root.FolderName);
				DBbackgroundWorker.ReportProgress(0, dirinfo.FullName);
				 StoreFileInfo(dirinfo,table);

				foreach (DirectoryInfo folder in dirinfo.GetDirectories())
				{
					TreeNode newNode = new TreeNode(ShortFolderName(folder.FullName));
					FolderItem child = new FolderItem(folder.FullName, newNode);
					newNode.Tag = child;

					TraverseFolder(child);
					root.TotalSize += child.TotalSize;
					root.Children.Add(child);
					root.FolderNode.Nodes.Add(child.FolderNode);
				}
			}
			catch (UnauthorizedAccessException)
			{
				//If we cannot traverse further because of the directory access rules then stop.
				return;
			}
		}



		//Calculates the size in bytes of a folder.
		private static void StoreFileInfo(DirectoryInfo info,DataTable table)
		{
			try
			{
				long size = 0;
				foreach (FileInfo file in info.GetFiles())
				{ size += file.Length;
					DoInsertToDataTable(table, file.DirectoryName, file.Name, file.Length,file.CreationTime, file.LastWriteTime, Environment.MachineName);
				}
			 
			}
			catch (UnauthorizedAccessException)
			{
				//Didn't have access, the size of the folder is then 0 bytes
				
			}
			catch (FileNotFoundException)
			{
				//File somehow not found, adds 0 to the size;
				
			}
		}



		public static string ShortFolderName(string fullpath)
		{
			string path = fullpath.Substring(fullpath.LastIndexOf(Path.DirectorySeparatorChar) + 1);
			if (string.IsNullOrEmpty(path.Trim()))
				return fullpath;
			else
				return path;
		}

		public void CalculateDisk(FolderItem root)
		{
			float angle = root.StartAngle;
			float sweep = root.SweepAngle;
			int level = root.Level;
			foreach (FolderItem child in root.Children)
			{
				child.PercentageOfParent = (float)child.TotalSize / (float)root.TotalSize;
				child.Level = level + 1;
				child.StartAngle = Math.Abs(angle);
				child.SweepAngle = Math.Abs(child.PercentageOfParent * sweep);

				CalculateDisk(child);

				angle += child.SweepAngle;
			}

			if (root.SweepAngle > 0.001)
				mapdepth = Math.Max(mapdepth, root.Level);
		}
	}



	public class FolderAnalyzer
	{
		int mapdepth = 0;
		string path = "";
		BackgroundWorker worker;

        //Depth of folder-map (for zoom)
		public int MapDepth { get { return mapdepth;} set{mapdepth = value;} }

        //Constructor
		public FolderAnalyzer(string path, BackgroundWorker worker)
		{
			this.path = path;
			this.worker = worker;
		}

        //Analyzes the folder given when the object was created and calculate disk.
		public FolderItem AnalyzeFolder()
		{
			TreeNode rootTreeNode = new TreeNode(ShortFolderName(path));
			FolderItem root = new FolderItem(path, rootTreeNode);
			rootTreeNode.Tag = root;
			TraverseFolder(root);
			root.PercentageOfParent = 1f;
			root.StartAngle = 0f;
			root.SweepAngle = 1f;
			root.Level = 1;
			CalculateDisk(root);
			return root;
		}

        //Recursively travese the folder structure.
		public void TraverseFolder( FolderItem root )
		{
			try
			{
				DirectoryInfo dirinfo = new DirectoryInfo(root.FolderName);
				worker.ReportProgress(0, dirinfo.FullName);
				root.Size = root.TotalSize = CalculateSize(dirinfo);

				foreach (DirectoryInfo folder in dirinfo.GetDirectories())
				{
					TreeNode newNode =  new TreeNode(ShortFolderName(folder.FullName));
					FolderItem child = new FolderItem(folder.FullName, newNode);
					newNode.Tag = child;
					
					TraverseFolder(child);
					root.TotalSize += child.TotalSize;
					root.Children.Add(child);
					root.FolderNode.Nodes.Add(child.FolderNode);
				}
			}
			catch (UnauthorizedAccessException)
			{
				//If we cannot traverse further because of the directory access rules then stop.
				return;
			}
		}

        //Calculates the size in bytes of a folder.
		private static long CalculateSize( DirectoryInfo info )
		{
			try
			{
				long size = 0;
				foreach (FileInfo file in info.GetFiles())
					size += file.Length;
				return size;
			}
			catch (UnauthorizedAccessException)
			{
				//Didn't have access, the size of the folder is then 0 bytes
				return 0;
			}
			catch (FileNotFoundException) 
			{
				//File somehow not found, adds 0 to the size;
				return 0;
			}
		}

        //Calculate pie-slice sizes and orientations.
		public void CalculateDisk(FolderItem root)
		{
			float angle = root.StartAngle;
			float sweep = root.SweepAngle;
			int level = root.Level;
			foreach (FolderItem child in root.Children)
			{
				child.PercentageOfParent = (float)child.TotalSize / (float)root.TotalSize;
				child.Level = level + 1;
				child.StartAngle = Math.Abs( angle );
				child.SweepAngle = Math.Abs(child.PercentageOfParent * sweep);

				CalculateDisk(child);

				angle += child.SweepAngle;
			}

			if( root.SweepAngle > 0.001 )
				mapdepth = Math.Max(mapdepth, root.Level);
		}


		//Static helper methods:

        //Format a size in bytes into a string of human readable sizes.
		public static string FormatFileSize(long sizeInBytes )
		{
			StringBuilder b = new StringBuilder();
			long gb = 1024 * 1024 * 1024;
			long mb = 1024 * 1024;
			long kb = 1024;

            //Number that decrements every time a number is appended to keep the tooltips less cluttered.
            int numinfo = 2;

			if (sizeInBytes > gb && numinfo > 0)
			{
				b.Append((int)(sizeInBytes / gb) + "gb " );
				sizeInBytes %= gb;
                numinfo--;
			}

            if (sizeInBytes > mb && numinfo > 0)
			{
				b.Append((int)(sizeInBytes / mb) + "mb ");
				sizeInBytes %= mb;
                numinfo--;
			}

            if (sizeInBytes > kb && numinfo > 0)
			{
				b.Append((int)(sizeInBytes / kb) + "kb ");
				sizeInBytes %= kb;
                numinfo--;
			}

            if (sizeInBytes > 0 && numinfo > 0)
			{
				b.Append( sizeInBytes + "b");
			}

			if (string.IsNullOrEmpty(b.ToString()))
				b.Append("0b");

			return b.ToString().TrimEnd();
		}

	    //Returns the last folder name of a full path.
		public static string ShortFolderName(string fullpath)
		{
			string path = fullpath.Substring(fullpath.LastIndexOf(Path.DirectorySeparatorChar)+1);
			if (string.IsNullOrEmpty(path.Trim()))
				return fullpath;
			else
				return path;
		}

		}
}
