// Copyright (c) 2003-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using ECInterfaces;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary />
	internal sealed class CnvtrDataComboItem
	{
		/// <summary />
		internal CnvtrDataComboItem(string name, ConvType type)
		{
			Name = name;
			Type = type;
		}

		/// <summary />
		internal string Name { get; set; }

		/// <summary />
		internal ConvType Type { get; set; }

		/// <summary />
		public override string ToString()
		{
			return Name;
		}
	}
}