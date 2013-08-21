using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Schema;

namespace SIL.HermitCrab
{
	/// <summary>
	/// This class represents the loader for HC.NET's XML input format.
	/// </summary>
	public class XmlLoader : Loader
	{
		static Stratum.PRuleOrder GetPRuleOrder(string ruleOrderStr)
		{
			switch (ruleOrderStr)
			{
				case "linear":
					return Stratum.PRuleOrder.LINEAR;

				case "simultaneous":
					return Stratum.PRuleOrder.SIMULTANEOUS;
			}

			return Stratum.PRuleOrder.LINEAR;
		}

		static Stratum.MRuleOrder GetMRuleOrder(string ruleOrderStr)
		{
			switch (ruleOrderStr)
			{
				case "linear":
					return Stratum.MRuleOrder.LINEAR;

				case "unordered":
					return Stratum.MRuleOrder.UNORDERED;
			}

			return Stratum.MRuleOrder.UNORDERED;
		}

		static PhonologicalRule.MultAppOrder GetMultAppOrder(string multAppOrderStr)
		{
			switch (multAppOrderStr)
			{
				case "simultaneous":
					return PhonologicalRule.MultAppOrder.SIMULTANEOUS;

				case "rightToLeftIterative":
					return PhonologicalRule.MultAppOrder.RL_ITERATIVE;

				case "leftToRightIterative":
					return PhonologicalRule.MultAppOrder.LR_ITERATIVE;
			}

			return PhonologicalRule.MultAppOrder.LR_ITERATIVE;
		}

		static MPRFeatureGroup.GroupMatchType GetGroupMatchType(string matchTypeStr)
		{
			switch (matchTypeStr)
			{
				case "all":
					return MPRFeatureGroup.GroupMatchType.ALL;

				case "any":
					return MPRFeatureGroup.GroupMatchType.ANY;
			}
			return MPRFeatureGroup.GroupMatchType.ANY;
		}

		static MPRFeatureGroup.GroupOutputType GetGroupOutputType(string outputTypeStr)
		{
			switch (outputTypeStr)
			{
				case "overwrite":
					return MPRFeatureGroup.GroupOutputType.OVERWRITE;

				case "append":
					return MPRFeatureGroup.GroupOutputType.APPEND;
			}
			return MPRFeatureGroup.GroupOutputType.OVERWRITE;
		}

		static MorphologicalTransform.RedupMorphType GetRedupMorphType(string redupMorphTypeStr)
		{
			switch (redupMorphTypeStr)
			{
				case "prefix":
					return MorphologicalTransform.RedupMorphType.PREFIX;

				case "suffix":
					return MorphologicalTransform.RedupMorphType.SUFFIX;

				case "implicit":
					return MorphologicalTransform.RedupMorphType.IMPLICIT;
			}
			return MorphologicalTransform.RedupMorphType.IMPLICIT;
		}

		static MorphCoOccurrence.AdjacencyType GetAdjacencyType(string adjacencyTypeStr)
		{
			switch (adjacencyTypeStr)
			{
				case "anywhere":
					return MorphCoOccurrence.AdjacencyType.ANYWHERE;

				case "somewhereToLeft":
					return MorphCoOccurrence.AdjacencyType.SOMEWHERE_TO_LEFT;

				case "somewhereToRight":
					return MorphCoOccurrence.AdjacencyType.SOMEWHERE_TO_RIGHT;

				case "adjacentToLeft":
					return MorphCoOccurrence.AdjacencyType.ADJACENT_TO_LEFT;

				case "adjacentToRight":
					return MorphCoOccurrence.AdjacencyType.ADJACENT_TO_RIGHT;
			}
			return MorphCoOccurrence.AdjacencyType.ANYWHERE;
		}

		Dictionary<string, string> m_repIds;
		XmlResolver m_resolver = null;

		public XmlLoader()
		{
			m_repIds = new Dictionary<string, string>();
		}

		public XmlResolver XmlResolver
		{
			set
			{
				m_resolver = value;
			}
		}

		public override Encoding DefaultOutputEncoding
		{
			get
			{
				return Encoding.UTF8;
			}
		}

		public override void Reset()
		{
			base.Reset();
			m_repIds.Clear();
		}

		public override void Load()
		{
			throw new NotImplementedException();
		}

		public override void Load(string configFile)
		{
			Reset();

			XmlReaderSettings settings = new XmlReaderSettings();
			/*settings.ProhibitDtd = false;*/
			settings.DtdProcessing = DtdProcessing.Parse;
			if (Type.GetType ("Mono.Runtime") == null)
			{
				settings.ValidationType = ValidationType.DTD;
				settings.ValidationEventHandler += new ValidationEventHandler(settings_ValidationEventHandler);
			}
			else
			{
				// Mono's dtd processing seems to have bugs. Workaround	don't do DTD validation.
				settings.ValidationType = ValidationType.None;
			}

			if (m_resolver != null)
				settings.XmlResolver = m_resolver;

			XmlReader reader = XmlReader.Create(configFile, settings);
			XmlDocument doc = new XmlDocument();
			try
			{
				doc.Load(reader);
			}
			catch (XmlException xe)
			{
				throw new LoadException(LoadException.LoadErrorType.PARSE_ERROR, this,
					string.Format(HCStrings.kstidParseError, configFile), xe);
			}
			finally
			{
				reader.Close();
			}

			XmlNode lang = doc.DocumentElement.SelectSingleNode("Language[@isActive='yes']");
			LoadLanguage(lang as XmlElement);

			XmlNode cmds = doc.DocumentElement.SelectSingleNode("Commands");
			if (cmds != null)
				LoadCommands(cmds as XmlElement);
			m_isLoaded = true;
		}

		void LoadCommands(XmlElement cmds)
		{
			foreach (XmlNode cmdNode in cmds.ChildNodes)
			{
				if (cmdNode.NodeType != XmlNodeType.Element)
					continue;
				XmlElement cmdElem = cmdNode as XmlElement;
				if (cmdElem.GetAttribute("isActive") == "no")
					continue;

				switch (cmdElem.Name)
				{
					case "OpenLanguage":
						m_morphers.TryGetValue(cmdElem.GetAttribute("language"), out m_curMorpher);
						break;

					case "MorpherSet":
						foreach (XmlNode varNode in cmdElem.ChildNodes)
						{
							if (varNode.NodeType != XmlNodeType.Element)
								continue;

							XmlElement varElem = varNode as XmlElement;
							switch (varElem.Name)
							{
								case "MorpherSetBoolean":
									bool boolVal = varElem.GetAttribute("value") == "true";
									switch (varElem.GetAttribute("variable"))
									{
										case "quitOnError":
											m_quitOnError = boolVal;
											break;

										case "traceInputs":
											m_traceInputs = boolVal;
											break;
									}
									break;

								case "MorpherSetInteger":
									int intVal = Convert.ToInt32(varElem.GetAttribute("value"));
									switch (varElem.GetAttribute("variable"))
									{
										case "deletionRuleReApplications":
											m_curMorpher.DelReapplications = intVal;
											break;
									}
									break;
							}
						}
						break;

					case "MorphAndLookupWord":
						string word = cmdElem.SelectSingleNode("CurrentInputWord").InnerText;
						MorphAndLookupWord(word, cmdElem.GetAttribute("resultFormat") == "prettyPrint");
						break;

					case "TraceBlocking":
						m_curMorpher.TraceBlocking = cmdElem.GetAttribute("on") == "true";
						break;

					case "TraceLexicalLookup":
						m_curMorpher.TraceLexLookup = cmdElem.GetAttribute("on") == "true";
						break;

					case "TraceMorpherRule":
						bool traceAnalysis = cmdElem.GetAttribute("analysisMode") == "true";
						bool traceSynthesis = cmdElem.GetAttribute("generateMode") == "true";
						string ruleId = cmdElem.GetAttribute("rule");
						if (string.IsNullOrEmpty(ruleId))
							m_curMorpher.SetTraceRules(traceAnalysis, traceSynthesis);
						else
							m_curMorpher.SetTraceRule(ruleId, traceAnalysis, traceSynthesis);
						break;

					case "TraceMorpherStrata":
						m_curMorpher.TraceStrataAnalysis = cmdElem.GetAttribute("analysisMode") == "true";
						m_curMorpher.TraceStrataSynthesis = cmdElem.GetAttribute("generateMode") == "true";
						break;

					case "TraceMorpherTemplates":
						m_curMorpher.TraceTemplatesAnalysis = cmdElem.GetAttribute("analysisMode") == "true";
						m_curMorpher.TraceTemplatesSynthesis = cmdElem.GetAttribute("generateMode") == "true";
						break;

					case "TraceMorpherSuccess":
						m_curMorpher.TraceSuccess = cmdElem.GetAttribute("on") == "true";
						break;
				}
			}
		}

