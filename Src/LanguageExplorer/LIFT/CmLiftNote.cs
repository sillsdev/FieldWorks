// Copyright (c) 2011-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.Lift.Parsing;

namespace LanguageExplorer.LIFT
{
	/// <summary>
	/// This class implements "note" from the LIFT standard.
	/// It doesn't really correspond to any CmObject in the FieldWorks model.
	/// </summary>
	public class CmLiftNote : LiftObject
	{
		public CmLiftNote()
		{
		}

		/// <summary />
		public CmLiftNote(string type, LiftMultiText contents)
		{
			Type = type;
			Content = contents;
		}

		public string Type { get; set; }

		public LiftMultiText Content { get; set; } // safe XML

		public override string XmlTag => "note";
	}
}