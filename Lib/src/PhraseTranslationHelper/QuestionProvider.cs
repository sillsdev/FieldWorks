// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2012, SIL International. All Rights Reserved.
// <copyright from='2011' to='2012' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: QuestionProvider.cs
// ---------------------------------------------------------------------------------------------
using System.Collections;
using System.Collections.Generic;
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
						yield return new TranslatablePhrase(category.Type, -1, string.Empty,
							ScrReference.StartOfBible(ScrVers.English).BBCCCVVV,
							ScrReference.EndOfBible(ScrVers.English).BBCCCVVV, processedCategories.Count);
						processedCategories.Add(category.Type.ToLowerInvariant());
					}

					for (int iQuestion = 0; iQuestion < category.Questions.Length; iQuestion++)
					{
						Question q = category.Questions[iQuestion];
						string sRef;
						int startRef, endRef;
						if (q.ScriptureReference == null)
						{
							sRef = section.ScriptureReference;
							startRef = section.StartRef;
							endRef = section.EndRef;
						}
						else
						{
							sRef = q.ScriptureReference;
							startRef = q.StartRef;
							endRef = q.EndRef;
						}
						if (q.Answers != null || q.Notes != null)
							yield return new TranslatablePhrase(q.Text, iCat, sRef, startRef, endRef, iQuestion, q);
						else
							yield return new TranslatablePhrase(q.Text, iCat, sRef, startRef, endRef, iQuestion);
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
					PhraseCustomization customization = m_customizations.FirstOrDefault(c => c.Reference == phrase.Reference && c.OriginalPhrase == phrase.OriginalPhrase);
					if (customization != null)
					{
						if (customization.Type == PhraseCustomization.CustomizationType.Deletion)
							phrase.IsExcluded = true;
						else if (customization.Type == PhraseCustomization.CustomizationType.Modification)
							phrase.ModifiedPhrase = customization.ModifiedPhrase;
					}
				}
				yield return phrase;
			}
		}
		#endregion

		#region Implementation of IEnumerable
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
		#endregion
	}
	#endregion
}
