// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Text;
using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.CacheLight;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Test.TestUtils;
using SIL.Utils;

namespace SIL.FieldWorks.Common.RootSites.SimpleRootSiteTests
{

	#region class SimpleStyleSheet
	public class SimpleStyleSheet : IVwStylesheet
	{
		ISilDataAccess m_da;

		public SimpleStyleSheet(ISilDataAccess da)
		{
			m_da = da;
		}

		#region IVwStylesheet Members
		public int CStyles
		{
			get { return 1; }
		}

		public void CacheProps(int cch, string _rgchName, int hvoStyle, ITsTextProps _ttp)
		{
			throw new NotImplementedException();
		}

		public ISilDataAccess DataAccess
		{
			get { return m_da; }
		}

		public void Delete(int hvoStyle)
		{
			throw new NotImplementedException();
		}

		public string GetBasedOn(string bstrName)
		{
			return string.Empty;
		}

		public int GetContext(string bstrName)
		{
			return 0;
		}

		public string GetDefaultBasedOnStyleName()
		{
			return "Normal";
		}

		public string GetDefaultStyleForContext(int nContext, bool fCharStyle)
		{
			return "Normal";
		}

		public string GetNextStyle(string bstrName)
		{
			return "Normal";
		}

		public ITsTextProps GetStyleRgch(int cch, string _rgchName)
		{
			return TsPropsFactoryClass.Create().MakeProps(null, 0, 0);
		}

		public int GetType(string bstrName)
		{
			return 0;
		}

		public bool IsBuiltIn(string bstrName)
		{
			return true;
		}

		public bool IsModified(string bstrName)
		{
			return false;
		}

		public int MakeNewStyle()
		{
			throw new NotImplementedException();
		}

		public ITsTextProps NormalFontStyle
		{
			get { return TsPropsFactoryClass.Create().MakeProps(null, 0, 0); }
		}

		public void PutStyle(string bstrName, string bstrUsage, int hvoStyle, int hvoBasedOn, int hvoNext, int nType, bool fBuiltIn, bool fModified, ITsTextProps _ttp)
		{
			throw new NotImplementedException();
		}

		public bool get_IsStyleProtected(string bstrName)
		{
			return true;
		}

		public int get_NthStyle(int ihvo)
		{
			return 1234;
		}

		public string get_NthStyleName(int ihvo)
		{
			return "Normal";
		}
		#endregion
	}
	#endregion

	public static class SimpleRootsiteTestsConstants
	{
		internal const int kclsidProject = 1;
		internal const int kflidDocTitle = 1001;
		internal const int kflidDocDivisions= 1002;
		internal const int kflidDocFootnotes = 1003;

		internal const int kclsidSection = 7;
		internal const int kflidSectionStuff = 7001;

		internal const int kclsidStText = 14;
		internal const int kflidTextParas = 14001;

		internal const int kclsidStTxtPara = 16;
		internal const int kflidParaContents = 16001;
		internal const int kflidParaProperties = 16002;

