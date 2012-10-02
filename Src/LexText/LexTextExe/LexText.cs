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
// File: LexText.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
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
		/// Application entry point. If LexText isn't already running,
		/// an instance of the app is created.
		/// </summary>
		/// <param name="rgArgs">Command-line arguments</param>
		/// <returns>0</returns>
		/// -----------------------------------------------------------------------------------
		[STAThread]
		public static int Main(string[] rgArgs)
		{
			return LexTextApp.Main(rgArgs);
		}
	}

	/// <summary>
	/// This class serves to make otherwise runtime dependent assemblies compile time dependent,
	/// which will make the installer easier to work with.
	/// </summary>
	internal class DoNothing
	{
		DoNothing()
		{
			// Ensures ParserUI is related to this assembly.
			SIL.FieldWorks.LexText.Controls.ImportWordSetDlg dlg = null;
			if (dlg == null)
				dlg = null;
			// Ensures LexEdDll is related to this assembly.
			SIL.FieldWorks.XWorks.LexEd.ImageHolder ih = null;
			if (ih == null)
				ih = null;
			// Ensures ITextDll is related to this assembly.
			SIL.FieldWorks.IText.ImageHolder ih2 = null;
			if (ih2 == null)
				ih2 = null;
			// Ensures MorphologyEditorDll is related to this assembly.
			SIL.FieldWorks.XWorks.MorphologyEditor.ImageHolder meih = null;
			if (meih == null)
				meih = null;
		}
	}
}
