// Copyright (c) 2011-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using SIL.LCModel;
using SIL.Lift.Parsing;

namespace LanguageExplorer.LIFT
{
	/// <summary>
	/// This class implements "phonetic" from the LIFT standard.
	/// It corresponds to LexPronunciation in the FieldWorks model.
	/// </summary>
	internal sealed class CmLiftPhonetic : LiftObject
	{
		internal CmLiftPhonetic()
		{
			Media = new List<LiftUrlRef>();
		}

		public override string XmlTag => "pronunciation";

		internal ICmObject CmObject { get; set; }

		internal LiftMultiText Form { get; set; } // safe XML

		internal List<LiftUrlRef> Media { get; }
	}
}