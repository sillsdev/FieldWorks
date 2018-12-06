// Copyright (c) 2014-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace SIL.FieldWorks.WordWorks.Parser
{
	public class ParseResult : IEquatable<ParseResult>
	{
		public ParseResult(string errorMessage)
			: this(Enumerable.Empty<ParseAnalysis>(), errorMessage)
		{
		}

		public ParseResult(IEnumerable<ParseAnalysis> analyses)
			: this(analyses, null)
		{
		}

		public ParseResult(IEnumerable<ParseAnalysis> analyses, string errorMessage)
		{
			Analyses = new ReadOnlyCollection<ParseAnalysis>(analyses.ToArray());
			ErrorMessage = errorMessage;
		}

		public ReadOnlyCollection<ParseAnalysis> Analyses { get; }

		public string ErrorMessage { get; }

		public bool IsValid
		{
			get { return Analyses.All(analysis => analysis.IsValid); }
		}

		public bool Equals(ParseResult other)
		{
			return Analyses.SequenceEqual(other.Analyses) && ErrorMessage == other.ErrorMessage;
		}

		public override bool Equals(object obj)
		{
			var other = obj as ParseResult;
			return other != null && Equals(other);
		}

		public override int GetHashCode()
		{
			var code = 23;
			foreach (var analysis in Analyses)
			{
				code = code * 31 + analysis.GetHashCode();
			}
			return code * 31 + (ErrorMessage == null ? 0 : ErrorMessage.GetHashCode());
		}
	}
}