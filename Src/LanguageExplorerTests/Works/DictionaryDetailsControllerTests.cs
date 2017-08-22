// Copyright (c) 2014-2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using NUnit.Framework;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Widgets;
using SIL.LCModel.DomainServices;
using SIL.FieldWorks.FwCoreDlgControls;
using LanguageExplorer.Works.DictionaryDetailsView;
using SIL.LCModel.Utils;

namespace LanguageExplorerTests.Works
{
#if RANDYTODO // Some of this can be salvaged, but not the part where it loads the main xml config files.
	[TestFixture]
	public class DictionaryDetailsControllerTests : XWorksAppTestBase, IDisposable
	{
		private IPropertyTable m_propertyTable;
		private Mediator m_mediator;
		private LcmStyleSheet m_styleSheet;
		private DictionaryDetailsController m_staticDDController; // for testing methods that would be static if not for m_propertyTable

		#region IDisposable
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
				if(m_mediator != null && !m_mediator.IsDisposed)
				{
					m_mediator.RemoveColleague(m_window);
					m_mediator.Dispose();
				}

				if (m_window != null && !m_window.IsDisposed)
					m_window.Dispose();
				m_window = null;

				if (m_propertyTable != null && !m_propertyTable.IsDisposed)
					m_propertyTable.Dispose();
				m_propertyTable = null;

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

		#endregion IDisposable

		#region Setup and Teardown
		protected override void Init()
		{
			m_application = new MockFwXApp(new MockFwManager { Cache = Cache }, null, null);
			m_configFilePath = Path.Combine(FwDirectoryFinder.CodeDirectory, m_application.DefaultConfigurationPathname);
			m_window = new MockFwXWindow(m_application, m_configFilePath);
			((MockFwXWindow)m_window).Init(Cache); // initializes Mediator values
			m_propertyTable = m_window.PropTable;
			m_window.LoadUI(m_configFilePath); // actually loads UI here; needed for non-null stylesheet

			m_styleSheet = FontHeightAdjuster.StyleSheetFromPropertyTable(m_propertyTable);
			GenerateStyles();

			m_staticDDController = new DictionaryDetailsController(new TestDictionaryDetailsView(), m_propertyTable);
			m_staticDDController.LoadNode(null, new ConfigurableDictionaryNode());
		}
		internal class TestDictionaryDetailsView : IDictionaryDetailsView
		{
			private List<StyleComboItem> m_styles;

#pragma warning disable 67
			public event EventHandler StyleSelectionChanged;
			public event EventHandler StyleButtonClick;
			public event EventHandler BeforeTextChanged;
			public event EventHandler BetweenTextChanged;
			public event EventHandler AfterTextChanged;
#pragma warning restore 67
			public string BeforeText { get; set; }
			public string BetweenText { get; set; }
			public string AfterText { get; set; }
			public string Style { get; private set; }
			public bool StylesVisible { get; set; }
			public bool StylesEnabled { get; set; }
			public bool SurroundingCharsVisible { get; set; }
			private UserControl m_optionsView;
			public UserControl OptionsView
			{
				get { return m_optionsView; }
				set
				{
					if (IsDisposed)
						throw new ObjectDisposedException($"{GetType()} in use after being disposed");
					m_optionsView?.Dispose();
					m_optionsView = value;
				}
			}
			public bool Visible { get; set; }
			public Control TopLevelControl { get { return null; } }
			public bool IsDisposed { get; private set; }
			public bool Enabled { get; set; }

			public void SetStyles(List<StyleComboItem> styles, string selectedStyle, bool usingParaStyles)
			{
				m_styles = styles;
				Style = selectedStyle;
			}

			public void SuspendLayout() { }

			public void ResumeLayout() { }

			#region Methods to support unit tests
			public IList<StyleComboItem> GetStyles()
			{
				return m_styles;
			}

			public IList<ListViewItem> GetListViewItems()
			{
				var listOptionsView = OptionsView as IDictionaryListOptionsView;
				var listView = (ListView)ReflectionHelper.GetField(listOptionsView, "listView");
				return listView.Items.Cast<ListViewItem>().ToList();
			}

			public string GetTooltipFromOverPanel()
			{
				if (OptionsView is ButtonOverPanel)
					throw new NotImplementedException();
				var labelOverPanel = OptionsView as LabelOverPanel;
				if (labelOverPanel == null)
					return null;
				var tip = (ToolTip)ReflectionHelper.GetField(labelOverPanel, "m_tt");
				var label = (Control)ReflectionHelper.GetField(labelOverPanel, "label");
				return tip.GetToolTip(label);
			}

			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			protected virtual void Dispose(bool disposing)
			{
				System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
				if(disposing && !IsDisposed)
				{
					if(OptionsView != null)
					{
						OptionsView.Dispose();
					}
				}
				IsDisposed = true;
			}

