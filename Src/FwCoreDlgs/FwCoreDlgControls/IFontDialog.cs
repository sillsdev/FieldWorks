// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: IFontDialog.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.FwCoreDlgControls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Interface that allows the FwBulletsTab to bring up the font dialog.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IFontDialog: IDisposable
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the font dialog with the given font information.
		/// </summary>
		/// <param name="fontInfo">The font info.</param>
		/// <param name="fAllowSubscript"><c>true</c> to allow super/subscripts, <c>false</c>
		/// to disable the controls (used when called from Borders and Bullets tab)</param>
		/// <param name="ws">The default writing system (usually UI ws)</param>
		/// <param name="wsf">The writing system factory</param>
		/// <param name="styleSheet">The style sheet.</param>
		/// <param name="fAlwaysDisableFontFeatures"><c>true</c> to disable the Font Features
		/// button even when a Graphite font is selected.</param>
		/// ------------------------------------------------------------------------------------
		void Initialize(FontInfo fontInfo, bool fAllowSubscript, int ws,
			ILgWritingSystemFactory wsf, FwStyleSheet styleSheet, bool fAlwaysDisableFontFeatures);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets a value indicating whether the user can choose a different font
		/// </summary>
		/// ------------------------------------------------------------------------------------
		bool CanChooseFont
		{ set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows the font dialog.
		/// </summary>
		/// <param name="parent">The parent.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		DialogResult ShowDialog(IWin32Window parent);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Saves the font info.
		/// </summary>
		/// <param name="fontInfo">The font info.</param>
		/// ------------------------------------------------------------------------------------
		void SaveFontInfo(FontInfo fontInfo);
	}
}
