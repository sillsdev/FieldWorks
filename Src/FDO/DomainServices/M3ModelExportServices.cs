using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Practices.ServiceLocation;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.FDO.DomainServices
{
	/// <summary>
	/// Services that are common for M3 model export.
	/// </summary>
	public static class M3ModelExportServices
	{
		/// <summary>
		/// Exports data for the grammar sketch.
		/// </summary>
		/// <param name="outputPath">The output path.</param>
		/// <param name="languageProject">The language project.</param>
		public static void ExportGrammarSketch(string outputPath, ILangProject languageProject)
		{
			if (string.IsNullOrEmpty(outputPath)) throw new ArgumentNullException("outputPath");
			if (languageProject == null) throw new ArgumentNullException("languageProject");

			var servLoc = languageProject.Cache.ServiceLocator;
			const Icu.UNormalizationMode mode = Icu.UNormalizationMode.UNORM_NFC;
			var morphologicalData = languageProject.MorphologicalDataOA;
			var doc = new XDocument(
				new XDeclaration("1.0", "utf-8", "yes"),
				new XElement("M3Dump",
					ExportLanguageProject(languageProject, mode),
					ExportPartsOfSpeech(languageProject, mode),
					ExportPhonologicalData(languageProject.PhonologicalDataOA, mode),
					new XElement("MoMorphData",
						ExportCompoundRules(servLoc.GetInstance<IMoEndoCompoundRepository>(),
							servLoc.GetInstance<IMoExoCompoundRepository>(), mode),
						ExportAdhocCoProhibitions(morphologicalData, mode),
						ExportProdRestrict(morphologicalData, mode)),
					ExportMorphTypes(servLoc.GetInstance<IMoMorphTypeRepository>(), mode),
					ExportLexiconFull(servLoc, mode),
					ExportFeatureSystem(languageProject.MsFeatureSystemOA, "FeatureSystem", mode),
					ExportFeatureSystem(languageProject.PhFeatureSystemOA, "PhFeatureSystem", mode)
				)
			);
			doc.Save(outputPath);
		}

		/// <summary>
		/// Export the grammar and lexicon.
		/// </summary>
		public static void ExportGrammarAndLexicon(string outputPath, ILangProject languageProject)
		{
			if (string.IsNullOrEmpty(outputPath)) throw new ArgumentNullException("outputPath");
			if (languageProject == null) throw new ArgumentNullException("languageProject");

			var servLoc = languageProject.Cache.ServiceLocator;
			const Icu.UNormalizationMode mode = Icu.UNormalizationMode.UNORM_NFD;
			var morphologicalData = languageProject.MorphologicalDataOA;
			var doc = new XDocument(
				new XDeclaration("1.0", "utf-8", "yes"),
				new XElement("M3Dump",
					ExportPartsOfSpeech(languageProject, mode),
					ExportPhonologicalData(languageProject.PhonologicalDataOA, mode),
					ExportParserParameters(morphologicalData, mode),
					ExportCompoundRules(servLoc.GetInstance<IMoEndoCompoundRepository>(),
						servLoc.GetInstance<IMoExoCompoundRepository>(), mode),
					ExportAdhocCoProhibitions(morphologicalData, mode),
					ExportProdRestrict(morphologicalData, mode),
					ExportMorphTypes(servLoc.GetInstance<IMoMorphTypeRepository>(), mode),
					ExportLexiconFull(servLoc, mode),
					ExportFeatureSystem(languageProject.MsFeatureSystemOA, "FeatureSystem", mode),
					ExportFeatureSystem(languageProject.PhFeatureSystemOA, "PhFeatureSystem", mode)
				)
			);
			doc.Save(outputPath);
		}

		private static XElement ExportLanguageProject(ILangProject languageProject, Icu.UNormalizationMode mode)
		{
			return new XElement("LangProject",
				new XElement("Name", languageProject.ShortName),
				ExportBestAnalysis(languageProject.Description, "Description", mode),
				ExportBestAnalysis(languageProject.MainCountry, "MainCountry", mode),
				ExportWritingSystems(languageProject.CurrentVernacularWritingSystems, "VernWss"),
				ExportWritingSystems(languageProject.CurrentAnalysisWritingSystems, "AnalysisWss"));
		}

		private static XElement ExportWritingSystems(IEnumerable<IWritingSystem> wss, string elementName)
		{
			return new XElement(elementName,
				from ws in wss
				select new XElement("WritingSystem",
					new XAttribute("Id", ws.Handle),
					new XAttribute("RightToLeft", ws.RightToLeftScript),
					new XElement("DefaultFont", ws.DefaultFontName)));
		}

		private static XElement ExportPartsOfSpeech(ILangProject languageProject, Icu.UNormalizationMode mode)
		{
			return new XElement("PartsOfSpeech",
								ExportPartsOfSpeechList(languageProject.PartsOfSpeechOA.PossibilitiesOS, mode));
		}

		/// <summary>
		/// Create elements for all PartOfSpeech objects in the owning vector.
		/// </summary>
		private static IEnumerable<XElement> ExportPartsOfSpeechList(IEnumerable<ICmPossibility> partsOfSpeech, Icu.UNormalizationMode mode)
		{
			var retval = new List<XElement>();
			foreach (IPartOfSpeech partOfSpeech in partsOfSpeech)
				ExportPartOfSpeech(partOfSpeech, retval, mode);
			return retval;
		}

		/// <summary>
		/// Return true if this is considered a valid template, worth exporting for the various grammar tools.
		/// Currently this means at least one of its main slots is valid.
		/// </summary>
		static bool IsValidTemplate(IMoInflAffixTemplate template)
		{
			if (template.Disabled)
				return false;
			return
				(from affixSlot in template.PrefixSlotsRS.Concat(template.SuffixSlotsRS)
				 where IsValidSlot(affixSlot)
				 select affixSlot).Take(1).Count() > 0;
		}

		private static bool IsValidSlot(IMoInflAffixSlot affixSlot)
		{
			return affixSlot.Affixes.Take(1).Count() > 0;
		}


		private static void ExportPartOfSpeech(IPartOfSpeech pos, ICollection<XElement> cats, Icu.UNormalizationMode mode)
		{
			// Add 'pos'.
			cats.Add(new XElement("PartOfSpeech",
								  new XAttribute("Id", pos.Hvo),
								CreateAttribute("DefaultInflectionClass", pos.DefaultInflectionClassRA),
								  ExportBestAnalysis(pos.Name, "Name", mode),
								  ExportBestAnalysis(pos.Description, "Description", mode),
								  ExportBestAnalysis(pos.Abbreviation, "Abbreviation", mode),
								  new XElement("NumberOfLexEntries", pos.NumberOfLexEntries),
								  new XElement("AffixSlots",
											   from affixSlot in pos.AffixSlotsOC
											   where IsValidSlot(affixSlot)
											   select new XElement("MoInflAffixSlot",
																	new XAttribute("Id", affixSlot.Hvo),
																	new XAttribute("Optional", affixSlot.Optional),
																	ExportBestAnalysis(affixSlot.Name, "Name", mode),
																	ExportBestAnalysis(affixSlot.Description, "Description", mode))),
				// Affix templates.
								  new XElement("AffixTemplates", from template in pos.AffixTemplatesOS where IsValidTemplate(template)
																  select ExportAffixtemplate(template, mode)),
				// Inflection classes.
								  new XElement("InflectionClasses", from inflectionClass in pos.InflectionClassesOC
																	 select ExportInflectionClass(inflectionClass, mode)),
				// Inflectable Features
								  new XElement("InflectableFeats", from inflectableFeature in pos.InflectableFeatsRC
																   select new XElement("InflectableFeature",
																	   CreateDstAttribute(inflectableFeature.Hvo))),
				// Stem names
								  new XElement("StemNames", from stemName in pos.StemNamesOC
															 select ExportStemName(stemName, mode)),
				// BearableFeatures
								  new XElement("BearableFeatures",
											   from bearableFeaature in pos.BearableFeaturesRC
												select new XElement("BearableFeature",
																	CreateDstAttribute(bearableFeaature.Hvo))),
								  from subpos in pos.SubPossibilitiesOS
								   select ExportItemAsReference(subpos, "SubPossibilities")));

			// Add owned parts of speech.
			foreach (IPartOfSpeech innerPos in pos.SubPossibilitiesOS)
				ExportPartOfSpeech(innerPos, cats, mode);
		}

		private static XElement ExportParserParameters(IMoMorphData morphologicalData, Icu.UNormalizationMode mode)
		{
			return new XElement("ParserParameters",
								XElement.Parse(Normalize(morphologicalData.ParserParameters, mode)));
		}

		private static XElement ExportCompoundRules(IRepository<IMoEndoCompound> endoCmpRepository, IRepository<IMoExoCompound> exoCmpRepository,
			Icu.UNormalizationMode mode)
		{
			return new XElement("CompoundRules",
				from endoCompound in endoCmpRepository.AllInstances()
				where !endoCompound.Disabled
				select new XElement("MoEndoCompound",
					new XAttribute("Id", endoCompound.Hvo),
					new XAttribute("HeadLast", endoCompound.HeadLast ? 1 : 0),
					ExportBestAnalysis(endoCompound.Name, "Name", mode),
					ExportBestAnalysis(endoCompound.Description, "Description", mode),
					ExportItemAsReference(endoCompound.LeftMsaOA, "LeftMsa"),
					ExportItemAsReference(endoCompound.RightMsaOA, "RightMsa"),
					from prod in endoCompound.ToProdRestrictRC
					select ExportItemAsReference(prod, "ToProdRestrict"),
					ExportItemAsReference(endoCompound.OverridingMsaOA, "OverridingMsa")),
				from exoCompound in exoCmpRepository.AllInstances()
				where !exoCompound.Disabled
				select new XElement("MoExoCompound",
					new XAttribute("Id", exoCompound.Hvo),
					ExportBestAnalysis(exoCompound.Name, "Name", mode),
					ExportBestAnalysis(exoCompound.Description, "Description", mode),
					ExportItemAsReference(exoCompound.LeftMsaOA, "LeftMsa"),
					ExportItemAsReference(exoCompound.RightMsaOA, "RightMsa"),
					from prod in exoCompound.ToProdRestrictRC
					select ExportItemAsReference(prod, "ToProdRestrict"),
					ExportItemAsReference(exoCompound.ToMsaOA, "ToMsa")));
		}

		private static XElement ExportAdhocCoProhibitions(IMoMorphData morphData, Icu.UNormalizationMode mode)
		{
			return new XElement("AdhocCoProhibitions",
				from adhocCoProhib in morphData.AdhocCoProhibitionsOC
				where !adhocCoProhib.Disabled
				select ExportAdhocCoProhibition(adhocCoProhib, mode));
		}

		private static XElement ExportAdhocCoProhibition(IMoAdhocProhib adhocProhib, Icu.UNormalizationMode mode)
		{
			switch (adhocProhib.ClassID)
			{
				case MoAdhocProhibGrTags.kClassId:
					return ExportAdhocCoProhibitionGroup((IMoAdhocProhibGr) adhocProhib, mode);
				case MoMorphAdhocProhibTags.kClassId:
					return ExportMorphAdhocCoProhibition((IMoMorphAdhocProhib) adhocProhib);
				case MoAlloAdhocProhibTags.kClassId:
					return ExportAlloAdhocCoProhibition((IMoAlloAdhocProhib) adhocProhib);
			}
			return null;
		}

		private static XElement ExportAdhocCoProhibitionGroup(IMoAdhocProhibGr adhocProhibGr, Icu.UNormalizationMode mode)
		{
			return new XElement("MoAdhocProhibGr",
				new XAttribute("Id", adhocProhibGr.Hvo),
				ExportBestAnalysis(adhocProhibGr.Name, "Name", mode),
				ExportBestAnalysis(adhocProhibGr.Description, "Description", mode),
				from adhocCoProhib in adhocProhibGr.MembersOC
				where !adhocCoProhib.Disabled
				select ExportAdhocCoProhibition(adhocCoProhib, mode));
		}

		private static XElement ExportAlloAdhocCoProhibition(IMoAlloAdhocProhib alloAdhocProhib)
		{
			return new XElement("MoAlloAdhocProhib",
				new XAttribute("Id", alloAdhocProhib.Hvo),
				new XAttribute("Adjacency", alloAdhocProhib.Adjacency),
				ExportItemAsReference(alloAdhocProhib.FirstAllomorphRA, "FirstAllomorph"),
				from restOfAllo in alloAdhocProhib.RestOfAllosRS
				select ExportItemAsReference(restOfAllo, alloAdhocProhib.RestOfAllosRS.IndexOf(restOfAllo), "RestOfAllos"));
		}

		private static XElement ExportMorphAdhocCoProhibition(IMoMorphAdhocProhib morphAdhocProhib)
		{
			return new XElement("MoMorphAdhocProhib",
				new XAttribute("Id", morphAdhocProhib.Hvo),
				new XAttribute("Adjacency", morphAdhocProhib.Adjacency),
				ExportItemAsReference(morphAdhocProhib.FirstMorphemeRA, "FirstMorpheme"),
				from restOfMorph in morphAdhocProhib.RestOfMorphsRS
				select ExportItemAsReference(restOfMorph, morphAdhocProhib.RestOfMorphsRS.IndexOf(restOfMorph), "RestOfMorphs"));
		}

		private static XElement ExportProdRestrict(IMoMorphData morphData, Icu.UNormalizationMode mode)
		{
			return new XElement("ProdRestrict",
				from restriction in morphData.ProdRestrictOA.ReallyReallyAllPossibilities
				select new XElement("CmPossibility",
					new XAttribute("Id", restriction.Hvo),
					ExportBestAnalysis(restriction.Name, "Name", mode),
					ExportBestAnalysis(restriction.Description, "Description", mode),
					ExportBestAnalysis(restriction.Abbreviation, "Abbreviation", mode)));
		}

		private static XElement ExportMorphTypes(IRepository<IMoMorphType> morphTypeRepository, Icu.UNormalizationMode mode)
		{
			return new XElement("MoMorphTypes",
				from morphType in morphTypeRepository.AllInstances()
				select new XElement("MoMorphType",
					new XAttribute("Id", morphType.Hvo),
					new XAttribute("Guid", morphType.Guid.ToString()),
					ExportBestAnalysis(morphType.Name, "Name", mode),
					ExportBestAnalysis(morphType.Abbreviation, "Abbreviation", mode),
					ExportBestAnalysis(morphType.Description, "Description", mode),
					new XElement("NumberOfLexEntries", morphType.NumberOfLexEntries)));
		}

		/// <summary>
		/// Export the full lexicon when exporting both grammar and lexicon.
		/// </summary>
		private static XElement ExportLexiconFull(IServiceLocator servLoc, Icu.UNormalizationMode mode)
		{
			return new XElement("Lexicon",
					ExportEntries(servLoc.GetInstance<ILexEntryRepository>()),
					ExportMSAs(servLoc),
					ExportSenses(servLoc.GetInstance<ILexSenseRepository>(), mode),
					ExportAllomorphs(servLoc, mode));
		}

		private static XElement ExportEntries(IRepository<ILexEntry> entryRepos)
		{
			return new XElement("Entries",
				from entry in entryRepos.AllInstances()
				select new XElement("LexEntry",
					new XAttribute("Id", entry.Hvo),
					new XElement("CitationForm", entry.CitationFormWithAffixType),
					from altForm in entry.AlternateFormsOS
					select ExportItemAsReference(altForm, entry.AlternateFormsOS.IndexOf(altForm), "AlternateForms"),
					new XElement("LexemeForm",
						CreateDstAttribute(entry.LexemeFormOA),
						// Protect against a null LexemeFormOA.  See FWR-3251.
						CreateAttribute("MorphType", entry.LexemeFormOA == null ? null : entry.LexemeFormOA.MorphTypeRA)),
						from sense in entry.AllSenses
						select ExportItemAsReference(sense, "Sense"),
						from msa in entry.MorphoSyntaxAnalysesOC
						select ExportItemAsReference(msa, "MorphoSyntaxAnalysis")));
		}

		private static XElement ExportMSAs(IServiceLocator servLoc)
		{
			return new XElement("MorphoSyntaxAnalyses",
				from stemMsa in servLoc.GetInstance<IMoStemMsaRepository>().AllInstances()
				select new XElement("MoStemMsa",
					new XAttribute("Id", stemMsa.Hvo),
					CreateAttribute("PartOfSpeech", stemMsa.PartOfSpeechRA),
					CreateAttribute("InflectionClass", stemMsa.InflectionClassRA),
					new XElement("InflectionFeatures", stemMsa.MsFeaturesOA == null ? null : ExportFeatureStructure(stemMsa.MsFeaturesOA)),
					from restriction in stemMsa.ProdRestrictRC
					select ExportItemAsReference(restriction, "ProdRestrict"),
					new XElement("FromPartsOfSpeech",
						from pos in stemMsa.FromPartsOfSpeechRC
						select ExportItemAsReference(pos, "FromPOS"))),
				from derivAffxMsa in servLoc.GetInstance<IMoDerivAffMsaRepository>().AllInstances()
				select new XElement("MoDerivAffMsa",
					new XAttribute("Id", derivAffxMsa.Hvo),
					CreateAttribute("AffixCategory", derivAffxMsa.AffixCategoryRA),
					CreateAttribute("FromPartOfSpeech", derivAffxMsa.FromPartOfSpeechRA),
					CreateAttribute("ToPartOfSpeech", derivAffxMsa.ToPartOfSpeechRA),
					CreateAttribute("FromInflectionClass", derivAffxMsa.FromInflectionClassRA),
					CreateAttribute("ToInflectionClass", derivAffxMsa.ToInflectionClassRA),
					CreateAttribute("FromStemName", derivAffxMsa.FromStemNameRA),
					new XElement("FromMsFeatures", ExportFeatureStructure(derivAffxMsa.FromMsFeaturesOA)),
					new XElement("ToMsFeatures", ExportFeatureStructure(derivAffxMsa.ToMsFeaturesOA)),
					from fromRestriction in derivAffxMsa.FromProdRestrictRC
					select ExportItemAsReference(fromRestriction, "FromProdRestrict"),
					from toRestriction in derivAffxMsa.ToProdRestrictRC
					select ExportItemAsReference(toRestriction, "ToProdRestrict")),
				from inflAffxMsa in servLoc.GetInstance<IMoInflAffMsaRepository>().AllInstances()
				select new XElement("MoInflAffMsa",
					new XAttribute("Id", inflAffxMsa.Hvo),
					CreateAttribute("AffixCategory", inflAffxMsa.AffixCategoryRA),
					CreateAttribute("PartOfSpeech", inflAffxMsa.PartOfSpeechRA),
					from slot in inflAffxMsa.SlotsRC
					select ExportItemAsReference(slot, "Slots"),
					new XElement("InflectionFeatures", ExportFeatureStructure(inflAffxMsa.InflFeatsOA)),
					from fromRestriction in inflAffxMsa.FromProdRestrictRC
					select ExportItemAsReference(fromRestriction, "FromProdRestrict")),
				from unclAffxMsa in servLoc.GetInstance<IMoUnclassifiedAffixMsaRepository>().AllInstances()
				select new XElement("MoUnclassifiedAffixMsa",
					new XAttribute("Id", unclAffxMsa.Hvo),
					CreateAttribute("PartOfSpeech", unclAffxMsa.PartOfSpeechRA))
				// "MoDerivStepMsa" not supported as of 14 November 2009.
			);
		}

		private static XElement ExportSenses(IRepository<ILexSense> senseRepos, Icu.UNormalizationMode mode)
		{
			return new XElement("Senses",
				from sense in senseRepos.AllInstances()
				select new XElement("LexSense",
					new XAttribute("Id", sense.Hvo),
					CreateAttribute("Msa", sense.MorphoSyntaxAnalysisRA),
					ExportBestAnalysis(sense.Gloss, "Gloss", mode),
					ExportBestAnalysis(sense.Definition, "Definition", mode)));
		}

		private static XElement ExportAllomorphs(IServiceLocator servLoc, Icu.UNormalizationMode mode)
		{
			return new XElement("Allomorphs",
				from stemAllo in servLoc.GetInstance<IMoStemAllomorphRepository>().AllInstances()
				where stemAllo.Owner.ClassID == LexEntryTags.kClassId
				select new XElement("MoStemAllomorph",
					new XAttribute("Id", stemAllo.Hvo),
					CreateAttribute("MorphType", stemAllo.MorphTypeRA),
					new XAttribute("IsAbstract", stemAllo.IsAbstract ? 1 : 0),
					CreateAttribute("StemName", stemAllo.StemNameRA),
					ExportBestVernacular(stemAllo.Form, "Form", mode),
					from env in stemAllo.PhoneEnvRC
					select ExportItemAsReference(env, "PhoneEnv")),
					from afxAllo in servLoc.GetInstance<IMoAffixAllomorphRepository>().AllInstances()
					where afxAllo.Owner.ClassID == LexEntryTags.kClassId
					select new XElement("MoAffixAllomorph",
						new XAttribute("Id", afxAllo.Hvo),
						CreateAttribute("MorphType", afxAllo.MorphTypeRA),
						new XAttribute("IsAbstract", afxAllo.IsAbstract ? 1 : 0),
						ExportBestVernacular(afxAllo.Form, "Form", mode),
						from env in afxAllo.PhoneEnvRC
						select ExportItemAsReference(env, "PhoneEnv"),
						from position in afxAllo.PositionRS
						select ExportItemAsReference(position, afxAllo.PositionRS.IndexOf(position), "Position"),
						from inflClass in afxAllo.InflectionClassesRC
						select ExportItemAsReference(inflClass, "InflectionClasses"),
						new XElement("MsEnvFeatures", ExportFeatureStructure(afxAllo.MsEnvFeaturesOA))),
					from processAllo in servLoc.GetInstance<IMoAffixProcessRepository>().AllInstances()
					where processAllo.Owner.ClassID == LexEntryTags.kClassId
					select new XElement("MoAffixProcess",
						new XAttribute("Id", processAllo.Hvo),
						CreateAttribute("MorphType", processAllo.MorphTypeRA),
						new XAttribute("IsAbstract", processAllo.IsAbstract ? 1 : 0),
						ExportBestVernacular(processAllo.Form, "Form", mode),
						from inflClass in processAllo.InflectionClassesRC
						select ExportItemAsReference(inflClass, "InflectionClasses"),
						new XElement("Input",
							from context in processAllo.InputOS
							select ExportContext(context)),
						new XElement("Output",
							from mapping in processAllo.OutputOS
							select ExportRuleMapping(mapping)))
				);
		}

		// ExportMorphTypes rules go above this line.

		private static XElement ExportPhonologicalData(IPhPhonData phonologicalData, Icu.UNormalizationMode mode)
		{
			return new XElement("PhPhonData",
								new XAttribute("Id", phonologicalData.Hvo),
								new XElement("Environments",
									from goodEnvironment in phonologicalData.Services.GetInstance<IPhEnvironmentRepository>().AllValidInstances()
									select new XElement("PhEnvironment",
										new XAttribute("Id", goodEnvironment.Hvo),
										new XAttribute("StringRepresentation",
											Normalize(goodEnvironment.StringRepresentation, mode)),
										CreateAttribute("LeftContext", goodEnvironment.LeftContextRA),
										CreateAttribute("RightContext", goodEnvironment.RightContextRA),
										ExportBestAnalysis(goodEnvironment.Name, "Name", mode),
										ExportBestAnalysis(goodEnvironment.Description, "Description", mode))),
								new XElement("NaturalClasses", from naturalClass in phonologicalData.NaturalClassesOS
																select ExportNaturalClass(naturalClass, mode)),
								new XElement("Contexts", from context in phonologicalData.ContextsOS
														  select ExportContext(context)),
								new XElement("PhonemeSets", from phonemeSet in phonologicalData.PhonemeSetsOS
															 select ExportPhonemeSet(phonemeSet, mode)),
								new XElement("FeatureConstraints", from featureConstraint in phonologicalData.FeatConstraintsOS
																 select ExportFeatureConstraint(featureConstraint)),
								new XElement("PhonRules", from phonRule in phonologicalData.PhonRulesOS
														   select ExportPhonRule(phonRule, mode)),
							   new XElement("PhIters"),
							   new XElement("PhIters"),
							   new XElement("PhIters"),
							   new XElement("PhIters"),
							   new XElement("PhIters"),
							   new XElement("PhIters"));
		}

		private static XElement ExportPhonRule(IPhSegmentRule phonRule, Icu.UNormalizationMode mode)
		{
			XElement retVal = null;
			if (phonRule.Disabled)
				return retVal;
			switch (phonRule.ClassName)
			{
				case "PhMetathesisRule":
					var asMetathesisRule = (IPhMetathesisRule)phonRule;
					retVal = new XElement("PhMetathesisRule",
						new XAttribute("Id", phonRule.Hvo),
						new XAttribute("Direction", phonRule.Direction),
						ExportBestAnalysis(phonRule.Name, "Name", mode),
						ExportBestAnalysis(phonRule.Description, "Description", mode),
						new XElement("StrucDesc",
							ExportContextList(phonRule.StrucDescOS)),
						new XElement("StrucChange", asMetathesisRule.StrucChange.Text));
					break;
				case "PhRegularRule":
					var asRegularRule = (IPhRegularRule) phonRule;
					var constraints = new List<IPhFeatureConstraint>(asRegularRule.FeatureConstraints);
					retVal = new XElement("PhRegularRule",
						new XAttribute("Id", phonRule.Hvo),
						new XAttribute("Direction", phonRule.Direction),
						ExportBestAnalysis(phonRule.Name, "Name", mode),
						ExportBestAnalysis(phonRule.Description, "Description", mode),
						new XElement("StrucDesc",
							ExportContextList(phonRule.StrucDescOS)),
						from constraint in constraints
						select ExportItemAsReference(constraint, constraints.IndexOf(constraint), "FeatureConstraints"),
						 new XElement("RightHandSides", from rhs in asRegularRule.RightHandSidesOS
														select new XElement("PhSegRuleRHS",
															 new XAttribute("Id", rhs.Hvo),
															 new XElement("StrucChange", from structChange in rhs.StrucChangeOS
																						 select ExportContext(structChange)),
															  new XElement("InputPOSes", from pos in rhs.InputPOSesRC
																						 select ExportItemAsReference(pos, "RequiredPOS")),
															  new XElement("LeftContext", ExportContext(rhs.LeftContextOA)),
															  new XElement("RightContext", ExportContext(rhs.RightContextOA)))));
					break;
				case "PhSegmentRule":
					retVal = new XElement("PhSegmentRule",
						new XAttribute("Id", phonRule.Hvo),
						new XAttribute("Direction", phonRule.Direction),
						CreateAttribute("ord", phonRule.IndexInOwner),
						ExportBestAnalysis(phonRule.Name, "Name", mode),
						ExportBestAnalysis(phonRule.Description, "Description", mode),
						new XElement("StrucDesc",
							ExportContextList(phonRule.StrucDescOS)));
					break;
			}

			return retVal;
		}

		private static IEnumerable<XElement> ExportContextList(IEnumerable<IPhSimpleContext> simpleContexts)
		{
			return simpleContexts.Select(simpleContext => ExportContext(simpleContext));
		}

		private static XElement ExportFeatureConstraint(IPhFeatureConstraint featureConstraint)
		{
			return new XElement("PhFeatureConstraint",
				new XAttribute("Id", featureConstraint.Hvo),
				ExportItemAsReference(featureConstraint.FeatureRA, "Feature"));
		}

		private static XElement ExportPhonemeSet(IPhPhonemeSet phonemeSet, Icu.UNormalizationMode mode)
		{
			return new XElement("PhPhonemeSet",
								new XAttribute("Id", phonemeSet.Hvo),
								ExportBestAnalysis(phonemeSet.Name, "Name", mode),
								ExportBestAnalysis(phonemeSet.Description, "Description", mode),
								new XElement("Phonemes", from phoneme in phonemeSet.PhonemesOC
														  select ExportPhoneme(phoneme, mode)),
								new XElement("BoundaryMarkers", from marker in phonemeSet.BoundaryMarkersOC
																 select ExportBoundaryMarker(marker, mode)));
		}

		private static XElement ExportBoundaryMarker(IPhBdryMarker bdryMarker, Icu.UNormalizationMode mode)
		{
			return new XElement("PhBdryMarker",
								new XAttribute("Id", bdryMarker.Hvo),
								new XAttribute("Guid", bdryMarker.Guid.ToString()),
								ExportBestAnalysis(bdryMarker.Name, "Name", mode),
								ExportCodes(bdryMarker.CodesOS, mode));
		}

		private static XElement ExportPhoneme(IPhPhoneme phoneme, Icu.UNormalizationMode mode)
		{
			return new XElement("PhPhoneme",
								new XAttribute("Id", phoneme.Hvo),
								ExportBestVernacular(phoneme.Name, "Name", mode),
								ExportBestAnalysis(phoneme.Description, "Description", mode),
								ExportCodes(phoneme.CodesOS, mode),
								new XElement("BasicIPASymbol", phoneme.BasicIPASymbol.Text),
								new XElement("PhonologicalFeatures", ExportFeatureStructure(phoneme.FeaturesOA)));
		}

		private static XElement ExportCodes(IEnumerable<IPhCode> codes, Icu.UNormalizationMode mode)
		{
			return new XElement("Codes", from phone in codes
										  select new XElement("PhCode",
															  new XAttribute("Id", phone.Hvo),
															  ExportBestVernacularOrAnalysis(phone.Representation,
																							 "Representation", mode)));
		}

		private static XElement ExportContext(IPhContextOrVar context)
		{
			if (context == null)
				return null;
			XElement retVal;
			switch (context.ClassName)
			{
				default:
					throw new ArgumentException("Unrecognized context class.");
				case "PhVariable":
					retVal = new XElement("PhVariable",
										  new XAttribute("Id", context.Hvo));
					break;
				case "PhIterationContext":
					var asPhIterationContext = (IPhIterationContext) context;
					retVal = new XElement("PhIterationContext",
										  new XAttribute("Id", context.Hvo),
										  new XAttribute("Minimum", asPhIterationContext.Minimum),
										  new XAttribute("Maximum", asPhIterationContext.Maximum),
										  ExportItemAsReference(asPhIterationContext.MemberRA, "Member"));
					break;
				case "PhSequenceContext":
					var asPhSequenceContext = (IPhSequenceContext)context;
					retVal = new XElement("PhSequenceContext",
										  new XAttribute("Id", context.Hvo),
										  from member in asPhSequenceContext.MembersRS
										  select ExportItemAsReference(member, asPhSequenceContext.MembersRS.IndexOf(member), "Members"));
					break;
				case "PhSimpleContextBdry":
					var asPhSimpleContextBdry = (IPhSimpleContextBdry)context;
					retVal = new XElement("PhSimpleContextBdry",
										  new XAttribute("Id", asPhSimpleContextBdry.Hvo),
										  CreateDstAttribute(asPhSimpleContextBdry.FeatureStructureRA));
					break;
				case "PhSimpleContextSeg":
					var asPhSimpleContextSeg = (IPhSimpleContextSeg)context;
					retVal = new XElement("PhSimpleContextSeg",
										  new XAttribute("Id", asPhSimpleContextSeg.Hvo),
										  CreateDstAttribute(asPhSimpleContextSeg.FeatureStructureRA));
					break;
				case "PhSimpleContextNC":
					var asPhSimpleContextNC = (IPhSimpleContextNC)context;
					retVal = new XElement("PhSimpleContextNC",
										  new XAttribute("Id", asPhSimpleContextNC.Hvo),
										  CreateDstAttribute(asPhSimpleContextNC.FeatureStructureRA),
										  from plus in asPhSimpleContextNC.PlusConstrRS
										  select ExportItemAsReference(plus, asPhSimpleContextNC.PlusConstrRS.IndexOf(plus), "PlusConstr"),
										  from minus in asPhSimpleContextNC.MinusConstrRS
										  select ExportItemAsReference(minus, asPhSimpleContextNC.MinusConstrRS.IndexOf(minus), "MinusConstr"));
					break;
			}

			return retVal;
		}

		private static XElement ExportNaturalClass(IPhNaturalClass naturalClass, Icu.UNormalizationMode mode)
		{
			return new XElement(naturalClass.ClassName,
								new XAttribute("Id", naturalClass.Hvo),
								ExportBestAnalysis(naturalClass.Name, "Name", mode),
								ExportBestAnalysis(naturalClass.Description, "Description", mode),
								ExportBestAnalysis(naturalClass.Abbreviation, "Abbreviation", mode),
								(naturalClass is IPhNCFeatures)
									? ExportNaturalClassContents(naturalClass as IPhNCFeatures)
									: ExportNaturalClassContents(naturalClass as IPhNCSegments));
		}

		private static IEnumerable<XElement> ExportNaturalClassContents(IPhNCFeatures naturalClass)
		{
			return new[] {new XElement("Features", ExportFeatureStructure(naturalClass.FeaturesOA)) };
		}

		private static IEnumerable<XElement> ExportNaturalClassContents(IPhNCSegments naturalClass)
		{
			return from segment in naturalClass.SegmentsRC
				   select ExportItemAsReference(segment, "Segments");
		}

		private static XElement ExportItemAsReference(ICmObject target, string elementName)
		{
			return new XElement(elementName,
				CreateDstAttribute(target));
		}

		private static XAttribute CreateDstAttribute(ICmObject target)
		{
			return CreateDstAttribute(target == null ? 0 : target.Hvo);
		}

		private static XAttribute CreateDstAttribute(int value)
		{
			return CreateAttribute("dst", value);
		}

		private static XAttribute CreateAttribute(string attributeName, ICmObject target)
		{
			return new XAttribute(attributeName, target == null ? 0 : target.Hvo);
		}

		private static XAttribute CreateAttribute(string attributeName, int value)
		{
			return new XAttribute(attributeName, value);
		}

		private static XElement ExportItemAsReference(ICmObject target, int index, string elementName)
		{
			return new XElement(elementName,
				CreateDstAttribute(target.Hvo),
				CreateAttribute("ord", index));
		}

		private static XElement ExportStemName(IMoStemName stemName, Icu.UNormalizationMode mode)
		{
			return new XElement("MoStemName",
								new XAttribute("Id", stemName.Hvo),
								ExportBestAnalysis(stemName.Name, "Name", mode),
								ExportBestAnalysis(stemName.Description, "Description", mode),
								ExportBestAnalysis(stemName.Abbreviation, "Abbreviation", mode),
								new XElement("Regions", from region in stemName.RegionsOC
														 select ExportFeatureStructure(region)));
		}

		private static XElement ExportFeatureSystem(IFsFeatureSystem featureSystem, string elementName, Icu.UNormalizationMode mode)
		{
			return new XElement(elementName,
				new XAttribute("Id", featureSystem.Hvo),
				new XElement("Types",
					from type in featureSystem.TypesOC
					 select new XElement("FsFeatStrucType",
						 new XAttribute("Id", type.Hvo),
						 ExportBestAnalysis(type.Name, "Name", mode),
						 ExportBestAnalysis(type.Description, "Description", mode),
						 ExportBestAnalysis(type.Abbreviation, "Abbreviation", mode),
						 new XElement("Features",
							from featureRef in type.FeaturesRS
							select ExportItemAsReference(featureRef, "Feature")))),
				new XElement("Features",
					from featDefn in featureSystem.FeaturesOC
						select ExportFeatureDefn(featDefn, mode)));
		}

		private static XElement ExportFeatureDefn(IFsFeatDefn featureDefn, Icu.UNormalizationMode mode)
		{
			switch (featureDefn.ClassName)
			{
				default:
					// FsOpenFeature doesn't appear to be exported as of 14 November 2009.
					throw new ArgumentException("Unrecognized IFsFeatDefn");
				case "FsClosedFeature":
					var closedFD = (IFsClosedFeature)featureDefn;
					return new XElement("FsClosedFeature",
						new XAttribute("Id", featureDefn.Hvo),
						ExportBestAnalysis(featureDefn.Name, "Name", mode),
						ExportBestAnalysis(featureDefn.Description, "Description", mode),
						ExportBestAnalysis(closedFD.Abbreviation, "Abbreviation", mode),
						new XElement("Values",
							from value in closedFD.ValuesOC
								 select new XElement("FsSymFeatVal",
									 new XAttribute("Id", value.Hvo),
									 ExportBestAnalysis(value.Name, "Name", mode),
									 ExportBestAnalysis(value.Description, "Description", mode),
									 ExportBestAnalysis(value.Abbreviation, "Abbreviation", mode))));
				case "FsComplexFeature":
					var complexFD = (IFsComplexFeature) featureDefn;
					return new XElement("FsComplexFeature",
						new XAttribute("Id", featureDefn.Hvo),
						ExportBestAnalysis(featureDefn.Name, "Name", mode),
						ExportBestAnalysis(featureDefn.Description, "Description", mode),
						ExportBestAnalysis(complexFD.Abbreviation, "Abbreviation", mode),
						ExportItemAsReference(complexFD.TypeRA, "Type"));
			}
		}

		private static XElement ExportFeatureStructure(IFsAbstractStructure absFeatStruc)
		{
			if (absFeatStruc == null)
				return null;

			switch (absFeatStruc.ClassName)
			{
				case "FsFeatStruc":
					var featStruc = (IFsFeatStruc)absFeatStruc;
					return new XElement("FsFeatStruc",
										new XAttribute("Id", featStruc.Hvo),
										CreateAttribute("Type", featStruc.TypeRA),
										from featureSpec in featStruc.FeatureSpecsOC
										 select ExportFeatureSpecification(featureSpec));
				default:
					// As of 14 November 2009, FsFeatStrucDisj is not supported.
					throw new ArgumentException("Unrecognized subclass.");
			}
		}

		private static XElement ExportFeatureSpecification(IFsFeatureSpecification featureSpec)
		{
			switch (featureSpec.ClassName)
			{
				default:
					// These are not supported as of 14 November 2009.
					// FsOpenValue
					// FsDisjunctiveValue
					// FsSharedValue
					throw new ArgumentException("Unrecognized feature specification");
				case "FsClosedValue":
					var closedValue = (IFsClosedValue) featureSpec;
					return new XElement("FsClosedValue",
						new XAttribute("Id", closedValue.Hvo),
						CreateAttribute("Value", closedValue.ValueRA),
						CreateAttribute("Feature", closedValue.FeatureRA));
				case "FsComplexValue":
					var complexValue = (IFsComplexValue)featureSpec;
					return new XElement("FsComplexValue",
						new XAttribute("Id", complexValue.Hvo),
						CreateAttribute("Feature", complexValue.FeatureRA),
						ExportFeatureStructure(complexValue.ValueOA));
				case "FsNegatedValue":
					var negatedValue = (IFsNegatedValue)featureSpec;
					return new XElement("FsNegatedValue",
						new XAttribute("Id", negatedValue.Hvo),
						CreateAttribute("Value", negatedValue.ValueRA),
						CreateAttribute("Feature", negatedValue.FeatureRA));
			}
		}

		private static XElement ExportInflectionClass(IMoInflClass inflectionClass, Icu.UNormalizationMode mode)
		{
			return new XElement("MoInflClass",
								new XAttribute("Id", inflectionClass.Hvo),
								ExportBestAnalysis(inflectionClass.Name, "Name", mode),
								ExportBestAnalysis(inflectionClass.Abbreviation, "Abbreviation", mode),
								ExportBestAnalysis(inflectionClass.Description, "Description", mode),
								new XElement("Subclasses", from inflClass in inflectionClass.SubclassesOC
															select ExportInflectionClass(inflClass, mode)));
		}

		private static XElement ExportAffixtemplate(IMoInflAffixTemplate template, Icu.UNormalizationMode mode)
		{
			return new XElement("MoInflAffixTemplate",
								new XAttribute("Id", template.Hvo),
								new XAttribute("Final", template.Final),
								ExportBestAnalysis(template.Name, "Name", mode),
								ExportBestAnalysis(template.Description, "Description", mode),
								  from slot in template.SlotsRS where IsValidSlot(slot)
								   select ExportItemAsReference(slot, template.SlotsRS.IndexOf(slot), "Slots"),
								  from pfxslot in template.PrefixSlotsRS where IsValidSlot(pfxslot)
								   select ExportItemAsReference(pfxslot, template.PrefixSlotsRS.IndexOf(pfxslot), "PrefixSlots"),
								  from sfxslot in template.SuffixSlotsRS where IsValidSlot(sfxslot)
								   select ExportItemAsReference(sfxslot, template.PrefixSlotsRS.IndexOf(sfxslot), "SuffixSlots"));
		}

		private static XElement ExportBestAnalysis(IMultiAccessorBase multiString, string elementName, Icu.UNormalizationMode mode)
		{
			if (multiString == null) throw new ArgumentNullException("multiString");
			if (String.IsNullOrEmpty(elementName)) throw new ArgumentNullException("elementName");

			return new XElement(elementName, Normalize(multiString.BestAnalysisAlternative, mode));
		}

		private static XElement ExportBestVernacular(IMultiAccessorBase multiString, string elementName, Icu.UNormalizationMode mode)
		{
			if (multiString == null) throw new ArgumentNullException("multiString");
			if (String.IsNullOrEmpty(elementName)) throw new ArgumentNullException("elementName");

			return new XElement(elementName, Normalize(multiString.BestVernacularAlternative, mode));
		}

		private static XElement ExportBestVernacularOrAnalysis(IMultiAccessorBase multiString, string elementName, Icu.UNormalizationMode mode)
		{
			if (multiString == null) throw new ArgumentNullException("multiString");
			if (String.IsNullOrEmpty(elementName)) throw new ArgumentNullException("elementName");

			return new XElement(elementName, Normalize(multiString.BestVernacularAnalysisAlternative, mode));
		}

		private static string Normalize(ITsString text, Icu.UNormalizationMode mode)
		{
			if (text == null) throw new ArgumentNullException("text");

			return Normalize(text.Text, mode);
		}

		private static string Normalize(string text, Icu.UNormalizationMode mode)
		{
			if (text == null) throw new ArgumentNullException("text");

			return Icu.Normalize(text, mode);
		}

		private static XElement ExportRuleMapping(IMoRuleMapping ruleMapping)
		{
			switch (ruleMapping.ClassName)
			{
				default:
					// MoInsertNC is not supported as of 14 November 2009.
					throw new ArgumentException("Unrecognized subclass of IMoRuleMapping");
				case "MoInsertPhones":
					var insertPhones = (IMoInsertPhones)ruleMapping;
					return new XElement("MoInsertPhones",
						new XAttribute("Id", ruleMapping.Hvo),
						from insertContent in insertPhones.ContentRS
						 select ExportItemAsReference(insertContent, insertPhones.ContentRS.IndexOf(insertContent), "Content"));
				case "MoCopyFromInput":
					var copyFromInput = (IMoCopyFromInput)ruleMapping;
					return new XElement("MoCopyFromInput",
						new XAttribute("Id", ruleMapping.Hvo),
						ExportItemAsReference(copyFromInput.ContentRA, "Content"));
				case "MoModifyFromInput":
					var modifyFromInput = (IMoModifyFromInput)ruleMapping;
					return new XElement("MoModifyFromInput",
						new XAttribute("Id", ruleMapping.Hvo),
						ExportItemAsReference(modifyFromInput.ContentRA, "Content"),
						ExportItemAsReference(modifyFromInput.ModificationRA, "Modification"));
			}
		}
		/// <summary>
		/// Export everything needed by parsers for GAFAWS data.
		/// </summary>
		public static void ExportGafaws(string outputFolder, string databaseName, ICollection<ICmPossibility> partsOfSpeech)
		{
			if (string.IsNullOrEmpty(outputFolder)) throw new ArgumentNullException("outputFolder");
			if (string.IsNullOrEmpty(databaseName)) throw new ArgumentNullException("databaseName");
			if (partsOfSpeech == null) throw new ArgumentNullException("partsOfSpeech");

			var doc = new XDocument(
				new XDeclaration("1.0", "utf-8", "yes"),
				new XElement("M3Dump",
					new XElement("PartsOfSpeech",
						from IPartOfSpeech pos in partsOfSpeech
						select ExportPartOfSpeechGafaws("PartOfSpeech", pos))));
			doc.Save(Path.Combine(outputFolder, databaseName + "GAFAWSFxtResult.xml"));
		}

		/// <summary>
		/// Create element for one Gafaws PartOfSpeech object.
		/// </summary>
		private static XElement ExportPartOfSpeechGafaws(string elementName, IPartOfSpeech partOfSpeech)
		{
			return new XElement(elementName,
					   new XAttribute("Id", partOfSpeech.Hvo),
					   new XElement("AffixSlots",
						   from affixSlot in partOfSpeech.AffixSlotsOC where IsValidSlot(affixSlot)
						   select new XElement("MoInflAffixSlot",
							   new XAttribute("Id", affixSlot.Hvo))),
					   new XElement("AffixTemplates",
						   from template in partOfSpeech.AffixTemplatesOS where IsValidTemplate(template)
						   select new XElement("MoInflAffixTemplate",
							   new XAttribute("Id", template.Hvo),
							   from pfxslot in template.PrefixSlotsRS where IsValidSlot(pfxslot)
							   select ExportItemAsReference(pfxslot, template.PrefixSlotsRS.IndexOf(pfxslot), "PrefixSlots"),
							   from sfxslot in template.SuffixSlotsRS where IsValidSlot(sfxslot)
							   select ExportItemAsReference(sfxslot, template.PrefixSlotsRS.IndexOf(sfxslot), "SuffixSlots"))),
					   from IPartOfSpeech ownedPos in partOfSpeech.SubPossibilitiesOS
					   select ExportPartOfSpeechGafaws("poss", ownedPos));
		}
	}
}