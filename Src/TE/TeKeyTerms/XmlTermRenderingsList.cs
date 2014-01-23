// Copyright (c) 2011-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: XmlTermRenderingsList.cs
// Responsibility: TE Team

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Xml.Serialization;
using SIL.FieldWorks.FDO;
using SIL.Utils;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Encapsulates a list of XmlTerm objects.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[XmlRoot("terms")]
	public class XmlTermRenderingsList
	{
		#region XML attributes
		/// <summary>The default language for vernacular renderings (expessed as an ICU locale)</summary>
		[XmlAttribute("xml:lang")]
		public string DefaultVernIcuLocale;

		/// <summary>The date and time of the export</summary>
		[XmlAttribute("exported")]
		public DateTime DateTimeExported;

		/// <summary>The name of the FW project</summary>
		[XmlAttribute("project")]
		public string ProjectName;

		/// <summary>A GUID that identifies the version of the key terms list used in the project. If
		/// this XML is imported into a project that uses the same list version, then the term IDs can
		/// be assumed to correspond 1-to-1.</summary>
		[XmlAttribute("listVer")]
		public Guid TermsListVersion;

		/// <summary>Machine name (or in the future, person's name) that produced the OXEKT file</summary>
		[XmlAttribute("contributor")]
		public string ExportedBy;
		#endregion

		#region XML elements
		/// <summary>List of key terms</summary>
		[XmlElement("term")]
		public List<XmlTerm> Terms = new List<XmlTerm>();
		#endregion

		#region Constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="XmlTermRenderingsList"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public XmlTermRenderingsList()
		{
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="XmlTermRenderingsList"/> class based on
		/// the given collection of Scripture notes.
		/// </summary>
		/// <param name="lp">The langauge project.</param>
		/// ------------------------------------------------------------------------------------
		public XmlTermRenderingsList(ILangProject lp)
		{
			DefaultVernIcuLocale = lp.Services.WritingSystems.DefaultVernacularWritingSystem.IcuLocale;
			DateTimeExported = DateTime.Now;
			ProjectName = lp.Cache.ProjectId.Name;
			using (WindowsIdentity whoami = WindowsIdentity.GetCurrent())
				ExportedBy = whoami != null ? whoami.Name.Normalize() : null;
			TermsListVersion = GetLangProjKtListVersion(lp);

			AddTerms(lp.KeyTermsList.PossibilitiesOS);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Recursively adds XML terms for any terms in the list that have occurrences with
		/// renderings or explicit non-renderings (i.e., "ignored").
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void AddTerms(IFdoOwningSequence<ICmPossibility> possibilities)
		{
			foreach (ICmPossibility poss in possibilities)
			{
				IChkTerm term = poss as IChkTerm;
				if (term != null && term.OccurrencesOS.Any(o =>
					o.Status != KeyTermRenderingStatus.AutoAssigned && o.Status != KeyTermRenderingStatus.Unassigned))
				{
					Terms.Add(new XmlTerm(term));
				}

				if (poss.SubPossibilitiesOS.Any())
					AddTerms(poss.SubPossibilitiesOS);
			}
		}
		#endregion

		#region Serialization and Deserialization
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Loads the specified file.
		/// </summary>
		/// <param name="filename">The name of the OXEKT file.</param>
		/// <returns>A loaded XmlTermRenderingsList</returns>
		/// ------------------------------------------------------------------------------------
		public static XmlTermRenderingsList LoadFromFile(string filename)
		{
			return LoadFromFile(filename, null, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Loads the specified file.
		/// </summary>
		/// <param name="filename">The name of the OXEKT file.</param>
		/// <param name="cache">The cache.</param>
		/// <param name="ResolveConflict">The delegate to call to resolve a conflict when a
		/// different rendering already exists.</param>
		/// <returns>A loaded XmlTermRenderingsList</returns>
		/// ------------------------------------------------------------------------------------
		public static XmlTermRenderingsList LoadFromFile(string filename, FdoCache cache,
			Func<IChkRef, string, string, bool> ResolveConflict)
		{
			Exception e;
			return LoadFromFile(filename, cache, ResolveConflict, out e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Loads the specified file.
		/// </summary>
		/// <param name="filename">The name of the OXEKT file.</param>
		/// <param name="cache">The cache.</param>
		/// <param name="ResolveConflict">The delegate to call to resolve a conflict when a
		/// different rendering already exists.</param>
		/// <param name="e">Exception that was encountered or null</param>
		/// <returns>A loaded XmlTermRenderingsList</returns>
		/// ------------------------------------------------------------------------------------
		public static XmlTermRenderingsList LoadFromFile(string filename, FdoCache cache,
			Func<IChkRef, string, string, bool> ResolveConflict, out Exception e)
		{
			XmlTermRenderingsList list =
				XmlSerializationHelper.DeserializeFromFile<XmlTermRenderingsList>(filename, true, out e);

			if (cache != null && list != null)
				list.WriteToCache(cache, ResolveConflict ?? ((occ, existing, imported) => { return false;}) );

			return (list ?? new XmlTermRenderingsList());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Serializes to file.
		/// </summary>
		/// <param name="filename">The filename.</param>
		/// <returns><c>true</c> if successfully serialized; <c>false</c> otherwise</returns>
		/// ------------------------------------------------------------------------------------
		public bool SerializeToFile(string filename)
		{
			return XmlSerializationHelper.SerializeToFile(filename, this);
		}
		#endregion

		#region Public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the specified annotations.
		/// </summary>
		/// <param name="terms">Collection of biblical terms that have vernacular renderings.</param>
		/// ------------------------------------------------------------------------------------
		public void Add(IEnumerable<IChkTerm> terms)
		{
			foreach (IChkTerm term in terms)
				Terms.Add(new XmlTerm(term));
		}
		#endregion

		#region Methods to write annotations to cache
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Writes the list of renderings to the specified cache.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="ResolveConflict">The delegate to call to resolve a conflict when a
		/// different rendering already exists.</param>
		/// ------------------------------------------------------------------------------------
		protected void WriteToCache(FdoCache cache, Func<IChkRef, string, string, bool> ResolveConflict)
		{
			ILangProject lp = cache.LangProject;
			bool matchOnTermId = (TermsListVersion == GetLangProjKtListVersion(lp));
			foreach (XmlTerm term in Terms)
				term.WriteToCache(lp, matchOnTermId, ResolveConflict);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the current version of the key terms list used in this project.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private Guid GetLangProjKtListVersion(ILangProject lp)
		{
			ICmResource resource = (from res in lp.TranslatedScriptureOA.ResourcesOC.ToArray()
					where res.Name.Equals(TeResourceHelper.BiblicalTermsResourceName)
					select res).FirstOrDefault();
			return (resource != null) ? resource.Version : Guid.Empty;
		}
		#endregion
	}
}