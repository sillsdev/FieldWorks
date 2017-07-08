// Copyright (c) 2003-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Diagnostics;
using System.Drawing.Printing;
using System.Windows.Forms;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.LCModel;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Framework;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.Utils;
using SIL.Xml;

namespace SIL.FieldWorks.XWorks
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
	public class RecordEditView : RecordView, IVwNotifyChange, IFocusablePanePortion
	{
		#region Data members

		/// <summary>
		/// Document for slice filters.
		/// </summary>
		private XDocument m_sliceFilterDocument;
		/// <summary>
		/// Mode string for DataTree to use at top level.
		/// </summary>
		protected string m_rootMode;
		/// <summary>
		/// handles creating the context menus for the data tree and funneling commands to the data tree.
		/// </summary>
		private DTMenuHandler m_menuHandler;

		/// <summary>
		/// indicates that when descendant objects are displayed they should be displayed within the context
		/// of its root object
		/// </summary>
		private bool m_showDescendantInRoot;
		private ImageList buttonImages;
		protected Panel m_panel;
		protected DataTree m_dataEntryForm;
		private IContainer components;
		private string m_layoutName;
		private string m_layoutChoiceField;
		private string m_titleField;
		private string m_titleStr;
		private string m_printLayout;

		//// <summary>
		//// used to associate menu commands with the slice that sent them
		//// </summary>
		//protected Slice m_sourceOfMenuCommandSlice=null;

		#endregion // Data members

		#region Construction and Removal

		/// <summary>
		/// Initializes a new instance of the <see cref="RecordEditView"/> class.
		/// </summary>
		public RecordEditView(XElement configurationParametersElement, XDocument sliceFilterDocument, LcmCache cache, RecordClerk recordClerk, DTMenuHandler dataTreeMenuHandler)
			: this(configurationParametersElement, sliceFilterDocument, cache, recordClerk, dataTreeMenuHandler, new DataTree())
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RecordEditView"/> class.
		/// </summary>
		public RecordEditView(XElement configurationParametersElement, XDocument sliceFilterDocument, LcmCache cache, RecordClerk recordClerk)
			: this(configurationParametersElement, sliceFilterDocument, cache, recordClerk, new DTMenuHandler(), new DataTree())
		{
		}

		protected RecordEditView(XElement configurationParametersElement, XDocument sliceFilterDocument, LcmCache cache, RecordClerk recordClerk, DTMenuHandler dataTreeMenuHandler, DataTree dataEntryForm)
			: base(configurationParametersElement, cache, recordClerk)
		{
			m_menuHandler = dataTreeMenuHandler;
			m_sliceFilterDocument = sliceFilterDocument;
			// This must be called before InitializeComponent()
			m_dataEntryForm = dataEntryForm;
			m_dataEntryForm.CurrentSliceChanged += m_dataEntryForm_CurrentSliceChanged;

			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			AccNameDefault = "RecordEditView";		// default accessibility name
		}

		#region Overrides of XWorksViewBase

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		public override void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			base.InitializeFlexComponent(flexComponentParameters);

			m_dataEntryForm.InitializeFlexComponent(flexComponentParameters);
		}

		#endregion

		/// <summary>
		/// About to show, so finish initializing.
		/// </summary>
		public void FinishInitialization()
		{
			InitBase();

			m_showDescendantInRoot = XmlUtils.GetOptionalBooleanAttributeValue(m_configurationParametersElement, "showDescendantInRoot", false);

			// retrieve persisted clerk index and set it.
			int idx = PropertyTable.GetValue(Clerk.PersistedIndexProperty, SettingsGroup.LocalSettings, -1);
			int lim = Clerk.ListSize;
			if (idx >= 0 && idx < lim)
			{
				int idxOld = Clerk.CurrentIndex;
				try
				{
					Clerk.JumpToIndex(idx);
				}
				catch
				{
					if (lim > idxOld && lim > 0)
						Clerk.JumpToIndex(idxOld >= 0 ? idxOld : 0);
				}
			}

			// If possible make it use the style sheet appropriate for its main window.
			m_dataEntryForm.StyleSheet = FontHeightAdjuster.StyleSheetFromPropertyTable(PropertyTable);
			ShowRecord();
			m_fullyInitialized = true;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged
		/// resources; <c>false</c> to release only unmanaged resources.
		/// </param>
		/// -----------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				if (components != null)
					components.Dispose();
				if (m_dataEntryForm != null)
				{
					m_dataEntryForm.CurrentSliceChanged -= m_dataEntryForm_CurrentSliceChanged;
					m_dataEntryForm.Dispose();
				}
				if (!string.IsNullOrEmpty(m_titleField))
					Cache.DomainDataByFlid.RemoveNotification(this);
			}
			m_dataEntryForm = null;

			base.Dispose(disposing);
		}

		#endregion // Construction and Removal

		#region Message Handlers

		/// <summary />
		public override void RecordNavigation_Message_Handler(object newValue)
		{
			// Do not call base since we don't want the superclass behavior.
			if (!m_fullyInitialized)
				return;

			// persist Clerk's CurrentIndex in a db specific way
			string propName = Clerk.PersistedIndexProperty;
			PropertyTable.SetProperty(propName, Clerk.CurrentIndex, SettingsGroup.LocalSettings, true, true);
			var window = PropertyTable.GetValue<IFwMainWnd>("window");

			try
			{
				window.SuspendIdleProcessing();
				ShowRecord(newValue as RecordNavigationInfo);
			}
			finally
			{
				window.ResumeIdleProcessing();
			}
		}

		public bool OnConsideringClosing(object argument, CancelEventArgs args)
		{
			CheckDisposed();

			args.Cancel = !PrepareToGoAway();
			return args.Cancel; // if we want to cancel, others don't need to be asked.
		}

		/// <summary>
		/// From IMainContentControl
		/// </summary>
		/// <returns>true if ok to go away</returns>
		public override bool PrepareToGoAway()
		{
			CheckDisposed();

			if (m_dataEntryForm != null)
				m_dataEntryForm.PrepareToGoAway();
			return base.PrepareToGoAway();
		}

		private void m_dataEntryForm_CurrentSliceChanged(object sender, EventArgs e)
		{
			if (!m_showDescendantInRoot)
				return;

			if (m_dataEntryForm.Descendant != null && Clerk.CurrentObject != m_dataEntryForm.Descendant)
				// if the user has clicked on a different descendant's slice, update the currently
				// selected record (we want to keep the browse view in sync), but do not change the
				// focus
				Clerk.JumpToRecord(m_dataEntryForm.Descendant.Hvo, true);
		}

		#endregion // Message Handlers

		#region Other methods

		protected override void SetInfoBarText()
		{
			if (m_informationBar == null)
				return;

			// See if we have an AlternativeTitle string table id for an alternate title.
			string titleStr = null;
			if (!string.IsNullOrEmpty(m_titleStr))
			{
				titleStr = m_titleStr;
			}
			else if (!string.IsNullOrEmpty(m_titleField))
			{
				ICmObject curObj = Clerk.CurrentObject;
				if (curObj != null)
				{
					int flid = Cache.MetaDataCacheAccessor.GetFieldId2(curObj.ClassID, m_titleField, true);
					int hvo = Cache.DomainDataByFlid.get_ObjectProp(curObj.Hvo, flid);
					if (hvo != 0)
					{
						ICmObject titleObj = Cache.ServiceLocator.GetObject(hvo);
						titleStr = titleObj.ShortName;
					}
				}
			}

			if (!string.IsNullOrEmpty(titleStr))
				((IPaneBar) m_informationBar).Text = titleStr;
			else
				base.SetInfoBarText();
		}

		/// <summary>
		/// Schedules the record to be shown when the application is idle.
		/// </summary>
		/// <param name="rni">The record navigation info.</param>
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
			ShowRecord(new RecordNavigationInfo(Clerk, Clerk.SuppressSaveOnChangeRecord, false, false));
		}

		/// <summary>
		/// Shows the record on idle. This is where the record is actually shown.
		/// </summary>
		void ShowRecordOnIdle(RecordNavigationInfo rni)
		{
			if (IsDisposed)
				return;

			base.ShowRecord();
#if DEBUG
			int msStart = Environment.TickCount;
			Debug.Assert(m_dataEntryForm != null);
#endif

			bool oldSuppressSaveOnChangeRecord = Clerk.SuppressSaveOnChangeRecord;
			Clerk.SuppressSaveOnChangeRecord = rni.SuppressSaveOnChangeRecord;
			PrepCacheForNewRecord();
			Clerk.SuppressSaveOnChangeRecord = oldSuppressSaveOnChangeRecord;

			if (Clerk.CurrentObject == null || Clerk.SuspendLoadingRecordUntilOnJumpToRecord)
			{
				m_dataEntryForm.Hide();
				m_dataEntryForm.Reset();	// in case user deleted the object it was based upon.
				return;
			}
			try
			{
				m_dataEntryForm.Show();
				using (new WaitCursor(this))
				{
				// Enhance: Maybe do something here to allow changing the templates without the starting the application.
				ICmObject obj = Clerk.CurrentObject;

				if (m_showDescendantInRoot)
				{
					// find the root object of the current object
					while (obj.Owner != Clerk.OwningObject)
						obj = obj.Owner;
				}

				m_dataEntryForm.ShowObject(obj, m_layoutName, m_layoutChoiceField, Clerk.CurrentObject, ShouldSuppressFocusChange(rni));
			}
			}
			catch (Exception error)
			{
				//don't really need to make the program stop just because we could not show this record.
				IApp app = PropertyTable.GetValue<IApp>("App");
				ErrorReporter.ReportException(error, app.SettingsKey, app.SupportEmailAddress,
					null, false);
			}
#if DEBUG
			int msEnd = Environment.TickCount;
			Debug.WriteLineIf(RuntimeSwitches.RecordTimingSwitch.TraceInfo, "ShowRecord took " + (msEnd - msStart) + " ms", RuntimeSwitches.RecordTimingSwitch.DisplayName);
#endif
		}

		/// <summary>
		/// If this is not the focused pane in a multipane suppress, or if the navigation info requested
		/// a suppression of the focus change then return true (suppress)
		/// </summary>
		/// <param name="rni"></param>
		/// <returns></returns>
		private bool ShouldSuppressFocusChange(RecordNavigationInfo rni)
		{
			return !IsFocusedPane || rni.SuppressFocusChange;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Base method saves any time you switch between records.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void PrepCacheForNewRecord()
		{
			Clerk.SaveOnChangeRecord();
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
				Cache.DomainDataByFlid.AddNotification(this);
			string titleId = XmlUtils.GetOptionalAttributeValue(m_configurationParametersElement, "altTitleId");
			if (titleId != null)
				m_titleStr = StringTable.Table.GetString(titleId, "AlternativeTitles");
			m_printLayout = XmlUtils.GetOptionalAttributeValue(m_configurationParametersElement, "printLayout");
		}

		protected override void SetupDataContext()
		{
			Debug.Assert(m_configurationParametersElement != null);

			base.SetupDataContext();

			//this will normally be the same name as the view, e.g. "basicEdit". This plus the name of the vector
			//should give us a unique context for the dataTree control parameters.

			string persistContext = XmlUtils.GetOptionalAttributeValue(m_configurationParametersElement, "persistContext");

			if (persistContext !="")
				persistContext=Clerk.Id+"."+persistContext+".DataTree";
			else
				persistContext=Clerk.Id+".DataTree";

			m_dataEntryForm.PersistenceProvder = PersistenceProviderFactory.CreatePersistenceProvider(PropertyTable);

			Clerk.UpdateRecordTreeBarIfNeeded();
			m_dataEntryForm.SliceFilter = m_sliceFilterDocument != null ? new SliceFilter(m_sliceFilterDocument) : new SliceFilter();
			// Already done: m_dataEntryForm.Dock = DockStyle.Fill;
#if RANDYTODO
			m_dataEntryForm.SmallImages = PropertyTable.GetValue<ImageList.ImageCollection>("smallImages");
#endif
			string sDatabase = Cache.ProjectId.Name;
			m_dataEntryForm.Initialize(Cache, true, Inventory.GetInventory("layouts", sDatabase),
				Inventory.GetInventory("parts", sDatabase));
			// Already done. m_dataEntryForm.InitializeFlexComponent(PropertyTable, Publisher, Subscriber);
			if (m_dataEntryForm.AccessibilityObject != null)
				m_dataEntryForm.AccessibilityObject.Name = "RecordEditView.DataTree";

			// set up the context menu, overriding the automatic menu creator/handler
			m_dataEntryForm.SetContextMenuHandler(m_menuHandler.ShowSliceContextMenu);

			Controls.Clear();
			Controls.Add(m_informationBar);
			Controls.Add(m_dataEntryForm);
			SetInfoBarText();
			m_dataEntryForm.BringToFront();
		}

		#endregion // Other methods

		/// <summary>
		/// get our DataTree for testing
		/// </summary>
		public DataTree DatTree
		{
			get
			{
				CheckDisposed();

				return m_dataEntryForm;
			}
		}

		#region ICtrlTabProvider implementation

		public override Control PopulateCtrlTabTargetCandidateList(List<Control> targetCandidates)
		{
			if (targetCandidates == null)
				throw new ArgumentNullException("targetCandidates");

			// when switching panes, we want to give the focus to the CurrentSlice(if any)
			if (m_dataEntryForm != null && m_dataEntryForm.CurrentSlice != null)
			{
				targetCandidates.Add(m_dataEntryForm.CurrentSlice);
				return m_dataEntryForm.CurrentSlice.ContainsFocus ? m_dataEntryForm.CurrentSlice : null;
			}

			return base.PopulateCtrlTabTargetCandidateList(targetCandidates);
		}

		#endregion  ICtrlTabProvider implementation

		#region Component Designer generated code
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(RecordEditView));
			this.buttonImages = new System.Windows.Forms.ImageList(this.components);
			this.m_panel = new System.Windows.Forms.Panel();
			this.m_dataEntryForm.AccessibilityObject.Name = "RecordEditView.DataTree";
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
			this.m_panel.Controls.Add(this.m_dataEntryForm);
			this.m_panel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_panel.Location = new System.Drawing.Point(0, 0);
			this.m_panel.Name = "m_panel";
			this.m_panel.Size = new System.Drawing.Size(752, 150);
			this.m_panel.TabIndex = 2;
			this.m_panel.AccessibilityObject.Name = "Panel";
			//
			// m_dataEntryForm
			//
			this.m_dataEntryForm.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_dataEntryForm.Location = new System.Drawing.Point(0, 0);
			this.m_dataEntryForm.Name = "m_dataEntryForm";
			this.m_dataEntryForm.PersistenceProvder = null;
			this.m_dataEntryForm.Size = new System.Drawing.Size(752, 150);
			this.m_dataEntryForm.SliceFilter = null;
			this.m_dataEntryForm.StyleSheet = null;
			this.m_dataEntryForm.TabIndex = 3;
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
			if (!string.IsNullOrEmpty(m_titleField))
			{
				ICmObject curObj = Clerk.CurrentObject;
				if (curObj != null)
				{
					int flid = Cache.MetaDataCacheAccessor.GetFieldId2(curObj.ClassID, m_titleField, true);
					if (hvo == curObj.Hvo && tag == flid)
						SetInfoBarText();
				}
			}
		}

		#region Print methods

		public bool OnPrint(object args)
		{
			CheckDisposed();

			if (m_printLayout == null || Clerk.CurrentObject == null)
				return false;
			// Don't bother; this edit view does not specify a print layout, or there's nothing to print.

			var area = PropertyTable.GetValue<string>("areaChoice");
			string toolId;
			switch (area)
			{
				case "notebook":
					toolId = "notebookDocument";
					break;
				case "lexicon":
					toolId = "lexiconDictionary";
					break;
				default:
					return false;
			}
			var toolInXmlConfig = FindToolInXMLConfig(toolId);
			if (toolInXmlConfig == null)
				return false;
			var innerControlNode = GetToolInnerControlNodeWithRightLayout(toolInXmlConfig);
			if (innerControlNode == null)
				return false;
			using (var docView = CreateDocView(innerControlNode))
			{
				if (docView == null)
					return false;

				using (var pd = new PrintDocument())
				using (var dlg = new PrintDialog())
				{
					dlg.Document = pd;
					dlg.AllowSomePages = true;
					dlg.AllowSelection = false;
					dlg.PrinterSettings.FromPage = 1;
					dlg.PrinterSettings.ToPage = 1;
					if (dlg.ShowDialog() == DialogResult.OK)
					{
						// REVIEW: .NET does not appear to handle the collation setting correctly
						// so for now, we do not support non-collated printing.  Forcing the setting
						// seems to work fine.
						dlg.Document.PrinterSettings.Collate = true;
						docView.PrintFromDetail(pd, Clerk.CurrentObject.Hvo);
					}
				}
				return true;
			}
		}

		private XElement GetToolInnerControlNodeWithRightLayout(XElement docViewConfig)
		{
			var paramNode = docViewConfig.XPathSelectElement("control//parameters[@layout = \"" + m_printLayout + "\"]");
			if (paramNode == null)
				return null;
			return paramNode.Parent;
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
			Debug.Assert(parentConfigNode != null,
				"Can't create a view without the XML control configuration.");
			XmlDocView docView;
			try
			{
				docView = (XmlDocView)DynamicLoader.CreateObjectUsingLoaderNode(parentConfigNode);
			}
			catch (Exception e)
			{
				return null;
			}
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
