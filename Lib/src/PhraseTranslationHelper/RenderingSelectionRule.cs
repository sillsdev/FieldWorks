// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2011' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: RenderingSelectionRule.cs
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Text.RegularExpressions;
using SIL.Utils;

namespace SILUBS.PhraseTranslationHelper
{
	#region class RenderingSelectionRule
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Little class to hold rules that govern which term rendering is selected based on
	/// regular expression matching against the original English question.
	/// (supports XML serialization)
	/// </summary>
	/// ------------------------------------------------------------------------------------
	[XmlType("RenderingSelectionRule")]
	public class RenderingSelectionRule
	{
		#region Data members
		private string m_questionMatchingPattern;
		private string m_renderingMatchingPattern;
		private string m_qVariable, m_rVariable;
		private static readonly Regex s_qSuffixMatchPattern = new Regex(@"^\{0\}\\w\*(?<var>\w*)\\b$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
		private static readonly Regex s_qPrefixMatchPattern = new Regex(@"^\\b(?<var>\w*)\\w\*\{0\}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
		private static readonly Regex s_precedingWordMatchPattern = new Regex(@"^\\b(?<var>\w*) \{0\}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
		private static readonly Regex s_followingWordMatchPattern = new Regex(@"^\{0\} (?<var>\w*)\\b$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
		private static readonly Regex s_rSuffixMatchPattern = new Regex(@"^(?<var>\w*)\$$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
		private static readonly Regex s_rPrefixMatchPattern = new Regex(@"^\^(?<var>\w*)", RegexOptions.Compiled | RegexOptions.CultureInvariant);
		#endregion

		internal enum QuestionMatchType
		{
			Undefined,
			Suffix,
			Prefix,
			PrecedingWord,
			FollowingWord,
			Custom,
		}

		internal enum RenderingMatchType
		{
			Undefined,
			Suffix,
			Prefix,
			Custom,
		}

		#region Public (XML) properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the name of the rule.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlAttribute("name")]
		public string Name { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets whether this rule is disabled.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlAttribute("disabled")]
		public bool Disabled { get; set; }

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the original phrase.
		/// </summary>
		/// --------------------------------------------------------------------------------
		[XmlAttribute("questionMatcher")]
		public string QuestionMatchingPattern
		{
			get { return m_questionMatchingPattern; }
			set
			{
				ErrorMessageQ = null;
				if (value == null)
				{
					m_questionMatchingPattern = null;
					m_qVariable = null;
					return;
				}
				m_questionMatchingPattern = value.Normalize(NormalizationForm.FormC);
				if (!m_questionMatchingPattern.Contains("{0}"))
					ErrorMessageQ = Properties.Resources.kstidKeyTermPlaceHolderMissing;
				else
				{
					try
					{
						new Regex(string.Format(m_questionMatchingPattern, "term"), RegexOptions.CultureInvariant);
					}
					catch (ArgumentException ex)
					{
						ErrorMessageQ = ex.Message;
					}
				}
			}
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the translation.
		/// </summary>
		/// --------------------------------------------------------------------------------
		[XmlAttribute("renderingSelector")]
		public string RenderingMatchingPattern
		{
			get { return m_renderingMatchingPattern; }
			set
			{
				ErrorMessageR = null;
				if (value == null)
				{
					m_renderingMatchingPattern = null;
					m_rVariable = null;
					return;
				}
				m_renderingMatchingPattern = value.Normalize(NormalizationForm.FormC);
				if (!string.IsNullOrEmpty(m_questionMatchingPattern) && ErrorMessageQ == null)
				{
					try
					{
						Regex.Replace("term", string.Format(m_questionMatchingPattern, "term"),
							m_renderingMatchingPattern, RegexOptions.CultureInvariant);
					}
					catch (ArgumentException ex)
					{
						ErrorMessageR = ex.Message;
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a specific term to which this rule applies. If null, then this rule
		/// can apply to any term.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlAttribute("term")]
		public string SpecificTerm { get; set; }
		#endregion

		#region Constructors
		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="RenderingSelectionRule"/> class,
		/// needed for XML serialization.
		/// </summary>
		/// --------------------------------------------------------------------------------
		public RenderingSelectionRule()
		{
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="RenderingSelectionRule"/> class
		/// </summary>
		/// --------------------------------------------------------------------------------
		public RenderingSelectionRule(string name)
		{
			Name = name;
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="RenderingSelectionRule"/> class
		/// </summary>
		/// --------------------------------------------------------------------------------
		public RenderingSelectionRule(string questionMatchingPattern, string replacement)
		{
			QuestionMatchingPattern = questionMatchingPattern;
			RenderingMatchingPattern = replacement;
		}
		#endregion

		#region Public Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines if this rule applies to the given question, and if so, will attempt to
		/// select a rendering to use.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string ChooseRendering(string question, IEnumerable<Word> term, IEnumerable<string> renderings)
		{
			if (!Valid)
				return null;

			Regex regExQuestion = null;
			try
			{
				regExQuestion = new Regex(string.Format(m_questionMatchingPattern, "(?i:" + term.ToString(@"\W+") + ")"), RegexOptions.CultureInvariant);
			}
			catch (ArgumentException ex)
			{
				ErrorMessageQ = ex.Message;
			}

			if (regExQuestion != null && regExQuestion.IsMatch(question))
			{
				Regex regExRendering;
				try
				{
					regExRendering = new Regex(m_renderingMatchingPattern, RegexOptions.Compiled | RegexOptions.CultureInvariant);
				}
				catch (ArgumentException ex)
				{
					ErrorMessageR = ex.Message;
					return null;
				}
				return renderings.FirstOrDefault(rendering => regExRendering.IsMatch(rendering.Normalize(NormalizationForm.FormC)));
			}
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a <see cref="System.String"/> that represents this instance.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string ToString()
		{
			return Name ?? m_questionMatchingPattern + ":" + m_renderingMatchingPattern;
		}
		#endregion

		#region Internal (non-XML) Properties
		internal bool Valid
		{
			get
			{
				return ErrorMessageQ == null && ErrorMessageR == null;
			}
		}

		internal string ErrorMessageQ { get; private set; }
		internal string ErrorMessageR { get; private set; }

		internal QuestionMatchType QuestionMatchCriteriaType
		{
			get
			{
				m_qVariable = null;
				if (string.IsNullOrEmpty(m_questionMatchingPattern))
					return QuestionMatchType.Undefined;
				QuestionMatchType type;
				Match match = s_qSuffixMatchPattern.Match(m_questionMatchingPattern);
				if (match.Success)
					type = QuestionMatchType.Suffix;
				else
				{
					match = s_qPrefixMatchPattern.Match(m_questionMatchingPattern);
					if (match.Success)
						type = QuestionMatchType.Prefix;
					else
					{
						match = s_precedingWordMatchPattern.Match(m_questionMatchingPattern);
						if (match.Success)
							type = QuestionMatchType.PrecedingWord;
						else
						{
							match = s_followingWordMatchPattern.Match(m_questionMatchingPattern);
							if (match.Success)
								type = QuestionMatchType.FollowingWord;
							else if (m_questionMatchingPattern.Contains("{0}"))
							{
								m_qVariable = m_questionMatchingPattern;
								return QuestionMatchType.Custom;
							}
							else
								return QuestionMatchType.Undefined;
						}
					}
				}
				m_qVariable = match.Result("${var}");
				return type;
			}
		}

		internal RenderingMatchType RenderingMatchCriteriaType
		{
			get
			{
				m_rVariable = null;
				if (string.IsNullOrEmpty(m_renderingMatchingPattern))
					return RenderingMatchType.Undefined;

				RenderingMatchType type;
				Match match = s_rSuffixMatchPattern.Match(m_renderingMatchingPattern);
				if (match.Success)
					type = RenderingMatchType.Suffix;
				else
				{
					match = s_rPrefixMatchPattern.Match(m_renderingMatchingPattern);
					if (match.Success)
						type = RenderingMatchType.Prefix;
					else
					{
						m_rVariable = m_renderingMatchingPattern;
						return RenderingMatchType.Custom;
					}
				}
				m_rVariable = match.Result("${var}");
				return type;
			}
		}

		internal string Description
		{
			get
			{
				string questionMatchCriteria, renderingMatchCriteria;
				switch (QuestionMatchCriteriaType)
				{
					case QuestionMatchType.Suffix:
						questionMatchCriteria = Properties.Resources.kstidRenderingSelectionRuleQuestionConditionEndsWith;
						break;
					case QuestionMatchType.Prefix:
						questionMatchCriteria = Properties.Resources.kstidRenderingSelectionRuleQuestionConditionStartsWith;
						break;
					case QuestionMatchType.PrecedingWord:
						questionMatchCriteria = Properties.Resources.kstidRenderingSelectionRuleQuestionConditionPrecededBy;
						break;
					case QuestionMatchType.FollowingWord:
						questionMatchCriteria = Properties.Resources.kstidRenderingSelectionRuleQuestionConditionFollowedBy;
						break;
					case QuestionMatchType.Custom:
						questionMatchCriteria = Properties.Resources.kstidRenderingSelectionRuleQuestionConditionCustom;
						break;
					default:
						return string.Empty;
				}
				questionMatchCriteria = string.Format(questionMatchCriteria, m_qVariable);
				switch (RenderingMatchCriteriaType)
				{
					case RenderingMatchType.Suffix:
						renderingMatchCriteria = Properties.Resources.kstidRenderingSelectionCriteriaEndsWith;
						break;
					case RenderingMatchType.Prefix:
						renderingMatchCriteria = Properties.Resources.kstidRenderingSelectionCriteriaStartsWith;
						break;
					case RenderingMatchType.Custom:
						renderingMatchCriteria = Properties.Resources.kstidRenderingSelectionCriteriaCustom;
						break;
					default:
						return string.Empty;
				}
				renderingMatchCriteria = string.Format(renderingMatchCriteria, m_rVariable);

				return string.Format(Properties.Resources.kstidRenderingSelectionConditionResultFrame,
					questionMatchCriteria, renderingMatchCriteria);
			}
		}

		internal string QuestionMatchSuffix
		{
			get
			{
				return QuestionMatchCriteriaType == QuestionMatchType.Suffix ? m_qVariable : null;
			}
			set
			{
				m_qVariable = value;
				QuestionMatchingPattern = (string.IsNullOrEmpty(value)) ? null : @"{0}\w*" + value + @"\b";
			}
		}

		internal string QuestionMatchPrefix
		{
			get
			{
				return QuestionMatchCriteriaType == QuestionMatchType.Prefix ? m_qVariable : null;
			}
			set
			{
				m_qVariable = value;
				QuestionMatchingPattern = (string.IsNullOrEmpty(value)) ? null : @"\b" + value + @"\w*{0}";
			}
		}

		internal string QuestionMatchPrecedingWord
		{
			get
			{
				return QuestionMatchCriteriaType == QuestionMatchType.PrecedingWord ? m_qVariable : null;
			}
			set
			{
				m_qVariable = value;
				QuestionMatchingPattern = (string.IsNullOrEmpty(value)) ? null : @"\b" + value + " {0}";
			}
		}

		internal string QuestionMatchFollowingWord
		{
			get
			{
				return QuestionMatchCriteriaType == QuestionMatchType.FollowingWord ? m_qVariable : null;
			}
			set
			{
				m_qVariable = value;
				QuestionMatchingPattern = (string.IsNullOrEmpty(value)) ? null : "{0} " + value + @"\b";
			}
		}

		internal string RenderingMatchSuffix
		{
			get
			{
				return RenderingMatchCriteriaType == RenderingMatchType.Suffix ? m_rVariable : null;
			}
			set
			{
				m_rVariable = value;
				RenderingMatchingPattern = (string.IsNullOrEmpty(value)) ? null : value + "$";
			}
		}

		internal string RenderingMatchPrefix
		{
			get
			{
				return RenderingMatchCriteriaType == RenderingMatchType.Prefix ? m_rVariable : null;
			}
			set
			{
				m_rVariable = value;
				RenderingMatchingPattern = (string.IsNullOrEmpty(value)) ? null : "^" + value;
			}
		}
		#endregion
	}
	#endregion
}