// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Text;
using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.XWorks.Archiving;
using SIL.Keyboarding;

namespace SIL.FieldWorks.XWorks
{
	[TestFixture]
	public class ArchivingTests : MemoryOnlyBackendProviderTestBase
	{
		[Test]
		public void StringBuilder_AppendLineFormat()
		{
			var A = "A";
			var B = "B";
			var C = "C";
			var format = "{0}{1}{2}";
			var delimiter = ";;";
			var expected = "ABC;;CBA;;BCA";

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
			CoreWritingSystemDefinition ws = Cache.LangProject.DefaultAnalysisWritingSystem;
			var testKeyboard = new DefaultKeyboardDefinition("test", "keyboard", "layout", "locale", true);
			ws.KnownKeyboards.Add(testKeyboard);

			Assert.That(ReapRamp.DoesWritingSystemUseKeyman(ws), Is.False,
				"Unable to determine if a writing system is keyman, the location or name of the class in Palaso probably changed");
		}
	}
}
