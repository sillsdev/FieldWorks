// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;

namespace LanguageExplorer.Controls.LexText
{
	/// <summary>
	/// Cheapo version of the FDO LexSense object.
	/// </summary>
	internal class LSense : LObject
	{
		#region Data members

		private string m_senseNum;
		private int m_status;
		private int m_senseType;
		private List<int> m_anthroCodes;
		private List<int> m_domainTypes;
		private List<int> m_usageTypes;
		private List<int> m_thesaurusItems;
		private List<int> m_semanticDomains;

		#endregion Data members

		#region Properties

		public string SenseNumber
		{
			get { return m_senseNum; }
		}

		public int SenseType
		{
			get { return m_senseType; }
			set { m_senseType = value; }
		}

		public int Status
		{
			get { return m_status; }
			set { m_status = value; }
		}

		public List<int> AnthroCodes
		{
			get { return m_anthroCodes; }
		}

		public List<int> DomainTypes
		{
			get { return m_domainTypes; }
		}

		public List<int> UsageTypes
		{
			get { return m_usageTypes; }
		}

		public List<int> ThesaurusItems
		{
			get { return m_thesaurusItems; }
		}

		public List<int> SemanticDomains
		{
			get { return m_semanticDomains; }
		}

		#endregion Properties

		#region Construction & initialization

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="hvo">Database ID of the object.</param>
		/// <param name="senseNum">Sense number.</param>
		public LSense(int hvo, string senseNum) : base(hvo)
		{
			m_senseNum = senseNum;
			m_anthroCodes = new List<int>();
			m_domainTypes = new List<int>();
			m_usageTypes = new List<int>();
			m_thesaurusItems = new List<int>();
			m_semanticDomains = new List<int>();
		}

		#endregion Construction & initialization
	}
}