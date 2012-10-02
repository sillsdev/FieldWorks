/* Title	: NetworkSelect
 * Author	: Jürgen Van Gorp - TIData BVBA IT Consultancy - Belgium - Jurgen.Van.Gorp@tidata.com
 * Version	: 1.0 - September 2004
 * Purpose  : Perform Network access using c#
 * Info		: Credits go to Adam ej Woods, http://www.aejw.com. Parts of his "Network Drives" code
 *			  was used in this project, although maybe no longer recognisable :-)
 * Info		: Credits go to Marc Merritt, http://www.thecodeproject.com/csharp/CompPickerLib.asp?, see
 *			  also http://www.thecodeproject.com/script/profile/whos_who.asp?id=2851, for his work on the
 *			  CompPicker and NetSend applications.
 * Info		: Credits go to Richard Deeming http://www.codeproject.com/script/profile/whos_who.asp?id=34187,
 *			  see also his Network shares http://www.codeproject.com/csharp/networkshares.asp.
 */

using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.IO;
using System.Data.SqlClient;

using Trinet.Networking;

namespace TIData.NetworkSelect
{
	public delegate void TreeViewEventHandler(object sender, TreeViewEventArgs e);
	/// <summary>
	/// Allows you to browse the Network and pick shares, computers, directories or files.
	/// Will ask for a password if a computer is not accessible.
	/// </summary>
	public class NetworkSelect : UserControl
	{
		#region Constants
		public  const bool	HIDE_CHECKBOXES			= false;
		public  const bool	SHOW_CHECKBOXES			= true;

		public  const bool	HIDE_FILES				= false;
		public  const bool	SHOW_FILES				= true;

		public  const bool	HIDE_SHARES				= false;
		public	const bool  SHOW_SHARES				= true;

		public  const bool	HIDE_HIDDENSHARES		= false;
		public	const bool  SHOW_HIDDENSHARES		= true;

		public  const bool	HIDE_DIRECTORIES		= false;
		public  const bool	SHOW_DIRECTORIES		= true;

		private const string	ROOT_NODE_NAME		= "Complete Network";

		private const int	ICON_DONTKNOW			= 0;
		private const int	ICON_COMPUTER			= 1;
		private const int	ICON_CABINET			= 2;
		private const int	ICON_PARTITION			= 3;
		private const int	ICON_FLOPPY				= 4;
		private const int	ICON_CD					= 5;
		private const int	ICON_NETWORKFOLDER		= 6;
		private const int	ICON_OPENFOLDER			= 7;
		private const int	ICON_CLOSEDFOLDER		= 8;
		private const int	ICON_FILE				= 9;
		private const int	ICON_SERVER				= 10;
		private const int	ICON_PRINTER			= 11;
		private const int	ICON_PLANNED			= 12;
		private const int	ICON_NETWORK			= 13;
		private const int	ICON_COMPLETENETWORK	= 14;
		private const int	ICON_PLEASEWAIT			= 15;
		private const int	ICON_IPC				= 16;
		private const int	ICON_FW				= 17;

		public  const string	TAG_ROOTNODE		= "ROO";
		public  const string	TAG_SERVER			= "SRV";
		public  const string	TAG_COMPUTER		= "CMP";
		public  const string	TAG_FILE			= "FIL";
		public	const string	TAG_DIRECTORY		= "DIR";
		public	const string	TAG_DOMAIN			= "DOM";
		public	const string	TAG_SHARE			= "SHA";
		public  const string    TAG_WAIT			= "WAI";
		public  const string	TAG_FLOPPY			= "FLO";
		public  const string	TAG_DRIVE			= "DRV";
		public  const string	TAG_PRINTER			= "PRN";
		public	const string	TAG_IPC				= "IPC";
		public	const string	TAG_DONTKNOW		= "???";
		public const string TAG_ADMIN = "ADM";
		public const string TAG_SQL = "SQL";

