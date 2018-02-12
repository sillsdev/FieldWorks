// Copyright (c) 2011-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using SIL.LCModel;
using SIL.Lift.Parsing;

namespace LanguageExplorer.Controls.LexText
{
	/// <summary>
	/// This class implements "phonetic" from the LIFT standard.
	/// It corresponds to LexPronunciation in the FieldWorks model.
	/// </summary>
	public class CmLiftPhonetic : LiftObject, ICmLiftObject
	{
		public CmLiftPhonetic()
		{
			Media = new List<LiftUrlRef>();
		}

		public ICmObject CmObject { get; set; }

		public LiftMultiText Form { get; set; } // safe XML

		public List<LiftUrlRef> Media { get; }

		public override string XmlTag => "pronunciation";
	}
}