using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

namespace SIL.HermitCrab
{
	public class ConfigNode
	{
		public enum NodeType {COMMAND, OBJECT};

		public static ConfigNode ToConfigNode(object obj)
		{
			return obj as ConfigNode;
		}

		NodeType m_type;
		string m_name;
		Dictionary<string, object> m_parameters;
		LegacyLoader m_loader;

		public ConfigNode(NodeType type, string name, LegacyLoader loader)
		{
			m_type = type;
			m_name = name;
			m_parameters = new Dictionary<string, object>();
			m_loader = loader;
		}

		public NodeType Type
		{
			get
			{
				return m_type;
			}
		}

		public string Name
		{
			get
			{
				return m_name;
			}
		}

		public void Add(string key, object value)
		{
			m_parameters[key] = value;
		}

		public List<string> GetStringList(string key)
		{
			return Get<List<object>>(key).ConvertAll<string>(Convert.ToString);
		}

		public bool GetStringList(string key, out List<string> value)
		{
			List<object> objList;
			if (Get(key, out objList))
			{
				value = objList.ConvertAll(Convert.ToString);
				return true;
			}

			value = null;
			return false;
		}

		public List<ConfigNode> GetNodeList(string key)
		{
			return Get<List<object>>(key).ConvertAll<ConfigNode>(ToConfigNode);
		}

		public bool GetNodeList(string key, out List<ConfigNode> value)
		{
			List<object> objList;
			if (Get(key, out objList))
			{
				value = objList.ConvertAll(ToConfigNode);
				return true;
			}

			value = null;
			return false;
		}

		public T Get<T>(string key)
		{
			T value;
			if (!Get(key, out value))
				throw new LoadException(LoadException.LoadErrorType.INVALID_FORMAT, m_loader,
					string.Format(HCStrings.kstidFieldNotDefined, key, m_name, (m_type == NodeType.COMMAND ? "command" : "object")));
			return value;
		}

		public bool Get<T>(string key, out T value)
		{
			object valObj;
			if (m_parameters.TryGetValue(key, out valObj))
			{
				if (!(valObj is T))
				{
					throw new LoadException(LoadException.LoadErrorType.INVALID_FORMAT, m_loader,
						string.Format(HCStrings.kstidInvalidField, key, m_name, (m_type == NodeType.COMMAND ? "command" : "object")));
				}
				value = (T) valObj;
				return true;
			}

			value = default(T);
			return false;
		}
	}

	/// <summary>
	/// This class parses the legacy HC input format in to a set of <see cref="ConfigNode"/> objects.
	/// </summary>
	public class LegacyParser
	{
		Regex m_spaceRegex;
		LegacyLoader m_loader;

		public LegacyParser(LegacyLoader loader)
		{
			m_spaceRegex = new Regex("\\s+");
			m_loader = loader;
		}

		public ICollection<ConfigNode> Parse(string configFile)
		{
			var nodes = new List<ConfigNode>();
			StreamReader r = null;
			try
			{
				r = new StreamReader(new FileStream(configFile, FileMode.Open, FileAccess.Read), Encoding.GetEncoding(1252));

				var parens = new[] { '(', ')', '<', '>' };
				var sb = new StringBuilder();
				int count = 0;
				char open = '(';
				char close = ')';
				while (!r.EndOfStream)
				{
					string line = r.ReadLine().Trim();

					if (string.IsNullOrEmpty(line) || line.StartsWith(";"))
						continue;

					int startIndex = 0;

					int index = 0;
					while ((index = line.IndexOfAny(parens, index)) != -1)
					{
						if (count == 0 && (line[index] == '(' || line[index] == '<'))
						{
							if (line[index] == '(')
							{
								open = '(';
								close = ')';
							}
							else
							{
								open = '<';
								close = '>';
							}
							startIndex = index + 1;
						}

						if (line[index] == open)
						{
							count++;
						}
						else if (line[index] == close)
						{
							count--;

							if (count == 0)
							{
								sb.Append(line.Substring(startIndex, index - startIndex));

								string str = m_spaceRegex.Replace(sb.ToString(), " ").Trim();
								if (open == '(')
									nodes.Add(ParseCommand(str));
								else
									nodes.Add(ParseObject(str));
								sb = new StringBuilder();
							}
						}
						index++;
					}

					if (count > 0)
					{
						sb.Append(line.Substring(startIndex, line.Length - startIndex));
						sb.Append('\n');
					}

				}
			}
			finally
			{
				if (r != null)
					r.Close();
			}

			return nodes;
		}

		ConfigNode ParseCommand(string cmdStr)
		{
			int index = cmdStr.IndexOf(' ');
			string name = cmdStr.Substring(0, index);
			index++;
			var cmd = new ConfigNode(ConfigNode.NodeType.COMMAND, name, m_loader);

			bool message = false;
			bool prettyPrint = false;
			if (cmdStr.IndexOf("message") == index)
			{
				message = true;
				index += 8;
			}
			else if (cmdStr.IndexOf("pretty_print") == index)
			{
				prettyPrint = true;
				index += 13;
			}

			cmd.Add("message", message);
			cmd.Add("pretty_print", prettyPrint);
			cmd.Add("param", ParseParameter(cmdStr, index, out index)); ;

			return cmd;
		}

		ConfigNode ParseObject(string objStr)
		{
			int begin = objStr.IndexOf(' ');
			string name = objStr.Substring(0, begin);
			begin++;
			var obj = new ConfigNode(ConfigNode.NodeType.OBJECT, name, m_loader);

			int end;
			while (begin < objStr.Length && (end = objStr.IndexOf(' ', begin)) != -1)
			{
				string key = objStr.Substring(begin, end - begin);
				obj.Add(key, ParseParameter(objStr, end + 1, out begin));
				begin++;
			}

			return obj;
		}

		List<object> ParseList(string listStr)
		{
			List<object> list = new List<object>();
			int index = 0;
			while (index < listStr.Length)
			{
				list.Add(ParseParameter(listStr, index, out index));
				if (index < listStr.Length && listStr[index] == ' ')
					index++;
			}

			return list;
		}

		int GetCloseIndex(string str, int index, char open, char close)
		{
			char[] delims = new char[2];
			delims[0] = open;
			delims[1] = close;

			int count = 0;
			while ((index = str.IndexOfAny(delims, index)) != -1)
			{
				if (str[index] == open)
				{
					count++;
				}
				else if (str[index] == close)
				{
					count--;

					if (count == 0)
					{
						return index;
					}
				}
				index++;
			}

			return -1;
		}

		object ParseParameter(string paramStr, int index, out int outIndex)
		{
			switch (paramStr[index])
			{
				case '<':
					{
						int closeIndex = GetCloseIndex(paramStr, index, '<', '>');
						if (closeIndex >= 0)
						{
							outIndex = closeIndex + 1;
							return ParseObject(paramStr.Substring(index + 1, closeIndex - (index + 1)).Trim());
						}
						break;
					}

				case '(':
					{
						int closeIndex = GetCloseIndex(paramStr, index, '(', ')');
						if (closeIndex >= 0)
						{
							outIndex = closeIndex + 1;
							return ParseList(paramStr.Substring(index + 1, closeIndex - (index + 1)).Trim());
						}
						break;
					}

				case '\x1F':
				case '\'':
					{
						int closeIndex = paramStr.IndexOf(paramStr[index], index + 1);
						if (closeIndex >= 0)
						{
							outIndex = closeIndex + 1;
							return paramStr.Substring(index + 1, closeIndex - (index + 1));
						}
						break;
					}
			}

