// Copyright (c) 2009-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>Struct pairing a field ID with a TsString</summary>
	public struct SearchField
	{
		/// <summary />
		public SearchField(int flid, ITsString tss)
		{
			Flid = flid;
			String = tss;
		}

		/// <summary />
		public int Flid { get; }

		/// <summary />
		public ITsString String { get; }
	}
}