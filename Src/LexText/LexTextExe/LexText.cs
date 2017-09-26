// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

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
		[STAThread]
		public static int Main(string[] rgArgs)
		{
			using (FieldWorks.StartFwApp(rgArgs))
			{
				return 0;
			}
		}
	}
}
