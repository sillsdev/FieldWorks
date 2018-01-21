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

		private string m_displayName;
		private int m_refProperty;
		private List<LAllomorph> m_alAlternateForms;
		private List<LSense> m_alSenses;
		private int m_type;

		#endregion Data members

		#region Properties

		public int Type
		{
			get { return m_type; }
			set { m_type = value; }
		}

		public int ReferenceProperty
		{
			get { return m_refProperty; }
			set { m_refProperty = value; }
		}

		public string DisplayName
		{
			get
			{
				return m_displayName;
			}
		}

		public List<LSense> Senses
		{
			get { return m_alSenses; }
		}

		public List<LAllomorph> AlternateForms
		{
			get { return m_alAlternateForms; }
		}

		#endregion Properties

		#region Construction & initialization

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="hvo">Database ID of the entry.</param>
		/// <param name="displayName">Display string of the entry.</param>
		public LEntry(int hvo, string displayName) : base(hvo)
		{
			m_displayName = displayName;
			m_alAlternateForms = new List<LAllomorph>();
			m_alSenses = new List<LSense>();
		}
		#endregion Construction & initialization

		#region Other methods

		public void AddAllomorph(LAllomorph allomorph)
		{
			m_alAlternateForms.Add(allomorph);
		}

		public void AddSense(LSense sense)
		{
			m_alSenses.Add(sense);
		}

		public override string ToString()
		{
			return m_displayName;
		}

		#endregion  Other methods
	}
}