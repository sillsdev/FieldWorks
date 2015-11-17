// ---------------------------------------------------------------------------------------------
// Copyright (c) 2012-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: XmlTranslation.cs
// ---------------------------------------------------------------------------------------------
using System.Xml.Serialization;

namespace SILUBS.PhraseTranslationHelper
{
	#region class XmlTranslation
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Little class to support XML serialization
	/// </summary>
	/// ------------------------------------------------------------------------------------
	[XmlType("Translation")]
	public class XmlTranslation
	{
		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the reference.
		/// </summary>
		/// --------------------------------------------------------------------------------
		[XmlAttribute("ref")]
		public string Reference { get; set; }
		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the phrase key (typically the text of the question in English.
		/// </summary>
		/// --------------------------------------------------------------------------------
		[XmlElement("OriginalPhrase")]
		public string PhraseKey { get; set; }
		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the translation.
		/// </summary>
		/// --------------------------------------------------------------------------------
		public string Translation { get; set; }
		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="XmlTranslation"/> class, needed
		/// for XML serialization.
		/// </summary>
		/// --------------------------------------------------------------------------------
		public XmlTranslation()
		{
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="XmlTranslation"/> class.
		/// </summary>
		/// --------------------------------------------------------------------------------
		public XmlTranslation(TranslatablePhrase tp)
		{
			Reference = tp.PhraseKey.ScriptureReference;
			PhraseKey = tp.PhraseKey.Text;
			Translation = tp.Translation;
		}
	}
	#endregion
}
