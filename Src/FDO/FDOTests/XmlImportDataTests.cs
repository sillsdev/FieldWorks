// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: XmlImportDataTests.cs
// Responsibility: mcconnel
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.Application.ApplicationServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.Test.TestUtils;
using SIL.Utils;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class XmlImportDataTests : BaseTest
	{
		private FdoCache m_cache;
		private DateTime m_now;

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Create the cache before each test
		/// </summary>
		///--------------------------------------------------------------------------------------
		[SetUp]
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="ThreadHelper is disposed in DestroyTestCache()")]
		public void CreateTestCache()
		{
			m_now = DateTime.Now;
			m_cache = FdoCache.CreateCacheWithNewBlankLangProj(
				new TestProjectId(FDOBackendProviderType.kMemoryOnly, "MemoryOnly.mem"), "en", "fr", "en", new ThreadHelper());
			IDataSetup dataSetup = m_cache.ServiceLocator.GetInstance<IDataSetup>();
			dataSetup.LoadDomain(BackendBulkLoadDomain.All);
			if (m_cache.LangProject != null)
			{
				if (m_cache.LangProject.DefaultVernacularWritingSystem == null)
				{
					List<IWritingSystem> rglgws = m_cache.ServiceLocator.WritingSystemManager.LocalWritingSystems.ToList();
					if (rglgws.Count > 0)
					{
						m_cache.DomainDataByFlid.BeginNonUndoableTask();
						m_cache.LangProject.DefaultVernacularWritingSystem = rglgws[rglgws.Count - 1];
						m_cache.DomainDataByFlid.EndNonUndoableTask();
					}
				}
			}
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Destroy the cache after each test
		/// </summary>
		///--------------------------------------------------------------------------------------
		[TearDown]
		public void DestroyTestCache()
		{
			if (m_cache != null)
			{
				m_cache.ThreadHelper.Dispose();
				m_cache.Dispose();
				m_cache = null;
			}
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method ImportData() on a single lexical entry.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void ImportData1()
		{
			DateTime dtLexOrig = m_cache.LangProject.LexDbOA.DateCreated;
			TimeSpan span = new TimeSpan(dtLexOrig.Ticks - m_now.Ticks);
			Assert.LessOrEqual(span.TotalMinutes, 1.0);		// should be only a second or two...
			XmlImportData xid = new XmlImportData(m_cache);
			using (var rdr = new StringReader(
				"<FwDatabase>" +
				"<LangProject>" +
				"<LexDb>" +
				"<LexDb id=\"I1D5042E0D192A7D\">" +
				"<DateCreated><Time val=\"1995-12-19 10:31:25.000\" /></DateCreated>" +
				"<DateModified><Time val=\"2007-06-25 15:54:13.000\" /></DateModified>" +
				"<Name>" +
				"<AUni ws=\"en\">English Lexical Database</AUni>" +
				"</Name>" +
				"<Entries>" +
				"<LexEntry id=\"I1D504E40D1AA689\">" +
				"<LexemeForm>" +
				"<MoStemAllomorph id=\"I1860116246B081E\">" +
				"<Form>" +
				"<AUni ws=\"x-ame\">adult</AUni>" +
				"</Form>" +
				"<MorphType><Link ws=\"en\" abbr=\"rt\" name=\"root\" /></MorphType>" +
				"</MoStemAllomorph>" +
				"</LexemeForm>" +
				"<DateCreated><Time val=\"1995-12-20 13:32:57.000\" /></DateCreated>" +
				"<DateModified><Time val=\"2008-05-12 16:36:46.000\" /></DateModified>" +
				"<MorphoSyntaxAnalyses>" +
				"<MoStemMsa id=\"I1860117246B081E\">" +
				"<PartOfSpeech><Link ws=\"en\" abbr=\"com n\" name=\"common noun\" /></PartOfSpeech>" +
				"</MoStemMsa>" +
				"</MorphoSyntaxAnalyses>" +
				"<Senses>" +
				"<LexSense id=\"I1D504E50D1AA689\">" +
				"<MorphoSyntaxAnalysis><Link target=\"I1860117246B081E\" class=\"MoMorphoSyntaxAnalysis\" /></MorphoSyntaxAnalysis>" +
				"<Gloss>" +
				"<AUni ws=\"en\">adult</AUni>" +
				"<AUni ws=\"fr\">adulte</AUni>" +
				"</Gloss>" +
				"<Definition>" +
				"<AStr ws=\"en\">" +
				"<Run ws=\"en\">a man or woman who is fully grown up</Run>" +
				"</AStr>" +
				"<AStr ws=\"fr\">" +
				"<Run ws=\"fr\">un homme ou une femme qui est parvenu au terme de la croissance</Run>" +
				"</AStr>" +
				"</Definition>" +
				"<AnthroCodes>" +
				"<Link ws=\"en\" abbr=\"156\" name=\"156 Social Personality\" />" +
				"<Link ws=\"en\" abbr=\"157\" name=\"157 Personality Traits\" />" +
				"<Link ws=\"en\" abbr=\"183\" name=\"183 Norms\" />" +
				"<Link ws=\"en\" abbr=\"828\" name=\"828 Ethnopsychology\" />" +
				"</AnthroCodes>" +
				"<SemanticDomains>" +
				"<Link ws=\"en\" abbr=\"4.3.1.3\" />" +
				"<Link ws=\"en\" abbr=\"4.3.1.3.1\" />" +
				"<Link ws=\"en\" abbr=\"2\" />" +
				"</SemanticDomains>" +
				"</LexSense>" +
				"</Senses>" +
				"</LexEntry>" +
				"</Entries>" +
				"</LexDb>" +
				"</LexDb>" +
				"</LangProject>" +
				"</FwDatabase>"))
			{
			StringBuilder sbLog = new StringBuilder();
			Assert.AreEqual(0, m_cache.LangProject.LexDbOA.Entries.Count(), "The lexicon starts out empty.");
			Assert.AreEqual(0, m_cache.LangProject.AnthroListOA.PossibilitiesOS.Count);
			Assert.AreEqual(0, m_cache.LangProject.SemanticDomainListOA.PossibilitiesOS.Count);
			Assert.AreEqual(0, m_cache.LangProject.PartsOfSpeechOA.PossibilitiesOS.Count);
			bool fOk = xid.ImportData(rdr, new StringWriter(sbLog), null);
			Assert.IsTrue(fOk, "Import of one lexical entry succeeded.");
			DateTime dtLexNew = m_cache.LangProject.LexDbOA.DateCreated;
			DateTime dt2 = new DateTime(1995, 12, 19, 10, 31, 25);
			Assert.AreEqual(dt2, dtLexNew, "LexDb DateCreated changed to reflect import value.");
			Assert.AreEqual(1, m_cache.LangProject.LexDbOA.Entries.Count(), "The import data had one entry.");
			Assert.AreEqual(0, m_cache.LangProject.LexDbOA.ReversalIndexesOC.Count);
			ILexEntry entry = m_cache.LangProject.LexDbOA.Entries.ToArray()[0];
			Assert.IsTrue(entry.LexemeFormOA is IMoStemAllomorph, "The entry is a stem.");
			IMultiUnicode mu = entry.LexemeFormOA.Form;
			Assert.AreEqual(1, mu.StringCount);
			int ws;
			ITsString tss = mu.GetStringFromIndex(0, out ws);
			Assert.AreEqual("adult", tss.Text);
			string sWs = m_cache.WritingSystemFactory.GetStrFromWs(ws);
			Assert.AreEqual("x-ame", sWs);
			Assert.AreEqual(1, entry.MorphoSyntaxAnalysesOC.Count, "The entry has only one MSA.");
			Assert.AreEqual(1, entry.SensesOS.Count, "The imported entry had one sense.");
			ILexSense sense = entry.SensesOS[0];
			mu = sense.Gloss;
			Assert.AreEqual(2, mu.StringCount, "The gloss is given in two writing systems/languages.");
			int wsEn = m_cache.WritingSystemFactory.GetWsFromStr("en");
			int wsFr = m_cache.WritingSystemFactory.GetWsFromStr("fr");
			tss = mu.get_String(wsEn);
			Assert.AreEqual("adult", tss.Text, "The English gloss imported okay.");
			tss = mu.get_String(wsFr);
			Assert.AreEqual("adulte", tss.Text, "The French gloss imported okay.");
			IMultiString ms = sense.Definition;
			Assert.AreEqual(2, ms.StringCount, "The definition is given in two writing systems/languages.");
			tss = ms.get_String(wsEn);
			ITsString tss0 = m_cache.TsStrFactory.MakeString("a man or woman who is fully grown up", wsEn);
			Assert.IsTrue(tss.Equals(tss0), "The English definition imported okay.");
			tss = ms.get_String(wsFr);
			tss0 = m_cache.TsStrFactory.MakeString("un homme ou une femme qui est parvenu au terme de la croissance", wsFr);
			Assert.IsTrue(tss.Equals(tss0), "The French definition imported okay.");
			Assert.AreEqual(4, sense.AnthroCodesRC.Count, "The sense has 4 anthopology category codes.");
			Assert.AreEqual(3, sense.SemanticDomainsRC.Count, "The sense is linked to 3 semantic domains.");
			Assert.IsTrue(sense.MorphoSyntaxAnalysisRA is IMoStemMsa, "The sense's MSA is a stem type MSA.");
			IMoStemMsa msa = sense.MorphoSyntaxAnalysisRA as IMoStemMsa;
			Assert.AreEqual(msa.Owner, sense.Owner, "The sense's MSA is owned by the sense's owner.");
			string pos = msa.PartOfSpeechRA.Name.get_String(wsEn).Text;
			Assert.AreEqual("common noun", pos, "The sense's part of speech is 'common noun'.");

			Assert.AreEqual(4, m_cache.LangProject.AnthroListOA.PossibilitiesOS.Count);
			ICmPossibility poss = m_cache.LangProject.AnthroListOA.PossibilitiesOS[0];
			Assert.IsTrue(poss is ICmAnthroItem);
			tss = poss.Name.get_String(wsEn);
			Assert.AreEqual("Social Personality", tss.Text);
			tss = poss.Abbreviation.get_String(wsEn);
			Assert.AreEqual("156", tss.Text);
			poss = m_cache.LangProject.AnthroListOA.PossibilitiesOS[1];
			Assert.IsTrue(poss is ICmAnthroItem);
			tss = poss.Name.get_String(wsEn);
			Assert.AreEqual("Personality Traits", tss.Text);
			tss = poss.Abbreviation.get_String(wsEn);
			Assert.AreEqual("157", tss.Text);
			poss = m_cache.LangProject.AnthroListOA.PossibilitiesOS[2];
			Assert.IsTrue(poss is ICmAnthroItem);
			tss = poss.Name.get_String(wsEn);
			Assert.AreEqual("Norms", tss.Text);
			tss = poss.Abbreviation.get_String(wsEn);
			Assert.AreEqual("183", tss.Text);
			poss = m_cache.LangProject.AnthroListOA.PossibilitiesOS[3];
			Assert.IsTrue(poss is ICmAnthroItem);
			tss = poss.Name.get_String(wsEn);
			Assert.AreEqual("Ethnopsychology", tss.Text);
			tss = poss.Abbreviation.get_String(wsEn);
			Assert.AreEqual("828", tss.Text);

			Assert.AreEqual(3, m_cache.LangProject.SemanticDomainListOA.PossibilitiesOS.Count);
			poss = m_cache.LangProject.SemanticDomainListOA.PossibilitiesOS[0];
			Assert.IsTrue(poss is ICmSemanticDomain);
			tss = poss.Name.get_String(wsEn);
			Assert.AreEqual("4.3.1.3", tss.Text);
			tss = poss.Abbreviation.get_String(wsEn);
			Assert.AreEqual("4.3.1.3", tss.Text);
			poss = m_cache.LangProject.SemanticDomainListOA.PossibilitiesOS[1];
			Assert.IsTrue(poss is ICmSemanticDomain);
			tss = poss.Name.get_String(wsEn);
			Assert.AreEqual("4.3.1.3.1", tss.Text);
			tss = poss.Abbreviation.get_String(wsEn);
			Assert.AreEqual("4.3.1.3.1", tss.Text);
			poss = m_cache.LangProject.SemanticDomainListOA.PossibilitiesOS[2];
			Assert.IsTrue(poss is ICmSemanticDomain);
			tss = poss.Name.get_String(wsEn);
			Assert.AreEqual("2", tss.Text);
			tss = poss.Abbreviation.get_String(wsEn);
			Assert.AreEqual("2", tss.Text);

			Assert.AreEqual(1, m_cache.LangProject.PartsOfSpeechOA.PossibilitiesOS.Count);
			poss = m_cache.LangProject.PartsOfSpeechOA.PossibilitiesOS[0];
			Assert.IsTrue(poss is IPartOfSpeech);
			tss = poss.Name.get_String(wsEn);
			Assert.AreEqual("common noun", tss.Text);
			tss = poss.Abbreviation.get_String(wsEn);
			Assert.AreEqual("com n", tss.Text);

			IWfiWordformRepository repoWfi = m_cache.ServiceLocator.GetInstance<IWfiWordformRepository>();
			Assert.AreEqual(0, repoWfi.Count);

			string sLog = sbLog.ToString();
			Assert.IsFalse(String.IsNullOrEmpty(sLog), "There should be some log information!");
			string[] rgsLog = sLog.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
			Assert.LessOrEqual(9, rgsLog.Length);
			Assert.AreEqual("data stream:0: Info: Creating new writing system for \"x-ame\".", rgsLog[0]);
			Assert.AreEqual("data stream:0: Info: Creating new item with ws=\"en\", abbr=\"com n\", and name=\"common noun\" in the Parts of Speech list.", rgsLog[1]);
			Assert.AreEqual("data stream:0: Info: Creating new item with ws=\"en\", abbr=\"156\", and name=\"Social Personality\" in the Anthropology Categories list.", rgsLog[2]);
			Assert.AreEqual("data stream:0: Info: Creating new item with ws=\"en\", abbr=\"157\", and name=\"Personality Traits\" in the Anthropology Categories list.", rgsLog[3]);
			Assert.AreEqual("data stream:0: Info: Creating new item with ws=\"en\", abbr=\"183\", and name=\"Norms\" in the Anthropology Categories list.", rgsLog[4]);
			Assert.AreEqual("data stream:0: Info: Creating new item with ws=\"en\", abbr=\"828\", and name=\"Ethnopsychology\" in the Anthropology Categories list.", rgsLog[5]);
			Assert.AreEqual("data stream:0: Info: Creating new item with ws=\"en\", abbr=\"4.3.1.3\", and name=\"4.3.1.3\" in the Semantic Domain list.", rgsLog[6]);
			Assert.AreEqual("data stream:0: Info: Creating new item with ws=\"en\", abbr=\"4.3.1.3.1\", and name=\"4.3.1.3.1\" in the Semantic Domain list.", rgsLog[7]);
			Assert.AreEqual("data stream:0: Info: Creating new item with ws=\"en\", abbr=\"2\", and name=\"2\" in the Semantic Domain list.", rgsLog[8]);
		}
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method ImportData() on additional data.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void ImportData2()
		{
			XmlImportData xid = new XmlImportData(m_cache);
			using (var rdr = new StringReader(
				"<FwDatabase>" +
				"<LangProject>" +
				"<LexDb>" +
				"<LexDb id=\"I1D5042E0D192A7D\">" +
				"<ReversalIndexes>" +
				"<ReversalIndex id=\"I1B901C10D93932B\">" +
				"<Name><AUni ws=\"en\">English Index</AUni></Name>" +
				"<WritingSystem><Uni>en</Uni></WritingSystem>" +
				"<Entries>" +
				"<ReversalIndexEntry id=\"I1B901F80D939332\">" +
				"<ReversalForm><AUni ws=\"en\">adolescent</AUni></ReversalForm>" +
				"<PartOfSpeech><Link target=\"I1B901DD0D93932D\"/></PartOfSpeech>" +
				"</ReversalIndexEntry>" +
				"</Entries>" +
				"<PartsOfSpeech>" +
				"<CmPossibilityList id=\"I1B901C20D93932B\">" +
				"<Name><AUni ws=\"en\">Parts Of Speech</AUni></Name>" +
				"<Abbreviation><AUni ws=\"en\">Parts Of Speech</AUni></Abbreviation>" +
				"<WsSelector><Integer val=\"-3\" /></WsSelector>" +
				"<IsSorted><Boolean val=\"true\" /></IsSorted>" +
				"<IsClosed><Boolean val=\"false\" /></IsClosed>" +
				"<ItemClsid><Integer val=\"7\" /></ItemClsid>" +
				"<UseExtendedFields><Boolean val=\"true\" /></UseExtendedFields>" +
				"<Possibilities>" +
				"<PartOfSpeech id=\"I1B901DC0D93932D\">" +
				"<Name><AUni ws=\"en\">noun</AUni></Name>" +
				"<Abbreviation><AUni ws=\"en\">n</AUni></Abbreviation>" +
				"<SubPossibilities>" +
				"<PartOfSpeech id=\"I1B901DD0D93932D\">" +
				"<Name><AUni ws=\"en\">common noun</AUni></Name>" +
				"<Abbreviation><AUni ws=\"en\">comm</AUni></Abbreviation>" +
				"</PartOfSpeech>" +
				"<PartOfSpeech id=\"I1B901DE0D93932D\">" +
				"<Name><AUni ws=\"en\">concrete noun</AUni></Name>" +
				"<Abbreviation><AUni ws=\"en\">conc</AUni></Abbreviation>" +
				"</PartOfSpeech>" +
				"<PartOfSpeech id=\"I1B901DF0D93932D\">" +
				"<Name><AUni ws=\"en\">nominal</AUni></Name>" +
				"<Abbreviation><AUni ws=\"en\">nom</AUni></Abbreviation>" +
				"</PartOfSpeech>" +
				"<PartOfSpeech id=\"I1B901E00D93932D\">" +
				"<Name><AUni ws=\"en\">possessive noun</AUni></Name>" +
				"<Abbreviation><AUni ws=\"en\">poss</AUni></Abbreviation>" +
				"</PartOfSpeech>" +
				"<PartOfSpeech id=\"I1B901E10D93932D\">" +
				"<Name><AUni ws=\"en\">proper noun</AUni></Name>" +
				"<Abbreviation><AUni ws=\"en\">prop</AUni></Abbreviation>" +
				"</PartOfSpeech>" +
				"</SubPossibilities>" +
				"</PartOfSpeech>" +
				"</Possibilities>" +
				"</CmPossibilityList>" +
				"</PartsOfSpeech>" +
				"</ReversalIndex>" +
				"</ReversalIndexes>" +
				"<Entries>" +
				"<LexEntry id=\"I1D5049B0D1AA622\">" +
				"<LexemeForm>" +
				"<MoStemAllomorph id=\"I1860114246B081E\">" +
				"<Form><AUni ws=\"x-ame\">adolescent</AUni></Form>" +
				"<MorphType><Link ws=\"en\" abbr=\"rt\" name=\"root\" /></MorphType>" +
				"</MoStemAllomorph>" +
				"</LexemeForm>" +
				"<MorphoSyntaxAnalyses>" +
				"<MoStemMsa id=\"I1860115246B081E\">" +
				"<PartOfSpeech><Link ws=\"en\" abbr=\"com n\" name=\"common noun\" /></PartOfSpeech>" +
				"</MoStemMsa>" +
				"</MorphoSyntaxAnalyses>" +
				"<Senses>" +
				"<LexSense id=\"I1D5049D0D1AA622\">" +
				"<MorphoSyntaxAnalysis><Link target=\"I1860115246B081E\" class=\"MoMorphoSyntaxAnalysis\" /></MorphoSyntaxAnalysis>" +
				"<Senses>" +
				"<LexSense id=\"I1D5049C0D1AA622\">" +
				"<MorphoSyntaxAnalysis><Link target=\"I1860115246B081E\" class=\"MoMorphoSyntaxAnalysis\" /></MorphoSyntaxAnalysis>" +
				"<Gloss><AUni ws=\"en\">adolescent</AUni></Gloss>" +
				"<Definition><AStr ws=\"en\"><Run ws=\"en\">a boy or girl from the period of puberty to adulthood</Run></AStr></Definition>" +
				"<AnthroCodes><Link ws=\"en\" abbr=\"561\" name=\"561 Age Stratification\" /></AnthroCodes>" +
				"<ReversalEntries><Link ws=\"en\" form=\"adolescent\" /></ReversalEntries>" +
				"<SemanticDomains><Link ws=\"en\" abbr=\"2.6.4.2\" /></SemanticDomains>" +
				"</LexSense>" +
				"</Senses>" +
				"</LexSense>" +
				"</Senses>" +
				"</LexEntry>" +
				"</Entries>" +
				"</LexDb>" +
				"</LexDb>" +
				"</LangProject>" +
				"<WfiWordform id=\"I1B904B50D76B9BD\">" +
				"<Form><AUni ws=\"x-ame\">this</AUni></Form>" +
				"</WfiWordform>" +
				"<WfiWordform id=\"I1D5021A0D2FB9E9\">" +
				"<Form><AUni ws=\"x-ame\">those</AUni></Form>" +
				"</WfiWordform>" +
				"</FwDatabase>"
				))
			{
			StringBuilder sbLog = new StringBuilder();
			bool fOk = xid.ImportData(rdr, new StringWriter(sbLog), null);
			Assert.IsTrue(fOk, "Import of one LexEntry, one RreversalIndexEntry, and two WfiWordforms succeeded.");
			IWritingSystem wsEn = m_cache.ServiceLocator.WritingSystemManager.Get("en");
			Assert.AreEqual(1, m_cache.LangProject.LexDbOA.ReversalIndexesOC.Count);
			IReversalIndex revIdx = m_cache.LangProject.LexDbOA.ReversalIndexesOC.ToArray()[0];
			IMultiUnicode mu = revIdx.Name;
			Assert.AreEqual(1, mu.StringCount);
			ITsString tss = mu.get_String(wsEn.Handle);
			Assert.AreEqual("English Index", tss.Text);
			Assert.AreEqual(wsEn.Id, revIdx.WritingSystem);
			Assert.AreEqual(1, revIdx.EntriesOC.Count);
			IReversalIndexEntry revEntry = revIdx.EntriesOC.ToArray()[0];
			mu = revEntry.ReversalForm;
			tss = mu.get_String(wsEn.Handle);
			Assert.AreEqual("adolescent", tss.Text);
			Assert.AreEqual("Parts Of Speech", revIdx.PartsOfSpeechOA.Name.get_String(wsEn.Handle).Text);
			Assert.AreEqual("Parts Of Speech", revIdx.PartsOfSpeechOA.Abbreviation.get_String(wsEn.Handle).Text);
			Assert.AreEqual(-3, revIdx.PartsOfSpeechOA.WsSelector);
			Assert.IsTrue(revIdx.PartsOfSpeechOA.IsSorted);
			Assert.IsFalse(revIdx.PartsOfSpeechOA.IsClosed);
			Assert.AreEqual(7, revIdx.PartsOfSpeechOA.ItemClsid);
			Assert.IsTrue(revIdx.PartsOfSpeechOA.UseExtendedFields);
			Assert.AreEqual(1, revIdx.PartsOfSpeechOA.PossibilitiesOS.Count);
			ICmPossibility poss = revIdx.PartsOfSpeechOA.PossibilitiesOS[0];
			Assert.AreEqual("noun", poss.Name.get_String(wsEn.Handle).Text);
			Assert.AreEqual("n", poss.Abbreviation.get_String(wsEn.Handle).Text);
			Assert.AreEqual(5, poss.SubPossibilitiesOS.Count);
			ICmPossibility subposs;
			subposs = poss.SubPossibilitiesOS[0];
			Assert.AreEqual("common noun", subposs.Name.get_String(wsEn.Handle).Text);
			Assert.AreEqual("comm", subposs.Abbreviation.get_String(wsEn.Handle).Text);
			Assert.AreEqual(revEntry.PartOfSpeechRA, subposs as IPartOfSpeech);
			subposs = poss.SubPossibilitiesOS[1];
			Assert.AreEqual("concrete noun", subposs.Name.get_String(wsEn.Handle).Text);
			Assert.AreEqual("conc", subposs.Abbreviation.get_String(wsEn.Handle).Text);
			subposs = poss.SubPossibilitiesOS[2];
			Assert.AreEqual("nominal", subposs.Name.get_String(wsEn.Handle).Text);
			Assert.AreEqual("nom", subposs.Abbreviation.get_String(wsEn.Handle).Text);
			subposs = poss.SubPossibilitiesOS[3];
			Assert.AreEqual("possessive noun", subposs.Name.get_String(wsEn.Handle).Text);
			Assert.AreEqual("poss", subposs.Abbreviation.get_String(wsEn.Handle).Text);
			subposs = poss.SubPossibilitiesOS[4];
			Assert.AreEqual("proper noun", subposs.Name.get_String(wsEn.Handle).Text);
			Assert.AreEqual("prop", subposs.Abbreviation.get_String(wsEn.Handle).Text);

			Assert.AreEqual(1, m_cache.LangProject.LexDbOA.Entries.Count(), "The import data had one entry.");
			ILexEntry entry = m_cache.LangProject.LexDbOA.Entries.ToArray()[0];
			IMoForm form = entry.LexemeFormOA;
			Assert.IsTrue(form is IMoStemAllomorph);
			int wsAme = m_cache.WritingSystemFactory.GetWsFromStr("x-ame");
			Assert.AreEqual(1, form.Form.StringCount);
			Assert.AreEqual("adolescent", form.Form.get_String(wsAme).Text);
			Assert.AreEqual("root", form.MorphTypeRA.Name.get_String(wsEn.Handle).Text);
			Assert.AreEqual(1, entry.MorphoSyntaxAnalysesOC.Count);
			IMoStemMsa msaStem = entry.MorphoSyntaxAnalysesOC.ToArray()[0] as IMoStemMsa;
			Assert.IsNotNull(msaStem);
			Assert.AreEqual("common noun", msaStem.PartOfSpeechRA.Name.get_String(wsEn.Handle).Text);
			Assert.AreNotEqual(revEntry.PartOfSpeechRA, msaStem.PartOfSpeechRA);
			Assert.AreEqual(revEntry.PartOfSpeechRA.Name.get_String(wsEn.Handle).Text,
				msaStem.PartOfSpeechRA.Name.get_String(wsEn.Handle).Text);
			Assert.AreEqual(1, entry.SensesOS.Count);
			ILexSense sense = entry.SensesOS[0];
			Assert.AreEqual(msaStem, sense.MorphoSyntaxAnalysisRA);
			Assert.AreEqual(1, sense.SensesOS.Count);
			ILexSense subsense = sense.SensesOS[0];
			Assert.AreEqual(msaStem, subsense.MorphoSyntaxAnalysisRA);
			Assert.AreEqual(1, subsense.Gloss.StringCount);
			Assert.AreEqual("adolescent", subsense.Gloss.get_String(wsEn.Handle).Text);
			Assert.AreEqual(1, subsense.Definition.StringCount);
			ITsString tss0 = m_cache.TsStrFactory.MakeString("a boy or girl from the period of puberty to adulthood", wsEn.Handle);
			Assert.IsTrue(tss0.Equals(subsense.Definition.get_String(wsEn.Handle)));
			Assert.AreEqual(1, subsense.AnthroCodesRC.Count);
			ICmAnthroItem anth = subsense.AnthroCodesRC.ToArray()[0];
			Assert.AreEqual("Age Stratification", anth.Name.get_String(wsEn.Handle).Text);
			Assert.AreEqual("561", anth.Abbreviation.get_String(wsEn.Handle).Text);
			Assert.AreEqual(1, subsense.SemanticDomainsRC.Count);
			ICmSemanticDomain sem = subsense.SemanticDomainsRC.ToArray()[0];
			Assert.AreEqual("2.6.4.2", sem.Name.get_String(wsEn.Handle).Text);
			Assert.AreEqual("2.6.4.2", sem.Abbreviation.get_String(wsEn.Handle).Text);
			Assert.AreEqual(1, subsense.ReversalEntriesRC.Count);
			IReversalIndexEntry rieSense = subsense.ReversalEntriesRC.ToArray()[0];
			Assert.AreEqual(revEntry, rieSense);
			IWfiWordformRepository repoWfi = m_cache.ServiceLocator.GetInstance<IWfiWordformRepository>();
			Assert.AreEqual(2, repoWfi.Count);
			IWfiWordform wfiThis = repoWfi.GetMatchingWordform(wsAme, "this");
			Assert.IsNotNull(wfiThis);
			Assert.AreEqual(0, wfiThis.AnalysesOC.Count);
			IWfiWordform wfiThose = repoWfi.GetMatchingWordform(wsAme, "those");
			Assert.IsNotNull(wfiThose);
			Assert.AreEqual(0, wfiThose.AnalysesOC.Count);

			string sLog = sbLog.ToString();
			Assert.IsFalse(String.IsNullOrEmpty(sLog), "There should be some log information!");
			string[] rgsLog = sLog.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
			Assert.LessOrEqual(4, rgsLog.Length);
			Assert.AreEqual("data stream:0: Info: Creating new writing system for \"x-ame\".", rgsLog[0]);
			Assert.AreEqual("data stream:0: Info: Creating new item with ws=\"en\", abbr=\"com n\", and name=\"common noun\" in the Parts of Speech list.", rgsLog[1]);
			Assert.AreEqual("data stream:0: Info: Creating new item with ws=\"en\", abbr=\"561\", and name=\"Age Stratification\" in the Anthropology Categories list.", rgsLog[2]);
			Assert.AreEqual("data stream:0: Info: Creating new item with ws=\"en\", abbr=\"2.6.4.2\", and name=\"2.6.4.2\" in the Semantic Domain list.", rgsLog[3]);
		}
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method ImportData() on even more data.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		//[Ignore("This needs to be rewritten so that imported files are in resources.")]
		public void ImportData3()
		{
			XmlImportData xid = new XmlImportData(m_cache);
			string sFwSrcDir = DirectoryFinder.FwSourceDirectory;
			using (var rdr = new StringReader(
				"<FwDatabase>" + Environment.NewLine +
				"<LangProject>" + Environment.NewLine +
				"<LexDb>" + Environment.NewLine +
				"<LexDb id=\"I1D5042E0D192A7D\">" + Environment.NewLine +
				"<Entries>" + Environment.NewLine +
				"<LexEntry id=\"I1D507650D33C672\">" + Environment.NewLine +
				"<LexemeForm>" + Environment.NewLine +
				"<MoStemAllomorph id=\"I186011A246B081E\">" + Environment.NewLine +
				"<Form><AUni ws=\"x-ame\">an</AUni></Form>" + Environment.NewLine +
				"<MorphType><Link ws=\"en\" abbr=\"rt\" name=\"root\" /></MorphType>" + Environment.NewLine +
				"</MoStemAllomorph>" + Environment.NewLine +
				"</LexemeForm>" + Environment.NewLine +
				"<AlternateForms>" + Environment.NewLine +
				"<MoStemAllomorph id=\"I186011B246B081E\">" + Environment.NewLine +
				"<Form><AUni ws=\"x-ame\">a</AUni></Form>" + Environment.NewLine +
				"<MorphType><Link ws=\"en\" abbr=\"rt\" name=\"root\" /></MorphType>" + Environment.NewLine +
				"</MoStemAllomorph>" + Environment.NewLine +
				"</AlternateForms>" + Environment.NewLine +
				"<DateCreated><Time val=\"1996-01-8 14:55:46.000\" /></DateCreated>" + Environment.NewLine +
				"<DateModified><Time val=\"2008-05-12 16:36:46.000\" /></DateModified>" + Environment.NewLine +
				"<Pronunciations>" + Environment.NewLine +
				"<LexPronunciation id=\"I18607B11FBCF4A0\">" + Environment.NewLine +
				"<Form><AUni ws=\"x-ame-fonipa\">an</AUni></Form>" + Environment.NewLine +
				"<MediaFiles>" + Environment.NewLine +
				"<CmMedia><MediaFile><Link path=\"" +
				Path.Combine(sFwSrcDir, "FDO/FDOTests/TestData/house.wav") +
				"\" /></MediaFile></CmMedia>" + Environment.NewLine +
				"</MediaFiles>" + Environment.NewLine +
				"</LexPronunciation>" + Environment.NewLine +
				"</Pronunciations>" + Environment.NewLine +
				"<MorphoSyntaxAnalyses>" + Environment.NewLine +
				"<MoStemMsa id=\"I186011C246B081E\">" + Environment.NewLine +
				"<PartOfSpeech><Link ws=\"en\" abbr=\"adj, indef art\" name=\"indefinite article\" /></PartOfSpeech>" + Environment.NewLine +
				"</MoStemMsa>" + Environment.NewLine +
				"</MorphoSyntaxAnalyses>" + Environment.NewLine +
				"<Senses>" + Environment.NewLine +
				"<LexSense id=\"I1D507670D33C672\">" + Environment.NewLine +
				"<MorphoSyntaxAnalysis>" + Environment.NewLine +
				"<Link target=\"I186011C246B081E\" class=\"MoMorphoSyntaxAnalysis\" />" + Environment.NewLine +
				"</MorphoSyntaxAnalysis>" + Environment.NewLine +
				"<Definition><AStr ws=\"en\"><Run ws=\"en\">sense group heading</Run></AStr></Definition>" + Environment.NewLine +
				"<Senses>" + Environment.NewLine +
				"<LexSense id=\"I1D507660D33C672\">" + Environment.NewLine +
				"<MorphoSyntaxAnalysis><Link target=\"I186011C246B081E\" class=\"MoMorphoSyntaxAnalysis\" /></MorphoSyntaxAnalysis>" + Environment.NewLine +
				"<Gloss><AUni ws=\"en\">one</AUni></Gloss>" + Environment.NewLine +
				"<Definition><AStr ws=\"en\"><Run ws=\"en\">one; one sort of</Run></AStr></Definition>" + Environment.NewLine +
				"</LexSense>" + Environment.NewLine +
				"<LexSense id=\"I1D5087F0D33C9E7\">" + Environment.NewLine +
				"<MorphoSyntaxAnalysis><Link target=\"I186011C246B081E\" class=\"MoMorphoSyntaxAnalysis\" /></MorphoSyntaxAnalysis>" + Environment.NewLine +
				"<Gloss><AUni ws=\"en\">any</AUni></Gloss>" + Environment.NewLine +
				"<Definition><AStr ws=\"en\"><Run ws=\"en\">each; any one</Run></AStr></Definition>" + Environment.NewLine +
				"</LexSense>" + Environment.NewLine +
				"<LexSense id=\"I1D508C10D33CF1F\">" + Environment.NewLine +
				"<MorphoSyntaxAnalysis><Link target=\"I186011C246B081E\" class=\"MoMorphoSyntaxAnalysis\" /></MorphoSyntaxAnalysis>" + Environment.NewLine +
				"<Gloss><AUni ws=\"en\">per</AUni></Gloss>" + Environment.NewLine +
				"<Definition><AStr ws=\"en\"><Run ws=\"en\">to each; in each; for each;</Run></AStr></Definition>" + Environment.NewLine +
				"<ScientificName><Str><Run ws=\"la\">Latin term</Run></Str></ScientificName>" + Environment.NewLine +
				"<Pictures>" + Environment.NewLine +
				"<CmPicture id=\"I1860A701FBCF6DA\">" + Environment.NewLine +
				"<PictureFile><Link path=\"" +
				Path.Combine(sFwSrcDir, "FDO/FDOTests/TestData/penguin.jpg") +
				"\" /></PictureFile>" + Environment.NewLine +
				"<Caption><AStr ws=\"en\"><Run ws=\"en\">English caption</Run></AStr></Caption>" + Environment.NewLine +
				"</CmPicture>" + Environment.NewLine +
				"</Pictures>" + Environment.NewLine +
				"</LexSense>" + Environment.NewLine +
				"</Senses>" + Environment.NewLine +
				"</LexSense>" + Environment.NewLine +
				"</Senses>" + Environment.NewLine +
				"</LexEntry>" + Environment.NewLine +
				"<LexEntry id=\"I1D501320D3373F8\">" + Environment.NewLine +
				"<EntryRefs>" + Environment.NewLine +
				"<LexEntryRef>" + Environment.NewLine +
				"<VariantEntryTypes><Link ws=\"en\" name=\"Irregularly Inflected Form\" /></VariantEntryTypes>" + Environment.NewLine +
				"<ComponentLexemes><Link target=\"I1D50AF20D2FCBF6\" wsa=\"en\" abbr=\"llcr\" wsv=\"x-ame\" entry=\"be\" /></ComponentLexemes>" + Environment.NewLine +
				"<Summary><AStr ws=\"en\"><Run ws=\"en\">1ps PRES INDIC</Run></AStr></Summary>" + Environment.NewLine +
				"</LexEntryRef>" + Environment.NewLine +
				"</EntryRefs>" + Environment.NewLine +
				"<LexemeForm>" + Environment.NewLine +
				"<MoStemAllomorph id=\"I1860118246B081E\">" + Environment.NewLine +
				"<Form><AUni ws=\"x-ame\">am</AUni></Form>" + Environment.NewLine +
				"<MorphType><Link ws=\"en\" abbr=\"stm\" name=\"stem\" /></MorphType>" + Environment.NewLine +
				"</MoStemAllomorph>" + Environment.NewLine +
				"</LexemeForm>" + Environment.NewLine +
				"<DateCreated><Time val=\"1996-01-8 09:03:52.000\" /></DateCreated>" + Environment.NewLine +
				"<DateModified><Time val=\"2008-05-12 16:36:46.000\" /></DateModified>" + Environment.NewLine +
				"<MorphoSyntaxAnalyses><MoStemMsa id=\"I1860119246B081E\" /></MorphoSyntaxAnalyses>" + Environment.NewLine +
				"</LexEntry>" + Environment.NewLine +
				"<LexEntry id=\"I1D50AF20D2FCBF6\">" + Environment.NewLine +
				"<LexemeForm>" + Environment.NewLine +
				"<MoStemAllomorph id=\"I1860121246B081E\">" + Environment.NewLine +
				"<Form><AUni ws=\"x-ame\">be</AUni></Form>" + Environment.NewLine +
				"<MorphType><Link ws=\"en\" abbr=\"stm\" name=\"stem\" /></MorphType>" + Environment.NewLine +
				"</MoStemAllomorph>" + Environment.NewLine +
				"</LexemeForm>" + Environment.NewLine +
				"<AlternateForms>" + Environment.NewLine +
				"<MoStemAllomorph id=\"I1860122246B081E\">" + Environment.NewLine +
				"<Form><AUni ws=\"x-ame\">is</AUni></Form>" + Environment.NewLine +
				"<MorphType><Link ws=\"en\" abbr=\"stm\" name=\"stem\" /></MorphType>" + Environment.NewLine +
				"</MoStemAllomorph>" + Environment.NewLine +
				"</AlternateForms>" + Environment.NewLine +
				"<DateCreated><Time val=\"1996-01-5 14:30:14.000\" /></DateCreated>" + Environment.NewLine +
				"<DateModified><Time val=\"2008-05-12 16:36:46.000\" /></DateModified>" + Environment.NewLine +
				"<MorphoSyntaxAnalyses>" + Environment.NewLine +
				"<MoStemMsa id=\"I1860123246B081E\">" + Environment.NewLine +
				"<PartOfSpeech><Link ws=\"en\" abbr=\"vi\" name=\"intransitive verb\" /></PartOfSpeech>" + Environment.NewLine +
				"</MoStemMsa>" + Environment.NewLine +
				"<MoStemMsa id=\"I1860124246B081E\">" + Environment.NewLine +
				"<PartOfSpeech><Link ws=\"en\" abbr=\"aux\" name=\"auxiliary verb\" /></PartOfSpeech>" + Environment.NewLine +
				"</MoStemMsa>" + Environment.NewLine +
				"</MorphoSyntaxAnalyses>" + Environment.NewLine +
				"<Senses>" + Environment.NewLine +
				"<LexSense id=\"I1D50AF40D2FCBF6\">" + Environment.NewLine +
				"<MorphoSyntaxAnalysis><Link target=\"I1860123246B081E\" class=\"MoMorphoSyntaxAnalysis\" /></MorphoSyntaxAnalysis>" + Environment.NewLine +
				"<Senses>" + Environment.NewLine +
				"<LexSense id=\"I1D503D10D337764\">" + Environment.NewLine +
				"<Definition><AStr ws=\"en\"><Run ws=\"en\">As a substantive verb:</Run></AStr></Definition>" + Environment.NewLine +
				"<Senses>" + Environment.NewLine +
				"<LexSense id=\"I1D50AF30D2FCBF6\">" + Environment.NewLine +
				"<MorphoSyntaxAnalysis><Link target=\"I1860123246B081E\" class=\"MoMorphoSyntaxAnalysis\" /></MorphoSyntaxAnalysis>" + Environment.NewLine +
				"<Gloss><AUni ws=\"en\">exist</AUni></Gloss>" + Environment.NewLine +
				"<Definition><AStr ws=\"en\"><Run ws=\"en\">to exist; live</Run></AStr></Definition>" + Environment.NewLine +
				"</LexSense>" + Environment.NewLine +
				"<LexSense id=\"I1D50B470D2FCC77\">" + Environment.NewLine +
				"<MorphoSyntaxAnalysis><Link target=\"I1860123246B081E\" class=\"MoMorphoSyntaxAnalysis\" /></MorphoSyntaxAnalysis>" + Environment.NewLine +
				"<Gloss><AUni ws=\"en\">happen</AUni></Gloss>" + Environment.NewLine +
				"<Definition><AStr ws=\"en\"><Run ws=\"en\">to happen or occur</Run></AStr></Definition>" + Environment.NewLine +
				"</LexSense>" + Environment.NewLine +
				"<LexSense id=\"I1D50B5C0D2FCC9F\">" + Environment.NewLine +
				"<MorphoSyntaxAnalysis><Link target=\"I1860123246B081E\" class=\"MoMorphoSyntaxAnalysis\" /></MorphoSyntaxAnalysis>" + Environment.NewLine +
				"<Gloss><AUni ws=\"en\">remain</AUni></Gloss>" + Environment.NewLine +
				"<Definition><AStr ws=\"en\"><Run ws=\"en\">to remain or continue</Run></AStr></Definition>" + Environment.NewLine +
				"</LexSense>" + Environment.NewLine +
				"<LexSense id=\"I1D50B780D2FCCCF\">" + Environment.NewLine +
				"<MorphoSyntaxAnalysis>" + Environment.NewLine +
				"<Link target=\"I1860123246B081E\" class=\"MoMorphoSyntaxAnalysis\" />" + Environment.NewLine +
				"</MorphoSyntaxAnalysis>" + Environment.NewLine +
				"<Gloss><AUni ws=\"en\">belong</AUni></Gloss>" + Environment.NewLine +
				"<Definition><AStr ws=\"en\"><Run ws=\"en\">to come to; belong</Run></AStr></Definition>" + Environment.NewLine +
				"</LexSense>" + Environment.NewLine +
				"<LexSense id=\"I1D50B970D2FCD31\">" + Environment.NewLine +
				"<MorphoSyntaxAnalysis><Link target=\"I1860123246B081E\" class=\"MoMorphoSyntaxAnalysis\" /></MorphoSyntaxAnalysis>" + Environment.NewLine +
				"<Gloss><AUni ws=\"en\">have place</AUni></Gloss>" + Environment.NewLine +
				"<Definition><AStr ws=\"en\"><Run ws=\"en\">to have a place or position</Run></AStr></Definition>" + Environment.NewLine +
				"</LexSense>" + Environment.NewLine +
				"</Senses>" + Environment.NewLine +
				"</LexSense>" + Environment.NewLine +
				"<LexSense id=\"I1D503E70D337812\">" + Environment.NewLine +
				"<Definition><AStr ws=\"en\"><Run ws=\"en\">As a copula:</Run></AStr></Definition>" + Environment.NewLine +
				"<Senses>" + Environment.NewLine +
				"<LexSense id=\"I1D504240D3378B9\">" + Environment.NewLine +
				"<SenseType><Link ws=\"en\" abbr=\"prim\" name=\"primary\" /></SenseType>" + Environment.NewLine +
				"<Senses>" + Environment.NewLine +
				"<LexSense id=\"I1D503F40D33782C\">" + Environment.NewLine +
				"<MorphoSyntaxAnalysis><Link target=\"I1860123246B081E\" class=\"MoMorphoSyntaxAnalysis\" /></MorphoSyntaxAnalysis>" + Environment.NewLine +
				"<Gloss><AUni ws=\"en\">is</AUni></Gloss>" + Environment.NewLine +
				"<Definition>" + Environment.NewLine +
				"<AStr ws=\"en\"><Run ws=\"en\">the linker between a subject and a predicate nominative, adjective, or pronoun so as to express attribution</Run></AStr>" + Environment.NewLine +
				"</Definition>" + Environment.NewLine +
				"</LexSense>" + Environment.NewLine +
				"<LexSense id=\"I1D504160D33789F\">" + Environment.NewLine +
				"<MorphoSyntaxAnalysis>" + Environment.NewLine +
				"<Link target=\"I1860123246B081E\" class=\"MoMorphoSyntaxAnalysis\" />" + Environment.NewLine +
				"</MorphoSyntaxAnalysis>" + Environment.NewLine +
				"<Gloss><AUni ws=\"en\">equals</AUni></Gloss>" + Environment.NewLine +
				"<Definition>" + Environment.NewLine +
				"<AStr ws=\"en\"><Run ws=\"en\">the linker between a subject and a predicate nominative, adjective, or pronoun so as to express identity</Run></AStr>" + Environment.NewLine +
				"</Definition>" + Environment.NewLine +
				"</LexSense>" + Environment.NewLine +
				"</Senses>" + Environment.NewLine +
				"</LexSense>" + Environment.NewLine +
				"<LexSense id=\"I1D5049A0D337999\">" + Environment.NewLine +
				"<SenseType><Link ws=\"en\" abbr=\"sec\" name=\"secondary\" /></SenseType>" + Environment.NewLine +
				"<Senses>" + Environment.NewLine +
				"<LexSense id=\"I1D5049B0D337999\">" + Environment.NewLine +
				"<MorphoSyntaxAnalysis><Link target=\"I1860123246B081E\" class=\"MoMorphoSyntaxAnalysis\" /></MorphoSyntaxAnalysis>" + Environment.NewLine +
				"<Gloss>" + Environment.NewLine +
				"<AUni ws=\"en\">costs</AUni>" + Environment.NewLine +
				"</Gloss>" + Environment.NewLine +
				"<Definition>" + Environment.NewLine +
				"<AStr ws=\"en\"><Run ws=\"en\">the linker between a subject and a predicate nominative, adjective, or pronoun so as to express value</Run></AStr>" + Environment.NewLine +
				"</Definition>" + Environment.NewLine +
				"</LexSense>" + Environment.NewLine +
				"<LexSense id=\"I1D5049D0D337999\">" + Environment.NewLine +
				"<MorphoSyntaxAnalysis><Link target=\"I1860123246B081E\" class=\"MoMorphoSyntaxAnalysis\" /></MorphoSyntaxAnalysis>" + Environment.NewLine +
				"<Gloss><AUni ws=\"en\">causes</AUni></Gloss>" + Environment.NewLine +
				"<Definition>" + Environment.NewLine +
				"<AStr ws=\"en\"><Run ws=\"en\">the linker between a subject and a predicate nominative, adjective, or pronoun so as to express cause</Run></AStr>" + Environment.NewLine +
				"</Definition>" + Environment.NewLine +
				"<SenseType><Link ws=\"en\" abbr=\"sec\" name=\"secondary\" /></SenseType>" + Environment.NewLine +
				"</LexSense>" + Environment.NewLine +
				"<LexSense id=\"I1D505150D337AC7\">" + Environment.NewLine +
				"<MorphoSyntaxAnalysis><Link target=\"I1860123246B081E\" class=\"MoMorphoSyntaxAnalysis\" /></MorphoSyntaxAnalysis>" + Environment.NewLine +
				"<Gloss><AUni ws=\"en\">signify</AUni></Gloss>" + Environment.NewLine +
				"<Definition>" + Environment.NewLine +
				"<AStr ws=\"en\"><Run ws=\"en\">the linker between a subject and a predicate nominative, adjective, or pronoun so as to express signification</Run></AStr>" + Environment.NewLine +
				"</Definition>" + Environment.NewLine +
				"</LexSense>" + Environment.NewLine +
				"</Senses>" + Environment.NewLine +
				"</LexSense>" + Environment.NewLine +
				"</Senses>" + Environment.NewLine +
				"</LexSense>" + Environment.NewLine +
				"</Senses>" + Environment.NewLine +
				"</LexSense>" + Environment.NewLine +
				"<LexSense id=\"I1D50BAC0D2FCD6A\">" + Environment.NewLine +
				"<MorphoSyntaxAnalysis><Link target=\"I1860124246B081E\" class=\"MoMorphoSyntaxAnalysis\" /></MorphoSyntaxAnalysis>" + Environment.NewLine +
				"<Senses>" + Environment.NewLine +
				"<LexSense id=\"I1D50BCF0D2FCD84\">" + Environment.NewLine +
				"<MorphoSyntaxAnalysis><Link target=\"I1860124246B081E\" class=\"MoMorphoSyntaxAnalysis\" /></MorphoSyntaxAnalysis>" + Environment.NewLine +
				"<Gloss><AUni ws=\"en\">PASS</AUni></Gloss>" + Environment.NewLine +
				"<Definition>" + Environment.NewLine +
				"<AStr ws=\"en\"><Run ws=\"en\">used with the past participle of a transitive verb to form the passive voice</Run></AStr>" + Environment.NewLine +
				"</Definition>" + Environment.NewLine +
				"</LexSense>" + Environment.NewLine +
				"<LexSense id=\"I1D50C750D2FCFD4\">" + Environment.NewLine +
				"<MorphoSyntaxAnalysis><Link target=\"I1860124246B081E\" class=\"MoMorphoSyntaxAnalysis\" /></MorphoSyntaxAnalysis>" + Environment.NewLine +
				"<Gloss><AUni ws=\"en\">PERF</AUni></Gloss>" + Environment.NewLine +
				"<Definition>" + Environment.NewLine +
				"<AStr ws=\"en\"><Run ws=\"en\">used with the past participle of certain intransitive verbs to form a perfect tense</Run></AStr>" + Environment.NewLine +
				"</Definition>" + Environment.NewLine +
				"</LexSense>" + Environment.NewLine +
				"<LexSense id=\"I1D50CA00D2FD01A\">" + Environment.NewLine +
				"<MorphoSyntaxAnalysis><Link target=\"I1860124246B081E\" class=\"MoMorphoSyntaxAnalysis\" /></MorphoSyntaxAnalysis>" + Environment.NewLine +
				"<Gloss><AUni ws=\"en\">CONT</AUni></Gloss>" + Environment.NewLine +
				"<Definition>" + Environment.NewLine +
				"<AStr ws=\"en\"><Run ws=\"en\">used with the present participle of another verb to express continuation</Run></AStr>" + Environment.NewLine +
				"</Definition>" + Environment.NewLine +
				"</LexSense>" + Environment.NewLine +
				"<LexSense id=\"I1D5058A0D33814A\">" + Environment.NewLine +
				"<Definition>" + Environment.NewLine +
				"<AStr ws=\"en\"><Run ws=\"en\">IRREALIS</Run></AStr>" + Environment.NewLine +
				"</Definition>" + Environment.NewLine +
				"<Senses>" + Environment.NewLine +
				"<LexSense id=\"I1D50CB70D2FD062\">" + Environment.NewLine +
				"<MorphoSyntaxAnalysis><Link target=\"I1860124246B081E\" class=\"MoMorphoSyntaxAnalysis\" /></MorphoSyntaxAnalysis>" + Environment.NewLine +
				"<Gloss><AUni ws=\"en\">IRR FUT</AUni></Gloss>" + Environment.NewLine +
				"<Definition>" + Environment.NewLine +
				"<AStr ws=\"en\"><Run ws=\"en\">used with the present participle or infinitive of another verb to express futurity</Run></AStr>" + Environment.NewLine +
				"</Definition>" + Environment.NewLine +
				"</LexSense>" + Environment.NewLine +
				"<LexSense id=\"I1D505A90D3381B9\">" + Environment.NewLine +
				"<MorphoSyntaxAnalysis><Link target=\"I1860124246B081E\" class=\"MoMorphoSyntaxAnalysis\" /></MorphoSyntaxAnalysis>" + Environment.NewLine +
				"<Gloss><AUni ws=\"en\">IRR OBLIG</AUni></Gloss>" + Environment.NewLine +
				"<Definition>" + Environment.NewLine +
				"<AStr ws=\"en\"><Run ws=\"en\">used with the present participle or infinitive of another verb to express obligation</Run></AStr>" + Environment.NewLine +
				"</Definition>" + Environment.NewLine +
				"</LexSense>" + Environment.NewLine +
				"<LexSense id=\"I1D505EA0D33826E\">" + Environment.NewLine +
				"<MorphoSyntaxAnalysis><Link target=\"I1860124246B081E\" class=\"MoMorphoSyntaxAnalysis\" /></MorphoSyntaxAnalysis>" + Environment.NewLine +
				"<Gloss><AUni ws=\"en\">IRR POSS</AUni></Gloss>" + Environment.NewLine +
				"<Definition>" + Environment.NewLine +
				"<AStr ws=\"en\"><Run ws=\"en\">used with the present participle or infinitive of another verb to express possibility</Run></AStr>" + Environment.NewLine +
				"</Definition>" + Environment.NewLine +
				"</LexSense>" + Environment.NewLine +
				"<LexSense id=\"I1D506120D338294\">" + Environment.NewLine +
				"<MorphoSyntaxAnalysis><Link target=\"I1860124246B081E\" class=\"MoMorphoSyntaxAnalysis\" /></MorphoSyntaxAnalysis>" + Environment.NewLine +
				"<Gloss><AUni ws=\"en\">IRR INT</AUni></Gloss>" + Environment.NewLine +
				"<Definition>" + Environment.NewLine +
				"<AStr ws=\"en\"><Run ws=\"en\">used with the present participle or infinitive of another verb to express intention</Run></AStr>" + Environment.NewLine +
				"</Definition>" + Environment.NewLine +
				"</LexSense>" + Environment.NewLine +
				"</Senses>" + Environment.NewLine +
				"</LexSense>" + Environment.NewLine +
				"</Senses>" + Environment.NewLine +
				"</LexSense>" + Environment.NewLine +
				"</Senses>" + Environment.NewLine +
				"</LexEntry>" + Environment.NewLine +
				"</Entries>" + Environment.NewLine +
				"</LexDb>" + Environment.NewLine +
				"</LexDb>" + Environment.NewLine +
				"<Texts>" + Environment.NewLine +
				"<Text id=\"I1D500E90D2FB820\">" + Environment.NewLine +
				"<Name><AUni ws=\"x-ame\">Life begins each morning!: Praise the Lord!</AUni></Name>" + Environment.NewLine +
				"<Contents>" + Environment.NewLine +
				"<StText>" + Environment.NewLine +
				"<Paragraphs>" + Environment.NewLine +
				"<StTxtPara id=\"I1D5016C0D2FB8D7\">" + Environment.NewLine +
				"<Contents>" + Environment.NewLine +
				"<Str>" + Environment.NewLine +
				"<Run ws=\"x-ame\">Whether one is 20, 40, or 60; </Run>" + Environment.NewLine +
				"<Run ws=\"x-ame\">whether one has succeeded, failed, or just muddled along; </Run>" + Environment.NewLine +
				"<Run ws=\"x-ame\">whether yesterday was full of sun or storm, </Run>" + Environment.NewLine +
				"<Run ws=\"x-ame\">or one of those dull days with no weather at all, </Run>" + Environment.NewLine +
				"<Run ws=\"x-ame\">Life Begins Each Morning!</Run>" + Environment.NewLine +
				"</Str>" + Environment.NewLine +
				"</Contents>" + Environment.NewLine +
				"</StTxtPara>" + Environment.NewLine +
				"<StTxtPara id=\"I18604621FBF274E\">" + Environment.NewLine +
				"<Contents>" + Environment.NewLine +
				"<Str>" + Environment.NewLine +
				"<Run ws=\"x-ame\">an apple</Run>" + Environment.NewLine +
				"</Str>" + Environment.NewLine +
				"</Contents>" + Environment.NewLine +
				"</StTxtPara>" + Environment.NewLine +
				"</Paragraphs>" + Environment.NewLine +
				"</StText>" + Environment.NewLine +
				"</Contents>" + Environment.NewLine +
				"</Text>" + Environment.NewLine +
				"</Texts>" + Environment.NewLine +
				"<AnalyzingAgents>" + Environment.NewLine +
				"<CmAgent id=\"I18601E1246B081E\">" + Environment.NewLine +
				"<Name><AUni ws=\"en\">default user</AUni></Name>" + Environment.NewLine +
				"<Human><Boolean val=\"true\" /></Human>" + Environment.NewLine +
				"<Approves>" + Environment.NewLine +
				"<CmAgentEvaluation id=\"I18601F5246B081F\">" + Environment.NewLine +
				"</CmAgentEvaluation>" + Environment.NewLine +
				"</Approves>" + Environment.NewLine +
				"</CmAgent>" + Environment.NewLine +
				"</AnalyzingAgents>" + Environment.NewLine +
				"</LangProject>" + Environment.NewLine +
				"<WfiWordform id=\"I18607CF1FBCF598\">" + Environment.NewLine +
				"<Form><AUni ws=\"x-ame\">an</AUni></Form>" + Environment.NewLine +
				"<Analyses>" + Environment.NewLine +
				"<WfiAnalysis id=\"I18605F01FBF27FC\">" + Environment.NewLine +
				"<Category><Link ws=\"en\" abbr=\"adj, indef art\" name=\"indefinite article\" /></Category>" + Environment.NewLine +
				"<Evaluations><Link target=\"I18601F5246B081F\" class=\"CmAgentEvaluation\" /></Evaluations>" +
				"<MorphBundles>" + Environment.NewLine +
				"<WfiMorphBundle id=\"I18601F4246B081F\">" + Environment.NewLine +
				"<Msa><Link target=\"I186011C246B081E\" class=\"MoMorphoSyntaxAnalysis\" /></Msa>" + Environment.NewLine +
				"<Morph><Link target=\"I186011A246B081E\" class=\"MoStemAllomorph\" /></Morph>" + Environment.NewLine +
				"<Sense><Link target=\"I1D508C10D33CF1F\" wsa=\"en\" abbr=\"llcr\" wsv=\"x-ame\" sense=\"an 1.3\" /></Sense>" + Environment.NewLine +
				"</WfiMorphBundle>" + Environment.NewLine +
				"</MorphBundles>" + Environment.NewLine +
				"<Meanings>" + Environment.NewLine +
				"<WfiGloss id=\"I18606651FBF2823\">" + Environment.NewLine +
				"</WfiGloss>" + Environment.NewLine +
				"</Meanings>" + Environment.NewLine +
				"</WfiAnalysis>" + Environment.NewLine +
				"</Analyses>" + Environment.NewLine +
				"</WfiWordform>" + Environment.NewLine +
				"<WfiWordform id=\"I1B904B70D76B9BE\">" + Environment.NewLine +
				"<Form><AUni ws=\"x-ame\">apple</AUni></Form>" + Environment.NewLine +
				"</WfiWordform>" + Environment.NewLine +
				"<CmBaseAnnotation id=\"I18604631FBF274E\">" + Environment.NewLine +
				"<BeginObject><Link target=\"I18604621FBF274E\" class=\"StTxtPara\" /></BeginObject>" + Environment.NewLine +
				"<EndObject><Link target=\"I18604621FBF274E\" class=\"StTxtPara\" /></EndObject>" + Environment.NewLine +
				"<Flid><Integer val=\"16002\" /></Flid>" + Environment.NewLine +
				"<BeginOffset><Integer val=\"1\" /></BeginOffset>" + Environment.NewLine +
				"<AnnotationType><Link ws=\"en\" name=\"Text Segment\" /></AnnotationType>" + Environment.NewLine +
				"<CompDetails><Uni>LLImport</Uni></CompDetails>" + Environment.NewLine +
				"</CmBaseAnnotation>" + Environment.NewLine +
				"<CmIndirectAnnotation>" + Environment.NewLine +
				"<AppliesTo><Link target=\"I18604631FBF274E\" class=\"Segment\" /></AppliesTo>" + Environment.NewLine +
				"<AnnotationType><Link ws=\"en\" name=\"Free Translation\" /></AnnotationType>" + Environment.NewLine +
				"<Comment><AStr ws=\"en\"><Run ws=\"en\">A free translation for an apple.</Run></AStr></Comment>" + Environment.NewLine +
				"<CompDetails><Uni>LLImport</Uni></CompDetails>" + Environment.NewLine +
				"</CmIndirectAnnotation>" + Environment.NewLine +
				"<CmBaseAnnotation id=\"I186047D1FBF278D\">" + Environment.NewLine +
				"<BeginObject><Link target=\"I18604621FBF274E\" class=\"StTxtPara\" /></BeginObject>" + Environment.NewLine +
				"<EndObject><Link target=\"I18604621FBF274E\" class=\"StTxtPara\" /></EndObject>" + Environment.NewLine +
				"<Flid><Integer val=\"16002\" /></Flid>" + Environment.NewLine +
				"<BeginOffset><Integer val=\"1\" /></BeginOffset>" + Environment.NewLine +
				"<EndOffset><Integer val=\"2\" /></EndOffset>" + Environment.NewLine +
				"<InstanceOf><Link target=\"I18606651FBF2823\" class=\"WfiGloss\" /></InstanceOf>" + Environment.NewLine +
				"<AnnotationType><Link ws=\"en\" name=\"Wordform In Context\" /></AnnotationType>" + Environment.NewLine +
				"<CompDetails><Uni>LLImport</Uni></CompDetails>" + Environment.NewLine +
				"</CmBaseAnnotation>" + Environment.NewLine +
				"<CmBaseAnnotation id=\"I186047E1FBF278D\">" + Environment.NewLine +
				"<BeginObject><Link target=\"I18604621FBF274E\" class=\"StTxtPara\" /></BeginObject>" + Environment.NewLine +
				"<EndObject><Link target=\"I18604621FBF274E\" class=\"StTxtPara\" /></EndObject>" + Environment.NewLine +
				"<Flid><Integer val=\"16002\" /></Flid>" + Environment.NewLine +
				"<BeginOffset><Integer val=\"2\" /></BeginOffset>" + Environment.NewLine +
				"<EndOffset><Integer val=\"5\" /></EndOffset>" + Environment.NewLine +
				"<InstanceOf><Link target=\"I1B904B70D76B9BE\" class=\"WfiWordform\" /></InstanceOf>" + Environment.NewLine +
				"<AnnotationType><Link ws=\"en\" name=\"Wordform In Context\" /></AnnotationType>" + Environment.NewLine +
				"<CompDetails><Uni>LLImport</Uni></CompDetails>" + Environment.NewLine +
				"</CmBaseAnnotation>" + Environment.NewLine +
				"</FwDatabase>" + Environment.NewLine
				))
			{
			StringBuilder sbLog = new StringBuilder();
			bool fOk = xid.ImportData(rdr, new StringWriter(sbLog), null);
			Assert.IsTrue(fOk, "Import of a bunch of stuff succeeded.");
			int wsEn = m_cache.WritingSystemFactory.GetWsFromStr("en");
			int wsAme = m_cache.WritingSystemFactory.GetWsFromStr("x-ame");
			Assert.AreEqual(0, m_cache.LangProject.LexDbOA.ReversalIndexesOC.Count);
			Assert.AreEqual(3, m_cache.LangProject.LexDbOA.Entries.Count());
			ILexEntry[] rgle = m_cache.LangProject.LexDbOA.Entries.ToArray();
			CheckFirstEntry(rgle[0], wsEn, wsAme);
			CheckSecondEntry(rgle[1], wsEn, wsAme, rgle[2]);
			CheckThirdEntry(rgle[2], wsEn, wsAme);
			Assert.AreEqual(1, m_cache.LangProject.TextsOC.Count);
			CheckTheText(m_cache.LangProject.TextsOC.ToArray()[0]);
			Assert.AreEqual(4, m_cache.LangProject.AnalyzingAgentsOC.Count);	// There are 3 standard agents.
			CheckTheAgent(m_cache.LangProject.AnalyzingAgentsOC.ToArray()[3], wsAme);
			CheckWfiWordforms(wsEn, wsAme);
			CheckAnnotations(wsEn, wsAme);

			string sLog = sbLog.ToString();
			Assert.IsFalse(String.IsNullOrEmpty(sLog), "There should be some log information!");
			string[] rgsLog = sLog.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
			Assert.LessOrEqual(12, rgsLog.Length);
			Assert.AreEqual("data stream:0: Info: Creating new writing system for \"x-ame\".", rgsLog[0]);
			Assert.AreEqual("data stream:0: Info: Creating new writing system for \"x-ame-fonipa\".", rgsLog[1]);
			Assert.AreEqual("data stream:0: Info: Creating new writing system for \"la\".", rgsLog[3]);
			Assert.AreEqual("data stream:0: Info: Creating new item with ws=\"en\", abbr=\"adj, indef art\", and name=\"indefinite article\" in the Parts of Speech list.", rgsLog[2]);
			Assert.AreEqual("data stream:0: Info: Creating new item with ws=\"en\", abbr=\"\", and name=\"Irregularly Inflected Form\" in the *** list.", rgsLog[4]);
			Assert.AreEqual("data stream:0: Info: Creating new item with ws=\"en\", abbr=\"vi\", and name=\"intransitive verb\" in the Parts of Speech list.", rgsLog[5]);
			Assert.AreEqual("data stream:0: Info: Creating new item with ws=\"en\", abbr=\"aux\", and name=\"auxiliary verb\" in the Parts of Speech list.", rgsLog[6]);
			Assert.AreEqual("data stream:0: Info: Creating new item with ws=\"en\", abbr=\"prim\", and name=\"primary\" in the *** list.", rgsLog[7]);
			Assert.AreEqual("data stream:0: Info: Creating new item with ws=\"en\", abbr=\"sec\", and name=\"secondary\" in the *** list.", rgsLog[8]);
			Assert.AreEqual("data stream:0: Info: Creating new item with ws=\"en\", abbr=\"\", and name=\"Text Segment\" in the *** list.", rgsLog[9]);
			Assert.AreEqual("data stream:0: Info: Creating new item with ws=\"en\", abbr=\"\", and name=\"Free Translation\" in the *** list.", rgsLog[10]);
			Assert.AreEqual("data stream:0: Info: Creating new item with ws=\"en\", abbr=\"\", and name=\"Wordform In Context\" in the *** list.", rgsLog[11]);
		}
		}

		private void CheckAnnotations(int wsEn, int wsAme)
		{
			IStTxtPara para = m_cache.LangProject.TextsOC.ToArray()[0].ContentsOA.ParagraphsOS[1] as IStTxtPara;
			IWfiWordformRepository repoWfi = m_cache.ServiceLocator.GetInstance<IWfiWordformRepository>();
			Assert.AreEqual(2, repoWfi.Count);
			IWfiWordform wfiAn = repoWfi.GetMatchingWordform(wsAme, "an");
			IWfiAnalysis anal = wfiAn.AnalysesOC.ToArray()[0];
			IWfiGloss gloss = anal.MeaningsOC.ToArray()[0];
			IWfiWordform wfiApple = repoWfi.GetMatchingWordform(wsAme, "apple");

			ICmBaseAnnotationRepository repoBase = m_cache.ServiceLocator.GetInstance<ICmBaseAnnotationRepository>();
			Assert.AreEqual(3, repoBase.Count);
			List<ICmBaseAnnotation> rgBase = new List<ICmBaseAnnotation>(3);
			rgBase.AddRange(repoBase.AllInstances());
			Assert.AreEqual(para, rgBase[0].BeginObjectRA);
			Assert.AreEqual(para, rgBase[0].EndObjectRA);
			Assert.AreEqual(StTxtParaTags.kflidContents, rgBase[0].Flid);
			Assert.AreEqual("LLImport", rgBase[0].CompDetails);
			Assert.AreEqual(1, rgBase[0].BeginOffset);
			Assert.AreEqual(0, rgBase[0].EndOffset);
			Assert.AreEqual("Text Segment", rgBase[0].AnnotationTypeRA.Name.get_String(wsEn).Text);
			Assert.IsNull(rgBase[0].InstanceOfRA);

			Assert.AreEqual(para, rgBase[1].BeginObjectRA);
			Assert.AreEqual(para, rgBase[1].EndObjectRA);
			Assert.AreEqual(StTxtParaTags.kflidContents, rgBase[1].Flid);
			Assert.AreEqual("LLImport", rgBase[1].CompDetails);
			Assert.AreEqual(1, rgBase[1].BeginOffset);
			Assert.AreEqual(2, rgBase[1].EndOffset);
			Assert.AreEqual("Wordform In Context", rgBase[1].AnnotationTypeRA.Name.get_String(wsEn).Text);
			Assert.AreEqual(gloss, rgBase[1].InstanceOfRA);

			Assert.AreEqual(para, rgBase[2].BeginObjectRA);
			Assert.AreEqual(para, rgBase[2].EndObjectRA);
			Assert.AreEqual(StTxtParaTags.kflidContents, rgBase[2].Flid);
			Assert.AreEqual("LLImport", rgBase[2].CompDetails);
			Assert.AreEqual(2, rgBase[2].BeginOffset);
			Assert.AreEqual(5, rgBase[2].EndOffset);
			Assert.AreEqual("Wordform In Context", rgBase[2].AnnotationTypeRA.Name.get_String(wsEn).Text);
			Assert.AreEqual(wfiApple, rgBase[2].InstanceOfRA);

			ICmIndirectAnnotationRepository repoIndirect = m_cache.ServiceLocator.GetInstance<ICmIndirectAnnotationRepository>();
			Assert.AreEqual(1, repoIndirect.Count);
			List<ICmIndirectAnnotation> rgIndir = new List<ICmIndirectAnnotation>(1);
			rgIndir.AddRange(repoIndirect.AllInstances());
			Assert.AreEqual(1, rgIndir[0].AppliesToRS.Count);
			Assert.AreEqual(rgBase[0], rgIndir[0].AppliesToRS[0]);
			Assert.AreEqual("Free Translation", rgIndir[0].AnnotationTypeRA.Name.get_String(wsEn).Text);
			Assert.AreEqual(1, rgIndir[0].Comment.StringCount);
			ITsString tss = m_cache.TsStrFactory.MakeString("A free translation for an apple.", wsEn);
			Assert.IsTrue(tss.Equals(rgIndir[0].Comment.get_String(wsEn)));
			Assert.AreEqual("LLImport", rgIndir[0].CompDetails);
		}

		private void CheckWfiWordforms(int wsEn, int wsAme)
		{
			IWfiWordformRepository repoWfi = m_cache.ServiceLocator.GetInstance<IWfiWordformRepository>();
			Assert.AreEqual(2, repoWfi.Count);
			IWfiWordform wfiAn = repoWfi.GetMatchingWordform(wsAme, "an");
			Assert.IsNotNull(wfiAn);
			Assert.AreEqual(1, wfiAn.AnalysesOC.Count);
			IWfiAnalysis anal = wfiAn.AnalysesOC.ToArray()[0];
			VerifyWfiAnalysis(wsEn, anal);

			IWfiWordform wfiApple = repoWfi.GetMatchingWordform(wsAme, "apple");
			Assert.IsNotNull(wfiApple);
			Assert.AreEqual(0, wfiApple.AnalysesOC.Count);
		}

		private void VerifyWfiAnalysis(int wsEn, IWfiAnalysis anal)
		{
			Assert.AreEqual("indefinite article", anal.CategoryRA.Name.get_String(wsEn).Text);
			Assert.AreEqual(1, anal.MorphBundlesOS.Count);
			IWfiMorphBundle mb = anal.MorphBundlesOS[0];
			ILexEntry entry = m_cache.LangProject.LexDbOA.Entries.ToArray()[0];
			Assert.AreEqual(entry.MorphoSyntaxAnalysesOC.ToArray()[0], mb.MsaRA);
			Assert.AreEqual(entry.LexemeFormOA, mb.MorphRA);
			Assert.AreEqual(entry.SensesOS[0].SensesOS[2], mb.SenseRA);
			Assert.AreEqual(1, anal.MeaningsOC.Count);
			IWfiGloss gloss = anal.MeaningsOC.ToArray()[0];
			Assert.AreEqual(0, gloss.Form.StringCount);
		}

		private void CheckTheAgent(ICmAgent agent, int wsAme)
		{
			Assert.AreEqual(1, agent.Name.StringCount);
			int wsEn = m_cache.WritingSystemFactory.GetWsFromStr("en");
			Assert.AreEqual("default user", agent.Name.get_String(wsEn).Text);
			Assert.IsTrue(agent.Human);

			IWfiWordformRepository repoWfi = m_cache.ServiceLocator.GetInstance<IWfiWordformRepository>();
			IWfiWordform wfAn = repoWfi.GetMatchingWordform(wsAme, "an");
			IWfiAnalysis anal = wfAn.AnalysesOC.ToArray()[0];
			Assert.AreEqual(Opinions.approves, anal.GetAgentOpinion(agent));
		}

		private void CheckTheText(IText text)
		{
			Assert.AreEqual(1, text.Name.StringCount);
			int wsAme = m_cache.WritingSystemFactory.GetWsFromStr("x-ame");
			Assert.AreEqual("Life begins each morning!: Praise the Lord!", text.Name.get_String(wsAme).Text);
			Assert.AreEqual(2, text.ContentsOA.ParagraphsOS.Count);
			IStTxtPara para = text.ContentsOA.ParagraphsOS[0] as IStTxtPara;
			Assert.IsNotNull(para);
			string sPara1 = para.Contents.Text;
			Assert.IsTrue(sPara1.StartsWith("Whether one is 20, 40, or 60;"));
			Assert.IsTrue(sPara1.EndsWith("Life Begins Each Morning!"));
			Assert.AreEqual(0, para.TranslationsOC.Count);
			Assert.IsNull(para.StyleRules);
			para = text.ContentsOA.ParagraphsOS[1] as IStTxtPara;
			Assert.IsNotNull(para);
			ITsString tss = m_cache.TsStrFactory.MakeString("an apple", wsAme);
			Assert.IsTrue(tss.Equals(para.Contents));
			Assert.AreEqual(0, para.TranslationsOC.Count);
			Assert.IsNull(para.StyleRules);
		}

		private void CheckFirstEntry(ILexEntry le, int wsEn, int wsAme)
		{
			string sFwSrcDir = DirectoryFinder.FwSourceDirectory;
			Assert.AreEqual(1, le.LexemeFormOA.Form.StringCount);
			Assert.AreEqual("an", le.LexemeFormOA.Form.get_String(wsAme).Text);
			Assert.AreEqual("root", le.LexemeFormOA.MorphTypeRA.Name.get_String(wsEn).Text);
			Assert.AreEqual(1, le.AlternateFormsOS.Count);
			Assert.AreEqual(1, le.AlternateFormsOS[0].Form.StringCount);
			Assert.AreEqual("a", le.AlternateFormsOS[0].Form.get_String(wsAme).Text);
			Assert.AreEqual("root", le.AlternateFormsOS[0].MorphTypeRA.Name.get_String(wsEn).Text);
			Assert.AreEqual(1, le.PronunciationsOS.Count);
			int wsIPA = m_cache.WritingSystemFactory.GetWsFromStr("x-ame-fonipa");
			Assert.AreEqual(1, le.PronunciationsOS[0].Form.StringCount);
			Assert.AreEqual("an", le.PronunciationsOS[0].Form.get_String(wsIPA).Text);
			Assert.AreEqual(1, le.PronunciationsOS[0].MediaFilesOS.Count);
			Assert.AreEqual(String.Format("{1}{0}FDO{0}FDOTests{0}TestData{0}house.wav", Path.DirectorySeparatorChar, sFwSrcDir),
				le.PronunciationsOS[0].MediaFilesOS[0].MediaFileRA.InternalPath);
			Assert.AreEqual(1, le.MorphoSyntaxAnalysesOC.Count);
			IMoStemMsa msa = le.MorphoSyntaxAnalysesOC.ToArray()[0] as IMoStemMsa;
			Assert.IsNotNull(msa, "MorphoSyntaxAnalysis is IMoStemMsa");
			Assert.AreEqual("indefinite article", msa.PartOfSpeechRA.Name.get_String(wsEn).Text);
			Assert.AreEqual(1, le.SensesOS.Count);
			Assert.AreEqual("sense group heading", le.SensesOS[0].Definition.get_String(wsEn).Text);
			Assert.AreEqual(msa, le.SensesOS[0].MorphoSyntaxAnalysisRA);
			Assert.AreEqual(3, le.SensesOS[0].SensesOS.Count);
			ILexSense sub = le.SensesOS[0].SensesOS[0];
			Assert.AreEqual(msa, sub.MorphoSyntaxAnalysisRA);
			Assert.AreEqual(1, sub.Gloss.StringCount);
			Assert.AreEqual("one", sub.Gloss.get_String(wsEn).Text);
			Assert.AreEqual(1, sub.Definition.StringCount);
			Assert.IsTrue(m_cache.TsStrFactory.MakeString("one; one sort of", wsEn).Equals(
				sub.Definition.get_String(wsEn)));
			sub = le.SensesOS[0].SensesOS[1];
			Assert.AreEqual(msa, sub.MorphoSyntaxAnalysisRA);
			Assert.AreEqual(1, sub.Gloss.StringCount);
			Assert.AreEqual("any", sub.Gloss.get_String(wsEn).Text);
			Assert.AreEqual(1, sub.Definition.StringCount);
			Assert.IsTrue(m_cache.TsStrFactory.MakeString("each; any one", wsEn).Equals(
				sub.Definition.get_String(wsEn)));
			sub = le.SensesOS[0].SensesOS[2];
			Assert.AreEqual(msa, sub.MorphoSyntaxAnalysisRA);
			Assert.AreEqual(1, sub.Gloss.StringCount);
			Assert.AreEqual("per", sub.Gloss.get_String(wsEn).Text);
			Assert.AreEqual(1, sub.Definition.StringCount);
			Assert.IsTrue(m_cache.TsStrFactory.MakeString("to each; in each; for each;", wsEn).Equals(
				sub.Definition.get_String(wsEn)));
			int wsLa = m_cache.WritingSystemFactory.GetWsFromStr("la");
			Assert.IsTrue(m_cache.TsStrFactory.MakeString("Latin term", wsLa).Equals(sub.ScientificName));
			Assert.AreEqual(1, sub.PicturesOS.Count);
			ICmPicture pict = sub.PicturesOS[0];
			Assert.AreEqual(String.Format("{1}{0}FDO{0}FDOTests{0}TestData{0}penguin.jpg", Path.DirectorySeparatorChar, sFwSrcDir),
				pict.PictureFileRA.InternalPath);
			Assert.AreEqual(1, pict.Caption.StringCount);
			Assert.IsTrue(m_cache.TsStrFactory.MakeString("English caption", wsEn).Equals(
				pict.Caption.get_String(wsEn)));
			Assert.AreEqual(0, le.EntryRefsOS.Count);
		}

		private void CheckSecondEntry(ILexEntry le, int wsEn, int wsAme, ILexEntry leRef)
		{
			Assert.AreEqual(0, le.SensesOS.Count);
			Assert.AreEqual(1, le.MorphoSyntaxAnalysesOC.Count);
			Assert.AreEqual(1, le.LexemeFormOA.Form.StringCount);
			Assert.AreEqual("am", le.LexemeFormOA.Form.get_String(wsAme).Text);
			Assert.AreEqual("stem", le.LexemeFormOA.MorphTypeRA.Name.get_String(wsEn).Text);
			IMoStemMsa msa = le.MorphoSyntaxAnalysesOC.ToArray()[0] as IMoStemMsa;
			Assert.IsNotNull(msa);
			Assert.IsNull(msa.PartOfSpeechRA);
			Assert.AreEqual(1, le.EntryRefsOS.Count);
			ILexEntryRef ler = le.EntryRefsOS[0];
			Assert.AreEqual(0, ler.ComplexEntryTypesRS.Count);
			Assert.AreEqual(1, ler.VariantEntryTypesRS.Count);
			Assert.AreEqual("Irregularly Inflected Form", ler.VariantEntryTypesRS[0].Name.get_String(wsEn).Text);
			Assert.AreEqual(1, ler.ComponentLexemesRS.Count);
			Assert.AreEqual(leRef, ler.ComponentLexemesRS[0]);
			Assert.AreEqual(1, ler.Summary.StringCount);
			Assert.IsTrue(m_cache.TsStrFactory.MakeString("1ps PRES INDIC", wsEn).Equals(
				ler.Summary.get_String(wsEn)));
			Assert.AreEqual(0, le.AlternateFormsOS.Count);
		}

		private void CheckThirdEntry(ILexEntry le, int wsEn, int wsAme)
		{
			Assert.AreEqual(1, le.LexemeFormOA.Form.StringCount);
			Assert.AreEqual("be", le.LexemeFormOA.Form.get_String(wsAme).Text);
			Assert.AreEqual("stem", le.LexemeFormOA.MorphTypeRA.Name.get_String(wsEn).Text);

			Assert.AreEqual(1, le.AlternateFormsOS.Count);
			Assert.AreEqual(1, le.AlternateFormsOS[0].Form.StringCount);
			Assert.AreEqual("is", le.AlternateFormsOS[0].Form.get_String(wsAme).Text);
			Assert.AreEqual(le.LexemeFormOA.MorphTypeRA, le.AlternateFormsOS[0].MorphTypeRA);

			Assert.AreEqual(3, le.MorphoSyntaxAnalysesOC.Count);
			IMoMorphSynAnalysis[] rgmsa = le.MorphoSyntaxAnalysesOC.ToArray();
			IMoStemMsa msa = rgmsa[0] as IMoStemMsa;
			Assert.IsNotNull(msa);
			Assert.AreEqual("intransitive verb", msa.PartOfSpeechRA.Name.get_String(wsEn).Text);
			msa = rgmsa[1] as IMoStemMsa;
			Assert.IsNotNull(msa);
			Assert.AreEqual("auxiliary verb", msa.PartOfSpeechRA.Name.get_String(wsEn).Text);
			msa = rgmsa[2] as IMoStemMsa;
			Assert.IsNotNull(msa);
			Assert.AreEqual("<Not Sure>", msa.InterlinearName);

			Assert.AreEqual(2, le.SensesOS.Count);
			ILexSense sense = le.SensesOS[0];
			Assert.AreEqual(rgmsa[0], sense.MorphoSyntaxAnalysisRA);
			Assert.AreEqual(2, sense.SensesOS.Count);
			ILexSense sub = sense.SensesOS[0];
			ITsString tss = m_cache.TsStrFactory.MakeString("As a substantive verb:", wsEn);
			Assert.IsTrue(tss.Equals(sub.Definition.get_String(wsEn)));
			Assert.AreEqual(5, sub.SensesOS.Count);
			ILexSense subsub = sub.SensesOS[0];
			Assert.AreEqual(0, subsub.SensesOS.Count);
			Assert.AreEqual(rgmsa[0], subsub.MorphoSyntaxAnalysisRA);
			Assert.AreEqual(1, subsub.Gloss.StringCount);
			Assert.AreEqual("exist", subsub.Gloss.get_String(wsEn).Text);
			Assert.AreEqual(1, subsub.Definition.StringCount);
			tss = m_cache.TsStrFactory.MakeString("to exist; live", wsEn);
			Assert.IsTrue(tss.Equals(subsub.Definition.get_String(wsEn)));
			subsub = sub.SensesOS[1];
			Assert.AreEqual(0, subsub.SensesOS.Count);
			Assert.AreEqual(rgmsa[0], subsub.MorphoSyntaxAnalysisRA);
			Assert.AreEqual(1, subsub.Gloss.StringCount);
			Assert.AreEqual("happen", subsub.Gloss.get_String(wsEn).Text);
			Assert.AreEqual(1, subsub.Definition.StringCount);
			tss = m_cache.TsStrFactory.MakeString("to happen or occur", wsEn);
			Assert.IsTrue(tss.Equals(subsub.Definition.get_String(wsEn)));
			subsub = sub.SensesOS[2];
			Assert.AreEqual(0, subsub.SensesOS.Count);
			Assert.AreEqual(rgmsa[0], subsub.MorphoSyntaxAnalysisRA);
			Assert.AreEqual(1, subsub.Gloss.StringCount);
			Assert.AreEqual("remain", subsub.Gloss.get_String(wsEn).Text);
			Assert.AreEqual(1, subsub.Definition.StringCount);
			tss = m_cache.TsStrFactory.MakeString("to remain or continue", wsEn);
			Assert.IsTrue(tss.Equals(subsub.Definition.get_String(wsEn)));
			subsub = sub.SensesOS[3];
			Assert.AreEqual(0, subsub.SensesOS.Count);
			Assert.AreEqual(rgmsa[0], subsub.MorphoSyntaxAnalysisRA);
			Assert.AreEqual(1, subsub.Gloss.StringCount);
			Assert.AreEqual("belong", subsub.Gloss.get_String(wsEn).Text);
			Assert.AreEqual(1, subsub.Definition.StringCount);
			tss = m_cache.TsStrFactory.MakeString("to come to; belong", wsEn);
			Assert.IsTrue(tss.Equals(subsub.Definition.get_String(wsEn)));
			subsub = sub.SensesOS[4];
			Assert.AreEqual(rgmsa[0], subsub.MorphoSyntaxAnalysisRA);
			Assert.AreEqual(0, subsub.SensesOS.Count);
			Assert.AreEqual(1, subsub.Gloss.StringCount);
			Assert.AreEqual("have place", subsub.Gloss.get_String(wsEn).Text);
			Assert.AreEqual(1, subsub.Definition.StringCount);
			tss = m_cache.TsStrFactory.MakeString("to have a place or position", wsEn);
			Assert.IsTrue(tss.Equals(subsub.Definition.get_String(wsEn)));
			sub = sense.SensesOS[1];
			tss = m_cache.TsStrFactory.MakeString("As a copula:", wsEn);
			Assert.IsTrue(tss.Equals(sub.Definition.get_String(wsEn)));
			Assert.AreEqual(2, sub.SensesOS.Count);
			subsub = sub.SensesOS[0];
			Assert.AreEqual("primary", subsub.SenseTypeRA.Name.get_String(wsEn).Text);
			Assert.AreEqual(2, subsub.SensesOS.Count);
			ILexSense subsubsub = subsub.SensesOS[0];
			Assert.AreEqual(0, subsubsub.SensesOS.Count);
			Assert.AreEqual(rgmsa[0], subsubsub.MorphoSyntaxAnalysisRA);
			Assert.AreEqual(1, subsubsub.Gloss.StringCount);
			Assert.AreEqual("is", subsubsub.Gloss.get_String(wsEn).Text);
			Assert.AreEqual(1, subsubsub.Definition.StringCount);
			tss = m_cache.TsStrFactory.MakeString("the linker between a subject and a predicate nominative, adjective, or pronoun so as to express attribution", wsEn);
			Assert.IsTrue(tss.Equals(subsubsub.Definition.get_String(wsEn)));
			subsubsub = subsub.SensesOS[1];
			Assert.AreEqual(0, subsubsub.SensesOS.Count);
			Assert.AreEqual(rgmsa[0], subsubsub.MorphoSyntaxAnalysisRA);
			Assert.AreEqual(1, subsubsub.Gloss.StringCount);
			Assert.AreEqual("equals", subsubsub.Gloss.get_String(wsEn).Text);
			Assert.AreEqual(1, subsubsub.Definition.StringCount);
			tss = m_cache.TsStrFactory.MakeString("the linker between a subject and a predicate nominative, adjective, or pronoun so as to express identity", wsEn);
			Assert.IsTrue(tss.Equals(subsubsub.Definition.get_String(wsEn)));
			subsub = sub.SensesOS[1];
			Assert.AreEqual("secondary", subsub.SenseTypeRA.Name.get_String(wsEn).Text);
			Assert.AreEqual(3, subsub.SensesOS.Count);
			subsubsub = subsub.SensesOS[0];
			Assert.AreEqual(0, subsubsub.SensesOS.Count);
			Assert.AreEqual(rgmsa[0], subsubsub.MorphoSyntaxAnalysisRA);
			Assert.AreEqual(1, subsubsub.Gloss.StringCount);
			Assert.AreEqual("costs", subsubsub.Gloss.get_String(wsEn).Text);
			Assert.AreEqual(1, subsubsub.Definition.StringCount);
			tss = m_cache.TsStrFactory.MakeString("the linker between a subject and a predicate nominative, adjective, or pronoun so as to express value", wsEn);
			Assert.IsTrue(tss.Equals(subsubsub.Definition.get_String(wsEn)));
			subsubsub = subsub.SensesOS[1];
			Assert.AreEqual(0, subsubsub.SensesOS.Count);
			Assert.AreEqual(rgmsa[0], subsubsub.MorphoSyntaxAnalysisRA);
			Assert.AreEqual(1, subsubsub.Gloss.StringCount);
			Assert.AreEqual("causes", subsubsub.Gloss.get_String(wsEn).Text);
			Assert.AreEqual(1, subsubsub.Definition.StringCount);
			tss = m_cache.TsStrFactory.MakeString("the linker between a subject and a predicate nominative, adjective, or pronoun so as to express cause", wsEn);
			Assert.IsTrue(tss.Equals(subsubsub.Definition.get_String(wsEn)));
			subsubsub = subsub.SensesOS[2];
			Assert.AreEqual(0, subsubsub.SensesOS.Count);
			Assert.AreEqual(rgmsa[0], subsubsub.MorphoSyntaxAnalysisRA);
			Assert.AreEqual(1, subsubsub.Gloss.StringCount);
			Assert.AreEqual("signify", subsubsub.Gloss.get_String(wsEn).Text);
			Assert.AreEqual(1, subsubsub.Definition.StringCount);
			tss = m_cache.TsStrFactory.MakeString("the linker between a subject and a predicate nominative, adjective, or pronoun so as to express signification", wsEn);
			Assert.IsTrue(tss.Equals(subsubsub.Definition.get_String(wsEn)));

			sense = le.SensesOS[1];
			Assert.AreEqual(rgmsa[1], sense.MorphoSyntaxAnalysisRA);
			Assert.AreEqual(4, sense.SensesOS.Count);
			sub = sense.SensesOS[0];
			Assert.AreEqual(rgmsa[1], sub.MorphoSyntaxAnalysisRA);
			Assert.AreEqual(1, sub.Gloss.StringCount);
			Assert.AreEqual("PASS", sub.Gloss.get_String(wsEn).Text);
			Assert.AreEqual(1, sub.Definition.StringCount);
			tss = m_cache.TsStrFactory.MakeString("used with the past participle of a transitive verb to form the passive voice", wsEn);
			Assert.IsTrue(tss.Equals(sub.Definition.get_String(wsEn)));
			Assert.AreEqual(0, sub.SensesOS.Count);
			sub = sense.SensesOS[1];
			Assert.AreEqual(rgmsa[1], sub.MorphoSyntaxAnalysisRA);
			Assert.AreEqual(1, sub.Gloss.StringCount);
			Assert.AreEqual("PERF", sub.Gloss.get_String(wsEn).Text);
			Assert.AreEqual(1, sub.Definition.StringCount);
			tss = m_cache.TsStrFactory.MakeString("used with the past participle of certain intransitive verbs to form a perfect tense", wsEn);
			Assert.IsTrue(tss.Equals(sub.Definition.get_String(wsEn)));
			Assert.AreEqual(0, sub.SensesOS.Count);
			sub = sense.SensesOS[2];
			Assert.AreEqual(rgmsa[1], sub.MorphoSyntaxAnalysisRA);
			Assert.AreEqual(1, sub.Gloss.StringCount);
			Assert.AreEqual("CONT", sub.Gloss.get_String(wsEn).Text);
			Assert.AreEqual(1, sub.Definition.StringCount);
			tss = m_cache.TsStrFactory.MakeString("used with the present participle of another verb to express continuation", wsEn);
			Assert.IsTrue(tss.Equals(sub.Definition.get_String(wsEn)));
			Assert.AreEqual(0, sub.SensesOS.Count);
			sub = sense.SensesOS[3];
			Assert.IsNotNull(sub.MorphoSyntaxAnalysisRA);
			Assert.AreEqual("<Not Sure>", sub.MorphoSyntaxAnalysisRA.InterlinearName);
			Assert.AreEqual(1, sub.Definition.StringCount);
			tss = m_cache.TsStrFactory.MakeString("IRREALIS", wsEn);
			Assert.IsTrue(tss.Equals(sub.Definition.get_String(wsEn)));
			Assert.AreEqual(4, sub.SensesOS.Count);
			subsub = sub.SensesOS[0];
			Assert.AreEqual(rgmsa[1], subsub.MorphoSyntaxAnalysisRA);
			Assert.AreEqual(1, subsub.Gloss.StringCount);
			Assert.AreEqual("IRR FUT", subsub.Gloss.get_String(wsEn).Text);
			Assert.AreEqual(1, subsub.Definition.StringCount);
			tss = m_cache.TsStrFactory.MakeString("used with the present participle or infinitive of another verb to express futurity", wsEn);
			Assert.IsTrue(tss.Equals(subsub.Definition.get_String(wsEn)));
			Assert.AreEqual(0, subsub.SensesOS.Count);
			subsub = sub.SensesOS[1];
			Assert.AreEqual(rgmsa[1], subsub.MorphoSyntaxAnalysisRA);
			Assert.AreEqual(1, subsub.Gloss.StringCount);
			Assert.AreEqual("IRR OBLIG", subsub.Gloss.get_String(wsEn).Text);
			Assert.AreEqual(1, subsub.Definition.StringCount);
			tss = m_cache.TsStrFactory.MakeString("used with the present participle or infinitive of another verb to express obligation", wsEn);
			Assert.IsTrue(tss.Equals(subsub.Definition.get_String(wsEn)));
			Assert.AreEqual(0, subsub.SensesOS.Count);
			subsub = sub.SensesOS[2];
			Assert.AreEqual(rgmsa[1], subsub.MorphoSyntaxAnalysisRA);
			Assert.AreEqual(1, subsub.Gloss.StringCount);
			Assert.AreEqual("IRR POSS", subsub.Gloss.get_String(wsEn).Text);
			Assert.AreEqual(1, subsub.Definition.StringCount);
			tss = m_cache.TsStrFactory.MakeString("used with the present participle or infinitive of another verb to express possibility", wsEn);
			Assert.IsTrue(tss.Equals(subsub.Definition.get_String(wsEn)));
			Assert.AreEqual(0, subsub.SensesOS.Count);
			subsub = sub.SensesOS[3];
			Assert.AreEqual(rgmsa[1], subsub.MorphoSyntaxAnalysisRA);
			Assert.AreEqual(1, subsub.Gloss.StringCount);
			Assert.AreEqual("IRR INT", subsub.Gloss.get_String(wsEn).Text);
			Assert.AreEqual(1, subsub.Definition.StringCount);
			tss = m_cache.TsStrFactory.MakeString("used with the present participle or infinitive of another verb to express intention", wsEn);
			Assert.IsTrue(tss.Equals(subsub.Definition.get_String(wsEn)));
			Assert.AreEqual(0, subsub.SensesOS.Count);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method ImportData() on yet more data.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void ImportData4()
		{
			DateTime dtLexOrig = m_cache.LangProject.LexDbOA.DateCreated;
			TimeSpan span = new TimeSpan(dtLexOrig.Ticks - m_now.Ticks);
			Assert.LessOrEqual(span.TotalMinutes, 1.0);
			// should be only a second or two
			XmlImportData xid = new XmlImportData(m_cache);
			using (var rdr = new StringReader(
				"<LexDb>" + Environment.NewLine +
				"<Entries>" + Environment.NewLine +
				"<LexEntry>" + Environment.NewLine +
				"<LexemeForm>" + Environment.NewLine +
				"<MoStemAllomorph>" + Environment.NewLine +
				"<MorphType>" + Environment.NewLine +
				"<Link ws=\"en\" name=\"phrase\" />" + Environment.NewLine +
				"</MorphType>" + Environment.NewLine +
				"<Form>" + Environment.NewLine +
				"<AUni ws=\"x-kal\">inline test entry</AUni>" + Environment.NewLine +
				"</Form>" + Environment.NewLine +
				"</MoStemAllomorph>" + Environment.NewLine +
				"</LexemeForm>" + Environment.NewLine +
				"<Etymology>" + Environment.NewLine +
				"<LexEtymology>" + Environment.NewLine +
				"<Source>" + Environment.NewLine +
				"<Uni>|bBold|r regular |iItalic|r|fw{greek}ignored*bold*words|b|ibold-italic|r|r|bBOLD  |r</Uni>" + Environment.NewLine +
				"</Source>" + Environment.NewLine +
				"</LexEtymology>" + Environment.NewLine +
				"</Etymology>" + Environment.NewLine +
				"<Senses>" + Environment.NewLine +
				"<LexSense>" + Environment.NewLine +
				"<Definition>" + Environment.NewLine +
				"<AStr ws=\"en\">" + Environment.NewLine +
				"<Run ws=\"en\" namedStyle=\"Heavy\">Bold</Run>" + Environment.NewLine +
				"<Run ws=\"en\"> regular </Run>" + Environment.NewLine +
				"<Run ws=\"en\" namedStyle=\"Emphasized Text\">Italic</Run>" + Environment.NewLine +
				"<Run ws=\"es\">greek</Run>" + Environment.NewLine +
				"<Run ws=\"es\">ignored</Run>" + Environment.NewLine +
				"<Run ws=\"en\" namedStyle=\"Emphasized Text\">bold</Run>" + Environment.NewLine +
				"<Run ws=\"en\" namedStyle=\"Emphasized Text\">words</Run>" + Environment.NewLine +
				"<Run ws=\"en\" namedStyle=\"Emphasized Text\">bold-italic</Run>" + Environment.NewLine +
				"<Run ws=\"en\" namedStyle=\"Heavy\">BOLD </Run>" + Environment.NewLine +
				"</AStr>" + Environment.NewLine +
				"</Definition>" + Environment.NewLine +
				"<Gloss>" + Environment.NewLine +
				"<AUni ws=\"es\">|bBold|r regular |iItalic|r|fw{greek}ignored*bold*words|b|ibold-italic|r|r|bBOLD |r</AUni>" + Environment.NewLine +
				"</Gloss>" + Environment.NewLine +
				"<Examples>" + Environment.NewLine +
				"<LexExampleSentence>" + Environment.NewLine +
				"<Translations />" + Environment.NewLine +
				"<Reference>" + Environment.NewLine +
				"<Str>" + Environment.NewLine +
				"<Run ws=\"en\" namedStyle=\"Heavy\">Bold</Run>" + Environment.NewLine +
				"<Run ws=\"en\"> regular </Run>" + Environment.NewLine +
				"<Run ws=\"en\" namedStyle=\"Emphasized Text\">Italic</Run>" + Environment.NewLine +
				"<Run ws=\"es\">greek</Run>" + Environment.NewLine +
				"<Run ws=\"es\">ignored</Run>" + Environment.NewLine +
				"<Run ws=\"en\" namedStyle=\"Emphasized Text\">bold</Run>" + Environment.NewLine +
				"<Run ws=\"en\" namedStyle=\"Emphasized Text\">words</Run>" + Environment.NewLine +
				"<Run ws=\"en\" namedStyle=\"Emphasized Text\">bold-italic</Run>" + Environment.NewLine +
				"<Run ws=\"en\" namedStyle=\"Heavy\">BOLD  </Run>" + Environment.NewLine +
				"</Str>" + Environment.NewLine +
				"</Reference>" + Environment.NewLine +
				"</LexExampleSentence>" + Environment.NewLine +
				"</Examples>" + Environment.NewLine +
				"</LexSense>" + Environment.NewLine +
				"</Senses>" + Environment.NewLine +
				"</LexEntry>" + Environment.NewLine +
				"</Entries>" + Environment.NewLine +
				"</LexDb>" + Environment.NewLine))
			{
			Assert.AreEqual(0, m_cache.LangProject.LexDbOA.Entries.Count(), "The lexicon starts out empty.");
			Assert.AreEqual(0, m_cache.LangProject.AnthroListOA.PossibilitiesOS.Count);
			Assert.AreEqual(0, m_cache.LangProject.SemanticDomainListOA.PossibilitiesOS.Count);
			Assert.AreEqual(0, m_cache.LangProject.PartsOfSpeechOA.PossibilitiesOS.Count);
			StringBuilder sbLog = new StringBuilder();
			bool fOk = xid.ImportData(rdr, new StringWriter(sbLog), null);
			Assert.IsTrue(fOk, "Import of one lexical entry succeeded.");
			Assert.AreEqual(1, m_cache.LangProject.LexDbOA.Entries.Count());
			ILexEntry le = m_cache.LangProject.LexDbOA.Entries.ToArray()[0];
			int wsKal = m_cache.WritingSystemFactory.GetWsFromStr("x-kal");
			Assert.AreEqual("inline test entry", le.LexemeFormOA.Form.get_String(wsKal).Text);
			Assert.AreEqual(MoMorphTypeTags.kguidMorphPhrase, le.LexemeFormOA.MorphTypeRA.Guid);
			Assert.AreEqual(1, le.SensesOS.Count);
			int wsEn = m_cache.WritingSystemFactory.GetWsFromStr("en");
			Assert.AreEqual("Bold regular Italicgreekignoredboldwordsbold-italicBOLD ", le.SensesOS[0].Definition.get_String(wsEn).Text);
			Assert.AreEqual(0, le.SensesOS[0].SensesOS.Count);

			string sLog = sbLog.ToString();
			Assert.IsFalse(String.IsNullOrEmpty(sLog), "There should be some log information!");
			string[] rgsLog = sLog.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
			Assert.LessOrEqual(1, rgsLog.Length);
			Assert.AreEqual("data stream:0: Info: Creating new writing system for \"x-kal\".", rgsLog[0]);
		}
	}
}
}
