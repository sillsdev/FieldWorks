// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2011' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: XmlTermRendering.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Serialization;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SILUBS.SharedScrUtils;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[XmlType("rendering")]
	public class XmlTermRendering
	{
		#region XML attributes
		/// <summary>The 1-based index of the word in the verse in the original language.</summary>
		[XmlAttribute("loc")]
		public int Location;

		/// <summary>The BCV (book/chapter/verse) reference of the occurrence, according to
		/// the English versification.</summary>
		[XmlAttribute("ref")]
		public int Reference;

		/// <summary>The vernacular rendering for this occurrence of this term. If null or
		/// empty, then this occrence is not explicitly rendered (i.e., it is "ignored")</summary>
		[XmlAttribute("rendering")]
		public string VernRendering;
		#endregion

		#region Constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="XmlTermRendering"/> class
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public XmlTermRendering()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="XmlTermRendering"/> class based on
		/// the given term occurrence/rendering.
		/// </summary>
		/// <param name="termOccurence">Object representing a single occurrence of a biblical
		/// term that has a vernacular rendering or is explicitly unrendered.</param>
		/// ------------------------------------------------------------------------------------
		public XmlTermRendering(IChkRef termOccurence) : this()
		{
			if (termOccurence.Status == KeyTermRenderingStatus.AutoAssigned ||
				termOccurence.Status == KeyTermRenderingStatus.Unassigned)
				throw new ArgumentException("Only occurrences with explicit renderings or explicitly unrendered terms are permitted.");

			Location = termOccurence.Location;
			Reference = termOccurence.Ref;

			if (termOccurence.Status != KeyTermRenderingStatus.Ignored)
				VernRendering = termOccurence.RenderingRA.Form.VernacularDefaultWritingSystem.Text;
		}
		#endregion

		#region Methods for writing rendering info to cache
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Writes this rendering to the specified cache.
		/// </summary>
		/// <param name="term">The term to which this rendering belongs.</param>
		/// <param name="ResolveConflict">The delegate to call to resolve a conflict when a
		/// different rendering already exists.</param>
		/// ------------------------------------------------------------------------------------
		internal void WriteToCache(IChkTerm term, Func<IChkRef, string, string, bool> ResolveConflict)
		{
			IChkRef occ = term.OccurrencesOS.FirstOrDefault(o => o.Ref == Reference && o.Location == Location);
			if (occ == null)
			{
				MessageBox.Show(string.Format("Couldn't find occurrence {0} in {1} for imported rendering of term: {2}.",
					Location, (new BCVRef(Reference)), term.Name.AnalysisDefaultWritingSystem), "Unable to Import");
				return;
			}

			if (string.IsNullOrEmpty(VernRendering))
			{
				if (occ.Status != KeyTermRenderingStatus.Ignored)
				{
					occ.Status = KeyTermRenderingStatus.Ignored;
					occ.RenderingRA = null;
				}
				return;
			}

			string existingRendering = occ.RenderingRA == null ? null :
				occ.RenderingRA.Form.VernacularDefaultWritingSystem.Text;

			if (existingRendering == VernRendering)
				return; // already set. Nothing to do.

			if (!string.IsNullOrEmpty(existingRendering))
			{
				if (!ResolveConflict(occ, existingRendering, VernRendering))
					return; // Leave existing rendering
			}

			// See if the word form already exists
			IWfiWordform wordForm = WfiWordformServices.FindOrCreateWordform(term.Cache,
				VernRendering, term.Services.WritingSystems.DefaultVernacularWritingSystem);

			KeyTermRef ktRef = new KeyTermRef(occ);
			// Change the reference's status and attach the word form to the ChkRef
			ktRef.AssignRendering(wordForm);
		}
		#endregion
	}
}
