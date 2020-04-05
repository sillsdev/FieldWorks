// Copyright (c) 2006-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;

namespace LanguageExplorer.Controls.DetailControls.Slices
{
	/// <summary />
	internal sealed class MorphTypeAtomicReferenceSlice : PossibilityAtomicReferenceSlice, IMorphTypeAtomicReferenceSlice
	{
		/// <summary />
		internal MorphTypeAtomicReferenceSlice(LcmCache cache, ICmObject obj, int flid)
			: base(new MorphTypeAtomicLauncher(), cache, obj, flid)
		{
		}
	}
}