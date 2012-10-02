// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
// ---------------------------------------------------------------------------------------------
using NUnit.Framework;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Test.TestUtils;
using SIL.Utils;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Helper class that contains methods that will be run before/after all other tests and
	/// fixture setups/teardowns are run.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[SetUpFixture]
	public class SetupFixtureClass
	{
		///--------------------------------------------------------------------------------------
		/// <summary>
		/// SetUp method that will be run once before any tests or setup methods
		/// </summary>
		///--------------------------------------------------------------------------------------
		[SetUp]
		public void SetUp()
		{
			BaseTest.SingletonReleasedInFixtureClass = true;
			RegistryHelper.CompanyName = "SIL";
			RegistryHelper.ProductName = "FieldWorks";

			FwRegistrySettings.Init();
			TeProjectSettings.InitSettings(RegistryHelper.SettingsKey(FwSubKey.TE, "Dummy"));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Teardown method that will be run once after any tests or setup methods
		/// </summary>
		///--------------------------------------------------------------------------------------
		[TearDown]
		public void TearDown()
		{
			TeProjectSettings.Release();
			FwRegistrySettings.Release();
			ReflectionHelper.CallStaticMethod("FwResources.dll",
				"SIL.FieldWorks.Resources.ResourceHelper", "ShutdownHelper");
			ReflectionHelper.CallStaticMethod("CoreImpl.dll",
				"SIL.CoreImpl.SingletonsContainer", "Release");
		}
	}
}
