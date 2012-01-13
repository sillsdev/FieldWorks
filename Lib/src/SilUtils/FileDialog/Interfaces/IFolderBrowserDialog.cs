// --------------------------------------------------------------------------------------------
// <copyright from='2012' to='2012' company='SIL International'>
// 	Copyright (c) 2012, SIL International. All Rights Reserved.
//
// 	Distributable under the terms of either the Common Public License or the
// 	GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
// --------------------------------------------------------------------------------------------
using System;
using System.Windows.Forms;

namespace SIL.Utils.FileDialog
{
	/// <summary>
	/// Interface to the FolderBrowserDialog
	/// </summary>
	public interface IFolderBrowserDialog
	{
		string Description { get; set; }
		Environment.SpecialFolder RootFolder { get; set; }
		string SelectedPath { get; set; }
		bool ShowNewFolderButton { get; set; }
		object Tag { get; set; }

		void Reset();
		DialogResult ShowDialog();
		DialogResult ShowDialog(IWin32Window owner);

		event EventHandler Disposed;
	}
}
