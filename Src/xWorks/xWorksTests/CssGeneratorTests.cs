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
using Palaso.TestUtilities;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.Utils;
using XCore;

// ReSharper disable InconsistentNaming - Justification: Underscores are standard for test names but nowhere else in our code
namespace SIL.FieldWorks.XWorks
{
	public class CssGeneratorTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private Mediator m_mediator;
		private FwStyleSheet m_styleSheet;
		private MockFwXApp m_application;
		private string m_configFilePath;
		private MockFwXWindow m_window;
		private readonly Color FontColor = Color.Blue;
		private readonly Color FontBGColor = Color.Green;
		private readonly string FontName = "foofoo";
		private readonly Color BorderColor = Color.Red;
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

		[TestFixtureSetUp]
		protected void Init()
		{
			FwRegistrySettings.Init();
			m_application = new MockFwXApp(new MockFwManager { Cache = Cache }, null, null);
			m_configFilePath = Path.Combine(FwDirectoryFinder.CodeDirectory, m_application.DefaultConfigurationPathname);
			m_window = new MockFwXWindow(m_application, m_configFilePath);
			m_window.Init(Cache); // initializes Mediator values
			m_mediator = m_window.Mediator;
			m_window.LoadUI(m_configFilePath); // actually loads UI here; needed for non-null stylesheet

			m_styleSheet = FontHeightAdjuster.StyleSheetFromMediator(m_mediator);
			m_owningTable = new StyleInfoTable("AbbySomebody", (IWritingSystemManager)Cache.WritingSystemFactory);
		}

		[TestFixtureTearDown]
		protected void TearDown()
		{
			m_application.Dispose();
			m_mediator.Dispose();
			FwRegistrySettings.Release();
		}

