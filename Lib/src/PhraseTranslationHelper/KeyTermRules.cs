// ---------------------------------------------------------------------------------------------
// Copyright (c) 2011-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: KeyTermRule.cs
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace SILUBS.PhraseTranslationHelper
{
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Little class to support XML serialization
	/// </summary>
	/// ------------------------------------------------------------------------------------
	[Serializable]
	[XmlType(AnonymousType=true)]
	[XmlRoot(Namespace="", IsNullable=false)]
	public class KeyTermRules
	{
		[XmlElement("KeyTermRule", Form=XmlSchemaForm.Unqualified)]
		public List<KeyTermRule> Items { get; set; }
	}

	[Serializable]
	[XmlType(AnonymousType=true)]
	public class KeyTermRule
	{

		[Serializable]
		public enum RuleType
		{
			MatchForRefOnly,
			Exclude,
		}

		private string m_id;

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the list of alternate forms of the terms.
		/// </summary>
		/// --------------------------------------------------------------------------------
		[XmlElement("Alternate", Form=XmlSchemaForm.Unqualified)]
		public KeyTermRulesKeyTermRuleAlternate[] Alternates { get; set; }

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the name.
		/// </summary>
		/// --------------------------------------------------------------------------------
		[XmlAttribute]
		public string id
		{
			get { return m_id; }
			set { m_id = value.ToLowerInvariant().Normalize(NormalizationForm.FormD); }
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
				return Rule == null ? null : Rule.ToString();
			}
			set
			{
				if (value == null || !Enum.IsDefined(typeof(RuleType), value))
					Rule = null;
				else
					Rule = (RuleType)Enum.Parse(typeof(RuleType), value, true);
			}
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the rule.
		/// </summary>
		/// --------------------------------------------------------------------------------
		[XmlIgnore]
		public RuleType? Rule { get; set; }
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Class to represent an alternate form of a key term
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[Serializable]
	[XmlType(AnonymousType=true)]
	public class KeyTermRulesKeyTermRuleAlternate
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
	}
}