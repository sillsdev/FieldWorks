// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: EntryObjects.cs
// Responsibility: Randy Regnier
// Last reviewed:
//
// <remarks>
// Implementation of:
//		ExtantEntryInfo - Class that gets the extant entries from the DB
//			which match the given information.
// </remarks>
// --------------------------------------------------------------------------------------------
using System.Collections.Generic;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.LexText.Controls
{
	#region WindowParams class
	/// <summary>
	/// A class that allows for parameters to be passed to the Go dialog form the client.
	/// Currently, this only works for XCore messages, not the IText entry point.
	/// </summary>
	public class WindowParams
	{
		#region Data members

		/// <summary>
		/// Window title.
		/// </summary>
		public string m_title;
		/// <summary>
		/// Text in label to the left of the form edit box.
		/// </summary>
		public string m_label;
		/// <summary>
		/// Text on OK button.
		/// </summary>
		public string m_btnText;

		#endregion Data members
	}
	#endregion WindowParams class

	#region LObject class
	/// <summary>
	/// Abstract base class for LEnty and LSense,
	/// which are 'cheap' versions of the corresponding FDO classes.
	/// </summary>
	internal abstract class LObject
	{
		protected int m_hvo;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="hvo">Database ID of the object.</param>
		public LObject(int hvo)
		{
			m_hvo = hvo;
		}

		public int HVO
		{
			get { return m_hvo; }
		}
	}
	#endregion LObject class

	#region LEntry class
	/// <summary>
	/// Cheapo version of the FDO LexEntry object.
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
	#endregion LEntry class

	#region LSense class
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
	#endregion LSense class

	#region LAllomorph class
	/// <summary>
	/// Cheapo version of the FDO MoForm object.
	/// </summary>
	internal class LAllomorph : LObject, ITssValue
	{
		#region Data members

		private int m_type;
		private ITsString m_form;

		#endregion Data members

		#region Properties

		public int Type
		{
			get { return m_type; }
		}

		#endregion Properties

		#region Construction & initialization

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="hvo">Database ID of the object.</param>
		/// <param name="type"></param>
		public LAllomorph(int hvo, int type) : base(hvo)
		{
			m_type = type;
			m_form = null;
		}

		public LAllomorph(IMoForm allo) : base(allo.Hvo)
		{
			m_type = allo.ClassID;
			m_form = allo.Form.BestVernacularAlternative;
		}

		#endregion Construction & initialization

		public override string ToString()
		{
			return (m_form == null || m_form.Text == null) ? m_hvo.ToString() : m_form.Text;
		}

		#region ITssValue Members

		/// <summary>
		/// Implementing this allows the fw combo box to do a better job of displaying items.
		/// </summary>
		public ITsString AsTss
		{
			get { return m_form; }
		}

		#endregion
	}
	#endregion LAllomorph class

	#region LMsa class
	/// <summary>
	/// Cheapo version of the FDO MoForm object.
	/// </summary>
	internal class LMsa : LObject
	{
		#region Data members

		private readonly string m_name;

		#endregion Data members

		#region Properties

		#endregion Properties

		#region Construction & initialization

		public LMsa(IMoMorphSynAnalysis msa) : base(msa.Hvo)
		{
			m_name = msa.InterlinearName;
		}

		public override string ToString()
		{
			return m_name ?? m_hvo.ToString();
		}

		#endregion Construction & initialization
	}
	#endregion LMsa class
}
