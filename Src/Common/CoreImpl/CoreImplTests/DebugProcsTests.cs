// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DebugProcsTest.cs
// Responsibility: FW Team

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;

namespace SIL.CoreImpl
{
#if DEBUG
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for DebugProcs
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class DebugProcsTests
								// can't derive from BaseTest because of dependencies.
	{							// If necessary we could explicitly instantiate a DebugProcs
								// object but that might not be necessary because we're testing it.
		#region DummyDebugProcs
		internal class DummyDebugProcs : DebugProcs
		{
			internal bool m_fHandlerCalled;

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Callback method that gets all debug output from unmanaged FieldWorks code.
			/// </summary>
			/// <param name="nReportType">Type of report</param>
			/// <param name="szMsg">Message</param>
			/// ------------------------------------------------------------------------------------
			public override void Report(CrtReportType nReportType, string szMsg)
			{
				CheckDisposed();
				m_fHandlerCalled = true;
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Exposes the GetMessage method for testing
			/// </summary>
			/// --------------------------------------------------------------------------------
			public string CallGetMessage(string expression, string filename, int nLine)
			{
				return GetMessage(expression, filename, nLine);
			}
		}
		#endregion // DummyDebugProcs

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the executable truncated to the correct length
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static string ExecutableName
		{
			get
			{
				var exeName = new StringBuilder(255);
				Win32.GetModuleFileName(IntPtr.Zero, exeName, exeName.Capacity);
				if (exeName.Length > 60 + 9) // "Program: ".Length
				{
					exeName.Remove(0, exeName.Length - 60 - 9);
					exeName.Insert(0, "...");
				}
				return exeName.ToString();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetMessage method when everything fits
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[SuppressMessage("Gendarme.Rules.Portability", "NewLineLiteralRule",
			Justification = "Unit test, testing expected message")]
		public void GetMessage_AllFit()
		{
			var expectedMsg = string.Format(
@"Assertion failed!

Program: {0}
File: bla.cpp
Line: 583

Expression: The expression that failed

For information on how your program can cause an assertion
failure, see the Visual C++ documentation on asserts

(Press Retry to debug the application - JIT must be enabled)",
					ExecutableName);

			using (var debugProcs = new DummyDebugProcs())
			{
				Assert.AreEqual(expectedMsg,
					debugProcs.CallGetMessage("The expression that failed", "bla.cpp", 583));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetMessage method if the complete path is to long
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[SuppressMessage("Gendarme.Rules.Portability", "NewLineLiteralRule",
			Justification = "Unit test, testing expected message")]
		public void GetMessage_PathToLong()
		{
			var expectedMsg = string.Format(
@"Assertion failed!

Program: {0}
File: /this/is/a/very/long/path/that/extends...{1}truncate.cpp
Line: 583

Expression: The expression that failed

For information on how your program can cause an assertion
failure, see the Visual C++ documentation on asserts

(Press Retry to debug the application - JIT must be enabled)",
					ExecutableName, Path.DirectorySeparatorChar);

			using (var debugProcs = new DummyDebugProcs())
			{
				Assert.AreEqual(expectedMsg,
					debugProcs.CallGetMessage("The expression that failed",
					"/this/is/a/very/long/path/that/extends/beyond/sixty/characters/so/that/we/have/to/truncate.cpp", 583));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetMessage method if the filename is to long
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[SuppressMessage("Gendarme.Rules.Portability", "NewLineLiteralRule",
			Justification = "Unit test, testing expected message")]
		public void GetMessage_FilenameToLong()
		{
			var expectedMsg = string.Format(
@"Assertion failed!

Program: {0}
File: /path/with_a_ver...have_to_truncate_before_it_fits.cpp
Line: 583

Expression: The expression that failed

For information on how your program can cause an assertion
failure, see the Visual C++ documentation on asserts

(Press Retry to debug the application - JIT must be enabled)",
					ExecutableName);

			using (var debugProcs = new DummyDebugProcs())
			{
				Assert.AreEqual(expectedMsg,
					debugProcs.CallGetMessage("The expression that failed",
					"/path/with_a_very_long_filename_that_we_have_to_truncate_before_it_fits.cpp", 583));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetMessage method if the path and filename are to long
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[SuppressMessage("Gendarme.Rules.Portability", "NewLineLiteralRule",
			Justification = "Unit test, testing expected message")]
		public void GetMessage_PathAndFilenameToLong()
		{
			var expectedMsg = string.Format(
@"Assertion failed!

Program: {0}
File: /path/that/has/too/many/character...with_lon...ame.cpp
Line: 123

Expression: The expression that failed

For information on how your program can cause an assertion
failure, see the Visual C++ documentation on asserts

(Press Retry to debug the application - JIT must be enabled)",
					ExecutableName);

			using (var debugProcs = new DummyDebugProcs())
			{
				Assert.AreEqual(expectedMsg,
					debugProcs.CallGetMessage("The expression that failed",
					"/path/that/has/too/many/characters/in/it/with_long_filename.cpp", 123));
			}
		}
	}
#endif
}