			~TestDictionaryDetailsView()
			{
				Dispose(false);
			}
			#endregion

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

		private static IList<StyleComboItem> GetAvailableStyles(IDictionaryDetailsView view)
		{
			return ((TestDictionaryDetailsView)view).GetStyles();
		}

		private static IDictionaryListOptionsView GetListOptionsView(IDictionaryDetailsView view)
		{
			return ((TestDictionaryDetailsView)view).OptionsView as IDictionaryListOptionsView;
		}

		private static IDictionarySenseOptionsView GetSenseOptionsView(IDictionaryDetailsView view)
		{
			return ((TestDictionaryDetailsView)view).OptionsView as IDictionarySenseOptionsView;
		}

		private static IDictionaryGroupingOptionsView GetGroupingOptionsView(IDictionaryDetailsView view)
		{
			return ((TestDictionaryDetailsView)view).OptionsView as IDictionaryGroupingOptionsView;
		}

		private static IList<ListViewItem> GetListViewItems(IDictionaryDetailsView view)
		{
			return ((TestDictionaryDetailsView)view).GetListViewItems();
		}

		private static void AssertShowingCharacterStyles(IDictionaryDetailsView view)
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

		private static void AssertShowingParagraphStyles(IDictionaryDetailsView view)
		{
			foreach (var style in GetAvailableStyles(view))
			{
				Assert.IsTrue(style.Style.IsParagraphStyle);
			}
		}
		#endregion Helpers

		#region Example tests
		[Test]
		public void ExampleLoadsParagraphStylesWhenShowInParaSet()
		{
			var testNode = new ConfigurableDictionaryNode
			{
				DictionaryNodeOptions =
					new DictionaryNodeListAndParaOptions { DisplayEachInAParagraph = true }
			};
			var controller = new DictionaryDetailsController(new TestDictionaryDetailsView(), m_propertyTable);
			controller.LoadNode(null, testNode);
			using (var view = controller.View)
			{
				// SUT
				AssertShowingParagraphStyles(view);
			}
		}

		[Test]
		public void ExampleLoadsCharacterStylesWhenShowInParaNotSet()
		{
			var testNode = new ConfigurableDictionaryNode
			{
				DictionaryNodeOptions =
					new DictionaryNodeListAndParaOptions { DisplayEachInAParagraph = false }
			};
			var controller = new DictionaryDetailsController(new TestDictionaryDetailsView(), m_propertyTable);
			controller.LoadNode(null, testNode);
			using (var view = controller.View)
			{
				// SUT
				AssertShowingCharacterStyles(view);
			}
		}
		#endregion

		#region Note tests
		[Test]
		public void NoteLoadsParagraphStylesWhenShowInParaSet()
		{
			var wsOptions = new DictionaryNodeWritingSystemAndParaOptions();
			{
				wsOptions.DisplayEachInAParagraph = true;
			};
			var controller = new DictionaryDetailsController(new TestDictionaryDetailsView(), m_propertyTable);
			controller.LoadNode(null, new ConfigurableDictionaryNode { DictionaryNodeOptions = wsOptions });
			using (var view = controller.View)
			{
				var optionsView = GetListOptionsView(view);
				Assert.IsTrue(optionsView.DisplayOptionCheckBox2Checked, "'Display each Note in a separate paragraph' should be checked.");
				//// Events are not actually fired during tests, so they must be run manually
				AssertShowingParagraphStyles(view);

				optionsView.DisplayOptionCheckBox2Checked = false;
				ReflectionHelper.CallMethod(controller, "DisplayInParaChecked", GetListOptionsView(view), wsOptions);

				Assert.IsFalse(wsOptions.DisplayEachInAParagraph, "DisplayEachInAParagraph should be false.");
				AssertShowingCharacterStyles(view);
			}
		}
		#endregion

		#region Sense tests
		[Test]
		public void SenseLoadsParagraphStylesWhenShowInParaSet()
		{
			var testNode = new ConfigurableDictionaryNode
			{
				DictionaryNodeOptions =
					new DictionaryNodeSenseOptions { DisplayEachSenseInAParagraph = true }
			};
			var controller = new DictionaryDetailsController(new TestDictionaryDetailsView(), m_propertyTable);
			controller.LoadNode(null, testNode);
			using (var view = controller.View)
			{
				// SUT
				AssertShowingParagraphStyles(view);
			}
		}

		[Test]
		public void SenseLoadsCharacterStylesWhenShowInParaNotSet()
		{
			var testNode = new ConfigurableDictionaryNode
			{
				DictionaryNodeOptions =
					new DictionaryNodeSenseOptions { DisplayEachSenseInAParagraph = false }
			};
			var controller = new DictionaryDetailsController(new TestDictionaryDetailsView(), m_propertyTable);
			controller.LoadNode(null, testNode);
			using (var view = controller.View)
			{
				// SUT
				AssertShowingCharacterStyles(view);
			}
		}

