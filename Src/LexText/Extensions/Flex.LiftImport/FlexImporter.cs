#if nono

using System;
using System.Collections.Generic;
using System.Xml;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Ling;
using WeSay.LexicalModel;
using FlexEntry=SIL.FieldWorks.FDO.Ling.LexEntry;
using FlexSense=SIL.FieldWorks.FDO.Ling.LexSense;
using LexEntry=WeSay.LexicalModel.LexEntry;

namespace Flex.LiftImport
{
	public class FlexImporter
	{
		protected FdoCache _cache;
		protected MoMorphTypeCollection _flexMorphTypes;

	   // public event EventHandler<string> Merged;

		public FlexImporter(FdoCache cache)
		{
			_cache = cache;
			_flexMorphTypes = new MoMorphTypeCollection(_cache);
		}

		public void ImportLiftFile(string path)
		{
			XmlDocument doc = new XmlDocument();
			doc.Load(path);

			IList<LexEntry> entries= new List<LexEntry>();
			LiftImporter importer= LiftImporter.CreateCorrectImporter(doc);
			importer.ReadFile(doc,entries);

			foreach (LexEntry entry in entries)
			{
			  MergeInOneEntry(entry);
			}
		}


/*
		/// <summary>
		/// Import a single entry selected out of a Lift file. For test use.
		/// </summary>
		/// <param name="path"></param>
		/// <param name="xpath"></param>
		/// <returns>either the entry which was created were merged into</returns>
		public LexEntry ImportOneWeSayEntry(string path, string xpath)
		{
			XmlDocument doc = new XmlDocument();
			doc.Load(path);
			return ImportOneEntry(doc.SelectSingleNode(xpath));
		}
*/

		internal ILexEntry MergeInOneEntry(LexEntry wsEntry)
		{
			ILexEntry flexEntry=null;
			Guid guid = wsEntry.Guid;
			int hvo = _cache.GetIdFromGuid(guid);
			if (hvo > 0)
			{
				flexEntry = FlexEntry.CreateFromDBObject(_cache, hvo);
			   // if (((LexSense)flexEntry.SensesOS[0]).Gloss.AnalysisDefaultWritingSystem != weSayEntry.Gloss)
//                MergeAnalysisString((flexEntry.SensesOS[0]).Gloss,weSayEntry.Gloss);
//                MergeVernacularString(flexEntry.LexemeFormOA.Form,weSayEntry.LexicalForm);
//                MergeVernacularString(((ILexExampleSentence)((LexSense)flexEntry.SensesOS[0]).ExamplesOS[0]).Example,weSayEntry.Example);
			}
			else
			{
				flexEntry = MakeFwEntryFromWeSayEntry(wsEntry);
			}
			return flexEntry;
		}


		private void MergeAnalysisString(MultiUnicodeAccessor fwString, string wsString)
		{
			if(wsString.Length >0)
				fwString.AnalysisDefaultWritingSystem = wsString;

			//MultiStringAccessor a = new MultiStringAccessor(_cache, 0, 0, null);
			//existing.LexemeFormOA.Form.MergeAlternatives();
		}

		private void MergeVernacularString (MultiUnicodeAccessor fwString, string wsString)
		{
			if (wsString.Length > 0)
				fwString.VernacularDefaultWritingSystem = wsString;

		}

		private void MergeVernacularString (MultiStringAccessor fwString, string wsString)
		{
			if (wsString.Length > 0)
				fwString.VernacularDefaultWritingSystem.Text = wsString;
		}

		private FlexEntry MakeFwEntryFromWeSayEntry(WeSay.LexicalModel.LexEntry weSayEntry)
		{
			//MoStemMsa msa = new MoStemMsa();
			//// I wouldn't even *pretend* to understand this weirdness. Is 'dummy' a technical term?
			//DummyGenericMSA dmsa = DummyGenericMSA.Create(msa);
			//MoMorphType mmt = _flexMorphTypes.Item(MoMorphType.kmtStem);
			//LexEntry entry = LexEntry.CreateEntry(_cache, EntryType.ketMajorEntry, mmt, weSayEntry.LexicalForm, null, weSayEntry.Gloss, dmsa);
			FlexEntry entry = new FlexEntry();
			_cache.LangProject.LexDbOA.EntriesOC.Add(entry);
			//(_cache, EntryType.ketMajorEntry, mmt, weSayEntry.LexicalForm, null, weSayEntry.Gloss, dmsa);

			entry.Guid = weSayEntry.Guid;

			entry.LexemeFormOA = new MoStemAllomorph();
//            entry.LexemeFormOA.Form.VernacularDefaultWritingSystem
//                    = weSayEntry.LexicalForm;
		   //LexSense.CreateSense(entry, dmsa, weSayEntry.Gloss);

			MakeSense(weSayEntry, entry);

//            if (Merged != null)
//            {
//                Merged.Invoke(this, "Added");
//            }
			return entry;
		}

		private static void MakeSense(WeSay.LexicalModel.LexEntry weSayEntry, FlexEntry flexEntry)
		{
			FlexSense sense = new FlexSense();
			flexEntry.SensesOS.Append(sense);
		  //  sense.Gloss.AnalysisDefaultWritingSystem = weSayEntry.Senses[0];

//            if (weSayEntry.Example != null && weSayEntry.Example.Length >0)
//            {
//                LexExampleSentence example = new LexExampleSentence();
//                sense.ExamplesOS.Append(example);
//                 example.Example.VernacularDefaultWritingSystem.Text = weSayEntry.Example;
//          }

		}
/*
		/// <summary>
		///
		/// </summary>
		/// <param name="ld"></param>
		/// <param name="cf"></param>
		/// <param name="defn"></param>
		/// <param name="hvoDomain"></param>
		/// <returns></returns>
		protected LexEntry MakeLexEntry(LexDb ld, string cf, string defn, int hvoDomain)
		{
			LexEntry le = new LexEntry();
			ld.EntriesOC.Add(le);
			le.CitationForm.VernacularDefaultWritingSystem = cf;
			LexSense ls = new LexSense();
			le.SensesOS.Append(ls);
			ls.Definition.AnalysisDefaultWritingSystem.Text = defn;
			if (hvoDomain != 0)
				ls.SemanticDomainsRC.Add(hvoDomain);
			MoMorphSynAnalysis msa = new MoStemMsa();
			le.MorphoSyntaxAnalysesOC.Add(msa);
			ls.MorphoSyntaxAnalysisRA = msa;
			return le;
		}

		private void TryToMerge(LexEntry entry, Guid guid)
		{
			Debug.Assert(entry.Guid != guid, "Don't assign the guid yourself.");

			int hvo = _cache.GetIdFromGuid(guid);
			if (hvo > 0)
			{
				LexEntry existing = LexEntry.CreateFromDBObject(_cache, hvo);
				existing.MergeObject(entry, true);
			}
			else
			{
				entry.Guid = guid; // let the new guy live and keep this guid
			}
		}
 * */
	}
}

#endif