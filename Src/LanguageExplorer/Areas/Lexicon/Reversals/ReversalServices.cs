// Copyright (c) 2019-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using System.Windows.Forms;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.DomainServices;

namespace LanguageExplorer.Areas.Lexicon.Reversals
{
	/// <summary>
	/// Common code for the two reversal tools
	/// </summary>
	internal static class ReversalServices
	{
		internal static IRecordList AllReversalEntriesFactoryMethod(LcmCache cache, FlexComponentParameters flexComponentParameters, string recordListId, StatusBar statusBar)
		{
			Require.That(recordListId == LanguageExplorerConstants.AllReversalEntries, $"I don't know how to create a record list with an ID of '{recordListId}', as I can only create one with an id of '{LanguageExplorerConstants.AllReversalEntries}'.");
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
			var currentGuid = ReversalIndexServices.GetObjectGuidIfValid(flexComponentParameters.PropertyTable, LanguageExplorerConstants.ReversalIndexGuid);
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
