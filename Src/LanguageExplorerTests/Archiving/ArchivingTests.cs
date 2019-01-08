// Copyright (c) 2013-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Text;
using LanguageExplorer.Archiving;
using NUnit.Framework;
using SIL.Keyboarding;
using SIL.LCModel;

namespace LanguageExplorerTests.Archiving
{
	/// <summary>
	/// Test Archiving system
	/// </summary>
	[TestFixture]
	public class ArchivingTests : MemoryOnlyBackendProviderTestBase
	{
		/// <summary>
		/// See that AppendLineFormat extension works right.
		/// </summary>
		[Test]
		public void StringBuilder_AppendLineFormat()
		{
			const string A = "A";
			const string B = "B";
			const string C = "C";
			const string format = "{0}{1}{2}";
			const string delimiter = ";;";
			const string expected = "ABC;;CBA;;BCA";

			var sb = new StringBuilder();
			sb.AppendLineFormat(format, new object[] { A, B, C }, delimiter);
			sb.AppendLineFormat(format, new object[] { C, B, A }, delimiter);
			sb.AppendLineFormat(format, new object[] { B, C, A }, delimiter);

			Assert.AreEqual(expected, sb.ToString());
		}

		/// <summary>
		/// This test verifies that a non-keyman keyboard returns false, it also serves to test the reflection which the current implementation
		/// uses to test if a keyboard is keyman.
		/// </summary>
		[Test]
		public void DoesWritingSystemUseKeyman_NonKeymanKeyboardReturnsFalse()
		{
			var ws = Cache.LangProject.DefaultAnalysisWritingSystem;
			var testKeyboard = new DefaultKeyboardDefinition("test", "keyboard", "layout", "locale", true);
			ws.KnownKeyboards.Add(testKeyboard);

			Assert.That(ReapRamp.DoesWritingSystemUseKeyman(ws), Is.False, "Unable to determine if a writing system is keyman, the location or name of the class in Palaso probably changed");
		}
	}
}