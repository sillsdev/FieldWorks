using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SIL.LCModel;

namespace SIL.FieldWorks.Discourse
{
	[TestFixture]
	public class MultilevelHeaderModelTests : MemoryOnlyBackendProviderTestBase
	{
		private DiscourseTestHelper m_helper;

		[OneTimeSetUp]
		public void OneTimeSetUp()
		{
			if (m_actionHandler.CurrentDepth == 0)
				m_actionHandler.BeginUndoTask("undoSetup", "redoSetup");
			m_helper = new DiscourseTestHelper(Cache) { Logic = new TestCCLogic(Cache) };
			m_helper.MakeTemplate(out _);
		}

		[Test]
		public void MultilevelHeaderModel_Constructor_NullTemplate()
		{
			Assert.That(new MultilevelHeaderModel(null).Headers.Count, Is.EqualTo(0));
		}

		[Test]
		public void MultilevelHeaderModel_Constructor_EmptyTemplate()
		{
			var result = new MultilevelHeaderModel(m_helper.MakeChartMarkers("<list><item name='loneliness'/></list>").PossibilitiesOS[0]).Headers;
			Assert.That(result.Count, Is.EqualTo(0));
		}

		[Test]
		public void MultilevelHeaderModel_Constructor()
		{
			var result = new MultilevelHeaderModel(MakeMultilevelMarkers().PossibilitiesOS[0]).Headers;

			Assert.That(result.Count, Is.EqualTo(3));

			var row0 = result[0];
			foreach (var node in row0)
			{
				Assert.That(node.Item, Is.Not.Null);
				Assert.That(node.IsLastInGroup, $"{node.Label.Text} is a top-level column group");
			}
			Assert.That(row0[0].Label.Text, Is.EqualTo("a"));
			Assert.That(row0[0].ColumnCount, Is.EqualTo(1));
			Assert.That(row0[1].Label.Text, Is.EqualTo("leaf"));
			Assert.That(row0[1].ColumnCount, Is.EqualTo(3));
			Assert.That(row0[2].Label.Text, Is.EqualTo("falls"));
			Assert.That(row0[2].ColumnCount, Is.EqualTo(2));

			var row1 = result[1];
			Assert.That(row1[0].Item, Is.Null, "placeholder for the 'a' 'group'");
			Assert.That(row1[0].ColumnCount, Is.EqualTo(1));
			Assert.That(row1[0].IsLastInGroup, Is.True);
			Assert.That(row1[1].Label.Text, Is.EqualTo("blade"));
			Assert.That(row1[1].ColumnCount, Is.EqualTo(2));
			Assert.That(row1[1].IsLastInGroup, Is.True);
			Assert.That(row1[2].Label.Text, Is.EqualTo("stem"));
			Assert.That(row1[2].ColumnCount, Is.EqualTo(1));
			Assert.That(row1[2].IsLastInGroup, Is.True);
			Assert.That(row1[3].Label.Text, Is.EqualTo("disconnects"));
			Assert.That(row1[3].ColumnCount, Is.EqualTo(1));
			Assert.That(row1[3].IsLastInGroup, Is.False);
			Assert.That(row1[4].Label.Text, Is.EqualTo("descends"));
			Assert.That(row1[4].ColumnCount, Is.EqualTo(1));
			Assert.That(row1[4].IsLastInGroup, Is.True);

			var row2 = result[2];
			Assert.That(row2[0].Item, Is.Null, "placeholder for the 'a' column");
			Assert.That(row2[0].ColumnCount, Is.EqualTo(1));
			Assert.That(row2[0].IsLastInGroup, Is.True);
			Assert.That(row2[1].Label.Text, Is.EqualTo("vein"));
			Assert.That(row2[1].ColumnCount, Is.EqualTo(1));
			Assert.That(row2[1].IsLastInGroup, Is.False);
			Assert.That(row2[2].Label.Text, Is.EqualTo("chlorophyl"));
			Assert.That(row2[2].ColumnCount, Is.EqualTo(1));
			Assert.That(row2[2].IsLastInGroup, Is.True);
			Assert.That(row2[3].Item, Is.Null, "placeholder for the 'stem' column");
			Assert.That(row2[3].ColumnCount, Is.EqualTo(1));
			Assert.That(row2[3].IsLastInGroup, Is.True);
			Assert.That(row2[4].Item, Is.Null, "placeholder for the 'disconnects' column");
			Assert.That(row2[4].ColumnCount, Is.EqualTo(1));
			Assert.That(row2[4].IsLastInGroup, Is.False);
			Assert.That(row2[5].Item, Is.Null, "placeholder for the 'descends' column");
			Assert.That(row2[5].ColumnCount, Is.EqualTo(1));
			Assert.That(row2[5].IsLastInGroup, Is.True);
		}

