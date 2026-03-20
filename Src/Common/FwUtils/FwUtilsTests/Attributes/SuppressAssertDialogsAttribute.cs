// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace SIL.FieldWorks.Common.FwUtils.Attributes
{
	/// <summary>
	/// NUnit assembly-level attribute that suppresses all assertion dialog boxes during tests.
	/// Sets environment variables that DebugProcs.dll (native) and EnvVarTraceListener (managed)
	/// honor, and ensures debug trace/assert output is mirrored to the console.
	///
	/// For test projects that link AppForTests.config, EnvVarTraceListener already handles
	/// assert-to-exception conversion and file logging. For the remaining projects, this
	/// attribute installs a listener that converts Debug.Fail into test failures.
	/// </summary>
	[AttributeUsage(AttributeTargets.Assembly)]
	public class SuppressAssertDialogsAttribute : TestActionAttribute
	{
		private ConsoleErrorTraceListener m_listener;
		private string m_previousAssertUiEnabled;
		private string m_previousAssertExceptionEnabled;
		private string m_previousTestMode;

		/// <summary/>
		public override ActionTargets Targets => ActionTargets.Suite;

		/// <summary/>
		public override void BeforeTest(ITest test)
		{
			base.BeforeTest(test);

			// Force environment variables that control native (DebugProcs.dll) and
			// managed (EnvVarTraceListener) assertion behavior.
			m_previousAssertUiEnabled = Environment.GetEnvironmentVariable("AssertUiEnabled");
			m_previousAssertExceptionEnabled = Environment.GetEnvironmentVariable(
				"AssertExceptionEnabled"
			);
			m_previousTestMode = Environment.GetEnvironmentVariable("FW_TEST_MODE");

			Environment.SetEnvironmentVariable("AssertUiEnabled", "false");
			Environment.SetEnvironmentVariable("AssertExceptionEnabled", "true");
			Environment.SetEnvironmentVariable("FW_TEST_MODE", "1");

			// If EnvVarTraceListener is already installed (via AppForTests.config), keep
			// its assert-to-exception and file logging behavior. Otherwise, our listener
			// is responsible for converting Debug.Fail into a test failure.
			bool hasEnvVarListener = false;
			bool hasConsoleErrorListener = false;
			foreach (TraceListener listener in Trace.Listeners)
			{
				// Check by type name to avoid a hard dependency on SIL.LCModel.Utils.
				if (listener.GetType().Name == "EnvVarTraceListener")
					hasEnvVarListener = true;

				if (listener is ConsoleErrorTraceListener)
					hasConsoleErrorListener = true;
			}

			// Suppress the DefaultTraceListener dialog even when another listener is
			// present so assertions never degrade back to modal UI.
			foreach (TraceListener listener in Trace.Listeners)
			{
				if (listener is DefaultTraceListener dtl)
				{
					dtl.AssertUiEnabled = false;
					break;
				}
			}

			if (!hasConsoleErrorListener)
			{
				m_listener = new ConsoleErrorTraceListener(throwOnFail: !hasEnvVarListener);
				Trace.Listeners.Insert(0, m_listener);
			}
		}

		/// <summary/>
		public override void AfterTest(ITest test)
		{
			if (m_listener != null)
			{
				Trace.Listeners.Remove(m_listener);
				m_listener = null;
			}

			Environment.SetEnvironmentVariable("AssertUiEnabled", m_previousAssertUiEnabled);
			Environment.SetEnvironmentVariable(
				"AssertExceptionEnabled",
				m_previousAssertExceptionEnabled
			);
			Environment.SetEnvironmentVariable("FW_TEST_MODE", m_previousTestMode);

			m_previousAssertUiEnabled = null;
			m_previousAssertExceptionEnabled = null;
			m_previousTestMode = null;

			base.AfterTest(test);
		}
	}

	/// <summary>
	/// Writes debug failure details to Console.Error and optionally converts
	/// Debug.Fail/failed Debug.Assert calls into exceptions.
	/// </summary>
	internal class ConsoleErrorTraceListener : TraceListener
	{
		private readonly bool m_throwOnFail;

		public ConsoleErrorTraceListener(bool throwOnFail)
		{
			m_throwOnFail = throwOnFail;
		}

		public override void Fail(string message)
		{
			WriteFailure(message, null);
		}

		public override void Fail(string message, string detailMessage)
		{
			WriteFailure(message, detailMessage);
		}

		public override void Write(string message) { }

		public override void WriteLine(string message) { }

		private void WriteFailure(string message, string detailMessage)
		{
			var full = string.IsNullOrEmpty(detailMessage)
				? message
				: $"{message}{Environment.NewLine}{detailMessage}";

			Console.Error.WriteLine("Debug.Fail/Assert fired during test:");
			Console.Error.WriteLine(full);
			Console.Error.Flush();

			if (m_throwOnFail)
				throw new AssertionDialogException(full);
		}
	}

	/// <summary>
	/// Exception thrown when a Debug.Fail or Debug.Assert fires during a test,
	/// replacing the modal Abort/Retry/Ignore dialog with a clear test failure.
	/// </summary>
	public class AssertionDialogException : Exception
	{
		public AssertionDialogException(string message)
			: base(
				$"Debug.Fail/Assert fired during test (would have shown a modal dialog):\n{message}"
			) { }
	}
}
