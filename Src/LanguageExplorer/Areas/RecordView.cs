// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.IO;
using System.Xml.Linq;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.Xml;

namespace LanguageExplorer.Areas
{
	/// <summary>
	/// RecordView is an abstract class for data views that show one object from a list.
	/// A RecordList class does most of the work of managing the list and current object.
	///	list management and navigation is entirely handled by the
	/// RecordList.
	///
	/// RecordList has no knowledge of how to display an individual object. A concrete subclass must handle
	/// this task.
	///
	/// Concrete subclasses must:
	///		1. Implement IxCoreColleague.Init, which should call InitBase, do any other initialization,
	///			and then set m_fullyInitialized.
	///		2. Implement the pane that shows the current object. Typically, set its Dock property to
	///			DockStyle.Fill and add it to this.Controls. This is typically done in an override
	///			of SetupDataContext.
	///		3. Implement ShowRecord to update the view of the object to a display of MyRecordList.CurrentObject.
	///	Subclasses may:
	///		- Override ReadParameters to extract info from the configuration node. (This is the
	///		representation of the XML <parameters></parameters> node from the <control></control>
	///		node used to invoke the window.)
	///		- Override GetMessageAdditionalTargets to provide message handlers in addition to the
	///		record list and this.
	/// </summary>
	internal abstract class RecordView : ViewBase
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		#region Consruction and disposal

		/// <summary>
		/// Initializes a new instance of the <see cref="RecordView"/> class.
		/// </summary>
		protected RecordView()
		{
			Init();
		}

		protected RecordView(XElement configurationParametersElement, LcmCache cache, IRecordList recordList)
			: base(configurationParametersElement, cache, recordList)
		{
			Init();
		}

		private void Init()
		{
			//it is up to the subclass to change this when it is finished Initializing.
			m_fullyInitialized = false;

			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			AccNameDefault = "RecordView"; // default accessibility name

			MyRecordList.RecordChanged += RecordList_RecordChanged_Handler;
		}

		protected virtual void RecordList_RecordChanged_Handler(object sender, RecordNavigationEventArgs e)
		{
			if (!m_fullyInitialized)
			{
				return;
			}

			var options = new ListUpdateHelperParameterObject
			{
				MyRecordList = MyRecordList,
				SuppressSaveOnChangeRecord = e.RecordNavigationInfo.SuppressSaveOnChangeRecord
			};
			using (new ListUpdateHelper(options))
			{
				ShowRecord(e.RecordNavigationInfo);
			}
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			// Must not be run more than once.
			if (IsDisposed)
			{
				return;
			}

			if( disposing )
			{
				MyRecordList.RecordChanged -= RecordList_RecordChanged_Handler;
				components?.Dispose();
			}

			base.Dispose( disposing );
		}

		#endregion // Consruction and disposal

		#region Other methods

		/// <summary>
		/// Shows the record.
		/// </summary>
		protected virtual void ShowRecord(RecordNavigationInfo rni)
		{
			if (!rni.SkipShowRecord)
			{
				ShowRecord();
			}
		}

		/// <summary>
		/// Shows the record.
		/// </summary>
		protected override void ShowRecord()
		{
			base.ShowRecord();
			if (m_configurationParametersElement != null && !XmlUtils.GetOptionalBooleanAttributeValue(m_configurationParametersElement, "omitFromHistory", false))
			{
				UpdateContextHistory();
			}
		}

		/// <summary>
		/// create and register a URL describing the current context, for use in going backwards and forwards
		/// </summary>
		protected virtual void UpdateContextHistory()
		{
			//are we the dominant pane? The thinking here is that if our record list is controlling the record tree bar, then we are.
			// The second condition prevents recording the intermediate record in the history when following a link
			// causes us to change areas and then change records.
			if (!MyRecordList.IsControllingTheRecordTreeBar || !string.IsNullOrEmpty(PropertyTable.GetValue("SuspendLoadingRecordUntilOnJumpToRecord", string.Empty)))
			{
				return;
			}
			//add our current state to the history system
			var guid = Guid.Empty;
			if (MyRecordList.CurrentObject != null)
			{
				guid = MyRecordList.CurrentObject.Guid;
			}
			MyRecordList.SelectedRecordChanged(true, true); // make sure we update the record count in the Status bar.
			PropertyTable.GetValue<LinkHandler>("LinkHandler").AddLinkToHistory(new FwLinkArgs(PropertyTable.GetValue<string>(AreaServices.ToolChoice), guid));
		}

		/// <summary>
		/// Note: currently called in the context of ListUpdateHelper, which suspends the record list from reloading its list
		/// until it is disposed. So, don't do anything here (eg. MyRecordList.SelectedRecordChanged())
		/// that depends upon a list being loaded yet.
		/// </summary>
		protected override void SetupDataContext()
		{
			TriggerMessageBoxIfAppropriate();

			if (m_treebarAvailability != TreebarAvailability.NotMyBusiness)
			{
				MyRecordList.ActivateUI(); // NB: optional would be a bug here
			}

			m_madeUpFieldIdentifier = MyRecordList.VirtualFlid;
		}

