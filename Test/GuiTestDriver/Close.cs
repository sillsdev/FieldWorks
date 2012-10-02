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
// File: Close.cs
// Responsibility: LastufkaM
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using NUnit.Framework;

namespace GuiTestDriver
{
	/// <summary>
	/// Summary description for Close.
	/// </summary>
	public class Close : ActionBase
	{
		public Close()
		{
		}
		public override void Execute(TestState ts)
		{
			AppHandle app = Application;
			app.Exit(false);
			Assertion.AssertNull("Non-null process", app.Process);
		}
	}
}
