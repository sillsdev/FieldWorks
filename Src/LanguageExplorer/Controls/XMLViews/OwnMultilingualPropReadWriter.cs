// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using SIL.LCModel;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// FieldReadWriter for strings stored in multilingual props of an object.
	/// </summary>
	internal class OwnMultilingualPropReadWriter : OwnStringPropReadWriter
	{
		private bool m_fFieldAllowsMultipleRuns;
		public OwnMultilingualPropReadWriter(LcmCache cache, int flid, int ws)
			: base(cache, flid, ws)
		{

			try
			{
				var fieldType = m_sda.MetaDataCache.GetFieldType(flid);
				m_fFieldAllowsMultipleRuns = fieldType == (int)CellarPropertyType.MultiString;
			}
			catch (KeyNotFoundException)
			{
				m_fFieldAllowsMultipleRuns = true; // Possibly a decorator field??
			}
		}

		public override ITsString CurrentValue(int hvo)
		{
			var hvoStringOwner = hvo;
			if (m_ghostParentHelper != null)
			{
				hvoStringOwner = m_ghostParentHelper.GetOwnerOfTargetProperty(hvo);
			}
			return hvoStringOwner == 0 ? null : m_sda.get_MultiStringAlt(hvoStringOwner, m_flid, m_ws);
		}

		// In this subclass we're setting a multistring.
		protected override void SetStringValue(int hvoStringOwner, ITsString tss)
		{
			if (!m_fFieldAllowsMultipleRuns && tss.RunCount > 1)
			{
				// Illegally trying to store a multi-run TSS in a single-run field. This will fail.
				// Typically it's just that we tried to insert an English comma or similar.
				// Patch it up by making the whole string take on the properties of the first run.
				var bldr = tss.GetBldr();
				bldr.SetProperties(0, bldr.Length, tss.get_Properties(0));
				tss = bldr.GetString();
			}
			m_sda.SetMultiStringAlt(hvoStringOwner, m_flid, m_ws, tss);
		}
	}
}