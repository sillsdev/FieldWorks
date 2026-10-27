// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using NUnit.Framework;
using SIL.SilSidePane;

namespace XCore
{
	/// <summary>
	/// How <see cref="SidebarAdapter"/> reflects areaChoice and ToolForAreaNamed_ changes in
	/// the side pane without setting those properties again.
	/// </summary>
	[TestFixture]
	[Apartment(ApartmentState.STA)]
	public class SidebarAdapterSelectionTests
	{
		/// <summary>
		/// Two areas with two tools each, in the shape of the sidebar section of Main.xml.
		/// </summary>
		private const string SidebarXml =
			"<root>"
			+ "<lists>"
			+ "<list id=\"AreasList\">"
			+ "<item label=\"Lexicon\" value=\"lexicon\"><parameters><panels>"
			+ "<listPanel label=\"Lexical Tools\" listId=\"LexicalToolsList\"/></panels></parameters></item>"
			+ "<item label=\"Grammar\" value=\"grammar\"><parameters><panels>"
			+ "<listPanel label=\"Grammar Tools\" listId=\"GrammarToolsList\"/></panels></parameters></item>"
			+ "</list>"
			+ "<list id=\"LexicalToolsList\">"
			+ "<item label=\"Lexicon Edit\" value=\"lexiconEdit\"><control/></item>"
			+ "<item label=\"Browse\" value=\"lexiconBrowse\"><control/></item>"
			+ "</list>"
			+ "<list id=\"GrammarToolsList\">"
			+ "<item label=\"Category Edit\" value=\"posEdit\"><control/></item>"
			+ "<item label=\"Category Browse\" value=\"posBrowse\"><control/></item>"
			+ "</list>"
			+ "</lists>"
			+ "<sidebar>"
			+ "<tab label=\"Areas\" list=\"AreasList\" behavior=\"singlePropertyAtomicValue\" property=\"areaChoice\"/>"
			+ "<tab label=\"Lexical Tools\" list=\"LexicalToolsList\" behavior=\"singlePropertyAtomicValue\""
			+ " property=\"currentContentControl\"/>"
			+ "<tab label=\"Grammar Tools\" list=\"GrammarToolsList\" behavior=\"singlePropertyAtomicValue\""
			+ " property=\"currentContentControl\"/>"
			+ "</sidebar>"
			+ "</root>";

		private Form m_window;
		private Mediator m_mediator;
		private PropertyTable m_propertyTable;
		private ImageCollection m_smallImages;
		private ImageCollection m_largeImages;
		private SidebarAdapter m_adapter;
		private Control m_panel;
		private ChoiceGroupCollection m_groups;

		[SetUp]
		public void SetUp()
		{
			m_window = new Form();
			m_mediator = new Mediator();
			m_propertyTable = new PropertyTable(m_mediator);
			m_smallImages = new ImageCollection(false);
			m_largeImages = new ImageCollection(true);
			m_adapter = new SidebarAdapter();
			m_panel = m_adapter.Init(m_window, m_smallImages, m_largeImages, m_mediator, m_propertyTable);
			m_window.Controls.Add(m_panel);

			var doc = new XmlDocument();
			doc.LoadXml(SidebarXml);
			m_groups = new ChoiceGroupCollection(m_mediator, m_propertyTable, m_adapter,
				doc.DocumentElement.SelectSingleNode("sidebar"));
			m_groups.Init();
			m_adapter.FinishInit();
			CreateHandles(m_window);
		}

		[TearDown]
		public void TearDown()
		{
			m_adapter.Dispose();
			m_window.Dispose();
			m_smallImages.Dispose();
			m_largeImages.Dispose();
			m_propertyTable.Dispose();
			m_mediator.Dispose();
		}

		/// <summary>
		/// The side pane's list views raise their selection events only once they have a
		/// window handle, which a hidden child of an unshown form never gets on its own.
		/// </summary>
		private static void CreateHandles(Control control)
		{
			var unused = control.Handle;
			foreach (Control child in control.Controls)
				CreateHandles(child);
		}