		[Test]
		public void NonSenseLoadsCharacterStyles()
		{
			var controller = new DictionaryDetailsController(new TestDictionaryDetailsView(), m_propertyTable);
			using(var view = controller.View)
			{
				controller.LoadNode(null, new ConfigurableDictionaryNode {Parent = new ConfigurableDictionaryNode()});
				AssertShowingCharacterStyles(view);
			}
		}

		[Test]
		public void LoadNode_SwitchesStyles()
		{
			// Load character styles
			var node = new ConfigurableDictionaryNode
			{
				Parent = new ConfigurableDictionaryNode(), // top-level nodes are always in paragraph. Specify a Parent to allow character styles.
				DictionaryNodeOptions = new DictionaryNodeListOptions
				{
					ListId = DictionaryNodeListOptions.ListIds.Complex,
					Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>()
				}
			};
			var controller = new DictionaryDetailsController(new TestDictionaryDetailsView(), m_propertyTable);
			node.StyleType = ConfigurableDictionaryNode.StyleTypes.Character;
			controller.LoadNode(null, node);
			AssertShowingCharacterStyles(controller.View);

			// Load paragraph styles
			node.DictionaryNodeOptions = new DictionaryNodeListAndParaOptions
			{
				ListId = DictionaryNodeListOptions.ListIds.Complex,
				Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>(),
				DisplayEachInAParagraph = true
			};
			controller.LoadNode(null, node); // SUT
			AssertShowingParagraphStyles(controller.View);

			// Load character styles
			node.DictionaryNodeOptions = new DictionaryNodeWritingSystemOptions
			{
				Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>()
			};
			node.StyleType = ConfigurableDictionaryNode.StyleTypes.Character;
			controller.LoadNode(null, node); // SUT
			AssertShowingCharacterStyles(controller.View);

			// Load paragraph styles
			node.Parent = null;
			controller.LoadNode(null, node); // SUT
			AssertShowingParagraphStyles(controller.View);

			controller.View.Dispose();
		}

		[Test]
		public void LoadNode_AllowsStyleOverride()
		{
			// Load paragraph styles
			var node = new ConfigurableDictionaryNode
			{
				Parent = new ConfigurableDictionaryNode(),
				DictionaryNodeOptions = new DictionaryNodeListAndParaOptions
				{
					Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>(),
					DisplayEachInAParagraph = true
				}
			};
			var controller = new DictionaryDetailsController(new TestDictionaryDetailsView(), m_propertyTable);
			// SUT
			controller.LoadNode(null, node);
			AssertShowingParagraphStyles(controller.View);

			//Load character styles
			node.DictionaryNodeOptions = new DictionaryNodeWritingSystemOptions
			{
				Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>()
			};
			// SUT
			node.StyleType = ConfigurableDictionaryNode.StyleTypes.Character;
			controller.LoadNode(null, node);
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
			var controller = new DictionaryDetailsController(new TestDictionaryDetailsView(), m_propertyTable);
			Assert.DoesNotThrow(() =>
			{
				// SUT
				controller.LoadNode(null, node);
			});
			controller.View.Dispose();
		}

		[Test]
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
			parentSenseNode.Children = new List<ConfigurableDictionaryNode> { childGramarNode };

			var controller = new DictionaryDetailsController(new TestDictionaryDetailsView(), m_propertyTable);
			controller.LoadNode(null, childGramarNode);
			using(var view = controller.View)
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
			parentComplexFormsNode.Children = new List<ConfigurableDictionaryNode> { childGramarNode };

			// SUT is LoadNode.  `using ... .View` to ensure disposal
			var controller = new DictionaryDetailsController(new TestDictionaryDetailsView(), m_propertyTable);
			Assert.DoesNotThrow(() => { using(controller.View) { controller.LoadNode(null, childGramarNode); } });
		}
		#endregion Sense tests

		#region List tests
		[Test]
		public void GetListItems()
		{
			var complexCount = Cache.LangProject.LexDbOA.ComplexEntryTypesOA.ReallyReallyAllPossibilities.Count;
			var variantCount = Cache.LangProject.LexDbOA.VariantEntryTypesOA.ReallyReallyAllPossibilities.Count;

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
			var controller = new DictionaryDetailsController(new TestDictionaryDetailsView(), m_propertyTable);
			using (var view = controller.View)
			{
				// SUT
				controller.LoadNode(null, node);
				var listViewItems = GetListViewItems(view);

				Assert.AreEqual(XmlViewsUtils.GetGuidForUnspecifiedVariantType().ToString(), listViewItems[0].Tag,
					"The saved selection should be first");
				Assert.AreEqual(listViewItems.Count, listViewItems.Count(item => item.Checked), "All items should be checked");
				Assert.AreEqual(1, listOptions.Options.Count, "Loading the node should not affect the original list");

				listOptions.Options[0].IsEnabled = false;
				// SUT
				controller.LoadNode(null, node);
				listViewItems = GetListViewItems(view);

				Assert.AreEqual(XmlViewsUtils.GetGuidForUnspecifiedVariantType().ToString(), listViewItems[0].Tag,
					"The saved item should be first");
				Assert.False(listViewItems[0].Checked, "This item was saved as unchecked");
				Assert.AreEqual(listViewItems.Count - 1, listViewItems.Count(item => item.Checked), "All new items should be checked");
			}
		}