		void LoadLanguage(XmlElement langElem)
		{
			string id = langElem.GetAttribute("id");
			m_curMorpher = new Morpher(id, langElem.SelectSingleNode("Name").InnerText);
			m_morphers.Add(m_curMorpher);

			XmlNodeList posList = langElem.SelectNodes("PartsOfSpeech/PartOfSpeech");
			foreach (XmlNode posNode in posList)
			{
				XmlElement posElem = posNode as XmlElement;
				string posId = posElem.GetAttribute("id");
				m_curMorpher.AddPOS(new PartOfSpeech(posId, posElem.InnerText, m_curMorpher));
			}

			XmlNodeList mprFeatList = langElem.SelectNodes("MorphologicalPhonologicalRuleFeatures/MorphologicalPhonologicalRuleFeature[@isActive='yes']");
			foreach (XmlNode mprFeatNode in mprFeatList)
			{
				XmlElement mprFeatElem = mprFeatNode as XmlElement;
				string mprFeatId = mprFeatElem.GetAttribute("id");
				m_curMorpher.AddMPRFeature(new MPRFeature(mprFeatId, mprFeatElem.InnerText, m_curMorpher));
			}

			XmlNodeList mprFeatGroupList = langElem.SelectNodes("MorphologicalPhonologicalRuleFeatures/MorphologicalPhonologicalRuleFeatureGroup[@isActive='yes']");
			foreach (XmlNode mprFeatGroupNode in mprFeatGroupList)
				LoadMPRFeatGroup(mprFeatGroupNode as XmlElement);

			XmlNode phonFeatSysNode = langElem.SelectSingleNode("PhonologicalFeatureSystem[@isActive='yes']");
			if (phonFeatSysNode != null)
				LoadFeatureSystem(phonFeatSysNode as XmlElement, m_curMorpher.PhoneticFeatureSystem);

			XmlNode headFeatsNode = langElem.SelectSingleNode("HeadFeatures");
			if (headFeatsNode != null)
				LoadFeatureSystem(headFeatsNode as XmlElement, m_curMorpher.HeadFeatureSystem);

			XmlNode footFeatsNode = langElem.SelectSingleNode("FootFeatures");
			if (footFeatsNode != null)
				LoadFeatureSystem(footFeatsNode as XmlElement, m_curMorpher.FootFeatureSystem);

			XmlNodeList charDefTableList = langElem.SelectNodes("CharacterDefinitionTable[@isActive='yes']");
			foreach (XmlNode charDefTableNode in charDefTableList)
				LoadCharDefTable(charDefTableNode as XmlElement);

			XmlNodeList featNatClassList = langElem.SelectNodes("NaturalClasses/FeatureNaturalClass[@isActive='yes']");
			foreach (XmlNode natClassNode in featNatClassList)
				LoadFeatNatClass(natClassNode as XmlElement);

			XmlNodeList segNatClassList = langElem.SelectNodes("NaturalClasses/SegmentNaturalClass[@isActive='yes']");
			foreach (XmlNode natClassNode in segNatClassList)
				LoadSegNatClass(natClassNode as XmlElement);

			XmlNodeList mrulesNodeList = langElem.SelectNodes("MorphologicalRules/*[@isActive='yes']");
			if (mrulesNodeList != null)
			{
				foreach (XmlNode mruleNode in mrulesNodeList)
				{
					XmlElement mruleElem = mruleNode as XmlElement;
					try
					{
						switch (mruleElem.Name)
						{
							case "MorphologicalRule":
								LoadMRule(mruleNode as XmlElement);
								break;

							case "RealizationalRule":
								LoadRealRule(mruleNode as XmlElement);
								break;

							case "CompoundingRule":
								LoadCompoundRule(mruleNode as XmlElement);
								break;
						}
					}
					catch (LoadException le)
					{
						if (m_quitOnError)
							throw le;
					}
				}
			}

			HCObjectSet<MorphologicalRule> templateRules = new HCObjectSet<MorphologicalRule>();
			XmlNodeList tempList = langElem.SelectNodes("Strata/AffixTemplate[@isActive='yes']");
			foreach (XmlNode tempNode in tempList)
				LoadAffixTemplate(tempNode as XmlElement, templateRules);

			XmlNodeList stratumList = langElem.SelectNodes("Strata/Stratum[@isActive='yes']");
			XmlElement surfaceElem = null;
			foreach (XmlNode stratumNode in stratumList)
			{
				XmlElement stratumElem = stratumNode as XmlElement;
				if (stratumElem.GetAttribute("id") == Stratum.SURFACE_STRATUM_ID)
					surfaceElem = stratumElem;
				else
					LoadStratum(stratumElem);
			}
			if (surfaceElem == null)
				throw CreateUndefinedObjectException(HCStrings.kstidNoSurfaceStratum, Stratum.SURFACE_STRATUM_ID);
			LoadStratum(surfaceElem);

			if (mrulesNodeList != null)
			{
				foreach (XmlNode mruleNode in mrulesNodeList)
				{
					XmlElement mruleElem = mruleNode as XmlElement;
					string ruleId = mruleElem.GetAttribute("id");
					if (!templateRules.Contains(ruleId))
					{
						MorphologicalRule mrule = m_curMorpher.GetMorphologicalRule(ruleId);
						if (mrule != null)
						{
							Stratum stratum = m_curMorpher.GetStratum(mruleElem.GetAttribute("stratum"));
							stratum.AddMorphologicalRule(mrule);
						}
					}
				}
			}

			XmlNodeList familyList = langElem.SelectNodes("Lexicon/Families/Family[@isActive='yes']");
			foreach (XmlNode familyNode in familyList)
			{
				XmlElement familyElem = familyNode as XmlElement;
				LexFamily family = new LexFamily(familyElem.GetAttribute("id"), familyElem.InnerText, m_curMorpher);
				m_curMorpher.Lexicon.AddFamily(family);
			}

			XmlNodeList entryList = langElem.SelectNodes("Lexicon/LexicalEntry[@isActive='yes']");
			foreach (XmlNode entryNode in entryList)
			{
				try
				{
					LoadLexEntry(entryNode as XmlElement);
				}
				catch (LoadException le)
				{
					if (m_quitOnError)
						throw le;
				}
			}

			// co-occurrence rules cannot be loaded until all of the morphemes and their allomorphs have been loaded
			XmlNodeList morphemeList = langElem.SelectNodes("Lexicon/LexicalEntry[@isActive='yes'] | MorphologicalRules/*[@isActive='yes']");
			foreach (XmlNode morphemeNode in morphemeList)
			{
				XmlElement morphemeElem = morphemeNode as XmlElement;
				string morphemeId = morphemeElem.GetAttribute("id");
				Morpheme morpheme = m_curMorpher.GetMorpheme(morphemeId);
				if (morpheme != null)
				{
					try
					{
						morpheme.RequiredMorphemeCoOccurrences = LoadMorphCoOccurs(morphemeElem.SelectSingleNode("RequiredMorphemeCoOccurrences"));
					}
					catch (LoadException le)
					{
						if (m_quitOnError)
							throw le;
					}
					try
					{
						morpheme.ExcludedMorphemeCoOccurrences = LoadMorphCoOccurs(morphemeElem.SelectSingleNode("ExcludedMorphemeCoOccurrences"));
					}
					catch (LoadException le)
					{
						if (m_quitOnError)
							throw le;
					}
				}

				XmlNodeList allomorphList = morphemeNode.SelectNodes("Allomorphs/Allomorph[@isActive='yes'] | MorphologicalSubrules/MorphologicalSubruleStructure[@isActive='yes']");
				foreach (XmlNode alloNode in allomorphList)
				{
					XmlElement alloElem = alloNode as XmlElement;
					string alloId = alloElem.GetAttribute("id");
					Allomorph allomorph = m_curMorpher.GetAllomorph(alloId);
					if (allomorph != null)
					{
						try
						{
							allomorph.RequiredAllomorphCoOccurrences = LoadAlloCoOccurs(alloElem.SelectSingleNode("RequiredAllomorphCoOccurrences"));
						}
						catch (LoadException le)
						{
							if (m_quitOnError)
								throw le;
						}
						try
						{
							allomorph.ExcludedAllomorphCoOccurrences = LoadAlloCoOccurs(alloElem.SelectSingleNode("ExcludedAllomorphCoOccurrences"));
						}
						catch (LoadException le)
						{
							if (m_quitOnError)
								throw le;
						}
					}
				}
			}

			XmlNodeList prules = langElem.SelectNodes("PhonologicalRules/*[@isActive='yes']");
			foreach (XmlNode pruleNode in prules)
			{
				XmlElement pruleElem = pruleNode as XmlElement;
				try
				{
					switch (pruleElem.Name)
					{
						case "MetathesisRule":
							LoadMetathesisRule(pruleElem);
							break;

						case "PhonologicalRule":
							LoadPRule(pruleElem);
							break;
					}
				}
				catch (LoadException le)
				{
					if (m_quitOnError)
						throw le;
				}
			}
		}

