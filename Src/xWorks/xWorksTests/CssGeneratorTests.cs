// Copyright (c) 2014-2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using ExCSS;
using NUnit.Framework;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.TestUtilities;
using SIL.FieldWorks.Common.Framework;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Widgets;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using XCore;

// ReSharper disable InconsistentNaming - Justification: Underscores are standard for test names but nowhere else in our code
namespace SIL.FieldWorks.XWorks
{
	public class CssGeneratorTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private ReadOnlyPropertyTable m_propertyTable;
		private LcmStyleSheet m_styleSheet;
		private MockFwXApp m_application;
		private string m_configFilePath;
		private MockFwXWindow m_window;
		private static readonly Color FontColor = Color.Blue;
		private static readonly Color FontBGColor = Color.Green;
		private static readonly string FontName = "foofoo";
		private static readonly Color BorderColor = Color.Red;
		private StyleInfoTable m_owningTable;
		private const FwTextAlign ParagraphAlignment = FwTextAlign.ktalJustify;
		private const bool FontBold = true;
		private const bool FontItalic = true;
		// Set these constants in MilliPoints since that is how the user values are stored in FwStyles
		private const int BorderTrailing = 5 * 1000;
		private const int BorderTop = 20 * 1000;
		private const int BorderBottom = 10 * 1000;
		private const int LeadingIndent = 24 * 1000;
		private const int TrailingIndent = 48 * 1000;
		private const int PadTop = 15 * 1000;
		private const int PadBottom = 30 * 1000;
		private const int FontSize = 10 * 1000;
		private const int DoubleSpace = 2 * 10000;	// Relative line heights are in multiples of 10000.
		private const float CssDoubleSpace = 2.0F;

		[OneTimeSetUp]
		protected void Init()
		{
			FwRegistrySettings.Init();
			m_application = new MockFwXApp(new MockFwManager { Cache = Cache }, null, null);
			m_configFilePath = Path.Combine(FwDirectoryFinder.CodeDirectory, m_application.DefaultConfigurationPathname);
			m_window = new MockFwXWindow(m_application, m_configFilePath);
			m_window.Init(Cache); // initializes Mediator values
			m_propertyTable = new ReadOnlyPropertyTable(m_window.PropTable);
			m_window.LoadUI(m_configFilePath); // actually loads UI here; needed for non-null stylesheet

			m_styleSheet = FontHeightAdjuster.StyleSheetFromPropertyTable(m_propertyTable);
			m_owningTable = new StyleInfoTable("AbbySomebody", Cache.ServiceLocator.WritingSystemManager);
		}

		[OneTimeTearDown]
		public override void FixtureTeardown()
		{
			ConfiguredLcmGenerator.Init();
			m_application.Dispose();
			m_window.PropTable.Dispose();
			FwRegistrySettings.Release();
			base.FixtureTeardown();
		}

		[SetUp]
		public void Setup()
		{
			ConfiguredLcmGenerator.Init();
			if (!m_styleSheet.Styles.Contains("FooStyle"))
			{
				GenerateStyle("FooStyle");
			}
		}

