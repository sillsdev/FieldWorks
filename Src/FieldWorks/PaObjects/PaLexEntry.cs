// Copyright (c) 2015-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.PaToFdoInterfaces;

namespace SIL.FieldWorks.PaObjects
{
	/// <summary />
	internal sealed class PaLexEntry : IPaLexEntry
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
		internal PaLexEntry()
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
				.Where(t => t.pcfi != null)
				.Select(t => t.pcfi));
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

		/// <summary />
		private sealed class PaLexSense : IPaLexSense
		{
			/// <summary />
			internal PaLexSense(ILexSense lxSense)
			{
				var svcloc = lxSense.Cache.ServiceLocator;

				AnthropologyNote = PaMultiString.Create(lxSense.AnthroNote, svcloc);
				Bibliography = PaMultiString.Create(lxSense.Bibliography, svcloc);
				Definition = PaMultiString.Create(lxSense.Definition, svcloc);
				DiscourseNote = PaMultiString.Create(lxSense.DiscourseNote, svcloc);
				EncyclopedicInfo = PaMultiString.Create(lxSense.EncyclopedicInfo, svcloc);
				GeneralNote = PaMultiString.Create(lxSense.GeneralNote, svcloc);
				Gloss = PaMultiString.Create(lxSense.Gloss, svcloc);
				GrammarNote = PaMultiString.Create(lxSense.GrammarNote, svcloc);
				PhonologyNote = PaMultiString.Create(lxSense.PhonologyNote, svcloc);
				Restrictions = PaMultiString.Create(lxSense.Restrictions, svcloc);
				SemanticsNote = PaMultiString.Create(lxSense.SemanticsNote, svcloc);
				SociolinguisticsNote = PaMultiString.Create(lxSense.SocioLinguisticsNote, svcloc);
				ReversalEntries = lxSense.ReferringReversalIndexEntries.Select(x => PaMultiString.Create(x.ReversalForm, svcloc));
				Guid = lxSense.Guid;

				ImportResidue = lxSense.ImportResidue.Text;
				Source = lxSense.Source.Text;
				ScientificName = lxSense.ScientificName.Text;

				AnthroCodes = lxSense.AnthroCodesRC.Select(PaCmPossibility.Create);
				DomainTypes = lxSense.DomainTypesRC.Select(PaCmPossibility.Create);
				Usages = lxSense.UsageTypesRC.Select(PaCmPossibility.Create);
				SemanticDomains = lxSense.SemanticDomainsRC.Select(PaCmPossibility.Create);
				Status = PaCmPossibility.Create(lxSense.StatusRA);
				SenseType = PaCmPossibility.Create(lxSense.SenseTypeRA);

				ICmPossibility poss = null;
				switch (lxSense.MorphoSyntaxAnalysisRA)
				{
					case IMoDerivAffMsa affMsa:
						poss = affMsa.FromPartOfSpeechRA;
						break;
					case IMoDerivStepMsa stepMsa:
						poss = stepMsa.PartOfSpeechRA;
						break;
					case IMoInflAffMsa affMsa:
						poss = affMsa.PartOfSpeechRA;
						break;
					case IMoStemMsa stemMsa:
						poss = stemMsa.PartOfSpeechRA;
						break;
					case IMoUnclassifiedAffixMsa affixMsa:
						poss = affixMsa.PartOfSpeechRA;
						break;
				}
				if (poss != null)
				{
					PartOfSpeech = PaCmPossibility.Create(poss);
				}
			}

			#region IPaLexSense Members

			/// <inheritdoc />
			[XmlIgnore]
			public IEnumerable<IPaCmPossibility> AnthroCodes { get; }

			/// <inheritdoc />
			[XmlIgnore]
			public IPaMultiString AnthropologyNote { get; }

			/// <inheritdoc />
			[XmlIgnore]
			public IPaMultiString Bibliography { get; }

			/// <inheritdoc />
			[XmlIgnore]
			public IPaMultiString Definition { get; }

			/// <inheritdoc />
			[XmlIgnore]
			public IPaMultiString DiscourseNote { get; }

			/// <inheritdoc />
			[XmlIgnore]
			public IEnumerable<IPaCmPossibility> DomainTypes { get; }

			/// <inheritdoc />
			[XmlIgnore]
			public IPaMultiString EncyclopedicInfo { get; }

			/// <inheritdoc />
			[XmlIgnore]
			public IPaMultiString GeneralNote { get; }

			/// <inheritdoc />
			[XmlIgnore]
			public IPaMultiString Gloss { get; }

			/// <inheritdoc />
			[XmlIgnore]
			public IPaMultiString GrammarNote { get; }

			/// <inheritdoc />
			public string ImportResidue { get; }

			/// <inheritdoc />
			[XmlIgnore]
			public IPaCmPossibility PartOfSpeech { get; }

			/// <inheritdoc />
			[XmlIgnore]
			public IPaMultiString PhonologyNote { get; }

			/// <inheritdoc />
			[XmlIgnore]
			public IPaMultiString Restrictions { get; }

			/// <inheritdoc />
			public string ScientificName { get; }

			/// <inheritdoc />
			[XmlIgnore]
			public IEnumerable<IPaCmPossibility> SemanticDomains { get; }

			/// <inheritdoc />
			[XmlIgnore]
			public IPaMultiString SemanticsNote { get; }

			/// <inheritdoc />
			[XmlIgnore]
			public IPaCmPossibility SenseType { get; }

			/// <inheritdoc />
			[XmlIgnore]
			public IPaMultiString SociolinguisticsNote { get; }

			/// <inheritdoc />
			public string Source { get; }

			/// <inheritdoc />
			[XmlIgnore]
			public IPaCmPossibility Status { get; }

			/// <inheritdoc />
			[XmlIgnore]
			public IEnumerable<IPaCmPossibility> Usages { get; }

			/// <inheritdoc />
			[XmlIgnore]
			public IEnumerable<IPaMultiString> ReversalEntries { get; }

			/// <inheritdoc />
			[XmlIgnore]
			public Guid Guid { get; }
			#endregion
		}

		/// <summary />
		private sealed class PaLexPronunciation : IPaLexPronunciation
		{
			/// <summary />
			internal PaLexPronunciation(ILexPronunciation lxPro)
			{
				Form = PaMultiString.Create(lxPro.Form, lxPro.Cache.ServiceLocator);
				Location = PaCmPossibility.Create(lxPro.LocationRA);
				CVPattern = lxPro.CVPattern.Text;
				Tone = lxPro.Tone.Text;
				Guid = lxPro.Guid;
				MediaFiles = lxPro.MediaFilesOS.Where(x => x?.MediaFileRA != null).Select(x => new PaMediaFile(x));
			}

			/// <inheritdoc />
			public string CVPattern { get; }

			/// <inheritdoc />
			public string Tone { get; }

			/// <inheritdoc />
			[XmlIgnore]
			public IPaMultiString Form { get; }

			/// <inheritdoc />
			[XmlIgnore]
			public IEnumerable<IPaMediaFile> MediaFiles { get; }

			/// <inheritdoc />
			[XmlIgnore]
			public IPaCmPossibility Location { get; }

			/// <inheritdoc />
			[XmlIgnore]
			public Guid Guid { get; }
		}

		/// <summary />
		private sealed class PaVariant : IPaVariant
		{
			private readonly PaVariantOfInfo _variantInfo;

			/// <summary />
			internal PaVariant(ILexEntryRef lxEntryRef)
			{
				var lx = lxEntryRef.OwnerOfClass<ILexEntry>();
				VariantForm = PaMultiString.Create(lx.LexemeFormOA.Form, lxEntryRef.Cache.ServiceLocator);
				_variantInfo = new PaVariantOfInfo(lxEntryRef);
			}

			/// <inheritdoc />
			[XmlIgnore]
			public IPaMultiString VariantForm { get; }

			/// <inheritdoc />
			[XmlIgnore]
			public IEnumerable<IPaCmPossibility> VariantType => _variantInfo.VariantType;

			/// <inheritdoc />
			[XmlIgnore]
			public IPaMultiString VariantComment => _variantInfo.VariantComment;
		}

		/// <summary />
		private sealed class PaVariantOfInfo : IPaVariantOfInfo
		{
			/// <summary />
			internal PaVariantOfInfo(ILexEntryRef lxEntryRef)
			{
				VariantComment = PaMultiString.Create(lxEntryRef.Summary, lxEntryRef.Cache.ServiceLocator);
				VariantType = lxEntryRef.VariantEntryTypesRS.Select(PaCmPossibility.Create);
			}

			/// <inheritdoc />
			[XmlIgnore]
			public IEnumerable<IPaCmPossibility> VariantType { get; }

			/// <inheritdoc />
			[XmlIgnore]
			public IPaMultiString VariantComment { get; }
		}

		/// <summary />
		private sealed class PaCmPossibility : IPaCmPossibility
		{
			/// <summary />
			internal static PaCmPossibility Create(ICmPossibility poss)
			{
				return (poss == null ? null : new PaCmPossibility(poss));
			}

			/// <summary />
			private PaCmPossibility(ICmPossibility poss)
			{
				var svcloc = poss.Cache.ServiceLocator;
				Abbreviation = PaMultiString.Create(poss.Abbreviation, svcloc);
				Name = PaMultiString.Create(poss.Name, svcloc);
			}

			#region IPaCmPossibility Members

			/// <inheritdoc />
			[XmlIgnore]
			public IPaMultiString Abbreviation { get; }

			/// <inheritdoc />
			[XmlIgnore]
			public IPaMultiString Name { get; }
			#endregion

			/// <inheritdoc />
			public override string ToString()
			{
				return $"{Name} ({Abbreviation})";
			}
		}

		/// <summary />
		private sealed class PaMediaFile : IPaMediaFile
		{
			/// <summary />
			internal PaMediaFile(ICmMedia mediaFile)
			{
				OriginalPath = mediaFile.MediaFileRA.OriginalPath;
				AbsoluteInternalPath = mediaFile.MediaFileRA.AbsoluteInternalPath;
				InternalPath = mediaFile.MediaFileRA.InternalPath;
				Label = PaMultiString.Create(mediaFile.Label, mediaFile.Cache.ServiceLocator);
			}

			#region IPaMediaFile Members

			/// <inheritdoc />
			[XmlIgnore]
			public IPaMultiString Label { get; }

			/// <inheritdoc />
			public string AbsoluteInternalPath { get; }

			/// <inheritdoc />
			public string InternalPath { get; }

			/// <inheritdoc />
			public string OriginalPath { get; }

			#endregion

			/// <inheritdoc />
			public override string ToString() => Path.GetFileName(AbsoluteInternalPath);
		}

		/// <summary />
		private sealed class PaComplexFormInfo : IPaComplexFormInfo
		{
			/// <summary />
			private PaComplexFormInfo(IPaMultiString complexFormComment, IEnumerable<IPaCmPossibility> complexFormType)
			{
				Components = new List<string>();
				ComplexFormComment = complexFormComment;
				ComplexFormType = complexFormType;
			}

			/// <summary />
			internal static PaComplexFormInfo Create(ILexEntryRef lxEntryRef)
			{
				if (lxEntryRef.RefType != LexEntryRefTags.krtComplexForm)
				{
					return null;
				}

				var pcfi = new PaComplexFormInfo(PaMultiString.Create(lxEntryRef.Summary, lxEntryRef.Cache.ServiceLocator), lxEntryRef.ComplexEntryTypesRS.Select(PaCmPossibility.Create));
				foreach (var component in lxEntryRef.ComponentLexemesRS)
				{
					switch (component)
					{
						case ILexEntry entry:
							pcfi.Components.Add(entry.HeadWord.Text);
							break;
						case ILexSense sense:
						{
							var text = sense.Entry.HeadWord.Text;
							if (sense.Entry.SensesOS.Count > 1)
							{
								text += $" {sense.IndexInOwner + 1}";
							}
							pcfi.Components.Add(text);
							break;
						}
					}
				}

				return pcfi;
			}

			/// <inheritdoc />
			[XmlIgnore]
			public List<string> Components { get; }

			/// <inheritdoc />
			[XmlIgnore]
			public IPaMultiString ComplexFormComment { get; }

			/// <inheritdoc />
			[XmlIgnore]
			public IEnumerable<IPaCmPossibility> ComplexFormType { get; }
		}
	}
}
