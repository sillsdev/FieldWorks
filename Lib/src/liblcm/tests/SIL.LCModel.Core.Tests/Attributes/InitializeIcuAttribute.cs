// Copyright (c) 2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using System.Reflection;
using Microsoft.Win32;
using NUnit.Framework;
using SIL.LCModel.Core.Text;

namespace SIL.LCModel.Core.Attributes
{
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class)]
	public class InitializeIcuAttribute : TestActionAttribute
	{
		public string IcuDataPath { get; set; }

		public override void BeforeTest(TestDetails testDetails)
		{
			base.BeforeTest(testDetails);
			string dir = null;
			if (string.IsNullOrEmpty(IcuDataPath))
			{
				if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ICU_DATA")))
					return;

				using (RegistryKey userKey = Registry.CurrentUser.OpenSubKey(@"Software\SIL"))
				using (RegistryKey machineKey = Registry.LocalMachine.OpenSubKey(@"Software\SIL"))
				{
					const string icuDirValueName = "Icu54DataDir";
					if (userKey?.GetValue(icuDirValueName) != null)
						dir = userKey.GetValue(icuDirValueName, null) as string;
					else if (machineKey?.GetValue(icuDirValueName) != null)
						dir = machineKey.GetValue(icuDirValueName, null) as string;
				}
			}
			else if (Path.IsPathRooted(IcuDataPath))
			{
				dir = IcuDataPath;
			}
			else
			{
				Uri uriBase = new Uri(Assembly.GetExecutingAssembly().CodeBase);
				string codeDir = Path.GetDirectoryName(Uri.UnescapeDataString(uriBase.AbsolutePath));
				if (codeDir != null)
					dir = Path.Combine(codeDir, IcuDataPath);
			}

			if (!string.IsNullOrEmpty(dir))
				Environment.SetEnvironmentVariable("ICU_DATA", dir);

			try
			{
				Icu.InitIcuDataDir();
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
			}
		}
	}
}
