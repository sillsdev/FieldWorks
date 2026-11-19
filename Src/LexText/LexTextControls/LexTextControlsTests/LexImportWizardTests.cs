// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.IO;
using NUnit.Framework;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// Tests when the file-selection step of the Standard Format lexicon import wizard may
	/// advance.
	/// </summary>
	[TestFixture]
	public class LexImportWizardTests
	{
		private string m_tempDir;

		[SetUp]
		public void SetUp()
		{
			m_tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
			Directory.CreateDirectory(m_tempDir);
		}

		[TearDown]
		public void TearDown()
		{
			Directory.Delete(m_tempDir, true);
		}

		[Test]
		public void CanLeaveFileStep_MissingPhaseFile_IsFalse()
		{
			var phaseFile = Path.Combine(m_tempDir, "Phase1Output.xml");

			Assert.That(LexImportWizard.CanLeaveFileStep(phaseFile, true, "", "MDFImport.map"),
				Is.False);
		}

		[Test]
		public void CanLeaveFileStep_ExistingPhaseFile_IsTrue()
		{
			var phaseFile = CreateFile("Phase1Output.xml");

			Assert.That(LexImportWizard.CanLeaveFileStep(phaseFile, true, "", "MDFImport.map"),
				Is.True);
		}

		/// <summary>LT-21638: a cleared Database File box crashed Save and Cancel.</summary>
		[TestCase("")]
		[TestCase("   ")]
		public void CanLeaveFileStep_BlankDatabaseFile_IsFalse(string databaseFile)
		{
			Assert.That(LexImportWizard.CanLeaveFileStep(databaseFile, false, "out.map",
				"MDFImport.map"), Is.False);
		}

		[Test]
		public void CanLeaveFileStep_SavingOverShippedMap_IsFalse()
		{
			var databaseFile = CreateFile("lexicon.db");

			Assert.That(LexImportWizard.CanLeaveFileStep(databaseFile, false, "MDFImport.map",
				"MDFImport.map"), Is.False);
		}

		[Test]
		public void CanLeaveFileStep_ExistingDatabaseFile_IsTrue()
		{
			var databaseFile = CreateFile("lexicon.db");

			Assert.That(LexImportWizard.CanLeaveFileStep(databaseFile, false, "out.map",
				"MDFImport.map"), Is.True);
		}

		private string CreateFile(string name)
		{
			var path = Path.Combine(m_tempDir, name);
			File.WriteAllText(path, @"\lx test");
			return path;
		}
	}
}
