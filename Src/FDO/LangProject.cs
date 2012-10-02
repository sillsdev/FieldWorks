// --------------------------------------------------------------------------------------------
// Copyright (C) 2002 SIL International. All rights reserved.
//
// Distributable under the terms of either the Common Public License or the
// GNU Lesser General Public License, as specified in the LICENSING.txt file.
//
// File: LangProject.cs
// Responsibility: Randy Regnier
// Last reviewed: never
//
//
// <remarks>
// Implementation of:
//		LangProject : BaseLangProject (formerly AfLpInfo)
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;
using System.Windows.Forms;
using System.Xml;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.Common.COMInterfaces;
using System.Data;
using System.Data.SqlClient;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.FwUtils; // for LanguageDefinitionFactory
using SIL.Utils;
using SIL.FieldWorks.Resources;

namespace SIL.FieldWorks.FDO.LangProj
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// The LangProject class
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class LangProject
	{
		#region Constants
		// These are magic writing system numbers that are not legal writing systems, but are used to signal
		// the application to get the appropriate writing system from the application. For example,
		// a language project has a list of one or more analysis encodings. kwsAnal would
		// tell the program to use the first writing system in this list.

		// ****** IMPORTANT ******
		// These constants are mirrored in CmTypes.h and need to be kept syncd.
		// On 8/24 the value of kwsLim in CmTypes.h is -7 and the value here is -16.
		// ****** IMPORTANT ******

		/// <summary>(-1) The first analysis writing system.</summary>
		public const int kwsAnal = -1;
		/// <summary>(-2) The first vernacular writing system.</summary>
		public const int kwsVern = -2;
		/// <summary>(-3) All analysis writing system.</summary>
		public const int kwsAnals = -3;
		/// <summary>(-4) All vernacular writing system.</summary>
		public const int kwsVerns = -4;
		/// <summary>(-5) All analysis then All vernacular writing system.</summary>
		public const int kwsAnalVerns = -5;
		/// <summary>(-6) All vernacular then All analysis writing system.</summary>
		public const int kwsVernAnals = -6;
		/// <summary>(-7) The first available analysis ws with data in the current sequence.</summary>
		public const int kwsFirstAnal = -7;
		/// <summary>(-8) The first available vernacular ws in the current sequence.</summary>
		public const int kwsFirstVern = -8;
		/// <summary>(-9) The first available analysis ws with data in the current sequence,
		/// or the first available vernacular ws in that sequence.</summary>
		public const int kwsFirstAnalOrVern = -9;
		/// <summary>(-10) The first available vernacular ws with data in the current sequence,
		/// or the first available analysis ws in that sequence.</summary>
		public const int kwsFirstVernOrAnal = -10;
		/// <summary>The first pronunciation writing system.</summary>
		public const int kwsPronunciation = -11;
		/// <summary>The first pronunciation writing system with data.</summary>
		public const int kwsFirstPronunciation = -12;
		/// <summary>All pronunciation writing systems.</summary>
		public const int kwsPronunciations = -13;
		/// <summary>The primary writing system for the current reversal index.</summary>
		public const int kwsReversalIndex = -14;
		/// <summary>The full list of writing systems for the current reversal index.</summary>
		public const int kwsAllReversalIndex = -15;
		/// <summary>The ws of the relevant text at an offset in its paragraph</summary>
		public const int kwsVernInParagraph = -16;
		/// <summary>(-17) The first available vern ws with data in the current sequence
		/// or else a ws named in the database. </summary>
		public const int kwsFirstVernOrNamed = -17;
		/// <summary> One beyond the last magic value.</summary>
		public const int kwsLim = -18;

		/// <summary>Translation Types Possibility List</summary>
		public const string kguidTranslationTypes = "d7f71649-e8cf-11d3-9764-00c04f186933";
		/// <summary>Back Translation item in Translation Types list</summary>
		public static readonly Guid kguidTranBackTranslation = new Guid("80a0dddb-8b4b-4454-b872-88adec6f2aba");
		/// <summary>Free Translation item in Translation Types list</summary>
		public static readonly Guid kguidTranFreeTranslation = new Guid("d7f7164a-e8cf-11d3-9764-00c04f186933");
		/// <summary>Literal Translation item in Translation Types list</summary>
		public static readonly Guid kguidTranLiteralTranslation = new Guid("d7f7164b-e8cf-11d3-9764-00c04f186933");

		/// <summary>Key Terms Checking Possibility List</summary>
		public static readonly Guid kguidChkKeyTermsList = new Guid("76FB50CA-F858-469c-B1DE-A73A863E9B10");
		/// <summary>Old Key Terms Checking Possibility List</summary>
		public static readonly Guid kguidOldKeyTermsList = new Guid("04B81EC0-4850-4dc2-98C1-3FC043F71845");

		/// <summary>Comment item in Annotation Definitions list</summary>
		public const string kguidAnnComment = "f094a0b0-01b8-4621-97f1-4d775bc29ce7";
		/// <summary>Consultant Note item in Annotation Definitions list</summary>
		public static readonly Guid kguidAnnConsultantNote = new Guid("56de9b1a-1ce7-42a1-aa76-512ebeff0dda");
		/// <summary>Translator Note item in Annotation Definitions list</summary>
		public static readonly Guid kguidAnnTranslatorNote = new Guid("80ae5729-9cd8-424d-8e71-96c1a8fd5821");
		/// <summary>Errors item in Annotation Definitions list</summary>
		public static readonly Guid kguidAnnCheckingError = new Guid("82e2fd92-48d8-43c9-ba84-cc4a2a5beead");

		/// <summary>Phonological rule morpheme boundary</summary>
		public static readonly Guid kguidPhRuleMorphBdry = new Guid("3bde17ce-e39a-4bae-8a5c-a8d96fd4cb56");
		/// <summary>Phonological rule word boundary</summary>
		public static readonly Guid kguidPhRuleWordBdry = new Guid("7db635e0-9ef3-4167-a594-12551ed89aaa");

		/// <summary>CmAgent representing the default M3Parser</summary>
		public static readonly Guid kguidAgentM3Parser = new Guid("1257A971-FCEF-4F06-A5E2-C289DE5AAF72");
		/// <summary>CmAgent representing the default HCParser</summary>
		public static readonly Guid kguidAgentHCParser = new Guid("5093D7D7-4F18-4AAD-8C86-88389476DF15");
		/// <summary>CmAgent representing the default User</summary>
		public static readonly Guid kguidAgentDefUser = new Guid("9303883A-AD5C-4CCF-97A5-4ADD391F8DCB");
		/// <summary>CmAgent representing the Computer (i.e., for Checking)</summary>
		public static readonly Guid kguidAgentComputer = new Guid("67E9B8BF-C312-458e-89C3-6E9326E48AA0");
		/// <summary>Free Translation item in Annotation Definitions list</summary>
		public const string kguidAnnFreeTranslation = "9ac9637a-56b9-4f05-a0e1-4243fbfb57db";
		/// <summary>Literal Translation item in Annotation Definitions list</summary>
		public const string kguidAnnLiteralTranslation = "b0b1bb21-724d-470a-be94-3d9a436008b8";
		/// <summary>Text Tag item in Annotation Definitions list</summary>
		public const string kguidAnnTextTag = "084a3afe-0d00-41da-bfcf-5d8deafa0296";
		/// <summary>Note item in Annotation Definitions list</summary>
		public static readonly Guid kguidAnnNote = new Guid("7ffc4eab-856a-43cc-bc11-0db55738c15b");
		/// <summary>Text item in Annotation Definitions list</summary>
		public const string kguidAnnText = "8d4cbd80-0dca-4a83-8a1f-9db3aa4cff54";
		/// <summary>Text Segment item in Annotation Definitions list</summary>
		public const string kguidAnnTextSegment = "b63f0702-32f7-4abb-b005-c1d2265636ad";
		/// <summary>Wordform in context</summary>
		public const string kguidAnnWordformInContext = "eb92e50f-ba96-4d1d-b632-057b5c274132";
		/// <summary>Constituent Chart Annotations</summary>
		public const string kguidConstituentChartAnnotation = "ec0a4dad-7e90-4e73-901a-21d25f0692e3";
		/// <summary>Constituent Chart Rows</summary>
		public const string kguidConstituentChartRow = "50c1a53d-925d-4f55-8ed7-64a297905346";
		/// <summary>Punctuation in context</summary>
		public const string kguidAnnPunctuationInContext = "cfecb1fe-037a-452d-a35b-59e06d15f4df";
		/// <summary>Annotation used to record when some process was last applied to the object.
		/// For example, this is used in IText to record when an StTxtPara was last parsed.
		/// BeginObject points to the object processed, and CompDetails contains a string
		/// representation of the UpdStmp of the object in question when processed</summary>
		public const string kguidAnnProcessTime = "20cf6c1c-9389-4380-91f5-dfa057003d51";

		/// <summary>Lex Complex Form Types Possibility List</summary>
		public const string kguidLexComplexFormTypes = "1ee09905-63dd-4c7a-a9bd-1d496743ccd6";
		/// <summary>Compound item in LexEntry Types list</summary>
		public const string kguidLexTypCompound = "1f6ae209-141a-40db-983c-bee93af0ca3c";
		/// <summary>Contraction item in LexEntry Types list</summary>
		public const string kguidLexTypContraction = "73266a3a-48e8-4bd7-8c84-91c730340b7d";
		/// <summary>Derivation item in LexEntry Types list</summary>
		public const string kguidLexTypDerivation = "98c273c4-f723-4fb0-80df-eede2204dfca";
		/// <summary>Idiom item in LexEntry Types list</summary>
		public const string kguidLexTypIdiom = "b2276dec-b1a6-4d82-b121-fd114c009c59";
		/// <summary>Phrasal Verb item in LexEntry Types list</summary>
		public const string kguidLexTypPhrasalVerb = "35cee792-74c8-444e-a9b7-ed0461d4d3b7";
		/// <summary>Saying item in LexEntry Types list</summary>
		public const string kguidLexTypSaying = "9466d126-246e-400b-8bba-0703e09bc567";

		/// <summary>Lex Variant Types Possibility List</summary>
		public const string kguidLexVariantTypes = "bb372467-5230-43ef-9cc7-4d40b053fb94";
		/// <summary>Dialectal Variant item in LexEntry Types list</summary>
		public const string kguidLexTypDialectalVar = "024b62c9-93b3-41a0-ab19-587a0030219a";
		/// <summary>Free Variant item in LexEntry Types list</summary>
		public const string kguidLexTypFreeVar = "4343b1ef-b54f-4fa4-9998-271319a6d74c";
		/// <summary>Inflectional Variant item in LexEntry Types list</summary>
		public const string kguidLexTypIrregInflectionVar = "f01d4fbc1-3b0c-4f52-9163-7ab0d4f4711c";
		/// <summary>Plural Variant item in LexEntry Types list</summary>
		public const string kguidLexTypPluralVar = "a32f1d1c-4832-46a2-9732-c2276d6547e8";
		/// <summary>Past Variant item in LexEntry Types list</summary>
		public const string kguidLexTypPastVar = "837ebe72-8c1d-4864-95d9-fa313c499d78";
		/// <summary>Spelling Variant item in LexEntry Types list</summary>
		public const string kguidLexTypSpellingVar = "0c4663b3-4d9a-47af-b9a1-c8565d8112ed";

		#endregion

		#region Data members

		private int m_iDefaultAnalysisWritingSystem = -1;
		private string m_sDefaultAnalysisWritingSystemICULocale = string.Empty;
		private MultiUnicodeAccessor m_DefaultAnalysisWritingSystemName = null;
		private int m_iDefaultUserWritingSystem = -1;
		private string m_sDefaultUserWritingSystemICULocale = string.Empty;
		private MultiUnicodeAccessor m_DefaultUserWritingSystemName = null;
		private int m_iDefaultVernacularWritingSystem = -1;
		private string m_sDefaultVernacularWritingSystemICULocale = string.Empty;
		private MultiUnicodeAccessor m_DefaultVernacularWritingSystemName = null;
		/// <summary>Default computer agent, cached for speed</summary>
		protected ICmAgent m_defaultComputerAgent;

		private static BidirHashtable m_magicWsIdToWsName;
		#endregion
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvo"></param>
		/// <param name="fCheckValidity"></param>
		/// <param name="fLoadIntoCache"></param>
		/// ------------------------------------------------------------------------------------
		protected override void InitExisting(FdoCache cache, int hvo, bool fCheckValidity, bool fLoadIntoCache)
		{
			base.InitExisting(cache, hvo, fCheckValidity, fLoadIntoCache);
			CacheDefaultWritingSystems();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Save information about default analysis and vernacular writing systems for fast
		/// access. This method is called when an existing Language Project is initialized. It
		/// should also be called explicitly whenever an app changes to a different default
		/// writing systems or changes the name or ICU locale of one of the existing default
		/// writing systems.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CacheDefaultWritingSystems()
		{
			// Preload writing systems
			CmObject.LoadObjectsIntoCache(m_cache, typeof(LgWritingSystem), null,
				LgWritingSystem.kClassId);

			if (CurAnalysisWssRS == null ||
				CurAnalysisWssRS.HvoArray.Length == 0 ||
				CurVernWssRS == null ||
				CurVernWssRS.HvoArray.Length == 0)
			{
				// The database must be in the early stages of being created because it still
				// doesn't have these writing systems set. So sad!
				return;
			}

			m_iDefaultAnalysisWritingSystem = CurAnalysisWssRS.HvoArray[0];
			//new LgWritingSystem(m_cache, CurAnalysisWssRS.HvoArray[0]).Code;

			m_sDefaultAnalysisWritingSystemICULocale =
				new LgWritingSystem(m_cache, CurAnalysisWssRS.HvoArray[0]).ICULocale;

			m_DefaultAnalysisWritingSystemName =
				new LgWritingSystem(m_cache, CurAnalysisWssRS.HvoArray[0]).Name;

			m_iDefaultVernacularWritingSystem = CurVernWssRS.HvoArray[0];
			//new LgWritingSystem(m_cache, CurVernWssRS.HvoArray[0]).Code;

			m_sDefaultVernacularWritingSystemICULocale =
				new LgWritingSystem(m_cache, CurVernWssRS.HvoArray[0]).ICULocale;

			m_DefaultVernacularWritingSystemName =
				new LgWritingSystem(m_cache, CurVernWssRS.HvoArray[0]).Name;

			m_iDefaultUserWritingSystem = m_cache.DefaultUserWs;

			m_DefaultUserWritingSystemName =
				new LgWritingSystem(m_cache, m_iDefaultUserWritingSystem).Name;

			m_sDefaultUserWritingSystemICULocale =
				new LgWritingSystem(m_cache, m_iDefaultUserWritingSystem).ICULocale;
		}

		/// <summary>
		/// Find (create, if not found) an agent which matches the input parameters.
		/// </summary>
		/// <param name="name">Name of the agent.</param>
		/// <param name="isHuman">True if it is a human agent, otherwise false.</param>
		/// <param name="version">Version number of the agent.</param>
		/// <returns>The extant or newly created ICmAgent object.</returns>
		public ICmAgent GetAnalyzingAgent(string name, bool isHuman, string version)
		{
			// TODO(Undo): Figure out how to put this into cache processing with Undo/Redo
			// capability (and whether its worth doing this).
			ICmAgent agent = null;

			IOleDbCommand odc = null;
			m_cache.DatabaseAccessor.CreateCommand(out odc);
			try
			{
				bool fMoreRows;
				string query = string.Format("exec FindOrCreateCmAgent '{0}', {1}, '{2}'",
					name, (isHuman ? 1 : 0), version);
				odc.ExecCommand(query, (int)SqlStmtType.knSqlStmtSelectWithOneRowset);
				odc.GetRowset(0);
				odc.NextRow(out fMoreRows);
				Debug.Assert(fMoreRows, "FindOrCreateCmAgent didn't work right in LangProject::GetAnalyzingAgent().");

				bool fIsNull;
				uint cbSpaceTaken;
				// Get id of extant agent.
				using (ArrayPtr rgHvo = MarshalEx.ArrayToNative(1, typeof(uint)))
				{
					odc.GetColValue(1, rgHvo, rgHvo.Size, out cbSpaceTaken, out fIsNull, 0);
					uint[] uIds = (uint[])MarshalEx.NativeToArray(rgHvo, 1, typeof(uint));
					agent = (ICmAgent)CmObject.CreateFromDBObject(m_cache, (int)uIds[0]);
				}
			}
			finally
			{
				DbOps.ShutdownODC(ref odc);
			}
			return agent;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the agent matching the given GUID
		/// </summary>
		/// <param name="guid"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected ICmAgent GetAgent(Guid guid)
		{
			int hvo = m_cache.GetIdFromGuid(guid);
			if (hvo != 0)
				return CmAgent.CreateFromDBObject(m_cache, hvo);
			else
				return null;
		}

		private static void InitMagicWsToWsId()
		{
			m_magicWsIdToWsName = new BidirHashtable();

			m_magicWsIdToWsName[kwsAnal] = "analysis";
			m_magicWsIdToWsName[kwsVern] = "vernacular";
			m_magicWsIdToWsName[kwsVerns] = "all vernacular";
			m_magicWsIdToWsName[kwsAnals] = "all analysis";
			m_magicWsIdToWsName[kwsAnalVerns] = "analysis vernacular";
			m_magicWsIdToWsName[kwsVernAnals] = "vernacular analysis";
			m_magicWsIdToWsName[kwsFirstAnal] = "best analysis";
			m_magicWsIdToWsName[kwsFirstVern] = "best vernacular";
			m_magicWsIdToWsName[kwsFirstAnalOrVern] = "best analorvern";
			m_magicWsIdToWsName[kwsFirstVernOrAnal] = "best vernoranal";
			m_magicWsIdToWsName[kwsFirstPronunciation] = "best pronunciation";
			m_magicWsIdToWsName[kwsPronunciations] = "all pronunciation";
			m_magicWsIdToWsName[kwsReversalIndex] = "reversal";
			m_magicWsIdToWsName[kwsAllReversalIndex] = "all reversal";
			m_magicWsIdToWsName[kwsVernInParagraph] = "vern in para";
			m_magicWsIdToWsName[kwsFirstVernOrNamed] = "best vernornamed";
		}

		static BidirHashtable MagicWsIdToWsName
		{
			get
			{
				if (m_magicWsIdToWsName == null)
					InitMagicWsToWsId();

				return m_magicWsIdToWsName;
			}
		}

		/// <summary>
		/// Returns the singular equivalent of a plural magic writing system.  If a singular magic ws is used as the argument, it should
		/// be returned unmodified.  For example, given kwsAnals (all analysis), this method will return kwsAnal (analysis).
		/// </summary>
		/// <param name="wsMagic">The plural magic ws to turn into a singular equivalent</param>
		/// <returns></returns>
		static public int PluralMagicWsToSingularMagicWs(int wsMagic)
		{
			switch (wsMagic)
			{
				case kwsAnal:
				case kwsAnals:
					return kwsAnal;
				case kwsVern:
				case kwsVerns:
					return kwsVern;
				case kwsPronunciations:
				case kwsPronunciation:
					return kwsPronunciation;
				case kwsAllReversalIndex:
				case kwsReversalIndex:
					return kwsReversalIndex;
				case kwsFirstAnal:
					return kwsFirstAnal;
				case kwsFirstVern:
					return kwsFirstVern;
				case kwsVernAnals:
					return kwsVernAnals;	// not singular, but handled okay elsewhere.
				case kwsFirstVernOrAnal:
					return kwsFirstVernOrAnal;
				case kwsAnalVerns:
					return kwsAnalVerns;	// not singular, but handled okay elsewhere.
				case kwsFirstAnalOrVern:
					return kwsFirstAnalOrVern;
				case kwsVernInParagraph:
					return kwsVernInParagraph;
				default:
					Debug.Assert(false, "A magic writing system ID (" + wsMagic + ") was encountered that this method does not understand.");
					return wsMagic;
			}
		}

		/// <summary>
		/// Returns the simple equivalent of a smart magic writing system.  If a simple magic ws is used as the argument, it should
		/// be returned unmodified.  For example, given kwsAnals (all analysis), this method will return kwsAnal (analysis).
		/// The WS returned should be safe to use in bulk edit. This means it should not be so smart as to pick one of several
		/// writing systems based on what is not empty. For example, kwsFirstAnal is simplified to kwsAnal, and so is kwsFirstAnalOrVern.
		/// </summary>
		/// <param name="wsMagic">The smart magic ws to turn into a simple equivalent</param>
		/// <returns></returns>
		static public int SmartMagicWsToSimpleMagicWs(int wsMagic)
		{
			switch (wsMagic)
			{
				case kwsAnal:
				case kwsAnals:
				case kwsFirstAnal:
				case kwsAnalVerns:
				case kwsFirstAnalOrVern:
					return kwsAnal;
				case kwsVern:
				case kwsVerns:
				case kwsFirstVern:
				case kwsVernAnals:
				case kwsFirstVernOrAnal:
					return kwsVern;
				case kwsPronunciations:
				case kwsPronunciation:
					return kwsPronunciation;
				case kwsAllReversalIndex:
				case kwsReversalIndex:
					return kwsReversalIndex;
				case kwsVernInParagraph:
					return kwsVernInParagraph;
				default:
					Debug.Assert(false, "A magic writing system ID (" + wsMagic + ") was encountered that this method does not understand.");
					return wsMagic;
			}
		}

		/// <summary>
		/// Return a plausible guess for interpreting the "magic" ws as a real ws.  An invalid
		/// "magic" ws returns itself.
		/// </summary>
		public int DefaultWsForMagicWs(int wsMagic)
		{
			switch (wsMagic)
			{
				case kwsAnal:
				case kwsAnals:
				case kwsFirstAnal:
				case kwsAnalVerns:
				case kwsFirstAnalOrVern:
				case kwsAllReversalIndex:
				case kwsReversalIndex:
					return m_cache.DefaultAnalWs;
				case kwsVern:
				case kwsVerns:
				case kwsFirstVern:
				case kwsVernAnals:
				case kwsFirstVernOrAnal:
				case kwsPronunciations:
				case kwsPronunciation:
				case kwsVernInParagraph:
					return m_cache.DefaultVernWs;
				default:
					return wsMagic;
			}
		}

		/// <summary>
		/// Get the magic WS id for the given string, or 0, if not a magic WS id.
		/// </summary>
		/// <param name="wsSpec"></param>
		/// <returns></returns>
		public static int GetMagicWsIdFromName(string wsSpec)
		{
			int wsMagic = 0;
			if (wsSpec == null)
				return 0;
			if (MagicWsIdToWsName.ContainsValue(wsSpec))
				wsMagic = (int)MagicWsIdToWsName.ReverseLookup(wsSpec);
			// JohnT: took this out, because ConfigureFieldDlg wants to pass names of specific writing systems
			// and get zero back, as indicated in the method comment.
			//Debug.Assert(wsMagic != 0, "Method encountered a Magic Ws string that it did not understand");
			return wsMagic;
		}

		/// <summary>
		/// Get the magic WS name for the given id, or "", if not a magic WS name.
		/// </summary>
		public static string GetMagicWsNameFromId(int wsMagic)
		{
			string wsName = "";
			if (MagicWsIdToWsName.Contains(wsMagic))
				wsName = (string)MagicWsIdToWsName[wsMagic];
			Debug.Assert(wsName != "", "Method encountered a Magic Ws ID that it did not understand");
			return wsName;
		}


		/// <summary>
		/// The 'magic' string that identifies a writing-system parameter.
		/// </summary>
		static public string WsParamLabel
		{
			get { return "$ws="; }
		}

		/// <summary>
		/// Strips the 'magic' prefix that identifies a configurable WS parameter.
		/// </summary>
		static public string GetWsSpecWithoutPrefix(string wsSpec)
		{
			if (wsSpec.StartsWith(WsParamLabel))
				return wsSpec.Substring(WsParamLabel.Length);
			return wsSpec;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the current analysis and vernacular writing systems.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Set<int> CurrentAnalysisAndVernWss
		{
			get
			{
				return GetAllWritingSystems("analysis vernacular", Cache, null, 0, 0);
			}
		}

		/// <summary>
		/// Get a Set of zero or more actual writing system ID from the given xml fragment.
		/// The contents of the mandatory 'ws' attribute may be a magic ws specification,
		/// or one of several pseudo magic writing system spcifications.
		/// </summary>
		/// <param name="frag"></param>
		/// <param name="cache"></param>
		/// <param name="currentWS"></param>
		/// <param name="hvo">object to use in determining 'best' names</param>
		/// <param name="flid">flid to use in determining 'best' names</param>
		/// <returns></returns>
		public static Set<int> GetAllWritingSystems(XmlNode frag, FdoCache cache,
			IWritingSystem currentWS, int hvo, int flid)
		{
			string sWs = XmlUtils.GetOptionalAttributeValue(frag, "ws");
			return GetAllWritingSystems(sWs, cache, currentWS, hvo, flid);
		}
		/// <summary>
		/// Get a Set of zero or more actual writing system IDs for the given ws identifier.
		/// </summary>
		/// <param name="sWs">One of our magic strings that signifies one or more writing systems</param>
		/// <param name="cache"></param>
		/// <param name="currentWS"></param>
		/// <param name="hvo">object to use in determining 'best' names</param>
		/// <param name="flid">flid to use in determining 'best' names</param>
		/// <returns></returns>
		public static Set<int> GetAllWritingSystems(string sWs, FdoCache cache, IWritingSystem currentWS, int hvo, int flid)
		{
			Set<int> allWsIds = new Set<int>();
			if (sWs != null)
			{
				switch (sWs)
				{
					case "all analysis":
						allWsIds.AddRange(cache.LangProject.CurAnalysisWssRS.HvoArray);
						break;
					case "all vernacular":
						allWsIds.AddRange(cache.LangProject.CurVernWssRS.HvoArray);
						break;
					case "analysis vernacular":
						allWsIds.AddRange(cache.LangProject.CurAnalysisWssRS.HvoArray);
						allWsIds.AddRange(cache.LangProject.CurVernWssRS.HvoArray);
						break;
					case "vernacular analysis":
						allWsIds.AddRange(cache.LangProject.CurVernWssRS.HvoArray);
						allWsIds.AddRange(cache.LangProject.CurAnalysisWssRS.HvoArray);
						break;
					case "all pronunciation":
						cache.LangProject.InitializePronunciationWritingSystems();
						allWsIds.AddRange(cache.LangProject.CurPronunWssRS.HvoArray);
						//if (allWsIds.Count == 0)
						//	allWsIds.Add(cache.LangProject.DefaultPronunciationWritingSystem);
						break;
					default:
						sWs = GetWsSpecWithoutPrefix(sWs);
						string[] rgsWs = sWs.Split(new char[] { ',' });
						for (int i = 0; i < rgsWs.Length; ++i)
						{
							int ws = InterpretWsLabel(cache, rgsWs[i], 0, hvo, flid, currentWS);
							if (ws != 0)
								allWsIds.Add(ws);
						}
						break;
				}
			}

			return allWsIds;
		}

		/// <summary>
		/// Return all the writing systems found in the language project.
		/// </summary>
		public Set<int> AllWritingSystems
		{
			get
			{
				Set<int> allWsIds = new Set<int>();
				allWsIds.AddRange(m_cache.LanguageEncodings.HvoArray);
				return allWsIds;
			}
		}

		/// <summary>
		/// If there are no pronunciation writing systems selected, make a default set, with IPA variants
		/// coming before EMC variants (if either of those exist).  If neither exists, the primary
		/// vernacular writing system is selected.
		/// </summary>
		public void InitializePronunciationWritingSystems()
		{
			if (CurPronunWssRS.Count > 0)
				return;
			string displayLocale =
				m_cache.LanguageWritingSystemFactoryAccessor.GetStrFromWs(m_cache.DefaultUserWs);
			Set<NamedWritingSystem> writingSystems = GetActiveNamedWritingSystems();
			ILgWritingSystem wsVern = CurVernWssRS[0];
			string sVern = wsVern.ICULocale.ToLowerInvariant();
			int idx = sVern.IndexOf("_");
			if (idx > 0)
				sVern = sVern.Substring(0, idx + 1);
			else
				sVern = sVern + '_';
			// Add any relevant Etic (or IPA for older DBs) writing systems.
			foreach (NamedWritingSystem nws in writingSystems)
			{
				AddWsToPronunciations(nws, sVern, "_etic");
				AddWsToPronunciations(nws, sVern, "_ipa");
			}
			if (CurPronunWssRS.Count == 0)
			{
				// Add any relevant Emic (or EMC for older DBs) writing systems.
				foreach (NamedWritingSystem nws in writingSystems)
				{
					AddWsToPronunciations(nws, sVern, "_emic");
					AddWsToPronunciations(nws, sVern, "_emc");
				}
			}
			// Add the primary vernacular writing system if nothing else fits.
			if (CurPronunWssRS.Count == 0)
			{
				CurPronunWssRS.Append(wsVern.Hvo);
			}
		}

		/// <summary>
		/// Add the specified writing system to your pronunciations, if it starts with the right prefix
		/// and ends with the right suffix.
		/// </summary>
		private void AddWsToPronunciations(NamedWritingSystem nws, string sPrefix, string sSuffix)
		{
			int idx;
			string icuLocale = nws.IcuLocale.ToLowerInvariant();
			if (icuLocale.IndexOf(sPrefix) == 0)
			{
				idx = icuLocale.LastIndexOf(sSuffix);
				if (idx >= sPrefix.Length && idx == icuLocale.Length - sSuffix.Length)
					CurPronunWssRS.Append(nws.Hvo);
			}
		}

		/// <summary>
		/// Get the writing system from the XML attributes ws, smartws or wsid, or use the supplied
		/// default if neither attribute exists, or no meaningfule value exists, even if the attribute does exist.
		/// </summary>
		/// <param name="frag"></param>
		/// <param name="cache"></param>
		/// <param name="currentWS"></param>
		/// <param name="wsDefault"></param>
		/// <returns></returns>
		static public int GetWritingSystem(XmlNode frag, FdoCache cache,
			IWritingSystem currentWS, int wsDefault)
		{
			return GetWritingSystem(frag, cache, currentWS, 0, 0, wsDefault);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="frag"></param>
		/// <param name="cache"></param>
		/// <param name="currentWS"></param>
		/// <param name="hvo"></param>
		/// <param name="flid"></param>
		/// <param name="wsDefault"></param>
		/// <returns></returns>
		public static int GetWritingSystem(XmlNode frag, FdoCache cache,
			IWritingSystem currentWS, int hvo, int flid, int wsDefault)
		{
			int wsid = wsDefault;
			string wsSpec = null;
			XmlAttribute xa = frag.Attributes["ws"];
			if (xa != null)
			{
				wsSpec = GetWsSpecWithoutPrefix(xa.Value);
				wsid = InterpretWsLabel(cache, wsSpec, wsid, hvo, flid, currentWS);
			}
			// if ws is still a magic id, then convert it to something real.
			if (wsid < 0)
			{
				wsid = cache.LangProject.ActualWs(wsid, hvo, flid);
			}
			return wsid;
		}

		/// <summary>
		/// Return the possible writing systems that we might want to preload for the given fragment.
		/// Note that currently this is for optimization and preloading; it is not (yet) guaranteed to
		/// return EVERY writing system that might be displayed by the given fragment.
		/// </summary>
		/// <param name="frag"></param>
		/// <param name="cache"></param>
		/// <returns></returns>
		public static Set<int> GetWritingSystems(XmlNode frag, FdoCache cache)
		{
			string wsSpec = null;
			XmlAttribute xa = frag.Attributes["ws"];
			if (xa == null)
				return new Set<int>(0);
			wsSpec = GetWsSpecWithoutPrefix(xa.Value);
			Set<int> result = new Set<int>();

			switch (wsSpec)
			{
				case "vernacular":
					result.Add(cache.DefaultVernWs);
					break;
				case "analysis":
					result.Add(cache.DefaultAnalWs);
					break;
				case "pronunciation":
				case "all pronunciation":
					result.Add(cache.LangProject.DefaultPronunciationWritingSystem);
					break;
				case "current":
					break;
				case "reversal":
					// Enhance JohnT: make this (currently for optimization preloading) routine work
					// better for reversal views
					//actualWS = GetReversalIndexEntryWritingSystem(cache, hvoObj, wsDefault);
					break;
				case "best analorvern":
				case "best vernoranal":
					return cache.LangProject.CurrentAnalysisAndVernWss;
				// Enhance JohnT: make this (currently for optimization preloading) routine work
				// better for the above two cases: possibly add all current analysis and vernacular writing systems
				case "analysis vernacular":
				case "av":
				case "vernacular analysis":
				case "va":
					result.Add(cache.DefaultVernWs);
					result.Add(cache.DefaultAnalWs);
					break;
				case "user":
					result.Add(cache.DefaultUserWs);
					break;
				case "best analysis":
				case "all analysis":
					result.AddRange(cache.LangProject.CurAnalysisWssRS.HvoArray);
					break;
				case "best vernacular":
				case "all vernacular":
					result.AddRange(cache.LangProject.CurVernWssRS.HvoArray);
					break;
				default:
					// See if we can get anywhere by treating it as an ICU locale.
					// Note that it is important to do this in a way that won't create a new writing system for
					// an invalid locale name, for example, if 'all analysis' is mistakenly passed to this routine.
					// Note however that the behavior of recognizing an ICU locale name for an existing writing system
					// definitely IS needed, e.g., when the user configures a Browse view to show an explicit writing system.
					int wsT = cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr(wsSpec);
					if (wsT != 0)
						result.Add(wsT);
					break;
			}
			return result;
		}

		/// <summary>
		/// Return true if the specified fragment requires an hvo (and possibly flid) for its interpretation.
		/// Currently this assumes just the "ws" attribute, since smartws is obsolete.
		/// </summary>
		/// <param name="frag"></param>
		/// <returns></returns>
		public static bool GetWsRequiresObject(XmlNode frag)
		{
			XmlAttribute xa = frag.Attributes["ws"];
			if (xa == null)
				return false;
			string wsSpec = xa.Value;
			return GetWsRequiresObject(wsSpec);
		}

		/// <summary>
		/// Return true if the specified fragment requires an hvo (and possibly flid) for its interpretation.
		/// Currently this assumes just the "ws" attribute, since smartws is obsolete.
		/// </summary>
		/// <returns></returns>
		public static bool GetWsRequiresObject(string wsSpec)
		{
			wsSpec = GetWsSpecWithoutPrefix(wsSpec);
			return wsSpec.StartsWith("best") || wsSpec.StartsWith("reversal") || wsSpec == "va" || wsSpec == "av";
		}

		/// <summary>
		/// Try to get an actual writing system id from some ws string specification.
		/// If it does not recognize the ws spec string, it returns 0.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="wsSpec"></param>
		/// <param name="wsDefault"></param>
		/// <param name="hvoObj"></param>
		/// <param name="flid"></param>
		/// <param name="currentWS"></param>
		/// <returns>A Set of writing system ids, or an empty Set, if it can't recognize the wsSpec parameter.</returns>
		internal static Set<int> GetWritingSystemIdsFromLabel(FdoCache cache, string wsSpec, int wsDefault, int hvoObj, int flid, IWritingSystem currentWS)
		{
			Set<int> writingSystemIds = new Set<int>();

			switch (wsSpec.Trim().ToLowerInvariant())
			{
				case "all analysis":
					{
						writingSystemIds.AddRange(cache.LangProject.CurAnalysisWssRS.HvoArray);
						break;
					}
				case "all vernacular":
					{
						writingSystemIds.AddRange(cache.LangProject.CurVernWssRS.HvoArray);
						break;
					}
				case "analysis vernacular":
					{
						writingSystemIds.AddRange(cache.LangProject.CurAnalysisWssRS.HvoArray);
						writingSystemIds.AddRange(cache.LangProject.CurVernWssRS.HvoArray);
						break;
					}
				case "vernacular analysis":
					{
						writingSystemIds.AddRange(cache.LangProject.CurVernWssRS.HvoArray);
						writingSystemIds.AddRange(cache.LangProject.CurAnalysisWssRS.HvoArray);
						break;
					}
				default:
					writingSystemIds.Add(InterpretWsLabel(cache, wsSpec, wsDefault, hvoObj, flid, currentWS));
					break;
			}

			return writingSystemIds;
		}

		/// <summary>
		/// Try to get an actual writing system id from some ws string specification.
		/// If it does not recognize the ws spec string, it returns 0.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="wsSpec"></param>
		/// <param name="wsDefault"></param>
		/// <param name="hvoObj"></param>
		/// <param name="flid"></param>
		/// <param name="currentWS"></param>
		/// <returns>An actual writing system id, or 0, if it can't recognize the wsSpec parameter.</returns>
		public static int InterpretWsLabel(FdoCache cache, string wsSpec, int wsDefault,
			int hvoObj, int flid, IWritingSystem currentWS)
		{
			int wsMagic = 0;
			return InterpretWsLabel(cache, wsSpec, wsDefault, hvoObj, flid, currentWS, out wsMagic);
		}

		/// <summary>
		/// Try to get an actual writing system id from some ws string specification.
		/// If it does not recognize the ws spec string, it returns 0.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="wsSpec"></param>
		/// <param name="wsDefault"></param>
		/// <param name="hvoObj"></param>
		/// <param name="flid"></param>
		/// <param name="currentWS"></param>
		/// <param name="wsMagic">returns the equivalent magic ws value</param>
		/// <returns>An actual writing system id, or 0, if it can't recognize the wsSpec parameter.</returns>
		public static int InterpretWsLabel(FdoCache cache, string wsSpec, int wsDefault,
			int hvoObj, int flid, IWritingSystem currentWS, out int wsMagic)
		{
			wsMagic = GetMagicWsIdFromName(wsSpec);	// note: doesn't cover "va" and "av".
			int actualWS;
			switch (wsSpec)
			{
				case "vernacular":
					actualWS = cache.LangProject.ActualWs(kwsVern, hvoObj, flid);
					break;
				case "analysis":
					actualWS = cache.LangProject.ActualWs(kwsAnal, hvoObj, flid);
					break;
				case "best analysis":
					actualWS = cache.LangProject.ActualWs(kwsFirstAnal, hvoObj, flid);
					if (actualWS == 0)
						actualWS = wsDefault;
					break;
				case "best vernacular":
					actualWS = cache.LangProject.ActualWs(kwsFirstVern, hvoObj, flid);
					if (actualWS == 0)
						actualWS = wsDefault;
					break;
				case "best analorvern":
					actualWS = cache.LangProject.ActualWs(kwsFirstAnalOrVern, hvoObj, flid);
					if (actualWS == 0)
						actualWS = wsDefault;
					break;
				case "best vernoranal":
					actualWS = cache.LangProject.ActualWs(kwsFirstVernOrAnal, hvoObj, flid);
					if (actualWS == 0)
						actualWS = wsDefault;
					break;
				case "pronunciation":
				case "all pronunciation":	// fixes LT-6665.
					actualWS = cache.LangProject.DefaultPronunciationWritingSystem;
					break;
				case "current":
					if (currentWS != null)
						actualWS = currentWS.WritingSystem;
					else
						actualWS = cache.DefaultUserWs;
					break;
				case "reversal":
					actualWS = GetReversalIndexEntryWritingSystem(cache, hvoObj, wsDefault);
					break;
				case "analysis vernacular":
				case "av":
					// Sometimes this is done, e.g., to figure out something about overall behavior of a column,
					// and we don't have a specific HVO. Since we prefer the analysis one, answer it when we don't
					// have a specific HVO.
					if (hvoObj == 0)
						actualWS = cache.DefaultAnalWs;
					else if (cache.MainCacheAccessor.get_MultiStringAlt(hvoObj, flid, cache.DefaultAnalWs).Length > 0)
						actualWS = cache.DefaultAnalWs;
					else if (cache.MainCacheAccessor.get_MultiStringAlt(hvoObj, flid, cache.DefaultVernWs).Length > 0)
						actualWS = cache.DefaultVernWs;
					else
						actualWS = cache.DefaultAnalWs;
					break;
				case "vernacular analysis":
				case "va":
					if (hvoObj == 0)
						actualWS = cache.DefaultVernWs;
					else if (cache.MainCacheAccessor.get_MultiStringAlt(hvoObj, flid, cache.DefaultVernWs).Length > 0)
						actualWS = cache.DefaultVernWs;
					else if (cache.MainCacheAccessor.get_MultiStringAlt(hvoObj, flid, cache.DefaultAnalWs).Length > 0)
						actualWS = cache.DefaultAnalWs;
					else
						actualWS = cache.DefaultVernWs;
					break;
				case "user":
					actualWS = cache.DefaultUserWs;
					break;
				default:
					// See if we can get anywhere by treating it as an ICU locale.
					// Note that it is important to do this in a way that won't create a new writing system for
					// an invalid locale name, for example, if 'all analysis' is mistakenly passed to this routine.
					// Note however that the behavior of recognizing an ICU locale name for an existing writing system
					// definitely IS needed, e.g., when the user configures a Browse view to show an explicit writing system.
					int wsT = cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr(wsSpec);
					if (wsT == 0)
						actualWS = wsDefault;
					else
						actualWS = wsT;
					break;
			}
			return actualWS;
		}

		/// <summary>
		/// Get the writing system for the given ReversalIndexEntry.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvoObj"></param>
		/// <param name="wsDefault"></param>
		/// <returns></returns>
		public static int GetReversalIndexEntryWritingSystem(FdoCache cache, int hvoObj, int wsDefault)
		{
			if (cache != null && hvoObj != 0)
			{
				IReversalIndex ri = null;
				int clid = cache.GetClassOfObject(hvoObj);
				switch (clid)
				{
					case ReversalIndex.kclsidReversalIndex:
						ri = ReversalIndex.CreateFromDBObject(cache, hvoObj);
						break;
					case ReversalIndexEntry.kclsidReversalIndexEntry:
						int hvoReversalIndex = cache.GetOwnerOfObjectOfClass(hvoObj, ReversalIndex.kclsidReversalIndex);
						if (hvoReversalIndex > 0)
							ri = ReversalIndex.CreateFromDBObject(cache, hvoReversalIndex);
						break;
					case PartOfSpeech.kclsidPartOfSpeech:
						// It may be nested, but we need the owner (index) of the list,
						// no matter how high up.
						int reversalIndexId = cache.GetOwnerOfObjectOfClass(hvoObj, ReversalIndex.kclsidReversalIndex);
						if (reversalIndexId > 0)
							ri = ReversalIndex.CreateFromDBObject(cache, reversalIndexId);
						break;
					case LexSense.kclsidLexSense:
					// Pick a plausible default reversal index for the LexSense.
					case LexDb.kclsidLexDb: // happens while initializing bulk edit combos
					default: // since this doesn't actually depend on the hvo, it's not a bad general default.
						List<int> rgriCurrent = cache.LangProject.LexDbOA.CurrentReversalIndices;
						if (rgriCurrent.Count > 0)
						{
							ri = ReversalIndex.CreateFromDBObject(cache, (int)rgriCurrent[0]);
						}
						else
						{
							if (cache.LangProject.LexDbOA.ReversalIndexesOC.Count > 0)
							{
								int hvo = cache.LangProject.LexDbOA.ReversalIndexesOC.HvoArray[0];
								ri = (IReversalIndex)CmObject.CreateFromDBObject(cache, hvo);
							}
						}
						break;
				}
				if (ri != null)
					return ri.WritingSystemRAHvo;
			}
			return wsDefault;
		}


		/*----------------------------------------------------------------------------------------------

			@param ws
			@return a real writing system
		----------------------------------------------------------------------------------------------*/
		/// <summary>
		/// Convert a writing system (could be magic) to a real writing system.
		/// </summary>
		/// <param name="magicName">Writing system to convert, which may be a 'magic'</param>
		/// <param name="hvo">Optional hvo that owns the string.</param>
		/// <param name="flid">Optional flid for the owned string.</param>
		/// <returns></returns>
		/// <remarks>
		/// The hvo and flid parameters are only used for the four 'magic' WSes
		/// that try to get data from a preferred list of WSes.
		/// </remarks>
		public int ActualWs(string magicName, int hvo, int flid)
		{
			int retWs = GetMagicWsIdFromName(magicName);
			if (retWs != 0)
				retWs = ActualWs(retWs, hvo, flid);
			return retWs;
		}


		/*----------------------------------------------------------------------------------------------

			@param ws
			@return a real writing system
		----------------------------------------------------------------------------------------------*/
		/// <summary>
		/// Convert a writing system (could be magic) to a real writing system.
		/// </summary>
		/// <param name="ws">Writing system to convert, which may be a 'magic'</param>
		/// <param name="hvo">Optional hvo that owns the string.</param>
		/// <param name="flid">Optional flid for the owned string.</param>
		/// <returns></returns>
		/// <remarks>
		/// The hvo and flid parameters are only used for the four 'magic' WSes
		/// that try to get data from a preferred list of WSes.
		/// </remarks>
		public int ActualWs(int ws, int hvo, int flid)
		{
			int actualWs;
			GetMagicStringAlt(ws, hvo, flid, false, out actualWs);
			return actualWs;
		}

		/// <summary>
		/// Extract a string and writing system from an object, flid, and 'magic' writing
		/// system code.
		/// </summary>
		/// <param name="ws">Writing system to convert, which may be a 'magic'</param>
		/// <param name="hvo">Hvo that owns the string.</param>
		/// <param name="flid">Flid for the owned string.</param>
		/// <returns></returns>
		public ITsString GetMagicStringAlt(int ws, int hvo, int flid)
		{
			int actualWs;
			return GetMagicStringAlt(ws, hvo, flid, true, out actualWs);
		}

		/// <summary>
		/// Check that the given (possibly magic) ws can make a string for our owning object and return the actual ws.
		/// </summary>
		/// <param name="ws">the (possibly magic) ws</param>
		/// <param name="hvoOwner">Hvo that owns the string.</param>
		/// <param name="flidOwning">Flid for the owned string.</param>
		/// <param name="actualWs">the actual ws we can make a string with</param>
		/// <returns>true, if we can make a string with the given logical ws.</returns>
		public bool TryWs(int ws, int hvoOwner, int flidOwning, out int actualWs)
		{
			ITsString tssResult = m_cache.LangProject.GetMagicStringAlt(ws, hvoOwner, flidOwning, false, out actualWs);
			return actualWs > 0;
		}

		/// <summary>
		/// Try the given ws and return the resulting tss and actualWs.
		/// </summary>
		/// <param name="ws"></param>
		/// <param name="hvoOwner"></param>
		/// <param name="flidOwning"></param>
		/// <param name="actualWs"></param>
		/// <param name="tssResult"></param>
		/// <returns></returns>
		public bool TryWs(int ws, int hvoOwner, int flidOwning, out int actualWs, out ITsString tssResult)
		{
			tssResult = m_cache.LangProject.GetMagicStringAlt(ws, hvoOwner, flidOwning, true, out actualWs);
			return actualWs > 0 && tssResult.Length > 0;
		}

		/// <summary>
		/// first try wsPreferred, then wsSecondary (both can be a magic).
		/// </summary>
		/// <param name="wsPreferred">(can be magic)</param>
		/// <param name="wsSecondary">(can be magic)</param>
		/// <param name="hvoOwner"></param>
		/// <param name="flidOwning"></param>
		/// <param name="actualWs"></param>
		/// <param name="tssResult"></param>
		/// <returns></returns>
		public bool TryWs(int wsPreferred, int wsSecondary, int hvoOwner, int flidOwning, out int actualWs, out ITsString tssResult)
		{
			actualWs = 0;
			if (!TryWs(wsPreferred, hvoOwner, flidOwning, out actualWs, out tssResult))
			{
				return TryWs(wsSecondary, hvoOwner, flidOwning, out actualWs, out tssResult);
			}
			return true;
		}

		/// <summary>
		/// Extract a string and writing system from an object, flid, and 'magic' writing
		/// system code.
		/// </summary>
		/// <param name="ws">Writing system to convert, which may be a 'magic'</param>
		/// <param name="hvo">Hvo that owns the string.</param>
		/// <param name="flid">Flid for the owned string.</param>
		/// <param name="fWantString">false if we don't really care about the string.
		/// This allows some branches to avoid retrieving it at all.</param>
		/// <param name="retWs">Retrieves the actual ws that the returned string
		/// belongs to.</param>
		/// <returns></returns>
		public ITsString GetMagicStringAlt(int ws, int hvo, int flid, bool fWantString, out int retWs)
		{
			Debug.Assert(ws != 0);
			SIL.FieldWorks.Common.COMInterfaces.ISilDataAccess sda = m_cache.MainCacheAccessor;
			retWs = 0; // Start on the pessimistic side.
			ITsString retTss = null;
			switch (ws)
			{
				case kwsVernInParagraph:
					// Even if we don't pass in a twfic, we can guess the ws in general for a text's paragraph
					// is the ws of the first character in its string.
					int clsid = Cache.GetClassOfObject(hvo);
					int hvoPara = 0;
					if (clsid == CmBaseAnnotation.kClassId)
					{
						// use the ws of CmBaseAnnotation.BeginOffset in the paragraph.
						retWs = StTxtPara.GetTwficWs(Cache, hvo);
						break; // We got it, don't want to try to figure from hvoPara (which is zero).
					}
					else if (clsid == StTxtPara.kClassId)
					{
						hvoPara = hvo;
						// use the ws of the first character in the paragraph.
					}
					else if (Cache.IsSameOrSubclassOf((int)Cache.GetDestinationClass((uint)flid), StPara.kClassId))
					{
						// use the ws of the first paragraph
						if (Cache.IsVectorProperty(flid))
						{
							int cPara = Cache.GetVectorSize(hvo, flid);
							if (cPara > 0)
								hvoPara = Cache.GetVectorItem(hvo, flid, 0);
						}
						else
						{
							hvoPara = Cache.GetObjProperty(hvo, flid);
						}
					}
					if (hvoPara != 0)
						retWs = StTxtPara.GetWsAtParaOffset(Cache, hvoPara, 0);
					else
						retWs = DefaultVernacularWritingSystem;
					break;
				case kwsAnals:
				case kwsAnal:
				case kwsAnalVerns:
					retWs = m_iDefaultAnalysisWritingSystem;
					break;
				case kwsVerns:
				case kwsVern:
				case kwsVernAnals:
					retWs = m_iDefaultVernacularWritingSystem;
					break;
				case kwsFirstAnal:
					// JohnT: we can't afford these, they are catastrophically expensive tests
					// (a whole SQL query each call to IsValidObject). Something will fail
					// if it isn't a valid object and no value has been preloaded.
					//Debug.Assert(m_cache.IsValidObject(hvo));
					//if (!m_cache.IsValidObject(hvo))
					//    break;
					if (flid == 0) // sometimes used this way, just trying for a ws...make robust
					{
						retWs = DefaultAnalysisWritingSystem;
						return null;
					}
					foreach (int wsLoop in CurAnalysisWssRS.HvoArray)
					{
						retTss = sda.get_MultiStringAlt(hvo, flid, wsLoop);
						if (retTss.Length > 0)
						{
							retWs = wsLoop;
							break;
						}
					}
					if (retWs == 0)
					{
						// Try non-current analysis WSes.
						foreach (int wsLoop in AnalysisWssRC.HvoArray)
						{
							retTss = sda.get_MultiStringAlt(hvo, flid, wsLoop);
							if (retTss.Length > 0)
							{
								retWs = wsLoop;
								break;
							}
						}
					}
					if (retWs == 0)
					{
						// Now try the default user ws.
						retTss = sda.get_MultiStringAlt(hvo, flid, DefaultUserWritingSystem);
						if (retTss.Length > 0)
						{
							retWs = DefaultUserWritingSystem;
							break;
						}
					}
					if (retWs == 0)
					{
						// Now try the cache's fallback WS.
						retTss = sda.get_MultiStringAlt(hvo, flid, m_cache.FallbackUserWs);
						if (retTss.Length > 0)
						{
							retWs = m_cache.FallbackUserWs;
							break;
						}
					}
					if (retWs == 0)
					{
						// Now try English.
						int en = m_cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr("en");
						retTss = sda.get_MultiStringAlt(hvo, flid, en);
						if (retTss.Length > 0)
						{
							retWs = en;
							break;
						}
					}
					break;
				case kwsFirstVernOrNamed:
				case kwsFirstVern:
					// JohnT: we can't afford these, they are catastrophically expensive tests
					// (a whole SQL query each call to IsValidObject). Something will fail
					// if it isn't a valid object and no value has been preloaded.
					//Debug.Assert(m_cache.IsValidObject(hvo));
					//if (!m_cache.IsValidObject(hvo))
					//    break;
					if (flid == 0) // sometimes used this way, just trying for a ws...make robust
					{
						retWs = DefaultVernacularWritingSystem;
						return null;
					}
					Set<int> triedWsList = new Set<int>();
					// try the current vernacular writing systems
					if (TryFirstWsInList(sda, hvo, flid, CurVernWssRS.HvoArray,
						ref triedWsList, out retWs, out retTss))
					{
						break;
					}
					// Try non-current vernacular WSes.
					if (TryFirstWsInList(sda, hvo, flid, VernWssRC.HvoArray,
							ref triedWsList, out retWs, out retTss))
					{
						break;
					}
					// Now try the default user ws.
					if (TryFirstWsInList(sda, hvo, flid, new int[] { DefaultUserWritingSystem },
						ref triedWsList, out retWs, out retTss))
					{
						break;
					}
					// Now try the cache's fallback ws.
					if (TryFirstWsInList(sda, hvo, flid, new int[] { m_cache.FallbackUserWs },
						ref triedWsList, out retWs, out retTss))
					{
						break;
					}
					// Now try English.
					if (TryFirstWsInList(sda, hvo, flid, new int[] { m_cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr("en") },
						ref triedWsList, out retWs, out retTss))
					{
						break;
					}
					if (ws == kwsFirstVernOrNamed)
					{
						// try to get a ws in the named writing systems that we haven't already tried.
						if (TryFirstWsInList(sda, hvo, flid, Cache.LanguageEncodings.HvoArray,
							ref triedWsList, out retWs, out retTss))
						{
							break;
						}
					}
					break;
				case kwsFirstAnalOrVern:
					// JohnT: we can't afford these, they are catastrophically expensive tests
					// (a whole SQL query each call to IsValidObject). Something will fail
					// if it isn't a valid object and no value has been preloaded.
					//Debug.Assert(m_cache.IsValidObject(hvo));
					//if (!m_cache.IsValidObject(hvo))
					//    break;
					if (flid == 0) // sometimes used this way, just trying for a ws...make robust
					{
						retWs = DefaultAnalysisWritingSystem;
						return null;
					}
					foreach (int wsLoop in CurAnalysisWssRS.HvoArray)
					{
						retTss = sda.get_MultiStringAlt(hvo, flid, wsLoop);
						if (retTss.Length > 0)
						{
							retWs = wsLoop;
							break;
						}
					}
					if (retWs == 0)
					{
						foreach (int wsLoop in CurVernWssRS.HvoArray)
						{
							retTss = sda.get_MultiStringAlt(hvo, flid, wsLoop);
							if (retTss.Length > 0)
							{
								retWs = wsLoop;
								break;
							}
						}
					}
					if (retWs == 0)
					{
						// Try non-current analysis WSes.
						foreach (int wsLoop in AnalysisWssRC.HvoArray)
						{
							retTss = sda.get_MultiStringAlt(hvo, flid, wsLoop);
							if (retTss.Length > 0)
							{
								retWs = wsLoop;
								break;
							}
						}
					}
					if (retWs == 0)
					{
						// Try non-current vernacular WSes.
						foreach (int wsLoop in VernWssRC.HvoArray)
						{
							retTss = sda.get_MultiStringAlt(hvo, flid, wsLoop);
							if (retTss.Length > 0)
							{
								retWs = wsLoop;
								break;
							}
						}
					}
					if (retWs == 0)
					{
						// Now try the default user ws.
						retTss = sda.get_MultiStringAlt(hvo, flid, DefaultUserWritingSystem);
						if (retTss.Length > 0)
						{
							retWs = DefaultUserWritingSystem;
							break;
						}
					}
					if (retWs == 0)
					{
						// Now try the cache's fallback ws.
						retTss = sda.get_MultiStringAlt(hvo, flid, m_cache.FallbackUserWs);
						if (retTss.Length > 0)
						{
							retWs = m_cache.FallbackUserWs;
							break;
						}
					}
					if (retWs == 0)
					{
						// Now try English.
						int en = m_cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr("en");
						retTss = sda.get_MultiStringAlt(hvo, flid, en);
						if (retTss.Length > 0)
						{
							retWs = en;
							break;
						}
					}
					break;
				case kwsFirstVernOrAnal:
					// JohnT: we can't afford these, they are catastrophically expensive tests
					// (a whole SQL query each call to IsValidObject). Something will fail
					// if it isn't a valid object and no value has been preloaded.
					//Debug.Assert(m_cache.IsValidObject(hvo));
					//if (!m_cache.IsValidObject(hvo))
					//    break;
					if (flid == 0) // sometimes used this way, just trying for a ws...make robust
					{
						retWs = DefaultVernacularWritingSystem;
						return null;
					}
					foreach (int wsLoop in CurVernWssRS.HvoArray)
					{
						retTss = sda.get_MultiStringAlt(hvo, flid, wsLoop);
						if (retTss.Length > 0)
						{
							retWs = wsLoop;
							break;
						}
					}
					if (retWs == 0)
					{
						foreach (int wsLoop in CurAnalysisWssRS.HvoArray)
						{
							retTss = sda.get_MultiStringAlt(hvo, flid, wsLoop);
							if (retTss.Length > 0)
							{
								retWs = wsLoop;
								break;
							}
						}
					}
					if (retWs == 0)
					{
						// Try non-current vernacular WSes.
						foreach (int wsLoop in VernWssRC.HvoArray)
						{
							retTss = sda.get_MultiStringAlt(hvo, flid, wsLoop);
							if (retTss.Length > 0)
							{
								retWs = wsLoop;
								break;
							}
						}
					}
					if (retWs == 0)
					{
						// Try non-current analysis WSes.
						foreach (int wsLoop in AnalysisWssRC.HvoArray)
						{
							retTss = sda.get_MultiStringAlt(hvo, flid, wsLoop);
							if (retTss.Length > 0)
							{
								retWs = wsLoop;
								break;
							}
						}
					}
					if (retWs == 0)
					{
						// Now try the default user ws.
						retTss = sda.get_MultiStringAlt(hvo, flid, DefaultUserWritingSystem);
						if (retTss.Length > 0)
						{
							retWs = DefaultUserWritingSystem;
							break;
						}
					}
					if (retWs == 0)
					{
						// Now try the cache's fallback ws.
						retTss = sda.get_MultiStringAlt(hvo, flid, m_cache.FallbackUserWs);
						if (retTss.Length > 0)
						{
							retWs = m_cache.FallbackUserWs;
							break;
						}
					}
					if (retWs == 0)
					{
						// Now try English.
						int en = m_cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr("en");
						retTss = sda.get_MultiStringAlt(hvo, flid, en);
						if (retTss.Length > 0)
						{
							retWs = en;
							break;
						}
					}
					break;
				default:
					retWs = ws;
					break;
			}
			if (retWs != 0 && fWantString && retTss == null)
			{
				retTss = sda.get_MultiStringAlt(hvo, flid, retWs);
			}
			return retTss;
		}

		static internal bool TryFirstWsInList(SIL.FieldWorks.Common.COMInterfaces.ISilDataAccess sda, int hvo, int flid,
			int[] wssToTry, ref Set<int> wssTried, out int retWs, out ITsString retTss)
		{
			retTss = null;
			retWs = 0;
			foreach (int wsLoop in wssToTry)
			{
				if (wssTried.Contains(wsLoop))
					continue;
				wssTried.Add(wsLoop);
				retTss = sda.get_MultiStringAlt(hvo, flid, wsLoop);
				if (retTss.Length > 0)
				{
					retWs = wsLoop;
					return true;
				}
			}
			return false;
		}

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The DateModified property in the base class (i.e. BaseCmProject) doesn't really
		/// return the correct date because it appears the modified date for the language
		/// project object doesn't get updated anywhere in the FDO Cache code. It may be just
		/// as well since updating it everytime a cmObject changes may take more time than
		/// just getting it the way we are below (i.e. using the MAX command on the cmObject
		/// table).
		/// JohnT: for some reason an earlier (pre-July 2009) version of this made a new
		/// connection to the database to retrieve this information, but this could time out
		/// (e.g., LT-9336).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override DateTime DateModified
		{
			get
			{
				if (m_cache.ServerName == string.Empty || m_cache.DatabaseName == string.Empty)
					return DateTime.Now; // Should only happen in tests!!!

				string modtime = DbOps.ReadString(m_cache, "select convert(nvarchar, MAX(Upddttm)) from cmobject", null);
				return DateTime.Parse(modtime);
			}

			set { base.DateModified = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the default analysis writing system (i.e. Writing System Code).
		/// </summary>
		/// <returns>The first writing system code in CurAnalysisWssRS.</returns>
		/// ------------------------------------------------------------------------------------
		public int DefaultAnalysisWritingSystem
		{
			get
			{
				//review Randy (JohnH): it seems like the writing system number, rather than the hvo, is what is useful.
				//this does seem inefficient, seems like the string functions could take an hvo.
				//				return new CurAnalysisWssRS.HvoArray[0];
				return m_iDefaultAnalysisWritingSystem;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the default analysis writing system's ICU Locale.
		/// </summary>
		/// <returns>The first writing system's ICU Locale in CurAnalysisWssRS.</returns>
		/// ------------------------------------------------------------------------------------
		public string DefaultAnalysisWritingSystemICULocale
		{
			get { return m_sDefaultAnalysisWritingSystemICULocale; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the default analysis writing system's name.
		/// </summary>
		/// <returns>The first writing system's Name in CurAnalysisWssRS.</returns>
		/// ------------------------------------------------------------------------------------
		public MultiUnicodeAccessor DefaultAnalysisWritingSystemName
		{
			get { return m_DefaultAnalysisWritingSystemName; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the default user writing system (i.e. Writing System Code).
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public int DefaultUserWritingSystem
		{
			get
			{
				return m_iDefaultUserWritingSystem;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the default user writing system's ICU Locale.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public string DefaultUserWritingSystemICULocale
		{
			get { return m_sDefaultUserWritingSystemICULocale; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the default user writing system's name.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public MultiUnicodeAccessor DefaultUserWritingSystemName
		{
			get { return m_DefaultUserWritingSystemName; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a font name for the default font for the vernacular writing system.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public string DefaultUserWritingSystemFont
		{
			get
			{
				/*JT says: does NOT ensure that the resulting font is actually available on the system.
				 * We had high hopes at one point of listing several fonts and returning the most
				 * desirable available one, and still have hopes of developing code to bundle up
				 * everything a WS needs and install it all in one operation, but neither has happened.
				 * And even if we develop 'bundle it up' code, there will be cases (e.g., where the font
				 * in question is proprietary) where we have to leave it out and hope the user installs
				 * it separately himself.
				* This is no small can of worms. I'm not sure how we should best proceed. But whatever
				* answer we come up with should be implemented in that get_DefaltSerif method of
				* ILgWritingSystem, not in a dozen inconsistent ways all over the system.
				*/

				//based on the comment from John Thomson, we should expect that this will someday get us
				//a font which does exist. Therefore, I (JH)would like to write this so that it does the checking here
				//so that clients don't need to check. It would like to throw an exception if the font is not available.
				//however, they don't really want to think this to Windows forms just to make the one call.
				//so for now, the client had better check to see if this is available.
				return m_cache.LanguageWritingSystemFactoryAccessor.
					get_EngineOrNull(DefaultUserWritingSystem).DefaultSerif;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the default vernacular writing system (i.e. Writing System Code).
		/// </summary>
		/// <returns>The first writing system code in CurVernWssRS.</returns>
		/// ------------------------------------------------------------------------------------
		public int DefaultVernacularWritingSystem
		{
			get
			{
				//return CurVernWssRS.HvoArray[0];
				return m_iDefaultVernacularWritingSystem;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the default vernacular writing system's ICU Locale.
		/// </summary>
		/// <returns>The first writing system's ICU Locale in CurVernWssRS.</returns>
		/// ------------------------------------------------------------------------------------
		public string DefaultVernacularWritingSystemICULocale
		{
			get { return m_sDefaultVernacularWritingSystemICULocale; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the default vernacular writing system's name.
		/// </summary>
		/// <returns>The first writing system's name in CurVernWssRS.</returns>
		/// ------------------------------------------------------------------------------------
		public MultiUnicodeAccessor DefaultVernacularWritingSystemName
		{
			get { return m_DefaultVernacularWritingSystemName; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a font name for the default font for the vernacular writing system.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public string DefaultVernacularWritingSystemFont
		{
			get
			{
				/*JT says: does NOT ensure that the resulting font is actually available on the system.
				 * We had high hopes at one point of listing several fonts and returning the most
				 * desirable available one, and still have hopes of developing code to bundle up
				 * everything a WS needs and install it all in one operation, but neither has happened.
				 * And even if we develop 'bundle it up' code, there will be cases (e.g., where the font
				 * in question is proprietary) where we have to leave it out and hope the user installs
				 * it separately himself.
				* This is no small can of worms. I'm not sure how we should best proceed. But whatever
				* answer we come up with should be implemented in that get_DefaltSerif method of
				* ILgWritingSystem, not in a dozen inconsistent ways all over the system.
				*/

				//based on the comment from John Thomson, we should expect that this will someday get us
				//a font which does exist. Therefore, I (JH)would like to write this so that it does the checking here
				//so that clients don't need to check. It would like to throw an exception if the font is not available.
				//however, they don't really want to think this to Windows forms just to make the one call.
				//so for now, the client had better check to see if this is available.
				return m_cache.LanguageWritingSystemFactoryAccessor.
					get_EngineOrNull(DefaultVernacularWritingSystem).DefaultSerif;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the default pronunciation writing system (i.e. Writing System Code).
		/// </summary>
		/// <returns>
		/// The first writing system code in CurPronunWssRS.  If that is
		/// empty, searches VernWssRS for a writing system whose ICULocale ends
		/// with "_IPA", and returns the corresponding Hvo if found.  If none are found, returns
		/// the default vernacular writing system (JT change---earlier version was 0). This is
		/// not likely to be very good for representing pronunciation, but it's better than
		/// returning zero, which (for example) will cause StringSlice to think the field
		/// is not multilingual at all.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public int DefaultPronunciationWritingSystem
		{
			get
			{
				InitializePronunciationWritingSystems();
				return CurPronunWssRS.HvoArray[0];
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a font name for the default font for the analysis writing system.
		/// </summary>
		/// <remarks> see comments under DefaultVernacularWritingSystemFont</remarks>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public string DefaultAnalysisWritingSystemFont
		{
			get
			{
				return GetDefaultFontForWs(DefaultAnalysisWritingSystem);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///  Get a font name for the default font for the given writing system.
		/// </summary>
		/// <remarks> see comments under DefaultVernacularWritingSystemFont</remarks>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public string GetDefaultFontForWs(int ws)
		{
			return m_cache.LanguageWritingSystemFactoryAccessor.
					get_EngineOrNull(ws).DefaultSerif;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// get the analyzing agent representing the current user
		/// </summary>
		/// <returns>a ICmAgent object interface</returns>
		/// <exception cref="ApplicationException"/>
		/// TODO JohnH:figure out what the right exception is here
		/// TODO JohnH:this needs a unit test
		/// ------------------------------------------------------------------------------------
		public ICmAgent DefaultUserAgent
		{
			get
			{
				ICmAgent a = GetAgent(kguidAgentDefUser);
				if (null == a)
				{
					throw new ApplicationException("There was no default user agent. This should be part of the NewLangProj.");
				}
				return a;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// get the analyzing agent representing the current parser
		/// </summary>
		/// <returns>a CmAgent Object</returns>
		/// <exception cref="ApplicationException"/>
		/// TODO JohnH:figure out what the right exception is here
		/// TODO JohnH:this needs a unit test
		/// ------------------------------------------------------------------------------------
		public ICmAgent DefaultParserAgent
		{
			get
			{
				ICmAgent agent = null;
				switch (MorphologicalDataOA.ActiveParser)
				{
					case "HC":
						agent = GetAgent(kguidAgentHCParser);
						break;
					case "XAmple":
						agent = GetAgent(kguidAgentM3Parser);
						break;
				}
				if (agent == null)
					throw new ApplicationException("There was no default parser agent.");
				return agent;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the analyzing agent representing the computer. Do not use this for the parser;
		/// there is a dedicated agent for that purpose.
		/// </summary>
		/// <returns>a CmAgent Object</returns>
		/// <exception cref="ApplicationException">There was no default computer agent. This
		/// should be part of the NewLangProj.</exception>
		/// ------------------------------------------------------------------------------------
		public ICmAgent DefaultComputerAgent
		{
			get
			{
				if (m_defaultComputerAgent != null)
					return m_defaultComputerAgent;

				m_defaultComputerAgent = GetAgent(kguidAgentComputer);
				if (null == m_defaultComputerAgent)
				{
					// Go ahead and make one. This should nor normally happen except in tests.
					m_defaultComputerAgent = new CmAgent();
					m_cache.LangProject.AnalyzingAgentsOC.Add(m_defaultComputerAgent);
					m_defaultComputerAgent.Name.SetAlternative("Computer",
						m_cache.LanguageWritingSystemFactoryAccessor.get_Engine("en").WritingSystem);
					m_defaultComputerAgent.Human = false;
					m_defaultComputerAgent.Version = "Normal";
					m_defaultComputerAgent.Guid = kguidAgentComputer;
				}
				return m_defaultComputerAgent;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// get the agent to assigning "constraint failure" annotations to
		/// </summary>
		/// <returns>a CmAgent Object</returns>
		/// <exception cref="ApplicationException"/>
		/// ------------------------------------------------------------------------------------
		public ICmAgent ConstraintCheckerAgent
		{
			get
			{
				return DefaultParserAgent;	//this will do until we decide that we need more than 1 computational agent.
			}
		}
		#endregion	// Properties

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overrides method to get a more suitable way to display a LangProject.
		/// </summary>
		/// <returns>The name of the language project.</returns>
		/// ------------------------------------------------------------------------------------
		public override string ToString()
		{
			string name = Name.AnalysisDefaultWritingSystem;
			if (name == null)
				name = Name.UserDefaultWritingSystem;
			return name;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method was changed by JohnT on instructions from KenZ so that it does not
		/// depend on which writing systems have names recorded in which encodings. Rather,
		/// we want to return the UIName for each language. Also, instead of
		/// just returning languages that have been installed into the current database, it
		/// now returns all languages that are EITHER in the database OR in the fwroot/languages
		/// directory, provided they've been properly installed in ICU so they actually have
		/// display names.
		///
		/// Here's a summary of what this method used to do, just in case we change our minds.
		/// assume your DB has 4 writing systems:
		///             HVO  | ICU Locale | English Name | Spanish Name | French Name | IPA Name
		///             -----+------------+--------------+--------------+-------------+---------
		/// def anal -> 1000 | en         | English      | inglés       |             |
		/// UI       -> 2345 | es         | Spanish      | español      | espagnol    | español
		/// def vern -> 4567 | fr         |              | francés      | francais    |
		///             2397 | en-ipa     | English IPA  |              |             | aipie
		///
		/// The returned list will be:
		///     Name        | Ws
		///     ------------+-------
		///		inglés      | (1000)
		///		español     | (2345)
		///		francés     | (4567)
		///		English IPA | (2397)
		///
		/// </summary>
		/// <returns>Complete set of all writing systems in the database or the languages
		/// folder, with the appropriate (ICU) display name for each.</returns>
		/// ------------------------------------------------------------------------------------
		public Set<NamedWritingSystem> GetAllNamedWritingSystems()
		{
			string displayLocale = Cache.LanguageWritingSystemFactoryAccessor.GetStrFromWs(Cache.DefaultUserWs);
			Set<NamedWritingSystem> namedWritingSystems = GetNamedWritingSystemsFromDb(displayLocale);
			return GetNamedWritingSystemsFromLDFs(Cache.LanguageWritingSystemFactoryAccessor,
				namedWritingSystems);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This static method obtains a list of NamedWritingSystem objects for all the
		/// writing systems currently known (and installed, at least to ICU). It is static
		/// so that it may be used for cases where there is no language project object,
		/// such as when creating a new database or in WorldPad. In this case, it retrieves
		/// only the writing systems for which there are XML files in the languages directory,
		/// and which have been installed by our install program or otherwise so that they
		/// are known to ICU.
		/// </summary>
		/// <param name="wsf"></param>
		/// <param name="displayLocale"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static Set<NamedWritingSystem> GetAllNamedWritingSystemsFromLDFs(ILgWritingSystemFactory wsf,
			string displayLocale)
		{
			return GetNamedWritingSystemsFromLDFs(wsf, new Set<NamedWritingSystem>());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds all of the LgWritingSystems in source to the namedWritingSystems set.
		/// </summary>
		/// <param name="namedWritingSystems"></param>
		/// <param name="source"></param>
		/// <param name="displayLocale">REVIEW (TimS): Looks like this param is not used (maybe
		/// used to be used). Is there a plan to use it or can it be removed?</param>
		/// ------------------------------------------------------------------------------------
		private static void AddWsNamedObjsToSet(Set<NamedWritingSystem> namedWritingSystems,
			IEnumerable source, string displayLocale)
		{
			foreach (LgWritingSystem ws in source)
			{
				NamedWritingSystem nws = new NamedWritingSystem(ws.ShortName, ws.ICULocale, ws.Hvo);
				// TE-6958 kept crashing when we tried to get a hashcode for a malformed ws. Instead
				// of crashing, let's just skip the bum ws. It's still a mystery how we apparently get
				// a ws without a name since the name will default to the IcuLocale if it is missing.
				if (nws.Name != null && nws.IcuLocale != null)
					namedWritingSystems.Add(nws);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This version is implemented for easier testing.
		/// TODO: Make this a member method.
		/// </summary>
		/// <param name="lp"></param>
		/// <param name="displayLocale"></param>
		/// <param name="fileList"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static Set<NamedWritingSystem> GetAllNamedWritingSystems(ILangProject lp,
			string displayLocale, string[] fileList)
		{
			Set<NamedWritingSystem> namedWritingSystems = (lp as LangProject).GetNamedWritingSystemsFromDb(displayLocale);
			GetNamedWritingSystemsFromLDFs(lp.Cache.LanguageWritingSystemFactoryAccessor, fileList, namedWritingSystems);
			return namedWritingSystems;
		}

		private Set<NamedWritingSystem> GetNamedWritingSystemsFromDb(string displayLocale)
		{
			Set<NamedWritingSystem> namedWritingSystems = new Set<NamedWritingSystem>();
			AddWsNamedObjsToSet(namedWritingSystems, Cache.LanguageEncodings, displayLocale);
			return namedWritingSystems;
		}

		private static Set<NamedWritingSystem> GetNamedWritingSystemsFromLDFs(ILgWritingSystemFactory wsf, Set<NamedWritingSystem> namedWritingSystems)
		{
			return GetNamedWritingSystemsFromLDFs(wsf,
				Directory.GetFiles(DirectoryFinder.LanguagesDirectory, "*.xml"),
				namedWritingSystems);
		}

		private static Set<NamedWritingSystem> GetNamedWritingSystemsFromLDFs(ILgWritingSystemFactory wsf,
			string[] fileList, Set<NamedWritingSystem> namedWritingSystems)
		{
			Set<string> names = new Set<string>();
			foreach (NamedWritingSystem nws in namedWritingSystems)
				names.Add(nws.IcuLocale);
			// Now add the ones from the XML files.
			foreach (string pathname in fileList)
			{
				string[] bits = pathname.Split('\\');
				string filename = bits[bits.Length - 1];
				bits = filename.Split('.');
				string icuLocale = bits[0]; // Name up to first '.'.
				// The first test excludes names like en.xml1.
				if (bits[1] == "xml" && !names.Contains(icuLocale.ToLowerInvariant()))
				{
					try
					{
						// This will get the language name from the XML language def. file. This
						// should be the same name the user chose to call the language when creating
						// its writing system.
						LanguageDefinitionFactory ldf = new LanguageDefinitionFactory(wsf, icuLocale);
						if (ldf.LanguageDefinition == null)
						{
							System.Diagnostics.Debug.WriteLine("The XML file for " + icuLocale +
								" did not parse properly.");
						}
						else
						{
							string displayName = ldf.LanguageDefinition.DisplayName;

							// REVIEW: These two lines are how we used to get the display name. Now we read
							// it from the language def. file (i.e. the .xml file). Will the name from the
							// XML file always be in the display locale?
							//Icu.UErrorCode err;
							//Icu.GetDisplayName(icuLocale, displayLocale, out displayName, out err);

							// If it can't find a name, the normal behavior is to return the icuLocale.
							// If that happens we leave this one out.
							// If anything worse happens (e.g., that might produce a bad error code),
							// the other checks we make here should detect it.
							if (displayName != null && displayName != icuLocale && displayName.Length != 0)
								namedWritingSystems.Add(new NamedWritingSystem(displayName, icuLocale));
						}
					}
					catch (FileNotFoundException e)
					{
						System.Diagnostics.Debug.WriteLine(e.Message);
						// LanguageDefinitionFactory can throw this error. Just ignore it.
					}
				}
			}
			return namedWritingSystems;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a set of NamedWritingSystem objects for all the vernacular and analysis
		/// writing systems in the current database.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public virtual Set<NamedWritingSystem> GetActiveNamedWritingSystems()
		{
			string displayLocale =
				m_cache.LanguageWritingSystemFactoryAccessor.GetStrFromWs(m_cache.DefaultUserWs);
			Set<NamedWritingSystem> namedWritingSystems =
				new Set<NamedWritingSystem>(VernWssRC.Count + AnalysisWssRC.Count);
			AddWsNamedObjsToSet(namedWritingSystems, VernWssRC, displayLocale);
			AddWsNamedObjsToSet(namedWritingSystems, AnalysisWssRC, displayLocale);
			return namedWritingSystems;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a set of NamedWritingSystem objects for all writing systems in the
		/// database.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public virtual Set<NamedWritingSystem> GetDbNamedWritingSystems()
		{
			string displayLocale =
				m_cache.LanguageWritingSystemFactoryAccessor.GetStrFromWs(m_cache.DefaultUserWs);
			Set<NamedWritingSystem> namedWritingSystems = new Set<NamedWritingSystem>();
			AddWsNamedObjsToSet(namedWritingSystems, Cache.LanguageEncodings, displayLocale);
			return namedWritingSystems;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a set of NamedWritingSystem objects for the current pronunciation
		/// writing systems.  If that set is empty, writing systems from the vernacular set
		/// with ICULocale variants of IPA or EMC are added to the current pronunciation writing
		/// systems.  If the pronunciation set is still empty, the first vernacular writing
		/// system is added to it.
		/// </summary>
		/// <returns>
		/// Set of current pronunciation writing systems as NamedWritingSystem objects
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public Set<NamedWritingSystem> GetPronunciationWritingSystems()
		{
			InitializePronunciationWritingSystems();
			string displayLocale =
				m_cache.LanguageWritingSystemFactoryAccessor.GetStrFromWs(m_cache.DefaultUserWs);
			Set<NamedWritingSystem> writingSystems = GetDbNamedWritingSystems();
			ILgWritingSystem wsVern = CurVernWssRS[0];
			string sVern = wsVern.ICULocale;
			int idx = sVern.IndexOf("_");
			if (idx > 0)
				sVern = sVern.Substring(0, idx + 1).ToLowerInvariant();
			else
				sVern = sVern.ToLower() + "_";
			Set<NamedWritingSystem> pronunciationWritingSystems =
				new Set<NamedWritingSystem>(CurPronunWssRS.Count + 1);
			AddWsNamedObjsToSet(pronunciationWritingSystems, CurPronunWssRS, displayLocale);
			foreach (NamedWritingSystem nws in writingSystems)
			{
				if (nws.IcuLocale == wsVern.ICULocale || nws.IcuLocale.IndexOf(sVern) == 0)
					pronunciationWritingSystems.Add(nws);
			}
			return pronunciationWritingSystems;
		}

		/// <summary>
		/// Get the language project's list of pronunciation writing systems into sync
		/// with the list in the property table (given in sValues).  The latter is directly
		/// set by the menu on the pronunciation slice.
		/// </summary>
		/// <param name="sValues">comma delimited list of database ids for writing systems</param>
		public void UpdatePronunciationWritingSystems(string sValues)
		{
			string[] rgsValues = sValues.Split(',');
			int[] newValues = new int[rgsValues.Length];
			for (int i = 0; i < rgsValues.Length; ++i)
				newValues[i] = Int32.Parse(rgsValues[i]);
			UpdatePronunciationWritingSystems(newValues);
		}

		/// <summary>
		/// Get the language project's list of pronunciation writing systems into sync with the supplied list.
		/// </summary>
		public void UpdatePronunciationWritingSystems(int[] newValues)
		{
			// It may not really be 'easier', but updating the whole proeprty in one shot will fire off far fewer UpdateProps.
			int[] originalData = m_cache.LangProject.CurPronunWssRS.HvoArray;
			int newValuesCount = newValues.Length;
			bool needsUpdating = (newValuesCount != originalData.Length); // Can't be the same, if they have different counts.
			if (!needsUpdating)
			{
				// Even with the same counts, the order may not be the same.
				// So check for change of order.
				for (int i = 0; i < newValuesCount; ++i)
				{
					if (newValues[i] != originalData[i])
					{
						needsUpdating = true;
						break;
					}
				}
			}
			if (needsUpdating)
			{
				m_cache.ReplaceReferenceProperty(m_hvo, (int)LangProjectTags.kflidCurPronunWss, 0, originalData.Length, ref newValues);
			}
		}

		/// <summary>
		/// Return the list of writing system for the language represented by the writing system of
		/// the given reversal index entry (or reversal index).
		/// </summary>
		/// <param name="hvoReversalIndexEntry">id of a reversal index entry or reversal index</param>
		/// <param name="forceIncludeEnglish">True, if it is to include English, no matter what.</param>
		/// <returns></returns>
		public int[] GetReversalIndexWritingSystems(int hvoReversalIndexEntry, bool forceIncludeEnglish)
		{
			// This method actually handles reversal index, reversal index entry, other classes, and even hvo 0.
			int wsPrimary = LangProject.GetReversalIndexEntryWritingSystem(m_cache,
					hvoReversalIndexEntry, Cache.DefaultAnalWs);
			string sIcuPrimary = m_cache.LanguageWritingSystemFactoryAccessor.GetStrFromWs(wsPrimary);
			int ich = sIcuPrimary.IndexOf('_');		// trim to only language id portion of ICULocale.
			if (ich >= 0)
				sIcuPrimary = sIcuPrimary.Substring(0, ich);
			List<int> rgwsWanted = new List<int>(4);
			rgwsWanted.Add(wsPrimary);
			foreach (int ws in m_cache.LangProject.CurAnalysisWssRS.HvoArray)
			{
				if (ws == wsPrimary)
					continue;
				string sIcu = m_cache.LanguageWritingSystemFactoryAccessor.GetStrFromWs(ws);
				ich = sIcu.IndexOf('_');	// trim to only language id portion of ICULocale.
				if (ich >= 0)
					sIcu = sIcu.Substring(0, ich);
				if (sIcu == sIcuPrimary)
					rgwsWanted.Add(ws);
			}

			if (forceIncludeEnglish)
			{
				int wsEnglish = m_cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr("en");
				if (!rgwsWanted.Contains(wsEnglish))
					rgwsWanted.Add(wsEnglish);
			}

			int[] rgws = new int[rgwsWanted.Count];
			for (int i = 0; i < rgwsWanted.Count; ++i)
				rgws[i] = (int)rgwsWanted[i];
			return rgws;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieves the UI name of a writing system, given its ICU locale.
		/// </summary>
		/// <param name="icuLocale">The ICU locale of the desired WS</param>
		/// <returns>the UI name of a writing system, or null if not found</returns>
		/// ------------------------------------------------------------------------------------
		public string GetWritingSystemName(string icuLocale)
		{
			foreach (NamedWritingSystem nws in GetAllNamedWritingSystems())
			{
				if (icuLocale == nws.IcuLocale)
				{
					return nws.Name;
				}
			}
			return null;
		}

		#region template creation
		internal CmPossibility CreateColumn(CmPossibility parent, XmlNode spec)
		{
			CmPossibility result = (CmPossibility)parent.SubPossibilitiesOS.Append(new CmPossibility());
			SetNameAndChildColumns(result, spec);
			return result;
		}

		private void SetNameAndChildColumns(CmPossibility parent, XmlNode spec)
		{
			parent.Name.AnalysisDefaultWritingSystem = XmlUtils.GetManditoryAttributeValue(spec, "name");
			foreach (XmlNode child in spec.ChildNodes)
			{
				if (child.Name == "column")
					CreateColumn(parent, child);
			}
		}

		/// <summary>
		/// Create a CmPossibility based on an XML specification of a constituent chart template.
		/// See CreateDefaultTemplate for an example.
		/// </summary>
		/// <param name="spec"></param>
		/// <returns></returns>
		public CmPossibility CreateChartTemplate(XmlNode spec)
		{
			// Make sure we have the containing objects; if not create them.
			IDsDiscourseData dData = m_cache.LangProject.DiscourseDataOA;
			if (dData == null)
			{
				dData = new DsDiscourseData();
				m_cache.LangProject.DiscourseDataOA = dData;
			}
			// Also make sure it has a templates list
			ICmPossibilityList templates = dData.ConstChartTemplOA;
			if (templates == null)
			{
				templates = new CmPossibilityList();
				dData.ConstChartTemplOA = templates;
			}
			CmPossibility template = new CmPossibility();
			m_cache.LangProject.DiscourseDataOA.ConstChartTemplOA.PossibilitiesOS.Append(template);
			SetNameAndChildColumns(template, spec);
			return template;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the default Constituent Chart template (creating it and any superstructure to hold it as needed).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual ICmPossibility GetDefaultChartTemplate()
		{
			if (m_cache.LangProject.DiscourseDataOA == null
				|| m_cache.LangProject.DiscourseDataOA.ConstChartTemplOA == null
				|| m_cache.LangProject.DiscourseDataOA.ConstChartTemplOA.PossibilitiesOS.Count == 0)
			{
				CreateDefaultTemplate();
			}
			return m_cache.LangProject.DiscourseDataOA.ConstChartTemplOA.PossibilitiesOS[0];
		}

		/// <summary>
		/// Create a default template. What happened to the version in the Blank Database?
		/// </summary>
		/// <returns></returns>
		private CmPossibility CreateDefaultTemplate()
		{
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(
				"<template name=\"default If you ever get this report it. There is a problem.\">"
				+ "<column name=\"prenuclear\">"
				+ "<column name=\"prenuc1\"/>"
				+ "<column name=\"prenuc2\"/>"
				+ "</column>"
				+ "<column name=\"nucleus\">"
				+ "<column name=\"subject\"/>"
				+ "<column name=\"verb\"/>"
				+ "<column name=\"object\"/>"
				+ "</column>"
				+ "<column name=\"postnuc\"/>"
				+ "</template>");
			return CreateChartTemplate(doc.DocumentElement);
		}
		#endregion template creation

		#region DefaultChartMarkers

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the default list of Constituent Chart markers (creating it and any superstructure
		/// to hold it as needed).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ICmPossibility GetDefaultChartMarkers()
		{
			if (m_cache.LangProject.DiscourseDataOA == null
				|| m_cache.LangProject.DiscourseDataOA.ChartMarkersOA == null
				|| m_cache.LangProject.DiscourseDataOA.ChartMarkersOA.PossibilitiesOS.Count == 0)
			{
				MakeDefaultChartMarkers();
			}
			return m_cache.LangProject.DiscourseDataOA.ChartMarkersOA.PossibilitiesOS[0];
		}

		/// <summary>
		/// Create a default set of markers. What happened to the version in the Blank Database?
		/// </summary>
		/// <returns></returns>
		private ICmPossibilityList MakeDefaultChartMarkers()
		{
			string xml =
				"<list>"
				+ " <item name=\"Group1 If you ever get this report it. There is a problem.\" abbr=\"G1\">"
					+ " <item name=\"Group1.1\" abbr=\"G1.1\">"
						+ " <item name=\"Item1\" abbr=\"I1\"/>"
					+ " </item>"
				+ " </item>"
				+ " <item name=\"Group2\" abbr=\"G2\">"
					+ " <item name=\"Item2\" abbr=\"I2\"/>"
					+ " <item name=\"Item3\" abbr=\"I3\"/>"
				+ " </item>"
				+ "</list>";
			return MakeChartMarkers(xml);
		}

		/// <summary>
		/// Create a CmPossibilityList based on a string of XML specifying Constituent Chart markers.
		/// See MakeDefaultChartMarkers for an example.
		/// </summary>
		/// <param name="xml"></param>
		/// <returns></returns>
		public ICmPossibilityList MakeChartMarkers(string xml)
		{
			ICmPossibilityList result = m_cache.LangProject.DiscourseDataOA.ChartMarkersOA = new CmPossibilityList();
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(xml);
			MakeListXml(result, doc.DocumentElement);
			return result;
		}

		private void MakeListXml(ICmPossibilityList list, XmlElement root)
		{
			foreach (XmlNode item in root)
			{
				ICmPossibility poss = list.PossibilitiesOS.Append(new CmPossibility());
				InitItem(item, poss);

			}
		}

		private static void InitItem(XmlNode item, ICmPossibility poss)
		{
			poss.Name.AnalysisDefaultWritingSystem = XmlUtils.GetManditoryAttributeValue(item, "name");
			string abbr = XmlUtils.GetOptionalAttributeValue(item, "abbr");
			if (String.IsNullOrEmpty(abbr))
				abbr = poss.Name.AnalysisDefaultWritingSystem;
			poss.Abbreviation.AnalysisDefaultWritingSystem = abbr;
			foreach (XmlNode subItem in item.ChildNodes)
			{
				ICmPossibility poss2 = poss.SubPossibilitiesOS.Append(new CmPossibility());
				InitItem(subItem, poss2);
			}
		}

		#endregion // DefaultChartMarkers

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return the set of all parts of speech, whether owned directly by the language
		/// project or owned as child parts of speech.
		/// <remarks>Used when dumping the database for the automated Parser</remarks>
		/// </summary>
		/// <remarks> Note that you may not find this method in source code,
		/// since it will be used from XML template and accessed dynamically.</remarks>
		/// ------------------------------------------------------------------------------------
		public FdoObjectSet<IPartOfSpeech> AllPartsOfSpeech
		{
			get
			{
				//			{
				//				string sQry = "select obj, txt from MoForm_Form "; // and ws???
				//
				//
				//				IDbColSpec dcs = DbColSpecClass.Create();
				//				dcs.Push((int)DbColType.koctBaseId, 0, 0, 0);
				//				dcs.Push((int)DbColType.koctMltAlt, 1, (int) BaseMoForm.MoFormTags.kflidForm , m_cache.LangProject.DefaultVernacularWritingSystem);
				//
				//				m_cache.LoadData(sQry, dcs, this.Hvo);
				//				System.Runtime.InteropServices.Marshal.ReleaseComObject(dcs);
				//
				//			}

				string query = "select id, class$ from PartOfSpeech_";

				//Enhance: we are using the form of this constructor that allows us to say "just load all of type"..
				//unfortunately, at the moment, this means we cannot specify the fact that we have only one type of object we are working with
				//since there are not that many, parts of speech, it's not worth messing with the code at this time.
				//But if this was straightened out for some other purpose, it could be used here.
				return new FdoObjectSet<IPartOfSpeech>(m_cache, query, false, true);
			}
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get XML representing every single feature structure in the database.
		/// </summary>
		/// <returns>a string consisting of 0 or more XML elements</returns>
		/// ------------------------------------------------------------------------------------
		public string PatrXmlForAllFeatureStructures
		{
			get
			{
				/*note that I am using the SQL server connections here rather than the preferred
				 * odbc 1 because I cannot find any code that gets a string parameter
				 * back from that kind of command.  And I know from experience that, if no one has ever
				 * tried to do this, then it typically is very hard or impossible.
				 * so, this could be enhanced by someone who knows what they're doing, and remove the
				 * dependency on SQL server.
				 * (John Hatton)
				 */
				SqlConnection connection = null;
				try
				{
					connection = new SqlConnection(string.Format("Server={0};database={1};user id=fwdeveloper;password=careful; Pooling=false;",
						m_cache.ServerName, m_cache.DatabaseName));
					connection.Open();
					SqlCommand command = connection.CreateCommand();
					command.CommandType = CommandType.Text;
					command.CommandText = "EXEC PATRString_FsFeatStruc 1";
					string result = (string)command.ExecuteScalar();
					if (result == null)
						return "";
					return result;
				}
				catch (Exception error)
				{
					Debug.Assert(false, error.Message);
					throw;
				}
				finally
				{
					if (connection != null)
					{
						connection.Close();
						connection.Dispose();
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the Key Terms list
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ICmPossibilityList KeyTermsList
		{
			get
			{
				foreach (ICmPossibilityList list in CheckListsOC)
				{
					if (list.Guid == kguidChkKeyTermsList)
						return list;
				}
				ICmPossibilityList keyTermList = CheckListsOC.Add(new CmPossibilityList());
				Cache.SetGuidProperty(keyTermList.Hvo, (int)CmObjectFields.kflidCmObject_Guid,
					kguidChkKeyTermsList);
				return keyTermList;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find the feature type for exception features.  Look in the Feature System.
		/// </summary>
		/// <returns>The feature type for exception features or null if there are none</returns>
		/// ------------------------------------------------------------------------------------
		public IFsFeatStrucType ExceptionFeatureType
		{
			get
			{
				IFsFeatStrucType exceps = null;
				IFsFeatureSystem fsys = MsFeatureSystemOA;
				if (fsys != null)
				{
					foreach (IFsFeatStrucType type in fsys.TypesOC)
						if (type.Name.AnalysisDefaultWritingSystem == "Exception Features") // TODO: this needs to be made geneal
						{
							exceps = type;
							break;
						}
				}
				return exceps;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a list of CmAnnotationDefns that belong to Scripture
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FdoOwningSequence<ICmPossibility> ScriptureAnnotationDfns
		{
			get
			{
				foreach (ICmAnnotationDefn dfn in AnnotationDefsOA.PossibilitiesOS)
				{
					if (m_cache.GetGuidFromId(dfn.Hvo) == kguidAnnNote)
						return dfn.SubPossibilitiesOS;
				}
				throw new Exception("Scripture annotation definitions are missing from project!");
			}
		}

		/// <summary>
		///  Virtual field for texts that can be interlinearized.
		/// </summary>
		/// <param name="cache"></param>
		/// <returns></returns>
		public static int InterlinearTextsFlid(FdoCache cache)
		{
			return BaseVirtualHandler.GetInstalledHandlerTag(cache, "LangProject", "InterlinearTexts");
		}

		/// <summary>
		/// Virtual list of texts that can be interlinearized.
		/// </summary>
		public List<int> InterlinearTexts
		{
			// load this list manually.
			get { return new List<int>(); }
			set
			{
				// not the standard way of setting this, property, but a test uses it.
				Cache.VwCacheDaAccessor.CacheVecProp(this.Hvo, InterlinearTextsFlid(Cache), value.ToArray(), value.Count);
			}
		}

		/// <summary>
		/// Rename the given project.
		/// </summary>
		/// <param name="sOldName"></param>
		/// <param name="sNewName"></param>
		/// <returns></returns>
		public static bool RenameProject(string sOldName, string sNewName)
		{
			try
			{
				IDisconnectDb dscdb = FwDisconnectClass.Create();
				string sReason = Strings.ksRenamingDatabase;
				string sExternal = String.Format(Strings.ksRenamingProject,
					sOldName, System.Windows.Forms.SystemInformation.ComputerName, sNewName);
				dscdb.Init(sOldName, MiscUtils.LocalServerName, sReason, sExternal, false, null, 0);
				dscdb.ForceDisconnectAll();
				IDbAdmin dba = DbAdminClass.Create();
				dba.SimplyRenameDatabase(sOldName, sNewName);
				return true;
			}
			catch
			{
				System.OperatingSystem osInfo = System.Environment.OSVersion;
				string caption = ResourceHelper.GetResourceString("kstidProjectRenameFailedCaption");
				string message = ResourceHelper.GetResourceString("kstidProjectRenameFailedMessage");
				if (osInfo.Version.Major >= 6)
					message += ResourceHelper.GetResourceString("kstidProjectRenameFailedVistaInfo");
				MessageBox.Show(message, caption, MessageBoxButtons.OK);
				return false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return ExtLinkRootDir if set, or set it to FWDataDirectory and return that value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string ExternalLinkRootDir
		{
			get
			{
				if (String.IsNullOrEmpty(this.ExtLinkRootDir))
				{
					using (SuppressSubTasks supressActionHandler = new SuppressSubTasks(Cache, true))
					{
						this.ExtLinkRootDir = DirectoryFinder.FWDataDirectory;
					}
				}
				return this.ExtLinkRootDir;
			}
		}
	}
}
