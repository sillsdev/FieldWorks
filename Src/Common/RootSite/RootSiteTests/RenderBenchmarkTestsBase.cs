using System;
using System.Drawing;
using System.IO;
using NUnit.Framework;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Scripture;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Infrastructure;
using SIL.LCModel.Core.WritingSystems;
using SIL.FieldWorks.Common.RootSites.RootSiteTests;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Utils;

namespace SIL.FieldWorks.Common.RootSites.RenderBenchmark
{
	/// <summary>
	/// Base class for benchmark tests, handling data generation for scenarios using Real Data.
	/// Creates Scripture styles in the DB and produces rich test data with chapter/verse markers,
	/// section headings, and diverse paragraph styles so StVc renders formatted output.
	/// </summary>
	public abstract class RenderBenchmarkTestsBase : RealDataTestsBase
	{
		protected ILgWritingSystemFactory m_wsf;
		protected int m_wsEng;
		protected int m_hvoRoot;
		protected int m_flidContainingTexts;
		protected int m_frag = 100; // Default to Book View (100)

		[SetUp]
		public override void TestSetup()
		{
			base.TestSetup(); // Creates Cache and DB and calls InitializeProjectData/CreateTestData
			m_flidContainingTexts = ScrBookTags.kflidFootnotes; // Default, can be overridden
		}

		protected override void InitializeProjectData()
		{
			m_wsf = Cache.WritingSystemFactory;
			m_wsEng = m_wsf.GetWsFromStr("en");
			if (m_wsEng == 0) throw new Exception("English WS not found");

			// Ensure Scripture exists in the real project
			if (Cache.LangProject.TranslatedScriptureOA == null)
			{
				var scriptureFactory = Cache.ServiceLocator.GetInstance<IScriptureFactory>();
				var script = scriptureFactory.Create();
				script.Versification = ScrVers.English;
				Cache.LangProject.TranslatedScriptureOA = script;
			}

			// Populate the DB with Scripture styles so the stylesheet can resolve them
			CreateScriptureStyles();
		}

		#region Scripture Style Creation

		/// <summary>
		/// Creates the essential Scripture paragraph and character styles in the DB.
		/// These mirror definitions from FlexStyles.xml and are required by StVc to
		/// produce formatted output (bold headings, superscript verse numbers, etc.).
		/// Without these, the Views engine falls back to plain black text.
		/// </summary>
		private void CreateScriptureStyles()
		{
			var styleFactory = Cache.ServiceLocator.GetInstance<IStStyleFactory>();
			var styles = Cache.LangProject.StylesOC;

			// Find or create the base Normal style (may already exist from template)
			IStStyle normalStyle = FindStyle(ScrStyleNames.Normal);
			if (normalStyle == null)
			{
				normalStyle = styleFactory.Create(styles, ScrStyleNames.Normal,
					ContextValues.Internal, StructureValues.Undefined, FunctionValues.Prose,
					false, 0, true);
				var normalBldr = TsStringUtils.MakePropsBldr();
				normalBldr.SetIntPropValues((int)FwTextPropType.ktptFontSize,
					(int)FwTextPropVar.ktpvMilliPoint, 10000); // 10pt
				normalBldr.SetStrPropValue((int)FwTextPropType.ktptFontFamily, "Charis SIL");
				normalStyle.Rules = normalBldr.GetTextProps();
			}

			// "Paragraph" - the main Scripture prose style (first-line indent 12pt)
			CreateParagraphStyle(styleFactory, styles, normalStyle);

			// "Section Head" - bold, centered, 9pt
			CreateSectionHeadStyle(styleFactory, styles, normalStyle);

			// "Chapter Number" - large drop-cap character style
			CreateChapterNumberStyle(styleFactory, styles);

			// "Verse Number" - superscript character style
			CreateVerseNumberStyle(styleFactory, styles);

			// "Title Main" - large bold centered style
			CreateTitleMainStyle(styleFactory, styles, normalStyle);
		}