		[Test]
		public void LoadNode_ContextIsVisibleOnNodeSwitch()
		{
			var testNode = new ConfigurableDictionaryNode
			{
				DictionaryNodeOptions = new DictionaryNodeSenseOptions { DisplayEachSenseInAParagraph = true }
			};
			var controller = new DictionaryDetailsController(new TestDictionaryDetailsView(), m_propertyTable);
			controller.LoadNode(null, testNode);
			Assert.False(controller.View.SurroundingCharsVisible, "Context should start hidden");
			testNode = new ConfigurableDictionaryNode
			{
				IsEnabled = true,
				DictionaryNodeOptions = new DictionaryNodeWritingSystemOptions(),
				Parent = testNode
			};
			controller.LoadNode(null, testNode);
			Assert.True(controller.View.SurroundingCharsVisible, "Context should now be visible");
			controller.View.Dispose();
		}

		[Test]
		public void LoadNode_ContextIsHiddenOnNodeSwitch()
		{
			var testNode = new ConfigurableDictionaryNode
			{
				DictionaryNodeOptions = new DictionaryNodeSenseOptions { DisplayEachSenseInAParagraph = false }
			};
			var controller = new DictionaryDetailsController(new TestDictionaryDetailsView(), m_propertyTable);
			controller.LoadNode(null, testNode);
			Assert.True(controller.View.SurroundingCharsVisible, "Context should start visible");
			testNode = new ConfigurableDictionaryNode
			{
				IsEnabled = true,
				DictionaryNodeOptions = new DictionaryNodeListAndParaOptions { DisplayEachInAParagraph = true}
			};
			controller.LoadNode(null, testNode);
			Assert.False(controller.View.SurroundingCharsVisible, "Context should now be hidden");
			controller.View.Dispose();
		}

		[Test]
		public void LoadNode_LoadingNullOptionsAfterListClearsOptionsView()
		{
			var listOptions = new DictionaryNodeListOptions
			{
				Options = ListOfEnabledDNOsFromStrings(new List<string> { XmlViewsUtils.GetGuidForUnspecifiedVariantType().ToString() }),
				ListId = DictionaryNodeListOptions.ListIds.Variant
			};
			var node = new ConfigurableDictionaryNode { DictionaryNodeOptions = listOptions };
			var controller = new DictionaryDetailsController(new TestDictionaryDetailsView(), m_propertyTable);
			controller.LoadNode(null, node);
			Assert.NotNull(((TestDictionaryDetailsView)controller.View).OptionsView, "Test setup failed, OptionsView shoud not be null");
			var optionlessNode = new ConfigurableDictionaryNode();
			controller.LoadNode(null, optionlessNode);
			Assert.Null(((TestDictionaryDetailsView)controller.View).OptionsView, "OptionsView should be set to null after loading a node without options");
			controller.View.Dispose();
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
			var controller = new DictionaryDetailsController(new TestDictionaryDetailsView(), m_propertyTable);
			controller.LoadNode(null, new ConfigurableDictionaryNode { DictionaryNodeOptions = options });
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
			var controller = new DictionaryDetailsController(new TestDictionaryDetailsView(), m_propertyTable);
			controller.LoadNode(null, new ConfigurableDictionaryNode { DictionaryNodeOptions = options });
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
			var controller = new DictionaryDetailsController(new TestDictionaryDetailsView(), m_propertyTable);
			controller.LoadNode(null, new ConfigurableDictionaryNode { DictionaryNodeOptions = options });
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
				Options = ListOfEnabledDNOsFromStrings(new List<string> { "en", "fr" }),
				WsType = DictionaryNodeWritingSystemOptions.WritingSystemType.Both
			};
			var controller = new DictionaryDetailsController(new TestDictionaryDetailsView(), m_propertyTable);
			controller.LoadNode(null, new ConfigurableDictionaryNode { DictionaryNodeOptions = wsOptions });
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
		public void LoadWsOptions_DisplayCheckboxEnable()
		{
			var wsOptions = new DictionaryNodeWritingSystemOptions
			{
				Options = ListOfEnabledDNOsFromStrings(new List<string> { "en", "fr" }),
				WsType = DictionaryNodeWritingSystemOptions.WritingSystemType.Both,
				DisplayWritingSystemAbbreviations = true
			};
			var controller = new DictionaryDetailsController(new TestDictionaryDetailsView(), m_propertyTable);
			controller.LoadNode(null, new ConfigurableDictionaryNode { DictionaryNodeOptions = wsOptions });
			using (var view = controller.View)
			{
				// Verify setup
				var listViewItems = GetListViewItems(view);
				Assert.AreEqual(2, listViewItems.Count(item => item.Checked), "There should be exactly two items checked initially");
				Assert.AreEqual("en", listViewItems.First(item => item.Checked).Tag, "English should be checked.");
				Assert.AreEqual("fr", listViewItems.Last(item => item.Checked).Tag, "French should be checked.");

				var optionsView = GetListOptionsView(view);

				var otherNamedItem = listViewItems.First(item => item.Checked);
				otherNamedItem.Checked = true;
				//// SUT
				//// Events are not actually fired during tests, so they must be run manually
				ReflectionHelper.CallMethod(controller, "ListItemCheckedChanged", GetListOptionsView(view), wsOptions,
					new ItemCheckedEventArgs(otherNamedItem));

				Assert.IsTrue(optionsView.DisplayOptionCheckBoxChecked, "DisplayOption checkbox should be checked.");
				Assert.IsTrue(optionsView.DisplayOptionCheckBoxEnabled, "DisplayOption checkbox should be enabled.");
			}
		}

