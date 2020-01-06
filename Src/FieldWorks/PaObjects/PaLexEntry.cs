// Copyright (c) 2015-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.PaToFdoInterfaces;

namespace SIL.FieldWorks.PaObjects
{
	/// <summary />
	public class PaLexEntry : IPaLexEntry
	{
		/// <summary>
		/// Loads all the lexical entries from the specified service locator into a collection
		/// of PaLexEntry objects.
		/// </summary>
		internal static List<PaLexEntry> GetAll(ILcmServiceLocator svcloc)
		{
			return svcloc.GetInstance<ILexEntryRepository>().AllInstances()
				.Where(lx => lx.LexemeFormOA != null && lx.LexemeFormOA.Form.StringCount > 0)
				.Select(lx => new PaLexEntry(lx)).ToList();
		}

		/// <summary>
		/// Loads all the lexical entries from the specified service locator into a collection
		/// of PaLexEntry objects and returns the collection in a serialized list.
		/// </summary>
		internal static string GetAllAsXml(ILcmServiceLocator svcloc)
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

		/// <summary />
		public PaLexEntry()
		{
		}

		/// <summary />
		internal PaLexEntry(ILexEntry lxEntry)
		{
			var svcloc = lxEntry.Cache.ServiceLocator;

			DateCreated = lxEntry.DateCreated;
			DateModified = lxEntry.DateModified;
			ExcludeAsHeadword = false; // MDL: remove when IPaLexEntry is updated

			ImportResidue = lxEntry.ImportResidue.Text;

			Pronunciations = lxEntry.PronunciationsOS.Select(x => new PaLexPronunciation(x));
			Senses = lxEntry.SensesOS.Select(x => new PaLexSense(x));
			ComplexForms = lxEntry.ComplexFormEntries.Where(x => x.LexemeFormOA != null).Select(x => PaMultiString.Create(x.LexemeFormOA.Form, svcloc));
			Allomorphs = lxEntry.AllAllomorphs.Select(x => PaMultiString.Create(x.Form, svcloc));
			LexemeForm = lxEntry.LexemeFormOA != null ? PaMultiString.Create(lxEntry.LexemeFormOA.Form, svcloc) : null;
			MorphType = PaCmPossibility.Create(lxEntry.PrimaryMorphType);
			CitationForm = PaMultiString.Create(lxEntry.CitationForm, svcloc);
			Note = PaMultiString.Create(lxEntry.Comment, svcloc);
			LiteralMeaning = PaMultiString.Create(lxEntry.LiteralMeaning, svcloc);
			Bibliography = PaMultiString.Create(lxEntry.Bibliography, svcloc);
			Restrictions = PaMultiString.Create(lxEntry.Restrictions, svcloc);
			SummaryDefinition = PaMultiString.Create(lxEntry.SummaryDefinition, svcloc);
			VariantOfInfo = lxEntry.VariantEntryRefs.Select(x => new PaVariantOfInfo(x));
			Variants = lxEntry.VariantFormEntryBackRefs.Select(x => new PaVariant(x));
			Guid = lxEntry.Guid;

			// append all etymology forms together separated by commas
			if (lxEntry.EtymologyOS.Count > 0)
			{
				Etymology = new PaMultiString();
				foreach (var etymology in lxEntry.EtymologyOS)
				{
					PaMultiString.Append((PaMultiString)Etymology, etymology.Form, svcloc);
				}
			}

			ComplexFormInfo = (lxEntry.EntryRefsOS
				.Select(eref => new { eref, pcfi = PaComplexFormInfo.Create(eref) })
				.Where(@t => @t.pcfi != null)
				.Select(@t => @t.pcfi));
		}

		#region IPaLexEntry implementation

		/// <inheritdoc />
		public bool ExcludeAsHeadword { get; set; }

		/// <inheritdoc />
		public string ImportResidue { get; set; }

		/// <inheritdoc />
		public DateTime DateCreated { get; set; }

		/// <inheritdoc />
		public DateTime DateModified { get; set; }

		/// <inheritdoc />
		[XmlIgnore]
		public IPaMultiString LexemeForm { get; }

		/// <inheritdoc />
		[XmlIgnore]
		public IPaCmPossibility MorphType { get; }

		/// <inheritdoc />
		[XmlIgnore]
		public IPaMultiString CitationForm { get; }

		/// <inheritdoc />
		[XmlIgnore]
		public IEnumerable<IPaVariant> Variants { get; }

		/// <inheritdoc />
		[XmlIgnore]
		public IEnumerable<IPaVariantOfInfo> VariantOfInfo { get; }

		/// <inheritdoc />
		[XmlIgnore]
		public IPaMultiString SummaryDefinition { get; }

		/// <inheritdoc />
		[XmlIgnore]
		public IPaMultiString Etymology { get; }

		/// <inheritdoc />
		[XmlIgnore]
		public IPaMultiString Note { get; }

		/// <inheritdoc />
		[XmlIgnore]
		public IPaMultiString LiteralMeaning { get; }

		/// <inheritdoc />
		[XmlIgnore]
		public IPaMultiString Bibliography { get; }

		/// <inheritdoc />
		[XmlIgnore]
		public IPaMultiString Restrictions { get; }

		/// <inheritdoc />
		[XmlIgnore]
		public IEnumerable<IPaLexPronunciation> Pronunciations { get; }

		/// <inheritdoc />
		[XmlIgnore]
		public IEnumerable<IPaLexSense> Senses { get; }

		/// <inheritdoc />
		[XmlIgnore]
		public IEnumerable<IPaMultiString> ComplexForms { get; }

		/// <inheritdoc />
		[XmlIgnore]
		public IEnumerable<IPaComplexFormInfo> ComplexFormInfo { get; }

		/// <inheritdoc />
		[XmlIgnore]
		public IEnumerable<IPaMultiString> Allomorphs { get; }

		/// <inheritdoc />
		[XmlIgnore]
		public Guid Guid { get; }

		#endregion
	}
}
