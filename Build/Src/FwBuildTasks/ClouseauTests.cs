// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using NUnit.Framework;

namespace FwBuildTasks
{
	[TestFixture]
	public class ClouseauTests
	{
		private TestBuildEngine _tbi;
		private Clouseau _task;

		[SetUp]
		public void TestSetup()
		{
			_tbi = new TestBuildEngine();
			_task = new Clouseau { BuildEngine = _tbi };
		}

		[Test]
		public void ProperlyImplementedIDisposable_LogsNeitherErrorsNorWarnings()
		{
			_task.InspectType(typeof(ProperlyImplementedIDisposable));
			Assert.IsEmpty(_tbi.Errors);
			Assert.IsEmpty(_tbi.Warnings);
			Assert.IsEmpty(_tbi.Messages);
		}

		[Test]
		public void NoProtectedDisposeBool_LogsError()
		{
			_task.InspectType(typeof(NoProtectedDisposeBool));
			Assert.IsNotEmpty(_tbi.Errors);
		}

#region test types
// ReSharper disable ClassWithVirtualMembersNeverInherited.Local
// Justification: Clouseau reflectively verifies the presence of `protected virtual void Dispose(bool disposing)`
		private class ProperlyImplementedIDisposable : IDisposable
		{
			~ProperlyImplementedIDisposable()
			{
				Dispose(false);
			}

			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			protected virtual void Dispose(bool disposing)
			{
				System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			}
		}

		//private class ProperlyImplementedIFwDisposable : IFwDisposable
		//{
		//	public void CheckDisposed()
		//	{
		//		if (IsDisposed)
		//			throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		//	}
		//}

		private class NoProtectedDisposeBool : IDisposable
		{
			public void Dispose() {}
		}

		#endregion test types
	}
}
