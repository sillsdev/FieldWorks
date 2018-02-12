// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;

namespace LanguageExplorer.Controls.LexText
{
	/// <summary>
	/// Cheapo version of the LCM LexSense object.
	/// </summary>
	internal class LSense : LObject
	{
		#region Data members
		#endregion Data members

		#region Properties

		public string SenseNumber { get; }

		public int SenseType { get; set; }

		public int Status { get; set; }

		public List<int> AnthroCodes { get; }

		public List<int> DomainTypes { get; }

		public List<int> UsageTypes { get; }

		public List<int> ThesaurusItems { get; }

		public List<int> SemanticDomains { get; }
		#endregion Properties

		#region Construction & initialization

		/// <summary>
		/// Constructor.
		/// </summary>
		public LSense(int hvo, string senseNum) : base(hvo)
		{
			SenseNumber = senseNum;
			AnthroCodes = new List<int>();
			DomainTypes = new List<int>();
			UsageTypes = new List<int>();
			ThesaurusItems = new List<int>();
			SemanticDomains = new List<int>();
		}

		#endregion Construction & initialization
	}
}