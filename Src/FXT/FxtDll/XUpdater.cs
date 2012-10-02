// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, 2008 SIL International. All Rights Reserved.
// <copyright from='2003' to='2008' company='SIL International'>
//		Copyright (c) 2003, 2008 SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: XUpdater.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// This updates a given FXT result file.
// Note: it has only been implemented and tested for the field types used for the Parser
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Ling;
using SIL.Utils;

namespace SIL.FieldWorks.Common.FXT
{
	public class XUpdater : XDumper
	{
		private const string ksAttribute = "attribute";
		private const string ksField = "field";
		private const string ksGroup = "group";
		private const string ksObjAtomic = "objAtomic";
		private const string ksRefVector = "refVector";
		private const string ksSimpleProperty = "simpleProperty";
		IFwMetaDataCache m_mdc;

		public XUpdater(FdoCache cache, string sFxtPath)
		{
			m_cache = cache;
			FxtDocument = new XmlDocument();
			FxtDocument.Load(sFxtPath);
			m_mdc = m_cache.MetaDataCacheAccessor;
		}

		public XmlDocument UpdateFXTResult(List<ChangedDataItem> changedItems, XmlDocument fxtResult)
		{
			m_templateRootNode = FxtDocument.SelectSingleNode("//template");
			m_format = "xml";

			foreach (ChangedDataItem item in changedItems)
			{
				int hvoItem = item.Hvo;
				int flid = item.Flid;
				int classid = item.ClassId;
				string sClassName = item.ClassName;

				XmlNode fxtClassNode = GetFxtClassNode(ref sClassName, hvoItem);
				if (fxtClassNode == null)
				{
					Trace.WriteLine("XUpdater: Could not find class " + sClassName + " in FXT file.");
					continue; // Not all classes are addressed in the FXT description
				}

				string sFieldName = m_mdc.GetFieldName((uint)flid);
				int fieldType = m_mdc.GetFieldType((uint) flid);

				switch (fieldType)
				{
					case (int)CellarModuleDefns.kcptBoolean:// boolean is an integer value of 0 or 1, so fall through
					case (int)CellarModuleDefns.kcptInteger:
						UpdateInteger(sClassName, sFieldName, hvoItem, fxtResult);
						break;
					case (int)CellarModuleDefns.kcptString:
					case (int)CellarModuleDefns.kcptUnicode:
						UpdateString(sClassName, sFieldName, hvoItem, fxtResult);
						break;
					case (int)CellarModuleDefns.kcptMultiString: // this one, two??
					case (int)CellarModuleDefns.kcptMultiUnicode:
						UpdateMultiUnicode(sClassName, sFieldName, hvoItem, fxtResult);
						break;

					case (int)CellarModuleDefns.kcptOwningAtom:
						// we may well never get here since these do not get added or removed
						UpdateOwningAtomic(sClassName, flid, sFieldName, hvoItem, fxtResult);
						break;

					case (int)CellarModuleDefns.kcptOwningCollection: // fall through
					case (int)CellarModuleDefns.kcptOwningSequence:
						UpdateOwningVector(sClassName, flid, sFieldName, hvoItem, fieldType, fxtResult);
						break;

					case (int)CellarModuleDefns.kcptReferenceAtom:
						UpdateReferenceAtomic(sClassName, flid, sFieldName, hvoItem, fxtResult);
						break;
					case (int)CellarModuleDefns.kcptReferenceCollection:
					case (int)CellarModuleDefns.kcptReferenceSequence:
						UpdateReferenceVector(sClassName, flid, sFieldName, hvoItem, fieldType, fxtResult);
						break;
					default:
						// not coded yet...
						break;
				}

			}
			return fxtResult;
		}

		private XmlNode GetFxtClassNode(ref string sClassName, int hvoItem)
		{
			XmlNode fxtClassNode = GetClassTemplateNode(sClassName);
			if (fxtClassNode == null)
			{
				// CmPossibilityList may well not show in the FXT file.
				// Or it could be that its owner is there and it is a group (but as the field)
				// Try the owner.
				ICmObject obj = CmObject.CreateFromDBObject(m_cache, hvoItem);
				int ownerFlid = obj.OwningFlid;
				int ownerClass = m_cache.GetOwnClsId(ownerFlid);
				sClassName = m_mdc.GetClassName((uint) ownerClass);
				fxtClassNode = GetClassTemplateNode(sClassName);
			}
			return fxtClassNode;
		}

		private void UpdateOwningAtomic(string sClassName, int flid, string sFieldName, int hvoItem, XmlDocument fxtResult)
		{
			// need to find the correct class as well as the field in the FXT document
			string sFxtParentName;
			FXTElementSearchProperties searchPropsFound;
			XmlNode fxtFieldNode = GetFxtFieldNodeForOwningAtomic(sFieldName, ksObjAtomic, ksGroup, sClassName, out sFxtParentName, out searchPropsFound);
			if (fxtFieldNode == null)
				return; // Not all fields are addressed in the FXT description

			string sXPath = "//" + sFxtParentName + "[ancestor-or-self::*[@Id='" + hvoItem + "']]";
			XmlNode resultParentNode = fxtResult.SelectSingleNode(sXPath); // is the owner
			if (resultParentNode == null)
			{
				XmlNode objResultNode = FindOrInsertAllElementObject(hvoItem, fxtResult);
				if (objResultNode != null)
				{
					resultParentNode = objResultNode.SelectSingleNode("descendant-or-self::" + sFxtParentName);
				}
				else
				{
					// the item does not have an Id; it's just the parent element
					resultParentNode = fxtResult.SelectSingleNode("//" + sFxtParentName);
					if (resultParentNode == null)
						// The object has not been added to the result yet, most likely it is being initialized, so wait for owner to add it
						return;
				}
			}

			int objPropHvo = m_cache.GetObjProperty(hvoItem, flid);
			if (searchPropsFound.ElementName == ksObjAtomic)
			{
				resultParentNode.RemoveAll();
				if (objPropHvo != 0)
				{
					XmlDocumentFragment frag = fxtResult.CreateDocumentFragment();
					frag.InnerXml = GetDumpResult(CmObject.CreateFromDBObject(m_cache, objPropHvo));
					resultParentNode.AppendChild(frag);
				}
			}
			else if (searchPropsFound.ElementName == ksGroup)
			{
				List<XmlNode> removeNodes = new List<XmlNode>();
				CmObject parent = CmObject.CreateFromDBObject(m_cache, hvoItem) as CmObject;
				XmlNode lastChild = GetGroupChildrenToRemove(fxtFieldNode, resultParentNode.FirstChild, removeNodes);
				foreach (XmlNode node in removeNodes)
					resultParentNode.RemoveChild(node);

				StringWriter writer = new StringWriter();
				DoGroupElement(writer, parent, fxtFieldNode);
				XmlDocumentFragment frag = fxtResult.CreateDocumentFragment();
				frag.InnerXml = writer.ToString().Trim();
				if (lastChild == null)
					resultParentNode.AppendChild(frag);
				else
					resultParentNode.InsertBefore(frag, lastChild);

				// try to insert the new object in to one of the special "All" elements
				FindOrInsertAllElementObject(objPropHvo, fxtResult);
			}

		}

