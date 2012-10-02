// <copyright from='2003' to='2007' company='SIL International'>
//    Copyright (c) 2007, SIL International. All Rights Reserved.
// </copyright>
//
// File: FWConverter.cs
// Responsibility: RandyR
// Last reviewed:
//
// <remarks>
// Implementation of FWConverter.
// </remarks>
//
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Xml.Xsl;

using SIL.WordWorks.GAFAWS;

namespace SIL.WordWorks.GAFAWS.FWConverter
{
	public class FWConverter : GafawsProcessor, IGAFAWSConverter
	{
		/// <summary>
		/// Default Constructor.
		/// </summary>
		public FWConverter()
		{
		}

		#region IGAFAWSConverter implementation

		/// <summary>
		/// Do whatever it takes to convert the input this processor knows about.
		/// </summary>
		public void Convert()
		{
			using (FWConverterDlg dlg = new FWConverterDlg())
			{
				dlg.ShowDialog();
				if (dlg.DialogResult == DialogResult.OK)
				{
					string catInfo = dlg.CatInfo;
					if (catInfo != null)
					{
						SqlConnection con = null;
						try
						{
							// 0 is the category id.
							// 1 is the entire connection string.
							string[] parts = catInfo.Split('^');
							con = new SqlConnection(parts[1]);
							con.Open();
							using (SqlCommand cmd = con.CreateCommand())
							{
								cmd.CommandType = CommandType.Text;
								string catIdQry;
								if (dlg.IncludeSubcategories)
								{
									catIdQry = string.Format("IN ({0}", parts[0]);
									cmd.CommandText = "SELECT Id\n" +
										string.Format("FROM fnGetOwnedIds({0}, 7004, 7004)", parts[0]);
									using (SqlDataReader reader = cmd.ExecuteReader())
									{
										while (reader.Read())
											catIdQry += string.Format(", {0}", reader.GetInt32(0));
									}
									catIdQry += ")";
								}
								else
								{
									catIdQry = string.Format("= {0}", parts[0]);
								}
								cmd.CommandText =
									"SELECT anal.Owner$ AS Wf_Id,\n" +
									"	anal.Id AS Anal_Id,\n" +
									"	mb.OwnOrd$ AS Mb_Ord,\n" +
									"	Mb.Sense AS Mb_Sense,\n" +
									"	mb.Morph AS Mb_Morph,\n" +
									"	mb.Msa AS Mb_Msa, msa.Class$ AS Msa_Class\n" +
									"FROM WfiAnalysis_ anal\n" +
									"--) Only use those that are human approved\n" +
									"JOIN CmAgentEvaluation eval ON eval.Target = anal.Id\n" +
									"JOIN CmAgent agt ON agt.Human = 1\n" +
									"JOIN CmAgent_Evaluations j_agt_eval ON agt.Id = j_agt_eval.Src AND j_agt_eval.Dst = eval.Id\n" +
									"--) Get morph bundles\n" +
									"JOIN WfiMorphBundle_ mb ON mb.Owner$ = anal.Id\n" +
									"--) Get MSA class\n" +
									"LEFT OUTER JOIN MoMorphSynAnalysis_ msa ON mb.msa = msa.Id\n" +
									String.Format("WHERE anal.Category {0} AND eval.Accepted = 1\n", catIdQry) +
									"ORDER BY anal.Owner$, anal.Id, mb.OwnOrd$";
								List<FwWordform> wordforms = new List<FwWordform>();
								using (SqlDataReader reader = cmd.ExecuteReader())
								{
									bool moreRows = reader.Read();
									while (moreRows)
									{
										/*
										 * Return values, in order are:
										 *	Wordform Id: int: 0
										 *	Analysis Id: int: 1
										 *	MorphBundle Ord: int: 2
										 *	Sense Id: int: 3
										 *	MoForm Id: int: 4
										 *	MSA Id: int: 5
										 *	MSA Class: int: 6
										*/
										FwWordform wordform = new FwWordform();
										moreRows = wordform.LoadFromDB(reader);
										wordforms.Add(wordform);
									}
								}
								// Convert all of the wordforms.
								Dictionary<string, FwMsa> prefixes = new Dictionary<string, FwMsa>();
								Dictionary<string, List<FwMsa>> stems = new Dictionary<string, List<FwMsa>>();
								Dictionary<string, FwMsa> suffixes = new Dictionary<string, FwMsa>();
								foreach (FwWordform wf in wordforms)
									wf.Convert(cmd, m_gd, prefixes, stems, suffixes);
							}
						}
						catch
						{
							// Eat exceptions.
						}
						finally
						{
							if (con != null)
								con.Close();
						}

						// Handle the processing and transforming here.
						string outputPathname = null;
						try
						{
							// Main processing.
							PositionAnalyzer anal = new PositionAnalyzer();
							anal.Process(m_gd);

							// Strip out all the _#### here.
							foreach (WordRecord wr in m_gd.WordRecords)
							{
								if (wr.Prefixes != null)
								{
									foreach (Affix afx in wr.Prefixes)
										afx.MIDREF = EatIds(afx.MIDREF);
								}

								wr.Stem.MIDREF = EatIds(wr.Stem.MIDREF);

								if (wr.Suffixes != null)
								{
									foreach (Affix afx in wr.Suffixes)
										afx.MIDREF = EatIds(afx.MIDREF);
								}
							}
							foreach (Morpheme morph in m_gd.Morphemes)
							{
								morph.MID = EatIds(morph.MID);
							}

							// Save, so it can be transformed.
							outputPathname = Path.GetTempFileName() + ".xml"; ;
							m_gd.SaveData(outputPathname);

							// Transform.
							XslCompiledTransform trans = new XslCompiledTransform();
							try
							{
								trans.Load(XSLPathname);
							}
							catch
							{
								MessageBox.Show("Could not load the XSL file.", "Information");
								return;
							}

							string htmlOutput = Path.GetTempFileName() + ".html";
							try
							{
								trans.Transform(outputPathname, htmlOutput);
							}
							catch
							{
								MessageBox.Show("Could not transform the input file.", "Information");
								return;
							}
							Process.Start(htmlOutput);
						}
						catch
						{
							// Eat exceptions.
						}
						finally
						{
							if (outputPathname != null && File.Exists(outputPathname))
								File.Delete(outputPathname);
						}
					}
				}
			}

			// Reset m_gd, in case it gets called for another file.
			m_gd = GAFAWSData.Create();
		}

