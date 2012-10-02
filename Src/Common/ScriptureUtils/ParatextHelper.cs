// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2006' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ParatextHelper.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using SIL.Utils;

namespace SIL.FieldWorks.Common.ScriptureUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ParatextHelper
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the paratext short names.
		/// </summary>
		/// <param name="threadHelper">The thread helper to invoke actions involving Paratext SO
		/// on the main UI thread.</param>
		/// <param name="pTScriptureText">The Paratext Scripture Objects ScriptureText object.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static string[] GetParatextShortNames(ThreadHelper threadHelper,
			SCRIPTUREOBJECTSLib.ISCScriptureText3 pTScriptureText)
		{
			try
			{
				// The list of short names is returned from the scripture object in a single
				// string with a CR LF pair between each abbreviation.
				string shortTextNames = threadHelper.Invoke(() => pTScriptureText.TextsPresent);
				if(shortTextNames == null)
					return null;
				shortTextNames = shortTextNames.Trim();
				if (shortTextNames == string.Empty)
					return null;

				// The Split will divide the string into an array of strings, each containing
				// an abbreviation.
				string[] shortNames = shortTextNames.Split(new Char[] {'\n'});

				// Since the Split method only splits using one character as the delimiter
				// (in this case '\n'), that still leaves a '\r' character in each of the
				// resulting strings. Therefore, trimming each string before returning it
				// will get rid of the '\r' characters.
				for (int i = 0; i < shortNames.Length; i++)
					shortNames[i] = shortNames[i].Trim();

				return shortNames;
			}
			catch (Exception e)
			{
				Logger.WriteError(e);
				return null;
			}
		}
	}
}
