using System;
using System.Drawing;
using System.IO;
using NUnit.Framework;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Scripture;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.Infrastructure;
using SIL.FieldWorks.Common.RootSites.RootSiteTests;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Utils;
using SIL.WritingSystems;

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
		protected int m_wsAr;  // Arabic (RTL)
		protected int m_wsFr;  // French (second analysis WS)
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

			// Create Arabic (RTL) writing system for bidirectional layout tests
			CoreWritingSystemDefinition arabic;
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("ar", out arabic);
			arabic.RightToLeftScript = true;
			Cache.ServiceLocator.WritingSystems.VernacularWritingSystems.Add(arabic);
			Cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems.Add(arabic);
			m_wsAr = arabic.Handle;

			// Create French writing system for multi-WS tests
			CoreWritingSystemDefinition french;
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("fr", out french);
			Cache.ServiceLocator.WritingSystems.AnalysisWritingSystems.Add(french);
			Cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems.Add(french);
			m_wsFr = french.Handle;

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
				case "many-paragraphs":
					CreateManyParagraphsScenario();
					break;
				case "footnote-heavy":
					CreateFootnoteHeavyScenario();
					break;
				case "mixed-styles":
					CreateMixedStylesScenario();
					break;
				case "long-prose":
					CreateLongProseScenario();
					break;
				case "multi-book":
					CreateMultiBookScenario();
					break;
				case "rtl-script":
					CreateRtlScriptScenario();
					break;
				case "multi-ws":
					CreateMultiWsScenario();
					break;
				case "lex-shallow":
					CreateLexEntryScenario(depth: 2, breadth: 3);
					break;
				case "lex-deep":
					CreateLexEntryScenario(depth: 4, breadth: 2);
					break;
				case "lex-extreme":
					CreateLexEntryScenario(depth: 6, breadth: 2);
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

		/// <summary>
		/// Stress: 50 sections, each with a single verse — forces massive paragraph layout overhead.
		/// </summary>
		private void CreateManyParagraphsScenario()
		{
			var book = CreateBook(6); // JOS
			m_hvoRoot = book.Hvo;
			AddRichSections(book, 50, versesPerSection: 1, chapterStart: 1);
		}

		/// <summary>
		/// Stress: 8 sections each containing 20 verses plus footnotes on every other verse.
		/// Forces footnote callers and footnote paragraph creation en masse.
		/// </summary>
		private void CreateFootnoteHeavyScenario()
		{
			var book = CreateBook(7); // JDG
			m_hvoRoot = book.Hvo;
			AddRichSectionsWithFootnotes(book, 8, versesPerSection: 20, chapterStart: 1);
		}

		/// <summary>
		/// Stress: Every verse run uses a different character style combination.
		/// Forces the style resolver to compute many distinct property sets.
		/// </summary>
		private void CreateMixedStylesScenario()
		{
			var book = CreateBook(8); // RUT
			m_hvoRoot = book.Hvo;
			AddMixedStyleSections(book, 6, versesPerSection: 15, chapterStart: 1);
		}

		/// <summary>
		/// Stress: 4 sections, each with a single paragraph containing 80 verses — very long
		/// unbroken paragraph that forces extensive line-breaking and layout computation.
		/// </summary>
		private void CreateLongProseScenario()
		{
			var book = CreateBook(9); // 1SA
			m_hvoRoot = book.Hvo;
			AddRichSections(book, 4, versesPerSection: 80, chapterStart: 1);
		}

		/// <summary>
		/// Stress: Creates 3 separate books with sections each, then sets root to the
		/// first book. Verifies the rendering engine handles large Scripture caches.
		/// </summary>
		private void CreateMultiBookScenario()
		{
			var book1 = CreateBook(10); // 2SA
			AddRichSections(book1, 5, versesPerSection: 10, chapterStart: 1);

			var book2 = CreateBook(11); // 1KI
			AddRichSections(book2, 5, versesPerSection: 10, chapterStart: 1);

			var book3 = CreateBook(12); // 2KI
			AddRichSections(book3, 5, versesPerSection: 10, chapterStart: 1);

			m_hvoRoot = book1.Hvo; // render the first; the others stress the backing store
		}

		/// <summary>
		/// Stress: Creates sections where all prose text is in Arabic (RTL), exercising
		/// bidirectional layout, Uniscribe/Graphite RTL shaping, and right-aligned paragraphs.
		/// Section headings remain English to force bidi mixing within the view.
		/// </summary>
		private void CreateRtlScriptScenario()
		{
			var book = CreateBook(13); // 1CH
			m_hvoRoot = book.Hvo;
			AddRtlSections(book, 4, versesPerSection: 10, chapterStart: 1);
		}

		/// <summary>
		/// Stress: Creates sections that alternate between English, Arabic, and French runs
		/// within the same paragraph, forcing the rendering engine to handle font fallback,
		/// writing-system switching, and mixed bidi text.
		/// </summary>
		private void CreateMultiWsScenario()
		{
			var book = CreateBook(14); // 2CH
			m_hvoRoot = book.Hvo;
			AddMultiWsSections(book, 5, versesPerSection: 8, chapterStart: 1);
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

		/// <summary>
		/// Creates sections with footnotes on every other verse, stressing the footnote
		/// rendering path (caller markers, footnote paragraph boxes, and layout).
		/// </summary>
		protected void AddRichSectionsWithFootnotes(IScrBook book, int sectionCount,
			int versesPerSection, int chapterStart)
		{
			var sectionFactory = Cache.ServiceLocator.GetInstance<IScrSectionFactory>();
			var stTextFactory = Cache.ServiceLocator.GetInstance<IStTextFactory>();
			var footnoteFactory = Cache.ServiceLocator.GetInstance<IScrFootnoteFactory>();
			int chapter = chapterStart;

			string[] proseFragments = new[]
			{
				"The Lord is my shepherd, I lack nothing. He makes me lie down in green pastures. ",
				"He leads me beside quiet waters, he refreshes my soul. He guides me along right paths. ",
				"Even though I walk through the darkest valley, I will fear no evil, for you are with me. ",
				"Your rod and your staff, they comfort me. You prepare a table before me. ",
				"You anoint my head with oil; my cup overflows. Surely your goodness and love will follow me. ",
				"And I will dwell in the house of the Lord forever. Hear my cry for mercy as I call to you. ",
			};

			string[] footnoteTexts = new[]
			{
				"Or righteousness; Hb. tsedeq",
				"Some manuscripts add for his name's sake",
				"Lit. in the valley of deep darkness",
				"Gk. adds in the presence of my enemies",
			};

			for (int s = 0; s < sectionCount; s++)
			{
				var section = sectionFactory.Create();
				book.SectionsOS.Add(section);

				section.HeadingOA = stTextFactory.Create();
				var headingBldr = new StTxtParaBldr(Cache) { ParaStyleName = ScrStyleNames.SectionHead };
				headingBldr.AppendRun($"Psalm {s + 1}: A Song of Ascents",
					StyleUtils.CharStyleTextProps(null, m_wsEng));
				headingBldr.CreateParagraph(section.HeadingOA);

				section.ContentOA = stTextFactory.Create();

				var paraBldr = new StTxtParaBldr(Cache) { ParaStyleName = ScrStyleNames.NormalParagraph };

				if (s == 0 || s % 3 == 0)
				{
					paraBldr.AppendRun(chapter.ToString(),
						StyleUtils.CharStyleTextProps(ScrStyleNames.ChapterNumber, m_wsEng));
					chapter++;
				}

				for (int v = 1; v <= versesPerSection; v++)
				{
					paraBldr.AppendRun(v.ToString() + "\u00A0",
						StyleUtils.CharStyleTextProps(ScrStyleNames.VerseNumber, m_wsEng));

					string prose = proseFragments[(s * versesPerSection + v) % proseFragments.Length];
					paraBldr.AppendRun(prose, StyleUtils.CharStyleTextProps(null, m_wsEng));

					// Add a footnote caller on every other verse
					if (v % 2 == 0)
					{
						var footnote = footnoteFactory.Create();
						book.FootnotesOS.Add(footnote);
						var footParaBldr = new StTxtParaBldr(Cache)
						{
							ParaStyleName = ScrStyleNames.NormalParagraph
						};
						string fnText = footnoteTexts[(s + v) % footnoteTexts.Length];
						footParaBldr.AppendRun(fnText, StyleUtils.CharStyleTextProps(null, m_wsEng));
						footParaBldr.CreateParagraph(footnote);
					}
				}

				paraBldr.CreateParagraph(section.ContentOA);
			}
		}

		/// <summary>
		/// Creates sections where each verse uses a different combination of character
		/// formatting (bold, italic, font-size, foreground colour). This forces the style
		/// resolver and text-properties builder to compute many distinct property sets,
		/// stressing the rendering property cache.
		/// </summary>
		protected void AddMixedStyleSections(IScrBook book, int sectionCount,
			int versesPerSection, int chapterStart)
		{
			var sectionFactory = Cache.ServiceLocator.GetInstance<IScrSectionFactory>();
			var stTextFactory = Cache.ServiceLocator.GetInstance<IStTextFactory>();
			int chapter = chapterStart;

			string[] proseFragments = new[]
			{
				"Blessed is the one who does not walk in step with the wicked. ",
				"But whose delight is in the law of the Lord, and who meditates on his law day and night. ",
				"That person is like a tree planted by streams of water, which yields its fruit in season. ",
				"Not so the wicked! They are like chaff that the wind blows away. ",
				"Therefore the wicked will not stand in the judgment, nor sinners in the assembly. ",
				"For the Lord watches over the way of the righteous, but the way of the wicked leads to destruction. ",
			};

			// Colour palette for rotating foreground colour
			int[] colours = new[]
			{
				(int)ColorUtil.ConvertColorToBGR(Color.FromArgb(0, 0, 0)),       // black
				(int)ColorUtil.ConvertColorToBGR(Color.FromArgb(128, 0, 0)),     // maroon
				(int)ColorUtil.ConvertColorToBGR(Color.FromArgb(0, 0, 128)),     // navy
				(int)ColorUtil.ConvertColorToBGR(Color.FromArgb(0, 100, 0)),     // dark green
				(int)ColorUtil.ConvertColorToBGR(Color.FromArgb(128, 0, 128)),   // purple
				(int)ColorUtil.ConvertColorToBGR(Color.FromArgb(139, 69, 19)),   // saddle brown
			};

			int[] fontSizes = new[] { 9000, 10000, 11000, 12000, 14000, 16000 }; // millipoints

			for (int s = 0; s < sectionCount; s++)
			{
				var section = sectionFactory.Create();
				book.SectionsOS.Add(section);

				section.HeadingOA = stTextFactory.Create();
				var headingBldr = new StTxtParaBldr(Cache) { ParaStyleName = ScrStyleNames.SectionHead };
				headingBldr.AppendRun($"Varied Styles Section {s + 1}",
					StyleUtils.CharStyleTextProps(null, m_wsEng));
				headingBldr.CreateParagraph(section.HeadingOA);

				section.ContentOA = stTextFactory.Create();

				var paraBldr = new StTxtParaBldr(Cache) { ParaStyleName = ScrStyleNames.NormalParagraph };

				if (s == 0 || s % 3 == 0)
				{
					paraBldr.AppendRun(chapter.ToString(),
						StyleUtils.CharStyleTextProps(ScrStyleNames.ChapterNumber, m_wsEng));
					chapter++;
				}

				for (int v = 1; v <= versesPerSection; v++)
				{
					// Verse number
					paraBldr.AppendRun(v.ToString() + "\u00A0",
						StyleUtils.CharStyleTextProps(ScrStyleNames.VerseNumber, m_wsEng));

					// Build a custom text props for this verse
					int idx = (s * versesPerSection + v);
					var bldr = TsStringUtils.MakePropsBldr();
					bldr.SetIntPropValues((int)FwTextPropType.ktptWs,
						(int)FwTextPropVar.ktpvDefault, m_wsEng);
					bldr.SetIntPropValues((int)FwTextPropType.ktptFontSize,
						(int)FwTextPropVar.ktpvMilliPoint, fontSizes[idx % fontSizes.Length]);
					bldr.SetIntPropValues((int)FwTextPropType.ktptForeColor,
						(int)FwTextPropVar.ktpvDefault, colours[idx % colours.Length]);

					// Alternate bold / italic
					if (idx % 3 == 0)
					{
						bldr.SetIntPropValues((int)FwTextPropType.ktptBold,
							(int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvForceOn);
					}
					if (idx % 4 == 0)
					{
						bldr.SetIntPropValues((int)FwTextPropType.ktptItalic,
							(int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvForceOn);
					}

					string prose = proseFragments[idx % proseFragments.Length];
					paraBldr.AppendRun(prose, bldr.GetTextProps());
				}

				paraBldr.CreateParagraph(section.ContentOA);
			}
		}

		/// <summary>
		/// Creates sections where all body text is Arabic (RTL). Section headings are
		/// English to create bidi mixing from the view's perspective. Chapter and verse
		/// numbers remain LTR (standard Scripture convention).
		/// </summary>
		protected void AddRtlSections(IScrBook book, int sectionCount,
			int versesPerSection, int chapterStart)
		{
			var sectionFactory = Cache.ServiceLocator.GetInstance<IScrSectionFactory>();
			var stTextFactory = Cache.ServiceLocator.GetInstance<IStTextFactory>();
			int chapter = chapterStart;

			// Arabic prose fragments (Bismillah-style phrases and common Quranic vocabulary)
			string[] arabicProse = new[]
			{
				"\u0628\u0650\u0633\u0652\u0645\u0650 \u0627\u0644\u0644\u0651\u0647\u0650 \u0627\u0644\u0631\u0651\u064E\u062D\u0652\u0645\u0646\u0650 \u0627\u0644\u0631\u0651\u064E\u062D\u064A\u0645\u0650. ",   // Bismillah
				"\u0627\u0644\u0652\u062D\u064E\u0645\u0652\u062F\u064F \u0644\u0650\u0644\u0651\u0647\u0650 \u0631\u064E\u0628\u0651\u0650 \u0627\u0644\u0652\u0639\u064E\u0627\u0644\u064E\u0645\u064A\u0646\u064E. ",   // Al-hamdu lillahi
				"\u0645\u064E\u0627\u0644\u0650\u0643\u0650 \u064A\u064E\u0648\u0652\u0645\u0650 \u0627\u0644\u062F\u0651\u064A\u0646\u0650. ",   // Maliki yawm al-din
				"\u0625\u0650\u064A\u0651\u064E\u0627\u0643\u064E \u0646\u064E\u0639\u0652\u0628\u064F\u062F\u064F \u0648\u064E\u0625\u0650\u064A\u0651\u064E\u0627\u0643\u064E \u0646\u064E\u0633\u0652\u062A\u064E\u0639\u064A\u0646\u064F. ",   // Iyyaka na'budu
				"\u0627\u0647\u0652\u062F\u0650\u0646\u064E\u0627 \u0627\u0644\u0635\u0651\u0650\u0631\u064E\u0627\u0637\u064E \u0627\u0644\u0652\u0645\u064F\u0633\u0652\u062A\u064E\u0642\u064A\u0645\u064E. ",   // Ihdina al-sirat
				"\u0635\u0650\u0631\u064E\u0627\u0637\u064E \u0627\u0644\u0651\u064E\u0630\u064A\u0646\u064E \u0623\u064E\u0646\u0652\u0639\u064E\u0645\u0652\u062A\u064E \u0639\u064E\u0644\u064E\u064A\u0652\u0647\u0650\u0645\u0652. ",   // Sirat alladhina
			};

			for (int s = 0; s < sectionCount; s++)
			{
				var section = sectionFactory.Create();
				book.SectionsOS.Add(section);

				// English heading (LTR in an otherwise RTL view)
				section.HeadingOA = stTextFactory.Create();
				var headingBldr = new StTxtParaBldr(Cache) { ParaStyleName = ScrStyleNames.SectionHead };
				headingBldr.AppendRun($"Section {s + 1}: Arabic Scripture",
					StyleUtils.CharStyleTextProps(null, m_wsEng));
				headingBldr.CreateParagraph(section.HeadingOA);

				section.ContentOA = stTextFactory.Create();
				var paraBldr = new StTxtParaBldr(Cache) { ParaStyleName = ScrStyleNames.NormalParagraph };

				if (s == 0 || s % 3 == 0)
				{
					paraBldr.AppendRun(chapter.ToString(),
						StyleUtils.CharStyleTextProps(ScrStyleNames.ChapterNumber, m_wsEng));
					chapter++;
				}

				for (int v = 1; v <= versesPerSection; v++)
				{
					paraBldr.AppendRun(v.ToString() + "\u00A0",
						StyleUtils.CharStyleTextProps(ScrStyleNames.VerseNumber, m_wsEng));

					// Arabic prose (RTL)
					string prose = arabicProse[(s * versesPerSection + v) % arabicProse.Length];
					paraBldr.AppendRun(prose, StyleUtils.CharStyleTextProps(null, m_wsAr));
				}

				paraBldr.CreateParagraph(section.ContentOA);
			}
		}

		/// <summary>
		/// Creates sections where each verse contains runs in three different writing systems
		/// (English, Arabic, French) within the same paragraph. This forces the rendering
		/// engine to handle font fallback, writing-system switching, mixed bidi text, and
		/// line-breaking across WS boundaries.
		/// </summary>
		protected void AddMultiWsSections(IScrBook book, int sectionCount,
			int versesPerSection, int chapterStart)
		{
			var sectionFactory = Cache.ServiceLocator.GetInstance<IScrSectionFactory>();
			var stTextFactory = Cache.ServiceLocator.GetInstance<IStTextFactory>();
			int chapter = chapterStart;

			string[] englishProse = new[]
			{
				"In the beginning was the Word. ",
				"The light shines in the darkness. ",
				"Grace and truth came through Him. ",
			};

			string[] frenchProse = new[]
			{
				"Au commencement \u00E9tait la Parole. ",
				"La lumi\u00E8re brille dans les t\u00E9n\u00E8bres. ",
				"La gr\u00E2ce et la v\u00E9rit\u00E9 sont venues par Lui. ",
			};

			string[] arabicProse = new[]
			{
				"\u0641\u064A \u0627\u0644\u0628\u062F\u0621 \u0643\u0627\u0646 \u0627\u0644\u0643\u0644\u0645\u0629. ",  // In the beginning was the Word
				"\u0627\u0644\u0646\u0648\u0631 \u064A\u0636\u064A\u0621 \u0641\u064A \u0627\u0644\u0638\u0644\u0627\u0645. ",  // The light shines in the darkness
				"\u0627\u0644\u0646\u0639\u0645\u0629 \u0648\u0627\u0644\u062D\u0642 \u0628\u0650\u0647. ",  // Grace and truth through Him
			};

			for (int s = 0; s < sectionCount; s++)
			{
				var section = sectionFactory.Create();
				book.SectionsOS.Add(section);

				section.HeadingOA = stTextFactory.Create();
				var headingBldr = new StTxtParaBldr(Cache) { ParaStyleName = ScrStyleNames.SectionHead };
				headingBldr.AppendRun($"Multi-WS Section {s + 1}",
					StyleUtils.CharStyleTextProps(null, m_wsEng));
				headingBldr.CreateParagraph(section.HeadingOA);

				section.ContentOA = stTextFactory.Create();
				var paraBldr = new StTxtParaBldr(Cache) { ParaStyleName = ScrStyleNames.NormalParagraph };

				if (s == 0 || s % 3 == 0)
				{
					paraBldr.AppendRun(chapter.ToString(),
						StyleUtils.CharStyleTextProps(ScrStyleNames.ChapterNumber, m_wsEng));
					chapter++;
				}

				for (int v = 1; v <= versesPerSection; v++)
				{
					paraBldr.AppendRun(v.ToString() + "\u00A0",
						StyleUtils.CharStyleTextProps(ScrStyleNames.VerseNumber, m_wsEng));

					int idx = (s * versesPerSection + v);

					// Rotate: English → Arabic → French within each verse
					paraBldr.AppendRun(englishProse[idx % englishProse.Length],
						StyleUtils.CharStyleTextProps(null, m_wsEng));
					paraBldr.AppendRun(arabicProse[idx % arabicProse.Length],
						StyleUtils.CharStyleTextProps(null, m_wsAr));
					paraBldr.AppendRun(frenchProse[idx % frenchProse.Length],
						StyleUtils.CharStyleTextProps(null, m_wsFr));
				}

				paraBldr.CreateParagraph(section.ContentOA);
			}
		}

		#endregion

		#region Lex Entry Scenario Data

		/// <summary>
		/// Creates a lexical entry scenario with nested senses at the specified depth and breadth.
		/// This is the primary scenario for tracking the exponential rendering overhead in
		/// XmlVc's <c>visibility="ifdata"</c> double-render pattern.
		/// </summary>
		/// <param name="depth">Number of nesting levels (2 = senses with one level of subsenses).</param>
		/// <param name="breadth">Number of child senses per parent at each level.</param>
		/// <remarks>
		/// <para>Total sense count = breadth + breadth^2 + ... + breadth^depth = breadth*(breadth^depth - 1)/(breadth - 1).</para>
		/// <para>With the ifdata double-render, each level doubles the work, so rendering time
		/// grows as O(breadth^depth * 2^depth) = O((2*breadth)^depth). Depth 6 with breadth 2
		/// produces 126 senses and ~4096x the per-sense overhead of depth 1.</para>
		/// </remarks>
		private void CreateLexEntryScenario(int depth, int breadth)
		{
			// Ensure LexDb exists
			if (Cache.LangProject.LexDbOA == null)
			{
				Cache.ServiceLocator.GetInstance<ILexDbFactory>().Create();
			}

			var entryFactory = Cache.ServiceLocator.GetInstance<ILexEntryFactory>();
			var senseFactory = Cache.ServiceLocator.GetInstance<ILexSenseFactory>();
			var morphFactory = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>();

			// Create the entry with a headword
			var entry = entryFactory.Create();
			var morph = morphFactory.Create();
			entry.LexemeFormOA = morph;
			morph.Form.set_String(m_wsEng, TsStringUtils.MakeString("benchmark-entry", m_wsEng));

			// Recursively create the sense tree
			CreateNestedSenses(entry, senseFactory, depth, breadth, "", 1);

			// Set root to entry HVO with LexEntry.Senses as the containing flid
			m_hvoRoot = entry.Hvo;
			m_flidContainingTexts = LexEntryTags.kflidSenses;
			m_frag = LexEntryVc.kFragEntry;
		}

		/// <summary>
		/// Recursively creates a tree of senses/subsenses.
		/// </summary>
		/// <param name="owner">The owning entry or sense.</param>
		/// <param name="senseFactory">Factory for creating new senses.</param>
		/// <param name="remainingDepth">Remaining nesting levels to create.</param>
		/// <param name="breadth">Number of children at each level.</param>
		/// <param name="prefix">Hierarchical number prefix (e.g., "1.2.").</param>
		/// <param name="startNumber">Starting sense number at this level.</param>
		private void CreateNestedSenses(ICmObject owner, ILexSenseFactory senseFactory,
			int remainingDepth, int breadth, string prefix, int startNumber)
		{
			if (remainingDepth <= 0)
				return;

			for (int i = 0; i < breadth; i++)
			{
				var sense = senseFactory.Create();
				string senseNum = prefix + (startNumber + i);

				// Add sense to entry or parent sense
				var entry = owner as ILexEntry;
				if (entry != null)
					entry.SensesOS.Add(sense);
				else
					((ILexSense)owner).SensesOS.Add(sense);

				// Set gloss and definition
				string gloss = $"gloss {senseNum}";
				string definition = $"This is the definition for sense {senseNum}, which demonstrates " +
					$"nested rendering at depth {remainingDepth} with {breadth}-way branching.";

				sense.Gloss.set_String(m_wsEng, TsStringUtils.MakeString(gloss, m_wsEng));
				sense.Definition.set_String(m_wsEng, TsStringUtils.MakeString(definition, m_wsEng));

				// Recurse for subsenses
				CreateNestedSenses(sense, senseFactory, remainingDepth - 1, breadth,
					senseNum + ".", 1);
			}
		}

		#endregion
	}
}
