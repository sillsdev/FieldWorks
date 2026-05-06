// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace SIL.FieldWorks.Common.FwUtils.Attributes
{
	/// <summary>
	/// Assembly-level test bootstrap that logs last-chance managed exceptions to the
	/// console so unattended test runs always have readable failure details.
	///
	/// This is intentionally a logging-only hook. It does not try to recover or keep
	/// the process alive after an unhandled exception.
	/// </summary>
	[AttributeUsage(AttributeTargets.Assembly)]
	public class LogUnhandledExceptionsAttribute : TestActionAttribute
	{
		private static readonly ConcurrentQueue<AggregateException> s_unobservedTaskExceptions =
			new ConcurrentQueue<AggregateException>();

		private UnhandledExceptionEventHandler m_unhandledExceptionHandler;
		private EventHandler<UnobservedTaskExceptionEventArgs> m_unobservedTaskExceptionHandler;

		/// <summary/>
		public override ActionTargets Targets => ActionTargets.Suite;

		/// <summary/>
		public override void BeforeTest(ITest test)
		{
			base.BeforeTest(test);
			ResetCapturedUnobservedTaskExceptionsForTesting();

			m_unhandledExceptionHandler = OnUnhandledException;
			AppDomain.CurrentDomain.UnhandledException += m_unhandledExceptionHandler;

			m_unobservedTaskExceptionHandler = OnUnobservedTaskException;
			TaskScheduler.UnobservedTaskException += m_unobservedTaskExceptionHandler;
		}

		/// <summary/>
		public override void AfterTest(ITest test)
		{
			var unobservedTaskExceptions = FlushAndDrainCapturedUnobservedTaskExceptions();

			if (m_unhandledExceptionHandler != null)
			{
				AppDomain.CurrentDomain.UnhandledException -= m_unhandledExceptionHandler;
				m_unhandledExceptionHandler = null;
			}

			if (m_unobservedTaskExceptionHandler != null)
			{
				TaskScheduler.UnobservedTaskException -= m_unobservedTaskExceptionHandler;
				m_unobservedTaskExceptionHandler = null;
			}

			base.AfterTest(test);

			ThrowIfCapturedUnobservedTaskExceptions(unobservedTaskExceptions);
		}

		private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			Console.Error.WriteLine("Unhandled managed exception during test run:");
			Console.Error.WriteLine($"IsTerminating: {e.IsTerminating}");
			Console.Error.WriteLine(e.ExceptionObject?.ToString() ?? "<null exception object>");
			Console.Error.Flush();
		}

		internal static void OnUnobservedTaskException(
			object sender,
			UnobservedTaskExceptionEventArgs e
		)
		{
			Console.Error.WriteLine("Unobserved task exception during test run:");
			Console.Error.WriteLine(e.Exception.ToString());
			Console.Error.Flush();

			s_unobservedTaskExceptions.Enqueue(e.Exception);
			e.SetObserved();
		}

		internal static void ResetCapturedUnobservedTaskExceptionsForTesting()
		{
			AggregateException ignored;
			while (s_unobservedTaskExceptions.TryDequeue(out ignored)) { }
		}

		internal static AggregateException[] FlushAndDrainCapturedUnobservedTaskExceptions()
		{
			for (int i = 0; i < 3; i++)
			{
				GC.Collect();
				GC.WaitForPendingFinalizers();
			}

			return DrainCapturedUnobservedTaskExceptions();
		}

		internal static AggregateException[] DrainCapturedUnobservedTaskExceptions()
		{
			var exceptions = new ConcurrentQueue<AggregateException>();
			AggregateException capturedException;
			while (s_unobservedTaskExceptions.TryDequeue(out capturedException))
				exceptions.Enqueue(capturedException);

			return exceptions.ToArray();
		}

		internal static void ThrowIfCapturedUnobservedTaskExceptions(
			AggregateException[] exceptions
		)
		{
			if (exceptions == null || exceptions.Length == 0)
				return;

			throw new AssertionException(BuildFailureMessage(exceptions));
		}

		internal static string BuildFailureMessage(AggregateException[] exceptions)
		{
			var builder = new StringBuilder();
			builder.AppendLine(
				string.Format(
					"{0} unobserved task exception(s) were detected during the test run.",
					exceptions.Length
				)
			);
			builder.AppendLine(
				"These exceptions were captured from TaskScheduler.UnobservedTaskException and surfaced at suite teardown so the test host fails deterministically without crashing on the finalizer thread."
			);

			for (int i = 0; i < exceptions.Length; i++)
			{
				builder.AppendLine();
				builder.AppendLine(string.Format("[{0}] {1}", i + 1, exceptions[i]));
			}

			return builder.ToString();
		}
	}
}
