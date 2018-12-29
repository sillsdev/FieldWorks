// Copyright (c) 2007-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.DomainServices;

namespace LanguageExplorer.Controls.Styles
{
	/// <summary>
	/// Interface that allows the FwBulletsTab to bring up the font dialog.
	/// </summary>
	public interface IFontDialog : IDisposable
	{
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
		void Initialize(FontInfo fontInfo, bool fAllowSubscript, int ws, ILgWritingSystemFactory wsf, LcmStyleSheet styleSheet, bool fAlwaysDisableFontFeatures);

		/// <summary>
		/// Sets a value indicating whether the user can choose a different font
		/// </summary>
		bool CanChooseFont { set; }

		/// <summary>
		/// Shows the font dialog.
		/// </summary>
		DialogResult ShowDialog(IWin32Window parent);

		/// <summary>
		/// Saves the font info.
		/// </summary>
		void SaveFontInfo(FontInfo fontInfo);
	}
}