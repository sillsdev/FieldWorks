// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.XWorks;

namespace LanguageExplorer.Areas.Lists.Tools.ReversalIndexPOS
{
	/// <summary>
	/// Summary description for ListExtension.
	/// </summary>
	internal sealed class ReversalIndexPOSRecordList : RecordList
	{
		/// <summary />
		internal ReversalIndexPOSRecordList(IFdoServiceLocator serviceLocator, ISilDataAccessManaged decorator, IReversalIndex reversalIndex)
			: base(decorator, true, CmPossibilityListTags.kflidPossibilities, reversalIndex.PartsOfSpeechOA, string.Empty)
		{
			m_flid = CmPossibilityListTags.kflidPossibilities;
			m_fontName = serviceLocator.WritingSystemManager.Get(reversalIndex.WritingSystem).DefaultFontName;
			m_oldLength = 0;
		}

		/// <summary />
		protected override IEnumerable<int> GetObjectSet()
		{
			ICmPossibilityList list = m_owningObject as ICmPossibilityList;
			return list.PossibilitiesOS.ToHvoArray();
		}

		/// <summary />
		protected override ClassAndPropInfo GetMatchingClass(string className)
		{
			if (className != "PartOfSpeech")
				return null;

			// A possibility list only allows one type of possibility to be owned in the list.
			ICmPossibilityList pssl = (ICmPossibilityList)m_owningObject;
			int possClass = pssl.ItemClsid;
			string sPossClass = m_cache.DomainDataByFlid.MetaDataCache.GetClassName((int)possClass);
			if (sPossClass != className)
				return null;
			foreach(ClassAndPropInfo cpi in m_insertableClasses)
			{
				if (cpi.signatureClassName == className)
				{
					return cpi;
				}
			}
			return null;
		}

		#region IVwNotifyChange implementation

		/// <summary />
		public override void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			CheckDisposed();

			if (m_owningObject != null && m_owningObject.Hvo != hvo)
				return;		// This PropChanged doesn't really apply to us.
			if (tag == m_flid)
				ReloadList();
			else
				base.PropChanged(hvo, tag, ivMin, cvIns, cvDel);
		}

		#endregion IVwNotifyChange implementation
	}
}