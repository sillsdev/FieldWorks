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
	public class CmLiftRelation : LiftObject, ICmLiftObject
	{
		public CmLiftRelation()
		{
			Order = -1;
		}

		public ICmObject CmObject { get; set; }

		public string Type { get; set; }

		public string Ref { get; set; }

		public int Order { get; set; }

		public LiftMultiText Usage { get; set; } // safe XML

		public override string XmlTag => "relation";
	}
}