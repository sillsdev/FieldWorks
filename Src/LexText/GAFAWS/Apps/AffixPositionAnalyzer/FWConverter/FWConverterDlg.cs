using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Data.SqlClient;

using TIData.NetworkSelect;

namespace SIL.WordWorks.GAFAWS.FWConverter
{
	public partial class FWConverterDlg : Form
	{
		private string m_catInfo = null;

		public FWConverterDlg()
		{
			InitializeComponent();

			m_source.SeekThroughDomains();
			m_source.SelectedItemChanged += new TIData.NetworkSelect.TreeViewEventHandler(m_source_SelectedItemChanged);
		}

		public string CatInfo
		{
			get { return m_catInfo; }
		}

		public bool IncludeSubcategories
		{
			get { return m_cbIncludeSubcategories.Enabled && m_cbIncludeSubcategories.Checked; }
		}

		void m_source_SelectedItemChanged(object sender, TreeViewEventArgs e)
		{
			TreeNode selNode = e.Node;
			string tag = e.Node.Tag as string;
			m_tvPOS.Nodes.Clear();
			m_catInfo = null;
			m_btnOk.Enabled = false;
			if (tag.StartsWith("SQL"))
			{
				SqlConnection con = null;
				try
				{
					string[] parts = tag.Split('^');
					SqlConnectionStringBuilder bldr = new SqlConnectionStringBuilder();
					bldr.DataSource = parts[1];
					bldr.InitialCatalog = parts[2];
					bldr.Password = "inscrutable";
					bldr.UserID = "sa";
					//bldr.UserInstance = true;
					string conStr = bldr.ToString();
					con = new SqlConnection(conStr);
					con.Open();
					using (SqlCommand cmd = con.CreateCommand())
					{
						cmd.CommandText = "DECLARE @catListId INT\n" +
							"SELECT @catListId=catList.Id\n" +
							"FROM CmObject lp\n" +
							"JOIN CmObject catList ON catList.Class$ = 8 AND catList.Owner$ = lp.Id AND catList.OwnFlid$ = 6001005\n" +
							"WHERE lp.Class$ = 6001\n" +
							"SELECT catId.Id, catId.Owner$, catId.OwnOrd$, catId.Level, catName.Ws, catName.Txt\n" +
							"FROM fnGetOwnedIds(@catListId, 8008, 7004) catId\n" +
							"JOIN CmPossibility_Name catName ON catName.Obj = catId.Id\n" +
							"ORDER BY Level, OwnOrd$";
						cmd.CommandType = CommandType.Text;
						using (SqlDataReader reader = cmd.ExecuteReader())
						{
							Dictionary<int, TreeNode> nodes = new Dictionary<int, TreeNode>();
							int prevCatId = 0;
							m_tvPOS.BeginUpdate();
							while (reader.Read())
							{
								int catId = reader.GetInt32(0);
								int catOwnerId = reader.GetInt32(1);
								int catOwnOrd = reader.GetInt32(2);
								int catLevel = reader.GetInt32(3);
								int catWS = reader.GetInt32(4);
								string catName = reader.GetString(5);

								if (prevCatId != catId)
								{
									TreeNode newNode = new TreeNode(catName);
									newNode.Tag = catId.ToString() + "^" + conStr;
									TreeNodeCollection tnCol = null;
									if (nodes.ContainsKey(catOwnerId))
									{
										// Put node in as child of owner.
										tnCol = nodes[catOwnerId].Nodes;
									}
									else
									{
										// Put node in at the top.
										tnCol = m_tvPOS.Nodes;
									}
									tnCol.Add(newNode);
									nodes.Add(catId, newNode);
									prevCatId = catId;
								}
							}
							m_tvPOS.EndUpdate();
						}
					}
				}
				finally
				{
					if (con != null)
						con.Close();
				}
			}
		}

		private void m_tvPOS_AfterSelect(object sender, TreeViewEventArgs e)
		{
			m_catInfo = (string)e.Node.Tag;
			m_btnOk.Enabled = (m_catInfo != null);
			m_cbIncludeSubcategories.Enabled = e.Node.Nodes.Count > 0;
		}
	}
}