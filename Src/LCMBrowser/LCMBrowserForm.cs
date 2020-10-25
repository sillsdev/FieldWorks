// Copyright (c) 2009-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using LanguageExplorer;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.DetailControls;
using LCMBrowser.Properties;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.FieldWorks.Resources;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Infrastructure;
using WeifenLuo.WinFormsUI.Docking;

namespace LCMBrowser
{
	/// <summary>
	/// LCMBrowserForm Class
	/// </summary>
	internal sealed partial class LCMBrowserForm : Form
	{
		#region Data members

		/// <summary>
		/// List of custom fields associated with the open project.
		/// </summary>
		internal static List<CustomFields> CFields = new List<CustomFields>();

		private const string kAppCaptionFmt = "{0} - {1}";

		/// <summary>
		/// Specifies whether you want to see all properties of a class (true)
		/// or exclude the virtual properties (false).
		/// </summary>
		internal static bool m_virtualFlag;

		/// <summary>
		/// Specifies whether you want to be able to update class properties here (true)
		/// or not (false).
		/// </summary>
		internal static bool m_updateFlag = true;
		/// <summary />
		private string m_appCaption;
		/// <summary />
		private string m_currOpenedProject;
		/// <summary />
		private readonly DockPanel m_dockPanel;
		/// <summary />
		private List<string> m_ruFiles;
		private InspectorWnd m_InspectorWnd;
		private LcmCache m_cache;
		private ILangProject m_lp;
		private ICmObjectRepository m_repoCmObject;
		private string m_fmtSelectPropsMenuText;
		private string m_fmtAddObjectMenuText;
		private ModelWnd m_modelWnd;
		private InspectorWnd m_langProjWnd;
		private InspectorWnd m_repositoryWnd;
		private int saveClid;
		private string FileName = string.Empty;
		private ToolStripTextBox m_tstxtGuidSrch;
		private ToolStripLabel m_tslblGuidSrch;
		private ISilDataAccessManaged m_silDataAccessManaged;

		#endregion Data members

		#region Construction

		/// <summary />
		internal LCMBrowserForm()
		{
			InitializeComponent();

			m_statuslabel.Text = string.Empty;
			m_sblblLoadTime.Text = string.Empty;
			m_appCaption = Text;

			m_dockPanel = new DockPanel
			{
				Dock = DockStyle.Fill,
				DefaultFloatWindowSize = new Size(600, 600)
			};
			m_dockPanel.ActiveDocumentChanged += m_dockPanel_ActiveDocumentChanged;
			m_dockPanel.ContentRemoved += DockPanelContentRemoved;
			m_dockPanel.ContentAdded += DockPanelContentAdded;
			Controls.Add(m_dockPanel);
			Controls.SetChildIndex(m_dockPanel, 0);
			m_dockPanel.BringToFront();

			m_ruFiles = new List<string>();
			for (var i = 1; i <= 9; i++)
			{
				m_ruFiles.Add(Settings.Default["RUFile" + i] as string);
			}
			BuildRecentlyUsedFilesMenus();
			m_fmtAddObjectMenuText = cmnuAddObject.Text;
		}

		#endregion Construction

		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Form.Load"/> event.
		/// </summary>
		protected override void OnLoad(EventArgs e)
		{
			var pt = Settings.Default.MainWndLocation;
			if (pt != Point.Empty)
			{
				Location = pt;
			}
			var sz = Settings.Default.MainWndSize;
			if (!sz.IsEmpty)
			{
				Size = sz;
			}

			base.OnLoad(e);

			OpenModelWindow();
			var showThem = Settings.Default.ShowCmObjectProperties;
			LCMClassList.ShowCmObjectProperties = showThem;
			m_tsbShowCmObjectProps.Checked = LCMClassList.ShowCmObjectProperties;
		}

		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Form.FormClosing"/> event.
		/// </summary>
		protected override void OnFormClosing(FormClosingEventArgs e)
		{
			base.OnFormClosing(e);

			var i = 1;
			foreach (var file in m_ruFiles)
			{
				Settings.Default["RUFile" + i++] = file;
			}
			Settings.Default.MainWndLocation = Location;
			Settings.Default.MainWndSize = Size;
			Settings.Default.Save();
			Settings.Default.ShowCmObjectProperties = m_tsbShowCmObjectProps.Checked;
			Settings.Default.Save();
		}

		/// <summary>
		/// Puts the specified file path at the top of recently used file list.
		/// </summary>
		private void PutFileAtTopOfRUFileList(string newfile)
		{
			// Check if it's already at the top of the stack.
			if (newfile == m_ruFiles[0])
			{
				return;
			}
			// Either remove the file from list or remove the last file from the list.
			var index = m_ruFiles.IndexOf(newfile);
			if (index >= 0)
			{
				m_ruFiles.RemoveAt(index);
			}
			else
			{
				m_ruFiles.RemoveAt(m_ruFiles.Count - 1);
			}
			// Insert the file at the top of the list and rebuild the menus.
			m_ruFiles.Insert(0, newfile);
			BuildRecentlyUsedFilesMenus();
		}

		/// <summary>
		/// Builds the recently used files menus.
		/// </summary>
		private void BuildRecentlyUsedFilesMenus()
		{
			// Remove old recently used file menu items.
			for (var i = 0; i <= 8; i++)
			{
				var index = mnuFile.DropDownItems.IndexOfKey("RUF" + i);
				if (index >= 0)
				{
					mnuFile.DropDownItems.RemoveAt(index);
				}
			}
			// Get the index where to add recently used file names.
			var insertIndex = mnuFile.DropDownItems.IndexOf(mnuFileSep1) + 1;
			// Add the recently used file names to the file menu.
			for (var i = 8; i >= 0; i--)
			{
				var file = m_ruFiles[i];
				if (!string.IsNullOrEmpty(file))
				{
					mnuFileSep1.Visible = true;
					var mnu = new ToolStripMenuItem
					{
						Name = "RUF" + i,
						Text = $"&{i + 1} {file}",
						Tag = file
					};
					mnu.Click += HandleRUFileMenuClick;
					mnuFile.DropDownItems.Insert(insertIndex, mnu);
				}
			}
		}

		/// <summary>
		/// Handles the user clicking on one of the recently used file menu items.
		/// </summary>
		private void HandleRUFileMenuClick(object sender, EventArgs l)
		{
			var mnu = (ToolStripMenuItem)sender;
			try
			{
				OpenFile(mnu.Tag as string);
			}
			catch (Exception e)
			{
				MessageBox.Show($"Exception caught: {e.Message} {mnu.Tag}. Open up Flex for this project to create the necessary writing systems.");
			}
		}

		/// <summary>
		/// Opens the specified file.
		/// </summary>
		private void OpenFile(string fileName)
		{
			if (fileName == null || !File.Exists(fileName) || m_currOpenedProject == fileName)
			{
				return;
			}
			// if we didn't clean up when we ran this before, do it now.
			if (m_cache != null && File.Exists(FileName + ".lock"))
			{
				m_cache.ServiceLocator.GetInstance<IActionHandler>().Commit();
				m_cache.Dispose();
				m_cache = null; // Don't try to use it again
			}
			// Save the filename so we can close it when we exit
			FileName = fileName;
			Cursor = Cursors.WaitCursor;
			try
			{
				var bepType = GetBEPTypeFromFileExtension(fileName);
				var isMemoryBEP = bepType == BackendProviderType.kMemoryOnly;

				var stopwatch = new Stopwatch();
				stopwatch.Start();

				// Init backend data provider
				// TODO: Get the correct ICU local for the user writing system

				var ui = new FwLcmUI(HelpTopicProviderBase.Instance, this);
				if (isMemoryBEP)
				{
					m_cache = LcmCache.CreateCacheWithNewBlankLangProj(new BrowserProjectId(bepType, null), "en", "en", "en", ui,
						FwDirectoryFinder.LcmDirectories, new LcmSettings());
				}
				else
				{
					using (var progressDlg = new ProgressDialogWithTask(this))
					{
						m_cache = LcmCache.CreateCacheFromExistingData(new BrowserProjectId(bepType, fileName), "en", ui, FwDirectoryFinder.LcmDirectories, new LcmSettings(), progressDlg);
					}
				}

				CFields = GetCustomFields(m_cache);
				m_lp = m_cache.LanguageProject;
				m_silDataAccessManaged = m_cache.GetManagedSilDataAccess();

				stopwatch.Stop();

				m_sblblLoadTime.Text = $"Load Time: {stopwatch.Elapsed}";

				// Close any windows from a previously opened project, if there is one.
				foreach (var dc in m_dockPanel.DocumentsToArray())
				{
					if (dc is InspectorWnd inspectorWnd)
					{
						inspectorWnd.Close();
					}
				}

				m_langProjWnd?.Close();
				m_langProjWnd = null;
				m_repositoryWnd = null;
				MakeAppCaption(m_lp.ShortName);
				OpenLangProjWindow();
				OpenRepositoryWindow();

				PutFileAtTopOfRUFileList(fileName);

				m_currOpenedProject = fileName;
			}
			finally
			{
				Cursor = Cursors.Default;
			}
		}

		/// <summary>
		/// Makes the application's caption.
		/// </summary>
		private void MakeAppCaption(string text)
		{
			Text = string.Format(kAppCaptionFmt, text, m_appCaption);
		}

		#region Dock Panel event handlers

		/// <summary>
		/// Handles the ActiveDocumentChanged event of the m_dockPanel control.
		/// </summary>
		private void m_dockPanel_ActiveDocumentChanged(object sender, EventArgs e)
		{
			var wnd = m_dockPanel.ActiveDocument as InspectorWnd;
			m_statuslabel.Text = (wnd == null ? string.Empty : $"{wnd.InspectorList.Count} Top Level Items");
		}

		/// <summary>
		/// Handles the ContentAdded event of the m_dockPanel control.
		/// </summary>
		private void DockPanelContentAdded(object sender, DockContentEventArgs e)
		{
			if (e.Content is InspectorWnd)
			{
				m_tsbShowCmObjectProps.Enabled = true;
			}
		}

		/// <summary>
		/// Handles the ContentRemoved event of the m_dockPanel control.
		/// </summary>
		private void DockPanelContentRemoved(object sender, DockContentEventArgs e)
		{
			if (m_dockPanel.DocumentsToArray().OfType<InspectorWnd>().Any())
			{
				return;
			}
			m_tsbShowCmObjectProps.Enabled = false;
		}

		#endregion

		#region Event handlers

		/// <summary>
		/// Handles the Click event of the openToolStripMenuItem control.
		/// </summary>
		private void HandleFileOpenClick(object sender, EventArgs e)
		{
			using (var dlg = new OpenFileDialog())
			{
				dlg.CheckFileExists = true;
				dlg.RestoreDirectory = true;
				dlg.Title = OpenFileDlgTitle;
				dlg.ValidateNames = true;
				dlg.Multiselect = false;
				dlg.Filter = OpenFileDlgFilter;
				if (dlg.ShowDialog(this) == DialogResult.OK)
				{
					OpenFile(dlg.FileName);
				}
			}
		}

		/// <summary>
		/// Gets the title for the open file dialog box.
		/// </summary>
		private static string OpenFileDlgTitle => "Open FieldWorks Language Project";

		/// <summary>
		/// Gets the file filter for the open file dialog box.
		/// </summary>
		private static string OpenFileDlgFilter => ResourceHelper.FileFilter(FileFilterType.FieldWorksProjectFiles);

		/// <summary>
		/// Handles the Click event of the mnuExit control.
		/// </summary>
		private void mnuExit_Click(object sender, EventArgs e)
		{
			Close();
		}

		/// <summary>
		/// Handles the DropDownOpening event of the mnuWindow control.
		/// </summary>
		private void mnuWindow_DropDownOpening(object sender, EventArgs e)
		{
			// Remove all the windows names at the end of the Windows menu list.
			var wndMenus = mnuWindow.DropDownItems;
			for (var i = wndMenus.Count - 1; wndMenus[i] != mnuWindowsSep; i--)
			{
				wndMenus[i].Click -= HandleWindowMenuItemClick;
				wndMenus.RemoveAt(i);
			}
			// Now add a menu item for each document in the dock panel.
			foreach (var dc in m_dockPanel.Documents)
			{
				var wnd = dc as DockContent;
				var mnu = new ToolStripMenuItem
				{
					Text = wnd.Text,
					Tag = wnd,
					Checked = m_dockPanel.ActiveContent == wnd
				};
				mnu.Click += HandleWindowMenuItemClick;
				mnuWindow.DropDownItems.Add(mnu);
			}

			mnuTileVertically.Enabled = (m_dockPanel.DocumentsCount > 1);
			mnuTileHorizontally.Enabled = (m_dockPanel.DocumentsCount > 1);
			mnuArrangeInline.Enabled = (m_dockPanel.DocumentsCount > 1);
		}

		/// <summary>
		/// Handles the window menu item click.
		/// </summary>
		private void HandleWindowMenuItemClick(object sender, EventArgs e)
		{
			var mnu = sender as ToolStripMenuItem;
			(mnu?.Tag as DockContent)?.Show(m_dockPanel);
		}

		#endregion Event handlers

