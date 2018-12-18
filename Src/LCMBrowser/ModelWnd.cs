// Copyright (c) 2009-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Infrastructure;
using WeifenLuo.WinFormsUI.Docking;

namespace LCMBrowser
{
	/// <summary />
	public partial class ModelWnd : DockContent
	{
		private LcmCache m_cache;
		private readonly IFwMetaDataCacheManaged m_mdc;
		private int m_sortCol;

		/// <summary />
		public ModelWnd()
		{
			InitializeComponent();

			m_tvModel.Font = SystemFonts.MenuFont;
			m_lvModel.Font = SystemFonts.MenuFont;

			// Add model browsing cache (no data, just model browsing).
			m_cache = LcmCache.CreateCacheWithNoLangProj(new BrowserProjectId(BackendProviderType.kMemoryOnly, null), "en",
				new SilentLcmUI(this), FwDirectoryFinder.LcmDirectories, new LcmSettings());
			m_mdc = m_cache.GetManagedMetaDataCache();
			PopulateModelTree();

			if (Properties.Settings.Default.ModelWndSplitterLocation > 0)
			{
				m_splitter.SplitterDistance = Properties.Settings.Default.ModelWndSplitterLocation;
			}
			if (Properties.Settings.Default.ModelWndCol0Width > 0)
			{
				m_hdrImplementor.Width = Properties.Settings.Default.ModelWndCol0Width;
			}
			if (Properties.Settings.Default.ModelWndCol1Width > 0)
			{
				m_hdrId.Width = Properties.Settings.Default.ModelWndCol1Width;
			}
			if (Properties.Settings.Default.ModelWndCol2Width > 0)
			{
				m_hdrName.Width = Properties.Settings.Default.ModelWndCol2Width;
			}
			if (Properties.Settings.Default.ModelWndCol3Width > 0)
			{
				m_hdrType.Width = Properties.Settings.Default.ModelWndCol3Width;
			}
			if (Properties.Settings.Default.ModelWndCol4Width > 0)
			{
				m_hdrSig.Width = Properties.Settings.Default.ModelWndCol4Width;
			}
			m_hdrImplementor.Tag = true;
			m_hdrId.Tag = true;
			m_hdrName.Tag = true;
			m_hdrSig.Tag = true;
			m_hdrType.Tag = true;
		}

		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Form.FormClosing"/> event.
		/// </summary>
		protected override void OnFormClosing(FormClosingEventArgs e)
		{
			Properties.Settings.Default.ModelWndSplitterLocation = m_splitter.SplitterDistance;
			Properties.Settings.Default.ModelWndCol0Width = m_hdrImplementor.Width;
			Properties.Settings.Default.ModelWndCol1Width = m_hdrId.Width;
			Properties.Settings.Default.ModelWndCol2Width = m_hdrName.Width;
			Properties.Settings.Default.ModelWndCol3Width = m_hdrType.Width;
			Properties.Settings.Default.ModelWndCol4Width = m_hdrSig.Width;

			base.OnFormClosing(e);
		}

		/// <summary>
		/// Populates the model tree.
		/// </summary>
		private void PopulateModelTree()
		{
			const int clid = 0;
			m_tvModel.SuspendLayout();
			AddNode(m_tvModel.Nodes, m_mdc.GetClassName(clid), clid);
			m_tvModel.Nodes[0].Expand();
			m_tvModel.ResumeLayout(false);
		}

		/// <summary>
		/// Adds the node.
		/// </summary>
		private void AddNode(TreeNodeCollection parentNodeCollection, string classname, int clid)
		{
			var isAbstract = m_mdc.GetAbstract(clid);
			var label = $"{classname}: ({clid})" + (isAbstract ? " abstract class" : string.Empty);
			var node = new TreeNode(label)
			{
				Tag = clid
			};

			var fAdded = false;
			for (var i = 0; i < parentNodeCollection.Count; i++)
			{
				if (node.Text.CompareTo(parentNodeCollection[i].Text) < 0)
				{
					parentNodeCollection.Insert(i, node);
					fAdded = true;
					break;
				}
			}
			if (!fAdded)
			{
				parentNodeCollection.Add(node);
			}
			AddSubNodes(node.Nodes, clid);
		}

