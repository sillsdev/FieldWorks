// Copyright (c) 2011-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SIL.FieldWorks.LexicalProvider
{
	/// <summary>
	/// Data contract used by WCF for holding information about a Lexeme
	/// </summary>
	[DataContract(Namespace = "LexicalData")]
	public sealed class LexicalEntry
	{
		/// <summary />
		public LexicalEntry(LexemeType type, string form, int homograph)
		{
			Type = type;
			LexicalForm = form;
			Homograph = homograph;
			Senses = new List<LexSense>();
		}

		/// <summary>
		/// Gets or sets the type.
		/// </summary>
		[DataMember]
		public LexemeType Type { get; private set; }

		/// <summary>
		/// Gets or sets the lexical form.
		/// </summary>
		[DataMember]
		public string LexicalForm { get; private set; }

		/// <summary>
		/// Gets or sets the homograph.
		/// </summary>
		[DataMember]
		public int Homograph { get; private set; }

		/// <summary>
		/// Gets or sets the senses.
		/// </summary>
		[DataMember]
		public IList<LexSense> Senses { get; set; }
	}
}