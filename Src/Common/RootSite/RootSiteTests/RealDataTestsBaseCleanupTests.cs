using System;
using System.Reflection;
using NUnit.Framework;

namespace SIL.FieldWorks.Common.RootSites.RootSiteTests
{
	[TestFixture]
	public class RealDataTestsBaseCleanupTests
	{
		[Test]
		public void RunSetupFailureCleanup_ReleasesMutexEvenWhenDisposeFails()
		{
			var deleteCalled = false;
			var releaseCalled = false;
			var method = GetRunSetupFailureCleanupMethod();

			var exception = Assert.Catch<InvalidOperationException>(
				() => InvokeRunSetupFailureCleanup(
					method,
					() => { throw new InvalidOperationException("dispose failed"); },
					() => deleteCalled = true,
					() => releaseCalled = true
				)
			);

			Assert.That(exception.Message, Is.EqualTo("dispose failed"));
			Assert.That(deleteCalled, Is.True, "delete cleanup should still run");
			Assert.That(releaseCalled, Is.True, "mutex release should be guaranteed");
		}

		private static MethodInfo GetRunSetupFailureCleanupMethod()
		{
			var method = typeof(RealDataTestsBase).GetMethod(
				"RunSetupFailureCleanup",
				BindingFlags.Static | BindingFlags.NonPublic
			);

			Assert.That(
				method,
				Is.Not.Null,
				"Expected RealDataTestsBase to expose a setup-failure cleanup helper."
			);

			return method;
		}

		private static void InvokeRunSetupFailureCleanup(
			MethodInfo method,
			Action disposeCache,
			Action deleteProjectDirectory,
			Action releaseProjectMutex
		)
		{
			try
			{
				method.Invoke(null, new object[] { disposeCache, deleteProjectDirectory, releaseProjectMutex });
			}
			catch (TargetInvocationException e) when (e.InnerException != null)
			{
				throw e.InnerException;
			}
		}
	}
}