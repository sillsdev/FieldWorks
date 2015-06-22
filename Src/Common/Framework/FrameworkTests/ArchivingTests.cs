using System.Text;
using NUnit.Framework;
using SIL.Archiving;

namespace SIL.FieldWorks.Common.Framework
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
