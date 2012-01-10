// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2012, SIL International. All Rights Reserved.
// <copyright from='2011' to='2012' company='SIL International'>
//		Copyright (c) 2012, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: QuestionSfmFileAccessor.cs
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using SIL.Utils;
using SILUBS.SharedScrUtils;
using System.Linq;

namespace SILUBS.PhraseTranslationHelper
{
	#region class QuestionSfmFileAccessor
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Gets the questions from the file
	/// </summary>
	/// ------------------------------------------------------------------------------------
	public static class QuestionSfmFileAccessor
	{
		private static readonly string s_kSectionHead = @"\rf";
		private static readonly string s_kRefMarker = @"\tqref";
		private static readonly string s_kQuestionMarker = @"\bttq";
		private static readonly string s_kAnswerMarker = @"\tqe";
		private static readonly string s_kCommentMarker = @"\an";
		private static readonly string s_kOverviewMarker = @"\oh";
		private static readonly string s_kDetailsMarker = @"\dh";
		private static HashSet<int> m_canonicalBookNumbers;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Parses the given string to get a starting and ending Scripture reference.
		/// </summary>
		/// <param name="sReference">The s reference.</param>
		/// <param name="startRef">The start ref.</param>
		/// <param name="endRef">The end ref.</param>
		/// ------------------------------------------------------------------------------------
		internal static void ParseRefRange(string sReference, out BCVRef startRef, out BCVRef endRef)
		{
			startRef = new BCVRef();
			endRef = new BCVRef();
			BCVRef.ParseRefRange(sReference, ref startRef, ref endRef);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Parses the given reference (that could be a verse bridge) and returns a BBBCCCVVV
		/// integer representing the start and end references.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static void Parse(string sReference, out int startRef, out int endRef)
		{
			BCVRef bcvStartRef, bcvEndRef;
			ParseRefRange(sReference, out bcvStartRef, out bcvEndRef);
			startRef = bcvStartRef;
			endRef = bcvEndRef;
			if (bcvStartRef.Valid)
				m_canonicalBookNumbers.Add(bcvStartRef.Book);
		}

		public static void Generate(string sourceQuestionsFilename, string alternativesFilename, string destFilename)
		{
			Dictionary<string, string[]> alternatives = null;

			if (alternativesFilename != null)
			{
				QuestionAlternativeOverrides qAlts = XmlSerializationHelper.DeserializeFromFile<QuestionAlternativeOverrides>(alternativesFilename);
				//alternatives = new Dictionary<string, string[]>();
				//foreach (Alternative a in qAlts.Items)
				//{
				//    Debug.Assert(!alternatives.ContainsKey(a.Text));
				//    alternatives[a.Text] = a.AlternateForms;
				//}
				alternatives = qAlts.Items.ToDictionary(a => a.Text, a => a.AlternateForms);
			}
			XmlSerializationHelper.SerializeToFile(destFilename, Generate(SourceLines(sourceQuestionsFilename), alternatives));
		}

		public static QuestionSections Generate(IEnumerable<string> sourceLines, Dictionary<string, string[]> alternatives)
		{
			m_canonicalBookNumbers = new HashSet<int>();

			// Initialize the ID textbox.
			Category currCat = null;
			string currRef = null;
			int startRef = 0, endRef = 0;
			Section currSection = null;
			bool currSectionRefSet = false;
			Question currQuestion = null;
			List<Section> sections = new List<Section>();
			List<Question> currentQuestions = new List<Question>();
			int cAnswers = 0, cComments = 0, cCategories = 0;
			int kSectHeadMarkerLen = s_kSectionHead.Length;
			int kRefMarkerLen = s_kRefMarker.Length;
			int kQMarkerLen = s_kQuestionMarker.Length;
			int kAMarkerLen = s_kAnswerMarker.Length;
			int kCommentMarkerLen = s_kCommentMarker.Length;
			Debug.Assert(s_kDetailsMarker.Length == s_kOverviewMarker.Length);
			int kCategoryMarkerLen = s_kDetailsMarker.Length;
			Regex regexVerseNum = new Regex(@"\((?<startVerse>\d+)(-(?<endVerse>\d+))?\)$", RegexOptions.Compiled);
			foreach (string sLine in SourceFields(sourceLines))
			{
				if (sLine.StartsWith(s_kQuestionMarker))
				{
					if (currQuestion != null && cAnswers == 0 && cComments == 0)
					{
						// Question continued in a subsequent field. Just append the text to the existing question.
						currQuestion.Text += " " + sLine.Substring(kQMarkerLen).Trim();
					}
					else
					{
						currQuestion = new Question();
						currentQuestions.Add(currQuestion);
						currQuestion.Text = sLine.Substring(kQMarkerLen).Trim();
						if (currRef != currSection.ScriptureReference)
						{
							currQuestion.ScriptureReference = currRef;
							currQuestion.StartRef = startRef;
							currQuestion.EndRef = endRef;
						}
						cAnswers = 0;
						cComments = 0;
					}
				}
				else if (sLine.StartsWith(s_kAnswerMarker))
				{
					string currAnswer = sLine.Substring(kAMarkerLen).Trim();
					Match match = regexVerseNum.Match(currAnswer);
					if (match.Success)
					{
						int startVerse = Int32.Parse(match.Result("${startVerse}"));
						string sEndVerse = match.Result("${endVerse}");
						int endVerse = string.IsNullOrEmpty(sEndVerse) ? startVerse : Int32.Parse(sEndVerse);
						BCVRef bcvStart, bcvEnd;
						if (currQuestion.StartRef > 0)
						{
							bcvStart = new BCVRef(currQuestion.StartRef);
							bcvEnd = new BCVRef(currQuestion.EndRef);
							if (startVerse < bcvStart.Verse)
								bcvStart.Verse = startVerse;
							if (endVerse > bcvEnd.Verse)
								bcvEnd.Verse = endVerse;
						}
						else
						{
							bcvStart = new BCVRef(currSection.StartRef);
							bcvEnd = new BCVRef(currSection.EndRef);
							bcvStart.Verse = startVerse;
							bcvEnd.Verse = endVerse;
						}
						currQuestion.StartRef = bcvStart.BBCCCVVV;
						currQuestion.EndRef = bcvEnd.BBCCCVVV;
						currQuestion.ScriptureReference = BCVRef.MakeReferenceString(
							currSection.ScriptureReference.Substring(0, 3), bcvStart, bcvEnd, ".", "-");
					}
					string[] source = currQuestion.Answers;
					currQuestion.Answers = new string[cAnswers + 1];
					if (source != null)
						Array.Copy(source, currQuestion.Answers, cAnswers);

					currQuestion.Answers[cAnswers++] = currAnswer;
				}
				else if (sLine.StartsWith(s_kCommentMarker))
				{
					if (currQuestion != null)
					{
						string[] source = currQuestion.Notes;
						currQuestion.Notes = new string[cComments + 1];
						if (source != null)
							Array.Copy(source, currQuestion.Notes, cComments);

						currQuestion.Notes[cComments++] = sLine.Substring(kCommentMarkerLen).Trim();
					}
				}
				else
				{
					if (sLine.StartsWith(s_kRefMarker))
					{
						currRef = sLine.Substring(kRefMarkerLen).Trim();
						Parse(currRef, out startRef, out endRef);
						if (!currSectionRefSet)
						{
							currSection.ScriptureReference = currRef;
							currSection.StartRef = startRef;
							currSection.EndRef = endRef;
							currSectionRefSet = true;
						}
					}
					else if (sLine.StartsWith(s_kSectionHead))
					{
						if (currentQuestions.Count > 0)
						{
							currCat.Questions = currentQuestions.ToArray();
							currentQuestions.Clear();
						}
						currSection = new Section();
						sections.Add(currSection);
						cCategories = 1;
						currSection.Categories = new Category[cCategories];
						currSection.Categories[0] = currCat = new Category();
						currSection.Heading = sLine.Substring(kSectHeadMarkerLen).Trim();
						currSectionRefSet = false;
					}
					else if (sLine.StartsWith(s_kOverviewMarker) || sLine.StartsWith(s_kDetailsMarker))
					{
						if (currentQuestions.Count > 0)
						{
							currCat.Questions = currentQuestions.ToArray();
							currentQuestions.Clear();
						}
						if (currCat.Type != null || currCat.Questions != null)
						{
							currCat = new Category();
							Category[] source = currSection.Categories;
							currSection.Categories = new Category[cCategories + 1];
							if (source != null)
								Array.Copy(source, currSection.Categories, cCategories);

							currSection.Categories[cCategories++] = currCat;
						}
						currCat.Type = sLine.Substring(kCategoryMarkerLen).Trim();
					}

					if (currQuestion != null)
					{
						currQuestion = null;
						cAnswers = 0;
						cComments = 0;
					}
				}
			}
			if (currCat != null && currentQuestions.Count > 0)
				currCat.Questions = currentQuestions.ToArray();

			QuestionSections questionSections = new QuestionSections();
			questionSections.Items = sections.ToArray();
			GenerateAlternateForms(questionSections, alternatives);
			return questionSections;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the source lines fromthe questions file.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static IEnumerable<string> SourceLines(string filename)
		{
			TextReader reader = null;
			try
			{
				reader = new StreamReader(filename, Encoding.UTF8);

				string sLine;
				while ((sLine = reader.ReadLine()) != null)
					yield return sLine;
			}
			finally
			{
				if (reader != null)
					reader.Close();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes the source lines and returns one field (possibly made up of more than one
		/// line) at a time.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		static IEnumerable<string> SourceFields(IEnumerable<string> sourceLines)
		{
			StringBuilder sb = new StringBuilder();
			foreach (string line in sourceLines)
			{
				if (line.StartsWith(@"\"))
				{
					if (sb.Length > 0)
						yield return sb.ToString();
					sb.Length = 0;
					sb.Append(line);
				}
				else
				{
					if (sb[sb.Length - 1] != ' ' && !line.StartsWith(" "))
						sb.Append(" ");
					sb.Append(line);
				}
			}
			if (sb.Length > 0)
				yield return sb.ToString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns an enumerator that iterates through the collection of questions.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to
		/// iterate through the collection.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public static void GenerateAlternateForms(QuestionSections sections, Dictionary<string, string[]> alternatives)
		{
			string[] altForms;
			foreach (Section section in sections.Items)
			{
				foreach (Category category in section.Categories)
				{
					if (category.Questions == null)
						continue;
					foreach (Question q in category.Questions)
					{
						if (alternatives != null && alternatives.TryGetValue(q.Text, out altForms))
							q.AlternateForms = altForms;
						else
						{
							QuestionProviderAlternateFormBuilder bldr = new QuestionProviderAlternateFormBuilder(q.Text);
							if (bldr.Alternatives.Count > 1)
							{
								q.AlternateForms = new string[bldr.Alternatives.Count];
								for (int index = 0; index < bldr.Alternatives.Count; index++)
									q.AlternateForms[index] = bldr.Alternatives[index];
							}
						}
					}
				}
			}
		}
	}
	#endregion

	#region QuestionProviderAlternateFormBuilder
	public class QuestionProviderAlternateFormBuilder
	{
		private readonly List<string> m_list = new List<string>();
		static Regex s_regexBracket = new Regex(@"\[(?<optionalPart>.+)\] ", RegexOptions.Compiled);
		static Regex s_regexParentheticalOr = new Regex(@" \(or, (?<alternateQestion>.+)\) *$", RegexOptions.Compiled);
		static Regex s_regexAlternatives = new Regex(@"(?<firstAlt>[a-zA-Z\-']+)/(?<secondAlt>[a-zA-Z\-']+)", RegexOptions.Compiled);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="QuestionProviderAlternateFormBuilder"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public QuestionProviderAlternateFormBuilder(string phrase)
		{
			Match match = s_regexParentheticalOr.Match(phrase);
			if (match.Success)
			{
				ProcessQuestion(match.Result("$`"));
				ProcessQuestion(match.Result("${alternateQestion}"));
			}
			else
			{
				foreach (string alt in phrase.Split(new[] { "//", "-OR-" }, StringSplitOptions.RemoveEmptyEntries))
					ProcessQuestion(alt);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes a single alternative word or phrase for a key term.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ProcessQuestion(string question)
		{
			Match match = s_regexBracket.Match(question);
			if (match.Success)
			{
				string sOptionalPartOmitted = match.Result("$`$'").Trim();
				// If bracketed part is the entire question, it should not be considered as optional (should never happen).
				if (sOptionalPartOmitted.Length > 0)
				{
					ProcessQuestion(match.Result("$`${optionalPart} $'"));
					ProcessQuestion(sOptionalPartOmitted);
					return;
				}
			}
			match = s_regexAlternatives.Match(question);
			if (match.Success)
			{
				string before = match.Result("$`");
				string after = match.Result("$'");
				List<string> alternatives = new List<string>();
				alternatives.Add(match.Result("${firstAlt}"));
				alternatives.Add(match.Result("${secondAlt}"));

				// Check for a multiple alternatives (e.g. "dog/wolf/fox")
				Match additionalMatch = s_regexAlternatives.Match(match.Result("${secondAlt}$'"));
				while (additionalMatch.Success && additionalMatch.Index == 0)
				{
					alternatives.Add(additionalMatch.Result("${secondAlt}"));
					after = additionalMatch.Result("$'");
					additionalMatch = s_regexAlternatives.Match(additionalMatch.Result("${secondAlt}$'"));
				}

				foreach (string alternative in alternatives)
					ProcessQuestion(before + alternative + after);
			}
			else
				m_list.Add(question.Trim());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the alternatives.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public List<string> Alternatives
		{
			get { return m_list; }
		}
	}
	#endregion
}