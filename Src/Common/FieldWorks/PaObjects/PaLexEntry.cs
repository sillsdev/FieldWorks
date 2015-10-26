// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using SIL.PaToFdoInterfaces;
using SIL.FieldWorks.FDO;
using System.Xml.Serialization;
using SIL.Utils;

namespace SIL.FieldWorks.PaObjects
{
	/// ----------------------------------------------------------------------------------------
	public class PaLexEntry : IPaLexEntry
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Loads all the lexical entries from the specified service locator into a collection
		/// of PaLexEntry objects.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal static List<PaLexEntry> GetAll(IFdoServiceLocator svcloc)
		{
			return svcloc.GetInstance<ILexEntryRepository>().AllInstances()
				.Where(lx => lx.LexemeFormOA != null && lx.LexemeFormOA.Form.StringCount > 0)
				.Select(lx => new PaLexEntry(lx)).ToList();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Loads all the lexical entries from the specified service locator into a collection
		/// of PaLexEntry objects and returns the collection in a serialized list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal static string GetAllAsXml(IFdoServiceLocator svcloc)
		{
			try
			{
				return XmlSerializationHelper.SerializeToString(GetAll(svcloc));
			}
			catch
			{
				return null;
			}
		}

		/// ------------------------------------------------------------------------------------
		public PaLexEntry()
		{
		}

		/// ------------------------------------------------------------------------------------
		internal PaLexEntry(ILexEntry lxEntry)
		{
			var svcloc = lxEntry.Cache.ServiceLocator;

			DateCreated = lxEntry.DateCreated;
			DateModified = lxEntry.DateModified;
			//ExcludeAsHeadword = lxEntry.ExcludeAsHeadword; remove
			ExcludeAsHeadword = false; // MDL: remove when IPaLexEntry is updated
			// ShowMainEntryIn = lxEntry.ShowMainEntryIn.Select(x => new PaLexShowMainEntryIn(x)).ToList(); // MDL: uncomment when IPaLexEntry is updated

			ImportResidue = lxEntry.ImportResidue.Text;

			xPronunciations = lxEntry.PronunciationsOS.Select(x => new PaLexPronunciation(x)).ToList();
			xSenses = lxEntry.SensesOS.Select(x => new PaLexSense(x)).ToList();
			xComplexForms = lxEntry.ComplexFormEntries.Where(x => x.LexemeFormOA != null).Select(x => PaMultiString.Create(x.LexemeFormOA.Form, svcloc)).ToList();
			xAllomorphs = lxEntry.AllAllomorphs.Select(x => PaMultiString.Create(x.Form, svcloc)).ToList();

			xLexemeForm = lxEntry.LexemeFormOA != null ? PaMultiString.Create(lxEntry.LexemeFormOA.Form, svcloc) : null;
			xMorphType = PaCmPossibility.Create(lxEntry.PrimaryMorphType);
			xCitationForm = PaMultiString.Create(lxEntry.CitationForm, svcloc);
			xNote = PaMultiString.Create(lxEntry.Comment, svcloc);
			xLiteralMeaning = PaMultiString.Create(lxEntry.LiteralMeaning, svcloc);
			xBibliography = PaMultiString.Create(lxEntry.Bibliography, svcloc);
			xRestrictions = PaMultiString.Create(lxEntry.Restrictions, svcloc);
			xSummaryDefinition = PaMultiString.Create(lxEntry.SummaryDefinition, svcloc);
			xVariantOfInfo = lxEntry.VariantEntryRefs.Select(x => new PaVariantOfInfo(x)).ToList();
			xVariants = lxEntry.VariantFormEntryBackRefs.Select(x => new PaVariant(x)).ToList();
			xGuid = lxEntry.Guid;

			if (lxEntry.EtymologyOA != null)
				xEtymology = PaMultiString.Create(lxEntry.EtymologyOA.Form, svcloc);

			xComplexFormInfo = (from eref in lxEntry.EntryRefsOS
								let pcfi = PaComplexFormInfo.Create(eref)
								where pcfi != null
								select pcfi).ToList();
		}

		#region IPaLexEntry implementation
		/// ------------------------------------------------------------------------------------
		/// MDL: Replace this with ShowMainEntryIn it behaves the same as ExcludeAsHeadword,
		/// but on a publicaton by publication basis including Main Dictionary and "$$all_entries$$".
		public bool ExcludeAsHeadword { get; set; }

		// ------------------------------------------------------------------------------------
		// Until you delete the comment markers, don't use three slashes
		// unless you want dreaded Error CS1591.
		//public List<PaLexShowMainEntryIn> xShowMainEntryIn { get; set; }  /// MDL: uncomment when added to IPaLexEntry
		//[XmlIgnore]
		// ------------------------------------------------------------------------------------
		// public IEnumerable<IPaLexShowMainEntryIn> xShowMainEntryIn /// MDL: uncomment when added to IPaLexEntry
		//{
		//	get { return xShowMainEntryIn.Cast<IPaLexPronunciation>(); }
		//}

		/// ------------------------------------------------------------------------------------
		public string ImportResidue { get; set; }

		/// ------------------------------------------------------------------------------------
		public DateTime DateCreated { get; set; }

		/// ------------------------------------------------------------------------------------
		public DateTime DateModified { get; set; }

		/// ------------------------------------------------------------------------------------
		public PaMultiString xLexemeForm { get; set; }

		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public IPaMultiString LexemeForm
		{
			get { return xLexemeForm; }
		}

		/// ------------------------------------------------------------------------------------
		public PaCmPossibility xMorphType { get; set; }

		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public IPaCmPossibility MorphType
		{
			get { return xMorphType; }
		}

		/// ------------------------------------------------------------------------------------
		public PaMultiString xCitationForm { get; set; }

		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public IPaMultiString CitationForm
		{
			get { return xCitationForm; }
		}

		/// ------------------------------------------------------------------------------------
		public List<PaVariant> xVariants { get; set; }

		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public IEnumerable<IPaVariant> Variants
		{
			get { return xVariants.Cast<IPaVariant>(); }
		}

		/// ------------------------------------------------------------------------------------
		public List<PaVariantOfInfo> xVariantOfInfo { get; set; }

		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public IEnumerable<IPaVariantOfInfo> VariantOfInfo
		{
			get { return xVariantOfInfo.Cast<IPaVariantOfInfo>(); }
		}

		/// ------------------------------------------------------------------------------------
		public PaMultiString xSummaryDefinition { get; set; }

		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public IPaMultiString SummaryDefinition
		{
			get { return xSummaryDefinition; }
		}

		/// ------------------------------------------------------------------------------------
		public PaMultiString xEtymology { get; set; }

		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public IPaMultiString Etymology
		{
			get { return xEtymology; }
		}

		/// ------------------------------------------------------------------------------------
		public PaMultiString xNote { get; set; }

		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public IPaMultiString Note
		{
			get { return xNote; }
		}

		/// ------------------------------------------------------------------------------------
		public PaMultiString xLiteralMeaning { get; set; }

		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public IPaMultiString LiteralMeaning
		{
			get { return xLiteralMeaning; }
		}

		/// ------------------------------------------------------------------------------------
		public PaMultiString xBibliography { get; set; }

		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public IPaMultiString Bibliography
		{
			get { return xBibliography; }
		}

		/// ------------------------------------------------------------------------------------
		public PaMultiString xRestrictions { get; set; }

		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public IPaMultiString Restrictions
		{
			get { return xRestrictions; }
		}

		/// ------------------------------------------------------------------------------------
		public List<PaLexPronunciation> xPronunciations { get; set; }

		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public IEnumerable<IPaLexPronunciation> Pronunciations
		{
			get { return xPronunciations.Cast<IPaLexPronunciation>(); }
		}

		/// ------------------------------------------------------------------------------------
		public List<PaLexSense> xSenses { get; set; }

		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public IEnumerable<IPaLexSense> Senses
		{
			get { return xSenses.Cast<IPaLexSense>(); }
		}

		/// ------------------------------------------------------------------------------------
		public List<PaMultiString> xComplexForms { get; set; }

		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public IEnumerable<IPaMultiString> ComplexForms
		{
			get { return xComplexForms.Cast<IPaMultiString>(); }
		}

		/// ------------------------------------------------------------------------------------
		public List<PaComplexFormInfo> xComplexFormInfo { get; set; }

		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public IEnumerable<IPaComplexFormInfo> ComplexFormInfo
		{
			get { return xComplexFormInfo.Cast<IPaComplexFormInfo>(); }
		}

		/// ------------------------------------------------------------------------------------
		public List<PaMultiString> xAllomorphs { get; set; }

		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public IEnumerable<IPaMultiString> Allomorphs
		{
			get { return xAllomorphs.Cast<IPaMultiString>(); }
		}

		/// ------------------------------------------------------------------------------------
		public Guid xGuid { get; set; }

		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public Guid Guid
		{
			get { return xGuid; }
		}

		#endregion
	}
}