		private const string	NAME_DONTKNOW		= "images\\DontKnow.ico";
		private const string	NAME_COMPUTER		= "images\\Computer.ico";
		private const string	NAME_CABINET		= "images\\Cabinet.ico";
		private const string	NAME_PARTITION		= "images\\Partition.ico";
		private const string	NAME_FLOPPY			= "images\\Floppy.ico";
		private const string	NAME_CD				= "images\\CD.ico";
		private const string	NAME_NETWORKFOLDER	= "images\\NetworkFolder.ico";
		private const string	NAME_OPENFOLDER		= "images\\OpenFolder.ico";
		private const string	NAME_CLOSEDFOLDER	= "images\\ClosedFolder.ico";
		private const string	NAME_FILE			= "images\\File.ico";
		private const string	NAME_SERVER			= "images\\Server.ico";
		private const string	NAME_PRINTER		= "images\\Printer.ico";
		private const string	NAME_PLANNED		= "images\\Planned.ico";
		private const string	NAME_NETWORK		= "images\\Network.ico";
		private const string	NAME_COMPLETENETWORK= "images\\CompleteNetwork.ico";
		private const string	NAME_PLEASEWAIT		= "images\\PleaseWait.ico";
		private const string NAME_IPC = "images\\IPC.ico";
		private const string NAME_IFW = "images\\LangProj.ico";

		private const int	ACTION_CLICK			= 0;
		private const int	ACTION_EXPAND			= 1;
		private const int	ACTION_COLAPSE			= 2;
		#endregion

		public event TreeViewEventHandler SelectedItemChanged;

		#region Variables

		private System.Windows.Forms.TreeView treeSourceFiles;
		private TreeNode	RootNode;
		private System.Windows.Forms.ImageList	imageList = new ImageList();
		public  bool		showFiles		= HIDE_FILES;
		public  bool		showDirs		= HIDE_DIRECTORIES;
		public  bool		showShares		= SHOW_SHARES;
		private ImageList m_imageList;
		private IContainer components;
		public  bool		showHiddenShares= HIDE_HIDDENSHARES;
		public  struct NetSelectInfo
		{
			public	int		Type;
			public	string	Path;
		}
		#endregion

