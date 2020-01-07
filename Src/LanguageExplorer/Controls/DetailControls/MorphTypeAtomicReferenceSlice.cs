// Copyright (c) 2006-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary />
	internal class MorphTypeAtomicReferenceSlice : PossibilityAtomicReferenceSlice
	{
		/// <summary />
		public MorphTypeAtomicReferenceSlice(LcmCache cache, ICmObject obj, int flid)
			: base(new MorphTypeAtomicLauncher(), cache, obj, flid)
		{
		}
	}
}