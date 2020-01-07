// Copyright (c) 2006-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using SIL.LCModel;
using SIL.LCModel.Application;

namespace LanguageExplorer.Areas.Lists.Tools.ReversalIndexPOS
{
	/// <summary />
	internal sealed class ReversalIndexPOSRecordList : ReversalListBase
	{
		internal const string ReversalEntriesPOS = "ReversalEntriesPOS";

		/// <summary />
		internal ReversalIndexPOSRecordList(StatusBar statusBar, ILcmServiceLocator serviceLocator, ISilDataAccessManaged decorator, IReversalIndex reversalIndex)
			: base(ReversalEntriesPOS, statusBar, decorator, true, new VectorPropertyParameterObject(reversalIndex.PartsOfSpeechOA, "Possibilities", CmPossibilityListTags.kflidPossibilities))
		{
			m_fontName = serviceLocator.WritingSystemManager.Get(reversalIndex.WritingSystem).DefaultFontName;
			m_oldLength = 0;
		}

		#region Overrides of RecordList

		protected override IEnumerable<int> GetObjectSet()
		{
			return ((ICmPossibilityList)OwningObject).PossibilitiesOS.ToHvoArray();
		}

		/// <summary />
		protected override ClassAndPropInfo GetMatchingClass(string className)
		{
			if (className != "PartOfSpeech")
			{
				return null;
			}
			// A possibility list only allows one type of possibility to be owned in the list.
			return m_cache.DomainDataByFlid.MetaDataCache.GetClassName(((ICmPossibilityList)OwningObject).ItemClsid) != className ? null : m_insertableClasses.FirstOrDefault(cpi => cpi.signatureClassName == className);
		}

		/// <summary />
		public override void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			if (OwningObject != null && OwningObject.Hvo != hvo)
			{
				return;     // This PropChanged doesn't really apply to us.
			}
			if (tag == m_flid)
			{
				ReloadList();
			}
			else
			{
				base.PropChanged(hvo, tag, ivMin, cvIns, cvDel);
			}
		}

		#endregion

		#region Overrides of ReversalListBase

		/// <summary />
		protected override ICmObject NewOwningObject(IReversalIndex ri)
		{
			return ri.PartsOfSpeechOA;
		}

		#endregion
	}
}