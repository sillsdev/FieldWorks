using NUnit.Framework;
using SIL.FieldWorks.FDO.DomainServices.DataMigration;

namespace SIL.FieldWorks.FDO.FDOTests.DataMigrationTests
{
	/// <summary>
	/// WritingSystemIdMigrator unit tests
	/// </summary>
	[TestFixture]
	public class WritingSystemIdMigratorTests
	{
		/// <summary>
		/// Test it.
		/// </summary>
		[Test]
		public void GetNextDuplPart()
		{
			Assert.That(WritingSystemIdMigrator.GetNextDuplPart(null), Is.EqualTo("dupl1"));
			Assert.That(WritingSystemIdMigrator.GetNextDuplPart(""), Is.EqualTo("dupl1"));
			Assert.That(WritingSystemIdMigrator.GetNextDuplPart("abc"), Is.EqualTo("abc-dupl1"));
			Assert.That(WritingSystemIdMigrator.GetNextDuplPart("dupl1"), Is.EqualTo("dupl2"));
			Assert.That(WritingSystemIdMigrator.GetNextDuplPart("abc-def-dupl12"), Is.EqualTo("abc-def-dupl13"));
		}
	}
}
