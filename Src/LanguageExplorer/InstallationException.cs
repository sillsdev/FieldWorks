// Copyright (c) 2003-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace LanguageExplorer
{
	/// <summary>
	/// An InstallationException is thrown when an error is encountered
	/// that is the result of a program installation problem.  For example
	/// missing files, corrupt COM interfaces, other nasty things.
	/// </summary>
	internal sealed class InstallationException : Exception
	{
		/// <summary />
		internal InstallationException(string message, Exception e) :
			base(message, e)
		{
		}
	}
}