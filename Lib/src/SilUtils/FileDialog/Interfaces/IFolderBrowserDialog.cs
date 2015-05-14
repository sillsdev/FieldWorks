// --------------------------------------------------------------------------------------------
// <copyright from='2012' to='2012' company='SIL International'>
// Copyright (c) 2012-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html).
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
