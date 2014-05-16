using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Paratext.LexicalContracts;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;

namespace SIL.FieldWorks.ParatextLexiconPlugin
{
	#region FdoLexEntryLexeme class
	/// <summary>
	/// </summary>
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification="m_lexicon is a reference")]
	internal class FdoLexEntryLexeme : Lexeme
	{
		private readonly LexemeKey m_key;
		private readonly FdoLexicon m_lexicon;

		public FdoLexEntryLexeme(FdoLexicon lexicon, LexemeKey key)
		{
			m_lexicon = lexicon;
			m_key = key;
		}

		public LexemeKey Key
		{
			get { return m_key; }
		}

		#region Lexeme Members

		public string Id
		{
			get { return m_key.Id; }
		}

		public LexemeType Type
		{
			get { return m_key.Type; }
		}

		public string LexicalForm
		{
			get { return m_key.LexicalForm; }
		}

		/// <summary>
		/// Gets a string that is suitable for display which may contain morphological abbreviations
		/// </summary>
		public string DisplayString
		{
			get
			{
				using (m_lexicon.ActivationContext.Activate())
				{
					ILexEntry entry;
					if (!m_lexicon.TryGetEntry(m_key, out entry))
						return null;

					return StringServices.CitationFormWithAffixTypeStaticForWs(entry, m_lexicon.DefaultVernWs);
				}
			}
		}

		public string CitationForm
		{
			get
			{
				using (m_lexicon.ActivationContext.Activate())
				{
					ILexEntry entry;
					if (!m_lexicon.TryGetEntry(m_key, out entry))
						return null;

					ITsString tss = entry.CitationForm.StringOrNull(m_lexicon.DefaultVernWs);
					return tss == null ? null : tss.Text;
				}
			}
		}

		public int HomographNumber
		{
			get
			{
				using (m_lexicon.ActivationContext.Activate())
				{
					ILexEntry entry;
					if (!m_lexicon.TryGetEntry(m_key, out entry))
						return 0;

					return entry.HomographNumber;
				}
			}
		}

		public IEnumerable<LexiconSense> Senses
		{
			get
			{
				using (m_lexicon.ActivationContext.Activate())
				{
					ILexEntry entry;
					if (!m_lexicon.TryGetEntry(m_key, out entry))
						return Enumerable.Empty<LexiconSense>();

					if (entry.AllSenses.Count == 1 && entry.SensesOS[0].Gloss.StringCount == 0)
						return Enumerable.Empty<LexiconSense>();

					return entry.AllSenses.Select(s => new LexSenseLexiconSense(m_lexicon, m_key, s)).ToArray();
				}
			}
		}

		public IEnumerable<LexicalRelation> LexicalRelations
		{
			get
			{
				using (m_lexicon.ActivationContext.Activate())
				{
					ILexEntry entry;
					if (!m_lexicon.TryGetEntry(m_key, out entry))
						return Enumerable.Empty<LexicalRelation>();

					var relations = new List<LexicalRelation>();
					foreach (ILexReference lexRef in entry.LexEntryReferences.Union(entry.AllSenses.SelectMany(s => s.LexSenseReferences)))
					{
						string name = GetLexReferenceName(entry, lexRef.OwnerOfClass<ILexRefType>()).Normalize();
						foreach (ICmObject obj in lexRef.TargetsRS)
						{
							ILexEntry otherEntry = null;
							switch (obj.ClassID)
							{
								case LexEntryTags.kClassId:
									otherEntry = (ILexEntry) obj;
									break;
								case LexSenseTags.kClassId:
									otherEntry = obj.OwnerOfClass<ILexEntry>();
									break;
							}
							if (otherEntry != null && otherEntry != entry)
								relations.Add(new FdoLexicalRelation(m_lexicon.GetEntryLexeme(otherEntry), name));
						}
					}

					return relations;
				}
			}
		}

		public IEnumerable<string> AlternateForms
		{
			get
			{
				using (m_lexicon.ActivationContext.Activate())
				{
					ILexEntry entry;
					if (!m_lexicon.TryGetEntry(m_key, out entry))
						return Enumerable.Empty<string>();

					var forms = new List<string>();
					foreach (IMoForm form in entry.AlternateFormsOS)
					{
						ITsString tss = form.Form.StringOrNull(m_lexicon.DefaultVernWs);
						if (tss != null)
							forms.Add(tss.Text.Normalize());
					}

					return forms;
				}
			}
		}

		private string GetLexReferenceName(ILexEntry lexEntry, ILexRefType lexRefType)
		{
			// The name we want to use for our lex reference is either the name or the reverse name
			// (depending on the direction of the relationship, if relevant) of the owning lex ref type.
			ITsString lexReferenceName = lexRefType.Name.BestVernacularAnalysisAlternative;

			if (lexRefType.MappingType == (int)MappingTypes.kmtEntryAsymmetricPair ||
				lexRefType.MappingType == (int)MappingTypes.kmtEntryOrSenseAsymmetricPair ||
				lexRefType.MappingType == (int)MappingTypes.kmtSenseAsymmetricPair ||
				lexRefType.MappingType == (int)MappingTypes.kmtEntryTree ||
				lexRefType.MappingType == (int)MappingTypes.kmtEntryOrSenseTree ||
				lexRefType.MappingType == (int)MappingTypes.kmtSenseTree)
			{
				if (lexEntry.OwnOrd == 0 && lexRefType.Name != null) // the original code had a check for name length as well.
					lexReferenceName = lexRefType.ReverseName.BestAnalysisAlternative;
			}

			return lexReferenceName.Text;
		}

		public LexiconSense AddSense()
		{
			LexiconSense sense = null;
			bool lexemeAdded = false;
			m_lexicon.UpdatingEntries = true;
			try
			{
				using (m_lexicon.ActivationContext.Activate())
				{
					NonUndoableUnitOfWorkHelper.Do(m_lexicon.Cache.ActionHandlerAccessor, () =>
						{
							ILexEntry entry;
							if (!m_lexicon.TryGetEntry(m_key, out entry))
							{
								entry = m_lexicon.CreateEntry(m_key);
								lexemeAdded = true;
							}

							if (entry.AllSenses.Count == 1 && entry.SensesOS[0].Gloss.StringCount == 0)
							{
								// An empty sense exists (probably was created during a call to AddLexeme)
								sense = new LexSenseLexiconSense(m_lexicon, m_key, entry.SensesOS[0]);
							}
							else
							{
								ILexSense newSense = m_lexicon.Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create(
									entry, new SandboxGenericMSA(), (string)null);
								sense = new LexSenseLexiconSense(m_lexicon, m_key, newSense);
							}
						});
				}
			}
			finally
			{
				m_lexicon.UpdatingEntries = false;
			}
			if (lexemeAdded)
				m_lexicon.OnLexemeAdded(this);
			m_lexicon.OnLexiconSenseAdded(this, sense);
			return sense;
		}

		public void RemoveSense(LexiconSense sense)
		{
			using (m_lexicon.ActivationContext.Activate())
			{
				ILexEntry entry;
				if (!m_lexicon.TryGetEntry(m_key, out entry))
					return;

				NonUndoableUnitOfWorkHelper.Do(m_lexicon.Cache.ActionHandlerAccessor, () =>
					{
						var leSense = (LexSenseLexiconSense)sense;
						if (entry.AllSenses.Count == 1)
						{
							foreach (int ws in leSense.Sense.Gloss.AvailableWritingSystemIds)
								leSense.Sense.Gloss.set_String(ws, (ITsString) null);
						}
						else
						{
							leSense.Sense.Delete();
						}
					});
			}
		}

		#endregion

		public override string ToString()
		{
			return DisplayString;
		}

		public override bool Equals(object obj)
		{
			var other = obj as FdoLexEntryLexeme;
			return other != null && m_key.Equals(other.m_key);
		}

		public override int GetHashCode()
		{
			return m_key.GetHashCode();
		}

		[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
			Justification="m_lexicon is a reference")]
		private class LexSenseLexiconSense : LexiconSense
		{
			private readonly FdoLexicon m_lexicon;
			private readonly LexemeKey m_lexemeKey;
			private readonly ILexSense m_lexSense;

			public LexSenseLexiconSense(FdoLexicon lexicon, LexemeKey lexemeKey, ILexSense lexSense)
			{
				m_lexicon = lexicon;
				m_lexemeKey = lexemeKey;
				m_lexSense = lexSense;
			}

			internal ILexSense Sense
			{
				get { return m_lexSense; }
			}

			public string Id
			{
				get
				{
					using (m_lexicon.ActivationContext.Activate())
						return m_lexSense.Guid.ToString();
				}
			}

			public string SenseNumber
			{
				get
				{
					using (m_lexicon.ActivationContext.Activate())
						return m_lexSense.LexSenseOutline.Text;
				}
			}

			public string Category
			{
				get
				{
					using (m_lexicon.ActivationContext.Activate())
						return m_lexSense.MorphoSyntaxAnalysisRA == null ? "" : m_lexSense.MorphoSyntaxAnalysisRA.PartOfSpeechForWsTSS(m_lexicon.Cache.DefaultAnalWs).Text;
				}
			}

			public IEnumerable<LanguageText> Definitions
			{
				get
				{
					using (m_lexicon.ActivationContext.Activate())
					{
						IMultiString definition = m_lexSense.Definition;
						var defs = new List<LanguageText>();
						foreach (IWritingSystem ws in m_lexicon.Cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems)
						{
							ITsString tss = definition.StringOrNull(ws.Handle);
							if (tss != null)
								defs.Add(new FdoLanguageText(ws.Id, tss.Text.Normalize()));
						}
						return defs;
					}
				}
			}

			public IEnumerable<LanguageText> Glosses
			{
				get
				{
					using (m_lexicon.ActivationContext.Activate())
					{
						IMultiUnicode gloss = m_lexSense.Gloss;
						var glosses = new List<LanguageText>();
						foreach (IWritingSystem ws in m_lexicon.Cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems)
						{
							ITsString tss = gloss.StringOrNull(ws.Handle);
							if (tss != null)
								glosses.Add(new FdoLanguageText(ws.Id, tss.Text.Normalize()));
						}
						return glosses;
					}
				}
			}

			public LanguageText AddGloss(string language, string text)
			{
				using (m_lexicon.ActivationContext.Activate())
				{
					LanguageText lexGloss = null;
					NonUndoableUnitOfWorkHelper.Do(m_lexSense.Cache.ActionHandlerAccessor, () =>
						{
							IWritingSystem ws;
							if (!m_lexicon.Cache.ServiceLocator.WritingSystemManager.TryGet(language, out ws))
								throw new ArgumentException("The specified language is unrecognized.", "language");
							m_lexSense.Gloss.set_String(ws.Handle, text.Normalize(NormalizationForm.FormD));
							lexGloss = new FdoLanguageText(language, text);
						});
					m_lexicon.OnLexiconGlossAdded(new FdoLexEntryLexeme(m_lexicon, m_lexemeKey), this, lexGloss);
					return lexGloss;
				}
			}

			public void RemoveGloss(string language)
			{
				using (m_lexicon.ActivationContext.Activate())
				{
					NonUndoableUnitOfWorkHelper.Do(m_lexSense.Cache.ActionHandlerAccessor, () =>
						{
							IWritingSystem ws;
							if (!m_lexicon.Cache.ServiceLocator.WritingSystemManager.TryGet(language, out ws))
								throw new ArgumentException("The specified language is unrecognized.", "language");
							m_lexSense.Gloss.set_String(ws.Handle, (ITsString) null);
						});
				}
			}

			public IEnumerable<LexiconSemanticDomain> SemanticDomains
			{
				get
				{
					using (m_lexicon.ActivationContext.Activate())
						return m_lexSense.SemanticDomainsRC.Select(sd => new FdoSemanticDomain(sd.ShortName.Normalize())).ToArray();
				}
			}

			public override bool Equals(object obj)
			{
				var other = obj as LexSenseLexiconSense;
				return other != null && m_lexSense == other.m_lexSense;
			}

			public override int GetHashCode()
			{
				return m_lexSense.GetHashCode();
			}
		}
	}
	#endregion
}
