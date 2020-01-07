// Copyright (c) 2014-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.LCModel;

namespace SIL.FieldWorks.WordWorks.Parser
{
	public class ParseMorph : IEquatable<ParseMorph>
	{
		public ParseMorph(IMoForm form, IMoMorphSynAnalysis msa)
			: this(form, msa, null)
		{
		}

		public ParseMorph(IMoForm form, IMoMorphSynAnalysis msa, ILexEntryInflType inflType)
		{
			Form = form;
			Msa = msa;
			InflType = inflType;
		}

		public IMoForm Form { get; }

		public IMoMorphSynAnalysis Msa { get; }

		public ILexEntryInflType InflType { get; }

		public bool IsValid => Form.IsValidObject && Msa.IsValidObject && (InflType == null || InflType.IsValidObject);

		public bool Equals(ParseMorph other)
		{
			return Form == other.Form && Msa == other.Msa && InflType == other.InflType;
		}

		public override bool Equals(object obj)
		{
			var other = obj as ParseMorph;
			return other != null && Equals(other);
		}

		public override int GetHashCode()
		{
			var code = 23;
			code = code * 31 + Form.Guid.GetHashCode();
			code = code * 31 + Msa.Guid.GetHashCode();
			code = code * 31 + (InflType == null ? 0 : InflType.Guid.GetHashCode());
			return code;
		}
	}
}