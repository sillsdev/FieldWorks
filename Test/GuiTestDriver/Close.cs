// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: Close.cs
// Responsibility: LastufkaM
// Last reviewed:
//
// <remarks>
// </remarks>

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
