// Copyright (c) 2012-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using NUnit.Framework;

namespace SIL.FieldWorks.Common.FwUtils.Attributes
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// NUnit helper attribute that sets the company and product name for use in tests.
	/// </summary>
	/// <remarks>When running unit tests the company is NUnit.org. This attribute overrides
	/// this setting so that the tests get the name they expect.
	/// Typically you'd include Src/AssemblyInfoForTests.cs in your unit tests project which
	/// applies the attribute on the assembly level. Alternatively you can include
	/// [assembly:SetCompanyAndProductForTests] in your code, or apply the attribute
	/// to a single unit test class.
	/// (see http://www.nunit.org/index.php?p=actionAttributes&amp;r=2.6.4)
	/// </remarks>
	/// ----------------------------------------------------------------------------------------
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class)]
	public class SetCompanyAndProductAndIcuEnvForTestsAttribute: TestActionAttribute
	{
		/// <summary/>
		public override void BeforeTest(TestDetails testDetails)
		{
			base.BeforeTest(testDetails);
			RegistryHelper.CompanyName = "SIL";
			RegistryHelper.ProductName = "FieldWorks";
			if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ICU_DATA")))
				return;

			var icuDirValueName = @"Icu54DataDir";
			using(var userKey = RegistryHelper.CompanyKey)
			using(var machineKey = RegistryHelper.CompanyKeyLocalMachine)
			{
				string dir = null;
				if (userKey != null && userKey.GetValue(icuDirValueName) != null)
				{
					dir = userKey.GetValue(icuDirValueName, dir) as string;
				}
				else if (machineKey != null && machineKey.GetValue(icuDirValueName) != null)
				{

					dir = machineKey.GetValue(icuDirValueName, dir) as string;
				}
				if (!string.IsNullOrEmpty(dir))
					Environment.SetEnvironmentVariable("ICU_DATA", dir);
			}
		}
	}
}
