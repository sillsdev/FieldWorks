// Copyright (c) 2005-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using LanguageExplorer.Areas;
using LanguageExplorer.LcmUi;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.WritingSystems;

namespace LanguageExplorer.Dumpster
{
#if RANDYTODO
	// TODO: I don't expect this class to survive, but its useful code moved elsewhere, as ordinary event handlers.
/*
<listener assemblyPath="LanguageExplorer.dll" class="LanguageExplorer.Dumpster.ReversalListener">
	<parameters clerk="AllReversalEntries"/>
</listener>

<clerk id="AllReversalEntries">
	<dynamicloaderinfo assemblyPath="LanguageExplorer.dll" class="LanguageExplorer.Dumpster.ReversalEntryClerk"/>
	<recordList owner="ReversalIndex" property="AllEntries">
		<dynamicloaderinfo assemblyPath="LanguageExplorer.dll" class="LanguageExplorer.Areas.Lexicon.AllReversalEntriesRecordList"/>
	</recordList>
	<filters/>
	<sortMethods>
		<sortMethod label="Form" assemblyPath="Filters.dll" class="SIL.FieldWorks.Filters.PropertyRecordSorter" sortProperty="ShortName"/>
	</sortMethods>
</clerk>
*/
#endif
	/// <summary>
	/// A listener class for reversal issues.
	/// This class currently handles these issues:
	/// 1. 'Find' dlg for reversal entries.
	/// 2.
	/// </summary>
	internal sealed class ReversalListener : IFlexComponent, IDisposable
	{
		private int instanceID = 0x00000F0;

		#region IDisposable & Co. implementation

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed { get; private set; }

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~ReversalListener()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// <summary>
		///
		/// </summary>
		/// <remarks>Must not be virtual.</remarks>
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		private void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
			{
				return;
			}

			if (disposing)
			{
				// Dispose managed resources here.
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			PropertyTable = null;
			Publisher = null;
			Subscriber = null;

			IsDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		#region Implementation of IPropertyTableProvider

		/// <summary>
		/// Placement in the IPropertyTableProvider interface lets FwApp call IPropertyTable.DoStuff.
		/// </summary>
		public IPropertyTable PropertyTable { get; private set; }

		#endregion

		#region Implementation of IPublisherProvider

		/// <summary>
		/// Get the IPublisher.
		/// </summary>
		public IPublisher Publisher { get; private set; }

		#endregion

		#region Implementation of ISubscriberProvider

		/// <summary>
		/// Get the ISubscriber.
		/// </summary>
		public ISubscriber Subscriber { get; private set; }

		#endregion

		#region Implementation of IFlexComponent

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		public void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			FlexComponentParameters.CheckInitializationValues(flexComponentParameters, new FlexComponentParameters(PropertyTable, Publisher, Subscriber));

			PropertyTable = flexComponentParameters.PropertyTable;
			Publisher = flexComponentParameters.Publisher;
			Subscriber = flexComponentParameters.Subscriber;

			var cache = PropertyTable.GetValue<LcmCache>("cache");
			var wsMgr = cache.ServiceLocator.WritingSystemManager;
			cache.DomainDataByFlid.BeginNonUndoableTask();
			var usedWses = new List<CoreWritingSystemDefinition>();
			foreach (var rev in cache.LanguageProject.LexDbOA.ReversalIndexesOC)
			{
				usedWses.Add(wsMgr.Get(rev.WritingSystem));
				if (rev.PartsOfSpeechOA == null)
				{
					rev.PartsOfSpeechOA = cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
				}
				rev.PartsOfSpeechOA.ItemClsid = PartOfSpeechTags.kClassId;
			}
			var corruptReversalIndices = new List<IReversalIndex>();
			foreach (var rev in cache.LanguageProject.LexDbOA.ReversalIndexesOC)
			{
				// Make sure each index has a name, if it is available from the writing system.
				if (string.IsNullOrEmpty(rev.WritingSystem))
				{
					// Delete a bogus IReversalIndex that has no writing system.
					// But, for now only store them for later deletion,
					// as immediate removal will wreck the looping.
					corruptReversalIndices.Add(rev);
					continue;
				}
				var revWs = wsMgr.Get(rev.WritingSystem);
				// TODO WS: is DisplayLabel the right thing to use here?
				rev.Name.SetAnalysisDefaultWritingSystem(revWs.DisplayLabel);
			}
			// Delete any corrupt reversal indices.
			foreach (IReversalIndex rev in corruptReversalIndices)
			{
				MessageBox.Show("Need to delete a corrupt reversal index (no writing system)", "Self-correction");
				cache.LangProject.LexDbOA.ReversalIndexesOC.Remove(rev);	// does this accomplish anything?
			}

			// Set up for the reversal index combo box or dropdown menu.
			var reversalIndexGuid = ReversalIndexEntryUi.GetObjectGuidIfValid(PropertyTable, "ReversalIndexGuid");
			if (reversalIndexGuid == Guid.Empty)
			{
				// We haven't established the reversal index yet.  Choose the first one available.
				var firstGuid = Guid.Empty;
				var reversalIds = cache.LanguageProject.LexDbOA.CurrentReversalIndices;
				if (reversalIds.Any())
				{
					firstGuid = reversalIds[0].Guid;
				}
				else if (cache.LanguageProject.LexDbOA.ReversalIndexesOC.Count > 0)
				{
					firstGuid = cache.LanguageProject.LexDbOA.ReversalIndexesOC.ToGuidArray()[0];
				}
				if (firstGuid != Guid.Empty)
				{
					SetReversalIndexGuid(firstGuid);
				}
			}
			cache.DomainDataByFlid.EndNonUndoableTask();
		}

