// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2008, SIL International. All Rights Reserved.
// <copyright from='2008' to='2008' company='SIL International'>
//		Copyright (c) 2008, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: BiblicalTermsLocalization.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.IO;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// BiblicalTermsLocalization class represents the BiblicalTermsLocalizations node in the
	/// localized versions of the new Key Terms list
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[XmlRoot("BiblicalTermsLocalizations")]
	public class BiblicalTermsLocalization
	{
		private int ws;
		private Guid m_version;
		private List<CategoryLocalization> m_categories;
		private List<TermLocalization> m_terms;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="BiblicalTermsLocalization"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public BiblicalTermsLocalization()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="BiblicalTermsLocalization"/> class.
		/// This version of the constructor is needed for testing.
		/// </summary>
		/// <param name="termsCapacity">The initial capacity for the terms list.</param>
		/// ------------------------------------------------------------------------------------
		public BiblicalTermsLocalization(int termsCapacity)
		{
			m_categories = new List<CategoryLocalization>(5);
			m_terms = new List<TermLocalization>(termsCapacity);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the writing system HVO.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public int WritingSystemHvo
		{
			get { return ws; }
			set { ws = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// A GUID identifying the version of the file
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlAttribute("version")]
		public Guid Version
		{
			get { return m_version; }
			set { m_version = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// List of localizations for the categories
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlArrayItem("Localization")]
		public List<CategoryLocalization> Categories
		{
			get { return m_categories; }
			set { m_categories = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// List of localizations for the terms
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlArrayItem("Localization")]
		public List<TermLocalization> Terms
		{
			get { return m_terms; }
			set { m_terms = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the localization for the given term.
		/// </summary>
		/// <param name="termId">The term id.</param>
		/// ------------------------------------------------------------------------------------
		internal TermLocalization FindTerm(int termId)
		{
			// ENHANCE: First time this gets called, read them into a hashtable for faster
			// lookup on subsequent calls
			foreach (TermLocalization loc in m_terms)
			{
				if (loc.Id == termId)
					return loc;
			}
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the given category and returns its name (gloss).
		/// </summary>
		/// <param name="category">The category.</param>
		/// ------------------------------------------------------------------------------------
		internal string GetCategoryName(string category)
		{
			foreach (CategoryLocalization loc in m_categories)
			{
				if (loc.Id == category)
					return loc.Gloss;
			}
			return null;
		}
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// CategoryLocalization class represents a Categories.Localization node in the localized versions
	/// of the new Key Terms list
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class CategoryLocalization
	{
		private string m_id;
		private string m_gloss;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="CategoryLocalization"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public CategoryLocalization()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="CategoryLocalization"/> class. Used for
		/// testing
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public CategoryLocalization(string id, string gloss)
		{
			m_id = id;
			m_gloss = gloss;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the id.
		/// </summary>
		/// <value>The id.</value>
		/// ------------------------------------------------------------------------------------
		[XmlAttribute]
		public string Id
		{
			get {return m_id;}
			set {m_id = value;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the gloss.
		/// </summary>
		/// <value>The gloss.</value>
		/// ------------------------------------------------------------------------------------
		[XmlAttribute]
		public string Gloss
		{
			get {return m_gloss;}
			set {m_gloss = value;}
		}
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// TermLocalization class represents a Terms.Localization node in the localized versions of
	/// the new Key Terms list
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class TermLocalization
	{
		private int m_id;
		private string m_gloss;
		private string m_description;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="TermLocalization"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public TermLocalization()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="TermLocalization"/> class. Used for
		/// testing.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public TermLocalization(int id, string gloss, string localization)
		{
			m_id = id;
			m_gloss = gloss;
			m_description = localization;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the id.
		/// </summary>
		/// <value>The id.</value>
		/// ------------------------------------------------------------------------------------
		[XmlAttribute]
		public int Id
		{
			get { return m_id; }
			set { m_id = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the gloss.
		/// </summary>
		/// <value>The gloss.</value>
		/// ------------------------------------------------------------------------------------
		[XmlAttribute]
		public string Gloss
		{
			get { return m_gloss; }
			set { m_gloss = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the description text.
		/// </summary>
		/// <value>The description text.</value>
		/// ------------------------------------------------------------------------------------
		[XmlText]
		public string DescriptionText
		{
			get { return m_description; }
			set { m_description = value; }
		}
	}
}