		void LoadMPRFeatGroup(XmlElement mprFeatGroupNode)
		{
			string mprFeatGroupId = mprFeatGroupNode.GetAttribute("id");
			string mprFeatGroupName = mprFeatGroupNode.SelectSingleNode("Name").InnerText;
			MPRFeatureGroup group = new MPRFeatureGroup(mprFeatGroupId, mprFeatGroupName, m_curMorpher);
			group.MatchType = GetGroupMatchType(mprFeatGroupNode.GetAttribute("matchType"));
			group.OutputType = GetGroupOutputType(mprFeatGroupNode.GetAttribute("outputType"));
			string mprFeatIdsStr = mprFeatGroupNode.GetAttribute("features");
			IEnumerable<MPRFeature> mprFeatures = LoadMPRFeatures(mprFeatIdsStr);
			foreach (MPRFeature mprFeat in mprFeatures)
				group.Add(mprFeat);
			m_curMorpher.AddMPRFeatureGroup(group);
		}

		void LoadStratum(XmlElement stratumNode)
		{
			string id = stratumNode.GetAttribute("id");
			string name = stratumNode.SelectSingleNode("Name").InnerText;
			Stratum stratum = new Stratum(id, name, m_curMorpher);
			stratum.CharacterDefinitionTable = GetCharDefTable(stratumNode.GetAttribute("characterDefinitionTable"));
			stratum.IsCyclic = stratumNode.GetAttribute("cyclicity") == "cyclic";
			stratum.PhonologicalRuleOrder = GetPRuleOrder(stratumNode.GetAttribute("phonologicalRuleOrder"));
			stratum.MorphologicalRuleOrder = GetMRuleOrder(stratumNode.GetAttribute("morphologicalRuleOrder"));

			string tempIdsStr = stratumNode.GetAttribute("affixTemplates");
			if (!string.IsNullOrEmpty(tempIdsStr))
			{
				string[] tempIds = tempIdsStr.Split(' ');
				foreach (string tempId in tempIds)
				{
					AffixTemplate template = m_curMorpher.GetAffixTemplate(tempId);
					if (template == null)
						throw CreateUndefinedObjectException(string.Format(HCStrings.kstidUnknownTemplate, tempId), tempId);
					stratum.AddAffixTemplate(template);
				}
			}

			m_curMorpher.AddStratum(stratum);
		}

		void LoadLexEntry(XmlElement entryNode)
		{
			string id = entryNode.GetAttribute("id");
			LexEntry entry = new LexEntry(id, id, m_curMorpher);

			string posId = entryNode.GetAttribute("partOfSpeech");
			PartOfSpeech pos = m_curMorpher.GetPOS(posId);
			if (pos == null)
				throw CreateUndefinedObjectException(string.Format(HCStrings.kstidUnknownPOS, posId), posId);
			entry.POS = pos;
			XmlElement glossElem = entryNode.SelectSingleNode("Gloss") as XmlElement;
			entry.Gloss = new Gloss(glossElem.GetAttribute("id"), glossElem.InnerText, m_curMorpher);

			entry.MPRFeatures = LoadMPRFeatures(entryNode.GetAttribute("ruleFeatures"));

			entry.HeadFeatures = LoadSynFeats(entryNode.SelectSingleNode("AssignedHeadFeatures"),
				m_curMorpher.HeadFeatureSystem);

			entry.FootFeatures = LoadSynFeats(entryNode.SelectSingleNode("AssignedFootFeatures"),
				m_curMorpher.FootFeatureSystem);

			Stratum stratum = GetStratum(entryNode.GetAttribute("stratum"));

			string familyId = entryNode.GetAttribute("family");
			if (!string.IsNullOrEmpty(familyId))
			{
				LexFamily family = m_curMorpher.Lexicon.GetFamily(familyId);
				if (family == null)
					throw CreateUndefinedObjectException(string.Format(HCStrings.kstidUnknownFamily, familyId), familyId);
				family.AddEntry(entry);
			}

			XmlNodeList alloNodes = entryNode.SelectNodes("Allomorphs/Allomorph[@isActive='yes']");
			foreach (XmlNode alloNode in alloNodes)
			{
				try
				{
					LoadAllomorph(alloNode as XmlElement, entry, stratum);
				}
				catch (LoadException le)
				{
					if (m_quitOnError)
						throw le;
				}
			}

			if (entry.AllomorphCount > 0)
			{
				stratum.AddEntry(entry);
				m_curMorpher.Lexicon.AddEntry(entry);
			}
		}

		void LoadAllomorph(XmlElement alloNode, LexEntry entry, Stratum stratum)
		{
			string alloId = alloNode.GetAttribute("id");
			string shapeStr = alloNode.SelectSingleNode("PhoneticShape").InnerText;
			PhoneticShape shape;
			try
			{
				shape = stratum.CharacterDefinitionTable.ToPhoneticShape(shapeStr, ModeType.SYNTHESIS);
			}
			catch (MissingPhoneticShapeException mpse)
			{
				LoadException le = new LoadException(LoadException.LoadErrorType.INVALID_ENTRY_SHAPE, this,
					string.Format(HCStrings.kstidInvalidLexEntryShape, shapeStr, entry.ID, stratum.CharacterDefinitionTable.ID, mpse.Position + 1, shapeStr.Substring(mpse.Position)));
				le.Data["shape"] = shapeStr;
				le.Data["charDefTable"] = stratum.CharacterDefinitionTable.ID;
				le.Data["entry"] = entry.ID;
				throw le;
			}

			if (shape.IsAllBoundaries)
			{
				LoadException le = new LoadException(LoadException.LoadErrorType.INVALID_ENTRY_SHAPE, this,
					string.Format(HCStrings.kstidInvalidLexEntryShapeAllBoundaries, shapeStr, entry.ID, stratum.CharacterDefinitionTable.ID));
				le.Data["shape"] = shapeStr;
				le.Data["charDefTable"] = stratum.CharacterDefinitionTable.ID;
				le.Data["entry"] = entry.ID;
				throw le;
			}
			LexEntry.RootAllomorph allomorph = new LexEntry.RootAllomorph(alloId, shapeStr, m_curMorpher, shape);
			allomorph.RequiredEnvironments = LoadEnvs(alloNode.SelectSingleNode("RequiredEnvironments"));
			allomorph.ExcludedEnvironments = LoadEnvs(alloNode.SelectSingleNode("ExcludedEnvironments"));
			allomorph.Properties = LoadProperties(alloNode.SelectSingleNode("Properties"));
			entry.AddAllomorph(allomorph);

			m_curMorpher.AddAllomorph(allomorph);
		}

		IEnumerable<Environment> LoadEnvs(XmlNode envsNode)
		{
			if (envsNode == null)
				return null;

			List<Environment> envs = new List<Environment>();
			XmlNodeList envList = envsNode.SelectNodes("Environment");
			foreach (XmlNode envNode in envList)
				envs.Add(LoadEnv(envNode, null, null));
			return envs;
		}

		Environment LoadEnv(XmlNode envNode, AlphaVariables varFeats, Dictionary<string, string> varFeatIds)
		{
			if (envNode == null)
				return new Environment();

			PhoneticPattern leftEnv = LoadPTemp(envNode.SelectSingleNode("LeftEnvironment/PhoneticTemplate") as XmlElement,
				varFeats, varFeatIds, null);
			PhoneticPattern rightEnv = LoadPTemp(envNode.SelectSingleNode("RightEnvironment/PhoneticTemplate") as XmlElement,
				varFeats, varFeatIds, null);
			return new Environment(leftEnv, rightEnv);
		}

		IEnumerable<MorphCoOccurrence> LoadMorphCoOccurs(XmlNode coOccursNode)
		{
			if (coOccursNode == null)
				return null;

			List<MorphCoOccurrence> coOccurs = new List<MorphCoOccurrence>();
			XmlNodeList coOccurList = coOccursNode.SelectNodes("MorphemeCoOccurrence");
			foreach (XmlNode coOccurNode in coOccurList)
				coOccurs.Add(LoadMorphCoOccur(coOccurNode as XmlElement));
			return coOccurs;
		}

