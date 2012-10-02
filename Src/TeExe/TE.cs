// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2002, SIL International. All Rights Reserved.
// <copyright from='2002' to='2002' company='SIL International'>
//		Copyright (c) 2002, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: TE.cs
// Responsibility: Eberhard Beilharz
// Last reviewed:
//
// <remarks>
// Application entry point for TE.
// </remarks>
// --------------------------------------------------------------------------------------------
using System;

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
		[STAThread]
		public static int Main(string[] rgArgs)
		{
			return TeApp.Main(rgArgs);
		}
	}
}