		/// <summary>
		/// Creates the new inspector window for the specified object.
		/// </summary>
		private InspectorWnd ShowNewInspectorWindow(object obj, string text = null, string toolTipText = null)
		{
			var wnd = ShowNewInspectorWndOne();
			if (m_updateFlag)
			{
				if (text.Contains("LangProj"))
				{
					wnd.InspectorGrid.ReadOnly = false;
					wnd.InspectorGrid.CellValueChanged += LangProjInspectorGrid_CellValueChanged;
				}
				else if (text.Contains("Repositories"))
				{
					wnd.InspectorGrid.ReadOnly = false;
					wnd.InspectorGrid.CellValueChanged += RepositoryInspectorGrid_CellValueChanged;
				}
			}
			wnd = ShowNewInspectorWndTwo(obj, text, toolTipText, wnd);
			wnd.WillObjDisappearOnRefresh += HandleWillObjDisappearOnRefresh;
			return wnd;
		}

		/// <summary />
		public InspectorWnd ShowNewInspectorWndOne()
		{
			return new InspectorWnd();
		}

		/// <summary />
		internal InspectorWnd ShowNewInspectorWndTwo(object obj, string text, string toolTipText, InspectorWnd wnd)
		{
			wnd.Text = !string.IsNullOrEmpty(text) ? text : GetNewInspectorWndTitle(obj);
			wnd.ToolTipText = (string.IsNullOrEmpty(toolTipText) ? wnd.Text : toolTipText);
			wnd.SetTopLevelObject(obj, GetNewInspectorList());
			wnd.InspectorGrid.ContextMenuStrip = m_cmnuGrid;
			wnd.InspectorGrid.Enter += InspectorGrid_Enter;
			wnd.InspectorGrid.Leave += InspectorGrid_Leave;
			wnd.FormClosed += HandleWindowClosed;
			wnd.Show(m_dockPanel);
			return wnd;
		}

		/// <summary>
		/// Gets the title for a new inspector window being built for the specified object.
		/// </summary>
		private static string GetNewInspectorWndTitle(object obj)
		{
			switch (obj)
			{
				case null:
					return string.Empty;
				case ITsString tsString:
					return $"ITsString: '{tsString.Text}'";
			}
			var objString = obj.ToString();
			var title = objString;
			var typeName = obj.GetType().Name;
			if (!objString.StartsWith(typeName))
			{
				title = typeName;
				if (string.IsNullOrWhiteSpace(objString))
				{
					title += $": {objString}";
				}
			}
			return title;
		}

		/// <summary>
		/// Gets the new inspector list.
		/// </summary>
		private IInspectorList GetNewInspectorList()
		{
			return new LCModelInspectorList(m_cache);
		}

		/// <summary>
		/// Handles the Leave event of the InspectorGrid control.
		/// </summary>
		private void InspectorGrid_Leave(object sender, EventArgs e)
		{
			tsbShowObjInNewWnd.Enabled = false;
		}

		/// <summary>
		/// Handles the Enter event of the InspectorGrid control.
		/// </summary>
		private void InspectorGrid_Enter(object sender, EventArgs e)
		{
			tsbShowObjInNewWnd.Enabled = true;
		}

		/// <summary>
		/// Handles the inspector window closed.
		/// </summary>
		private void HandleWindowClosed(object sender, FormClosedEventArgs e)
		{
			if (sender is DockContent dockContent)
			{
				dockContent.FormClosed -= HandleWindowClosed;
			}
			if (sender is InspectorWnd asInspectorWnd)
			{
				asInspectorWnd.InspectorGrid.Enter -= InspectorGrid_Enter;
				asInspectorWnd.InspectorGrid.Leave -= InspectorGrid_Leave;
			}

			tsbShowObjInNewWnd.Enabled = false;

			if (sender is InspectorWnd inspectorWnd)
			{
				inspectorWnd.WillObjDisappearOnRefresh -= HandleWillObjDisappearOnRefresh;
			}
			if (sender == m_modelWnd)
			{
				m_modelWnd = null;
			}
			else if (sender == m_langProjWnd)
			{
				m_langProjWnd = null;
			}
			else if (sender == m_repositoryWnd)
			{
				m_repositoryWnd = null;
			}
		}

		/// <summary>
		/// Handles the Click event of the mnuTileVertically control.
		/// </summary>
		private void mnuTileVertically_Click(object sender, EventArgs e)
		{
			TileWindows(DockAlignment.Right);
		}

		/// <summary>
		/// Handles the Click event of the mnuTileHorizontally control.
		/// </summary>
		private void mnuTileHorizontally_Click(object sender, EventArgs e)
		{
			TileWindows(DockAlignment.Bottom);
		}

		/// <summary>
		/// Handles the Click event of the mnuArrangeInline control.
		/// </summary>
		private void mnuArrangeInline_Click(object sender, EventArgs e)
		{
			m_dockPanel.SuspendLayout();

			var currentWnd = m_dockPanel.ActiveDocument;
			var documents = m_dockPanel.DocumentsToArray();
			var wndAnchor = (DockContent)documents[0];
			for (var i = documents.Length - 1; i >= 0; i--)
			{
				var wnd = (DockContent)documents[i];
				wnd.DockTo(wndAnchor.Pane, DockStyle.Fill, 0);
			}

			((DockContent)currentWnd).Activate();
			m_dockPanel.ResumeLayout();
		}

		/// <summary>
		/// Tiles the windows.
		/// </summary>
		private void TileWindows(DockAlignment alignment)
		{
			m_dockPanel.SuspendLayout();

			var documents = m_dockPanel.DocumentsToArray();
			if (!(documents[0] is DockContent))
			{
				return;
			}
			var currentWnd = m_dockPanel.ActiveDocument;
			for (var i = documents.Length - 1; i > 0; i--)
			{
				var proportion = 1.0 / (i + 1);
				var wnd = documents[i] as DockContent;
				wnd?.Show(m_dockPanel.Panes[0], alignment, proportion);
			}

			((DockContent)currentWnd).Activate();
			m_dockPanel.ResumeLayout();
		}

		/// <summary>
		/// Handles the Click event of the m_tsbShowObjInNewWnd control.
		/// </summary>
		private void m_tsbShowObjInNewWnd_Click(object sender, EventArgs e)
		{
			var wnd = m_dockPanel.ActiveContent as InspectorWnd;
			var io = wnd?.CurrentInspectorObject;
			if (io?.Object == null)
			{
				return;
			}
			var text = io.DisplayName;
			if (text.StartsWith("[") && text.EndsWith("]"))
			{
				text = text.Trim('[', ']');
				if (int.TryParse(text, out _))
				{
					text = null;
				}
			}
			if (text != null && text != io.DisplayType)
			{
				text += (": " + io.DisplayType);
			}
			m_InspectorWnd = ShowNewInspectorWindow(io.Object, text);
		}

		/// <summary>
		/// Handles the Opening event of the grid's context menu.
		/// </summary>
		private void HandleOpenObjectInNewWindowContextMenuClick(object sender, CancelEventArgs e)
		{
			saveClid = 0;
			var wnd = m_dockPanel.ActiveContent as InspectorWnd;
			if (wnd == null)
			{
				return;
			}
			var io = wnd.CurrentInspectorObject;
			cmnuShowInNewWindow.Enabled = io?.Object != null;
			cmnuAddObject.Enabled = io != null && AddObjectFromHere(io);
			cmnuDeleteObject.Enabled = io?.Object is ICmObject;
			cmnuMoveObjectUp.Enabled = io != null && MoveObjectUpFromHere(io);
			cmnuMoveObjectDown.Enabled = io != null && MoveObjectDownFromHere(io);
			var type = io?.Object is ICmObject ? io.Object.GetType().Name : "???";
			if (io == null || !io.DisplayName.EndsWith("OS") && !io.DisplayName.EndsWith("OC") &&
				!io.DisplayName.EndsWith("OA") && !io.DisplayName.EndsWith("RS") &&
				!io.DisplayName.EndsWith("RC") && !io.DisplayName.EndsWith("RA"))
			{
				return;
			}
			var mdc = m_cache.ServiceLocator.GetInstance<IFwMetaDataCacheManaged>();
			var owner = io.ParentInspectorObject.Object as ICmObject ?? (ICmObject)io.ParentInspectorObject.OwningObject;
			var currentObj = io.Object as ICmObject ?? ((ICmObject)io.OwningObject);
			int flid;
			try
			{
				var work = StripOffTypeChars(io.DisplayName);
				flid = mdc.GetFieldId2(owner.ClassID, work, true);
			}
			catch
			{
				MessageBox.Show("No Flid for clicked line: " + io.DisplayValue);
				return;
			}

			var clid = m_cache.DomainDataByFlid.MetaDataCache.GetDstClsId(flid);
			if (io.DisplayName.EndsWith("RS") || io.DisplayName.EndsWith("RC") || io.DisplayName.EndsWith("RA"))
			{
				var dispFlag = io.DisplayName.EndsWith("RA") && io.Object != null;
				type = GetTypeForRefObjs(flid, clid, dispFlag, ref saveClid);
				cmnuAddObject.Text = $"Add New Reference of type {type}...";
				cmnuSelectProps.Text = string.Format(m_fmtSelectPropsMenuText, type);
			}
			else
			{
				switch (clid)
				{
					case 7: // cmPossibility
						if (PossOwnedByPossList(mdc, currentObj, ref clid))
						{
							type = mdc.GetClassName(clid);
						}
						break;
					case 15: // stPara
						if (StParaOwnedByScrBook(mdc, currentObj, ref clid))
						{
							type = mdc.GetClassName(clid);
						}
						break;
					default:
						try
						{
							type = mdc.GetClassName(clid);
						}
						catch
						{
							MessageBox.Show($"No class name for clid: {clid}");
							return;
						}
						break;
				}
				cmnuAddObject.Text = string.Format(m_fmtAddObjectMenuText, type);
				cmnuSelectProps.Text = string.Format(m_fmtSelectPropsMenuText, type);
			}
		}

		/// <summary>
		/// Get the possible classes for objects for the selected reference property.
		/// </summary>
		private string GetTypeForRefObjs(int flid, int sclid, bool dispFlag, ref int saveClid)
		{
			var list = new List<string>();
			var mdc = m_cache.GetManagedMetaDataCache();
			var clid = int.Parse(flid.ToString().Substring(0, flid.ToString().Length - 3));
			var type = mdc.GetClassName(sclid);
			switch (type)
			{
				case "CmObject":
					switch (mdc.GetClassName(clid) + "_" + mdc.GetFieldName(flid))
					{
						case "LexEntryRef_ComponentLexemes":
						case "LexEntryRef_PrimaryLexemes":
						case "LexReference_Targets":
							return "LexEntry or LexSense";
						case "Segment_Analyses":
							return "IAnalysis";
						// No work here yet since the following two areas are unimplemented so far.
						// For now they can fall into the default (per FWR-890)
						//case "MoPhonolRuleApp_Rule":
						//    return "PhSegmentRule";
						//case "PhPhonRuleFeat_Item":
						//    return "MoInflClass, FsSymFeatVal or CmPossibility";
						case "MoPhonolRuleApp_Rule":
						case "PhPhonRuleFeat_Item":
						case "StTxtPara_TextObjects":
						case "CmAnnotation_InstanceOf":
						case "CmBaseAnnotation_BeginObject":
						case "CmBaseAnnotation_EndObject":
						case "CmBaseAnnotation_OtherObjects":
						default:
							if (!dispFlag)
							{
								var classes = mdc.GetAllSubclasses(sclid);
								list.AddRange(classes.Where(cl => cl != sclid).Select(cl => mdc.GetClassName(cl)));
								using (var dlg1 = new RealListChooser("ClassName", list))
								{
									dlg1.ShowDialog(this);
									if (dlg1.DialogResult == DialogResult.Cancel)
									{
										break;
									}
									if (mdc.GetClassId(dlg1.ChosenClass) == 0)
									{
										MessageBox.Show($"No clid for selected class: {dlg1.ChosenClass}");
										break;
									}
									saveClid = mdc.GetClassId(dlg1.ChosenClass);
									return dlg1.ChosenClass; // get the clid for the selected class
								}
							}
							break;
					}   // switch for properties with a signature of CmObject
					break;
				case "CmPossibility":
					{
						switch ($"{mdc.GetClassName(clid)}_{mdc.GetFieldName(flid)}")
						{
							case "CmOverlay_PossItems":
							case "RnGenericRec_PhraseTags":
							case "ConstChartTag_Tag":
							case "ConstituentChartCellPart_Column":
							case "DsChart_Template":
							case "MoDerivAffMsa_AffixCategory":
							case "MoInflAffMsa_AffixCategory":
							case "CmPossibility_Status":
							case "RnGenericRec_Status":
							case "CmPossibility_Confidence":
							case "RnGenericRec_Confidence":
							case "CmPerson_Education":
							case "Text_Genres":
							case "CmPerson_Positions":
							case "CmPossibility_Restrictions":
							case "RnGenericRec_Restrictions":
							case "RnRoledPartic_Role":
							case "TextTag_Tag":
							case "RnGenericRec_TimeOfEvent":
							case "CmTranslation_Type":
							case "LexSense_DomainTypes":
							case "LexSense_SenseType":
							case "LexSense_Status":
							case "LexSense_UsageTypes":
							case "MoCompoundRule_ToProdRestrict":
							case "MoDerivAffMsa_FromProdRestrict":
							case "MoDerivAffMsa_ToProdRestrict":
							case "MoDerivStepMsa_ProdRestrict":
							case "MoInflAffMsa_FromProdRestrict":
							case "MoStemMsa_ProdRestrict":
							case "RnGenericRec_Type":
							case "ScrScriptureNote_Categories":
								return "CmPossibility";
						}  //cmPossibility fields Switch
					} //CmPossibilityLabel case
					break;
				default:
					return type;
			}  //classes switch
			return type;
		}

