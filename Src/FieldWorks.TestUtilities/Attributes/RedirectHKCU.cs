// Copyright (c) 2012-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Runtime.InteropServices;

namespace FieldWorks.TestUtilities.Attributes
{
	/// <summary>
	/// NUnit helper attribute that optionally redirects HKCU to a subkey so that multiple
	/// builds can run in parallel. Depending on wether the environment variable
	/// BUILDAGENT_SUBKEY is set the registry tree HKCU is redirected to a temporary key.
	/// This means that for the life of the process everything it attempts to read or write to
	/// HKCU/X will go to/come from tempKey/X. When running on Jenkins each build will have a
	/// different BUILDAGENT_SUBKEY so that each build writes to/reads from a separate location
	/// in the registry.
	/// </summary>
	/// <seealso href="http://www.nunit.org/index.php?p=actionAttributes&amp;r=2.6.2"/>
	[AttributeUsage(AttributeTargets.Assembly)]
	public class RedirectHKCU : NUnit.Framework.TestActionAttribute
	{
		private static readonly UIntPtr HKEY_CURRENT_USER = new UIntPtr(0x80000001);

		[DllImport("Advapi32.dll")]
		private static extern int RegOverridePredefKey(UIntPtr hKey, UIntPtr hNewKey);

		[DllImport("Advapi32.dll")]
		private static extern int RegCreateKey(UIntPtr hKey, string lpSubKey, out UIntPtr phkResult);

		[DllImport("Advapi32.dll")]
		private static extern int RegCloseKey(UIntPtr hKey);

		private static string KeyPart
		{
			get
			{
				var subKey = Environment.GetEnvironmentVariable("BUILDAGENT_SUBKEY");
				if (string.IsNullOrEmpty(subKey))
				{
					return string.Empty;
				}
				if (subKey.EndsWith("\\"))
				{
					return subKey;
				}
				return subKey + "\\";
			}
		}

		private static string TmpRegistryKey => $@"Software\SIL\BuildAgents\{KeyPart}\HKCU";

		/// <inheritdoc />
		public override void BeforeTest(NUnit.Framework.TestDetails testDetails)
		{
			base.BeforeTest(testDetails);

			if (Environment.OSVersion.Platform != PlatformID.Unix && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BUILDAGENT_SUBKEY")))
			{
				UIntPtr hKey;
				RegCreateKey(HKEY_CURRENT_USER, TmpRegistryKey, out hKey);
				RegOverridePredefKey(HKEY_CURRENT_USER, hKey);
				RegCloseKey(hKey);
			}
		}

		/// <inheritdoc />
		public override void AfterTest(NUnit.Framework.TestDetails testDetails)
		{
			if (Environment.OSVersion.Platform != PlatformID.Unix && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BUILDAGENT_SUBKEY")))
			{
				// End redirection. Otherwise test might fail when we run them multiple
				// times in NUnit.
				RegOverridePredefKey(HKEY_CURRENT_USER, UIntPtr.Zero);
			}
			base.AfterTest(testDetails);
		}

		/// <inheritdoc />
		public override NUnit.Framework.ActionTargets Targets => NUnit.Framework.ActionTargets.Suite;
	}
}