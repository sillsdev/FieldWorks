// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// FieldReadWriter for strings stored in a multilingual prop of the FIRST object
	/// owned in an sequence property of the base object.
	/// </summary>
	internal class OwnSeqMultilingualPropReadWriter : OwnMultilingualPropReadWriter
	{
		int m_flidObj;
		int m_clid; // to create if missing

		public OwnSeqMultilingualPropReadWriter(LcmCache cache, int flidString, int ws, int flidObj, int clid)
			: base(cache, flidString, ws)
		{
			m_flidObj = flidObj;
			m_clid = clid;
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

		public override ITsString CurrentValue(int hvo)
		{
			return m_sda.get_VecSize(hvo, m_flidObj) > 0 ? base.CurrentValue(m_sda.get_VecItem(hvo, m_flidObj, 0)) : null;
		}

		public override void SetNewValue(int hvo, ITsString tss)
		{
			var firstSeqObj = 0;
			var fHadOwningItem = m_sda.get_VecSize(hvo, m_flidObj) > 0;
			if (fHadOwningItem)
			{
				firstSeqObj = m_sda.get_VecItem(hvo, m_flidObj, 0);
			}
			else
			{
				// make first vector item if we know the class to base it on.
				if (m_clid == 0)
				{
					return;
				}
				firstSeqObj = m_sda.MakeNewObject(m_clid, hvo, m_flidObj, 0);
			}
			base.SetNewValue(firstSeqObj, tss);
		}
	}
}