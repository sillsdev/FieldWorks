// Copyright (c) 2009-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.Controls.DetailControls.Slices
{
	/// <summary />
	internal sealed class RecordReferenceVectorSlice : CustomReferenceVectorSlice
	{
		/// <summary />
		internal RecordReferenceVectorSlice()
			: base(new RecordReferenceVectorLauncher())
		{
		}
	}
}
