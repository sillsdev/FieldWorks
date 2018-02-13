// Copyright (c) 2005-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary>
	/// This class displays an atomic reference property. Currently it must be for a property for which
	/// where ReferenceTargetCandidates returns a useful list of results.
	/// </summary>
	internal class AtomicRefTypeAheadSlice : ViewPropertySlice
	{
		internal AtomicRefTypeAheadSlice(ICmObject obj, int flid) : base(new AtomicRefTypeAheadView(obj.Hvo, flid), obj, flid)
		{
		}
	}
}