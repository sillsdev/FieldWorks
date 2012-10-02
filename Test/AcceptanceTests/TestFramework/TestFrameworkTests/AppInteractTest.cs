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
// File: AppInteractTest.cs
// Responsibility: Eberhard Beilharz
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
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
			CheckDisposed();
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
			CheckDisposed();
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
