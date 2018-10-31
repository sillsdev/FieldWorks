// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary />
	public class CnvtrTypeComboItem
	{
		/// <summary />
		public CnvtrTypeComboItem(string name, ConverterType type, string implementType)
		{
			Name = name;
			Type = type;
			ImplementType = implementType;
		}

		/// <summary />
		public string Name { get; }

		/// <summary />
		public ConverterType Type { get; }

		/// <summary />
		public string ImplementType { get; }

		/// <summary />
		public override string ToString()
		{
			return Name;
		}
	}
}