// Copyright (c) 2014-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace SIL.FieldWorks.WordWorks.Parser
{
	public sealed class ParseResult : IEquatable<ParseResult>
	{
		internal ParseResult(string errorMessage)
			: this(Enumerable.Empty<ParseAnalysis>(), errorMessage)
		{
		}

		internal ParseResult(IEnumerable<ParseAnalysis> analyses)
			: this(analyses, null)
		{
		}

		internal ParseResult(IEnumerable<ParseAnalysis> analyses, string errorMessage)
		{
			Analyses = new ReadOnlyCollection<ParseAnalysis>(analyses.ToArray());
			ErrorMessage = errorMessage;
		}

		public ReadOnlyCollection<ParseAnalysis> Analyses { get; }

		internal string ErrorMessage { get; }

		internal bool IsValid
		{
			get { return Analyses.All(analysis => analysis.IsValid); }
		}

		public bool Equals(ParseResult other)
		{
			return Analyses.SequenceEqual(other.Analyses) && ErrorMessage == other.ErrorMessage;
		}

		public override bool Equals(object obj)
		{
			return obj is ParseResult other && Equals(other);
		}

		public override int GetHashCode()
		{
			return Analyses.Aggregate(23, (current, analysis) => current * 31 + analysis.GetHashCode()) * 31 + (ErrorMessage == null ? 0 : ErrorMessage.GetHashCode());
		}
	}
}