		MorphCoOccurrence LoadMorphCoOccur(XmlElement coOccurNode)
		{
			MorphCoOccurrence.AdjacencyType adjacency = GetAdjacencyType(coOccurNode.GetAttribute("adjacency"));
			string[] morphemeIds = coOccurNode.GetAttribute("morphemes").Split(' ');
			HCObjectSet<HCObject> morphemes = new HCObjectSet<HCObject>();
			foreach (string morphemeId in morphemeIds)
			{
				Morpheme morpheme = m_curMorpher.GetMorpheme(morphemeId);
				if (morpheme == null)
					throw CreateUndefinedObjectException(string.Format(HCStrings.kstidUnknownMorpheme, morphemeId), morphemeId);
				morphemes.Add(morpheme);
			}
			return new MorphCoOccurrence(morphemes, MorphCoOccurrence.ObjectType.MORPHEME, adjacency);
		}

		IEnumerable<MorphCoOccurrence> LoadAlloCoOccurs(XmlNode coOccursNode)
		{
			if (coOccursNode == null)
				return null;

			List<MorphCoOccurrence> coOccurs = new List<MorphCoOccurrence>();
			XmlNodeList coOccurList = coOccursNode.SelectNodes("AllomorphCoOccurrence");
			foreach (XmlNode coOccurNode in coOccurList)
				coOccurs.Add(LoadAlloCoOccur(coOccurNode as XmlElement));
			return coOccurs;
		}

		MorphCoOccurrence LoadAlloCoOccur(XmlElement coOccurNode)
		{
			MorphCoOccurrence.AdjacencyType adjacency = GetAdjacencyType(coOccurNode.GetAttribute("adjacency"));
			string[] allomorphIds = coOccurNode.GetAttribute("allomorphs").Split(' ');
			HCObjectSet<HCObject> allomorphs = new HCObjectSet<HCObject>();
			foreach (string allomorphId in allomorphIds)
			{
				Allomorph allomorph = m_curMorpher.GetAllomorph(allomorphId);
				if (allomorph == null)
					throw CreateUndefinedObjectException(string.Format(HCStrings.kstidUnknownAllo, allomorphId), allomorphId);
				allomorphs.Add(allomorph);
			}
			return new MorphCoOccurrence(allomorphs, MorphCoOccurrence.ObjectType.ALLOMORPH, adjacency);
		}

		IDictionary<string, string> LoadProperties(XmlNode propsNode)
		{
			if (propsNode == null)
				return null;

			Dictionary<string, string> properties = new Dictionary<string, string>();
			if (propsNode != null)
			{
				XmlNodeList propList = propsNode.SelectNodes("Property");
				foreach (XmlNode propNode in propList)
				{
					XmlElement propElem = propNode as XmlElement;
					properties[propElem.GetAttribute("name")] = propElem.InnerText;
				}
			}
			return properties;
		}

		FeatureValues LoadSynFeats(XmlNode node, FeatureSystem featSys)
		{
			FeatureValues fvs = new FeatureValues();
			if (node != null)
			{
				XmlNodeList featValList = node.SelectNodes("FeatureValueList[@isActive='yes']");
				foreach (XmlNode featValNode in featValList)
				{
					XmlElement featValElem = featValNode as XmlElement;
					string featId = featValElem.GetAttribute("feature");
					Feature feature = featSys.GetFeature(featId);
					if (feature == null)
						throw CreateUndefinedObjectException(string.Format(HCStrings.kstidUnknownFeat, featId), featId);
					string valueIdsStr = featValElem.GetAttribute("values");
					if (!string.IsNullOrEmpty(valueIdsStr))
					{
						string[] valueIds = valueIdsStr.Split(' ');
						foreach (string valueId in valueIds)
						{
							FeatureValue value = feature.GetPossibleValue(valueId);
							if (value == null)
								throw CreateUndefinedObjectException(string.Format(HCStrings.kstidUnknownFeatValue, valueId, featId), valueId);
							fvs.Add(feature, new ClosedValueInstance(value));
						}
					}
					else
					{
						fvs.Add(feature, LoadSynFeats(featValNode, featSys));
					}
				}
			}

			return fvs;
		}

		void LoadFeatureSystem(XmlElement featSysNode, FeatureSystem featSys)
		{
			XmlNodeList features = featSysNode.SelectNodes("FeatureDefinition[@isActive='yes']");
			foreach (XmlNode featDefNode in features)
				LoadFeature(featDefNode as XmlElement, featSysNode, featSys);
		}

		Feature LoadFeature(XmlElement featDefNode, XmlElement featSysNode, FeatureSystem featSys)
		{
			XmlElement featElem = featDefNode.SelectSingleNode("Feature") as XmlElement;
			string featId = featElem.GetAttribute("id");
			Feature feature = featSys.GetFeature(featId);
			if (feature != null)
				return feature;

			string featureName = featElem.InnerText;
			string defValId = featElem.GetAttribute("defaultValue");
			feature = new Feature(featId, featureName, m_curMorpher);
			XmlNode valueListNode = featDefNode.SelectSingleNode("ValueList");
			if (valueListNode != null)
			{
				XmlNodeList valueList = valueListNode.SelectNodes("Value");
				foreach (XmlNode valueNode in valueList)
				{
					XmlElement valueElem = valueNode as XmlElement;
					string valueId = valueElem.GetAttribute("id");
					FeatureValue value = new FeatureValue(valueId, valueElem.InnerText, m_curMorpher);
					try
					{
						featSys.AddValue(value);
						feature.AddPossibleValue(value);
					}
					catch (InvalidOperationException ioe)
					{
						throw new LoadException(LoadException.LoadErrorType.TOO_MANY_FEATURE_VALUES, this,
							HCStrings.kstidTooManyFeatValues, ioe);
					}
				}
				if (!string.IsNullOrEmpty(defValId))
					feature.DefaultValue = new ClosedValueInstance(feature.GetPossibleValue(defValId));
			}
			else
			{
				XmlElement featListElem = featDefNode.SelectSingleNode("FeatureList") as XmlElement;
				string featDefIdsStr = featListElem.GetAttribute("features");
				string[] featDefIds = featDefIdsStr.Split(' ');
				foreach (string featDefId in featDefIds)
				{
					XmlNode subFeatDefNode = featSysNode.SelectSingleNode(string.Format("FeatureDefinition[@id = '{0}']", featDefId));
					Feature subFeature = LoadFeature(subFeatDefNode as XmlElement, featSysNode, featSys);
					feature.AddSubFeature(subFeature);
				}
			}
			featSys.AddFeature(feature);
			return feature;
		}

		void LoadCharDefTable(XmlElement charDefTableNode)
		{
			string id = charDefTableNode.GetAttribute("id");
			string name = charDefTableNode.SelectSingleNode("Name").InnerText;
			CharacterDefinitionTable charDefTable = null;

#if IPA_CHAR_DEF_TABLE
			if (id == "ipa")
				charDefTable = new IPACharacterDefinitionTable(id, name, m_curMorpher);
			else
				charDefTable = new CharacterDefinitionTable(id, name, m_curMorpher);
#else
			charDefTable = new CharacterDefinitionTable(id, name, m_curMorpher);
#endif

			charDefTable.Encoding = charDefTableNode.SelectSingleNode("Encoding").InnerText;

			XmlNodeList segDefList = charDefTableNode.SelectNodes("SegmentDefinitions/SegmentDefinition[@isActive='yes']");
			foreach (XmlNode segDefNode in segDefList)
			{
				XmlElement segDefElem = segDefNode as XmlElement;
				XmlElement repElem = segDefElem.SelectSingleNode("Representation") as XmlElement;
				string strRep = repElem.InnerText;
				charDefTable.AddSegmentDefinition(strRep, LoadFeatValues(segDefElem));
				m_repIds[repElem.GetAttribute("id")] = strRep;
			}

			XmlNodeList bdryDefList = charDefTableNode.SelectNodes("BoundaryDefinitions/BoundarySymbol");
			foreach (XmlNode bdryDefNode in bdryDefList)
			{
				XmlElement bdryDefElem = bdryDefNode as XmlElement;
				string strRep = bdryDefElem.InnerText;
				charDefTable.AddBoundaryDefinition(strRep);
				m_repIds[bdryDefElem.GetAttribute("id")] = strRep;
			}

			m_curMorpher.AddCharacterDefinitionTable(charDefTable);
		}

		void LoadFeatNatClass(XmlElement natClassNode)
		{
			string id = natClassNode.GetAttribute("id");
			string name = natClassNode.SelectSingleNode("Name").InnerText;
			NaturalClass natClass = new NaturalClass(id, name, m_curMorpher);
			ICollection<FeatureValue> featVals = LoadFeatValues(natClassNode);
			natClass.Features = new FeatureBundle(featVals, m_curMorpher.PhoneticFeatureSystem);

			m_curMorpher.AddNaturalClass(natClass);
		}

