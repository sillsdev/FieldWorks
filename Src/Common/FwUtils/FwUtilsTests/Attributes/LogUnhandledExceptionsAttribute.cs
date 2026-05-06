// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
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
		private UnhandledExceptionEventHandler m_unhandledExceptionHandler;
		private EventHandler<UnobservedTaskExceptionEventArgs> m_unobservedTaskExceptionHandler;

		/// <summary/>
		public override ActionTargets Targets => ActionTargets.Suite;

		/// <summary/>
		public override void BeforeTest(ITest test)
		{
			base.BeforeTest(test);

			m_unhandledExceptionHandler = OnUnhandledException;
			AppDomain.CurrentDomain.UnhandledException += m_unhandledExceptionHandler;

			m_unobservedTaskExceptionHandler = OnUnobservedTaskException;
			TaskScheduler.UnobservedTaskException += m_unobservedTaskExceptionHandler;
		}

		/// <summary/>
		public override void AfterTest(ITest test)
		{
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
		}

		private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			Console.Error.WriteLine("Unhandled managed exception during test run:");
			Console.Error.WriteLine($"IsTerminating: {e.IsTerminating}");
			Console.Error.WriteLine(e.ExceptionObject?.ToString() ?? "<null exception object>");
			Console.Error.Flush();
		}

		private static void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
		{
			Console.Error.WriteLine("Unobserved task exception during test run:");
			Console.Error.WriteLine(e.Exception.ToString());
			Console.Error.Flush();

			// Escalate instead of allowing the exception to be quietly ignored at finalization time.
			throw new UnobservedTaskExceptionLoggedException(e.Exception);
		}
	}

	/// <summary>
	/// Exception used to surface unobserved task failures in test output after the original
	/// AggregateException has been written to the console.
	/// </summary>
	public class UnobservedTaskExceptionLoggedException : Exception
	{
		public UnobservedTaskExceptionLoggedException(AggregateException innerException)
			: base("Unobserved task exception during test run.", innerException)
		{
		}
	}
}