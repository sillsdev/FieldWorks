// Copyright (c) 2014-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.LCModel;

namespace SIL.FieldWorks.WordWorks.Parser
{
	public sealed class ParseMorph : IEquatable<ParseMorph>
	{
		internal ParseMorph(IMoForm form, IMoMorphSynAnalysis msa)
			: this(form, msa, null)
		{
		}

		internal ParseMorph(IMoForm form, IMoMorphSynAnalysis msa, ILexEntryInflType inflType)
		{
			Form = form;
			Msa = msa;
			InflType = inflType;
		}

		public IMoForm Form { get; }

		internal IMoMorphSynAnalysis Msa { get; }

		internal ILexEntryInflType InflType { get; }

		internal bool IsValid => Form.IsValidObject && Msa.IsValidObject && (InflType == null || InflType.IsValidObject);

		public bool Equals(ParseMorph other)
		{
			return Form == other.Form && Msa == other.Msa && InflType == other.InflType;
		}

		public override bool Equals(object obj)
		{
			return obj is ParseMorph other && Equals(other);
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