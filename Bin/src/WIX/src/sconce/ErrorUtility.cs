//-------------------------------------------------------------------------------------------------
// <copyright file="ErrorUtility.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
//
//    The use and distribution terms for this software are covered by the
//    Common Public License 1.0 (http://opensource.org/licenses/cpl.php)
//    which can be found in the file CPL.TXT at the root of this distribution.
//    By using this software in any fashion, you are agreeing to be bound by
//    the terms of this license.
//
//    You must not remove this notice, or any other, from this software.
// </copyright>
//
// <summary>
// Contains the ErrorUtility class.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure
{
	using System;
	using System.Threading;

	/// <summary>
	/// Provides miscellaneous error-related helper methods to the project.
	/// </summary>
	public sealed class ErrorUtility
	{
		#region Member Variables
		//==========================================================================================
		// Member Variables
		//==========================================================================================

		#endregion

		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		/// <summary>
		/// Prevent direct instantiation of this class.
		/// </summary>
		private ErrorUtility()
		{
		}
		#endregion

		#region Methods
		//==========================================================================================
		// Methods
		//==========================================================================================

		/// <summary>
		/// Returns a value indicating if the error is "unrecoverable," meaning that it should not
		/// be swallowed by an exception handler.
		/// </summary>
		/// <param name="e">The exception to test.</param>
		/// <returns>true if <paramref name="e"/> is "unrecoverable" and should be rethrown; otherwise, false.</returns>
		public static bool IsExceptionUnrecoverable(Exception e)
		{
			return (e is ThreadAbortException || e is StackOverflowException);
		}
		#endregion
	}
}
