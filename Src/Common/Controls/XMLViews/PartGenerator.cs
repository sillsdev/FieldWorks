using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SIL.FieldWorks.Common.COMInterfaces;
using System.Xml;
using SIL.Utils;
using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary>
	/// PartGenerator takes as input elements with attributes like
	/// class="LexSense" fieldType="mlstring" restrictions="customOnly"
	/// and generates a sequence of clones of the first non-comment child of the
	/// generate element, one for each field indicated by the parameters.
	/// </summary>
	public class PartGenerator
	{
		XmlVc m_vc;
		IFwMetaDataCache m_mdc;
		XmlNode m_input;
		uint m_clsid;
		/// <summary>
		/// class of item upon which to apply first layout
		/// </summary>
		protected int m_rootClassId = 0;
		string m_className;
		string m_fieldType;
		string m_restrictions;
		/// <summary>
		///	columnSpec
		/// </summary>
		protected XmlNode m_source;

		/// <summary>
		/// Make a part generator for the specified "generate" element, interpreting names
		/// using the specified metadatacache, using vc and rootClassId for handling generate nodes
		/// that refer to layouts.
		/// </summary>
		/// <param name="mdc"></param>
		/// <param name="input"></param>
		/// <param name="vc">for parts/layouts</param>
		/// <param name="rootClassId">class of root object from which column layouts can be computed</param>
		public PartGenerator(IFwMetaDataCache mdc, XmlNode input, XmlVc vc, int rootClassId)
		{
			m_mdc = mdc;
			m_vc = vc;
			m_rootClassId = rootClassId;
			m_input = input;
			InitMemberVariablesFromInput(mdc, input);
		}

		/// <summary>
		/// initialize fields based on input node.
		/// </summary>
		/// <param name="mdc"></param>
		/// <param name="input"></param>
		protected virtual void InitMemberVariablesFromInput(IFwMetaDataCache mdc, XmlNode input)
		{
			m_className = XmlUtils.GetManditoryAttributeValue(input, "class");
			m_clsid = mdc.GetClassId(m_className);
			m_fieldType = XmlUtils.GetManditoryAttributeValue(input, "fieldType");
			m_restrictions = XmlUtils.GetOptionalAttributeValue(input, "restrictions", "none");
			m_source = XmlUtils.GetFirstNonCommentChild(input);
		}

		/// <summary>
		/// Make a part generator for the specified "generate" element, interpreting names
		/// using the specified metadatacache. Doesn't handle generate nodes refering to layouts.
		/// Use the constructor with Vc for that.
		/// </summary>
		/// <param name="mdc"></param>
		/// <param name="input"></param>
		public PartGenerator(IFwMetaDataCache mdc, XmlNode input)
			: this(mdc, input, null, 0)
		{
		}

		/// <summary>
		/// This is the definition of what it means for a field to pass all current restrictions.
		/// </summary>
		/// <param name="flid"></param>
		/// <returns></returns>
		bool Accept(uint flid)
		{
			switch(m_restrictions)
			{
				case "none":
					return true;
				case "customOnly":
					// Currently the decisive way to tell, from the mdc, is whether the field name
					// starts with 'custom'. There is an explicit column in the database for this,
					// but it isn't yet loaded into the MDC and made accessible through its interface.
					string fieldName = m_mdc.GetFieldName(flid);
					return (fieldName.StartsWith("custom"));
			}
			return true;
		}

		/// <summary>
		/// Get an array of unsigned integers, the ids of the fields generated (that pass the
		/// restrictions).
		/// </summary>
		List<uint> FieldIds
		{
			get
			{
				FieldType fieldType;
				switch(m_fieldType)
				{
					case "mlstring":
						fieldType = FieldType.kgrfcptMulti; // bitmap selects ML fields
						break;
					case "string":
						fieldType = FieldType.kgrfcptString; // bitmap selects all string fields
						break;
					case "simplestring":
						fieldType = FieldType.kgrfcptSimpleString; // bitmap selects non-multilingual string fields
						break;
					default:
						throw new Exception("Unrecognized field type for generator: " + m_fieldType);
				}
				// Todo JohnT: handle restrictions.
				List<uint> acceptedFields = new List<uint>();
				foreach (uint ui in DbOps.GetFieldsInClassOfType(m_mdc, m_clsid, fieldType))
				{
					if (Accept(ui))
						acceptedFields.Add(ui);
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
				List<uint> acceptedFields = FieldIds;
				string[] fieldNames = new string[acceptedFields.Count];
				for (int i = 0; i < fieldNames.Length; i++)
					fieldNames[i] = m_mdc.GetFieldName(acceptedFields[i]);
				return fieldNames;
			}
		}

		/// <summary>
		/// Generate the nodes that the constructor arguments indicate.
		/// </summary>
		/// <returns></returns>
		public XmlNode[] Generate()
		{
			List<uint> ids = FieldIds;
			XmlNode[] result = new XmlNode[ids.Count];
			for(int iresult = 0; iresult < result.Length; iresult++)
			{
				XmlNode output = m_source.Clone();
				result[iresult] = output;
				uint fieldId = ids[iresult];
				string labelName = m_mdc.GetFieldLabel(fieldId);
				string fieldName = m_mdc.GetFieldName(fieldId);
				string className = m_mdc.GetOwnClsName(fieldId);
				if (labelName == null || labelName.Length == 0)
					labelName = fieldName;
				// generate parts for any given custom layout
				GeneratePartsFromLayouts(m_rootClassId, fieldName, fieldId, ref output);
				ReplaceParamsInAttributes(output, labelName, fieldName, fieldId, className);
				// LT-6956 : custom fields have the additional attribute "originalLabel", so add it here.
				XmlUtils.AppendAttribute(output, "originalLabel", labelName);
			}
			return result;
		}

		private void SetupWsParams(XmlNode output, uint fieldId)
		{
			if (fieldId == 0)
				return;
			int ws = m_mdc.GetFieldWs(fieldId);
			if (ws != 0)
			{
				// We've got the ws of the field, but there's a good chance it's for a multistring and is "plural"  However, the column can
				// only show one ws at a time, so we use this method to convert the "plural" ws to a "singular" one.
				// Since this may be a bulk edit field, we also want to use a simple WSID for the active one, while keeping the
				// other, if present, to indicate the other options.
				int wsSingular = FDO.LangProj.LangProject.PluralMagicWsToSingularMagicWs(ws);
				int wsSimple = FDO.LangProj.LangProject.SmartMagicWsToSimpleMagicWs(ws);
				string newWsName = "$ws=" + FDO.LangProj.LangProject.GetMagicWsNameFromId(wsSimple);
				if (wsSimple != wsSingular)
				{
					// replace wsName, and also append an "originalWs", which is not exactly the 'original' ws, but
					// one of the ones the column configure dialog recognizes that will allow all relevant options
					// to be chosen. If we use this part generator for cases where we want to allow multiple WSs
					// to show, we need to change this to just use ws instead of wsSingular, but then we will need
					// to generalize ColumnConfigureDialog.UpdateWsComboValue.
					ReplaceAttrAndAppend visitorWs = new ReplaceAttrAndAppend("$wsName", newWsName,
						"originalWs", FDO.LangProj.LangProject.GetMagicWsNameFromId(wsSingular));
					XmlUtils.VisitAttributes(output, visitorWs);
					visitorWs.DoTheAppends(); // after loop terminates.

				}
				else
				{
					// no substitution, just replace wsName
					XmlUtils.VisitAttributes(output, new ReplaceSubstringInAttr("$wsName", newWsName));
				}
			}
		}

		private void ReplaceParamsInAttributes(XmlNode output, string labelName, string fieldName, uint customFieldId, string className)
		{
			SetupWsParams(output, customFieldId);
			ReplaceSubstringInAttr visitorFn = new ReplaceSubstringInAttr("$fieldName", fieldName);
			XmlUtils.VisitAttributes(output, visitorFn);
			AppendClassAttribute(output, fieldName, className);
			ReplaceSubstringInAttr visitorLab = new ReplaceSubstringInAttr("$label", labelName);
			XmlUtils.VisitAttributes(output, visitorLab);
		}

		private static void AppendClassAttribute(XmlNode output, string fieldName, string className)
		{
			// Desired node may be a child of a child...  (See LT-6447.)
			foreach (XmlNode node in output.SelectNodes(".//*"))
			{
				if (XmlUtils.GetOptionalAttributeValue(node, "field") == fieldName)
					XmlUtils.AppendAttribute(node, "class", className);
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
		protected List<XmlNode> GeneratePartsFromLayouts(int layoutClass, string fieldNameForReplace, uint fieldIdForWs, ref XmlNode layoutNode)
		{
			if (m_vc == null || layoutClass == 0)
				return null;
			string layout = XmlUtils.GetOptionalAttributeValue(layoutNode, "layout");
			if (layout == null)
				return null;
			string className = m_mdc.GetClassName((uint)layoutClass);
			string layoutGeneric = "";
			string layoutSpecific = "";
			if (layout.Contains("$fieldName") && !fieldNameForReplace.Contains("$fieldName"))
			{
				// this is generic layout name that requires further specification.
				layoutGeneric = layout;

				// first try to substitute the field name to see if we can get an existing part.
				XmlNode layoutNodeForCustomField = layoutNode.CloneNode(true);
				ReplaceParamsInAttributes(layoutNodeForCustomField, "", fieldNameForReplace, fieldIdForWs, className);
				layoutSpecific = XmlUtils.GetOptionalAttributeValue(layoutNodeForCustomField, "layout");
			}
			else
			{
				// possibly need to look for the most generic layout name by default.
				layoutGeneric = "$fieldName";
				layoutSpecific = layout;
			}
			XmlNode partNode = m_vc.GetNodeForPart(layoutSpecific, false, layoutClass);
			if (partNode != null)
			{
				// Enhance: Validate existing part!
				// specific part already exists, just keep it.
				return null;
			}

			// couldn't find a specific part, so get the generic part in order to generate the specific part
			partNode = m_vc.GetNodeForPart(layoutGeneric, false, layoutClass);
			if (partNode == null)
				throw new ApplicationException("Couldn't find generic Part (" + className + "-Jt-" + layout + ")");
			if (partNode != null)
			{
				List<XmlNode> generatedParts = new List<XmlNode>();
				// clone the generic node so we can substitute any attributes that need to be substituted.
				XmlNode generatedPartNode = partNode.CloneNode(true);
				ReplaceParamsInAttributes(generatedPartNode, "", fieldNameForReplace, fieldIdForWs, className);
				Inventory.GetInventory("parts", m_vc.Cache.DatabaseName).AddNodeToInventory(generatedPartNode);
				generatedParts.Add(generatedPartNode);
				// now see if we need to create other parts from further generic layouts.
				if (fieldNameForReplace.Contains("$fieldName"))
				{
					// use the generated part, since it contains a template reference.
					partNode = generatedPartNode;
				}

				XmlNode nextLayoutNode = null;
				if (partNode.Attributes["layout"] != null)
					nextLayoutNode = partNode;
				else if (partNode.ChildNodes.Count > 0)
					nextLayoutNode = partNode.SelectSingleNode(".//*[@layout]");
				if (nextLayoutNode != null)
				{
					// now build the new node from its layouts
					string fieldName = XmlUtils.GetManditoryAttributeValue(nextLayoutNode, "field");
					int field = m_vc.Cache.GetFlid(0, className, fieldName);
					int nextLayoutClass = (int)m_vc.Cache.GetDestinationClass((uint)field);
					List<XmlNode> furtherGeneratedParts = GeneratePartsFromLayouts(nextLayoutClass, fieldNameForReplace, fieldIdForWs, ref nextLayoutNode);
					if (furtherGeneratedParts != null)
						generatedParts.AddRange(furtherGeneratedParts);
				}
				return generatedParts;
			}
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// In addition to replacing an old attribute value, appends a new attribute after the one
		/// where the replacement occurs. Call DoTheAppends after running VisitAttributes.
		/// </summary>
		class ReplaceAttrAndAppend : ReplaceSubstringInAttr
		{
			string m_newAttrName;
			string m_newAttrVal;
			string m_pattern; // dup of base class variable, saves modifying it.
			List<XmlAttribute> m_targets = new List<XmlAttribute>();
			internal ReplaceAttrAndAppend(string pattern, string replacement, string newAttrName, string newAttrVal)
				: base(pattern, replacement)
			{
				m_newAttrName = newAttrName;
				m_newAttrVal = newAttrVal;
				m_pattern = pattern;
			}

			public override bool Visit(XmlAttribute xa)
			{
				string old = xa.Value;
				int index = old.IndexOf(m_pattern);
				if (index >= 0)
				{
					m_targets.Add(xa);
				}
				base.Visit(xa); // AFTER we did our test, otherwise, it fails.
				return false; // continue iterating
			}
			internal void DoTheAppends()
			{
				foreach (XmlAttribute xa in m_targets)
					XmlUtils.AppendAttribute(xa.OwnerElement, m_newAttrName, m_newAttrVal);
			}
		}

		/// <summary>
		/// Return an array list of the non-comment children of root,
		/// except that any "generate" elements are replaced with what they generate.
		/// </summary>
		/// <param name="root">The root.</param>
		/// <param name="mdc">The MDC.</param>
		/// <param name="vc">for parts/layouts</param>
		/// <param name="rootClassId">the class of the rootObject used to generate the part</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		static internal List<XmlNode> GetGeneratedChildren(XmlNode root, IFwMetaDataCache mdc, XmlVc vc, int rootClassId)
		{
			return GetGeneratedChildren(root, mdc, null, vc, rootClassId);
		}

		/// <summary>
		/// Return an array list of the non-comment children of root,
		/// except that any "generate" elements are replaced with what they generate.
		/// This is for generating parts that are completely defined in the xml root.
		/// For generating parts through "layouts" use the interface that passes a Vc.
		/// </summary>
		/// <param name="root">The root.</param>
		/// <param name="mdc">The MDC.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		static public List<XmlNode> GetGeneratedChildren(XmlNode root, IFwMetaDataCache mdc)
		{
			return GetGeneratedChildren(root, mdc, null, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return an array list of the non-comment children of root,
		/// except that any "generate" elements are replaced with what they generate.
		/// </summary>
		/// <param name="root">The root.</param>
		/// <param name="mdc">The MDC.</param>
		/// <param name="keyAttrNames">if non-null, gives a list of key attribute names.
		/// generated children which match another node in root in all key attributes are omitted.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		static public List<XmlNode> GetGeneratedChildren(XmlNode root, IFwMetaDataCache mdc, string[] keyAttrNames)
		{
			return GetGeneratedChildren(root, mdc, keyAttrNames, null, 0);
		}


		/// <summary>
		///
		/// </summary>
		/// <param name="root"></param>
		/// <param name="mdc"></param>
		/// <param name="keyAttrNames"></param>
		/// <param name="vc">for parts/layouts</param>
		/// <param name="rootClassId">class of the root object used to compute parts/layouts</param>
		/// <returns></returns>
		static private List<XmlNode> GetGeneratedChildren(XmlNode root, IFwMetaDataCache mdc, string[] keyAttrNames,
			XmlVc vc, int rootClassId)
		{
			List<XmlNode> result = new List<XmlNode>();
			string generateModeForColumns = XmlUtils.GetOptionalAttributeValue(root, "generate");
			bool m_fGenerateChildPartsForParentLayouts = (generateModeForColumns == "childPartsForParentLayouts");

			// childPartsForParentLayouts
			foreach(XmlNode child in root.ChildNodes)
			{
				if (child is XmlComment)
					continue;
				if (m_fGenerateChildPartsForParentLayouts)
				{
					ChildPartGenerator cpg = new ChildPartGenerator(mdc, child, vc, rootClassId);
					cpg.GenerateChildPartsIfNeeded();
				}
				if (child.Name != "generate")
				{
					result.Add(child);
					continue;
				}

				PartGenerator generator = new PartGenerator(mdc, child, vc, rootClassId);
				foreach (XmlNode genNode in generator.Generate())
				{
					bool match = false;
					if (keyAttrNames != null)
					{
						foreach (XmlNode matchNode in root.ChildNodes)
						{
							if (MatchNodes(matchNode, genNode, keyAttrNames))
							{
								match = true;
								break;
							}
						}
					}
					if (!match) // not already present, or not checking; add it.
						result.Add(genNode);
				}
			}
			return result;
		}

		/// <summary>
		/// Answer true if the name and every attr specified in keyAttrNames matches.
		/// </summary>
		/// <param name="matchNode"></param>
		/// <param name="genNode"></param>
		/// <param name="keyAttrNames"></param>
		/// <returns></returns>
		static bool MatchNodes(XmlNode matchNode, XmlNode genNode, string[] keyAttrNames)
		{
			if (matchNode.Name != genNode.Name)
				return false;
			foreach (string attrName in keyAttrNames)
			{
				string matchAttrVal = XmlUtils.GetOptionalAttributeValue(matchNode, attrName, null);
				string genAttrVal = XmlUtils.GetOptionalAttributeValue(genNode, attrName, null);
				if (matchAttrVal != genAttrVal)
					return false;
			}
			return true;
		}
	}

	/// <summary>
	/// Generate parts needed to provide paths to fields specified by a given layout
	/// </summary>
	internal class ChildPartGenerator : PartGenerator
	{
		internal ChildPartGenerator(IFwMetaDataCache mdc, XmlNode input, XmlVc vc, int rootClassId)
			: base(mdc, input, vc, rootClassId)
		{
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="mdc"></param>
		/// <param name="input">the column node (not generate node)</param>
		protected override void InitMemberVariablesFromInput(IFwMetaDataCache mdc, XmlNode input)
		{
			if (input.Name == "generate")
			{
				// first column child is the node we want to try to generate.
				m_source = input.SelectSingleNode("./column");
				return;
			}
			else if (input.Name != "column")
				throw new ArgumentException("ChildPartGenerator expects input to be column node, not {0}", input.Name);
			m_source = input;
			//m_clsid = m_rootClassId;
			//m_className = mdc.GetClassName(m_rootClassId);
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public List<XmlNode> GenerateChildPartsIfNeeded()
		{
			string fieldName = XmlUtils.GetOptionalAttributeValue(m_source, "layout");
			return GeneratePartsFromLayouts(m_rootClassId, fieldName, 0, ref m_source);
		}
	}

}
