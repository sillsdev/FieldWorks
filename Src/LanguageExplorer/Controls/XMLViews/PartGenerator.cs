// Copyright (c) 2005-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using SIL.LCModel.Utils;
using SIL.LCModel;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.Xml;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// PartGenerator takes as input elements with attributes like
	/// class="LexSense" fieldType="mlstring" restrictions="customOnly"
	/// and generates a sequence of clones of the first non-comment child of the
	/// generate element, one for each field indicated by the parameters.
	/// </summary>
	internal class PartGenerator
	{
		XmlVc m_vc;
		readonly LcmCache m_cache;
		/// <summary>
		/// The metadata cache
		/// </summary>
		protected IFwMetaDataCache m_mdc;
		XElement m_input;
		int m_clsid;
		/// <summary>
		/// class of item upon which to apply first layout
		/// </summary>
		protected int m_rootClassId;
		string m_className;
		string m_fieldType;
		string m_restrictions;
		private int m_destClsid;
		/// <summary>
		///	columnSpec
		/// </summary>
		protected XElement m_source;

		/// <summary>
		/// Make a part generator for the specified "generate" element, interpreting names
		/// using the specified metadatacache, using vc and rootClassId for handling generate nodes
		/// that refer to layouts.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="input"></param>
		/// <param name="vc">for parts/layouts</param>
		/// <param name="rootClassId">class of root object from which column layouts can be computed</param>
		internal PartGenerator(LcmCache cache, XElement input, XmlVc vc, int rootClassId)
		{
			m_cache = cache;
			m_mdc = cache.MetaDataCacheAccessor;
			m_vc = vc;
			m_rootClassId = rootClassId;
			m_input = input;
			InitMemberVariablesFromInput(cache.MetaDataCacheAccessor, input);
		}

		/// <summary>
		/// initialize fields based on input node.
		/// </summary>
		protected virtual void InitMemberVariablesFromInput(IFwMetaDataCache mdc, XElement input)
		{
			m_className = XmlUtils.GetMandatoryAttributeValue(input, "class");
			m_clsid = mdc.GetClassId(m_className);
			m_fieldType = XmlUtils.GetMandatoryAttributeValue(input, "fieldType");
			m_restrictions = XmlUtils.GetOptionalAttributeValue(input, "restrictions", "none");
			m_source = XmlUtils.GetFirstNonCommentChild(input);
			var destClass = XmlUtils.GetOptionalAttributeValue(input, "destClass");
			if (!string.IsNullOrEmpty(destClass))
			{
				m_destClsid = m_mdc.GetClassId(destClass);
			}
		}

		/// <summary>
		/// Make a part generator for the specified "generate" element, interpreting names
		/// using the specified metadatacache. Doesn't handle generate nodes refering to layouts.
		/// Use the constructor with Vc for that.
		/// </summary>
		public PartGenerator(LcmCache cache, XElement input)
			: this(cache, input, null, 0)
		{
		}

		/// <summary>
		/// This is the definition of what it means for a field to pass all current restrictions.
		/// </summary>
		private bool Accept(int flid)
		{
			if (m_mdc.get_IsVirtual(flid))
			{
				return false;
			}

			// LT-13636 -- The line below kept custom fields referencing custom lists from being valid browse columns.
			//if (m_destClsid != 0 && m_mdc.GetDstClsId(flid) != m_destClsid) is too strict! Should accept a subclass!
			if (m_destClsid != 0)
			{
				var flidDestClass = m_mdc.GetDstClsId(flid);
				var acceptableClasses = ((IFwMetaDataCacheManaged)m_mdc).GetAllSubclasses(m_destClsid);
				if (!acceptableClasses.ContainsCollection(new[] {flidDestClass}))
				{
					return false;
				}
			}

			switch (m_restrictions)
			{
				case "none":
					return true;
				case "customOnly":
					return ((IFwMetaDataCacheManaged) m_mdc).IsCustom(flid);
				case "featureDefns":
					return flid == FsFeatureSystemTags.kflidFeatures;
			}
			return true;
		}

		/// <summary>
		/// Get an array of unsigned integers, the ids of the fields generated (that pass the
		/// restrictions).
		/// </summary>
		protected List<int> FieldIds
		{
			get
			{
				CellarPropertyTypeFilter fieldTypes;
				switch(m_fieldType)
				{
					case "mlstring":
						fieldTypes = CellarPropertyTypeFilter.AllMulti; // bitmap selects ML fields
						break;
					case "string":
						fieldTypes = CellarPropertyTypeFilter.AllString; // bitmap selects all string fields
						break;
					case "simplestring":
						fieldTypes = CellarPropertyTypeFilter.AllSimpleString; // bitmap selects non-multilingual string fields
						break;
					case "integer":
						fieldTypes = CellarPropertyTypeFilter.Integer;
						break;
					case "gendate":
						fieldTypes = CellarPropertyTypeFilter.GenDate;
						break;
					case "refatom":
						fieldTypes = CellarPropertyTypeFilter.ReferenceAtomic;
						break;
					case "owningatom":
						fieldTypes = CellarPropertyTypeFilter.OwningAtomic;
						break;
					case "atom":
						fieldTypes = CellarPropertyTypeFilter.AllAtomic;
						break;
					case "vector":
						fieldTypes = CellarPropertyTypeFilter.AllVector;
						break;
					default:
						throw new Exception("Unrecognized field type for generator: " + m_fieldType);
				}
				// Todo JohnT: handle restrictions.
				var acceptedFields = new List<int>();
				int[] uiIds;
				if (m_mdc is IFwMetaDataCacheManaged)
				{
					uiIds = ((IFwMetaDataCacheManaged)m_mdc).GetFields(m_clsid, true, (int)fieldTypes);
				}
				else
				{
					var countFoundFlids = m_mdc.GetFields(m_clsid, true, (int)fieldTypes, 0, ArrayPtr.Null);
					using (var flids = MarshalEx.ArrayToNative<int>(countFoundFlids))
					{
						countFoundFlids = m_mdc.GetFields(m_clsid, true, (int)fieldTypes, countFoundFlids, flids);
						uiIds = MarshalEx.NativeToArray<int>(flids, countFoundFlids);
					}
				}
				foreach (var ui in uiIds)
				{
					if (Accept(ui))
					{
						acceptedFields.Add(ui);
					}
				}
				return acceptedFields;
			}
		}

		/// <summary>
		/// Get the list of field names for which we will generate elements.
		/// </summary>
		public string[] FieldNames
		{
			get
			{
				var acceptedFields = FieldIds;
				var fieldNames = new string[acceptedFields.Count];
				for (var i = 0; i < fieldNames.Length; i++)
				{
					fieldNames[i] = m_mdc.GetFieldName(acceptedFields[i]);
				}
				return fieldNames;
			}
		}

		/// <summary>
		/// Generate the nodes that the constructor arguments indicate.
		/// </summary>
		public virtual XElement[] Generate()
		{
			var ids = FieldIds;
			var result = new XElement[ids.Count];
			for(var iresult = 0; iresult < result.Length; iresult++)
			{
				var output = m_source.Clone();
				result[iresult] = output;
				var fieldId = ids[iresult];
				var labelName = m_mdc.GetFieldLabel(fieldId);
				var fieldName = m_mdc.GetFieldName(fieldId);
				var className = m_mdc.GetOwnClsName(fieldId);
				if (string.IsNullOrEmpty(labelName))
				{
					labelName = fieldName;
				}
				// generate parts for any given custom layout
				GeneratePartsFromLayouts(m_rootClassId, fieldName, fieldId, ref output);
				ReplaceParamsInAttributes(output, labelName, fieldName, fieldId, className);
				// LT-6956 : custom fields have the additional attribute "originalLabel", so add it here.
				XmlUtils.SetAttribute(output, "originalLabel", labelName);
			}
			return result;
		}

		private void SetupWsParams(XElement output, int fieldId)
		{
			if (fieldId == 0)
			{
				return;
			}
			var ws = m_mdc.GetFieldWs(fieldId);
			if (ws == 0)
			{
				return;
			}
			// We've got the ws of the field, but there's a good chance it's for a multistring and is "plural"  However, the column can
			// only show one ws at a time, so we use this method to convert the "plural" ws to a "singular" one.
			// Since this may be a bulk edit field, we also want to use a simple WSID for the active one, while keeping the
			// other, if present, to indicate the other options.
			var wsSingular = WritingSystemServices.PluralMagicWsToSingularMagicWs(ws);
			var wsSimple = WritingSystemServices.SmartMagicWsToSimpleMagicWs(ws);
			var newWsName = "$ws=" + WritingSystemServices.GetMagicWsNameFromId(wsSimple);
			if (wsSimple != wsSingular)
			{
				// replace wsName, and also append an "originalWs", which is not exactly the 'original' ws, but
				// one of the ones the column configure dialog recognizes that will allow all relevant options
				// to be chosen. If we use this part generator for cases where we want to allow multiple WSs
				// to show, we need to change this to just use ws instead of wsSingular, but then we will need
				// to generalize ColumnConfigureDialog.UpdateWsComboValue.
				var visitorWs = new ReplaceAttrAndAppend("$wsName", newWsName, "originalWs", WritingSystemServices.GetMagicWsNameFromId(wsSingular));
				XmlUtils.VisitAttributes(output, visitorWs);
				visitorWs.DoTheAppends(); // after loop terminates.

			}
			else
			{
				// no substitution, just replace wsName
				XmlUtils.VisitAttributes(output, new ReplaceSubstringInAttr("$wsName", newWsName));
			}
		}

		/// <summary />
		protected void ReplaceParamsInAttributes(XElement output, string labelName, string fieldName, int customFieldId, string className)
		{
			SetupWsParams(output, customFieldId);
			var visitorFn = new ReplaceSubstringInAttr("$fieldName", fieldName);
			XmlUtils.VisitAttributes(output, visitorFn);
			AppendClassAttribute(output, fieldName, className);
			var visitorLab = new ReplaceSubstringInAttr("$label", labelName);
			XmlUtils.VisitAttributes(output, visitorLab);
			if (customFieldId == 0 || !(m_mdc is IFwMetaDataCacheManaged))
			{
				return;
			}
			var mdc = m_mdc as IFwMetaDataCacheManaged;
			if (!mdc.IsCustom(customFieldId) || mdc.GetDstClsId(customFieldId) == 0)
			{
				return;
			}
			var guidList = mdc.GetFieldListRoot(customFieldId);
			if (guidList == Guid.Empty)
			{
				return;
			}

			var list = m_cache.ServiceLocator.GetInstance<ICmPossibilityListRepository>().GetObject(guidList);
			var targetList = list.Owner != null ? $"{list.Owner.ClassName}.{mdc.GetFieldName(list.OwningFlid)}" : $"unowned.{guidList.ToString()}";
			XmlUtils.VisitAttributes(output, new ReplaceSubstringInAttr("$targetList", targetList));
		}

		private static void AppendClassAttribute(XElement output, string fieldName, string className)
		{
			// Desired node may be a child of a child...  (See LT-6447.)
			foreach (var node in output.XPathSelectElements(".//*"))
			{
				if (XmlUtils.GetOptionalAttributeValue(node, "field") == fieldName)
				{
					XmlUtils.SetAttribute(node, "class", className);
				}
			}
		}

		/// <summary>
		/// make sure we can get a PartNode for this child, if not,
		/// try to (recursively) generate parts refering to the parent(s) until we can find the owner of the layout.
		///
		/// clone this (generic) part:
		///
		/// <code>
		///		<part id="{className}-Jt-$fieldName" type="jtview">
		///			<obj class="{className}" field="{OwnerOfClassVirtualProperty}" layout="$fieldName"/>
		///		</part>
		/// </code>
		///
		/// into this (specific) part:
		///
		/// <code>
		///		<part id="{className}-Jt-{layoutName}" type="jtview">
		///			<obj class="{className}" field="{OwnerOfClassVirtualProperty}" layout="{layoutName}"/>
		///		</part>
		/// </code>
		/// </summary>
		/// <param name="layoutClass"></param>
		/// <param name="fieldNameForReplace"></param>
		/// <param name="fieldIdForWs"></param>
		/// <param name="layoutNode"></param>
		/// <returns>list of part nodes generated</returns>
		protected List<XElement> GeneratePartsFromLayouts(int layoutClass, string fieldNameForReplace, int fieldIdForWs, ref XElement layoutNode)
		{
			if (m_vc == null || layoutClass == 0)
			{
				return null;
			}
			var layout = XmlUtils.GetOptionalAttributeValue(layoutNode, "layout");
			if (layout == null)
			{
				return null;
			}
			var className = m_mdc.GetClassName(layoutClass);
			string layoutGeneric;
			string layoutSpecific;
			if (layout.Contains("$fieldName") && !fieldNameForReplace.Contains("$fieldName"))
			{
				// this is generic layout name that requires further specification.
				layoutGeneric = layout;
				// first try to substitute the field name to see if we can get an existing part.
				var layoutNodeForCustomField = layoutNode.Clone();
				ReplaceParamsInAttributes(layoutNodeForCustomField, "", fieldNameForReplace, fieldIdForWs, className);
				layoutSpecific = XmlUtils.GetOptionalAttributeValue(layoutNodeForCustomField, "layout");
			}
			else
			{
				// possibly need to look for the most generic layout name by default.
				layoutGeneric = "$fieldName";
				layoutSpecific = layout;
			}
			var partNode = m_vc.GetNodeForPart(layoutSpecific, false, layoutClass);
			if (partNode != null)
			{
				// Enhance: Validate existing part!
				// specific part already exists, just keep it.
				return null;
			}

			// couldn't find a specific part, so get the generic part in order to generate the specific part
			partNode = m_vc.GetNodeForPart(layoutGeneric, false, layoutClass);
			if (partNode == null)
			{
#if !__MonoCS__
				throw new ApplicationException("Couldn't find generic Part (" + className + "-Jt-" + layout + ")");
#else
// TODO-Linux: Fix this in the correct way.
				return null;
#endif
			}
			var generatedParts = new List<XElement>();
			// clone the generic node so we can substitute any attributes that need to be substituted.
			var generatedPartNode = partNode.Clone();
			ReplaceParamsInAttributes(generatedPartNode, "", fieldNameForReplace, fieldIdForWs, className);
			Inventory.GetInventory("parts", m_vc.Cache.ProjectId.Name).AddNodeToInventory(generatedPartNode);
			generatedParts.Add(generatedPartNode);
			// now see if we need to create other parts from further generic layouts.
			if (fieldNameForReplace.Contains("$fieldName"))
			{
				// use the generated part, since it contains a template reference.
				partNode = generatedPartNode;
			}

			XElement nextLayoutNode = null;
			var layoutAttr = partNode.Attribute("layout");
			if (layoutAttr != null && layoutAttr.Value.Contains("$fieldName"))
			{
				nextLayoutNode = partNode;
			}
			else if (partNode.Elements().Any())
			{
				nextLayoutNode = partNode.XPathSelectElement(".//*[contains(@layout, '$fieldName')]");
			}
			if (nextLayoutNode == null)
			{ return generatedParts;}
			// now build the new node from its layouts
			var fieldName = XmlUtils.GetMandatoryAttributeValue(nextLayoutNode, "field");
			var field = m_vc.Cache.DomainDataByFlid.MetaDataCache.GetFieldId(className, fieldName, true);
			var nextLayoutClass = m_vc.Cache.GetDestinationClass(field);
			var furtherGeneratedParts = GeneratePartsFromLayouts(nextLayoutClass, fieldNameForReplace, fieldIdForWs, ref nextLayoutNode);
			if (furtherGeneratedParts != null)
			{
				generatedParts.AddRange(furtherGeneratedParts);
			}
			return generatedParts;
		}

		/// <summary>
		/// In addition to replacing an old attribute value, appends a new attribute after the one
		/// where the replacement occurs. Call DoTheAppends after running VisitAttributes.
		/// </summary>
		private sealed class ReplaceAttrAndAppend : ReplaceSubstringInAttr
		{
			string m_newAttrName;
			string m_newAttrVal;
			string m_pattern; // dup of base class variable, saves modifying it.
			List<XAttribute> m_targets = new List<XAttribute>();

			internal ReplaceAttrAndAppend(string pattern, string replacement, string newAttrName, string newAttrVal)
				: base(pattern, replacement)
			{
				m_newAttrName = newAttrName;
				m_newAttrVal = newAttrVal;
				m_pattern = pattern;
			}

			public override bool Visit(XAttribute xa)
			{
				var old = xa.Value;
				var index = old.IndexOf(m_pattern);
				if (index >= 0)
				{
					m_targets.Add(xa);
				}
				base.Visit(xa); // AFTER we did our test, otherwise, it fails.
				return false; // continue iterating
			}

			internal void DoTheAppends()
			{
				foreach (var xa in m_targets)
				{
					XmlUtils.SetAttribute(xa.Parent, m_newAttrName, m_newAttrVal);
				}
			}
		}

		/// <summary>
		/// Return an array list of the non-comment children of root,
		/// except that any "generate" elements are replaced with what they generate.
		/// </summary>
		/// <param name="root">The root.</param>
		/// <param name="cache">The LCM cache.</param>
		/// <param name="vc">for parts/layouts</param>
		/// <param name="rootClassId">the class of the rootObject used to generate the part</param>
		internal static List<XElement> GetGeneratedChildren(XElement root, LcmCache cache, XmlVc vc, int rootClassId)
		{
			return GetGeneratedChildren(root, cache, null, vc, rootClassId);
		}

		/// <summary>
		/// Return an array list of the non-comment children of root,
		/// except that any "generate" elements are replaced with what they generate.
		/// This is for generating parts that are completely defined in the xml root.
		/// For generating parts through "layouts" use the interface that passes a Vc.
		/// </summary>
		public static List<XElement> GetGeneratedChildren(XElement root, LcmCache cache)
		{
			return GetGeneratedChildren(root, cache, null, 0);
		}

		/// <summary>
		/// Return an array list of the non-comment children of root,
		/// except that any "generate" elements are replaced with what they generate.
		/// </summary>
		/// <param name="root">The root.</param>
		/// <param name="cache">The LCM cache.</param>
		/// <param name="keyAttrNames">if non-null, gives a list of key attribute names.
		/// generated children which match another node in root in all key attributes are omitted.</param>
		public static List<XElement> GetGeneratedChildren(XElement root, LcmCache cache, string[] keyAttrNames)
		{
			return GetGeneratedChildren(root, cache, keyAttrNames, null, 0);
		}

		/// <summary />
		/// <param name="root"></param>
		/// <param name="cache"></param>
		/// <param name="keyAttrNames"></param>
		/// <param name="vc">for parts/layouts</param>
		/// <param name="rootClassId">class of the root object used to compute parts/layouts</param>
		private static List<XElement> GetGeneratedChildren(XElement root, LcmCache cache, string[] keyAttrNames, XmlVc vc, int rootClassId)
		{
			var result = new List<XElement>();
			var generateModeForColumns = XmlUtils.GetOptionalAttributeValue(root, "generate");
			var fGenerateChildPartsForParentLayouts = (generateModeForColumns == "childPartsForParentLayouts");

			// childPartsForParentLayouts
			foreach(var child in root.Elements())
			{
				if (fGenerateChildPartsForParentLayouts)
				{
					var cpg = new ChildPartGenerator(cache, child, vc, rootClassId);
					cpg.GenerateChildPartsIfNeeded();
				}
				if (child.Name != "generate")
				{
					result.Add(child);
					continue;
				}

				var generator = generateModeForColumns == "objectValuePartsForParentLayouts" ? new ObjectValuePartGenerator(cache, child, vc, rootClassId) : new PartGenerator(cache, child, vc, rootClassId);
				foreach (var genNode in generator.Generate())
				{
					var match = false;
					if (keyAttrNames != null)
					{
						foreach (var matchNode in root.Elements())
						{
							if (MatchNodes(matchNode, genNode, keyAttrNames))
							{
								match = true;
								break;
							}
						}
					}

					if (!match) // not already present, or not checking; add it.
					{
						result.Add(genNode);
					}
				}
			}
			return result;
		}

		/// <summary>
		/// Answer true if the name and every attr specified in keyAttrNames matches.
		/// </summary>
		private static bool MatchNodes(XElement matchNode, XElement genNode, string[] keyAttrNames)
		{
			if (matchNode.Name != genNode.Name)
			{
				return false;
			}
			foreach (var attrName in keyAttrNames)
			{
				var matchAttrVal = XmlUtils.GetOptionalAttributeValue(matchNode, attrName, null);
				var genAttrVal = XmlUtils.GetOptionalAttributeValue(genNode, attrName, null);
				if (matchAttrVal != genAttrVal)
				{
					return false;
				}
			}
			return true;
		}
	}
}
