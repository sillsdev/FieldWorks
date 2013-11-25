// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: AppInteractTest.cs
// Responsibility: Eberhard Beilharz
// Last reviewed:
//
// <remarks>
// </remarks>

using System;
using System.Diagnostics;
using NUnit.Framework;
using SIL.FieldWorks.Test.TestUtils;

namespace SIL.FieldWorks.AcceptanceTests.Framework
{
	/// <summary>
	/// Tests the <see cref="AppInteract"/> class.
	/// </summary>
	[TestFixture]
	public class AppInteractTest : BaseTest
	{
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="AppInteractTest"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public AppInteractTest()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test with non-existent file.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(System.ComponentModel.Win32Exception))]
		public void NonExistentExecutable()
		{
			AppInteract app = new AppInteract(@"NonExistent.exe");
			Assert.IsNotNull(app);
			Assert.IsFalse(app.Start());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the functionality of the <see cref="AppInteract"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AppInteractFunctionality()
		{
			AppInteract app = new AppInteract(@"DummyTestExe.exe");
			Assert.IsFalse(app.Start());
			Process proc = app.Process;
			Assert.IsNotNull(proc, "Null process");
			Assert.IsNotNull(proc.MainWindowHandle, "Null window handle");

			app.Exit();
			Assert.IsNull(app.Process, "Non-null process");
		}
	}
}