		internal const int kclsidStFootnote = 25;
		internal const int kflidFootnoteMarker = 25002;
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Base class for tests that use <see cref="SimpleRootSite"/>. This class is specific for
	/// Rootsite tests.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification="Unit test. Variable disposed in Teardown method")]
	public class SimpleRootsiteTestsBase<T> : BaseTest
		where T: IRealDataCache, new()
	{
		/// <summary>Defines the possible languages</summary>
		[Flags]
		public enum Lng
		{
			/// <summary>No paragraphs</summary>
			None = 0,
			/// <summary>English paragraphs</summary>
			English = 1,
			/// <summary>French paragraphs</summary>
			French = 2,
			/// <summary>UserWs paragraphs</summary>
			UserWs = 4,
			/// <summary>Empty paragraphs</summary>
			Empty = 8,
			/// <summary>Paragraph with 3 writing systems</summary>
			Mixed = 16,
		}

		#region Data members

		/// <summary>The data cache</summary>
		protected T m_cache;
		/// <summary>The draft form</summary>
		protected SimpleBasicView m_basicView;
		/// <summary></summary>
		protected int m_hvoRoot;
		/// <summary>Fragment for view constructor</summary>
		protected int m_frag = 1;

		/// <summary>Text for the first and third test paragraph (French)</summary>
		internal const string kFirstParaFra = "C'est une paragraph en francais.";
		/// <summary>Text for the second and fourth test paragraph (French).</summary>
		/// <remarks>This text needs to be shorter than the text for the first para!</remarks>
		internal const string kSecondParaFra = "C'est une deuxieme paragraph.";

		/// <summary>Writing System Manager (reset for each test)</summary>
		protected WritingSystemManager m_wsManager;
		/// <summary>Id of English Writing System (reset for each test)</summary>
		protected int m_wsEng;
		/// <summary>Id of French Writing System (reset for each test)</summary>
		protected int m_wsFrn;
		/// <summary>Id of German Writing System (reset for each test)</summary>
		protected int m_wsDeu;
		/// <summary>Id of User Writing System (reset for each test)</summary>
		protected int m_wsUser;
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fixture setup
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			base.FixtureSetup();

			SetupTestModel(Properties.Resources.TextCacheModel_xml);

			m_cache = new T();
			m_cache.MetaDataCache = MetaDataCache.CreateMetaDataCache("TestModel.xml");
			m_cache.ParaContentsFlid = SimpleRootsiteTestsConstants.kflidParaContents;
			m_cache.ParaPropertiesFlid = SimpleRootsiteTestsConstants.kflidParaProperties;
			m_cache.TextParagraphsFlid = SimpleRootsiteTestsConstants.kflidTextParas;

			Debug.Assert(m_wsManager == null);
			m_wsManager = new WritingSystemManager();
			m_cache.WritingSystemFactory = m_wsManager;

			WritingSystem enWs;
			m_wsManager.GetOrSet("en", out enWs);
			m_wsEng = enWs.Handle;

			WritingSystem frWs;
			m_wsManager.GetOrSet("fr", out frWs);
			m_wsFrn = frWs.Handle;

			WritingSystem deWs;
			m_wsManager.GetOrSet("de", out deWs);
			m_wsDeu = deWs.Handle;

			m_wsManager.UserWs = m_wsEng;
			m_wsUser = m_wsManager.UserWs;
		}

