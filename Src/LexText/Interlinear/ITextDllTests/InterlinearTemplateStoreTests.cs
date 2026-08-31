// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using System;
using System.IO;
using NUnit.Framework;

namespace SIL.FieldWorks.IText
{
	/// <summary>Unit tests for the per-project LT-22712 template file, using a throwaway project
	/// folder rather than a real LCM project.</summary>
	[TestFixture]
	public class InterlinearTemplateStoreTests
	{
		private string m_projectFolder;

		[SetUp]
		public void SetUp()
		{
			m_projectFolder = Path.Combine(Path.GetTempPath(), "InterlinearTemplateStoreTests_" + Guid.NewGuid());
			Directory.CreateDirectory(m_projectFolder);
		}

		[TearDown]
		public void TearDown()
		{
			if (Directory.Exists(m_projectFolder))
				Directory.Delete(m_projectFolder, recursive: true);
		}

		[Test]
		public void Load_NoSavedTemplate_ReturnsTheDefault()
		{
			Assert.That(InterlinearTemplateStore.HasSavedTemplate(m_projectFolder), Is.False);
			Assert.That(InterlinearTemplateStore.Load(m_projectFolder), Is.EqualTo(InterlinearTemplateDefault.Text));
		}

		[Test]
		public void SaveThenLoad_RoundTrips()
		{
			const string custom = "{{words_source}} \\\\\n{{morphemes_source}} \\\\\n{{gloss}} \\\\";

			InterlinearTemplateStore.Save(m_projectFolder, custom);

			Assert.That(InterlinearTemplateStore.HasSavedTemplate(m_projectFolder), Is.True);
			Assert.That(InterlinearTemplateStore.Load(m_projectFolder), Is.EqualTo(custom));
		}

		[Test]
		public void Save_OverwritesAPreviouslySavedTemplate()
		{
			InterlinearTemplateStore.Save(m_projectFolder, "first version");
			InterlinearTemplateStore.Save(m_projectFolder, "second version");

			Assert.That(InterlinearTemplateStore.Load(m_projectFolder), Is.EqualTo("second version"));
		}
	}
}
