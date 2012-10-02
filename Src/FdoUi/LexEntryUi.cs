using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using System.Xml;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Utils;
using SIL.Utils;
using SIL.FieldWorks.LexText.Controls;
using XCore;

namespace SIL.FieldWorks.FdoUi
{
	/// <summary>
	/// User-interface behavior for LexEntries.
	/// </summary>
	public class LexEntryUi : CmObjectUi
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make one. The argument should really be a LexEntry.
		/// </summary>
		/// <param name="obj"></param>
		/// ------------------------------------------------------------------------------------
		public LexEntryUi(ICmObject obj)
			: base(obj)
		{
			Debug.Assert(obj is ILexEntry);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create default valued entry.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal LexEntryUi()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override IVwViewConstructor VernVc
		{
			get
			{
				CheckDisposed();
				return new LexEntryVc(m_cache);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Given an object id, a (string-valued) property ID, and a range of characters,
		/// return the LexEntry that is the best guess as to a useful LE to show to
		/// provide information about that wordform.
		///
		/// Note: the interface takes this form because eventually we may want to query to
		/// see whether the text has been analyzed and we have a known morpheme breakdown
		/// for this wordform. Otherwise, at present we could just pass the text.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvoSrc"></param>
		/// <param name="tagSrc"></param>
		/// <param name="ichMin"></param>
		/// <param name="ichLim"></param>
		/// <returns>LexEntry or null.</returns>
		/// ------------------------------------------------------------------------------------
		public static LexEntryUi FindEntryForWordform(FdoCache cache, int hvoSrc, int tagSrc,
			int ichMin, int ichLim)
		{
			ITsString tssContext = cache.MainCacheAccessor.get_StringProp(hvoSrc, tagSrc);
			if (tssContext == null)
				return null;
			ITsString tssWf = tssContext.GetSubstring(ichMin, ichLim);
			return FindEntryForWordform(cache, tssWf);
		}

		/// <summary>
		/// Find the list of LexEntry objects which conceivably match the given wordform.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="tssWf"></param>
		/// <returns></returns>
		public static List<int> FindEntriesForWordform(FdoCache cache, ITsString tssWf)
		{
			if (tssWf == null)
				return new List<int>();

			string wf = tssWf.Text;
			if (wf == null || wf == string.Empty)
				return new List<int>();
			int wsVern = StringUtils.GetWsAtOffset(tssWf, 0);
			// Adjust the wf string to escape any single quotes that may be present,
			// else the SQL query will cause a crash (TE-7033):
			string wfSqlSafe = wf.Replace("'", "''");
			// Check for Wordform, Lexeme Form, Alternate Form, and Citation Form.
			string sql = String.Format("SELECT le.Id, le.HomographNumber" +
				" FROM WfiWordform_Form wwf" +
				" JOIN WfiWordform_Analyses wwa ON wwa.Src=wwf.Obj" +
				" JOIN WfiAnalysis_MorphBundles wamb ON wamb.Src=wwa.Dst" +
				" JOIN WfiMorphBundle wmb ON wmb.Id=wamb.Dst" +
				" JOIN MoStemMsa msm ON msm.Id=wmb.Msa" +
				" JOIN CmObject co ON co.Id=msm.Id" +
				" JOIN LexEntry le ON le.Id=co.Owner$" +
				" WHERE wwf.Ws={0} AND wwf.Txt=N'{1}'" +
				" UNION" +
				" SELECT le.Id, le.HomographNumber" +
				" FROM MoForm_Form mff" +
				" JOIN CmObject co ON co.Id=mff.Obj" +
				" JOIN LexEntry le ON le.Id=co.Owner$" +
				" WHERE mff.Ws={0} AND mff.Txt=N'{1}'" +
				" UNION" +
				" SELECT le.Id, le.HomographNumber" +
				" FROM LexEntry le" +
				" JOIN LexEntry_CitationForm lcf ON lcf.Obj=le.Id AND lcf.Ws={0} AND lcf.Txt=N'{1}'" +
				" ORDER BY le.HomographNumber", wsVern, wfSqlSafe);
			return DbOps.ReadIntsFromCommand(cache, sql, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find wordform given a cache and the string.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="wf"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static LexEntryUi FindEntryForWordform(FdoCache cache, ITsString tssWf)
		{
			if (tssWf == null)
				return null;

			string wf = tssWf.Text;
			if (wf == null || wf == string.Empty)
				return null;

			int wsVern = StringUtils.GetWsAtOffset(tssWf, 0);
			string sql;
			List<int> entries;
			// Check for Lexeme form.
			sql = string.Format("SELECT le.Id, le.HomographNumber FROM LexEntry le" +
				" JOIN LexEntry_LexemeForm llf ON llf.Src=le.Id" +
				" JOIN MoForm_Form mf ON mf.Obj=llf.Dst AND" +
				" mf.Ws={0} AND mf.Txt=?" +
				" ORDER BY le.HomographNumber", wsVern);
			entries = DbOps.ReadIntsFromCommand(cache, sql, wf);
			if (entries.Count == 0)
			{
				// Check for Citation form.
				sql = string.Format("SELECT le.Id, le.HomographNumber FROM LexEntry le" +
					" JOIN LexEntry_CitationForm lcf ON lcf.Obj=le.Id AND" +
					" lcf.Ws={0} AND lcf.Txt=?" +
					" ORDER BY le.HomographNumber", wsVern);
				entries = DbOps.ReadIntsFromCommand(cache, sql, wf);
			}
			if (entries.Count == 0)
			{
				// Check for Alternate forms.
				sql = string.Format("SELECT le.Id, le.HomographNumber FROM LexEntry le" +
					" JOIN LexEntry_AlternateForms laf on laf.Src=le.Id" +
					" JOIN MoForm_Form mf ON mf.Obj=laf.Dst AND " +
					" mf.Ws={0} AND mf.Txt=?" +
					" ORDER BY le.HomographNumber, laf.Ord", wsVern);
				entries = DbOps.ReadIntsFromCommand(cache, sql, wf);
			}
			int hvoLe = 0;
			if (entries.Count == 0)
			{
				// Look for the most commonly used analysis of the wordform.
				// Enhance JohnT: first look to see whether the paragraph is analyzed
				// and we know exactly which analysis to use.
				string sql2 = "declare @wfid int"
					+ " select @wfid = obj from WfiWordform_Form where Txt=?"
					+ " select AnalysisId from dbo.fnGetDefaultAnalysisGloss(@wfid)";
				int hvoAnn;
				DbOps.ReadOneIntFromCommand(cache, sql2, wf, out hvoAnn);
				if (hvoAnn == 0)
					return null;
				// Pick an arbitrary stem morpheme lex entry from the most common analysis.
				string sql3 = string.Format("select le.id from WfiMorphBundle wmb"
					+ " join CmObject co on co.owner$ = {0} and co.id = wmb.id"
					+ " join MoStemMsa msm on msm.id = wmb.msa"
					+ " join CmObject comsm on comsm.id = msm.id"
					+ " join LexEntry le on le.id = comsm.owner$", hvoAnn);
				DbOps.ReadOneIntFromCommand(cache, sql3, null, out hvoLe);
				if (hvoLe == 0)
					return null;
			}
			else
			{
				// Enhance JohnT: should we do something about multiple homographs?
				hvoLe = entries[0];
			}
			return new LexEntryUi(new LexEntry(cache, hvoLe));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create the mediator if it doesn't already exist.  Ensure that the string table in
		/// the mediator is loaded from the Flex string table.
		/// </summary>
		/// <param name="mediator"></param>
		/// <param name="fRestoreStringTable">output flag that we should restore the original string table</param>
		/// <param name="stOrig">output is the original string table</param>
		/// ------------------------------------------------------------------------------------
		protected static Mediator EnsureValidMediator(Mediator mediator,
			out bool fRestoreStringTable, out StringTable stOrig)
		{
			if (mediator == null)
			{
				mediator = new Mediator();
				fRestoreStringTable = false;
				stOrig = null;
			}
			else
			{
				try
				{
					stOrig = mediator.StringTbl;
					// Check whether this is the Flex string table: look for a lexicon type
					// string and compare the value with what is produced when it's not found.
					string s = stOrig.GetString("MoCompoundRule-Plural", "AlternativeTitles");
					fRestoreStringTable = (s == "*MoCompoundRule-Plural*");
				}
				catch
				{
					stOrig = null;
					fRestoreStringTable = true;
				}
			}
			if (fRestoreStringTable || stOrig == null)
			{
				string dir = DirectoryFinder.GetFWCodeSubDirectory("Language Explorer\\Configuration");
				mediator.StringTbl = new SIL.Utils.StringTable(dir);
			}
			return mediator;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvoSrc"></param>
		/// <param name="tagSrc"></param>
		/// <param name="wsSrc"></param>
		/// <param name="ichMin"></param>
		/// <param name="ichLim"></param>
		/// <param name="owner"></param>
		/// <param name="mediator"></param>
		/// <param name="helpProvider"></param>
		/// <param name="helpFileKey">string key to get the help file name</param>
		/// ------------------------------------------------------------------------------------
		public static void DisplayOrCreateEntry(FdoCache cache, int hvoSrc, int tagSrc, int wsSrc,
			int ichMin, int ichLim, IWin32Window owner, Mediator mediator,
			IHelpTopicProvider helpProvider, string helpFileKey)
		{
			ITsString tssContext = cache.MainCacheAccessor.get_StringProp(hvoSrc, tagSrc);
			if (tssContext == null)
				return;
			string seg = tssContext.Text;
			// If the string is empty, it might be because it's multilingual.  Try that alternative.
			// (See TE-6374.)
			if (seg == null && wsSrc != 0)
			{
				tssContext = cache.MainCacheAccessor.get_MultiStringAlt(hvoSrc, tagSrc, wsSrc);
				if (tssContext != null)
					seg = tssContext.Text;
			}
			ITsString tssWf = null;
			if (seg != null)
				tssWf = tssContext.GetSubstring(ichMin, ichLim);
			if (tssWf == null || tssWf.Length == 0)
				return;
			DisplayEntries(cache, owner, mediator, helpProvider, helpFileKey, tssWf);
		}

		internal static void DisplayEntry(FdoCache cache, IWin32Window owner, Mediator mediatorIn,
			IHelpTopicProvider helpProvider, string helpFileKey, ITsString tssWfIn)
		{
			ITsString tssWf = tssWfIn;
			LexEntryUi leui = LexEntryUi.FindEntryForWordform(cache, tssWf);

			// if we do not find a match for the word then try converting it to lowercase and see if there
			// is an entry in the lexicon for the Wordform in lowercase. This is needed for occurences of
			// words which are capitalized at the beginning of sentences.  LT-7444 RickM
			if (leui == null)
			{
				//We need to be careful when converting to lowercase therefore use Icu.ToLower()
				//get the WS of the tsString
				int wsWf = StringUtils.GetWsAtOffset(tssWf, 0);
				//use that to get the locale for the WS, which is used for
				string wsLocale = cache.LanguageWritingSystemFactoryAccessor.get_EngineOrNull(wsWf).IcuLocale;
				string sLower = Icu.ToLower(tssWf.Text, wsLocale);
				ITsTextProps ttp = tssWf.get_PropertiesAt(0);
				ITsStrFactory tsf = TsStrFactoryClass.Create();
				tssWf = tsf.MakeStringWithPropsRgch(sLower, sLower.Length, ttp);
				leui = LexEntryUi.FindEntryForWordform(cache, tssWf);
			}

			// Ensure that we have a valid mediator with the proper string table.
			bool fRestore = false;
			StringTable stOrig = null;
			Mediator mediator = EnsureValidMediator(mediatorIn, out fRestore, out stOrig);
			FdoCache cache2 = (FdoCache)mediator.PropertyTable.GetValue("cache");
			if (cache2 != cache)
				mediator.PropertyTable.SetProperty("cache", cache);
			EnsureWindowConfiguration(mediator);
			EnsureFlexVirtuals(cache, mediator);
			IVwStylesheet styleSheet = GetStyleSheet(cache, mediator);
			if (leui == null)
			{
				int hvoLe = ShowFindEntryDialog(cache, mediator, tssWf, owner);
				if (hvoLe == 0)
				{
					// Restore the original string table in the mediator if needed.
					if (fRestore)
						mediator.StringTbl = stOrig;
					return;
				}
				leui = new LexEntryUi(new LexEntry(cache, hvoLe));
			}
			if (mediator != null)
				leui.Mediator = mediator;
			leui.ShowSummaryDialog(owner, tssWf, helpProvider, helpFileKey, styleSheet);
			// Restore the original string table in the mediator if needed.
			if (fRestore)
				mediator.StringTbl = stOrig;
		}

		internal static void DisplayEntries(FdoCache cache, IWin32Window owner, Mediator mediatorIn,
			IHelpTopicProvider helpProvider, string helpFileKey, ITsString tssWfIn)
		{
			ITsString tssWf = tssWfIn;
			List<int> rghvo = LexEntryUi.FindEntriesForWordform(cache, tssWf);

			// if we do not find a match for the word then try converting it to lowercase and see if there
			// is an entry in the lexicon for the Wordform in lowercase. This is needed for occurences of
			// words which are capitalized at the beginning of sentences.  LT-7444 RickM
			if (rghvo == null || rghvo.Count == 0)
			{
				//We need to be careful when converting to lowercase therefore use Icu.ToLower()
				//get the WS of the tsString
				int wsWf = StringUtils.GetWsAtOffset(tssWf, 0);
				//use that to get the locale for the WS, which is used for
				string wsLocale = cache.LanguageWritingSystemFactoryAccessor.get_EngineOrNull(wsWf).IcuLocale;
				string sLower = Icu.ToLower(tssWf.Text, wsLocale);
				ITsTextProps ttp = tssWf.get_PropertiesAt(0);
				ITsStrFactory tsf = TsStrFactoryClass.Create();
				tssWf = tsf.MakeStringWithPropsRgch(sLower, sLower.Length, ttp);
				rghvo = LexEntryUi.FindEntriesForWordform(cache, tssWf);
			}

			StringTable stOrig;
			Mediator mediator;
			IVwStylesheet styleSheet;
			bool fRestore = EnsureFlexTypeSetup(cache, mediatorIn, out stOrig, out mediator, out styleSheet);
			if (rghvo == null || rghvo.Count == 0)
			{
				int hvoLe = ShowFindEntryDialog(cache, mediator, tssWf, owner);
				if (hvoLe == 0)
				{
					// Restore the original string table in the mediator if needed.
					if (fRestore)
						mediator.StringTbl = stOrig;
					return;
				}
				rghvo = new List<int>(1);
				rghvo.Add(hvoLe);
			}
			using (SummaryDialogForm form =
				new SummaryDialogForm(rghvo, tssWf, helpProvider, helpFileKey, styleSheet, cache, mediator))
			{
				form.ShowDialog(owner);
				if (form.ShouldLink)
					form.LinkToLexicon();
			}
			// Restore the original string table in the mediator if needed.
			if (fRestore)
				mediator.StringTbl = stOrig;
		}

		private static bool EnsureFlexTypeSetup(FdoCache cache, Mediator mediatorIn, out StringTable stOrig, out Mediator mediator, out IVwStylesheet styleSheet)
		{
			// Ensure that we have a valid mediator with the proper string table.
			bool fRestore = false;
			stOrig = null;
			mediator = EnsureValidMediator(mediatorIn, out fRestore, out stOrig);
			FdoCache cache2 = (FdoCache)mediator.PropertyTable.GetValue("cache");
			if (cache2 != cache)
				mediator.PropertyTable.SetProperty("cache", cache);
			EnsureWindowConfiguration(mediator);
			EnsureFlexVirtuals(cache, mediator);
			styleSheet = GetStyleSheet(cache, mediator);
			return fRestore;
		}

		/// <summary>
		/// Determine a stylesheet from a mediator, or create a new one. Currently this is done
		/// by looking for the main window and seeing whether it has a StyleSheet property that
		/// returns one. (We use reflection because the relevant classes are in DLLs we can't
		/// reference.)
		/// </summary>
		/// <param name="mediator"></param>
		/// <returns></returns>
		private static IVwStylesheet StyleSheetFromMediator(Mediator mediator)
		{
			if (mediator == null)
				return null;
			Form mainWindow = mediator.PropertyTable.GetValue("window") as Form;
			if (mainWindow == null)
				return null;
			System.Reflection.PropertyInfo pi = mainWindow.GetType().GetProperty("StyleSheet");
			if (pi == null)
				return null;
			return pi.GetValue(mainWindow, null) as IVwStylesheet;
		}

		private static IVwStylesheet GetStyleSheet(FdoCache cache, Mediator mediator)
		{
			IVwStylesheet vss = StyleSheetFromMediator(mediator);
			if (vss != null)
				return vss;
			// Get a style sheet for the Language Explorer, and store it in the
			// (new) mediator.
			FwStyleSheet styleSheet = new FwStyleSheet();
			styleSheet.Init(cache, cache.LangProject.LexDbOA.Hvo,
				(int)LexDb.LexDbTags.kflidStyles);
			mediator.PropertyTable.SetProperty("FwStyleSheet", styleSheet);
			mediator.PropertyTable.SetPropertyPersistence("FwStyleSheet", false);
			return styleSheet as IVwStylesheet;
		}

		private static void EnsureWindowConfiguration(Mediator mediator)
		{
			XmlNode xnWindow = (XmlNode)mediator.PropertyTable.GetValue("WindowConfiguration");
			if (xnWindow == null)
			{
				string configFile = DirectoryFinder.GetFWCodeFile("Language Explorer\\Configuration\\Main.xml");
				// This can be called from TE...in that case, we don't complain about missing include
				// files (true argument) but just trust that we put enough in the installer to make it work.
				XmlDocument configuration = XmlUtils.LoadConfigurationWithIncludes(configFile, true);
				XmlNode windowConfigurationNode = configuration.SelectSingleNode("window");
				mediator.PropertyTable.SetProperty("WindowConfiguration", windowConfigurationNode);
				mediator.PropertyTable.SetPropertyPersistence("WindowConfiguration", false);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Assuming the selection can be expanded to a word and a corresponding LexEntry can
		/// be found, show the related words dialog with the words related to the selected one.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="sel">The sel.</param>
		/// <param name="owner">The owner.</param>
		/// <param name="mediatorIn"></param>
		/// <param name="helpProvider"></param>
		/// <param name="helpFileKey"></param>
		/// ------------------------------------------------------------------------------------
		public static void DisplayRelatedEntries(FdoCache cache, IVwSelection sel, IWin32Window owner,
			Mediator mediatorIn, IHelpTopicProvider helpProvider, string helpFileKey)
		{
			if (sel == null)
				return;
			IVwSelection sel2 = sel.EndPoint(false);
			if (sel2 == null)
				return;
			IVwSelection sel3 = sel2.GrowToWord();
			if (sel3 == null)
				return;
			ITsString tss;
			int ichMin, ichLim, hvo, tag, ws;
			bool fAssocPrev;
			sel3.TextSelInfo(false, out tss, out ichMin, out fAssocPrev, out hvo, out tag, out ws);
			sel3.TextSelInfo(true, out tss, out ichLim, out fAssocPrev, out hvo, out tag, out ws);
			if (tss.Text == null)
				return;
			ITsString tssWf = tss.GetSubstring(ichMin, ichLim);
			LexEntryUi leui = FindEntryForWordform(cache, tssWf);
			// This doesn't work as well (unless we do a commit) because it may not see current typing.
			//LexEntryUi leui = LexEntryUi.FindEntryForWordform(cache, hvo, tag, ichMin, ichLim);
			if (leui == null)
			{
				if (tssWf != null && tssWf.Length > 0)
					RelatedWords.ShowNotInDictMessage(owner);
				return;
			}
			int hvoEntry = leui.Object.Hvo;
			int[] domains;
			int[] lexrels;
			IVwCacheDa cdaTemp;
			if (!RelatedWords.LoadDomainAndRelationInfo(cache, hvoEntry, out domains, out lexrels, out cdaTemp, owner))
				return;
			StringTable stOrig;
			Mediator mediator;
			IVwStylesheet styleSheet;
			bool fRestore = EnsureFlexTypeSetup(cache, mediatorIn, out stOrig, out mediator, out styleSheet);
			using (RelatedWords rw = new RelatedWords(cache, sel3, hvoEntry, domains, lexrels, cdaTemp, styleSheet))
			{
				rw.ShowDialog(owner);
			}
			if (fRestore)
				mediator.StringTbl = stOrig;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the flex config file.
		/// </summary>
		/// <value>The flex config file.</value>
		/// ------------------------------------------------------------------------------------
		public static string FlexConfigFile
		{
			get { return DirectoryFinder.GetFWCodeFile(@"Language Explorer\Configuration\Main.xml"); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// For dialogs which involve displaying lexical data, ensure that the virtual
		/// properties needed by the XMLViews based displays are installed. Also ensure that
		/// they aren't holding a pointer to a previous temporary cache which was disposed by a
		/// previous lookup.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="mediator">The mediator.</param>
		/// ------------------------------------------------------------------------------------
		public static void EnsureFlexVirtuals(FdoCache cache, Mediator mediator)
		{
			TSStringPropertyVirtualHandler vh =
				cache.GetVirtualProperty("LexEntry", "HeadWord") as TSStringPropertyVirtualHandler;
			if (vh == null || vh.Cache.IsDisposed)
			{
				// This can be called from TE...in that case, we don't complain about missing include
				// files (true argument) but just trust that we put enough in the installer to make it work.
				XmlDocument configuration = XmlUtils.LoadConfigurationWithIncludes(FlexConfigFile, true);
				XmlNode configNode = configuration.SelectSingleNode("window");
				BaseVirtualHandler.InstallVirtuals(configNode.SelectSingleNode("virtuals"), cache, true);
				if (mediator != null)
				{
					mediator.PropertyTable.SetProperty("WindowConfiguration", configNode);
					mediator.PropertyTable.SetPropertyPersistence("WindowConfiguration", false);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Launch the Find Entry dialog, and if one is created or selected return it.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="mediator">The mediator.</param>
		/// <param name="tssForm">The TSS form.</param>
		/// <param name="owner">The owner.</param>
		/// <returns>The HVO of the selected or created entry</returns>
		/// ------------------------------------------------------------------------------------
		internal static int ShowFindEntryDialog(FdoCache cache, Mediator mediator,
			ITsString tssForm, IWin32Window owner)
		{
			// Ensure that we have a valid mediator with the proper string table.
			bool fRestore = false;
			StringTable stOrig = null;
			mediator = EnsureValidMediator(mediator, out fRestore, out stOrig);
			EnsureFlexVirtuals(cache, mediator);
			using (GoDlg dlg = new GoDlg())
			{
				WindowParams wp = new WindowParams();
				wp.m_btnText = FdoUiStrings.ksShow;
				wp.m_title = FdoUiStrings.ksFindInDictionary;
				wp.m_label = FdoUiStrings.ksFind_;
				dlg.Owner = owner as Form;
				dlg.SetDlgInfo(cache, wp, mediator, tssForm);
				dlg.SetHelpTopic("khtpFindInDictionary");
				if (dlg.ShowDialog() == DialogResult.OK)
				{
					int entryId = dlg.SelectedID;
					Debug.Assert(entryId != 0);
					// Restore the original string table in the mediator if needed.
					if (fRestore)
						mediator.StringTbl = stOrig;
					return entryId;
				}
			}
			// Restore the original string table in the mediator if needed.
			if (fRestore)
				mediator.StringTbl = stOrig;
			return 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="owner"></param>
		/// <param name="tssWf"></param>
		/// <param name="helpProvider"></param>
		/// <param name="helpFileKey">string key to get the help file name</param>
		/// ------------------------------------------------------------------------------------
		public void ShowSummaryDialog(IWin32Window owner, ITsString tssWf,
			IHelpTopicProvider helpProvider, string helpFileKey, IVwStylesheet styleSheet)
		{
			CheckDisposed();

			EnsureFlexVirtuals(m_cache, null);
			using (SummaryDialogForm form =
					   new SummaryDialogForm(this, tssWf, helpProvider, helpFileKey, styleSheet))
			{
				form.ShowDialog(owner);
				if (form.ShouldLink)
					form.LinkToLexicon();
			}
		}

		protected override bool ShouldDisplayMenuForClass(uint specifiedClsid,
			XCore.UIItemDisplayProperties display)
		{
			if (LexEntry.kClassId == specifiedClsid)
				return true;
			else	// the else clause here may be superflous...
				return base.ShouldDisplayMenuForClass(specifiedClsid, display);
		}
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Override to support kfragHeadword with a properly live display of the headword.
	/// Also, the default of displaying the vernacular writing system can be overridden.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class LexEntryVc : CmVernObjectVc
	{
		const int kfragFormForm = 9543; // arbitrary.
		/// <summary>
		/// use with WfiMorphBundle to display the headword with variant info appended.
		/// </summary>
		public const int kfragEntryAndVariant = 9544;
		/// <summary>
		/// use with EntryRef to display the variant type info
		/// </summary>
		public const int kfragVariantTypes = 9545;

		int m_ws;
		int m_wsActual = 0;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="cache"></param>
		/// ------------------------------------------------------------------------------------
		public LexEntryVc(FdoCache cache)
			: base(cache)
		{
			m_ws = cache.DefaultVernWs;
		}

		public int WritingSystemCode
		{
			get
			{
				CheckDisposed();
				return m_ws;
			}
			set
			{
				CheckDisposed();
				m_ws = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Display a view of the LexEntry (or fragment thereof).
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="hvo"></param>
		/// <param name="frag"></param>
		/// ------------------------------------------------------------------------------------
		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			CheckDisposed();

			switch (frag)
			{
				case (int)VcFrags.kfragHeadWord:
					// This case should stay in sync with
					// LexEntry.LexemeFormMorphTypeAndHomographStatic
					vwenv.OpenParagraph();
					AddHeadwordWithHomograph(vwenv, hvo);
					vwenv.CloseParagraph();
					break;
				case (int)kfragEntryAndVariant:
					IWfiMorphBundle wfb = WfiMorphBundle.CreateFromDBObject(m_cache, hvo);
					int hvoMf = wfb.MorphRAHvo;
					int hvoLexEntry = m_cache.GetOwnerOfObject(hvoMf);
					// if morphbundle morph (entry) is in a variant relationship to the morph bundle sense
					// display its entry headword and variant type information (LT-4053)
					ILexEntryRef ler = null;
					ILexEntry variant = LexEntry.CreateFromDBObject(m_cache, hvoLexEntry);
					if ((variant as LexEntry).IsVariantOfSenseOrOwnerEntry(wfb.SenseRA, out ler))
					{
						// build Headword from sense's entry
						vwenv.OpenParagraph();
						vwenv.OpenInnerPile();
						vwenv.AddObj(wfb.SenseRA.EntryID, this, (int)VcFrags.kfragHeadWord);
						vwenv.CloseInnerPile();
						vwenv.OpenInnerPile();
						// now add variant type info
						vwenv.AddObj(ler.Hvo, this, kfragVariantTypes);
						vwenv.CloseInnerPile();
						vwenv.CloseParagraph();
						break;
					}
					else
					{
						// build Headword even though we aren't in a variant relationship.
						vwenv.AddObj(hvoLexEntry, this, (int)VcFrags.kfragHeadWord);
					}
					break;
				case kfragVariantTypes:
					ler = LexEntryRef.CreateFromDBObject(m_cache, hvo);
					bool fNeedInitialPlus = true;
					vwenv.OpenParagraph();
					foreach (ILexEntryType let in ler.VariantEntryTypesRS)
					{
						// just concatenate them together separated by comma.
						ITsString tssVariantTypeRevAbbr = let.ReverseAbbr.BestAnalysisAlternative;
						if (tssVariantTypeRevAbbr != null && tssVariantTypeRevAbbr.Length > 0)
						{
							if (fNeedInitialPlus)
								vwenv.AddString(StringUtils.MakeTss("+", m_cache.DefaultUserWs));
							else
								vwenv.AddString(StringUtils.MakeTss(",", m_cache.DefaultUserWs));
							vwenv.AddString(tssVariantTypeRevAbbr);
							fNeedInitialPlus = false;
						}
					}
					vwenv.CloseParagraph();
					break;
				case kfragFormForm: // form of MoForm
					vwenv.AddStringAltMember((int)MoForm.MoFormTags.kflidForm, m_wsActual, this);
					break;
				default:
					base.Display(vwenv, hvo, frag);
					break;
			}
		}

		private void AddHeadwordWithHomograph(IVwEnv vwenv, int hvo)
		{
			ISilDataAccess sda = vwenv.DataAccess;
			int hvoLf = sda.get_ObjectProp(hvo,
				(int)LexEntry.LexEntryTags.kflidLexemeForm);
			int hvoType = 0;
			if (hvoLf != 0)
			{
				hvoType = sda.get_ObjectProp(hvoLf,
					(int)MoForm.MoFormTags.kflidMorphType);
			}

			// If we have a type of morpheme, show the appropriate prefix that indicates it.
			// We want vernacular so it will match the point size of any aligned vernacular text.
			// (The danger is that the vernacular font doesn't have these characters...not sure what
			// we can do about that, but most do, and it looks awful in analysis if that is a
			// much different size from vernacular.)
			string sPrefix = null;
			if (hvoType != 0)
			{
				sPrefix = sda.get_UnicodeProp(hvoType, (int)MoMorphType.MoMorphTypeTags.kflidPrefix);
			}

			// LexEntry.ShortName1; basically tries for form of the lexeme form, then the citation form.
			bool fGotLabel = false;
			int wsActual = 0;
			if (hvoLf != 0)
			{
				// if we have a lexeme form and its label is non-empty, use it.
				if (TryMultiStringAlt(sda, hvoLf, (int)MoForm.MoFormTags.kflidForm, out wsActual))
				{
					m_wsActual = wsActual;
					fGotLabel = true;
					if (sPrefix != null)
						vwenv.AddString(StringUtils.MakeTss(sPrefix, wsActual));
					vwenv.AddObjProp((int)LexEntry.LexEntryTags.kflidLexemeForm, this, kfragFormForm);
				}
			}
			if (!fGotLabel)
			{
				// If we didn't get a useful form from the lexeme form try the citation form.
				if (TryMultiStringAlt(sda, hvo, (int)LexEntry.LexEntryTags.kflidCitationForm, out wsActual))
				{
					m_wsActual = wsActual;
					if (sPrefix != null)
						vwenv.AddString(StringUtils.MakeTss(sPrefix, wsActual));
					vwenv.AddStringAltMember((int)LexEntry.LexEntryTags.kflidCitationForm, wsActual, this);
					fGotLabel = true;
				}
			}
			if (!fGotLabel)
			{
				// If that fails just show two questions marks.
				if (sPrefix != null)
					vwenv.AddString(StringUtils.MakeTss(sPrefix, wsActual));
				vwenv.AddString(m_cache.MakeUserTss(FdoUiStrings.ksQuestions));	// was "??", not "???"
			}

			// If we have a lexeme form type show the appropriate postfix.
			if (hvoType != 0)
			{
				vwenv.AddString(StringUtils.MakeTss(
					sda.get_UnicodeProp(hvoType, (int)MoMorphType.MoMorphTypeTags.kflidPostfix), wsActual));
			}

			// Show homograph number if non-zero.
			int nHomograph = sda.get_IntProp(hvo,
				(int)LexEntry.LexEntryTags.kflidHomographNumber);
			vwenv.NoteDependency(new int[] { hvo }, new int[] { (int)LexEntry.LexEntryTags.kflidHomographNumber }, 1);
			if (nHomograph > 0)
			{
				// Use a string builder to embed the properties in with the TsString.
				// this allows our TsStringCollectorEnv to properly encode the superscript.
				// ideally, TsStringCollectorEnv could be made smarter to handle SetIntPropValues
				// since AppendTss treats the given Tss as atomic.
				ITsIncStrBldr tsBldr = TsIncStrBldrClass.Create();
				tsBldr.SetIntPropValues((int)FwTextPropType.ktptSuperscript,
					(int)FwTextPropVar.ktpvEnum,
					(int)FwSuperscriptVal.kssvSub);
				tsBldr.SetIntPropValues((int)FwTextPropType.ktptBold,
					(int)FwTextPropVar.ktpvEnum,
					(int)FwTextToggleVal.kttvForceOn);
				tsBldr.SetIntPropValues((int)FwTextPropType.ktptWs,
					(int)FwTextPropVar.ktpvDefault, m_cache.DefaultUserWs);
				tsBldr.Append(nHomograph.ToString());
				vwenv.AddString(tsBldr.GetString());
			}
		}

		private bool TryMultiStringAlt(ISilDataAccess sda, int hvo, int flid, out int wsActual)
		{
			MultiStringAccessor accessor = new MultiStringAccessor(m_cache, hvo, flid, "");
			return accessor.TryWs(m_ws, out wsActual);
		}
	}
}