		private XmlNode GetGroupChildrenToRemove(XmlNode fxtGroupNode, XmlNode curChild, List<XmlNode> removeNodes)
		{
			foreach (XmlNode node in fxtGroupNode.ChildNodes)
			{
				switch (node.Name)
				{
					case "element":
						curChild = GetNextElement(curChild, (node as XmlElement).GetAttribute("name"));
						if (curChild != null)
						{
							XmlElement childElem = curChild as XmlElement;
							string oldHvo = childElem.GetAttribute("dst");
							if (!string.IsNullOrEmpty(oldHvo))
							{
								XmlNode removeNode = childElem.OwnerDocument.SelectSingleNode("//*[@Id = '" + oldHvo + "']");
								removeNode.ParentNode.RemoveChild(removeNode);
							}
							removeNodes.Add(curChild);
							curChild = curChild.NextSibling;
						}
						break;

					case "group":
						curChild = GetGroupChildrenToRemove(node, curChild, removeNodes);
						break;

					default:
						if (node.NodeType == XmlNodeType.Element)
						{
							curChild = GetNextElement(curChild, node.Name);
							if (curChild != null)
							{
								XmlElement childElem = curChild as XmlElement;
								string oldHvo = childElem.GetAttribute("dst");
								if (!string.IsNullOrEmpty(oldHvo))
								{
									XmlNode removeNode = childElem.OwnerDocument.SelectSingleNode("//*[@Id = '" + oldHvo + "']");
									removeNode.ParentNode.RemoveChild(removeNode);
								}
								removeNodes.Add(curChild);
								curChild = curChild.NextSibling;
							}
						}

						break;
				}

				if (curChild == null)
					break;
			}

			return curChild;
		}

		XmlNode GetNextElement(XmlNode node, string name)
		{
			while (node != null && (node.NodeType != XmlNodeType.Element || node.Name != name))
			{
				node = node.NextSibling;
			}
			return node;
		}

		XmlNode FindOrInsertAllElementObject(int hvo, XmlDocument fxtResult)
		{
			XmlNode allNode = GetResultParentNode(hvo, null, fxtResult, null);
			if (allNode == null)
				return null;

			XmlNode objNode = allNode.SelectSingleNode("*[@Id='" + hvo + "']");
			if (objNode == null)
			{
				XmlDocumentFragment objPropFrag = fxtResult.CreateDocumentFragment();
				objPropFrag.InnerXml = GetDumpResult(CmObject.CreateFromDBObject(m_cache, hvo));
				allNode.AppendChild(objPropFrag);
				objNode = allNode.SelectSingleNode("*[@Id='" + hvo + "']");
			}
			return objNode;
		}

		private XmlNode GetFxtFieldNodeForOwningAtomic(string sFieldName, string ksObjAtomic, string ksGroup, string sClassName, out string sFxtParentName, out FXTElementSearchProperties searchPropsFound)
		{
			string[] asAttributeValues = new string[3];
			asAttributeValues[0] = sFieldName + "OA";
			asAttributeValues[1] = sFieldName + "OAHvo";
			List<FXTElementSearchProperties> searchPropsList = new List<FXTElementSearchProperties>(2);
			FXTElementSearchProperties searchProps =
				new FXTElementSearchProperties(ksAttribute, ksSimpleProperty, asAttributeValues);
			searchPropsList.Add(searchProps);
			searchProps = new FXTElementSearchProperties(ksObjAtomic, "objProperty", asAttributeValues);
			searchPropsList.Add(searchProps);
			searchProps = new FXTElementSearchProperties(ksGroup, "objProperty", asAttributeValues);
			searchPropsList.Add(searchProps);

			string sAttrValueFound;
			return GetFxtFieldNode(sClassName, searchPropsList, out sFxtParentName, out searchPropsFound, out sAttrValueFound);
		}

