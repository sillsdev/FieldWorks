// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.IO;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils.Attributes;

namespace SIL.FieldWorks.Common.FwUtils
{
	[TestFixture]
	public class SuppressAssertDialogsAttributeTests
	{
		private TraceListener[] m_originalListeners;
		private TextWriter m_originalError;

		[SetUp]
		public void SetUp()
		{
			m_originalListeners = new TraceListener[Trace.Listeners.Count];
			Trace.Listeners.CopyTo(m_originalListeners, 0);
			Trace.Listeners.Clear();
			m_originalError = Console.Error;

			Environment.SetEnvironmentVariable("AssertUiEnabled", null);
			Environment.SetEnvironmentVariable("AssertExceptionEnabled", null);
			Environment.SetEnvironmentVariable("FW_TEST_MODE", null);
		}

		[TearDown]
		public void TearDown()
		{
			Trace.Listeners.Clear();
			Trace.Listeners.AddRange(m_originalListeners);
			Console.SetError(m_originalError);

			Environment.SetEnvironmentVariable("AssertUiEnabled", null);
			Environment.SetEnvironmentVariable("AssertExceptionEnabled", null);
			Environment.SetEnvironmentVariable("FW_TEST_MODE", null);
		}

		[Test]
		public void ConsoleErrorTraceListener_Fail_ThrowsAssertionDialogExceptionAndWritesToStderr()
		{
			var stderr = new StringWriter();
			Console.SetError(stderr);

			var listener = new ConsoleErrorTraceListener(throwOnFail: true);

			var exception = Assert.Throws<AssertionDialogException>(
				() => listener.Fail("boom", "detail")
			);

			Assert.That(exception.Message, Does.Contain("boom"));
			Assert.That(exception.Message, Does.Contain("detail"));
			Assert.That(stderr.ToString(), Does.Contain("Debug.Fail/Assert fired during test:"));
		}

		[Test]
		public void SuppressAssertDialogsAttribute_BeforeAndAfterTest_RestoresEnvironmentVariables()
		{
			Environment.SetEnvironmentVariable("AssertUiEnabled", "true");
			Environment.SetEnvironmentVariable("AssertExceptionEnabled", "false");
			Environment.SetEnvironmentVariable("FW_TEST_MODE", "0");

			var defaultListener = new DefaultTraceListener();
			Trace.Listeners.Add(defaultListener);

			var attribute = new SuppressAssertDialogsAttribute();

			attribute.BeforeTest(null);

			Assert.That(Environment.GetEnvironmentVariable("AssertUiEnabled"), Is.EqualTo("false"));
			Assert.That(
				Environment.GetEnvironmentVariable("AssertExceptionEnabled"),
				Is.EqualTo("true")
			);
			Assert.That(Environment.GetEnvironmentVariable("FW_TEST_MODE"), Is.EqualTo("1"));
			Assert.That(defaultListener.AssertUiEnabled, Is.False);
			Assert.That(CountListeners<ConsoleErrorTraceListener>(), Is.EqualTo(1));

			attribute.AfterTest(null);

			Assert.That(Environment.GetEnvironmentVariable("AssertUiEnabled"), Is.EqualTo("true"));
			Assert.That(
				Environment.GetEnvironmentVariable("AssertExceptionEnabled"),
				Is.EqualTo("false")
			);
			Assert.That(Environment.GetEnvironmentVariable("FW_TEST_MODE"), Is.EqualTo("0"));
			Assert.That(CountListeners<ConsoleErrorTraceListener>(), Is.EqualTo(0));
		}

		[Test]
		public void SuppressAssertDialogsAttribute_DoesNotAddDuplicateConsoleListener()
		{
			Trace.Listeners.Add(new ConsoleErrorTraceListener(throwOnFail: false));
			var attribute = new SuppressAssertDialogsAttribute();

			attribute.BeforeTest(null);

			Assert.That(CountListeners<ConsoleErrorTraceListener>(), Is.EqualTo(1));

			attribute.AfterTest(null);
		}

		[Test]
		public void SuppressAssertDialogsAttribute_WithEnvVarTraceListener_LeavesFailAsLoggingOnly()
		{
			Trace.Listeners.Add(new EnvVarTraceListener());
			var stderr = new StringWriter();
			Console.SetError(stderr);

			var attribute = new SuppressAssertDialogsAttribute();
			attribute.BeforeTest(null);

			var listener = FindConsoleErrorTraceListener();
			Assert.That(listener, Is.Not.Null);

			Assert.DoesNotThrow(() => listener.Fail("boom", null));
			Assert.That(stderr.ToString(), Does.Contain("Debug.Fail/Assert fired during test:"));

			attribute.AfterTest(null);
		}

		private static int CountListeners<T>()
			where T : TraceListener
		{
			int count = 0;
			foreach (TraceListener listener in Trace.Listeners)
			{
				if (listener is T)
					count++;
			}

			return count;
		}

		private static ConsoleErrorTraceListener FindConsoleErrorTraceListener()
		{
			foreach (TraceListener listener in Trace.Listeners)
			{
				if (listener is ConsoleErrorTraceListener consoleListener)
					return consoleListener;
			}

			return null;
		}

		private sealed class EnvVarTraceListener : TraceListener
		{
			public override void Write(string message) { }

			public override void WriteLine(string message) { }
		}
	}
}