		/// <summary>
		/// Gets the name of the converter that is suitable for display in a list
		/// of other converts.
		/// </summary>
		public string Name
		{
			get { return "FieldWorks converter"; }
		}

		/// <summary>
		/// Gets a description of the converter that is suitable for display.
		/// </summary>
		public string Description
		{
			get { return "Prepare FieldWorks data for processing. Only user-approved analyses that are the selected part of speech are included in the processing."; }
		}

		/// <summary>
		/// Gets the pathname of the XSL file used to turn the XML into HTML.
		/// </summary>
		public string XSLPathname
		{
			get
			{
				return Path.Combine(Path.GetDirectoryName(
					Assembly.GetExecutingAssembly().CodeBase),
					"AffixPositionChart_FW.xsl");
			}
		}

		#endregion IGAFAWSConverter implementation

		private string EatIds(string input)
		{
			string output = "";

			string[] parts = input.Split('_');
			for (int i = 0; i < parts.Length - 1; ++i)
			{
				output += parts[i];
				string nextPart = parts[i + 1];
				while (nextPart.IndexOfAny(new char[] {'1', '2', '3', '4', '5', '6', '7', '8', '9', '0'}) == 0)
				{
					nextPart = nextPart.Substring(1);
				}
				parts[i + 1] = nextPart;
			}
			output += parts[parts.Length - 1];

			return output.Trim();
		}
	}

	internal class FwWordform
	{
		private int m_id = 0;
		private List<FwAnalysis> m_analyses = new List<FwAnalysis>();

		internal FwWordform()
		{
		}