		private void UpdateReferenceAtomic(string sClassName, int flid, string sFieldName, int hvoItem, XmlDocument fxtResult)
		{
			const string ksRefAtomic = "refAtomic";

			// need to find the correct class as well as the field in the FXT document
			string[] asAttributeValues = new string[2];
			asAttributeValues[0] = sFieldName + "RA";
			asAttributeValues[1] = sFieldName + "RAHvo";
			List<FXTElementSearchProperties> searchPropsList = new List<FXTElementSearchProperties>(2);
			FXTElementSearchProperties searchProps =
				new FXTElementSearchProperties(ksAttribute, ksSimpleProperty, asAttributeValues);
			searchPropsList.Add(searchProps);
			searchProps = new FXTElementSearchProperties("refAtomic", ksSimpleProperty, asAttributeValues);
			searchPropsList.Add(searchProps);

			FXTElementSearchProperties searchPropsFound;
			string sAttrValueFound;
			string sFxtParentName;
			XmlNode fxtFieldNode = GetFxtFieldNode(sClassName, searchPropsList, out sFxtParentName, out searchPropsFound, out sAttrValueFound);
			if (fxtFieldNode == null)
				return; // Not all fields are addressed in the FXT description

			XmlNode resultNode = null;
			XmlNode fxtNameAttr;
			string sNewResultValue = m_cache.GetIntProperty(hvoItem, flid).ToString();
			XmlNode resultParentNode = fxtResult.SelectSingleNode("//" + sFxtParentName + "[@Id='" + hvoItem + "']");
			if (resultParentNode == null)
			{
				XmlNode objResultNode = FindOrInsertAllElementObject(hvoItem, fxtResult);
				if (objResultNode != null)
				{
					resultParentNode = objResultNode.SelectSingleNode("descendant-or-self::" + sFxtParentName);
				}
				else
				{
					// the item does not have an Id; it's just the parent element
					resultParentNode = fxtResult.SelectSingleNode("//" + sFxtParentName);
					if (resultParentNode == null)
						// The object has not been added to the result yet, most likely it is being initialized, so wait for owner to add it
						return;
				}
			}

			if (searchPropsFound.ElementName == ksRefAtomic)
			{
				fxtNameAttr = fxtFieldNode.SelectSingleNode("@name");
				resultNode = resultParentNode.SelectSingleNode(fxtNameAttr.InnerText);
				XmlNode dst = resultNode.SelectSingleNode("@dst");
				dst.InnerText = sNewResultValue;
			}
			else
			{
				fxtNameAttr = fxtFieldNode.SelectSingleNode("@name");
				XmlNode resultAttr = resultParentNode.SelectSingleNode("@" + fxtNameAttr.InnerText);
				resultAttr.InnerText = sNewResultValue;
			}

		}

		private void UpdateOwningVector(string sClassName, int flid, string sFieldName, int hvoItem, int fieldType, XmlDocument fxtResult)
		{
			// hvoItem is the owner and something inside the owning sequence field has changed
			// so we want to find the owner object in the result file and then look for the field items within it.
			// Note: some use refVector when put real items elsewhere
			// Note: others use objVector for when the items are stored right here
			//   Parents (i.e. the owner)
			//      some have <element name=""> ;
			//      others have just an (output) element;
			//      some have just the <class>
			XmlNode resultNode;

			// figure out the name of the element that corresponds to the field
			bool fIsRefVector;
			string sFxtItemLabel;
			string sResultOwningElementName;
			XmlNode fxtFieldNode = GetFxtNodeAndFxtItemLabel(sClassName, flid, sFieldName, fieldType, out sResultOwningElementName, out fIsRefVector, out sFxtItemLabel);
			if (fxtFieldNode == null)
				return;  // Not all fields are addressed in the FXT description file
			string sXPath = "//" + sResultOwningElementName + "[ancestor-or-self::*[@Id='" + hvoItem + "']]";
			resultNode = fxtResult.SelectSingleNode(sXPath); // is the owner
			if (resultNode == null)
			{
				XmlNode objResultNode = FindOrInsertAllElementObject(hvoItem, fxtResult);
				if (objResultNode != null)
				{
					resultNode = objResultNode.SelectSingleNode("descendant-or-self::" + sResultOwningElementName);
				}
				else
				{
					// the item does not have an Id; it's just the parent element
					resultNode = fxtResult.SelectSingleNode("//" + sResultOwningElementName);
					if (resultNode == null)
						// The object has not been added to the result yet, most likely it is being initialized, so wait for owner to add it
						return;
				}
			}

			// get set of field items in the database
			List<int> hvosDatabase = GetHvosInDatabase(hvoItem, flid);

			// get set of field items in the FXT result file
			List<int> hvosResult = GetHvosInFxtResult(resultNode, sFxtItemLabel, fxtFieldNode);

			HandleOwningVectorDeletions(fxtResult, resultNode, hvosResult, hvosDatabase, flid);

			HandleVectorAdditions(fxtResult, resultNode, sFxtItemLabel, hvosResult, hvosDatabase, sResultOwningElementName, fIsRefVector);

			UpdateVectorOrds(resultNode, hvosDatabase);
		}

		private void UpdateReferenceVector(string sClassName, int flid, string sFieldName, int hvoItem, int fieldType, XmlDocument fxtResult)
		{
			XmlNode fxtFieldNode;
			string sResultOwningElementName;
			XmlNode resultNode;

			string[] asAttributeValues = new string[1];
			asAttributeValues[0] = sFieldName;
			FXTElementSearchProperties searchProps =
				new FXTElementSearchProperties(ksRefVector, ksField, asAttributeValues);
			List<FXTElementSearchProperties> searchPropsList = new List<FXTElementSearchProperties>(1);
			searchPropsList.Add(searchProps);
			fxtFieldNode = GetFxtFieldNode(sClassName, searchPropsList, out sResultOwningElementName);
			if (fxtFieldNode == null)
				return; // Not all fields are addressed in the FXT description

			XmlNode itemLabelAttr =  fxtFieldNode.SelectSingleNode("@itemLabel");
			string sFxtItemLabel = itemLabelAttr.InnerText;

			// get the owner object in the fxt result
			string sParentName = GetFxtParentNodeName(fxtFieldNode);
			resultNode = fxtResult.SelectSingleNode("//*[@Id='" + hvoItem + "']"); // is the owner
			if (resultNode == null)
			{
				XmlNode objResultNode = FindOrInsertAllElementObject(hvoItem, fxtResult);
				if (objResultNode != null)
				{
					resultNode = objResultNode.SelectSingleNode("descendant-or-self::" + sResultOwningElementName);
				}
				else
				{
					// the item does not have an Id; it's just the parent element
					resultNode = fxtResult.SelectSingleNode("//" + sResultOwningElementName);
					if (resultNode == null)
						// The object has not been added to the result yet, most likely it is being initialized, so wait for owner to add it
						return;
				}
			}

			// get set of field items in the database
			List<int> hvosDatabase = GetHvosInDatabase(hvoItem, flid);

			// get set of field items in the FXT result file
			List<int> hvosResult = GetHvosInFxtResult(resultNode, sFxtItemLabel, fxtFieldNode);

			HandleReferenceVectorDeletions(fxtResult, resultNode, hvosResult, hvosDatabase);

			HandleVectorAdditions(fxtResult, resultNode, sFxtItemLabel, hvosResult, hvosDatabase, sParentName, true);

			UpdateVectorOrds(resultNode, hvosDatabase);
		}

