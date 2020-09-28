using System.Linq;
using NUnit.Framework;
using SIL.LCModel;

namespace SIL.FieldWorks.IText
{
	[TestFixture]
	class ConfigureInterlinearDlgTests : MemoryOnlyBackendProviderReallyRestoredForEachTestTestBase
	{
		[Test]
		void InitRowChoices_MorphemesHaveOwnTable()
		{
			var morphemeRows = new InterlinLineChoices(Cache, Cache.WritingSystemFactory.GetWsFromStr("fr"), Cache.WritingSystemFactory.GetWsFromStr("en"), InterlinLineChoices.InterlinMode.Analyze);
			// Verify preconditions
			Assert.That(morphemeRows.AllLineSpecs.Count, Is.EqualTo(10));
			Assert.That(morphemeRows[0].WordLevel, Is.True);
			Assert.That(morphemeRows[1].WordLevel && morphemeRows[1].MorphemeLevel, Is.True);
			Assert.That(morphemeRows[2].WordLevel && morphemeRows[2].MorphemeLevel, Is.True);
			Assert.That(morphemeRows[3].WordLevel && morphemeRows[3].MorphemeLevel, Is.True);
			Assert.That(morphemeRows[4].WordLevel && morphemeRows[4].MorphemeLevel, Is.True);
			Assert.That(morphemeRows[5].WordLevel, Is.True);
			Assert.That(morphemeRows[6].WordLevel, Is.True);
			Assert.That(morphemeRows[7].WordLevel, Is.False);
			Assert.That(morphemeRows[8].WordLevel, Is.False);
			Assert.That(morphemeRows[9].WordLevel, Is.False);
			var rowChoices = ConfigureInterlinDialog.InitRowChoices(morphemeRows);
			Assert.That(rowChoices.Count(), Is.EqualTo(5));
		}

		[Test]
		void InitRowChoices_ChartChoices()
		{
			var morphemeRows = new InterlinLineChoices(Cache, Cache.WritingSystemFactory.GetWsFromStr("fr"), Cache.WritingSystemFactory.GetWsFromStr("en"), InterlinLineChoices.InterlinMode.Chart);
			// Verify preconditions
			Assert.That(morphemeRows.AllLineSpecs.Count, Is.EqualTo(6));
			Assert.That(morphemeRows[0].WordLevel, Is.True);
			Assert.That(morphemeRows[1].WordLevel, Is.True);
			Assert.That(morphemeRows[2].WordLevel && morphemeRows[2].MorphemeLevel, Is.True);
			Assert.That(morphemeRows[3].WordLevel && morphemeRows[3].MorphemeLevel, Is.True);
			Assert.That(morphemeRows[4].WordLevel && morphemeRows[4].MorphemeLevel, Is.True);
			Assert.That(morphemeRows[5].WordLevel && morphemeRows[5].MorphemeLevel, Is.True);
			var rowChoices = ConfigureInterlinDialog.InitRowChoices(morphemeRows);
			Assert.That(rowChoices.Count(), Is.EqualTo(2));
		}
	}
}
