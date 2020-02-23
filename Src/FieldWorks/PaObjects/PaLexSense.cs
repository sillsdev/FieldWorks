// Copyright (c) 2015-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using SIL.LCModel;
using SIL.PaToFdoInterfaces;

namespace SIL.FieldWorks.PaObjects
{
	/// <summary />
	public class PaLexSense : IPaLexSense
	{
		/// <summary />
		public PaLexSense()
		{
		}

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
		public string ImportResidue { get; set; }

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
		public string ScientificName { get; set; }

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
		public string Source { get; set; }

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
}
