// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: InstallationException.cs
// Responsibility: FieldWorks team
// Last reviewed:

using System;
using SIL.FieldWorks.Resources;

namespace SIL.FieldWorks.Common.FwUtils
{

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// An InstallationException is thrown when an error is encountered
	/// that is the result of a program installation problem.  For example
	/// missing files, corrupt COM interfaces, other nasty things.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class InstallationException : Exception
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Default Constructor
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public InstallationException() : base(null)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Default Constructor
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public InstallationException(Exception e) :
			base(ResourceHelper.GetResourceString("kstidInvalidInstallation"), e)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Default Constructor
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public InstallationException(string message, Exception e) :
			base(message, e)
		{
		}
	}
}
