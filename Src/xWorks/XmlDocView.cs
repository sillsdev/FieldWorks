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
// --------------------------------------------------------------------------------------------
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
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.Framework;

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
	public class XmlDocView : XWorksViewBase
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
			int hvo=-1;
			if (clerk.CurrentObject!= null)
				hvo = clerk.CurrentObject.Hvo;
			FdoCache cache = Cache;
			m_mediator.SendMessage("AddContextToHistory",
				FwLink.Create(toolName, cache.GetGuidFromId(hvo), cache.ServerName,
					cache.DatabaseName), false);

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

		private void SelectAndScrollToCurrentRecord()
		{
			bool fOldForce = BaseVirtualHandler.ForceBulkLoadIfPossible;
			// Scroll the display to the given record.  (See LT-927 for explanation.)
			try
			{
				BaseVirtualHandler.ForceBulkLoadIfPossible = true;	// disable "compute-every-time"
				// Todo JohnT: move IP to specified record.  (The following code does this,
				// but makes a range selection of the entire object instead of an IP.)
				// Also override HandleSelChange to update current record.
				RecordClerk clerk = Clerk;
				IVwRootBox rootb = (m_mainView as IVwRootSite).RootBox;
				if (rootb != null)
				{
					int idx = clerk.CurrentIndex;
					if (idx < 0)
						return;
					// Review JohnT: is there a better way to obtain the needed rgvsli[]?
					IVwSelection sel = rootb.Selection;
					if (sel == null)
						sel = rootb.MakeSimpleSel(true, false, false, true);
					int cvsli = sel.CLevels(false) - 1;
					// Out variables for AllTextSelInfo.
					int ihvoRoot;
					int tagTextProp;
					int cpropPrevious;
					int ichAnchor;
					int ichEnd;
					int ws;
					bool fAssocPrev;
					int ihvoEnd;
					ITsTextProps ttpBogus;
					SelLevInfo[] rgvsli = SelLevInfo.AllTextSelInfo(sel, cvsli,
						out ihvoRoot, out tagTextProp, out cpropPrevious, out ichAnchor, out ichEnd,
						out ws, out fAssocPrev, out ihvoEnd, out ttpBogus);
					rgvsli[cvsli - 1].ihvo = idx;
					sel = rootb.MakeTextSelInObj(ihvoRoot, cvsli, rgvsli, 0, null,
						false, false, false, true, true);
					if (sel == null)
					{
						// If the current selection is in a property that the new selection
						// lacks, making the selection can fail.  If that happens, try again at
						// a higher level.  See LT-6011.
						int cvsliNew = cvsli - 1;
						while (sel == null && cvsliNew > 0)
						{
							// Shift the SelLevInfo values down one place in the array, getting
							// rid of the first one.
							for (int i = 1; i <= cvsliNew; ++i)
								rgvsli[i - 1] = rgvsli[i];
							sel = rootb.MakeTextSelInObj(ihvoRoot, cvsliNew, rgvsli, 0, null,
								false, false, false, true, true);
							--cvsliNew;
						}
					}
					// TODO: implement and use kssoCenter.
					(m_mainView as IVwRootSite).ScrollSelectionIntoView(sel,
						VwScrollSelOpts.kssoDefault);
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
			finally
			{
				BaseVirtualHandler.ForceBulkLoadIfPossible = fOldForce;	// reenable "compute-every-time"
			}
		}

		protected override void SetupDataContext()
		{
			TriggerMessageBoxIfAppropriate();
			System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.WaitCursor;

			//m_flid = RecordClerk.GetFlidOfVectorFromName(m_vectorName, Cache, out m_owningObject);
			RecordClerk clerk = Clerk;
			clerk.ActivateUI(false);
			m_fakeFlid = clerk.VirtualFlid;
			// retrieve persisted clerk index and set it.
			int idx = m_mediator.PropertyTable.GetIntProperty(clerk.PersistedIndexProperty, -1, PropertyTable.SettingsGroup.LocalSettings);
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
			clerk.SelectedRecordChanged();
			m_hvoOwner = clerk.OwningObject.Hvo;

			clerk.IsDefaultSort = false;

			// Create the main view

			// Review JohnT: should it be m_configurationParameters or .FirstChild?
			m_mainView = new XmlSeqView(m_hvoOwner, m_fakeFlid, m_configurationParameters);
			m_mainView.Init(m_mediator, m_configurationParameters); // Required call to xCore.Colleague.
			m_mainView.Dock = System.Windows.Forms.DockStyle.Fill;
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
				// If possible retrieve the style sheet appropriate for its main window.
				IMainWindowDelegatedFunctions containingForm = FindForm() as IMainWindowDelegatedFunctions;
				if (containingForm != null)
				{
					return containingForm.StyleSheet;
				}
				else
				{
					// No Parent, so try digging the window out of the Mediator.
					containingForm = m_mediator.PropertyTable.GetValue("window") as IMainWindowDelegatedFunctions;
					return containingForm.StyleSheet;
				}
				//return null;
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
			Clerk.ViewChangedSelectedRecord(e);
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

			XmlDocConfigureDlg dlg = new XmlDocConfigureDlg();
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
		protected override IxCoreColleague[] GetMessageAdditionalTargets()
		{
			return new IxCoreColleague[] {m_mainView};
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
	}
}
