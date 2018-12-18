// Copyright (c) 2003-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Controls.XMLViews;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.DomainServices;
using SIL.Xml;

namespace LanguageExplorer.Areas.Lexicon.Tools.Edit
{
	/// <summary>
	/// This is a class that can be used as the rootsite of a RecordDocView, to make it a
	/// RecordDocXmlView.
	/// </summary>
	internal class XmlDocItemView : XmlView, IChangeRootObject
	{
		private string m_configObjectName;

		#region implemention of IChangeRootObject

		public void SetRoot(int hvo)
		{
			if (m_hvoRoot == hvo)
			{
				return; // OnRecordNavigation is often called repeatedly wit the same HVO, we don't need to recompute every time.
			}
			m_hvoRoot = hvo;
			RootBox?.SetRootObject(m_hvoRoot, m_xmlVc, 1, m_styleSheet);
			// If the root box doesn't exist yet, the right root will be used in MakeRoot.
		}
		#endregion

		internal XmlDocItemView(int hvoRoot, XElement xnSpec, string sLayout) :
			base(hvoRoot, sLayout, XmlUtils.GetOptionalBooleanAttributeValue(xnSpec, "editable", true))
		{
			if (m_xnSpec == null)
			{
				m_xnSpec = xnSpec;
			}
		}

		public override void MakeRoot()
		{
			base.MakeRoot();
			m_xmlVc.IdentifySource = true; // We need this to know our context for the context menu!
		}

		// Context menu exists just for one invocation (until idle).
		private ContextMenuStrip m_contextMenu;

		/// <summary>
		/// Provides a context menu so we can configure parts of the dictionary preview.
		/// </summary>
		protected override bool DoContextMenu(IVwSelection sel, Point pt, Rectangle rcSrcRoot, Rectangle rcDstRoot)
		{
			int hvo, tag, ihvo, cpropPrevious;
			IVwPropertyStore propStore;
			sel.PropInfo(false, 0, out hvo, out tag, out ihvo, out cpropPrevious, out propStore);
			string nodePath = null;
			if (propStore != null)
			{
				nodePath = propStore.get_StringProperty((int)FwTextPropType.ktptBulNumTxtBef);
			}
			if (string.IsNullOrEmpty(nodePath))
			{
				if (sel.SelType == VwSelType.kstPicture)
				{
					return true;
				}
				// may be a literal string, where we can get it from the string itself.
				ITsString tss;
				int ich, ws;
				bool fAssocPrev;
				sel.TextSelInfo(false, out tss, out ich, out fAssocPrev, out hvo, out tag, out ws);
				nodePath = tss.get_Properties(0).GetStrPropValue((int)FwTextPropType.ktptBulNumTxtBef);
			}
			if (m_configObjectName == null)
			{
				m_configObjectName = StringTable.Table.LocalizeAttributeValue(XmlUtils.GetOptionalAttributeValue(m_xnSpec, "configureObjectName", null));
			}
			var label = string.IsNullOrEmpty(nodePath) ? string.Format(AreaResources.ksConfigure, m_configObjectName) : string.Format(AreaResources.ksConfigureIn, nodePath.Split(':')[3], m_configObjectName);
			m_contextMenu = new ContextMenuStrip();
			var item = new ToolStripMenuItem(label);
			m_contextMenu.Items.Add(item);
			item.Click += RunConfigureDialogAt;
			item.Tag = nodePath;
			m_contextMenu.Show(this, pt);
			m_contextMenu.Closed += m_contextMenu_Closed;
			return true;
		}

		private void RunConfigureDialogAt(object sender, EventArgs e)
		{
			var item = (ToolStripMenuItem)sender;
			var nodePath = (string)item.Tag;
			RunConfigureDialog(nodePath ?? string.Empty);
		}

		private void RunConfigureDialog(string nodePath)
		{
			using (var dlg = new XmlDocConfigureDlg())
			{
				var mainWindow = PropertyTable.GetValue<IFwMainWnd>(FwUtils.window);
				// If this is optional and defaults to DictionaryPublicationLayout,
				// it messes up our Dictionary when we make something else configurable (like Classified Dictionary).
				var sProp = XmlUtils.GetOptionalAttributeValue(m_xnSpec, "layoutProperty");
				Debug.Assert(sProp != null, "When making a view configurable you need to put a 'layoutProperty' in the XML configuration.");
				dlg.SetConfigDlgInfo(m_xnSpec, Cache, (LcmStyleSheet)StyleSheet, mainWindow, PropertyTable, Publisher, sProp);
				if (nodePath != null)
				{
					dlg.SetActiveNode(nodePath);
				}
				if (dlg.ShowDialog(this) == DialogResult.OK)
				{
					// Configuration may well have changed. Reset XML tables and redraw.
					var sNewLayout = PropertyTable.GetValue<string>(sProp);
					ResetTables(sNewLayout);
				}
				if (dlg.MasterRefreshRequired)
				{
					mainWindow.RefreshAllViews();
				}
			}
		}

		private void m_contextMenu_Closed(object sender, ToolStripDropDownClosedEventArgs e)
		{
			Application.Idle += DisposeContextMenu;
		}

		void DisposeContextMenu(object sender, EventArgs e)
		{
#if RANDYTODO_TEST_Application_Idle
// TODO: Remove when finished sorting out idle issues.
Debug.WriteLine($"Start: Application.Idle run at: '{DateTime.Now:HH:mm:ss.ffff}': on '{GetType().Name}'.");
#endif
			Application.Idle -= DisposeContextMenu;
			if (m_contextMenu != null)
			{
				m_contextMenu.Items[0].Click -= RunConfigureDialogAt;
				m_contextMenu.Items[0].Dispose();
				m_contextMenu.Closed -= m_contextMenu_Closed;
				m_contextMenu.Dispose();
				m_contextMenu = null;
			}
#if RANDYTODO_TEST_Application_Idle
// TODO: Remove when finished sorting out idle issues.
Debug.WriteLine($"End: Application.Idle run at: '{DateTime.Now:HH:mm:ss.ffff}': on '{GetType().Name}'.");
#endif
		}

		protected override void OnHandleCreated(EventArgs e)
		{
			// Must do this BEFORE base.OnHandleCreated, which will otherwise create the root box
			// with no stylesheet.
			if (StyleSheet == null)
			{
				SetupStylesheet();
			}
			base.OnHandleCreated(e);
		}

		private void SetupStylesheet()
		{
			StyleSheet = FwUtils.StyleSheetFromPropertyTable(PropertyTable);
		}
	}
}