			outIndex = paramStr.IndexOf(' ', index);
			if (outIndex == -1)
				outIndex = paramStr.Length;
			return paramStr.Substring(index, outIndex - index);
		}
	}

	/// <summary>
	/// This class represents the loader for the legacy HC input format.
	/// </summary>
	public class LegacyLoader : Loader
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

				case "rl_iterative":
					return PhonologicalRule.MultAppOrder.RL_ITERATIVE;

				case "lr_iterative":
					return PhonologicalRule.MultAppOrder.LR_ITERATIVE;
			}

			return PhonologicalRule.MultAppOrder.LR_ITERATIVE;
		}

		readonly LegacyParser m_parser;
		string m_rootPath;

		public LegacyLoader()
		{
			m_parser = new LegacyParser(this);
		}

		public override Encoding DefaultOutputEncoding
		{
			get
			{
				return Encoding.GetEncoding(1252);
			}
		}

		public override void Load()
		{
			throw new NotImplementedException();
		}

		public override void Load(string configFile)
		{
			Reset();
			m_rootPath = Path.GetDirectoryName(configFile);
			LoadConfigNodes(m_parser.Parse(configFile));
			m_isLoaded = true;
		}

		public override void Reset()
		{
			base.Reset();
			m_rootPath = null;
		}

		void LoadConfigNodes(ICollection<ConfigNode> nodes)
		{
			foreach (ConfigNode node in nodes)
			{
				try
				{
					switch (node.Type)
					{
						case ConfigNode.NodeType.COMMAND:
							LoadCommand(node);
							break;

						case ConfigNode.NodeType.OBJECT:
							LoadObject(node);
							break;
					}
				}
				catch (MorphException me)
				{
					if (m_output != null)
						m_output.Write(me);
					if (m_quitOnError)
						throw;
				}
				catch (LoadException le)
				{
					if (m_output != null)
						m_output.Write(le);
					if (m_quitOnError)
						throw;
				}
			}
		}

		void LoadCommand(ConfigNode cmd)
		{
			bool message = cmd.Get<bool>("message");

			switch (cmd.Name)
			{
				case "open_language":
					string language = cmd.Get<string>("param");
					m_curMorpher = new Morpher(language, language);
					m_morphers.Add(m_curMorpher);
					break;

				case "morpher_set":
					List<object> list = cmd.Get<List<object>>("param");
					switch (list[0] as string)
					{
						case "*pfeatures*":
							LoadFeatureSystem(list[1] as List<object>);
							break;

						case "*strata*":
							CheckCurMorpher();
							m_curMorpher.ClearStrata();
							List<string> strataList = (list[1] as List<object>).ConvertAll<string>(Convert.ToString);
							foreach (string stratumName in strataList)
								m_curMorpher.AddStratum(new Stratum(stratumName, stratumName, m_curMorpher));
							m_curMorpher.AddStratum(new Stratum(Stratum.SURFACE_STRATUM_ID, Stratum.SURFACE_STRATUM_ID, m_curMorpher));
							break;

						case "*quit_on_error*":
							m_quitOnError = (list[1] as string) == "true";
							break;

						case "*del_re_apps*":
							CheckCurMorpher();
							m_curMorpher.DelReapplications = Convert.ToInt32(list[1] as string);
							break;

						case "*trace_inputs*":
							m_traceInputs = (list[1] as string) == "true";
							break;
					}
					break;

				case "set_stratum":
					SetStratumSetting(cmd.Get<ConfigNode>("param"));
					break;

				case "load_char_def_table":
				case "load_nat_class":
				case "load_morpher_rule":
					LoadObject(cmd.Get<ConfigNode>("param"));
					break;

				case "merge_in_dictionary_file":
				case "morpher_input_from_file":
					string path = cmd.Get<string>("param");
					if (!Path.IsPathRooted(path))
					{
						path = Path.Combine(m_rootPath, path);
					}
					ICollection<ConfigNode> newNodes = m_parser.Parse(path);
					LoadConfigNodes(newNodes);
					break;

				case "morph_and_lookup_word":
					string word = cmd.Get<string>("param");
					bool prettyPrint = cmd.Get<bool>("pretty_print");
					CheckCurMorpher();
					MorphAndLookupWord(word, prettyPrint);
					break;

				case "remove_morpher_rule":
					CheckCurMorpher();
					string name = cmd.Get<string>("param");
					m_curMorpher.RemovePhonologicalRule(name);
					m_curMorpher.RemoveMorphologicalRule(name);
					foreach (Stratum stratum in m_curMorpher.Strata)
					{
						stratum.RemovePhonologicalRule(name);
						stratum.RemoveMorphologicalRule(name);
					}
					break;

				case "remove_nat_class":
					CheckCurMorpher();
					m_curMorpher.RemoveNaturalClass(cmd.Get<string>("param"));
					break;

				case "del_char_def_table":
					CheckCurMorpher();
					m_curMorpher.RemoveCharacterDefinitionTable(cmd.Get<string>("param"));
					break;

				case "close_language":
					CheckCurMorpher();
					m_morphers.Remove(m_curMorpher.ID);
					m_curMorpher = null;
					break;

				case "assign_default_morpher_feature_value":
					CheckCurMorpher();
					List<object> feat = cmd.Get<List<object>>("param");
					string featName = feat[0] as string;
					Feature feature = m_curMorpher.HeadFeatureSystem.GetFeature(featName);
					if (feature == null)
					{
						feature = m_curMorpher.FootFeatureSystem.GetFeature(featName);
						if (feature == null)
						{
							feature = new Feature(featName, featName, m_curMorpher);
							m_curMorpher.HeadFeatureSystem.AddFeature(feature);
							m_curMorpher.FootFeatureSystem.AddFeature(feature);
						}
					}

					List<string> values = ((List<object>) feat[1]).ConvertAll(Convert.ToString);
					HCObjectSet<FeatureValue> featVals = new HCObjectSet<FeatureValue>();
					foreach (string value in values)
					{
						FeatureValue val = new FeatureValue(value, value, m_curMorpher);
						feature.AddPossibleValue(val);
						featVals.Add(val);
					}
					feature.DefaultValue = new ClosedValueInstance(featVals);
					break;

				case "trace_morpher_rule":
					CheckCurMorpher();
					List<string> traceRuleParams = cmd.GetStringList("param");
					bool traceAnalysis = traceRuleParams[0] == "true";
					bool traceSynthesis = traceRuleParams[1] == "true";
					if (traceRuleParams.Count == 3)
						Output.TraceManager.SetTraceRule(traceRuleParams[2], traceAnalysis, traceSynthesis);
					else
						Output.TraceManager.SetTraceRules(traceAnalysis, traceSynthesis);
					break;

				case "trace_morpher_strata":
					CheckCurMorpher();
					List<string> traceStrataParams = cmd.GetStringList("param");
					Output.TraceManager.TraceStrataAnalysis = traceStrataParams[0] == "true";
					Output.TraceManager.TraceStrataSynthesis = traceStrataParams[1] == "true";
					break;

				case "trace_morpher_templates":
					CheckCurMorpher();
					List<string> traceTemplatesParams = cmd.GetStringList("param");
					Output.TraceManager.TraceTemplatesAnalysis = traceTemplatesParams[0] == "true";
					Output.TraceManager.TraceTemplatesSynthesis = traceTemplatesParams[1] == "true";
					break;

				case "trace_lexical_lookup":
					CheckCurMorpher();
					bool traceLexLookup = false;
					string traceLexLookupStr;
					if (cmd.Get("param", out traceLexLookupStr))
						traceLexLookup = traceLexLookupStr == "true";
					Output.TraceManager.TraceLexLookup = traceLexLookup;
					break;

				case "trace_blocking":
					CheckCurMorpher();
					bool traceBlocking = false;
					string traceBlockingStr;
					if (cmd.Get("param", out traceBlockingStr))
						traceBlocking = traceBlockingStr == "true";
					Output.TraceManager.TraceBlocking = traceBlocking;
					break;
			}
		}

		void LoadObject(ConfigNode obj)
		{
			switch (obj.Name)
			{
				case "char_table":
					LoadCharDefTable(obj);
					break;

				case "nat_class":
					LoadNaturalClass(obj);
					break;

				case "prule":
					LoadPRule(obj);
					break;

				case "mrule":
					LoadMRule(obj);
					break;

				case "rz_rule":
					LoadRealRule(obj);
					break;

				case "comp_rule":
					LoadCompRule(obj);
					break;

				case "lex":
					LoadLexEntry(obj);
					break;
			}
		}

		void LoadFeatureSystem(List<object> featureList)
		{
			CheckCurMorpher();
			m_curMorpher.PhoneticFeatureSystem.Reset();
			IEnumerator<object> enumerator = featureList.GetEnumerator();
			while (enumerator.MoveNext())
			{
				string featureName = enumerator.Current as string;
				Feature feature = new Feature(featureName, featureName, m_curMorpher);
				enumerator.MoveNext();
				foreach (object valueObj in (List<object>) enumerator.Current)
				{
					string valueName = valueObj as string;
					FeatureValue value = new FeatureValue(featureName + valueName, valueName, m_curMorpher);
					feature.AddPossibleValue(value);
					try
					{
						m_curMorpher.PhoneticFeatureSystem.AddValue(value);
					}
					catch (InvalidOperationException ioe)
					{
						throw new LoadException(LoadException.LoadErrorType.TOO_MANY_FEATURE_VALUES, this,
							HCStrings.kstidTooManyFeatValues, ioe);
					}
				}
				m_curMorpher.PhoneticFeatureSystem.AddFeature(feature);
			}
		}

		void LoadCharDefTable(ConfigNode charDefTableSpec)
		{
			CheckCurMorpher();
			string name = charDefTableSpec.Get<string>("name");
			CharacterDefinitionTable charDefTable = m_curMorpher.GetCharacterDefinitionTable(name);
			if (charDefTable == null)
			{
#if IPA_CHAR_DEF_TABLE
				if (name == "*ipa*")
					charDefTable = new IPACharacterDefinitionTable(name, name, m_curMorpher);
				else
					charDefTable = new CharacterDefinitionTable(name, name, m_curMorpher);
#else
				charDefTable = new CharacterDefinitionTable(name, name, m_curMorpher);
#endif
				m_curMorpher.AddCharacterDefinitionTable(charDefTable);
			}

			charDefTable.Reset();
			charDefTable.Encoding = charDefTableSpec.Get<string>("encoding");

			List<object> segDefs = charDefTableSpec.Get<List<object>>("seg_defs");
			foreach (object obj in segDefs)
			{
				List<object> segDef = obj as List<object>;
				string strRep = segDef[0] as string;
				charDefTable.AddSegmentDefinition(strRep, LoadFeatValues((segDef[1] as List<object>)));
			}

			List<string> bdryDefs;
			if (charDefTableSpec.GetStringList("bdry_defs", out bdryDefs))
			{
				foreach (string def in bdryDefs)
				{
					charDefTable.AddBoundaryDefinition(def);
				}
			}
		}

		void SetStratumSetting(ConfigNode stratumSetting)
		{
			CheckCurMorpher();
			string stratumName = stratumSetting.Get<string>("nm");
			if (stratumName == "*surface*")
				stratumName = Stratum.SURFACE_STRATUM_ID;
			Stratum stratum = m_curMorpher.GetStratum(stratumName);
			if (stratum == null)
				throw CreateUndefinedObjectException(string.Format(HCStrings.kstidUnknownStratum, stratumName), stratumName);
			switch (stratumSetting.Get<string>("type"))
			{
				case "ctable":
					string tableName = stratumSetting.Get<string>("value");
					stratum.CharacterDefinitionTable = GetCharDefTable(tableName);
					break;

				case "cyclicity":
					string cyclic = stratumSetting.Get<string>("value");
					stratum.IsCyclic = cyclic == "cyclic";
					break;

				case "prule":
					string pruleOrder = stratumSetting.Get<string>("value");
					stratum.PhonologicalRuleOrder = GetPRuleOrder(pruleOrder);
					break;

				case "mrule":
					string mruleOrder = stratumSetting.Get<string>("value");
					stratum.MorphologicalRuleOrder = GetMRuleOrder(mruleOrder);
					break;

				case "templates":
					List<ConfigNode> tempSpecs = stratumSetting.GetNodeList("value");
					foreach (AffixTemplate template in stratum.AffixTemplates)
						m_curMorpher.RemoveAffixTemplate(template.ID);
					stratum.ClearAffixTemplates();
					foreach (ConfigNode tempSpec in tempSpecs)
					{
						AffixTemplate template = LoadAffixTemplate(tempSpec);
						stratum.AddAffixTemplate(template);
						m_curMorpher.AddAffixTemplate(template);
					}
					break;

			}
		}

		ICollection<FeatureValue> LoadFeatValues(List<object> mappingList)
		{
			List<FeatureValue> featVals = new List<FeatureValue>();
			IEnumerator<object> enumerator = mappingList.GetEnumerator();
			while (enumerator.MoveNext())
			{
				string featName = enumerator.Current as string;
				Feature feature = m_curMorpher.PhoneticFeatureSystem.GetFeature(featName);
				if (feature == null)
					throw CreateUndefinedObjectException(string.Format(HCStrings.kstidUnknownFeat, featName), featName);
				enumerator.MoveNext();
				string valueName = enumerator.Current as string;
				FeatureValue value = feature.GetPossibleValue(featName + valueName);
				if (value == null)
					throw CreateUndefinedObjectException(string.Format(HCStrings.kstidUnknownFeatValue, valueName, featName), valueName);
				featVals.Add(value);
			}

			return featVals;
		}

		AlphaVariables LoadVarFeats(List<object> mappingList)
		{
			IDictionary<string, Feature> varFeats = new Dictionary<string, Feature>();
			if (mappingList != null)
			{
				IEnumerator<object> enumerator = mappingList.GetEnumerator();
				while (enumerator.MoveNext())
				{
					string varName = enumerator.Current as string;
					enumerator.MoveNext();
					string featId = enumerator.Current as string;
					Feature feature = m_curMorpher.PhoneticFeatureSystem.GetFeature(featId);
					if (feature == null)
						throw CreateUndefinedObjectException(string.Format(HCStrings.kstidUnknownFeat, featId), featId);
					varFeats[varName] = feature;
				}
			}

			return new AlphaVariables(varFeats);
		}

		void LoadNaturalClass(ConfigNode natClassSpec)
		{
			CheckCurMorpher();
			string name = natClassSpec.Get<string>("name");
			NaturalClass natClass = m_curMorpher.GetNaturalClass(name);
			if (natClass == null)
			{
				natClass = new NaturalClass(name, name, m_curMorpher);
				m_curMorpher.AddNaturalClass(natClass);
			}

			ICollection<FeatureValue> featVals = LoadFeatValues(natClassSpec.Get<List<object>>("features"));
			natClass.Features = new FeatureBundle(featVals, m_curMorpher.PhoneticFeatureSystem);
		}

		void LoadPRule(ConfigNode pruleSpec)
		{
			CheckCurMorpher();
			string name = pruleSpec.Get<string>("nm");
			StandardPhonologicalRule rule;
			PhonologicalRule prule = m_curMorpher.GetPhonologicalRule(name);
			if (prule != null)
			{
				rule = prule as StandardPhonologicalRule;
			}
			else
			{
				rule = new StandardPhonologicalRule(name, name, m_curMorpher);
				m_curMorpher.AddPhonologicalRule(rule);
			}
			rule.Reset();

			string multAppOrderStr;
			if (pruleSpec.Get("mult_applic", out multAppOrderStr))
				rule.MultApplication = GetMultAppOrder(multAppOrderStr);

			List<object> varFeatsList;
			pruleSpec.Get("var_fs", out varFeatsList);
			rule.AlphaVariables = LoadVarFeats(varFeatsList);

			rule.LHS = new PhoneticPattern(true);
			LoadPSeq(rule.LHS, pruleSpec.GetNodeList("in_pseq"), rule.AlphaVariables);

			List<ConfigNode> subruleList = pruleSpec.GetNodeList("subrules");
			foreach (ConfigNode srSpec in subruleList)
				LoadPSubrule(srSpec, rule);

			List<string> strata = pruleSpec.GetStringList("str");
			foreach (Stratum stratum in m_curMorpher.Strata)
			{
				if (strata.Contains(stratum.ID))
					stratum.AddPhonologicalRule(rule);
				else
					stratum.RemovePhonologicalRule(name);
			}
		}

		void LoadPSubrule(ConfigNode psubruleSpec, StandardPhonologicalRule rule)
		{
			PhoneticPattern outSeq = new PhoneticPattern(true);
			LoadPSeq(outSeq, psubruleSpec.GetNodeList("out_pseq"), rule.AlphaVariables);

			PhoneticPattern leftEnv = null;
			ConfigNode leftEnvSpec;
			if (psubruleSpec.Get("left_environ", out leftEnvSpec))
				leftEnv = LoadPTemp(leftEnvSpec, rule.AlphaVariables);

			PhoneticPattern rightEnv = null;
			ConfigNode rightEnvSpec;
			if (psubruleSpec.Get("right_environ", out rightEnvSpec))
				rightEnv = LoadPTemp(rightEnvSpec, rule.AlphaVariables);

			StandardPhonologicalRule.Subrule sr;
			try
			{
				sr = new StandardPhonologicalRule.Subrule(outSeq, new Environment(leftEnv, rightEnv), rule);
			}
			catch (ArgumentException ae)
			{
				LoadException le = new LoadException(LoadException.LoadErrorType.INVALID_SUBRULE_TYPE, this,
					HCStrings.kstidInvalidSubruleType, ae);
				le.Data["rule"] = rule.ID;
				throw le;
			}

			List<object> reqPOSs;
			if (!psubruleSpec.Get("r_pos", out reqPOSs))
			{
				reqPOSs = new List<object>();
			}
			sr.RequiredPOSs = reqPOSs.ConvertAll<PartOfSpeech>(ToPOS);

			List<object> exFeats;
			if (!psubruleSpec.Get("x_rf", out exFeats))
			{
				exFeats = new List<object>();
			}
			sr.ExcludedMPRFeatures = LoadMPRFeatures(exFeats);

			List<object> reqFeats;
			if (!psubruleSpec.Get("r_rf", out reqFeats))
			{
				reqFeats = new List<object>();
			}
			sr.RequiredMPRFeatures = LoadMPRFeatures(reqFeats);

			rule.AddSubrule(sr);
		}

		PhoneticPattern LoadPTemp(ConfigNode ptempSpec, AlphaVariables varFeats)
		{
			bool initial = false;
			string initStr;
			if (ptempSpec.Get("init", out initStr))
				initial = initStr == "true";

			bool final = false;
			string finStr;
			if (ptempSpec.Get("fin", out finStr))
				final = finStr == "true";

			var pattern = new PhoneticPattern();
			if (initial)
				pattern.Add(new MarginContext(Direction.LEFT));
			LoadPSeq(pattern, ptempSpec.GetNodeList("pseq"), varFeats);
			if (final)
				pattern.Add(new MarginContext(Direction.RIGHT));
			return pattern;
		}

		SimpleContext LoadNatClassCtxt(ConfigNode ctxtSpec, AlphaVariables varFeats)
		{
			string classStr = ctxtSpec.Get<string>("class");
			NaturalClass natClass = m_curMorpher.GetNaturalClass(classStr);
			if (natClass == null)
				throw CreateUndefinedObjectException(string.Format(HCStrings.kstidUnknownNatClass, classStr), classStr);

			Dictionary<string, bool> vars = new Dictionary<string, bool>();
			List<string> varsList;
			if (ctxtSpec.GetStringList("alpha_vars", out varsList))
			{
				IEnumerator<string> enumerator = varsList.GetEnumerator();
				while (enumerator.MoveNext())
				{
					string key = enumerator.Current;
					enumerator.MoveNext();
					bool polarity = enumerator.Current == "+";
					vars.Add(key, polarity);
				}
			}
			return new NaturalClassContext(natClass, vars, varFeats);
		}

		void LoadLexEntry(ConfigNode entrySpec)
		{
			CheckCurMorpher();
			string id = entrySpec.Get<string>("id");
			string shapeStr = entrySpec.Get<string>("sh");
			LexEntry entry = new LexEntry(id, shapeStr, m_curMorpher);

			string glossStr;
			if (entrySpec.Get("gl", out glossStr))
				entry.Gloss = new Gloss(glossStr, glossStr, m_curMorpher);
			entry.POS = ToPOS(entrySpec.Get<string>("pos"));
			string stratumName = entrySpec.Get<string>("str");
			Stratum stratum = m_curMorpher.GetStratum(stratumName);
			if (stratum == null)
				throw CreateUndefinedObjectException(string.Format(HCStrings.kstidUnknownLexEntryStratum, id, stratumName), stratumName);
			entry.Stratum = stratum;
			PhoneticShape pshape;
			try
			{
				pshape = stratum.CharacterDefinitionTable.ToPhoneticShape(shapeStr, ModeType.SYNTHESIS);
			}
			catch (MissingPhoneticShapeException mpse)
			{
				LoadException le = new LoadException(LoadException.LoadErrorType.INVALID_ENTRY_SHAPE, this,
					string.Format(HCStrings.kstidInvalidLexEntryShape, shapeStr, id, stratum.CharacterDefinitionTable.ID, mpse.Position + 1, shapeStr.Substring(mpse.Position)));
				le.Data["shape"] = shapeStr;
				le.Data["charDefTable"] = stratum.CharacterDefinitionTable.ID;
				le.Data["entry"] = entry.ID;
				throw le;
			}

			if (pshape.IsAllBoundaries)
			{
				LoadException le = new LoadException(LoadException.LoadErrorType.INVALID_ENTRY_SHAPE, this,
					string.Format(HCStrings.kstidInvalidLexEntryShapeAllBoundaries, shapeStr, id, stratum.CharacterDefinitionTable.ID));
				le.Data["shape"] = shapeStr;
				le.Data["charDefTable"] = stratum.CharacterDefinitionTable.ID;
				le.Data["entry"] = entry.ID;
				throw le;
			}
			LexEntry.RootAllomorph allomorph = new LexEntry.RootAllomorph(id + "_allo", shapeStr, m_curMorpher, pshape);
			entry.AddAllomorph(allomorph);
			m_curMorpher.AddAllomorph(allomorph);
			List<object> mprFeats;
			if (!entrySpec.Get("rf", out mprFeats))
			{
				mprFeats = new List<object>();
			}
			entry.MPRFeatures = LoadMPRFeatures(mprFeats);

			FeatureValues headFeats;
			List<object> headFeatsList;
			if (entrySpec.Get("hf", out headFeatsList))
				headFeats = LoadSynFeats(headFeatsList, m_curMorpher.HeadFeatureSystem);
			else
				headFeats = new FeatureValues();
			entry.HeadFeatures = headFeats;

			FeatureValues footFeats;
			List<object> footFeatsList;
			if (entrySpec.Get("ff", out footFeatsList))
				footFeats = LoadSynFeats(footFeatsList, m_curMorpher.FootFeatureSystem);
			else
				footFeats = new FeatureValues();
			entry.FootFeatures = footFeats;

			stratum.AddEntry(entry);
			string familyStr;
			if (entrySpec.Get("fam", out familyStr))
			{
				LexFamily family = m_curMorpher.Lexicon.GetFamily(familyStr);
				if (family == null)
				{
					family = new LexFamily(familyStr, familyStr, m_curMorpher);
					m_curMorpher.Lexicon.AddFamily(family);
				}
				family.AddEntry(entry);
			}
			m_curMorpher.Lexicon.AddEntry(entry);
		}

		MPRFeatureSet LoadMPRFeatures(List<object> mprFeatsList)
		{
			MPRFeatureSet mprFeats = new MPRFeatureSet();
			foreach (object mprFeatObj in mprFeatsList)
				mprFeats.Add(ToMPRFeature(mprFeatObj));
			return mprFeats;
		}

		FeatureValues LoadSynFeats(List<object> featsList, FeatureSystem featSys)
		{
			FeatureValues fv = new FeatureValues();
			IEnumerator<object> enumerator = featsList.GetEnumerator();
			while (enumerator.MoveNext())
			{
				string featureName = enumerator.Current as string;
				Feature feature = featSys.GetFeature(featureName);
				if (feature == null)
				{
					feature = new Feature(featureName, featureName, m_curMorpher);
					featSys.AddFeature(feature);
				}

				enumerator.MoveNext();
				List<string> values = ((List<object>) enumerator.Current).ConvertAll(Convert.ToString);
				HCObjectSet<FeatureValue> featVals = new HCObjectSet<FeatureValue>();
				foreach (string valueName in values)
				{
					string valueId = featureName + valueName;
					FeatureValue value = featSys.GetValue(valueId);
					if (value == null)
					{
						value = new FeatureValue(valueId, valueName, m_curMorpher);
						try
						{
							featSys.AddValue(value);
						}
						catch (InvalidOperationException ioe)
						{
							throw new LoadException(LoadException.LoadErrorType.TOO_MANY_FEATURE_VALUES, this,
								HCStrings.kstidTooManyFeatValues, ioe);
						}
					}

					if (value.Feature != feature)
					{
						if (value.Feature != null)
							value.Feature.RemovePossibleValue(valueId);
						feature.AddPossibleValue(value);
					}

					featVals.Add(value);
				}
				fv.Add(feature, new ClosedValueInstance(featVals));
			}
			return fv;
		}

		void LoadPSeq(PhoneticPattern pattern, List<ConfigNode> seqList, AlphaVariables varFeats)
		{
			foreach (ConfigNode spec in seqList)
			{
				switch (spec.Name)
				{
					case "simp_cntxt":
						pattern.Add(LoadNatClassCtxt(spec, varFeats));
						break;

					case "bdry":
						pattern.Add(LoadBdryCtxt(spec));
						break;

					case "opt_seq":
						pattern.Add(LoadOptSeq(spec, varFeats, pattern.IsTarget));
						break;

					case "seg":
						pattern.Add(LoadSegCtxt(spec));
						break;
				}
			}
		}

		SimpleContext LoadSegCtxt(ConfigNode segSpec)
		{
			string strRep = segSpec.Get<string>("rep");
			string ctableName = segSpec.Get<string>("ctable");
			CharacterDefinitionTable charDefTable = GetCharDefTable(ctableName);
			SegmentDefinition segDef = charDefTable.GetSegmentDefinition(strRep);
			if (segDef == null)
				throw CreateUndefinedObjectException(string.Format(HCStrings.kstidInvalidRuleSeg, strRep, ctableName), strRep);
			return new SegmentContext(segDef);
		}

		BoundaryContext LoadBdryCtxt(ConfigNode bdrySpec)
		{
			string strRep = bdrySpec.Get<string>("rep");
			string ctableName = bdrySpec.Get<string>("ctable");
			CharacterDefinitionTable charDefTable = GetCharDefTable(ctableName);
			BoundaryDefinition bdryDef = charDefTable.GetBoundaryDefinition(strRep);
			return new BoundaryContext(bdryDef);
		}

		NestedPhoneticPattern LoadOptSeq(ConfigNode optSeqSpec, AlphaVariables varFeats, bool isTarget)
		{
			string minStr = optSeqSpec.Get<string>("min");
			string maxStr = optSeqSpec.Get<string>("max");
			int min = Convert.ToInt32(minStr);
			int max = Convert.ToInt32(maxStr);

			PhoneticPattern pattern = new PhoneticPattern(isTarget);
			LoadPSeq(pattern, optSeqSpec.GetNodeList("seq"), varFeats);
			return new NestedPhoneticPattern(pattern, min, max);
		}

		void LoadMRule(ConfigNode mruleSpec)
		{
			CheckCurMorpher();
			string name = mruleSpec.Get<string>("nm");
			AffixalMorphologicalRule rule = null;
			MorphologicalRule mrule = m_curMorpher.GetMorphologicalRule(name);
			if (mrule != null)
			{
				rule = mrule as AffixalMorphologicalRule;
				foreach (AffixalMorphologicalRule.Subrule sr in rule.Subrules)
					m_curMorpher.RemoveAllomorph(sr.ID);
			}
			else
			{
				rule = new AffixalMorphologicalRule(name, name, m_curMorpher);
				m_curMorpher.AddMorphologicalRule(rule);
			}
			rule.Reset();

			string glossStr;
			if (mruleSpec.Get("gl", out glossStr))
				rule.Gloss = new Gloss(glossStr, glossStr, m_curMorpher);

			List<object> reqPOSs;
			if (!mruleSpec.Get("r_pos", out reqPOSs))
			{
				reqPOSs = new List<object>();
			}
			rule.RequiredPOSs = reqPOSs.ConvertAll(ToPOS);

			PartOfSpeech outPOS = null;
			string outPOSStr;
			if (mruleSpec.Get("pos", out outPOSStr))
				outPOS = ToPOS(outPOSStr);
			rule.OutPOS = outPOS;

			List<object> reqHeadFeatsList;
			if (mruleSpec.Get("r_hf", out reqHeadFeatsList))
				rule.RequiredHeadFeatures = LoadSynFeats(reqHeadFeatsList, m_curMorpher.HeadFeatureSystem);
			else
				rule.RequiredHeadFeatures = new FeatureValues();

			List<object> reqFootFeatsList;
			if (mruleSpec.Get("r_ff", out reqFootFeatsList))
				rule.RequiredFootFeatures = LoadSynFeats(reqFootFeatsList, m_curMorpher.FootFeatureSystem);
			else
				rule.RequiredFootFeatures = new FeatureValues();

			List<object> outHeadFeatsList;
			if (mruleSpec.Get("hf", out outHeadFeatsList))
				rule.OutHeadFeatures = LoadSynFeats(outHeadFeatsList, m_curMorpher.HeadFeatureSystem);
			else
				rule.OutHeadFeatures = new FeatureValues();

			List<object> outFootFeatsList;
			if (mruleSpec.Get("ff", out outFootFeatsList))
				rule.OutFootFeatures = LoadSynFeats(outFootFeatsList, m_curMorpher.FootFeatureSystem);
			else
				rule.OutFootFeatures = new FeatureValues();

			List<string> obligFeatsList;
			HCObjectSet<Feature> obligFeats = new HCObjectSet<Feature>();
			if (mruleSpec.GetStringList("of", out obligFeatsList))
			{
				foreach (string obligFeat in obligFeatsList)
				{
					Feature feature = m_curMorpher.HeadFeatureSystem.GetFeature(obligFeat);
					if (feature == null)
					{
						feature = new Feature(obligFeat, obligFeat, m_curMorpher);
						m_curMorpher.HeadFeatureSystem.AddFeature(feature);
					}
					obligFeats.Add(feature);
				}
			}
			rule.ObligatoryHeadFeatures = obligFeats;

			bool blockable = true;
			string blockableStr;
			if (mruleSpec.Get("blockable", out blockableStr))
				blockable = blockableStr == "true";
			rule.IsBlockable = blockable;

			List<ConfigNode> subruleList = mruleSpec.GetNodeList("subrules");
			foreach (ConfigNode srSpec in subruleList)
				LoadMSubrule(srSpec, rule);

			string stratumName;
			if (!mruleSpec.Get("stratum", out stratumName))
			{
				stratumName = mruleSpec.Get<string>("str");
			}
			foreach (Stratum stratum in m_curMorpher.Strata)
			{
				if (stratumName == stratum.ID)
					stratum.AddMorphologicalRule(rule);
				else
					stratum.RemoveMorphologicalRule(name);
			}
		}

		void LoadRealRule(ConfigNode realRuleSpec)
		{
			CheckCurMorpher();
			string name = realRuleSpec.Get<string>("nm");
			RealizationalRule rule;
			MorphologicalRule mrule = m_curMorpher.GetMorphologicalRule(name);
			if (mrule != null)
			{
				rule = mrule as RealizationalRule;
				foreach (RealizationalRule.Subrule sr in rule.Subrules)
					m_curMorpher.RemoveAllomorph(sr.ID);
			}
			else
			{
				rule = new RealizationalRule(name, name, m_curMorpher);
				m_curMorpher.AddMorphologicalRule(rule);
			}
			rule.Reset();

			string glossStr;
			if (realRuleSpec.Get("gl", out glossStr))
				rule.Gloss = new Gloss(glossStr, glossStr, m_curMorpher);

			List<object> reqHeadFeatsList;
			if (realRuleSpec.Get("r_hf", out reqHeadFeatsList))
				rule.RequiredHeadFeatures = LoadSynFeats(reqHeadFeatsList, m_curMorpher.HeadFeatureSystem);
			else
				rule.RequiredHeadFeatures = new FeatureValues();

			List<object> reqFootFeatsList;
			if (realRuleSpec.Get("r_ff", out reqFootFeatsList))
				rule.RequiredFootFeatures = LoadSynFeats(reqFootFeatsList, m_curMorpher.FootFeatureSystem);
			else
				rule.RequiredFootFeatures = new FeatureValues();

			List<object> realFeatsList;
			if (realRuleSpec.Get("rz_f", out realFeatsList))
				rule.RealizationalFeatures = LoadSynFeats(realFeatsList, m_curMorpher.HeadFeatureSystem);
			else
				rule.RealizationalFeatures = new FeatureValues();

			bool blockable = true;
			string blockableStr;
			if (realRuleSpec.Get("blockable", out blockableStr))
				blockable = blockableStr == "true";
			rule.IsBlockable = blockable;

			List<ConfigNode> subruleList = realRuleSpec.GetNodeList("subrules");
			foreach (ConfigNode srSpec in subruleList)
				LoadMSubrule(srSpec, rule);
		}

		void LoadMSubrule(ConfigNode msubruleSpec, AffixalMorphologicalRule rule)
		{
			List<object> varFeatsList;
			msubruleSpec.Get("var_fs", out varFeatsList);
			AlphaVariables varFeatures = LoadVarFeats(varFeatsList);

			ConfigNode lhs = msubruleSpec.Get<ConfigNode>("in");
			List<PhoneticPattern> lhsList = new List<PhoneticPattern>();
			List<object> pseqList = lhs.Get<List<object>>("pseq");
			for (int i = 0; i < pseqList.Count; i++)
			{
				PhoneticPattern pattern = new PhoneticPattern();
				LoadPSeq(pattern, ((List<object>) pseqList[i]).ConvertAll(ConfigNode.ToConfigNode),
					varFeatures);
				lhsList.Add(pattern);
			}

			ConfigNode rhs = msubruleSpec.Get<ConfigNode>("out");
			List<object> poutList = rhs.Get<List<object>>("p_out");
			List<MorphologicalOutput> rhsList = LoadPhonOutput(poutList, varFeatures, rule.ID);

			string id = rule.ID + "_subrule" + rule.SubruleCount;
			AffixalMorphologicalRule.Subrule sr = new AffixalMorphologicalRule.Subrule(id, id, m_curMorpher,
				lhsList, rhsList, varFeatures, MorphologicalTransform.RedupMorphType.IMPLICIT);

			List<object> exFeats;
			if (!lhs.Get("x_rf", out exFeats))
			{
				exFeats = new List<object>();
			}
			sr.ExcludedMPRFeatures = LoadMPRFeatures(exFeats);

			List<object> reqFeats;
			if (!lhs.Get("r_rf", out reqFeats))
			{
				reqFeats = new List<object>();
			}
			sr.RequiredMPRFeatures = LoadMPRFeatures(reqFeats);

			List<object> outFeats;
			if (!rhs.Get("rf", out outFeats))
			{
				outFeats = new List<object>();
			}
			sr.OutputMPRFeatures = LoadMPRFeatures(outFeats);

			rule.AddSubrule(sr);
			m_curMorpher.AddAllomorph(sr);
		}

		void LoadCompRule(ConfigNode compRuleSpec)
		{
			CheckCurMorpher();
			string name = compRuleSpec.Get<string>("nm");
			CompoundingRule rule;
			MorphologicalRule mrule = m_curMorpher.GetMorphologicalRule(name);
			if (mrule != null)
			{
				rule = mrule as CompoundingRule;
				foreach (CompoundingRule.Subrule sr in rule.Subrules)
					m_curMorpher.RemoveAllomorph(sr.ID);
			}
			else
			{
				rule = new CompoundingRule(name, name, m_curMorpher);
				m_curMorpher.AddMorphologicalRule(rule);
			}
			rule.Reset();

			string glossStr;
			if (compRuleSpec.Get("gl", out glossStr))
				rule.Gloss = new Gloss(glossStr, glossStr, m_curMorpher);

			List<object> headPOSs;
			if (!compRuleSpec.Get("head_pos", out headPOSs))
			{
				headPOSs = new List<object>();
			}
			rule.HeadRequiredPOSs = headPOSs.ConvertAll(ToPOS);

			List<object> nonHeadPOSs;
			if (!compRuleSpec.Get("nonhead_pos", out nonHeadPOSs))
			{
				nonHeadPOSs = new List<object>();
			}
			rule.NonHeadRequiredPOSs = nonHeadPOSs.ConvertAll(ToPOS);

			PartOfSpeech outPOS = null;
			string outPOSStr;
			if (compRuleSpec.Get("pos", out outPOSStr))
				outPOS = ToPOS(outPOSStr);
			rule.OutPOS = outPOS;

			List<object> headReqHeadFeatsList;
			if (compRuleSpec.Get("head_r_hf", out headReqHeadFeatsList))
				rule.HeadRequiredHeadFeatures = LoadSynFeats(headReqHeadFeatsList, m_curMorpher.HeadFeatureSystem);
			else
				rule.HeadRequiredHeadFeatures = new FeatureValues();

			List<object> headReqFootFeatsList;
			if (compRuleSpec.Get("head_r_ff", out headReqFootFeatsList))
				rule.HeadRequiredFootFeatures = LoadSynFeats(headReqFootFeatsList, m_curMorpher.FootFeatureSystem);
			else
				rule.HeadRequiredFootFeatures = new FeatureValues();

			List<object> nonHeadReqHeadFeatsList;
			if (compRuleSpec.Get("nonhead_r_hf", out nonHeadReqHeadFeatsList))
				rule.NonHeadRequiredHeadFeatures = LoadSynFeats(nonHeadReqHeadFeatsList, m_curMorpher.HeadFeatureSystem);
			else
				rule.NonHeadRequiredHeadFeatures = new FeatureValues();

			List<object> nonHeadReqFootFeatsList;
			if (compRuleSpec.Get("nonhead_r_ff", out nonHeadReqFootFeatsList))
				rule.NonHeadRequiredFootFeatures = LoadSynFeats(nonHeadReqFootFeatsList, m_curMorpher.FootFeatureSystem);
			else
				rule.NonHeadRequiredFootFeatures = new FeatureValues();

			List<object> outHeadFeatsList;
			if (compRuleSpec.Get("hf", out outHeadFeatsList))
				rule.OutHeadFeatures = LoadSynFeats(outHeadFeatsList, m_curMorpher.HeadFeatureSystem);
			else
				rule.OutHeadFeatures = new FeatureValues();

			List<object> outFootFeatsList;
			if (compRuleSpec.Get("ff", out outFootFeatsList))
				rule.OutFootFeatures = LoadSynFeats(outFootFeatsList, m_curMorpher.FootFeatureSystem);
			else
				rule.OutFootFeatures = new FeatureValues();

			List<string> obligFeatsList;
			HCObjectSet<Feature> obligFeats = new HCObjectSet<Feature>();
			if (compRuleSpec.GetStringList("of", out obligFeatsList))
			{
				foreach (string obligFeat in obligFeatsList)
				{
					Feature feature = m_curMorpher.HeadFeatureSystem.GetFeature(obligFeat);
					if (feature == null)
					{
						feature = new Feature(obligFeat, obligFeat, m_curMorpher);
						m_curMorpher.HeadFeatureSystem.AddFeature(feature);
					}
					obligFeats.Add(feature);
				}
			}
			rule.ObligatoryHeadFeatures = obligFeats;

			bool blockable = true;
			string blockableStr;
			if (compRuleSpec.Get("blockable", out blockableStr))
				blockable = blockableStr == "true";
			rule.IsBlockable = blockable;

			List<ConfigNode> subruleList = compRuleSpec.GetNodeList("subrules");
			foreach (ConfigNode srSpec in subruleList)
				LoadCompSubrule(srSpec, rule);

			string stratumName;
			if (!compRuleSpec.Get("stratum", out stratumName))
			{
				stratumName = compRuleSpec.Get<string>("str");
			}
			foreach (Stratum stratum in m_curMorpher.Strata)
			{
				if (stratumName == stratum.ID)
					stratum.AddMorphologicalRule(rule);
				else
					stratum.RemoveMorphologicalRule(name);
			}
		}

		void LoadCompSubrule(ConfigNode compSubruleSpec, CompoundingRule rule)
		{
			List<object> varFeatsList;
			compSubruleSpec.Get("var_fs", out varFeatsList);
			AlphaVariables varFeatures = LoadVarFeats(varFeatsList);

			ConfigNode headLhs = compSubruleSpec.Get<ConfigNode>("head");
			List<PhoneticPattern> headLhsList = new List<PhoneticPattern>();
			List<object> headPseqList = headLhs.Get<List<object>>("pseq");
			for (int i = 0; i < headPseqList.Count; i++)
			{
				PhoneticPattern pattern = new PhoneticPattern();
				LoadPSeq(pattern, ((List<object>) headPseqList[i]).ConvertAll(ConfigNode.ToConfigNode),
					varFeatures);
				headLhsList.Add(pattern);
			}

			ConfigNode nonHeadLhs = compSubruleSpec.Get<ConfigNode>("nonhead");
			List<PhoneticPattern> nonHeadLhsList = new List<PhoneticPattern>();
			List<object> nonHeadPseqList = nonHeadLhs.Get<List<object>>("pseq");
			for (int i = 0; i < nonHeadPseqList.Count; i++)
			{
				PhoneticPattern pattern = new PhoneticPattern();
				LoadPSeq(pattern, ((List<object>) nonHeadPseqList[i]).ConvertAll(ConfigNode.ToConfigNode),
					varFeatures);
				nonHeadLhsList.Add(pattern);
			}

			ConfigNode rhs = compSubruleSpec.Get<ConfigNode>("out");
			List<object> poutList = rhs.Get<List<object>>("p_out");
			List<MorphologicalOutput> rhsList = LoadPhonOutput(poutList, varFeatures, rule.ID);

			string id = rule.ID + "_subrule" + rule.SubruleCount;
			CompoundingRule.Subrule sr = new CompoundingRule.Subrule(id, id, m_curMorpher,
				headLhsList, nonHeadLhsList, rhsList, varFeatures);

			List<object> exFeats;
			if (!headLhs.Get("x_rf", out exFeats))
			{
				exFeats = new List<object>();
			}
			sr.ExcludedMPRFeatures = LoadMPRFeatures(exFeats);

			List<object> reqFeats;
			if (!headLhs.Get("r_rf", out reqFeats))
			{
				reqFeats = new List<object>();
			}
			sr.RequiredMPRFeatures = LoadMPRFeatures(reqFeats);

			List<object> outFeats;
			if (!rhs.Get("rf", out outFeats))
			{
				outFeats = new List<object>();
			}
			sr.OutputMPRFeatures = LoadMPRFeatures(outFeats);

			rule.AddSubrule(sr);
			m_curMorpher.AddAllomorph(sr);
		}

		List<MorphologicalOutput> LoadPhonOutput(List<object> poutList, AlphaVariables varFeatures, string ruleName)
		{
			List<MorphologicalOutput> rhsList = new List<MorphologicalOutput>();
			foreach (object poutObj in poutList)
			{
				if (poutObj is string)
				{
					int partition = Convert.ToInt32(poutObj as string) - 1;
					rhsList.Add(new CopyFromInput(partition));
				}
				else if (poutObj is ConfigNode)
				{
					SimpleContext ctxt = LoadNatClassCtxt(poutObj as ConfigNode, varFeatures);
					rhsList.Add(new InsertSimpleContext(ctxt));
				}
				else
				{
					IList<object> list = poutObj as IList<object>;
					if (list[1] is ConfigNode)
					{
						int partition = Convert.ToInt32(list[0] as string) - 1;
						SimpleContext ctxt = LoadNatClassCtxt(list[1] as ConfigNode, varFeatures);
						rhsList.Add(new ModifyFromInput(partition, ctxt, m_curMorpher));
					}
					else
					{
						string shapeStr = list[0] as string;
						string ctableName = list[1] as string;
						CharacterDefinitionTable charDefTable = GetCharDefTable(ctableName);
						PhoneticShape pshape;
						try
						{
							pshape = charDefTable.ToPhoneticShape(shapeStr, ModeType.SYNTHESIS);
						}
						catch (MissingPhoneticShapeException mpse)
						{
							LoadException le = new LoadException(LoadException.LoadErrorType.INVALID_RULE_SHAPE, this,
								string.Format(HCStrings.kstidInvalidRuleShape, shapeStr, ruleName, ctableName, mpse.Position + 1, shapeStr.Substring(mpse.Position)));
							le.Data["shape"] = shapeStr;
							le.Data["charDefTable"] = charDefTable.ID;
							le.Data["rule"] = ruleName;
							throw le;
						}

						rhsList.Add(new InsertSegments(pshape));
					}
				}
			}
			return rhsList;
		}

		AffixTemplate LoadAffixTemplate(ConfigNode tempSpec)
		{
			CheckCurMorpher();
			string name = tempSpec.Get<string>("nm");
			AffixTemplate template = new AffixTemplate(name, name, m_curMorpher);

			List<object> reqPOSs;
			if (!tempSpec.Get("r_pos", out reqPOSs))
			{
				reqPOSs = new List<object>();
			}
			template.RequiredPOSs = reqPOSs.ConvertAll(ToPOS);

			List<object> slots = tempSpec.Get<List<object>>("slots");
			for (int i = 0; i < slots.Count; i++)
			{
				string slotName;
				if (slots[i] is string)
				{
					if ((slots[i++] as string) != "name")
					{
						throw new LoadException(LoadException.LoadErrorType.INVALID_FORMAT, this,
							string.Format(HCStrings.kstidInvalidSlot, name));
					}
					slotName = slots[i++] as string;
				}
				else
				{
					slotName = name + i;
				}
				Slot slot = new Slot(slotName, slotName, m_curMorpher);
				List<string> rules = (slots[i] as List<object>).ConvertAll(Convert.ToString);
				RealizationalRule lastRule = null;
				foreach (string ruleId in rules)
				{
					RealizationalRule rule = m_curMorpher.GetMorphologicalRule(ruleId) as RealizationalRule;
					slot.AddRule(rule);
					lastRule = rule;
				}
				slot.IsOptional = lastRule.RealizationalFeatures.NumFeatures > 0;
				template.AddSlot(slot);
			}

			return template;
		}

		void CheckCurMorpher()
		{
			if (m_curMorpher == null)
				throw new LoadException(LoadException.LoadErrorType.NO_CURRENT_MORPHER, this, HCStrings.kstidNoLang);
		}

		MPRFeature ToMPRFeature(object value)
		{
			string name = value as string;
			MPRFeature mprFeat = m_curMorpher.GetMPRFeature(name);
			if (mprFeat == null)
			{
				mprFeat = new MPRFeature(name, name, m_curMorpher);
				m_curMorpher.AddMPRFeature(mprFeat);
			}
			return mprFeat;
		}

		PartOfSpeech ToPOS(object value)
		{
			string name = value as string;
			PartOfSpeech pos = m_curMorpher.GetPOS(name);
			if (pos == null)
			{
				pos = new PartOfSpeech(name, name, m_curMorpher);
				m_curMorpher.AddPOS(pos);
			}

			return pos;
		}

		CharacterDefinitionTable GetCharDefTable(string name)
		{
			CharacterDefinitionTable charDefTable = m_curMorpher.GetCharacterDefinitionTable(name);
			if (charDefTable == null)
				throw CreateUndefinedObjectException(string.Format(HCStrings.kstidUnknownCharDefTable, name), name);
			return charDefTable;
		}
	}
}
