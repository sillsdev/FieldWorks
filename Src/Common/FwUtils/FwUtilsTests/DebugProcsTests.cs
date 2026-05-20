// Copyright (c) 2003-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using NUnit.Framework;
using SIL.LCModel.Core.KernelInterfaces;

namespace SIL.FieldWorks.Common.FwUtils
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

		#region FakeDebugReportTransport
		private sealed class FakeDebugReportTransport : IDebugReportTransport
		{
			internal int SetSinkCallCount { get; private set; }
			internal int ClearSinkCallCount { get; private set; }
			internal int DisposeCallCount { get; private set; }
			internal bool ThrowOnSetSink { get; set; }
			internal bool ThrowOnClearSink { get; set; }

			public void SetSink(IDebugReportSink sink)
			{
				SetSinkCallCount++;
				if (ThrowOnSetSink)
					throw new COMException("SetSink failed");
			}

			public void ClearSink()
			{
				ClearSinkCallCount++;
				if (ThrowOnClearSink)
					throw new COMException("ClearSink failed");
			}

			public void Dispose()
			{
				DisposeCallCount++;
			}
		}
		#endregion // FakeDebugReportTransport

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

		private static string ExpectedAssertMessage(string fileLine, int lineNumber)
		{
			return string.Join(Environment.NewLine, new[]
			{
				"Assertion failed!",
				string.Empty,
				string.Format("Program: {0}", ExecutableName),
				fileLine,
				string.Format("Line: {0}", lineNumber),
				string.Empty,
				"Expression: The expression that failed",
				string.Empty,
				"For information on how your program can cause an assertion",
				"failure, see the Visual C++ documentation on asserts",
				string.Empty,
				"(Press Retry to debug the application - JIT must be enabled)"
			});
		}

		[Test]
		public void Constructor_TransportFactoryThrows_DoesNotThrow()
		{
			DebugProcs debugProcs = null;

			Assert.DoesNotThrow(() => debugProcs = new DebugProcs(() =>
			{
				throw new COMException("DebugReport unavailable");
			}));
			Assert.That(debugProcs, Is.Not.Null);
			Assert.DoesNotThrow(() => debugProcs.Dispose());
			Assert.DoesNotThrow(() => debugProcs.Dispose());
		}

		[Test]
		public void Constructor_SetSinkThrows_DisposesTransport()
		{
			var transport = new FakeDebugReportTransport { ThrowOnSetSink = true };
			DebugProcs debugProcs = null;

			Assert.DoesNotThrow(() => debugProcs = new DebugProcs(() => transport));

			Assert.That(transport.SetSinkCallCount, Is.EqualTo(1));
			Assert.That(transport.DisposeCallCount, Is.EqualTo(1));
			Assert.DoesNotThrow(() => debugProcs.Dispose());
		}

		[Test]
		public void Dispose_InjectedTransport_ClearsSinkAndDisposesOnce()
		{
			var transport = new FakeDebugReportTransport();
			var debugProcs = new DebugProcs(() => transport);

			Assert.That(transport.SetSinkCallCount, Is.EqualTo(1));
			debugProcs.Dispose();
			debugProcs.Dispose();

			Assert.That(transport.ClearSinkCallCount, Is.EqualTo(1));
			Assert.That(transport.DisposeCallCount, Is.EqualTo(1));
			Assert.That(debugProcs.IsDisposed, Is.True);
		}

		[Test]
		public void Dispose_ClearSinkThrows_DisposesTransportAndDoesNotThrow()
		{
			var transport = new FakeDebugReportTransport { ThrowOnClearSink = true };
			var debugProcs = new DebugProcs(() => transport);

			Assert.DoesNotThrow(() => debugProcs.Dispose());
			Assert.DoesNotThrow(() => debugProcs.Dispose());

			Assert.That(transport.ClearSinkCallCount, Is.EqualTo(1));
			Assert.That(transport.DisposeCallCount, Is.EqualTo(1));
			Assert.That(debugProcs.IsDisposed, Is.True);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetMessage method when everything fits
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetMessage_AllFit()
		{
			var expectedMsg = ExpectedAssertMessage("File: bla.cpp", 583);

			using (var debugProcs = new DummyDebugProcs())
			{
				Assert.That(debugProcs.CallGetMessage("The expression that failed", "bla.cpp", 583), Is.EqualTo(expectedMsg));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetMessage method if the complete path is to long
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetMessage_PathToLong()
		{
			var expectedMsg = ExpectedAssertMessage(
				string.Format("File: /this/is/a/very/long/path/that/extends...{0}truncate.cpp", Path.DirectorySeparatorChar),
				583);

			using (var debugProcs = new DummyDebugProcs())
			{
				Assert.That(debugProcs.CallGetMessage("The expression that failed",
					"/this/is/a/very/long/path/that/extends/beyond/sixty/characters/so/that/we/have/to/truncate.cpp", 583), Is.EqualTo(expectedMsg));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetMessage method if the filename is to long
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetMessage_FilenameToLong()
		{
			var expectedMsg = ExpectedAssertMessage(
				"File: /path/with_a_ver...have_to_truncate_before_it_fits.cpp", 583);

			using (var debugProcs = new DummyDebugProcs())
			{
				Assert.That(debugProcs.CallGetMessage("The expression that failed",
					"/path/with_a_very_long_filename_that_we_have_to_truncate_before_it_fits.cpp", 583), Is.EqualTo(expectedMsg));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetMessage method if the path and filename are to long
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetMessage_PathAndFilenameToLong()
		{
			var expectedMsg = ExpectedAssertMessage(
				"File: /path/that/has/too/many/character...with_lon...ame.cpp", 123);

			using (var debugProcs = new DummyDebugProcs())
			{
				Assert.That(debugProcs.CallGetMessage("The expression that failed",
					"/path/that/has/too/many/characters/in/it/with_long_filename.cpp", 123), Is.EqualTo(expectedMsg));
			}
		}
	}
#endif
}
