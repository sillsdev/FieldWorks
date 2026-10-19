// Copyright (c) 2026 SIL Global
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using Microsoft.Win32;
using NUnit.Framework;

namespace SIL.FieldWorks.ParatextLexiconPlugin
{
	/// <summary>
	/// Tests that the plugin does not follow per-user paths a source-tree build left behind
	/// (LT-22840). Uses throw-away keys under a test hive, never the FieldWorks keys.
	/// </summary>
	[TestFixture]
	public class ParatextLexiconPluginDirectoryFinderTests
	{
		private string m_root;
		private string m_hive;
		private RegistryKey m_userKey;
		private RegistryKey m_machineKey;

		[SetUp]
		public void SetUp()
		{
			m_root = Path.Combine(Path.GetTempPath(), "ParatextFinderTests", Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(Path.Combine(m_root, "tree", "DistFiles"));
			File.WriteAllText(Path.Combine(m_root, "tree", "FieldWorks.sln"), string.Empty);
			Directory.CreateDirectory(Path.Combine(m_root, "installed"));
			m_hive = @"Software\SIL\FwParatextFinderTests\" + Guid.NewGuid().ToString("N");
			m_userKey = Registry.CurrentUser.CreateSubKey(m_hive + @"\user");
			m_machineKey = Registry.CurrentUser.CreateSubKey(m_hive + @"\machine");
			m_machineKey.SetValue("RootCodeDir", "MachineCode");
			m_machineKey.SetValue("RootDataDir", "MachineData");
			m_machineKey.SetValue("ProjectsDir", "MachineProjects");
		}

		[TearDown]
		public void TearDown()
		{
			m_userKey.Dispose();
			m_machineKey.Dispose();
			Registry.CurrentUser.DeleteSubKeyTree(m_hive, false);
			Directory.Delete(m_root, true);
		}

		[TestCase("RootCodeDir")]
		[TestCase("RootDataDir")]
		[TestCase("ProjectsDir")]
		public void SourceTreeUserValue_FallsBackToMachineValue(string name)
		{
			m_userKey.SetValue(name, Path.Combine(m_root, "tree", "DistFiles"));
			Assert.That(ParatextLexiconPluginDirectoryFinder.GetDirectory(
				m_userKey, m_machineKey, name, "Default"), Is.EqualTo(ExpectedMachineValue(name)));
		}

		private static string ExpectedMachineValue(string name)
		{
			return name == "RootCodeDir" ? "MachineCode" : name == "RootDataDir" ? "MachineData" : "MachineProjects";
		}

		[TestCase("RootCodeDir")]
		[TestCase("RootDataDir")]
		[TestCase("ProjectsDir")]
		public void InstalledUserValue_Wins(string name)
		{
			var installed = Path.Combine(m_root, "installed");
			m_userKey.SetValue(name, installed);
			Assert.That(ParatextLexiconPluginDirectoryFinder.GetDirectory(
				m_userKey, m_machineKey, name, "Default"), Is.EqualTo(installed));
		}

		[Test]
		public void NoUserValue_UsesMachineValue()
		{
			Assert.That(ParatextLexiconPluginDirectoryFinder.GetDirectory(
				m_userKey, m_machineKey, "RootCodeDir", "Default"), Is.EqualTo("MachineCode"));
		}
	}
}
