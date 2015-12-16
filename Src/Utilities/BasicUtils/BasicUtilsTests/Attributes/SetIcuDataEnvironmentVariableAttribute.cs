// Copyright (c) 2015 SIL International
// This software is licensed under the MIT License (http://opensource.org/licenses/MIT)
using System;
using NUnit.Framework;

namespace SIL.Utils.Attributes
{
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class |
		AttributeTargets.Interface | AttributeTargets.Method)]
	public class SetIcuDataEnvironmentVariableAttribute: TestActionAttribute
	{
		public override void BeforeTest(TestDetails testDetails)
		{
			base.BeforeTest(testDetails);
			if (!testDetails.IsSuite)
				return;

			if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ICU_DATA")))
				return;

			string icuDirValueName = string.Format("Icu54DataDir");
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