		private void UpdateVectorOrds(XmlNode resultNode, List<int> hvosDatabase)
		{
			for (int i = 0; i < hvosDatabase.Count; i++)
			{
				string sDstOrIdXPath = "[@dst='" + hvosDatabase[i] + "' or @Id='" + hvosDatabase[i] + "']";
				XmlNode node = resultNode.SelectSingleNode("descendant-or-self::*" + sDstOrIdXPath);
				if (node != null)
				{
					XmlAttribute attr = node.Attributes["ord"];
					if (attr != null)
						attr.Value = i.ToString();
				}
			}
		}

		private void HandleVectorAdditions(XmlDocument fxtResult, XmlNode resultNode, string sFxtItemLabel, List<int> hvosResult, List<int> hvosDatabase, string sParentName, bool fIsRefVector)
		{
			string sOriginalParentName = sParentName;
			// if the item is not in the fxt result but is in the database, need to add it to the fxt result
			int prevHvo = 0;
			foreach (int hvo in hvosDatabase)
			{
				if (!hvosResult.Contains(hvo))
				{
					// item is in the database, but not in the FXT result, so need to add it
					ICmObject obj = CmObject.CreateFromDBObject(m_cache, hvo);
					bool addItem = true;
					if (fIsRefVector)
					{
						InsertReferencedItemInResult(hvo, resultNode, sParentName, sFxtItemLabel, obj);
						XmlNode node = fxtResult.SelectSingleNode("//*[@Id='" + hvo + "']");
						if (node != null)
							addItem = false; // it is already there; no need to add the real item
						sParentName = null;
					}

					if (addItem)
					{
						XmlNode resultParentNode = GetResultParentNode(hvo, sParentName, fxtResult, resultNode);
						XmlDocumentFragment frag = fxtResult.CreateDocumentFragment();
						frag.InnerXml = GetDumpResult(obj);

						if (prevHvo != 0)
						{
							string sDstOrIdXPath = "[@dst='" + prevHvo + "' or @Id='" + prevHvo + "']";
							XmlNode insertNode = resultParentNode.SelectSingleNode("descendant-or-self::*" + sDstOrIdXPath);
							resultParentNode.InsertAfter(frag, insertNode);
						}
						else if (hvosResult.Count > 0)
						{
							string sDstOrIdXPath = "[@dst='" + hvosResult[0] + "' or @Id='" + hvosResult[0] + "']";
							XmlNode insertNode = resultParentNode.SelectSingleNode("descendant-or-self::*" + sDstOrIdXPath);
							resultParentNode.InsertBefore(frag, insertNode);
						}
						else
						{
							resultParentNode.AppendChild(frag);
						}
					}

					sParentName = sOriginalParentName; // make sure it's back to the original
				}

				prevHvo = hvo;
			}
		}

		/// <summary>
		/// Gets the name of the parent node in the result based on the target FXT node
		/// </summary>
		/// <param name="fxtFieldNode"></param>
		/// <returns></returns>
		private string GetFxtParentNodeName(XmlNode fxtFieldNode)
		{
			XmlNode fxtFieldParentElementNode;
			XmlNode fxtNameAttr;
			// need to know where to insert it (use sParentName)
			string sParentName;
			fxtFieldParentElementNode = fxtFieldNode.ParentNode;
			if (fxtFieldParentElementNode == null)
				throw new XUpdaterException("Cannot find parent node to use in FXT while adding a new item.");
			if (fxtFieldParentElementNode.LocalName == "element")
			{
				fxtNameAttr = fxtFieldParentElementNode.SelectSingleNode("@name");
				sParentName = fxtNameAttr.InnerText;
			}
			else
			{
				sParentName = fxtFieldParentElementNode.LocalName;
			}
			return sParentName;
		}

		private void InsertReferencedItemInResult(int hvo, XmlNode resultNode, string sParentName, string sFxtItemLabel, ICmObject obj)
		{
			XmlNode ownerNode = resultNode.SelectSingleNode("descendant-or-self::" + sParentName);
			XmlElement newRefElement = XmlUtils.AppendElement(ownerNode, sFxtItemLabel);
			XmlUtils.AppendAttribute(newRefElement, "dst", hvo.ToString());
			XmlUtils.AppendAttribute(newRefElement, "ord", obj.OwnOrd.ToString());
		}

		private XmlNode GetResultParentNode(int hvo, string sParentName, XmlDocument fxtResult, XmlNode resultNode)
		{
			XmlNode resultParentNode;
			int classid = m_cache.GetClassOfObject(hvo);
			if (sParentName == null)
			{
				string sFxtAllPattern = "";
				switch (classid)
				{
					case MoAffixAllomorph.kclsidMoAffixAllomorph: // fall through
					case MoStemAllomorph.kclsidMoStemAllomorph:
					case MoAffixProcess.kclsidMoAffixProcess:
						sFxtAllPattern = "AllAllomorphs";
						break;
					case MoDerivAffMsa.kclsidMoDerivAffMsa: // fall through
					case MoInflAffMsa.kclsidMoInflAffMsa:   // fall through
					case MoStemMsa.kclsidMoStemMsa:         // fall through
					case MoUnclassifiedAffixMsa.kclsidMoUnclassifiedAffixMsa:
						sFxtAllPattern = "AllMSAs";
						break;
					case LexSense.kclsidLexSense:
						sFxtAllPattern = "AllSenses";
						break;
					default:
						return null;
				}
				XmlNode fxtOwnerNode = FxtDocument.SelectSingleNode("//*[objVector[@objProperty='" + sFxtAllPattern + "']]");
				resultParentNode = fxtResult.SelectSingleNode("//" + fxtOwnerNode.LocalName);
			}
			else
			{
				if (resultNode != null)
					resultParentNode = resultNode.SelectSingleNode("ancestor-or-self::" + sParentName);
				else
					resultParentNode = fxtResult.SelectSingleNode("//" + sParentName);
			}
			return resultParentNode;
		}

		private string GetDumpResult(ICmObject obj)
		{
			MemoryStream ms = new MemoryStream();
			StreamWriter sw = new StreamWriter(ms);
			StreamReader sr = new StreamReader(ms);

			DumpObject(sw, (CmObject)obj, null);
			sw.Flush();
			ms.Seek(0, SeekOrigin.Begin);
			string sDumpResult = sr.ReadToEnd();
			sw.Close();
			sr.Close();
			return sDumpResult.Trim();
		}

