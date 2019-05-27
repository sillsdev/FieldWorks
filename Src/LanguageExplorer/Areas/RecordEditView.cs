// Copyright (c) 2003-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Printing;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Xml.XPath;
using LanguageExplorer.Controls.DetailControls;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.Utils;
using SIL.Xml;

namespace LanguageExplorer.Areas
{
	/// <summary>
	/// RecordEditView implements a RecordView (view showing one object at a time from a sequence)
	/// in which the single object is displayed using a DataTree configured using XDEs.
	/// It requires that the XML configuration node have the attribute 'templatePath' (in addition
	/// to 'field' as required by RecordView to specify the list of objects). This tells it where
	/// to start looking for XDEs. This path is relative to the FW root directory (DistFiles in
	/// a development system), e.g., "IText\XDEs".
	/// This version uses the DetailControls version of DataTree, and will eventually replace the
	/// original.
	/// </summary>
	internal class RecordEditView : RecordView, IVwNotifyChange, IFocusablePanePortion
	{
		#region Data members

		/// <summary>
		/// Document for slice filters.
		/// </summary>
		private XDocument m_sliceFilterDocument;
		private UiWidgetController _uiWidgetController;

		/// <summary>
		/// Mode string for DataTree to use at top level.
		/// </summary>
		protected string m_rootMode;
		/// <summary>
		/// indicates that when descendant objects are displayed they should be displayed within the context
		/// of its root object
		/// </summary>
		private bool m_showDescendantInRoot;
		private ImageList buttonImages;
		protected Panel m_panel;
		private IContainer components;
		private string m_layoutName;
		private string m_layoutChoiceField;
		private string m_titleField;
		private string m_titleStr;
		private string m_printLayout;

		#endregion // Data members

		#region Construction and Removal

		internal RecordEditView(XElement configurationParametersElement, XDocument sliceFilterDocument, LcmCache cache, IRecordList recordList, DataTree dataTree, UiWidgetController uiWidgetController)
			: base(configurationParametersElement, cache, recordList)
		{
			m_sliceFilterDocument = sliceFilterDocument;
			_uiWidgetController = uiWidgetController;
			// This must be called before InitializeComponent()
			MyDataTree = dataTree;
			MyDataTree.CurrentSliceChanged += DataTreeCurrentSliceChanged;
			InitializeComponent();
			AccNameDefault = "RecordEditView";
		}

		#region Overrides of MainUserControl
		/// <inheritdoc />
		internal override void RegisterUiWidgets(bool shouldRegister)
		{
			if (shouldRegister)
			{
				var userController = new UserControlUiWidgetParameterObject(this);
				// Add handler stuff from this class and possibly from subclasses.
				userController.MenuItemsForUserControl[MainMenu.File].Add(Command.CmdPrint, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(PrintMenu_Click, () => CanShowPrintMenu));
				_uiWidgetController.AddHandlers(userController);
			}
			else
			{
				_uiWidgetController.RemoveUserControlHandlers(this);
			}
		}
		#endregion


		#region Overrides of ViewBase

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		public override void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			base.InitializeFlexComponent(flexComponentParameters);

			MyDataTree.InitializeFlexComponent(flexComponentParameters);
		}

		#endregion

