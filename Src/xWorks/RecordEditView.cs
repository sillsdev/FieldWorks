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
using System.Collections;
using System.Diagnostics;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Xml;
using System.Reflection;
using System.IO;
using System.Resources;

using SIL.FieldWorks;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.FDO;
using SIL.Utils;
using SIL.FieldWorks.Common.Framework;
using XCore;
using System.Collections.Generic;

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
	public class RecordEditView : RecordView
	{
		#region Data members

		/// <summary>
		/// Mode string for DataTree to use at top level.
		/// </summary>
		protected string m_rootMode;
		/// <summary>
		/// handles creating the context menus for the data tree and funneling commands to the data tree.
		/// </summary>
		DTMenuHandler m_menuHandler = null;

		private System.Windows.Forms.ImageList buttonImages;
		protected System.Windows.Forms.Panel m_panel;
		protected DataTree m_dataEntryForm;
		private System.ComponentModel.IContainer components;
		private string m_layoutName;

		/// <summary>
		/// used to associate menu commands with the slice that sent them
		/// </summary>
		//protected Slice m_sourceOfMenuCommandSlice=null;

		#endregion // Data members

		#region Construction and Removal
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="VectorEditor"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public RecordEditView()
		{
			// This must be called before InitializeComponent()
			m_dataEntryForm = CreateNewDataTree();

			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			base.AccNameDefault = "RecordEditView";		// default accessibility name
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
			IMainWindowDelegatedFunctions containingForm = this.FindForm() as IMainWindowDelegatedFunctions;
			if (containingForm != null)
				m_dataEntryForm.StyleSheet = containingForm.StyleSheet;
			m_fullyInitialized = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new DataTree object. (Used to let subclasses create their own version of
		/// a DataTree if needed)
		/// </summary>
		/// <returns>A new DataTree object</returns>
		/// ------------------------------------------------------------------------------------
		protected virtual DataTree CreateNewDataTree()
		{
			return new DataTree();
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
					m_dataEntryForm.Dispose();
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
			XWindow window = (XWindow)m_mediator.PropertyTable.GetValue("window");

			Clerk.SuppressSaveOnChangeRecord = (argument as RecordNavigationInfo).SuppressSaveOnChangeRecord;
			try
			{
				window.SuspendIdleProcessing();
				ShowRecord();
			}
			finally
			{
				window.ResumeIdleProcessing();
				Clerk.SuppressSaveOnChangeRecord = false;
			}
			return true;	//we handled this.
		}

		public bool OnConsideringClosing(object argument, System.ComponentModel.CancelEventArgs args)
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

		#endregion // Message Handlers

		#region Other methods

		private void SetStatusBarContents()
		{
		}

		protected override void SetInfoBarText()
		{
			if (m_informationBar == null)
				return;

			// See if we have an AlternativeTitle string table id for an alternate title.
			string titleId = XmlUtils.GetAttributeValue(m_configurationParameters,
				"altTitleId");
			if (titleId != null)
			{
				string titleStr = StringTbl.GetString(titleId, "AlternativeTitles");
				if (titleStr != null && titleStr != String.Empty)
				{
					((IPaneBar)m_informationBar).Text = titleStr;
					return;
				}
			}

			base.SetInfoBarText();
		}

		protected override void ShowRecord()
		{
			int msStart = Environment.TickCount;
			Debug.Assert(m_dataEntryForm != null);

			PrepCacheForNewRecord();

			base.ShowRecord();

			if(Clerk.CurrentObject == null || Clerk.SuspendLoadingRecordUntilOnJumpToRecord)
			{
				m_dataEntryForm.Hide();
				m_dataEntryForm.Reset();	// in case user deleted the object it was based upon.
				return;
			}
			try
			{
				m_dataEntryForm.Show();
				Cursor.Current = Cursors.WaitCursor;
				// Enhance: Maybe do something here to allow changing the templates without the starting the application.
				m_dataEntryForm.ShowObject(Clerk.CurrentObject.Hvo, m_layoutName);
				SetStatusBarContents();

				Cursor.Current = Cursors.Default;
			}
			catch(Exception error)
			{
				if (m_mediator.PropertyTable.GetBoolProperty("DoingAutomatedTest", false))
					throw;
				else	//don't really need to make the program stop just because we could not show this record.
					SIL.Utils.ErrorReporter.ReportException(error, null, false);
			}
			int msEnd = Environment.TickCount;
			Debug.WriteLineIf(RuntimeSwitches.RecordTimingSwitch.TraceInfo, "ShowRecord took " + (msEnd - msStart) + " ms", RuntimeSwitches.RecordTimingSwitch.DisplayName);
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

			XmlAttribute xa = m_configurationParameters.Attributes["layout"];
			if (xa != null)
				m_layoutName = xa.Value;
		}

		protected override void SetupDataContext()
		{
			Debug.Assert(m_configurationParameters != null);

			base.SetupDataContext();

			//this will normally be the same name as the view, e.g. "basicEdit". This plus the name of the vector
			//should give us a unique context for the dataTree control parameters.

			string persistContext=XmlUtils.GetOptionalAttributeValue(m_configurationParameters, "persistContext");

			if (persistContext !="")
				persistContext=m_vectorName+"."+persistContext+".DataTree";
			else
				persistContext=m_vectorName+".DataTree";

			m_dataEntryForm.PersistenceProvder = new XCore.PersistenceProvider(persistContext, m_mediator.PropertyTable);

			Clerk.UpdateRecordTreeBarIfNeeded();
			SetupSliceFilter();
			m_dataEntryForm.Dock = System.Windows.Forms.DockStyle.Fill;
			m_dataEntryForm.StringTbl = this.StringTbl;
			m_dataEntryForm.SmallImages =(ImageCollection) this.m_mediator.PropertyTable.GetValue("smallImages");
			string sDatabase = Cache.DatabaseName;
			m_dataEntryForm.Initialize(Cache, true, Inventory.GetInventory("layouts", sDatabase),
				Inventory.GetInventory("parts", sDatabase));
			m_dataEntryForm.Init(m_mediator, m_configurationParameters);
			m_dataEntryForm.AccessibilityObject.Name = "RecordEditView.DataTree";
			//set up the context menu, overriding the automatic menu creator/handler

			m_menuHandler = DTMenuHandler.Create(m_dataEntryForm, m_configurationParameters);
			m_menuHandler.Init(m_mediator, m_configurationParameters);

//			m_dataEntryForm.SetContextMenuHandler(new SliceMenuRequestHandler((m_menuHandler.GetSliceContextMenu));
			m_dataEntryForm.SetContextMenuHandler(new SliceShowMenuRequestHandler(m_menuHandler.ShowSliceContextMenu));

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
			string filterPath="";
			try
			{
				filterPath =XmlUtils.GetOptionalAttributeValue(this.m_configurationParameters, "filterPath");
				if (filterPath!= null)
				{
					XmlDocument document = new XmlDocument();
					document.Load(SIL.FieldWorks.Common.Utils.DirectoryFinder.GetFWCodeFile(filterPath));
					m_dataEntryForm.SliceFilter = new SliceFilter(document);
				}
				else //just set up a minimal filter
					m_dataEntryForm.SliceFilter = new SliceFilter();
			}
			catch (Exception )
			{
				throw new ConfigurationException ("Could not load the filter.", m_configurationParameters);
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
		protected override IxCoreColleague[] GetMessageAdditionalTargets()
		{
			if(!m_fullyInitialized)
				return new IxCoreColleague[] {};;

			if (m_dataEntryForm != null) // Unlikely it is null, but I have observed it..JohnT.
				return new IxCoreColleague[]{m_dataEntryForm, m_menuHandler};
			else
				return new IxCoreColleague[]{m_menuHandler};

		}

		#region IxCoreCtrlTabProvider implementation

		public override Control PopulateCtrlTabTargetCandidateList(List<Control> targetCandidates)
		{
			if (targetCandidates == null)
				throw new ArgumentNullException("'targetCandidates' is null.");

			// when switching panes, we want to give the focus to the CurrentSlice(if any)
			if (m_dataEntryForm != null && m_dataEntryForm.CurrentSlice != null)
			{
				targetCandidates.Add(m_dataEntryForm.CurrentSlice);
				return m_dataEntryForm.CurrentSlice.ContainsFocus ? m_dataEntryForm.CurrentSlice : null;
			}
			else
			{
				return base.PopulateCtrlTabTargetCandidateList(targetCandidates);
			}
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
	}
}
