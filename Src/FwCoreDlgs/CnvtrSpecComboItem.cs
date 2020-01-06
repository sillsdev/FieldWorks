// Copyright (c) 2003-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary />
	internal sealed class CnvtrSpecComboItem
	{
		/// <summary />
		internal CnvtrSpecComboItem(string name, string specs)
		{
			Name = name;
			Specs = specs;
		}

		/// <summary />
		public string Name { get; set; }

		/// <summary />
		public string Specs { get; set; }

		/// <summary />
		public override string ToString()
		{
			return Name;
		}
	}
}