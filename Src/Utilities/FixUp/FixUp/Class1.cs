using System;
using System.Diagnostics;
using System.Collections;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Ling;

namespace FixUp
{
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	class FixUp
	{
		private FdoCache m_cache;

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			string db = args[0];
			FdoCache c = FdoCache.Create(db);
			Console.WriteLine("Loaded Cache: "+c.ToString());

			FixUp f = new FixUp(c );
			f.AddMissingMSAs();
			f.RemoveExtraAnalyses();
			c.Dispose();
		}

		public FixUp(FdoCache cache )
		{
			m_cache = cache;
		}

		protected void AddMissingMSAs()
		{
			int count = 0;
			Console.WriteLine("AddMissingMSAs()...");
			LexDb l = m_cache.LangProj.LexDbOA;
			foreach(LexEntry e in l.Entries)
			{
				if(e.MorphoSyntaxAnalysesOC.Count == 0)
				{
					++count;
					Console.WriteLine("Adding Stem MSAs to "+e.ShortName);
					MoStemMsa s = e.MorphoSyntaxAnalysesOC.Add(new SIL.FieldWorks.FDO.Ling.MoStemMsa()) as MoStemMsa;
				}
			}
			Console.WriteLine("Changed "+count.ToString()+" entries.");
		}

		/// <summary>
		/// This patch removes all analyses from the DB that have no annotations from IText,
		/// and that have no human evaluations. Since some analyses can be approved by the parser that
		/// are duplicates (old IText bug), it also includes parser approved analyses.
		/// These will come back the next time the parser is run, if the grammar and lexicon allow such.
		/// </summary>
		protected void RemoveExtraAnalyses()
		{
			int count = 0;
			Console.WriteLine("");
			Console.WriteLine("RemoveExtraAnalyses()...");
			foreach (WfiWordform wf in m_cache.LangProj.WordformInventoryOA.WordformsOC)
			{
				ArrayList anals = new ArrayList();
				foreach (WfiAnalysis anal in wf.AnalysesOC)
				{
					bool isHumanApproved = false;
					bool hasAnnotation = false;
					foreach (LinkedObjectInfo loi in anal.LinkedObjects)
					{
						int relObjClass = loi.RelObjClass;
						if (relObjClass == CmAnnotation.kclsidCmAnnotation)
						{
							hasAnnotation = true;
							break;
						}
						else if (relObjClass == CmAgentEvaluation.kclsidCmAgentEvaluation)
						{
							// See if the evaluation is from a human.
							CmAgentEvaluation eval = CmAgentEvaluation.CreateFromDBObject(m_cache, loi.RelObjId);
							CmAgent agent = CmAgent.CreateFromDBObject(m_cache, eval.OwnerHVO);
							if (agent.Human)
							{
								isHumanApproved = true;
								break;
							}
						}
					}
					if (hasAnnotation || isHumanApproved)
						continue;
					anals.Add(anal);
				}
				if (anals.Count > 0)
				{
					Console.WriteLine("Deleting {0} analyses from wordform: '{1}'.", anals.Count.ToString(), wf.Form.VernacularDefaultWritingSystem);
					foreach (WfiAnalysis anal in anals)
					{
						++count;
						//m_cache.DatabaseAccessor.BeginTrans();
						Debug.WriteLine(string.Format("Deleting analysis: {0}.", anal.Hvo));
						anal.DeleteUnderlyingObject();
						//m_cache.DatabaseAccessor.CommitTrans();
					}
				}
			}
			Console.WriteLine("Deleted " + count.ToString() + " analyses.");
		}
	}
}