		private void HandleOwningVectorDeletions(XmlDocument fxtResult, XmlNode resultNode, List<int> hvosResult, List<int> hvosDatabase, int flid)
		{
			// if the item is no longer in the database, need to remove it from the fxt result
			foreach (int hvo in hvosResult.ToArray())
			{
				if (!hvosDatabase.Contains(hvo))
				{
					string sDstOrIdXPath = "[@dst='" + hvo + "' or @Id='" + hvo + "']";
					XmlNode nodeToDelete = resultNode.SelectSingleNode("descendant-or-self::*" + sDstOrIdXPath);
					List<uint[]> listOfFlids = new List<uint[]>();
					GetFieldIdsOfClass(flid, ref listOfFlids);

					HandleDaughterDeletions(nodeToDelete, fxtResult, listOfFlids);
					XmlNodeList resultNodes = fxtResult.SelectNodes("//*" + sDstOrIdXPath);
					DeleteResultNodes(resultNodes);
					hvosResult.Remove(hvo);
				}
			}
		}

		private void GetFieldIdsOfClass(int flid, ref List<uint[]> listOfFlids)
		{
			uint classId = m_mdc.GetDstClsId((uint)flid);
			uint[] flids = DbOps.GetFieldsInClassOfType(m_mdc, (int)classId, FieldType.kgrfcptOwning);
			if (flids.Length > 0)
				listOfFlids.Add(flids);
			GetFieldIdsOfSubClasses(classId, listOfFlids);
			return;
		}

		private void GetFieldIdsOfSubClasses(uint classId, List<uint[]> listOfFlids)
		{
			int directSubclassCount;
			m_mdc.GetDirectSubclasses(classId, 0, out directSubclassCount, null);
			if (directSubclassCount == 0)
				return;
			uint[] uSubclassIds;
			using (ArrayPtr clids = MarshalEx.ArrayToNative(directSubclassCount, typeof(uint)))
			{
				m_mdc.GetDirectSubclasses(classId, directSubclassCount, out directSubclassCount, clids);
				uSubclassIds = (uint[])MarshalEx.NativeToArray(clids, directSubclassCount, typeof(uint));
			}
			foreach (uint uClassId in uSubclassIds)
			{
				uint[] subclassFlids = DbOps.GetFieldsInClassOfType(m_mdc, (int)uClassId, FieldType.kgrfcptOwning);
				if (subclassFlids.Length > 0)
					listOfFlids.Add(subclassFlids);
				GetFieldIdsOfSubClasses(uClassId, listOfFlids);
			}
		}

		private void HandleDaughterDeletions(XmlNode resultNode, XmlDocument fxtResult, List<uint[]> listOfFlids)
		{
			if (listOfFlids.Count == 0 || resultNode.ChildNodes.Count == 0)
				return; // nothing to do because there are no owning fields or no daughter nodes
			List<XmlNodeList> nodesToDeleteList = new List<XmlNodeList>();
			foreach (XmlNode daughterNode in resultNode.ChildNodes)
			{
				int flid;
				if (IsOwningField(daughterNode, listOfFlids, out flid))
				{
					List<uint[]> daughterListOfFlids = new List<uint[]>();
					GetFieldIdsOfClass(flid, ref daughterListOfFlids);

					HandleDaughterDeletions(daughterNode, fxtResult, daughterListOfFlids);

					XmlNode dstOrId = daughterNode.SelectSingleNode("@dst | @Id");
					if (dstOrId == null)
						continue; // skip it
					string hvo = dstOrId.InnerText;
					XmlNodeList resultNodes = fxtResult.SelectNodes("//*[@dst='" + hvo + "' or @Id='" + hvo + "']");
					// remember the nodes to delete and then delete them later; if we delete them now, we delete a node in the foreach loop!
					nodesToDeleteList.Add(resultNodes);
				}
			}
			foreach (XmlNodeList nodesToDelete in nodesToDeleteList)
			{
				DeleteResultNodes(nodesToDelete);
			}
		}

		private void DeleteResultNodes(XmlNodeList resultNodes)
		{
			foreach (XmlNode node in resultNodes)
			{
				if (node == null)
					continue;
				XmlNode parent = node.ParentNode;
				parent.RemoveChild(node);
			}
		}

		private bool IsOwningField(XmlNode resultNode, List<uint[]> listOfFlids, out int flid)
		{
			foreach (uint[] flids in listOfFlids)
			{
				foreach (uint nestedFlid in flids)
				{
					string sFieldName = m_mdc.GetFieldName(nestedFlid);
					string sClassName = m_mdc.GetOwnClsName(nestedFlid);
					string sResultOwningElementName;
					string sResultElementName;
					FXTElementSearchProperties searchPropsFound;
					XmlNode fxtFieldNode;

					int fieldType = m_mdc.GetFieldType(nestedFlid);
					if (fieldType == (int) CellarModuleDefns.kcptOwningAtom)
					{
						fxtFieldNode =
							GetFxtFieldNodeForOwningAtomic(sFieldName, ksObjAtomic, ksGroup, sClassName,
														   out sResultOwningElementName, out searchPropsFound);
						sResultElementName = sResultOwningElementName;
						if (fxtFieldNode == null)
							continue;
						if (searchPropsFound.ElementName == ksGroup)
						{
							XmlNode elementNode = fxtFieldNode.SelectSingleNode("element");
							if (elementNode == null)
								continue;
							XmlNode nameAttr = elementNode.SelectSingleNode("@name");
							if (nameAttr == null)
								continue;
							sResultElementName = nameAttr.InnerText;
						}
					}
					else
					{
						bool fIsRefVector;
						string sFxtItemLabel;
						fxtFieldNode =
							GetFxtNodeAndFxtItemLabel(sClassName, (int) nestedFlid, sFieldName, fieldType,
													  out sResultOwningElementName, out fIsRefVector, out sFxtItemLabel);
						sResultElementName = sFxtItemLabel;
					}
					if (fxtFieldNode != null && sResultElementName == resultNode.LocalName)
					{
						flid = (int) nestedFlid;
						return true;
					}
				}
			}

			flid = 0;
			return false;
		}

