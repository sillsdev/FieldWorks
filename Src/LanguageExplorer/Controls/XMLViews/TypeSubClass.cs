// Copyright (c) 2003-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>Specify particular handling for some types of LexReference.</summary>
	public enum TypeSubClass
	{
		/// <summary>sequence, collection, or simple pair</summary>
		Normal = 0,
		/// <summary>normal name of tree or unequal pair (refers to 2nd and following elements of vector)</summary>
		Forward,
		/// <summary>reverse name of tree or unequal pair (refers back to first element of vector)</summary>
		Reverse
	}
}