		/// <summary>
		/// Check to see if the current object is owned by CmPossibilityList.
		/// </summary>
		private static bool PossOwnedByPossList(IFwMetaDataCacheManaged mdc, ICmObject owner, ref int clid)
		{
			do
			{
				if (owner.ClassID == mdc.GetClassId("CmPossibilityList")) //if the owner is CmPossibilityList
				{
					if (!(owner is ICmPossibilityList holdposs))
					{
						MessageBox.Show("PossOwnedByPossList; holdposs is null");
						return false;
					}
					clid = holdposs.ItemClsid;
					return true;
				}
				owner = owner.Owner;
			} while (owner != null);

			return false;
		}

		/// <summary>
		/// Handles the Click event of the mnuOptions control.
		/// </summary>
		private void mnuOptions_Click(object sender, EventArgs e)
		{
			using (var dlg = new OptionsDlg(Settings.Default.ShadeColor))
			{
				dlg.ShadingEnabled = Settings.Default.UseShading;
				dlg.SelectedColor = Settings.Default.ShadeColor;

				if (dlg.ShowDialog(this) != DialogResult.OK)
				{
					return;
				}
				Settings.Default.UseShading = dlg.ShadingEnabled;
				var clrNew = Color.Empty;

				if (dlg.ShadingEnabled)
				{
					clrNew = dlg.SelectedColor;
					Settings.Default.ShadeColor = clrNew;
				}

				foreach (var dc in m_dockPanel.DocumentsToArray())
				{
					if (dc is InspectorWnd inspectorWnd)
					{
						inspectorWnd.InspectorGrid.ShadingColor = clrNew;
					}
				}
			}
		}

