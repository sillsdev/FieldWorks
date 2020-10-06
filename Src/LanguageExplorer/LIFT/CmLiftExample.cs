// Copyright (c) 2011-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using SIL.LCModel;
using SIL.Lift.Parsing;

namespace LanguageExplorer.LIFT
{
	/// <summary>
	/// This class implements "example" from the LIFT standard.
	/// It corresponds to LexExampleSentence in the FieldWorks model.
	/// </summary>
	internal sealed class CmLiftExample : LiftObject
	{
		internal CmLiftExample()
		{
			Notes = new List<CmLiftNote>();
			Translations = new List<LiftTranslation>();
		}

		public override string XmlTag => "example";

		internal ICmObject CmObject { get; set; }

		internal string Source { get; set; }

		internal LiftMultiText Content { get; set; } // safe XML

		internal List<LiftTranslation> Translations { get; }

		internal List<CmLiftNote> Notes { get; }
	}
}