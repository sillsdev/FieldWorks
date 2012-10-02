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
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Diagnostics;
using System.Xml;
using SIL.FieldWorks.FwCoreDlgs;
using XCore;
using SIL.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FdoUi;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.Common.FwUtils;
using System.Drawing.Printing;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// XmlDocView is a view that shows a complete list as a single view.
	/// A RecordClerk class does most of the work of managing the list and current object.
	///	list management and navigation is entirely(?) handled by the
	/// RecordClerk.
	///
	/// The actual view of each object is specified by a child <jtview></jtview> node
	/// of the view node. This specifies how to display an individual list item.
	/// </summary>
	public class XmlDocView : XWorksViewBase, IFindAndReplaceContext, IPostLayoutInit
	{
		protected int m_hvoOwner; // the root HVO.
		/// <summary>
		/// Object currently being edited.
		/// </summary>
		protected ICmObject m_currentObject;
		protected int m_currentIndex = -1;
		protected XmlSeqView m_mainView;
		protected string m_configObjectName;

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
		public XmlDocView()
		{
			m_fullyInitialized = false;

			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();


			base.AccNameDefault = "XmlDocView";		// default accessibility name
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
					components.Dispose();
			}
			m_currentObject = null;

			base.Dispose( disposing );
		}

		#endregion // Consruction and disposal

		#region Properties

		#endregion Properties

		#region Other methods

		protected override void SetInfoBarText()
		{
			if (m_informationBar == null)
				return;

			string titleStr = "";
			// See if we have an AlternativeTitle string table id for an alternate title.
			string titleId = XmlUtils.GetAttributeValue(m_configurationParameters,
				"altTitleId");
			if (titleId != null)
			{
				titleStr = StringTbl.GetString(titleId, "AlternativeTitles");
			}
			else if (Clerk.OwningObject != null)
			{

				if (XmlUtils.GetBooleanAttributeValue(m_configurationParameters,
					"ShowOwnerShortname"))
		{
					titleStr = Clerk.OwningObject.ShortName;
			}
		}

			bool fBaseCalled = false;
			if (titleStr == string.Empty)
		{
				base.SetInfoBarText();
				fBaseCalled = true;
				//				titleStr = ((IPaneBar)m_informationBar).Text;	// can't get to work.
				// (EricP) For some reason I can't provide an IPaneBar get-accessor to return
				// the new Text value. If it's desirable to allow TitleFormat to apply to
				// Clerk.CurrentObject, then we either have to duplicate what the
				// base.SetInfoBarText() does here, or get the string set by the base.
				// for now, let's just return.
				if (titleStr == null || titleStr == string.Empty)
					return;
			}

			// If we have a format attribute, format the title accordingly.
			string sFmt = XmlUtils.GetAttributeValue(m_configurationParameters,
				"TitleFormat");
			if (sFmt != null)
			{
				titleStr = String.Format(sFmt, titleStr);
			}

			// if we haven't already set the text through the base,
			// or if we had some formatting to do, then set the infoBar text.
			if (!fBaseCalled || sFmt != null)
				((IPaneBar)m_informationBar).Text = titleStr;
		}

		/// <summary>
		/// Read in the parameters to determine which sequence/collection we are editing.
		/// </summary>
		protected override void ReadParameters()
		{
			base.ReadParameters();
			string backColorName = XmlUtils.GetOptionalAttributeValue(m_configurationParameters,
				"backColor", "Window");
			BackColor = Color.FromName(backColorName);
			m_configObjectName = XmlUtils.GetLocalizedAttributeValue(m_mediator.StringTbl,
				m_configurationParameters, "configureObjectName", null);
		}

		public virtual bool OnRecordNavigation(object argument)
		{
			CheckDisposed();

			if (!m_fullyInitialized
				|| RecordNavigationInfo.GetSendingClerk(argument) != Clerk) // Don't pretend to have handled it if it isn't our clerk.
				return false;

			// persist Clerk's CurrentIndex in a db specific way
			string propName = Clerk.PersistedIndexProperty;
			m_mediator.PropertyTable.SetProperty(propName, Clerk.CurrentIndex, PropertyTable.SettingsGroup.LocalSettings);
			m_mediator.PropertyTable.SetPropertyPersistence(propName, true, PropertyTable.SettingsGroup.LocalSettings);

			Clerk.SuppressSaveOnChangeRecord = (argument as RecordNavigationInfo).SuppressSaveOnChangeRecord;
			using (WaitCursor wc = new WaitCursor(this))
			{
				//DateTime dt0 = DateTime.Now;
				try
				{
					ShowRecord();
				}
				finally
				{
					Clerk.SuppressSaveOnChangeRecord = false;
				}
				//DateTime dt1 = DateTime.Now;
				//TimeSpan ts = TimeSpan.FromTicks(dt1.Ticks - dt0.Ticks);
				//Debug.WriteLine("XmlDocView.OnRecordNavigation(): ShowRecord() took " + ts.ToString() + " at " + dt1.ToString());
			}
			return true;	//we handled this.
		}

		public bool OnClerkOwningObjChanged(object sender)
		{
			CheckDisposed();

			if (this.Clerk != sender || (m_mainView == null))
				return false;

			if (Clerk.OwningObject == null)
			{
				//this happens, for example, when they user sets a filter on the
				//list we are dependent on, but no records are selected by the filter.
				//thus, we now do not have an object to get records out of,
				//so we need to just show a blank list.
				this.m_hvoOwner = -1;
			}
			else
			{
				m_hvoOwner = Clerk.OwningObject.Hvo;
				m_mainView.ResetRoot(m_hvoOwner);
				SetInfoBarText();
			}
			return false; //allow others clients of this clerk to know about it as well.
		}

		protected override void ShowRecord()
		{
			RecordClerk clerk = Clerk;

			// See if it is showing the same record, as before.
			if (m_currentObject != null && clerk.CurrentObject != null
				&& m_currentIndex == clerk.CurrentIndex
				&& m_currentObject.Hvo == clerk.CurrentObject.Hvo)
			{
				SetInfoBarText();
				return;
			}

			// See if the main owning object has changed.
			if (clerk.OwningObject.Hvo != m_hvoOwner)
			{
				m_hvoOwner = clerk.OwningObject.Hvo;
				m_mainView.ResetRoot(m_hvoOwner);
			}

			m_currentObject = clerk.CurrentObject;
			m_currentIndex = clerk.CurrentIndex;
			//add our current state to the history system
			string toolName = m_mediator.PropertyTable.GetStringProperty(
				"currentContentControl", "");
			Guid guid = Guid.Empty;
			if (clerk.CurrentObject != null)
				guid = clerk.CurrentObject.Guid;
			m_mediator.SendMessage("AddContextToHistory", new FwLinkArgs(toolName, guid), false);

			SelectAndScrollToCurrentRecord();
			base.ShowRecord();
		}

		/// <summary>
		/// Ensure that we have the current record selected and visible in the window.  See LT-9109.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			if (m_mainView != null && m_mainView.RootBox != null && !m_mainView.PaintInProgress && !m_mainView.LayoutInProgress
				&& m_mainView.RootBox.Selection == null)
			{
				SelectAndScrollToCurrentRecord();
			}
		}

		/// <summary>
		/// Pass command (from RecordEditView) on to printing view.
		/// </summary>
		/// <param name="pd">PrintDocument</param>
		/// <param name="recordHvo"></param>
		public void PrintFromDetail(PrintDocument pd, int recordHvo)
		{
			m_mainView.PrintFromDetail(pd, recordHvo);
		}

		private void SelectAndScrollToCurrentRecord()
		{
			// Scroll the display to the given record.  (See LT-927 for explanation.)
			try
			{
				RecordClerk clerk = Clerk;
				int levelFlid = 0;
				var indexes = new List<int>();
				if (Clerk is SubitemRecordClerk)
				{
					var subitemClerk = Clerk as SubitemRecordClerk;
					levelFlid = subitemClerk.SubitemFlid;
					if (subitemClerk.Subitem != null)
					{
						// There's a subitem. See if we can select it.
						var item = subitemClerk.Subitem;
						while (item.OwningFlid == levelFlid)
						{
							indexes.Add(item.OwnOrd);
							item = item.Owner;
						}
					}
				}
				indexes.Add(clerk.CurrentIndex);
				// Suppose it is the fifth subrecord of the second subrecord of the ninth main record.
				// At this point, indexes holds 4, 1, 8. That is, like information for MakeSelection,
				// it holds the indexes we want to select from innermost to outermost.
				IVwRootBox rootb = (m_mainView as IVwRootSite).RootBox;
				if (rootb != null)
				{
					int idx = clerk.CurrentIndex;
					if (idx < 0)
						return;
					// Review JohnT: is there a better way to obtain the needed rgvsli[]?
					IVwSelection sel = rootb.Selection;
					if (sel != null)
					{
						// skip moving the selection if it's already in the right record.
						int clevels = sel.CLevels(false);
						if (clevels >= indexes.Count)
						{
							for (int ilevel = indexes.Count - 1; ilevel >= 0; ilevel--)
							{
								int hvoObj, tag, ihvo, cpropPrevious;
								IVwPropertyStore vps;
								sel.PropInfo(false, clevels - indexes.Count + ilevel, out hvoObj, out tag, out ihvo,
									out cpropPrevious, out vps);
								if (ihvo != indexes[ilevel] || tag != levelFlid)
									break;
								if (ilevel == 0)
								{
									// selection is already in the right object, just make sure it's visible.
									(m_mainView as IVwRootSite).ScrollSelectionIntoView(sel,
										VwScrollSelOpts.kssoDefault);
									return;
								}
							}
						}
					}
					var rgvsli = new SelLevInfo[indexes.Count];
					for (int i = 0; i < indexes.Count; i++)
					{
						rgvsli[i].ihvo = indexes[i];
						rgvsli[i].tag = levelFlid;
					}
					rgvsli[rgvsli.Length-1].tag = m_fakeFlid;
					rootb.MakeTextSelInObj(0, rgvsli.Length, rgvsli, rgvsli.Length, rgvsli, false, false, false, true, true);
					m_mainView.ScrollSelectionIntoView(rootb.Selection, VwScrollSelOpts.kssoBoth);

					// It's a pity this next step is needed!
					rootb.Activate(VwSelectionState.vssEnabled);
				}
			}
			catch
			{
				// not much we can do to handle errors, but don't let the program die just
				// because the display hasn't yet been laid out, so selections can't fully be
				// created and displayed.
			}
		}

		/// <summary>
		/// We wait until containing controls are laid out to try to scroll our selection into view,
		/// because it depends somewhat on the window being the true size.
		/// </summary>
		public void PostLayoutInit()
		{
			IVwRootBox rootb = (m_mainView as IVwRootSite).RootBox;
			if (rootb != null && rootb.Selection != null)
				(m_mainView as IVwRootSite).ScrollSelectionIntoView(rootb.Selection, VwScrollSelOpts.kssoBoth);
		}

		protected override void SetupDataContext()
		{
			TriggerMessageBoxIfAppropriate();
			using (new WaitCursor(this))
			{
				//m_flid = RecordClerk.GetFlidOfVectorFromName(m_vectorName, Cache, out m_owningObject);
				RecordClerk clerk = Clerk;
				clerk.ActivateUI(false);
				// Enhance JohnT: could use logic similar to RecordView.InitBase to load persisted list contents (filtered and sorted).
				if (clerk.RequestedLoadWhileSuppressed)
					clerk.UpdateList(false);
				m_fakeFlid = clerk.VirtualFlid;
				m_hvoOwner = clerk.OwningObject.Hvo;
				if (!clerk.SetCurrentFromRelatedClerk())
				{
					// retrieve persisted clerk index and set it.
					int idx = m_mediator.PropertyTable.GetIntProperty(clerk.PersistedIndexProperty, -1,
						PropertyTable.SettingsGroup.LocalSettings);
					if (idx >= 0 && !clerk.HasEmptyList)
					{
						int idxOld = clerk.CurrentIndex;
						try
						{
							clerk.JumpToIndex(idx);
						}
						catch
						{
							clerk.JumpToIndex(idxOld >= 0 ? idxOld : 0);
						}
					}
					clerk.SelectedRecordChanged(false);
				}

				clerk.IsDefaultSort = false;

				// Create the main view

				// Review JohnT: should it be m_configurationParameters or .FirstChild?
				IApp app = (IApp)m_mediator.PropertyTable.GetValue("App");
				m_mainView = new XmlSeqView(m_hvoOwner, m_fakeFlid, m_configurationParameters, Clerk.VirtualListPublisher, app);
				m_mainView.Init(m_mediator, m_configurationParameters); // Required call to xCore.Colleague.
				m_mainView.Dock = DockStyle.Fill;
				m_mainView.Cache = Cache;
				m_mainView.SelectionChangedEvent +=
					new FwSelectionChangedEventHandler(OnSelectionChanged);
				m_mainView.ShowRangeSelAfterLostFocus = true;	// This makes selections visible.
				// If the rootsite doesn't have a rootbox, we can't display the record!
				// Calling ShowRecord() from InitBase() sets the state to appear that we have
				// displayed (and scrolled), even though we haven't.  See LT-7588 for the effect.
				m_mainView.MakeRoot();
				SetupStylesheet();
				Controls.Add(m_mainView);
				if (Controls.Count == 2)
				{
					Controls.SetChildIndex(m_mainView, 1);
					m_mainView.BringToFront();
				}
				m_fullyInitialized = true; // Review JohnT: was this really the crucial last step?
			}
		}

		protected override void SetupStylesheet()
		{
			FwStyleSheet ss = StyleSheet;
			if (ss != null)
				m_mainView.StyleSheet = ss;
		}

		private FwStyleSheet StyleSheet
		{
			get
			{
				return FontHeightAdjuster.StyleSheetFromMediator(m_mediator);
			}
		}

		/// <summary>
		///	invoked when our XmlDocView selection changes.
		/// </summary>
		/// <param name="sender">unused</param>
		/// <param name="e">the event arguments</param>
		public void OnSelectionChanged(object sender, FwObjectSelectionEventArgs e)
		{
			CheckDisposed();

			// paranoid sanity check.
			Debug.Assert(e.Hvo != 0);
			if (e.Hvo == 0)
				return;
			Clerk.ViewChangedSelectedRecord(e, m_mainView.RootBox.Selection);
			// Change it if it's actually changed.
			SetInfoBarText();
		}

		/// <summary>
		/// The configure dialog may be launched any time this tool is active.
		/// Its name is derived from the name of the tool.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayConfigureXmlDocView(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			if (m_configObjectName == null || m_configObjectName == "")
			{
				display.Enabled = display.Visible = false;
				return true;
			}
			display.Enabled = true;
			display.Visible = true;
			// Enhance JohnT: make this configurable. We'd like to use the 'label' attribute of the 'tool'
			// element, but we don't have it, only the two-level-down 'parameters' element
			// so use "configureObjectName" parameter for now.
			// REVIEW: FOR LOCALIZABILITY, SHOULDN'T THE "..." BE PART OF THE SOURCE FOR display.Text?
			display.Text = String.Format(display.Text, m_configObjectName + "...");
			return true; //we've handled this
		}

		/// <summary>
		/// Launch the configure dialog.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <returns></returns>
		public bool OnConfigureXmlDocView(object commandObject)
		{
			CheckDisposed();

			using (XmlDocConfigureDlg dlg = new XmlDocConfigureDlg())
			{
				string sProp = XmlUtils.GetOptionalAttributeValue(m_configurationParameters, "layoutProperty");
				if (String.IsNullOrEmpty(sProp))
					sProp = "DictionaryPublicationLayout";
				dlg.SetConfigDlgInfo(m_configurationParameters, Cache, StyleSheet,
				this.FindForm() as IMainWindowDelegateCallbacks, m_mediator, sProp);
				if (dlg.ShowDialog(this) == DialogResult.OK)
				{
					string sNewLayout = m_mediator.PropertyTable.GetStringProperty(sProp, null);
					m_mainView.ResetTables(sNewLayout);
					SelectAndScrollToCurrentRecord();
				}
			}
			return true; // we handled it
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
			base.m_configurationParameters = configurationParameters;

			ReadParameters();

			m_mediator.AddColleague(this);

			m_mediator.PropertyTable.SetProperty("ShowRecordList", false);

			SetupDataContext();
			ShowRecord();
		}

		/// In some initialization paths (e.g., when a child of a MultiPane), we don't have
		/// a containing form by the time we get initialized. Try again when our parent is set.
		protected override void OnParentChanged(EventArgs e)
		{
			base.OnParentChanged(e);
			if (m_mainView != null && m_mainView.StyleSheet == null)
				SetupStylesheet();
		}

		#endregion // Other methods

		#region IxCoreColleague implementation

		public override void Init(Mediator mediator, XmlNode configurationParameters)
		{
			CheckDisposed();

			InitBase(mediator, configurationParameters);
		}

		/// <summary>
		/// subclasses should override if they have more targets
		/// </summary>
		/// <returns></returns>
		protected override void GetMessageAdditionalTargets(System.Collections.Generic.List<IxCoreColleague> collector)
		{
			collector.Add(m_mainView);
		}

		#endregion // IxCoreColleague implementation

		#region Component Designer generated code
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private void InitializeComponent()
		{
			this.SuspendLayout();
			//
			// RecordView
			//
			this.Name = "RecordView";
			this.Size = new System.Drawing.Size(752, 150);
			this.ResumeLayout(false);

		}
		#endregion

		/// <summary>
		///	see if it makes sense to provide the "delete record" command now.
		///	Currently we don't support this in document view, except for reversal entries.
		///	If we decide to support it, we will need to do additional work
		///	(cf LT-1222) to ensure that
		///		(a) The clerk's idea of the current entry corresponds to where the selection is
		///		(b) After deleting it, the clerk's list gets updated.
		///	The former is not happening because we haven't written a SelectionChange method
		///	to notice the selection in the view and change the clerk to match.
		///	Not sure why the clerk's list isn't being updated...it may be only a problem
		///	for homographs.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public bool OnDisplayDeleteRecord(object commandObject,
			ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			display.Enabled = false;
			// Don't claim to have handled it if the clerk is holding reversal entries.
			return !(Clerk.Id == "reversalEntries");
		}

		/// <summary>
		/// Implements the command that just does Find, without Replace.
		/// </summary>
		public bool OnFindAndReplaceText(object argument)
		{
			return ((IFwMainWnd)ParentForm).OnEditFind(argument);
		}

		/// <summary>
		/// Enables the command that just does Find, without Replace.
		/// </summary>
		public virtual bool OnDisplayFindAndReplaceText(object commandObject, ref UIItemDisplayProperties display)
		{
			display.Enabled = display.Visible = (ParentForm is FwXWindow);
			return true; //we've handled this
		}

		/// <summary>
		/// Implements the command that just does Find, without Replace.
		/// </summary>
		public bool OnReplaceText(object argument)
		{
			return ((FwXWindow)ParentForm).OnEditReplace(argument);
		}

		/// <summary>
		/// Enables the command that just does Find, without Replace.
		/// </summary>
		public virtual bool OnDisplayReplaceText(object commandObject, ref UIItemDisplayProperties display)
		{
			display.Enabled = !m_mainView.ReadOnlyView;
			return true; //we've handled this
		}

		/// <summary>
		/// If this gets called (which it never should), just say we did it, unless we are in the context of reversal entries.
		/// In the case of reversal entries, we say we did not do it, so the record clerk deals with it.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <returns></returns>
		public bool OnDeleteRecord(object commandObject)
		{
			CheckDisposed();

			if (Clerk.Id == "reversalEntries")
			{
				return false; // Let the clerk do it.
			}
			else
			{
				Debug.Assert(false);
			}
			return true;
		}

		public string FindTabHelpId
		{
			get { return XmlUtils.GetOptionalAttributeValue(m_configurationParameters, "findHelpId", null); }
		}
	}
}
