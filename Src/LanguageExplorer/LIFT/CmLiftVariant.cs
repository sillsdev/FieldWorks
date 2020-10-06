// Copyright (c) 2011-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using SIL.LCModel;
using SIL.Lift.Parsing;

namespace LanguageExplorer.LIFT
{
	/// <summary>
	/// This class implements "variant" from the LIFT standard.  (It represents an allomorph, not what
	/// FieldWorks understands to be a Variant.)
	/// It corresponds to MoForm (or one of its subclasses) in the FieldWorks model.
	/// </summary>
	internal sealed class CmLiftVariant : LiftObject
	{
		internal CmLiftVariant()
		{
			Relations = new List<CmLiftRelation>();
			Pronunciations = new List<CmLiftPhonetic>();
		}

		public override string XmlTag => "variant";

		internal ICmObject CmObject { get; set; }

		internal string Ref { get; set; }

		internal LiftMultiText Form { get; set; } // safe XML

		internal List<CmLiftPhonetic> Pronunciations { get; }

		internal List<CmLiftRelation> Relations { get; }

		internal string RawXml { get; set; }
	}
}