using System;
using System.Diagnostics;
using LiftIO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Ling;

namespace SIL.FieldWorks.LexText
{
	/// <summary>
	/// This class is called by the LiftParser, as it encounters each element of a lift file.
	/// There is at least one other ILexiconMerger, used in WeSay.
	///
	/// NB: this doesn't yet merge (dec 2006). Just blindly adds.
	/// </summary>
	public class FlexLiftMerger : ILexiconMerger<ILexEntry, ILexSense, ILexExampleSentence>
	{
		private readonly FdoCache _cache;

		public FlexLiftMerger(FdoCache cache)
		{
			_cache = cache;
		}

		private Guid GetGuidOrEmptyFromIdString(string id)
		{
			try
			{
				return new Guid(id);
			}
			catch (Exception)
			{
				//enchance: log this, we're throwing away the id they had
				return Guid.Empty;
			}
		}

		public ILexEntry GetOrMakeEntry(IdentifyingInfo idInfo)
		{
			Guid guid = GetGuidOrEmptyFromIdString(idInfo.id);
			bool canPrune;
			ILexEntry entry = TryToFindMatchingEntry(guid, idInfo, out canPrune);
			if (canPrune)
			{
				return null;
			}
			if(entry != null)
			{
				return entry;
			}
			return CreateEntry(ref idInfo, ref guid);
		}

		private ILexEntry TryToFindMatchingEntry(Guid guid, IdentifyingInfo idInfo, out bool canPrune)
		{
			ILexEntry entry = null;
			if (guid != Guid.Empty)
			{
				int hvo = _cache.GetIdFromGuid(guid);
				if (hvo > 0)
				{
					entry = LexEntry.CreateFromDBObject(_cache, hvo);
				}

				canPrune = CanSafelyPruneMerge(idInfo, entry);
				return entry;
			}
			canPrune = false;
			return null;
		}

		private ILexEntry CreateEntry(ref IdentifyingInfo idInfo, ref Guid guid)
		{
			ILexEntry entry = new LexEntry();
			//entry.Guid = guid;  not avail yet
		   SetGuid(entry, guid);

			if (idInfo.creationTime > DateTime.MinValue)
			{
				entry.DateCreated = idInfo.creationTime;
			}

			if (idInfo.modificationTime > DateTime.MinValue)
			{
				entry.DateModified = idInfo.modificationTime;
			}
			return entry;
		}

		private void SetGuid(ICmObject cmo, Guid guid)
		{
			Debug.Assert(cmo.Hvo > -1);
			_cache.SetGuidProperty(cmo.Hvo, (int)CmObjectFields.kflidCmObject_Guid, guid);
		}

		private static bool CanSafelyPruneMerge(IdentifyingInfo idInfo, ILexEntry entry)
		{
			return entry != null
				&& entry.DateModified  == idInfo.modificationTime
				&& entry.DateModified.Kind != DateTimeKind.Unspecified
				 && idInfo.modificationTime.Kind != DateTimeKind.Unspecified;
		}

		public ILexSense GetOrMakeSense(ILexEntry entry, IdentifyingInfo idInfo)
		{
			Guid guid = GetGuidOrEmptyFromIdString(idInfo.id);
			bool canPrune;
			ILexSense sense = TryToFindMatchingSense(guid, idInfo, out canPrune);
			if (canPrune)
			{
				return null;
			}
			if (sense != null)
			{
				return sense;
			}
			return CreateSense(entry, ref idInfo, ref guid);
		}

		private ILexSense CreateSense(ILexEntry entry, ref IdentifyingInfo idInfo, ref Guid guid)
		{
			ILexSense s = new LexSense();
			entry.SensesOS.Append(s);
			if (guid != Guid.Empty)
			{
				SetGuid(s, guid);
			}
			return s;
		}

		private ILexSense TryToFindMatchingSense(Guid guid, IdentifyingInfo idInfo, out bool canPrune)
		{
			canPrune = false; //no date info avail on senses
			ILexSense sense = null;
			if (guid != Guid.Empty)
			{
				int hvo = _cache.GetIdFromGuid(guid);
				if (hvo > 0)
				{
					sense = LexSense.CreateFromDBObject(_cache, hvo);
				}
				return sense;
			}
			return null;
		}

		public ILexExampleSentence GetOrMakeExample(ILexSense sense, IdentifyingInfo idInfo)
		{
			//nb, has no guid or dates
			ILexExampleSentence x = new LexExampleSentence();
			sense.ExamplesOS.Append(x);
			return x;
		}

		public void MergeInLexemeForm(ILexEntry entry, SimpleMultiText forms)
		{
			if (entry.LexemeFormOA == null)
			{
				//base the type on the first form

				string form = forms.FirstValue.Value;//<---HACK. What to do here?
				if (form == null)
				{
					return;
				}
			//TODO!!!!!!!!!!!!!!!!!!!!! beware ws order assumption here:
				entry.LexemeFormOA = MoForm.MakeMorph(_cache, entry, form);
		   }

			// now merge in the rest
			MergeIn(entry.LexemeFormOA.Form, forms);
		}



		public void MergeInGloss(ILexSense sense, SimpleMultiText forms)
		{
		  MergeIn(sense.Gloss, forms);
		}

		public void MergeInExampleForm(ILexExampleSentence example, SimpleMultiText forms)
		{
		  MergeIn(example.Example, forms);
		}

		public void MergeInTranslationForm(ILexExampleSentence example, SimpleMultiText forms)
		{
		   //MergeIn(example.TranslationsOC, forms);
		}

		public void MergeInDefinition(ILexSense sense, SimpleMultiText simpleMultiText)
		{
			throw new NotImplementedException();
		}

		//hack: our parser would need send us more than a simple SimpleMultiText to encode these
		private void MergeIn(MultiStringAccessor multiString, SimpleMultiText forms)
		{
			if (forms != null && forms.Keys != null)
			{
				foreach (string key in forms.Keys)
				{
					int wsHvo = GetWsFromLiftLang(key);
					multiString.SetAlternative(forms[key], wsHvo);
				}
			}
		}

		private  void MergeIn(MultiUnicodeAccessor multiText, SimpleMultiText forms)
		{
			if (forms != null && forms.Keys != null)
			{
				foreach (string key in forms.Keys)
				{
					int wsHvo = GetWsFromLiftLang(key);
					if (wsHvo > 0)
					{
						multiText.SetAlternative(forms[key], wsHvo);
					}
				}

			}
		   // multiText.MergeIn(MultiText.Create(forms));
		}

		public int GetWsFromLiftLang(string key)
		{
			int hvo = _cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr(key);
			if (hvo <1)//currently gets 0 if unfound
			{
				if (key.StartsWith("x-")) //strip of private-use rfc4646 tag
				{
					hvo = _cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr(key.Substring(2));
				}
				if (hvo < 1)//currently gets 0 if unfound
				{
					int ws = _cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr(key);
					_cache.LangProject.AnalysisWssRC.Add(ws);
					_cache.LangProject.CurAnalysisWssRS.Append(ws);
					_cache.LangProject.VernWssRC.Add(ws);
					_cache.LangProject.CurVernWssRS.Append(ws);

				}
				return hvo;
			}
			return hvo;
		}

		private Guid GetGuidFromIdString(string id)
		{
			try
			{
				return new Guid(id);
			}
			catch (Exception)
			{
				//enchance: log this, we're throwing away the id they had
				return Guid.NewGuid();
			}
		}
	}
}