		private void CreateParagraphStyle(IStStyleFactory factory, ILcmOwningCollection<IStStyle> styles, IStStyle basedOn)
		{
			if (FindStyle(ScrStyleNames.NormalParagraph) != null) return;

			var style = factory.Create(styles, ScrStyleNames.NormalParagraph,
				ContextValues.Text, StructureValues.Body, FunctionValues.Prose,
				false, 0, true);
			style.BasedOnRA = basedOn;
			style.NextRA = style; // next is self

			var bldr = TsStringUtils.MakePropsBldr();
			bldr.SetIntPropValues((int)FwTextPropType.ktptFirstIndent,
				(int)FwTextPropVar.ktpvMilliPoint, 12000); // 12pt first line indent
			style.Rules = bldr.GetTextProps();
		}

		private void CreateSectionHeadStyle(IStStyleFactory factory, ILcmOwningCollection<IStStyle> styles, IStStyle basedOn)
		{
			if (FindStyle(ScrStyleNames.SectionHead) != null) return;

			var style = factory.Create(styles, ScrStyleNames.SectionHead,
				ContextValues.Text, StructureValues.Heading, FunctionValues.Prose,
				false, 0, true);
			style.BasedOnRA = basedOn;

			var bldr = TsStringUtils.MakePropsBldr();
			bldr.SetIntPropValues((int)FwTextPropType.ktptBold,
				(int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvForceOn);
			bldr.SetIntPropValues((int)FwTextPropType.ktptFontSize,
				(int)FwTextPropVar.ktpvMilliPoint, 12000); // 12pt bold
			bldr.SetIntPropValues((int)FwTextPropType.ktptAlign,
				(int)FwTextPropVar.ktpvEnum, (int)FwTextAlign.ktalCenter);
			bldr.SetIntPropValues((int)FwTextPropType.ktptSpaceBefore,
				(int)FwTextPropVar.ktpvMilliPoint, 8000); // 8pt space before
			bldr.SetIntPropValues((int)FwTextPropType.ktptSpaceAfter,
				(int)FwTextPropVar.ktpvMilliPoint, 4000); // 4pt space after
			bldr.SetIntPropValues((int)FwTextPropType.ktptForeColor,
				(int)FwTextPropVar.ktpvDefault, (int)ColorUtil.ConvertColorToBGR(Color.FromArgb(0, 51, 102))); // dark blue
			style.Rules = bldr.GetTextProps();
		}

		private void CreateChapterNumberStyle(IStStyleFactory factory, ILcmOwningCollection<IStStyle> styles)
		{
			if (FindStyle(ScrStyleNames.ChapterNumber) != null) return;

			var style = factory.Create(styles, ScrStyleNames.ChapterNumber,
				ContextValues.Text, StructureValues.Body, FunctionValues.Chapter,
				true, 0, true); // isCharStyle = true

			var bldr = TsStringUtils.MakePropsBldr();
			bldr.SetIntPropValues((int)FwTextPropType.ktptBold,
				(int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvForceOn);
			bldr.SetIntPropValues((int)FwTextPropType.ktptFontSize,
				(int)FwTextPropVar.ktpvMilliPoint, 24000); // 24pt large chapter number
			bldr.SetIntPropValues((int)FwTextPropType.ktptForeColor,
				(int)FwTextPropVar.ktpvDefault, (int)ColorUtil.ConvertColorToBGR(Color.FromArgb(128, 0, 0))); // dark red
			style.Rules = bldr.GetTextProps();
		}

		private void CreateVerseNumberStyle(IStStyleFactory factory, ILcmOwningCollection<IStStyle> styles)
		{
			if (FindStyle(ScrStyleNames.VerseNumber) != null) return;

			var style = factory.Create(styles, ScrStyleNames.VerseNumber,
				ContextValues.Text, StructureValues.Body, FunctionValues.Verse,
				true, 0, true); // isCharStyle = true

			var bldr = TsStringUtils.MakePropsBldr();
			bldr.SetIntPropValues((int)FwTextPropType.ktptSuperscript,
				(int)FwTextPropVar.ktpvEnum, (int)FwSuperscriptVal.kssvSuper);
			bldr.SetIntPropValues((int)FwTextPropType.ktptBold,
				(int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvForceOn);
			bldr.SetIntPropValues((int)FwTextPropType.ktptForeColor,
				(int)FwTextPropVar.ktpvDefault, (int)ColorUtil.ConvertColorToBGR(Color.FromArgb(0, 102, 0))); // dark green
			style.Rules = bldr.GetTextProps();
		}

		private void CreateTitleMainStyle(IStStyleFactory factory, ILcmOwningCollection<IStStyle> styles, IStStyle basedOn)
		{
			if (FindStyle(ScrStyleNames.MainBookTitle) != null) return;

			var style = factory.Create(styles, ScrStyleNames.MainBookTitle,
				ContextValues.Title, StructureValues.Body, FunctionValues.Prose,
				false, 0, true);
			style.BasedOnRA = basedOn;

			var bldr = TsStringUtils.MakePropsBldr();
			bldr.SetIntPropValues((int)FwTextPropType.ktptBold,
				(int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvForceOn);
			bldr.SetIntPropValues((int)FwTextPropType.ktptFontSize,
				(int)FwTextPropVar.ktpvMilliPoint, 20000); // 20pt
			bldr.SetIntPropValues((int)FwTextPropType.ktptAlign,
				(int)FwTextPropVar.ktpvEnum, (int)FwTextAlign.ktalCenter);
			bldr.SetIntPropValues((int)FwTextPropType.ktptSpaceBefore,
				(int)FwTextPropVar.ktpvMilliPoint, 36000); // 36pt space before
			bldr.SetIntPropValues((int)FwTextPropType.ktptSpaceAfter,
				(int)FwTextPropVar.ktpvMilliPoint, 12000); // 12pt space after
			bldr.SetIntPropValues((int)FwTextPropType.ktptForeColor,
				(int)FwTextPropVar.ktpvDefault, (int)ColorUtil.ConvertColorToBGR(Color.FromArgb(0, 0, 128))); // navy blue
			style.Rules = bldr.GetTextProps();
		}

		private IStStyle FindStyle(string name)
		{
			foreach (var s in Cache.LangProject.StylesOC)
			{
				if (s.Name == name) return s;
			}
			return null;
		}

		#endregion

		protected override void CreateTestData()
		{
			// Individual tests call SetupScenarioData
		}

		protected void SetupScenarioData(string scenarioId)
		{
			m_frag = 100;
			m_flidContainingTexts = ScrBookTags.kflidFootnotes;

			switch (scenarioId)
			{
				case "simple":
				case "simple-test":
					CreateSimpleScenario();
					break;
				case "medium":
					CreateMediumScenario();
					break;
				case "complex":
					CreateComplexScenario();
					break;
				case "deep-nested":
					CreateDeepNestedScenario();
					break;
				case "custom-heavy":
					CreateCustomHeavyScenario();
					break;
				default:
					CreateSimpleScenario();
					break;
			}
		}

		private void CreateSimpleScenario()
		{
			var book = CreateBook(1); // GEN
			m_hvoRoot = book.Hvo;
			AddRichSections(book, 3, versesPerSection: 4, chapterStart: 1);
		}

		private void CreateMediumScenario()
		{
			var book = CreateBook(2); // EXO
			m_hvoRoot = book.Hvo;
			AddRichSections(book, 5, versesPerSection: 6, chapterStart: 1);
		}

		private void CreateComplexScenario()
		{
			var book = CreateBook(3); // LEV
			m_hvoRoot = book.Hvo;
			AddRichSections(book, 10, versesPerSection: 8, chapterStart: 1);
		}

		private void CreateDeepNestedScenario()
		{
			var book = CreateBook(4); // NUM
			m_hvoRoot = book.Hvo;
			AddRichSections(book, 3, versesPerSection: 12, chapterStart: 1);
		}

		private void CreateCustomHeavyScenario()
		{
			var book = CreateBook(5); // DEU
			m_hvoRoot = book.Hvo;
			AddRichSections(book, 5, versesPerSection: 8, chapterStart: 1);
		}

		#region Rich Data Factories

		protected IScrBook CreateBook(int bookNum)
		{
			var bookFactory = Cache.ServiceLocator.GetInstance<IScrBookFactory>();
			return bookFactory.Create(bookNum);
		}

		/// <summary>
		/// Creates sections with formatted section headings (bold centered "Section Head" style),
		/// chapter numbers (large bold red), verse numbers (superscript bold green), and body
		/// text (indented "Paragraph" style). This produces richly styled output from StVc.
		/// </summary>
		protected void AddRichSections(IScrBook book, int sectionCount, int versesPerSection, int chapterStart)
		{
			var sectionFactory = Cache.ServiceLocator.GetInstance<IScrSectionFactory>();
			var stTextFactory = Cache.ServiceLocator.GetInstance<IStTextFactory>();
			int chapter = chapterStart;

			// Sample prose fragments for realistic text variety
			string[] proseFragments = new[]
			{
				"In the beginning God created the heavens and the earth. ",
				"The earth was formless and empty, darkness was over the surface of the deep. ",
				"And God said, Let there be light, and there was light. ",
				"God saw that the light was good, and he separated the light from the darkness. ",
				"God called the light day, and the darkness he called night. ",
				"And there was evening, and there was morning, the first day. ",
				"Then God said, Let there be a vault between the waters to separate water. ",
				"So God made the vault and separated the water under the vault from the water above it. ",
				"God called the vault sky. And there was evening, and there was morning, the second day. ",
				"And God said, Let the water under the sky be gathered to one place, and let dry ground appear. ",
				"God called the dry ground land, and the gathered waters he called seas. And God saw that it was good. ",
				"Then God said, Let the land produce vegetation: seed bearing plants and trees on the land. ",
			};

			for (int s = 0; s < sectionCount; s++)
			{
				var section = sectionFactory.Create();
				book.SectionsOS.Add(section);

				// Create heading StText and add a heading paragraph
				section.HeadingOA = stTextFactory.Create();
				var headingBldr = new StTxtParaBldr(Cache)
				{
					ParaStyleName = ScrStyleNames.SectionHead
				};
				string headingText = $"Section {s + 1}: The Account of Day {s + 1}";
				headingBldr.AppendRun(headingText, StyleUtils.CharStyleTextProps(null, m_wsEng));
				headingBldr.CreateParagraph(section.HeadingOA);

				// Create content StText and add body paragraphs with chapter/verse
				section.ContentOA = stTextFactory.Create();

				var paraBldr = new StTxtParaBldr(Cache)
				{
					ParaStyleName = ScrStyleNames.NormalParagraph
				};

				// First section gets a chapter number
				if (s == 0 || s % 3 == 0)
				{
					paraBldr.AppendRun(chapter.ToString(),
						StyleUtils.CharStyleTextProps(ScrStyleNames.ChapterNumber, m_wsEng));
					chapter++;
				}

				// Add verses with prose text
				for (int v = 1; v <= versesPerSection; v++)
				{
					// Verse number run
					paraBldr.AppendRun(v.ToString() + "\u00A0",
						StyleUtils.CharStyleTextProps(ScrStyleNames.VerseNumber, m_wsEng));

					// Prose text run (cycle through fragments for variety)
					string prose = proseFragments[(s * versesPerSection + v) % proseFragments.Length];
					paraBldr.AppendRun(prose, StyleUtils.CharStyleTextProps(null, m_wsEng));
				}

				paraBldr.CreateParagraph(section.ContentOA);
			}
		}

		#endregion
	}
}
