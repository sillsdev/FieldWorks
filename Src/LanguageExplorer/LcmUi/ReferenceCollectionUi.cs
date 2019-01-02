// Copyright (c) 2006-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using LanguageExplorer.Areas;
using SIL.LCModel;
using SIL.LCModel.Core.Cellar;

namespace LanguageExplorer.LcmUi
{
	/// <summary>
	/// Handling reference collections is rather minimal at the moment, basically allowing a
	/// different context menu to be used.
	/// </summary>
	public class ReferenceCollectionUi : VectorReferenceUi
	{
		public ReferenceCollectionUi(LcmCache cache, ICmObject rootObj, int referenceFlid, int targetHvo)
			: base(cache, rootObj, referenceFlid, targetHvo)
		{
			Debug.Assert(m_iType == CellarPropertyType.ReferenceCollection);
		}

		protected override string ContextMenuId => m_cache.DomainDataByFlid.MetaDataCache.GetDstClsId(m_flid) == PhEnvironmentTags.kClassId ? AreaServices.mnuEnvReferenceChoices : base.ContextMenuId;
	}
}