		[SetUp]
		public void ResetAssemblyFile()
		{
			ConfiguredXHTMLGenerator.AssemblyFile = "FDO";
			if (!m_styleSheet.Styles.Contains("FooStyle"))
			{
				GenerateStyle("FooStyle");
			}
		}

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
				checkList = ((DictionaryNodeListOptions) options).Options;
			}
			else if (options is DictionaryNodeWritingSystemOptions)
			{
				checkList = ((DictionaryNodeWritingSystemOptions) options).Options;
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

		[Test]
		public void GenerateCssForConfiguration_NullModelThrowsNullArgument()
		{
			Assert.Throws(typeof(ArgumentNullException), () => CssGenerator.GenerateCssFromConfiguration(null, m_mediator));
		}

		[Test]
		public void GenerateCssForConfiguration_SimpleConfigurationGeneratesLetHead()
		{
			//SUT
			var cssResult = CssGenerator.GenerateLetterHeaderCss(m_mediator);
			// verify that the css result contains .letHead similar to: .letHead {-moz-column-count:1;-webkit-column-count:1;column-count:1;clear:both;text-align:center;width:100%;}
			Assert.IsTrue(Regex.Match(cssResult, @"\.letHead\s*{\s*-moz-column-count:1;\s*-webkit-column-count:1;\s*column-count:1;\s*clear:both;\s*text-align:center;\s*width:100%;").Success,
							  "Css did not generate LetHead rules match");
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
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
			// verify that the css result contains a line similar to: .lexentry {clear:both;white-space:pre;}
			Assert.IsTrue(Regex.Match(cssResult, @"\.lexentry\s*{\s*clear:both;\s*white-space:pre-wrap;").Success,
							  "Css for root node(lexentry) did not generate 'clear' and 'white-space' rules match");
			// verify that the css result contains a line similar to: .lexentry .headword {
			Assert.IsTrue(Regex.Match(cssResult, @"\.lexentry>\s*\.mainheadword>\s*span\s*{.*").Success,
							  "Css for child node(headword) did not generate a specific match");
		}

		[Test]
		public void GenerateCssForConfiguration_SharedConfigurationGeneratesValidCss()
		{
			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLHeadWord",
				CSSClassNameOverride = "mainheadword",
				Style = "FooStyle",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "fr" })
			};
			var sharedNode = new ConfigurableDictionaryNode
			{
				Label = "SharedSubentries",
				Children = new List<ConfigurableDictionaryNode> { headwordNode },
				FieldDescription = "Subentries",
				CSSClassNameOverride = "sharedsubentries"
			};
			var subentriesNode = new ConfigurableDictionaryNode { FieldDescription = "Subentries", ReferenceItem = "SharedSubentries" };
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { subentriesNode }
			};
			var model = DictionaryConfigurationModelTests.CreateSimpleSharingModel(mainEntryNode, sharedNode);
			PopulateFieldsForTesting(model);

			//SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
			// verify that the css result contains a line similar to: .sharedsubentries .sharedsubentry .headword span{
			Assert.IsTrue(Regex.Match(cssResult, @"\.sharedsubentries\s*\.sharedsubentry>\s*\.mainheadword>\s*span\s*{.*").Success,
				"Css for child node(headword) did not generate a match{0}{1}", Environment.NewLine, cssResult);
		}

		[Test]
		public void GenerateCssForConfiguration_LinksLookLikePlainText()
		{
			var mainEntryNode = new ConfigurableDictionaryNode { FieldDescription = "LexEntry" };
			PopulateFieldsForTesting(mainEntryNode);

			var model = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { mainEntryNode } };
			//SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
			// verify that the css result contains a line similar to a { text-decoration:inherit; color:inherit; }
			Assert.IsTrue(Regex.Match(cssResult, @"\s*a\s*{[^}]*text-decoration:inherit;").Success, "Links should inherit underlines and similar.");
			Assert.IsTrue(Regex.Match(cssResult, @"\s*a\s*{[^}]*color:inherit;").Success, "Links should inherit color.");
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
			GenerateEmptyPseudoStyle(CssGenerator.BeforeAfterBetweenStyleName);
			var model = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { mainEntryNode } };
			//SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
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
			GenerateEmptyPseudoStyle(CssGenerator.BeforeAfterBetweenStyleName);
			var model = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { mainEntryNode } };
			//SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
			// Check result for before, between and after rules
			Assert.IsTrue(Regex.Match(cssResult, @"\.mainheadword>\s*span\s*:\s*first-child:before\s*{\s*content\s*:\s*'\\\s*'beforeText\\'\s*';\s*}").Success,
							  "css before rule with 'beforeText' content not found on headword");
			Assert.IsTrue(Regex.Match(cssResult, @"\.mainheadword>\s*.mainheadwor\s*\+\s*.mainheadwor:before\s*{\s*content\s*:\s*'\\\s*'betweenText\\'\s*';\s*}").Success,
							  "css before rule with 'betweenText' content not found on headword");
			Assert.IsTrue(Regex.Match(cssResult, @"\.mainheadword>\s*span\s*:\s*last-child:after\s*{\s*content\s*:\s*'\\\s*'afterText\\'\s*';\s*}").Success,
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
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { groupingNode },
				FieldDescription = "LexEntry"
			};
			PopulateFieldsForTesting(mainEntryNode);
			GenerateEmptyPseudoStyle(CssGenerator.BeforeAfterBetweenStyleName);
			var model = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { mainEntryNode } };
			//SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
			// Check the result for before and after rules for the group
			Assert.IsTrue(Regex.Match(cssResult, @"\.grouping_hwg\s*:before\s*{\s*content\s*:\s*'{';\s*}").Success,
							  "css before rule for the grouping node was not generated");
			Assert.IsTrue(Regex.Match(cssResult, @"\.grouping_hwg\s*:after\s*{\s*content\s*:\s*'}';\s*}").Success,
							  "css after rule for the grouping node was not generated");
			// Check result for before and after rules equivalent to .headword span:first-child{content:'Z';} and .headword span:last-child{content:'A'}
			Assert.IsTrue(Regex.Match(cssResult, @"\.grouping_hwg>\s*\.mh>\s*span\s*:\s*first-child:before\s*{\s*content\s*:\s*'Z';\s*}").Success,
							  "css before rule with Z content not found on headword");
			Assert.IsTrue(Regex.Match(cssResult, @"\.grouping_hwg>\s*\.mh>\s*span\s*:\s*last-child:after\s*{\s*content\s*:\s*'A';\s*}").Success,
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
				DictionaryNodeOptions = new DictionaryNodeGroupingOptions { DisplayGroupInParagraph = true }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { groupingNode },
				FieldDescription = "LexEntry"
			};
			PopulateFieldsForTesting(mainEntryNode);
			GenerateEmptyPseudoStyle(CssGenerator.BeforeAfterBetweenStyleName);
			var model = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { mainEntryNode } };
			//SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
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
			GenerateEmptyPseudoStyle(CssGenerator.BeforeAfterBetweenStyleName);
			var model = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { mainEntryNode } };
			//SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
			// Check result for between rule equivalent to lexentry> .mainheadword> span.writingsystemprefix + span:not(:last-child):after{content:' ';}
			Assert.IsTrue(Regex.Match(cssResult, @".*\.lexentry\s*>\s*\.mainheadword>\s*span\.writingsystemprefix\s*\~\s*span\.writingsystemprefix:before{.*content:' ';.*}", RegexOptions.Singleline).Success,
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
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { subentryNode },
				FieldDescription = "LexEntry"
			};
			PopulateFieldsForTesting(mainEntryNode);
			GenerateEmptyPseudoStyle(CssGenerator.BeforeAfterBetweenStyleName);
			var model = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { mainEntryNode } };
			//SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
			// Check result for before and after rules equivalent to .subentries .subentry .headword span:first-child{content:'Z';} and .headword span:last-child{content:'A'}
			Assert.IsTrue(Regex.Match(cssResult, @"\.subentries\s*\.subentry>\s*\.headword>\s*span\s*:\s*first-child:before\s*{\s*content\s*:\s*'Z';\s*}").Success,
							  "css before rule with Z content not found on headword");
			Assert.IsTrue(Regex.Match(cssResult, @"\.subentries\s*\.subentry>\s\.headword>\s*span\s*:\s*last-child:after\s*{\s*content\s*:\s*'A';\s*}").Success,
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
			GeneratePseudoStyle(CssGenerator.BeforeAfterBetweenStyleName);
			var model = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { mainEntryNode } };
			//SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
			// Check result for before and after rules equivalent to .headword span:first-child{content:'Z';font-size:10pt;color:#00F;}
			// and .headword span:last-child{content:'A';font-size:10pt;color:#00F;}
			Assert.IsTrue(Regex.Match(cssResult, @"\.mainheadword>\s*span\s*:\s*first-child:before\s*{\s*content\s*:\s*'Z';\s*font-size\s*:\s*10pt;\s*color\s*:\s*#00F;\s*}").Success,
							  "css before rule with Z content with css format not found on headword");
			Assert.IsTrue(Regex.Match(cssResult, @"\.mainheadword>\s*span\s*:\s*last-child:after\s*{\s*content\s*:\s*'A';\s*font-size\s*:\s*10pt;\s*color\s*:\s*#00F;\s*}").Success,
							  "css after rule with A content with css format not found on headword");
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
			GeneratePseudoStyle(CssGenerator.BeforeAfterBetweenStyleName);
			var model = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { mainEntryNode } };
			//SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
			Assert.That(cssResult, Contains.Substring(".lexentry> .senses:after"));
			Assert.That(cssResult, Is.Not.StringContaining(".lexentry> .senses .sense:after"));
			Assert.That(cssResult, Is.Not.StringContaining(".lexentry> .senses .sense:last-child:after"));
		}

		[Test]
		public void GenerateCssForStyleName_CharacterStyleWorks()
		{
			GenerateStyle("Dictionary-Vernacular");
			//SUT
			var styleDeclaration = CssGenerator.GenerateCssStyleFromFwStyleSheet("Dictionary-Vernacular", CssGenerator.DefaultStyle, m_mediator);
			VerifyFontInfoInCss(FontColor, FontBGColor, FontName, FontBold, FontItalic, FontSize, styleDeclaration.ToString());
		}

		[Test]
		public void GenerateCssForStyleName_ParagraphBorderWorks()
		{
			GenerateParagraphStyle("Dictionary-Paragraph");
			//SUT
			var styleDeclaration = CssGenerator.GenerateCssStyleFromFwStyleSheet("Dictionary-Paragraph", CssGenerator.DefaultStyle, m_mediator);
			//border leading omitted from paragraph style definition which should result in 0pt left width
			VerifyParagraphBorderInCss(BorderColor, 0, BorderTrailing, BorderBottom, BorderTop, styleDeclaration.ToString());
		}

		[Test]
		public void GenerateCssForStyleName_ParagraphMarginIsAbsolute_NoParent_Works()
		{
			GenerateParagraphStyle("Dictionary-Paragraph-Padding");
			//SUT
			var styleDeclaration = CssGenerator.GenerateCssStyleFromFwStyleSheet("Dictionary-Paragraph-Padding", CssGenerator.DefaultStyle, null, m_mediator);
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
			var styleDeclaration = CssGenerator.GenerateCssStyleFromFwStyleSheet("ParagraphMarginAbsoluteParentOverrideChild", CssGenerator.DefaultStyle, senses, m_mediator);
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
			var styleDeclaration = CssGenerator.GenerateCssStyleFromFwStyleSheet("ParagraphMarginAbsoluteParentOverrideChild", CssGenerator.DefaultStyle, senses, m_mediator);
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
			var gpDeclaration = CssGenerator.GenerateCssStyleFromFwStyleSheet(grandParentStyle.Name, CssGenerator.DefaultStyle, entry, m_mediator);
			Assert.That(gpDeclaration.ToString(), Contains.Substring("margin-left:5pt"), "Grandparent margin incorrectly generated");
			var parentDeclaration = CssGenerator.GenerateCssStyleFromFwStyleSheet(parentStyle.Name, CssGenerator.DefaultStyle, senses, m_mediator);
			Assert.That(parentDeclaration.ToString(), Contains.Substring("margin-left:7pt"), "Parent margin incorrectly generated");
			var childDeclaration = CssGenerator.GenerateCssStyleFromFwStyleSheet(childStyle.Name, CssGenerator.DefaultStyle, subSenses, m_mediator);
			Assert.That(childDeclaration.ToString(), Contains.Substring("margin-left:8pt"), "Child margin incorrectly generated");
		}

		[Test]
		public void GenerateCssForStyleName_HangingIndentWithExistingMargin_NoParentWorks()
		{
			var hangingIndent = -15 * 1000;
			var testStyle = GenerateParagraphStyle("Dictionary-Paragraph-Padding-Hanging");
			testStyle.SetExplicitParaIntProp((int)FwTextPropType.ktptFirstIndent, 0, hangingIndent);
			//SUT
			var styleDeclaration = CssGenerator.GenerateCssStyleFromFwStyleSheet("Dictionary-Paragraph-Padding-Hanging", CssGenerator.DefaultStyle, m_mediator);
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
			var parentDeclaration = CssGenerator.GenerateCssStyleFromFwStyleSheet(parentStyle.Name, CssGenerator.DefaultStyle, entry, m_mediator);
			var childDeclaration = CssGenerator.GenerateCssStyleFromFwStyleSheet(childStyleName, CssGenerator.DefaultStyle, senses, m_mediator);
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

			var grandChildDeclaration = CssGenerator.GenerateCssStyleFromFwStyleSheet(grandChildStyleName, CssGenerator.DefaultStyle, examples, m_mediator, true);

			Assert.AreEqual(2, grandChildDeclaration.Count);
			// Indent values are converted into pt values on export
			var firstSenseChildCss = grandChildDeclaration[0].ToString();
			var allOtherSenseChildrenCss = grandChildDeclaration[1].ToString();
			Assert.That(firstSenseChildCss, Is.Not.StringMatching(allOtherSenseChildrenCss));
			var firstSenseIndent = parentHangingIndent - grandChildHangingIndent;
			var otherSenseIndent = childHangingIndent - grandChildHangingIndent;
			Assert.That(firstSenseChildCss, Contains.Substring("margin-left:" + firstSenseIndent / 1000 + "pt"));
			Assert.That(firstSenseChildCss, Contains.Substring("text-indent:" + grandChildHangingIndent / 1000 + "pt"));
			Assert.That(allOtherSenseChildrenCss, Contains.Substring("margin-left:" + otherSenseIndent / 1000 + "pt"));
			Assert.That(allOtherSenseChildrenCss, Contains.Substring("text-indent:" + grandChildHangingIndent / 1000 + "pt"));
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
			var childDeclaration = CssGenerator.GenerateCssStyleFromFwStyleSheet(childStyle.Name, CssGenerator.DefaultStyle, headword, m_mediator);
			Assert.That(childDeclaration.ToString(), Is.Not.StringContaining("margin-left"));
		}

		[Test]
		public void GenerateCssForStyleName_HangingIndentWithNoMarginWorks()
		{
			var hangingIndent = -15 * 1000;
			var testStyle = GenerateEmptyParagraphStyle("Dictionary-Paragraph-Hanging-No-Padding");
			testStyle.SetExplicitParaIntProp((int)FwTextPropType.ktptFirstIndent, 0, hangingIndent);
			//SUT
			var styleDeclaration = CssGenerator.GenerateCssStyleFromFwStyleSheet("Dictionary-Paragraph-Hanging-No-Padding", CssGenerator.DefaultStyle, m_mediator);
			// Indent values are converted into pt values on export
			Assert.That(styleDeclaration.ToString(), Contains.Substring("margin-left:" + -hangingIndent / 1000 + "pt"));
			Assert.That(styleDeclaration.ToString(), Contains.Substring("text-indent:" + hangingIndent / 1000 + "pt"));
		}

		[Test]
		public void GenerateCssForStyleName_ParagraphAlignmentWorks()
		{
			GenerateParagraphStyle("Dictionary-Paragraph-Justify");
			//SUT
			var styleDeclaration = CssGenerator.GenerateCssStyleFromFwStyleSheet("Dictionary-Paragraph-Justify", CssGenerator.DefaultStyle, m_mediator);
			Assert.That(styleDeclaration.ToString(), Contains.Substring("align:" + ParagraphAlignment.AsCssString()));
		}

		[Test]
		public void GenerateCssForStyleName_ParagraphRelativeLineSpacingWorks()
		{
			GenerateParagraphStyle("Dictionary-Paragraph-RelativeLine");
			//SUT
			var styleDeclaration = CssGenerator.GenerateCssStyleFromFwStyleSheet("Dictionary-Paragraph-RelativeLine", CssGenerator.DefaultStyle, m_mediator);
			Assert.That(styleDeclaration.ToString(), Contains.Substring("line-height:" + CssDoubleSpace + ";"));
		}

		[Test]
		public void GenerateCssForStyleName_ParagraphAbsoluteLineSpacingWorks()
		{
			var style = GenerateParagraphStyle("Dictionary-Paragraph-Absolute");
			style.SetExplicitParaIntProp((int)FwTextPropType.ktptLineHeight, (int)FwTextPropVar.ktpvMilliPoint, 9 * 1000);
			//SUT
			var styleDeclaration = CssGenerator.GenerateCssStyleFromFwStyleSheet("Dictionary-Paragraph-Absolute", CssGenerator.DefaultStyle, m_mediator);
			Assert.That(styleDeclaration.ToString(), Contains.Substring("line-height:9pt;"));
		}

		[Test]
		public void GenerateCssForConfiguration_ConfigWithCharStyleWorks()
		{
			ConfiguredXHTMLGenerator.AssemblyFile = "xWorksTests";
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
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
			VerifyFontInfoInCss(FontColor, FontBGColor, FontName, true, true, FontSize, cssResult);
		}

		[Test]
		public void GenerateCssForConfiguration_CharStyleWsOverrideWorks()
		{
			ConfiguredXHTMLGenerator.AssemblyFile = "xWorksTests";
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
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
			//make sure that fontinfo with the french overrides is present
			VerifyFontInfoInCss(FontColor, FontBGColor, "french", true, false, FontSize, cssResult);
			//make sure that the default options are also present
			VerifyFontInfoInCss(FontColor, FontBGColor, FontName, true, true, FontSize, cssResult);
		}

		[Test]
		public void GenerateCssForConfiguration_ConfigWithParaStyleWorks()
		{
			ConfiguredXHTMLGenerator.AssemblyFile = "xWorksTests";
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
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
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
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
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
			var cssResult = CssGenerator.GenerateCssStyleFromFwStyleSheet("Child", CssGenerator.DefaultStyle, m_mediator);
			// The css should have the overridden border color, but report all other values as the parent style
			//border leading omitted from paragraph style definition which should result in 0pt left width
			VerifyParagraphBorderInCss(Color.HotPink, 0, BorderTrailing, BorderBottom, BorderTop, cssResult.ToString());
		}

		[Test]
		public void GenerateCssForStyleName_CharStyleUnsetValuesAreNotExported()
		{
			GenerateEmptyStyle("EmptyChar");
			var cssResult = CssGenerator.GenerateCssStyleFromFwStyleSheet("EmptyChar", CssGenerator.DefaultStyle, m_mediator);
			Assert.AreEqual(cssResult.ToString().Trim(), String.Empty);
		}

		[Test]
		public void GenerateCssForStyleName_DefaultVernMagicConfigResultsInRealLanguageCss()
		{
			ConfiguredXHTMLGenerator.AssemblyFile = "xWorksTests";
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
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
			//Verify that vernacular was converted into french to match the vernholder node
			Assert.That(cssResult, Contains.Substring(".vernholder> span[lang|=\"fr\"]"));
		}

		[Test]
		public void GenerateCssForStyleName_DefaultAnalysisMagicConfigResultsInRealLanguageCss()
		{
			ConfiguredXHTMLGenerator.AssemblyFile = "xWorksTests";
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
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
			//Verify that analysis was converted into english to match the analyholder node
			Assert.That(cssResult, Contains.Substring(".analyholder> span[lang|=\"en\"]"));
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
			entry.CitationForm.set_String(wsFr, Cache.TsStrFactory.MakeString("homme", wsFr));
			//SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
			Assert.That(cssResult, Is.Not.StringContaining(".lexentry"));
			Assert.That(cssResult, Contains.Substring(".bolo"));

			var xhtmResult = new StringBuilder();
			using (var XHTMLWriter = XmlWriter.Create(xhtmResult))
			{
				XHTMLWriter.WriteStartElement("body");
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, false, false, null);
				var content = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entry, testNode, null, settings);
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
			entry.CitationForm.set_String(wsFr, Cache.TsStrFactory.MakeString("HeadWordTest", wsFr));
			//SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
			Assert.That(cssResult, Is.Not.StringContaining(".headword"));
			Assert.That(cssResult, Contains.Substring(".tailwind"));

			var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, false, false, null);
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entry, testParentNode, null, settings);
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
			sense.Gloss.set_String(wsEn, Cache.TsStrFactory.MakeString("gloss", wsEn));
			//SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
			Assert.That(cssResult, Contains.Substring(".lexentry> .senses .sense> .gloss"));

			var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, false, false, null);
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entry, testEntryNode, null, settings);
			const string positiveTest = "/*[@class='lexentry']/span[@class='senses']/span[@class='sense']/span[@class='gloss']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(positiveTest, 1);
		}

		[Test]
		public void GenerateCssForConfiguration_CharStyleSubscriptWorks()
		{
			ConfiguredXHTMLGenerator.AssemblyFile = "xWorksTests";
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
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
			//make sure that fontinfo with the subscript overrides made it into css
			VerifyExtraFontInfoInCss(0, FwSuperscriptVal.kssvSub, FwUnderlineType.kuntNone, Color.Black, cssResult);
			Assert.IsTrue(Regex.Match(cssResult, @".*\.sil*\.fieldworks.xworks.testrootclass>\s*span\[lang|='fr']\{.*position\:relative\*top\:-0.2em.*", RegexOptions.Singleline).Success,
				  "Subscript's positiion not generated properly");
		}

		[Test]
		public void GenerateCssForConfiguration_CharStyleSuperscriptWorks()
		{
			ConfiguredXHTMLGenerator.AssemblyFile = "xWorksTests";
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
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
			//make sure that fontinfo with the superscript overrides made it into css
			VerifyExtraFontInfoInCss(0, FwSuperscriptVal.kssvSuper, FwUnderlineType.kuntNone, Color.Black, cssResult);
			Assert.IsTrue(Regex.Match(cssResult, @".*\.sil*\.fieldworks.xworks.testrootclass>\s*span\[lang|='fr']\{.*position\:relative\*top\:\0.2em.*", RegexOptions.Singleline).Success,
				  "Superscript's positiion not generated properly");
		}

		[Test]
		public void GenerateCssForConfiguration_CharStyleBasicUnderlineWorks()
		{
			ConfiguredXHTMLGenerator.AssemblyFile = "xWorksTests";
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
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
			//make sure that fontinfo with the underline overrides made it into css
			VerifyExtraFontInfoInCss(0, FwSuperscriptVal.kssvOff, FwUnderlineType.kuntSingle, Color.HotPink, cssResult);
		}

		[Test]
		public void GenerateCssForConfiguration_CharStyleDoubleUnderlineWorks()
		{
			ConfiguredXHTMLGenerator.AssemblyFile = "xWorksTests";
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
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
			//make sure that fontinfo with the underline overrides made it into css
			VerifyExtraFontInfoInCss(0, FwSuperscriptVal.kssvOff, FwUnderlineType.kuntDouble, Color.Khaki, cssResult);
		}

		[Test]
		public void GenerateCssForConfiguration_CharStyleDashedUnderlineWorks()
		{
			ConfiguredXHTMLGenerator.AssemblyFile = "xWorksTests";
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
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
			//make sure that fontinfo with the underline overrides made it into css
			VerifyExtraFontInfoInCss(0, FwSuperscriptVal.kssvOff, FwUnderlineType.kuntDashed, Color.Black, cssResult);
		}

		[Test]
		public void GenerateCssForConfiguration_CharStyleStrikethroughWorks()
		{
			ConfiguredXHTMLGenerator.AssemblyFile = "xWorksTests";
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
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
			//make sure that fontinfo with the underline overrides made it into css
			VerifyExtraFontInfoInCss(0, FwSuperscriptVal.kssvOff, FwUnderlineType.kuntStrikethrough, Color.Black, cssResult);
		}

		[Test]
		public void GenerateCssForConfiguration_CharStyleDottedUnderlineWorks()
		{
			ConfiguredXHTMLGenerator.AssemblyFile = "xWorksTests";
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
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
			//make sure that fontinfo with the underline overrides made it into css
			VerifyExtraFontInfoInCss(0, FwSuperscriptVal.kssvOff, FwUnderlineType.kuntDotted, Color.Black, cssResult);
		}

		[Test]
		public void GenerateCssForConfiguration_CharStyleDisableSuperWorks()
		{
			ConfiguredXHTMLGenerator.AssemblyFile = "xWorksTests";
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
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
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
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
			Assert.That(cssResult, Contains.Substring(".lexentry> .senses .sense> .morphosyntaxanalysisra> .mlpartofspeech"));
			Assert.That(cssResult, Contains.Substring(".lexentry> .senses .sense> .morphosyntaxanalysisra> .mlinflectionclass"));
			Assert.That(cssResult, Contains.Substring(".lexentry> .senses .sense> .morphosyntaxanalysisra> .slots .slot> .name"));
			Assert.False(Regex.Match(cssResult, @"{\s*}").Success); // make sure we filter out empty rules
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
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
			Assert.That(cssResult, Contains.Substring(".lexentry> .variantformentrybackrefs .variantformentrybackref> .pronunciations .pronunciation> .form"));
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
				FieldDescription = "LookupComplexEntryType",
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
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
			Assert.That(cssResult, Contains.Substring(".lexentry> .subentries .subentry> .complexformtypes .complexformtype> .reverseabbr> span"));
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
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
			Assert.IsTrue(Regex.Match(cssResult, @".lexentry> .visiblecomplexformbackrefs> .complexformtypes .complexformtype> .name:before{.*content:'<';.*}",
				RegexOptions.Singleline).Success, "Before not generated.");
			Assert.IsTrue(Regex.Match(cssResult, @".lexentry> .visiblecomplexformbackrefs> .complexformtypes .complexformtype .name> .nam\+ .nam:before{.*content:',';.*}",
				RegexOptions.Singleline).Success, "Between not generated.");
			Assert.IsTrue(Regex.Match(cssResult, @".lexentry> .visiblecomplexformbackrefs> .complexformtypes .complexformtype> .name:after{.*content:'>';.*}",
				RegexOptions.Singleline).Success, "After not generated.");
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
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
			Assert.IsTrue(Regex.Match(cssResult, @".lexentry> .variantformentrybackrefs:before{.*content:'\[';.*}",
				RegexOptions.Singleline).Success, "Before not generated for Variant Entry.");
			Assert.IsTrue(Regex.Match(cssResult, @".lexentry .variantformentrybackrefs> .variantformentrybackref\+ .variantformentrybackref:before{.*content:'\; ';.*}",
				RegexOptions.Singleline).Success, "Between not generated Variant Entry.");
			Assert.IsTrue(Regex.Match(cssResult, @".lexentry> .variantformentrybackrefs:after{.*content:'\]';.*}",
				RegexOptions.Singleline).Success, "After not generated Variant Entry.");
			Assert.False(Regex.Match(cssResult, @".lexentry .variantformentrybackrefs> .span\+ .span:before").Success);
			Assert.IsTrue(Regex.Match(cssResult, @".lexentry> .variantformentrybackrefs .variantformentrybackref> .variantentrytypes .variantentrytype> .name:before{.*content:'<';.*}",
				RegexOptions.Singleline).Success, "Before not generated Variant Entry Type.");
			Assert.IsTrue(Regex.Match(cssResult, @".lexentry> .variantformentrybackrefs .variantformentrybackref> .variantentrytypes .variantentrytype .name> .nam\+ .nam:before{.*content:',';.*}",
				RegexOptions.Singleline).Success, "Between not generated Variant Entry Type.");
			Assert.IsTrue(Regex.Match(cssResult, @".lexentry> .variantformentrybackrefs .variantformentrybackref> .variantentrytypes .variantentrytype> .name:after{.*content:'>';.*}",
				RegexOptions.Singleline).Success, "After not generated Variant Entry Type.");
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
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
			Assert.That(cssResult, Contains.Substring(".lexentry> .senses .sense> .otherreferencedcomplexforms .otherreferencedcomplexform> .headword"));
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
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
			Assert.That(cssResult, Contains.Substring(".lexentry> .complexforms .complexform> .headword"));
			Assert.IsTrue(Regex.Match(cssResult, @"\.lexentry>\s*\.complexforms\s*\.complexform{.*display\s*:\s*block;.*}", RegexOptions.Singleline).Success);
		}

		[Test]
		public void GenerateCssForConfiguration_SenseSubEntriesHeadWord()
		{
			var form = new ConfigurableDictionaryNode { FieldDescription = "HeadWord", Style = "FooStyle"};
			var subentries = new ConfigurableDictionaryNode
			{
				FieldDescription = "Subentries",
				Children = new List<ConfigurableDictionaryNode> { form }
			};
			var senses = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "Senses",
				Children = new List<ConfigurableDictionaryNode> { subentries }
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
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
			Assert.That(cssResult, Contains.Substring(".lexentry> .senses .sense> .subentries .subentry> .headword"));
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
			//SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
			Assert.That(cssResult, Contains.Substring(".lexentry> .senses > .sensecontent > .sense> .gloss"));
			Assert.That(cssResult, Contains.Substring(".lexentry> .senses > .sensecontent > .sense> .morphosyntaxanalysisra"));
			Assert.IsTrue(Regex.Match(cssResult,
				@"\.lexentry>\s*\.senses\s*>\s*\.sharedgrammaticalinfo\s*>\s*\.morphosyntaxanalysisra\s*{.*font-family\s*:\s*'foofoo'\,serif.*}",
				RegexOptions.Singleline).Success, "Style for sharedgrammaticalinfo not placed correctly");
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
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
			Assert.IsTrue(Regex.Match(cssResult,
				@"\.entry>\s*\.senses>\s*span.sensecontent\+\s*span\:before\{\s*content\:\'\*\'\;",
				RegexOptions.Singleline).Success, "Between Material for Senses not placed correctly");
		}

		[Test]
		public void GenerateCssForConfiguration_WritingSystemAudioWorks()
		{
			IWritingSystem wsEnAudio;
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
			GenerateEmptyPseudoStyle(CssGenerator.BeforeAfterBetweenStyleName);
			var model = new DictionaryConfigurationModel();
			model.Parts = new List<ConfigurableDictionaryNode> { entry };
			PopulateFieldsForTesting(entry);
			// SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
			Assert.That(cssResult, Contains.Substring(".lexentry> .lexemeformoa> span[lang|=\"en-Zxxx-x-audio\"]{"));
			Assert.IsTrue(Regex.Match(cssResult, @"a.en-Zxxx-x-audio{.*text-decoration:none;.*}", RegexOptions.Singleline).Success,
							  "Audio not generated.");
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
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
			Assert.That(cssResult, Contains.Substring(".lexentry> .senses > .sensecontent > .sense> .gloss"));
			Assert.IsTrue(Regex.Match(cssResult, @"\.lexentry>\s*\.senses\s*>\s*\.sensecontent(\s*\+\s*\.sensecontent)?\s*{.*display\s*:\s*block;.*}", RegexOptions.Singleline).Success);
			Assert.False(Regex.Match(cssResult, @"{\s*}").Success); // make sure we filter out empty rules
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
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
			Assert.IsTrue(Regex.Match(cssResult, @"\.lexentry>\s*\.senses\s*>\s*\.sensecontent\s*>\s*\.sense>\s*\.examples\s*\.example\s*{.*display\s*:\s*block;.*}", RegexOptions.Singleline).Success);
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
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
			Assert.IsFalse(Regex.Match(cssResult, @"\.lexentry>\s*\.senses\s*\.sense>\s*\.examples\s*\.example\s*{.*display\s*:\s*block;.*}", RegexOptions.Singleline).Success);
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
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
			Assert.That(cssResult, Contains.Substring(".lexentry> .senses > .sensecontent > .sense> .gloss"));
			Assert.IsTrue(Regex.Match(cssResult, @"\.lexentry>\s*\.senses\s*>\s*\.sensecontent\s*\+\s*\.sensecontent\s*{.*display\s*:\s*block;.*}", RegexOptions.Singleline).Success);
			Assert.IsTrue(Regex.Match(cssResult, @"\.lexentry>\s*\.senses\s*>\s*\.sensecontent\s*>\s*\.sense\s*{.*font-style\s*:\s*italic;.*}", RegexOptions.Singleline).Success);
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
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
			Assert.That(cssResult, Contains.Substring(".lexentry> .senses > .sensecontent > .sense> .gloss"));
			Assert.IsTrue(Regex.Match(cssResult, @"\.lexentry>\s*\.senses\s*>\s*\.sensecontent\s*{.*display\s*:\s*block;.*}", RegexOptions.Singleline).Success);
			Assert.IsTrue(Regex.Match(cssResult, @"\.lexentry>\s*\.senses\s*>\s*\.sensecontent\s*>\s*\.sense\s*{.*font-style\s*:\s*italic;.*}", RegexOptions.Singleline).Success);
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
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
			Assert.IsTrue(Regex.Match(cssResult, @".*\.lexentry>\s*\.senses\s*>\s*\.sensecontent\s*\.sensenumber", RegexOptions.Singleline).Success,
							  "sense number style selector was not generated.");
			VerifyFontInfoInCss(FontColor, FontBGColor, FontName, FontBold, FontItalic, FontSize, cssResult);
		}

		[Test]
		public void GenerateCssForConfiguration_ReversalSenseNumberWorks()
		{
			GenerateStyle("Dictionary-RevSenseNum");
			var gloss = new ConfigurableDictionaryNode { FieldDescription = "Gloss", Style = "FooStyle" };
			var senses = new ConfigurableDictionaryNode
			{
				FieldDescription = "ReferringSenses",
				CSSClassNameOverride = "ReferringSenses",
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
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
			Assert.That(cssResult, Contains.Substring(".reversalindexentry> .referringsenses > .sensecontent > .referringsense> .gloss"));
			Assert.IsTrue(Regex.Match(cssResult, @"\.reversalindexentry>\s*\.referringsenses\s*>\s*\.sensecontent\s*\.sensenumber\s*{.*font-style\s*:\s*italic;.*}", RegexOptions.Singleline).Success);
			Assert.False(Regex.Match(cssResult, @"{\s*}").Success); // make sure we filter out empty rules
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
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
			Assert.IsTrue(Regex.Match(cssResult, @".*\.lexentry>\s*\.senses\s*>\s*\.sensecontent\s*\.sensenumber:before{.*content:'\['.*}", RegexOptions.Singleline).Success,
							  "Before content not applied to the sense number selector.");
			Assert.IsTrue(Regex.Match(cssResult, @".*\.lexentry>\s*\.senses\s*>\s*\.sensecontent\s*\.sensenumber:after{.*content:'\]'.*}", RegexOptions.Singleline).Success,
							  "After content not applied to the sense number selector.");
		}

		[Test]
		public void GenerateCssForConfiguration_PrimaryEntryReferencesTypeBeforeAndAfterWork()
		{
			var entrytypes = new ConfigurableDictionaryNode
			{
				FieldDescription = "EntryTypes",
				CSSClassNameOverride = "entrytypes",
				Style = "FooStyle",
				Before = "*",
				After = "@"
			};
			var primaryentry = new ConfigurableDictionaryNode
			{
				FieldDescription = "EntryRefsWithThisMainSense",
				Style = "FooStyle",
				Before = "[",
				After = "]",
				Children = new List<ConfigurableDictionaryNode> { entrytypes }
			};
			var senses = new ConfigurableDictionaryNode
			{
				FieldDescription = "ReferringSenses",
				CSSClassNameOverride = "ReferringSenses",
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
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
			Assert.IsTrue(Regex.Match(cssResult, @"\.reversalindexentry>\s*\.referringsenses\s*\.referringsense\>\s*\.entryrefswiththismainsense\s*\.entryrefswiththismainsens\>\s*\.entrytypes\:before{.*content:'\*'.*}", RegexOptions.Singleline).Success);
			Assert.IsTrue(Regex.Match(cssResult, @"\.reversalindexentry>\s*\.referringsenses\s*\.referringsense\>\s*\.entryrefswiththismainsense\s*\.entryrefswiththismainsens\>\s*\.entrytypes\:after{.*content:'\@'.*}", RegexOptions.Singleline).Success);
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
			GenerateEmptyPseudoStyle(CssGenerator.BeforeAfterBetweenStyleName);
			var model = new DictionaryConfigurationModel();
			model.Parts = new List<ConfigurableDictionaryNode> { entry };
			PopulateFieldsForTesting(entry);
			// SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
			Assert.IsTrue(Regex.Match(cssResult, @".*\.lexentry\s*\.senses>\s*\.sense\s*\+\s*\.sense:before{.*content:','.*}", RegexOptions.Singleline).Success,
				"Between selector not generated.{0}{0}{1}", Environment.NewLine, cssResult);
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
			GenerateEmptyPseudoStyle(CssGenerator.BeforeAfterBetweenStyleName);
			var model = new DictionaryConfigurationModel();
			model.Parts = new List<ConfigurableDictionaryNode> { entry };
			PopulateFieldsForTesting(entry);
			// SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
			Assert.IsTrue(Regex.Match(cssResult, @".*\.lexentry>\s*\.lexemeform>\s*span\.writingsystemprefix\s*\~\s*span\.writingsystemprefix:before{.*content:','.*}", RegexOptions.Singleline).Success,
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
			GenerateEmptyPseudoStyle(CssGenerator.BeforeAfterBetweenStyleName);
			var model = new DictionaryConfigurationModel();
			model.Parts = new List<ConfigurableDictionaryNode> { entry };
			PopulateFieldsForTesting(entry);
			// SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
			Assert.IsTrue(Regex.Match(cssResult, @".*\.lexentry>\s*\.lexemeform>\s*span\[lang\|=\'fr\'\]:before{.*content:','.*}", RegexOptions.Singleline).Success,
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
			GenerateEmptyPseudoStyle(CssGenerator.BeforeAfterBetweenStyleName);
			var model = new DictionaryConfigurationModel();
			model.Parts = new List<ConfigurableDictionaryNode> { entry };
			PopulateFieldsForTesting(entry);
			// SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
			Assert.IsTrue(Regex.Match(cssResult, @".*\.lexentry>\s*\.lexemeform>\s*span\.writingsystemprefix\s*\~\s*span\.writingsystemprefix:before\s*{.*content:','.*}", RegexOptions.Singleline).Success,
							  "Between Multi-WritingSystem with Abbr selector not generated for LexemeForm.");
			Assert.IsTrue(Regex.Match(cssResult, @".*\.lexentry>\s*\.headword>\s*span\.writingsystemprefix\s*\~\s*span\.writingsystemprefix:before\s*{.*content:','.*}", RegexOptions.Singleline).Success,
							  "Between Multi-WritingSystem with Abbr selector not generated for HeadWord.");
			Assert.IsTrue(Regex.Match(cssResult, @".*\.lexentry>\s*\.lexemeform>\s*span\.writingsystemprefix:after\s*{.*content:' '.*}", RegexOptions.Singleline).Success,
							  "writingsystemprefix:after not generated for headword.");
			Assert.IsTrue(Regex.Match(cssResult, @".*\.lexentry>\s*\.headword>\s*span\.writingsystemprefix:after\s*{.*content:' '.*}", RegexOptions.Singleline).Success,
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
			GenerateEmptyPseudoStyle(CssGenerator.BeforeAfterBetweenStyleName);
			var model = new DictionaryConfigurationModel();
			model.Parts = new List<ConfigurableDictionaryNode> { entry };
			PopulateFieldsForTesting(entry);
			wsOpts.Options[1].IsEnabled = false; // uncheck French ws
			// SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
			Assert.IsFalse(Regex.Match(cssResult, @".*\.lexentry>\s*\.lexemeform>\s*span\.writingsystemprefix\s*\+\s*span:not\(:last-child\):after\s*{.*content:','.*}", RegexOptions.Singleline).Success,
							  "Between Multi-WritingSystem selector should not be generated for LexemeForm (only 1 ws checked).");
			Assert.IsFalse(Regex.Match(cssResult, @".*\.lexentry>\s*\.headword>\s*span\.writingsystemprefix\s*\+\s*span:not\(:last-child\):after\s*{.*content:','.*}", RegexOptions.Singleline).Success,
							  "Between Multi-WritingSystem selector should not be generated for HeadWord (only 1 ws checked).");
			Assert.IsTrue(Regex.Match(cssResult, @".*\.lexentry>\s*\.lexemeform>\s*span\.writingsystemprefix:after\s*{.*content:' '.*}", RegexOptions.Singleline).Success,
							  "writingsystemprefix:after not generated for headword.");
			Assert.IsTrue(Regex.Match(cssResult, @".*\.lexentry>\s*\.headword>\s*span\.writingsystemprefix:after\s*{.*content:' '.*}", RegexOptions.Singleline).Success,
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
			GeneratePseudoStyle(CssGenerator.BeforeAfterBetweenStyleName);
			var model = new DictionaryConfigurationModel();
			model.Parts = new List<ConfigurableDictionaryNode> { entry };
			PopulateFieldsForTesting(entry);
			// SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
			Assert.IsTrue(Regex.Match(cssResult, @".*\.lexentry\s*\.senses>\s*\.sense\s*\+\s*\.sense:before{.*content:',';.*font-size:10pt;.*color:#00F.*}", RegexOptions.Singleline).Success,
				 "Between selector with format not generated.{0}{0}{1}", Environment.NewLine, cssResult);
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
			Assert.That(classAttribute, Is.StringMatching("owningentry_headword"));
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
			Assert.That(classAttribute, Is.StringMatching("headword"));
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
			Assert.That(classAttribute, Is.StringMatching("lexentry"));
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
			Assert.That(classAttribute, Is.StringMatching("originalfield_dup"));
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
			Assert.That(classAttribute, Is.StringMatching("override_dup"));
		}

		/// <summary>
		/// The css for a picture is floated right and we want to clear the float at each entry.
		/// </summary>
		[Test]
		public void GenerateCssForConfiguration_PictureCssIsGenerated()
		{
			TestStyle style = GenerateStyle("Normal");
			style.SetExplicitParaIntProp((int)FwTextPropType.ktptLeadingIndent, 0, LeadingIndent);
			ConfiguredXHTMLGenerator.AssemblyFile = "xWorksTests";
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
			var cssWithPictureRules = CssGenerator.GenerateCssFromConfiguration(config, m_mediator);
			Assert.IsTrue(Regex.Match(cssWithPictureRules, @".*\.entry.*picture.*{.*float:right.*}", RegexOptions.Singleline).Success,
							  "picture not floated right");
			Assert.IsTrue(Regex.Match(cssWithPictureRules, @".*\.entry.*picture.*img.*{.*max-width:1in;.*}", RegexOptions.Singleline).Success,
							  "css for image did not contain height contraint attribute");
			Assert.IsTrue(Regex.Match(cssWithPictureRules, @".*\.entry.*pictures.*picture.*{.*margin:\s*0pt\s*0pt\s*4pt\s*4pt.*;.*}", RegexOptions.Singleline).Success,
							  "css for image did not contain valid margin attribute");
			Assert.IsTrue(Regex.Match(cssWithPictureRules, @".*\.entry.*{.*clear:both.*}", RegexOptions.Singleline).Success,
							  "float not cleared at entry");
			Assert.IsTrue(Regex.Match(cssWithPictureRules, @".*\.entry*\>\s*.*pictures.*picture*\>\s*.captionContent\s*.caption*\{.*margin-left:\s*24pt", RegexOptions.Singleline).Success,
							  "css for caption did not contain valid margin attribute");
		}

		/// <summary>
		/// The css for a picture sub fields  Before Between After Css is Generated.
		/// </summary>
		[Test]
		public void GenerateCssForConfiguration_PictureSubfieldsBeforeBetweenAfterIsAreGenerated()
		{
			TestStyle style = GenerateStyle("Normal");
			style.SetExplicitParaIntProp((int)FwTextPropType.ktptLeadingIndent, 0, LeadingIndent);
			ConfiguredXHTMLGenerator.AssemblyFile = "xWorksTests";
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
			var cssResult = CssGenerator.GenerateCssFromConfiguration(config, m_mediator);

			var senseNumberBefore = @".entry> .pictures .picture> .captionContent .sensenumbertss:before\{\s*content:'\[';";
			Assert.IsTrue(Regex.Match(cssResult, senseNumberBefore, RegexOptions.Singleline).Success, "expected Sense Number before rule is generated");

			var senseNumberAfter = @".entry> .pictures .picture> .captionContent .sensenumbertss:after\{\s*content:'\]';";
			Assert.IsTrue(Regex.Match(cssResult, senseNumberAfter, RegexOptions.Singleline).Success, "expected Sense Number after rule is generated");

			var senseNumberBetween = @".entry> .pictures .picture .sensenumbertss> .sensenumberts\+ .sensenumberts:before\{\s*content:', ';";
			Assert.IsTrue(Regex.Match(cssResult, senseNumberBetween, RegexOptions.Singleline).Success, "expected Sense Number between rule is generated");

			var captionBefore = @".entry> .pictures .picture> .captionContent .caption:before\{\s*content:'\{';";
			Assert.IsTrue(Regex.Match(cssResult, captionBefore, RegexOptions.Singleline).Success, "expected Caption before rule is generated");

			var captionAfter = @".entry> .pictures .picture> .captionContent .caption:after\{\s*content:'\}';";
			Assert.IsTrue(Regex.Match(cssResult, captionAfter, RegexOptions.Singleline).Success, "expected Caption after rule is generated");

			var captionBetween = @".entry> .pictures .picture .caption> .captio\+ .captio:before\{\s*content:' ';";
			Assert.IsTrue(Regex.Match(cssResult, captionBetween, RegexOptions.Singleline).Success, "expected Caption between rule is generated");
		}

		/// <summary>
		/// The css for a picture Before Between After Css is Generated.
		/// </summary>
		[Test]
		public void GenerateCssForConfiguration_PictureBeforeBetweenAfterIsAreGenerated()
		{
			TestStyle style = GenerateStyle("Normal");
			style.SetExplicitParaIntProp((int)FwTextPropType.ktptLeadingIndent, 0, LeadingIndent);
			ConfiguredXHTMLGenerator.AssemblyFile = "xWorksTests";

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
			var cssResult = CssGenerator.GenerateCssFromConfiguration(config, m_mediator);

			var pictureBefore = @".entry> .pictures> div:first-child:before\{\s*content:'\[';";
			Assert.IsTrue(Regex.Match(cssResult, pictureBefore, RegexOptions.Singleline).Success, "expected Picture before rule is generated");

			var pictureAfter = @".entry> .pictures> div:last-child:after\{\s*content:'\]';";
			Assert.IsTrue(Regex.Match(cssResult, pictureAfter, RegexOptions.Singleline).Success, "expected Picture after rule is generated");

			var pictureBetween = @".entry> .pictures> div\+ div:before\{\s*content:', ';";
			Assert.IsTrue(Regex.Match(cssResult, pictureBetween, RegexOptions.Singleline).Success, "expected Picture between rule is generated");
		}


		/// <summary>
		/// Part of LT-12572.
		/// </summary>
		[Test]
		public void GenerateCssForConfiguration_PictureWritesRulesForHeadwordAndGlossInCaptionArea()
		{
			TestStyle style = GenerateStyle("Normal");
			style.SetExplicitParaIntProp((int)FwTextPropType.ktptLeadingIndent, 0, LeadingIndent);
			ConfiguredXHTMLGenerator.AssemblyFile = "xWorksTests";

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
			var cssWithPictureRules = CssGenerator.GenerateCssFromConfiguration(config, m_mediator);

			Assert.IsTrue(Regex.Match(cssWithPictureRules, @".*\.entry.*pictures.*picture> .captionContent .caption", RegexOptions.Singleline).Success,
				"css for image did not contain expected rule");
			Assert.IsTrue(Regex.Match(cssWithPictureRules, @".*\.entry.*pictures.*picture> .captionContent .headword", RegexOptions.Singleline).Success,
				"css for image did not contain expected headword rule");
			Assert.IsTrue(Regex.Match(cssWithPictureRules, @".*\.entry.*pictures.*picture> .captionContent .owner_gloss", RegexOptions.Singleline).Success,
				"css for image did not contain expected gloss rule");
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
			sense.Gloss.set_String(wsEn, Cache.TsStrFactory.MakeString("gloss", wsEn));
			//SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
			Assert.That(Regex.Replace(cssResult, @"\t|\n|\r", ""), Contains.Substring(".lexentry> .senses .sense> .gloss> span.writingsystemprefix" +
				"{font-family:\'foofoo\',serif;font-size:10pt;font-weight:bold;font-style:italic;color:#00F;"));
			Assert.That(Regex.Replace(cssResult, @"\t|\n|\r", ""), Contains.Substring(".lexentry> .senses .sense> .gloss> span.writingsystemprefix:after{content:' ';}"));
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
			const string englishStyle = "span[lang|=\"en\"]{font-family:'english',serif;color:#F00;}";
			const string frenchStyle = "span[lang|=\"fr\"]{font-family:'french',serif;color:#008000;}";
			//SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
			Assert.That(Regex.Replace(cssResult, @"\t|\n|\r", ""), Contains.Substring(defaultStyle + englishStyle + frenchStyle));
		}

		[Test]
		public void GenerateCssForConfiguration_GenerateDictionaryNormalParagraphStyle()
		{
			GenerateNormalStyle("Dictionary-Normal");
			var testSensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Senses"
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
			//SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
			Assert.IsTrue(Regex.Match(cssResult, @"div.entry{\s*margin-left:24pt;\s*padding-right:48pt;\s*}", RegexOptions.Singleline).Success,
							  "Generate Dictionary-Normal Paragraph Style not generated.");
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
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
			Assert.IsTrue(Regex.Match(cssResult, @"div.minorentry{\s*margin-left:24pt;\s*padding-right:48pt;\s*}", RegexOptions.Singleline).Success,
							  "Dictionary-Minor Paragraph Style not generated.");
			Assert.IsTrue(Regex.Match(cssResult, @"div.specialminorentry{\s*padding-right:32pt;\s*}", RegexOptions.Singleline).Success,
							  "Dictionary-Minor Paragraph Style for node with style attribute not generated.");
			Assert.IsTrue(Regex.Match(cssResult, @"div.optionsminorentry{\s*padding-right:16pt;\s*}", RegexOptions.Singleline).Success,
							  "Dictionary-Minor Paragraph Style for node with paragraph options not generated.");
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
			var style = new TestStyle(fontInfo, Cache) { Name = name, IsParagraphStyle = false };
			SafelyAddStyleToSheetAndTable(name, style);
			return style;
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
			style.SetDefaultFontInfo(fontInfo1);

			SafelyAddStyleToSheetAndTable(name, style);
		}

		private void GenerateNumberingStyle(string name, VwBulNum schemeType)
		{
			var fontInfo = new FontInfo();
			fontInfo.m_fontColor.ExplicitValue = Color.Green;
			fontInfo.m_fontSize.ExplicitValue = 14000;
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
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
			const string regExPected = @".lexentry>\s.senses\s>\s.sensecontent:before.*{.*content:'\\25A0';.*font-size:14pt;.*color:Green;.*}";
			Assert.IsTrue(Regex.Match(cssResult, regExPected, RegexOptions.Singleline).Success, "Bulleted style not generated.");
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
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
			const string regExPected = @".lexentry>\s.senses\s>\s.sensecontent\s.\s.sensecontent:not\(:first-child\):before.*{.*content:'\\25A0';.*font-size:14pt;.*color:Green;.*}";
			Assert.IsTrue(Regex.Match(cssResult, regExPected, RegexOptions.Singleline).Success, "Bulleted style not generated.");
		}

		[Test]
		public void GenerateCssForNumberingStyleForSenses()
		{
			GenerateNumberingStyle("Numbered List", VwBulNum.kvbnArabic);
			var senses = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "Senses",
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
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
			const string regexExpected = @".lexentry>\s.senses{.*counter-reset:\ssensesos;.*}.*.lexentry>\s.senses\s>\s.sensecontent:before{.*counter-increment:\ssensesos;.*content:\scounter.sensesos,\sdecimal.\s'\s';.*font-size:14pt;.*color:Green;.*}";
			Assert.IsTrue(Regex.Match(cssResult, regexExpected, RegexOptions.Singleline).Success, "Numbering style not generated for Senses.");
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
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
			const string regexExpected = @".lexentry>\s.subentries{.*counter-reset:[\s]subentries;.*}.*.lexentry>\s.subentries\s.subentry:before{.*counter-increment:[\s]subentries;.*content:\scounter.subentries,\supper-roman.\s'\s';.*font-size:14pt;.*color:Green;.*}";
			Assert.IsTrue(Regex.Match(cssResult, regexExpected, RegexOptions.Singleline).Success,
				"Numbering style not generated for Subentry.");
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
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
			const string regexExpected = @".lexentry>\s.senses\s>\s.sensecontent\s>\s.sense>\s.examplesos{.*counter-reset:[\s]examplesos;.*}.*.lexentry>\s.senses\s>\s.sensecontent\s>\s.sense>\s.examplesos\s.exampleso:before{.*counter-increment:[\s]examplesos;.*content:[\s]counter.examplesos,[\s]upper-alpha.\s'\s';.*font-size:14pt;.*color:Green;.*}";
			Assert.IsTrue(Regex.Match(cssResult, regexExpected, RegexOptions.Singleline).Success, "Numbering style not generated for Examples.");
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
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
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
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
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
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
			const string regExpected = @".lexentry>\s.senses\s>\s.sensecontent";
			Assert.IsTrue(Regex.Match(cssResult, regExpected, RegexOptions.Singleline).Success, "Sense List style should generate a match.");
			const string regExNotExpected = regExpected + @"(\s\+\s.sensecontent)?:not\(:first-child\):before";
			Assert.IsFalse(Regex.Match(cssResult, regExNotExpected, RegexOptions.Singleline).Success,
							  "Sense List style should not generate a match, since it is not a bulleted style.");
		}

		[Test]
		public void GenerateCssForBulletStyleForSubSenses()
		{
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
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
			const string regExPected = @".lexentry>\s.senses\s*>\s*.sensecontent\s*>\s*.sense>\s.senses\s>\s.sensecontent:before.*{.*content:'\\25A0';.*font-size:14pt;.*color:Green;.*}";
			Assert.IsTrue(Regex.Match(cssResult, regExPected, RegexOptions.Singleline).Success, "Bulleted style for SubSenses not generated.");
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
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
			var regexExpected1 = @"\.lexentry>\s\.subentries\s\.subentry{[^}]*\sfont-size:12pt;[^}]*\scolor:#F00;[^}]*\sdisplay:block;[^}]*}";
			Assert.IsTrue(Regex.Match(cssResult, regexExpected1, RegexOptions.Singleline).Success,
				"expected subentry rule not generated");
			var regexExpected2 = @"\.lexentry>\s\.subentries\s\.subentry:before{[^}]*\scontent:'\\25A0';[^}]*font-size:14pt;[^}]*color:Green;[^}]*}";
			Assert.IsTrue(Regex.Match(cssResult, regexExpected2, RegexOptions.Singleline).Success,
				"expected subentry:before rule not generated");
			// Check that the bullet info values occur only in the :before section, and that the primary values
			// do not occur in the :before section.
			var regexUnwanted1 = @"\.lexentry>\s\.subentries\s\.subentry{[^}]*\scontent:'\\25A0';[^}]*}";
			Assert.IsFalse(Regex.Match(cssResult, regexUnwanted1, RegexOptions.Singleline).Success,
				"subentry rule has unwanted content value");
			var regexUnwanted2 = @"\.lexentry>\s\.subentries\s\.subentry{[^}]*\sfont-size:14pt;[^}]*}";
			Assert.IsFalse(Regex.Match(cssResult, regexUnwanted2, RegexOptions.Singleline).Success,
				"subentry rule has unwanted font-size value");
			var regexUnwanted3 = @"\.lexentry>\s\.subentries\s\.subentry{[^}]*\scolor:Green;[^}]*}";
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
				var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
				const string regexExpected1 = @"\.lexentry>\s.mlrs\s\.mlr>\s\.configtargets\s\.configtarget>\s\.costume{[^}]*}";
				Assert.IsTrue(Regex.Match(cssResult, regexExpected1, RegexOptions.Singleline).Success, "expected costume rule not generated");
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
				var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
				const string regexExpected1 = @"\.lexentry>\s\.custom-location \.custom-locatio{[^}]*}";
				Assert.IsTrue(Regex.Match(cssResult, regexExpected1, RegexOptions.Singleline).Success,
					"expected custom-location rule not generated");
				const string regexExpected2 = @"\.lexentry>\s\.custom-location \.custom-locatio>\s\.name{[^}]*}";
				Assert.IsTrue(Regex.Match(cssResult, regexExpected2, RegexOptions.Singleline).Success,
					"expected custom-location>name rule not generated");
				const string regexExpected3 = @"\.lexentry>\s\.custom-location \.custom-locatio>\s\.abbreviation{[^}]*}";
				Assert.IsTrue(Regex.Match(cssResult, regexExpected3, RegexOptions.Singleline).Success,
					"expected custom-location>abbreviation rule not generated");
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
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
			var regexExpected1 = @"\.lexentry>\s\.note_test-one{[^}]*}";
			Assert.IsTrue(Regex.Match(cssResult, regexExpected1, RegexOptions.Singleline).Success,
				"expected duplicated config node rename rule not generated");
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
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
			var regexExpected1 = @"\.lexentry>\s\.note_-test{[^}]*}";
			Assert.IsTrue(Regex.Match(cssResult, regexExpected1, RegexOptions.Singleline).Success,
				"expected duplicated config node rename rule not generated");
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
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
			var regexExpected1 = @"\.lexentry>\s\.note_-test-{[^}]*}";
			Assert.IsTrue(Regex.Match(cssResult, regexExpected1, RegexOptions.Singleline).Success,
				"expected duplicated config node rename rule not generated");
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
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);

			//Following Testcase removed(no longer needed) as a fix for LT-17238 ("Between" contents should not come between spans that are all in a single string with embedded WSs)
			//var regexItem1 = @".entry> .pronunciations .pronunciation> .form> span\+ span:before\{\s*content:' ';\s*\}";
			//Assert.IsTrue(Regex.Match(cssResult, regexItem1, RegexOptions.Singleline).Success, "expected collection item between rule is generated");

			var regexItem2 = @".entry> .pronunciations .pronunciation> .form> span:first-child:before\{\s*content:'\[';\s*\}";
			Assert.IsTrue(Regex.Match(cssResult, regexItem2, RegexOptions.Singleline).Success, "expected collection item before rule is generated");

			var regexItem3 = @".entry> .pronunciations .pronunciation> .form> span:last-child:after\{\s*content:'\]';\s*\}";
			Assert.IsTrue(Regex.Match(cssResult, regexItem3, RegexOptions.Singleline).Success, "expected collection item after rule is generated");

			var regexCollection1 = @".entry .pronunciations> .pronunciation\+ .pronunciation:before\{\s*content:', ';\s*\}";
			Assert.IsTrue(Regex.Match(cssResult, regexCollection1, RegexOptions.Singleline).Success, "expected collection between rule is generated");

			// The following two checks test the fix for LT-17048.  The preceding four checks should be the same before and after the fix.
			var regexCollection2 = @".entry> .pronunciations:before\{\s*content:'\{Pron: ';\s*\}";
			Assert.IsTrue(Regex.Match(cssResult, regexCollection2, RegexOptions.Singleline).Success, "expected collection before rule is generated");

			var regexCollection3 = @".entry> .pronunciations:after\{\s*content:'\} ';\s*\}";
			Assert.IsTrue(Regex.Match(cssResult, regexCollection3, RegexOptions.Singleline).Success, "expected collection after rule is generated");
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
			var cssPara = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
			((DictionaryNodeSenseOptions)sensesConfig.DictionaryNodeOptions).DisplayEachSenseInAParagraph = false;
			var cssInline = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);

			const string regexBefore = @"^\.lexentry> \.senses:before\{";
			const string regexAfter = @"^\.lexentry> \.senses:after\{";

			Assert.AreNotEqual(cssPara, cssInline, "The css should change depending on senses showing in a paragraph");
			Assert.IsTrue(Regex.IsMatch(cssInline, regexBefore, RegexOptions.Multiline), "The css for inline senses should have a senses:before rule");
			Assert.IsTrue(Regex.IsMatch(cssInline, regexAfter, RegexOptions.Multiline), "The css for inline senses should have a senses:after rule");
			Assert.IsFalse(Regex.IsMatch(cssPara, regexBefore, RegexOptions.Multiline), "The css for paragraphed senses should not have a senses:before rule");
			Assert.IsFalse(Regex.IsMatch(cssPara, regexAfter, RegexOptions.Multiline), "The css for paragraphed senses should not have a senses:after rule");
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

		/// <summary>
		/// Generates test styles for the pseudo selectors :before / :after
		/// </summary>
		private void GeneratePseudoStyle(string name)
		{
			var fontInfo = new FontInfo
			{
				m_fontColor = { ExplicitValue = FontColor },
				m_fontSize = { ExplicitValue = FontSize }
			};
			var style = new TestStyle(fontInfo, Cache) { Name = name, IsParagraphStyle = false };
			SafelyAddStyleToSheetAndTable(name, style);
		}

		/// <summary>
		/// Generates empty test styles for the pseudo selectors :before / :after
		/// </summary>
		private void GenerateEmptyPseudoStyle(string name)
		{
			var fontInfo = new FontInfo();
			var style = new TestStyle(fontInfo, Cache) { Name = name, IsParagraphStyle = false };
			SafelyAddStyleToSheetAndTable(name, style);
		}

		private void VerifyFontInfoInCss(Color color, Color bgcolor, string fontName, bool bold, bool italic, int size, string css)
		{
			Assert.That(css, Contains.Substring("color:" + HtmlColor.FromRgb(color.R, color.G, color.B)), "font color missing");
			Assert.That(css, Contains.Substring("background-color:" + HtmlColor.FromRgb(bgcolor.R, bgcolor.G, bgcolor.B)), "background-color missing");
			Assert.That(css, Contains.Substring("font-family:'" + fontName + "'"), "font name missing");
			Assert.That(css, Contains.Substring("font-weight:" + (bold ? "bold" : "normal") + ";"), "font bold missing");
			Assert.That(css, Contains.Substring("font-style:" + (italic ? "italic" : "normal") + ";"), "font italic missing");
			// Font sizes are stored as millipoint integers in the styles by FLEx and turned into pt values on export
			Assert.That(css, Contains.Substring("font-size:" + (float)size / 1000 + "pt;"), "font size missing");
		}

		private void VerifyExtraFontInfoInCss(int offset, FwSuperscriptVal superscript,
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
						Assert.That(css, Is.Not.StringContaining("border-bottom:"), "underline should not have been applied");
						Assert.That(css, Is.Not.StringContaining("text-decoration:underline"), "underline should not have been applied");
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
	}

	internal class TestStyle : BaseStyleInfo
	{
		public TestStyle(FontInfo defaultFontInfo, FdoCache cache)
			: base(cache)
		{
			m_defaultFontInfo = defaultFontInfo;
		}

		public TestStyle(InheritableStyleProp<BulletInfo> defaultBulletFontInfo, FdoCache cache)
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