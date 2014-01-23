// Copyright (c) 2008-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: BiblicalTermsList.cs
// Responsibility: TE Team

using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.IO;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// BiblicalTermsList class represents the node by that same name in the new Key Terms list
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[XmlRoot("BiblicalTermsList")]
	public class BiblicalTermsList
	{
		private Guid m_version;
		private List<Term> m_keyTerms;

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
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlElement("Term")]
		public List<Term> KeyTerms
		{
			get { return m_keyTerms; }
			set { m_keyTerms = value; }
		}
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Term class represents the node by that same name in the new Key Terms list
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class Term
	{
		private int m_id;
		private string m_lemma;
		private string m_category;
		private string m_origId;
		private string m_language;
		private string m_translit;
		private string m_gloss;
		private string m_domain;
		private string m_form;
		private string m_including;
		private List<long> m_refs;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="Term"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Term()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="Term"/> class. Used for testing
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Term(int id, string category, string lemma, string language,
			string gloss, string form, string including, params long[] refs)
		{
			m_id = id;
			m_category = category;
			m_lemma = lemma;
			m_language = language;
			m_gloss = gloss;
			m_form = form;
			m_including = including;
			m_refs = new List<long>(refs);
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
			get {return m_id;}
			set {m_id = value;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the lemma.
		/// </summary>
		/// <value>The lemma.</value>
		/// ------------------------------------------------------------------------------------
		public string Lemma
		{
			get {return m_lemma;}
			set {m_lemma = value;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the category.
		/// </summary>
		/// <value>The category.</value>
		/// ------------------------------------------------------------------------------------
		public string Category
		{
			get {return m_category;}
			set {m_category = value;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the orig ID.
		/// </summary>
		/// <value>The orig ID.</value>
		/// ------------------------------------------------------------------------------------
		public string OrigID
		{
			get {return m_origId;}
			set {m_origId = value;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the language.
		/// </summary>
		/// <value>The language.</value>
		/// ------------------------------------------------------------------------------------
		public string Language
		{
			get {return m_language;}
			set {m_language = value;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the transliteration.
		/// </summary>
		/// <value>The transliteration.</value>
		/// ------------------------------------------------------------------------------------
		public string Transliteration
		{
			get {return m_translit;}
			set {m_translit = value;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the gloss.
		/// </summary>
		/// <value>The gloss.</value>
		/// ------------------------------------------------------------------------------------
		public string Gloss
		{
			get {return m_gloss;}
			set {m_gloss = value;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the domain.
		/// </summary>
		/// <value>The domain.</value>
		/// ------------------------------------------------------------------------------------
		public string Domain
		{
			get { return m_domain; }
			set { m_domain = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the form.
		/// </summary>
		/// <value>The form.</value>
		/// ------------------------------------------------------------------------------------
		public string Form
		{
			get { return m_form; }
			set { m_form = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the including.
		/// </summary>
		/// <value>The including.</value>
		/// ------------------------------------------------------------------------------------
		public string Including
		{
			get { return m_including; }
			set { m_including = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the references.
		/// </summary>
		/// <value>The references.</value>
		/// ------------------------------------------------------------------------------------
		[XmlArrayItem("Verse")]
		public List<long> References
		{
			get {return m_refs;}
			set {m_refs = value;}
		}
	}
}