		[Test]
		public void LoadSenseOptions_ChecksRightBoxes()
		{
			var subSenseConfig = new ConfigurableDictionaryNode
		{
				FieldDescription = "SensesOS",
				DictionaryNodeOptions = new DictionaryNodeSenseOptions
			{
					DisplayEachSenseInAParagraph = false,
					BeforeNumber = "",
					AfterNumber = ") ",
					NumberingStyle = "%A",
					NumberEvenASingleSense = true,
					ShowSharedGrammarInfoFirst = true
				}
			};
			var senseConfig = new ConfigurableDictionaryNode
				{
				FieldDescription = "SensesOS",
				DictionaryNodeOptions = new DictionaryNodeSenseOptions
				{
					DisplayEachSenseInAParagraph = true,
					DisplayFirstSenseInline = true,
					BeforeNumber = "",
					AfterNumber = ") ",
					NumberingStyle = "%d",
					NumberEvenASingleSense = true,
					ShowSharedGrammarInfoFirst = true
				},
				Children = new List<ConfigurableDictionaryNode> { subSenseConfig }
			};
			var entryConfig = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { senseConfig }
			};
			CssGeneratorTests.PopulateFieldsForTesting(entryConfig);

			var controller = new DictionaryDetailsController(new TestDictionaryDetailsView(), m_propertyTable);
			controller.LoadNode(null, senseConfig);
			using (var view = controller.View)
			{
				var optionsView = GetSenseOptionsView(view);
				Assert.IsNotNull(optionsView, "DictionaryNodeSenseOptions should cause SenseOptionsView to be created");
				Assert.IsTrue(optionsView.SenseInPara, "checkbox set properly for showing senses in paragraph for Sense");
				Assert.IsTrue(optionsView.FirstSenseInline, "checkbox for showing first senses in line with the entry");
				Assert.AreEqual("", optionsView.BeforeText, "proper text before number loads for Sense");
				Assert.AreEqual(") ", optionsView.AfterText, "proper text after number loads for Sense");
				Assert.AreEqual("%d", optionsView.NumberingStyle, "proper numbering style loads for Sense");
				Assert.IsTrue(optionsView.NumberSingleSense, "checkbox set properly for numbering even single Sense");
				Assert.IsTrue(optionsView.ShowGrammarFirst, "checkbox set properly for show common gram info first for Senses");
				// controls are not part of IDictionarySenseOptionsView, so work around that limitation.
				ValidateSenseControls(optionsView, false);
			}

			var controller2 = new DictionaryDetailsController(new TestDictionaryDetailsView(), m_propertyTable);
			controller2.LoadNode(null, subSenseConfig);
			using (var view = controller2.View)
			{
				var optionsView = GetSenseOptionsView(view);
				Assert.IsNotNull(optionsView, "DictionaryNodeSenseOptions should cause SenseOptionsView to be created");
				Assert.IsFalse(optionsView.SenseInPara, "checkbox set properly for showing senses in paragraph for Subsense");
				Assert.AreEqual("", optionsView.BeforeText, "proper text before number loads for Subsense");
				Assert.AreEqual(") ", optionsView.AfterText, "proper text after number loads for Subsense");
				Assert.AreEqual("%A", optionsView.NumberingStyle, "proper numbering style loads for Subsense");
				Assert.IsTrue(optionsView.NumberSingleSense, "checkbox set properly for numbering even single Subsense");
				Assert.IsTrue(optionsView.ShowGrammarFirst, "checkbox set properly for hide common gram info for Subsenses");
				// controls are not part of IDictionarySenseOptionsView, so work around that limitation.
				ValidateSenseControls(optionsView, true);
			}
		}

