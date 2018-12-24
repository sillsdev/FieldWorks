// Copyright (c) 2011-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using SIL.LCModel;
using SIL.Lift.Parsing;

namespace LanguageExplorer.Controls.LexText
{
	/// <summary>
	/// This class implements "example" from the LIFT standard.
	/// It corresponds to LexExampleSentence in the FieldWorks model.
	/// </summary>
	public class CmLiftExample : LiftObject, ICmLiftObject
	{
		public CmLiftExample()
		{
			Notes = new List<CmLiftNote>();
			Translations = new List<LiftTranslation>();
		}

		public ICmObject CmObject { get; set; }

		public string Source { get; set; }

		public LiftMultiText Content { get; set; } // safe XML

		public List<LiftTranslation> Translations { get; }

		public List<CmLiftNote> Notes { get; }

		public override string XmlTag => "example";
	}
}