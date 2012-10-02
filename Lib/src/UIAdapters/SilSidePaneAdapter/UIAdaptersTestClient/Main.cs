// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File:
// Responsibility:
// --------------------------------------------------------------------------------------------

using System;
using System.Windows.Forms;

namespace UIAdaptersTestClient
{
	/// <summary>
	/// Test program to aid in the development of SIBAdapter
	/// </summary>
	class MainClass
	{
		public static void Main(string[] args)
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new MainWindow());
		}
	}
}
