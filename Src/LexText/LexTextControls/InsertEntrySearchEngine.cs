using System;
using System.Collections.Generic;
using System.Linq;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainImpl;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// This is the search engine for InsertEntryDlg.
	/// </summary>
	internal class InsertEntrySearchEngine : SearchEngine
	{
		private readonly Virtuals m_virtuals;

		public InsertEntrySearchEngine(FdoCache cache)
			: base(cache, SearchType.Prefix)
		{
			m_virtuals = Cache.ServiceLocator.GetInstance<Virtuals>();
		}

		protected override IEnumerable<ITsString> GetStrings(SearchField field, ICmObject obj)
		{
			var entry = (ILexEntry) obj;

			int ws = field.String.get_WritingSystemAt(0);
			switch (field.Flid)
			{
				case LexEntryTags.kflidCitationForm:
					var cf = entry.CitationForm.StringOrNull(ws);
					if (cf != null && cf.Length > 0)
						yield return cf;
					break;

				case LexEntryTags.kflidLexemeForm:
					var lexemeForm = entry.LexemeFormOA;
					if (lexemeForm != null)
					{
						var formOfLexemeForm = lexemeForm.Form.StringOrNull(ws);
						if (formOfLexemeForm != null && formOfLexemeForm.Length > 0)
							yield return formOfLexemeForm;
					}
					break;

				case LexEntryTags.kflidAlternateForms:
					foreach (IMoForm form in entry.AlternateFormsOS)
					{
						var af = form.Form.StringOrNull(ws);
						if (af != null && af.Length > 0)
							yield return af;
					}
					break;

				case LexSenseTags.kflidGloss:
					foreach (ILexSense sense in entry.SensesOS)
					{
						var gloss = sense.Gloss.StringOrNull(ws);
						if (gloss != null && gloss.Length > 0)
							yield return gloss;
					}
					break;

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
				return true;

			switch (flid)
			{
				case LexEntryTags.kflidCitationForm:
				case LexEntryTags.kflidLexemeForm:
				case LexEntryTags.kflidAlternateForms:
				case LexEntryTags.kflidSenses:
				case MoFormTags.kflidForm:
				case LexSenseTags.kflidSenses:
				case LexSenseTags.kflidGloss:
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
					return true;
			}
			throw new ArgumentException("Unrecognized field.", "field");
		}
	}
}
