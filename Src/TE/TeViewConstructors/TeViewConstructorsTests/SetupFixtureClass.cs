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
using SIL.FieldWorks.Common.FwUtils;
using SIL.Utils;
using SIL.FieldWorks.Common.Framework;

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
			RegistryHelper.CompanyName = "SIL";
			RegistryHelper.ProductName = "FieldWorks";

			FwRegistrySettings.Init();
			using (var registryKey = RegistryHelper.SettingsKey(FwSubKey.TE, "Dummy"))
			{
				TeProjectSettings.InitSettings(registryKey);
			}
			Options.Init();
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Teardown method that will be run once after any tests or setup methods
		/// </summary>
		///--------------------------------------------------------------------------------------
		[TearDown]
		public void TearDown()
		{
			Options.Release();
			TeProjectSettings.Release();
			FwRegistrySettings.Release();
			ReflectionHelper.CallStaticMethod("FwResources.dll",
				"SIL.FieldWorks.Resources.ResourceHelper", "ShutdownHelper");
		}
	}
}