		void LoadSegNatClass(XmlElement natClassNode)
		{
			string id = natClassNode.GetAttribute("id");
			string name = natClassNode.SelectSingleNode("Name").InnerText;
			NaturalClass natClass = new NaturalClass(id, name, m_curMorpher);
			XmlNodeList segList = natClassNode.SelectNodes("Segment");
			FeatureBundle fb = null;
			if (segList != null)
			{
				foreach (XmlNode segNode in segList)
				{
					XmlElement segElem = segNode as XmlElement;

					CharacterDefinitionTable charDefTable = GetCharDefTable(segElem.GetAttribute("characterTable"));
					string strRep = m_repIds[segElem.GetAttribute("representation")];
					SegmentDefinition segDef = charDefTable.GetSegmentDefinition(strRep);
					if (m_curMorpher.PhoneticFeatureSystem.HasFeatures)
					{
						if (fb == null)
							fb = segDef.SynthFeatures.Clone();
						else
							fb.Intersection(segDef.SynthFeatures);
					}
					else
					{
						natClass.AddSegmentDefinition(segDef);
					}
				}
			}
			if (fb == null)
				fb = new FeatureBundle(false, m_curMorpher.PhoneticFeatureSystem);
			natClass.Features = fb;

			m_curMorpher.AddNaturalClass(natClass);
		}

		ICollection<FeatureValue> LoadFeatValues(XmlNode node)
		{
			List<FeatureValue> featVals = new List<FeatureValue>();
			XmlNodeList featValList = node.SelectNodes("FeatureValuePair[@isActive='yes']");
			foreach (XmlNode featValNode in featValList)
			{
				XmlElement featValElem = featValNode as XmlElement;
				string featId = featValElem.GetAttribute("feature");
				Feature feature = m_curMorpher.PhoneticFeatureSystem.GetFeature(featId);
				if (feature == null)
					throw CreateUndefinedObjectException(string.Format(HCStrings.kstidUnknownFeat, featId), featId);
				string valueId = featValElem.GetAttribute("value");
				FeatureValue value = feature.GetPossibleValue(valueId);
				if (value == null)
					throw CreateUndefinedObjectException(string.Format(HCStrings.kstidUnknownFeatValue, valueId, featId), valueId);
				featVals.Add(value);
			}
			return featVals;
		}

		void LoadPRule(XmlElement pruleNode)
		{
			string id = pruleNode.GetAttribute("id");
			string name = pruleNode.SelectSingleNode("Name").InnerText;
			StandardPhonologicalRule prule = new StandardPhonologicalRule(id, name, m_curMorpher);
			prule.MultApplication = GetMultAppOrder(pruleNode.GetAttribute("multipleApplicationOrder"));
			Dictionary<string, string> varFeatIds;
			prule.AlphaVariables = LoadAlphaVars(pruleNode.SelectSingleNode("VariableFeatures") as XmlElement,
				out varFeatIds);
			XmlElement pseqElem = pruleNode.SelectSingleNode("PhoneticInputSequence/PhoneticSequence") as XmlElement;
			prule.LHS = new PhoneticPattern(true);
			LoadPSeq(prule.LHS, pseqElem, prule.AlphaVariables, varFeatIds, null);

			XmlNodeList subruleList = pruleNode.SelectNodes("PhonologicalSubrules/PhonologicalSubrule");
			foreach (XmlNode subruleNode in subruleList)
				LoadPSubrule(subruleNode as XmlElement, prule, varFeatIds);

			m_curMorpher.AddPhonologicalRule(prule);
			string[] stratumIds = pruleNode.GetAttribute("ruleStrata").Split(' ');
			foreach (string stratumId in stratumIds)
				GetStratum(stratumId).AddPhonologicalRule(prule);
		}

		void LoadPSubrule(XmlElement psubruleNode, StandardPhonologicalRule prule, Dictionary<string, string> varFeatIds)
		{
			XmlElement structElem = psubruleNode.SelectSingleNode("PhonologicalSubruleStructure[@isActive='yes']") as XmlElement;
			PhoneticPattern rhs = new PhoneticPattern(true);
			LoadPSeq(rhs, structElem.SelectSingleNode("PhoneticOutput/PhoneticSequence") as XmlElement, prule.AlphaVariables,
				varFeatIds, null);

			Environment env = LoadEnv(structElem.SelectSingleNode("Environment"), prule.AlphaVariables, varFeatIds);

			StandardPhonologicalRule.Subrule sr = null;
			try
			{
				sr = new StandardPhonologicalRule.Subrule(rhs, env, prule);
			}
			catch (ArgumentException ae)
			{
				LoadException le = new LoadException(LoadException.LoadErrorType.INVALID_SUBRULE_TYPE, this,
					HCStrings.kstidInvalidSubruleType, ae);
				le.Data["rule"] = prule.ID;
				throw le;
			}

			sr.RequiredPOSs = LoadPOSs(structElem.GetAttribute("requiredPartsOfSpeech"));

			sr.RequiredMPRFeatures = LoadMPRFeatures(structElem.GetAttribute("requiredMPRFeatures"));
			sr.ExcludedMPRFeatures = LoadMPRFeatures(structElem.GetAttribute("excludedMPRFeatures"));

			prule.AddSubrule(sr);
		}

		void LoadMetathesisRule(XmlElement metathesisNode)
		{
			string id = metathesisNode.GetAttribute("id");
			string name = metathesisNode.SelectSingleNode("Name").InnerText;
			MetathesisRule metathesisRule = new MetathesisRule(id, name, m_curMorpher);
			metathesisRule.MultApplication = GetMultAppOrder(metathesisNode.GetAttribute("multipleApplicationOrder"));

			string[] changeIds = metathesisNode.GetAttribute("structuralChange").Split(' ');
			Dictionary<string, int> partIds = new Dictionary<string, int>();
			int partition = 0;
			foreach (string changeId in changeIds)
				partIds[changeId] = partition++;

			metathesisRule.Pattern = LoadPTemp(metathesisNode.SelectSingleNode("StructuralDescription/PhoneticTemplate") as XmlElement,
				null, null, partIds);

			m_curMorpher.AddPhonologicalRule(metathesisRule);
			string[] stratumIds = metathesisNode.GetAttribute("ruleStrata").Split(' ');
			foreach (string stratumId in stratumIds)
				GetStratum(stratumId).AddPhonologicalRule(metathesisRule);
		}

		void LoadMRule(XmlElement mruleNode)
		{
			string id = mruleNode.GetAttribute("id");
			string name = mruleNode.SelectSingleNode("Name").InnerText;
			AffixalMorphologicalRule mrule = new AffixalMorphologicalRule(id, name, m_curMorpher);
			XmlElement glossElem = mruleNode.SelectSingleNode("Gloss") as XmlElement;
			if (glossElem != null)
				mrule.Gloss = new Gloss(glossElem.GetAttribute("id"), glossElem.InnerText, m_curMorpher);

			string multApp = mruleNode.GetAttribute("multipleApplication");
			if (!string.IsNullOrEmpty(multApp))
				mrule.MaxNumApps = Convert.ToInt32(multApp);

			mrule.RequiredPOSs = LoadPOSs(mruleNode.GetAttribute("requiredPartsOfSpeech"));

			string outPOSId = mruleNode.GetAttribute("outputPartOfSpeech");
			PartOfSpeech outPOS = null;
			if (!string.IsNullOrEmpty(outPOSId))
			{
				outPOS = m_curMorpher.GetPOS(outPOSId);
				if (outPOS == null)
					throw CreateUndefinedObjectException(string.Format(HCStrings.kstidUnknownPOS, outPOSId), outPOSId);
			}
			mrule.OutPOS = outPOS;

			mrule.RequiredHeadFeatures = LoadSynFeats(mruleNode.SelectSingleNode("RequiredHeadFeatures"),
				m_curMorpher.HeadFeatureSystem);
			mrule.RequiredFootFeatures = LoadSynFeats(mruleNode.SelectSingleNode("RequiredFootFeatures"),
				m_curMorpher.FootFeatureSystem);

			mrule.OutHeadFeatures = LoadSynFeats(mruleNode.SelectSingleNode("OutputHeadFeatures"),
				m_curMorpher.HeadFeatureSystem);
			mrule.OutFootFeatures = LoadSynFeats(mruleNode.SelectSingleNode("OutputFootFeatures"),
				m_curMorpher.FootFeatureSystem);

			HCObjectSet<Feature> obligHeadFeats = new HCObjectSet<Feature>();
			string obligHeadIdsStr = mruleNode.GetAttribute("outputObligatoryFeatures");
			if (!string.IsNullOrEmpty(obligHeadIdsStr))
			{
				string[] obligHeadIds = obligHeadIdsStr.Split(' ');
				foreach (string obligHeadId in obligHeadIds)
				{
					Feature feature = m_curMorpher.HeadFeatureSystem.GetFeature(obligHeadId);
					if (feature == null)
						throw CreateUndefinedObjectException(string.Format(HCStrings.kstidUnknownFeat, obligHeadId), obligHeadId);
					obligHeadFeats.Add(feature);
				}
			}
			mrule.ObligatoryHeadFeatures = obligHeadFeats;

			mrule.IsBlockable = mruleNode.GetAttribute("blockable") == "true";

			XmlNodeList subruleList = mruleNode.SelectNodes("MorphologicalSubrules/MorphologicalSubruleStructure[@isActive='yes']");
			foreach (XmlNode subruleNode in subruleList)
			{
				try
				{
					LoadMSubrule(subruleNode as XmlElement, mrule);
				}
				catch (LoadException le)
				{
					if (m_quitOnError)
						throw le;
				}
			}

			if (mrule.SubruleCount > 0)
				m_curMorpher.AddMorphologicalRule(mrule);
		}