		/// <summary>
		/// Adds the sub nodes.
		/// </summary>
		private void AddSubNodes(TreeNodeCollection parentNodes, int superClassId)
		{
			foreach (var subclassClid in m_mdc.GetDirectSubclasses(superClassId))
			{
				AddNode(parentNodes, m_mdc.GetClassName(subclassClid), subclassClid);
			}
		}

		/// <summary>
		/// Handles the AfterSelect event of the m_tvModel control.
		/// </summary>
		private void m_tvModel_AfterSelect(object sender, TreeViewEventArgs e)
		{
			AfterSelectMethod();
		}

		/// <summary>
		/// Refresh the lv (list view) control based on the tv (tree view) control.
		/// </summary>
		public void AfterSelectMethod()
		{
			var clid = (int)m_tvModel.SelectedNode.Tag;
			// Get flids.
			var fields = m_mdc.GetFields(clid, true, (int)CellarPropertyTypeFilter.All);
			var list = new List<ListViewItem>();

			for (var i = fields.Length - 1; i >= 0; --i)
			{
				var flid = fields[i];
				if (flid == 0 || (LCMBrowserForm.m_virtualFlag == false && (flid >= 20000000 && flid < 30000000)))
				{
					// Keep looking for suitable flids lower in the array.
					continue;
				}
				var className = m_mdc.GetOwnClsName(flid);
				var lvi = new ListViewItem(className);
				list.Add(lvi);
				// flid
				lvi.SubItems.Add(flid.ToString());
				// field name
				lvi.SubItems.Add(m_mdc.GetFieldName(flid));
				var flidType = m_mdc.GetFieldType(flid);
				var type = "Not recognized";
				var signature = "Not recognized";
				int dstClid;
				switch (flidType)
				{
					// Basic data types.
					case (int)CellarPropertyType.Boolean:
						type = "Basic";
						signature = "Boolean";
						break;
					case (int)CellarPropertyType.Integer:
						type = "Basic";
						signature = "Integer";
						break;
					case (int)CellarPropertyType.Numeric:
						type = "Basic";
						signature = "Numeric";
						break;
					case (int)CellarPropertyType.Float:
						type = "Basic";
						signature = "Float";
						break;
					case (int)CellarPropertyType.Time:
						type = "Basic";
						signature = "Time";
						break;
					case (int)CellarPropertyType.Guid:
						type = "Basic";
						signature = "Guid";
						break;
					case (int)CellarPropertyType.Image:
						type = "Basic";
						signature = "Image";
						break;
					case (int)CellarPropertyType.GenDate:
						type = "Basic";
						signature = "GenDate";
						break;
					case (int)CellarPropertyType.Binary:
						type = "Basic";
						signature = "Binary";
						break;
					case (int)CellarPropertyType.String:
						type = "Basic";
						signature = "String";
						break;
					case (int)CellarPropertyType.MultiString:
						type = "Basic";
						signature = "MultiString";
						break;
					case (int)CellarPropertyType.Unicode:
						type = "Basic";
						signature = "Unicode";
						break;
					case (int)CellarPropertyType.MultiUnicode:
						type = "Basic";
						signature = "MultiUnicode";
						break;

					// CmObjects.
					case (int)CellarPropertyType.OwningAtomic:
						type = "OA";
						dstClid = m_mdc.GetDstClsId(flid);
						signature = m_mdc.GetClassName(dstClid);
						break;
					case (int)CellarPropertyType.ReferenceAtomic:
						type = "RA";
						dstClid = m_mdc.GetDstClsId(flid);
						signature = m_mdc.GetClassName(dstClid);
						break;
					case (int)CellarPropertyType.OwningCollection:
						type = "OC";
						dstClid = m_mdc.GetDstClsId(flid);
						signature = m_mdc.GetClassName(dstClid);
						break;
					case (int)CellarPropertyType.ReferenceCollection:
						type = "RC";
						dstClid = m_mdc.GetDstClsId(flid);
						signature = m_mdc.GetClassName(dstClid);
						break;
					case (int)CellarPropertyType.OwningSequence:
						type = "OS";
						dstClid = m_mdc.GetDstClsId(flid);
						signature = m_mdc.GetClassName(dstClid);
						break;
					case (int)CellarPropertyType.ReferenceSequence:
						type = "RS";
						dstClid = m_mdc.GetDstClsId(flid);
						signature = m_mdc.GetClassName(dstClid);
						break;
				}

				if (flid >= 20000000 && flid < 30000000)
				{
					type += " (Virt)";
				}
				else if (flid > 10000000)
				{
					type += " (BackRef)";
				}

				lvi.SubItems.Add(type);
				lvi.SubItems.Add(signature);
			}

			// Add custom fields
			if (LCMBrowserForm.CFields != null && LCMBrowserForm.CFields.Count > 0)
			{
				foreach (var cf in LCMBrowserForm.CFields)
				{
					if (clid != cf.ClassID)
					{
						continue;
					}
					var clasName = m_mdc.GetClassName(cf.ClassID);
					var lv = new ListViewItem(clasName);
					list.Add(lv);
					// flid
					lv.SubItems.Add(cf.FieldID.ToString());
					// field name
					lv.SubItems.Add(cf.Name);
					// Type
					string holdType;
					switch (cf.Type)
					{
						case "ICmPossibility":
							holdType = "Custom - RA";
							break;
						case "LcmReferenceCollection<ICmPossibility>":
							holdType = "Custom - RC";
							break;
						case "IStText":
							holdType = "Custom - OA";
							break;
						default:
							holdType = "Custom";
							break;
					}
					lv.SubItems.Add(holdType);
					// Signature
					string holdSig;
					switch (cf.Type)
					{
						case "ITsString":
							holdSig = "String";
							break;
						case "System.Int32":
							holdSig = "Integer";
							break;
						case "SIL.FieldWorks.Common.FwUtils.GenDate":
							holdSig = "GenDate";
							break;
						case "ICmPossibility":
							holdSig = "CmPossibility";
							break;
						case "LcmReferenceCollection<ICmPossibility>":
							holdSig = "CmPossibility";
							break;
						case "IStText":
							holdSig = "StText";
							break;
						default:
							MessageBox.Show($@"Type not recognized for signature for custom model fields.  Type: {cf.Type}");
							holdSig = "Unknown";
							break;
					}
					lv.SubItems.Add(holdSig);
				}
			}
			LoadListView(list);
		}

