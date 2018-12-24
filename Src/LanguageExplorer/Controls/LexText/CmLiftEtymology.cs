// Copyright (c) 2011-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;
using SIL.Lift.Parsing;

namespace LanguageExplorer.Controls.LexText
{
	/// <summary>
	/// This class implements "etymology" from the LIFT standard.
	/// It corresponds to LexEtymology in the FieldWorks model.
	/// </summary>
	public class CmLiftEtymology : LiftObject, ICmLiftObject
	{
		public string Type { get; set; }

		public string Source { get; set; }

		public LiftMultiText Gloss { get; set; } // safe XML

		public LiftMultiText Form { get; set; } // safe XML

		public ICmObject CmObject { get; set; }

		public override string XmlTag => "etymology";
	}
}