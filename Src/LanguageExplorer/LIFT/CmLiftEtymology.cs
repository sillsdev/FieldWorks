// Copyright (c) 2011-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;
using SIL.Lift.Parsing;

namespace LanguageExplorer.LIFT
{
	/// <summary>
	/// This class implements "etymology" from the LIFT standard.
	/// It corresponds to LexEtymology in the FieldWorks model.
	/// </summary>
	internal sealed class CmLiftEtymology : LiftObject
	{
		public override string XmlTag => "etymology";

		internal string Type { get; set; }

		internal string Source { get; set; }

		internal LiftMultiText Gloss { get; set; } // safe XML

		internal LiftMultiText Form { get; set; } // safe XML

		internal ICmObject CmObject { get; set; }
	}
}