		void LoadRealRule(XmlElement realRuleNode)
		{
			string id = realRuleNode.GetAttribute("id");
			string name = realRuleNode.SelectSingleNode("Name").InnerText;
			RealizationalRule realRule = new RealizationalRule(id, name, m_curMorpher);
			XmlElement glossElem = realRuleNode.SelectSingleNode("Gloss") as XmlElement;
			if (glossElem != null)
				realRule.Gloss = new Gloss(glossElem.GetAttribute("id"), glossElem.InnerText, m_curMorpher);

			realRule.RequiredHeadFeatures = LoadSynFeats(realRuleNode.SelectSingleNode("RequiredHeadFeatures"),
				m_curMorpher.HeadFeatureSystem);
			realRule.RequiredFootFeatures = LoadSynFeats(realRuleNode.SelectSingleNode("RequiredFootFeatures"),
				m_curMorpher.FootFeatureSystem);

			realRule.RealizationalFeatures = LoadSynFeats(realRuleNode.SelectSingleNode("RealizationalFeatures"), m_curMorpher.HeadFeatureSystem);

			realRule.IsBlockable = realRuleNode.GetAttribute("blockable") == "true";

			XmlNodeList subruleList = realRuleNode.SelectNodes("MorphologicalSubrules/MorphologicalSubruleStructure[@isActive='yes']");
			foreach (XmlNode subruleNode in subruleList)
			{
				try
				{
					LoadMSubrule(subruleNode as XmlElement, realRule);
				}
				catch (LoadException le)
				{
					if (m_quitOnError)
						throw le;
				}
			}

			if (realRule.SubruleCount > 0)
				m_curMorpher.AddMorphologicalRule(realRule);
		}

		void LoadMSubrule(XmlElement msubruleNode, AffixalMorphologicalRule mrule)
		{
			string id = msubruleNode.GetAttribute("id");
			Dictionary<string, string> varFeatIds;
			AlphaVariables varFeats = LoadAlphaVars(msubruleNode.SelectSingleNode("VariableFeatures"), out varFeatIds);

			XmlElement inputElem = msubruleNode.SelectSingleNode("InputSideRecordStructure") as XmlElement;

			Dictionary<string, int> partIds = new Dictionary<string, int>();
			List<PhoneticPattern> lhsList = LoadReqPhoneticInput(inputElem.SelectSingleNode("RequiredPhoneticInput"), 0,
				varFeats, varFeatIds, partIds);

			XmlElement outputElem = msubruleNode.SelectSingleNode("OutputSideRecordStructure") as XmlElement;

			List<MorphologicalOutput> rhsList = LoadPhoneticOutput(outputElem.SelectSingleNode("MorphologicalPhoneticOutput"),
				varFeats, varFeatIds, partIds, mrule.ID);

			MorphologicalTransform.RedupMorphType redupMorphType = GetRedupMorphType(outputElem.GetAttribute("redupMorphType"));

			AffixalMorphologicalRule.Subrule sr = new AffixalMorphologicalRule.Subrule(id, id, m_curMorpher,
				lhsList, rhsList, varFeats, redupMorphType);

			sr.RequiredMPRFeatures = LoadMPRFeatures(inputElem.GetAttribute("requiredMPRFeatures"));
			sr.ExcludedMPRFeatures = LoadMPRFeatures(inputElem.GetAttribute("excludedMPRFeatures"));
			sr.OutputMPRFeatures = LoadMPRFeatures(outputElem.GetAttribute("MPRFeatures"));

			sr.RequiredEnvironments = LoadEnvs(msubruleNode.SelectSingleNode("RequiredEnvironments"));
			sr.ExcludedEnvironments = LoadEnvs(msubruleNode.SelectSingleNode("ExcludedEnvironments"));

			sr.Properties = LoadProperties(msubruleNode.SelectSingleNode("Properties"));

			mrule.AddSubrule(sr);
			m_curMorpher.AddAllomorph(sr);
		}

		List<PhoneticPattern> LoadReqPhoneticInput(XmlNode reqPhonInputNode, int partition, AlphaVariables varFeats, Dictionary<string, string> varFeatIds,
			Dictionary<string, int> partIds)
		{
			List<PhoneticPattern> lhsList = new List<PhoneticPattern>();
			XmlNodeList pseqList = reqPhonInputNode.SelectNodes("PhoneticSequence");
			foreach (XmlNode pseqNode in pseqList)
			{
				XmlElement pseqElem = pseqNode as XmlElement;
				PhoneticPattern pattern = new PhoneticPattern();
				LoadPSeq(pattern, pseqElem, varFeats, varFeatIds, null);
				lhsList.Add(pattern);
				partIds[pseqElem.GetAttribute("id")] = partition++;
			}
			return lhsList;
		}

		List<MorphologicalOutput> LoadPhoneticOutput(XmlNode phonOutputNode, AlphaVariables varFeats, Dictionary<string, string> varFeatIds,
			Dictionary<string, int> partIds, string ruleId)
		{
			List<MorphologicalOutput> rhsList = new List<MorphologicalOutput>();
			foreach (XmlNode partNode in phonOutputNode.ChildNodes)
			{
				if (partNode.NodeType != XmlNodeType.Element)
					continue;
				XmlElement partElem = partNode as XmlElement;
				switch (partElem.Name)
				{
					case "CopyFromInput":
						rhsList.Add(new CopyFromInput(partIds[partElem.GetAttribute("index")]));
						break;

					case "InsertSimpleContext":
						SimpleContext insCtxt = LoadNatClassCtxt(partElem.SelectSingleNode("SimpleContext") as XmlElement,
							varFeats, varFeatIds);
						rhsList.Add(new InsertSimpleContext(insCtxt));
						break;

					case "ModifyFromInput":
						SimpleContext modCtxt = LoadNatClassCtxt(partElem.SelectSingleNode("SimpleContext") as XmlElement,
							varFeats, varFeatIds);
						rhsList.Add(new ModifyFromInput(partIds[partElem.GetAttribute("index")], modCtxt, m_curMorpher));
						break;

					case "InsertSegments":
						CharacterDefinitionTable charDefTable = GetCharDefTable(partElem.GetAttribute("characterTable"));
						string shapeStr = partElem.SelectSingleNode("PhoneticShape").InnerText;
						PhoneticShape pshape;
						try
						{
							pshape = charDefTable.ToPhoneticShape(shapeStr, ModeType.SYNTHESIS);
						}
						catch (MissingPhoneticShapeException mpse)
						{
							LoadException le = new LoadException(LoadException.LoadErrorType.INVALID_RULE_SHAPE, this,
								string.Format(HCStrings.kstidInvalidRuleShape, shapeStr, ruleId, charDefTable.ID, mpse.Position+1, shapeStr.Substring(mpse.Position)));
							le.Data["shape"] = shapeStr;
							le.Data["charDefTable"] = charDefTable.ID;
							le.Data["rule"] = ruleId;
							throw le;
						}

						rhsList.Add(new InsertSegments(pshape));
						break;
				}
			}
			return rhsList;
		}

