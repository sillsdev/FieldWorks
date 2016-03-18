// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.FDO;

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
		internal string ClerkIdentifier { get; private set; }
		/// <summary>
		/// The list that owns the objects.
		/// </summary>
		internal ICmPossibilityList OwningList { get; private set; }
		/// <summary>
		/// 'true' to expand the tree control.
		/// </summary>
		internal bool Expand { get; private set; }
		/// <summary>
		/// 'true; if the list has nested items.
		/// </summary>
		internal bool Hierarchical { get; private set; }
		/// <summary>
		/// 'true' if we also show the list item's abbreviation.
		/// </summary>
		internal bool IncludeAbbr { get; private set; }
		/// <summary>
		/// Writing System to use for shownig the list itme in the tree control.
		/// </summary>
		internal string Ws { get; private set; }

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