// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ParseFilerProcessingTests.cs
// Responsibility: Randy Regnier
// Last reviewed:
//
// <remarks>
// Implements the ParseFilerProcessingTests unit tests.
// </remarks>
// buildtest ParseFiler-nodep
// --------------------------------------------------------------------------------------------
using System;
using System.IO;
using System.Data.SqlClient;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.WordWorks.Parser;

namespace ParserCoreTests
{
	/// <summary>
	/// Summary description for ParseFilerProcessingTests.
	/// </summary>
	[TestFixture]
	public class ParseFilerProcessingTests
	{
		#region Data Members

		protected string m_databaseName;
		protected ParseFiler m_filer;
		protected SqlConnection m_connection;
		protected List<int> m_newObjects;
		protected int m_vernacularWS;
		protected int m_wfiID;

		#endregion Data Members

		#region Non-test methods

		/// <summary>
		/// Constructor.
		/// </summary>
		public ParseFilerProcessingTests()
		{
			m_newObjects = new List<int>();
		}

		protected int AnalyzingAgentId
		{
			get
			{
				int agentId = 0;
				try
				{
					SqlCommand command = m_connection.CreateCommand();
					command.CommandText = "exec FindOrCreateCmAgent 'M3Parser', 0, 'Normal'";
					agentId = (int)command.ExecuteScalar();
				}
				catch
				{
					agentId = 0;
				}
				return agentId;
			}
		}

		protected void AddIdToList(int hvo)
		{
			if (!m_newObjects.Contains(hvo))
				m_newObjects.Add(hvo);
		}

		protected int CheckAnnotationSize(string form, int expectedSize, bool isStarting)
		{
			int wfID = GetWordform(form);
			AddIdToList(wfID);
			int actualSize = 0;
			SqlCommand command = m_connection.CreateCommand();
			command.CommandText = string.Format("select count(*) from CmBaseAnnotation"
				+ " where BeginObject={0}",
				wfID);
			object obj = command.ExecuteScalar();
			if (obj != null)
				actualSize = (int)obj;
			string msg = String.Format("Wrong number of {0} annotations for: {1}", isStarting ? "starting" : "ending", form);
			Assert.AreEqual(expectedSize, actualSize, msg);
			return wfID;
		}

		protected int CheckAnalysisSize(string form, int expectedSize, bool isStarting)
		{
			int wfID = GetWordform(form);
			AddIdToList(wfID);
			int actualSize = 0;
			SqlCommand command = m_connection.CreateCommand();
			command.CommandText = string.Format("select count(*) from WfiWordform_Analyses"
				+ " where Src={0}",
				wfID);
			object obj = command.ExecuteScalar();
			if (obj != null)
				actualSize = (int)obj;
			string msg = String.Format("Wrong number of {0} analyses for: {1}", isStarting ? "starting" : "ending", form);
			Assert.AreEqual(expectedSize, actualSize, msg);
			return wfID;
		}

		protected void CheckEvaluationSize(int analysisID, int expectedSize, bool isStarting, string additionalMessage)
		{
			int actualSize = 0;
			SqlCommand command = m_connection.CreateCommand();
			command.CommandText = String.Format("SELECT COUNT(*) FROM CmAgentEvaluation_"
				+ " WHERE Owner$ = {0} AND Target = {1}",
				AnalyzingAgentId, analysisID);
			object obj = command.ExecuteScalar();
			if (obj != null)
				actualSize = (int)obj;
			string msg = String.Format("Wrong number of {0} evaluations for analysis: {1} ({2})", isStarting ? "starting" : "ending", analysisID, additionalMessage);
			Assert.AreEqual(expectedSize, actualSize, msg);
		}

		protected int GetWordform(string form)
		{
			int wfID = 0;
			SqlCommand command = m_connection.CreateCommand();
			command.CommandText = String.Format("select Obj "
				+ "from WfiWordform_Form "
				+ "where Txt='{0}' and Ws={1}", form, m_vernacularWS.ToString());
			object obj = command.ExecuteScalar();
			if (obj == null)
			{
				command = m_connection.CreateCommand();
				command.CommandText = String.Format("declare @newKid int "
					+ "exec  MakeObj_WfiWordform {0}, '{1}', 0, 0, {2}, 5063001, null, @newKid output, null "
					+ "select @newKid",
					m_vernacularWS.ToString(), form, m_wfiID.ToString());
				wfID = (int)command.ExecuteScalar();
				AddIdToList(wfID);
			}
			else
				wfID = (int)obj;

			return wfID;
		}

