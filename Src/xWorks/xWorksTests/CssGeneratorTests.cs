using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	class CssGeneratorTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private Mediator m_mediator;
		private FwStyleSheet m_styleSheet;
		private MockFwXApp m_application;
		private string m_configFilePath;
		private MockFwXWindow m_window;

		[Test]
		public void GenerateCssForConfiguration_NullModelThrowsNullArgument()
		{
			Assert.Throws(typeof(ArgumentNullException), () => CssGenerator.GenerateCssFromConfiguration(null, m_mediator));
		}

		[Test]
		public void GenerateCssForConfiguration_SimpleConfigurationGeneratesValidCss()
		{
			var headwordNode = new ConfigurableDictionaryNode
				{
					FieldDescription = "HeadWord",
					Label = "Headword",
					DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "fr" })
				};
			var mainEntryNode = new ConfigurableDictionaryNode
				{
					Children = new List<ConfigurableDictionaryNode> { headwordNode },
					FieldDescription = "LexEntry"
				};

			var model = new DictionaryConfigurationModel();
			model.Parts = new List<ConfigurableDictionaryNode> { mainEntryNode };
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
			Debug.WriteLine(cssResult);
			Assert.That(cssResult, Contains.Substring("entry"));
		}

		[Test]
		public void GenerateCssForConfiguration_BeforeAfterConfigGeneratesBeforeAfterCss()
		{
			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "HeadWord",
				Label = "Headword",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "fr" }),
				Before = "Z",
				After = "A"
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { headwordNode },
				FieldDescription = "LexEntry"
			};

			var model = new DictionaryConfigurationModel();
			model.Parts = new List<ConfigurableDictionaryNode> { mainEntryNode };
			//SUT
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
			// Check result for before and after rules equivalent to .headword:before{content:'Z';} and .headword:after{content:'A'}
			Assert.IsTrue(Regex.Match(cssResult, ".headword\\s*:\\s*before\\s*{\\s*content\\s*:\\s*'Z';\\s*}").Success,
							  "css before rule with Z content not found on headword");
			Assert.IsTrue(Regex.Match(cssResult, ".headword\\s*:\\s*after\\s*{\\s*content\\s*:\\s*'A';\\s*}").Success,
							  "css after rule with A content not found on headword");
		}

		[Test]
		public void GenerateCssForStyleName_CharacterStyleWorks()
		{
			GenerateStyle("Dictionary-Vernacular");
			var styleDeclaration = CssGenerator.GenerateCssStyleFromFwStyleSheet("Dictionary-Vernacular", m_mediator);
			Debug.WriteLine(styleDeclaration.ToString());
			Assert.That(styleDeclaration.ToString(), Contains.Substring("font-size"));
		}

		[Test]
		public void GenerateCssForStyleName_ParagraphBorderWorks()
		{
			GenerateParagraphStyle("Dictionary-Paragraph");
			var styleDeclaration = CssGenerator.GenerateCssStyleFromFwStyleSheet("Dictionary-Paragraph", m_mediator);
			Debug.WriteLine(styleDeclaration.ToString());

			Assert.That(styleDeclaration.ToString(), Contains.Substring("border-color:#F00"));
			Assert.That(styleDeclaration.ToString(), Contains.Substring("border-top-width:20pt"));
			Assert.That(styleDeclaration.ToString(), Contains.Substring("border-bottom-width:10pt"));
			Assert.That(styleDeclaration.ToString(), Contains.Substring("border-left-width:0pt"));
			Assert.That(styleDeclaration.ToString(), Contains.Substring("border-right-width:5pt"));
		}

		//For development work and css document structure inspection
		//[Test]
		//public void GrabMeSomeCss()
		//{
		//   using(var reader = new StreamReader("D:\\TemporaryWork\\TestDataForXHTML\\TestData.css"))
		//   {
		//      var parser = new ExCSS.Parser();
		//      var styleSheet = parser.Parse(reader.ReadToEnd());
		//      Debug.WriteLine("Look what we have here.");
		//   }
		//}

		[Ignore("Won't pass yet.")]
		[Test]
		public void GenerateCssForConfiguration_DefaultRootConfigGeneratesResult()
		{
			GenerateStyle("Dictionary-Headword");
			string defaultRoot =
				Path.Combine(Path.Combine(DirectoryFinder.DefaultConfigurations, "Dictionary"), "Root.xml");
			var model = new DictionaryConfigurationModel(defaultRoot);
			var cssResult = CssGenerator.GenerateCssFromConfiguration(model, m_mediator);
			var parser = new ExCSS.Parser();
			var styleSheet = parser.Parse(cssResult);
			Debug.WriteLine(cssResult);
			Assert.AreEqual(0, styleSheet.Errors.Count);
		}

		[TestFixtureSetUp]
		protected void Init()
		{
			FwRegistrySettings.Init();
			m_application = new MockFwXApp(new MockFwManager { Cache = Cache }, null, null);
			m_configFilePath = Path.Combine(DirectoryFinder.FWCodeDirectory, m_application.DefaultConfigurationPathname);
			m_window = new MockFwXWindow(m_application, m_configFilePath);
			((MockFwXWindow)m_window).Init(Cache); // initializes Mediator values
			m_mediator = m_window.Mediator;
			m_window.LoadUI(m_configFilePath); // actually loads UI here; needed for non-null stylesheet

			m_styleSheet = FontHeightAdjuster.StyleSheetFromMediator(m_mediator);
		}

		[TestFixtureTearDown]
		protected void TearDown()
		{
			m_application.Dispose();
			m_mediator.Dispose();
			FwRegistrySettings.Release();
		}

		private void GenerateStyle(string name)
		{
			var fontInfo = new FontInfo();
			fontInfo.m_fontColor.ExplicitValue = Color.Blue;
			fontInfo.m_fontName.ExplicitValue = "foofoo";
			fontInfo.m_italic.ExplicitValue = true;
			fontInfo.m_bold.ExplicitValue = true;
			fontInfo.m_fontSize.ExplicitValue = 10;
			var style = new TestStyle(fontInfo, Cache) { Name = name, IsParagraphStyle = false };
			m_styleSheet.Styles.Add(style);
		}

		private IStyle GenerateParagraphStyle(string name)
		{
			var fontInfo = new FontInfo();
			fontInfo.m_fontColor.ExplicitValue = Color.Blue;
			fontInfo.m_fontName.ExplicitValue = "foofoo";
			fontInfo.m_italic.ExplicitValue = true;
			fontInfo.m_bold.ExplicitValue = true;
			fontInfo.m_fontSize.ExplicitValue = 10;
			var style = new TestStyle(fontInfo, Cache) { Name = name, IsParagraphStyle = true };
			style.SetExplicitParaIntProp((int)FwTextPropType.ktptBorderTrailing, 0, 5);
			style.SetExplicitParaIntProp((int)FwTextPropType.ktptBorderColor, 0, (int)ColorUtil.ConvertColorToBGR(Color.Red));
			style.SetExplicitParaIntProp((int)FwTextPropType.ktptBorderTop, 0, 20);
			style.SetExplicitParaIntProp((int)FwTextPropType.ktptBorderBottom, 0, 10);
			style.SetExplicitParaIntProp((int)FwTextPropType.ktptLeadingIndent, 0, 24);
			style.SetExplicitParaIntProp((int)FwTextPropType.ktptTrailingIndent, 0, 48);
			style.SetExplicitParaIntProp((int)FwTextPropType.ktptPadTop, 0, 15);
			style.SetExplicitParaIntProp((int)FwTextPropType.ktptPadBottom, 0, 30);
			style.SetExplicitParaIntProp((int)FwTextPropType.ktptAlign, 0, (int)FwTextAlign.ktalJustify);
			m_styleSheet.Styles.Add(style);
			return style;
		}
	}

	class TestStyle : BaseStyleInfo
	{
		public TestStyle(FontInfo defaultFontInfo, FdoCache cache) : base(cache)
		{
			m_defaultFontInfo = defaultFontInfo;
		}
	}
}