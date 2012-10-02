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