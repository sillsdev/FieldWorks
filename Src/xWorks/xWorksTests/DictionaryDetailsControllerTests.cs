// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FwCoreDlgControls;
using SIL.FieldWorks.XWorks.DictionaryDetailsView;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	[TestFixture]
	class DictionaryDetailsControllerTests : XWorksAppTestBase, IDisposable
	{
		private Mediator m_mediator;
		private FwStyleSheet m_styleSheet;

		#region IDisposable and Gendarme requirements
		~DictionaryDetailsControllerTests()
		{
			Dispose(false);
		}
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (disposing && !IsDisposed)
			{
				if (m_mediator != null && !m_mediator.IsDisposed)
					m_mediator.RemoveColleague(m_window);

				if (m_window != null && !m_window.IsDisposed)
					m_window.Dispose();
				m_window = null;

				if (m_mediator != null && !m_mediator.IsDisposed)
					m_mediator.Dispose();
				m_mediator = null;
			}
			IsDisposed = true;
		}
		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		protected bool IsDisposed
		{
			get;
			private set;
		}
		#endregion IDisposable and Gendarme Requirements

		#region Setup and Teardown
		protected override void Init()
		{
			m_application = new MockFwXApp(new MockFwManager { Cache = Cache }, null, null);
			m_configFilePath = Path.Combine(DirectoryFinder.FWCodeDirectory, m_application.DefaultConfigurationPathname);
			m_window = new MockFwXWindow(m_application, m_configFilePath);
			((MockFwXWindow)m_window).Init(Cache); // initializes Mediator values
			m_mediator = m_window.Mediator;
			m_window.LoadUI(m_configFilePath); // actually loads UI here; needed for non-null stylesheet

			m_styleSheet = FontHeightAdjuster.StyleSheetFromMediator(m_mediator);
			GenerateStyles();
		}

		protected void GenerateStyles()
		{
			for (int i = 0; i < 5; i++)
			{
				m_styleSheet.Styles.Add(new BaseStyleInfo { Name = string.Format("ParaStyle{0}", i), IsParagraphStyle = true });
				m_styleSheet.Styles.Add(new BaseStyleInfo { Name = string.Format("CharStyle{0}", i), IsParagraphStyle = false });
			}
		}
		#endregion Setup and Teardown

		#region Helpers
		private static List<DictionaryNodeListOptions.DictionaryNodeOption> ListOfEnabledDNOsFromStrings(IEnumerable<String> idList)
		{
			return idList.Select(id => new DictionaryNodeListOptions.DictionaryNodeOption { Id = id, IsEnabled = true }).ToList();
		}

		private static IList<StyleComboItem> GetAvailableStyles(DetailsView view)
		{
			var ddStyle = (ComboBox)ReflectionHelper.GetField(view, "dropDownStyle");
			return ddStyle.Items.Cast<StyleComboItem>().ToList();
		}

		private static ListOptionsView GetListOptionsView(DetailsView view)
		{
			var panelOptions = (Panel)ReflectionHelper.GetField(view, "panelOptions");
			return (ListOptionsView)panelOptions.Controls[0];
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule", Justification = "ListOptionsView is disposed by its parent")]
		private static IList<ListViewItem> GetListViewItems(DetailsView view)
		{
			var listOptionsView = GetListOptionsView(view);
			var listView = (ListView)ReflectionHelper.GetField(listOptionsView, "listView");
			return listView.Items.Cast<ListViewItem>().ToList();
		}

		private static void AssertShowingCharacterStyles(DetailsView view)
		{
			var styles = GetAvailableStyles(view);

			// The first character style should be (none), specified by null
			Assert.IsNull(styles[0].Style);

			// The rest should be character styles
			for (int i = 1; i < styles.Count; i++)
			{
				Assert.IsTrue(styles[i].Style.IsCharacterStyle);
			}
		}

		private static void AssertShowingParagraphStyles(DetailsView view)
		{
			foreach (var style in GetAvailableStyles(view))
			{
				Assert.IsTrue(style.Style.IsParagraphStyle);
			}
		}
		#endregion Helpers

		#region Sense tests
		[Test]
		public void SenseLoadsParagraphStyles()
		{
			using (var view = new DictionaryDetailsController( // SUT
				new ConfigurableDictionaryNode { DictionaryNodeOptions = new DictionaryNodeSenseOptions() }, m_mediator).View)
			{
				AssertShowingParagraphStyles(view);
			}
		}

		[Test]
		public void NonSenseLoadsCharacterStyles()
		{
			using(var view = new DictionaryDetailsController(new ConfigurableDictionaryNode(), m_mediator).View)
				AssertShowingCharacterStyles(view);
		}

		[Test]
		public void LoadNodeSwitchesStyles()
		{
			// Load character styles
			var node = new ConfigurableDictionaryNode { DictionaryNodeOptions = new DictionaryNodeListOptions
			{
				Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>()
			} };
			var controller = new DictionaryDetailsController(node, m_mediator);
			AssertShowingCharacterStyles(controller.View);
			controller.View.Dispose();

			// Load paragraph styles
			node.DictionaryNodeOptions = new DictionaryNodeComplexFormOptions
			{
				Options =  new List<DictionaryNodeListOptions.DictionaryNodeOption>()
			};
			controller.LoadNode(node); // SUT
			AssertShowingParagraphStyles(controller.View);
			controller.View.Dispose();

			// Load character styles
			node.DictionaryNodeOptions = new DictionaryNodeWritingSystemOptions
			{
				Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>()
			};
			controller.LoadNode(node); // SUT
			AssertShowingCharacterStyles(controller.View);
			controller.View.Dispose();
		}
		#endregion Sense tests

		#region List tests
		[Test]
		public void CannotUncheckOnlyCheckedItemInList()
		{
			var wsOptions = new DictionaryNodeWritingSystemOptions
			{
				Options = ListOfEnabledDNOsFromStrings(new List<string>
				{
					WritingSystemServices.GetMagicWsNameFromId(WritingSystemServices.kwsVern)
				}),
				WsType = DictionaryNodeWritingSystemOptions.WritingSystemType.Vernacular
			};
			var controller = new DictionaryDetailsController(new ConfigurableDictionaryNode { DictionaryNodeOptions = wsOptions }, m_mediator);
			using (var view = controller.View)
			{
				// Verify setup
				var listViewItems = GetListViewItems(view);
				Assert.AreEqual(1, listViewItems.Count(item => item.Checked), "There should be exactly one item checked initially");

				var checkedItem = listViewItems.First(item => item.Checked);
				checkedItem.Checked = false;
				// SUT
				// Events are not actually fired during tests, so they must be run manually
				ReflectionHelper.CallMethod(controller, "WritingSystemCheckedChanged",
					GetListOptionsView(view), wsOptions, new ItemCheckedEventArgs(checkedItem));

				Assert.AreEqual(1, listViewItems.Count(item => item.Checked), "There should still be exactly one item checked");
				Assert.AreEqual(checkedItem, listViewItems.First(item => item.Checked), "The same item should be checked");
				Assert.AreEqual(1, wsOptions.Options.Count(option => option.IsEnabled), "There should be exactly one enabled option in the model");
				Assert.AreEqual(WritingSystemServices.GetMagicWsNameFromId(WritingSystemServices.kwsVern),
					wsOptions.Options.First(option => option.IsEnabled).Id,
					"The same item should still be enabled");
			}

			// TODO pH 2014.03: verify for non-WS list types
		}

		[Test]
		public void CannotMoveTopItemUp()
		{
			var wsOptions = new DictionaryNodeWritingSystemOptions
			{
				Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>(),
				WsType = DictionaryNodeWritingSystemOptions.WritingSystemType.Both
			};
			var controller = new DictionaryDetailsController(new ConfigurableDictionaryNode { DictionaryNodeOptions = wsOptions }, m_mediator);
			using (var view = controller.View)
			{
				var listViewItems = GetListViewItems(view);
				var originalListViewItems = new List<ListViewItem>();
				originalListViewItems.AddRange(listViewItems);

				// SUT
				Assert.Throws<ArgumentOutOfRangeException>(() =>
					controller.Reorder(listViewItems.First(), DictionaryConfigurationController.Direction.Up),
					"Should not be able to move the top item up");

				Assert.AreEqual(originalListViewItems.Count, listViewItems.Count, "Number of items definitely should not have changed");
				for (int i = 0; i < listViewItems.Count; i++)
				{
					Assert.AreEqual(originalListViewItems[i], listViewItems[i], "Order should not have changed");
				}
			}

			// TODO pH 2014.03: verify for non-WS list types
		}

		[Test]
		public void CannotMoveBottomItemDown()
		{
			var wsOptions = new DictionaryNodeWritingSystemOptions
			{
				Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>(),
				WsType = DictionaryNodeWritingSystemOptions.WritingSystemType.Both
			};
			var controller = new DictionaryDetailsController(new ConfigurableDictionaryNode { DictionaryNodeOptions = wsOptions }, m_mediator);
			using (var view = controller.View)
			{
				var listViewItems = GetListViewItems(view);
				var originalListViewItems = new List<ListViewItem>();
				originalListViewItems.AddRange(listViewItems);

				// SUT
				Assert.Throws<ArgumentOutOfRangeException>(() =>
					controller.Reorder(listViewItems.Last(), DictionaryConfigurationController.Direction.Down),
					"Should not be able to move the bottom item down");

				Assert.AreEqual(originalListViewItems.Count, listViewItems.Count, "Number of items definitely should not have changed");
				for (int i = 0; i < listViewItems.Count; i++)
				{
					Assert.AreEqual(originalListViewItems[i], listViewItems[i], "Order should not have changed");
				}
			}

			// TODO pH 2014.03: verify for non-WS list types
		}

		#region Writing System tests
		[Test]
		// REVIEW (Hasso) 2014.02: would we like to permit checking both defaults, when available?  Default Anal + named Vernac?
		public void CheckDefaultWsUnchecksAllOthers()
		{
			var wsOptions = new DictionaryNodeWritingSystemOptions
			{
				Options = ListOfEnabledDNOsFromStrings(new List<string> { "en", "fr" } ),
				WsType = DictionaryNodeWritingSystemOptions.WritingSystemType.Both
			};
			var controller = new DictionaryDetailsController(new ConfigurableDictionaryNode { DictionaryNodeOptions = wsOptions }, m_mediator);
			using (var view = controller.View)
			{
				// Verify setup
				var listViewItems = GetListViewItems(view);
				Assert.AreEqual(2, listViewItems.Count(item => item.Checked), "There should be exactly two items checked initially");
				Assert.AreEqual("en", listViewItems.First(item => item.Checked).Tag, "English should be checked.");
				Assert.AreEqual("fr", listViewItems.Last(item => item.Checked).Tag, "French should be checked.");

				var defaultItem = listViewItems.First(item => item.Tag is int);
				defaultItem.Checked = true;
				// SUT
				// Events are not actually fired during tests, so they must be run manually
				ReflectionHelper.CallMethod(controller, "WritingSystemCheckedChanged",
					GetListOptionsView(view), wsOptions, new ItemCheckedEventArgs(defaultItem));

				Assert.AreEqual(1, listViewItems.Count(item => item.Checked), "There should be exactly one item checked");
				Assert.AreEqual(defaultItem, listViewItems.First(item => item.Checked), "The default WS should be checked");
				Assert.AreEqual(1, wsOptions.Options.Count(option => option.IsEnabled), "There should be exactly one enabled option in the model");
				Assert.AreEqual(WritingSystemServices.GetMagicWsNameFromId((int)defaultItem.Tag),
					wsOptions.Options.First(option => option.IsEnabled).Id, "The default WS should be enabled");
			}
		}

		[Test]
		public void CheckNamedWsUnchecksDefault()
		{
			var wsOptions = new DictionaryNodeWritingSystemOptions
			{
				Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>(),
				WsType = DictionaryNodeWritingSystemOptions.WritingSystemType.Vernacular
			};
			var controller = new DictionaryDetailsController(new ConfigurableDictionaryNode { DictionaryNodeOptions = wsOptions }, m_mediator);
			using (var view = controller.View)
			{
				// Verify setup
				var listViewItems = GetListViewItems(view);
				Assert.AreEqual(1, listViewItems.Count(item => item.Checked), "There should be exactly one item checked initially");
				Assert.AreEqual(WritingSystemServices.kwsVern, listViewItems.First(item => item.Checked).Tag,
					"Default should be checked by default.");

				var namedItem = listViewItems.First(item => !(item.Tag is int));
				namedItem.Checked = true;
				// SUT
				// Events are not actually fired during tests, so they must be run manually
				ReflectionHelper.CallMethod(controller, "WritingSystemCheckedChanged",
					GetListOptionsView(view), wsOptions, new ItemCheckedEventArgs(namedItem));

				Assert.AreEqual(1, listViewItems.Count(item => item.Checked), "There should still be exactly one item checked");
				Assert.AreEqual(namedItem, listViewItems.First(item => item.Checked), "The named WS should be checked");
				Assert.AreEqual(1, wsOptions.Options.Count(option => option.IsEnabled), "There should be exactly one enabled option in the model");
				Assert.AreEqual(namedItem.Tag, wsOptions.Options.First(option => option.IsEnabled).Id, "The named WS should be enabled");
			}
		}

		[Test]
		public void CheckNamedWsPreservesOtherNamedWss()
		{
			var wsOptions = new DictionaryNodeWritingSystemOptions
			{
				Options = ListOfEnabledDNOsFromStrings(new List<string> { "en" } ),
				WsType = DictionaryNodeWritingSystemOptions.WritingSystemType.Both
			};
			var controller = new DictionaryDetailsController(new ConfigurableDictionaryNode { DictionaryNodeOptions = wsOptions }, m_mediator);
			using (var view = controller.View)
			{
				// Verify setup
				var listViewItems = GetListViewItems(view);
				Assert.AreEqual(1, listViewItems.Count(item => item.Checked), "There should be exactly one item checked initially");
				Assert.AreEqual("en", listViewItems.First(item => item.Checked).Tag, "English should be checked.");

				var otherNamedItem = listViewItems.First(item => !(item.Checked || item.Tag is int));
				otherNamedItem.Checked = true;
				// SUT
				// Events are not actually fired during tests, so they must be run manually
				ReflectionHelper.CallMethod(controller, "WritingSystemCheckedChanged",
					GetListOptionsView(view), wsOptions, new ItemCheckedEventArgs(otherNamedItem));

				Assert.AreEqual(2, listViewItems.Count(item => item.Checked), "There should now be two items checked");
				Assert.AreEqual("en", listViewItems.First(item => item.Checked).Tag, "English should still be the first checked item");
				Assert.AreEqual(otherNamedItem, listViewItems.Last(item => item.Checked), "The other named WS should be checked");
				Assert.AreEqual(2, wsOptions.Options.Count(option => option.IsEnabled), "There should be exactly two enabled options in the model");
				Assert.AreEqual("en", wsOptions.Options.First(option => option.IsEnabled).Id, "English should still be saved first");
				Assert.AreEqual(otherNamedItem.Tag, wsOptions.Options.Last(option => option.IsEnabled).Id, "The other named WS should be enabled");
			}
		}

		[Test]
		public void CannotReorderDefaultWs()
		{
			var wsOptions = new DictionaryNodeWritingSystemOptions
			{
				Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>(),
				WsType = DictionaryNodeWritingSystemOptions.WritingSystemType.Both
			};
			var controller = new DictionaryDetailsController(new ConfigurableDictionaryNode { DictionaryNodeOptions = wsOptions }, m_mediator);
			using (var view = controller.View)
			{
				var listViewItems = GetListViewItems(view);
				var originalListViewItems = new List<ListViewItem>();
				originalListViewItems.AddRange(listViewItems);

				// SUT
				Assert.Throws<ArgumentOutOfRangeException>(() =>
					controller.Reorder(listViewItems[0], DictionaryConfigurationController.Direction.Down),
					"Should not be able to reorder default writing systems");
				Assert.Throws<ArgumentOutOfRangeException>(() =>
					controller.Reorder(listViewItems.Last(item => item.Tag is int), DictionaryConfigurationController.Direction.Up),
					"Should not be able to reorder default writing systems");

				Assert.AreEqual(originalListViewItems.Count, listViewItems.Count, "Number of items definitely should not have changed");
				for(int i = 0; i < listViewItems.Count; i++)
				{
					Assert.AreEqual(originalListViewItems[i], listViewItems[i], "Order should not have changed");
				}
			}
		}

		[Test]
		public void CannotMoveNamedWsAboveDefault()
		{
			var wsOptions = new DictionaryNodeWritingSystemOptions
			{
				Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>(),
				WsType = DictionaryNodeWritingSystemOptions.WritingSystemType.Both
			};
			var controller = new DictionaryDetailsController(new ConfigurableDictionaryNode { DictionaryNodeOptions = wsOptions }, m_mediator);
			using (var view = controller.View)
			{
				var listViewItems = GetListViewItems(view);
				var originalListViewItems = new List<ListViewItem>();
				originalListViewItems.AddRange(listViewItems);

				// SUT
				Assert.Throws<ArgumentOutOfRangeException>(() => controller.Reorder(
					listViewItems[listViewItems.Last(item => item.Tag is int).Index + 1], DictionaryConfigurationController.Direction.Up),
					"Should not be able to move a named writing system above a default writing systems");

				Assert.AreEqual(originalListViewItems.Count, listViewItems.Count, "Number of items definitely should not have changed");
				for(int i = 0; i < listViewItems.Count; i++)
				{
					Assert.AreEqual(originalListViewItems[i], listViewItems[i], "Order should not have changed");
				}
			}
		}
		#endregion Writing System tests
		#endregion List tests
	}
}
