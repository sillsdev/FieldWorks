// Copyright (c) 2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using Microsoft.Win32;
using NUnit.Framework;

namespace SIL.CoreImpl.Attributes
{
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class)]
	public class SetIcuEnvForTestsAttribute : TestActionAttribute
	{
		public override void BeforeTest(TestDetails testDetails)
		{
			base.BeforeTest(testDetails);
			if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ICU_DATA")))
				return;

			var icuDirValueName = @"Icu54DataDir";
			using (RegistryKey userKey = Registry.CurrentUser.CreateSubKey(@"Software\SIL"))
			using (RegistryKey machineKey = Registry.LocalMachine.OpenSubKey(@"Software\SIL"))
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
