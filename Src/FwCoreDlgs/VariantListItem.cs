// Copyright (c) 2019-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.WritingSystems;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary/>
	internal sealed class VariantListItem
	{
		private VariantSubtag _variant;

		/// <summary/>
		internal VariantListItem(VariantSubtag variant)
		{
			_variant = variant;
		}

		/// <summary/>
		internal string Code => _variant?.Code;

		/// <summary/>
		internal string Name => _variant == null ? "None" : _variant.Name;

		/// <summary/>
		public override string ToString()
		{
			return Code;
		}
	}
}