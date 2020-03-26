// Copyright (c) 2003-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary />
	internal sealed class CnvtrTypeComboItem
	{
		/// <summary />
		internal CnvtrTypeComboItem(string name, ConverterType type, string implementType)
		{
			Name = name;
			Type = type;
			ImplementType = implementType;
		}

		/// <summary />
		internal string Name { get; }

		/// <summary />
		internal ConverterType Type { get; }

		/// <summary />
		internal string ImplementType { get; }

		/// <summary />
		public override string ToString()
		{
			return Name;
		}
	}
}