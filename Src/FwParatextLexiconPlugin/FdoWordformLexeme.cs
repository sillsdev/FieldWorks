using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Paratext.LexicalContracts;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;

namespace SIL.FieldWorks.ParatextLexiconPlugin
{
	#region FdoWordformLexeme class
	/// <summary>
	/// </summary>
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification="m_lexicon is a reference")]
	internal class FdoWordformLexeme : Lexeme
	{
		private readonly LexemeKey m_key;
		private readonly FdoLexicon m_lexicon;

		public FdoWordformLexeme(FdoLexicon lexicon, LexemeKey key)
		{
			m_lexicon = lexicon;
			m_key = key;
		}

		#region Lexeme Members

		public string Id
		{
			get { return m_key.Id; }
		}

		public LexemeType Type
		{
			get { return LexemeType.Word; }
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
			get { return LexicalForm; }
		}

		public string CitationForm
		{
			get { return null; }
		}

		public int HomographNumber
		{
			get { return 0; }
		}

		public IEnumerable<LexiconSense> Senses
		{
			get
			{
				using (m_lexicon.ActivationContext.Activate())
				{
					IWfiWordform wf;
					if (!m_lexicon.TryGetWordform(m_key.LexicalForm, out wf))
						return Enumerable.Empty<LexiconSense>();

					return wf.AnalysesOC.Where(a => a.ApprovalStatusIcon == (int) Opinions.approves).SelectMany(a => a.MeaningsOC)
						.Select(gloss => new WfiGlossLexiconSense(m_lexicon, m_key, gloss)).ToArray();
				}
			}
		}

		public IEnumerable<LexicalRelation> LexicalRelations
		{
			get { return Enumerable.Empty<LexicalRelation>(); }
		}

		public LexiconSense AddSense()
		{
			LexiconSense sense = null;
			bool lexemeAdded = false;
			using (m_lexicon.ActivationContext.Activate())
			{
				NonUndoableUnitOfWorkHelper.Do(m_lexicon.Cache.ActionHandlerAccessor, () =>
					{
						IWfiWordform wordform;
						if (!m_lexicon.TryGetWordform(m_key.LexicalForm, out wordform))
						{
							wordform = m_lexicon.CreateWordform(m_key.LexicalForm);
							lexemeAdded = true;
						}
						// For wordforms, our "senses" could be new meanings of an analysis for the word
						// or it could be a brand new analysis. Because we have no idea what the user actually
						// wanted, we just assume the worst (they want to create a new analysis for the word
						// with a new meaning).
						IWfiAnalysis analysis = m_lexicon.Cache.ServiceLocator.GetInstance<IWfiAnalysisFactory>().Create();
						wordform.AnalysesOC.Add(analysis);
						analysis.ApprovalStatusIcon = (int) Opinions.approves; // Assume the analysis from the external application is user approved
						IMoStemAllomorph morph = m_lexicon.Cache.ServiceLocator.GetInstance<IMoStemAllomorphRepository>().AllInstances().FirstOrDefault(allo =>
							{
								ITsString tss = allo.Form.StringOrNull(m_lexicon.DefaultVernWs);
								if (tss != null)
									return tss.Text == LexicalForm.Normalize(NormalizationForm.FormD);
								return false;
							});
						if (morph != null)
						{
							IWfiMorphBundle mb = m_lexicon.Cache.ServiceLocator.GetInstance<IWfiMorphBundleFactory>().Create();
							analysis.MorphBundlesOS.Add(mb);
							mb.MorphRA = morph;
							var entry = morph.OwnerOfClass<ILexEntry>();
							mb.SenseRA = entry.SensesOS[0];
							mb.MsaRA = entry.SensesOS[0].MorphoSyntaxAnalysisRA;
						}
						IWfiGloss gloss = m_lexicon.Cache.ServiceLocator.GetInstance<IWfiGlossFactory>().Create();
						analysis.MeaningsOC.Add(gloss);
						sense = new WfiGlossLexiconSense(m_lexicon, m_key, gloss);
					});
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
				NonUndoableUnitOfWorkHelper.Do(m_lexicon.Cache.ActionHandlerAccessor, () =>
					{
						var glossSense = (WfiGlossLexiconSense) sense;
						if (!glossSense.Gloss.Analysis.OccurrencesInTexts.Any(seg => seg.AnalysesRS.Contains(glossSense.Gloss)))
						{
							IWfiAnalysis analysis = glossSense.Gloss.Analysis;
							if (analysis.MeaningsOC.Count == 1 && !analysis.OccurrencesInTexts.Any())
								analysis.Delete();
							else
								glossSense.Gloss.Delete();
						}
					});
			}
		}

		#endregion

		public override bool Equals(object obj)
		{
			var other = obj as FdoWordformLexeme;
			return other != null && m_key.Equals(other.m_key);
		}

		public override int GetHashCode()
		{
			return m_key.GetHashCode();
		}

		public override string ToString()
		{
			return DisplayString;
		}

		[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
			Justification="m_lexicon is a reference")]
		private class WfiGlossLexiconSense : LexiconSense
		{
			private readonly FdoLexicon m_lexicon;
			private readonly LexemeKey m_lexemeKey;
			private readonly IWfiGloss m_gloss;

			public WfiGlossLexiconSense(FdoLexicon lexicon, LexemeKey lexemeKey, IWfiGloss gloss)
			{
				m_lexicon = lexicon;
				m_lexemeKey = lexemeKey;
				m_gloss = gloss;
			}

			internal IWfiGloss Gloss
			{
				get { return m_gloss; }
			}

			public string Id
			{
				get
				{
					using (m_lexicon.ActivationContext.Activate())
						return m_gloss.Guid.ToString();
				}
			}

			public string SenseNumber
			{
				get { return null; }
			}

			public string Category
			{
				get { return null; }
			}

			public IEnumerable<LanguageText> Definitions
			{
				get { return Enumerable.Empty<LanguageText>(); }
			}

			public IEnumerable<LanguageText> Glosses
			{
				get
				{
					using (m_lexicon.ActivationContext.Activate())
					{
						var glosses = new List<LanguageText>();
						IMultiUnicode fdoGlosses = m_gloss.Form;
						foreach (IWritingSystem ws in m_lexicon.Cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems)
						{
							ITsString tssGloss = fdoGlosses.StringOrNull(ws.Handle);
							if (tssGloss != null)
								glosses.Add(new FdoLanguageText(ws.Id, tssGloss.Text.Normalize()));
						}
						return glosses;
					}
				}
			}

			public LanguageText AddGloss(string language, string text)
			{
				LanguageText lexGloss = null;
				using (m_lexicon.ActivationContext.Activate())
				{
					NonUndoableUnitOfWorkHelper.Do(m_gloss.Cache.ActionHandlerAccessor, () =>
						{
							IWritingSystem ws;
							if (!m_lexicon.Cache.ServiceLocator.WritingSystemManager.TryGet(language, out ws))
								throw new ArgumentException("The specified language is unrecognized.", "language");
							m_gloss.Form.set_String(ws.Handle, text.Normalize(NormalizationForm.FormD));
							lexGloss = new FdoLanguageText(language, text);
						});
				}
				m_lexicon.OnLexiconGlossAdded(new FdoWordformLexeme(m_lexicon, m_lexemeKey), this, lexGloss);
				return lexGloss;
			}

			public void RemoveGloss(string language)
			{
				using (m_lexicon.ActivationContext.Activate())
				{
					NonUndoableUnitOfWorkHelper.Do(m_gloss.Cache.ActionHandlerAccessor, () =>
						{
							IWritingSystem ws;
							if (!m_lexicon.Cache.ServiceLocator.WritingSystemManager.TryGet(language, out ws))
								throw new ArgumentException("The specified language is unrecognized.", "language");
							m_gloss.Form.set_String(ws.Handle, (ITsString) null);
						});
				}
			}

			public IEnumerable<LexiconSemanticDomain> SemanticDomains
			{
				get { return Enumerable.Empty<LexiconSemanticDomain>(); }
			}

			public override bool Equals(object obj)
			{
				var other = obj as WfiGlossLexiconSense;
				return other != null && m_gloss == other.m_gloss;
			}

			public override int GetHashCode()
			{
				return m_gloss.GetHashCode();
			}
		}
	}
	#endregion
}
