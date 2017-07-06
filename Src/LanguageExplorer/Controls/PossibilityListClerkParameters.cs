// Copyright (c) 2016-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;

namespace LanguageExplorer.Controls
{
	/// <summary>
	/// Class that bundles the parameters used to create a RecordClerk for a possibility list.
	/// </summary>
	internal class PossibilityListClerkParameters
	{
		/// <summary>
		/// The clerk identifier
		/// </summary>
		internal string ClerkIdentifier { get; }
		/// <summary>
		/// The list that owns the objects.
		/// </summary>
		internal ICmPossibilityList OwningList { get; }
		/// <summary>
		/// 'true' to expand the tree control.
		/// </summary>
		internal bool Expand { get; }
		/// <summary>
		/// 'true; if the list has nested items.
		/// </summary>
		internal bool Hierarchical { get; }
		/// <summary>
		/// 'true' if we also show the list item's abbreviation.
		/// </summary>
		internal bool IncludeAbbr { get; }
		/// <summary>
		/// Writing System to use for shownig the list itme in the tree control.
		/// </summary>
		internal string Ws { get; }

		internal PossibilityListClerkParameters(string clerkIdentifier, ICmPossibilityList owningList, bool expand, bool hierarchical, bool includeAbbr, string ws)
		{
			ClerkIdentifier = clerkIdentifier;
			OwningList = owningList;
			Expand = expand;
			Hierarchical = hierarchical;
			IncludeAbbr = includeAbbr;
			Ws = ws;
		}
	}
}