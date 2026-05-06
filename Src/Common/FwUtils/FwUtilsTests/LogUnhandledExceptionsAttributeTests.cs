// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Threading.Tasks;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils.Attributes;

namespace SIL.FieldWorks.Common.FwUtils
{
	[TestFixture]
	public class LogUnhandledExceptionsAttributeTests
	{
		[SetUp]
		public void SetUp()
		{
			LogUnhandledExceptionsAttribute.ResetCapturedUnobservedTaskExceptionsForTesting();
		}

		[TearDown]
		public void TearDown()
		{
			LogUnhandledExceptionsAttribute.ResetCapturedUnobservedTaskExceptionsForTesting();
		}

		[Test]
		public void OnUnobservedTaskException_RecordsExceptionWithoutThrowing()
		{
			var aggregateException = new AggregateException(new InvalidOperationException("boom"));
			var eventArgs = new UnobservedTaskExceptionEventArgs(aggregateException);

			Assert.DoesNotThrow(() => LogUnhandledExceptionsAttribute.OnUnobservedTaskException(this, eventArgs));
			CollectionAssert.AreEqual(
				new[] { aggregateException },
				LogUnhandledExceptionsAttribute.DrainCapturedUnobservedTaskExceptions());
		}

		[Test]
		public void ThrowIfCapturedUnobservedTaskExceptions_DoesNothingWhenNoExceptionsWereCaptured()
		{
			Assert.DoesNotThrow(() => LogUnhandledExceptionsAttribute.ThrowIfCapturedUnobservedTaskExceptions(
				LogUnhandledExceptionsAttribute.DrainCapturedUnobservedTaskExceptions()));
		}

		[Test]
		public void ThrowIfCapturedUnobservedTaskExceptions_ThrowsAssertionExceptionWithCapturedDetails()
		{
			var first = new AggregateException(new InvalidOperationException("first failure"));
			var second = new AggregateException(new ApplicationException("second failure"));

			var exception = Assert.Throws<AssertionException>(() =>
				LogUnhandledExceptionsAttribute.ThrowIfCapturedUnobservedTaskExceptions(new[] { first, second }));

			Assert.That(exception.Message, Does.Contain("2 unobserved task exception(s)"));
			Assert.That(exception.Message, Does.Contain("first failure"));
			Assert.That(exception.Message, Does.Contain("second failure"));
			Assert.That(exception.Message, Does.Contain("fails deterministically"));
		}

		[Test]
		public void OnUnobservedTaskException_MarksExceptionObserved()
		{
			var aggregateException = new AggregateException(new InvalidOperationException("boom"));
			var eventArgs = new UnobservedTaskExceptionEventArgs(aggregateException);

			LogUnhandledExceptionsAttribute.OnUnobservedTaskException(this, eventArgs);

			Assert.That(eventArgs.Observed, Is.True);
		}
	}
}