		/// <summary>
		/// Handles the Click event of the cmnuAddObject control.
		/// </summary>
		private void CmnuAddObjectClick(object sender, EventArgs e)
		{
			ICmPossibilityList holdposs;
			int oclid;
			var ord = 0;
			int idx;
			int flid;
			var io = (m_dockPanel.ActiveContent as InspectorWnd)?.CurrentInspectorObject;
			if (io == null)
			{
				return;
			}

			var type = io.DisplayName.Substring(io.DisplayName.Length - 2, 2);
			if (type != "RS" && type != "RC" && type != "RA")
			{
				switch (type)
				{
					case "OA":
						ord = -2;
						break;
					case "OC":
						ord = -1;
						break;
					case "OS":
						ord = 0;
						break;
					default:
						MessageBox.Show($"The parent of an added object must be OA, OC, or OS.  It is: '{type}'");
						break;
				}
			}
			var owner = io.ParentInspectorObject.Object as ICmObject ?? ((ICmObject)io.ParentInspectorObject.OwningObject);
			if (owner == null)
			{
				MessageBox.Show("owner for add is null");
				return;
			}
			var currentObj = io.Object as ICmObject ?? io.ParentInspectorObject.Object as ICmObject;
			if (currentObj == null)
			{
				MessageBox.Show("currentObj for add is null");
				return;
			}
			var mdc = m_cache.ServiceLocator.GetInstance<IFwMetaDataCacheManaged>();
			try
			{
				var work = StripOffTypeChars(io.DisplayName);
				flid = mdc.GetFieldId2(owner.ClassID, work, true);
			}
			catch
			{
				MessageBox.Show($"No Flid for clicked line: {io.DisplayValue}");
				return;
			}
			var clid = m_cache.MetaDataCacheAccessor.GetDstClsId(flid);
			if (type == "RS" || type == "RC" || type == "RA")
			{
				if (DisplayReferenceObjectsToAdd(flid, type, currentObj, out var refTarget))
				{
					if (refTarget != null)
					{
						switch (type)
						{
							case "RS":
							case "RC":
								var count = m_cache.DomainDataByFlid.get_VecSize(currentObj.Hvo, flid);
								NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
									m_cache.DomainDataByFlid.Replace(currentObj.Hvo, flid, count, count, new[] { refTarget.Hvo }, 1)
								);
								break;
							case "RA":
								NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
									m_cache.DomainDataByFlid.SetObjProp(currentObj.Hvo, flid, refTarget.Hvo)
								);
								break;
						}
					}
				}
			}
			else
			{
				// if the classname is abstract, we need to get concrete classes and let the user choose
				if (DisplayOwningObjectToAdd(mdc, currentObj, ref clid))
				{
					// Add a new owned class
					NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
					{
						try
						{
							m_cache.DomainDataByFlid.MakeNewObject(clid, currentObj.Hvo, flid, ord);
						}
						catch
						{
							MessageBox.Show($"The creation of class id {clid} failed.");
						}
					});
				}
			}

			//Refresh the display
			foreach (var dc in m_dockPanel.DocumentsToArray())
			{
				(dc as InspectorWnd)?.RefreshView("Add");
			}
		}

		/// <summary>
		/// Receives field.  Return field less OA, OC, OS, RA, RC, or RS if there.
		///  </summary>
		private static string StripOffTypeChars(string field)
		{
			return field.EndsWith("OS") || field.EndsWith("RS") || field.EndsWith("OC") || field.EndsWith("RC") || field.EndsWith("OA") || field.EndsWith("RA")
				? field.Substring(0, field.Length - 2)
				: field;
		}

		/// <summary>
		/// Handles getting the correct class (clid) to add for owning properties.
		/// </summary>
		private bool DisplayOwningObjectToAdd(IFwMetaDataCacheManaged mdc, ICmObject owner, ref int clid)
		{
			// if the chosen classname is abstract, we need to get concrete classes and let the user choose
			var list = new List<string>();
			if (!mdc.GetAbstract(clid))
			{
				return true;
			}
			if (clid == 15 && StParaOwnedByScrBook(mdc, owner, ref clid))   //stPara is abstact - see if it is a special condition
			{
				return true;
			}
			var clidSubs = mdc.GetAllSubclasses(clid);
			list.AddRange(clidSubs.Where(t => !mdc.GetAbstract(t)).Select(mdc.GetClassName));
			if (clidSubs.Length > 1)
			{
				using (var dlg = new RealListChooser("ClassName", list))
				{
					dlg.ShowDialog(this);
					if (dlg.DialogResult == DialogResult.Cancel)
					{
						return false;
					}
					clid = mdc.GetClassId(dlg.ChosenClass);  // get the clid for the selected class
					if (clid != 0)
					{
						return true;
					}
					MessageBox.Show($"No clid for selected class: {dlg.ChosenClass}");
					return false;
				}
			}
			clid = clidSubs[0];
			return true;
			// class is concrete
		}

		/// <summary>
		/// Handles getting the correct object for a reference property to add.
		/// </summary>
		private bool DisplayReferenceObjectsToAdd(int flid, string type, ICmObject currObject, out ICmObject refTarget)
		{
			var labels = new List<ObjectLabel>();
			var mdc = m_cache.ServiceLocator.GetInstance<IFwMetaDataCacheManaged>();
			var refObjs = new HashSet<int>();

			if (m_cache.MetaDataCacheAccessor != null)
			{
				var sclid = m_cache.MetaDataCacheAccessor.GetDstClsId(flid); //signature clid of field
				if (type != "RA")
				{
					int count;
					try
					{
						count = m_cache.DomainDataByFlid.get_VecSize(currObject.Hvo, flid);
					}
					catch
					{
						MessageBox.Show("Can't get count for reference sequence or collection.");
						goto Skip;
					}

					if (count > 0)
					{
						for (var i = 0; count > i; i++)
						{
							try
							{
								refObjs.Add(m_cache.DomainDataByFlid.get_VecItem(currObject.Hvo, flid, i));
							}
							catch {}
						}
					}
				}
				Skip:
				var desiredClasses = GetClassesForRefObjs(flid, sclid, out var possList);
				if (desiredClasses != null && possList == null)
				{
					if (refObjs.Count == 0)
					{
						var objects = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>()
							.AllInstances(sclid)
							.Where(obj => desiredClasses.Contains(obj.ClassID));

						labels.AddRange(objects.Select(obj => ObjectLabel.CreateObjectLabelOnly(m_cache, obj, "ObjectIdName", "best analysis")));
					}
					else
					{
						var objects = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>()
							.AllInstances(sclid)
							.Where(obj => desiredClasses.Contains(obj.ClassID) && !refObjs.Contains(obj.Hvo));
						labels.AddRange(objects.Select(obj => ObjectLabel.CreateObjectLabelOnly(m_cache, obj, "ObjectIdName", "best analysis")));
					}
					if (labels.Count == 0)
					{
						MessageBox.Show($"No objects exist within the selected classes.  Try again.  Selected classes are: {desiredClasses.Aggregate(string.Empty, (current, de) => current + mdc.GetClassName(de) + " ")}");
					}
					else
					{
						using (var dlg = new SimpleListChooser(m_cache, null, null, labels, null, "Object List", null))
						{
							var res = dlg.ShowDialog();
							if (res == DialogResult.Cancel)
							{
								refTarget = null;
								dlg.Close();
								return false;
							}
							if (dlg.ChosenOne != null)
							{
								refTarget = dlg.ChosenOne.Object;
								dlg.Close();
								return true;
							}
						}
					}
				}
				else
				{
					labels = (List<ObjectLabel>)ObjectLabel.CreateObjectLabels(m_cache, possList, "ObjectIdName", "best analysis");
					using (var dlg = new SimpleListChooser(m_cache, null, null, labels, null, "Object List", null))
					{
						var res = dlg.ShowDialog();
						if (res == DialogResult.Cancel)
						{
							refTarget = null;
							dlg.Close();
							return false;
						}
						if (dlg.ChosenOne != null)
						{
							refTarget = dlg.ChosenOne.Object;
							dlg.Close();
							return true;
						}
					}
				}
			}
			refTarget = null;
			return false;
		}

		/// <summary>
		/// Get the possible classes for objects for the selected reference property.
		/// </summary>
		private HashSet<int> GetClassesForRefObjs(int flid, int sclid, out List<ICmPossibility> possList)
		{
			var mdc = m_cache.GetManagedMetaDataCache();
			var clid = int.Parse(flid.ToString().Substring(0, flid.ToString().Length - 3));
			var classList1 = new HashSet<int>();
			var list = new List<string>();
			possList = null;
			switch (mdc.GetClassName(sclid))
			{
				case "CmObject":
					switch (mdc.GetClassName(clid) + "_" + mdc.GetFieldName(flid))
					{
						case "LexEntryRef_ComponentLexemes":
						case "LexEntryRef_PrimaryLexemes":
						case "LexReference_Targets":
							classList1 = new HashSet<int>(mdc.GetAllSubclasses(mdc.GetClassId("LexEntry")));
							foreach (var cid in mdc.GetAllSubclasses(mdc.GetClassId("LexSense")))
							{
								classList1.Add(cid);
							}
							return classList1;

						case "Segment_Analyses":
							classList1 = new HashSet<int>(mdc.GetAllSubclasses(mdc.GetClassId("WfiGloss")));
							foreach (var cid in mdc.GetAllSubclasses(mdc.GetClassId("WfiAnalysis")))
							{
								classList1.Add(cid);
							}
							foreach (var cid in mdc.GetAllSubclasses(mdc.GetClassId("WfiWordform")))
							{
								classList1.Add(cid);
							}
							foreach (var cid in mdc.GetAllSubclasses(mdc.GetClassId("PunctuationForm")))
							{
								classList1.Add(cid);
							}
							return classList1;
						// No work here yet since the following two areas are unimplemented so far.
						// For now they can fall into the default (per FWR-890)
						//case "MoPhonolRuleApp_Rule":
						//    classList1 = new HashSet<int>(mdc.GetAllSubclasses(mdc.GetClassId("PhSegmentRule")));
						//    return classList1;
						//case "PhPhonRuleFeat_Item":
						//    classList1 = new HashSet<int>(mdc.GetAllSubclasses(mdc.GetClassId("MoInflClass")));
						//    foreach (int cid in mdc.GetAllSubclasses(mdc.GetClassId("FsSymFeatVal"))) classList1.Add(cid);
						//    foreach (int cid in mdc.GetAllSubclasses(mdc.GetClassId("CmPossibility"))) classList1.Add(cid);
						//    return classList1;
						case "MoPhonolRuleApp_Rule":
						case "PhPhonRuleFeat_Item":
						case "StTxtPara_TextObjects":
						case "CmAnnotation_InstanceOf":
						case "CmBaseAnnotation_BeginObject":
						case "CmBaseAnnotation_EndObject":
						case "CmBaseAnnotation_OtherObjects":
						default:
							if (saveClid == 0)
							{
								var classes = mdc.GetAllSubclasses(sclid);
								list.AddRange(classes.Where(cl => cl != sclid).Select(cl => mdc.GetClassName(cl)));
								using (var dlg = new RealListChooser("ClassName", list))
								{
									dlg.ShowDialog(this);
									if (dlg.DialogResult == DialogResult.Cancel)
									{
										return classList1;
									}
									classList1.Add(mdc.GetClassId(dlg.ChosenClass)); // get the clid for the selected class
									if (mdc.GetClassId(dlg.ChosenClass) == 0)
									{
										MessageBox.Show($"No clid for selected class: {dlg.ChosenClass}");
										return classList1;
									}
									return classList1;
								}
							}
							classList1 = new HashSet<int>(mdc.GetAllSubclasses(saveClid));
							return classList1;
					}   // switch for properties with a signature of CmObject

				case "CmPossibility":
					{
						switch (mdc.GetClassName(clid) + "_" + mdc.GetFieldName(flid))
						{
							case "ConstChartTag_Tag":
								possList = new List<ICmPossibility>(m_cache.LangProject.DiscourseDataOA.ChartMarkersOA.ReallyReallyAllPossibilities);
								return classList1;
							case "ConstituentChartCellPart_Column":
							case "DsChart_Template":
								possList = new List<ICmPossibility>(m_cache.LangProject.DiscourseDataOA.ConstChartTemplOA.ReallyReallyAllPossibilities);
								return classList1;
							case "MoDerivAffMsa_AffixCategory":
							case "MoInflAffMsa_AffixCategory":
								possList = new List<ICmPossibility>(m_cache.LangProject.AffixCategoriesOA.ReallyReallyAllPossibilities);
								return classList1;
							case "CmPossibility_Status":
							case "RnGenericRec_Status":
								possList = new List<ICmPossibility>(m_cache.LangProject.StatusOA.ReallyReallyAllPossibilities);
								return classList1;
							case "CmPossibility_Confidence":
							case "RnGenericRec_Confidence":
								possList = new List<ICmPossibility>(m_cache.LangProject.ConfidenceLevelsOA.ReallyReallyAllPossibilities);
								return classList1;
							case "CmPerson_Education":
								possList = new List<ICmPossibility>(m_cache.LangProject.EducationOA.ReallyReallyAllPossibilities);
								return classList1;
							case "Text_Genres":
								possList = new List<ICmPossibility>(m_cache.LangProject.GenreListOA.ReallyReallyAllPossibilities);
								return classList1;
							case "CmPerson_Positions":
								possList = new List<ICmPossibility>(m_cache.LangProject.PositionsOA.ReallyReallyAllPossibilities);
								return classList1;
							case "CmPossibility_Restrictions":
							case "RnGenericRec_Restrictions":
								possList = new List<ICmPossibility>(m_cache.LangProject.RestrictionsOA.ReallyReallyAllPossibilities);
								return classList1;
							case "RnRoledPartic_Role":
								possList = new List<ICmPossibility>(m_cache.LangProject.RolesOA.ReallyReallyAllPossibilities);
								return classList1;
							case "TextTag_Tag":
								possList = new List<ICmPossibility>(m_cache.LangProject.TextMarkupTagsOA.ReallyReallyAllPossibilities);
								return classList1;
							case "RnGenericRec_TimeOfEvent":
								possList = new List<ICmPossibility>(m_cache.LangProject.TimeOfDayOA.ReallyReallyAllPossibilities);
								return classList1;
							case "CmTranslation_Type":
								possList = new List<ICmPossibility>(m_cache.LangProject.TranslationTagsOA.ReallyReallyAllPossibilities);
								return classList1;
							case "LexSense_DomainTypes":
								possList = new List<ICmPossibility>(m_cache.LangProject.LexDbOA.DomainTypesOA.ReallyReallyAllPossibilities);
								return classList1;
							case "LexSense_SenseType":
								possList = new List<ICmPossibility>(m_cache.LangProject.LexDbOA.SenseTypesOA.ReallyReallyAllPossibilities);
								return classList1;
							case "LexSense_Status":
								possList = new List<ICmPossibility>(m_cache.LangProject.StatusOA.ReallyReallyAllPossibilities);
								return classList1;
							case "LexSense_UsageTypes":
								possList = new List<ICmPossibility>(m_cache.LangProject.LexDbOA.UsageTypesOA.ReallyReallyAllPossibilities);
								return classList1;
							case "MoCompoundRule_ToProdRestrict":
							case "MoDerivAffMsa_FromProdRestrict":
							case "MoDerivAffMsa_ToProdRestrict":
							case "MoDerivStepMsa_ProdRestrict":
							case "MoInflAffMsa_FromProdRestrict":
							case "MoStemMsa_FromProdRestrict":
								possList = new List<ICmPossibility>(m_cache.LangProject.MorphologicalDataOA.ProdRestrictOA.ReallyReallyAllPossibilities);
								return classList1;
							case "RnGenericRec_Type":
								possList = new List<ICmPossibility>(m_cache.LangProject.ResearchNotebookOA.RecTypesOA.ReallyReallyAllPossibilities);
								return classList1;
							case "ScrScriptureNote_Categories":
								possList = new List<ICmPossibility>(m_cache.LangProject.TranslatedScriptureOA.NoteCategoriesOA.ReallyReallyAllPossibilities);
								return classList1;
							case "CmOverlay_PossItems":
							case "RnGenericRec_PhraseTags":
							default:
								classList1 = new HashSet<int>(mdc.GetAllSubclasses(mdc.GetClassId("CmPossibility")));
								return classList1;
						}  //cmPossibility fields Switch
					} //CmPossibilityLabel case
				default:
					classList1 = new HashSet<int>(mdc.GetAllSubclasses(sclid));
					return classList1;
			}  //classes switch
		}

		/// <summary>
		/// Check to see if the current object is owned by ScrBook.
		/// </summary>
		private static bool StParaOwnedByScrBook(IFwMetaDataCacheManaged mdc, ICmObject owner, ref int clid)
		{
			do
			{
				if (owner.ClassID == mdc.GetClassId("ScrBook")) //if the owner is an object of type ScrBook
				{
					clid = mdc.GetClassId("ScrTxtPara");   //scrTextPara
					return true;
				}
				owner = owner.Owner;
			} while (owner != null);

			return false;
		}

		/// <summary>
		/// See if an object can be added under the current object.
		/// </summary>
		private static bool AddObjectFromHere(IInspectorObject node)
		{
			return node.DisplayName.Substring(node.DisplayName.Length - 2, 2) == "OC" ||
				   node.DisplayName.Substring(node.DisplayName.Length - 2, 2) == "OS" ||
				   node.DisplayName.Substring(node.DisplayName.Length - 2, 2) == "RC" ||
				   node.DisplayName.Substring(node.DisplayName.Length - 2, 2) == "RS" ||
				   (node.DisplayName.EndsWith("OA") || node.DisplayName.EndsWith("RA")) &&
				   node.HasChildren == false && node.Object == null;
		}

		/// <summary>
		/// See if an object can be moved up in the current list.
		/// </summary>
		private static bool MoveObjectUpFromHere(IInspectorObject node)
		{
			var nodePos = (node.DisplayName.Contains("[") ? int.Parse(node.DisplayName.Substring(1, node.DisplayName.IndexOf("]") - 1)) : 0);
			if (node.ParentInspectorObject != null)
			{
				return (node.ParentInspectorObject.DisplayName.EndsWith("OS") || node.ParentInspectorObject.DisplayName.EndsWith("RS")) && (node.OwningObject as Array)?.Length > 1 && nodePos > 0;
			}
			return false;
		}

		/// <summary>
		/// See if an object can be moved down into the current list.
		/// </summary>
		private static bool MoveObjectDownFromHere(IInspectorObject node)
		{
			var nodePos = (node.DisplayName.Contains("[") ? int.Parse(node.DisplayName.Substring(1, node.DisplayName.IndexOf("]") - 1)) : 0);
			return node.ParentInspectorObject != null && (node.ParentInspectorObject.DisplayName.EndsWith("OS") || node.ParentInspectorObject.DisplayName.EndsWith("RS"))
													  && (node.OwningObject as Array)?.Length > 1 && nodePos < ((Array)node.OwningObject).Length - 1;
		}

		/// <summary>
		/// Opens an inspector window for the LCM model.
		/// </summary>
		private void OpenModelWindow()
		{
			if (m_modelWnd == null)
			{
				m_modelWnd = new ModelWnd();
				m_modelWnd.FormClosed += HandleWindowClosed;
			}

			m_modelWnd.Show(m_dockPanel);
		}

		/// <summary>
		/// Handles the Click event of the cmnuDeleteObject control.
		/// </summary>
		private void CmnuDeleteObjectClick(object sender, EventArgs e)
		{
			var mWnd = m_dockPanel.ActiveContent as InspectorWnd;
			var io = mWnd?.CurrentInspectorObject;
			if (!(io?.Object is ICmObject))
			{
				return;
			}
			var objToDelete = (ICmObject)io.Object;
			NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () => m_cache.DomainDataByFlid.DeleteObj(objToDelete.Hvo));

			//Refresh the display
			foreach (var dc in m_dockPanel.DocumentsToArray())
			{
				(dc as InspectorWnd)?.RefreshView("Delete");
			}
		}

		/// <summary>
		/// Handles the Click event of the cmnuMoveObjectUp control.
		/// </summary>
		private void CmnuMoveObjectUpClick(object sender, EventArgs e)
		{
			int inx;
			var mWnd = m_dockPanel.ActiveContent as InspectorWnd;
			var io = mWnd?.CurrentInspectorObject;
			if (io == null)
			{
				return;
			}
			if (!io.ParentInspectorObject.DisplayName.EndsWith("OS") && !io.ParentInspectorObject.DisplayName.EndsWith("RS"))
			{
				return;
			}
			var objToMove = io.Object as ICmObject ?? io.OriginalObject as ICmObject;
			if (objToMove == null) // we're on a field
			{
				MessageBox.Show("owner object couldn't be created.");
				return;
			}
			var owner = (io.ParentInspectorObject.Object as ICmObject ?? io.ParentInspectorObject.OriginalObject as ICmObject) ??
						io.ParentInspectorObject.OwningObject as ICmObject;
			if (owner == null) // we're on a field
			{
				MessageBox.Show("owner object couldn't be created.");
				return;
			}
			var work = StripOffTypeChars(io.ParentInspectorObject.DisplayName);
			var mdc = m_cache.ServiceLocator.GetInstance<IFwMetaDataCacheManaged>();
			var flid = mdc.GetFieldId2(owner.ClassID, work, true);
			if (io.ParentInspectorObject.DisplayName.EndsWith("OS"))
			{
				NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
				{
					m_cache.DomainDataByFlid.MoveOwn(owner.Hvo, flid, objToMove.Hvo, owner.Hvo, objToMove.OwningFlid, objToMove.OwnOrd - 1);
				});
			}
			else //for reference objects, add the reference to the new location, then delete it.
			{

				// index of object to move
				inx = m_cache.DomainDataByFlid.GetObjIndex(owner.Hvo, flid, objToMove.Hvo);
				NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
					m_cache.DomainDataByFlid.Replace(owner.Hvo, flid, inx - 1, inx - 1, new[] { objToMove.Hvo }, 1)
				);

				NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
					m_cache.DomainDataByFlid.Replace(owner.Hvo, flid, inx + 1, inx + 2, null, 0)
				);
			}

			//Refresh the display
			foreach (var dc in m_dockPanel.DocumentsToArray().OfType<InspectorWnd>())
			{
				(dc).RefreshView("Up");
			}
		}

		/// <summary>
		/// Handles the Click event of the cmnuMoveObjectDown control.
		/// </summary>
		private void CmnuMoveObjectDownClick(object sender, EventArgs e)
		{
			var mWnd = m_dockPanel.ActiveContent as InspectorWnd;
			var io = mWnd?.CurrentInspectorObject;
			if (io == null)
			{
				return;
			}
			var objToMove = io.Object as ICmObject ?? io.OriginalObject as ICmObject;
			if (objToMove == null) // we're on a field
			{
				MessageBox.Show("object to move couldn't be created.");
				return;
			}

			var owner = (io.ParentInspectorObject.Object as ICmObject ?? io.ParentInspectorObject.OriginalObject as ICmObject) ?? io.ParentInspectorObject.OwningObject as ICmObject;
			if (owner == null) // we're on a field
			{
				MessageBox.Show("owner object couldn't be created.");
				return;
			}

			if (!io.ParentInspectorObject.DisplayName.EndsWith("OS") && !io.ParentInspectorObject.DisplayName.EndsWith("RS"))
			{
				return;
			}
			var work = io.ParentInspectorObject.DisplayName.EndsWith("OS") || io.ParentInspectorObject.DisplayName.EndsWith("RS")
				? io.ParentInspectorObject.DisplayName.Substring(0, io.ParentInspectorObject.DisplayName.Length - 2)
				: io.DisplayName;

			var mdc = m_cache.ServiceLocator.GetInstance<IFwMetaDataCacheManaged>();
			var flid = mdc.GetFieldId2(owner.ClassID, work, true);

			if (io.ParentInspectorObject.DisplayName.EndsWith("OS"))
			{
				NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
				{
					m_cache.DomainDataByFlid.MoveOwn(owner.Hvo, flid, objToMove.Hvo, owner.Hvo, objToMove.OwningFlid, objToMove.OwnOrd + 2);
				});
			}
			else //for reference objects, add the reference to the new location, then delete it.
			{
				// index of object to move
				var inx = m_cache.DomainDataByFlid.GetObjIndex(owner.Hvo, flid, objToMove.Hvo);
				var cnt = m_cache.DomainDataByFlid.get_VecSize(owner.Hvo, flid);
				var inx2 = Math.Min(cnt, inx + 2);
				NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
					m_cache.DomainDataByFlid.Replace(owner.Hvo, flid, inx2, inx2, new[] { objToMove.Hvo }, 1)
				);
				NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
					m_cache.DomainDataByFlid.Replace(owner.Hvo, flid, inx, inx + 1, null, 0)
				);
			}
			//Refresh the display
			foreach (var dc in m_dockPanel.DocumentsToArray())
			{
				(dc as InspectorWnd)?.RefreshView("Down");
			}
		}

		/// <summary>
		/// Handles the Click event of the cmnuSelectProps control.
		/// </summary>
		private void CmnuSelectPropsClick(object sender, EventArgs e)
		{
			var wnd = m_dockPanel.ActiveContent as InspectorWnd;
			var io = wnd?.CurrentInspectorObject;
			if (!(io?.Object is ICmObject))
			{
				return;
			}

			ShowPropertySelectorDialog(io.Object as ICmObject);
		}

		/// <summary>
		/// Shows the property selector dialog.
		/// </summary>
		private void ShowPropertySelectorDialog(ICmObject cmObj)
		{
			using (var dlg = new ClassPropertySelector(cmObj))
			{
				if (dlg.ShowDialog(this) != DialogResult.OK)
				{
					return;
				}

				foreach (var dc in m_dockPanel.DocumentsToArray().OfType<InspectorWnd>())
				{
					dc.RefreshView();
				}
			}
		}

		/// <summary>
		/// Handles the Click event of the m_tsbShowCmObjectProps control.
		/// </summary>
		private void MTsbShowCmObjectPropsClick(object sender, EventArgs e)
		{
			m_tsbShowCmObjectProps.Checked = !m_tsbShowCmObjectProps.Checked;
			LCMClassList.ShowCmObjectProperties = m_tsbShowCmObjectProps.Checked;

			foreach (var dc in m_dockPanel.DocumentsToArray())
			{
				(dc as InspectorWnd)?.RefreshView();
			}
		}

		/// <summary>
		/// Handles the KeyPress event of the tstxtGuidSrch control.
		/// </summary>
		private void TstxtGuidSrchKeyPress(object sender, KeyPressEventArgs e)
		{
			if (e.KeyChar != (char)Keys.Enter)
			{
				return;
			}
			if (m_cache == null)
			{
				return;
			}
			var guid = new Guid(m_tstxtGuidSrch.Text.Trim());
			if (m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().TryGetObject(guid, out var obj))
			{
				var wnd = ShowNewInspectorWindow(obj, obj.ToString());
				wnd.ToolTipText = wnd.Text + Environment.NewLine + "Guid: " + m_tstxtGuidSrch.Text.Trim();
			}

			e.KeyChar = (char)0;
			e.Handled = true;
		}

		/// <summary>
		/// Save to the database; does NOT force clearing undo stack.
		/// </summary>
		private void MnuSaveFileLcmClick(object sender, EventArgs e)
		{
			m_cache?.ServiceLocator.GetInstance<IUndoStackManager>().Save();
		}

		/// <summary>
		/// Handles the Click event of the mnuToolsAllowEdit control.
		/// </summary>
		private void MnuToolsAllowEditClick(object sender, EventArgs e)
		{
			if (mnuToolsAllowEdit.Checked)
			{
				mnuToolsAllowEdit.Checked = false;
				m_updateFlag = false;
			}
			else
			{
				mnuToolsAllowEdit.Checked = true;
				m_updateFlag = true;
			}
		}

		/// <summary>
		/// Handles the DropDownOpening event of the viewToolStripMenuItem control.
		/// </summary>
		private void MnuViewDropDownOpening(object sender, EventArgs e)
		{
			mnuViewLangProject.Enabled = m_lp != null;
			mnuViewRepositories.Enabled = m_cache != null;
		}

		/// <summary>
		/// Handles the Click event of the MnuViewLcmModelClick control.
		/// </summary>
		private void MnuViewLcmModelClick(object sender, EventArgs e)
		{
			OpenModelWindow();
		}

		/// <summary>
		/// Handles the Click event of the mnuViewLangProject control.
		/// </summary>
		private void MnuViewLangProjectClick(object sender, EventArgs e)
		{
			OpenLangProjWindow();
		}

		/// <summary>
		/// Handles the Click event of the mnuViewRepositories control.
		/// </summary>
		private void MnuViewRepositoriesClick(object sender, EventArgs e)
		{
			OpenRepositoryWindow();
		}

		/// <summary>
		/// Handles the Click event of the mnuDisplayVirtual control.
		/// </summary>
		private void MnuDisplayVirtualClick(object sender, EventArgs e)
		{
			if (mnuDisplayVirtual.Checked)
			{
				mnuDisplayVirtual.Checked = false;
				m_virtualFlag = false;
			}
			else
			{
				mnuDisplayVirtual.Checked = true;
				m_virtualFlag = true;
			}

			//Refresh the display
			foreach (var dc in m_dockPanel.DocumentsToArray())
			{
				(dc as InspectorWnd)?.RefreshView();
			}

			m_modelWnd.AfterSelectMethod();
		}

		/// <summary>
		/// Handles the Click event of the mnuClassProperties control.
		/// </summary>
		private void MnuClassPropertiesClick(object sender, EventArgs e)
		{
			ShowPropertySelectorDialog(null);
		}

		/// <summary>
		/// Opens an inspector window with the language project as the top level object.
		/// </summary>
		private void OpenLangProjWindow()
		{
			if (m_langProjWnd != null)
			{
				m_langProjWnd.Show(m_dockPanel);
			}
			else
			{
				m_langProjWnd = ShowNewInspectorWindow(m_lp, m_lp.ShortName + ": LangProj");
			}
		}

		/// <summary>
		/// Opens an inspector window with a collection of the language project's
		/// repositories as the top level object.
		/// </summary>
		private void OpenRepositoryWindow()
		{
			if (m_repositoryWnd != null)
			{
				m_repositoryWnd.Show(m_dockPanel);
			}
			else
			{
				var repositories = LCMClassList.RepositoryTypes.Select(repoType => m_cache.ServiceLocator.GetInstance(repoType)).ToList();

				// Go through all the service types and find those that are repositories. For each
				// repository type, get its instance from the service locator and store that in a list.
				m_repositoryWnd = ShowNewInspectorWindow(repositories, m_cache.ProjectId.UiName + ": Repositories");
			}
		}
		/// <summary>
		/// Gets the BEP type from the specified file path.
		/// </summary>
		private static BackendProviderType GetBEPTypeFromFileExtension(string pathname)
		{
			switch (Path.GetExtension(pathname).ToLower())
			{
				default:
					return BackendProviderType.kMemoryOnly;
				case LcmFileHelper.ksFwDataXmlFileExtension:
					return BackendProviderType.kXML;

			}
		}

		/// <summary>
		/// Gets the custom fields defined in the project file.
		/// </summary>
		private static List<CustomFields> GetCustomFields(LcmCache cache)
		{
			var type = string.Empty;
			var list = new List<CustomFields>();
			foreach (var fd in FieldDescription.FieldDescriptors(cache))
			{
				if (fd.IsCustomField)
				{
					switch (fd.Type.ToString())
					{
						case "String":
							type = "ITsString";
							break;
						case "Integer":
							type = "System.Int32";
							break;
						case "GenDate":
							type = "SIL.LCModel.Core.Cellar.GenDate";
							break;
						case "ReferenceAtomic":
							type = "ICmPossibility";
							break;
						case "ReferenceCollection":
							type = "LcmReferenceCollection<ICmPossibility>";
							break;
						case "MinObj":
							type = "IStText";
							break;

						case "MultiUnicode":
							// This is a guess
							type = "WfiWordForm";
							break;
						default:
							MessageBox.Show($"In 'GetCustomFields, Type was unknown: {fd.Type.ToString()} ");
							break;
					}

					var cf = new CustomFields(fd.Name, fd.Class, fd.Id, type);
					list.Add(cf);
				}
			}
			return list.Count > 0 ? list : null;
		}

		/// <summary>
		/// Handles the LangProj CellValueChanged event of the DataGrid control in the Inspector object.
		/// </summary>
		void LangProjInspectorGrid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
		{
			UpdateLcmProp(sender, m_langProjWnd);
		}

		/// <summary>
		/// UpdateLcmProp - update LCM with changed values in the repository and LangProject windows.
		/// /// </summary>
		private void UpdateLcmProp(object sender, InspectorWnd mWnd)
		{
			var sendObj = (InspectorGrid)sender;
			if (mWnd.CurrentInspectorObject.DisplayValue == sendObj.EditingControl.Text)
			{
				return;     // nothing changed so return
			}
			var dType = mWnd.CurrentInspectorObject.DisplayType;

			if (dType != "System.String" && dType != "System.Int32" && dType != "System.Boolean" &&
				dType != "System.DateTime" && dType != "SIL.FieldWorks.Common.FwUtils.GenDate.PrecisionType")
			{
				return;
			}

			if (!BuildLcmPINeeded(mWnd.CurrentInspectorObject, out var pi, out var obj, out var hvo))
			{
				throw new ApplicationException("The necessary Info to update the LCM wasn't obtained.");
			}
			var intNewVal = 0;
			var dtNewVal = new DateTime();
			var guidNewVal = new Guid();
			var genDate = new GenDate();
			GenDate genDate1;
			string strNewVal;
			var node = mWnd.CurrentInspectorObject;
			switch (dType)
			{
				case "System.Int32":
					try
					{
						intNewVal = int.Parse(sendObj.EditingControl.Text);
						node.DisplayValue = sendObj.EditingControl.Text;
					}
					catch
					{
						MessageBox.Show($"Value entered is not a valid integer: {sendObj.EditingControl.Text}");
						node.DisplayValue = node.DisplayValue;
						return;
					}

					if (node.ParentInspectorObject.DisplayType.IndexOf("GenDate") > 0)
					{
						if (ValidateGenDate(node, obj, pi, intNewVal, out genDate))
						{
							UpdateValues(node.ParentInspectorObject, obj, pi, "GenDate", string.Empty, intNewVal, genDate, dtNewVal, guidNewVal, false);
							node.ParentInspectorObject.DisplayValue = genDate.ToLongString();
							return;
						}
						MessageBox.Show("GenDate Format (Update Integer) is invalid.");
						return;
					}

					UpdateValues(node, obj, pi, "Integer", "", intNewVal, genDate, dtNewVal, guidNewVal, false);
					break;
				case "System.Guid":
					node.DisplayValue = sendObj.EditingControl.Text;
					guidNewVal = new Guid(sendObj.EditingControl.Text);
					UpdateValues(node, obj, pi, "Guid", string.Empty, 0, genDate, dtNewVal, guidNewVal, false);
					break;
				case "System.Boolean":
					bool boolNewVal;
					try
					{
						boolNewVal = bool.Parse(sendObj.EditingControl.Text);
						node.DisplayValue = sendObj.EditingControl.Text;
					}
					catch
					{
						MessageBox.Show($"Value entered is not a valid boolean: {sendObj.EditingControl.Text}");
						node.DisplayValue = node.DisplayValue;
						return;
					}
					if (node.ParentInspectorObject.DisplayType.IndexOf("GenDate") > 0)
					{
						if (node.ParentInspectorObject != null && node.ParentInspectorObject.Flid > 0)
						{
							genDate1 = m_silDataAccessManaged.get_GenDateProp(obj.Hvo, node.ParentInspectorObject.Flid);
						}
						else
						{
							genDate1 = (GenDate)pi.GetValue(obj, null);
						}
						switch (node.DisplayName)
						{
							case "IsEmpty":
								if (boolNewVal)
								{
									genDate = new GenDate(genDate1.Precision, 0, 0, 0, genDate1.IsAD);
									ResetDisplayDates(node, mWnd, "IsEmpty", "0", hvo);
								}
								else
								{
									genDate = new GenDate(genDate1.Precision, 1, 1, 1, genDate1.IsAD);
									ResetDisplayDates(node, mWnd, "IsEmpty", "1", hvo);
								}
								break;
							case "IsAD":
								if (boolNewVal)
								{
									genDate = new GenDate(genDate1.Precision, 1, 1, genDate1.Year, true);
									ResetDisplayDates(node, mWnd, "IsAD", "1", hvo);
								}
								else
								{
									genDate = new GenDate(genDate1.Precision, 0, 0, genDate1.Year, false);
									ResetDisplayDates(node, mWnd, "IsAD", "0", hvo);
								}
								break;
							default:
								MessageBox.Show($"GenDate boolean not IsAd or IsEmpty: {node.DisplayName}");
								return;
						}

						node.ParentInspectorObject.DisplayValue = genDate.ToLongString();
						UpdateValues(node.ParentInspectorObject, obj, pi, "GenDate", string.Empty, intNewVal, genDate, dtNewVal, guidNewVal, boolNewVal);
						break;
					}

					UpdateValues(node, obj, pi, "Boolean", "", 0, genDate, dtNewVal, guidNewVal, boolNewVal);

					break;
				case "System.String":
					for (var l = node.Level; l > 0; l--)
					{
						if (ParentIsType(ref node, "TsString"))
						{
							MessageBox.Show("This type of string cannot be editted because it is under a TsString");
							return;
						}
					}
					node = mWnd.CurrentInspectorObject;
					mWnd.CurrentInspectorObject.DisplayValue = sendObj.EditingControl.Text;
					strNewVal = sendObj.EditingControl.Text;
					UpdateValues(node, obj, pi, "String", strNewVal, 0, genDate, dtNewVal, guidNewVal, false);
					break;
				case "System.DateTime":
					if (DateTime.TryParse(sendObj.EditingControl.Text, out dtNewVal))
					{
						node.DisplayValue = sendObj.EditingControl.Text;
						UpdateValues(node, obj, pi, "DateTime", string.Empty, 0, genDate, dtNewVal, guidNewVal, false);

						break;
					}
					MessageBox.Show("Date Format is invalid.");
					node.DisplayValue = node.DisplayValue;
					return;
				case "SIL.FieldWorks.Common.FwUtils.GenDate.PrecisionType":
					strNewVal = sendObj.EditingControl.Text;
					GenDate.PrecisionType newPreType;
					switch (strNewVal)
					{
						case "Before":
							newPreType = GenDate.PrecisionType.Before;
							break;
						case "Exact":
							newPreType = GenDate.PrecisionType.Exact;
							break;
						case "Approximate":
							newPreType = GenDate.PrecisionType.Approximate;
							break;
						case "After":
							newPreType = GenDate.PrecisionType.After;
							break;
						default:
							MessageBox.Show("Precision must be: Before, After, Exact or Approximate.");
							node.DisplayValue = node.DisplayValue;
							return;
					}

					if (node.ParentInspectorObject != null && node.ParentInspectorObject.Flid > 0)
					{
						genDate1 = m_silDataAccessManaged.get_GenDateProp(obj.Hvo, node.ParentInspectorObject.Flid);
					}
					else
					{
						genDate1 = (GenDate)pi.GetValue(obj, null);
					}
					node.DisplayValue = strNewVal;
					try
					{
						genDate = new GenDate(newPreType, genDate1.Month, genDate1.Day, genDate1.Year, genDate1.IsAD);
					}
					catch
					{
						MessageBox.Show("Gendate with new Precision is invalid.");
						return;
					}
					node.ParentInspectorObject.DisplayValue = genDate.ToLongString();
					UpdateValues(node.ParentInspectorObject, obj, pi, "GenDate", string.Empty, 0, genDate, dtNewVal, guidNewVal, false);
					break;
				default:
					throw new ApplicationException("Illegal type");
			}
		}

		/// <summary>
		/// Updates the object.  Custom fields update the cache, others the PropertyInfo.
		/// </summary>
		private void UpdateValues(IInspectorObject node, ICmObject obj, PropertyInfo pi, string operation, string strVal, int intVal, GenDate genVal, DateTime dtVal, Guid guidVal, bool boolVal)
		{
			switch (operation)
			{
				case "String":
					NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, UpdateStringValue);
					break;
				case "GenDate":
					NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, UpdateGenDateValue);
					break;
				case "DateTime":
					NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, UpdateDateTimeValue);
					break;
				case "Boolean":
					NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, UpdateBooleanValue);
					break;
				case "Integer":
					NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, UpdateIntegerValue);
					break;
				case "Guid":
					NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, UpdateGuidValue);
					break;
				default:
					MessageBox.Show($"Operation passed to UpdateValues is invalid: {operation}");
					break;
			}

			void UpdateGenDateValue()
			{
				if (node != null && node.Flid > 0)
				{
					m_silDataAccessManaged.SetGenDate(obj.Hvo, node.Flid, genVal);
				}
				else if (pi == null && node != null)
				{
					if (GetPI(node, StripOffTypeChars(node.DisplayName), obj, out pi))
					{
						pi.SetValue(obj, genVal, null);
					}
				}
				else
				{
					pi.SetValue(obj, genVal, null);
				}
			}

			void UpdateStringValue()
			{
				if (node != null && node.Flid > 0)
				{
					m_cache.DomainDataByFlid.set_UnicodeProp(obj.Hvo, node.Flid, strVal);
				}
				else if (pi == null && node != null)
				{
					if (GetPI(node, StripOffTypeChars(node.DisplayName), obj, out pi))
					{
						pi.SetValue(obj, strVal, null);
					}
				}
				else
				{
					pi.SetValue(obj, strVal, null);
				}
			}

			void UpdateDateTimeValue()
			{
				if (node != null && node.Flid > 0)
				{
					m_silDataAccessManaged.SetDateTime(obj.Hvo, node.Flid, dtVal);
				}
				else if (pi == null && node != null)
				{
					if (GetPI(node, StripOffTypeChars(node.DisplayName), obj, out pi))
					{
						pi.SetValue(obj, dtVal, null);
					}
				}
				else
				{
					pi.SetValue(obj, dtVal, null);
				}
			}

			void UpdateBooleanValue()
			{
				if (node != null && node.Flid > 0)
				{
					m_cache.DomainDataByFlid.SetBoolean(obj.Hvo, node.Flid, boolVal);
				}
				else if (pi == null && node != null)
				{
					if (GetPI(node, StripOffTypeChars(node.DisplayName), obj, out pi))
					{
						pi.SetValue(obj, boolVal, null);
					}
				}
				else
				{
					pi.SetValue(obj, boolVal, null);
				}
			}

			void UpdateIntegerValue()
			{
				if (node != null && node.Flid > 0)
				{
					m_cache.DomainDataByFlid.SetInt(obj.Hvo, node.Flid, intVal);
				}
				else if (pi == null && node != null)
				{
					if (GetPI(node, StripOffTypeChars(node.DisplayName), obj, out pi))
					{
						pi.SetValue(obj, intVal, null);
					}
				}
				else
				{
					pi.SetValue(obj, intVal, null);
				}
			}

			void UpdateGuidValue()
			{
				if (node != null && node.Flid > 0)
				{
					m_cache.DomainDataByFlid.SetGuid(obj.Hvo, node.Flid, guidVal);
				}
				else if (pi == null && node != null)
				{
					if (GetPI(node, StripOffTypeChars(node.DisplayName), obj, out pi))
					{
						pi.SetValue(obj, guidVal, null);
					}
				}
				else
				{
					pi.SetValue(obj, guidVal, null);
				}
			}
		}

		private static bool GetPI(IInspectorObject node, string fieldName, ICmObject obj, out PropertyInfo pi)
		{
			var type = obj.GetType();

			if (!(node != null && node.Flid > 0))  //custom properties don't have PropertyInfo
			{
				pi = type.GetProperty(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
				if (pi == null)
				{
					MessageBox.Show("The PI Object to get property info from is null.");
					return false;
				}
			}
			else
			{
				pi = null;
			}
			return true;
		}

		/// <summary>
		/// Gets the property info if it's available.
		/// </summary>
		private bool BuildLcmPINeeded(IInspectorObject node, out PropertyInfo pi, out ICmObject obj, out int hvo)
		{
			var fieldName = string.Empty;
			hvo = 0;
			for (var l = node.Level; l > 0; l--)
			{
				if (GetHvoNode(ref node, ref hvo, ref fieldName))
				{
					break;
				}
				if (l == 0)
				{
					MessageBox.Show($"The hvo could not be created for {node.DisplayName}");
					pi = null; obj = null; hvo = 0;
					return false;
				}
			}

			obj = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo);
			if (obj == null)
			{
				MessageBox.Show("The LCM Object to update is null.");
				pi = null;
				return false;
			}

			return GetPI(node, fieldName, obj, out pi);
		}

		/// <summary>
		/// Gets the hvo node and column name if it's available at this level
		/// </summary>
		private static bool GetHvoNode(ref IInspectorObject node, ref int hvo, ref string fieldName)
		{
			try
			{
				hvo = ((ICmObject)node.OwningObject).Hvo;
				fieldName = node.DisplayName;
				return true;
			}
			catch
			{
				node = node.ParentInspectorObject;
				return false;
			}
		}

		/// <summary>
		/// Checks if the parent of the current node is of a certain type
		/// </summary>
		private static bool ParentIsType(ref IInspectorObject node, string type)
		{
			if (node.DisplayType.IndexOf(type) > 0)
			{
				return true;
			}
			node = node.ParentInspectorObject;
			return false;
		}

		/// <summary>
		/// Handles determining whether or not the specified object will disappear if the
		/// view it's in is refresh. For our purposes here, we assume that properties that
		/// are part of the CmObject base class will disappear (unless the property is a guid).
		/// </summary>
		private static bool HandleWillObjDisappearOnRefresh(object sender, IInspectorObject io)
		{
			// Check if the selected object will disappear after refreshing the grid.
			return (io.DisplayName != "Guid" && LCMClassList.IsCmObjectProperty(io.DisplayName));
		}

		/// <summary>
		/// Verify that, with the changes made, the gendate is still valid.
		/// </summary>
		private bool ValidateGenDate(IInspectorObject io, ICmObject obj, PropertyInfo pi, int mdy, out GenDate genDate)
		{
			DateTime dt1, dt;
			GenDate genDate1;
			if (io.ParentInspectorObject != null && io.ParentInspectorObject.Flid > 0)
			{
				genDate1 = m_silDataAccessManaged.get_GenDateProp(obj.Hvo, io.ParentInspectorObject.Flid);
			}
			else
			{
				genDate1 = (GenDate)pi.GetValue(obj, null);
			}
			switch (io.DisplayName)
			{
				case "Day":
					try
					{
						genDate = new GenDate(genDate1.Precision, genDate1.Month, mdy, genDate1.Year, genDate1.IsAD);
						io.DisplayValue = mdy.ToString();
						return true;
					}
					catch
					{
						MessageBox.Show("GenDate day is invalid");
						genDate = genDate1;
						io.DisplayValue = genDate1.Day.ToString();
						return false;
					}
				case "Month":
					try
					{
						genDate = new GenDate(genDate1.Precision, mdy, genDate1.Day, genDate1.Year, genDate1.IsAD);
						io.DisplayValue = mdy.ToString();
						return true;
					}
					catch
					{
						MessageBox.Show("GenDate month is invalid");
						genDate = genDate1;
						io.DisplayValue = genDate1.Month.ToString();
						return false;
					}
				case "Year":
					try
					{
						genDate = new GenDate(genDate1.Precision, genDate1.Month, genDate1.Day, mdy, genDate1.IsAD);
						io.DisplayValue = mdy.ToString();
						return true;
					}
					catch
					{
						MessageBox.Show("GenDate year is invalid");
						genDate = genDate1;
						io.DisplayValue = genDate1.Year.ToString();
						return false;
					}
				default:
					MessageBox.Show($"GenDate Integer passed is not Month, Day or Year: {io.DisplayName}");
					genDate = genDate1;
					return false;
			}
		}

		/// <summary>
		/// Looks through the grid display and update the display integers of the current GenDate to 0.
		/// </summary>
		private static void ResetDisplayDates(IInspectorObject node, InspectorWnd mWnd, string type, string value, int hvo)
		{
			var genDateFlag = false;

			for (var i = 0; 1 < mWnd.InspectorList.Count; i++)
			{
				if (genDateFlag)
				{
					if (mWnd.InspectorList[i].DisplayName == "Day")
					{
						mWnd.InspectorList[i].DisplayValue = value;
					}
					else if (mWnd.InspectorList[i].DisplayName == "Month")
					{
						mWnd.InspectorList[i].DisplayValue = value;
						if (type == "IsAD")
						{
							break;
						}
					}
					else if (mWnd.InspectorList[i].DisplayName == "Year")
					{
						mWnd.InspectorList[i].DisplayValue = value;
						break;
					}
				}

				if (mWnd.InspectorList[i].DisplayName == node.ParentInspectorObject.DisplayName && hvo == ((ICmObject)node.ParentInspectorObject.OwningObject).Hvo)
				{
					genDateFlag = true;
				}
			}
		}

		/// <summary>
		/// Handles the Repository CellValueChanged event of the DataGrid control in the Inspector object.
		/// </summary>
		private void RepositoryInspectorGrid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
		{
			UpdateLcmProp(sender, m_repositoryWnd);
		}

		/// <summary />
		private sealed class LCModelInspectorList : GenericInspectorObjectList
		{
			private readonly LcmCache _cache;
			private readonly IFwMetaDataCacheManaged _mdc;

			/// <summary />
			internal LCModelInspectorList(LcmCache cache)
			{
				_cache = cache;
				_mdc = _cache.ServiceLocator.GetInstance<IFwMetaDataCacheManaged>();
			}

			#region overridden methods
			/// <summary>
			/// Initializes the list using the specified top level object.
			/// </summary>
			public override void Initialize(object topLevelObj)
			{
				base.Initialize(topLevelObj);

				foreach (var io in this)
				{
					if (io.Object == null || io.Object.GetType().GetInterface("IRepository`1") == null)
					{
						continue;
					}
					var pi = io.Object.GetType().GetProperty("Count");
					var count = (int)pi.GetValue(io.Object, null);
					io.DisplayValue = FormatCountString(count);
					io.DisplayName = io.DisplayType;
					io.HasChildren = (count > 0);
				}

				Sort(CompareInspectorObjectNames);
			}

			/// <summary>
			/// Gets a list of IInspectorObject objects for the properties of the specified object.
			/// </summary>
			protected override List<IInspectorObject> GetInspectorObjects(object obj, int level)
			{
				if (obj == null)
				{
					return BaseGetInspectorObjects(null, level);
				}
				var tmpObj = obj;
				var io = obj as IInspectorObject;
				if (io != null)
				{
					tmpObj = io.Object;
				}
				if (tmpObj == null)
				{
					return BaseGetInspectorObjects(obj, level);
				}
				if (tmpObj.GetType().GetInterface("IRepository`1") != null)
				{
					return GetInspectorObjectsForRepository(tmpObj, io, level);
				}
				if (tmpObj is IMultiAccessorBase multiAccessorBase)
				{
					return GetInspectorObjectsForMultiString(multiAccessorBase, io, level);
				}
				if (m_virtualFlag == false && io?.ParentInspectorObject != null && io.ParentInspectorObject.DisplayName == "Values"
					&& io.ParentInspectorObject.ParentInspectorObject.DisplayType == "MultiUnicodeAccessor")
				{
					return GetInspectorObjectsForUniRuns(tmpObj as ITsString, io, level);
				}
				if (tmpObj is ITsString tsString)
				{
					return GetInspectorObjectsForTsString(tsString, io, level);
				}
				if (m_virtualFlag == false && tmpObj is TextProps textProps)
				{
					return GetInspectorObjectsForTextProps(textProps, io, level);
				}
				if (m_virtualFlag == false && io != null && io.DisplayName == "Values" && (io.ParentInspectorObject.DisplayType == "MultiUnicodeAccessor"
																										  || io.ParentInspectorObject.DisplayType == "MultiStringAccessor"))
				{
					return GetInspectorObjectsForValues(tmpObj, io, level);
				}
				return io != null && io.DisplayName.EndsWith("RC") && io.Flid > 0 && _mdc.IsCustom(io.Flid)
					? GetInspectorObjectsForCustomRC(tmpObj, io, level)
					: BaseGetInspectorObjects(obj, level);
			}

			/// <summary>
			/// Gets the inspector objects for the specified repository object;
			/// </summary>
			private List<IInspectorObject> GetInspectorObjectsForRepository(object obj, IInspectorObject ioParent, int level)
			{
				var i = 0;
				var list = new List<IInspectorObject>();
				foreach (var io in GetRepositoryInstances(obj).Select(instance => CreateInspectorObject(instance, obj, ioParent, level)))
				{
					switch (m_virtualFlag)
					{
						case false when obj.ToString().Contains("LexSenseRepository"):
						{
							var tmpObj = (ILexSense)io.Object;
							io.DisplayValue = tmpObj.FullReferenceName.Text;
							io.DisplayName = $"[{i++}]: {GetObjectOnly(tmpObj.ToString())}";
							break;
						}
						case false when obj.ToString().Contains("LexEntryRepository"):
						{
							var tmpObj = (ILexEntry)io.Object;
							io.DisplayValue = tmpObj.HeadWord.Text;
							io.DisplayName = $"[{i++}]: {GetObjectOnly(tmpObj.ToString())}";
							break;
						}
						default:
							io.DisplayName = $"[{i++}]";
							break;
					}
					list.Add(io);
				}

				i = IndexOf(obj);
				if (i < 0)
				{
					return list;
				}
				this[i].DisplayValue = FormatCountString(list.Count);
				this[i].HasChildren = list.Any();
				return list;
			}

			/// <summary>
			/// Gets the inspector objects for the specified MultiString.
			/// </summary>
			private List<IInspectorObject> GetInspectorObjectsForMultiString(IMultiAccessorBase msa, IInspectorObject ioParent, int level)
			{
				var list = m_virtualFlag ? BaseGetInspectorObjects(msa, level) : GetMultiStringInspectorObjects(msa, ioParent, level);
				var allStrings = new Dictionary<int, string>();
				try
				{
					// Put this in a try/catch because VirtualStringAccessor
					// didn't implement StringCount when this was written.
					for (var i = 0; i < msa.StringCount; i++)
					{
						var tss = msa.GetStringFromIndex(i, out var ws);
						allStrings[ws] = tss.Text;
					}
				}
				catch { }

				if (!m_virtualFlag)
				{
					return list;
				}
				var io = CreateInspectorObject(allStrings, msa, ioParent, level);
				io.DisplayName = "AllStrings";
				io.DisplayValue = FormatCountString(allStrings.Count);
				io.HasChildren = (allStrings.Count > 0);
				list.Insert(0, io);
				list.Sort((x, y) => x.DisplayName.CompareTo(y.DisplayName));
				return list;
			}

			/// <summary>
			/// Gets a list of IInspectorObject objects (same as base), but includes s lot of
			/// specifics if you choose not to see virtual fields.
			/// </summary>
			private List<IInspectorObject> GetMultiStringInspectorObjects(object obj, IInspectorObject ioParent, int level)
			{
				if (ioParent != null)
				{
					obj = ioParent.Object;
				}
				var list = new List<IInspectorObject>();
				if (obj is ICollection collection)
				{
					var i = 0;
					foreach (var item in collection)
					{
						var io = CreateInspectorObject(item, obj, ioParent, level);
						io.DisplayName = $"[{i++}]";
						list.Add(io);
					}

					return list;
				}

				foreach (var pi in GetPropsForObj(obj))
				{
					try
					{
						var propObj = pi.GetValue(obj, null);
						var inspectorObject = CreateInspectorObject(pi, propObj, obj, ioParent, level);
						if (obj.ToString().IndexOf("MultiUnicodeAccessor") > 0 && inspectorObject.DisplayName != "Values" || obj.ToString().IndexOf("MultiStringAccessor") > 0 && inspectorObject.DisplayName != "Values")
						{
							continue;
						}
						if (inspectorObject.Object is ICollection collection1)
						{
							inspectorObject.DisplayValue = $"Count = {collection1.Count}";
							inspectorObject.HasChildren = (collection1.Count > 0);
						}
						list.Add(inspectorObject);
					}
					catch (Exception e)
					{
					}
				}

				list.Sort(CompareInspectorObjectNames);
				return list;
			}

			/// <summary>
			/// Gets the inspector objects for the specified TsString.
			/// </summary>
			private List<IInspectorObject> GetInspectorObjectsForTsString(ITsString tss, IInspectorObject ioParent, int level)
			{
				var list = new List<IInspectorObject>();
				var runCount = tss.RunCount;
				var tssriList = new List<TsStringRunInfo>();
				for (var i = 0; i < runCount; i++)
				{
					tssriList.Add(new TsStringRunInfo(i, tss));
				}
				var io = CreateInspectorObject(tssriList, tss, ioParent, level);
				io.DisplayName = "Runs";
				io.DisplayValue = FormatCountString(tssriList.Count);
				io.HasChildren = (tssriList.Count > 0);
				list.Add(io);

				if (!m_virtualFlag)
				{
					return list;
				}
				io = CreateInspectorObject(tss.Length, tss, ioParent, level);
				io.DisplayName = "Length";
				list.Add(io);
				io = CreateInspectorObject(tss.Text, tss, ioParent, level);
				io.DisplayName = "Text";
				list.Add(io);

				return list;
			}

			/// <summary>
			/// Gets the inspector objects for the specified TextProps.
			/// </summary>
			private List<IInspectorObject> GetInspectorObjectsForTextProps(TextProps txp, IInspectorObject ioParent, int level)
			{
				if (ioParent != null)
				{
					txp = ioParent.Object as TextProps;
				}
				var list = new List<IInspectorObject>();
				var saveIntPropCount = 0;
				var saveStrPropCount = 0;
				foreach (var pi in GetPropsForObj(txp))
				{
					if (pi.Name != "IntProps" && pi.Name != "StrProps" && pi.Name != "IntPropCount" && pi.Name != "StrPropCount")
					{
						continue;
					}
					IInspectorObject io;
					switch (pi.Name)
					{
						case "IntProps":
							var propObj = pi.GetValue(txp, null);
							io = CreateInspectorObject(pi, propObj, txp, ioParent, level);
							io.DisplayValue = "Count = " + saveIntPropCount;
							io.HasChildren = (saveIntPropCount > 0);
							list.Add(io);
							break;
						case "StrProps":
							var propObj1 = pi.GetValue(txp, null);
							io = CreateInspectorObject(pi, propObj1, txp, ioParent, level);
							io.DisplayValue = "Count = " + saveStrPropCount;
							io.HasChildren = (saveStrPropCount > 0);
							list.Add(io);
							break;
						case "StrPropCount":
							saveStrPropCount = (int)pi.GetValue(txp, null);
							break;
						case "IntPropCount":
							saveIntPropCount = (int)pi.GetValue(txp, null);
							break;
					}
				}

				list.Sort(CompareInspectorObjectNames);
				return list;
			}

			/// <summary>
			/// Gets a list of IInspectorObject objects representing all the properties for the
			/// specified object, which is assumed to be at the specified level.
			/// </summary>
			private List<IInspectorObject> GetInspectorObjectsForValues(object obj, IInspectorObject ioParent, int level)
			{
				if (ioParent != null)
				{
					obj = ioParent.Object;
				}
				var list = new List<IInspectorObject>();
				if (ioParent.OwningObject is IMultiAccessorBase multiStr)
				{
					foreach (var ws in multiStr.AvailableWritingSystemIds)
					{
						var wsObj = _cache.ServiceLocator.WritingSystemManager.Get(ws);
						var ino = CreateInspectorObject(multiStr.get_String(ws), obj, ioParent, level);
						ino.DisplayName = wsObj.DisplayLabel;
						list.Add(ino);
					}
					return list;
				}

				var props = GetPropsForObj(obj);
				foreach (var pi in props)
				{
					try
					{
						var propObj = pi.GetValue(obj, null);
						list.Add(CreateInspectorObject(pi, propObj, obj, ioParent, level));
					}
					catch (Exception e)
					{
						list.Add(CreateExceptionInspectorObject(e, obj, pi.Name, level, ioParent));
					}
				}

				list.Sort(CompareInspectorObjectNames);
				return list;
			}

			/// <summary>
			/// Condenses the 'Run' information for MultiUnicodeAccessor entries because
			/// there will only be 1 run,
			/// </summary>
			private List<IInspectorObject> GetInspectorObjectsForUniRuns(ITsString obj, IInspectorObject ioParent, int level)
			{
				var list = new List<IInspectorObject>();
				if (obj == null)
				{
					return list;
				}
				var ino = CreateInspectorObject(obj, ioParent.OwningObject, ioParent, level);
				ino.DisplayName = "Writing System";
				ino.DisplayValue = obj.get_WritingSystemAt(0).ToString();
				ino.HasChildren = false;
				list.Add(ino);

				var tss = new TsStringRunInfo(0, obj);
				ino = CreateInspectorObject(tss, obj, ioParent, level);
				ino.DisplayName = "Text";
				ino.DisplayValue = tss.Text;
				ino.HasChildren = false;
				list.Add(ino);
				return list;
			}

			/// <summary>
			/// Create the reference collection list for the custom reference collection.
			/// </summary>
			private List<IInspectorObject> GetInspectorObjectsForCustomRC(object obj, IInspectorObject ioParent, int level)
			{
				if (obj == null)
				{
					return null;
				}
				// Inspectors for custom reference collections are supposed to be configured with
				// obj being an array of the HVOs.
				if (!(obj is ICollection collection))
				{
					MessageBox.Show("Custom Reference collection not properly configured with array of HVOs");
					return null;
				}
				var list = new List<IInspectorObject>();
				var n = 0;
				// Just like an ordinary reference collection, we want to make one inspector for each
				// item in the collection, where the first argument to CreateInspectorObject is the
				// cmObject. Keep this code in sync with BaseGetInspectorObjects.
				foreach (int hvoItem in collection)
				{
					var hvoNum = Int32.Parse(hvoItem.ToString());
					var objItem = _cache.ServiceLocator.GetObject(hvoNum);
					var io = CreateInspectorObject(objItem, obj, ioParent, level);
					io.DisplayName = $"[{n++}]";
					list.Add(io);
				}
				return list;
			}

			/// <summary>
			/// Gets a list of IInspectorObject objects representing all the properties for the
			/// specified object, which is assumed to be at the specified level.
			/// </summary>
			private List<IInspectorObject> BaseGetInspectorObjects(object obj, int level)
			{
				var ioParent = obj as IInspectorObject;
				if (ioParent != null)
				{
					obj = ioParent.Object;
				}

				var list = new List<IInspectorObject>();
				if (obj is ICollection collection)
				{
					var i = 0;
					foreach (var item in collection)
					{
						var io = CreateInspectorObject(item, obj, ioParent, level);
						io.DisplayName = $"[{i++}]";
						list.Add(io);
					}
					return list;
				}

				foreach (var pi in GetPropsForObj(obj))
				{
					try
					{
						var propObj = pi.GetValue(obj, null);
						var io1 = CreateInspectorObject(pi, propObj, obj, ioParent, level);
						if (io1.DisplayType == "System.DateTime")
						{
							io1.HasChildren = false;
						}
						list.Add(io1);
					}
					catch (Exception e)
					{
						list.Add(CreateExceptionInspectorObject(e, obj, pi.Name, level, ioParent));
					}
				}

				if (CFields != null && CFields.Any() && obj != null)
				{
					list.AddRange(CFields.Where(cf2 => obj.ToString().Contains(_mdc.GetClassName(cf2.ClassID))).Select(cf2 => CreateCustomInspectorObject(obj, ioParent, level, cf2)));
				}

				list.Sort(CompareInspectorObjectNames);
				return list;
			}

			/// <summary>
			/// Gets the properties specified in the meta data cache for the specified object .
			/// </summary>
			protected override PropertyInfo[] GetPropsForObj(object obj)
			{
				var propArray = base.GetPropsForObj(obj);
				var cmObj = obj as ICmObject;
				var props = new List<PropertyInfo>(propArray);
				if (_mdc == null || cmObj == null)
				{
					return propArray;
				}

				RevisePropsList(cmObj, ref props);
				return props.ToArray();
			}

			/// <summary>
			/// Create InspectorObjects for the custom fields for the current object.
			/// </summary>
			private IInspectorObject CreateCustomInspectorObject(object obj, IInspectorObject parentIo, int level, CustomFields cf)
			{
				var to = obj as ICmObject;
				var managedSilDataAccess = _cache.GetManagedSilDataAccess();
				var iValue = string.Empty;
				var fieldId = cf.FieldID;
				IInspectorObject io = null;
				if (obj != null)
				{
					switch (cf.Type)
					{
						case "ITsString":
							var oValue = _cache.DomainDataByFlid.get_StringProp(to.Hvo, fieldId);
							io = base.CreateInspectorObject(null, oValue, obj, parentIo, level);
							iValue = oValue.Text;
							io.HasChildren = false;
							io.DisplayName = cf.Name;
							break;
						case "System.Int32":
							var sValue = _cache.DomainDataByFlid.get_IntProp(to.Hvo, fieldId);
							io = base.CreateInspectorObject(null, sValue, obj, parentIo, level);
							iValue = sValue.ToString();
							io.HasChildren = false;
							io.DisplayName = cf.Name;
							break;
						case "SIL.FieldWorks.Common.FwUtils.GenDate":
							// tried get_TimeProp, get_UnknowbProp, get_Prop
							var genObj = managedSilDataAccess.get_GenDateProp(to.Hvo, fieldId);
							io = base.CreateInspectorObject(null, genObj, obj, parentIo, level);
							iValue = genObj.ToString();
							io.HasChildren = true;
							io.DisplayName = cf.Name;
							break;
						case "LcmReferenceCollection<ICmPossibility>":  // ReferenceCollection
							var count = _cache.DomainDataByFlid.get_VecSize(to.Hvo, fieldId);
							iValue = $"Count = {count}";
							var objects = _cache.GetManagedSilDataAccess().VecProp(to.Hvo, fieldId);
							objects.Initialize();
							io = base.CreateInspectorObject(null, objects, obj, parentIo, level);
							io.HasChildren = count > 0;
							io.DisplayName = $"{cf.Name}RC";
							break;
						case "ICmPossibility":  // ReferenceAtomic
							var rValue = _cache.DomainDataByFlid.get_ObjectProp(to.Hvo, fieldId);
							var posObj = (rValue == 0 ? null : (ICmPossibility)_cache.ServiceLocator.GetObject(rValue));
							io = base.CreateInspectorObject(null, posObj, obj, parentIo, level);
							iValue = (posObj == null ? "null" : posObj.NameHierarchyString);
							io.HasChildren = posObj != null;
							io.DisplayName = $"{cf.Name}RA";
							break;
						case "IStText": //    multi-paragraph text (OA) StText)
							var mValue = _cache.DomainDataByFlid.get_ObjectProp(to.Hvo, fieldId);
							var paraObj = (mValue == 0 ? null : (IStText)_cache.ServiceLocator.GetObject(mValue));
							io = base.CreateInspectorObject(null, paraObj, obj, parentIo, level);
							iValue = (paraObj == null ? "null" : "StText: " + paraObj.Hvo.ToString());
							io.HasChildren = mValue > 0;
							io.DisplayName = $"{cf.Name}OA";
							break;
						default:
							MessageBox.Show($"The type of the custom field is {cf.Type}");
							break;
					}
				}

				io.DisplayType = cf.Type;
				io.DisplayValue = iValue ?? "null";
				io.Flid = cf.FieldID;

				return io;
			}

			/// <summary>
			/// Removes properties from the specified list of properties, those properties the
			/// user has specified he doesn't want to see in the browser.
			/// </summary>
			private void RevisePropsList(ICmObject cmObj, ref List<PropertyInfo> props)
			{
				if (cmObj == null)
				{
					return;
				}
				for (var i = props.Count - 1; i >= 0; i--)
				{
					if (props[i].Name == "Guid")
					{
						continue;
					}
					if (!LCMClassList.IsPropertyDisplayed(cmObj, props[i].Name))
					{
						props.RemoveAt(i);
						continue;
					}
					var work = StripOffTypeChars(props[i].Name);
					var flid = 0;
					if (_mdc.FieldExists(cmObj.ClassID, work, true))
					{
						flid = _mdc.GetFieldId2(cmObj.ClassID, work, true);
					}
					else
					{
						if (m_virtualFlag == false)
						{
							props.RemoveAt(i);
							continue;
						}
					}
					if (m_virtualFlag == false && (flid >= 20000000 && flid < 30000000 || _mdc.get_IsVirtual(flid)))
					{
						props.RemoveAt(i);
					}
				}
			}

			/// <summary>
			/// Gets an inspector object for the specified property info., checking for various
			/// LCM interface types.
			/// </summary>
			protected override IInspectorObject CreateInspectorObject(PropertyInfo pi, object obj, object owningObj, IInspectorObject ioParent, int level)
			{
				var io = base.CreateInspectorObject(pi, obj, owningObj, ioParent, level);
				if (pi == null && io != null)
				{
					io.DisplayType = StripOffLCMNamespace(io.DisplayType);
				}
				else if (pi != null && io == null)
				{
					io.DisplayType = pi.PropertyType.Name;
				}
				else if (pi != null)
				{
					io.DisplayType = (io.DisplayType == "System.__ComObject" ?
					pi.PropertyType.Name : StripOffLCMNamespace(io.DisplayType));
				}
				switch (obj)
				{
					case null:
						return io;
					case char c:
						io.DisplayValue = $"'{io.DisplayValue}'   (U+{(int)c:X4})";
						return io;
					case ILcmVector _:
						{
							var mi = obj.GetType().GetMethod("ToArray");
							try
							{
								var array = mi.Invoke(obj, null) as ICmObject[];
								io.Object = array;
								io.DisplayValue = FormatCountString(array.Length);
								io.HasChildren = (array.Length > 0);
							}
							catch (Exception e)
							{
								io = CreateExceptionInspectorObject(e, obj, pi.Name, level, ioParent);
							}

							break;
						}
					case ICollection<ICmObject> collection:
						{
							var array = collection.ToArray();
							io.Object = array;
							io.DisplayValue = FormatCountString(array.Length);
							io.HasChildren = array.Length > 0;
							break;
						}
				}

				const string fmtAppend = "{0}, {{{1}}}";
				const string fmtReplace = "{0}";
				const string fmtStrReplace = "\"{0}\"";

				switch (obj)
				{
					case ICmFilter cmFilter:
						{
							io.DisplayValue = string.Format(fmtAppend, io.DisplayValue, cmFilter.Name);
							break;
						}
					case IMultiAccessorBase accessorBase:
						{
							io.DisplayValue = string.Format(fmtReplace, accessorBase.AnalysisDefaultWritingSystem.Text);
							break;
						}
					case ITsString tsString:
						{
							io.DisplayValue = string.Format(fmtStrReplace, tsString.Text);
							io.HasChildren = true;
							break;
						}
					case ITsTextProps tsTextProps:
						io.Object = new TextProps(tsTextProps, _cache);
						io.DisplayValue = string.Empty;
						io.HasChildren = true;
						break;
					case IPhNCSegments phNcSegments:
						{
							io.DisplayValue = string.Format(fmtAppend, io.DisplayValue, phNcSegments.Name.AnalysisDefaultWritingSystem.Text);
							break;
						}
					case IPhEnvironment environment:
						{
							io.DisplayValue = $"{io.DisplayValue}, {{Name: {environment.Name.AnalysisDefaultWritingSystem.Text}, Pattern: {environment.StringRepresentation.Text}}}";
							break;
						}
					case IMoEndoCompound endoCompound:
						{
							io.DisplayValue = string.Format(fmtAppend, io.DisplayValue, endoCompound.Name.AnalysisDefaultWritingSystem.Text);
							break;
						}
					default:
						{
							if (obj.GetType().GetInterface("IRepository`1") != null)
							{
								io.DisplayName = io.DisplayType;
							}
							break;
						}
				}

				return io;
			}

			#endregion

			/// <summary />
			private string StripOffLCMNamespace(string type)
			{
				if (string.IsNullOrEmpty(type))
				{
					return string.Empty;
				}
				if (!type.StartsWith("SIL.LCModel"))
				{
					return type;
				}
				type = type.Replace("SIL.LCModel.Infrastructure.Impl.", string.Empty);
				type = type.Replace("SIL.LCModel.Infrastructure.", string.Empty);
				type = type.Replace("SIL.LCModel.DomainImpl.", string.Empty);
				type = type.Replace("SIL.LCModel.", string.Empty);

				return CleanupGenericListType(type);
			}

			/// <summary>
			/// Gets a list of all the instances in the specified repository.
			/// </summary>
			private static IEnumerable<object> GetRepositoryInstances(object repository)
			{
				var list = new List<object>();

				try
				{
					// Get an object that represents all the repository's collection of instances
					var repoInstances = (repository.GetType().GetMethods().Where(mi => mi.Name == "AllInstances").Select(mi => mi.Invoke(repository, null))).FirstOrDefault();
					if (repoInstances == null)
					{
						throw new MissingMethodException($"Repository {repository.GetType().Name} is missing 'AllInstances' method.");
					}
					if (!(repoInstances is IEnumerable ienum))
					{
						throw new NullReferenceException($"Repository {repository.GetType().Name} is not an IEnumerable");
					}
					var enumerator = ienum.GetEnumerator();
					while (enumerator.MoveNext())
					{
						list.Add(enumerator.Current);
					}
				}
				catch (Exception e)
				{
					list.Add(e);
				}

				return list;
			}

			/// <summary>
			/// Returns the object number only (as a string).
			/// </summary>
			private static string GetObjectOnly(string objectName)
			{
				var idx = objectName.IndexOf(":");
				return idx <= 0 ? string.Empty : objectName.Substring(idx + 1);
			}

			/// <summary />
			private sealed class TsStringRunInfo
			{
				/// <summary />
				internal string Text { get; }

				/// <summary />
				internal TsStringRunInfo(int irun, ITsString tss)
				{
					Text = "\"" + (tss.get_RunText(irun) ?? string.Empty) + "\"";
				}

				/// <summary>
				/// Returns a <see cref="T:System.String"/> that represents this instance.
				/// </summary>
				public override string ToString()
				{
					return Text ?? string.Empty;
				}
			}

			/// <summary />
			private sealed class TextProps
			{
				/// <summary />
				private int StrPropCount { get; set; }
				/// <summary />
				private int IntPropCount { get; set; }
				/// <summary />
				private int IchMin { get; }
				/// <summary />
				private int IchLim { get; }
				/// <summary />
				private TextStrPropInfo[] StrProps { get; set; }
				/// <summary />
				private TextIntPropInfo[] IntProps { get; set; }

				/// <summary />
				internal TextProps(ITsTextProps ttp, LcmCache cache)
				{
					StrPropCount = ttp.StrPropCount;
					IntPropCount = ttp.IntPropCount;
					SetProps(ttp, cache);
				}

				/// <summary>
				/// Sets the int and string properties.
				/// </summary>
				private void SetProps(ITsTextProps ttp, LcmCache cache)
				{
					// Get the string properties.
					StrPropCount = ttp.StrPropCount;
					StrProps = new TextStrPropInfo[StrPropCount];
					for (var i = 0; i < StrPropCount; i++)
					{
						StrProps[i] = new TextStrPropInfo(ttp, i);
					}

					// Get the integer properties.
					IntPropCount = ttp.IntPropCount;
					IntProps = new TextIntPropInfo[IntPropCount];
					for (var i = 0; i < IntPropCount; i++)
					{
						IntProps[i] = new TextIntPropInfo(ttp, i, cache);
					}
				}

				/// <summary>
				/// Returns a <see cref="T:System.String"/> that represents this instance.
				/// </summary>
				public override string ToString()
				{
					return $"{{IchMin={IchMin}, IchLim={IchLim}, StrPropCount={StrPropCount}, IntPropCount={IntPropCount}}}";
				}

				/// <summary />
				private sealed class TextIntPropInfo
				{
					private readonly string _toStringValue;

					/// <summary />
					internal TextIntPropInfo(ITsTextProps props, int iprop, LcmCache cache)
					{
						var value = props.GetIntProp(iprop, out var tpt, out _);
						var fwTextPropType = (FwTextPropType)tpt;

						_toStringValue = $"{value}  ({fwTextPropType})";

						if (tpt != (int)FwTextPropType.ktptWs)
						{
							return;
						}
						var ws = cache.ServiceLocator.WritingSystemManager.Get(value);
						_toStringValue += $"  {{{ws}}}";
					}

					/// <summary>
					/// Returns a <see cref="T:System.String"/> that represents this instance.
					/// </summary>
					public override string ToString()
					{
						return _toStringValue;
					}
				}

				/// <summary />
				private sealed class TextStrPropInfo
				{
					private readonly string _toStringValue;

					/// <summary />
					internal TextStrPropInfo(ITsTextProps props, int iprop)
					{
						_toStringValue = $"{props.GetStrProp(iprop, out var tpt)}  ({(FwTextPropType)tpt})";
					}

					/// <summary>
					/// Returns a <see cref="T:System.String"/> that represents this instance.
					/// </summary>
					public override string ToString()
					{
						return _toStringValue;
					}
				}
			}
		}
	}
}
