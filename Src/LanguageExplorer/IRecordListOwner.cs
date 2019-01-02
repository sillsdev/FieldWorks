// Copyright (c) 2004-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer
{
	/// <summary>
	/// This interface is implemented to help handle side-effects of changing the contents of an
	/// object that may be stored in a list.  Its single method returns either null or the
	/// list object stored under the given name.
	/// </summary>
	internal interface IRecordListOwner
	{
		/// <summary>Find the IRecordListUpdater object with the given name.</summary>
		IRecordListUpdater FindRecordListUpdater(string name);
	}
}