		// REVIEW (Hasso) 2017.04: This is not the best way to test that the controller properly updates the GUI, since it also asserts GUI structure.
		// REVIEW (cont): A better way would be to use a view interface with a test implementation. If you break this test, please fix it *properly*
		// REVIEW (apology): we're too close to a release to be bothered with such a refactor.
		private static void ValidateSenseControls(object iView, bool isSubsense)
		{
			var label = isSubsense ? "Subsense" : "sense";
			Assert.AreEqual(typeof(SenseOptionsView), iView.GetType());
			var view = (Control)iView;
			var controlsChecked = 0;
			foreach (Control control in view.Controls)
			{
				if (control is GroupBox && control.Name == "groupBoxSenseNumber")
				{
					Assert.AreEqual(isSubsense ? xWorksStrings.ksSubsenseNumberConfig : "Sense Number Configuration",
						control.Text, "groupBoxSenseNumber has incorrect Text");
					++controlsChecked;
				}
				else if (control is FlowLayoutPanel && control.Name == "senseStructureVerticalFlow")
				{
					var innerControls = 0;
					foreach (Control innerControl in control.Controls)
					{
						if (innerControl is CheckBox && innerControl.Name == "checkBoxShowGrammarFirst")
						{
							Assert.IsTrue(innerControl.Enabled && innerControl.Visible, "checkBoxShowGrammarFirst should be enabled and visible for {0}", label);
							++innerControls;
						}
						else if (innerControl is CheckBox && innerControl.Name == "checkBoxSenseInPara")
						{
							Assert.IsTrue(innerControl.Enabled && innerControl.Visible, "checkBoxSenseInPara should be enabled and visible for {0}", label);
							++innerControls;
						}
						else if (innerControl is CheckBox && innerControl.Name == "checkBoxFirstSenseInline")
						{
							if (isSubsense)
								Assert.IsFalse(innerControl.Enabled || innerControl.Visible, "checkBoxFirstSenseInline should be disabled and invisible when no paras");
							else
								Assert.IsTrue(innerControl.Enabled && innerControl.Visible, "checkBoxFirstSenseInline should be enabled and visible when paras");
							++innerControls;
						}
					}
					Assert.AreEqual(3, innerControls, "Matched incorrect number of controls within senseStructureVerticalFlow for {0}", label);
					++controlsChecked;
				}
			}
			Assert.AreEqual(2, controlsChecked, "Matched incorrect number of controls for {0}", label);
		}

