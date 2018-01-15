// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using SIL.LCModel;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// FieldReadWriter for strings stored in (non-multilingual) props of the object itself.
	/// </summary>
	internal class OwnStringPropReadWriter : FieldReadWriter
	{
		protected int m_flid;
		protected int m_flidType;
		protected int m_ws;
		protected LcmCache m_cache;

		public OwnStringPropReadWriter(LcmCache cache, int flid, int ws)
			: base(cache.MainCacheAccessor)
		{
			m_cache = cache;
			m_flid = flid;
			m_flidType = GetFlidType();
			m_ws = ws;
		}

		private int GetFlidType()
		{
			return m_cache.MetaDataCacheAccessor.GetFieldType(m_flid);
		}

		internal override List<int> FieldPath => new List<int>(new[] {m_flid} );

		public override ITsString CurrentValue(int hvo)
		{
			var hvoStringOwner = hvo;
			if (m_ghostParentHelper != null)
			{
				hvoStringOwner = m_ghostParentHelper.GetOwnerOfTargetProperty(hvo);
			}
			if (hvoStringOwner == 0)
			{
				return null; // hasn't been created yet.
			}
			if (m_flidType != (int) CellarPropertyType.Unicode)
			{
				return m_sda.get_StringProp(hvoStringOwner, m_flid);
			}
			var ustring = m_sda.get_UnicodeProp(hvoStringOwner, m_flid);
			// Enhance: For the time being Default Analysis Ws is sufficient. If there is ever
			// a Unicode vernacular field that is made Bulk Editable, we will need to rethink this code.
			return TsStringUtils.MakeString(ustring ?? string.Empty, m_cache.DefaultAnalWs);
		}

		public override void SetNewValue(int hvo, ITsString tss)
		{
			var hvoStringOwner = hvo;
			if (m_ghostParentHelper != null)
			{
				hvoStringOwner = m_ghostParentHelper.FindOrCreateOwnerOfTargetProp(hvo, m_flid);
			}
			if (m_flidType == (int) CellarPropertyType.Unicode)
			{
				SetUnicodeStringValue(hvoStringOwner, tss);
			}
			else
			{
				SetStringValue(hvoStringOwner, tss);
			}
		}

		private void SetUnicodeStringValue(int hvoStringOwner, ITsString tss)
		{
			var strValue = (tss == null) ? string.Empty : tss.Text;
			m_sda.set_UnicodeProp(hvoStringOwner, m_flid, strValue);
		}

		protected virtual void SetStringValue(int hvoStringOwner, ITsString tss)
		{
			m_sda.SetString(hvoStringOwner, m_flid, tss);
		}

		public override int WritingSystem => m_ws;
	}
}