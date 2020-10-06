// Copyright (c) 2011-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;
using SIL.Lift.Parsing;

namespace LanguageExplorer.LIFT
{
	/// <summary>
	/// This class implements "relation" from the LIFT standard.
	/// It relates to LexRelation or LexEntryRef in the FieldWorks model.
	/// </summary>
	internal sealed class CmLiftRelation : LiftObject
	{
		internal CmLiftRelation()
		{
			Order = -1;
		}

		public override string XmlTag => "relation";

		internal ICmObject CmObject { get; set; }

		internal string Type { get; set; }

		internal string Ref { get; set; }

		internal int Order { get; set; }

		internal LiftMultiText Usage { get; set; } // safe XML
	}
}