		void LoadCompoundRule(XmlElement compRuleNode)
		{
			string id = compRuleNode.GetAttribute("id");
			string name = compRuleNode.SelectSingleNode("Name").InnerText;
			CompoundingRule compRule = new CompoundingRule(id, name, m_curMorpher);
			XmlElement glossElem = compRuleNode.SelectSingleNode("Gloss") as XmlElement;
			if (glossElem != null)
				compRule.Gloss = new Gloss(glossElem.GetAttribute("id"), glossElem.InnerText, m_curMorpher);

			string multApp = compRuleNode.GetAttribute("multipleApplication");
			if (!string.IsNullOrEmpty(multApp))
				compRule.MaxNumApps = Convert.ToInt32(multApp);

			compRule.HeadRequiredPOSs = LoadPOSs(compRuleNode.GetAttribute("headPartsOfSpeech"));

			compRule.NonHeadRequiredPOSs = LoadPOSs(compRuleNode.GetAttribute("nonheadPartsOfSpeech"));

			string outPOSId = compRuleNode.GetAttribute("outputPartOfSpeech");
			PartOfSpeech outPOS = null;
			if (!string.IsNullOrEmpty(outPOSId))
			{
				outPOS = m_curMorpher.GetPOS(outPOSId);
				if (outPOS == null)
					throw CreateUndefinedObjectException(string.Format(HCStrings.kstidUnknownPOS, outPOSId), outPOSId);
			}
			compRule.OutPOS = outPOS;

			compRule.HeadRequiredHeadFeatures = LoadSynFeats(compRuleNode.SelectSingleNode("HeadRequiredHeadFeatures"),
				m_curMorpher.HeadFeatureSystem);
			compRule.HeadRequiredFootFeatures = LoadSynFeats(compRuleNode.SelectSingleNode("HeadRequiredFootFeatures"),
				m_curMorpher.FootFeatureSystem);

			compRule.NonHeadRequiredHeadFeatures = LoadSynFeats(compRuleNode.SelectSingleNode("NonHeadRequiredHeadFeatures"),
				m_curMorpher.HeadFeatureSystem);
			compRule.NonHeadRequiredFootFeatures = LoadSynFeats(compRuleNode.SelectSingleNode("NonHeadRequiredFootFeatures"),
				m_curMorpher.FootFeatureSystem);

			compRule.OutHeadFeatures = LoadSynFeats(compRuleNode.SelectSingleNode("OutputHeadFeatures"),
				m_curMorpher.HeadFeatureSystem);
			compRule.OutFootFeatures = LoadSynFeats(compRuleNode.SelectSingleNode("OutputFootFeatures"),
				m_curMorpher.FootFeatureSystem);

			HCObjectSet<Feature> obligHeadFeats = new HCObjectSet<Feature>();
			string obligHeadIdsStr = compRuleNode.GetAttribute("outputObligatoryFeatures");
			if (!string.IsNullOrEmpty(obligHeadIdsStr))
			{
				string[] obligHeadIds = obligHeadIdsStr.Split(' ');
				foreach (string obligHeadId in obligHeadIds)
				{
					Feature feature = m_curMorpher.HeadFeatureSystem.GetFeature(obligHeadId);
					if (feature == null)
						throw CreateUndefinedObjectException(string.Format(HCStrings.kstidUnknownFeat, obligHeadId), obligHeadId);
					obligHeadFeats.Add(feature);
				}
			}
			compRule.ObligatoryHeadFeatures = obligHeadFeats;

			compRule.IsBlockable = compRuleNode.GetAttribute("blockable") == "true";

			XmlNodeList subruleList = compRuleNode.SelectNodes("CompoundSubrules/CompoundSubruleStructure[@isActive='yes']");
			foreach (XmlNode subruleNode in subruleList)
			{
				try
				{
					LoadCompoundSubrule(subruleNode as XmlElement, compRule);
				}
				catch (LoadException le)
				{
					if (m_quitOnError)
						throw le;
				}
			}

			if (compRule.SubruleCount > 0)
				m_curMorpher.AddMorphologicalRule(compRule);
		}

		void LoadCompoundSubrule(XmlElement compSubruleNode, CompoundingRule compRule)
		{
			string id = compSubruleNode.GetAttribute("id");
			Dictionary<string, string> varFeatIds;
			AlphaVariables varFeats = LoadAlphaVars(compSubruleNode.SelectSingleNode("VariableFeatures"), out varFeatIds);

			XmlElement headElem = compSubruleNode.SelectSingleNode("HeadRecordStructure") as XmlElement;

			Dictionary<string, int> partIds = new Dictionary<string, int>();
			List<PhoneticPattern> headLhsList = LoadReqPhoneticInput(headElem.SelectSingleNode("RequiredPhoneticInput"), 0,
				varFeats, varFeatIds, partIds);

			List<PhoneticPattern> nonHeadLhsList = LoadReqPhoneticInput(compSubruleNode.SelectSingleNode("NonHeadRecordStructure/RequiredPhoneticInput"),
				headLhsList.Count, varFeats, varFeatIds, partIds);

			XmlElement outputElem = compSubruleNode.SelectSingleNode("OutputSideRecordStructure") as XmlElement;

			List<MorphologicalOutput> rhsList = LoadPhoneticOutput(outputElem.SelectSingleNode("MorphologicalPhoneticOutput"), varFeats,
				varFeatIds, partIds, compRule.ID);

			CompoundingRule.Subrule sr = new CompoundingRule.Subrule(id, id, m_curMorpher,
				headLhsList, nonHeadLhsList, rhsList, varFeats);

			sr.RequiredMPRFeatures = LoadMPRFeatures(headElem.GetAttribute("requiredMPRFeatures"));
			sr.ExcludedMPRFeatures = LoadMPRFeatures(headElem.GetAttribute("excludedMPRFeatures"));
			sr.OutputMPRFeatures = LoadMPRFeatures(outputElem.GetAttribute("MPRFeatures"));

			sr.Properties = LoadProperties(compSubruleNode.SelectSingleNode("Properties"));

			compRule.AddSubrule(sr);
		}

		void LoadAffixTemplate(XmlElement tempNode, HCObjectSet<MorphologicalRule> templateRules)
		{
			string id = tempNode.GetAttribute("id");
			string name = tempNode.SelectSingleNode("Name").InnerText;
			AffixTemplate template = new AffixTemplate(id, name, m_curMorpher);

			string posIdsStr = tempNode.GetAttribute("requiredPartsOfSpeech");
			template.RequiredPOSs = LoadPOSs(posIdsStr);

			XmlNodeList slotList = tempNode.SelectNodes("Slot[@isActive='yes']");
			foreach (XmlNode slotNode in slotList)
			{
				XmlElement slotElem = slotNode as XmlElement;
				string slotId = slotElem.GetAttribute("id");
				string slotName = slotElem.SelectSingleNode("Name").InnerText;

				Slot slot = new Slot(slotId, slotName, m_curMorpher);
				string ruleIdsStr = slotElem.GetAttribute("morphologicalRules");
				string[] ruleIds = ruleIdsStr.Split(' ');
				MorphologicalRule lastRule = null;
				foreach (string ruleId in ruleIds)
				{
					MorphologicalRule rule = m_curMorpher.GetMorphologicalRule(ruleId);
					if (rule != null)
					{
						slot.AddRule(rule);
						lastRule = rule;
						templateRules.Add(rule);
					}
					else
					{
						if (m_quitOnError)
							throw CreateUndefinedObjectException(string.Format(HCStrings.kstidUnknownMRule, ruleId), ruleId);
					}
				}

				string optionalStr = slotElem.GetAttribute("optional");
				if (string.IsNullOrEmpty(optionalStr) && lastRule is RealizationalRule)
					slot.IsOptional = (lastRule as RealizationalRule).RealizationalFeatures.NumFeatures > 0;
				else
					slot.IsOptional = optionalStr == "true";
				template.AddSlot(slot);
			}

			m_curMorpher.AddAffixTemplate(template);
		}

		IEnumerable<PartOfSpeech> LoadPOSs(string posIdsStr)
		{
			HCObjectSet<PartOfSpeech> result = new HCObjectSet<PartOfSpeech>();
			if (!string.IsNullOrEmpty(posIdsStr))
			{
				string[] posIds = posIdsStr.Split(' ');
				foreach (string posId in posIds)
				{
					PartOfSpeech pos = m_curMorpher.GetPOS(posId);
					if (pos == null)
						throw CreateUndefinedObjectException(string.Format(HCStrings.kstidUnknownPOS, posId), posId);
					result.Add(pos);
				}
			}
			return result;
		}

		MPRFeatureSet LoadMPRFeatures(string mprFeatIdsStr)
		{
			if (string.IsNullOrEmpty(mprFeatIdsStr))
				return null;

			MPRFeatureSet result = new MPRFeatureSet();
			string[] mprFeatIds = mprFeatIdsStr.Split(' ');
			foreach (string mprFeatId in mprFeatIds)
			{
				MPRFeature mprFeat = m_curMorpher.GetMPRFeature(mprFeatId);
				if (mprFeat == null)
					throw CreateUndefinedObjectException(string.Format(HCStrings.kstidUnknownMPRFeat, mprFeatId), mprFeatId);
				result.Add(mprFeat);
			}
			return result;
		}

