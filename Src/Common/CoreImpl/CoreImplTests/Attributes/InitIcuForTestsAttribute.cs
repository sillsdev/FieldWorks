// Copyright (c) 2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using Microsoft.Win32;
using NUnit.Framework;

namespace SIL.CoreImpl.Attributes
{
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class)]
	public class InitIcuForTestsAttribute : TestActionAttribute
	{
		public override void BeforeTest(TestDetails testDetails)
		{
			base.BeforeTest(testDetails);
			if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ICU_DATA")))
				return;

			using (RegistryKey userKey = Registry.CurrentUser.CreateSubKey(@"Software\SIL"))
			using (RegistryKey machineKey = Registry.LocalMachine.OpenSubKey(@"Software\SIL"))
			{
				const string icuDirValueName = "Icu54DataDir";
				string dir = null;
				if (userKey?.GetValue(icuDirValueName) != null)
					dir = userKey.GetValue(icuDirValueName, null) as string;
				else if (machineKey?.GetValue(icuDirValueName) != null)
					dir = machineKey.GetValue(icuDirValueName, null) as string;
				if (!string.IsNullOrEmpty(dir))
					Environment.SetEnvironmentVariable("ICU_DATA", dir);
			}

			try
			{
				FieldWorks.Common.FwKernelInterfaces.Icu.InitIcuDataDir();
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
			}

			Icu.Wrapper.ConfineIcuVersions(54);
		}
	}
}
