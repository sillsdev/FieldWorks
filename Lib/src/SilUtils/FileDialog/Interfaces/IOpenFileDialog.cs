// --------------------------------------------------------------------------------------------
// <copyright from='2011' to='2011' company='SIL International'>
// Copyright (c) 2011-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html).
// </copyright>
// --------------------------------------------------------------------------------------------
using System.IO;

namespace SIL.Utils.FileDialog
{
	/// <summary>
	/// Interface to the OpenFileDialog
	/// </summary>
	public interface IOpenFileDialog: IFileDialog
	{
		bool Multiselect { get; set; }

		Stream OpenFile();
	}
}