		/// <summary>
		/// Handles the ColumnClick event of the m_lvModel control.
		/// </summary>
		private void m_lvModel_ColumnClick(object sender, ColumnClickEventArgs e)
		{
			// If the column clicked is the same as the last column clicked,
			// then reverse the sort order.
			if (m_sortCol == e.Column)
			{
				m_lvModel.Columns[e.Column].Tag = !((bool)m_lvModel.Columns[e.Column].Tag);
			}

			m_sortCol = e.Column;
			LoadListView(new List<ListViewItem>(m_lvModel.Items.Cast<ListViewItem>()));
		}

		/// <summary>
		/// Loads the list view, sorted by the current sort column.
		/// </summary>
		private void LoadListView(List<ListViewItem> list)
		{
			m_lvModel.SuspendLayout();
			m_lvModel.Items.Clear();
			var sortAscending = (bool)m_lvModel.Columns[m_sortCol].Tag;

			if (sortAscending)
			{
				list.Sort((x, y) => x.SubItems[m_sortCol].Text.CompareTo(y.SubItems[m_sortCol].Text));
			}
			else
			{
				list.Sort((x, y) => y.SubItems[m_sortCol].Text.CompareTo(x.SubItems[m_sortCol].Text));
			}

			m_lvModel.Items.AddRange(list.ToArray());
			m_lvModel.ResumeLayout(true);
		}
	}
}