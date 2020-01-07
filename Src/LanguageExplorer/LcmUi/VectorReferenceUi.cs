// Copyright (c) 2006-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using SIL.LCModel;
using SIL.LCModel.Core.Cellar;

namespace LanguageExplorer.LcmUi
{
	/// <summary>
	/// Handles things common to ReferenceSequence and ReferenceCollection classes.
	/// </summary>
	public class VectorReferenceUi : ReferenceBaseUi
	{
		protected int m_iCurrent = -1;
		protected CellarPropertyType m_iType;

		public VectorReferenceUi(LcmCache cache, ICmObject rootObj, int referenceFlid, int targetHvo)
			: base(cache, rootObj, referenceFlid, targetHvo)
		{
			m_iType = (CellarPropertyType)cache.DomainDataByFlid.MetaDataCache.GetFieldType(m_flid);
			Debug.Assert(m_iType == CellarPropertyType.ReferenceSequence || m_iType == CellarPropertyType.ReferenceCollection);
		}
	}
}