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
// File: RecordView.cs
// Responsibility: WordWorks
// Last reviewed:
//
// <remarks>
// </remarks>
// ------------------- -------------------------------------------------------------------------
using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Diagnostics;
using System.Xml;

using XCore;
using SIL.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FdoUi;
using SIL.FieldWorks.Common.Controls;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// RecordView is an abstract class for data views that show one object from a list.
	/// A RecordClerk class does most of the work of managing the list and current object.
	///	list management and navigation is entirely handled by the
	/// RecordClerk.
	///
	/// RecordClerk has no knowledge of how to display an individual object. A concrete subclass must handle
	/// this task.
	///
	/// Concrete subclasses must:
	///		1. Implement IxCoreColleague.Init, which should call InitBase, do any other initialization,
	///			and then set m_fullyInitialized.
	///		2. Implement the pane that shows the current object. Typically, set its Dock property to
	///			DockStyle.Fill and add it to this.Controls. This is typically done in an override
	///			of SetupDataContext.
	///		3. Implement ShowRecord to update the view of the object to a display of Clerk.CurrentObject.
	///	Subclasses may:
	///		- Override ReadParameters to extract info from the configuration node. (This is the
	///		representation of the XML <parameters></parameters> node from the <control></control>
	///		node used to invoke the window.)
	///		- Override GetMessageAdditionalTargets to provide message handlers in addition to the
	///		record clerk and this.
	/// </summary>
	public abstract class RecordView : XWorksViewBase
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		#region Consruction and disposal
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ViewManager"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public RecordView()
		{
			//it is up to the subclass to change this when it is finished Initializing.
			m_fullyInitialized = false;

			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			//MakePaneBar();

			base.AccNameDefault = "RecordView";		// default accessibility name
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged
		/// resources; <c>false</c> to release only unmanaged resources.
		/// </param>
		/// -----------------------------------------------------------------------------------
		protected override void Dispose( bool disposing )
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}

			base.Dispose( disposing );
		}

		#endregion // Consruction and disposal

		#region Properties

		#endregion Properties

		#region Other methods

		public virtual bool OnRecordNavigation(object argument)
		{
			CheckDisposed();

			if(!m_fullyInitialized)
				return false;
			// If it's not from our clerk, we may be intercepting a message intended for another pane.
			if (RecordNavigationInfo.GetSendingClerk(argument) != Clerk)
				return false;

			Clerk.SuppressSaveOnChangeRecord = (argument as RecordNavigationInfo).SuppressSaveOnChangeRecord;
			try
			{
				ShowRecord(argument as RecordNavigationInfo);
			}
			finally
			{
				Clerk.SuppressSaveOnChangeRecord = false;
			}
			return true;	//we handled this.
		}

		protected virtual void ShowRecord(RecordNavigationInfo rni)
		{
			if (!rni.SkipShowRecord)
			{
				ShowRecord();
			}
		}

		protected override void ShowRecord()
		{
			base.ShowRecord();
			if (m_configurationParameters != null
				&& !XmlUtils.GetOptionalBooleanAttributeValue(m_configurationParameters, "omitFromHistory", false))
			{
				UpdateContextHistory();
			}
		}

		/// <summary>
		/// create and register a URL describing the current context, for use in going backwards and forwards
		/// </summary>
		protected virtual void UpdateContextHistory()
		{
			//are we the dominant pane? The thinking here is that if our clerk is controlling the record tree bar, then we are.
			if(Clerk.IsControllingTheRecordTreeBar)
			{
				//add our current state to the history system
				string toolName = m_mediator.PropertyTable.GetStringProperty("currentContentControl","");
				int hvo=-1;
				if (Clerk. CurrentObject!= null)
					hvo = Clerk. CurrentObject.Hvo;
				FdoCache cache = Cache;
				m_mediator.SendMessage("AddContextToHistory", FwLink.Create(toolName,
					cache.GetGuidFromId(hvo), cache.ServerName, cache.DatabaseName), false);
			}
		}

		/// <summary>
		/// Note: currently called in the context of ListUpdateHelper, which suspends the clerk from reloading its list
		/// until it is disposed. So, don't do anything here (eg. Clerk.SelectedRecordChanged())
		/// that depends upon a list being loaded yet.
		/// </summary>
		protected override void SetupDataContext()
		{
			TriggerMessageBoxIfAppropriate();

			if(m_treebarAvailability!=TreebarAvailability.NotMyBusiness)
				Clerk.ActivateUI(m_treebarAvailability == TreebarAvailability.Required);//nb optional would be a bug here

			m_fakeFlid = Clerk.VirtualFlid;
		}

		/// <summary>
		/// Initialize this as an IxCoreColleague
		/// </summary>
		/// <remarks> subclasses must call this from their Init.
		/// This was done, rather than providing an Init() here in the normal way,
		/// to drive home the point that the subclass must set m_fullyInitialized
		/// to true when it is fully initialized.</remarks>
		/// <param name="mediator"></param>
		/// <param name="configurationParameters"></param>
		protected void InitBase(Mediator mediator, XmlNode configurationParameters)
		{
			Debug.Assert(m_fullyInitialized == false, "No way we are fully initialized yet!");

			m_mediator = mediator;
			m_configurationParameters = configurationParameters;

			ReadParameters();

			RecordClerk clerk = ExistingClerk;
			if (clerk == null)
			{
				// NOTE: new clerks do not typically complete ReloadList()
				// until Clerk.ActivateUI() is set (eg. here in SetupDataContext()).
				// however, we should further delay loading the list
				// if the subclass is initializing sorters/filters.
				// so we use ListUpdateHelper below to delay reloading the list.
				clerk = Clerk;
				Debug.Assert(clerk != null);
			}
			// suspend any loading of the Clerk's list items until after a
			// subclass (possibly) initializes sorters/filters
			// in SetupDataContext()
			using (RecordClerk.ListUpdateHelper luh = new RecordClerk.ListUpdateHelper(clerk))
			{
				luh.ClearBrowseListUntilReload = true;
				clerk.UpdateOwningObjectIfNeeded();
				SetTreebarAvailability();
				AddPaneBar();

				// NB: It is critical that we get added *after* our RecordClerk,
				// so that it will get messages, for example about a change of cache, before we do.
				mediator.AddColleague(this);
				SetupDataContext();
			}
			// In case it hasn't yet been loaded, load it!  See LT-10185.
			if (!Clerk.ListLoadingSuppressed && Clerk.RequestedLoadWhileSuppressed)
				Clerk.UpdateList(true, true);
			ShowRecord();
		}

		private void SetTreebarAvailability()
		{
			string a = XmlUtils.GetOptionalAttributeValue(m_configurationParameters, "treeBarAvailability", "");

			if(a == "NotMyBusiness")
				m_treebarAvailability = TreebarAvailability.NotMyBusiness;
			else
			{
				if(a != "")
					m_treebarAvailability = (TreebarAvailability)Enum.Parse(typeof(TreebarAvailability), a, true);
				else
					m_treebarAvailability = DefaultTreeBarAvailability;

				//m_previousShowTreeBarValue= m_mediator.PropertyTable.GetBoolProperty("ShowRecordList", true);

				//				string e = XmlUtils.GetOptionalAttributeValue(m_configurationParameters, "treeBarAvailability", DefaultTreeBarAvailability);
				//				m_treebarAvailability = (TreebarAvailability)Enum.Parse(typeof(TreebarAvailability), e, true);

				switch (m_treebarAvailability)
				{
					default:
						break;
					case TreebarAvailability.NotAllowed:
						m_mediator.PropertyTable.SetProperty("ShowRecordList", false);
						break;

					case TreebarAvailability.Required:
						m_mediator.PropertyTable.SetProperty("ShowRecordList", true);
						break;

					case TreebarAvailability.Optional:
						//Just use it however the last guy left it (see: PrepareToGoAway())
						break;
				}
			}
		}

		/// <summary>
		/// if the XML configuration does not specify the availability of the treebar
		/// (e.g. treeBarAvailability="Required"), then use this.
		/// </summary>
		protected virtual TreebarAvailability DefaultTreeBarAvailability
		{
			get
			{
				return TreebarAvailability.Required;
			}
		}

		#endregion // Other methods

		#region Component Designer generated code
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private void InitializeComponent()
		{
			//
			// RecordView
			//
			this.Name = "RecordView";
			this.Size = new System.Drawing.Size(752, 150);

		}
		#endregion
	}
}