		private void HandleReferenceVectorDeletions(XmlDocument fxtResult, XmlNode resultNode, List<int> hvosResult, List<int> hvosDatabase)
		{
			// if the item is no longer in the database, need to remove it from the fxt result
			foreach (int hvo in hvosResult.ToArray())
			{
				if (!hvosDatabase.Contains(hvo))
				{
					XmlNodeList resultNodes = resultNode.SelectNodes("*[@dst='" + hvo + "']");
					// item is in the FXT result, but not in the database; delete all occurences of it
					foreach (XmlNode node in resultNodes)
					{
						XmlNode parent = node.ParentNode;
						parent.RemoveChild(node);
					}
					hvosResult.Remove(hvo);
				}
			}
		}


		private List<int> GetHvosInFxtResult(XmlNode resultNode, string sFxtItemLabel, XmlNode fxtNode)
		{
			XmlNodeList resultNodes;
			if (fxtNode.PreviousSibling == null && fxtNode.NextSibling == null)
			{ // just get all the nodes
				resultNodes = resultNode.ChildNodes;
			}
			else
			{  // make sure we get just the relevant nodes
				resultNodes = resultNode.SelectNodes(sFxtItemLabel);
			}

			List<int> hvosResult = new List<int>(resultNodes.Count);
			foreach (XmlNode node in resultNodes)
			{
				if (node.NodeType != XmlNodeType.Element)
					continue;
				XmlNode dstOrId = node.SelectSingleNode("@Id");
				if (dstOrId == null)
					dstOrId = node.SelectSingleNode("@dst");
				if (dstOrId != null)
					hvosResult.Add(Convert.ToInt32(dstOrId.InnerText));
			}
			return hvosResult;
		}

		private XmlNode GetFxtNodeAndFxtItemLabel(string sClassName, int flid, string sFieldName, int fieldType, out string sResultOwningElementName,
			out bool fIsRefVector, out string sFxtItemLabel)
		{
			const string ksObjVector = "objVector";
			string sObjFieldType = "OS";
			if (fieldType == (int) CellarModuleDefns.kcptOwningCollection)
				sObjFieldType = "OC";

			string[] asAttributeValues = new string[1];
			asAttributeValues[0] = sFieldName + sObjFieldType;
			FXTElementSearchProperties searchProps =
				new FXTElementSearchProperties(ksObjVector, "objProperty", asAttributeValues);
			List<FXTElementSearchProperties> searchPropsList = new List<FXTElementSearchProperties>(2);
			searchPropsList.Add(searchProps);
			string[] asAttributeValues2 = new string[1];
			asAttributeValues2[0] = sFieldName;
			FXTElementSearchProperties searchProps2 = new FXTElementSearchProperties(ksRefVector, ksField, asAttributeValues2);
			searchPropsList.Add(searchProps2);

			string sAttrValueFound;
			FXTElementSearchProperties searchPropsFound;
			XmlNode fxtFieldNode =
				GetFxtFieldNode(sClassName, searchPropsList, out sResultOwningElementName, out searchPropsFound,
								out sAttrValueFound);
			if (fxtFieldNode == null)
			{
				sResultOwningElementName = "";
				fIsRefVector = false;
				sFxtItemLabel = "";
				return null; // Not all fields are addressed in the FXT description
			}

			if (searchPropsFound.ElementName == ksObjVector)
			{
				fIsRefVector = false;
				// want the signature of the field
				uint destClassId = m_cache.GetDestinationClass((uint) flid);

				if (m_cache.GetAbstract((int)destClassId))
				{
					int iCount = 0;
					StringBuilder sbSubClasses = new StringBuilder();
					GetNamesOfSubClasses(destClassId, sbSubClasses, iCount);
					sFxtItemLabel = sbSubClasses.ToString();
				}
				else
					sFxtItemLabel = m_cache.GetClassName(destClassId);
			}
			else
			{
				fIsRefVector = true;
				XmlNode fxtItemLabelAttr = fxtFieldNode.SelectSingleNode("@itemLabel");
				sFxtItemLabel = fxtItemLabelAttr.InnerText;
			}
			return fxtFieldNode;
		}

		private void GetNamesOfSubClasses(uint destClassId, StringBuilder sbSubClasses, int iCount)
		{
			int directSubclassCount;
			m_mdc.GetDirectSubclasses(destClassId, 0, out directSubclassCount, null);
			if (directSubclassCount == 0)
				return;
			uint[] uSubclassIds;
			using (ArrayPtr clids = MarshalEx.ArrayToNative(directSubclassCount, typeof (uint)))
			{
				m_mdc.GetDirectSubclasses(destClassId, directSubclassCount, out directSubclassCount, clids);
				uSubclassIds = (uint[]) MarshalEx.NativeToArray(clids, directSubclassCount, typeof (uint));
			}
			foreach (uint uClassId in uSubclassIds)
			{
				if (iCount > 0)
				sbSubClasses.Append(" | ");
				iCount++;
				sbSubClasses.Append(m_cache.GetClassName(uClassId));
				GetNamesOfSubClasses(uClassId, sbSubClasses, iCount);
			}
		}

		private List<int> GetHvosInDatabase(int hvoItem, int flid)
		{
			// need to deal with special cases - ignore invalid items
			if (flid == (int)PhPhonData.PhPhonDataTags.kflidEnvironments)
				return new List<int>(PhEnvironment.ValidEnvironments(m_cache));

			int[] hvos = m_cache.GetVectorProperty(hvoItem, flid, false);
			List<int> hvosDatabase = new List<int>(hvos);
			return hvosDatabase;
		}

		/// <summary>
		/// Find the element node in the FXT description file that is for this class and field
		/// </summary>
		/// <param name="sClassName">the class</param>
		/// <param name="searchProps"></param>
		/// <returns></returns>
		private XmlNode GetFxtFieldNode(string sClassName, List<FXTElementSearchProperties> searchProps)
		{
			FXTElementSearchProperties searchPropsFound;
			string sAttrValuefound;
			string sResultOwningElementName;
			return GetFxtFieldNode(sClassName, searchProps, out sResultOwningElementName, out searchPropsFound, out sAttrValuefound);
		}