		/// <summary>
		/// Load one wordform from DB 'reader'.
		/// As long as the wf id is the same, then keep going,
		/// as it is the same wordform.
		/// </summary>
		/// <param name="reader">'true' if there are more rows (wordforms) to process, otherwise, 'false'.</param>
		/// <returns></returns>
		internal bool LoadFromDB(SqlDataReader reader)
		{
			bool moreRows = true; // Start on the optimistic side.
			if (m_id == 0)
			{
				int id = reader.GetInt32(0);
				m_id = id;
				while (moreRows && (reader.GetInt32(0) == m_id))
				{
					FwAnalysis anal = new FwAnalysis();
					moreRows = anal.LoadFromDB(reader);
					m_analyses.Add(anal);
				}
			}
			return moreRows;
		}

		internal void Convert(SqlCommand cmd, GAFAWSData gData, Dictionary<string, FwMsa> prefixes, Dictionary<string, List<FwMsa>> stems, Dictionary<string, FwMsa> suffixes)
		{
			foreach (FwAnalysis anal in m_analyses)
				anal.Convert(cmd, gData, prefixes, stems, suffixes);
		}
	}

	internal class FwAnalysis
	{
		private int m_id;
		private SortedDictionary<int, FwMorphBundle> m_morphBundles = new SortedDictionary<int, FwMorphBundle>();

		internal FwAnalysis()
		{
		}

		/// <summary>
		/// Load one wordform from DB 'reader'.
		/// As long as the wf id is the same, then keep going,
		/// as it is the same wordform.
		/// </summary>
		/// <param name="reader">'true' if there are more rows (wordforms) to process, otherwise, 'false'.</param>
		/// <returns></returns>
		internal bool LoadFromDB(SqlDataReader reader)
		{
			bool moreRows = true; // Start on the optimistic side.

			if (m_id == 0)
			{
				int analId = reader.GetInt32(1);
				m_id = analId;
				while (moreRows && (reader.GetInt32(1) == m_id))
				{
					int wfId = reader.GetInt32(0);
					analId = reader.GetInt32(1);
					int ord = reader.GetInt32(2);
					int senseId = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);
					int morphId = reader.IsDBNull(4) ? 0 : reader.GetInt32(4);
					int msaId = reader.IsDBNull(5) ? 0 : reader.GetInt32(5);
					int msaClass = reader.IsDBNull(6) ? 0 : reader.GetInt32(6);
					//Debug.WriteLine(String.Format("wfId: {0} analId: {1} ord: {2} senseId: {3} morphId:> {4} msaId: {5} msaClass: {6}",
					//	wfId, analId, ord, senseId, morphId, msaId, msaClass));
					FwMorphBundle mb = new FwMorphBundle(
						ord,
						senseId,
						morphId,
						new FwMsa(msaId, msaClass));
					moreRows = reader.Read();
					m_morphBundles.Add(ord, mb);
				}
			}

			return moreRows;
		}

		private bool CanConvert
		{
			get
			{
				foreach (FwMorphBundle mb in m_morphBundles.Values)
				{
					if (mb.MSA.Id == 0)
						return false;
				}
				if (m_morphBundles.Count == 1 && m_morphBundles[1].MSA.Class != 5001)
				{
					//FwMorphBundle mb = m_morphBundles[1];
					//if (mb.MSA.Class != 5001)
						return false;
				}
				return m_morphBundles.Count > 0;
			}
		}

