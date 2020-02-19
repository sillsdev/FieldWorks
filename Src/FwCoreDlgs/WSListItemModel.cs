// Copyright (c) 2019-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.LCModel.Core.WritingSystems;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary>
	/// This class models a list item for a writing system.
	/// The boolean indicates if the item is in the Current list and should be ticked in the UI.
	/// </summary>
	public class WSListItemModel : Tuple<bool, CoreWritingSystemDefinition, CoreWritingSystemDefinition>
	{
		/// <summary/>
		public WSListItemModel(bool isInCurrent, CoreWritingSystemDefinition originalWsDef, CoreWritingSystemDefinition workingWs) : base(isInCurrent, originalWsDef, workingWs)
		{
		}

		/// <summary/>
		public bool InCurrentList => Item1;

		/// <summary/>
		public CoreWritingSystemDefinition WorkingWs => Item3;

		/// <summary/>
		public CoreWritingSystemDefinition OriginalWs => Item2;

		/// <summary/>
		public override string ToString()
		{
			return WorkingWs.DisplayLabel;
		}
	}
}