		/// <summary>
		/// Find the element node in the FXT description file that is for this class and field
		/// </summary>
		/// <param name="sClassName">the class</param>
		/// <param name="searchProps">The search props.</param>
		/// <param name="sResultOwningElementName">Name of the owning element in the result.</param>
		/// <returns></returns>
		private XmlNode GetFxtFieldNode(string sClassName, List<FXTElementSearchProperties> searchProps, out string sResultOwningElementName)
		{
			FXTElementSearchProperties searchPropsFound;
			string sAttrValuefound;
			return GetFxtFieldNode(sClassName, searchProps, out sResultOwningElementName, out searchPropsFound, out sAttrValuefound);
		}


		/// <summary>
		/// Find the element node in the FXT description file that is for this class and field
		/// </summary>
		/// <param name="sClassName">the class</param>
		/// <param name="searchProps">The search props.</param>
		/// <param name="sResultOwningElementName">Name of the owning element in the result.</param>
		/// <param name="searchPropsFound">The search props found.</param>
		/// <param name="sAttrValueFound">The search attribute value found.</param>
		/// <returns></returns>
		private XmlNode GetFxtFieldNode(string sClassName, List<FXTElementSearchProperties> searchProps, out string sResultOwningElementName,
			out FXTElementSearchProperties searchPropsFound, out string sAttrValueFound)
		{
			XmlNode fieldNode;
			// find the class
			XmlNode classNode = FxtDocument.SelectSingleNode("//class[@name='" + sClassName + "']");
			if (classNode == null)
				throw new XUpdaterException("Could not find class " + sClassName + " in FXT document " + m_sTemplateFilePath + " while getting field node.");
			// for the various element/attribute/attribute values combinations, try to find the element here
			foreach (FXTElementSearchProperties prop in searchProps)
			{
				foreach (string sValue in prop.AttributeValues)
				{
					string sXPath = "descendant-or-self::" + prop.ElementName + "[@" + prop.AttributeName + "='" + sValue + "']";
					fieldNode = classNode.SelectSingleNode(sXPath);
					if (fieldNode != null)
					{
						searchPropsFound = prop;
						sAttrValueFound = sValue;
						sResultOwningElementName = GetFxtParentNodeName(fieldNode);
						return fieldNode;
					}
				}
			}
			// did not find it;  look in any call elements
			XmlNodeList callNodes = classNode.SelectNodes("descendant-or-self::call");
			foreach (XmlNode callNode in callNodes)
			{
				XmlNode nameAttr = callNode.SelectSingleNode("@name");
				fieldNode = GetFxtFieldNode(nameAttr.InnerText, searchProps, out sResultOwningElementName, out searchPropsFound, out sAttrValueFound);
				if (fieldNode != null)
				{
					XmlNode flagsAttr = callNode.SelectSingleNode("@flags");
					// assume element name for a class will always be the class name
					if (flagsAttr != null && flagsAttr.InnerText == "NoWrapper" && sResultOwningElementName == nameAttr.InnerText)
						sResultOwningElementName = GetFxtParentNodeName(callNode);
					return fieldNode;
				}
			}
			searchPropsFound = null;
			sAttrValueFound = null;
			sResultOwningElementName = null;
			return null;
		}
		private void UpdateInteger(string sClassName, string sFieldName, int hvoItem, XmlDocument fxtResult)
		{
			XmlNode fxtFieldNode;
			string sResultOwningElementName;
			XmlNode resultNode;

			string[] asAttributeValues = new string[1];
			asAttributeValues[0] = sFieldName;
			FXTElementSearchProperties searchProps =
				new FXTElementSearchProperties(ksAttribute, ksSimpleProperty, asAttributeValues);
			List<FXTElementSearchProperties> searchPropsList = new List<FXTElementSearchProperties>(1);
			searchPropsList.Add(searchProps);
			fxtFieldNode = GetFxtFieldNode(sClassName, searchPropsList, out sResultOwningElementName);
			if (fxtFieldNode == null)
				return; // Not all fields are addressed in the FXT description

			ICmObject obj = CmObject.CreateFromDBObject(m_cache, hvoItem);
			string x = GetSimplePropertyString(fxtFieldNode, (CmObject)obj);
			XmlNode fxtNameAttr = fxtFieldNode.SelectSingleNode("@name");
			resultNode = fxtResult.SelectSingleNode("//" + sResultOwningElementName + "[@Id='" + hvoItem + "']");
			if (resultNode == null)
			{
				resultNode = FindOrInsertAllElementObject(hvoItem, fxtResult);
				if (resultNode == null)
					// The object has not been added to the result yet, most likely it is being initialized, so wait for owner to add it
					return;
			}

			XmlNode resultAttrNode = resultNode.SelectSingleNode("@" + fxtNameAttr.InnerText);
			resultAttrNode.InnerText = x;
		}

		private void UpdateMultiUnicode(string sClassName, string sFieldName, int hvoItem, XmlDocument fxtResult)
		{
			XmlNode fxtFieldNode;
			XmlNode fxtFieldParentElementNode;
			XmlNode fxtNameAttr;
			string sXPath;
			XmlNode resultNode;

			string[] asAttributeValues = new string[1];
			asAttributeValues[0] = sFieldName;
			FXTElementSearchProperties searchProps =
				new FXTElementSearchProperties("string", ksSimpleProperty, asAttributeValues);
			List<FXTElementSearchProperties> searchPropsList = new List<FXTElementSearchProperties>(1);
			searchPropsList.Add(searchProps);

			fxtFieldNode = GetFxtFieldNode(sClassName, searchPropsList);
			if (fxtFieldNode == null)
				return; // Not all fields are addressed in the FXT description

			fxtFieldParentElementNode = fxtFieldNode.SelectSingleNode("parent::element");
			fxtNameAttr = fxtFieldParentElementNode.SelectSingleNode("@name");
			sXPath = "//" + fxtNameAttr.InnerText + "[parent::*[@Id='" + hvoItem + "']]";

			resultNode = fxtResult.SelectSingleNode(sXPath);
			if (resultNode == null)
			{
				XmlNode objResultNode = FindOrInsertAllElementObject(hvoItem, fxtResult);
				if (objResultNode != null)
				{
					resultNode = objResultNode.SelectSingleNode(fxtNameAttr.InnerText);
				}
				else
				{
					// The object has not been added to the result yet, most likely it is being initialized, so wait for owner to add it
					return;
				}
			}
			ICmObject obj = CmObject.CreateFromDBObject(m_cache, hvoItem);
			string x = GetSimplePropertyString(fxtFieldNode, (CmObject) obj);
			resultNode.InnerText = x;
		}

