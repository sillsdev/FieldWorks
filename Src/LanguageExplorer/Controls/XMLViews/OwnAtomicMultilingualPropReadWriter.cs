// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// FieldReadWriter for strings stored in a multilingual prop of an object
	/// owned in an atomic property of the base object.
	/// </summary>
	internal class OwnAtomicMultilingualPropReadWriter : OwnMultilingualPropReadWriter
	{
		int m_flidObj;
		int m_clid; // to create if missing

		public OwnAtomicMultilingualPropReadWriter(LcmCache cache, int flidString, int ws, int flidObj, int clid)
			: base(cache, flidString, ws)
		{
			m_flidObj = flidObj;
			m_clid = clid;
		}

		public override ITsString CurrentValue(int hvo)
		{
			return base.CurrentValue(m_sda.get_ObjectProp(hvo, m_flidObj));
		}

		internal override List<int> FieldPath
		{
			get
			{
				var fieldPath = base.FieldPath;
				fieldPath.Insert(0, m_flidObj);
				return fieldPath;
			}
		}

		public override void SetNewValue(int hvo, ITsString tss)
		{
			var ownedAtomicObj = m_sda.get_ObjectProp(hvo, m_flidObj);
			var fHadObject = ownedAtomicObj != 0;
			if (!fHadObject)
			{
				if (m_clid == 0)
				{
					return;
				}
				ownedAtomicObj = m_sda.MakeNewObject(m_clid, hvo, m_flidObj, -2);
			}
			base.SetNewValue(ownedAtomicObj, tss);
		}
	}
}