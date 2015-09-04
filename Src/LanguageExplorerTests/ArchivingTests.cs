// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Text;
using NUnit.Framework;
using LanguageExplorer.Archiving;

namespace LanguageExplorerTests
{
	/// <summary>
	/// Test Archiving system
	/// </summary>
	[TestFixture]
	public class ArchivingTests
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
	}
}
