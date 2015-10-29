// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Text;
using NUnit.Framework;
using Palaso.WritingSystems;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO.FDOTests;
using LanguageExplorer.Archiving;

namespace LanguageExplorerTests
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
			var ws = Cache.LangProject.DefaultAnalysisWritingSystem;
			var palasoWs = ws as PalasoWritingSystem;
			var testKeyboard = new DefaultKeyboardDefinition { IsAvailable = true };
			// ReSharper disable once PossibleNullReferenceException
			palasoWs.AddKnownKeyboard(testKeyboard);

			Assert.That(ReapRamp.DoesWritingSystemUseKeyman(ws), Is.False,
				"Unable to determine if a writing system is keyman, the location or name of the class in Palaso probably changed");
		}
	}
}
