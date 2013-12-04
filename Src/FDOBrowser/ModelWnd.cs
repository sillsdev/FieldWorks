using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FdoUi;
using SIL.Utils;
using WeifenLuo.WinFormsUI.Docking;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;
using XCore;

namespace FDOBrowser
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class ModelWnd : DockContent
	{
		private readonly Stack<ICmObject> m_backFDO = new Stack<ICmObject>();
		private readonly Stack<ICmObject> m_forwardFDO = new Stack<ICmObject>();
		//private ICmObject m_currentFDO;
		private ToolStripStatusLabel m_statuslabel = null;
		private readonly FdoCache m_cache;
		private readonly IFwMetaDataCacheManaged m_mdc;
		private int m_sortCol = 0;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ModelWnd"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ModelWnd()
		{
			InitializeComponent();

			m_tvModel.Font = SystemFonts.MenuFont;
			m_lvModel.Font = SystemFonts.MenuFont;

			// Add model browsing cache (no data, just model browsing).
			m_cache = FdoCache.CreateCacheWithNoLangProj(new BrowserProjectId(FDOBackendProviderType.kMemoryOnly, null), "en", new SilentFdoUI(this));
			m_mdc = (IFwMetaDataCacheManaged)m_cache.MainCacheAccessor.MetaDataCache;
			PopulateModelTree();

			if (Properties.Settings.Default.ModelWndSplitterLocation > 0)
				m_splitter.SplitterDistance = Properties.Settings.Default.ModelWndSplitterLocation;

			if (Properties.Settings.Default.ModelWndCol0Width > 0)
				m_hdrImplementor.Width = Properties.Settings.Default.ModelWndCol0Width;

			if (Properties.Settings.Default.ModelWndCol1Width > 0)
				m_hdrId.Width = Properties.Settings.Default.ModelWndCol1Width;

			if (Properties.Settings.Default.ModelWndCol2Width > 0)
				m_hdrName.Width = Properties.Settings.Default.ModelWndCol2Width;

			if (Properties.Settings.Default.ModelWndCol3Width > 0)
				m_hdrType.Width = Properties.Settings.Default.ModelWndCol3Width;

			if (Properties.Settings.Default.ModelWndCol4Width > 0)
				m_hdrSig.Width = Properties.Settings.Default.ModelWndCol4Width;

			m_hdrImplementor.Tag = true;
			m_hdrId.Tag = true;
			m_hdrName.Tag = true;
			m_hdrSig.Tag = true;
			m_hdrType.Tag = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ModelWnd"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ModelWnd(ToolStripStatusLabel statuslabel) : this()
		{
			m_statuslabel = statuslabel;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Form.FormClosing"/> event.
		/// </summary>
		/// ------------------------------------------------------------------------------------
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Populates the model tree.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void PopulateModelTree()
		{
			int clid = 0;
			string classname = m_mdc.GetClassName(clid);
			m_tvModel.SuspendLayout();
			AddNode(m_tvModel.Nodes, classname, clid);
			m_tvModel.Nodes[0].Expand();
			m_tvModel.ResumeLayout(false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the node.
		/// </summary>
		/// <param name="parentNodeCollection">The parent node collection.</param>
		/// <param name="classname">The classname.</param>
		/// <param name="clid">The clid.</param>
		/// ------------------------------------------------------------------------------------
		private void AddNode(TreeNodeCollection parentNodeCollection, string classname, int clid)
		{
			bool isAbstract = m_mdc.GetAbstract(clid);
			string label = classname + ": (" + clid + ")" + (isAbstract ? " abstract class" : "");
			TreeNode node = new TreeNode(label) {Tag = clid};

			bool fAdded = false;
			for (int i = 0; i < parentNodeCollection.Count; i++)
			{
				if (node.Text.CompareTo(parentNodeCollection[i].Text) < 0)
				{
					parentNodeCollection.Insert(i, node);
					fAdded = true;
					break;
				}
			}

			if (!fAdded)
				parentNodeCollection.Add(node);

			AddSubNodes(node.Nodes, clid);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the sub nodes.
		/// </summary>
		/// <param name="parentNodes">The parent node collection.</param>
		/// <param name="superClassId">The super class clid.</param>
		/// ------------------------------------------------------------------------------------
		private void AddSubNodes(TreeNodeCollection parentNodes, int superClassId)
		{
			foreach (var subclassClid in m_mdc.GetDirectSubclasses(superClassId))
				AddNode(parentNodes, m_mdc.GetClassName(subclassClid), subclassClid);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the AfterSelect event of the m_tvModel control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.Windows.Forms.TreeViewEventArgs"/> instance
		/// containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void m_tvModel_AfterSelect(object sender, TreeViewEventArgs e)
		{
			AfterSelectMethod();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Refresh the lv (list view) control based on the tv (tree view) control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void AfterSelectMethod()
		{
			string holdType = "", holdSig = "";
			int clid = (int)m_tvModel.SelectedNode.Tag;

			// Get flids.
			int[] uFlids = m_mdc.GetFields(clid, true, (int)CellarPropertyTypeFilter.All);
			List<ListViewItem> list = new List<ListViewItem>();

			for (int i = uFlids.Length - 1; i >= 0; --i)
			{
				int flid = uFlids[i];
				if (flid == 0)
					continue; // Keep looking for suitable flids lower in the array.

				else
				{
					if (FDOBrowserForm.m_virtualFlag == false && (flid >= 20000000 && flid < 30000000))
						continue;
					else
					{
						string className = m_mdc.GetOwnClsName(flid);
						ListViewItem lvi = new ListViewItem(className);
						list.Add(lvi);
						// flid
						lvi.SubItems.Add(flid.ToString());
						// field name
						string fieldname = m_mdc.GetFieldName(flid);
						lvi.SubItems.Add(fieldname);
						int flidType = m_mdc.GetFieldType(flid);
						string type = "Not recognized";
						string signature = "Not recognized";
						int dstClid;
						switch (flidType)
						{
								// Basic data types.
							case (int) CellarPropertyType.Boolean:
								type = "Basic";
								signature = "Boolean";
								break;
							case (int) CellarPropertyType.Integer:
								type = "Basic";
								signature = "Integer";
								break;
							case (int) CellarPropertyType.Numeric:
								type = "Basic";
								signature = "Numeric";
								break;
							case (int) CellarPropertyType.Float:
								type = "Basic";
								signature = "Float";
								break;
							case (int) CellarPropertyType.Time:
								type = "Basic";
								signature = "Time";
								break;
							case (int) CellarPropertyType.Guid:
								type = "Basic";
								signature = "Guid";
								break;
							case (int) CellarPropertyType.Image:
								type = "Basic";
								signature = "Image";
								break;
							case (int) CellarPropertyType.GenDate:
								type = "Basic";
								signature = "GenDate";
								break;
							case (int) CellarPropertyType.Binary:
								type = "Basic";
								signature = "Binary";
								break;
							case (int) CellarPropertyType.String:
								type = "Basic";
								signature = "String";
								break;
							case (int) CellarPropertyType.MultiString:
								type = "Basic";
								signature = "MultiString";
								break;
							case (int) CellarPropertyType.Unicode:
								type = "Basic";
								signature = "Unicode";
								break;
							case (int) CellarPropertyType.MultiUnicode:
								type = "Basic";
								signature = "MultiUnicode";
								break;

								// CmObjects.
							case (int) CellarPropertyType.OwningAtomic:
								type = "OA";
								dstClid = m_mdc.GetDstClsId(flid);
								signature = m_mdc.GetClassName(dstClid);
								break;
							case (int) CellarPropertyType.ReferenceAtomic:
								type = "RA";
								dstClid = m_mdc.GetDstClsId(flid);
								signature = m_mdc.GetClassName(dstClid);
								break;
							case (int) CellarPropertyType.OwningCollection:
								type = "OC";
								dstClid = m_mdc.GetDstClsId(flid);
								signature = m_mdc.GetClassName(dstClid);
								break;
							case (int) CellarPropertyType.ReferenceCollection:
								type = "RC";
								dstClid = m_mdc.GetDstClsId(flid);
								signature = m_mdc.GetClassName(dstClid);
								break;
							case (int) CellarPropertyType.OwningSequence:
								type = "OS";
								dstClid = m_mdc.GetDstClsId(flid);
								signature = m_mdc.GetClassName(dstClid);
								break;
							case (int) CellarPropertyType.ReferenceSequence:
								type = "RS";
								dstClid = m_mdc.GetDstClsId(flid);
								signature = m_mdc.GetClassName(dstClid);
								break;
						}

						if (flid >= 20000000 && flid < 30000000)
							type += " (Virt)";
						else if (flid > 10000000)
							type += " (BackRef)";

						lvi.SubItems.Add(type);
						lvi.SubItems.Add(signature);
					}
				}
			}

			// Add custom fields

			if (FDOBrowserForm.CFields != null && FDOBrowserForm.CFields.Count > 0)
			{
				foreach (CustomFields cf in FDOBrowserForm.CFields)
				{
					if (clid == cf.ClassID)
					{
						string clasName = m_mdc.GetClassName(cf.ClassID);
						ListViewItem lv = new ListViewItem(clasName);
						list.Add(lv);
						// classname
						//lv.SubItems.Add(clasName);
						// flid
						lv.SubItems.Add(cf.FieldID.ToString());
						// field name
						lv.SubItems.Add(cf.Name);
						// Type
						switch (cf.Type)
						{
							case "ICmPossibility":
								holdType = "Custom - RA";
								break;
							case "FdoReferenceCollection<ICmPossibility>":
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
							case "FdoReferenceCollection<ICmPossibility>":
								holdSig = "CmPossibility";
								break;
							case "IStText":
								holdSig = "StText";
								break;
							default:
								MessageBox.Show(String.Format("Type not recognized for signature for custom model fields.  Type: {0}", cf.Type));
								break;
						}
						lv.SubItems.Add(holdSig);

					}
				}
			}
			LoadListView(list);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the ColumnClick event of the m_lvModel control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void m_lvModel_ColumnClick(object sender, ColumnClickEventArgs e)
		{
			// If the column clicked is the same as the last column clicked,
			// then reverse the sort order.
			if (m_sortCol == e.Column)
				m_lvModel.Columns[e.Column].Tag = !((bool)m_lvModel.Columns[e.Column].Tag);

			m_sortCol = e.Column;
			LoadListView(new List<ListViewItem>(m_lvModel.Items.Cast<ListViewItem>()));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Loads the list view, sorted by the current sort column.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void LoadListView(List<ListViewItem> list)
		{
			m_lvModel.SuspendLayout();
			m_lvModel.Items.Clear();
			bool sortAscending = (bool)m_lvModel.Columns[m_sortCol].Tag;

			list.Sort((x, y) => sortAscending ?
				x.SubItems[m_sortCol].Text.CompareTo(y.SubItems[m_sortCol].Text) :
				y.SubItems[m_sortCol].Text.CompareTo(x.SubItems[m_sortCol].Text));

			m_lvModel.Items.AddRange(list.ToArray());
			m_lvModel.ResumeLayout(true);
		}
	}
}
