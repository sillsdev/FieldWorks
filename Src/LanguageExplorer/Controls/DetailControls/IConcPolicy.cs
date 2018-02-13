// Copyright (c) 2005-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary>
	/// This interface is used to configure a TwoLevelConc. It supplies the basic
	/// information: the list of top-level objects that are displayed, and the
	/// flid used to obtain the key to display for each.
	///
	/// Enhance JohnT: add methods to handle spelling change, etc.
	/// </summary>
	internal interface IConcPolicy
	{
		/// <summary>
		/// The number of slices in the top-level concordance.
		/// </summary>
		int Count { get; }
		/// <summary>
		/// Get the ith item (HVO) to display. If this returns 0, the key to display
		/// is obtained from KeyFor without calling FlidFor.
		/// </summary>
		int Item(int i);
		/// <summary>
		/// Get the flid to use to obtain a key for the ith item. If it answers 0,
		/// Use the KeyFor instead.
		/// </summary>
		int FlidFor(int islice, int hvo);
		/// <summary>
		/// Get the key to display for the ith slice, given its index and hvo.
		/// This method is used if Item(islice) returns 0, or if FlidFor(islice, hvo)
		/// returns 0. If it is used, the key will definitely not be editable.
		/// </summary>
		ITsString KeyFor(int islice, int hvo);
	}
}