		[Test]
		public void AddSubpossibilities_Placeholder()
		{
			var result = new List<MultilevelHeaderNode>();
			MultilevelHeaderModel.AddSubpossibilities(result, new MultilevelHeaderNode());
			Assert.That(result.Count, Is.EqualTo(1), "Should be only one 'subpossibility'");
			Assert.That(result[0].Item, Is.Null);
			Assert.That(result[0].IsLastInGroup, Is.False, "inherits from parent");
			Assert.That(result[0].ColumnCount, Is.EqualTo(1), "one leaf");
		}

		[Test]
		public void AddSubpossibilities_Leaf()
		{
			var templateList = m_helper.MakeChartMarkers("<list><item name='leaf' abbr='l'/></list>");

			var result = new List<MultilevelHeaderNode>();
			MultilevelHeaderModel.AddSubpossibilities(result, new MultilevelHeaderNode(templateList.PossibilitiesOS.First(), 1, true));
			Assert.That(result.Count, Is.EqualTo(1), "Should be only one 'subpossibility'");
			Assert.That(result[0].Item, Is.Null);
			Assert.That(result[0].IsLastInGroup, Is.True, "inherits from parent");
			Assert.That(result[0].ColumnCount, Is.EqualTo(1), "one leaf");
		}

		[Test]
		public void AddSubpossibilities_OneLevel()
		{
			var templateList = m_helper.MakeChartMarkers(
				"<list><item name='loneliness'><item name='a'/><item name='leaf'/><item name='falls'/></item></list>");

			var result = new List<MultilevelHeaderNode>();
			MultilevelHeaderModel.AddSubpossibilities(result, new MultilevelHeaderNode(templateList.PossibilitiesOS.First(), 3, true));
			Assert.That(result.Count, Is.EqualTo(3));
			foreach (var node in result)
			{
				Assert.That(node.Item, Is.Not.Null);
				Assert.That(node.ColumnCount, Is.EqualTo(1));
			}
			Assert.That(result[0].Label.Text, Is.EqualTo("a"));
			Assert.That(result[0].IsLastInGroup, Is.False);
			Assert.That(result[1].Label.Text, Is.EqualTo("leaf"));
			Assert.That(result[1].IsLastInGroup, Is.False);
			Assert.That(result[2].Label.Text, Is.EqualTo("falls"));
			Assert.That(result[2].IsLastInGroup, Is.True);
		}

		[Test]
		public void AddSubpossibilities_Multilevel()
		{
			var templateList = MakeMultilevelMarkers();

			var result = new List<MultilevelHeaderNode>();
			MultilevelHeaderModel.AddSubpossibilities(result, new MultilevelHeaderNode(templateList.PossibilitiesOS.First(), 3, true));
			Assert.That(result.Count, Is.EqualTo(3));
			foreach (var node in result)
			{
				Assert.That(node.Item, Is.Not.Null);
				Assert.That(node.IsLastInGroup, $"{node.Label.Text} is a column group");
			}
			Assert.That(result[0].Label.Text, Is.EqualTo("a"));
			Assert.That(result[0].ColumnCount, Is.EqualTo(1));
			Assert.That(result[1].Label.Text, Is.EqualTo("leaf"));
			Assert.That(result[1].ColumnCount, Is.EqualTo(3));
			Assert.That(result[2].Label.Text, Is.EqualTo("falls"));
			Assert.That(result[2].ColumnCount, Is.EqualTo(2));
		}

		private ICmPossibilityList MakeMultilevelMarkers()
		{
			return m_helper.MakeChartMarkers(
				"<list><item name='loneliness'>" +
					"<item name='a'/>" +
					"<item name='leaf'>" +
						"<item name='blade'>" +
							"<item name='vein'/>" +
							"<item name='chlorophyl'/>" +
						"</item>" +
						"<item name='stem'/>" +
					"</item>" +
					"<item name='falls'>" +
						"<item name='disconnects'/>" +
						"<item name='descends'/>" +
					"</item>" +
				"</item></list>");
		}
	}

	[TestFixture]
	public class MultilevelHeaderNodeTests
	{
		[Test]
		public void Label_NullItem()
		{
			Assert.That(new MultilevelHeaderNode().Label, Is.Null);
		}
	}
}
