// ---------------------------------------------------------------------------------------------
// Copyright (c) 2011-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: KeyTermRule.cs
// Responsibility: bogle
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace SILUBS.PhraseTranslationHelper
{
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Little class to support XML serialization
	/// </summary>
	/// ------------------------------------------------------------------------------------
	public class KeyTermRule
	{
		public enum RuleType
		{
			None,
			MatchForRefOnly,
			Exclude,
		}

		private string m_name;

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="KeyTermRule"/> class, needed
		/// for XML serialization.
		/// </summary>
		/// --------------------------------------------------------------------------------
		public KeyTermRule()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="KeyTermRule"/> class, needed for unit
		/// tests.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public KeyTermRule(string name, RuleType ruleType)
		{
			Name = name;
			Rule = ruleType;
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the name.
		/// </summary>
		/// --------------------------------------------------------------------------------
		[Xml("id")]
		public string Name
		{
			get { return m_name; }
			set { m_name = value.ToLowerInvariant().Normalize(NormalizationForm.FormD); }
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the rule.
		/// </summary>
		/// --------------------------------------------------------------------------------
		[XmlAttribute("rule")]
		public string RuleStr
		{
			get
			{
				return Rule == RuleType.None ? null : Rule.ToString();
			}
			set
			{
				try
				{
					Rule = (RuleType)Enum.Parse(typeof(RuleType), value);
				}
				catch
				{
					Rule = RuleType.None;
				}
			}
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the rule.
		/// </summary>
		/// --------------------------------------------------------------------------------
		[XmlEnum()]
		public RuleType Rule { get; set; }

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the list of alternate forms of the terms.
		/// </summary>
		/// --------------------------------------------------------------------------------
		[XmlArrayItem("Alternate")]
		public List<KeyTermAlternate> Alternates { get; set; }
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class KeyTermAlternate
	{
		private string m_name;
		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the name.
		/// </summary>
		/// --------------------------------------------------------------------------------
		[XmlAttribute("name")]
		public string Name
		{
			get { return m_name; }
			set { m_name = value.ToLowerInvariant().Normalize(NormalizationForm.FormD); }
		}

		public KeyTermAlternate()
		{
		}
	}
}
