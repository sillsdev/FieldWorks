// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: InstallationException.cs
// Responsibility: FieldWorks team
// Last reviewed:
// --------------------------------------------------------------------------------------------
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