		protected string MakeXML(string xmlFragment, bool isGoodXML)
		{
			string xmlData = "<?xml version='1.0' encoding='UTF-8' ?>\n"
				+ "<AnalyzingAgentResults>\n"
				+ "<AnalyzingAgent name='M3' human='false' version='Normal'>\n"
				+ "<StateInformation />\n"
				+ "</AnalyzingAgent>\n"
				+ "<WordSet>\n"
				+ xmlFragment;
			if (isGoodXML)
				xmlData += "</WordSet>\n"
					+ "</AnalyzingAgentResults>\n";
			return xmlData;
		}

		#endregion // Non-tests

		#region Setup and TearDown

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a ParseFiler.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		protected void SetUp()
		{
			string server = Environment.MachineName + "\\SILFW";
			string database = "TestLangProj";

			string cnxString = "Server=" + server
				+ "; Database=" + database
				+ "; User ID=FWDeveloper;"
				+ "Password=careful; Pooling=false;";
			m_connection = new SqlConnection(cnxString);
			m_connection.Open();
			SqlCommand command = m_connection.CreateCommand();
			command.CommandText = "select top 1 Dst "
				+ "from LangProject_CurVernWss "
				+ "order by Ord";
			m_vernacularWS = (int)command.ExecuteScalar();
			command = m_connection.CreateCommand();
			command.CommandText = "select top 1 Id "
				+ "from WordformInventory";
			m_wfiID = (int)command.ExecuteScalar();
			m_filer = new ParseFiler(m_connection, AnalyzingAgentId);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get rid of ParseFiler and cache after each test..
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		protected void TearDown()
		{
			SqlCommand command = m_connection.CreateCommand();
			foreach (int hvo in m_newObjects)
			{
				command.CommandText = string.Format("EXEC DeleteObjects '{0}'", hvo);
				command.ExecuteNonQuery();
			}
			m_newObjects.Clear();
			m_connection.Close();
		}

		#endregion Setup and TearDown

		#region Tests

		[Test]
		public void TooManyAnalyses()
		{
			int hvoBearTEST = CheckAnnotationSize("bearsTEST", 0, true);
			string xmlFragment = "<Wordform DbRef='" + hvoBearTEST.ToString() + "' Form='bearsTEST'>\n"
				+ "<Exception code='ReachedMaxAnalyses' totalAnalyses='448'/>\n"
				+ "</Wordform>";
			m_filer.ProcessParse(MakeXML(xmlFragment, true));
			CheckAnnotationSize("bearsTEST", 1, false);
		}

		[Test]
		public void BufferOverrun()
		{
			int hvoBearTEST = CheckAnnotationSize("bearsTEST", 0, true);
			string xmlFragment = "<Wordform DbRef='" + hvoBearTEST.ToString() + "' Form='bearsTEST'>\n"
				+ "<Exception code='ReachedMaxBufferSize' totalAnalyses='117'/>\n";
			m_filer.ProcessParse(MakeXML(xmlFragment, false));
			CheckAnnotationSize("bearsTEST", 1, false);
		}

		[Test]
		public void TwoAnalyses()
		{
			int hvoBearTEST = CheckAnalysisSize("bearsTEST", 0, true);
			string xmlFragment = "";
			using (FdoCache cache = FdoCache.Create("TestLangProj"))
			{
				ILexDb ldb = cache.LangProject.LexDbOA;

				// Noun
				ILexEntry bearN = (ILexEntry)ldb.EntriesOC.Add(new LexEntry());
				AddIdToList(bearN.Hvo);
				IMoStemAllomorph bearNForm = (IMoStemAllomorph)bearN.AlternateFormsOS.Append(new MoStemAllomorph());
				bearNForm.Form.VernacularDefaultWritingSystem = "bearNTEST";
				IMoStemMsa bearNMSA = (IMoStemMsa)bearN.MorphoSyntaxAnalysesOC.Add(new MoStemMsa());

				ILexEntry sPL = (ILexEntry)ldb.EntriesOC.Add(new LexEntry());
				AddIdToList(sPL.Hvo);
				IMoAffixAllomorph sPLForm = (IMoAffixAllomorph)sPL.AlternateFormsOS.Append(new MoAffixAllomorph());
				sPLForm.Form.VernacularDefaultWritingSystem = "sPLTEST";
				IMoInflAffMsa sPLMSA =
					(IMoInflAffMsa)sPL.MorphoSyntaxAnalysesOC.Add(new MoInflAffMsa());

				// Verb
				ILexEntry bearV = (ILexEntry)ldb.EntriesOC.Add(new LexEntry());
				AddIdToList(bearV.Hvo);
				IMoStemAllomorph bearVForm = (IMoStemAllomorph)bearV.AlternateFormsOS.Append(new MoStemAllomorph());
				bearVForm.Form.VernacularDefaultWritingSystem = "bearVTEST";
				IMoStemMsa bearVMSA = (IMoStemMsa)bearV.MorphoSyntaxAnalysesOC.Add(new MoStemMsa());

				ILexEntry sAGR = (ILexEntry)ldb.EntriesOC.Add(new LexEntry());
				AddIdToList(sAGR.Hvo);
				IMoAffixAllomorph sAGRForm = (IMoAffixAllomorph)sAGR.AlternateFormsOS.Append(new MoAffixAllomorph());
				sAGRForm.Form.VernacularDefaultWritingSystem = "sAGRTEST";
				IMoInflAffMsa sAGRMSA =
					(IMoInflAffMsa)sAGR.MorphoSyntaxAnalysesOC.Add(new MoInflAffMsa());


				xmlFragment = "<Wordform DbRef='" + hvoBearTEST.ToString() + "' Form='bearsTEST'>\n"
					+ "<WfiAnalysis>\n"
					+ "<Morphs>\n"
					+ "<Morph>\n"
					+ "<MoForm DbRef='" + bearNForm.Hvo.ToString() + "' Label='bearNTEST'/>\n"
					+ "<MSI DbRef='" + bearNMSA.Hvo.ToString() + "'/>\n"
					+ "</Morph>\n"
					+ "<Morph>\n"
					+ "<MoForm DbRef='" + sPLForm.Hvo.ToString() + "' Label='sPLTEST'/>\n"
					+ "<MSI DbRef='" + sPLMSA.Hvo.ToString() + "'/>\n"
					+ "</Morph>\n"
					+ "</Morphs>\n"
					+ "</WfiAnalysis>\n"
					+ "<WfiAnalysis>\n"
					+ "<Morphs>\n"
					+ "<Morph>\n"
					+ "<MoForm DbRef='" + bearVForm.Hvo.ToString() + "' Label='bearVTEST'/>\n"
					+ "<MSI DbRef='" + bearVMSA.Hvo.ToString() + "'/>\n"
					+ "</Morph>\n"
					+ "<Morph>\n"
					+ "<MoForm DbRef='" + sAGRForm.Hvo.ToString() + "' Label='sAGRTEST'/>\n"
					+ "<MSI DbRef='" + sAGRMSA.Hvo.ToString() + "'/>\n"
					+ "</Morph>\n"
					+ "</Morphs>\n"
					+ "</WfiAnalysis>\n"
					+ "</Wordform>\n";
			}
			m_filer.ProcessParse(MakeXML(xmlFragment, true));
			CheckAnalysisSize("bearsTEST", 2, false);
		}

		[Test]
		public void TwoWordforms()
		{
			int hvoBearTEST = CheckAnalysisSize("bearTEST", 0, true);
			int hvoBullTEST = CheckAnalysisSize("bullTEST", 0, true);
			string xmlFragment = "";
			using (FdoCache cache = FdoCache.Create("TestLangProj"))
			{
				ILexDb ldb = cache.LangProject.LexDbOA;

				// Bear
				ILexEntry bearN = (ILexEntry)ldb.EntriesOC.Add(new LexEntry());
				AddIdToList(bearN.Hvo);
				IMoStemAllomorph bearNForm = (IMoStemAllomorph)bearN.AlternateFormsOS.Append(new MoStemAllomorph());
				bearNForm.Form.VernacularDefaultWritingSystem = "bearNTEST";
				IMoStemMsa bearNMSA = (IMoStemMsa)bearN.MorphoSyntaxAnalysesOC.Add(new MoStemMsa());

				// Bull
				ILexEntry bullN = (ILexEntry)ldb.EntriesOC.Add(new LexEntry());
				AddIdToList(bullN.Hvo);
				IMoStemAllomorph bullNForm = (IMoStemAllomorph)bullN.AlternateFormsOS.Append(new MoStemAllomorph());
				bullNForm.Form.VernacularDefaultWritingSystem = "bullNTEST";
				IMoStemMsa bullNMSA = (IMoStemMsa)bullN.MorphoSyntaxAnalysesOC.Add(new MoStemMsa());

				xmlFragment =
					"<Wordform DbRef='" + hvoBearTEST.ToString() + "' Form='bearTEST'>\n"
					+ "<WfiAnalysis>\n"
					+ "<Morphs>\n"
					+ "<Morph>\n"
					+ "<MoForm DbRef='" + bearNForm.Hvo.ToString() + "' Label='bearNTEST'/>\n"
					+ "<MSI DbRef='" + bearNMSA.Hvo.ToString() + "'/>\n"
					+ "</Morph>\n"
					+ "</Morphs>\n"
					+ "</WfiAnalysis>\n"
					+ "</Wordform>\n"
					+ "<Wordform DbRef='" + hvoBullTEST.ToString() + "' Form='bullTEST'>\n"
					+ "<WfiAnalysis>\n"
					+ "<Morphs>\n"
					+ "<Morph>\n"
					+ "<MoForm DbRef='" + bullNForm.Hvo.ToString() + "' Label='bullNTEST'/>\n"
					+ "<MSI DbRef='" + bullNMSA.Hvo.ToString() + "'/>\n"
					+ "</Morph>\n"
					+ "</Morphs>\n"
					+ "</WfiAnalysis>\n"
					+ "</Wordform>\n";
			}
			m_filer.ProcessParse(MakeXML(xmlFragment, true));
			CheckAnalysisSize("bearTEST", 1, false);
			CheckAnalysisSize("bullTEST", 1, false);
		}

		/// <summary>
		/// Ensure analyses with 'duplicate' analyses are both approved.
		/// "Duplicate" means the MSA and MoForm IDs are the same in two different analyses.
		/// </summary>
		[Test]
		public void DuplicateAnalysesApproval()
		{
			int hvoBearTEST = CheckAnalysisSize("bearTEST", 0, true);
			string xmlFragment = "";
			int anal1Hvo;
			int anal2Hvo;
			int anal3Hvo;

			using (FdoCache cache = FdoCache.Create("TestLangProj"))
			{
				IWfiAnalysis anal = null;
				ILexDb ldb = cache.LangProject.LexDbOA;

				// Bear entry
				ILexEntry bearN = (ILexEntry)ldb.EntriesOC.Add(new LexEntry());
				AddIdToList(bearN.Hvo);
				IMoStemAllomorph bearNForm = (IMoStemAllomorph)bearN.AlternateFormsOS.Append(new MoStemAllomorph());
				bearNForm.Form.VernacularDefaultWritingSystem = "bearNTEST";
				IMoStemMsa bearNMSA = (IMoStemMsa)bearN.MorphoSyntaxAnalysesOC.Add(new MoStemMsa());
				ILexSense bearNLS = (ILexSense)bearN.SensesOS.Append(new LexSense()); ;

				IWfiWordform wf = WfiWordform.CreateFromDBObject(cache, hvoBearTEST);
				// First of two duplicate analyses
				anal = (IWfiAnalysis)wf.AnalysesOC.Add(new WfiAnalysis());
				anal1Hvo = anal.Hvo;
				IWfiMorphBundle mb = (IWfiMorphBundle)anal.MorphBundlesOS.Append(new WfiMorphBundle());
				mb.MorphRA = bearNForm;
				mb.MsaRA = bearNMSA;
				CheckEvaluationSize(anal1Hvo, 0, true, "anal1Hvo");

				// Non-duplicate, to make sure it does not get approved.
				anal = (IWfiAnalysis)wf.AnalysesOC.Add(new WfiAnalysis());
				anal2Hvo = anal.Hvo;
				mb = (IWfiMorphBundle)anal.MorphBundlesOS.Append(new WfiMorphBundle());
				mb.SenseRA = bearNLS;
				CheckEvaluationSize(anal2Hvo, 0, true, "anal2Hvo");

				// Second of two duplicate analyses
				anal = (IWfiAnalysis)wf.AnalysesOC.Add(new WfiAnalysis());
				anal3Hvo = anal.Hvo;
				mb = (IWfiMorphBundle)anal.MorphBundlesOS.Append(new WfiMorphBundle());
				mb.MorphRA = bearNForm;
				mb.MsaRA = bearNMSA;
				CheckEvaluationSize(anal3Hvo, 0, true, "anal3Hvo");
				CheckAnalysisSize("bearTEST", 3, false);

				xmlFragment =
					"<Wordform DbRef='" + hvoBearTEST.ToString() + "' Form='bearTEST'>\n"
					+ "<WfiAnalysis>\n"
					+ "<Morphs>\n"
					+ "<Morph>\n"
					+ "<MoForm DbRef='" + bearNForm.Hvo.ToString() + "' Label='bearNTEST'/>\n"
					+ "<MSI DbRef='" + bearNMSA.Hvo.ToString() + "'/>\n"
					+ "</Morph>\n"
					+ "</Morphs>\n"
					+ "</WfiAnalysis>\n"
					+ "</Wordform>\n";
			}

			m_filer.ProcessParse(MakeXML(xmlFragment, true));
			CheckEvaluationSize(anal1Hvo, 1, false, "anal1Hvo");
			CheckEvaluationSize(anal2Hvo, 0, false, "anal2Hvo");
			CheckEvaluationSize(anal3Hvo, 1, false, "anal3Hvo");
		}

		#endregion // Tests
	}
}
