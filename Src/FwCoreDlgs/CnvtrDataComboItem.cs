// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using ECInterfaces;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary />
	public class CnvtrDataComboItem
	{
		/// <summary />
		public CnvtrDataComboItem(string name, ConvType type)
		{
			Name = name;
			Type = type;
		}

		/// <summary />
		public string Name { get; set; }

		/// <summary />
		public ConvType Type { get; set; }

		/// <summary />
		public override string ToString()
		{
			return Name;
		}
	}
}