		/// <summary>
		/// Initialize this as an IxCoreColleague
		/// </summary>
		/// <remarks> subclasses must call this from their Init.
		/// This was done, rather than providing an Init() here in the normal way,
		/// to drive home the point that the subclass must set m_fullyInitialized
		/// to true when it is fully initialized.</remarks>
		protected void InitBase()
		{
			Debug.Assert(m_fullyInitialized == false, "No way we are fully initialized yet!");

			ReadParameters();

			if (MyRecordList == null)
			{
				Debug.Assert(MyRecordList != null);
			}
			// Someone might have suppressed loading the list.
			// If so, then pass the buck on to ListUpdateHelper and suspend any loading of the record list's list items until after a
			// subclass (possibly) initializes sorters/filters
			// in SetupDataContext()
			using (new ListUpdateHelper(new ListUpdateHelperParameterObject { MyRecordList = MyRecordList, ClearBrowseListUntilReload =  true}))
			{
				MyRecordList.UpdateOwningObjectIfNeeded();
				SetTreebarAvailability();
				AddPaneBar();

				//Historical comments here indicated that MyRecordList should be processed by the mediator before the
				//view. This is handled by Priority now, RecordView is by default just after RecordList in the processing.
				SetupDataContext();
			}
			// In case it hasn't yet been loaded, load it!  See LT-10185.
			if (!MyRecordList.ListLoadingSuppressed && MyRecordList.RequestedLoadWhileSuppressed)
			{
				MyRecordList.UpdateList(true, true); // sluggishness culprit for LT-12844 was in here
			}
		}

		private string GetRecordListPersistPathname()
		{
			return GetSortFilePersistPathname(Cache, MyRecordList.Id);
		}

		internal static string GetSortFilePersistPathname(LcmCache cache, string recordListId)
		{
			var filename = recordListId + "_SortSeq";
			//(This extension is also known to ProjectRestoreService.RestoreFrom7_0AndNewerBackup.)
			// Also to IFwMainWnd.DiscardProperties().
			var filenameWithExt = Path.ChangeExtension(filename, "fwss");
			var tempDirectory = Path.Combine(cache.ProjectId.ProjectFolder, LcmFileHelper.ksSortSequenceTempDir);
			if (!Directory.Exists(tempDirectory))
			{
				Directory.CreateDirectory(tempDirectory);
			}
			return Path.Combine(tempDirectory, filenameWithExt);
		}

		protected virtual void PersistSortSequence()
		{
			if (MyRecordList == null)
			{
				return; // temporary record list, such as a concordance in find example dialog.
			}

			// If we're being disposed because the application is crashing, we do NOT want to save the sort
			// sequence. It might contain bad objects, or represent a filtered state that is NOT going to
			// be persisted because of the crash. LT-11446.
			if (FwUtils.InCrashedState)
			{
				return;
			}
			var pathname = GetRecordListPersistPathname();
#if DEBUG
			var watch = new Stopwatch();
			watch.Start();
#endif
			MyRecordList.PersistListOn(pathname);
#if DEBUG
			watch.Stop();
			Debug.WriteLine("Saving record list " + pathname + " took " + watch.ElapsedMilliseconds + " ms.");
#endif
		}

		// Enhance JohnT: need to verify that sort sequence is current.
		private bool RestoreSortSequence()
		{
			var pathname = GetRecordListPersistPathname();
			if (!File.Exists(pathname))
			{
				return false;
			}
#if DEBUG
			var watch = new Stopwatch();
			watch.Start();
#endif
			var result = MyRecordList.RestoreListFrom(pathname);
#if DEBUG
			watch.Stop();
			Debug.WriteLine("Restoring record list " + pathname + " took " + watch.ElapsedMilliseconds + " ms.");
#endif
			return result;
		}

		private void SetTreebarAvailability()
		{
			var treeBarAvailability = XmlUtils.GetOptionalAttributeValue(m_configurationParametersElement, "treeBarAvailability", string.Empty);
			switch (treeBarAvailability)
			{
				case "":
					m_treebarAvailability = DefaultTreeBarAvailability;
						break;
				case "Required":
					m_treebarAvailability = TreebarAvailability.Required;
						break;
				case "NotAllowed":
					m_treebarAvailability = TreebarAvailability.NotAllowed;
						break;
				case "NotMyBusiness":
					m_treebarAvailability = TreebarAvailability.NotMyBusiness;
						break;
				default:
					throw new NotImplementedException(string.Format("TreebarAvailability '{0}' is not recognized.", treeBarAvailability));
			}
		}

		/// <summary>
		/// if the XML configuration does not specify the availability of the treebar
		/// (e.g. treeBarAvailability="Required"), then use this.
		/// </summary>
		protected virtual TreebarAvailability DefaultTreeBarAvailability => TreebarAvailability.Required;

		#endregion // Other methods

#region Component Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
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
