// Copyright (c) 2011-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;
using SIL.Lift.Parsing;

namespace LanguageExplorer.Controls.LexText
{
	/// <summary>
	/// This class implements "reversal" from the LIFT standard.
	/// It roughly corresponds to ReversalIndexEntry in the FieldWorks model.
	/// </summary>
	public class CmLiftReversal : LiftObject, ICmLiftObject
	{
		public ICmObject CmObject { get; set; }

		public string Type { get; set; }

		public LiftMultiText Form { get; set; } // safe XML

		public CmLiftReversal Main { get; set; }

		public LiftGrammaticalInfo GramInfo { get; set; }

		public override string XmlTag => "reversal";
	}
}