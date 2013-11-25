// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: LexText.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.XWorks.LexText
{
	/// <summary>
	/// Summary description for LexText.
	/// </summary>
	public class LexText
	{
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Application entry point. If Flex isn't already running,
		/// an instance of the app is created.
		/// </summary>
		/// <param name="rgArgs">Command-line arguments</param>
		/// <returns>0</returns>
		/// -----------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Portability", "ExitCodeIsLimitedOnUnixRule",
			Justification = "Gendarme bug on Windows: doesn't recognize that we're returning 0")]
		[STAThread]
		public static int Main(string[] rgArgs)
		{
			using (FieldWorks.StartFwApp(FwUtils.ksFlexAbbrev, rgArgs))
			{
				return 0;
			}
		}
	}
}
