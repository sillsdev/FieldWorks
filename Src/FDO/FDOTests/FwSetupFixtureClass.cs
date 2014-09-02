// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.Utils;

namespace SIL.FieldWorks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Helper class that contains methods that will be run before/after all other tests and
	/// fixture setups/teardowns are run.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[SetUpFixture]
	public class FwSetupFixtureClass
	{
		///--------------------------------------------------------------------------------------
		/// <summary>
		/// SetUp method that will be run once before any tests or setup methods
		/// </summary>
		///--------------------------------------------------------------------------------------
		[SetUp]
		public void SetUp()
		{
			FdoTestHelper.SetupStaticFdoProperties();
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Teardown method that will be run once after any tests or setup methods
		/// </summary>
		///--------------------------------------------------------------------------------------
		[TearDown]
		public void TearDown()
		{
			ReflectionHelper.CallStaticMethod("FwResources.dll",
				"SIL.FieldWorks.Resources.ResourceHelper", "ShutdownHelper");
		}
	}
}
