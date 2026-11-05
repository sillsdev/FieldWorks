// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.IO;
using System.Windows.Forms;
using NUnit.Framework;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// Tests the file-name validation that guards saving settings, cancelling, and finishing
	/// in the Standard Format lexicon import wizard.
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

		/// <summary>LT-21638: a cleared Database File box crashed Save and Cancel.</summary>
		[TestCase("")]
		[TestCase("   ")]
		public void UsesInvalidFileNames_BlankDatabaseFile_IsInvalid(string databaseFile)
		{
			var settingsFile = CreateFile("import-settings.map");
			using (var wizard = new LexImportWizard())
			{
				SetFileNames(wizard, databaseFile, settingsFile, settingsFile);

				Assert.That(wizard.UsesInvalidFileNames(true), Is.True);
			}
		}

		[Test]
		public void UsesInvalidFileNames_MissingDatabaseFile_IsInvalid()
		{
			var settingsFile = CreateFile("import-settings.map");
			using (var wizard = new LexImportWizard())
			{
				SetFileNames(wizard, Path.Combine(m_tempDir, "missing.db"), settingsFile,
					settingsFile);

				Assert.That(wizard.UsesInvalidFileNames(true), Is.True);
			}
		}

		[Test]
		public void UsesInvalidFileNames_ExistingFiles_AreValid()
		{
			var databaseFile = CreateFile("lexicon.db");
			var settingsFile = CreateFile("import-settings.map");
			using (var wizard = new LexImportWizard())
			{
				SetFileNames(wizard, databaseFile, settingsFile, settingsFile);

				Assert.That(wizard.UsesInvalidFileNames(true), Is.False);
			}
		}

		private string CreateFile(string name)
		{
			var path = Path.Combine(m_tempDir, name);
			File.WriteAllText(path, @"\lx test");
			return path;
		}

		private static void SetFileNames(LexImportWizard wizard, string databaseFile,
			string settingsFile, string saveAsFile)
		{
			FindControl(wizard, "m_DatabaseFileName").Text = databaseFile;
			FindControl(wizard, "m_SettingsFileName").Text = settingsFile;
			FindControl(wizard, "m_SaveAsFileName").Text = saveAsFile;
		}

		private static Control FindControl(Form form, string name)
		{
			var found = form.Controls.Find(name, true);
			Assert.That(found, Has.Length.EqualTo(1), name);
			return found[0];
		}
	}
}
