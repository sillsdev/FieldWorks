// Copyright (c) 2004-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer
{
	/// <summary>
	/// Interface to be implemented by users of the IPaneBar interface.
	/// </summary>
	public interface IPaneBarUser
	{
		/// <summary>
		/// Get or set the IPaneBar for its user.
		/// </summary>
		IPaneBar MainPaneBar
		{
			get;
			set;
		}
	}
}