		[Test]
		public void LoadSenseOptions_NumberingStyleList()
		{
			var subSubSenseConfig = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				DictionaryNodeOptions = new DictionaryNodeSenseOptions
				{
					DisplayEachSenseInAParagraph = false,
					BeforeNumber = "",
					AfterNumber = ") ",
					NumberingStyle = "%a",
					NumberEvenASingleSense = true,
					ShowSharedGrammarInfoFirst = true
				}
			};
			var subSenseConfig = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				DictionaryNodeOptions = new DictionaryNodeSenseOptions
				{
					DisplayEachSenseInAParagraph = false,
					BeforeNumber = "",
					AfterNumber = ") ",
					NumberingStyle = "%A",
					NumberEvenASingleSense = true,
					ShowSharedGrammarInfoFirst = true
				},
				Children = new List<ConfigurableDictionaryNode> { subSubSenseConfig }
			};
			var senseConfig = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				DictionaryNodeOptions = new DictionaryNodeSenseOptions
				{
					DisplayEachSenseInAParagraph = true,
					DisplayFirstSenseInline = true,
					BeforeNumber = "",
					AfterNumber = ") ",
					NumberingStyle = "%d",
					NumberEvenASingleSense = true,
					ShowSharedGrammarInfoFirst = true
				},
				Children = new List<ConfigurableDictionaryNode> { subSenseConfig }
			};
			var entryConfig = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { senseConfig }
			};
			CssGeneratorTests.PopulateFieldsForTesting(entryConfig);

			var controller = new DictionaryDetailsController(new TestDictionaryDetailsView(), m_propertyTable);
			using (var view = controller.View)
			{
				controller.LoadNode(null, senseConfig);
				var expectedNumberingStyle = XmlVcDisplayVec.SupportedNumberingStyles.Where(prop => prop.FormatString != "%O").ToList();

				var optionsView = GetSenseOptionsView(view);
				var realView = optionsView as SenseOptionsView;
				Assert.IsNotNull(realView);
				var outputNumberingStyle = realView.DropdownNumberingStyles.Cast<NumberingStyleComboItem>().ToList();
				Assert.AreEqual(expectedNumberingStyle.Count(), outputNumberingStyle.Count, "Sense number's numbering style should be same count.");
				Assert.AreEqual(expectedNumberingStyle.First().Label, outputNumberingStyle.First().Label, "Sense number's numbering style should have 'none' option.");
				Assert.IsTrue(expectedNumberingStyle.All(c => outputNumberingStyle.Count(p => p.Label == c.Label) == 1), "Sense number's numbering style should be same.");

				controller.LoadNode(null, subSenseConfig);

				expectedNumberingStyle = XmlVcDisplayVec.SupportedNumberingStyles.Where(prop => prop.FormatString != "%O").ToList();

				optionsView = GetSenseOptionsView(view);
				realView = optionsView as SenseOptionsView;
				Assert.IsNotNull(realView);
				outputNumberingStyle = realView.DropdownNumberingStyles.Cast<NumberingStyleComboItem>().ToList();
				Assert.AreEqual(expectedNumberingStyle.Count, outputNumberingStyle.Count, "SubSense number's numbering style should be same count.");
				Assert.AreEqual(expectedNumberingStyle.First().Label, outputNumberingStyle.First().Label, "SubSense number's numbering style should have 'none' option.");
				Assert.IsTrue(expectedNumberingStyle.All(c => outputNumberingStyle.Count(p => p.Label == c.Label) == 1), "SubSense number's numbering style should be same.");

				controller.LoadNode(null, subSubSenseConfig);

				expectedNumberingStyle = XmlVcDisplayVec.SupportedNumberingStyles.Where(prop => prop.FormatString != "%O").ToList();

				optionsView = GetSenseOptionsView(view);
				realView = optionsView as SenseOptionsView;
				Assert.IsNotNull(realView);
				outputNumberingStyle = realView.DropdownNumberingStyles.Cast<NumberingStyleComboItem>().ToList();
				Assert.AreEqual(expectedNumberingStyle.Count(), outputNumberingStyle.Count, "SubSubSense number's numbering style should be same count.");
				Assert.AreEqual(expectedNumberingStyle.First().Label, outputNumberingStyle.First().Label, "SubSubSense number's numbering style should have 'none' option.");
				Assert.IsTrue(expectedNumberingStyle.All(c => outputNumberingStyle.Count(p => p.Label == c.Label) == 1), "SubSubSense number's numbering style should be same.");
			}
		}

		[Test]
		public void CheckNamedWsUnchecksDefault()
		{
			var wsOptions = (DictionaryNodeWritingSystemOptions)ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "vernacular" },
				DictionaryNodeWritingSystemOptions.WritingSystemType.Vernacular);
			var controller = new DictionaryDetailsController(new TestDictionaryDetailsView(), m_propertyTable);
			controller.LoadNode(null, new ConfigurableDictionaryNode { DictionaryNodeOptions = wsOptions });
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
				Options = ListOfEnabledDNOsFromStrings(new List<string> { "en" }),
				WsType = DictionaryNodeWritingSystemOptions.WritingSystemType.Both
			};
			var controller = new DictionaryDetailsController(new TestDictionaryDetailsView(), m_propertyTable);
			controller.LoadNode(null, new ConfigurableDictionaryNode { DictionaryNodeOptions = wsOptions });
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
			var controller = new DictionaryDetailsController(new TestDictionaryDetailsView(), m_propertyTable);
			controller.LoadNode(null, new ConfigurableDictionaryNode { DictionaryNodeOptions = wsOptions });
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
				for (int i = 0; i < listViewItems.Count; i++)
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
			var controller = new DictionaryDetailsController(new TestDictionaryDetailsView(), m_propertyTable);
			controller.LoadNode(null, new ConfigurableDictionaryNode { DictionaryNodeOptions = wsOptions });
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
				for (int i = 0; i < listViewItems.Count; i++)
				{
					Assert.AreEqual(originalListViewItems[i], listViewItems[i], "Order should not have changed");
				}
			}
		}
		#endregion Writing System tests
		#endregion List tests

		#region SharedItem tests
		[Test]
		public void UsersAreNotifiedOfSharedItems()
		{
			var subEntryHeadword = new ConfigurableDictionaryNode { FieldDescription = "HeadWord" };
			var sensesUnderSubentries = new ConfigurableDictionaryNode { FieldDescription = "SensesOS", ReferenceItem = "SharedSenses" };
			var subsubEntries = new ConfigurableDictionaryNode { FieldDescription = "Subentries", ReferenceItem = "SharedSubentries"};
			var sharedSubentries = new ConfigurableDictionaryNode
			{
				Label = "SharedSubentries", FieldDescription = "Subentries",
				Children = new List<ConfigurableDictionaryNode> { subEntryHeadword, sensesUnderSubentries, subsubEntries }
			};
			var subSenseGloss = new ConfigurableDictionaryNode { FieldDescription = "Gloss" };
			var subsenses = new ConfigurableDictionaryNode { FieldDescription = "SensesOS", ReferenceItem = "SharedSenses" };
			var subentriesUnderSenses = new ConfigurableDictionaryNode { FieldDescription = "Subentries", ReferenceItem = "SharedSubentries"};
			var sharedSenses = new ConfigurableDictionaryNode
			{
				Label = "SharedSenses", FieldDescription = "SensesOS",
				Children = new List<ConfigurableDictionaryNode> { subSenseGloss, subsenses, subentriesUnderSenses }
			};
			var mainEntryHeadword = new ConfigurableDictionaryNode { FieldDescription = "HeadWord" };
			var senses = new ConfigurableDictionaryNode { FieldDescription = "SensesOS", ReferenceItem = "SharedSenses" };
			var subentries = new ConfigurableDictionaryNode { FieldDescription = "Subentries", ReferenceItem = "SharedSubentries"};
			var mainEntry = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> {  mainEntryHeadword, senses, subentries }
			};
			var model = new DictionaryConfigurationModel
			{
				Parts = new List<ConfigurableDictionaryNode> { mainEntry },
				SharedItems = new List<ConfigurableDictionaryNode> { sharedSenses, sharedSubentries }
			};
			CssGeneratorTests.PopulateFieldsForTesting(model); // PopulateFieldsForTesting populates each node's Label with its FieldDescription

			using (var view = new TestDictionaryDetailsView())
			{
				var controller = new DictionaryDetailsController(view, m_propertyTable);
				// SUT (actually, ensures that messages will show after the user selects a different Config in the ConfigDlg)
				controller.LoadNode(new DictionaryConfigurationModel(), mainEntry);
				Assert.IsNullOrEmpty(view.GetTooltipFromOverPanel(), "Unshared nodes require no explanation");

				controller.LoadNode(model, mainEntry); // SUT (unshared node)
				Assert.IsNullOrEmpty(view.GetTooltipFromOverPanel(), "Unshared nodes require no explanation");

				controller.LoadNode(model, subentries); // SUT (Master Parent)
				var tooltip = view.GetTooltipFromOverPanel();
				StringAssert.Contains("LexEntry > SensesOS > Subentries", tooltip);
				StringAssert.Contains("LexEntry > Subentries > Subentries", tooltip);
				StringAssert.DoesNotContain("LexEntry > Subentries" + Environment.NewLine, tooltip, "The Master Parent itself shouldn't be listed");
				StringAssert.DoesNotContain("LexEntry > Subentries > Subentries > Subentries", tooltip, "Node finder shouldn't recurse indefinitely");
				StringAssert.DoesNotContain("SharedSubentries", tooltip, "The SharedItem's name should not be in the path");

				controller.LoadNode(model, subsubEntries); // SUT (Subordinate Parent)
				tooltip = view.GetTooltipFromOverPanel();
				StringAssert.Contains("LexEntry > Subentries.", tooltip, "Tooltip should indicate the Master Parent's location");
				StringAssert.Contains("LexEntry > Subentries > Subentries", tooltip, "Tooltip should indicate the node's full path");
				StringAssert.DoesNotContain("LexEntry > Senses", tooltip, "No other nodes should be listed");
				StringAssert.DoesNotContain("LexEntry > Subentries > Subentries >", tooltip, "No other nodes should be listed");

				controller.LoadNode(model, subEntryHeadword); // SUT (shared child)
				tooltip = view.GetTooltipFromOverPanel();
				StringAssert.Contains("LexEntry > Subentries", tooltip, "Tooltip should indicate the Master Parent's location");
				StringAssert.DoesNotContain("LexEntry > Subentries > ", tooltip, "No other nodes should be listed");
			}
		}
		#endregion SharedItem tests
		#region GroupingNode tests

		[Test]
		public void LoadGroupingOptions_SetsAllInfo()
		{
			var groupConfig = new ConfigurableDictionaryNode
			{
				FieldDescription = "grouper",
				DictionaryNodeOptions = new DictionaryNodeGroupingOptions
				{
					DisplayEachInAParagraph = true,
					Description = "Test"
				}
			};
			var entryConfig = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> {groupConfig}
			};
			CssGeneratorTests.PopulateFieldsForTesting(entryConfig);

			var controller = new DictionaryDetailsController(new TestDictionaryDetailsView(), m_propertyTable);
			controller.LoadNode(null, groupConfig);
			using (var view = controller.View)
			{
				var optionsView = GetGroupingOptionsView(view);
				Assert.IsTrue(optionsView.DisplayInParagraph);
				Assert.That(optionsView.Description, Is.StringMatching("Test"));
			}
		}
		#endregion
	}
#endif
}
