using System.Linq;
using NUnit.Framework;
using SIL.LCModel;

namespace SIL.FieldWorks.IText
{
	[TestFixture]
	class ConfigureInterlinearDlgTests : MemoryOnlyBackendProviderReallyRestoredForEachTestTestBase
	{
		[Test]
		public void InitRowChoices_MorphemesHaveOwnTable()
		{
			var morphemeRows = new InterlinLineChoices(Cache, Cache.WritingSystemFactory.GetWsFromStr("fr"), Cache.WritingSystemFactory.GetWsFromStr("en"), InterlinLineChoices.InterlinMode.Analyze);
			morphemeRows.SetStandardState();
			// Verify preconditions
			Assert.That(morphemeRows.AllLineSpecs.Count, Is.EqualTo(8));
			Assert.That(morphemeRows[0].WordLevel, Is.True);
			Assert.That(morphemeRows[1].WordLevel && morphemeRows[1].MorphemeLevel, Is.True);
			Assert.That(morphemeRows[2].WordLevel && morphemeRows[2].MorphemeLevel, Is.True);
			Assert.That(morphemeRows[3].WordLevel && morphemeRows[3].MorphemeLevel, Is.True);
			Assert.That(morphemeRows[4].WordLevel && morphemeRows[4].MorphemeLevel, Is.True);
			Assert.That(morphemeRows[5].WordLevel, Is.True);
			Assert.That(morphemeRows[6].WordLevel, Is.True);
			Assert.That(morphemeRows[7].WordLevel, Is.False);
			// SUT
			var rowChoices = ConfigureInterlinDialog.InitRowChoices(morphemeRows);
			Assert.That(rowChoices.Count(), Is.EqualTo(5));
		}

		[Test]
		public void InitRowChoices_ChartChoices()
		{
			var morphemeRows = new InterlinLineChoices(Cache, Cache.WritingSystemFactory.GetWsFromStr("fr"), Cache.WritingSystemFactory.GetWsFromStr("en"), InterlinLineChoices.InterlinMode.Chart);
			morphemeRows.SetStandardChartState();
			// Verify preconditions
			Assert.That(morphemeRows.AllLineSpecs.Count, Is.EqualTo(6));
			Assert.That(morphemeRows[0].WordLevel, Is.True); // row 1
			Assert.That(morphemeRows[1].WordLevel, Is.True); // row 2
			Assert.That(morphemeRows[2].WordLevel && morphemeRows[2].MorphemeLevel, Is.True); // this and other morpheme combine to row 3
			Assert.That(morphemeRows[3].WordLevel && morphemeRows[3].MorphemeLevel, Is.True);
			Assert.That(morphemeRows[4].WordLevel && morphemeRows[4].MorphemeLevel, Is.True);
			Assert.That(morphemeRows[5].WordLevel && morphemeRows[5].MorphemeLevel, Is.True);
			// SUT
			var rowChoices = ConfigureInterlinDialog.InitRowChoices(morphemeRows);
			Assert.That(rowChoices.Count(), Is.EqualTo(3));
		}
	}
}
