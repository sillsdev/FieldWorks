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
// File: Substitution.cs
// ---------------------------------------------------------------------------------------------
using System.Text;
using System.Xml.Serialization;
using System.Text.RegularExpressions;
using System;

namespace SILUBS.PhraseTranslationHelper
{
	#region class Substitution
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Little class to hold rules about phrases that are substituted before parsing
	/// (supports XML serialization)
	/// </summary>
	/// ------------------------------------------------------------------------------------
	[XmlType("Substitution")]
	public class Substitution
	{
		#region Data members
		private Regex m_regEx;
		private string m_matchingPattern;
		private bool m_isRegex;
		private bool m_matchCase;
		private bool m_isValid = true;
		#endregion

		#region Public (XML) properties
		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the original phrase.
		/// </summary>
		/// --------------------------------------------------------------------------------
		[XmlAttribute("pattern")]
		public string MatchingPattern
		{
			get { return m_matchingPattern; }
			set
			{
				m_matchingPattern = value;
				m_regEx = null;
			}
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the translation.
		/// </summary>
		/// --------------------------------------------------------------------------------
		[XmlAttribute("replacement")]
		public string Replacement { get; set; }

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the reference.
		/// </summary>
		/// --------------------------------------------------------------------------------
		[XmlAttribute("regex")]
		public bool IsRegex
		{
			get { return m_isRegex; }
			set
			{
				m_isRegex = value;
				m_regEx = null;
			}
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets whether the macth is case-sensitive.
		/// </summary>
		/// --------------------------------------------------------------------------------
		[XmlAttribute("case")]
		public bool MatchCase
		{
			get { return m_matchCase; }
			set
			{
				m_matchCase = value;
				m_regEx = null;
			}
		}
		#endregion

		#region Constructors
		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="Substitution"/> class, needed
		/// for XML serialization.
		/// </summary>
		/// --------------------------------------------------------------------------------
		public Substitution()
		{
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="Substitution"/> class.
		/// </summary>
		/// --------------------------------------------------------------------------------
		public Substitution(string matchingPattern, string replacement, bool regEx,
			bool matchCase)
		{
			MatchingPattern = matchingPattern;
			Replacement = replacement;
			IsRegex = regEx;
			MatchCase = matchCase;
		}
		#endregion

		#region Internal (non-XML) Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a regular expression object representing this substitution (regardless of
		/// whether this substitution is marked as a regular expression).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal Regex RegEx
		{
			get
			{
				if (m_regEx == null)
				{
					string pattern = MatchingPattern.Normalize(NormalizationForm.FormD);
					if (!IsRegex)
						pattern = Regex.Escape(pattern);
					RegexOptions options = RegexOptions.Compiled | RegexOptions.CultureInvariant;
					if (!MatchCase)
						options |= RegexOptions.IgnoreCase;
					try
					{
						m_regEx = new Regex(pattern, options);
						m_isValid = true;
					}
					catch(ArgumentException ex)
					{
						if (!IsRegex)
							throw; // Not sure what else to do - hopefully this can't happen.

						ErrorMessage = ex.Message;
						//System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PhraseSubstitutionsDlg));
						//MessageBox.Show(ErrorMessage, resources.GetString("$this.Text"));
						m_regEx = new Regex(string.Empty);
					}
				}
				return m_regEx;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a replacement string suitable for using in a regular expression replacement.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal string RegExReplacementString
		{
			get
			{
				return (string.IsNullOrEmpty(Replacement)) ? string.Empty :
					Replacement.Normalize(NormalizationForm.FormD); // ToLowerInvariant()?
			}
		}

		internal bool Valid
		{
			get
			{
				m_isValid &= RegEx.ToString().Length > 0;
				return m_isValid;
			}
		}

		internal string ErrorMessage { get; private set; }
		#endregion
	}
	#endregion
}