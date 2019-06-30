// Copyright (c) 2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Areas.Lexicon.Reversals
{
	/// <summary>
	/// Common code for the two reversal tools
	/// </summary>
	internal static class ReversalServices
	{
		private static bool s_reversalIndicesAreKnownToExist;

		/// <summary>
		/// Make sure the required reversal indices exist.
		/// </summary>
		internal static void EnsureReversalIndicesExist(LcmCache cache, IPropertyTable propertyTable)
		{
			Guard.AgainstNull(cache, nameof(cache));
			Guard.AgainstNull(propertyTable, nameof(propertyTable));

			if (s_reversalIndicesAreKnownToExist)
			{
				return;
			}
			var wsMgr = cache.ServiceLocator.WritingSystemManager;
			NonUndoableUnitOfWorkHelper.Do(cache.ActionHandlerAccessor, () =>
			{
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
				foreach (var rev in corruptReversalIndices)
				{
					MessageBox.Show("Need to delete a corrupt reversal index (no writing system)", "Self-correction");
					// does this accomplish anything?
					cache.LangProject.LexDbOA.ReversalIndexesOC.Remove(rev);
				}
				// Set up for the reversal index combo box or dropdown menu.
				var reversalIndexGuid = RecordListServices.GetObjectGuidIfValid(propertyTable, "ReversalIndexGuid");
				if (reversalIndexGuid == Guid.Empty)
				{
					// We haven't established the reversal index yet. Choose the first one available.
					var firstGuid = Guid.Empty;
					var reversalIds = cache.LanguageProject.LexDbOA.CurrentReversalIndices;
					if (reversalIds.Any())
					{
						firstGuid = reversalIds[0].Guid;
					}
					else if (cache.LanguageProject.LexDbOA.ReversalIndexesOC.Any())
					{
						firstGuid = cache.LanguageProject.LexDbOA.ReversalIndexesOC.ToGuidArray()[0];
					}
					if (firstGuid != Guid.Empty)
					{
						propertyTable.SetProperty("ReversalIndexGuid", firstGuid.ToString(), true, true, SettingsGroup.LocalSettings);
					}
				}
			});
			s_reversalIndicesAreKnownToExist = true;
		}

		internal static IRecordList AllReversalEntriesFactoryMethod(LcmCache cache, FlexComponentParameters flexComponentParameters, string recordListId, StatusBar statusBar)
		{
			Require.That(recordListId == LexiconArea.AllReversalEntries, $"I don't know how to create a record list with an ID of '{recordListId}', as I can only create one with an id of '{LexiconArea.AllReversalEntries}'.");
			/*
			<clerk id="AllReversalEntries">
				<dynamicloaderinfo assemblyPath="LexEdDll.dll" class="SIL.FieldWorks.XWorks.LexEd.ReversalEntryClerk" />
				<recordList owner="ReversalIndex" property="AllEntries">
				<dynamicloaderinfo assemblyPath="LexEdDll.dll" class="SIL.FieldWorks.XWorks.LexEd.AllReversalEntriesRecordList" />
				</recordList>
				<filters />
				<sortMethods>
				<sortMethod label="Form" assemblyPath="Filters.dll" class="SIL.FieldWorks.Filters.PropertyRecordSorter" sortProperty="ShortName" />
				</sortMethods>
				<!--<recordFilterListProvider assemblyPath="Filters.dll" class="SIL.FieldWorks.Filters.WfiRecordFilterListProvider"/>-->
			</clerk>
			*/
			var currentGuid = RecordListServices.GetObjectGuidIfValid(flexComponentParameters.PropertyTable, "ReversalIndexGuid");
			IReversalIndex revIdx = null;
			if (currentGuid != Guid.Empty)
			{
				revIdx = (IReversalIndex)cache.ServiceLocator.GetObject(currentGuid);
				// This looks like our best chance to update a global "Current Reversal Index Writing System" value.
				WritingSystemServices.CurrentReversalWsId = cache.WritingSystemFactory.GetWsFromStr(revIdx.WritingSystem);
				// Generate and store the expected path to a configuration file specific to this reversal index.  If it doesn't
				// exist, code elsewhere will make up for it.
				var layoutName = Path.Combine(LcmFileHelper.GetConfigSettingsDir(cache.ProjectId.ProjectFolder), "ReversalIndex", revIdx.ShortName + LanguageExplorerConstants.DictionaryConfigurationFileExtension);
				flexComponentParameters.PropertyTable.SetProperty("ReversalIndexPublicationLayout", layoutName, true, true);
			}
			return new AllReversalEntriesRecordList(statusBar, cache.ServiceLocator, cache.ServiceLocator.GetInstance<ISilDataAccessManaged>(), revIdx);
		}
	}
}
