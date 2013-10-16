using System.Xml;
using Palaso.WritingSystems;
using Palaso.WritingSystems.Collation;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.CoreImpl
{
	/// <summary>
	///
	/// </summary>
	public interface IWritingSystem : ILgWritingSystem
	{
		/// <summary>
		/// Gets or sets the abbreviation.
		/// </summary>
		/// <value>The abbreviation.</value>
		string Abbreviation
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the sort rules type.
		/// </summary>
		/// <value>The sort rules type.</value>
		WritingSystemDefinition.SortRulesType SortUsing
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the sort rules.
		/// </summary>
		/// <value>The sort rules.</value>
		string SortRules
		{
			get;
			set;
		}

		/// <summary>
		/// Returns an ICollator interface that can be used to sort strings based
		/// on the custom collation rules.
		/// </summary>
		ICollator Collator
		{
			get;
		}

		/// <summary>
		/// Gets or sets the language subtag.
		/// </summary>
		/// <value>The language.</value>
		LanguageSubtag LanguageSubtag
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the script subtag.
		/// </summary>
		/// <value>The script.</value>
		ScriptSubtag ScriptSubtag
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the region subtag.
		/// </summary>
		/// <value>The region.</value>
		RegionSubtag RegionSubtag
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the variant subtag.
		/// </summary>
		/// <value>The variant.</value>
		VariantSubtag VariantSubtag
		{
			get;
			set;
		}

		/// <summary>
		/// Gets the displayable name for the ISO code, built from LanguageName, ScriptName,
		/// RegionName, and VariantName.
		/// </summary>
		/// <value>The display label.</value>
		string DisplayLabel
		{
			get;
		}

		/// <summary>
		/// Gets the icu locale.
		/// </summary>
		/// <value>The icu locale.</value>
		string IcuLocale
		{
			get;
		}

		/// <summary>
		/// Gets the RFC5646 language tag.  (This is preferred over the IcuLocale.)
		/// </summary>
		/// <value>The RFC5646 language tag.</value>
		string RFC5646
		{
			get;
		}

		/// <summary>
		/// Gets or sets the valid chars.
		/// </summary>
		/// <value>The valid chars.</value>
		string ValidChars
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the matched pairs.
		/// </summary>
		/// <value>The matched pairs.</value>
		string MatchedPairs
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the punctuation patterns.
		/// </summary>
		/// <value>The punctuation patterns.</value>
		string PunctuationPatterns
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the quotation marks.
		/// </summary>
		/// <value>The quotation marks.</value>
		string QuotationMarks
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the legacy mapping.
		/// </summary>
		/// <value>The legacy mapping.</value>
		string LegacyMapping
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets a value indicating whether Graphite is enabled for this writing system.
		/// </summary>
		/// <value><c>true</c> if Graphite is enabled, otherwise <c>false</c>.</value>
		bool IsGraphiteEnabled
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="IWritingSystem"/> has been modified.
		/// </summary>
		/// <value><c>true</c> if modified, otherwise <c>false</c>.</value>
		bool Modified
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="IWritingSystem"/> will be deleted.
		/// </summary>
		/// <value><c>true</c> if it will be deleted, otherwise <c>false</c>.</value>
		bool MarkedForDeletion
		{
			get;
			set;
		}

		/// <summary>
		/// Copies all of the properties from the source writing system to this writing system.
		/// </summary>
		/// <param name="source">The source writing system.</param>
		void Copy(IWritingSystem source);

		/// <summary>
		/// Validates the collation rules.
		/// </summary>
		/// <param name="message">The error message.</param>
		/// <returns></returns>
		bool ValidateCollationRules(out string message);

		/// <summary>
		/// Gets the writing system manager.
		/// </summary>
		/// <value>The writing system manager.</value>
		IWritingSystemManager WritingSystemManager
		{
			get;
		}

		/// <summary>
		/// Writes an LDML representation of this writing system to the specified writer.
		/// </summary>
		/// <param name="writer">The writer.</param>
		void WriteLdml(XmlWriter writer);
	}
}
