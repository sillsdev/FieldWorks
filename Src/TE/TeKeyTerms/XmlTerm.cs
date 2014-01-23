// Copyright (c) 2011-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: XmlTerm.cs
// Responsibility: TE Team

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Serialization;
using SIL.FieldWorks.FDO;
using SIL.Utils;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Encapsulates the information about a key term, including its list of renderings.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[XmlType("term")]
	public class XmlTerm
	{
		#region Data members
		private IChkTerm m_term;
		#endregion

		#region XML attributes
		/// <summary>A unique identifier for the term.</summary>
		[XmlAttribute("id")]
		public string TermId;
		#endregion


		#region XML elements
		/// <summary>The lemma form of the specific Greek or Hebrew word or phrase in the
		/// original text, which is not necessarily the surface (i.e., inflected) form.
		/// </summary>
		[XmlElement("orig")]
		public XmlKeyWord KeyWord;

		/// <summary>Renderings for specific occurrences of the term in Scripture</summary>
		[XmlElement("rendering")]
		public List<XmlTermRendering> Renderings = new List<XmlTermRendering>();
		#endregion

		#region Non-XML Properties
		[XmlIgnore]
		private int TermIdNum
		{
			get { return Int32.Parse(TermId.Substring(1)); }
			set { TermId = string.Format("I{0}", value); }
		}
		#endregion

		#region Constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="XmlTerm"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public XmlTerm()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="XmlTerm"/> class based on the given
		/// collection of Scripture notes.
		/// </summary>
		/// <param name="term">The key term.</param>
		/// ------------------------------------------------------------------------------------
		public XmlTerm(IChkTerm term)
		{
			m_term = term;
			TermIdNum = term.TermId;
			KeyWord = new XmlKeyWord(term);
			foreach (IChkRef occurrence in term.OccurrencesOS.Where(o =>
				o.Status != KeyTermRenderingStatus.AutoAssigned && o.Status != KeyTermRenderingStatus.Unassigned))
			{
				Renderings.Add(new XmlTermRendering(occurrence));
			}
			Debug.Assert(Renderings.Count > 0);
		}
		#endregion

		//#region Public methods
		///// ------------------------------------------------------------------------------------
		///// <summary>
		///// Adds the specified renderings.
		///// </summary>
		///// <param name="termRenderings">The collection of references that correspond to the
		///// biblical terms that have vernacular renderings.</param>
		///// ------------------------------------------------------------------------------------
		//public void Add(IEnumerable<IChkRef> termRenderings)
		//{
		//    foreach (IChkRef rendering in termRenderings)
		//        Renderings.Add(new XmlTermRendering(rendering));
		//}
		//#endregion

		#region Methods to write annotations to cache
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Writes the list of renderings to the specified cache.
		/// </summary>
		/// <param name="lp">The language project.</param>
		/// <param name="termsListVersionsMatch">flag indicating whether the term list versions
		/// in the project is the same as the term list version used by the XML data that was
		/// read.</param>
		/// <param name="ResolveConflict">The delegate to call to resolve a conflict when a
		/// different rendering already exists.</param>
		/// ------------------------------------------------------------------------------------
		internal void WriteToCache(ILangProject lp, bool termsListVersionsMatch,
			Func<IChkRef, string, string, bool> ResolveConflict)
		{
			m_term = FindTerm(lp, termsListVersionsMatch);
			foreach (XmlTermRendering rendering in Renderings)
				rendering.WriteToCache(m_term, ResolveConflict);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the term in the project's list of key terms corresponding to this XML term.
		/// </summary>
		/// <param name="lp">The language project.</param>
		/// <param name="matchOnTermId">if set to <c>true</c> the matching logic can on term id].</param>
		/// ------------------------------------------------------------------------------------
		private IChkTerm FindTerm(ILangProject lp, bool matchOnTermId)
		{
			IEnumerable<IChkTerm> allTerms = lp.Services.GetInstance<IChkTermRepository>().AllInstances().Where(t => t.OccurrencesOS.Any());
			if (matchOnTermId)
				return allTerms.FirstOrDefault(term => term.TermId == TermIdNum);

			return allTerms.FirstOrDefault(term => term.OccurrencesOS.First().KeyWord.Text == KeyWord.Text &&
				term.OccurrencesOS.Select(o => o.Ref).SequenceEqual(Renderings.Select(r => r.Reference)));
		}
		#endregion
	}
}