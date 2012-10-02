//--------------------------------------------------------------------------------------------------
// <copyright file="Utility.cs" company="Microsoft">
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
// Utility methods for the Wix NAntTasks project.
// </summary>
//--------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.NAntTasks
{
	using System;

	using NAnt.Core;
	using NAnt.Core.Util;

	/// <summary>
	/// Utility methods for the Wix NAntTasks project.
	/// </summary>
	internal sealed class Utility
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
		private Utility()
		{
		}
		#endregion

		#region Properties
		//==========================================================================================
		// Properties
		//==========================================================================================

		#endregion

		#region Methods
		//==========================================================================================
		// Methods
		//==========================================================================================

		/// <summary>
		/// Surrounds the path with quotes if it has any embedded spaces.
		/// </summary>
		/// <param name="path">The path to quote.</param>
		/// <returns>The quoted path if it needs quotes.</returns>
		public static string QuotePathIfNeeded(string path)
		{
			// If the path is not null/emtpy, if it contains the space character somewhere,
			// and if it's not already surround in quotes, then surround it in quotes.
			if (!StringUtils.IsNullOrEmpty(path) && path.IndexOf(' ') >= 0)
			{
				if (path[0] != '"' || path[path.Length - 1] != '"' || path.Length == 1)
				{
					path = "\"" + path + "\"";
				}
			}
			return path;
		}

		/// <summary>
		/// Verifies that the specified argument is not null and throws an exception if it is.
		/// </summary>
		/// <param name="argument">The argument to check.</param>
		/// <param name="argumentName">The name of the argument.</param>
		public static void VerifyArgumentNonNull(object argument, string argumentName)
		{
			if (argument == null)
			{
				throw new ArgumentNullException(argumentName);
			}
		}

		/// <summary>
		/// Verifies that the specifed string argument is not null and not empty and throws an
		/// exception if it is.
		/// </summary>
		/// <param name="argument">The argument to check.</param>
		/// <param name="argumentName">The name of the argument.</param>
		public static void VerifyStringArgument(string argument, string argumentName)
		{
			VerifyArgumentNonNull(argument, argumentName);
			if (argumentName.Length == 0)
			{
				throw new ArgumentException("An empty string is not allowed.", argumentName);
			}
		}
		#endregion
	}
}