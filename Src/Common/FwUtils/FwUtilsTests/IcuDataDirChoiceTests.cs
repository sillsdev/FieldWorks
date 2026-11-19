// Copyright (c) 2026 SIL Global
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using NUnit.Framework;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Tests which ICU data directory FwUtils.InitializeIcu chooses (LT-22840).
	/// </summary>
	[TestFixture]
	public class IcuDataDirChoiceTests
	{
		private string m_root;
		private string m_sourceTreeIcu;
		private string m_installedIcu;
		private string m_otherInstalledIcu;

		[SetUp]
		public void SetUp()
		{
			m_root = Path.Combine(Path.GetTempPath(), "IcuDataDirChoiceTests",
				Guid.NewGuid().ToString("N"));
			var tree = Directory.CreateDirectory(Path.Combine(m_root, "tree")).FullName;
			File.WriteAllText(Path.Combine(tree, "FieldWorks.sln"), string.Empty);
			m_sourceTreeIcu = Directory.CreateDirectory(
				Path.Combine(tree, "DistFiles", "Icu70", "icudt70l")).FullName;
			m_installedIcu = Directory.CreateDirectory(
				Path.Combine(m_root, "installed", "Icu70", "icudt70l")).FullName;
			m_otherInstalledIcu = Directory.CreateDirectory(
				Path.Combine(m_root, "machine", "Icu70", "icudt70l")).FullName;
		}

		[TearDown]
		public void TearDown()
		{
			Directory.Delete(m_root, true);
		}

		[Test]
		public void SourceTreeUserKey_WinsOverMachineValue()
		{
			Assert.That(FwUtils.ChooseIcuDataDir(m_sourceTreeIcu, m_otherInstalledIcu),
				Is.EqualTo(m_sourceTreeIcu));
		}

		[Test]
		public void MissingUserKeyDirectory_IsIgnored()
		{
			Assert.That(FwUtils.ChooseIcuDataDir(Path.Combine(m_root, "gone"), m_otherInstalledIcu),
				Is.EqualTo(m_otherInstalledIcu));
		}

		[Test]
		public void InstalledUserKey_BeatsMachineValue()
		{
			Assert.That(FwUtils.ChooseIcuDataDir(m_installedIcu, m_otherInstalledIcu),
				Is.EqualTo(m_installedIcu));
		}

		[Test]
		public void MissingMachineDirectory_IsIgnored()
		{
			Assert.That(FwUtils.ChooseIcuDataDir(null, Path.Combine(m_root, "gone")), Is.Null);
		}

		[Test]
		public void NoValues_GivesNull()
		{
			Assert.That(FwUtils.ChooseIcuDataDir(null, null), Is.Null);
		}
	}
}