		#region Constructors and Destructors
		public NetworkSelect()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				;
			}
			base.Dispose( disposing );
		}
		#endregion

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NetworkSelect));
			this.treeSourceFiles = new System.Windows.Forms.TreeView();
			this.m_imageList = new System.Windows.Forms.ImageList(this.components);
			this.SuspendLayout();
			//
			// treeSourceFiles
			//
			this.treeSourceFiles.Dock = System.Windows.Forms.DockStyle.Fill;
			this.treeSourceFiles.ImageIndex = 0;
			this.treeSourceFiles.ImageList = this.m_imageList;
			this.treeSourceFiles.Location = new System.Drawing.Point(0, 0);
			this.treeSourceFiles.Name = "treeSourceFiles";
			this.treeSourceFiles.SelectedImageIndex = 0;
			this.treeSourceFiles.Size = new System.Drawing.Size(504, 344);
			this.treeSourceFiles.TabIndex = 2;
			this.treeSourceFiles.AfterCheck += new System.Windows.Forms.TreeViewEventHandler(this.treeSourceFiles_AfterCheck);
			this.treeSourceFiles.AfterCollapse += new System.Windows.Forms.TreeViewEventHandler(this.treeSourceFiles_AfterCollapse);
			this.treeSourceFiles.BeforeCollapse += new System.Windows.Forms.TreeViewCancelEventHandler(this.treeSourceFiles_BeforeCollapse);
			this.treeSourceFiles.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeSourceFiles_AfterSelect);
			this.treeSourceFiles.AfterExpand += new System.Windows.Forms.TreeViewEventHandler(this.treeSourceFiles_AfterExpand);
			//
			// m_imageList
			//
			this.m_imageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("m_imageList.ImageStream")));
			this.m_imageList.TransparentColor = System.Drawing.Color.Transparent;
			this.m_imageList.Images.SetKeyName(0, "DontKnow.ico");
			this.m_imageList.Images.SetKeyName(1, "Computer.ico");
			this.m_imageList.Images.SetKeyName(2, "Cabinet.ico");
			this.m_imageList.Images.SetKeyName(3, "Partition.ico");
			this.m_imageList.Images.SetKeyName(4, "Floppy.ico");
			this.m_imageList.Images.SetKeyName(5, "CD.ico");
			this.m_imageList.Images.SetKeyName(6, "NetworkFolder.ico");
			this.m_imageList.Images.SetKeyName(7, "OpenFolder.ico");
			this.m_imageList.Images.SetKeyName(8, "ClosedFolder.ico");
			this.m_imageList.Images.SetKeyName(9, "File.ico");
			this.m_imageList.Images.SetKeyName(10, "Server.ico");
			this.m_imageList.Images.SetKeyName(11, "Printer.ico");
			this.m_imageList.Images.SetKeyName(12, "Planned.ico");
			this.m_imageList.Images.SetKeyName(13, "Network.ico");
			this.m_imageList.Images.SetKeyName(14, "CompleteNetwork.ico");
			this.m_imageList.Images.SetKeyName(15, "PleaseWait.ico");
			this.m_imageList.Images.SetKeyName(16, "IPC.ico");
			this.m_imageList.Images.SetKeyName(17, "LangProj.ico");
			//
			// NetworkSelect
			//
			this.Controls.Add(this.treeSourceFiles);
			this.Name = "NetworkSelect";
			this.Size = new System.Drawing.Size(504, 344);
			this.ResumeLayout(false);

		}
		#endregion

		#region Public Methods


		private void SeekThroughDomains( bool ShowCheckboxes, bool ShowShares,	bool ShowHiddenShares,
			bool ShowDirectories, bool ShowFiles)
		{
			treeSourceFiles.CheckBoxes = ShowCheckboxes;
			showShares = ShowShares;
			showHiddenShares = ShowHiddenShares;
			showDirs = ShowDirectories;
			showFiles = ShowFiles;
			ClearMotherNode();
			CreateMotherNode();
			GoDownTheDrain(RootNode);
		}

		public void SeekThroughDomains()
		{
			SeekThroughDomains(false, false, false, false, false);
		}

		public string GetCurrentDomain()
		{
			string retVal;
			TreeNode keepNode = treeSourceFiles.SelectedNode;
			while ((treeSourceFiles.SelectedNode.Tag.ToString().Substring(0,3) != TAG_DOMAIN) &&
				(treeSourceFiles.SelectedNode.Tag.ToString().Substring(0,3) != TAG_ROOTNODE))
				treeSourceFiles.SelectedNode = treeSourceFiles.SelectedNode.Parent;
			if (treeSourceFiles.SelectedNode.Tag.ToString().Substring(0,3) == TAG_ROOTNODE)
				retVal =  "";
			else
				retVal =  treeSourceFiles.SelectedNode.Tag.ToString().Substring(3);
			treeSourceFiles.SelectedNode = keepNode;
			return (retVal);
		}


		public string GetCurrentComputer()
		{
			string retVal;
			TreeNode keepNode = treeSourceFiles.SelectedNode;
			while ((treeSourceFiles.SelectedNode.Tag.ToString().Substring(0,3) != TAG_SERVER) &&
				(treeSourceFiles.SelectedNode.Tag.ToString().Substring(0,3) != TAG_COMPUTER) &&
				(treeSourceFiles.SelectedNode.Tag.ToString().Substring(0,3) != TAG_ROOTNODE) )
				treeSourceFiles.SelectedNode = treeSourceFiles.SelectedNode.Parent;
			if (treeSourceFiles.SelectedNode.Tag.ToString().Substring(0,3) == TAG_ROOTNODE)
				retVal =  "";
			else
				retVal =  treeSourceFiles.SelectedNode.Tag.ToString().Substring(3);
			treeSourceFiles.SelectedNode = keepNode;
			return (retVal);
		}


		public string GetCurrentShare()
		{
			string retVal;
			TreeNode keepNode = treeSourceFiles.SelectedNode;
			while ((treeSourceFiles.SelectedNode.Tag.ToString().Substring(0,3) != TAG_SHARE) &&
				(treeSourceFiles.SelectedNode.Tag.ToString().Substring(0,3) != TAG_IPC) &&
				(treeSourceFiles.SelectedNode.Tag.ToString().Substring(0,3) != TAG_DRIVE) &&
				(treeSourceFiles.SelectedNode.Tag.ToString().Substring(0,3) != TAG_PRINTER) &&
				(treeSourceFiles.SelectedNode.Tag.ToString().Substring(0,3) != TAG_ADMIN) &&
				(treeSourceFiles.SelectedNode.Tag.ToString().Substring(0,3) != TAG_ROOTNODE))
				treeSourceFiles.SelectedNode = treeSourceFiles.SelectedNode.Parent;
			if (treeSourceFiles.SelectedNode.Tag.ToString().Substring(0,3) == TAG_ROOTNODE)
				retVal = "";
			else
				retVal =  treeSourceFiles.SelectedNode.Tag.ToString().Substring(3);
			treeSourceFiles.SelectedNode = keepNode;
			return (retVal);
		}


		public string GetCurrentDirectory()
		{
			string retVal;
			TreeNode keepNode = treeSourceFiles.SelectedNode;
			while ((treeSourceFiles.SelectedNode.Tag.ToString().Substring(0,3) != TAG_DIRECTORY) &&
				(treeSourceFiles.SelectedNode.Tag.ToString().Substring(0,3) != TAG_ROOTNODE))
				treeSourceFiles.SelectedNode = treeSourceFiles.SelectedNode.Parent;
			if (treeSourceFiles.SelectedNode.Tag.ToString().Substring(0,3) == TAG_ROOTNODE)
				retVal = "";
			else
				retVal = treeSourceFiles.SelectedNode.Tag.ToString().Substring(3);
			treeSourceFiles.SelectedNode = keepNode;
			return (retVal);
		}


		public string GetCurrentFile()
		{
			string retVal;
			TreeNode keepNode = treeSourceFiles.SelectedNode;
			while ((treeSourceFiles.SelectedNode.Tag.ToString().Substring(0,3) != TAG_FILE) &&
				(treeSourceFiles.SelectedNode.Tag.ToString().Substring(0,3) != TAG_ROOTNODE) )
				treeSourceFiles.SelectedNode = treeSourceFiles.SelectedNode.Parent;
			if (treeSourceFiles.SelectedNode.Tag.ToString().Substring(0,3) == TAG_ROOTNODE)
				retVal = "";
			else
				retVal = treeSourceFiles.SelectedNode.Tag.ToString().Substring(3);
			treeSourceFiles.SelectedNode = keepNode;
			return (retVal);
		}


		public string GetFullName()
		{
			TreeNode walkingNode = treeSourceFiles.SelectedNode;
			string retVal = walkingNode.Text;
			while (GetTag(walkingNode)!=TAG_DOMAIN)
			{
				retVal = walkingNode.Text + "\\" + retVal;
				walkingNode = walkingNode.Parent;
			}
			return("\\\\" + retVal);
		}


		#endregion

		#region Private Methods

		private void ClearMotherNode()
		{
			if (treeSourceFiles.Nodes.Count != 0)
			{
				treeSourceFiles.SelectedNode = treeSourceFiles.SelectedNode.FirstNode;
				foreach(TreeNode t in treeSourceFiles.Nodes)
					t.Remove();
			}
		}

		private void CreateMotherNode()
		{
			RootNode = new TreeNode();
#if !FULLSearch
			// Search all computers.
			RootNode.Text = ROOT_NODE_NAME;
			RootNode.Tag = TAG_ROOTNODE + ROOT_NODE_NAME;
			RootNode.SelectedImageIndex = ICON_COMPLETENETWORK;
			RootNode.ImageIndex = ICON_COMPLETENETWORK;
#else
			// Only use the local host.
			RootNode.Text = SystemInformation.ComputerName;
			RootNode.Tag = TAG_COMPUTER + SystemInformation.ComputerName;
			RootNode.SelectedImageIndex = ICON_COMPUTER;
			RootNode.ImageIndex = ICON_COMPUTER;
#endif
			treeSourceFiles.Nodes.Add(RootNode);
			treeSourceFiles.SelectedNode = RootNode;
		}


		private void SetPleaseWait(TreeNode baseNode)
		{
			baseNode.SelectedImageIndex = ICON_PLEASEWAIT;
			baseNode.ImageIndex			= ICON_PLEASEWAIT;
			baseNode.Text		= "Working hard for you, please wait...";
			treeSourceFiles.Refresh();
		}


		private void ClearPleaseWait(TreeNode oldNode)
		{
			SelectProperIcon(oldNode, ACTION_EXPAND);
			oldNode.Text = oldNode.Tag.ToString().Substring(3);
		}


		private void treeSourceFiles_AfterCheck(object sender, System.Windows.Forms.TreeViewEventArgs e)
		{
			foreach(TreeNode anyNode in e.Node.Nodes)
				anyNode.Checked = e.Node.Checked;
		}


		private void treeSourceFiles_AfterCollapse(object sender, System.Windows.Forms.TreeViewEventArgs e)
		{
			SelectProperIcon(e.Node,ACTION_COLAPSE);
		}


		private void treeSourceFiles_AfterExpand(object sender, System.Windows.Forms.TreeViewEventArgs e)
		{
			TreeNode finalSelectedNode = e.Node;
			TreeNode	currentNode;
			// set the first directory below our current location
			treeSourceFiles.SelectedNode = e.Node.FirstNode;
			while(treeSourceFiles.SelectedNode != null) // .NextNode returns null after last node found
			{
				currentNode = treeSourceFiles.SelectedNode;
				SelectProperIcon(treeSourceFiles.SelectedNode,ACTION_EXPAND);
				GoDownTheDrain(treeSourceFiles.SelectedNode); // Go get all subdivisions
				// make sure we move down in the tree so that next node gets the real next node
				treeSourceFiles.SelectedNode = currentNode.NextNode;
				// Remove any DOM and CMP nodes that have no child nodes.
				if (currentNode.Nodes.Count == 0)
				{
					if (currentNode.Tag.ToString().StartsWith("DOM"))
					{
						currentNode.Parent.Nodes.Remove(currentNode);
					}
					else if (currentNode.Tag.ToString().StartsWith("CMP"))
					{
						if (currentNode.Parent.Nodes.Count == 1)
						{
							finalSelectedNode = currentNode.Parent.Parent;
							currentNode.Parent.Parent.Nodes.Remove(currentNode.Parent);
						}
						else
						{
							currentNode.Parent.Nodes.Remove(currentNode);
						}
					}
				}
			}
			// don't forget to set our current node back to the root of this branch where we started
			// we don't want to loose our place
			treeSourceFiles.SelectedNode = finalSelectedNode;
		}


		private void SelectProperIcon(TreeNode someNode, int action)
		{
			// someNode is a treenode
			// action = ACTION_CLICK if clicked upon
			//          ACTION_EXPAND if node expanded
			//          ACTION_COLAPSE if node colapsed
			string theTag = someNode.Tag.ToString().PadRight(3).Substring(0,3);
			string theExtension = someNode.Tag.ToString().PadLeft(3);
			theExtension = theExtension.Substring(theExtension.Length-3,3).ToUpperInvariant();
			switch (theTag)
			{
				case TAG_SQL:
					someNode.SelectedImageIndex = ICON_FW;
					someNode.ImageIndex = ICON_FW;
					break;
				case TAG_ROOTNODE:
					someNode.SelectedImageIndex = ICON_COMPLETENETWORK;
					someNode.ImageIndex = ICON_COMPLETENETWORK;
					break;
				case TAG_FLOPPY:
					someNode.SelectedImageIndex = ICON_FLOPPY;
					someNode.ImageIndex = ICON_FLOPPY;
					break;
				case TAG_DRIVE:
					someNode.SelectedImageIndex = ICON_PARTITION;
					someNode.ImageIndex = ICON_PARTITION;
					break;
				case TAG_ADMIN:
					someNode.SelectedImageIndex = ICON_PARTITION;
					someNode.ImageIndex = ICON_PARTITION;
					break;
				case TAG_SERVER:
					someNode.SelectedImageIndex = ICON_SERVER;
					someNode.ImageIndex = ICON_SERVER;
					break;
				case TAG_COMPUTER:
					someNode.SelectedImageIndex = ICON_COMPUTER;
					someNode.ImageIndex = ICON_COMPUTER;
					break;
				case TAG_SHARE:
					someNode.SelectedImageIndex = ICON_NETWORKFOLDER;
					someNode.ImageIndex = ICON_NETWORKFOLDER;
					break;
				case TAG_PRINTER:
					someNode.SelectedImageIndex = ICON_PRINTER;
					someNode.ImageIndex = ICON_PRINTER;
					break;
				case TAG_IPC:
					someNode.SelectedImageIndex = ICON_IPC;
					someNode.ImageIndex = ICON_IPC;
					break;
				case TAG_WAIT:
					someNode.SelectedImageIndex = ICON_PLEASEWAIT;
					someNode.ImageIndex = ICON_PLEASEWAIT;
					break;
				case TAG_DOMAIN:
					someNode.SelectedImageIndex = ICON_NETWORK;
					someNode.ImageIndex = ICON_NETWORK;
					break;
				case TAG_DIRECTORY:
					if (action == ACTION_COLAPSE)
					{
						someNode.SelectedImageIndex = ICON_CLOSEDFOLDER;
						someNode.ImageIndex = ICON_CLOSEDFOLDER;
					}
					else
					{
						someNode.SelectedImageIndex = ICON_OPENFOLDER;
						someNode.ImageIndex = ICON_OPENFOLDER;
					}
					break;
				case TAG_FILE:
					if ( (theExtension == "ZIP") |
						(theExtension == "CAB") |
						(theExtension == "TAR") |
						(theExtension == "LZH") |
						(theExtension == "ARJ") )
					{
						someNode.SelectedImageIndex = ICON_CABINET;
						someNode.ImageIndex = ICON_CABINET;
					}
					else
					{
						someNode.SelectedImageIndex = ICON_FILE;
						someNode.ImageIndex = ICON_FILE;
					}
					break;
				default:
					someNode.SelectedImageIndex = ICON_DONTKNOW;
					someNode.ImageIndex = ICON_DONTKNOW;
					break;
			}
		}


		private void GoDownTheDrain(TreeNode anyNode)
		{
			CompEnum cEnum;

			Cursor.Current = Cursors.WaitCursor;
			string theTag = anyNode.Tag.ToString().PadRight(3).Substring(0,3);
			try
			{
				switch (theTag)
				{
					case TAG_ROOTNODE:
						SetPleaseWait(anyNode);
						cEnum = new CompEnum(CompEnum.ServerType.SV_TYPE_DOMAIN_ENUM,null);
						ClearPleaseWait(anyNode);
						treeSourceFiles.SelectedNode = anyNode;
						for (int i=0;i<cEnum.Length;i++)
							NodeInsert(TAG_DOMAIN,cEnum[i].Name,ICON_NETWORK);
						treeSourceFiles.SelectedNode = anyNode;
						break;
					case TAG_DOMAIN:
						// Only look for machines that have a SQL server running
						SetPleaseWait(anyNode);
						cEnum = new CompEnum((uint)CompEnum.ServerType.SV_TYPE_SQLSERVER,
							anyNode.Tag.ToString().Substring(3));
						ClearPleaseWait(anyNode);
						treeSourceFiles.SelectedNode = anyNode;
						for (int i=0;i<cEnum.Length;i++)
							NodeInsert(TAG_COMPUTER,cEnum[i].Name,ICON_COMPUTER);
						treeSourceFiles.SelectedNode = anyNode.Parent;
						break;
					case TAG_SERVER:
					case TAG_COMPUTER:
						SqlConnection con = null;
						try
						{
							SqlConnectionStringBuilder bldr = new SqlConnectionStringBuilder();
							string computer = anyNode.Tag.ToString().Substring(3);
							if (computer == SystemInformation.ComputerName)
								computer = "(local)"; // A bit faster, presumably.
							bldr.DataSource = computer + @"\SILFW";
							bldr.InitialCatalog = "master";
							bldr.Password = "inscrutable";
							bldr.UserID = "sa";
							//bldr.IntegratedSecurity = false;
							//bldr.UserInstance = true;
							string conStr = bldr.ToString();
							con = new SqlConnection(conStr);
							con.Open();
							using (SqlCommand cmd = con.CreateCommand())
							{
								cmd.CommandText = "exec master..sp_GetFWDBs";
								//cmd.CommandType = CommandType.StoredProcedure; // It is, but that doesn't work
								cmd.CommandType = CommandType.Text; // Plain text works.
								cmd.CommandTimeout = 10; // Default is 30, but that makes it take forever.
								using (SqlDataReader reader = cmd.ExecuteReader())
								{
									while (reader.Read())
									{
										string dbName = reader.GetString(0);
										NodeInsert(TAG_SQL + @"^" + bldr.DataSource + "^", dbName, ICON_FW);
									}
								}
							}
						}
						catch
						{
							// Eat exceptions.
						}
						finally
						{
							if (con != null)
								con.Close();
						}
						break;
					case TAG_SQL:
						break;
					case TAG_DRIVE:
					case TAG_ADMIN:
					case TAG_SHARE:
					case TAG_DIRECTORY:
						if (showDirs)
						{
							string fullPath = GetFullPath(anyNode);
							SetPleaseWait(anyNode);
							try
							{
								DirectoryInfo dirInfo = new DirectoryInfo(fullPath);
								DirectoryInfo[] Flds = dirInfo.GetDirectories();
								for (int i=0; i < Flds.Length; i++)
									NodeInsert(TAG_DIRECTORY,Flds[i].Name,ICON_CLOSEDFOLDER);
								if (showFiles)
								{
									FileInfo[] Fils = dirInfo.GetFiles();
									for (int i=0; i < Fils.Length; i++)
										NodeInsert(TAG_FILE,Fils[i].Name,ICON_FILE);
								}
							}
							catch (Exception ex)
							{
								MessageBox.Show("Sorry, could not search for directories in this share. The " +
									"reason for the error was:\n\n" + ex.Message, "Can't go further",
									System.Windows.Forms.MessageBoxButtons.OK,
									System.Windows.Forms.MessageBoxIcon.Information);
							}
							ClearPleaseWait(anyNode);
						}
						break;
				}
				if (anyNode != RootNode)
					anyNode.Collapse();
				else
					anyNode.Expand();
			}
			catch (Exception ex)
			{
				MessageBox.Show("I'm sorry, it's impossible to go further down. The error that was " +
					"returned, is:\n\n" + ex.Message,"Can't go further down",
					System.Windows.Forms.MessageBoxButtons.OK,
					System.Windows.Forms.MessageBoxIcon.Warning);
			}
			Cursor.Current = Cursors.Default;
		}


		private string GetTag(TreeNode aNode)
		{
			return (aNode.Tag.ToString().Substring(0,3));
		}


		private string GetFullPath(TreeNode aNode)
		{
			TreeNode walkingNode = aNode;
			string retVal = "";
			while (GetTag(walkingNode)==TAG_DIRECTORY)
			{
				retVal = walkingNode.Text + "\\" + retVal;
				walkingNode = walkingNode.Parent;
			}
			return("\\\\" + walkingNode.Parent.Text + "\\" + walkingNode.Text + "\\" + retVal);
		}


		private void NodeInsert(string tag, string name, int iconNr)
		{
			if ( (!showHiddenShares) & (name.Substring(name.Length-1) == "$") )
				return;
			TreeNode newNode = new TreeNode();
			newNode.Tag = tag + name;
			newNode.Text = name;
			newNode.SelectedImageIndex = iconNr;
			newNode.ImageIndex = iconNr;
			newNode.Collapse();
			treeSourceFiles.SelectedNode.Nodes.Add(newNode);
		}


		private void KillNodesBelow(TreeNode startNode)
		{
			if (startNode != null)
			{
				for (int i = startNode.Nodes.Count; i-->0;)
				{
					KillNodesBelow(startNode.Nodes[0]);
					startNode.Nodes[0].Remove();
				}
			}
		}


		private void treeSourceFiles_BeforeCollapse(object sender, System.Windows.Forms.TreeViewCancelEventArgs e)
		{
			TreeNode currentNode;
			treeSourceFiles.SelectedNode = e.Node.FirstNode;
			while(treeSourceFiles.SelectedNode != null)
			{
				currentNode = treeSourceFiles.SelectedNode;
				KillNodesBelow(treeSourceFiles.SelectedNode);
				treeSourceFiles.SelectedNode=currentNode.NextNode;
			}
			treeSourceFiles.SelectedNode = e.Node;
		}

		private void treeSourceFiles_AfterSelect(object sender, TreeViewEventArgs e)
		{
			if (SelectedItemChanged != null)
				SelectedItemChanged.Invoke(this, e);
		}

		#endregion


	}
}
