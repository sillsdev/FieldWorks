// Copyright (c) 2011-2019 SIL International
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
	public class CmLiftVariant : LiftObject, ICmLiftObject
	{
		public CmLiftVariant()
		{
			Relations = new List<CmLiftRelation>();
			Pronunciations = new List<CmLiftPhonetic>();
		}

		public ICmObject CmObject { get; set; }

		public string Ref { get; set; }

		public LiftMultiText Form { get; set; } // safe XML

		public List<CmLiftPhonetic> Pronunciations { get; }

		public List<CmLiftRelation> Relations { get; }

		public string RawXml { get; set; }

		public override string XmlTag => "variant";
	}
}