		AlphaVariables LoadAlphaVars(XmlNode alphaVarsNode, out Dictionary<string, string> varFeatIds)
		{
			Dictionary<string, Feature> varFeats = new Dictionary<string, Feature>();
			varFeatIds = new Dictionary<string, string>();
			if (alphaVarsNode != null)
			{
				XmlNodeList varFeatList = alphaVarsNode.SelectNodes("VariableFeature");
				foreach (XmlNode varFeatNode in varFeatList)
				{
					XmlElement varFeatElem = varFeatNode as XmlElement;
					string varName = varFeatElem.GetAttribute("name");
					string featId = varFeatElem.GetAttribute("phonologicalFeature");
					Feature feature = m_curMorpher.PhoneticFeatureSystem.GetFeature(featId);
					if (feature == null)
						throw CreateUndefinedObjectException(string.Format(HCStrings.kstidUnknownFeat, featId), featId);
					varFeats[varName] = feature;
					varFeatIds[varFeatElem.GetAttribute("id")] = varName;
				}
			}

			return new AlphaVariables(varFeats);
		}

		void LoadPSeq(PhoneticPattern pattern, XmlElement pseqNode, AlphaVariables alphaVars, Dictionary<string, string> varFeatIds,
			Dictionary<string, int> partIds)
		{
			if (pseqNode == null)
				return;

			foreach (XmlNode recNode in pseqNode.ChildNodes)
			{
				if (recNode.NodeType != XmlNodeType.Element)
					continue;

				XmlElement recElem = recNode as XmlElement;
				IEnumerable<PhoneticPatternNode> nodes = null;
				switch (recElem.Name)
				{
					case "SimpleContext":
						nodes = LoadNatClassCtxtPSeq(recElem, alphaVars, varFeatIds);
						break;

					case "BoundaryMarker":
						nodes = LoadBdryCtxt(recElem);
						break;

					case "Segment":
						nodes = LoadSegCtxt(recElem);
						break;

					case "OptionalSegmentSequence":
						nodes = LoadOptSeq(recElem, alphaVars, varFeatIds, pattern.IsTarget);
						break;

					case "Segments":
						nodes = LoadSegCtxts(recElem);
						break;
				}

				int partition = -1;
				string id = recElem.GetAttribute("id");
				if (partIds != null && !string.IsNullOrEmpty(id))
					partition = partIds[id];
				pattern.AddPartition(nodes, partition);
			}
		}

		IEnumerable<PhoneticPatternNode> LoadNatClassCtxtPSeq(XmlElement ctxtNode, AlphaVariables alphaVars,
			Dictionary<string, string> varFeatIds)
		{
			yield return LoadNatClassCtxt(ctxtNode, alphaVars, varFeatIds);
		}

		NaturalClassContext LoadNatClassCtxt(XmlElement ctxtNode, AlphaVariables alphaVars,
			Dictionary<string, string> varFeatIds)
		{
			string classId = ctxtNode.GetAttribute("naturalClass");
			NaturalClass natClass = m_curMorpher.GetNaturalClass(classId);
			if (natClass == null)
				throw CreateUndefinedObjectException(string.Format(HCStrings.kstidUnknownNatClass, classId), classId);

			Dictionary<string, bool> vars = new Dictionary<string, bool>();
			XmlNodeList varsList = ctxtNode.SelectNodes("AlphaVariables/AlphaVariable");
			foreach (XmlNode varNode in varsList)
			{
				XmlElement varElem = varNode as XmlElement;

				string varStr = varFeatIds[varElem.GetAttribute("variableFeature")];
				vars[varStr] = varElem.GetAttribute("polarity") == "plus";
			}
			return new NaturalClassContext(natClass, vars, alphaVars);
		}

		IEnumerable<PhoneticPatternNode> LoadBdryCtxt(XmlElement bdryNode)
		{
			string strRep = m_repIds[bdryNode.GetAttribute("representation")];
			CharacterDefinitionTable charDefTable = GetCharDefTable(bdryNode.GetAttribute("characterTable"));
			BoundaryDefinition bdryDef = charDefTable.GetBoundaryDefinition(strRep);
			yield return new BoundaryContext(bdryDef);
		}

		IEnumerable<PhoneticPatternNode> LoadSegCtxt(XmlElement ctxtNode)
		{
			CharacterDefinitionTable charDefTable = GetCharDefTable(ctxtNode.GetAttribute("characterTable"));
			string strRep = m_repIds[ctxtNode.GetAttribute("representation")];
			SegmentDefinition segDef = charDefTable.GetSegmentDefinition(strRep);
			yield return new SegmentContext(segDef);
		}

		IEnumerable<PhoneticPatternNode> LoadSegCtxts(XmlElement ctxtsNode)
		{
			CharacterDefinitionTable charDefTable = GetCharDefTable(ctxtsNode.GetAttribute("characterTable"));
			string shapeStr = ctxtsNode.SelectSingleNode("PhoneticShape").InnerText;
			PhoneticShape shape;
			try
			{
				shape = charDefTable.ToPhoneticShape(shapeStr, ModeType.SYNTHESIS);
			}
			catch (MissingPhoneticShapeException mpse)
			{
				LoadException le = new LoadException(LoadException.LoadErrorType.INVALID_RULE_SHAPE, this,
					string.Format(HCStrings.kstidInvalidPseqShape, shapeStr, charDefTable.ID, mpse.Position + 1, shapeStr.Substring(mpse.Position)));
				le.Data["shape"] = shapeStr;
				le.Data["charDefTable"] = charDefTable.ID;
				throw le;
			}

			List<PhoneticPatternNode> nodes = new List<PhoneticPatternNode>();
			for (PhoneticShapeNode node = shape.Begin; node != shape.Last; node = node.Next)
			{
				switch (node.Type)
				{
					case PhoneticShapeNode.NodeType.SEGMENT:
						nodes.Add(new SegmentContext(node as Segment));
						break;

					case PhoneticShapeNode.NodeType.BOUNDARY:
						nodes.Add(new BoundaryContext(node as Boundary));
						break;
				}
			}
			return nodes;
		}

		IEnumerable<PhoneticPatternNode> LoadOptSeq(XmlElement optSeqNode, AlphaVariables varFeats,
			Dictionary<string, string> varFeatIds, bool isTarget)
		{
			string minStr = optSeqNode.GetAttribute("min");
			int min = string.IsNullOrEmpty(minStr) ? 0 : Convert.ToInt32(minStr);
			string maxStr = optSeqNode.GetAttribute("max");
			int max = string.IsNullOrEmpty(maxStr) ? -1 : Convert.ToInt32(maxStr);
			PhoneticPattern pattern = new PhoneticPattern(isTarget);
			LoadPSeq(pattern, optSeqNode, varFeats, varFeatIds, null);
			yield return new NestedPhoneticPattern(pattern, min, max);
		}

		PhoneticPattern LoadPTemp(XmlElement ptempNode, AlphaVariables varFeats, Dictionary<string, string> varFeatIds,
			Dictionary<string, int> partIds)
		{
			if (ptempNode == null)
				return null;

			bool initial = ptempNode.GetAttribute("initialBoundaryCondition") == "true";
			bool final = ptempNode.GetAttribute("finalBoundaryCondition") == "true";
			PhoneticPattern pattern = new PhoneticPattern();
			if (initial)
				pattern.Add(new MarginContext(Direction.LEFT));
			LoadPSeq(pattern, ptempNode.SelectSingleNode("PhoneticSequence") as XmlElement, varFeats,
				varFeatIds, partIds);
			if (final)
				pattern.Add(new MarginContext(Direction.RIGHT));

			return pattern;
		}

		CharacterDefinitionTable GetCharDefTable(string id)
		{
			CharacterDefinitionTable charDefTable = m_curMorpher.GetCharacterDefinitionTable(id);
			if (charDefTable == null)
				throw CreateUndefinedObjectException(string.Format(HCStrings.kstidUnknownCharDefTable, id), id);
			return charDefTable;
		}

		Stratum GetStratum(string id)
		{
			Stratum stratum = m_curMorpher.GetStratum(id);
			if (stratum == null)
				throw CreateUndefinedObjectException(string.Format(HCStrings.kstidUnknownStratum, id), id);
			return stratum;
		}

		void settings_ValidationEventHandler(object sender, ValidationEventArgs e)
		{
			throw new LoadException(LoadException.LoadErrorType.INVALID_FORMAT, this,
				e.Message + " Line: " + e.Exception.LineNumber + ", Pos: " + e.Exception.LinePosition);
		}
	}
}
