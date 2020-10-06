// Copyright (c) 2011-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.Lift.Parsing;

namespace LanguageExplorer.LIFT
{
	/// <summary>
	/// This class implements "note" from the LIFT standard.
	/// It doesn't really correspond to any CmObject in the FieldWorks model.
	/// </summary>
	internal sealed class CmLiftNote : LiftObject
	{
		/// <summary />
		internal CmLiftNote(string type, LiftMultiText contents)
		{
			Type = type;
			Content = contents;
		}

		public override string XmlTag => "note";

		internal string Type { get; set; }

		internal LiftMultiText Content { get; set; } // safe XML
	}
}