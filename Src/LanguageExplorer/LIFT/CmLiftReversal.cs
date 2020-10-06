// Copyright (c) 2011-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;
using SIL.Lift.Parsing;

namespace LanguageExplorer.LIFT
{
	/// <summary>
	/// This class implements "reversal" from the LIFT standard.
	/// It roughly corresponds to ReversalIndexEntry in the FieldWorks model.
	/// </summary>
	internal sealed class CmLiftReversal : LiftObject
	{
		public override string XmlTag => "reversal";

		internal ICmObject CmObject { get; set; }

		internal string Type { get; set; }

		internal LiftMultiText Form { get; set; } // safe XML

		internal CmLiftReversal Main { get; set; }

		internal LiftGrammaticalInfo GramInfo { get; set; }
	}
}