		#endregion

		private void SetReversalIndexGuid(Guid reversalIndexGuid)
		{
			PropertyTable.SetProperty("ReversalIndexGuid", reversalIndexGuid.ToString(), true, true);
		}

		#region XCore Message handlers

		#region Go Dlg

#if RANDYTODO
		// TODO: Move elsewhere.
		/// <summary>
		/// Handles the xCore message to go to a reversal entry.
		/// </summary>
		/// <param name="argument">The xCore Command object.</param>
		/// <returns>true</returns>
		public bool OnGotoReversalEntry(object argument)
		{
			using (var dlg = new ReversalEntryGoDlg())
			{
				dlg.ReversalIndex = Entry.ReversalIndex;
				var cache = PropertyTable.GetValue<LcmCache>("cache");
				dlg.SetDlgInfo(cache, null, PropertyTable, Publisher);
				if (dlg.ShowDialog() == DialogResult.OK)
				{
					// Can't Go to a subentry, so we have to go to its main entry.
					var selEntry = (IReversalIndexEntry) dlg.SelectedObject;
					Publisher.Publish("JumpToRecord", selEntry.MainEntry.Hvo);
				}
			}
			return true;
		}

		private IReversalIndexEntry Entry
		{
			get
			{
				IReversalIndexEntry rie = null;
#if RANDYTODO
				// TODO: Use another mechanism to get the record list in whatever replaces this listener.
				// TODO: It will be an instance of "ReversalEntryRecordList" (see above for details)
				string recordListId = XmlUtils.GetMandatoryAttributeValue(m_configurationParameters, "clerk");
				string propertyName = RecordList.GetCorrespondingPropertyName(recordListId);
				var recordList = PropertyTable.GetValue<IRecordList>(propertyName);
				if (recordList != null)
					rie = recordList.CurrentObject as IReversalIndexEntry;
#endif
				return rie;
			}
		}
#endif

#if RANDYTODO
		public virtual bool OnDisplayGotoReversalEntry(object commandObject,
			ref UIItemDisplayProperties display)
		{
			IReversalIndexEntry rie = Entry;
			if (rie == null || rie.Owner.Hvo == 0)
			{
				display.Enabled = display.Visible = false;
			}
			else
			{
				display.Enabled = rie.ReversalIndex.EntriesOC.Count > 1 && InFriendlyArea;
				display.Visible = InFriendlyArea;
			}
			return true; //we've handled this
		}
#endif
		#endregion Go Dlg

		#region Reversal Index Combo

#if RANDYTODO
		/// <summary>
		/// Called (by xcore) to control display params of the reversal index menu, e.g. whether it should be enabled.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayReversalIndexHvo(object commandObject, ref UIItemDisplayProperties display)
		{
			// Do NOT check InFriendlyArea. This menu should be enabled in every context where it occurs at all.
			// And, it gets tested during creation of the pane bar, BEFORE the properties InFriendlyArea uses
			// are set, so we get inaccurate answers.
			display.Enabled = true; // InFriendlyArea;
			display.Visible = display.Enabled;

			return true; // We dealt with it.
		}

		/// <summary>
		/// This is called when XCore wants to display something that relies on the list with the
		/// id "ReversalIndexList"
		/// </summary>
		/// <param name="parameter"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public bool OnDisplayReversalIndexList(object parameter, ref UIListDisplayProperties display)
		{
			display.List.Clear();
			var lp = m_propertyTable.GetValue<LcmCache>("cache").LanguageProject;
			// List all existing reversal indexes.  (LT-4479, as amended)
			// But only for analysis wss
			foreach (IReversalIndex ri in from ri in lp.LexDbOA.ReversalIndexesOC
										  where lp.AnalysisWss.Contains(ri.WritingSystem)
										  select ri)
			{
				display.List.Add(ri.ShortName, ri.Guid.ToString(), null, null);
			}
			display.List.Sort();
			return true; // We handled this, no need to ask anyone else.
		}
#endif

		#endregion Reversal Index Combo

		/// <summary>
		///
		/// </summary>
		/// <remarks>
		/// This is something of a hack until we come up with a generic solution to
		/// the problem on how to control we are CommandSet are handled by listeners are
		/// visible.
		/// </remarks>
		private bool InFriendlyArea
		{
			get
			{
				var areaChoice = PropertyTable.GetValue<string>(AreaServices.AreaChoice);
				var toolFor = PropertyTable.GetValue<string>($"{AreaServices.ToolForAreaNamed_}{AreaServices.LexiconAreaMachineName}");

				return areaChoice == AreaServices.LexiconAreaMachineName && toolFor.StartsWith("reversal");
			}
		}

		#endregion XCore Message handlers
	}
}