		public static void SetupTestModel(string cacheModelfile)
		{
			FileUtils.Manager.SetFileAdapter(new MockFileOS());
			using (TextWriter fw = FileUtils.OpenFileForWrite("TestModel.xsd", Encoding.UTF8))
			{
				fw.Write(CacheLightTests.Properties.Resources.TestModel_xsd);
				fw.Close();
			}
			using (TextWriter fw = FileUtils.OpenFileForWrite("TestModel.xml", Encoding.UTF8))
			{
				fw.Write(cacheModelfile);
				fw.Close();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Teardown
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureTearDown]
		public override void FixtureTeardown()
		{
			FileUtils.Manager.Reset();

			// GrowToWord causes a Char Property Engine to be created, and the test runner
			// fails if we don't shut the factory down.
			m_cache.Dispose();
			m_cache = default(T);

			base.FixtureTeardown();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Create a new basic view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[SetUp]
		public virtual void TestSetup()
		{
			m_cache.ClearAllData();
			m_hvoRoot = m_cache.MakeNewObject(SimpleRootsiteTestsConstants.kclsidProject, 0, -1, -1);

			var styleSheet = new SimpleStyleSheet(m_cache);

			Assert.IsNull(m_basicView);
			m_basicView = new SimpleBasicView();
			m_basicView.Cache = m_cache;
			m_basicView.Visible = false;
			m_basicView.StyleSheet = styleSheet;
			m_basicView.WritingSystemFactory = m_wsManager;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shuts down the view
		/// </summary>
		/// <remarks>This method is called after each test</remarks>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public virtual void TestTearDown()
		{
			m_basicView.CloseRootBox();
			m_basicView.Dispose();
			m_basicView = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set up the test form.
		/// </summary>
		/// <param name="display"></param>
		/// ------------------------------------------------------------------------------------
		protected void ShowForm(SimpleViewVc.DisplayType display)
		{
			m_basicView.DisplayType = display;

			// We don't actually want to show it, but we need to force the view to create the root
			// box and lay it out so that various test stuff can happen properly.
			m_basicView.Width = 300;
			m_basicView.Height = 307 - 25;
			m_basicView.MakeRoot(m_hvoRoot, SimpleRootsiteTestsConstants.kflidDocFootnotes, m_frag, m_wsEng);
			m_basicView.CallLayout();
			m_basicView.AutoScrollPosition = new Point(0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Insert the specified paragraphs and show the dialog
		/// </summary>
		/// <param name="lng">Language</param>
		/// <param name="display"></param>
		/// ------------------------------------------------------------------------------------
		protected void ShowForm(Lng lng, SimpleViewVc.DisplayType display)
		{
			if ((lng & Lng.English) == Lng.English)
				MakeEnglishParagraphs();
			if ((lng & Lng.French) == Lng.French)
				MakeFrenchParagraphs();
			if ((lng & Lng.UserWs) == Lng.UserWs)
				MakeUserWsParagraphs();
			if ((lng & Lng.Empty) == Lng.Empty)
				MakeEmptyParagraphs();
			if ((lng & Lng.Mixed) == Lng.Mixed)
				MakeMixedWsParagraph();

			ShowForm(display);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add English paragraphs
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void MakeEnglishParagraphs()
		{
			AddParagraphs(m_wsEng, SimpleBasicView.kFirstParaEng, SimpleBasicView.kSecondParaEng);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add French paragraphs
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void MakeFrenchParagraphs()
		{
			AddParagraphs(m_wsFrn, kFirstParaFra, kSecondParaFra);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add paragraphs with the user interface writing system
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void MakeUserWsParagraphs()
		{
			AddParagraphs(m_wsUser, "blabla", "abc");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a run of text to the specified paragraph
		/// </summary>
		/// <param name="hvoPara">HVO of the paragraph</param>
		/// <param name="runText">Text of the run to add</param>
		/// <param name="ws">writing system ID</param>
		/// ------------------------------------------------------------------------------------
		public void AddRunToMockedPara(int hvoPara, string runText, int ws)
		{
			var propFact = TsPropsFactoryClass.Create();
			var runStyle = propFact.MakeProps(null, ws, 0);
			ITsString contents = m_cache.get_StringProp(hvoPara, SimpleRootsiteTestsConstants.kflidParaContents);
			var bldr = contents.GetBldr();
			bldr.Replace(bldr.Length, bldr.Length, runText, runStyle);
			m_cache.SetString(hvoPara, SimpleRootsiteTestsConstants.kflidParaContents, bldr.GetString());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes a paragraph containing runs, each of which has a different writing system.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void MakeMixedWsParagraph()
		{
			int para = AddFootnoteAndParagraph();

			AddRunToMockedPara(para, "ws1", m_wsEng);
			AddRunToMockedPara(para, "ws2", m_wsDeu);
			AddRunToMockedPara(para, "ws3", m_wsFrn);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add empty paragraphs
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void MakeEmptyParagraphs()
		{
			AddParagraphs(m_wsUser, "", "");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a footnote with a single paragraph to the cache
		/// </summary>
		/// <returns>HVO of the new paragraph</returns>
		/// ------------------------------------------------------------------------------------
		protected int AddFootnoteAndParagraph()
		{
			int cTexts = m_cache.get_VecSize(m_hvoRoot, SimpleRootsiteTestsConstants.kflidDocFootnotes);
			int hvoFootnote = m_cache.MakeNewObject(SimpleRootsiteTestsConstants.kclsidStFootnote, m_hvoRoot, SimpleRootsiteTestsConstants.kflidDocFootnotes, cTexts);
			int hvoPara = m_cache.MakeNewObject(SimpleRootsiteTestsConstants.kclsidStTxtPara, hvoFootnote, SimpleRootsiteTestsConstants.kflidTextParas, 0);
			ITsStrFactory tsStrFactory = TsStrFactoryClass.Create();
			m_cache.CacheStringProp(hvoFootnote, SimpleRootsiteTestsConstants.kflidFootnoteMarker,
				tsStrFactory.MakeString("a", m_wsFrn));
			m_cache.CacheStringProp(hvoPara, SimpleRootsiteTestsConstants.kflidParaContents,
				tsStrFactory.MakeString(string.Empty, m_wsFrn));
			return hvoPara;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds two footnotes and their paragraphs to the cache
		/// </summary>
		/// <param name="ws">The writing system ID</param>
		/// <param name="firstPara">Text of the first paragraph</param>
		/// <param name="secondPara">Text of the second paragraph</param>
		/// ------------------------------------------------------------------------------------
		private void AddParagraphs(int ws, string firstPara, string secondPara)
		{
			var para1 = AddFootnoteAndParagraph();
			var para2 = AddFootnoteAndParagraph();
			AddRunToMockedPara(para1, firstPara, ws);
			AddRunToMockedPara(para2, secondPara, ws);
		}
	}
}