		private static IEnumerable<Control> AllControls(Control root)
		{
			yield return root;
			foreach (Control child in root.Controls)
				foreach (var descendant in AllControls(child))
					yield return descendant;
		}

		/// <summary>
		/// Every side-pane item widget, across all the tabs' item lists. The form is never
		/// shown, so a list's Visible flag cannot tell which tab is current.
		/// </summary>
		private IEnumerable<ListViewItem> AllItemWidgets()
		{
			return AllControls(m_panel).OfType<ListView>().SelectMany(list => list.Items.Cast<ListViewItem>());
		}

		/// <summary>
		/// The tool selected in the item list that holds the named tool, or null when none is.
		/// Each tab's list keeps its own selection, so the list is named by one of its tools.
		/// </summary>
		private string SelectedToolInTheListHolding(string tool)
		{
			var list = AllItemWidgets().First(widget => ((Item)widget.Tag).Name == tool).ListView;
			if (list.SelectedItems.Count == 0)
				return null;
			return ((ListPropertyChoice)((Item)list.SelectedItems[0].Tag).Tag).Value;
		}

		private void ShowArea(string area, string rememberedTool)
		{
			m_propertyTable.SetProperty("areaChoice", area, false);
			m_propertyTable.SetProperty("ToolForAreaNamed_" + area, rememberedTool, false);
			m_adapter.OnPropertyChanged("areaChoice");
		}

		[Test]
		public void AreaChoiceChange_SelectsTheRememberedToolWithoutSettingAnyProperty()
		{
			ShowArea("grammar", "posBrowse");

			Assert.That(SelectedToolInTheListHolding("posBrowse"), Is.EqualTo("posBrowse"));
			Assert.That(m_mediator.JobItems, Is.EqualTo(0), "the selection must not set a property");
			Assert.That(m_propertyTable.PropertyExists("currentContentControl"), Is.False);
			Assert.That(m_propertyTable.PropertyExists("areaChoiceParameters"), Is.False);
		}

		[Test]
		public void ReturnToAnArea_SelectsTheToolRememberedSinceTheLastVisit()
		{
			ShowArea("lexicon", "lexiconEdit");
			ShowArea("grammar", "posEdit");

			ShowArea("lexicon", "lexiconBrowse");

			Assert.That(SelectedToolInTheListHolding("lexiconBrowse"), Is.EqualTo("lexiconBrowse"));
			Assert.That(m_mediator.JobItems, Is.EqualTo(0), "the selection must not set a property");
			Assert.That(m_propertyTable.PropertyExists("currentContentControl"), Is.False);
		}

		[Test]
		public void ToolForAreaChange_SelectsThatToolWithoutSettingAnyProperty()
		{
			ShowArea("lexicon", "lexiconEdit");
			m_propertyTable.SetProperty("ToolForAreaNamed_lexicon", "lexiconBrowse", false);

			m_adapter.OnPropertyChanged("ToolForAreaNamed_lexicon");

			Assert.That(SelectedToolInTheListHolding("lexiconBrowse"), Is.EqualTo("lexiconBrowse"));
			Assert.That(m_mediator.JobItems, Is.EqualTo(0), "the selection must not set a property");
			Assert.That(m_propertyTable.PropertyExists("currentContentControl"), Is.False);
		}

		[Test]
		public void ItemSelectedOutsideTheAdapter_StillSetsTheProperty()
		{
			ShowArea("lexicon", "lexiconEdit");

			AllItemWidgets().First(widget => ((Item)widget.Tag).Name == "lexiconBrowse").Selected = true;

			Assert.That(m_propertyTable.GetStringProperty("currentContentControl", null), Is.EqualTo("lexiconBrowse"));
			Assert.That(m_mediator.JobItems, Is.GreaterThan(0), "a selection the adapter did not make is a click");
		}
	}
}
