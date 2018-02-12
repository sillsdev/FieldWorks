// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;

namespace LanguageExplorer.Controls.LexText
{
	/// <summary>
	/// Cheapo version of the LCM LexEntry object.
	/// </summary>
	internal class LEntry : LObject
	{
		#region Data members
		#endregion Data members

		#region Properties

		public int Type { get; set; }

		public int ReferenceProperty { get; set; }

		public string DisplayName { get; }

		public List<LSense> Senses { get; }

		public List<LAllomorph> AlternateForms { get; }
		#endregion Properties

		#region Construction & initialization

		/// <summary />
		public LEntry(int hvo, string displayName) : base(hvo)
		{
			DisplayName = displayName;
			AlternateForms = new List<LAllomorph>();
			Senses = new List<LSense>();
		}
		#endregion Construction & initialization

		#region Other methods

		public void AddAllomorph(LAllomorph allomorph)
		{
			AlternateForms.Add(allomorph);
		}

		public void AddSense(LSense sense)
		{
			Senses.Add(sense);
		}

		public override string ToString()
		{
			return DisplayName;
		}

		#endregion  Other methods
	}
}