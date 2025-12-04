// Copyright (c) 2025 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using NUnit.Framework;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Assembly-level setup and teardown for FwUtilsTests.
	/// Handles cleanup of native COM objects to prevent access violations during VSTest shutdown.
	/// </summary>
	[SetUpFixture]
	public class AssemblySetupFixture
	{
		/// <summary>
		/// One-time cleanup after all tests in the assembly complete.
		/// Forces garbage collection to release COM objects while native DLLs are still loaded.
		/// </summary>
		/// <remarks>
		/// VSTest can crash with 0xC0000005 (Access Violation) during process shutdown if
		/// COM objects with pointers to native memory are released by the finalizer thread
		/// after native DLLs have been unloaded. This cleanup forces finalization while
		/// the native code is still available.
		/// </remarks>
		[OneTimeTearDown]
		public void AssemblyTearDown()
		{
			// Force multiple GC passes to ensure all COM wrappers are finalized
			// while native DLLs are still loaded.
			for (int i = 0; i < 3; i++)
			{
				GC.Collect();
				GC.WaitForPendingFinalizers();
			}
		}
	}
}
