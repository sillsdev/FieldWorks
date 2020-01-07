// Copyright (c) 2014-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace SIL.FieldWorks.WordWorks.Parser
{
	public class ParseAnalysis : IEquatable<ParseAnalysis>
	{
		public ParseAnalysis(IEnumerable<ParseMorph> morphs)
		{
			Morphs = new ReadOnlyCollection<ParseMorph>(morphs.ToArray());
		}

		public ReadOnlyCollection<ParseMorph> Morphs { get; }

		public bool IsValid
		{
			get { return Morphs.All(morph => morph.IsValid); }
		}

		public bool Equals(ParseAnalysis other)
		{
			return Morphs.SequenceEqual(other.Morphs);
		}

		public override bool Equals(object obj)
		{
			var other = obj as ParseAnalysis;
			return other != null && Equals(other);
		}

		public override int GetHashCode()
		{
			var code = 23;
			foreach (var morph in Morphs)
			{
				code = code * 31 + morph.GetHashCode();
			}
			return code;
		}
	}
}