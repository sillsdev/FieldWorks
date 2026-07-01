// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// How versions and copyrights are read out of the generated version attributes
	/// (CommonAssemblyInfo.cs, generated from CommonAssemblyInfoTemplate.cs + MasterVersionInfo.txt).
	/// This test assembly links the generated file, so these tests run against the real attribute
	/// shapes the product ships — including the empty-FWBETAVERSION informational version
	/// ("9.x.y.NNNNN NNNNN " with a trailing space).
	/// </summary>
	[TestFixture]
	public class VersionInfoProviderTests
	{
		private static Assembly TestAssembly => typeof(VersionInfoProviderTests).Assembly;

		// The year range stamped into this build by the Substitute task, e.g. "2002-2026".
		private static string AssemblyYearRange()
		{
			var attribute = (AssemblyCopyrightAttribute)Attribute.GetCustomAttribute(
				TestAssembly, typeof(AssemblyCopyrightAttribute));
			Assert.That(attribute, Is.Not.Null, "the test assembly must link the generated CommonAssemblyInfo.cs");
			var match = Regex.Match(attribute.Copyright, @"\d{4}-\d{4}");
			Assert.That(match.Success, Is.True, "unexpected copyright shape: " + attribute.Copyright);
			return match.Value;
		}

		[Test]
		public void CopyrightString_ComesFromTheAssembly()
		{
			var provider = new VersionInfoProvider(TestAssembly, true);
			Assert.That(provider.CopyrightString,
				Is.EqualTo($"Copyright © {AssemblyYearRange()} SIL International"));
		}

		[Test]
		public void CopyrightString_Sensitive_KeepsTheAssemblyYears_AndDropsSilIdentification()
		{
			var provider = new VersionInfoProvider(TestAssembly, false);
			Assert.That(provider.CopyrightString, Does.Not.Contain("SIL"),
				"sensitive mode must hide SIL-identifying information");
			Assert.That(provider.CopyrightString, Is.EqualTo($"Copyright © {AssemblyYearRange()}"),
				"sensitive mode must keep the years current from the assembly, not a year hardcoded in source");
		}

		[Test]
		public void FallbackCopyrightStrings_AreNotFrozenInThePast()
		{
			// The fallbacks (used when an assembly carries no copyright attribute) must not trail
			// the running year; they were once hardcoded "2002-2021" and shipped stale for years.
			StringAssert.Contains(DateTime.Now.Year.ToString(), VersionInfoProvider.kDefaultCopyrightString);
			StringAssert.Contains(DateTime.Now.Year.ToString(), VersionInfoProvider.kSensitiveCopyrightString);
		}

		[Test]
		public void ApplicationVersion_ReportsTheProvidersAssembly_NotTheEntryAssembly()
		{
			// Under a test runner the entry assembly is testhost, not FieldWorks; a provider
			// constructed for a specific assembly must report THAT assembly's version.
			var provider = new VersionInfoProvider(TestAssembly, true);
			Assert.That(provider.ApplicationVersion, Does.Contain(provider.NumericAppVersion));
		}

		[Test]
		public void MajorVersion_HasNoTrailingWhitespace_WhenThereIsNoBetaSuffix()
		{
			// With FWBETAVERSION empty the informational version ends in a space
			// ("9.3.10.46183 46183 "); the parsed display strings must not inherit it.
			var provider = new VersionInfoProvider(TestAssembly, true);
			Assert.That(provider.MajorVersion, Is.EqualTo(provider.MajorVersion.TrimEnd()));
		}

		[Test]
		public void ApplicationVersion_CarriesTheEncodedBuildDate()
		{
			var provider = new VersionInfoProvider(TestAssembly, true);
			// The second token of the informational version is an OADate day number; it must come
			// back out as an ISO date, not as the raw number.
			Assert.That(provider.ApplicationVersion, Does.Match(@"\d{4}-\d{2}-\d{2}"));
		}
	}
}