		private void UpdateString(string sClassName, string sFieldName, int hvoItem, XmlDocument fxtResult)
		{
			XmlNode fxtFieldNode;
			XmlNode fxtFieldParentElementNode;
			XmlNode fxtNameAttr = null;
			string sXPath;
			XmlNode resultNode;
			const string ksXmlstring = "xmlstring";

			string[] asAttributeValues = new string[1];
			asAttributeValues[0] = sFieldName;
			FXTElementSearchProperties searchProps =
				new FXTElementSearchProperties(ksAttribute, ksSimpleProperty, asAttributeValues);
			List<FXTElementSearchProperties> searchPropsList = new List<FXTElementSearchProperties>(2);
			searchPropsList.Add(searchProps);
			string[] asAttributeValues2 = new string[1];
			asAttributeValues[0] = sFieldName;
			FXTElementSearchProperties searchProps2 =
				new FXTElementSearchProperties(ksXmlstring, ksSimpleProperty, asAttributeValues);
			searchPropsList.Add(searchProps2);

			string sResultOwningElementName;
			FXTElementSearchProperties searchPropsFound;
			string sAttrValueFound;
			fxtFieldNode = GetFxtFieldNode(sClassName, searchPropsList, out sResultOwningElementName, out searchPropsFound, out sAttrValueFound);
			if (fxtFieldNode == null)
				return; // Not all fields are addressed in the FXT description

			if (searchPropsFound.ElementName == ksXmlstring)
			{
				// assume the owner is unique and is the field name (e.g. ParserParameters in M3Parser.fxt)
				sXPath = "//" + sFieldName;
			}
			else
			{
				fxtNameAttr = fxtFieldNode.SelectSingleNode("@name");
				string sResultAttrName = fxtNameAttr.InnerText;

				fxtFieldParentElementNode = fxtFieldNode.SelectSingleNode("parent::element");
				XmlNode fxtParentNameAttr = fxtFieldParentElementNode.SelectSingleNode("@name");
				sXPath = "//" + fxtParentNameAttr.InnerText + "[@Id='" + hvoItem + "']/@" + sResultAttrName;
			}

			resultNode = fxtResult.SelectSingleNode(sXPath);
			if (resultNode == null)
			{
				XmlNode objNode = FindOrInsertAllElementObject(hvoItem, fxtResult);
				if (objNode != null && fxtNameAttr != null)
				{
					resultNode = objNode.SelectSingleNode("@" + fxtNameAttr.InnerText);
				}
				else
				{
					// The object has not been added to the result yet, most likely it is being initialized, so wait for owner to add it
					return;
				}
			}

			ICmObject obj = CmObject.CreateFromDBObject(m_cache, hvoItem);
			string x = GetSimplePropertyString(fxtFieldNode, (CmObject) obj);
			if (searchPropsFound.ElementName == ksXmlstring)
			{
				string sDumpResult = GetXmlStringDumpResult(fxtFieldNode, obj);
				string sInnerXml = GetInnerXml(sDumpResult);
				resultNode.InnerXml = sInnerXml;
			}
			else
				resultNode.InnerText = x;
		}

		private string GetInnerXml(string sDumpResult)
		{
			// skip past the first element
			int iInnerBegin = sDumpResult.IndexOf(">") + 1;
			// N.B. following assumes there is inner XML!
			int iInnerEnd = sDumpResult.LastIndexOf("</");
			int iInnerLength = iInnerEnd - iInnerBegin;
			return sDumpResult.Substring(iInnerBegin, iInnerLength);
		}

		private string GetXmlStringDumpResult(XmlNode fxtFieldNode, ICmObject obj)
		{
			MemoryStream ms = new MemoryStream();
			StreamWriter sw = new StreamWriter(ms);
			StreamReader sr = new StreamReader(ms);

			DoXmlStringOutput(sw, (CmObject) obj, fxtFieldNode);
			sw.Flush();
			ms.Seek(0, SeekOrigin.Begin);
			string sDumpResult = sr.ReadToEnd();
			sw.Close();
			sr.Close();
			return sDumpResult;
		}
	}
	public class FXTElementSearchProperties
	{
		private string m_sElementName;
		private string m_sAttributeName;
		private string[] m_saAttributeValues;

		public FXTElementSearchProperties(string sElementName, string sAttributeName, string[] saAttributeValues)
		{
			m_sElementName = sElementName;
			m_sAttributeName = sAttributeName;
			m_saAttributeValues = saAttributeValues;
		}
		/// <summary>
		/// Get the attribute name
		/// </summary>
		public string AttributeName
		{
			get
			{
				CheckDisposed();
				return m_sAttributeName;
			}
		}
		/// <summary>
		/// Get the attribute values
		/// </summary>
		public string[] AttributeValues
		{
			get
			{
				CheckDisposed();
				return m_saAttributeValues;
			}
		}
		/// <summary>
		/// Get the element name
		/// </summary>
		public string ElementName
		{
			get
			{
				CheckDisposed();
				return m_sElementName;
			}
		}

		#region IDisposable & Co. implementation
		// Region last reviewed: never

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		/// True, if the object has been disposed.
		/// </summary>
		private bool m_isDisposed = false;

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed
		{
			get { return m_isDisposed; }
		}

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~FXTElementSearchProperties()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// <summary>
		///
		/// </summary>
		/// <remarks>Must not be virtual.</remarks>
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		private void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{

				// Dispose managed resources here.
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

	}
	public class XUpdaterException : ApplicationException
	{
		public XUpdaterException(string sMessage) :  base(sMessage)
		{

		}
	}
}