		private ConfiguredLcmGenerator.GeneratorSettings DefaultSettings
		{
			get { return new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null); }
		}

		[Test]
		public void GenerateCssForConfiguration_NullModelThrowsNullArgument()
		{
			Assert.Throws(typeof(ArgumentNullException), () => CssGenerator.GenerateCssFromConfiguration(null, m_propertyTable));
		}

		[Test]
		public void GenerateLetterHeaderCss_CssUsesDefinedStyleInfo()
		{
			var letHeadStyle = GenerateParagraphStyle(CssGenerator.LetterHeadingStyleName);
			letHeadStyle.SetExplicitParaIntProp((int)FwTextPropType.ktptAlign, (int)FwTextPropVar.ktpvEnum,
				(int)FwTextAlign.ktalCenter);
			var mediatorStyles = FontHeightAdjuster.StyleSheetFromPropertyTable(m_propertyTable);
			var styleSheet = new StyleSheet();
			//SUT
			styleSheet.Rules.AddRange(CssGenerator.GenerateLetterHeaderCss(m_propertyTable, mediatorStyles));
			// verify that the css result contains boilerplate rules and the text-align center expected from the letHeadStyle test style
			Assert.IsTrue(Regex.Match(styleSheet.ToString(), @"\.letHead\s*{\s*-moz-column-count:1;\s*-webkit-column-count:1;\s*column-count:1;\s*clear:both;\s*width:100%;.*text-align:center").Success,
							  "GenerateLetterHeaderCss did not generate the expected css rules");
		}

		[Test]
		public void GenerateCssForConfiguration_SimpleConfigurationGeneratesValidCss()
		{
			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLHeadWord",
				Label = "Headword",
				CSSClassNameOverride = "mainheadword",
				Style = "FooStyle",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "fr" })
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { headwordNode },
				FieldDescription = "LexEntry"
			};
			var model = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { mainEntryNode } };
			PopulateFieldsForTesting(model);

			//SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			// verify that the css result contains a line similar to: .lexentry {clear:both;white-space:pre;}
			VerifyRegex(cssResult, @"\.lexentry\s*{\s*clear:both;\s*white-space:pre-wrap;",
							  "Css for root node(lexentry) did not generate 'clear' and 'white-space' rules match");
			// verify that the css result contains a line similar to: .mainheadword span { {
			VerifyRegex(cssResult, @"^\s*\.mainheadword>\s*span\s*{.*",
							  "Css for child node(headword) did not generate a specific match");
		}

		[Test]
		public void GenerateCssForConfiguration_SharedConfigurationGeneratesValidCss()
		{
			var subEntryHeadwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLHeadWord",
				CSSClassNameOverride = "mainheadword",
				Style = "FooStyle",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "fr" })
			};
			var sharedNode = new ConfigurableDictionaryNode
			{
				Label = "SharedSubentries",
				Children = new List<ConfigurableDictionaryNode> { subEntryHeadwordNode },
				FieldDescription = "Subentries",
				CSSClassNameOverride = "sharedsubentries"
			};
			var subentriesNode = new ConfigurableDictionaryNode { FieldDescription = "Subentries", ReferenceItem = "SharedSubentries" };
			var mainHeadwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLHeadWord",
				CSSClassNameOverride = "mainheadword",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "fr" }),
				Before = " "
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { subentriesNode, mainHeadwordNode }
			};
			var model = DictionaryConfigurationModelTests.CreateSimpleSharingModel(mainEntryNode, sharedNode);
			PopulateFieldsForTesting(model);

			var cssGenerator = new CssGenerator();
			cssGenerator.Init(m_propertyTable);
			//SUT
			cssGenerator.AddStyles(mainHeadwordNode);
			cssGenerator.AddStyles(subEntryHeadwordNode);
			var cssResult = cssGenerator.GetStylesString();
			// verify that the css result contains a line similar to: .sharedsubentries .sharedsubentry .headword span{
			VerifyRegex(cssResult, @"^\s*\.mainheadword-sharedsubentries>\s*span\s*{.*",
				"Css for child node(headword) did not generate a match");
		}

		[Test]
		public void GenerateCssForConfiguration_LinksLookLikePlainText()
		{
			var mainEntryNode = new ConfigurableDictionaryNode { FieldDescription = "LexEntry" };
			PopulateFieldsForTesting(mainEntryNode);

			var model = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { mainEntryNode } };
			//SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			// verify that the css result contains a line similar to a { text-decoration:inherit; color:inherit; }
			Assert.IsTrue(Regex.Match(cssResult, @"\s*a\s*{[^}]*text-decoration:inherit;").Success, "Links should inherit underlines and similar.");
			Assert.IsTrue(Regex.Match(cssResult, @"\s*a\s*{[^}]*color:inherit;").Success, "Links should inherit color.");
		}

		[Test]
		public void GenerateCssForConfiguration_AddStyleDoesNotGenerateEmptyStyle()
		{
			var emptyNode = new ConfigurableDictionaryNode { FieldDescription = "Nothing"};
			var mainEntryNode = new ConfigurableDictionaryNode { FieldDescription = "LexEntry", Children = new List<ConfigurableDictionaryNode> { emptyNode } };
			PopulateFieldsForTesting(mainEntryNode);

			var cssGenerator = new CssGenerator();
			cssGenerator.Init(m_propertyTable);
			//SUT
			cssGenerator.AddStyles(emptyNode);
			Assert.That(cssGenerator.GetStylesString(), Is.EqualTo(string.Empty));
		}

		[Test]
		public void GenerateCssForConfiguration_GeneratesShimForBidirectionalText()
		{
			var mainEntryNode = new ConfigurableDictionaryNode { FieldDescription = "LexEntry" };
			PopulateFieldsForTesting(mainEntryNode);

			var model = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { mainEntryNode } };
			//SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			Assert.That(Regex.Match(cssResult, @"\*\[dir='ltr'\], \*\[dir='rtl'\]\s*{[^}]*unicode-bidi:\s*-moz-isolate;").Success, "Missing -moz-isolate rule");
			Assert.That(Regex.Match(cssResult, @"\*\[dir='ltr'\], \*\[dir='rtl'\]\s*{[^}]*unicode-bidi:\s*-ms-isolate;").Success, "Missing -ms-isolate rule");
			Assert.That(Regex.Match(cssResult, @"\*\[dir='ltr'\], \*\[dir='rtl'\]\s*{[^}]*unicode-bidi:\s*isolate;").Success, "Missing isolate rule");
		}

		[Test]
		public void GenerateCssForConfiguration_BeforeAfterSpanConfigGeneratesBeforeAfterCss()
		{
			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLHeadWord",
				Label = "Headword",
				CSSClassNameOverride = "mainheadword",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "fr" }),
				Before = "Z",
				After = "A"
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { headwordNode },
				FieldDescription = "LexEntry"
			};
			PopulateFieldsForTesting(mainEntryNode);
			var model = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { mainEntryNode } };
			//SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			// Check result for before and after rules equivalent to .headword span:first-child{content:'Z';} and .headword span:last-child{content:'A'}
			Assert.IsTrue(Regex.Match(cssResult, @"\.mainheadword>\s*span\s*:\s*first-child:before\s*{\s*content\s*:\s*'Z';\s*}").Success,
							  "css before rule with Z content not found on headword");
			Assert.IsTrue(Regex.Match(cssResult, @"\.mainheadword>\s*span\s*:\s*last-child:after\s*{\s*content\s*:\s*'A';\s*}").Success,
							  "css after rule with A content not found on headword");
		}

		[Test]
		public void GenerateCssForConfiguration_BeforeAfterSpanConfigGeneratesApostropheBeforeBetweenAfterCss()
		{
			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLHeadWord",
				Label = "Headword",
				CSSClassNameOverride = "mainheadword",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "fr" }),
				Before = "'beforeText'",
				Between = "'betweenText'",
				After = "'afterText'"
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { headwordNode },
				FieldDescription = "LexEntry"
			};
			PopulateFieldsForTesting(mainEntryNode);
			var model = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { mainEntryNode } };
			//SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			// Check result for before, between and after rules
			VerifyRegex(cssResult, @"\.mainheadword>\s*span\s*:\s*first-child:before\s*{\s*content\s*:\s*'\\\s*'beforeText\\'\s*';\s*}",
							  "css before rule with 'beforeText' content not found on headword");
			VerifyRegex(cssResult, @"\.mainheadword>\s*.mainheadwor\s*\+\s*.mainheadwor:before\s*{\s*content\s*:\s*'\\\s*'betweenText\\'\s*';\s*}",
							  "css before rule with 'betweenText' content not found on headword");
			VerifyRegex(cssResult, @"\.mainheadword>\s*span\s*:\s*last-child:after\s*{\s*content\s*:\s*'\\\s*'afterText\\'\s*';\s*}",
							  "css after rule with 'afterText' content not found on headword");
		}

		[Test]
		public void GenerateCssForConfiguration_BeforeAfterGroupingSpanWorks()
		{
			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLHeadWord",
				Label = "Headword",
				CSSClassNameOverride = "mh",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "fr" }),
				Before = "Z",
				After = "A"
			};
			var groupingNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "hwg",
				Children = new List<ConfigurableDictionaryNode> {headwordNode},
				Before = "{",
				After = "}",
				DictionaryNodeOptions = new DictionaryNodeGroupingOptions()
			};
			var mainHeadword = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLHeadWord",
				Label = "Headword",
				CSSClassNameOverride = "mh",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "fr" }),
				After = " "
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { groupingNode, mainHeadword },
				FieldDescription = "LexEntry"
			};
			PopulateFieldsForTesting(mainEntryNode);
			var model = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { mainEntryNode } };
			var cssGenerator = new CssGenerator();
			cssGenerator.Init(m_propertyTable);
			//SUT
			cssGenerator.AddStyles(mainHeadword);
			cssGenerator.AddStyles(groupingNode);
			cssGenerator.AddStyles(headwordNode);
			var cssResult = cssGenerator.GetStylesString();
			// Check the result for before and after rules for the group
			Assert.IsTrue(Regex.Match(cssResult, @"\.grouping_hwg\s*:before\s*{\s*content\s*:\s*'{';\s*}").Success,
							  "css before rule for the grouping node was not generated");
			Assert.IsTrue(Regex.Match(cssResult, @"\.grouping_hwg\s*:after\s*{\s*content\s*:\s*'}';\s*}").Success,
							  "css after rule for the grouping node was not generated");
			// Check result for before and after rules equivalent to .headword span:first-child{content:'Z';} and .headword span:last-child{content:'A'}
			Assert.IsTrue(Regex.Match(cssResult, @"\.mh-grouping_hwg>\s*span\s*:\s*first-child:before\s*{\s*content\s*:\s*'Z';\s*}").Success,
							  "css before rule with Z content not found on headword");
			Assert.IsTrue(Regex.Match(cssResult, @"\.mh-grouping_hwg>\s*span\s*:\s*last-child:after\s*{\s*content\s*:\s*'A';\s*}").Success,
							  "css after rule with A content not found on headword");
		}

		[Test]
		public void GenerateCssForConfiguration_BeforeAfterGroupingParagraphWorks()
		{
			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLHeadWord",
				Label = "Headword",
				CSSClassNameOverride = "mh",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "fr" }),
			};
			var groupingNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "hwg",
				Children = new List<ConfigurableDictionaryNode> { headwordNode },
				DictionaryNodeOptions = new DictionaryNodeGroupingOptions { DisplayEachInAParagraph = true }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { groupingNode },
				FieldDescription = "LexEntry"
			};
			PopulateFieldsForTesting(mainEntryNode);
			var model = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { mainEntryNode } };
			//SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			// Check the result for before and after rules for the group
			Assert.IsTrue(Regex.Match(cssResult, @"\.grouping_hwg\s*{\s*display\s*:\s*block;\s*}").Success,
							  "paragraph selection did not result in block display for css");
		}

		[Test]
		public void GenerateCssForConfiguration_BetweenSpaceIsNotAddedAfterSingleHeadword()
		{
			var wsOpts = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] {"fr", "en"});
			((DictionaryNodeWritingSystemOptions) wsOpts).DisplayWritingSystemAbbreviations = true;
			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLHeadWord",
				Label = "Headword",
				CSSClassNameOverride = "mainheadword",
				DictionaryNodeOptions = wsOpts,
				Between = " "
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { headwordNode },
				FieldDescription = "LexEntry"
			};
			PopulateFieldsForTesting(mainEntryNode);
			var model = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { mainEntryNode } };
			//SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			// Check result for between rule equivalent to lexentry> .mainheadword> span.writingsystemprefix + span:not(:last-child):after{content:' ';}
			VerifyRegex(cssResult, @".*\.mainheadword>\s*span\.writingsystemprefix\s*\~\s*span\.writingsystemprefix:before{.*content:' ';.*}",
				"Between selector not generated.");
		}

		[Test]
		public void GenerateCssForConfiguration_BeforeAfterConfigGeneratesBeforeAfterCss_SubentryHeadword()
		{
			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLHeadWord",
				Label = "Headword",
				CSSClassNameOverride = "headword",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "fr" }),
				Before = "Z",
				After = "A"
			};
			var subentryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> {headwordNode},
				FieldDescription = "Subentries"
			};
			var mainEntryHeadword = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLHeadWord",
				Label = "Headword",
				CSSClassNameOverride = "headword",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "fr" }),
				After = " "
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { mainEntryHeadword, subentryNode },
				FieldDescription = "LexEntry"
			};
			PopulateFieldsForTesting(mainEntryNode);
			//SUT
			var cssGenerator = new CssGenerator();
			cssGenerator.Init(m_propertyTable);
			cssGenerator.AddStyles(mainEntryHeadword);
			cssGenerator.AddStyles(headwordNode);
			var cssResult = cssGenerator.GetStylesString();
			// Check result for before and after rules equivalent to .headword-subentries span:first-child{content:'Z';} and .headword span:last-child{content:'A'}
			VerifyRegex(cssResult, @"\.headword-subentries>\s*span\s*:\s*first-child:before\s*{\s*content\s*:\s*'Z';\s*}",
							  "css before rule with Z content not found on headword");
			VerifyRegex(cssResult, @"\.headword-subentries>\s*span\s*:\s*last-child:after\s*{\s*content\s*:\s*'A';\s*}",
							  "css after rule with A content not found on headword");
		}

		[Test]
		public void GenerateCssForConfiguration_BeforeAfterConfigGeneratesBeforeAfterFormattedCss()
		{
			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLHeadWord",
				Label = "Headword",
				CSSClassNameOverride = "mainheadword",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "fr" }),
				Before = "Z",
				After = "A"
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { headwordNode },
				FieldDescription = "LexEntry"
			};
			PopulateFieldsForTesting(mainEntryNode);
			using (new TempContextStyle(this, CssGenerator.BeforeAfterBetweenStyleName))
			{
				var model = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { mainEntryNode } };
				//SUT
				var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
				// Check result for before and after rules equivalent to .headword span:first-child{content:'Z';font-size:10pt;color:#00F;}
				// and .headword span:last-child{content:'A';font-size:10pt;color:#00F;}
				Assert.IsTrue(Regex.Match(cssResult,
						@"\.mainheadword>\s*span\s*:\s*first-child:before\s*{\s*content\s*:\s*'Z';\s*font-size\s*:\s*10pt;\s*color\s*:\s*#00F;\s*}").Success,
					"css before rule with Z content with css format not found on headword");
				Assert.IsTrue(Regex.Match(cssResult,
						@"\.mainheadword>\s*span\s*:\s*last-child:after\s*{\s*content\s*:\s*'A';\s*font-size\s*:\s*10pt;\s*color\s*:\s*#00F;\s*}").Success,
					"css after rule with A content with css format not found on headword");
			}
		}

		[Test]
		public void GenerateCssForConfiguration_BeforeAfterConfigGeneratesBeforeAfterCss()
		{
			var senses = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "Senses",
				Before = "Z",
				After = "A"
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				CSSClassNameOverride = "lexentry",
				Children = new List<ConfigurableDictionaryNode> { senses }
			};
			PopulateFieldsForTesting(mainEntryNode);
			using (new TempContextStyle(this, CssGenerator.BeforeAfterBetweenStyleName))
			{
				var model = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { mainEntryNode } };
				//SUT
				var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
				Assert.That(cssResult, Contains.Substring(".senses:after"));
				Assert.That(cssResult, Does.Not.Contain(".senses .sense:after"));
				Assert.That(cssResult, Does.Not.Contain(".senses .sense:last-child:after"));
			}
		}

		[Test]
		public void GenerateCssForConfiguration_DefinitionOrGlossBeforeAfterConfigGeneratesBeforeAfterCss()
		{
			var wsOpts = new DictionaryNodeWritingSystemOptions
			{
				Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>
				{
					new DictionaryNodeListOptions.DictionaryNodeOption {Id = "fr"},
					new DictionaryNodeListOptions.DictionaryNodeOption {Id = "en"}
				}
			};
			var definitionOrGloss = new ConfigurableDictionaryNode
			{
				FieldDescription = "DefinitionOrGloss",
				Before = "<",
				Between = ",",
				After = ">",
				DictionaryNodeOptions = wsOpts
			};
			var senses = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "Senses",
				Children = new List<ConfigurableDictionaryNode> { definitionOrGloss }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				CSSClassNameOverride = "lexentry",
				Children = new List<ConfigurableDictionaryNode> { senses }
			};

			PopulateFieldsForTesting(mainEntryNode);
			var model = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { mainEntryNode } };
			//SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			VerifyRegex(cssResult, @".definitionorgloss> span:first-child:before{.*content:'<';.*}", "Before not generated.");
			VerifyRegex(cssResult, @".definitionorgloss> span\+span\[lang\=\'en\']:before{.*content:',';.*}", "Between not generated.");
			VerifyRegex(cssResult, @".definitionorgloss> span:last-child:after{.*content:'>';.*}", "After not generated.");
		}

		[Test]
		public void GenerateCssForStyleName_CharacterStyleWorks()
		{
			GenerateStyle("Dictionary-Vernacular");
			//SUT
			var styleDeclaration = CssGenerator.GenerateCssStyleFromLcmStyleSheet("Dictionary-Vernacular", CssGenerator.DefaultStyle, m_propertyTable);
			VerifyFontInfoInCss(FontColor, FontBGColor, FontName, FontBold, FontItalic, FontSize, styleDeclaration.ToString());
		}

		[Test]
		public void GenerateCssForStyleName_ParagraphBorderWorks()
		{
			GenerateParagraphStyle("Dictionary-Paragraph");
			//SUT
			var styleDeclaration = CssGenerator.GenerateCssStyleFromLcmStyleSheet("Dictionary-Paragraph", CssGenerator.DefaultStyle, m_propertyTable);
			//border leading omitted from paragraph style definition which should result in 0pt left width
			VerifyParagraphBorderInCss(BorderColor, 0, BorderTrailing, BorderBottom, BorderTop, styleDeclaration.ToString());
		}

		[Test]
		public void GenerateCssForConfiguration_WithBulletStyleOnNoteAndWritingSystemsCss()
		{
			var wsOpts = new DictionaryNodeWritingSystemAndParaOptions
			{
				Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>
				{
					new DictionaryNodeListOptions.DictionaryNodeOption {Id = "en"},
					new DictionaryNodeListOptions.DictionaryNodeOption {Id = "fr"}
				},
				DisplayWritingSystemAbbreviations = true,
				DisplayEachInAParagraph = true
			};
			GenerateBulletStyle("Bulleted List");
			var anthroNote = new ConfigurableDictionaryNode
			{
				FieldDescription = "AnthroNote",
				Before = "<",
				Between = ",",
				After = ">",
				DictionaryNodeOptions = wsOpts,
				Style = "Bulleted List"
			};
			var bibliography = new ConfigurableDictionaryNode
			{
				FieldDescription = "Bibliography",
				Before = "<",
				Between = ",",
				After = ">",
				DictionaryNodeOptions = wsOpts,
				Style = "Bulleted List"
			};
			var senses = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "Senses",
				Children = new List<ConfigurableDictionaryNode> { anthroNote, bibliography }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				CSSClassNameOverride = "lexentry",
				Children = new List<ConfigurableDictionaryNode> { senses }
			};

			PopulateFieldsForTesting(mainEntryNode);
			var model = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { mainEntryNode } };
			//SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			VerifyRegex(cssResult, @"\s*\.anthronote:before{.*font-size:12pt;.*color:#F00;.*content:'\\25A0';.*}",
				"AnthroNote content not generated.");
			VerifyRegex(cssResult, @"\s*\.anthronote>.*span.writingsystemprefix.~.span.writingsystemprefix:before",
				"AnthroNote between content not generated.");
			Assert.That(Regex.Match(cssResult, @".*\.anthronote:after").Success, Is.False, "AnthroNote after content should not generated.");
			VerifyRegex(cssResult, @"\s*\.bibliography> span{.*font-size:12pt;.*color:#F00;.*content:'\\25A0';.*}",
				"Bibliography content not generated.");
			Assert.That(Regex.Match(cssResult, @"\s*\.bibliography>.*span.writingsystemprefix.~.span.writingsystemprefix:before").Success, Is.False,
				"Bibliography between content should not generated.");
			Assert.That(Regex.Match(cssResult, @"\s*\.bibliography:after").Success, Is.False, "Bibliography after content should not generated.");
		}

		[Test]
		public void GenerateCssForCustomBulletStyleForSenses()
		{
			GenerateCustomBulletStyle("Bulleted List1");
			var senses = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "Senses",
				DictionaryNodeOptions = new DictionaryNodeSenseOptions
				{
					NumberStyle = "Dictionary-SenseNum",
					DisplayEachSenseInAParagraph = true
				},
				Style = "Bulleted List1"
			};
			var entry = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				CSSClassNameOverride = "lexentry",
				Children = new List<ConfigurableDictionaryNode> { senses }
			};
			var model = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { entry } };
			PopulateFieldsForTesting(model);
			// SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			const string regExPected = @"\s*.senses\s>\s.sensecontent:before.*{.*content:'@';.*font-size:14pt;.*color:Green;.*}";
			VerifyRegex(cssResult, regExPected, "Custom bullet content not generated.");
		}

		[Test]
		public void GenerateCssForStyleName_ParagraphMarginIsAbsolute_NoParent_Works()
		{
			GenerateParagraphStyle("Dictionary-Paragraph-Padding");
			//SUT
			var styleDeclaration = CssGenerator.GenerateCssStyleFromLcmStyleSheet("Dictionary-Paragraph-Padding", CssGenerator.DefaultStyle, null, m_propertyTable);
			// Indent values are converted into pt values on export
			Assert.That(styleDeclaration.ToString(), Contains.Substring("margin-left:" + LeadingIndent / 1000 + "pt"));
			Assert.That(styleDeclaration.ToString(), Contains.Substring("padding-right:" + TrailingIndent / 1000 + "pt"));
			Assert.That(styleDeclaration.ToString(), Contains.Substring("padding-top:" + PadTop / 1000 + "pt"));
			Assert.That(styleDeclaration.ToString(), Contains.Substring("padding-bottom:" + PadBottom / 1000 + "pt"));
		}

		[Test]
		public void GenerateCssForStyleName_ParagraphMarginIsAbsolute_ParentOverrideWorks()
		{
			var childIndent = 15 * 1000;
			var parentStyle = GenerateParagraphStyle("ParagraphMarginAbsoluteParentOverrideParent");
			var childStyle = GenerateParagraphStyle("ParagraphMarginAbsoluteParentOverrideChild");
			childStyle.SetExplicitParaIntProp((int)FwTextPropType.ktptLeadingIndent, 0, childIndent);
			var gloss = new ConfigurableDictionaryNode { FieldDescription = "Gloss" };
			var senses = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "Senses",
				DictionaryNodeOptions = new DictionaryNodeSenseOptions { DisplayEachSenseInAParagraph = true },
				Children = new List<ConfigurableDictionaryNode> { gloss },
				Style = childStyle.Name
			};
			var entry = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Style = parentStyle.Name,
				Children = new List<ConfigurableDictionaryNode> { senses }
			};
			PopulateFieldsForTesting(entry);
			//SUT
			var styleDeclaration = CssGenerator.GenerateCssStyleFromLcmStyleSheet("ParagraphMarginAbsoluteParentOverrideChild", CssGenerator.DefaultStyle, senses, m_propertyTable);
			// Indent values are converted into pt values on export
			// LeadingIndent is the value generated for the parent (24).
			// In order for the child to have a correct indent (15) it must overcome the larger indent of the parent by a negative amount
			Assert.That(styleDeclaration.ToString(), Contains.Substring("margin-left:" + (childIndent - LeadingIndent) / 1000 + "pt"));
			Assert.That(styleDeclaration.ToString(), Contains.Substring("padding-right:" + TrailingIndent / 1000 + "pt"));
			Assert.That(styleDeclaration.ToString(), Contains.Substring("padding-top:" + PadTop / 1000 + "pt"));
			Assert.That(styleDeclaration.ToString(), Contains.Substring("padding-bottom:" + PadBottom / 1000 + "pt"));
		}

		[Test]
		public void GenerateCssForStyleName_ParagraphMarginIsAbsolute_ChildEqualToParentResultsInZeroMargin()
		{
			var parentStyle = GenerateParagraphStyle("ParagraphMarginAbsoluteParentOverrideParent");
			var childStyle = GenerateParagraphStyle("ParagraphMarginAbsoluteParentOverrideChild");
			var gloss = new ConfigurableDictionaryNode { FieldDescription = "Gloss" };
			var senses = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "Senses",
				DictionaryNodeOptions = new DictionaryNodeSenseOptions { DisplayEachSenseInAParagraph = true },
				Children = new List<ConfigurableDictionaryNode> { gloss },
				Style = childStyle.Name
			};
			var entry = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Style = parentStyle.Name,
				Children = new List<ConfigurableDictionaryNode> { senses }
			};
			PopulateFieldsForTesting(entry);
			//SUT
			var styleDeclaration = CssGenerator.GenerateCssStyleFromLcmStyleSheet("ParagraphMarginAbsoluteParentOverrideChild", CssGenerator.DefaultStyle, senses, m_propertyTable);
			// Indent values are converted into pt values on export
			Assert.That(styleDeclaration.ToString(), Contains.Substring("margin-left:" + 0 + "pt"));
			Assert.That(styleDeclaration.ToString(), Contains.Substring("padding-right:" + TrailingIndent / 1000 + "pt"));
			Assert.That(styleDeclaration.ToString(), Contains.Substring("padding-top:" + PadTop / 1000 + "pt"));
			Assert.That(styleDeclaration.ToString(), Contains.Substring("padding-bottom:" + PadBottom / 1000 + "pt"));
		}

		[Test]
		public void GenerateCssForStyleName_ParagraphMarginIsAbsolute_GrandParentAndParentWork()
		{
			var grandParentStyle = GenerateParagraphStyle("ParagraphMarginAbsoluteGrandPooBah");
			var parentStyle = GenerateParagraphStyle("ParagraphMarginAbsoluteParental");
			var childStyle = GenerateParagraphStyle("ParagraphMarginAbsoluteKiddo");
			grandParentStyle.SetExplicitParaIntProp((int)FwTextPropType.ktptLeadingIndent, 0, 5 * 1000);
			parentStyle.SetExplicitParaIntProp((int)FwTextPropType.ktptLeadingIndent, 0, 12 * 1000);
			childStyle.SetExplicitParaIntProp((int)FwTextPropType.ktptLeadingIndent, 0, 20 * 1000);
			var gloss = new ConfigurableDictionaryNode { FieldDescription = "Gloss" };
			var subSenses = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "Senses",
				DictionaryNodeOptions = new DictionaryNodeSenseOptions { DisplayEachSenseInAParagraph = true },
				Children = new List<ConfigurableDictionaryNode> { gloss },
				Style = childStyle.Name
			};
			var senses = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "Senses",
				DictionaryNodeOptions = new DictionaryNodeSenseOptions { DisplayEachSenseInAParagraph = true },
				Children = new List<ConfigurableDictionaryNode> { subSenses },
				Style = parentStyle.Name
			};
			var entry = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Style = grandParentStyle.Name,
				Children = new List<ConfigurableDictionaryNode> { senses }
			};
			PopulateFieldsForTesting(entry);
			// In order to generate the correct indentation at each level we should see 5pt margin for each style
			//SUT
			var gpDeclaration = CssGenerator.GenerateCssStyleFromLcmStyleSheet(grandParentStyle.Name, CssGenerator.DefaultStyle, entry, m_propertyTable);
			Assert.That(gpDeclaration.ToString(), Contains.Substring("margin-left:5pt"), "Grandparent margin incorrectly generated");
			var parentDeclaration = CssGenerator.GenerateCssStyleFromLcmStyleSheet(parentStyle.Name, CssGenerator.DefaultStyle, senses, m_propertyTable);
			Assert.That(parentDeclaration.ToString(), Contains.Substring("margin-left:7pt"), "Parent margin incorrectly generated");
			var childDeclaration = CssGenerator.GenerateCssStyleFromLcmStyleSheet(childStyle.Name, CssGenerator.DefaultStyle, subSenses, m_propertyTable);
			Assert.That(childDeclaration.ToString(), Contains.Substring("margin-left:8pt"), "Child margin incorrectly generated");
		}

		[Test]
		public void GenerateCssForStyleName_SensesAndSubSenses_BeforeBetweenAfterWork()
		{
			var gloss = new ConfigurableDictionaryNode { FieldDescription = "Gloss" };
			var subSenses = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "Senses",
				DictionaryNodeOptions = new DictionaryNodeSenseOptions(),
				Children = new List<ConfigurableDictionaryNode> { gloss },
				Before = "^",
				Between = ",",
				After = ":"
			};
			var senses = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "Senses",
				DictionaryNodeOptions = new DictionaryNodeSenseOptions(),
				Children = new List<ConfigurableDictionaryNode> { subSenses },
				Before = "#",
				Between = ";",
				After = "."
			};
			var entry = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { senses }
			};
			PopulateFieldsForTesting(entry);
			// In order to generate the correct indentation at each level we should see 5pt margin for each style
			//SUT
			var cssGenerator = new CssGenerator();
			cssGenerator.Init(m_propertyTable);
			cssGenerator.AddStyles(entry);
			var senseClassName = cssGenerator.AddStyles(senses);
			var subsenseClassName = cssGenerator.AddStyles(subSenses);
			Assert.That(senseClassName, Is.Not.EqualTo(subsenseClassName));
			var styleResults = cssGenerator.GetStylesString();
			Assert.That(styleResults, Contains.Substring($"{senseClassName}:before"));
			Assert.That(styleResults, Contains.Substring($"{subsenseClassName}:before"));
			Assert.That(styleResults, Contains.Substring($"{senseClassName}:after"));
			Assert.That(styleResults, Contains.Substring($"{subsenseClassName}:after"));
			Assert.That(styleResults, Contains.Substring($"{senseClassName}> .sensecontent + .sensecontent:before"));
			Assert.That(styleResults, Contains.Substring($"{subsenseClassName}> .sensecontent + .sensecontent:before"));
		}

		[Test]
		public void GenerateCssForStyleName_HangingIndentWithExistingMargin_NoParentWorks()
		{
			var hangingIndent = -15 * 1000;
			var testStyle = GenerateParagraphStyle("Dictionary-Paragraph-Padding-Hanging");
			testStyle.SetExplicitParaIntProp((int)FwTextPropType.ktptFirstIndent, 0, hangingIndent);
			//SUT
			var styleDeclaration = CssGenerator.GenerateCssStyleFromLcmStyleSheet("Dictionary-Paragraph-Padding-Hanging", CssGenerator.DefaultStyle, m_propertyTable);
			// Indent values are converted into pt values on export
			Assert.That(styleDeclaration.ToString(), Contains.Substring("margin-left:" + (LeadingIndent - hangingIndent) / 1000 + "pt"));
			Assert.That(styleDeclaration.ToString(), Contains.Substring("text-indent:" + hangingIndent / 1000 + "pt"));
		}

		[Test]
		public void GenerateCssForStyleName_HangingIndentWithExistingMargin_ParentOverrideWorks()
		{
			var parentHangingIndent = -8 * 1000;
			var childHangingIndent = -10 * 1000;
			var childStyleName = "Dictionary-Paragraph-Padding-Hanging-Child";
			var parentStyle = GenerateParagraphStyle("Dictionary-Paragraph-Padding-Hanging-Parent");
			var childStyle = GenerateParagraphStyle(childStyleName);
			var gloss = new ConfigurableDictionaryNode { FieldDescription = "Gloss" };
			var senses = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "Senses",
				DictionaryNodeOptions = new DictionaryNodeSenseOptions { DisplayEachSenseInAParagraph = true },
				Children = new List<ConfigurableDictionaryNode> { gloss },
				Style = childStyle.Name
			};
			var entry = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Style = parentStyle.Name,
				Children = new List<ConfigurableDictionaryNode> { senses }
			};
			PopulateFieldsForTesting(entry);
			parentStyle.SetExplicitParaIntProp((int)FwTextPropType.ktptFirstIndent, 0, parentHangingIndent);
			childStyle.SetExplicitParaIntProp((int)FwTextPropType.ktptFirstIndent, 0, childHangingIndent);
			//SUT
			var parentDeclaration = CssGenerator.GenerateCssStyleFromLcmStyleSheet(parentStyle.Name, CssGenerator.DefaultStyle, entry, m_propertyTable);
			var childDeclaration = CssGenerator.GenerateCssStyleFromLcmStyleSheet(childStyleName, CssGenerator.DefaultStyle, senses, m_propertyTable);
			// Indent values are converted into pt values on export
			Assert.That(parentDeclaration.ToString(), Contains.Substring("margin-left:" + (LeadingIndent - parentHangingIndent) / 1000 + "pt"));
			Assert.That(parentDeclaration.ToString(), Contains.Substring("text-indent:" + parentHangingIndent / 1000 + "pt"));

			Assert.That(childDeclaration.ToString(), Contains.Substring("text-indent:" + childHangingIndent / 1000 + "pt"));
			// The child margin should be the negation of the parent adjusted margin plus the LeadingIndent less the childs hanging indent
			var adjustedChildIndent = parentHangingIndent - childHangingIndent;
			Assert.That(childDeclaration.ToString(), Contains.Substring("margin-left:" + adjustedChildIndent / 1000 + "pt"));
		}

		[Test]
		public void GenerateCssForStyleName_ComplexFormsUnderSenses_FirstSenseAndFollowingSenseRulesGenerated()
		{
			var parentHangingIndent = -8 * 1000;
			var childHangingIndent = -10 * 1000;
			var grandChildHangingIndent = -3 * 1000;
			var grandChildStyleName = "Dictionary-Paragraph-Padding-Hanging-GrandBaby";
			var childStyleName = "Dictionary-Paragraph-Padding-Hanging-Child";
			var parentStyle = GenerateParagraphStyle("Dictionary-Paragraph-Padding-Hanging-Parent");
			var childStyle = GenerateParagraphStyle(childStyleName);
			var grandChildStyle = GenerateParagraphStyle(grandChildStyleName);
			var exampleChild = new ConfigurableDictionaryNode { FieldDescription = "Example" };
			var examples = new ConfigurableDictionaryNode
			{
				FieldDescription = "ExamplesOS",
				DictionaryNodeOptions = new DictionaryNodeListAndParaOptions { DisplayEachInAParagraph = true },
				Style = grandChildStyleName,
				Children = new List<ConfigurableDictionaryNode> { exampleChild }
			};
			var gloss = new ConfigurableDictionaryNode { FieldDescription = "Gloss" };
			var senses = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "Senses",
				DictionaryNodeOptions = new DictionaryNodeSenseOptions { DisplayEachSenseInAParagraph = true },
				Children = new List<ConfigurableDictionaryNode> { gloss, examples },
				Style = childStyle.Name
			};
			var entry = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Style = parentStyle.Name,
				Children = new List<ConfigurableDictionaryNode> { senses }
			};
			PopulateFieldsForTesting(entry);
			parentStyle.SetExplicitParaIntProp((int)FwTextPropType.ktptFirstIndent, 0, parentHangingIndent);
			childStyle.SetExplicitParaIntProp((int)FwTextPropType.ktptFirstIndent, 0, childHangingIndent);
			grandChildStyle.SetExplicitParaIntProp((int)FwTextPropType.ktptFirstIndent, 0, grandChildHangingIndent);

			var grandChildDeclaration = CssGenerator.GenerateCssStyleFromLcmStyleSheet(grandChildStyleName, CssGenerator.DefaultStyle, examples, m_propertyTable, true);

			Assert.AreEqual(2, grandChildDeclaration.Count);
			// Indent values are converted into pt values on export
			var firstSenseChildCss = grandChildDeclaration[0].ToString();
			var allOtherSenseChildrenCss = grandChildDeclaration[1].ToString();
			Assert.That(firstSenseChildCss, Is.Not.EqualTo(allOtherSenseChildrenCss));
			var firstSenseIndent = parentHangingIndent - grandChildHangingIndent;
			var otherSenseIndent = childHangingIndent - grandChildHangingIndent;
			Assert.That(firstSenseChildCss, Contains.Substring("margin-left:" + firstSenseIndent / 1000 + "pt"));
			Assert.That(firstSenseChildCss, Contains.Substring("text-indent:" + grandChildHangingIndent / 1000 + "pt"));
			Assert.That(allOtherSenseChildrenCss, Contains.Substring("margin-left:" + otherSenseIndent / 1000 + "pt"));
			Assert.That(allOtherSenseChildrenCss, Contains.Substring("text-indent:" + grandChildHangingIndent / 1000 + "pt"));
		}

		[Test]
		public void GenerateCssForStyleName_Parallel_Passage_ReferenceRulesGenerated()
		{
			var grandChildStyleName = "Dictionary-Parallel-Passage-Reference-GrandBaby";
			var childStyleName = "Dictionary-Parallel-Passage-Reference-Child";
			var parentStyle = GenerateParagraphStyle("Dictionary-Parallel-Passage-Reference-Parent");
			var childStyle = GenerateParagraphStyle(childStyleName);
			var grandChildStyle = GenerateParagraphStyle(grandChildStyleName);
			var SubSubSenseOptions = new DictionaryNodeSenseOptions { NumberingStyle = "%A", ParentSenseNumberingStyle = "%.", NumberEvenASingleSense = true };
			var subSenseOptions = new DictionaryNodeSenseOptions { NumberingStyle = "%a", ParentSenseNumberingStyle = "%." };
			var senseOptions = new DictionaryNodeSenseOptions { NumberingStyle = "%d" };
			var exampleChild = new ConfigurableDictionaryNode { FieldDescription = "Example" };
			var examples = new ConfigurableDictionaryNode
			{
				FieldDescription = "ExamplesOS",
				DictionaryNodeOptions = new DictionaryNodeListAndParaOptions { DisplayEachInAParagraph = true },
				Style = grandChildStyleName,
				Children = new List<ConfigurableDictionaryNode> { exampleChild }
			};
			var gloss = new ConfigurableDictionaryNode { FieldDescription = "Gloss" };
			var subSubsenses = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				Label = "SubSubsenses",
				DictionaryNodeOptions = SubSubSenseOptions,
				Children = new List<ConfigurableDictionaryNode> { gloss },
				Style = grandChildStyleName,
			};
			var subSense = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				Label = "Subsenses",
				DictionaryNodeOptions = subSenseOptions,
				Children = new List<ConfigurableDictionaryNode> { gloss, subSubsenses },
				Style = childStyle.Name
			};
			var senses = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "Senses",
				DictionaryNodeOptions = senseOptions,
				Children = new List<ConfigurableDictionaryNode> { gloss, subSense },
				Style = parentStyle.Name,
			};
			var entry = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { senses }
			};
			PopulateFieldsForTesting(entry);
			parentStyle.SetExplicitParaIntProp((int)FwTextPropType.ktptKeepWithNext, 0, 1);
			childStyle.SetExplicitParaIntProp((int)FwTextPropType.ktptKeepTogether, 0, 1);
			grandChildStyle.SetExplicitParaIntProp((int)FwTextPropType.ktptKeepWithNext, 0, 1);

			var grandChildDeclaration = CssGenerator.GenerateCssStyleFromLcmStyleSheet(grandChildStyleName, CssGenerator.DefaultStyle, examples, m_propertyTable, true);
			Assert.That(grandChildDeclaration[0].ToString(), Contains.Substring("page-break-inside:initial"));
			var childDeclaration = CssGenerator.GenerateCssStyleFromLcmStyleSheet(childStyleName, CssGenerator.DefaultStyle, examples, m_propertyTable, true);
			Assert.That(childDeclaration[0].ToString(), Contains.Substring("page-break-inside:avoid"));
			var parentDeclaration = CssGenerator.GenerateCssStyleFromLcmStyleSheet(parentStyle.Name, CssGenerator.DefaultStyle, examples, m_propertyTable, true);
			Assert.That(parentDeclaration[0].ToString(), Contains.Substring("page-break-inside:initial"));
		}

		[Test]
		public void GenerateCssForStyleName_ParentMargin_DoesNotAffectCharacterStyle()
		{
			var childStyle = GenerateStyle("HeadWordStyle");
			var parentStyle = GenerateParagraphStyle("Dictionary-Paragraph-Padding-Hanging-Parent");
			var wsOpts = new DictionaryNodeWritingSystemOptions
			{
				Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>
				{
					new DictionaryNodeListOptions.DictionaryNodeOption {Id = "en"},
					new DictionaryNodeListOptions.DictionaryNodeOption {Id = "fr"}
				},
				DisplayWritingSystemAbbreviations = true
			};
			var headword = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLHeadWord",
				CSSClassNameOverride = "headword",
				DictionaryNodeOptions = wsOpts,
				Style = childStyle.Name
			};
			var entry = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Style = parentStyle.Name,
				Children = new List<ConfigurableDictionaryNode> { headword }
			};
			PopulateFieldsForTesting(entry);
			//SUT
			var childDeclaration = CssGenerator.GenerateCssStyleFromLcmStyleSheet(childStyle.Name, CssGenerator.DefaultStyle, headword, m_propertyTable);
			Assert.That(childDeclaration.ToString(), Does.Not.Contain("margin-left"));
		}

		[Test]
		public void GenerateCssForStyleName_HangingIndentWithNoMarginWorks()
		{
			var hangingIndent = -15 * 1000;
			var testStyle = GenerateEmptyParagraphStyle("Dictionary-Paragraph-Hanging-No-Padding");
			testStyle.SetExplicitParaIntProp((int)FwTextPropType.ktptFirstIndent, 0, hangingIndent);
			//SUT
			var styleDeclaration = CssGenerator.GenerateCssStyleFromLcmStyleSheet("Dictionary-Paragraph-Hanging-No-Padding", CssGenerator.DefaultStyle, m_propertyTable);
			// Indent values are converted into pt values on export
			Assert.That(styleDeclaration.ToString(), Contains.Substring("margin-left:" + -hangingIndent / 1000 + "pt"));
			Assert.That(styleDeclaration.ToString(), Contains.Substring("text-indent:" + hangingIndent / 1000 + "pt"));
		}

		[Test]
		public void GenerateCssForStyleName_ParagraphAlignmentWorks()
		{
			GenerateParagraphStyle("Dictionary-Paragraph-Justify");
			//SUT
			var styleDeclaration = CssGenerator.GenerateCssStyleFromLcmStyleSheet("Dictionary-Paragraph-Justify", CssGenerator.DefaultStyle, m_propertyTable);
			Assert.That(styleDeclaration.ToString(), Contains.Substring("align:" + ParagraphAlignment.AsCssString()));
		}

		[Test]
		public void GenerateCssForStyleName_ParagraphRelativeLineSpacingWorks()
		{
			GenerateParagraphStyle("Dictionary-Paragraph-RelativeLine");
			//SUT
			var styleDeclaration = CssGenerator.GenerateCssStyleFromLcmStyleSheet("Dictionary-Paragraph-RelativeLine", CssGenerator.DefaultStyle, m_propertyTable);
			Assert.That(styleDeclaration.ToString(), Contains.Substring("line-height:" + CssDoubleSpace + ";"));
		}

		[Test]
		public void GenerateCssForStyleName_ParagraphAbsoluteLineSpacingWorks()
		{
			var style = GenerateParagraphStyle("Dictionary-Paragraph-Absolute");
			style.SetExplicitParaIntProp((int)FwTextPropType.ktptLineHeight, (int)FwTextPropVar.ktpvMilliPoint, 9 * 1000);
			//SUT
			var styleDeclaration = CssGenerator.GenerateCssStyleFromLcmStyleSheet("Dictionary-Paragraph-Absolute", CssGenerator.DefaultStyle, m_propertyTable);
			Assert.That(styleDeclaration.ToString(), Contains.Substring("line-height:9pt;"));
		}

		[Test]
		public void GenerateCssForStyleName_ParagraphLineSpacingExactlyWorks()
		{
			int exactly = -12000;
			var style = GenerateParagraphStyle("Dictionary-Paragraph-LineSpacingExactly");
			style.SetExplicitParaIntProp((int)FwTextPropType.ktptLineHeight, (int)FwTextPropVar.ktpvMilliPoint, exactly);
			//SUT
			var styleDeclaration = CssGenerator.GenerateCssStyleFromLcmStyleSheet("Dictionary-Paragraph-LineSpacingExactly", CssGenerator.DefaultStyle, m_propertyTable);
			Assert.That(styleDeclaration.ToString(), Contains.Substring("line-height:12pt;"));
		}

		[Test]
		public void GenerateCssForStyleName_ParagraphLineSpacingAtleastWorks()
		{
			int atleast = 12000;
			var style = GenerateParagraphStyle("Dictionary-Paragraph-LineSpacingAtleast");
			style.SetExplicitParaIntProp((int)FwTextPropType.ktptLineHeight, (int)FwTextPropVar.ktpvMilliPoint, atleast);
			//SUT
			var styleDeclaration = CssGenerator.GenerateCssStyleFromLcmStyleSheet("Dictionary-Paragraph-LineSpacingAtleast", CssGenerator.DefaultStyle, m_propertyTable);
			Assert.That(styleDeclaration.ToString(), Contains.Substring("flex-line-height:12pt;"));
			Assert.That(styleDeclaration.ToString(), Contains.Substring("line-height:12pt;"));
		}

		[Test]
		public void GenerateCssForConfiguration_ConfigWithCharStyleWorks()
		{
			ConfiguredLcmGenerator.AssemblyFile = "xWorksTests";
			GenerateStyle("Dictionary-Headword");
			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SIL.FieldWorks.XWorks.TestRootClass",
				Label = "Headword",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "fr" }),
				Style = "Dictionary-Headword",
				IsEnabled = true
			};

			var model = new DictionaryConfigurationModel();
			model.Parts = new List<ConfigurableDictionaryNode> { headwordNode };
			//SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			VerifyFontInfoInCss(FontColor, FontBGColor, FontName, true, true, FontSize, cssResult);
		}

		[Test]
		public void GenerateCssForConfiguration_CharStyleWsOverrideWorks()
		{
			ConfiguredLcmGenerator.AssemblyFile = "xWorksTests";
			var style = GenerateStyle("WsStyleTest");
			var fontInfo = new FontInfo();
			fontInfo.m_italic.ExplicitValue = false; //override the italic value to false
			fontInfo.m_fontName.ExplicitValue = "french"; //override the fontname to 'french'
			style.SetWsStyle(fontInfo, Cache.DefaultVernWs);
			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SIL.FieldWorks.XWorks.TestRootClass",
				Label = "Headword",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "fr" }),
				Style = "WsStyleTest",
				IsEnabled = true
			};

			var model = new DictionaryConfigurationModel();
			model.Parts = new List<ConfigurableDictionaryNode> { headwordNode };
			//SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			//make sure that fontinfo with the french overrides is present
			VerifyFontInfoInCss(FontColor, FontBGColor, "french", true, false, FontSize, cssResult);
			//make sure that the default options are also present
			VerifyFontInfoInCss(FontColor, FontBGColor, FontName, true, true, FontSize, cssResult);
		}

		[Test]
		public void GenerateCssForConfiguration_ConfigWithParaStyleWorks()
		{
			ConfiguredLcmGenerator.AssemblyFile = "xWorksTests";
			GenerateParagraphStyle("Dictionary-Paragraph-Border");
			var minorEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SIL.FieldWorks.XWorks.TestRootClass",
				CSSClassNameOverride = "minor",
				Label = "Minor Entry",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "fr" }),
				Style = "Dictionary-Paragraph-Border",
				IsEnabled = true
			};

			var model = new DictionaryConfigurationModel();
			model.Parts = new List<ConfigurableDictionaryNode> { minorEntryNode };
			//SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			Assert.That(cssResult, Contains.Substring(".minor"));
			VerifyFontInfoInCss(FontColor, FontBGColor, FontName, true, true, FontSize, cssResult);
			//border leading omitted from paragraph style definition which should result in 0pt left width
			VerifyParagraphBorderInCss(BorderColor, 0, BorderTrailing, BorderBottom, BorderTop, cssResult);
		}

		[Ignore("Won't pass yet.")]
		[Test]
		public void GenerateCssForConfiguration_DefaultRootConfigGeneratesResult()
		{
			GenerateStyle("Dictionary-Headword");
			string defaultRoot =
				Path.Combine(Path.Combine(FwDirectoryFinder.DefaultConfigurations, "Dictionary"), "Root" + DictionaryConfigurationModel.FileExtension);
			var model = new DictionaryConfigurationModel(defaultRoot, Cache);
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			var parser = new Parser();
			var styleSheet = parser.Parse(cssResult);
			Debug.WriteLine(cssResult);
			Assert.AreEqual(0, styleSheet.Errors.Count);
		}

		[Test]
		public void GenerateCssForConfiguration_FwStyleInheritanceWorks()
		{
			var parentStyle = GenerateParagraphStyle("Parent");
			var childStyle = GenerateEmptyParagraphStyle("Child");
			childStyle.SetBasedOnStyle(parentStyle);
			childStyle.SetExplicitParaIntProp((int)FwTextPropType.ktptBorderColor, 0, (int)ColorUtil.ConvertColorToBGR(Color.HotPink));
			//SUT - Generate using default font info
			var cssResult = CssGenerator.GenerateCssStyleFromLcmStyleSheet("Child", CssGenerator.DefaultStyle, m_propertyTable);
			// The css should have the overridden border color, but report all other values as the parent style
			//border leading omitted from paragraph style definition which should result in 0pt left width
			VerifyParagraphBorderInCss(Color.HotPink, 0, BorderTrailing, BorderBottom, BorderTop, cssResult.ToString());
		}

		[Test]
		public void GenerateCssForStyleName_CharStyleUnsetValuesAreNotExported()
		{
			GenerateEmptyStyle("EmptyChar");
			var cssResult = CssGenerator.GenerateCssStyleFromLcmStyleSheet("EmptyChar", CssGenerator.DefaultStyle, m_propertyTable);
			Assert.AreEqual(cssResult.ToString().Trim(), String.Empty);
		}

		[Test]
		public void GenerateCssForStyleName_DefaultVernMagicConfigResultsInRealLanguageCss()
		{
			ConfiguredLcmGenerator.AssemblyFile = "xWorksTests";
			GenerateParagraphStyle("VernacularStyle");
			var testNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SIL.FieldWorks.XWorks.TestRootClass",
				CSSClassNameOverride = "vernholder",
				Label = "Vern Holder",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "vernacular" }),
				Style = "VernacularStyle",
				IsEnabled = true
			};
			var model = new DictionaryConfigurationModel
				{
					Parts = new List<ConfigurableDictionaryNode> { testNode }
				};
			//SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			//Verify that vernacular was converted into french to match the vernholder node
			Assert.That(cssResult, Contains.Substring(".vernholder> span[lang='fr']"));
		}

		[Test]
		public void GenerateCssForStyleName_DefaultAnalysisMagicConfigResultsInRealLanguageCss()
		{
			ConfiguredLcmGenerator.AssemblyFile = "xWorksTests";
			GenerateParagraphStyle("AnalysisStyle");
			var testNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SIL.FieldWorks.XWorks.TestRootClass",
				CSSClassNameOverride = "analyholder",
				Label = "Analy Holder",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "analysis" }),
				Style = "AnalysisStyle",
				IsEnabled = true
			};
			var model = new DictionaryConfigurationModel
			{
				Parts = new List<ConfigurableDictionaryNode> { testNode }
			};
			//SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			//Verify that analysis was converted into english to match the analyholder node
			Assert.That(cssResult, Contains.Substring(".analyholder> span[lang='en']"));
		}

		[Test]
		public void ClassMappingOverrides_ApplyAtRoot()
		{
			// Code that prevents empty output requires subnodes to generate anything.
			var subNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLHeadWord",
				Label = "Headword",
				IsEnabled = true,
				CSSClassNameOverride = "headword",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "vernacular" }),
				Children = new List<ConfigurableDictionaryNode>()
			};
			var testNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Label = "Bow, Bolo, Ect",
				IsEnabled = true,
				CSSClassNameOverride = "Bolo",
				Children = new List<ConfigurableDictionaryNode> { subNode }
			};
			subNode.Parent = testNode;
			var model = new DictionaryConfigurationModel
			{
				Parts = new List<ConfigurableDictionaryNode> { testNode }
			};
			var factory = Cache.ServiceLocator.GetInstance<ILexEntryFactory>();
			var entry = factory.Create();
			var wsFr = Cache.WritingSystemFactory.GetWsFromStr("fr");
			entry.CitationForm.set_String(wsFr, TsStringUtils.MakeString("homme", wsFr));
			//SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			Assert.That(cssResult, Does.Not.Contain(".lexentry"));
			Assert.That(cssResult, Contains.Substring(".bolo"));

			var xhtmResult = new StringBuilder();
			using (var XHTMLWriter = XmlWriter.Create(xhtmResult))
			{
				XHTMLWriter.WriteStartElement("body");
				var content = ConfiguredLcmGenerator.GenerateContentForEntry(entry, testNode, null, DefaultSettings).ToString();
				XHTMLWriter.WriteRaw(content);
				XHTMLWriter.WriteEndElement();
				XHTMLWriter.Flush();
				var result = xhtmResult.ToString();
				const string positiveTest = "//*[@class='bolo']";
				const string negativeTest = "//*[@class='lexentry']";
				AssertThatXmlIn.String(result).HasNoMatchForXpath(negativeTest);
				AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(positiveTest, 1);
			}
		}

		[Test]
		public void ClassMappingOverrides_ApplyToChildren()
		{
			var testChildNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "HeadWord",
				CSSClassNameOverride = "tailwind",
				Style = "FooStyle"
			};
			var testParentNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Label = "Bow, Bolo, Ect",
				Children = new List<ConfigurableDictionaryNode> { testChildNode }
			};
			var model = new DictionaryConfigurationModel
			{
				Parts = new List<ConfigurableDictionaryNode> { testParentNode }
			};
			PopulateFieldsForTesting(testParentNode);
			// Make a LexEntry with a headword so something is Generated
			var factory = Cache.ServiceLocator.GetInstance<ILexEntryFactory>();
			var entry = factory.Create();
			var wsFr = Cache.WritingSystemFactory.GetWsFromStr("fr");
			entry.CitationForm.set_String(wsFr, TsStringUtils.MakeString("HeadWordTest", wsFr));
			//SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			Assert.That(cssResult, Does.Not.Contain(".headword"));
			Assert.That(cssResult, Contains.Substring(".tailwind"));

			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entry, testParentNode, null, DefaultSettings).ToString();
			const string positiveTest = "//*[@class='tailwind']";
			const string negativeTest = "//*[@class='headword']";
			AssertThatXmlIn.String(result).HasNoMatchForXpath(negativeTest);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(positiveTest, 1);
		}

		[Test]
		public void CssAndXhtmlMatchOnSenseCollectionItems()
		{
			var glossNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Gloss",
				Style = "FooStyle",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "en" })
			};
			var testSensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Senses",
				Children = new List<ConfigurableDictionaryNode> { glossNode }
			};
			var testEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { testSensesNode }
			};
			var model = new DictionaryConfigurationModel
			{
				Parts = new List<ConfigurableDictionaryNode> { testEntryNode }
			};
			PopulateFieldsForTesting(testEntryNode);
			var factory = Cache.ServiceLocator.GetInstance<ILexEntryFactory>();
			var entry = factory.Create();
			var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			entry.SensesOS.Add(sense);
			var wsEn = Cache.WritingSystemFactory.GetWsFromStr("en");
			sense.Gloss.set_String(wsEn, TsStringUtils.MakeString("gloss", wsEn));
			//SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			Assert.That(cssResult, Contains.Substring(".gloss"));

			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entry, testEntryNode, null, DefaultSettings).ToString();
			const string positiveTest = "/*[@class='lexentry']/span[@class='senses']/span[@class='sense']/span[@class='gloss']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(positiveTest, 1);
		}

		[Test]
		public void GenerateCssForConfiguration_CharStyleSubscriptWorks()
		{
			ConfiguredLcmGenerator.AssemblyFile = "xWorksTests";
			var style = GenerateStyle("subscript");
			var fontInfo = new FontInfo();
			fontInfo.m_superSub.ExplicitValue = FwSuperscriptVal.kssvSub;
			style.SetWsStyle(fontInfo, Cache.DefaultVernWs);
			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SIL.FieldWorks.XWorks.TestRootClass",
				Label = "Headword",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "fr" }),
				Style = "subscript",
				IsEnabled = true
			};

			var model = new DictionaryConfigurationModel();
			model.Parts = new List<ConfigurableDictionaryNode> { headwordNode };
			//SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			//make sure that fontinfo with the subscript overrides made it into css
			VerifyExtraFontInfoInCss(0, FwSuperscriptVal.kssvSub, FwUnderlineType.kuntNone, Color.Black, cssResult);
			Assert.IsTrue(Regex.Match(cssResult, @".*\.sil*\.fieldworks.xworks.testrootclass>\s*span\[lang='fr'\]\{.*position\:relative;\s*top\:0.3em.*", RegexOptions.Singleline).Success,
				  "Subscript's position not generated properly");
		}

		[Test]
		public void GenerateCssForConfiguration_CharStyleSuperscriptWorks()
		{
			ConfiguredLcmGenerator.AssemblyFile = "xWorksTests";
			var style = GenerateStyle("superscript");
			var fontInfo = new FontInfo();
			fontInfo.m_superSub.ExplicitValue = FwSuperscriptVal.kssvSuper;
			style.SetWsStyle(fontInfo, Cache.DefaultVernWs);
			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SIL.FieldWorks.XWorks.TestRootClass",
				Label = "Headword",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "fr" }),
				Style = "superscript",
				IsEnabled = true
			};

			var model = new DictionaryConfigurationModel();
			model.Parts = new List<ConfigurableDictionaryNode> { headwordNode };
			//SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			//make sure that fontinfo with the superscript overrides made it into css
			VerifyExtraFontInfoInCss(0, FwSuperscriptVal.kssvSuper, FwUnderlineType.kuntNone, Color.Black, cssResult);
			Assert.IsTrue(Regex.Match(cssResult, @".*\.sil*\.fieldworks.xworks.testrootclass>\s*span\[lang='fr']\{.*position\:relative;\s*top\:-0.6em.*", RegexOptions.Singleline).Success,
				  "Superscript's position not generated properly");
		}

		[Test]
		public void GenerateCssForConfiguration_CharStyleBasicUnderlineWorks()
		{
			ConfiguredLcmGenerator.AssemblyFile = "xWorksTests";
			var style = GenerateStyle("underline");
			var fontInfo = new FontInfo();
			fontInfo.m_underline.ExplicitValue = FwUnderlineType.kuntSingle;
			fontInfo.m_underlineColor.ExplicitValue = Color.HotPink;
			style.SetWsStyle(fontInfo, Cache.DefaultVernWs);
			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SIL.FieldWorks.XWorks.TestRootClass",
				Label = "Headword",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "fr" }),
				Style = "underline",
				IsEnabled = true
			};

			var model = new DictionaryConfigurationModel();
			model.Parts = new List<ConfigurableDictionaryNode> { headwordNode };
			//SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			//make sure that fontinfo with the underline overrides made it into css
			VerifyExtraFontInfoInCss(0, FwSuperscriptVal.kssvOff, FwUnderlineType.kuntSingle, Color.HotPink, cssResult);
		}

		[Test]
		public void GenerateCssForConfiguration_CharStyleDoubleUnderlineWorks()
		{
			ConfiguredLcmGenerator.AssemblyFile = "xWorksTests";
			var style = GenerateStyle("doubleline");
			var fontInfo = new FontInfo();
			fontInfo.m_underline.ExplicitValue = FwUnderlineType.kuntDouble;
			fontInfo.m_underlineColor.ExplicitValue = Color.Khaki;
			style.SetWsStyle(fontInfo, Cache.DefaultVernWs);
			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SIL.FieldWorks.XWorks.TestRootClass",
				Label = "Headword",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "fr" }),
				Style = "doubleline",
				IsEnabled = true
			};

			var model = new DictionaryConfigurationModel();
			model.Parts = new List<ConfigurableDictionaryNode> { headwordNode };
			//SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			//make sure that fontinfo with the underline overrides made it into css
			VerifyExtraFontInfoInCss(0, FwSuperscriptVal.kssvOff, FwUnderlineType.kuntDouble, Color.Khaki, cssResult);
		}

		[Test]
		public void GenerateCssForConfiguration_CharStyleDashedUnderlineWorks()
		{
			ConfiguredLcmGenerator.AssemblyFile = "xWorksTests";
			var style = GenerateStyle("dashed");
			var fontInfo = new FontInfo();
			fontInfo.m_underline.ExplicitValue = FwUnderlineType.kuntDashed;
			fontInfo.m_underlineColor.ExplicitValue = Color.Black;
			style.SetWsStyle(fontInfo, Cache.DefaultVernWs);
			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SIL.FieldWorks.XWorks.TestRootClass",
				Label = "Headword",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "fr" }),
				Style = "dashed",
				IsEnabled = true
			};

			var model = new DictionaryConfigurationModel();
			model.Parts = new List<ConfigurableDictionaryNode> { headwordNode };
			//SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			//make sure that fontinfo with the underline overrides made it into css
			VerifyExtraFontInfoInCss(0, FwSuperscriptVal.kssvOff, FwUnderlineType.kuntDashed, Color.Black, cssResult);
		}

		[Test]
		public void GenerateCssForConfiguration_CharStyleStrikethroughWorks()
		{
			ConfiguredLcmGenerator.AssemblyFile = "xWorksTests";
			var style = GenerateStyle("strike");
			var fontInfo = new FontInfo();
			fontInfo.m_underline.ExplicitValue = FwUnderlineType.kuntStrikethrough;
			fontInfo.m_underlineColor.ExplicitValue = Color.Black;
			style.SetWsStyle(fontInfo, Cache.DefaultVernWs);
			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SIL.FieldWorks.XWorks.TestRootClass",
				Label = "Headword",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "fr" }),
				Style = "strike",
				IsEnabled = true
			};

			var model = new DictionaryConfigurationModel();
			model.Parts = new List<ConfigurableDictionaryNode> { headwordNode };
			//SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			//make sure that fontinfo with the underline overrides made it into css
			VerifyExtraFontInfoInCss(0, FwSuperscriptVal.kssvOff, FwUnderlineType.kuntStrikethrough, Color.Black, cssResult);
		}

		[Test]
		public void GenerateCssForConfiguration_CharStyleDottedUnderlineWorks()
		{
			ConfiguredLcmGenerator.AssemblyFile = "xWorksTests";
			var style = GenerateStyle("dotted");
			var fontInfo = new FontInfo();
			fontInfo.m_underline.ExplicitValue = FwUnderlineType.kuntDotted;
			fontInfo.m_underlineColor.ExplicitValue = Color.Black;
			style.SetWsStyle(fontInfo, Cache.DefaultVernWs);
			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SIL.FieldWorks.XWorks.TestRootClass",
				Label = "Headword",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "fr" }),
				Style = "dotted",
				IsEnabled = true
			};

			var model = new DictionaryConfigurationModel();
			model.Parts = new List<ConfigurableDictionaryNode> { headwordNode };
			//SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			//make sure that fontinfo with the underline overrides made it into css
			VerifyExtraFontInfoInCss(0, FwSuperscriptVal.kssvOff, FwUnderlineType.kuntDotted, Color.Black, cssResult);
		}

		[Test]
		public void GenerateCssForConfiguration_CharStyleDisableSuperWorks()
		{
			ConfiguredLcmGenerator.AssemblyFile = "xWorksTests";
			var style = GenerateStyle("notsosuper");
			var fontInfo = new FontInfo();
			fontInfo.m_superSub.ExplicitValue = FwSuperscriptVal.kssvOff;
			style.SetWsStyle(fontInfo, Cache.DefaultVernWs);
			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SIL.FieldWorks.XWorks.TestRootClass",
				Label = "Headword",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "fr" }),
				Style = "notsosuper",
				IsEnabled = true
			};

			var model = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { headwordNode } };
			//SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			//make sure that fontinfo with the superscript overrides made it into css
			VerifyExtraFontInfoInCss(0, FwSuperscriptVal.kssvOff, FwUnderlineType.kuntNone, Color.Black, cssResult);
		}

		[Test]
		public void GenerateCssForConfiguration_GramInfoFieldsWork()
		{
			var pos = new ConfigurableDictionaryNode {
				FieldDescription = "MLPartOfSpeech", Style = "FooStyle"
			};
			var inflectionClass = new ConfigurableDictionaryNode {
				FieldDescription = "MLInflectionClass", Style = "FooStyle"
			};
			var slots = new ConfigurableDictionaryNode
			{
				FieldDescription = "Slots",
				Children =
					new List<ConfigurableDictionaryNode> {
						new ConfigurableDictionaryNode { FieldDescription = "Name", Style = "FooStyle" }
					}
			};
			var gramInfo = new ConfigurableDictionaryNode
			{
				FieldDescription = "MorphoSyntaxAnalysisRA",
				Label = "Gram. Info.",
				Children = new List<ConfigurableDictionaryNode> { pos, inflectionClass, slots }
			};
			var senses = new ConfigurableDictionaryNode
			{
				FieldDescription = "Senses",
				Label = "Senses",
				Children = new List<ConfigurableDictionaryNode> { gramInfo }
			};
			var entry = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Label = "Main Entry",
				Children = new List<ConfigurableDictionaryNode> { senses }
			};

			var model = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { entry } };
			PopulateFieldsForTesting(entry);
			//SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			Assert.That(cssResult, Contains.Substring(".mlpartofspeech"));
			Assert.That(cssResult, Contains.Substring(".mlinflectionclass"));
			Assert.That(cssResult, Contains.Substring(".name"));
		}

		[Test]
		public void GenerateCssForConfiguration_VariantPronunciationFormWorks()
		{
			var pronunciationForm = new ConfigurableDictionaryNode { FieldDescription = "Form", Style = "FooStyle" };
			var pronunciations = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwningEntry",
				SubField = "PronunciationsOS",
				Label = "Variant Pronunciations",
				CSSClassNameOverride = "Pronunciations",
				Children = new List<ConfigurableDictionaryNode> { pronunciationForm }
			};
			var variantForms = new ConfigurableDictionaryNode
			{
				FieldDescription = "VariantFormEntryBackRefs",
				Children = new List<ConfigurableDictionaryNode> { pronunciations }
			};
			var entry = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { variantForms }
			};

			var model = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { entry } };
			PopulateFieldsForTesting(entry);
			//SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			VerifyRegex(cssResult, @"\s*\.form", "No form style generated");
		}

		[Test]
		public void GenerateCssForConfiguration_SubentryTypeWorks()
		{
			var revAbbrevNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ReverseAbbr",
				Style = "FooStyle",
				DictionaryNodeOptions = new DictionaryNodeWritingSystemOptions()
			};
			var refTypeNode = new ConfigurableDictionaryNode
			{
				FieldDescription = ConfiguredLcmGenerator.LookupComplexEntryType,
				CSSClassNameOverride = "complexformtypes",
				Children = new List<ConfigurableDictionaryNode> { revAbbrevNode }
			};
			var subentryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { refTypeNode },
				DictionaryNodeOptions = new DictionaryNodeListAndParaOptions(),
				FieldDescription = "Subentries"
			};
			var entry = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { subentryNode },
				FieldDescription = "LexEntry"
			};
			var model = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { entry } };
			PopulateFieldsForTesting(entry);
			//SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			VerifyRegex(cssResult, @"\s*\.reverseabbr> span", "No reverse abbreviation span style generated");
		}

		[Test]
		public void GenerateCssForConfiguration_GeneratesComplexFormTypesBeforeBetweenAfter()
		{
			var complexFormTypeNameNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Name",
				Before = "<",
				Between = ",",
				After = ">"
			};
			var complexFormTypeNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ComplexEntryTypesRS",
				CSSClassNameOverride = "complexformtypes",
				Children = new List<ConfigurableDictionaryNode> { complexFormTypeNameNode },
			};
			var complexFormNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "VisibleComplexFormBackRefs",
				Children = new List<ConfigurableDictionaryNode> { complexFormTypeNode }
			};
			var mainHeadwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "HeadWord",
				CSSClassNameOverride = "entry"
			};
			var entry = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { mainHeadwordNode, complexFormNode },
				FieldDescription = "LexEntry"
			};

			var model = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { entry } };
			PopulateFieldsForTesting(entry);
			//SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			VerifyRegex(cssResult, @".name:before{\s*content:'<';\s*}",
				"Before not generated:");
			VerifyRegex(cssResult, @".name> .nam \+ .nam:before{\s*content:',';\s*}",
				"Between not generated:");
			VerifyRegex(cssResult, @".name:after{\s*content:'>';\s*}",
				"After not generated:");
		}

		/// <summary>Verify that Complex Forms are not factored when displayed in paragraphs</summary>
		[Test]
		public void GenerateCssForConfiguration_GeneratesComplexFormTypesBeforeBetweenAfterInParagraphs()
		{
			var complexFormTypeNameNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Name",
				Before = "<",
				Between = ",",
				After = ">"
			};
			var complexFormTypeNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ComplexEntryTypesRS",
				CSSClassNameOverride = "complexformtypes",
				Children = new List<ConfigurableDictionaryNode> { complexFormTypeNameNode },
			};
			var complexFormNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "VisibleComplexFormBackRefs",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetFullyEnabledListOptions(Cache, DictionaryNodeListOptions.ListIds.Complex),
				Children = new List<ConfigurableDictionaryNode> { complexFormTypeNode }
			};
			((IParaOption)complexFormNode.DictionaryNodeOptions).DisplayEachInAParagraph = true; // displaying in a paragraph should suppress factoring
			var mainHeadwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "HeadWord",
				CSSClassNameOverride = "entry"
			};
			var entry = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { mainHeadwordNode, complexFormNode },
				FieldDescription = "LexEntry"
			};

			var model = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { entry } };
			PopulateFieldsForTesting(entry);
			//SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			VerifyRegex(cssResult, @"^\s*\.name:before{\s*content:'<';\s*}", "Before not generated:");
			VerifyRegex(cssResult, @"^\s*.name> .nam \+ .nam:before{\s*content:',';\s*}",
				"Between not generated:");
			VerifyRegex(cssResult, @"^\s*\.name:after{\s*content:'>';\s*}", "After not generated:");
		}

		[Test]
		public void GenerateCssForConfiguration_GeneratesVariantTypesBeforeBetweenAfter()
		{
			var variantFormTypeNameNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Name",
				Before = "<",
				Between = ",",
				After = ">"
			};
			var variantFormTypeNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "VariantEntryTypesRS",
				CSSClassNameOverride = "variantentrytypes",
				Children = new List<ConfigurableDictionaryNode> { variantFormTypeNameNode },
			};
			var variantNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "VariantFormEntryBackRefs",
				Before = "[",
				Between = "; ",
				After = "]",
				Children = new List<ConfigurableDictionaryNode> { variantFormTypeNode }
			};
			var mainHeadwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "HeadWord",
				CSSClassNameOverride = "entry"
			};
			var entry = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { mainHeadwordNode, variantNode },
				FieldDescription = "LexEntry"
			};

			var model = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { entry } };
			PopulateFieldsForTesting(entry);
			//SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			VerifyRegex(cssResult, @".variantformentrybackrefs:before{.*content:'\[';.*}",
				"Before not generated for Variant Entry.");
			VerifyRegex(cssResult, @".variantformentrybackrefs>\s+.variantformentrybackref\s*\+\s*.variantformentrybackref:before{.*content:'\; ';.*}",
				"Between not generated Variant Entry.");
			VerifyRegex(cssResult, @".variantformentrybackrefs:after{.*content:'\]';.*}", "After not generated Variant Entry.");
			// Review: Was this assert correct before? VerifyRegex(cssResult, @".variantformentrybackrefs> .span \+ .span:before");
			VerifyRegex(cssResult, @"^\.name:before{.*content:'<';.*}", "Before not generated Variant Entry Type.");
			VerifyRegex(cssResult, @"^\.name:after{.*content:'>';.*}", "After not generated Variant Entry Type.");
			VerifyRegex(cssResult, @"^\.name> .nam \+ .nam:before{.*content:',';.*}", "Between not generated Variant Entry Type.");
	  }

		[Test]
		public void GenerateCssForConfiguration_GeneratesVariantNameSuffixBeforeBetweenAfter()
		{
			var variantFormTypeNameNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Name",
				Before = "<",
				Between = ",",
				After = ">"
			};
			var variantFormTypeNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "VariantEntryTypesRS",
				CSSClassNameOverride = "variantentrytypes",
				Children = new List<ConfigurableDictionaryNode> { variantFormTypeNameNode },
			};
			var variantNode = new ConfigurableDictionaryNode
			{
				Label = "Variant Forms",
				LabelSuffix = "Inflectional Variants",
				FieldDescription = "VariantFormEntryBackRefs",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetFullyEnabledListOptions(Cache, DictionaryNodeListOptions.ListIds.Variant),
				IsDuplicate = true,
				Before = "[",
				Between = "; ",
				After = "]",
				Children = new List<ConfigurableDictionaryNode> { variantFormTypeNode }
			};
			var mainHeadwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "HeadWord",
				CSSClassNameOverride = "entry"
			};
			var entry = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { mainHeadwordNode, variantNode },
				FieldDescription = "LexEntry"
			};

			var model = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { entry } };
			PopulateFieldsForTesting(entry);
			//SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			VerifyRegex(cssResult, @".variantformentrybackrefs_inflectional-variants:before{.*content:'\[';.*}",
				"Before not generated for Variant Entry.");
			VerifyRegex(cssResult, @"^\.variantformentrybackrefs_inflectional-variants>\s+\.variantformentrybackref_inflectional-variants\s*\+\s*\.variantformentrybackref_inflectional-variants:before{.*content:'\; ';.*}",
				"Between should have been generated using class selectors because this element has type factoring.");
			Assert.False(Regex.Match(cssResult, @".lexentry>? .variantformentrybackrefs_inflectional-variants>? span\+ span:before").Success,
				"Between should not have been generated using generic spans because this element has type factoring." + Environment.NewLine + cssResult);
			VerifyRegex(cssResult, @".variantformentrybackrefs_inflectional-variants:after{.*content:'\]';.*}",
				"After not generated Variant Entry.");
			VerifyRegex(cssResult, @"^\s*\.name:before{.*content:'<';.*}",
				"Before not generated Variant Entry Type:");
			VerifyRegex(cssResult, @"^\s*\.name> .nam \+ .nam:before{.*content:',';.*}",
				"Between not generated Variant Entry Type:");
			VerifyRegex(cssResult, @"^\s*\.name:after{.*content:'>';.*}",
				"After not generated Variant Entry Type:");
		}

		[Test]
		public void GenerateCssForConfiguration_SenseComplexFormsNotSubEntriesHeadWord()
		{
			var form = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwningEntry",
				SubField = "HeadWord",
				CSSClassNameOverride = "HeadWord",
				Style = "FooStyle"
			};
			var complexformsnotsubentries = new ConfigurableDictionaryNode
			{
				FieldDescription = "ComplexFormsNotSubentries",
				CSSClassNameOverride = "otherreferencedcomplexforms",
				Children = new List<ConfigurableDictionaryNode> { form }
			};
			var senses = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "Senses",
				Children = new List<ConfigurableDictionaryNode> { complexformsnotsubentries }
			};
			var headwordMain = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLHeadword",
				CSSClassNameOverride = "HeadWord",
				After = " "
			};
			var entry = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				CSSClassNameOverride = "lexentry",
				Children = new List<ConfigurableDictionaryNode> { senses, headwordMain }
			};

			var model = new DictionaryConfigurationModel();
			model.Parts = new List<ConfigurableDictionaryNode> { entry };
			PopulateFieldsForTesting(entry);
			var cssGenerator = new CssGenerator();
			cssGenerator.Init(m_propertyTable);
			//SUT
			cssGenerator.AddGlobalStyles(model, m_propertyTable);
			cssGenerator.AddStyles(headwordMain);
			cssGenerator.AddStyles(form);
			var cssResult = cssGenerator.GetStylesString();
			VerifyRegex(cssResult, @"^\s*\.headword-otherreferencedcomplexforms", "Headword node not generated for non subentry headword");
		}

		[Test]
		public void GenerateCssForConfiguration_ComplexFormsEachInOwnParagraph()
		{
			var form = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwningEntry",
				SubField = "HeadWord",
				CSSClassNameOverride = "HeadWord",
				Style = "FooStyle"
			};
			var complexForms = new ConfigurableDictionaryNode
			{
				FieldDescription = "ComplexFormsNotSubentries",
				CSSClassNameOverride = "complexforms",
				DictionaryNodeOptions = new DictionaryNodeListAndParaOptions { DisplayEachInAParagraph = true },
				Children = new List<ConfigurableDictionaryNode> { form }
			};
			var entry = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { complexForms }
			};

			var model = new DictionaryConfigurationModel();
			model.Parts = new List<ConfigurableDictionaryNode> { entry };
			PopulateFieldsForTesting(entry);
			//SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			Assert.That(cssResult, Contains.Substring(".headword")); // Make sure that the headword style was generated
			Assert.IsTrue(Regex.Match(cssResult, @"\.complexforms\s*\.complexform{.*display\s*:\s*block;.*}", RegexOptions.Singleline).Success);
		}

		[Test]
		public void GenerateCssForConfiguration_SenseShowGramInfoFirstWorks()
		{
			GenerateStyle("Dictionary-Contrasting");
			var pos = new ConfigurableDictionaryNode { FieldDescription = "MLPartOfSpeech" };
			var inflectionClass = new ConfigurableDictionaryNode { FieldDescription = "MLInflectionClass" };
			var gramInfo = new ConfigurableDictionaryNode
			{
				FieldDescription = "MorphoSyntaxAnalysisRA",
				Label = "Gram. Info.",
				Children = new List<ConfigurableDictionaryNode> { pos, inflectionClass },
				Style = "Dictionary-Contrasting"
			};
			var gloss = new ConfigurableDictionaryNode { FieldDescription = "Gloss", Style = "FooStyle" };
			var senses = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "Senses",
				DictionaryNodeOptions = new DictionaryNodeSenseOptions { ShowSharedGrammarInfoFirst = true },
				Children = new List<ConfigurableDictionaryNode> { gramInfo, gloss }
			};
			var entry = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				CSSClassNameOverride = "lexentry",
				Children = new List<ConfigurableDictionaryNode> { senses }
			};

			var model = new DictionaryConfigurationModel();
			model.Parts = new List<ConfigurableDictionaryNode> { entry };
			PopulateFieldsForTesting(entry);
			var cssGenerator = new CssGenerator();
			cssGenerator.Init(m_propertyTable);
			cssGenerator.AddGlobalStyles(model, m_propertyTable);
			//SUT
			cssGenerator.AddStyles(senses);
			cssGenerator.AddStyles(gramInfo);
			var cssResult = cssGenerator.GetStylesString();
			VerifyRegex(cssResult, @"^\s*\.morphosyntaxanalysisra", "Style for morphosyntaxanalysisra not generated");
			VerifyRegex(cssResult, @"^\s*\.morphosyntaxanalysisra\s*{.*font-family\s*:\s*'foofoo'\,serif.*}",
				"Style for morphosyntaxanalysisra not placed correctly");
		}

		[Test]
		public void GenerateCssForConfiguration_GramInfoAfterText()
		{
			GenerateStyle("Dictionary-Contrasting");
			var pos = new ConfigurableDictionaryNode { FieldDescription = "MLPartOfSpeech" };
			var inflectionClass = new ConfigurableDictionaryNode { FieldDescription = "MLInflectionClass" };
			var afterText = "ExactlyOnce";
			var gramInfo = new ConfigurableDictionaryNode
			{
				FieldDescription = "MorphoSyntaxAnalysisRA",
				Label = "Gram. Info.",
				Children = new List<ConfigurableDictionaryNode> { pos, inflectionClass },
				Style = "Dictionary-Contrasting",
				After = afterText
			};
			var gloss = new ConfigurableDictionaryNode { FieldDescription = "Gloss", Style = "FooStyle" };
			var senses = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "Senses",
				DictionaryNodeOptions = new DictionaryNodeSenseOptions { ShowSharedGrammarInfoFirst = true },
				Children = new List<ConfigurableDictionaryNode> { gramInfo, gloss }
			};
			var entry = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				CSSClassNameOverride = "lexentry",
				Children = new List<ConfigurableDictionaryNode> { senses }
			};

			var model = new DictionaryConfigurationModel();
			model.Parts = new List<ConfigurableDictionaryNode> { entry };
			PopulateFieldsForTesting(entry);
			var cssGenerator = new CssGenerator();
			cssGenerator.Init(m_propertyTable);
			cssGenerator.AddGlobalStyles(model, m_propertyTable);

			//SUT
			cssGenerator.AddStyles(senses);
			cssGenerator.AddStyles(gramInfo);
			var cssResult = cssGenerator.GetStylesString();

			// Check that the after text is included once, not more or less.
			var firstIndex = cssResult.IndexOf(afterText);
			var lastIndex = cssResult.LastIndexOf(afterText);
			Assert.IsTrue(firstIndex != -1 && firstIndex == lastIndex,
				string.Format("After text \'{0}\' was not included exactly one time.", afterText));
		}

		[Test]
		public void GenerateCssForConfiguration_GramInfoFirstHasNoBetweenMaterialWorks()
		{
			GenerateStyle("Dictionary-Contrasting");
			var pos = new ConfigurableDictionaryNode { FieldDescription = "MLPartOfSpeech" };
			var inflectionClass = new ConfigurableDictionaryNode { FieldDescription = "MLInflectionClass" };
			var gramInfo = new ConfigurableDictionaryNode
			{
				FieldDescription = "MorphoSyntaxAnalysisRA",
				Label = "Gram. Info.",
				Children = new List<ConfigurableDictionaryNode> { pos, inflectionClass },
				Style = "Dictionary-Contrasting"
			};
			var gloss = new ConfigurableDictionaryNode { FieldDescription = "Gloss", Style = "FooStyle" };
			var senses = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "Senses",
				DictionaryNodeOptions = new DictionaryNodeSenseOptions { ShowSharedGrammarInfoFirst = true },
				Children = new List<ConfigurableDictionaryNode> { gramInfo, gloss },
				Between = "*"
			};
			var entry = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				CSSClassNameOverride = "entry",
				Children = new List<ConfigurableDictionaryNode> { senses }
			};

			var model = new DictionaryConfigurationModel();
			model.Parts = new List<ConfigurableDictionaryNode> { entry };
			PopulateFieldsForTesting(entry);
			//SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			VerifyRegex(cssResult, @"^\s*\.senses>\s*span\.sensecontent \+\s*span\:before\{\s*content\:\'\*\'\;", "Between Material for Senses not placed correctly");
		}

		[Test]
		public void GenerateCssForConfiguration_WritingSystemAudioWorks()
		{
			CoreWritingSystemDefinition wsEnAudio;
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("en-Zxxx-x-audio", out wsEnAudio);
			Cache.ServiceLocator.WritingSystems.AddToCurrentVernacularWritingSystems(wsEnAudio);
			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexemeFormOA",
				Label = "Lexeme Form",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "en-Zxxx-x-audio" }),
				Style = "FooStyle"
			};
			var entry = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { headwordNode },
				FieldDescription = "LexEntry"
			};
			var model = new DictionaryConfigurationModel();
			model.Parts = new List<ConfigurableDictionaryNode> { entry };
			PopulateFieldsForTesting(entry);
			// SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			// Not using regex to avoid figuring out all the escapes necessary
			Assert.That(cssResult, Contains.Substring(".lexemeformoa> span[lang='en-Zxxx-x-audio']{"));
			VerifyRegex(cssResult, @"a.en-Zxxx-x-audio{.*text-decoration:none;.*}", "Audio not generated.");
		}

		[Test]
		public void GenerateCssForConfiguration_SenseDisplayInParaWorks()
		{
			var gloss = new ConfigurableDictionaryNode { FieldDescription = "Gloss", Style = "FooStyle" };
			var senses = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "Senses",
				DictionaryNodeOptions = new DictionaryNodeSenseOptions { DisplayEachSenseInAParagraph = true },
				Children = new List<ConfigurableDictionaryNode> { gloss }
			};
			var entry = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				CSSClassNameOverride = "lexentry",
				Children = new List<ConfigurableDictionaryNode> { senses }
			};

			var model = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { entry } };
			PopulateFieldsForTesting(model);
			//SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			VerifyRegex(cssResult, @"^\.gloss", "gloss missing");
			VerifyRegex(cssResult, @"^\s*\.senses\s*>\s*\.sensecontent\s*{\s*display\s*:\s*block;.*}");
		}

		[Test]
		public void GenerateCssForConfiguration_ExampleDisplayInParaWorks()
		{
			var examples = new ConfigurableDictionaryNode
			{
				FieldDescription = "ExamplesOS",
				CSSClassNameOverride = "examples",
				DictionaryNodeOptions = new DictionaryNodeListAndParaOptions { DisplayEachInAParagraph = true }
			};
			var senses = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "Senses",
				DictionaryNodeOptions = new DictionaryNodeSenseOptions { DisplayEachSenseInAParagraph = true },
				Children = new List<ConfigurableDictionaryNode> { examples }
			};
			var entry = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				CSSClassNameOverride = "lexentry",
				Children = new List<ConfigurableDictionaryNode> { senses }
			};

			var model = new DictionaryConfigurationModel();
			model.Parts = new List<ConfigurableDictionaryNode> { entry };
			PopulateFieldsForTesting(entry);
			//SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			Assert.IsTrue(Regex.Match(cssResult, @"\.example\s*{.*display\s*:\s*block;.*}", RegexOptions.Singleline).Success);
		}

		[Test]
		public void GenerateCssForConfiguration_ExampleUncheckedDisplayInParaWorks()
		{
			var examples = new ConfigurableDictionaryNode
			{
				FieldDescription = "ExamplesOS",
				CSSClassNameOverride = "examples",
				DictionaryNodeOptions = new DictionaryNodeListAndParaOptions { DisplayEachInAParagraph = false }
			};
			var senses = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "Senses",
				DictionaryNodeOptions = new DictionaryNodeSenseOptions { DisplayEachSenseInAParagraph = true },
				Children = new List<ConfigurableDictionaryNode> { examples }
			};
			var entry = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				CSSClassNameOverride = "lexentry",
				Children = new List<ConfigurableDictionaryNode> { senses }
			};

			var model = new DictionaryConfigurationModel();
			model.Parts = new List<ConfigurableDictionaryNode> { entry };
			PopulateFieldsForTesting(entry);
			//SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			Assert.IsFalse(Regex.Match(cssResult, @"\.example\s*{.*display\s*:\s*block;.*}", RegexOptions.Singleline).Success);
		}

		[Test]
		public void GenerateCssForConfiguration_SenseParaStyleNotAppliedToInLineFirstSense()
		{
			GenerateStyle("Sense-List");
			var gloss = new ConfigurableDictionaryNode { FieldDescription = "Gloss", Style = "FooStyle" };
			var senses = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "Senses",
				DictionaryNodeOptions = new DictionaryNodeSenseOptions { DisplayEachSenseInAParagraph = true, DisplayFirstSenseInline = true },
				Children = new List<ConfigurableDictionaryNode> { gloss },
				Style = "Sense-List"
			};
			var entry = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { senses }
			};

			var model = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { entry } };
			PopulateFieldsForTesting(model);
			//SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			VerifyRegex(cssResult, @"^\s*\.senses\s*>\s*\.sensecontent\s*\+\s*\.sensecontent\s*{.*display\s*:\s*block;.*}", "First sense inline style not generated");
			VerifyRegex(cssResult, @"^\s*\.senses\s*>\s*\.sensecontent\s*>\s*\.sense\s*{.*font-style\s*:\s*italic;.*}", "Style for each sense not generated");
		}

		[Test]
		public void GenerateCssForConfiguration_SenseParaStyleAppliedToFirstSense()
		{
			GenerateStyle("Sense-List");
			var gloss = new ConfigurableDictionaryNode { FieldDescription = "Gloss", Style = "FooStyle" };
			var senses = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "Senses",
				DictionaryNodeOptions = new DictionaryNodeSenseOptions { DisplayEachSenseInAParagraph = true, DisplayFirstSenseInline = false },
				Children = new List<ConfigurableDictionaryNode> { gloss },
				Style = "Sense-List"
			};
			var entry = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { senses }
			};

			var model = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { entry } };
			PopulateFieldsForTesting(model);
			//SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			VerifyRegex(cssResult, @"\s*\.senses\s*>\s*\.sensecontent\s*{.*display\s*:\s*block;.*}", "Block display not applied to all senses");
			VerifyRegex(cssResult, @"\s*\.senses\s*>\s*\.sensecontent\s*>\s*\.sense\s*{.*font-style\s*:\s*italic;.*}", "Font style missing");
		}

		[Test]
		public void GenerateCssForConfiguration_SenseNumberCharStyleWorks()
		{
			GenerateStyle("Dictionary-SenseNum");

			var senses = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "Senses",
				DictionaryNodeOptions = new DictionaryNodeSenseOptions { NumberStyle = "Dictionary-SenseNum" }
			};
			var entry = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { senses }
			};

			var model = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { entry } };
			PopulateFieldsForTesting(entry);
			// SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			VerifyRegex(cssResult, @"\s*\.senses\s*>\s*\.sensecontent\s*\.sensenumber", "sense number style selector was not generated.");
			VerifyFontInfoInCss(FontColor, FontBGColor, FontName, FontBold, FontItalic, FontSize, cssResult);
		}

		[Test]
		public void GenerateCssForConfiguration_CharStyleFontFeaturesWorks()
		{
			ConfiguredLcmGenerator.AssemblyFile = "xWorksTests";
			var style = GenerateStyle("underline");
			var fontInfo = new FontInfo { m_features = { ExplicitValue = "smcps=1,Eng=2" } };
			style.SetWsStyle(fontInfo, Cache.DefaultVernWs);
			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SIL.FieldWorks.XWorks.TestRootClass",
				Label = "Headword",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "fr" }),
				Style = "underline",
				IsEnabled = true
			};

			var model = new DictionaryConfigurationModel();
			model.Parts = new List<ConfigurableDictionaryNode> { headwordNode };
			//SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			//make sure that fontinfo with the underline overrides made it into css
			VerifyFontInfoInCss(FontColor, FontBGColor, FontName, FontBold, FontItalic, FontSize, cssResult, "\"smcps\" 1,\"Eng\" 2");
		}

		[Test]
		public void GenerateCssForConfiguration_ReversalSenseNumberWorks()
		{
			GenerateStyle("Dictionary-RevSenseNum");
			var gloss = new ConfigurableDictionaryNode { FieldDescription = "Gloss", Style = "FooStyle" };
			var senses = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesRS",
				CSSClassNameOverride = "refdsenses",
				DictionaryNodeOptions = new DictionaryNodeSenseOptions
				{
					DisplayEachSenseInAParagraph = true, NumberStyle = "Dictionary-RevSenseNum"
				},
				Children = new List<ConfigurableDictionaryNode> { gloss }
			};
			var entry = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { senses },
				FieldDescription = "ReversalIndexEntry"
			};

			var model = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { entry } };
			PopulateFieldsForTesting(entry);
			// SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			VerifyRegex(cssResult, @"^.gloss\s*{\s*font-family", "Gloss with style was not generated from reversal sense");
			VerifyRegex(cssResult, @"^\.refdsenses\s*>\s*\.sensecontent\s*\.sensenumber\s*{.*font-style\s*:\s*italic;.*}", "Sense Number missing");
		}

		[Test]
		public void GenerateCssForConfiguration_SenseNumberBeforeAndAfterWork()
		{
			var senses = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "Senses",
				DictionaryNodeOptions = new DictionaryNodeSenseOptions { BeforeNumber = "[", AfterNumber = "]" }
			};
			var entry = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { senses }
			};

			var model = new DictionaryConfigurationModel();
			model.Parts = new List<ConfigurableDictionaryNode> { entry };
			PopulateFieldsForTesting(entry);
			// SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			VerifyRegex(cssResult, @"\s*\.senses\s*>\s*\.sensecontent\s*\.sensenumber:before{.*content:'\['.*}", "Before content not applied to the sense number selector.");
			VerifyRegex(cssResult, @"\s*\.senses\s*>\s*\.sensecontent\s*\.sensenumber:after{.*content:'\]'.*}", "After content not applied to the sense number selector.");
		}

		[Test]
		public void GenerateCssForConfiguration_PrimaryEntryReferencesTypeContextWorks()
		{
			const string lang2 = "ru";
			var reverseName = new ConfigurableDictionaryNode
			{
				FieldDescription = "ReverseName",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "en", lang2 }),
				Before = "beef",
				Between = "viet",
				After = "aft"
			};
			var entrytypes = new ConfigurableDictionaryNode
			{
				FieldDescription = "EntryTypes",
				Children = new List<ConfigurableDictionaryNode> { reverseName },
				Before = "b4",
				Between = "twixt",
				After = "farther back"
			};
			var headword = new ConfigurableDictionaryNode
			{
				FieldDescription = "TestHeadword", Between = "bh", After = "ah"
			};
			var primaryentry = new ConfigurableDictionaryNode
			{
				FieldDescription = "EntryRefsWithThisMainSense",
				Before = "[",
				After = "]",
				Children = new List<ConfigurableDictionaryNode> { entrytypes, headword }
			};
			var senses = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesRS",
				Children = new List<ConfigurableDictionaryNode> { primaryentry }
			};
			var entry = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { senses },
				FieldDescription = "ReversalIndexEntry"
			};
			var model = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { entry } };
			PopulateFieldsForTesting(entry);
			// SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			VerifyRegex(cssResult, @"\s*\.entrytypes:before{\s*content:'b4';\s*}"); // TODO? (Hasso) 2016.10: put on .types .type first-child
			VerifyRegex(cssResult, @"\s*\.entryrefswiththismainsens\s*\+\s*.entrytypes:before\s*{\s*content:'twixt';\s*}",
				"Until everything else is restructured under the yet-to-be-added Targets node, Factoring Type.Between goes between typed factions");
			VerifyRegex(cssResult, @"\s*\.entrytypes:after{\s*content:'farther back';\s*}");
			VerifyRegex(cssResult, @"^\.testheadword:after{\s*content:'ah';\s*}",
				"Headword's selector should *not* have changed due to factoring");
			VerifyRegex(cssResult, @"\s*\.reversename>\s*span:first-child:before{\s*content:'beef';\s*}");
			VerifyRegex(cssResult, @"\s*\.reversename>\s*span\+span\[lang='" + lang2 + @"'\]:before{\s*content:'viet';\s*}");
			VerifyRegex(cssResult, @"\s*\.reversename>\s*span:last-child:after{\s*content:'aft';\s*}");
		}

		[Test]
		public void GenerateCssForConfiguration_BetweenWorks()
		{
			var senses = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "Senses",
				Between = ","
			};
			var entry = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { senses }
			};
			var model = new DictionaryConfigurationModel();
			model.Parts = new List<ConfigurableDictionaryNode> { entry };
			PopulateFieldsForTesting(entry);
			// SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			VerifyRegex(cssResult, @".*\.senses>\s*\.sense\s*\+\s*\.sense:before{.*content:','.*}", "Between selector not generated.");
		}

		[Test]
		public void GenerateCssForConfiguration_BetweenSingleWsWithAbbrSpanWorks()
		{
			var headword = new ConfigurableDictionaryNode
			{
				FieldDescription = "Headword",
				CSSClassNameOverride = "Lexemeform",
				Between = ",",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguageswithDisplayWsAbbrev(new[] { "en" })
			};
			var entry = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { headword }
			};
			var model = new DictionaryConfigurationModel();
			model.Parts = new List<ConfigurableDictionaryNode> { entry };
			PopulateFieldsForTesting(entry);
			// SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			VerifyRegex(cssResult, @".*\.lexemeform>\s*span\.writingsystemprefix\s*\~\s*span\.writingsystemprefix:before{.*content:','.*}",
				"Between span selector not generated.");
		}

		[Test]
		public void GenerateCssForConfiguration_BetweenMultiWsWithoutAbbrSpanWorks()
		{
			var wsOpts = new DictionaryNodeWritingSystemOptions
			{
				Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>
				{
					new DictionaryNodeListOptions.DictionaryNodeOption { Id = "en" },
					new DictionaryNodeListOptions.DictionaryNodeOption { Id = "fr" }
				},
				DisplayWritingSystemAbbreviations = false
			};
			var headword = new ConfigurableDictionaryNode
			{
				FieldDescription = "Headword",
				CSSClassNameOverride = "Lexemeform",
				Between = ",",
				DictionaryNodeOptions = wsOpts
			};
			var entry = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { headword }
			};
			var model = new DictionaryConfigurationModel();
			model.Parts = new List<ConfigurableDictionaryNode> { entry };
			PopulateFieldsForTesting(entry);
			// SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			VerifyRegex(cssResult, @".*\.lexemeform>\s*span\+span\[lang\='fr'\]:before{.*content:','.*}",
							  "Between Multi-WritingSystem without Abbr selector not generated.");
		}

		[Test]
		public void GenerateCssForConfiguration_BetweenMultiWsWithAbbrSpanWorks()
		{
			var wsOpts = new DictionaryNodeWritingSystemOptions
			{
				Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>
				{
					new DictionaryNodeListOptions.DictionaryNodeOption { Id = "en" },
					new DictionaryNodeListOptions.DictionaryNodeOption { Id = "fr" }
				},
				DisplayWritingSystemAbbreviations = true
			};
			var headword = new ConfigurableDictionaryNode
			{
				FieldDescription = "HeadWord",
				CSSClassNameOverride = "headword",
				Between = ",",
				DictionaryNodeOptions = wsOpts
			};
			var lexemeForm = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexemeFormOA",
				CSSClassNameOverride = "lexemeform",
				Between = ",",
				DictionaryNodeOptions = wsOpts
			};
			var entry = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { headword, lexemeForm }
			};
			var model = new DictionaryConfigurationModel();
			model.Parts = new List<ConfigurableDictionaryNode> { entry };
			PopulateFieldsForTesting(entry);
			// SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			Assert.IsTrue(Regex.Match(cssResult, @".*\.lexemeform>\s*span\.writingsystemprefix\s*\~\s*span\.writingsystemprefix:before\s*{.*content:','.*}", RegexOptions.Singleline).Success,
							  "Between Multi-WritingSystem with Abbr selector not generated for LexemeForm.");
			Assert.IsTrue(Regex.Match(cssResult, @".*\.headword>\s*span\.writingsystemprefix\s*\~\s*span\.writingsystemprefix:before\s*{.*content:','.*}", RegexOptions.Singleline).Success,
							  "Between Multi-WritingSystem with Abbr selector not generated for HeadWord.");
			Assert.IsTrue(Regex.Match(cssResult, @".*\.lexemeform>\s*span\.writingsystemprefix:after\s*{.*content:' '.*}", RegexOptions.Singleline).Success,
							  "writingsystemprefix:after not generated for headword.");
			Assert.IsTrue(Regex.Match(cssResult, @".*\.headword>\s*span\.writingsystemprefix:after\s*{.*content:' '.*}", RegexOptions.Singleline).Success,
							  "writingsystemprefix:after not generated for lexemeform.");
		}

		[Test]
		public void GenerateCssForConfiguration_BetweenMultiWsWithAbbrSpan_NotEnabled_DeselectsRules()
		{
			var wsOpts = new DictionaryNodeWritingSystemOptions
			{
				Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>
				{
					new DictionaryNodeListOptions.DictionaryNodeOption { Id = "en" },
					new DictionaryNodeListOptions.DictionaryNodeOption { Id = "fr" }
				},
				DisplayWritingSystemAbbreviations = true
			};
			var headword = new ConfigurableDictionaryNode
			{
				FieldDescription = "HeadWord",
				CSSClassNameOverride = "headword",
				Between = ",",
				DictionaryNodeOptions = wsOpts
			};
			var lexemeForm = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexemeFormOA",
				CSSClassNameOverride = "lexemeform",
				Between = ",",
				DictionaryNodeOptions = wsOpts
			};
			var entry = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { headword, lexemeForm }
			};
			var model = new DictionaryConfigurationModel();
			model.Parts = new List<ConfigurableDictionaryNode> { entry };
			PopulateFieldsForTesting(entry);
			wsOpts.Options[1].IsEnabled = false; // uncheck French ws
			// SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			Assert.IsFalse(Regex.Match(cssResult, @".*\.lexemeform>\s*span\.writingsystemprefix\s*\+\s*span:not\(:last-child\):after\s*{.*content:','.*}", RegexOptions.Singleline).Success,
							  "Between Multi-WritingSystem selector should not be generated for LexemeForm (only 1 ws checked).");
			Assert.IsFalse(Regex.Match(cssResult, @".*\.headword>\s*span\.writingsystemprefix\s*\+\s*span:not\(:last-child\):after\s*{.*content:','.*}", RegexOptions.Singleline).Success,
							  "Between Multi-WritingSystem selector should not be generated for HeadWord (only 1 ws checked).");
			Assert.IsTrue(Regex.Match(cssResult, @".*\.lexemeform>\s*span\.writingsystemprefix:after\s*{.*content:' '.*}", RegexOptions.Singleline).Success,
							  "writingsystemprefix:after not generated for headword.");
			Assert.IsTrue(Regex.Match(cssResult, @".*\.headword>\s*span\.writingsystemprefix:after\s*{.*content:' '.*}", RegexOptions.Singleline).Success,
							  "writingsystemprefix:after not generated for lexemeform.");
		}

		[Test]
		public void GenerateCssForConfiguration_BetweenWorksWithFormatCss()
		{
			var senses = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "Senses",
				Between = ","
			};
			var entry = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { senses }
			};
			using (new TempContextStyle(this, CssGenerator.BeforeAfterBetweenStyleName))
			{
				var model = new DictionaryConfigurationModel();
				model.Parts = new List<ConfigurableDictionaryNode> { entry };
				PopulateFieldsForTesting(entry);
				// SUT
				var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
				VerifyRegex(cssResult, @".*\.senses>\s*\.sense\s*\+\s*\.sense:before{.*content:',';.*font-size:10pt;.*color:#00F.*}",
					"Between selector with format not generated.");
			}
		}

		/// <summary>
		/// When there is no css override an underscore should be used to separate FieldDescription and SubField
		/// </summary>
		[Test]
		public void ClassAttributeForConfig_SubFieldWithNoOverrideGivesCorrectClass()
		{
			var form = new ConfigurableDictionaryNode { FieldDescription = "OwningEntry", SubField = "HeadWord" };

			//SUT
			var classAttribute = CssGenerator.GetClassAttributeForConfig(form);
			Assert.That(classAttribute, Does.Match("owningentry_headword"));
		}

		/// <summary>
		/// When there is a css override the fielddescription should not appear in the css class name
		/// </summary>
		[Test]
		public void ClassAttributeForConfig_SubFieldWithOverrideGivesCorrectClass()
		{
			var form = new ConfigurableDictionaryNode { FieldDescription = "OwningEntry", SubField = "Display", CSSClassNameOverride = "HeadWord" };

			//SUT
			var classAttribute = CssGenerator.GetClassAttributeForConfig(form);
			// Should be headword and should definitely not have owningentry present.
			Assert.That(classAttribute, Does.Match("headword"));
		}

		/// <summary>
		/// css class names are traditionally all lower case. This tests that we enforce that.
		/// </summary>
		[Test]
		public void ClassAttributeForConfig_ClassNameIsToLowered()
		{
			var entry = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
			};

			//SUT
			var classAttribute = CssGenerator.GetClassAttributeForConfig(entry);
			Assert.That(classAttribute, Does.Match("lexentry"));
		}

		/// <summary>
		/// Duplicate nodes should not conflict with the original
		/// Test that we append label suffix with no CSSClassNameOverride
		/// </summary>
		[Test]
		public void ClassAttributeForConfig_DuplicateNodeClassUsesLabelSuffix()
		{
			var entry = new ConfigurableDictionaryNode
			{
				FieldDescription = "originalfield",
				LabelSuffix = "dup",
				IsDuplicate = true
			};

			//SUT
			var classAttribute = CssGenerator.GetClassAttributeForConfig(entry);
			Assert.That(classAttribute, Does.Match("originalfield_dup"));
		}

		/// <summary>
		/// Duplicate nodes should not conflict with the original
		/// Test that we append label suffix when CSSClassNameOverride is used.
		/// </summary>
		[Test]
		public void ClassAttributeForConfig_DuplicateNodeOverrideUsesLabelSuffix()
		{
			var entry = new ConfigurableDictionaryNode
			{
				FieldDescription = "originalfield",
				CSSClassNameOverride = "override",
				LabelSuffix = "dup",
				IsDuplicate = true
			};

			//SUT
			var classAttribute = CssGenerator.GetClassAttributeForConfig(entry);
			Assert.That(classAttribute, Does.Match("override_dup"));
		}

		/// <summary>
		/// The css for a picture is floated right and we want to clear the float at each entry.
		/// </summary>
		[Test]
		public void GenerateCssForConfiguration_PictureCssIsGenerated()
		{
			TestStyle style = GenerateStyle("Normal");
			style.SetExplicitParaIntProp((int)FwTextPropType.ktptLeadingIndent, 0, LeadingIndent);
			ConfiguredLcmGenerator.AssemblyFile = "xWorksTests";
			var pictureFileNode = new ConfigurableDictionaryNode { FieldDescription = "PictureFileRA" };
			var senseNumberNode = new ConfigurableDictionaryNode { FieldDescription = "SenseNumberTSS"};
			var captionNode = new ConfigurableDictionaryNode { FieldDescription = "Caption", Style = "Normal" };
			var memberNode = new ConfigurableDictionaryNode
			{
				DictionaryNodeOptions = new DictionaryNodePictureOptions { MaximumWidth = 1 },
				CSSClassNameOverride = "pictures",
				FieldDescription = "Pictures",
				Children = new List<ConfigurableDictionaryNode> { pictureFileNode, senseNumberNode, captionNode }
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Senses",
			};
			var rootNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SIL.FieldWorks.XWorks.TestPictureClass",
				CSSClassNameOverride = "entry",
				Children = new List<ConfigurableDictionaryNode> { sensesNode, memberNode }
			};
			PopulateFieldsForTesting(rootNode);

			var config = new DictionaryConfigurationModel()
				{
					Parts = new List<ConfigurableDictionaryNode> { rootNode }
				};

			// SUT
			var cssWithPictureRules = CssGenerator.GenerateCssFromConfiguration(config, m_propertyTable);
			VerifyRegex(cssWithPictureRules, @"^\s*\.picture.*{.*float:right.*}", "picture not floated right");
			VerifyRegex(cssWithPictureRules, @"^\s*\.picture.*img.*{.*max-width:1in;.*}", "css for image did not contain height contraint attribute");
			VerifyRegex(cssWithPictureRules, @"^\s*\.pictures.*picture.*{.*margin:\s*0pt\s*0pt\s*4pt\s*4pt.*;.*}", "css for image did not contain valid margin attribute");
			VerifyRegex(cssWithPictureRules, @"^\s*\.entry\s*{.*clear:both.*}", "float not cleared at entry");
			VerifyRegex(cssWithPictureRules, @"^\s*\s*.captionContent\s*.caption*\{.*margin-left:\s*24pt", "css for caption did not contain valid margin attribute");
		}

		/// <summary>
		/// The css for a picture sub fields  Before Between After Css is Generated.
		/// </summary>
		[Test]
		public void GenerateCssForConfiguration_PictureSubfieldsBeforeBetweenAfterIsAreGenerated()
		{
			TestStyle style = GenerateStyle("Normal");
			style.SetExplicitParaIntProp((int)FwTextPropType.ktptLeadingIndent, 0, LeadingIndent);
			ConfiguredLcmGenerator.AssemblyFile = "xWorksTests";
			var pictureFileNode = new ConfigurableDictionaryNode { FieldDescription = "PictureFileRA" };
			var senseNumberNode = new ConfigurableDictionaryNode
			{
				Before = "[",
				After = "]",
				Between = ", ",
				FieldDescription = "SenseNumberTSS"
			};
			var captionNode = new ConfigurableDictionaryNode
			{
				Before = "{",
				After = "}",
				Between = " ",
				FieldDescription = "Caption",
				Style = "Normal"
			};
			var memberNode = new ConfigurableDictionaryNode
			{
				DictionaryNodeOptions = new DictionaryNodePictureOptions { MaximumWidth = 1 },
				CSSClassNameOverride = "pictures",
				FieldDescription = "Pictures",
				Children = new List<ConfigurableDictionaryNode> { pictureFileNode, senseNumberNode, captionNode }
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Senses",
			};
			var rootNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SIL.FieldWorks.XWorks.TestPictureClass",
				CSSClassNameOverride = "entry",
				Children = new List<ConfigurableDictionaryNode> { sensesNode, memberNode }
			};
			PopulateFieldsForTesting(rootNode);

			var config = new DictionaryConfigurationModel()
			{
				Parts = new List<ConfigurableDictionaryNode> { rootNode }
			};

			// SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(config, m_propertyTable);

			var senseNumberBefore = @"^.captionContent .sensenumbertss:before\{\s*content:'\[';";
			VerifyRegex(cssResult, senseNumberBefore, "expected Sense Number before rule is generated");

			var senseNumberAfter = @"^.captionContent .sensenumbertss:after\{\s*content:'\]';";
			VerifyRegex(cssResult, senseNumberAfter, "expected Sense Number after rule is generated");

			var senseNumberBetween = @"^.captionContent .sensenumbertss>\s*\.sensenumberts \+\s*\.sensenumberts:before\{\s*content:', ';";
			VerifyRegex(cssResult, senseNumberBetween, "expected Sense Number between rule is generated");

			var captionBefore = @"^.captionContent .caption:before\{\s*content:'\{';";
			VerifyRegex(cssResult, captionBefore, "expected Caption before rule is generated");

			var captionAfter = @"^.captionContent .caption:after\{\s*content:'\}';";
			VerifyRegex(cssResult, captionAfter, "expected Caption after rule is generated");

			var captionBetween = @"^.captionContent .caption>\s*\.captio \+\s*\.captio:before\{\s*content:' ';";
			VerifyRegex(cssResult, captionBetween, "expected Caption between rule is generated");
		}

		/// <summary>
		/// The css for a picture Before Between After Css is Generated.
		/// </summary>
		[Test]
		public void GenerateCssForConfiguration_PictureBeforeBetweenAfterIsAreGenerated()
		{
			TestStyle style = GenerateStyle("Normal");
			style.SetExplicitParaIntProp((int)FwTextPropType.ktptLeadingIndent, 0, LeadingIndent);
			ConfiguredLcmGenerator.AssemblyFile = "xWorksTests";

			var memberNode = new ConfigurableDictionaryNode
			{
				DictionaryNodeOptions = new DictionaryNodePictureOptions { MaximumWidth = 1 },
				CSSClassNameOverride = "pictures",
				FieldDescription = "PicturesOfSenses",
				Before = "[",
				After = "]",
				Between = ", "
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Senses",
			};
			var rootNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SIL.FieldWorks.XWorks.TestPictureClass",
				CSSClassNameOverride = "entry",
				Children = new List<ConfigurableDictionaryNode> { sensesNode, memberNode }
			};
			PopulateFieldsForTesting(rootNode);

			var config = new DictionaryConfigurationModel()
			{
				Parts = new List<ConfigurableDictionaryNode> { rootNode }
			};

			// SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(config, m_propertyTable);

			var pictureBefore = @".pictures> div:first-child:before\{\s*content:'\[';";
			VerifyRegex(cssResult, pictureBefore, "expected Picture before rule is generated");

			var pictureAfter = @".pictures> div:last-child:after\{\s*content:'\]';";
			VerifyRegex(cssResult, pictureAfter, "expected Picture after rule is generated");

			var pictureBetween = @".*\.pictures>\s*div\s*\+\s*div:before\{\s*content:', ';";
			VerifyRegex(cssResult, pictureBetween, "expected Picture between rule is generated");

			// Verify that the before/after/between picture content is not nested in 'captionContent'.
			RegexOptions options = RegexOptions.Singleline | RegexOptions.Multiline;
			var captionContentPictureBefore = @".captionContent .pictures> div:first-child:before\{\s*content:'\[';";
			string message = "did not expect Picture before rule to be nested in captionContent.";
			Assert.IsFalse(Regex.Match(cssResult, captionContentPictureBefore, options).Success,
				string.Format("{3}Expected{0}{1}{0}but got{0}{2}", Environment.NewLine, pictureBefore, cssResult, message + Environment.NewLine));

			var captionContentPictureAfter = @".captionContent .pictures> div:last-child:after\{\s*content:'\]';";
			message = "did not expect Picture after rule to be nested in captionContent.";
			Assert.IsFalse(Regex.Match(cssResult, captionContentPictureAfter, options).Success,
				string.Format("{3}Expected{0}{1}{0}but got{0}{2}", Environment.NewLine, pictureAfter, cssResult, message + Environment.NewLine));

			var captionContentPictureBetween = @".captionContent .*\.pictures>\s*div\s*\+\s*div:before\{\s*content:', ';";
			VerifyRegex(cssResult, pictureBetween, "expected Picture between rule is generated");
			message = "did not expect Picture between rule to be nested in captionContent.";
			Assert.IsFalse(Regex.Match(cssResult, captionContentPictureBetween, options).Success,
				string.Format("{3}Expected{0}{1}{0}but got{0}{2}", Environment.NewLine, pictureBetween, cssResult, message + Environment.NewLine));
		}


		/// <summary>
		/// Part of LT-12572.
		/// </summary>
		[Test]
		public void GenerateCssForConfiguration_PictureWritesRulesForHeadwordAndGlossInCaptionArea()
		{
			TestStyle style = GenerateStyle("Normal");
			style.SetExplicitParaIntProp((int)FwTextPropType.ktptLeadingIndent, 0, LeadingIndent);
			ConfiguredLcmGenerator.AssemblyFile = "xWorksTests";

			var captionNode = new ConfigurableDictionaryNode { FieldDescription = "Caption", Style = "Normal" };
			var headwordNode = new ConfigurableDictionaryNode {
				FieldDescription = "Owner",
				SubField="OwnerOutlineName",
				Style = "Normal",
				CSSClassNameOverride="headword"
			};
			var glossNode = new ConfigurableDictionaryNode {

				FieldDescription = "Owner",
				SubField="Gloss",
				Style = "Normal",
			};

			var memberNode = new ConfigurableDictionaryNode
				{
					DictionaryNodeOptions = new DictionaryNodePictureOptions { MaximumWidth = 1 },
					CSSClassNameOverride = "pictures",
					FieldDescription = "Pictures",
					Children = new List<ConfigurableDictionaryNode> { captionNode, headwordNode, glossNode }
				};
			var rootNode = new ConfigurableDictionaryNode
				{
					FieldDescription = "SIL.FieldWorks.XWorks.TestPictureClass",
					CSSClassNameOverride = "entry",
					Children = new List<ConfigurableDictionaryNode> { memberNode }
				};
			PopulateFieldsForTesting(rootNode);

			var config = new DictionaryConfigurationModel()
				{
					Parts = new List<ConfigurableDictionaryNode> { rootNode }
				};

			// SUT
			var cssWithPictureRules = CssGenerator.GenerateCssFromConfiguration(config, m_propertyTable);

			VerifyRegex(cssWithPictureRules, @"^\.captionContent .caption", "css for image did not contain expected rule");
			VerifyRegex(cssWithPictureRules, @"^\.captionContent .headword", "css for image did not contain expected headword rule");
			VerifyRegex(cssWithPictureRules, @"^\.captionContent .owner_gloss", "css for image did not contain expected gloss rule");
		}

		[Test]
		public void GenerateCssForConfiguration_GlossWithMultipleWs()
		{
			GenerateStyle("Writing System Abbreviation");
			var glossNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Gloss",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguageswithDisplayWsAbbrev(new[] { "en", "fr" })
			};
			var testSensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Senses",
				Children = new List<ConfigurableDictionaryNode> { glossNode }
			};
			var testEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { testSensesNode }
			};
			var model = new DictionaryConfigurationModel
			{
				Parts = new List<ConfigurableDictionaryNode> { testEntryNode }
			};
			PopulateFieldsForTesting(testEntryNode);
			var factory = Cache.ServiceLocator.GetInstance<ILexEntryFactory>();
			var entry = factory.Create();
			var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			entry.SensesOS.Add(sense);
			var wsEn = Cache.WritingSystemFactory.GetWsFromStr("en");
			sense.Gloss.set_String(wsEn, TsStringUtils.MakeString("gloss", wsEn));
			//SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			Assert.That(Regex.Replace(cssResult, @"\t|\n|\r", ""), Contains.Substring(".gloss> span.writingsystemprefix" +
				"{font-family:\'foofoo\',serif;font-size:10pt;font-weight:bold;font-style:italic;color:#00F;"));
			Assert.That(Regex.Replace(cssResult, @"\t|\n|\r", ""), Contains.Substring(".gloss> span.writingsystemprefix:after{content:' ';}"));
		}

		[Test]
		public void GenerateCssForConfiguration_WsSpanWithNormalStyle()
		{
			var style = GenerateEmptyStyle("Normal");
			// Mimic more closely what can happen in the program.
			style.IsParagraphStyle = true;
			style.SetExplicitParaIntProp((int)FwTextPropType.ktptLeadingIndent, 0, LeadingIndent);
			style.SetExplicitParaIntProp((int)FwTextPropType.ktptTrailingIndent, 0, TrailingIndent);
			style.SetExplicitParaIntProp((int)FwTextPropType.ktptSpaceBefore, 0, PadTop);
			style.SetExplicitParaIntProp((int)FwTextPropType.ktptSpaceAfter, 0, PadBottom);
			var engFontInfo = new FontInfo {m_fontName = {ExplicitValue = "english"}, m_fontColor = {ExplicitValue = Color.Red}};
			style.SetWsStyle(engFontInfo, Cache.WritingSystemFactory.GetWsFromStr("en"));
			var frFontInfo = new FontInfo {m_fontName = {ExplicitValue = "french"}, m_fontColor = {ExplicitValue = Color.Green}};
			style.SetWsStyle(frFontInfo, Cache.WritingSystemFactory.GetWsFromStr("fr"));
			var glossNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Gloss",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguageswithDisplayWsAbbrev(new[] { "en", "fr" })
			};
			var testSensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Senses",
				Children = new List<ConfigurableDictionaryNode> { glossNode }
			};
			var testEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { testSensesNode }
			};
			var model = new DictionaryConfigurationModel
			{
				Parts = new List<ConfigurableDictionaryNode> { testEntryNode }
			};
			PopulateFieldsForTesting(testEntryNode);
			// Default (no ws) style info
			const string defaultStyle = "body{font-size:10pt;}";
			const string englishStyle = "span[lang='en']{font-family:'english',serif;color:#F00;}";
			const string frenchStyle = "span[lang='fr']{font-family:'french',serif;color:#008000;}";
			//SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			Assert.That(Regex.Replace(cssResult, @"\t|\n|\r", ""), Contains.Substring(defaultStyle + englishStyle + frenchStyle));
		}

		[Test]
		public void GenerateCssForConfiguration_NormalStyleForWsDoesNotOverrideNodeStyle()
		{
			// Set up Normal style to default to Green, except for English which is Red
			var normalStyle = GenerateEmptyStyle("Normal");
			normalStyle.IsParagraphStyle = true;
			var engFontInfo = new FontInfo { m_fontName = { ExplicitValue = "english" }, m_fontColor = { ExplicitValue = Color.Red } };
			normalStyle.SetWsStyle(engFontInfo, Cache.WritingSystemFactory.GetWsFromStr("en"));
			var defFontInfo = new FontInfo { m_fontColor = { ExplicitValue = Color.Green } };
			normalStyle.SetDefaultFontInfo(defFontInfo);

			// Set up Dictionary-Contrasting to be Yellow
			var nodeStyle = GenerateEmptyStyle("Dictionary-Contrasting");
			nodeStyle.IsParagraphStyle = true;
			defFontInfo = new FontInfo { m_fontColor = { ExplicitValue = Color.Yellow } };
			nodeStyle.SetDefaultFontInfo(defFontInfo);

			// Normally the Definition node would have ws options, but we need to test
			// the case of a node that doesn't have ws options
			var definitionNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Definition",
				Style = "Dictionary-Contrasting"
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				Children = new List<ConfigurableDictionaryNode> { definitionNode }
			};
			var entryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { sensesNode }
			};
			var model = new DictionaryConfigurationModel
			{
				Parts = new List<ConfigurableDictionaryNode> { entryNode }
			};
			PopulateFieldsForTesting(entryNode);
			// Default (no ws) style info
			const string englishGeneralStyle = "span[lang='en']{font-family:'english',serif;color:#F00;}";
			const string definitionSelector = ".definition span[lang='en']{color:#FF0;}";
			//SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			// Using substring instead of regex to avoid spending all the time figuring out which regex characters to escape in this css
			Assert.That(Regex.Replace(cssResult, @"\t|\n|\r", ""), Contains.Substring(englishGeneralStyle));
			Assert.That(Regex.Replace(cssResult, @"\t|\n|\r", ""), Contains.Substring(definitionSelector));
	  }

		[Test]
		public void GenerateCssForConfiguration_GenerateMainEntryParagraphStyle()
		{
			GenerateNormalStyle("Dictionary-Normal");
			var rtlStyle = GenerateEmptyParagraphStyle("Dictionary-RTL");
			rtlStyle.SetExplicitParaIntProp((int)FwTextPropType.ktptRightToLeft, 0,
				(int)TriStateBool.triTrue);
			var testSensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Senses"
			};
			var testEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { testSensesNode },
				CSSClassNameOverride = "entry"
			};
			var model = new DictionaryConfigurationModel
			{
				Parts = new List<ConfigurableDictionaryNode> { testEntryNode }
			};
			PopulateFieldsForTesting(testEntryNode);
			//SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			Assert.IsTrue(
				Regex.Match(cssResult,
					@"div.entry{\s*margin-left:24pt;\s*padding-right:48pt;\s*}",
					RegexOptions.Singleline).Success,
				"Dictionary-Normal Paragraph Style not generated when main entry has no style selected.");
			model.Parts[0].Style = "Dictionary-RTL";
			cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			Assert.IsTrue(
				Regex.Match(cssResult, @"div.entry{\s*direction:rtl;\s*}",
					RegexOptions.Singleline).Success,
				"Main Entry style was not used as the main page style");
		}

		[Test]
		public void GenerateCssForConfiguration_GenerateDictionaryMinorParagraphStyle()
		{
			GenerateNormalStyle("Dictionary-Minor");
			var majorStyle = GenerateEmptyParagraphStyle("Dictionary-Major");
			var optionsStyle = GenerateEmptyParagraphStyle("Dictionary-Options-Minor");
			majorStyle.SetExplicitParaIntProp((int)FwTextPropType.ktptTrailingIndent, 0, 32000);
			optionsStyle.SetExplicitParaIntProp((int)FwTextPropType.ktptTrailingIndent, 0, 16000);
			var testSensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Senses"
			};
			var minorEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				CSSClassNameOverride = "minorentry",
				Children = new List<ConfigurableDictionaryNode> { testSensesNode }
			};
			var extraEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				CSSClassNameOverride = "specialminorentry",
				Style = majorStyle.Name,
				Children = new List<ConfigurableDictionaryNode> { testSensesNode }
			};
			var specialMinor = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				CSSClassNameOverride = "optionsminorentry",
				Style = optionsStyle.Name,
				Children = new List<ConfigurableDictionaryNode> { testSensesNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { testSensesNode }
			};
			var model = new DictionaryConfigurationModel
			{
				Parts = new List<ConfigurableDictionaryNode> { mainEntryNode, minorEntryNode, extraEntryNode, specialMinor }
			};
			model.Parts.ForEach(PopulateFieldsForTesting);
			//SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			VerifyRegex(cssResult, @"div.minorentry{\s*margin-left:24pt;\s*padding-right:48pt;\s*}",
							  "Dictionary-Minor Paragraph Style not generated.");
			VerifyRegex(cssResult, @"div.specialminorentry{\s*padding-right:32pt;\s*}",
							  "Dictionary-Minor Paragraph Style for node with style attribute not generated.");
			VerifyRegex(cssResult, @"div.optionsminorentry{\s*padding-right:16pt;\s*}",
							  "Dictionary-Minor Paragraph Style for node with paragraph options not generated.");
		}

		[Test]
		public void GenerateCssForConfiguration_DictionaryMinorUnusedDoesNotOverride()
		{
			var minorStyle = GenerateEmptyParagraphStyle("Dictionary-Minor");
			var secStyle = GenerateEmptyParagraphStyle("Dictionary-Secondary");
			var vernWs = Cache.ServiceLocator.WritingSystemManager.GetStrFromWs(Cache.DefaultVernWs);

			var minorEntryNode = new ConfigurableDictionaryNode
			{
				Label = "Minor Entry (Variants)",
				FieldDescription = "LexEntry",
				CSSClassNameOverride = "minorentryvariant",
				Style = "Dictionary-Secondary",
				StyleType = ConfigurableDictionaryNode.StyleTypes.Paragraph
			};
			// mainEntry node is just a placeholder
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry"
			};
			var model = new DictionaryConfigurationModel
			{
				Parts = new List<ConfigurableDictionaryNode> { mainEntryNode, minorEntryNode }
			};
			PopulateFieldsForTesting(model);
			SetStyleFontColor(minorStyle, Color.Blue); // set Dictionary-Minor to Blue
			SetStyleFontColor(secStyle, Color.Green); // set Dictionary-Secondary to Green

			//SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			StringAssert.DoesNotContain("color:#00F;", cssResult, "Dictionary-Minor Paragraph Style should not be generated.");
			// The problem we are testing for occurred in the section of CssGenerator labeled:
			// "Then generate the rules for all the writing system overrides"
			// So I chose to check specifically for one of the default writing systems; DefaultAnalWs would have worked too.
			var vernStyle = "span[lang='" + vernWs + "']{color:#008000;}";
			Assert.That(Regex.Replace(cssResult, @"\t|\n|\r", ""), Contains.Substring(@"div.minorentryvariant " + vernStyle),
				"Dictionary-Secondary Paragraph Style should be generated.");
		}

		[Test]
		public void GenerateCssForBulletStyleForSenses()
		{
			GenerateBulletStyle("Bulleted List");
			var senses = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "Senses",
				DictionaryNodeOptions = new DictionaryNodeSenseOptions
				{
					NumberStyle = "Dictionary-SenseNum", DisplayEachSenseInAParagraph = true
				},
				Style = "Bulleted List"
			};
			var entry = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				CSSClassNameOverride = "lexentry",
				Children = new List<ConfigurableDictionaryNode> { senses }
			};
			var model = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { entry } };
			PopulateFieldsForTesting(model);
			// SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			const string regExPected = @".senses\s>\s.sensecontent:before.*{.*content:'\\25A0';.*font-size:14pt;.*color:Green;.*font-family:Arial;.*font-weight:bold;.*font-style:italic;.*background-color:Brown;.*}";
			VerifyRegex(cssResult, regExPected, "Bulleted style not generated.");
		}

		[Test]
		public void GenerateCssForBulletStyleForSensesWithDisplayFirstSenseInline()
		{
			GenerateBulletStyle("Bulleted List");
			var senses = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "Senses",
				DictionaryNodeOptions = new DictionaryNodeSenseOptions
				{
					NumberStyle = "Dictionary-SenseNum",
					DisplayEachSenseInAParagraph = true,
					DisplayFirstSenseInline = true
				},
				Style = "Bulleted List"
			};
			var entry = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				CSSClassNameOverride = "lexentry",
				Children = new List<ConfigurableDictionaryNode> { senses }
			};
			var model = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { entry } };
			PopulateFieldsForTesting(model);
			// SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			const string regExPected = @"^\.senses\s+>\s*\.sensecontent:not\(:first-child\):before.*{.*content:'\\25A0';.*font-size:14pt;.*color:Green;.*font-family:Arial;.*font-weight:bold;.*font-style:italic;.*background-color:Brown;.*}";
			VerifyRegex(cssResult, regExPected, "Bulleted style not generated.");
		}

		[Test]
		public void GenerateCssForNumberingStyleForSenses()
		{
			GenerateNumberingStyle("Numbered List", VwBulNum.kvbnArabic);
			var senses = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "sensesos",
				DictionaryNodeOptions = new DictionaryNodeSenseOptions
				{
					NumberStyle = "Dictionary-SenseNum",
					DisplayEachSenseInAParagraph = true
				},
				Style = "Numbered List"
			};
			var entry = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				CSSClassNameOverride = "lexentry",
				Children = new List<ConfigurableDictionaryNode> { senses }
			};
			var model = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { entry } };
			PopulateFieldsForTesting(model);
			// SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			const string sensesCounterReset = @"\s*.sensesos\s*{\s*counter-reset:\ssensesos;.*}";
			const string sensesCounterInc = @".*\s.sensesos\s>\s.sensecontent:before{.*counter-increment:\ssensesos;.*content:\scounter.sensesos,\sdecimal.\s'\s';.*font-size:14pt;.*color:Green;.*}";
			VerifyRegex(cssResult, sensesCounterReset, "Numbering style counter reset not generated for Senses.");
			VerifyRegex(cssResult, sensesCounterInc, "Numbering style counter-increment not generated for Senses.");
	  }

		[Test]
		public void GenerateCssForNumberingStyleForSubentries()
		{
			GenerateNumberingStyle("Numbered List", VwBulNum.kvbnRomanUpper);
			var dictNodeOptions = new DictionaryNodeListAndParaOptions
			{
				DisplayEachInAParagraph = true,
				Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>()
			};
			dictNodeOptions.Options.Add(new DictionaryNodeListOptions.DictionaryNodeOption { Id = "a0000000-dd15-4a03-9032-b40faaa9a754" });
			dictNodeOptions.Options.Add(new DictionaryNodeListOptions.DictionaryNodeOption { Id = "1f6ae209-141a-40db-983c-bee93af0ca3c" });
			var subentriesConfig = new ConfigurableDictionaryNode
			{
				FieldDescription = "Subentries",
				DictionaryNodeOptions = dictNodeOptions,
				Style = "Numbered List"
			};
			var entryConfig = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { subentriesConfig }
			};
			var model = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { entryConfig } };
			PopulateFieldsForTesting(entryConfig);
			// SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			const string regexExpected = @"\s*\.subentries{.*counter-reset:[\s]subentries;.*}";
			const string counterIncrement = @"\s*\.subentries\s.subentry:before{.*counter-increment:[\s]subentries;.*content:\scounter.subentries,\supper-roman.\s'\s';.*font-size:14pt;.*color:Green;.*}";
			VerifyRegex(cssResult, regexExpected, "counter-reset style not generated for Subentry.");
			VerifyRegex(cssResult, regexExpected, "counter-increment style not generated for Subentry.");
	  }

		[Test]
		public void GenerateCssForNumberingStyleForExamples()
		{
			GenerateNumberingStyle("Numbered List", VwBulNum.kvbnLetterUpper);
			var dictNodeOptions = new DictionaryNodeListAndParaOptions
			{
				DisplayEachInAParagraph = true,
				Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>()
			};
			dictNodeOptions.Options.Add(new DictionaryNodeListOptions.DictionaryNodeOption { Id = "a0000000-dd15-4a03-9032-b40faaa9a754" });
			dictNodeOptions.Options.Add(new DictionaryNodeListOptions.DictionaryNodeOption { Id = "1f6ae209-141a-40db-983c-bee93af0ca3c" });
			var examples = new ConfigurableDictionaryNode
			{
				FieldDescription = "ExamplesOS",
				DictionaryNodeOptions = dictNodeOptions,
				Style = "Numbered List"
			};
			var senses = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "Senses",
				DictionaryNodeOptions = new DictionaryNodeSenseOptions { DisplayEachSenseInAParagraph = true },
				Children = new List<ConfigurableDictionaryNode> { examples }
			};
			var entry = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				CSSClassNameOverride = "lexentry",
				Children = new List<ConfigurableDictionaryNode> { senses }
			};

			var model = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { entry } };
			PopulateFieldsForTesting(entry);
			//SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			const string regexExpected = @"\s.examplesos{.*counter-reset:[\s]examplesos;.*}.*\s*\s\.examplesos\s\.exampleso:before{.*counter-increment:[\s]examplesos;.*content:[\s]counter.examplesos,[\s]upper-alpha.\s'\s';.*font-size:14pt;.*color:Green;.*}";
			VerifyRegex(cssResult, regexExpected, "Numbering style not generated for Examples.");
		}

		[Test]
		public void GenerateCssForDirectionRightToLeftForEntry()
		{
			var entryParagraphStyle = GenerateParagraphStyle("Dictionary-Entry");
			entryParagraphStyle.SetExplicitParaIntProp((int)FwTextPropType.ktptRightToLeft, 2, (int)TriStateBool.triTrue);
			var entry = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				CSSClassNameOverride = "lexentry",
				Style = "Dictionary-Entry"
			};
			var model = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { entry } };
			PopulateFieldsForTesting(model);
			// SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			Assert.That(cssResult, Contains.Substring("direction:rtl"));
			const string regExPectedForPadding = @".lexentry.*{.*text-align:justify;.*border-color:#F00;.*border-left-width:0pt;.*border-right-width:5pt;.*border-top-width:20pt;.*border-bottom-width:10pt;.*margin-right:24pt;.*line-height:2;.*padding-bottom:30pt;.*padding-top:15pt;.*padding-left:48pt;.*}";
			Assert.IsTrue(Regex.Match(cssResult, regExPectedForPadding, RegexOptions.Singleline).Success, "Margin Right and/or Padding Left not generated.");
		}

		[Test]
		public void GenerateCssForDirectionNotSetForEntry()
		{
			GenerateParagraphStyle("Dictionary-Entry-NoRTL");
			var entry = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				CSSClassNameOverride = "lexentry",
				Style = "Dictionary-Entry-NoRTL"
			};
			var model = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { entry } };
			PopulateFieldsForTesting(model);
			// SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			Assert.That(cssResult, !Contains.Substring("direction"));
		}

		[Test]
		public void GenerateCssForNonBulletStyleForSenses()
		{
			GenerateSenseStyle("Sense List");
			var senses = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "Senses",
				DictionaryNodeOptions = new DictionaryNodeSenseOptions { NumberStyle = "Dictionary-SenseNum", DisplayEachSenseInAParagraph = true },
				Style = "Sense List"
			};
			var entry = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				CSSClassNameOverride = "lexentry",
				Children = new List<ConfigurableDictionaryNode> { senses }
			};
			var model = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { entry } };
			PopulateFieldsForTesting(entry);
			// SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			const string regExpected = @"\s.senses\s>\s.sensecontent";
			VerifyRegex(cssResult, regExpected, "Sense List style should generate a match.");
			const string regExNotExpected = regExpected + @"(\s*\.sensecontent)?:not\(:first-child\):before";
			Assert.IsFalse(Regex.Match(cssResult, regExNotExpected, RegexOptions.Singleline).Success, "Sense List style should not generate a match, since it is not a bulleted style.");
		}

		[Test]
		public void GenerateCssForBulletStyle_OneStyleWhenSubSenseMatchesSense()
		{
			var cssGenerator = new CssGenerator();
			GenerateBulletStyle("Bulleted List");
			var subsenses = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "Senses",
				DictionaryNodeOptions = new DictionaryNodeSenseOptions
				{
					NumberStyle = "Dictionary-SenseNum", DisplayEachSenseInAParagraph = true
				},
				Style = "Bulleted List"
			};
			var senses = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "Senses",
				DictionaryNodeOptions = new DictionaryNodeSenseOptions
				{
					NumberStyle = "Dictionary-SenseNum", DisplayEachSenseInAParagraph = true
				},
				Style = "Bulleted List",
				Children = new List<ConfigurableDictionaryNode> { subsenses }
			};

			var entry = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				CSSClassNameOverride = "lexentry",
				Children = new List<ConfigurableDictionaryNode> { senses }
			};
			var model = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { entry } };
			PopulateFieldsForTesting(entry);
			// SUT
			// Add the sense and subsense styles
			cssGenerator.AddGlobalStyles(model, m_propertyTable); // Gets the bullet information prepped
			cssGenerator.Init(m_propertyTable);
			cssGenerator.AddStyles(senses);
			cssGenerator.AddStyles(subsenses);
			var cssResult = cssGenerator.GetStylesString();
			const string regExPected = @".*senses\s>\s.sensecontent:before.*{.*content:'\\25A0';.*font-size:14pt;.*color:Green;.*font-family:Arial;.*font-weight:bold;.*font-style:italic;.*background-color:Brown;.*}";
			Assert.That(Regex.Match(cssResult, regExPected, RegexOptions.Singleline).Success, "Bulleted style for SubSenses not generated.");
			Assert.That(!Regex.Match(cssResult, regExPected, RegexOptions.Singleline).NextMatch().Success, "Bulleted style for SubSenses not generated.");
		}

		[Test]
		public void GenerateCssForBulletStyle_TwoStylesWhenSubSensesAreDifferent()
		{
			var cssGenerator = new CssGenerator();
			GenerateBulletStyle("Bulleted List");
			var subsenses = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "Senses",
				DictionaryNodeOptions = new DictionaryNodeSenseOptions
				{
					NumberStyle = "Dictionary-SenseNum", DisplayEachSenseInAParagraph = true
				},
				Style = "Bulleted List"
			};
			var senses = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "Senses",
				DictionaryNodeOptions = new DictionaryNodeSenseOptions
				{
					NumberStyle = "Dictionary-SenseNum", DisplayEachSenseInAParagraph = true
				},
				Children = new List<ConfigurableDictionaryNode> { subsenses }
			};

			var entry = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				CSSClassNameOverride = "lexentry",
				Children = new List<ConfigurableDictionaryNode> { senses }
			};
			var model = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { entry } };
			PopulateFieldsForTesting(entry);
			// SUT
			// Add the sense and subsense styles
			cssGenerator.AddGlobalStyles(model, m_propertyTable); // Gets the bullet information prepped
			cssGenerator.Init(m_propertyTable);
			cssGenerator.AddStyles(senses);
			cssGenerator.AddStyles(subsenses);
			var cssResult = cssGenerator.GetStylesString();
			const string regExPectedForSub = @"\.senses-senses\s>\s.sensecontent:before.*{.*content:'\\25A0';.*font-size:14pt;.*color:Green;.*font-family:Arial;.*font-weight:bold;.*font-style:italic;.*background-color:Brown;.*}";
			VerifyRegex(cssResult, regExPectedForSub, "Bulleted style for SubSenses not generated.");
			const string regExPectedForSense = @"\.senses\s>\s\.sensecontent"; // Make sure there is a .sense > .sensecontent rule as well as the bulletted sub-sense
			VerifyRegex(cssResult, regExPectedForSense, "Non-bulleted style for Senses not generated.");
		}

		[Test]
		public void GenerateCssForBulletStyleForRootSubentries()
		{
			GenerateBulletStyle("Bulleted List");
			var dictNodeOptions = new DictionaryNodeListAndParaOptions
			{
				DisplayEachInAParagraph = true, Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>()
			};
			dictNodeOptions.Options.Add(new DictionaryNodeListOptions.DictionaryNodeOption { Id = "a0000000-dd15-4a03-9032-b40faaa9a754" } );
			dictNodeOptions.Options.Add(new DictionaryNodeListOptions.DictionaryNodeOption { Id = "1f6ae209-141a-40db-983c-bee93af0ca3c" } );
			var subentriesConfig = new ConfigurableDictionaryNode
			{
				FieldDescription = "Subentries",
				DictionaryNodeOptions = dictNodeOptions,
				Style = "Bulleted List"
			};
			var entryConfig = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { subentriesConfig }
			};
			var model = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { entryConfig } };
			PopulateFieldsForTesting(entryConfig);
			// SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			var regexExpected1 = @"\.subentries\s\.subentry{[^}]*\sfont-size:12pt;[^}]*\scolor:#F00;[^}]*\sdisplay:block;[^}]*}";
			Assert.IsTrue(Regex.Match(cssResult, regexExpected1, RegexOptions.Singleline).Success,
				"expected subentry rule not generated");
			var regexExpected2 = @"\.subentries\s\.subentry:before{[^}]*\scontent:'\\25A0';[^}]*font-size:14pt;[^}]*color:Green;[^}]*}";
			Assert.IsTrue(Regex.Match(cssResult, regexExpected2, RegexOptions.Singleline).Success,
				"expected subentry:before rule not generated");
			// Check that the bullet info values occur only in the :before section, and that the primary values
			// do not occur in the :before section.
			var regexUnwanted1 = @"\.subentries\s\.subentry{[^}]*\scontent:'\\25A0';[^}]*}";
			Assert.IsFalse(Regex.Match(cssResult, regexUnwanted1, RegexOptions.Singleline).Success,
				"subentry rule has unwanted content value");
			var regexUnwanted2 = @".subentries\s\.subentry{[^}]*\sfont-size:14pt;[^}]*}";
			Assert.IsFalse(Regex.Match(cssResult, regexUnwanted2, RegexOptions.Singleline).Success,
				"subentry rule has unwanted font-size value");
			var regexUnwanted3 = @".subentries\s\.subentry{[^}]*\scolor:Green;[^}]*}";
			Assert.IsFalse(Regex.Match(cssResult, regexUnwanted3, RegexOptions.Singleline).Success,
				"subentry rule has unwanted color value");
			var regexUnwanted4 = @"\.lexentry>\s\.subentries\s\.subentry:before{[^}]*\sfont-size:12pt;[^}]*}";
			Assert.IsFalse(Regex.Match(cssResult, regexUnwanted4, RegexOptions.Singleline).Success,
				"subentry:before rule has unwanted font-size value");
			var regexUnwanted5 = @"\.lexentry>\s\.subentries\s\.subentry:before{[^}]*\scolor:#F00;[^}]*}";
			Assert.IsFalse(Regex.Match(cssResult, regexUnwanted5, RegexOptions.Singleline).Success,
				"subentry:before rule has unwanted color value");
			var regexUnwanted6 = @"\.lexentry>\s\.subentries\s\.subentry:before{[^}]*\sdisplay:block;[^}]*}";
			Assert.IsFalse(Regex.Match(cssResult, regexUnwanted6, RegexOptions.Singleline).Success,
				"subentry:before rule has unwanted display value");
		}

		[Test]
		public void GenerateCssForCustomFieldUnderISenseOrEntry()
		{
			var customConfig = new ConfigurableDictionaryNode
			{
				FieldDescription = "Costume", DictionaryNodeOptions = new DictionaryNodeListOptions(),
				Style = "FooStyle", IsCustomField = true
			};
			var targets = new ConfigurableDictionaryNode
			{
				FieldDescription = "ConfigTargets",
				Children = new List<ConfigurableDictionaryNode> { customConfig }
			};
			var crossRefs = new ConfigurableDictionaryNode
			{
				FieldDescription = "MinimalLexReferences", CSSClassNameOverride = "mlrs",
				Children = new List<ConfigurableDictionaryNode> { targets }
			};
			var entryConfig = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { crossRefs }
			};
			var model = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { entryConfig } };
			PopulateFieldsForTesting(model);
			using (new CustomFieldForTest(Cache, "Costume", Cache.MetaDataCacheAccessor.GetClassId("LexEntry"), 0, CellarPropertyType.Nil, Guid.Empty))
			{
				// SUT
				var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
				const string regexExpected1 = @"\s*\.costume{[^}]*}";
				VerifyRegex(cssResult, regexExpected1, "expected costume rule not generated");
			}
		}

		[Test]
		public void GenerateCssForCustomFieldStartsWithNumber()
		{
			var customConfig = new ConfigurableDictionaryNode
			{
				FieldDescription = "12Costume",
				DictionaryNodeOptions = new DictionaryNodeListOptions(),
				Style = "FooStyle",
				IsCustomField = true
			};
			var entryConfig = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { customConfig }
			};
			var model = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { entryConfig } };
			PopulateFieldsForTesting(model);
			using (new CustomFieldForTest(Cache, "Costume", Cache.MetaDataCacheAccessor.GetClassId("LexEntry"), 0, CellarPropertyType.Nil, Guid.Empty))
			{
				// SUT
				var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
				const string regexExpected1 = @"\s*.cf12costume{[^}]*}";
				VerifyRegex(cssResult, regexExpected1, "Class name started with number");
			}
		}

		[Test]
		public void GenerateCssForCustomFieldWithSpaces()
		{
			var nameConfig = new ConfigurableDictionaryNode
			{
				FieldDescription = "Name",
				Style = "FooStyle",
				DictionaryNodeOptions = new DictionaryNodeWritingSystemOptions()
			};
			var abbrConfig = new ConfigurableDictionaryNode
			{
				FieldDescription = "Abbreviation",
				Style = "FooStyle",
				DictionaryNodeOptions = new DictionaryNodeWritingSystemOptions()
			};
			var customConfig = new ConfigurableDictionaryNode
			{
				FieldDescription = "Custom Location", DictionaryNodeOptions = new DictionaryNodeListOptions(),
				Style = "FooStyle", IsCustomField = true,
				Children = new List<ConfigurableDictionaryNode> { nameConfig, abbrConfig }
			};
			var entryConfig = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { customConfig }
			};
			var model = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { entryConfig } };
			PopulateFieldsForTesting(model);
			using (new CustomFieldForTest(Cache, "Custom Location", Cache.MetaDataCacheAccessor.GetClassId("LexEntry"), 0,
				CellarPropertyType.ReferenceCollection, Guid.Empty))
			{
				// SUT
				var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
				const string regexExpected1 = @"\s*\.custom-location \.custom-locatio{[^}]*}";
				VerifyRegex(cssResult, regexExpected1, "expected custom-location rule not generated");
				const string regexExpected2 = @"\s*\.name{[^}]*}";
				VerifyRegex(cssResult, regexExpected2, "expected custom-location name rule not generated");
				const string regexExpected3 = @"\s*\.abbreviation{[^}]*}";
				VerifyRegex(cssResult, regexExpected3, "expected custom-location>abbreviation rule not generated");
			}
		}

		[Test]
		public void GenerateCssForDuplicateConfigNodeWithSpaces()
		{
			var noteConfig = new ConfigurableDictionaryNode
			{
				FieldDescription = "Note",
				IsDuplicate = true,
				LabelSuffix = "Test One",
				Style = "FooStyle",
				DictionaryNodeOptions = new DictionaryNodeWritingSystemOptions()
			};
			var entryConfig = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { noteConfig }
			};
			var model = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { entryConfig } };
			PopulateFieldsForTesting(model);
			// SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			var regexExpected1 = @"\s\.note_test-one{[^}]*}";
			VerifyRegex(cssResult, regexExpected1, "expected duplicated config node rename rule not generated");
		}

		[Test]
		public void GenerateCssForDuplicateConfigNodeWithPunc()
		{
			var noteConfig = new ConfigurableDictionaryNode
			{
				FieldDescription = "Note",
				IsDuplicate = true,
				LabelSuffix = "#Test",
				Style = "FooStyle",
				DictionaryNodeOptions = new DictionaryNodeWritingSystemOptions()
			};
			var entryConfig = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { noteConfig }
			};
			var model = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { entryConfig } };
			PopulateFieldsForTesting(model);
			// SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			var regexExpected1 = @"^\s*\.note_-test{[^}]*}";
			VerifyRegex(cssResult, regexExpected1, "expected duplicated config node rename rule not generated");
		}

		[Test]
		public void GenerateCssForDuplicateConfigNodeWithMultiPunc()
		{
			var noteConfig = new ConfigurableDictionaryNode
			{
				FieldDescription = "Note",
				IsDuplicate = true,
				LabelSuffix = "#Test#",
				Style = "FooStyle",
				DictionaryNodeOptions = new DictionaryNodeWritingSystemOptions()
			};
			var entryConfig = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { noteConfig }
			};
			var model = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { entryConfig } };
			PopulateFieldsForTesting(model);
			// SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			var regexExpected1 = @"\s*\.note_-test-{[^}]*}";
			VerifyRegex(cssResult, regexExpected1, "expected duplicated config node rename rule not generated");
		}

		[Test]
		public void GenerateCssForCollectionBeforeAndAfter()
		{
			var pronunciationConfig = new ConfigurableDictionaryNode
			{
				Before = "[",
				After = "]",
				Between = " ",
				Label = "Pronunciation",
				FieldDescription = "Form",
				DictionaryNodeOptions = new DictionaryNodeWritingSystemOptions {
					WsType = DictionaryNodeWritingSystemOptions.WritingSystemType.Pronunciation,
					DisplayWritingSystemAbbreviations = false,
					Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>
					{
						new DictionaryNodeListOptions.DictionaryNodeOption { Id = "pronunciation" }
					}
				}
			};
			var pronunciationsConfig = new ConfigurableDictionaryNode
			{
				Before = "{Pron: ",
				Between = ", ",
				After = "} ",
				Label = "Pronunciations",
				FieldDescription = "PronunciationsOS",
				CSSClassNameOverride = "pronunciations",
				Children = new List<ConfigurableDictionaryNode> { pronunciationConfig }
			};
			var entryConfig = new ConfigurableDictionaryNode
			{
				Label = "Main Entry",
				FieldDescription = "LexEntry",
				CSSClassNameOverride = "entry",
				Style = "Dictionary-Normal",
				Children = new List<ConfigurableDictionaryNode> { pronunciationsConfig }
			};
			var model = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { entryConfig } };
			PopulateFieldsForTesting(entryConfig);
			// SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);

			//Following Testcase removed(no longer needed) as a fix for LT-17238 ("Between" contents should not come between spans that are all in a single string with embedded WSs)
			//var regexItem1 = @".entry> .pronunciations .pronunciation> .form> span\+ span:before\{\s*content:' ';\s*\}";
			//Assert.IsTrue(Regex.Match(cssResult, regexItem1, RegexOptions.Singleline).Success, "expected collection item between rule is generated");

			var regexItem2 = @".form> span:first-child:before\{\s*content:'\[';\s*\}";
			Assert.IsTrue(Regex.Match(cssResult, regexItem2, RegexOptions.Singleline).Success, "expected collection item before rule is generated");

			var regexItem3 = @".form> span:last-child:after\{\s*content:'\]';\s*\}";
			Assert.IsTrue(Regex.Match(cssResult, regexItem3, RegexOptions.Singleline).Success, "expected collection item after rule is generated");

			var regexCollection1 = @"^\.pronunciations>\s+.pronunciation\s+\+\s+\.pronunciation:before\{\s*content:', ';\s*\}";
			VerifyRegex(cssResult, regexCollection1, "expected collection between rule is generated");

			// The following two checks test the fix for LT-17048.  The preceding four checks should be the same before and after the fix.
			var regexCollection2 = @".pronunciations:before\{\s*content:'\{Pron: ';\s*\}";
			VerifyRegex(cssResult, regexCollection2, "expected collection before rule is generated");

			var regexCollection3 = @".pronunciations:after\{\s*content:'\} ';\s*\}";
			VerifyRegex(cssResult, regexCollection3, "expected collection after rule is generated");
		}

		[Test]
		public void GenerateCssForConfiguration_NoBeforeAfterForSenseParagraphs()
		{
			var glossConfig = new ConfigurableDictionaryNode
			{
				Before = "{",
				Between = "|",
				After = "}",
				FieldDescription = "Gloss",
				Children = new List<ConfigurableDictionaryNode>()
			};
			var sensesConfig = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				Before = "[[",
				After = "]]",
				DictionaryNodeOptions = new DictionaryNodeSenseOptions { DisplayEachSenseInAParagraph = true },
				Children = new List<ConfigurableDictionaryNode> { glossConfig }
			};
			var entryConfig = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { sensesConfig }
			};
			PopulateFieldsForTesting(entryConfig);
			var model = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { entryConfig } };
			//SUT
			var cssPara = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			((DictionaryNodeSenseOptions)sensesConfig.DictionaryNodeOptions).DisplayEachSenseInAParagraph = false;
			var cssInline = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);

			const string regexBefore = @"^\.senses:before\{";
			const string regexAfter = @"^\.senses:after\{";

			Assert.AreNotEqual(cssPara, cssInline, "The css should change depending on senses showing in a paragraph");
			VerifyRegex(cssInline, regexBefore, "The css for inline senses should have a senses:before rule");
			VerifyRegex(cssInline, regexAfter, "The css for inline senses should have a senses:after rule");
			Assert.IsFalse(Regex.IsMatch(cssPara, regexBefore, RegexOptions.Multiline), "The css for paragraphed senses should not have a senses:before rule");
			Assert.IsFalse(Regex.IsMatch(cssPara, regexAfter, RegexOptions.Multiline), "The css for paragraphed senses should not have a senses:after rule");
		}

		[Test]
		public void GenerateCssForConfiguration_SpecificLanguageColorIsNotOverridenByParagraphStyle()
		{
			var discussionNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Discussion",
				CSSClassNameOverride = "discussion",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "en" },
										DictionaryNodeWritingSystemOptions.WritingSystemType.Analysis)
			};
			var extNoteNode = new ConfigurableDictionaryNode
			{
				Label = "Extended Note",
				FieldDescription = "ExtendedNoteOS",
				CSSClassNameOverride = "extendednotecontents",
				Children = new List<ConfigurableDictionaryNode> { discussionNode },
				Style = "Dictionary-Sense",
				StyleType = ConfigurableDictionaryNode.StyleTypes.Paragraph,
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetFullyEnabledListOptions(Cache, DictionaryNodeListOptions.ListIds.Note)
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { extNoteNode },
				FieldDescription = "LexEntry"
			};
			var model = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { mainEntryNode } };
			PopulateFieldsForTesting(model);
			var vernWs = Cache.ServiceLocator.WritingSystemManager.Get(Cache.DefaultVernWs);
			Assert.AreEqual("fr", vernWs.LanguageTag); // just verifying
			// Set Dictionary-Sense to default to Green
			var greenFontInfo = new FontInfo {m_fontColor = {ExplicitValue = Color.Green}};
			var newteststyle = GenerateStyleFromFontInfo(Cache, "Dictionary-Sense", greenFontInfo);
			// But make it Blue, if we're doing French
			var blueFontInfo = new FontInfo {m_fontColor = {ExplicitValue = Color.Blue}};
			newteststyle.SetWsStyle(blueFontInfo, vernWs.Handle);
			SafelyAddStyleToSheetAndTable(newteststyle.Name, newteststyle);

			//SUT
			var result = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable);
			// default (analysis ws) rule
			const string regexPrimary = @"^\.extendednotecontents\{\s*color:#008000;";
			// specific (embedded vernacular ws) rule affecting any span inside .extendednotecontents (at any level)
			const string regexSpecific = @"^\.extendednotecontents span\[lang='fr']\{\s*color:#00F";
			VerifyRegex(result, regexPrimary, "The css for the default color should be there.");
			VerifyRegex(result, regexSpecific, "The css for the specific language color should be there.");
		}

		[Test]
		public void GenerateCssForConfiguration_ContentNormalizedComposed()
		{
			var icuNormalizer = CustomIcu.GetIcuNormalizer(FwNormalizationMode.knmNFD);
			var model = new DictionaryConfigurationModel
			{
				Parts = new List<ConfigurableDictionaryNode>
				{
					new ConfigurableDictionaryNode
					{
						FieldDescription = "LexEntry",
						CSSClassNameOverride = icuNormalizer.Normalize("자ㄱㄴ시"),
						Children = new List<ConfigurableDictionaryNode>
						{
							new ConfigurableDictionaryNode
							{
								FieldDescription = "MLHeadWord",
								Before = icuNormalizer.Normalize("garçon"),
								Between = icuNormalizer.Normalize("자ㄱㄴ시"),
								After = icuNormalizer.Normalize("Brötchen")
							}
						}
					}
				}
			};
			PopulateFieldsForTesting(model);
			var result = CssGenerator.GenerateCssFromConfiguration(model, m_propertyTable); // SUT
			Assert.That(result, Is.Not.Null.Or.Empty);
			Assert.That(TsStringUtils.MakeString(result, 1).get_IsNormalizedForm(FwNormalizationMode.knmNFC));
		}

		#region Test Helper Methods

		/// <summary>Populate fields that need to be populated on node and its children, including Parent, Label, and IsEnabled</summary>
		internal static void PopulateFieldsForTesting(ConfigurableDictionaryNode node)
		{
			Assert.NotNull(node);
			PopulateFieldsForTesting(new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { node } });
		}

		/// <summary>Populate fields that need to be populated on node and its children, including Parent, Label, and IsEnabled</summary>
		internal static void PopulateFieldsForTesting(DictionaryConfigurationModel model)
		{
			Assert.NotNull(model);
			PopulateFieldsForTesting(model.Parts.Concat(model.SharedItems));
			DictionaryConfigurationModel.SpecifyParentsAndReferences(model.Parts, model.SharedItems);
		}

		private static void PopulateFieldsForTesting(IEnumerable<ConfigurableDictionaryNode> nodes)
		{
			foreach (var node in nodes)
			{
				// avoid test problems in ConfigurableDictionaryNode.GetHashCode() if no Label is set
				if (string.IsNullOrEmpty(node.Label))
					node.Label = node.FieldDescription;

				node.IsEnabled = true;
				if (node.DictionaryNodeOptions != null)
					EnableAllListOptions(node.DictionaryNodeOptions);

				if (node.Children != null)
					PopulateFieldsForTesting(node.Children);
			}
		}

		private static void EnableAllListOptions(DictionaryNodeOptions options)
		{
			List<DictionaryNodeListOptions.DictionaryNodeOption> checkList = null;
			if (options is DictionaryNodeSenseOptions || options is DictionaryNodePictureOptions || options is DictionaryNodeGroupingOptions)
			{
				return;
			}
			if (options is DictionaryNodeListOptions) // also covers DictionaryNodeListAndParaOptions
			{
				checkList = ((DictionaryNodeListOptions)options).Options;
			}
			else if (options is DictionaryNodeWritingSystemOptions)
			{
				checkList = ((DictionaryNodeWritingSystemOptions)options).Options;
			}
			else
			{
				Assert.Fail("Unknown subclass of DictionaryNodeOptions");
			}
			if (checkList == null)
				return;
			foreach (var nodeOption in checkList)
			{
				nodeOption.IsEnabled = true;
			}
		}

		private TestStyle GenerateStyle(string name)
		{
			var fontInfo = new FontInfo();
			fontInfo.m_fontColor.ExplicitValue = FontColor;
			fontInfo.m_backColor.ExplicitValue = FontBGColor;
			fontInfo.m_fontName.ExplicitValue = FontName;
			fontInfo.m_italic.ExplicitValue = true;
			fontInfo.m_bold.ExplicitValue = true;
			fontInfo.m_fontSize.ExplicitValue = FontSize;
			var style = GenerateStyleFromFontInfo(Cache, name, fontInfo);
			SafelyAddStyleToSheetAndTable(name, style);
			return style;
		}

		private static TestStyle GenerateStyleFromFontInfo(LcmCache cache, string name, FontInfo fontInfo, bool isParagraphStyle = false)
		{
			return new TestStyle(fontInfo, cache) { Name = name, IsParagraphStyle = isParagraphStyle };
		}

		private void SafelyAddStyleToSheetAndTable(string name, TestStyle style)
		{
			if (m_styleSheet.Styles.Contains(name))
				m_styleSheet.Styles.Remove(name);
			m_styleSheet.Styles.Add(style);
			if (m_owningTable.ContainsKey(name))
				m_owningTable.Remove(name);
			m_owningTable.Add(name, style);
		}

		private void GenerateBulletStyle(string name)
		{
			var fontInfo = new FontInfo();
			fontInfo.m_fontColor.ExplicitValue = Color.Green;
			fontInfo.m_fontSize.ExplicitValue = 14000;
			fontInfo.m_fontName.ExplicitValue = "Arial";
			fontInfo.m_bold.ExplicitValue = true;
			fontInfo.m_italic.ExplicitValue = true;
			fontInfo.m_underline.ExplicitValue = FwUnderlineType.kuntNone;
			fontInfo.m_backColor.ExplicitValue = Color.Blue;
			var bulletinfo = new BulletInfo
			{
				m_numberScheme = (VwBulNum)105,
				FontInfo = fontInfo
			};
			var inherbullt = new InheritableStyleProp<BulletInfo>(bulletinfo);
			var style = new TestStyle(inherbullt, Cache) { Name = name, IsParagraphStyle = true };

			var fontInfo1 = new FontInfo();
			fontInfo1.m_fontColor.ExplicitValue = Color.Red;
			fontInfo1.m_fontSize.ExplicitValue = 12000;
			fontInfo1.m_fontName.ExplicitValue = "Arial";
			fontInfo1.m_bold.ExplicitValue = false;
			fontInfo1.m_italic.ExplicitValue = false;
			fontInfo1.m_underline.ExplicitValue = FwUnderlineType.kuntNone;
			fontInfo.m_backColor.ExplicitValue = Color.Brown;
			style.SetDefaultFontInfo(fontInfo1);

			SafelyAddStyleToSheetAndTable(name, style);
		}

		private void GenerateCustomBulletStyle(string name)
		{
			var fontInfo = new FontInfo();
			fontInfo.m_fontColor.ExplicitValue = Color.Green;
			fontInfo.m_fontSize.ExplicitValue = 14000;
			fontInfo.m_fontName.ExplicitValue = "Arial";
			fontInfo.m_bold.ExplicitValue = true;
			fontInfo.m_italic.ExplicitValue = true;
			fontInfo.m_underline.ExplicitValue = FwUnderlineType.kuntNone;
			fontInfo.m_backColor.ExplicitValue = Color.Blue;
			var bulletinfo = new BulletInfo
			{
				m_numberScheme = (VwBulNum)100,
				m_bulletCustom = "@",
				FontInfo = fontInfo
			};
			var inherbullt = new InheritableStyleProp<BulletInfo>(bulletinfo);
			var style = new TestStyle(inherbullt, Cache) { Name = name, IsParagraphStyle = true };

			var fontInfo1 = new FontInfo();
			fontInfo1.m_fontColor.ExplicitValue = Color.Red;
			fontInfo1.m_fontSize.ExplicitValue = 12000;
			fontInfo1.m_fontName.ExplicitValue = "Arial";
			fontInfo1.m_bold.ExplicitValue = false;
			fontInfo1.m_italic.ExplicitValue = false;
			fontInfo1.m_underline.ExplicitValue = FwUnderlineType.kuntNone;
			fontInfo.m_backColor.ExplicitValue = Color.Brown;
			style.SetDefaultFontInfo(fontInfo1);

			SafelyAddStyleToSheetAndTable(name, style);
		}

		private void GenerateNumberingStyle(string name, VwBulNum schemeType)
		{
			var fontInfo = new FontInfo();
			fontInfo.m_fontColor.ExplicitValue = Color.Green;
			fontInfo.m_fontSize.ExplicitValue = 14000;
			fontInfo.m_fontName.ExplicitValue = "Arial";
			fontInfo.m_bold.ExplicitValue = true;
			fontInfo.m_italic.ExplicitValue = true;
			fontInfo.m_underline.ExplicitValue = FwUnderlineType.kuntNone;
			fontInfo.m_backColor.ExplicitValue = Color.Blue;
			var bulletinfo = new BulletInfo
			{
				m_numberScheme = schemeType,
				FontInfo = fontInfo
			};
			var inherbullt = new InheritableStyleProp<BulletInfo>(bulletinfo);
			var style = new TestStyle(inherbullt, Cache) { Name = name, IsParagraphStyle = true };

			var fontInfo1 = new FontInfo();
			fontInfo1.m_fontColor.ExplicitValue = Color.Red;
			fontInfo1.m_fontSize.ExplicitValue = 12000;
			fontInfo1.m_fontName.ExplicitValue = "Arial";
			fontInfo1.m_bold.ExplicitValue = false;
			fontInfo1.m_italic.ExplicitValue = false;
			fontInfo1.m_underline.ExplicitValue = FwUnderlineType.kuntNone;
			fontInfo.m_backColor.ExplicitValue = Color.Brown;
			style.SetDefaultFontInfo(fontInfo1);

			SafelyAddStyleToSheetAndTable(name, style);
		}

		private void GenerateSenseStyle(string name)
		{
			var fontInfo = new FontInfo
			{
				m_backColor = { ExplicitValue = FontBGColor },
				m_fontName = { ExplicitValue = FontName },
				m_italic = { ExplicitValue = true },
				m_bold = { ExplicitValue = true },
				m_fontSize = { ExplicitValue = FontSize }
			};
			var style = new TestStyle(fontInfo, Cache) { Name = name, IsParagraphStyle = true };
			// Padding style settings
			style.SetExplicitParaIntProp((int)FwTextPropType.ktptLeadingIndent, 0, LeadingIndent);
			style.SetExplicitParaIntProp((int)FwTextPropType.ktptTrailingIndent, 0, TrailingIndent);
			style.SetExplicitParaIntProp((int)FwTextPropType.ktptSpaceBefore, 0, PadTop);
			style.SetExplicitParaIntProp((int)FwTextPropType.ktptSpaceAfter, 0, PadBottom);
			SafelyAddStyleToSheetAndTable(name, style);
		}

		private TestStyle GenerateEmptyStyle(string name)
		{
			var fontInfo = new FontInfo();
			var style = new TestStyle(fontInfo, Cache) { Name = name, IsParagraphStyle = false };
			SafelyAddStyleToSheetAndTable(name, style);
			return style;
		}

		private TestStyle GenerateParagraphStyle(string name)
		{
			var fontInfo = new FontInfo
			{
				m_fontColor = { ExplicitValue = FontColor },
				m_backColor = { ExplicitValue = FontBGColor },
				m_fontName = { ExplicitValue = FontName },
				m_italic = { ExplicitValue = true },
				m_bold = { ExplicitValue = true },
				m_fontSize = { ExplicitValue = FontSize }
			};
			var style = new TestStyle(fontInfo, Cache) { Name = name, IsParagraphStyle = true };
			// Border style settings
			style.SetExplicitParaIntProp((int)FwTextPropType.ktptBorderColor, 0, (int)ColorUtil.ConvertColorToBGR(BorderColor));
			//border leading omitted from paragraph style definition which should result in 0pt left width
			style.SetExplicitParaIntProp((int)FwTextPropType.ktptBorderTrailing, 0, BorderTrailing);
			style.SetExplicitParaIntProp((int)FwTextPropType.ktptBorderTop, 0, BorderTop);
			style.SetExplicitParaIntProp((int)FwTextPropType.ktptBorderBottom, 0, BorderBottom);
			// Padding style settings
			style.SetExplicitParaIntProp((int)FwTextPropType.ktptLeadingIndent, 0, LeadingIndent);
			style.SetExplicitParaIntProp((int)FwTextPropType.ktptTrailingIndent, 0, TrailingIndent);
			style.SetExplicitParaIntProp((int)FwTextPropType.ktptSpaceBefore, 0, PadTop);
			style.SetExplicitParaIntProp((int)FwTextPropType.ktptSpaceAfter, 0, PadBottom);
			// Alignment setting
			style.SetExplicitParaIntProp((int)FwTextPropType.ktptAlign, 0, (int)ParagraphAlignment);
			// Line space setting (set to double space)
			style.SetExplicitParaIntProp((int)FwTextPropType.ktptLineHeight, (int)FwTextPropVar.ktpvRelative, DoubleSpace);
			SafelyAddStyleToSheetAndTable(name, style);
			return style;
		}

		private void GenerateNormalStyle(string name)
		{
			var fontInfo = new FontInfo { m_fontSize = { ExplicitValue = FontSize } };
			var style = new TestStyle(fontInfo, Cache) { Name = name, IsParagraphStyle = true };
			// Padding style settings
			style.SetExplicitParaIntProp((int)FwTextPropType.ktptLeadingIndent, 0, LeadingIndent);
			style.SetExplicitParaIntProp((int)FwTextPropType.ktptTrailingIndent, 0, TrailingIndent);
			SafelyAddStyleToSheetAndTable(name, style);
		}

		private TestStyle GenerateEmptyParagraphStyle(string name)
		{
			var fontInfo = new FontInfo();
			var style = new TestStyle(fontInfo, Cache) { Name = name, IsParagraphStyle = true };
			SafelyAddStyleToSheetAndTable(name, style);
			return style;
		}

		private sealed class TempContextStyle : IDisposable
		{
			private readonly CssGeneratorTests m_tests;
			private readonly string m_name;

			/// <summary>Creates a temporary Style with the specified name</summary>
			public TempContextStyle(CssGeneratorTests tests, string name)
			{
				m_tests = tests;
				m_name = name;
				var fontInfo = new FontInfo
				{
					m_fontColor = { ExplicitValue = FontColor },
					m_fontSize = { ExplicitValue = FontSize }
				};
				var style = new TestStyle(fontInfo, m_tests.Cache) { Name = m_name, IsParagraphStyle = false };
				m_tests.SafelyAddStyleToSheetAndTable(m_name, style);
			}

			~TempContextStyle()
			{
				Dispose(false);
			}

			private void Dispose(bool disposing)
			{
				Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + " ******");

				if (!disposing)
					return;
				var fontInfo = new FontInfo();
				var style = new TestStyle(fontInfo, m_tests.Cache) { Name = m_name, IsParagraphStyle = false };
				m_tests.SafelyAddStyleToSheetAndTable(m_name, style);
			}

			/// <summary>Replace the populated style with an empty one</summary>
			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}
		}

		private static void SetStyleFontColor(TestStyle style, Color color)
		{
			style.SetDefaultFontInfo(new FontInfo() { m_fontColor = { ExplicitValue = color } });
		}

		private static void VerifyFontInfoInCss(Color color, Color bgcolor, string fontName, bool bold, bool italic, int size, string css, string fontFeatures = null)
		{
			Assert.That(css, Contains.Substring("color:" + HtmlColor.FromRgb(color.R, color.G, color.B)), "font color missing");
			Assert.That(css, Contains.Substring("background-color:" + HtmlColor.FromRgb(bgcolor.R, bgcolor.G, bgcolor.B)), "background-color missing");
			Assert.That(css, Contains.Substring("font-family:'" + fontName + "'"), "font name missing");
			Assert.That(css, Contains.Substring("font-weight:" + (bold ? "bold" : "normal") + ";"), "font bold missing");
			Assert.That(css, Contains.Substring("font-style:" + (italic ? "italic" : "normal") + ";"), "font italic missing");
			// Font sizes are stored as millipoint integers in the styles by FLEx and turned into pt values on export
			Assert.That(css, Contains.Substring("font-size:" + (float)size / 1000 + "pt;"), "font size missing");
			if (fontFeatures != null)
			{
				Assert.That(css, Contains.Substring("font-feature-settings:" + fontFeatures));
			}
		}

		private static void VerifyExtraFontInfoInCss(int offset, FwSuperscriptVal superscript,
														  FwUnderlineType underline, Color underlineColor, string css)
		{
			switch (underline)
			{
				case (FwUnderlineType.kuntSingle):
					{
						Assert.That(css, Contains.Substring("text-decoration:underline;"), "underline not applied");
						Assert.That(css, Contains.Substring("text-decoration-color:" + HtmlColor.FromRgb(underlineColor.R, underlineColor.G, underlineColor.B)),
										"underline color missing");
						break;
					}
				case (FwUnderlineType.kuntDashed):
					{
						Assert.That(css, Contains.Substring("border-bottom:1px dashed"), "dashed underline not applied");
						Assert.That(css, Contains.Substring("border-bottom-color:" + HtmlColor.FromRgb(underlineColor.R, underlineColor.G, underlineColor.B)),
										"underline color missing");
						break;
					}
				case (FwUnderlineType.kuntDotted):
					{
						Assert.That(css, Contains.Substring("border-bottom:1px dotted"), "dotted underline not applied");
						Assert.That(css, Contains.Substring("border-bottom-color:" + HtmlColor.FromRgb(underlineColor.R, underlineColor.G, underlineColor.B)),
										"underline color missing");
						break;
					}
				case (FwUnderlineType.kuntNone):
					{
						Assert.That(css, Does.Not.Contain("border-bottom:"), "underline should not have been applied");
						Assert.That(css, Does.Not.Contain("text-decoration:underline"), "underline should not have been applied");
						break;
					}
				case (FwUnderlineType.kuntStrikethrough):
					{
						Assert.That(css, Contains.Substring("text-decoration:line-through;"), "strike through not applied");
						Assert.That(css, Contains.Substring("text-decoration-color:" + HtmlColor.FromRgb(underlineColor.R, underlineColor.G, underlineColor.B)),
										"strike through color missing");
						break;
					}
				case (FwUnderlineType.kuntDouble):
					{
						Assert.That(css, Contains.Substring("border-bottom:1px solid"), "double underline not applied");
						Assert.That(css, Contains.Substring("text-decoration:underline;"), "double underline not applied");
						Assert.That(css, Contains.Substring("text-decoration-color:" + HtmlColor.FromRgb(underlineColor.R, underlineColor.G, underlineColor.B)),
										"underline color missing");
						Assert.That(css, Contains.Substring("border-bottom-color:" + HtmlColor.FromRgb(underlineColor.R, underlineColor.G, underlineColor.B)),
										"underline color missing");
						break;
					}
				default:
					Assert.Fail("Um, I don't know how to do that yet");
					break;
			}
			if (offset != 0)
			{
				Assert.That(css, Contains.Substring("position:relative;"), "offset was not applied");
				// Offsets are converted into pt values on export
				Assert.That(css, Contains.Substring("bottom:" + offset / 1000 + ";"), "offset was not applied");
			}
			switch (superscript)
			{
				case (FwSuperscriptVal.kssvSub):
					{
						Assert.That(css, Contains.Substring("font-size:58%"), "subscript did not affect size");
						Assert.That(css, Contains.Substring("position:relative;"), "subscript was not applied");
						Assert.That(css, Contains.Substring("top:0.3em;"), "subscript was not applied");
						break;
					}
				case (FwSuperscriptVal.kssvSuper):
					{
						Assert.That(css, Contains.Substring("font-size:58%"), "superscript did not affect size");
						Assert.That(css, Contains.Substring("position:relative;"), "superscript was not applied");
						Assert.That(css, Contains.Substring("top:-0.6em;"), "superscript was not applied");
						break;
					}
				case (FwSuperscriptVal.kssvOff):
					{
						//superscript and subscript are disabled either by having the value of vertical-align:initial, or by having no vertical-align at all.
						if (css.Contains("vertical-align"))
						{
							Assert.That(css, Contains.Substring("vertical-align:initial;"), "superscript was not disabled");
						}
						break;
					}
			}
		}

		private static void VerifyParagraphBorderInCss(Color color, int leading, int trailing, int bottom, int top, string css)
		{
			Assert.That(css, Contains.Substring("border-color:" + HtmlColor.FromRgb(color.R, color.G, color.B)));
			// border widths are converted into pt values on export
			Assert.That(css, Contains.Substring("border-top-width:" + top / 1000 + "pt"));
			Assert.That(css, Contains.Substring("border-bottom-width:" + bottom / 1000 + "pt"));
			Assert.That(css, Contains.Substring("border-left-width:" + leading / 1000 + "pt"));
			Assert.That(css, Contains.Substring("border-right-width:" + trailing / 1000 + "pt"));
		}

		public static void VerifyRegex(string input, string pattern, string message = null, RegexOptions options = RegexOptions.Singleline | RegexOptions.Multiline)
		{
			Assert.IsTrue(Regex.Match(input, pattern, options).Success,
				string.Format("{3}Expected{0}{1}{0}but got{0}{2}", Environment.NewLine, pattern, input,
					message == null ? string.Empty : message + Environment.NewLine));
		}

		#endregion // Test Helper Methods

	}

	internal class TestStyle : BaseStyleInfo
	{
		public TestStyle(FontInfo defaultFontInfo, LcmCache cache)
			: base(cache)
		{
			m_defaultFontInfo = defaultFontInfo;
		}

		public TestStyle(InheritableStyleProp<BulletInfo> defaultBulletFontInfo, LcmCache cache)
			: base(cache)
		{
			m_bulletInfo = defaultBulletFontInfo;
		}

		public void SetWsStyle(FontInfo fontInfo, int wsId)
		{
			m_fontInfoOverrides[wsId] = fontInfo;
		}

		public void SetDefaultFontInfo(FontInfo info)
		{
			m_defaultFontInfo = info;
		}

		/// <summary>
		/// Sets the based on style and resets all properties to inherited.
		/// </summary>
		/// <param name="parent"></param>
		public void SetBasedOnStyle(BaseStyleInfo parent)
		{
			m_basedOnStyle = parent;
			SetAllPropertiesToInherited();
		}
	}
}