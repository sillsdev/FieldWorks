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
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
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
		private DictionaryDetailsController m_staticDDController; // for testing methods that would be static if not for m_mediator

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

				if (m_staticDDController != null && m_staticDDController.View != null && !m_staticDDController.View.IsDisposed)
					m_staticDDController.View.Dispose();
				m_staticDDController = null;
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
			m_configFilePath = Path.Combine(FwDirectoryFinder.CodeDirectory, m_application.DefaultConfigurationPathname);
			m_window = new MockFwXWindow(m_application, m_configFilePath);
			((MockFwXWindow)m_window).Init(Cache); // initializes Mediator values
			m_mediator = m_window.Mediator;
			m_window.LoadUI(m_configFilePath); // actually loads UI here; needed for non-null stylesheet

			m_styleSheet = FontHeightAdjuster.StyleSheetFromMediator(m_mediator);
			GenerateStyles();

			m_staticDDController = new DictionaryDetailsController(new ConfigurableDictionaryNode(), m_mediator);
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
		public static List<DictionaryNodeListOptions.DictionaryNodeOption> ListOfEnabledDNOsFromStrings(IEnumerable<String> idList)
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
		public void SenseLoadsParagraphStylesWhenShowInParaSet()
		{
			using (var view = new DictionaryDetailsController( // SUT
				new ConfigurableDictionaryNode { DictionaryNodeOptions = new DictionaryNodeSenseOptions { DisplayEachSenseInAParagraph = true }}, m_mediator).View)
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
				ListId = DictionaryNodeListOptions.ListIds.Complex,
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

		[Test]
		public void LoadNode_MissingWsOptionListDoesNotThrow()
		{
			// Load character styles
			var node = new ConfigurableDictionaryNode
			{
				DictionaryNodeOptions = new DictionaryNodeWritingSystemOptions
				{
					WsType = DictionaryNodeWritingSystemOptions.WritingSystemType.Analysis
				}
			};
			DictionaryDetailsController controller = null;
			// SUT - controller constructor calls LoadNode
			Assert.DoesNotThrow(()=> { controller = new DictionaryDetailsController(node, m_mediator); });
			controller.View.Dispose();
		}

		[Test]
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule", Justification = "ListOptionsView is disposed by its parent")]
		public void ShowGrammaticalInfo_LinksToSense()
		{
			var parentSenseOptions = new DictionaryNodeSenseOptions { ShowSharedGrammarInfoFirst = true };
			var parentSenseNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Senses",
				DictionaryNodeOptions = parentSenseOptions
			};
			var childGramarNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MorphoSyntaxAnalysisRA",
				Parent = parentSenseNode
			};
			parentSenseNode.Children = new List<ConfigurableDictionaryNode>{ childGramarNode };

			using (var view = new DictionaryDetailsController(childGramarNode, m_mediator).View)
			{
				var optionsView = GetListOptionsView(view);
				optionsView.DisplayOptionCheckBoxChecked = false;

				Assert.False(parentSenseOptions.ShowSharedGrammarInfoFirst, "ShowSharedGrammarInfoFirst should have been updated");
			}
		}

		[Test]
		public void ShowGrammaticalInfo_DoesNotCrashForNonSense()
		{
			var parentComplexFormsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Referenced Complex Forms",
				DictionaryNodeOptions = null
			};
			var childGramarNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MorphoSyntaxAnalysisRA",
				Parent = parentComplexFormsNode
			};
			parentComplexFormsNode.Children = new List<ConfigurableDictionaryNode>{ childGramarNode };

			// SUT is constructor.  `using ... .View` to ensure disposal
			Assert.DoesNotThrow(() => { using(new DictionaryDetailsController(childGramarNode, m_mediator).View) {} });
		}
		#endregion Sense tests

		#region List tests
		[Test]
		public void FlattenPossibilityList()
		{
			ICmPossibilityList theList = null;
			UndoableUnitOfWorkHelper.Do("undo", "redo", m_actionHandler,
				() =>
				{
					theList = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
					var topItem = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
					theList.PossibilitiesOS.Add(topItem);
					var secondLevelItem = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
					var thirdLevelItemItem = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
					topItem.SubPossibilitiesOS.Add(secondLevelItem);
					secondLevelItem.SubPossibilitiesOS.Add(thirdLevelItemItem);
				});

			Assert.AreEqual(3, DictionaryDetailsController.FlattenPossibilityList(theList.PossibilitiesOS).Count);
		}

		[Test]
		public void GetListItems()
		{
			var complexCount = DictionaryDetailsController.FlattenPossibilityList(
				Cache.LangProject.LexDbOA.ComplexEntryTypesOA.PossibilitiesOS).Count;
			var variantCount = DictionaryDetailsController.FlattenPossibilityList(
				Cache.LangProject.LexDbOA.VariantEntryTypesOA.PossibilitiesOS).Count;

			var listItems = VerifyGetListItems(DictionaryNodeListOptions.ListIds.Complex, complexCount + 1); // +1 for <None> element
			StringAssert.Contains(xWorksStrings.ksNoComplexFormType, listItems[0].Text);
			Assert.AreEqual(XmlViewsUtils.GetGuidForUnspecifiedComplexFormType().ToString(), listItems[0].Tag);

			listItems = VerifyGetListItems(DictionaryNodeListOptions.ListIds.Variant, variantCount + 1); // +1 for <None> element
			StringAssert.Contains(xWorksStrings.ksNoVariantType, listItems[0].Text);
			Assert.AreEqual(XmlViewsUtils.GetGuidForUnspecifiedVariantType().ToString(), listItems[0].Tag);

			listItems = VerifyGetListItems(DictionaryNodeListOptions.ListIds.Minor, complexCount + variantCount + 2); // Minor has 2 <None> elements
			StringAssert.Contains(xWorksStrings.ksNoVariantType, listItems[0].Text);
			Assert.AreEqual(XmlViewsUtils.GetGuidForUnspecifiedVariantType().ToString(), listItems[0].Tag);
			StringAssert.Contains(xWorksStrings.ksNoComplexFormType, listItems[variantCount + 1].Text,
				"<No Complex Form Type> should immediately follow the Variant Types");
			Assert.AreEqual(XmlViewsUtils.GetGuidForUnspecifiedComplexFormType().ToString(), listItems[variantCount + 1].Tag,
				"<No Complex Form Type> should immediately follow the Variant Types");
		}

		private List<ListViewItem> VerifyGetListItems(DictionaryNodeListOptions.ListIds listId, int expectedCount)
		{
			string label;
			var result = m_staticDDController.GetListItemsAndLabel(listId, out label); // SUT
			Assert.AreEqual(expectedCount, result.Count, String.Format("Incorrect number of {0} Types", listId));
			StringAssert.Contains(listId.ToString(), label);
			return result;
		}

		[Test]
		public void GetListItems_ThrowsIfUnknown()
		{
			string label;
			Assert.Throws<ArgumentException>(() => m_staticDDController.GetListItemsAndLabel(DictionaryNodeListOptions.ListIds.None, out label));
		}

		[Test]
		public void LoadList_NewItemsChecked()
		{
			var listOptions = new DictionaryNodeListOptions
			{
				Options = ListOfEnabledDNOsFromStrings(new List<string> { XmlViewsUtils.GetGuidForUnspecifiedVariantType().ToString() }),
				ListId = DictionaryNodeListOptions.ListIds.Variant
			};
			var node = new ConfigurableDictionaryNode { DictionaryNodeOptions = listOptions };
			var controller = new DictionaryDetailsController(node, m_mediator); // SUT
			using (var view = controller.View)
			{
				var listViewItems = GetListViewItems(view);

				Assert.AreEqual(XmlViewsUtils.GetGuidForUnspecifiedVariantType().ToString(), listViewItems[0].Tag,
					"The saved selection should be first");
				Assert.AreEqual(listViewItems.Count, listViewItems.Count(item => item.Checked), "All items should be checked");
			}
			Assert.AreEqual(1, listOptions.Options.Count, "Loading the node should not affect the original list");

			listOptions.Options[0].IsEnabled = false;
			controller.LoadNode(node); // SUT
			using (var view = controller.View)
			{
				var listViewItems = GetListViewItems(view);

				Assert.AreEqual(XmlViewsUtils.GetGuidForUnspecifiedVariantType().ToString(), listViewItems[0].Tag,
					"The saved item should be first");
				Assert.False(listViewItems[0].Checked, "This item was saved as unchecked");
				Assert.AreEqual(listViewItems.Count - 1, listViewItems.Count(item => item.Checked), "All new items should be checked");
			}
		}

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
			VerifyCannotUncheckOnlyCheckedItemInList(wsOptions);
			Assert.AreEqual(1, wsOptions.Options.Count(option => option.IsEnabled), "There should be exactly one enabled option in the model");
			Assert.AreEqual(WritingSystemServices.GetMagicWsNameFromId(WritingSystemServices.kwsVern),
				wsOptions.Options.First(option => option.IsEnabled).Id, "The same item should still be enabled");

			string label;
			var listOptions = new DictionaryNodeListOptions
			{
				// For non-WS lists, we must save any unchecked items explicitly.
				Options = m_staticDDController.GetListItemsAndLabel(DictionaryNodeListOptions.ListIds.Variant, out label)
					.Select(lvi => new DictionaryNodeListOptions.DictionaryNodeOption { Id = (string)lvi.Tag, IsEnabled = false }).ToList(),
				ListId = DictionaryNodeListOptions.ListIds.Variant
			};
			listOptions.Options.Last().IsEnabled = true;
			var selectedId = listOptions.Options.Last().Id;
			VerifyCannotUncheckOnlyCheckedItemInList(listOptions);
			Assert.AreEqual(1, listOptions.Options.Count(option => option.IsEnabled), "There should be exactly one enabled option in the model");
			Assert.AreEqual(selectedId, listOptions.Options.First(option => option.IsEnabled).Id, "The same item should still be enabled");
		}

		private void VerifyCannotUncheckOnlyCheckedItemInList(DictionaryNodeOptions options)
		{
			var controller = new DictionaryDetailsController(new ConfigurableDictionaryNode { DictionaryNodeOptions = options }, m_mediator);
			using (var view = controller.View)
			{
				// Verify setup
				var listViewItems = GetListViewItems(view);
				Assert.AreEqual(1, listViewItems.Count(item => item.Checked), "There should be exactly one item checked initially");

				var checkedItem = listViewItems.First(item => item.Checked);
				checkedItem.Checked = false;
				// SUT
				// Events are not actually fired during tests, so they must be run manually
				ReflectionHelper.CallMethod(controller, "ListItemCheckedChanged",
					GetListOptionsView(view), options as DictionaryNodeWritingSystemOptions, new ItemCheckedEventArgs(checkedItem));

				Assert.AreEqual(1, listViewItems.Count(item => item.Checked), "There should still be exactly one item checked");
				Assert.AreEqual(checkedItem, listViewItems.First(item => item.Checked), "The same item should be checked");
			}
		}

		[Test]
		public void CannotMoveTopItemUp()
		{
			VerifyCannotMoveTopItemUp(new DictionaryNodeWritingSystemOptions
			{
				Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>(),
				WsType = DictionaryNodeWritingSystemOptions.WritingSystemType.Both
			});
			VerifyCannotMoveTopItemUp(new DictionaryNodeListOptions
			{
				Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>(),
				ListId = DictionaryNodeListOptions.ListIds.Complex
			});
		}

		private void VerifyCannotMoveTopItemUp(DictionaryNodeOptions options)
		{
			var controller = new DictionaryDetailsController(new ConfigurableDictionaryNode { DictionaryNodeOptions = options }, m_mediator);
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
		}

		[Test]
		public void CannotMoveBottomItemDown()
		{
			VerifyCannotMoveBottomItemDown(new DictionaryNodeWritingSystemOptions
			{
				Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>(),
				WsType = DictionaryNodeWritingSystemOptions.WritingSystemType.Both
			});
			VerifyCannotMoveBottomItemDown(new DictionaryNodeListOptions
			{
				Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>(),
				ListId = DictionaryNodeListOptions.ListIds.Variant
			});
		}

		private void VerifyCannotMoveBottomItemDown(DictionaryNodeOptions options)
		{
			var controller = new DictionaryDetailsController(new ConfigurableDictionaryNode { DictionaryNodeOptions = options }, m_mediator);
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
				ReflectionHelper.CallMethod(controller, "ListItemCheckedChanged",
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
				ReflectionHelper.CallMethod(controller, "ListItemCheckedChanged",
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
				ReflectionHelper.CallMethod(controller, "ListItemCheckedChanged",
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
