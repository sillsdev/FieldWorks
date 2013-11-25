// Copyright (c) 2012-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Runtime.InteropServices;

namespace SIL.Utils.Attributes
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// NUnit helper attribute that optionally redirects HKCU to a subkey so that multiple
	/// builds can run in parallel. Depending on wether the environment variable
	/// BUILDAGENT_SUBKEY is set the registry tree HKCU is redirected to a temporary key.
	/// This means that for the life of the process everything it attempts to read or write to
	/// HKCU/X will go to/come from tempKey/X. When running on Jenkins each build will have a
	/// different BUILDAGENT_SUBKEY so that each build writes to/reads from a separate location
	/// in the registry.
	/// </summary>
	/// <seealso href="http://www.nunit.org/index.php?p=actionAttributes&r=2.6.2"/>
	/// ----------------------------------------------------------------------------------------
	[AttributeUsage(AttributeTargets.Assembly)]
	public class RedirectHKCU : NUnit.Framework.TestActionAttribute
	{
		private static readonly UIntPtr HKEY_CURRENT_USER = new UIntPtr(0x80000001);

		[DllImport("Advapi32.dll")]
		private extern static int RegOverridePredefKey(UIntPtr hKey, UIntPtr hNewKey);

		[DllImport("Advapi32.dll")]
		private extern static int RegCreateKey(UIntPtr hKey, string lpSubKey, out UIntPtr phkResult);

		[DllImport("Advapi32.dll")]
		private extern static int RegCloseKey(UIntPtr hKey);

		private static string KeyPart
		{
			get
			{
				var subKey = Environment.GetEnvironmentVariable("BUILDAGENT_SUBKEY");
				if (string.IsNullOrEmpty(subKey))
					return string.Empty;
				if (subKey.EndsWith("\\"))
					return subKey;
				return subKey + "\\";
			}
		}

		private static string TmpRegistryKey
		{
			// keep in sync with Generic/RedirectHKCU.h and SetupInclude.targets
			get { return string.Format(@"Software\SIL\BuildAgents\{0}\HKCU", KeyPart); }
		}

		/// <summary>
		/// Method gets called once at the very start of running the tests
		/// </summary>
		public override void BeforeTest(NUnit.Framework.TestDetails testDetails)
		{
			base.BeforeTest(testDetails);

			if (Environment.OSVersion.Platform != PlatformID.Unix &&
				!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BUILDAGENT_SUBKEY")))
			{
				UIntPtr hKey;
				RegCreateKey(HKEY_CURRENT_USER, TmpRegistryKey, out hKey);
				RegOverridePredefKey(HKEY_CURRENT_USER, hKey);
				RegCloseKey(hKey);
			}
		}

		/// <summary>
		/// Method gets called once at the end of running the tests
		/// </summary>
		public override void AfterTest(NUnit.Framework.TestDetails testDetails)
		{
			if (Environment.OSVersion.Platform != PlatformID.Unix &&
				!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BUILDAGENT_SUBKEY")))
			{
				// End redirection. Otherwise test might fail when we run them multiple
				// times in NUnit.
				RegOverridePredefKey(HKEY_CURRENT_USER, UIntPtr.Zero);
			}
			base.AfterTest(testDetails);
		}

		public override NUnit.Framework.ActionTargets Targets
		{
			get { return NUnit.Framework.ActionTargets.Suite; }
		}
	}
}