		internal void Convert(SqlCommand cmd, GAFAWSData gData, Dictionary<string, FwMsa> prefixes, Dictionary<string, List<FwMsa>> stems, Dictionary<string, FwMsa> suffixes)
		{
			if (!CanConvert)
				return;

			WordRecord wr = new WordRecord();
			// Deal with prefixes, if any.
			int startStemOrd = 0;
			foreach (KeyValuePair<int, FwMorphBundle> kvp in m_morphBundles)
			{
				FwMorphBundle mb = kvp.Value;
				string msaKey = mb.GetMsaKey(cmd);
				if (mb.MSA.Class == 5001 || mb.MSA.Class == 5031 || mb.MSA.Class == 5032 || mb.MSA.Class == 5117) // What about 5117-MoUnclassifiedAffixMsa?
				{
					// stem or derivational prefix, so bail out of this loop.
					startStemOrd = kvp.Key;
					break;
				}

				// Add prefix, if not already present.
				if (wr.Prefixes == null)
					wr.Prefixes = new List<Affix>();
				if (!prefixes.ContainsKey(msaKey))
				{
					prefixes.Add(msaKey, mb.MSA);
					gData.Morphemes.Add(new Morpheme(MorphemeType.prefix, msaKey));
				}
				Affix afx = new Affix();
				afx.MIDREF = msaKey;
				wr.Prefixes.Add(afx);
			}

			// Deal with suffixes, if any.
			// Work through the suffixes from the end of the word.
			// We stop when we hit the stem or a derivational suffix.
			int endStemOrd = 0;
			for (int i = m_morphBundles.Count; i > 0; --i)
			{
				FwMorphBundle mb = m_morphBundles[i];
				string msaKey = mb.GetMsaKey(cmd);
				if (mb.MSA.Class == 5001 || mb.MSA.Class == 5031 || mb.MSA.Class == 5032 || mb.MSA.Class == 5117) // What about 5117-MoUnclassifiedAffixMsa?
				{
					// stem or derivational suffix, so bail out of this loop.
					endStemOrd = i;
					break;
				}

				// Add suffix, if not already present.
				if (wr.Suffixes == null)
					wr.Suffixes = new List<Affix>();
				if (!suffixes.ContainsKey(msaKey))
				{
					suffixes.Add(msaKey, mb.MSA);
					gData.Morphemes.Add(new Morpheme(MorphemeType.suffix, msaKey));
				}
				Affix afx = new Affix();
				afx.MIDREF = msaKey;
				wr.Suffixes.Insert(0, afx);
			}

			// Deal with stem.
			List<FwMsa> localStems = new List<FwMsa>();
			string sStem = "";
			foreach (KeyValuePair<int, FwMorphBundle> kvp in m_morphBundles)
			{
				FwMorphBundle mb = kvp.Value;
				int currentOrd = kvp.Key;
				if (currentOrd >= startStemOrd && currentOrd <= endStemOrd)
				{
					string msaKey = mb.GetMsaKey(cmd);
					string spacer = (currentOrd == 1) ? "" : " ";
					sStem += spacer + msaKey;
				}
			}
			if (!stems.ContainsKey(sStem))
			{
				stems.Add(sStem, localStems);
				gData.Morphemes.Add(new Morpheme(MorphemeType.stem, sStem));
			}

			Stem stem = new Stem();
			stem.MIDREF = sStem;
			wr.Stem = stem;

			// Add wr.
			gData.WordRecords.Add(wr);
		}
	}

	internal class FwMorphBundle
	{
		private int m_ord;
		private int m_senseId;
		private int m_morphId;
		private FwMsa m_msa;

		internal FwMorphBundle(int ord, int senseId, int morphId, FwMsa msa)
		{
			m_ord = ord;
			m_senseId = senseId;
			m_morphId = morphId;
			m_msa = msa;
		}

		internal FwMsa MSA
		{
			get { return m_msa; }
		}

		internal string GetMsaKey(SqlCommand cmd)
		{
			string msaBase = "_" + m_msa.Id;
			cmd.CommandText = "SELECT mff.Txt\n" +
				"FROM CmObject msa\n" +
				"JOIN LexEntry_LexemeForm j_entry_form ON msa.Owner$ = j_entry_form.Src\n" +
				"JOIN MoForm_Form mff ON mff.Obj = j_entry_form.Dst\n" +
				"WHERE msa.Id =" + m_msa.Id.ToString();
			string txt = cmd.ExecuteScalar() as string;
			if (txt == null || txt == "")
				txt = "???";
			return txt + msaBase;
		}
	}

	internal class FwMsa
	{
		private int m_id;
		private int m_msaClass;

		internal FwMsa(int id, int msaClass)
		{
			m_id = id;
			m_msaClass = msaClass;
		}

		internal int Class
		{
			get { return m_msaClass; }
		}

		internal int Id
		{
			get { return m_id; }
		}
	}
}
