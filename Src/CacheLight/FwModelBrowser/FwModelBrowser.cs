//#define USINGCPP

using System;
using System.Drawing;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.IO;
using System.Diagnostics;

using SIL.FieldWorks.CacheLight;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.FwModelBrowser
{
	/// <summary>
	/// Summary description for FwModelBrowser.
	/// </summary>
	public class FwModelBrowser : Form, IFWDisposable
	{
		private IFwMetaDataCache m_mdc;
#if USINGCPP
		private IOleDbEncap m_ode;
#endif

		private System.Windows.Forms.StatusBar m_statusBar;
		private SplitContainer m_splitContainer;
		private TreeView m_tvClasses;
		private ListView m_lvfields;
		private ColumnHeader m_chImplementor;
		private ColumnHeader m_chId;
		private ColumnHeader m_chName;
		private ColumnHeader m_chType;
		private ColumnHeader m_chSig;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:FwModelBrowser"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FwModelBrowser()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

#if USINGCPP
			m_ode = OleDbEncapClass.Create();
			m_ode.Init(SystemInformation.ComputerName + "\\SILFW", "TestLangProj", null,
				FwKernelLib.OdeLockTimeoutMode.koltReturnError,
				(int)FwKernelLib.OdeLockTimeoutValue.koltvFwDefault);
			m_mdc = FwMetaDataCacheClass.Create();
			m_mdc.Init(m_ode);
#else
			string modelDir = DirectoryFinder.FwSourceDirectory;
			modelDir = modelDir.Substring(0, modelDir.LastIndexOf('\\'));
			modelDir = Path.Combine(modelDir, @"Output\XMI");
			m_mdc = MetaDataCache.CreateMetaDataCache(Path.Combine(modelDir, "xmi2cellar3.xml"));
#endif

			uint clid = 0;
			string classname = m_mdc.GetClassName(clid);
			m_tvClasses.SuspendLayout();
			AddNode(m_tvClasses.Nodes, classname, clid);
			m_tvClasses.Nodes[0].Expand();
			m_tvClasses.ResumeLayout(false);
		}

		private void AddNode(TreeNodeCollection parentNodeCollection, string classname, uint clid)
		{
			bool isAbstract = m_mdc.GetAbstract(clid);
			string label = classname + ": (" + clid.ToString() + ")" + (isAbstract ? " abstract class" : "");
			TreeNode node = new TreeNode(label);
			node.Tag = clid;
			parentNodeCollection.Add(node);
			AddSubNodes(node.Nodes, clid);
		}

		private void AddSubNodes(TreeNodeCollection parentNodeCollection, uint superClassClid)
		{
			int directSubclassCount;
			m_mdc.GetDirectSubclasses(superClassClid, 0, out directSubclassCount, null);
			uint[] uIds;
			using (ArrayPtr clids = MarshalEx.ArrayToNative(directSubclassCount, typeof(uint)))
			{
				m_mdc.GetDirectSubclasses(superClassClid, directSubclassCount, out directSubclassCount, clids);
				uIds = (uint[])MarshalEx.NativeToArray(clids, directSubclassCount, typeof(uint));
			}
			SortedList<string, uint> list = new SortedList<string,uint>(uIds.Length);
			foreach (uint subclassClid in uIds)
			{
				string classname = m_mdc.GetClassName(subclassClid);
				list.Add(classname, subclassClid);
			}
			foreach (KeyValuePair<string, uint> kvp in list)
			{
				AddNode(parentNodeCollection, kvp.Key, kvp.Value);
			}
		}

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if (components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.m_statusBar = new System.Windows.Forms.StatusBar();
			this.m_splitContainer = new System.Windows.Forms.SplitContainer();
			this.m_tvClasses = new System.Windows.Forms.TreeView();
			this.m_lvfields = new System.Windows.Forms.ListView();
			this.m_chImplementor = new System.Windows.Forms.ColumnHeader();
			this.m_chId = new System.Windows.Forms.ColumnHeader();
			this.m_chName = new System.Windows.Forms.ColumnHeader();
			this.m_chType = new System.Windows.Forms.ColumnHeader();
			this.m_chSig = new System.Windows.Forms.ColumnHeader();
			this.m_splitContainer.Panel1.SuspendLayout();
			this.m_splitContainer.Panel2.SuspendLayout();
			this.m_splitContainer.SuspendLayout();
			this.SuspendLayout();
			//
			// m_statusBar
			//
			this.m_statusBar.Location = new System.Drawing.Point(0, 520);
			this.m_statusBar.Name = "m_statusBar";
			this.m_statusBar.Size = new System.Drawing.Size(768, 22);
			this.m_statusBar.TabIndex = 0;
			//
			// m_splitContainer
			//
			this.m_splitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_splitContainer.Location = new System.Drawing.Point(0, 0);
			this.m_splitContainer.Name = "m_splitContainer";
			//
			// m_splitContainer.Panel1
			//
			this.m_splitContainer.Panel1.Controls.Add(this.m_tvClasses);
			this.m_splitContainer.Panel1MinSize = 100;
			//
			// m_splitContainer.Panel2
			//
			this.m_splitContainer.Panel2.Controls.Add(this.m_lvfields);
			this.m_splitContainer.Size = new System.Drawing.Size(768, 520);
			this.m_splitContainer.SplitterDistance = 256;
			this.m_splitContainer.TabIndex = 2;
			//
			// m_tvClasses
			//
			this.m_tvClasses.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_tvClasses.Location = new System.Drawing.Point(0, 0);
			this.m_tvClasses.Name = "m_tvClasses";
			this.m_tvClasses.Size = new System.Drawing.Size(256, 520);
			this.m_tvClasses.TabIndex = 1;
			this.m_tvClasses.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.m_tvClasses_AfterSelect);
			//
			// m_lvfields
			//
			this.m_lvfields.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
			this.m_chImplementor,
			this.m_chId,
			this.m_chName,
			this.m_chType,
			this.m_chSig});
			this.m_lvfields.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_lvfields.FullRowSelect = true;
			this.m_lvfields.GridLines = true;
			this.m_lvfields.HideSelection = false;
			this.m_lvfields.Location = new System.Drawing.Point(0, 0);
			this.m_lvfields.MultiSelect = false;
			this.m_lvfields.Name = "m_lvfields";
			this.m_lvfields.Size = new System.Drawing.Size(508, 520);
			this.m_lvfields.TabIndex = 3;
			this.m_lvfields.UseCompatibleStateImageBehavior = false;
			this.m_lvfields.View = System.Windows.Forms.View.Details;
			//
			// m_chImplementor
			//
			this.m_chImplementor.Text = "Implementor";
			this.m_chImplementor.Width = 100;
			//
			// m_chId
			//
			this.m_chId.Text = "Id";
			this.m_chId.Width = 50;
			//
			// m_chName
			//
			this.m_chName.Text = "Name";
			this.m_chName.Width = 100;
			//
			// m_chType
			//
			this.m_chType.Text = "Type";
			this.m_chType.Width = 40;
			//
			// m_chSig
			//
			this.m_chSig.Text = "Signature";
			this.m_chSig.Width = 200;
			//
			// FwModelBrowser
			//
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(768, 542);
			this.Controls.Add(this.m_splitContainer);
			this.Controls.Add(this.m_statusBar);
			this.Name = "FwModelBrowser";
			this.Text = "FieldWorks Model Browser";
			this.m_splitContainer.Panel1.ResumeLayout(false);
			this.m_splitContainer.Panel2.ResumeLayout(false);
			this.m_splitContainer.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			Application.Run(new FwModelBrowser());
		}

		private void m_tvClasses_AfterSelect(object sender, System.Windows.Forms.TreeViewEventArgs e)
		{
			m_lvfields.Items.Clear();
			uint clid = (uint)m_tvClasses.SelectedNode.Tag;
			int countFoundFlids = m_mdc.GetFields(clid, true, (int)CellarModuleDefns.kgrfcptAll,
				0, null);
			int allFlidCount = countFoundFlids;
			uint[] uIds;
			using (ArrayPtr flids = MarshalEx.ArrayToNative(allFlidCount, typeof(uint)))
			{
				countFoundFlids = m_mdc.GetFields(clid, true, (int)CellarModuleDefns.kgrfcptAll,
					allFlidCount, flids);
				uIds = (uint[])MarshalEx.NativeToArray(flids, allFlidCount, typeof(uint));
			}
			m_lvfields.SuspendLayout();
			List<ListViewItem> list = new List<ListViewItem>();
			for (int i = uIds.Length - 1; i >= 0; --i)
			{
				uint flid = uIds[i];
				if (flid == 0)
					continue; // Keep looking for suitable flids lower in the array.

				string className = m_mdc.GetOwnClsName(flid);
				ListViewItem lvi = new ListViewItem(className); //m_lvfields.Items.Add(className);
				list.Add(lvi);
				// flid
				lvi.SubItems.Add(flid.ToString());
				// field name
				string fieldname = m_mdc.GetFieldName(flid);
				lvi.SubItems.Add(fieldname);
				int flidType = m_mdc.GetFieldType(flid);
				string type = "Not recognized";
				string signature = "Not recognized";
				uint dstClid;
				switch (flidType)
				{
					// Basic data types.
					case (int)CellarModuleDefns.kcptBoolean:
						type = "Basic";
						signature = "Boolean";
						break;
					case (int)CellarModuleDefns.kcptInteger:
						type = "Basic";
						signature = "Integer";
						break;
					case (int)CellarModuleDefns.kcptNumeric:
						type = "Basic";
						signature = "Numeric";
						break;
					case (int)CellarModuleDefns.kcptFloat:
						type = "Basic";
						signature = "Float";
						break;
					case (int)CellarModuleDefns.kcptTime:
						type = "Basic";
						signature = "Time";
						break;
					case (int)CellarModuleDefns.kcptGuid:
						type = "Basic";
						signature = "Guid";
						break;
					case (int)CellarModuleDefns.kcptImage:
						type = "Basic";
						signature = "Image";
						break;
					case (int)CellarModuleDefns.kcptGenDate:
						type = "Basic";
						signature = "GenDate";
						break;
					case (int)CellarModuleDefns.kcptBinary:
						type = "Basic";
						signature = "Binary";
						break;
					case (int)CellarModuleDefns.kcptString:
						type = "Basic";
						signature = "String";
						break;
					case (int)CellarModuleDefns.kcptBigString:
						type = "Basic";
						signature = "String (big)";
						break;
					case (int)CellarModuleDefns.kcptMultiString:
						type = "Basic";
						signature = "MultiString";
						break;
					case (int)CellarModuleDefns.kcptMultiBigString:
						type = "Basic";
						signature = "MultiString (big)";
						break;
					case (int)CellarModuleDefns.kcptUnicode:
						type = "Basic";
						signature = "Unicode";
						break;
					case (int)CellarModuleDefns.kcptBigUnicode:
						type = "Basic";
						signature = "Unicode (big)";
						break;
					case (int)CellarModuleDefns.kcptMultiUnicode:
						type = "Basic";
						signature = "MultiUnicode";
						break;
					case (int)CellarModuleDefns.kcptMultiBigUnicode:
						type = "Basic";
						signature = "MultiUnicode (big)";
						break;

					// CmObjects.
					case (int)CellarModuleDefns.kcptOwningAtom:
						type = "OA";
						dstClid = m_mdc.GetDstClsId(flid);
						signature = m_mdc.GetClassName(dstClid);
						break;
					case (int)CellarModuleDefns.kcptReferenceAtom:
						type = "RA";
						dstClid = m_mdc.GetDstClsId(flid);
						signature = m_mdc.GetClassName(dstClid);
						break;
					case (int)CellarModuleDefns.kcptOwningCollection:
						type = "OC";
						dstClid = m_mdc.GetDstClsId(flid);
						signature = m_mdc.GetClassName(dstClid);
						break;
					case (int)CellarModuleDefns.kcptReferenceCollection:
						type = "RC";
						dstClid = m_mdc.GetDstClsId(flid);
						signature = m_mdc.GetClassName(dstClid);
						break;
					case (int)CellarModuleDefns.kcptOwningSequence:
						type = "OS";
						dstClid = m_mdc.GetDstClsId(flid);
						signature = m_mdc.GetClassName(dstClid);
						break;
					case (int)CellarModuleDefns.kcptReferenceSequence:
						type = "RS";
						dstClid = m_mdc.GetDstClsId(flid);
						signature = m_mdc.GetClassName(dstClid);
						break;
				}
				// Type
				lvi.SubItems.Add(type);
				// Signature
				lvi.SubItems.Add(signature);
			}
			m_lvfields.Items.AddRange(list.ToArray());
			m_lvfields.ResumeLayout(true);
		}
	}
}
