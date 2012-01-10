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
// File: PhraseCustomization.cs
// ---------------------------------------------------------------------------------------------
using System.Xml.Serialization;

namespace SILUBS.PhraseTranslationHelper
{
	#region class PhraseCustomization
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Little class to support XML serialization of customizations (additions/changes/deletions
	/// </summary>
	/// ------------------------------------------------------------------------------------
	[XmlType("PhraseCustomization")]
	public class PhraseCustomization
	{
		#region CustomizationType enumeration
		public enum CustomizationType
		{
			Modification,
			Deletion,
			InsertionBefore,
			AdditionAfter,
		}
		#endregion

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the reference.
		/// </summary>
		/// --------------------------------------------------------------------------------
		[XmlAttribute("ref")]
		public string Reference { get; set; }
		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the original phrase.
		/// </summary>
		/// --------------------------------------------------------------------------------
		public string OriginalPhrase { get; set; }
		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the edited/customized phrase.
		/// </summary>
		/// --------------------------------------------------------------------------------
		public string ModifiedPhrase { get; set; }
		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the translation.
		/// </summary>
		/// --------------------------------------------------------------------------------
		[XmlAttribute("type")]
		public CustomizationType Type { get; set; }
		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="XmlTranslation"/> class, needed
		/// for XML serialization.
		/// </summary>
		/// --------------------------------------------------------------------------------
		public PhraseCustomization()
		{
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="XmlTranslation"/> class.
		/// </summary>
		/// --------------------------------------------------------------------------------
		public PhraseCustomization(TranslatablePhrase tp)
		{
			Reference = tp.Reference;
			OriginalPhrase = tp.OriginalPhrase;
			ModifiedPhrase = tp.ModifiedPhrase;
			if (tp.IsExcluded)
				Type = CustomizationType.Deletion;
			else
			{
				//if (tp.OriginalPhrase
				//Type = tp.Translation;
				Type = CustomizationType.Modification;
			}
		}
	}
	#endregion
}
