// --------------------------------------------------------------------------------------------
#region
// <copyright from='2002' to='2008' company='SIL International'>
//		Copyright (c) 2002-2008, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FdoTestBase.cs
// Responsibility: RandyR
// Last reviewed:
//
// <remarks>
// Implements FdoTestBase, the base class for the FDO tests
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.Test.TestUtils;
using SIL.Utils;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO.Infrastructure.Impl;

namespace SIL.FieldWorks.FDO.FDOTests
{
	#region FdoTestBase class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Base class for FDO tests that use an FdoCache.
	/// Subclasses must implement the CreateCache method to get an FdoCache to work with.
	/// There are subclasses of this class for each supported backend provider.
	///
	/// For normal FDO tests (testing FDO public API),
	/// you should derive from MemoryOnlyBackendProviderTestBase,
	/// and add relevant test data to its nearly empty LanguageProperty.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public abstract class FdoTestBase : BaseTest
	{
		private FdoCache m_cache;
		/// <summary></summary>
		protected bool m_internalRestart;
		/// <summary></summary>
		protected IActionHandler m_actionHandler;

		/// <summary>
		/// Get the FdoCache.
		/// </summary>
		public FdoCache Cache
		{
			get { return m_cache; }
		}

		/// <summary>
		/// Restart the cache.
		/// </summary>
		/// <param name="doCommit">'True' to end the task and commit the changes. 'False' to skip the commit.</param>
		protected virtual void RestartCache(bool doCommit)
		{
			if (doCommit)
			{
				m_cache.DomainDataByFlid.EndNonUndoableTask();
				m_cache.ServiceLocator.GetInstance<IUndoStackManager>().Save();
			}

			m_internalRestart = true;

			try
			{
				FixtureTeardown();

				FixtureSetup();
			}
			finally
			{
				m_internalRestart = false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If a test overrides this, it should call this base implementation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			base.FixtureSetup();

			SetupEverythingButBase();
		}

		/// <summary>
		///
		/// </summary>
		protected void SetupEverythingButBase()
		{
			m_cache = CreateCache();
			m_actionHandler = m_cache.ServiceLocator.GetInstance<IActionHandler>();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If a test overrides this, it should call this base implementation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureTearDown]
		public override void FixtureTeardown()
		{
			DisposeEverythingButBase();

			base.FixtureTeardown();
		}

		/// <summary>
		///
		/// </summary>
		protected void DisposeEverythingButBase()
		{
			if (m_cache != null)
			{
				if (m_cache.ThreadHelper != null)
					m_cache.ThreadHelper.Dispose();
				m_cache.Dispose();
			}
			m_cache = null;
			m_actionHandler = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Done before each test.
		/// Overriders should call base method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public virtual void TestSetup()
		{
			ClipboardUtils.Manager.SetClipboardAdapter(new ClipboardStub());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Done after each test.
		/// Overriders should call base method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public virtual void TestTearDown()
		{
			/* Do nothing. */
		}

		/// <summary>
		/// Called by FixtureSetup().
		/// Subclasses must override this to create an FdoCache that is loaded with
		/// whatever data is needed.
		/// </summary>
		/// <returns>An FdoCache that is loaded with data used by the tests.</returns>
		protected abstract FdoCache CreateCache();

		/// <summary>
		/// Actually create the system.
		/// Called by subclass overrides of CreateCache(), with parameters that suit
		/// the subclass.
		/// </summary>
		/// <param name="projectId"></param>
		/// <param name="loadType"></param>
		/// <returns>a working FdoCache</returns>
		protected FdoCache BootstrapSystem(IProjectIdentifier projectId, BackendBulkLoadDomain loadType)
		{
			// This thread helper will be disposed in FixtureTeardown along with the cache.
			var retval = m_internalRestart ? FdoCache.CreateCacheFromExistingData(projectId, "en", new DummyProgressDlg()) :
				FdoCache.CreateCacheWithNewBlankLangProj(projectId, "en", "fr", "en", new ThreadHelper());
			var dataSetup = retval.ServiceLocator.GetInstance<IDataSetup>();
			dataSetup.LoadDomain(loadType);
			return retval;
		}
	}
	#endregion

	#region MemoryOnlyBackendProviderBasicTestBase class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Base class for testing the FdoCache with the FDOBackendProviderType.kMemoryOnly
	/// backend provider where each test should start with a fresh system.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public abstract class MemoryOnlyBackendProviderBasicTestBase : FdoTestBase
	{
		/// <summary>
		/// Override to create and load a very basic cache.
		/// </summary>
		/// <returns>An FdoCache that has no data at all in it.</returns>
		protected override FdoCache CreateCache()
		{
			// Make a dummy random file so tests work correctly
			string projectPath = Path.GetFullPath(Path.GetRandomFileName());
			return BootstrapSystem(new TestProjectId(FDOBackendProviderType.kMemoryOnly, projectPath), BackendBulkLoadDomain.All);
		}

		/// <summary>
		/// Override to do nothing.
		/// </summary>
		/// <param name="doCommit">'True' to end the task and commit the changes. 'False' to skip the commit.</param>
		protected override void RestartCache(bool doCommit)
		{
			// Do nothing.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Change the default vernacular writing system. (This removes any previous vernacular
		/// writing system(s) from the list.)
		/// </summary>
		/// <param name="ws">the writing system which will become the default
		/// vernacular writing system</param>
		/// ------------------------------------------------------------------------------------
		public void ChangeDefaultVernWs(IWritingSystem ws)
		{
			Cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems.Clear();
			Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem = ws;
		}

		#region Footnote stuff
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add a back translation footnote ref ORC in the given translation for the given footnote
		/// </summary>
		/// <param name="trans">The given translation, usually a back translation</param>
		/// <param name="ichPos">The 0-based character offset into the translation string
		/// at which we will insert the reference ORC</param>
		/// <param name="ws">writing system of the ORC</param>
		/// <param name="footnote">The given footnote</param>
		/// ------------------------------------------------------------------------------------
		public void AddBtFootnote(ICmTranslation trans, int ichPos, int ws, IStFootnote footnote)
		{
			// Insert a footnote reference ORC into the given translation string
			ITsStrBldr tsStrBldr = trans.Translation.get_String(ws).GetBldr();
			TsStringUtils.InsertOrcIntoPara(footnote.Guid, FwObjDataTypes.kodtNameGuidHot, tsStrBldr, ichPos, ichPos, ws);
			trans.Translation.set_String(ws, tsStrBldr.GetString());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add a back translation footnote in the given translation for the given footnote.
		/// Inserts a ref ORC in the translation and sets the the BT text in the footnote.
		/// </summary>
		/// <param name="trans">The given back translation of an StTxtPara</param>
		/// <param name="ichPos">The 0-based character offset into the back translation string
		/// at which we will insert the reference ORC</param>
		/// <param name="ws">writing system of the BT and ORC</param>
		/// <param name="footnote">The given footnote</param>
		/// <param name="footnoteBtText">text for the back translation of the footnote</param>
		/// <returns>the back translation of the given footnote</returns>
		/// ------------------------------------------------------------------------------------
		public ICmTranslation AddBtFootnote(ICmTranslation trans, int ichPos, int ws, IStFootnote footnote, string footnoteBtText)
		{
			AddBtFootnote(trans, ichPos, ws, footnote);

			// Add the given footnote BT text to the footnote.
			IStTxtPara para = (IStTxtPara)footnote.ParagraphsOS[0];
			ICmTranslation footnoteTrans = para.GetOrCreateBT();
			ITsStrBldr tssFootnoteBldr = footnoteTrans.Translation.get_String(ws).GetBldr();
			tssFootnoteBldr.ReplaceRgch(0, 0, footnoteBtText, footnoteBtText.Length, StyleUtils.CharStyleTextProps(null, ws));
			footnoteTrans.Translation.set_String(ws, tssFootnoteBldr.GetString());
			return footnoteTrans;
		}
		#endregion
	}
	#endregion

	#region MemoryOnlyBackendProviderTestBase class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Base class for testing the FdoCache with the FDOBackendProviderType.kMemoryOnly
	/// backend provider. This class does *not* restore the data between test runs.
	/// Derive from MemoryOnlyBackendProviderRestoredForEachTestTestBase if that is desired.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public abstract class MemoryOnlyBackendProviderTestBase : MemoryOnlyBackendProviderBasicTestBase
	{
		// REVIEW (TimS): Why do we have this class?
	}
	#endregion

	#region MemoryOnlyBackendProviderRestoredForEachTestTestBase class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Base class for testing the FdoCache with the FDOBackendProviderType.kMemoryOnly
	/// backend provider where each test should start with an 'almost' fresh system.
	/// It is 'almost' a fresh system, since it doesn't actually create a new FDO system
	/// for every test. It just loops through Undo as long as possible "MemoryOnlyUndoForEachTestBase "
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public abstract class MemoryOnlyBackendProviderRestoredForEachTestTestBase : MemoryOnlyBackendProviderBasicTestBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Override to start an undoable UOW.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void TestSetup()
		{
			base.TestSetup();

			// This allows tests to do any kind of data changes
			// without worrying about starting a UOW.
			m_actionHandler.BeginUndoTask("Undo doing stuff", "Redo doing stuff");

			CreateTestData();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the test data.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void CreateTestData()
		{
			// Default is to do nothing
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Override to end the undoable UOW, Undo everything, and 'commit',
		/// which will essentially clear out the Redo stack.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void TestTearDown()
		{
			UndoAll();
			base.TestTearDown();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// End the undoable UOW and Undo everything.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void UndoAll()
		{
			IActionHandler actionHandler = Cache.ActionHandlerAccessor;
			// This ends a UOW, but with no Commit.
			if (actionHandler.CurrentDepth > 0)
				actionHandler.EndUndoTask();
			// Undo the UOW (or more than one of them, if the test made new ones).
			while (actionHandler.CanUndo())
				actionHandler.Undo();

			// Need to 'Commit' to clear out redo stack,
			// since nothing is really saved.
			actionHandler.Commit();
		}

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a run of text to the specified paragraph
		/// </summary>
		/// <param name="para"></param>
		/// <param name="runText"></param>
		/// <param name="runStyleName"></param>
		/// ------------------------------------------------------------------------------------
		public void AddRunToMockedPara(IStTxtPara para, string runText, string runStyleName)
		{
			ITsPropsFactory propFact = TsPropsFactoryClass.Create();
			ITsTextProps runStyle = propFact.MakeProps(runStyleName, Cache.DefaultVernWs, 0);
			ITsStrBldr bldr = para.Contents.GetBldr();
			bldr.Replace(bldr.Length, bldr.Length, runText, runStyle);
			para.Contents = bldr.GetString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a run of text to the specified paragraph
		/// </summary>
		/// <param name="para"></param>
		/// <param name="runText"></param>
		/// <param name="ws"></param>
		/// ------------------------------------------------------------------------------------
		public void AddRunToMockedPara(IStTxtPara para, string runText, int ws)
		{
			ITsPropsFactory propFact = TsPropsFactoryClass.Create();
			ITsTextProps runStyle = propFact.MakeProps(null, ws, 0);
			ITsStrBldr bldr = para.Contents.GetBldr();
			bldr.Replace(bldr.Length, bldr.Length, runText, runStyle);
			para.Contents = bldr.GetString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a run of text to the specified translation
		/// </summary>
		/// <param name="trans"></param>
		/// <param name="btWS"></param>
		/// <param name="runText"></param>
		/// <param name="runStyleName"></param>
		/// ------------------------------------------------------------------------------------
		protected void AddRunToMockedTrans(ICmTranslation trans, int btWS, string runText, string runStyleName)
		{
			AddRunToMockedTrans(trans, btWS, btWS, runText, runStyleName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a run of text to the specified translation
		/// </summary>
		/// <param name="trans">The translation where the run of text will be appended.</param>
		/// <param name="btWS">The writing system of the back translation</param>
		/// <param name="runWS">The writing system of the run</param>
		/// <param name="runText">The run text.</param>
		/// <param name="runStyleName">Name of the run style.</param>
		/// ------------------------------------------------------------------------------------
		protected void AddRunToMockedTrans(ICmTranslation trans, int btWS, int runWS, string runText, string runStyleName)
		{
			ITsPropsFactory propFact = TsPropsFactoryClass.Create();
			ITsTextProps runProps = propFact.MakeProps(runStyleName, runWS, 0);
			ITsStrBldr bldr = trans.Translation.get_String(btWS).GetBldr();
			bldr.Replace(bldr.Length, bldr.Length, runText, runProps);
			trans.Translation.set_String(btWS, bldr.GetString());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add an empty back translation to the paragraph, or return the existing one.
		/// </summary>
		/// <param name="owner">the owning paragraph</param>
		/// <param name="wsTrans"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected ICmTranslation AddBtToMockedParagraph(IStTxtPara owner, int wsTrans)
		{
			ICmTranslation trans = owner.GetOrCreateBT();
			trans.Translation.set_String(wsTrans, Cache.TsStrFactory.MakeString(string.Empty, wsTrans));
			return trans;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a free translation to the specified segment of the specified paragraph for the
		/// specified writing systems.
		/// </summary>
		/// <param name="para">The paragraph.</param>
		/// <param name="iSeg">The index of the segment.</param>
		/// <param name="freeTrans">The text of the translation.</param>
		/// <param name="wss">The list of writing systems.</param>
		/// ------------------------------------------------------------------------------------
		protected static void AddSegmentFt(IStTxtPara para, int iSeg, string freeTrans,
			params int[] wss)
		{
			ISegment segment = para.SegmentsOS[iSeg];
			foreach (int ws in wss)
				segment.FreeTranslation.set_String(ws, freeTrans);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a run to the free translation of the specified segment of the specified
		/// paragraph having the specified style in the default analysis writing system.
		/// </summary>
		/// <param name="para">The paragraph.</param>
		/// <param name="iSeg">The index of the segment.</param>
		/// <param name="freeTrans">The text of the translation.</param>
		/// <param name="styleName">The name of the style.</param>
		/// ------------------------------------------------------------------------------------
		protected void AddRunToSegmentFt(IStTxtPara para, int iSeg, string freeTrans,
			string styleName)
		{
			AddRunToSegmentFt(para, iSeg, freeTrans, styleName, Cache.DefaultAnalWs);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a run to the free translation of the specified segment of the specified
		/// paragraph having the specified style for the specified writing systems.
		/// </summary>
		/// <param name="para">The paragraph.</param>
		/// <param name="iSeg">The index of the segment.</param>
		/// <param name="freeTrans">The text of the translation.</param>
		/// <param name="styleName">The name of the style.</param>
		/// <param name="wss">The list of writing systems.</param>
		/// ------------------------------------------------------------------------------------
		protected void AddRunToSegmentFt(IStTxtPara para, int iSeg, string freeTrans,
			string styleName, params int[] wss)
		{
			ISegment segment = para.SegmentsOS[iSeg];
			foreach (int ws in wss)
			{
				ITsStrBldr bldr = segment.FreeTranslation.get_String(ws).GetBldr();
				bldr.Replace(bldr.Length, bldr.Length, freeTrans, StyleUtils.CharStyleTextProps(styleName, ws));
				segment.FreeTranslation.set_String(ws, bldr.GetString());
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Append a run of text to a back translation of a paragraph.
		/// </summary>
		/// <param name="para">given paragraph</param>
		/// <param name="ws">given writing system for the back translation</param>
		/// <param name="runText">given text to append to back translation</param>
		/// ------------------------------------------------------------------------------------
		public void AppendRunToBt(IStTxtPara para, int ws, string runText)
		{
			ICmTranslation trans = para.GetOrCreateBT();
			ITsStrBldr bldr = trans.Translation.get_String(ws).GetBldr();
			ITsTextProps ttp = StyleUtils.CharStyleTextProps(null, ws);
			int bldrLength = bldr.Length;
			bldr.ReplaceRgch(bldrLength, bldrLength, runText, runText.Length, ttp);
			trans.Translation.set_String(ws, bldr.GetString());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add an empty but properly initialized (with WS) content paragraph to the specified
		/// StText in the mock fdocache
		/// </summary>
		/// <param name="owner">the hvo of the StText</param>
		/// <param name="paraStyleName">the paragraph style name</param>
		/// ------------------------------------------------------------------------------------
		protected IStTxtPara AddParaToMockedText(IStText owner, string paraStyleName)
		{
			IStTxtPara para = owner.AddNewTextPara(paraStyleName);
			para.Contents = Cache.TsStrFactory.MakeString(string.Empty, Cache.DefaultVernWs);
			return para;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds an interlinear text to the language projectin the mocked fdocache
		/// </summary>
		/// <param name="name">The name (in English).</param>
		/// <returns>The new text</returns>
		/// ------------------------------------------------------------------------------------
		public IText AddInterlinearTextToLangProj(string name)
		{
			return AddInterlinearTextToLangProj(name, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds an interlinear text to the language projectin the mocked fdocache
		/// </summary>
		/// <param name="name">The name (in English).</param>
		/// <param name="fCreateContents">if set to <c>true</c> also creates an StText for the
		/// Contents.</param>
		/// <returns>The new text</returns>
		/// ------------------------------------------------------------------------------------
		public IText AddInterlinearTextToLangProj(string name, bool fCreateContents)
		{
			IFdoServiceLocator servloc = Cache.ServiceLocator;
			IText text = servloc.GetInstance<ITextFactory>().Create();
			Cache.LangProject.TextsOC.Add(text);
			int wsEn = servloc.GetInstance<ILgWritingSystemFactory>().GetWsFromStr("en");
			text.Name.set_String(wsEn, name);

			if (fCreateContents)
				text.ContentsOA = servloc.GetInstance<IStTextFactory>().Create();
			return text;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a paragraph with the specified text to the given interlinear text
		/// </summary>
		/// <param name="itext">The itext.</param>
		/// <param name="paraText">Paragraph contents.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public IStTxtPara AddParaToInterlinearTextContents(IText itext, string paraText)
		{
			Assert.IsNotNull(itext.ContentsOA);
			IStTxtPara para = itext.ContentsOA.AddNewTextPara(null);
			int wsFr = Cache.ServiceLocator.GetInstance<ILgWritingSystemFactory>().GetWsFromStr("fr");
			para.Contents = TsStringUtils.MakeTss(paraText, wsFr);
			return para;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Change the default analysis writing system. (This removes any previous "current"
		/// analysis writing system(s) from the list.) This also ensures that the given WS is
		/// in the list of all analysis WSs.
		/// </summary>
		/// <param name="ws">the writing system to set as the default analysis WS</param>
		/// ------------------------------------------------------------------------------------
		protected void ChangeDefaultAnalWs(IWritingSystem ws)
		{
			Cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems.Clear();
			Cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem = ws;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a new style and add it to the Language Project stylesheet.
		/// </summary>
		/// <param name="name">style name</param>
		/// <param name="context">style context</param>
		/// <param name="structure">style structure</param>
		/// <param name="function">style function</param>
		/// <param name="isCharStyle">true if character style, otherwise false</param>
		/// <param name="styleCollection">The style collection.</param>
		/// <returns>The style</returns>
		/// ------------------------------------------------------------------------------------
		public IStStyle AddTestStyle(string name, ContextValues context, StructureValues structure,
			FunctionValues function, bool isCharStyle, IFdoOwningCollection<IStStyle> styleCollection)
		{
			return AddTestStyle(name, context, structure, function, isCharStyle, 0, styleCollection);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a new style and add it to the Language Project stylesheet.
		/// </summary>
		/// <param name="name">style name</param>
		/// <param name="context">style context</param>
		/// <param name="structure">style structure</param>
		/// <param name="function">style function</param>
		/// <param name="isCharStyle">true if character style, otherwise false</param>
		/// <param name="userLevel">The user level.</param>
		/// <param name="styleCollection">The style collection.</param>
		/// <returns>The style</returns>
		/// ------------------------------------------------------------------------------------
		public IStStyle AddTestStyle(string name, ContextValues context, StructureValues structure,
			FunctionValues function, bool isCharStyle, int userLevel, IFdoOwningCollection<IStStyle> styleCollection)
		{
			return AddStyle(styleCollection, name, context, structure, function, isCharStyle, userLevel, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a new style on the specified style list.
		/// </summary>
		/// <param name="styleList">The style list to add the style to</param>
		/// <param name="name">style name</param>
		/// <param name="context">style context</param>
		/// <param name="structure">style structure</param>
		/// <param name="function">style function</param>
		/// <param name="isCharStyle">true if character style, otherwise false</param>
		/// <param name="userLevel">User level</param>
		/// <param name="isBuiltIn">true if style is a bult-in style</param>
		/// ------------------------------------------------------------------------------------
		public IStStyle AddStyle(IFdoOwningCollection<IStStyle> styleList, string name,
			ContextValues context, StructureValues structure, FunctionValues function,
			bool isCharStyle, int userLevel, bool isBuiltIn)
		{
			IStStyle style = Cache.ServiceLocator.GetInstance<IStStyleFactory>().Create();
			styleList.Add(style);
			style.Name = name;
			style.Context = context;
			style.Structure = structure;
			style.Function = function;
			style.Type = (isCharStyle ? StyleType.kstCharacter : StyleType.kstParagraph);
			style.UserLevel = userLevel;
			ITsPropsBldr bldr = TsPropsBldrClass.Create();
			style.Rules = bldr.GetTextProps();
			style.IsBuiltIn = isBuiltIn;
			return style;
		}
		#endregion
	}
	#endregion
}
