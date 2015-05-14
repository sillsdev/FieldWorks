// ---------------------------------------------------------------------------------------------
// Copyright (c) 2011-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: QuestionProvider.cs
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using SIL.Utils;
using SILUBS.SharedScrUtils;

namespace SILUBS.PhraseTranslationHelper
{
	#region class QuestionProvider
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Gets the questions from the file
	/// </summary>
	/// ------------------------------------------------------------------------------------
	public class QuestionProvider : IEnumerable<TranslatablePhrase>
	{
		private QuestionSections m_sections;
		private readonly IEnumerable<PhraseCustomization> m_customizations;
		private IDictionary<string, string> m_sectionHeads;
		private int[] m_availableBookIds;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="QuestionProvider"/> class.
		/// </summary>
		/// <param name="sections">Class representing the questions, organized by Scripture
		/// section and category.</param>
		/// <param name="customizations">.</param>
		/// ------------------------------------------------------------------------------------
		public QuestionProvider(QuestionSections sections, IEnumerable<PhraseCustomization> customizations)
		{
			m_sections = sections;
			m_customizations = customizations;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="QuestionProvider"/> class.
		/// </summary>
		/// <param name="filename">The filename.</param>
		/// <param name="customizations">Collection of any user customizations (exclusions,
		/// modifications, and additions). Pass null if there are no customizations.</param>
		/// ------------------------------------------------------------------------------------
		public QuestionProvider(string filename, IEnumerable<PhraseCustomization> customizations) :
			this(XmlSerializationHelper.DeserializeFromFile<QuestionSections>(filename), customizations)
		{
		}

		#region Public Properties
		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Gets a dictionary that correlates (textual) Scripture references to
		/// corresponding section head text (note that these are not the section heads in
		/// the vernacular Scripture but rather from the master question file).
		/// </summary>
		/// --------------------------------------------------------------------------------
		public IDictionary<string, string> SectionHeads
		{
			get
			{
				if (m_sectionHeads == null)
					m_sectionHeads = m_sections.Items.ToDictionary(s => s.ScriptureReference, s => s.Heading);
				return m_sectionHeads;
			}
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Gets an array of canonical book ids for which questions exist.
		/// </summary>
		/// --------------------------------------------------------------------------------
		public int[] AvailableBookIds
		{
			get
			{
				if (m_availableBookIds == null)
					m_availableBookIds = m_sections.Items.Select(s => BCVRef.GetBookFromBcv(s.StartRef)).Distinct().ToArray();
				return m_availableBookIds;
			}
		}
		#endregion

		#region Private helper methods
		private IEnumerable<TranslatablePhrase> GetPhrases()
		{
			HashSet<string> processedCategories = new HashSet<string>();
			foreach (Section section in m_sections.Items)
			{
				for (int iCat = 0; iCat < section.Categories.Length; iCat++)
				{
					Category category = section.Categories[iCat];

					if (category.Type != null && !processedCategories.Contains(category.Type.ToLowerInvariant()))
					{
						yield return new TranslatablePhrase(new SimpleQuestionKey(category.Type), -1, processedCategories.Count);
						processedCategories.Add(category.Type.ToLowerInvariant());
					}

					for (int iQuestion = 0; iQuestion < category.Questions.Length; iQuestion++)
					{
						Question q = category.Questions[iQuestion];
						if (q.ScriptureReference == null)
						{
							q.ScriptureReference = section.ScriptureReference;
							q.StartRef = section.StartRef;
							q.EndRef = section.EndRef;
						}
						yield return new TranslatablePhrase(q, iCat, iQuestion + 1);
					}
				}
			}
		}
		#endregion

		#region Implementation of IEnumerable<TranslatablePhrase>
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns an enumerator that iterates through the collection of questions.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to
		/// iterate through the collection.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public IEnumerator<TranslatablePhrase> GetEnumerator()
		{
			foreach (TranslatablePhrase phrase in GetPhrases())
			{
				if (m_customizations != null)
				{
					foreach (TranslatablePhrase insertedPhrase in GetPhraseCustomizations(phrase, 0.25f))
						yield return insertedPhrase;
				}
				else
					yield return phrase;
			}
		}

		private IEnumerable<TranslatablePhrase> GetPhraseCustomizations(TranslatablePhrase phrase, float seqOffset)
		{
			TranslatablePhrase addedPhrase = null;
			foreach (PhraseCustomization customization in m_customizations.Where(c => phrase.PhraseKey.Matches(c.Reference, c.OriginalPhrase)))
			{
				switch (customization.Type)
				{

					case PhraseCustomization.CustomizationType.Deletion:
						phrase.IsExcluded = true;
						break;
					case PhraseCustomization.CustomizationType.Modification:
						if (phrase.ModifiedPhrase != null)
						{
							throw new InvalidOperationException("Only one modified version of a phrase is permitted. Phrase '" + phrase.OriginalPhrase +
								"' has already been modied as '" + phrase.ModifiedPhrase + "'. Value of of subsequent modification attempt was: '" +
								customization.ModifiedPhrase + "'.");
						}
						phrase.ModifiedPhrase = customization.ModifiedPhrase;
						break;
					case PhraseCustomization.CustomizationType.InsertionBefore:
						if (phrase.InsertedPhraseBefore != null)
						{
							throw new InvalidOperationException("Only one phrase is permitted to be inserted. Phrase '" + phrase.OriginalPhrase +
								"' already has a phrase inserted before it: '" + phrase.InsertedPhraseBefore + "'. Value of of subsequent insertion attempt was: '" +
								customization.ModifiedPhrase + "'.");
						}
						Question addedQuestion = new Question(phrase.QuestionInfo, customization.ModifiedPhrase, customization.Answer);
						phrase.InsertedPhraseBefore = addedQuestion;
						TranslatablePhrase tpInserted = new TranslatablePhrase(addedQuestion, phrase.Category, phrase.SequenceNumber - seqOffset);
						foreach (TranslatablePhrase translatablePhrase in GetPhraseCustomizations(tpInserted, seqOffset / 4))
							yield return translatablePhrase;
						break;
					case PhraseCustomization.CustomizationType.AdditionAfter:
						if (phrase.AddedPhraseAfter != null)
						{
							throw new InvalidOperationException("Only one phrase is permitted to be added. Phrase '" + phrase.OriginalPhrase +
								"' already has a phrase added after it: '" + phrase.AddedPhraseAfter + "'. Value of of subsequent addition attempt was: '" +
								customization.ModifiedPhrase + "'.");
						}
						addedQuestion = new Question(phrase.QuestionInfo, customization.ModifiedPhrase, customization.Answer);
						phrase.AddedPhraseAfter = addedQuestion;
						addedPhrase = new TranslatablePhrase(addedQuestion, phrase.Category, phrase.SequenceNumber + seqOffset);
						break;
				}
			}
			yield return phrase;
			if (addedPhrase != null)
			{
				foreach (TranslatablePhrase tpAdded in GetPhraseCustomizations(addedPhrase, seqOffset / 4))
					yield return tpAdded;
			}
		}
		#endregion

		#region Implementation of IEnumerable
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "Caller is responsible to dispose enumerator")]
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
		#endregion
	}
	#endregion
}