		/// <summary>
		/// About to show, so finish initializing.
		/// </summary>
		public void FinishInitialization()
		{
			InitBase();
			m_showDescendantInRoot = XmlUtils.GetOptionalBooleanAttributeValue(m_configurationParametersElement, "showDescendantInRoot", false);
			// retrieve persisted record list index and set it.
			var idx = PropertyTable.GetValue(MyRecordList.PersistedIndexProperty, -1, SettingsGroup.LocalSettings);
			var lim = MyRecordList.ListSize;
			if (idx >= 0 && idx < lim)
			{
				var idxOld = MyRecordList.CurrentIndex;
				try
				{
					MyRecordList.JumpToIndex(idx);
				}
				catch
				{
					if (lim > idxOld && lim > 0)
					{
						MyRecordList.JumpToIndex(idxOld >= 0 ? idxOld : 0);
					}
				}
			}
			// If possible make it use the style sheet appropriate for its main window.
			MyDataTree.StyleSheet = FwUtils.StyleSheetFromPropertyTable(PropertyTable);
			ShowRecord();
			m_fullyInitialized = true;
		}

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				components?.Dispose();
				_uiWidgetController.RemoveUserControlHandlers(this);
				if (MyDataTree != null)
				{
					MyDataTree.CurrentSliceChanged -= DataTreeCurrentSliceChanged;
					MyDataTree.Dispose();
				}
				if (!string.IsNullOrEmpty(m_titleField))
				{
					Cache.DomainDataByFlid.RemoveNotification(this);
				}
			}
			MyDataTree = null;

			base.Dispose(disposing);
		}

		#endregion // Construction and Removal

		#region Message Handlers
		protected override void RecordList_RecordChanged_Handler(object sender, RecordNavigationEventArgs e)
		{
			// Don't call base, since we don't want that behavior.
			if (!m_fullyInitialized)
			{
				return;
			}

#if RANDYTODO
			// TODO: As of 21JUL17 nobody cares about that 'propName' changing, so skip the broadcast.
#endif
			// persist record lists's CurrentIndex in a db specific way
			var propName = MyRecordList.PersistedIndexProperty;
			PropertyTable.SetProperty(propName, MyRecordList.CurrentIndex, true, settingsGroup: SettingsGroup.LocalSettings);
			var window = PropertyTable.GetValue<IFwMainWnd>(FwUtils.window);
			using (new IdleProcessingHelper(window))
			{
				ShowRecord(e.RecordNavigationInfo);
			}
		}

		public bool OnConsideringClosing(object argument, CancelEventArgs args)
		{
			args.Cancel = !PrepareToGoAway();
			return args.Cancel; // if we want to cancel, others don't need to be asked.
		}

		/// <summary>
		/// From IMainContentControl
		/// </summary>
		/// <returns>true if ok to go away</returns>
		public override bool PrepareToGoAway()
		{
			MyDataTree?.PrepareToGoAway();
			return base.PrepareToGoAway();
		}

		private void DataTreeCurrentSliceChanged(object sender, CurrentSliceChangedEventArgs e)
		{
			if (!m_showDescendantInRoot)
			{
				return;
			}
			if (MyDataTree.Descendant != null && MyRecordList.CurrentObject != MyDataTree.Descendant)
			{
				// if the user has clicked on a different descendant's slice, update the currently
				// selected record (we want to keep the browse view in sync), but do not change the
				// focus
				MyRecordList.JumpToRecord(MyDataTree.Descendant.Hvo, true);
			}
		}

		#endregion // Message Handlers

		#region Other methods

		protected override void SetInfoBarText()
		{
			if (m_informationBar == null)
			{
				return;
			}
			// See if we have an AlternativeTitle string table id for an alternate title.
			string titleStr = null;
			if (!string.IsNullOrEmpty(m_titleStr))
			{
				titleStr = m_titleStr;
			}
			else if (!string.IsNullOrEmpty(m_titleField))
			{
				var curObj = MyRecordList.CurrentObject;
				if (curObj != null)
				{
					var flid = Cache.MetaDataCacheAccessor.GetFieldId2(curObj.ClassID, m_titleField, true);
					var hvo = Cache.DomainDataByFlid.get_ObjectProp(curObj.Hvo, flid);
					if (hvo != 0)
					{
						var titleObj = Cache.ServiceLocator.GetObject(hvo);
						titleStr = titleObj.ShortName;
					}
				}
			}

			if (!string.IsNullOrEmpty(titleStr))
			{
				((IPaneBar)m_informationBar).Text = titleStr;
			}
			else
			{
				base.SetInfoBarText();
			}
		}

		/// <summary>
		/// Schedules the record to be shown when the application is idle.
		/// </summary>
		protected override void ShowRecord(RecordNavigationInfo rni)
		{
			if (!rni.SkipShowRecord)
			{
#if RANDYTODO
				m_mediator.IdleQueue.Add(IdleQueuePriority.High, ShowRecordOnIdle, rni);
#else
				ShowRecordOnIdle(rni);
#endif
			}
		}

		/// <summary>
		/// Shows the record.
		/// </summary>
		protected override void ShowRecord()
		{
			ShowRecord(new RecordNavigationInfo(MyRecordList, MyRecordList.SuppressSaveOnChangeRecord, false, false));
		}

		/// <summary>
		/// Shows the record on idle. This is where the record is actually shown.
		/// </summary>
		void ShowRecordOnIdle(RecordNavigationInfo rni)
		{
			if (IsDisposed)
			{
				return;
			}
			base.ShowRecord();
#if DEBUG
			var msStart = Environment.TickCount;
			Debug.Assert(MyDataTree != null);
#endif
			var oldSuppressSaveOnChangeRecord = MyRecordList.SuppressSaveOnChangeRecord;
			MyRecordList.SuppressSaveOnChangeRecord = rni.SuppressSaveOnChangeRecord;
			PrepCacheForNewRecord();
			MyRecordList.SuppressSaveOnChangeRecord = oldSuppressSaveOnChangeRecord;
			if (MyRecordList.CurrentObject == null || MyRecordList.SuspendLoadingRecordUntilOnJumpToRecord)
			{
				MyDataTree.Hide();
				MyDataTree.Reset(); // in case user deleted the object it was based upon.
				return;
			}
			try
			{
				MyDataTree.Show();
				using (new WaitCursor(this))
				{
					// Enhance: Maybe do something here to allow changing the templates without the starting the application.
					var obj = MyRecordList.CurrentObject;

					if (m_showDescendantInRoot)
					{
						// find the root object of the current object
						while (obj.Owner != MyRecordList.OwningObject)
						{
							obj = obj.Owner;
						}
					}
					MyDataTree.ShowObject(obj, m_layoutName, m_layoutChoiceField, MyRecordList.CurrentObject, ShouldSuppressFocusChange(rni));
				}
			}
			catch (Exception error)
			{
				//don't really need to make the program stop just because we could not show this record.
				var app = PropertyTable.GetValue<IApp>(LanguageExplorerConstants.App);
				ErrorReporter.ReportException(error, app.SettingsKey, app.SupportEmailAddress, null, false);
			}
#if DEBUG
			var msEnd = Environment.TickCount;
			var traceSwitch = new TraceSwitch("Works_Timing", "Used for diagnostic timing output", "Off");
			Debug.WriteLineIf(traceSwitch.TraceInfo, "ShowRecord took " + (msEnd - msStart) + " ms", traceSwitch.DisplayName);
#endif
		}

		/// <summary>
		/// If this is not the focused pane in a multipane suppress, or if the navigation info requested
		/// a suppression of the focus change then return true (suppress)
		/// </summary>
		private bool ShouldSuppressFocusChange(RecordNavigationInfo rni)
		{
			return !IsFocusedPane || rni.SuppressFocusChange;
		}

		/// <summary>
		/// Base method saves any time you switch between records.
		/// </summary>
		protected virtual void PrepCacheForNewRecord()
		{
			MyRecordList.SaveOnChangeRecord();
		}

		/// <summary>
		/// Read in the parameters to determine which collection we are editing.
		/// </summary>
		protected override void ReadParameters()
		{
			base.ReadParameters();
			m_layoutName = XmlUtils.GetOptionalAttributeValue(m_configurationParametersElement, "layout");
			m_layoutChoiceField = XmlUtils.GetOptionalAttributeValue(m_configurationParametersElement, "layoutChoiceField");
			m_titleField = XmlUtils.GetOptionalAttributeValue(m_configurationParametersElement, "titleField");
			if (!string.IsNullOrEmpty(m_titleField))
			{
				Cache.DomainDataByFlid.AddNotification(this);
			}
			var titleId = XmlUtils.GetOptionalAttributeValue(m_configurationParametersElement, "altTitleId");
			if (titleId != null)
			{
				m_titleStr = StringTable.Table.GetString(titleId, "AlternativeTitles");
			}
			m_printLayout = XmlUtils.GetOptionalAttributeValue(m_configurationParametersElement, "printLayout");
		}

		protected override void SetupDataContext()
		{
			Debug.Assert(m_configurationParametersElement != null);

			base.SetupDataContext();
			MyDataTree.PersistenceProvder = PersistenceProviderFactory.CreatePersistenceProvider(PropertyTable);
			MyRecordList.UpdateRecordTreeBarIfNeeded();
			MyDataTree.SliceFilter = m_sliceFilterDocument != null ? new SliceFilter(m_sliceFilterDocument) : new SliceFilter();
			// Already done: m_dataEntryForm.Dock = DockStyle.Fill;
#if RANDYTODO
			m_dataEntryForm.SmallImages = PropertyTable.GetValue<ImageList.ImageCollection>("smallImages");
#endif
			var sDatabase = Cache.ProjectId.Name;
			MyDataTree.Initialize(Cache, true, Inventory.GetInventory("layouts", sDatabase), Inventory.GetInventory("parts", sDatabase));
			// Already done. m_dataEntryForm.InitializeFlexComponent(PropertyTable, Publisher, Subscriber);
			if (MyDataTree.AccessibilityObject != null)
			{
				MyDataTree.AccessibilityObject.Name = "RecordEditView.DataTree";
			}
			Controls.Clear();
			Controls.Add(m_informationBar);
			Controls.Add(MyDataTree);
			SetInfoBarText();
			MyDataTree.BringToFront();
		}

		#endregion // Other methods

		/// <summary>
		/// get our DataTree for testing
		/// </summary>
		public DataTree MyDataTree { get; protected set; }

		#region ICtrlTabProvider implementation

		public override Control PopulateCtrlTabTargetCandidateList(List<Control> targetCandidates)
		{
			Guard.AgainstNull(targetCandidates, nameof(targetCandidates));

			// when switching panes, we want to give the focus to the CurrentSlice(if any)
			if (MyDataTree?.CurrentSlice != null)
			{
				targetCandidates.Add(MyDataTree.CurrentSlice);
				return MyDataTree.CurrentSlice.ContainsFocus ? MyDataTree.CurrentSlice : null;
			}
			return base.PopulateCtrlTabTargetCandidateList(targetCandidates);
		}

		#endregion  ICtrlTabProvider implementation

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(RecordEditView));
			this.buttonImages = new System.Windows.Forms.ImageList(this.components);
			this.m_panel = new System.Windows.Forms.Panel();
			this.MyDataTree.AccessibilityObject.Name = "RecordEditView.DataTree";
			this.SuspendLayout();
			//
			// m_informationBar
			//
			//this.m_informationBar.DockPadding.All = 5;
			//this.m_informationBar.Name = "m_informationBar";
			//
			// buttonImages
			//
			this.buttonImages.ImageSize = new System.Drawing.Size(16, 16);
			this.buttonImages.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("buttonImages.ImageStream")));
			this.buttonImages.TransparentColor = System.Drawing.Color.Fuchsia;
			//
			// m_panel
			//
			this.m_panel.Controls.Add(this.MyDataTree);
			this.m_panel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_panel.Location = new System.Drawing.Point(0, 0);
			this.m_panel.Name = "m_panel";
			this.m_panel.Size = new System.Drawing.Size(752, 150);
			this.m_panel.TabIndex = 2;
			this.m_panel.AccessibilityObject.Name = "Panel";
			//
			// m_dataEntryForm
			//
			this.MyDataTree.Dock = System.Windows.Forms.DockStyle.Fill;
			this.MyDataTree.Location = new System.Drawing.Point(0, 0);
			this.MyDataTree.Name = "MyDataTree";
			this.MyDataTree.PersistenceProvder = null;
			this.MyDataTree.Size = new System.Drawing.Size(752, 150);
			this.MyDataTree.SliceFilter = null;
			this.MyDataTree.StyleSheet = null;
			this.MyDataTree.TabIndex = 3;
			//
			// RecordEditView
			//
			//this.Controls.Add(this.m_informationBar);
			this.Controls.Add(this.m_panel);
			this.Name = "RecordEditView";
			this.Controls.SetChildIndex(this.m_panel, 0);
			//this.Controls.SetChildIndex(this.m_informationBar, 0);
			this.ResumeLayout(false);

		}
		#endregion

		public void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			// if the title field property changed, update the pane bar text
			if (string.IsNullOrEmpty(m_titleField))
			{
				return;
			}
			var curObj = MyRecordList.CurrentObject;
			if (curObj == null)
			{
				return;
			}
			var flid = Cache.MetaDataCacheAccessor.GetFieldId2(curObj.ClassID, m_titleField, true);
			if (hvo == curObj.Hvo && tag == flid)
			{
				SetInfoBarText();
			}
		}

		#region Print methods

		private static Tuple<bool, bool> CanShowPrintMenu => new Tuple<bool, bool>(true, true);

		private void PrintMenu_Click(object sender, EventArgs e)
		{
			if (!ContainsFocus || m_printLayout == null || MyRecordList.CurrentObject == null)
			{
				return; // Don't bother; this edit view does not specify a print layout, or there's nothing to print.
			}
			var area = PropertyTable.GetValue<string>(AreaServices.AreaChoice);
			string toolId;
			switch (area)
			{
				case AreaServices.NotebookAreaMachineName:
					toolId = AreaServices.NotebookDocumentToolMachineName;
					break;
				case AreaServices.LexiconAreaMachineName:
					toolId = AreaServices.LexiconDictionaryMachineName;
					break;
				default:
					return;
			}
			var toolInXmlConfig = FindToolInXMLConfig(toolId);
			if (toolInXmlConfig == null)
			{
				return;
			}
			var innerControlNode = GetToolInnerControlNodeWithRightLayout(toolInXmlConfig);
			if (innerControlNode == null)
			{
				return;
			}
			using (var docView = CreateDocView(innerControlNode))
			{
				using (var pd = new PrintDocument())
				using (var dlg = new PrintDialog())
				{
					dlg.Document = pd;
					dlg.AllowSomePages = true;
					dlg.AllowSelection = false;
					dlg.PrinterSettings.FromPage = 1;
					dlg.PrinterSettings.ToPage = 1;
					if (dlg.ShowDialog() != DialogResult.OK)
					{
						return;
					}
					// REVIEW: .NET does not appear to handle the collation setting correctly
					// so for now, we do not support non-collated printing.  Forcing the setting
					// seems to work fine.
					dlg.Document.PrinterSettings.Collate = true;
					docView.PrintFromDetail(pd, MyRecordList.CurrentObject.Hvo);
				}
			}
		}

		private XElement GetToolInnerControlNodeWithRightLayout(XElement docViewConfig)
		{
			var paramNode = docViewConfig.XPathSelectElement("control//parameters[@layout = \"" + m_printLayout + "\"]");
			return paramNode?.Parent;
		}

		private XElement FindToolInXMLConfig(string docToolValue)
		{
			// At this point m_configurationParameters holds the RecordEditView parameter node.
			// We need to find the tool that has a value attribute matching our input
			// parameter (docToolValue).
#if RANDYTODO
			var path = ".//tools/tool[@value = \""+docToolValue+"\"]";
			return m_configurationParametersElement.Document.XPathSelectElement(path);
#else
			return null; // TODO: Find it another way, since there is no tool element now.
#endif
		}

		private XmlDocView CreateDocView(XElement parentConfigNode)
		{
			var docView = (XmlDocView)DynamicLoader.CreateObjectUsingLoaderNode(parentConfigNode);
			// TODO: Not right yet!
			docView.InitializeFlexComponent(new FlexComponentParameters(PropertyTable, Publisher, Subscriber));
			return docView;
		}

		#endregion

		public bool IsFocusedPane
		{
			get;
			set;
		}
	}
}