// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: RecordEditView.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Printing;
using System.Windows.Forms;
using System.Xml;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.FDO;
using SIL.Utils;
using XCore;
using System.Collections.Generic;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;

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
	public class RecordEditView : RecordView, IVwNotifyChange
	{
		#region Data members

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
		public RecordEditView()
			: this(new DataTree())
		{
		}

		protected RecordEditView(DataTree dataEntryForm)
		{
			// This must be called before InitializeComponent()
			m_dataEntryForm = dataEntryForm;
			m_dataEntryForm.CurrentSliceChanged += m_dataEntryForm_CurrentSliceChanged;

			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			AccNameDefault = "RecordEditView";		// default accessibility name
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize this as an IxCoreColleague
		/// </summary>
		/// <param name="mediator"></param>
		/// <param name="configurationParameters"></param>
		/// ------------------------------------------------------------------------------------
		public override void Init(Mediator mediator, XmlNode configurationParameters)
		{
			CheckDisposed();

			InitBase(mediator, configurationParameters);

			m_showDescendantInRoot = XmlUtils.GetOptionalBooleanAttributeValue(configurationParameters, "showDescendantInRoot", false);

			// retrieve persisted clerk index and set it.
			int idx = m_mediator.PropertyTable.GetIntProperty(Clerk.PersistedIndexProperty, -1, PropertyTable.SettingsGroup.LocalSettings);
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
			m_dataEntryForm.StyleSheet = FontHeightAdjuster.StyleSheetFromMediator(m_mediator);
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

		public override bool OnRecordNavigation(object argument)
		{
			CheckDisposed();

			if(!m_fullyInitialized)
				return false;
			if (RecordNavigationInfo.GetSendingClerk(argument) != Clerk)
				return false;

			// persist Clerk's CurrentIndex in a db specific way
			string propName = Clerk.PersistedIndexProperty;
			m_mediator.PropertyTable.SetProperty(propName, Clerk.CurrentIndex, PropertyTable.SettingsGroup.LocalSettings);
			m_mediator.PropertyTable.SetPropertyPersistence(propName, true, PropertyTable.SettingsGroup.LocalSettings);
			var window = (XWindow)m_mediator.PropertyTable.GetValue("window");

			try
			{
				window.SuspendIdleProcessing();
				ShowRecord(argument as RecordNavigationInfo);
			}
			finally
			{
				window.ResumeIdleProcessing();
			}
			return true;	//we handled this.
		}

		public bool OnConsideringClosing(object argument, CancelEventArgs args)
		{
			CheckDisposed();

			args.Cancel = !PrepareToGoAway();
			return args.Cancel; // if we want to cancel, others don't need to be asked.
		}

		/// <summary>
		/// From IxCoreContentControl
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
				if (m_mediator.PropertyTable.GetBoolProperty("DoingAutomatedTest", false))
					ShowRecordOnIdle(rni.SuppressSaveOnChangeRecord);
				else
					m_mediator.IdleQueue.Add(IdleQueuePriority.High, ShowRecordOnIdle, rni);
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
		/// <param name="parameter">The parameter.</param>
		bool ShowRecordOnIdle(object parameter)
		{
			if (IsDisposed)
				return true;

			base.ShowRecord();
			int msStart = Environment.TickCount;
			Debug.Assert(m_dataEntryForm != null);

			var rni = (RecordNavigationInfo) parameter;
			bool oldSuppressSaveOnChangeRecord = Clerk.SuppressSaveOnChangeRecord;
			Clerk.SuppressSaveOnChangeRecord = rni.SuppressSaveOnChangeRecord;
			PrepCacheForNewRecord();
			Clerk.SuppressSaveOnChangeRecord = oldSuppressSaveOnChangeRecord;

			if (Clerk.CurrentObject == null || Clerk.SuspendLoadingRecordUntilOnJumpToRecord)
			{
				m_dataEntryForm.Hide();
				m_dataEntryForm.Reset();	// in case user deleted the object it was based upon.
				return true;
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

					m_dataEntryForm.ShowObject(obj, m_layoutName, m_layoutChoiceField, Clerk.CurrentObject, rni.SuppressFocusChange);
				}
			}
			catch (Exception error)
			{
				if (m_mediator.PropertyTable.GetBoolProperty("DoingAutomatedTest", false))
					throw;

				//don't really need to make the program stop just because we could not show this record.
				IApp app = (IApp)m_mediator.PropertyTable.GetValue("App");
				ErrorReporter.ReportException(error, app.SettingsKey, m_mediator.FeedbackInfoProvider.SupportEmailAddress,
					null, false);
			}
			int msEnd = Environment.TickCount;
			Debug.WriteLineIf(RuntimeSwitches.RecordTimingSwitch.TraceInfo, "ShowRecord took " + (msEnd - msStart) + " ms", RuntimeSwitches.RecordTimingSwitch.DisplayName);
			return true;
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

			m_layoutName = XmlUtils.GetOptionalAttributeValue(m_configurationParameters, "layout");
			m_layoutChoiceField = XmlUtils.GetOptionalAttributeValue(m_configurationParameters, "layoutChoiceField");
			m_titleField = XmlUtils.GetAttributeValue(m_configurationParameters, "titleField");
			if (!string.IsNullOrEmpty(m_titleField))
				Cache.DomainDataByFlid.AddNotification(this);
			string titleId = XmlUtils.GetAttributeValue(m_configurationParameters, "altTitleId");
			if (titleId != null)
				m_titleStr = StringTbl.GetString(titleId, "AlternativeTitles");
			m_printLayout = XmlUtils.GetAttributeValue(m_configurationParameters, "printLayout");
		}

		protected override void SetupDataContext()
		{
			Debug.Assert(m_configurationParameters != null);

			base.SetupDataContext();

			//this will normally be the same name as the view, e.g. "basicEdit". This plus the name of the vector
			//should give us a unique context for the dataTree control parameters.

			string persistContext = XmlUtils.GetOptionalAttributeValue(m_configurationParameters, "persistContext");

			if (persistContext !="")
				persistContext=m_vectorName+"."+persistContext+".DataTree";
			else
				persistContext=m_vectorName+".DataTree";

			m_dataEntryForm.PersistenceProvder = new PersistenceProvider(persistContext, m_mediator.PropertyTable);

			Clerk.UpdateRecordTreeBarIfNeeded();
			SetupSliceFilter();
			m_dataEntryForm.Dock = DockStyle.Fill;
			m_dataEntryForm.StringTbl = StringTbl;
			m_dataEntryForm.SmallImages =(ImageCollection) m_mediator.PropertyTable.GetValue("smallImages");
			string sDatabase = Cache.ProjectId.Name;
			m_dataEntryForm.Initialize(Cache, true, Inventory.GetInventory("layouts", sDatabase),
				Inventory.GetInventory("parts", sDatabase));
			m_dataEntryForm.Init(m_mediator, m_configurationParameters);
			if (m_dataEntryForm.AccessibilityObject != null)
				m_dataEntryForm.AccessibilityObject.Name = "RecordEditView.DataTree";
			//set up the context menu, overriding the automatic menu creator/handler

			m_menuHandler = DTMenuHandler.Create(m_dataEntryForm, m_configurationParameters);
			m_menuHandler.Init(m_mediator, m_configurationParameters);

//			m_dataEntryForm.SetContextMenuHandler(new SliceMenuRequestHandler((m_menuHandler.GetSliceContextMenu));
			m_dataEntryForm.SetContextMenuHandler(m_menuHandler.ShowSliceContextMenu);

			Controls.Add(m_dataEntryForm);
			m_dataEntryForm.BringToFront();
		}

		/// <summary>
		/// a slice filter is used to hide some slices.
		/// </summary>
		/// <remarks> this will set up a filter even if you do not specify a filter path, since
		/// some filtering is done by the FDO classes (CmObject.IsFieldRelevant)
		/// </remarks>
		/// <example>
		///		to set up a slice filter,kids the relative path in the filterPath attribute of the parameters:
		///		<control assemblyPath="xWorks.dll" class="SIL.FieldWorks.XWorks.RecordEditView">
		///			<parameters field="Entries" templatePath="LexEd\XDEs" filterPath="LexEd\basicFilter.xml">
		///			...
		///</example>
		private void SetupSliceFilter()
		{
			try
			{
				string filterPath = XmlUtils.GetOptionalAttributeValue(m_configurationParameters, "filterPath");
				if (filterPath!= null)
				{
#if __MonoCS__
					// TODO-Linux: fix the data
					filterPath = filterPath.Replace(@"\", "/");
#endif
					var document = new XmlDocument();
					document.Load(DirectoryFinder.GetFWCodeFile(filterPath));
					m_dataEntryForm.SliceFilter = new SliceFilter(document);
				}
				else //just set up a minimal filter
					m_dataEntryForm.SliceFilter = new SliceFilter();
			}
			catch (Exception e)
			{
				throw new ConfigurationException ("Could not load the filter.", m_configurationParameters, e);
			}
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

		/// <summary>
		/// subclasses should override if they have more targets
		/// </summary>
		/// <returns></returns>
		protected override void GetMessageAdditionalTargets(List<IxCoreColleague> collector)
		{
			if(!m_fullyInitialized)
				return;

			if (m_dataEntryForm != null) // Unlikely it is null, but I have observed it..JohnT.
				collector.Add(m_dataEntryForm);

			collector.Add(m_menuHandler);
		}

		#region IxCoreCtrlTabProvider implementation

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

		#endregion  IxCoreCtrlTabProvider implementation

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
			this.m_dataEntryForm.SmallImages = null;
			this.m_dataEntryForm.StringTbl = null;
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

			if (m_printLayout == null)
				return false;
			// Don't bother; this edit view does not specify a print layout.

			var area = m_mediator.PropertyTable.GetStringProperty("areaChoice", null);
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
			var docViewConfig = FindToolInXMLConfig(toolId);
			if (docViewConfig == null)
				return false;
			var innerControlNode = GetToolInnerControlNodeWithRightLayout(docViewConfig);
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

		private XmlNode GetToolInnerControlNodeWithRightLayout(XmlNode docViewConfig)
		{
			var paramNode = docViewConfig.SelectSingleNode("control//parameters[@layout = \"" + m_printLayout + "\"]");
			if (paramNode == null)
				return null;
			return paramNode.ParentNode;
		}

		private XmlNode FindToolInXMLConfig(string docToolValue)
		{
			// At this point m_configurationParameters holds the RecordEditView parameter node.
			// We need to find the tool that has a value attribute matching our input
			// parameter (docToolValue).
			var path = ".//tools/tool[@value = \""+docToolValue+"\"]";
			return m_configurationParameters.OwnerDocument.SelectSingleNode(path);
		}

		private XmlDocView CreateDocView(XmlNode parentConfigNode)
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
			docView.Init(m_mediator, parentConfigNode.SelectSingleNode("parameters"));
			return docView;
		}

		#endregion

	}
}
