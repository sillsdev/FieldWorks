// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.CoreImpl
{
	/// <summary>
	/// Interface main content controls may implement if they want to do more initialization after
	/// the main content control is laid out (e.g., when their true size is known).
	/// </summary>
	public interface IPostLayoutInit
	{
		/// <summary>
		/// Continue init, but after implementation has been laid out.
		/// </summary>
		void PostLayoutInit();
	}
}