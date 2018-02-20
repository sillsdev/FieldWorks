// Copyright (c) 2014-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using LanguageExplorer.Controls.XMLViews;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel;
using SIL.LCModel.DomainImpl;

namespace LanguageExplorer.Controls.LexText
{
	/// <summary>
	/// This is the search engine for EntryGoDlg.
	/// </summary>
	internal class EntryGoSearchEngine : SearchEngine
	{
		private readonly Virtuals m_virtuals;

		public EntryGoSearchEngine(LcmCache cache)
			: base(cache, SearchType.FullText)
		{
			m_virtuals = Cache.ServiceLocator.GetInstance<Virtuals>();
		}

		protected override IEnumerable<ITsString> GetStrings(SearchField field, ICmObject obj)
		{
			var entry = (ILexEntry) obj;

			var ws = field.String.get_WritingSystemAt(0);
			switch (field.Flid)
			{
				case LexEntryTags.kflidCitationForm:
					var cf = entry.CitationForm.StringOrNull(ws);
					if (cf != null && cf.Length > 0)
					{
						yield return cf;
					}
					break;

				case LexEntryTags.kflidLexemeForm:
					var lexemeForm = entry.LexemeFormOA;
					var formOfLexemeForm = lexemeForm?.Form.StringOrNull(ws);
					if (formOfLexemeForm != null && formOfLexemeForm.Length > 0)
					{
						yield return formOfLexemeForm;
					}
					break;

				case LexEntryTags.kflidAlternateForms:
					foreach (var form in entry.AlternateFormsOS)
					{
						var af = form.Form.StringOrNull(ws);
						if (af != null && af.Length > 0)
						{
							yield return af;
						}
					}
					break;

				case LexSenseTags.kflidGloss:
					foreach (var sense in entry.SensesOS)
					{
						var gloss = sense.Gloss.StringOrNull(ws);
						if (gloss != null && gloss.Length > 0)
						{
							yield return gloss;
						}
					}
					break;

				case LexSenseTags.kflidDefinition:
					foreach (var sense in entry.SensesOS)
					{
						var dffn = sense.Definition.StringOrNull(ws);
						if (dffn != null && dffn.Length > 0)
						{
							yield return dffn;
						}
					}
					break;

/*
				case LexSenseTags.kflidReversalEntries:
					foreach (var sense in entry.SensesOS)
					{
						foreach (var revEntry in sense.ReversalEntriesRC)
						{
							var revForm = revEntry.ReversalForm.StringOrNull(ws);
							if (revForm != null && revForm.Length > 0)
							{
								yield return revForm;
							}
						}
					}
					break;
*/

				default:
					throw new ArgumentException("Unrecognized field.", "field");
			}
		}

		protected override IList<ICmObject> GetSearchableObjects()
		{
			return Cache.ServiceLocator.GetInstance<ILexEntryRepository>().AllInstances().Cast<ICmObject>().ToArray();
		}

		protected override bool IsIndexResetRequired(int hvo, int flid)
		{
			if (flid == m_virtuals.LexDbEntries)
			{
				return true;
			}

			switch (flid)
			{
				case LexEntryTags.kflidCitationForm:
				case LexEntryTags.kflidLexemeForm:
				case LexEntryTags.kflidAlternateForms:
				case LexEntryTags.kflidSenses:
				case MoFormTags.kflidForm:
				case LexSenseTags.kflidSenses:
				case LexSenseTags.kflidGloss:
				case LexSenseTags.kflidDefinition:
//				case LexSenseTags.kflidReversalEntries:
				case ReversalIndexEntryTags.kflidReversalForm:
					return true;
			}
			return false;
		}

		protected override bool IsFieldMultiString(SearchField field)
		{
			switch (field.Flid)
			{
				case LexEntryTags.kflidCitationForm:
				case LexEntryTags.kflidLexemeForm:
				case LexEntryTags.kflidAlternateForms:
				case LexSenseTags.kflidGloss:
				case LexSenseTags.kflidDefinition:
//				case LexSenseTags.kflidReversalEntries:
					return true;
			}

			throw new ArgumentException("Unrecognized field.", "field");
		}
	}
}
