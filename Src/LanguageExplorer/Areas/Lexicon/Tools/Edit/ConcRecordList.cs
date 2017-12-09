// Copyright (c) 2017-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using SIL.LCModel;

namespace LanguageExplorer.Areas.Lexicon.Tools.Edit
{
	/// <summary>
	/// This one is used for concordances. Currently a concordance never controls the record bar, and indicating this
	/// prevents a variety of activity that undesirably calls CurrentObject, which causes problems because in a concordance
	/// list the HVOs don't correspond to real FDO objects.
	/// </summary>
	/// <remarks>
	/// Only used by LanguageExplorer.Areas.Lexicon.Tools.Edit.FindExampleSentenceDlg.
	/// </remarks>
	internal sealed class ConcRecordList : TemporaryRecordList
	{
		/// <summary />
		internal ConcRecordList(StatusBar statusBar, LcmCache cache, ILexSense owningSense)
			: base("OccurrencesOfSense", statusBar, null, AreaServices.Default, null, false, false, new ConcDecorator(cache.ServiceLocator), true, cache.MetaDataCacheAccessor.GetFieldId2(LexSenseTags.kClassId, "Occurrences", false), owningSense, "Occurrences")
		{
		}

		#region Overrides of RecordList/TemporaryRecordList

		public override bool IsControllingTheRecordTreeBar
		{
			get
			{
				return false;
			}
			set
			{
				// Do not do anything here, unless you want to manage the "RecordList.RecordListRepository.ActiveRecordList" property.
			}
		}

		#endregion Overrides of RecordList/TemporaryRecordList
	}
}
