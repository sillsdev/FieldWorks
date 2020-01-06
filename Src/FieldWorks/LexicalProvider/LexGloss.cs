// Copyright (c) 2011-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Runtime.Serialization;

namespace SIL.FieldWorks.LexicalProvider
{
	/// <summary>
	/// Data contract used by WCF for holding information about a Gloss
	/// </summary>
	[DataContract(Namespace = "LexicalData")]
	public sealed class LexGloss
	{
		/// <summary />
		public LexGloss(string language, string text)
		{
			Language = language;
			Text = text;
		}

		/// <summary>
		/// Gets or sets the language.
		/// </summary>
		[DataMember]
		public string Language { get; private set; }

		/// <summary>
		/// Gets or sets the text.
		/// </summary>
		[DataMember]
		public string Text { get; private set; }
	}
}