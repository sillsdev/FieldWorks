// Copyright (c) 2009-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Controls
{
	/// <summary>Struct pairing a field ID with a TsString</summary>
	internal struct SearchField
	{
		/// <summary />
		internal SearchField(int flid, ITsString tss)
		{
			Flid = flid;
			String = tss;
		}

		/// <summary />
		internal int Flid { get; }

		/// <summary />
		internal ITsString String { get; }
	}
}