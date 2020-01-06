// Copyright (c) 2011-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SIL.FieldWorks.LexicalProvider
{
	/// <summary>
	/// Data contract used by WCF for holding information about a Sense
	/// </summary>
	[DataContract(Namespace = "LexicalData")]
	public sealed class LexSense
	{
		/// <summary />
		public LexSense(string id)
		{
			Id = id;
			Glosses = new List<LexGloss>();
		}

		/// <summary>
		/// Gets or sets the id.
		/// </summary>
		[DataMember]
		public string Id { get; private set; }

		/// <summary>
		/// Gets or sets the glosses.
		/// </summary>
		[DataMember]
		public IList<LexGloss> Glosses { get; set; }
	}
}