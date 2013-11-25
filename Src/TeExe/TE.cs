// Copyright (c) 2002-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: TE.cs
// Responsibility: TE Team
//
// <remarks>
// Application entry point for TE.
// </remarks>

using System;
using System.Diagnostics.CodeAnalysis;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.TE
{
	/// <summary>
	/// The only method in this class is <see cref="Main"/>. All other methods should go
	/// in a Dll, so that NUnit tests can be written.
	/// </summary>
	public class TE
	{
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Application entry point. If TE isn't already running, an instance of the app is
		/// created.
		/// </summary>
		///
		/// <param name="rgArgs">Command-line arguments</param>
		///
		/// <returns>0</returns>
		/// -----------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Portability", "ExitCodeIsLimitedOnUnixRule",
			Justification = "Gendarme bug on Windows: doesn't recognize that we're returning 0")]
		[STAThread]
		public static int Main(string[] rgArgs)
		{
			using (FieldWorks.StartFwApp(FwUtils.ksTeAbbrev, rgArgs))
			{
				return 0;
			}
		}
	}
}
