using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Data;
using System.Xml;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using System.Diagnostics;

namespace ConvertLib
{
	/// <summary>
	/// Stream reader that will change control charaters to spaces. Will prevent conversion of projects
	/// with control characters in the text from failing. Could quote the characters, but they are not
	/// likely to be really wanted in the text.
	/// </summary>
	public class FilteredStreamReader : StreamReader
	{
		public FilteredStreamReader(string path) : base(path)
		{
		}

		/// <summary>
		/// Overrides read to filter out control characters.
		/// </summary>
		public override int Read()
		{
			return (int)ConvertControlCharToSpace((char)base.Read());
		}

		/// <summary>
		/// Overrides read to filter out control characters.
		/// </summary>
		public override int Read(char[] buffer, int index, int count)
		{
			int readLen = base.Read(buffer, index, count);
			for (int i = index; i < index + readLen; i++)
				buffer[i] = ConvertControlCharToSpace(buffer[i]);
			return readLen;
		}

		/// <summary>
		/// Overrides read to filter out control characters.
		/// </summary>
		public override string ReadLine()
		{
			string nextLine = base.ReadLine();
			StringBuilder buffer = new StringBuilder(nextLine.Length);
			foreach (char c in nextLine)
				buffer.Append(ConvertControlCharToSpace(c));
			return buffer.ToString();
		}

		private char ConvertControlCharToSpace(char c)
		{
			if (c < ' ' && c != '\n' && c != '\r')
				return ' ';
			return c;
		}
	}

	public class Convert
	{
		public string m_FileName;
		public string m_ModelName;
		public string m_OutFileName;
		public int nodeValue = 0, holdClassNum = 0;
		public string nodeClass = "", holdNode = "", holdClassName = "", holdVersion = "";
		public bool StartClass = false, StartElem = false, ClosedElem = false, CustomFlag = false;
		public bool PreserveSpace = false;
		public Stack<string> guidStack = new Stack<string>();
		public Dictionary<string, int> oOrdList = new Dictionary<string, int>();
		public List<Custom> CustomList = new List<Custom>();
		public StringCollection elemDEntry = new StringCollection();
		public Stack<string> stackElements = new Stack<string>();
		public Stack<RTClass> stackClasses = new Stack<RTClass>();
		public List<RTClass> listClasses = new List<RTClass>();
		public RTClass currentRT = new RTClass();
		public RTClass langProjClass = new RTClass();
		public Classes hClass = new Classes();
		public Dictionary<string, Classes> classList = new Dictionary<string, Classes>();
		public Dictionary<int, Dictionary<int, Classes>> modList = new Dictionary<int, Dictionary<int, Classes>>();
		public Dictionary<int, Classes> dicClass = new Dictionary<int, Classes>();

		public void Conversion()
		{
			string[] vLines = new string[5]
			{
				"This database is generated from the Converter utility",
				"The conversion is based on the version 7 Data model",
				"It takes in 2 parameters",
				"    Parameter 1 The database (in XML) to be converted",
				"    Parameter 2 The output file"
			};

			m_ModelName = GetModelName();

			if (File.Exists(m_FileName) == false)
			{
				throw new Exception("*** ERROR: Can't open the input file: " + m_FileName + ".");
			}

			else if (File.Exists(m_ModelName) == false)
			{
				throw new Exception("*** ERROR: Can't open the model file: " + m_ModelName + ".");
			}

			else if (m_FileName.IndexOf(".xml") != m_FileName.Length - 4)
			{
				throw new Exception("*** ERROR: The input filename must end in .xml " + m_FileName + ".");
			}

			else if (m_ModelName.IndexOf(".xml") != m_ModelName.Length - 4)
			{
				throw new Exception("*** ERROR: The model filename must end in .xml " + m_ModelName + ".");
			}

			else if (m_OutFileName.IndexOf(".fwdata") != m_OutFileName.Length - 7)
			{
				throw new Exception("*** ERROR: The output filename must end in .fwdata " + m_OutFileName + ".");
			}

			else if (m_OutFileName == m_FileName)
			{
				throw new Exception("*** ERROR: The input and output filenames must be different. Input: " + m_FileName + " Output: "+ m_OutFileName + ".");
			}

			// create reader & open input files
			else
			{
				//Parse the model file into modList and classList
				XmlTextReader modFile = new XmlTextReader(m_ModelName);
				modFile.WhitespaceHandling = WhitespaceHandling.None;

				ProcessModel(modFile);

				//Output File
				XmlTextWriter xmlOutput = new XmlTextWriter(m_OutFileName, null);
				xmlOutput.Formatting = Formatting.Indented;
				xmlOutput.WriteProcessingInstruction("xml", "version=\"1.0\" encoding=\"utf-8\"");

				for (int j = 0; j < 5; j++)
					xmlOutput.WriteComment(vLines[j]);

				//Input File
				XmlTextReader inFile = new XmlTextReader(new FilteredStreamReader(m_FileName));
				inFile.WhitespaceHandling = WhitespaceHandling.All;

				ReadInputFile(inFile);

				while (!inFile.EOF)
				{
					XmlNodeType type = inFile.NodeType;
					if (type != XmlNodeType.Whitespace)
						PreserveSpace = false;
					switch (type)
					{
						case XmlNodeType.Comment:
							break;
						case XmlNodeType.ProcessingInstruction:
							break;
						case XmlNodeType.XmlDeclaration:
							break;
						case XmlNodeType.Element:
							holdNode = inFile.Name;
							PreserveSpace = (holdNode == "Run");
							ProcessElement(inFile, xmlOutput);
							break;
						case XmlNodeType.EndElement:
							holdNode = inFile.Name;
							ProcessEndElement(inFile, xmlOutput);
							break;
						case XmlNodeType.Text:
							ProcessText(inFile, xmlOutput);
							break;
						case XmlNodeType.Whitespace:
							if (PreserveSpace)
							{
								ProcessText(inFile, xmlOutput);
								PreserveSpace = false;
							}
							break;
						case XmlNodeType.DocumentType:
							//WriteDocTypeOut(inFile, xmlOutput);
							break;
						default:
							throw new Exception("Unrecognized XML Node Type is " + type.ToString());

					} //switch statement

					ReadInputFile(inFile);
				} // while read
				//Closing Tags
				inFile.Close();
				xmlOutput.Flush();
				xmlOutput.Close();
			}
		}

		private static void ReadInputFile(XmlTextReader inFile)
		{
			try
			{
				inFile.Read();
			}
			catch (FileNotFoundException)
			{
				// references to DTD files in XML won't be found, just skip them.
				ReadInputFile(inFile);
			}
		}

		private void ProcessElement(XmlTextReader inFile, XmlTextWriter xmlOutput)
		{
			string saveGuid = "";

			if (holdNode == "CustomField" || holdNode == "AdditionalFields")
				return;

			else if (holdNode == "FwDatabase")		// write the root element
			{
				xmlOutput.WriteStartElement("languageproject");
				WriteXMLAttribute("version", holdVersion, xmlOutput);

				if (holdVersion == null)
				{
					throw new Exception("The version attribute was not found in the root node of the model file " + m_ModelName);
				}
				// Make an initial pass to process the Custom Fields
				// Flag will be false if there aren't any.
				if (ProcessCustomFields() == true)
				{
					WriteCustomHeaders(xmlOutput);
				}
			}

			else if (inFile.GetAttribute("id") != null)		// write an rt element
			{

				if (stackElements.Count > 0)
				{
					if (stackClasses.Count == 0)
						currentRT = langProjClass;

					PopPastSubentries();
					inFile.MoveToAttribute("id");
					//inFile.Value; // "I983B657C-9F1E-4A96-999A-CE200EA01302")
					if (stackElements.Count > 0 && currentRT.elementList.TryGetValue(int.Parse(GetElementNumber(stackElements.Peek())), out elemDEntry) != true)
					{
						elemDEntry = new StringCollection();
						currentRT.elementList.Add(int.Parse(GetElementNumber(stackElements.Peek())), elemDEntry);
					}
					elemDEntry.Add("<objsur t=\"o\" guid=\"" + inFile.Value.Substring(1) + "\"/>");
				}
				CreateRtElement(inFile, xmlOutput);
			}

			else if (holdNode == "Link")		// write a link element
			{
				inFile.MoveToAttribute("target");
				saveGuid = inFile.Value.Substring(1);
				PopPastSubentries();
				if (currentRT.elementList.TryGetValue(int.Parse(GetElementNumber(stackElements.Peek())), out elemDEntry) != true)
				{
					elemDEntry = new StringCollection();
					currentRT.elementList.Add(int.Parse(GetElementNumber(stackElements.Peek())), elemDEntry);
				}
				elemDEntry.Add("<objsur t=\"r\" guid=\"" + saveGuid + "\"/>");
			}
			else if (holdNode == "Run" && inFile.GetAttribute("ws") == null) // Make sure runs have WS
			{
				WriteElement(inFile, xmlOutput, "", "ws=\"en\""); // write the element, guessing it should be English
			}

			else if (stackClasses.Count > 0 && holdNode.Length >= 6 && holdNode.Substring(0,6) == "Custom")		// write a custom data element
			{
				WriteElement(inFile, xmlOutput, GetNewCustomName(inFile), "");		// write the next element in the class
			}

			else											  // Class Sub-element
			{
				WriteElement(inFile, xmlOutput, "", "");		// write the next element in the class
			}
		}


		/*********************************************************************/
		/* create the new Rt object for the class.                           */
		/*********************************************************************/
		private void CreateRtElement(XmlTextReader inFile, XmlTextWriter xmlOutput)
		{
			string stackClass = "", stackNum = "", baseClassName = holdNode;
			int modNum = 0, fieldNum = 0, classNum = 0;

			StartElem = false;
			if (holdNode != "LangProject")
			{
				RTClass clas = new RTClass();
				stackClasses.Push(clas);
				listClasses.Add(clas);
				currentRT = clas;
			}
			else
				currentRT = langProjClass;

			if (stackElements.Count > 0)
			{
				stackClass = GetElementName(stackElements.Peek());
				stackNum = (GetElementNumber(stackElements.Peek())).PadLeft(3, '0');
				modNum = int.Parse(stackNum.Substring(0, 1));
				classNum = int.Parse(stackNum.Substring(1));
				modList.TryGetValue(modNum, out dicClass);

				dicClass.TryGetValue(classNum, out hClass);
				hClass.fields.TryGetValue(stackClass, out fieldNum);
				if (stackNum != "000" && fieldNum != 0)
					currentRT.ownFlid = int.Parse((stackNum + (fieldNum.ToString().PadLeft(3, '0'))).TrimStart('0'));
				currentRT.owningGuid = guidStack.Peek();
			}

			// Build Hierarchy
			do
			{
				classList.TryGetValue(baseClassName, out hClass);
				classNum = hClass.ClassNum;
				currentRT.hierarchy.Add(int.Parse(hClass.ModNum + classNum.ToString().PadLeft(3, '0')));
				if (baseClassName !="CmObject")
				{
					classList.TryGetValue(baseClassName, out hClass);
					baseClassName = hClass.BaseClassName;
				}
			}
			while (currentRT.hierarchy[currentRT.hierarchy.Count-1] != 0);

			StartClass = true;
			currentRT.ClassName = holdNode;
			for (int i = 0; i < inFile.AttributeCount; i++)
			{
				inFile.MoveToAttribute(i);
				if (inFile.Name == "id")
				{
					currentRT.Guid = inFile.Value.Substring(1);
					guidStack.Push(currentRT.Guid);
				}
				else
				{
					throw new Exception("Attribute " + inFile.Name + " exists for " + holdNode);
				}
			}
		}

		/*********************************************************************/
		// returns the new name (from the custom list) for this custom data. */
		/*********************************************************************/
		private string GetNewCustomName(XmlTextReader inFile)
		{
			inFile.MoveToAttribute("name");
			foreach (Custom cclas in CustomList)
			{
				if (cclas.CustomName == inFile.Value && cclas.Creator == GetElementOwner())
					return cclas.CustomLabel;
			}
			return "";
		}

		/*********************************************************************/
		// Figure out the application that created the current custom field. */
		/*********************************************************************/
		private string GetElementOwner()
		{
			switch (stackClasses.Peek().ClassName)
			{
				case "RnGenericRec":
					return "DN";
				case "RnEvent":
					return "DN";
				case "RnAnalysis":
					return "DN";
				case "LexEntry":
					return "FLEX";
				case "LexSense":
					return "FLEX";
				case "LexExampleSentence":
					return "FLEX";
				case "MoForm": // include all its subclasses
				case "MoAffixForm":
				case "MoStemAllomorph":
				case "MoAffixAllomorph":
				case "MoAffixProcess":
					return "FLEX";
				default:
					return "TLE";
			}
		}

		/**********************************************************************/
		/* Writes the output for the next line element (some sort of element).*/
		/**********************************************************************/
		private void WriteElement(XmlTextReader inFile, XmlTextWriter xmlOutput, string cust, string newAttrs)
		{
			string attributes = "";
			int idx = 0;

			if (inFile.GetAttribute("val") != null) // time,date, integer, boolean elements
			{
				ParseAttributes(inFile, newAttrs, ref attributes);
				inFile.MoveToAttribute("val");
				if (nodeClass == "Custom")
					elemDEntry[elemDEntry.Count - 1] = elemDEntry[elemDEntry.Count - 1].Substring(0, elemDEntry[elemDEntry.Count - 1].Length - 1) + " " + inFile.Name + "=\"" + inFile.Value + "\"" + (attributes == "" ? "" : " ") + attributes + @"/>";
				else
					elemDEntry[elemDEntry.Count - 1] = "<" + nodeClass + " " + inFile.Name + "=\"" + inFile.Value + "\"" + (attributes == "" ? "" : " ") + attributes + @"/>";
				ClosedElem = true;
				return;
			}

			else
			{
				stackElements.Push(holdNode);
				nodeClass = GetElementName(holdNode);
				if (GetElementNumber(holdNode) != "")
				{
					nodeValue = int.Parse(GetElementNumber(holdNode));
				}
			}

			if (StartElem == true && GetElementNumber(holdNode) == "") //sub-element
			{
				if (cust != "")
					attributes = "name = " + cust;
				else
				{
					ParseAttributes(inFile, newAttrs, ref attributes);
				}

				inFile.MoveToContent();
				if (inFile.IsEmptyElement)        // This element is closed by a /
				{
					elemDEntry.Add("<" + nodeClass + (attributes == "" ? "" : " ") + attributes + @"/>");
					PopToElement(nodeClass);
				}
				else
					elemDEntry.Add("<" + nodeClass + (attributes == "" ? "" : " ") + attributes + ">");
			}

			else
			{
				StartElem = true;

				ParseAttributes(inFile, newAttrs, ref attributes);

				if (holdNode.Length >= 6 && holdNode.Substring(0, 6) == "Custom")
				{
					nodeClass = "Custom";
					if (cust != "")
					{
						idx = attributes.IndexOf("custom");
						attributes = attributes.Substring(0, idx) + TranslateText(cust) + "\"";
					}
				}

				if (currentRT.elementList.TryGetValue(nodeValue, out elemDEntry) != true)
				{
					elemDEntry = new StringCollection();
					currentRT.elementList.Add(nodeValue, elemDEntry);
				}
				elemDEntry.Add("<" + nodeClass + (attributes == "" ? "" : " ") + attributes + ">");
			}
		}

		private Regex m_guidRegex = new Regex(
			"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}");

		private void ParseAttributes(XmlTextReader inFile, string newAttrs, ref string text)
		{
			text = newAttrs;

			if (inFile.HasAttributes == true)
			{
				for (int i = 0; i < inFile.AttributeCount; i++)
				{
					inFile.MoveToAttribute(i);
					if (inFile.Name != "val" &&
						inFile.Name != "id"  && !(holdNode == "Link" && inFile.Name == "target"))
					{
						text = text + (text == "" ? "" : " ") + inFile.Name;
						if (inFile.HasValue)
						{
							string value = inFile.Value;
							// Check whether the value is a guid prefaced by a single character.
							// If so, chop off the first character -- we no longer need it.
							Match match = m_guidRegex.Match(value);
							if (match.Success && match.Index == 1)
								text = text + "=\"" + TranslateText(value.Substring(1)) + "\"";
							else
								text = text + "=\"" + TranslateText(value) + "\"";
						}
					}
				}
			}
		}

		private string GetModelName() //Locate the Model File
		{
			string rootPath = "HKEY_LOCAL_MACHINE\\SOFTWARE\\SIL\\FieldWorks\\7.0";
			string stringValue = "RootCodeDir";
			string dataKey = Registry.GetValue(rootPath, stringValue, null).ToString();

			if (dataKey != null)
				return dataKey + String.Format("{0}Templates{0}MasterFieldWorksModel7.0.xml", Path.DirectorySeparatorChar);
			else
				return null;
		}

		/**********************************************************************************************/
		/* Gets the owner's class id from the end of the name passed in.                              */
		/* Custom elements are under CmObject, so 0 is a valid owning number.                         */
		/**********************************************************************************************/
		private string GetElementNumber(string ElementName)   //Pop subentries off the stack of elements
		{
			if (ElementName == "WsStyles9999")
				return "";
			else if (ElementName.Length >= 6 && (ElementName.Substring(0,6) == "Custom"))
				return "0";
			else
				return Regex.Replace(ElementName, "[a-zA-Z]", "");
		}

		/**********************************************************************************************/
		/* Gets the element name less the owner's class id which is usually tacked on the end.        */
		/**********************************************************************************************/
		private string GetElementName(string ElementName)
		{
			if (ElementName == "WsStyles9999")
				return "WsStyles9999";
			else
			{
				for (int i = ElementName.Length - 1; i >= 0; i--)
				{
					if (!Char.IsNumber(ElementName, i))
						return ElementName.Substring(0, i + 1);
				}
				return ElementName;
			}
		}

		/**********************************************************************************************/
		/* Remove elements from thr stack of elements which aren't owned by some class.               */
		/**********************************************************************************************/
		private void PopPastSubentries()   //Pop subentries off the stack of elements
		{
			if (stackElements.Count > 0 && GetElementNumber(stackElements.Peek()) == "")
			{
				do
				{
					stackElements.Pop();
				}
				while (GetElementNumber(stackElements.Peek()) == ""); // This is a subelement
			}

		}

		/**********************************************************************************************/
		/* Remove elements from the stack of elements until we get to the one passed in.              */
		/**********************************************************************************************/
		private void PopToElement(string element)
		{
			if (stackElements.Count > 0)			//element stack is not empty
			{
				if (element == "")
				{
					throw new Exception("Cannot pop to a blank element");
				}
				else
				{
					while (stackElements.Count > 0 && stackElements.Peek() != element)
					{
						stackElements.Pop();
					}

					if (stackElements.Count > 0)
					{
						stackElements.Pop();
					}
				}
			}
		}

		/**********************************************************************************************/
		/* Process the end statements of an element.                                                   */
		/**********************************************************************************************/
		private void ProcessEndElement(XmlTextReader inFile, XmlTextWriter xmlOutput)
		{
			string elemHold = GetElementName(inFile.Name);

			if (GetElementNumber(inFile.Name) != "")
			{
				nodeValue = int.Parse(GetElementNumber(inFile.Name));
			}

			if (ClosedElem == true)
			{
				PopToElement(inFile.Name);
				ClosedElem = false;
				StartElem = false;
			}

			else if (stackClasses.Count > 0 && stackClasses.Peek().ClassName == elemHold)
			{
				guidStack.Pop();
				stackClasses.Pop();
				if (stackClasses.Count > 0)
					currentRT = stackClasses.Peek();
				StartClass = false;

				if (stackClasses.Count == 0)
				{
					if (listClasses.Count >= 1)  // Write out whatever elements are tabled up.
					{
						foreach (RTClass group in listClasses)
						{
							WriteRtElement(inFile, xmlOutput, group);
						}
					}
					listClasses.Clear();
					StartClass = false;
				}
			}

			else if (elemHold == "FwDatabase")		// write the last closing element in the XML file
			{
				if (listClasses.Count != 0)
				{
					throw new Exception("End of fwDatabase tag received, but still items that haven't been printed.");
				}
			}

			else if (elemHold == "LangProject")	// Write the LangProject class
			{
				WriteRtElement(inFile, xmlOutput, langProjClass);
			}

			else if (elemHold != "AdditionalFields")	//It's not a close class, FwDatabase or time. close it.
			{
				if (stackClasses.Count == 0)
					currentRT = langProjClass;

				if (currentRT.elementList.TryGetValue(nodeValue, out elemDEntry) != true)
				{
					throw new Exception("This is an ending element for " + elemHold + " but the element list for class " + nodeValue.ToString() + " doesn't exist");
				}
				if (elemHold.Length >= 6 && elemHold.Substring(0, 6) == "Custom")
					elemDEntry.Add(@"</Custom>");
				else
					elemDEntry.Add(@"</" + elemHold + @">");

				PopToElement(inFile.Name);
				StartElem = false;
			}
		}

		/**********************************************************************************************/
		/* Translate text to characters XML can handle before it gets tabled up.                      */
		/**********************************************************************************************/
		private void ProcessText(XmlTextReader inFile, XmlTextWriter xmlOutput)
		{
			elemDEntry[elemDEntry.Count - 1] = elemDEntry[elemDEntry.Count - 1] + TranslateText(inFile.Value);
		}

		/********************************************************************/
		/* Read through the input file initially looking for custom fields  */
		/********************************************************************/

		private bool ProcessCustomFields()
		{
			bool userViewFlag = false;
			int saveFlid = 0;
			string savePossList = "", saveLabel = "", saveHelpString = "", saveWsSel = "";

			//Input File
			XmlTextReader initFile = new XmlTextReader(new FilteredStreamReader(m_FileName));
			initFile.WhitespaceHandling = WhitespaceHandling.None;

			ReadInputFile(initFile);

			while (!initFile.EOF)
			{
				if (initFile.Name == "CustomField" && initFile.NodeType == XmlNodeType.Element)
				{
					BuildCustomField(initFile);
					CustomFlag = true;
				}

				else
					if (initFile.Name == "LangProject" && initFile.NodeType == XmlNodeType.Element)
					{
						if (CustomFlag == false)     //No custom fields
							return false;
					}
				else
					if (initFile.Name == "UserViewField")
					{
						if (userViewFlag == true)
						{
							GetValuesFromUserView(saveFlid, saveLabel, savePossList, saveHelpString, saveWsSel);
							userViewFlag = false;
						}
						else
							userViewFlag = true;
					}
				else
					if (userViewFlag == true && initFile.Name.Length > 4 && initFile.Name.Substring(0,4) == "Flid" && initFile.NodeType == XmlNodeType.Element)
					{
						ReadInputFile(initFile);     // To get to field containing flid
						initFile.MoveToAttribute("val");
						saveFlid = int.Parse(initFile.Value);
					}
				else
					if (userViewFlag == true && initFile.Name.Length > 5 && initFile.Name.Substring(0, 5) == "Label" && initFile.NodeType == XmlNodeType.Element)
					{
						ReadInputFile(initFile);     // To get to AUni
						ReadInputFile(initFile);     // To get to Text
						saveLabel = initFile.Value;
					}
				else
					if (userViewFlag == true && initFile.Name.Length > 8 && initFile.Name.Substring(0, 8) == "PossList" && initFile.NodeType == XmlNodeType.Element)
					{
						ReadInputFile(initFile);     // To get to Link
						initFile.MoveToAttribute("target");
						savePossList = initFile.Value.Substring(1);
					}
				else
					if (userViewFlag == true && initFile.Name.Length > 10 && initFile.Name.Substring(0, 10) == "WsSelector" && initFile.NodeType == XmlNodeType.Element)
					{
						ReadInputFile(initFile);     // To get to Link
						initFile.MoveToAttribute("val");
						saveWsSel = initFile.Value;
					}
				else
					if (userViewFlag == true && initFile.Name.Length > 10 && initFile.Name.Substring(0, 10) == "HelpString" && initFile.NodeType == XmlNodeType.Element)
					{
						ReadInputFile(initFile);     // To get to AUni
						ReadInputFile(initFile);     // To get to Text
						saveHelpString = initFile.Value;
					}

			ReadInputFile(initFile);
			} // while read
			initFile.Close();
			return CustomFlag;
		}
		/********************************************************************/
		/* Build a custom object based on the initial information.          */
		/********************************************************************/
		private void BuildCustomField(XmlTextReader initFile)
		{
			Custom cclas = new Custom();
			CustomList.Add(cclas);
			if (initFile.HasAttributes == true)
			{
				for (int i = 0; i < initFile.AttributeCount; i++)
				{
					initFile.MoveToAttribute(i);
					if (initFile.Name != "val" && initFile.Name != "id")
					{
						switch (initFile.Name)
						{
							case "name":
								cclas.CustomName = initFile.Value;
								break;
							case "flid":
								cclas.CustomFlid = int.Parse(initFile.Value);
								break;
							case "class":
								cclas.CustomClass = initFile.Value;
								break;
							case "type":
								cclas.CustomType = CustomType(initFile.Value);
								break;
							case "wsSelector":
								cclas.CustomWsSelector = initFile.Value;
								break;
							case "userLabel":
								cclas.CustomLabel = initFile.Value;
								break;
							case "target":
								cclas.CustomDestClass = CustomDestClassLookup(initFile.Value);
								break;
							case "big":
								break;
							case "helpString":
								cclas.CustomHelpString = initFile.Value;
								break;
							default:
								throw new Exception("Unrecognized attribute on custom field is " + initFile.Value.ToString());

						} //switch statement
					}
				}
				switch (cclas.CustomClass)
				{
					case "RnGenericRec":
						cclas.Creator = "DN";
						break;
					case "RnEvent":
						cclas.Creator = "DN";
						cclas.CustomClass = "RnGenericRec";
						break;
					case "RnAnalysis":
						cclas.Creator = "DN";
						cclas.CustomClass = "RnGenericRec";
						break;
					case "LexEntry":
						cclas.Creator = "FLEX";
						break;
					case "LexSense":
						cclas.Creator = "FLEX";
						break;
					case "LexExampleSentence":
						cclas.Creator = "FLEX";
						break;
					case "MoForm":
						cclas.Creator = "FLEX";
						break;
					default:
						cclas.Creator = "TLE";
						break;
				}
			}
		}
		/********************************************************************/
		/* Return the type or convert it if needed							*/
		/********************************************************************/

		private string CustomType(string type)
		{
			switch (type)
			{
				case "OwningAtom":
					return "OA";
				case "ReferenceAtom":
					return "RA";
				case "ReferenceSequence":
					return "RS";
				case "OwningCollection":
					return "OC";
				case "ReferenceCollection":
					return "RC";
				case "OwningSequence":
					return "OS";
				default:
					return type;
			} //switch statement
		}
		/********************************************************************/
		/* Return the classid for the target which is destclass.            */
		/********************************************************************/
		private int CustomDestClassLookup(string target)
		{
			classList.TryGetValue(target, out hClass);
			return hClass.ClassNum;
		}

		/********************************************************************/
		/* If these values in the UserViewField Class match the Flid,       */
		/* update the custom fields.                                        */
		/********************************************************************/

		private void GetValuesFromUserView(int saveFlid, string saveLabel, string savePoss, string saveHelpString, string saveWsSel)
		{
			foreach (Custom cclas in CustomList)
			{
				if (saveFlid == cclas.CustomFlid)
				{
					cclas.CustomLabel = saveLabel;
					cclas.CustomWsSelector = saveWsSel;
					if (cclas.CustomType.Substring(0, 1) == "R")
						cclas.CustomListRoot = savePoss;
					if (saveHelpString != "")
						cclas.CustomHelpString = saveHelpString;
				}
			}
		}

		/********************************************************************/
		/* We're on a label field in the UserViewField Class whose flid     */
		/* matches one of the custom fields, so we need to update its name. */
		/********************************************************************/
		private void WriteCustomHeaders(XmlTextWriter xmlOut)
		{
			xmlOut.WriteStartElement("AdditionalFields");
			foreach (Custom cclas in CustomList)
			{
				xmlOut.WriteStartElement("CustomField");
				if (cclas.CustomLabel != "")
					xmlOut.WriteAttributeString("name", cclas.CustomLabel);
				if (cclas.CustomClass != "")
					xmlOut.WriteAttributeString("class", cclas.CustomClass);
				if (cclas.CustomDestClass != 0)
					xmlOut.WriteAttributeString("destclass", cclas.CustomDestClass.ToString());
				if (cclas.CustomType != "")
					xmlOut.WriteAttributeString("type", cclas.CustomType);
				if (cclas.CustomWsSelector != "")
					xmlOut.WriteAttributeString("wsSelector", cclas.CustomWsSelector);
				if (cclas.CustomListRoot != "")
					xmlOut.WriteAttributeString("listRoot", cclas.CustomListRoot);
				if (cclas.CustomHelpString != "")
					xmlOut.WriteAttributeString("helpString", cclas.CustomHelpString);
				xmlOut.WriteEndElement();
			}
			xmlOut.WriteEndElement();
		}

		/********************************************************************/
		/* XML data contains certain characters that aren't read correctly  */
		/* so we tranlate them to a different representation.               */
		/********************************************************************/
		private string TranslateText(string tText)
		{
			string tempArea = "";
			foreach (char a in tText)
			{
				switch (a)
				{
					case '<':
						tempArea = tempArea + "&lt;";
						break;
					case '>':
						tempArea = tempArea + "&gt;";
						break;
					case '&':
						tempArea = tempArea + "&amp;";
						break;
					//case '\'':
					//    tempArea = tempArea + "&apos;";
					//    break;
					case '"':
						tempArea = tempArea + "&quot;";
						break;
					default:
						tempArea = tempArea + a;
						break;
				}
			}
			return tempArea;
		}

		private void WriteDocTypeOut(XmlTextReader inFile, XmlTextWriter xmlOutput)
		{
			if (currentRT.ClassName == "")  //There is no cuurrently instantiazed class
				xmlOutput.WriteDocType(inFile.Name, null, null, null);
			else
			{
				throw new Exception("DocType input isn't until after the root element");
			}
		}

		/***********************************************************************************************/
		/* We're ready to write the rt element we have tabled up.  This method writes the 'heading'    */
		/* and controls the writing of the hierarchical data for each hierarchy.                       */
		/***********************************************************************************************/
		private void WriteRtElement(XmlTextReader inFile, XmlTextWriter xmlOutput, RTClass group)
		{
			int i = 0, owningOrd = 0;
			int BaseModNum =0, BaseClassNum = 0;

			if (group.owningGuid != "")
			{
				if (group.ownFlid != 0 || (group.owningGuid != "" && group.ownFlid == 0))
				{
					if (oOrdList.TryGetValue(group.owningGuid + group.ownFlid.ToString(), out owningOrd) != true)
					{
						owningOrd = 0;
						oOrdList.Add(group.owningGuid + group.ownFlid.ToString(), owningOrd);
					}
					owningOrd++;
					oOrdList[group.owningGuid + group.ownFlid.ToString()] = owningOrd;
					xmlOutput.WriteStartElement("rt");
					xmlOutput.WriteAttributeString("class", group.ClassName);
					xmlOutput.WriteAttributeString("guid", group.Guid);
					xmlOutput.WriteAttributeString("ownerguid", group.owningGuid);
					// The following are removed in data migration to 7000010, so why bother
					// putting them out to begin with?  If there are too many objects, removing
					// them can lead to running out of memory.  (See LT-11241.)
					//xmlOutput.WriteAttributeString("owningflid", group.ownFlid.ToString());
					//xmlOutput.WriteAttributeString("owningord", owningOrd.ToString());
				}
				else
				{
					throw new Exception("How can class " + group.ClassName + " have an owningGuid but no ownFlid?");
				}
			}
			else
			{
					xmlOutput.WriteStartElement("rt");
					xmlOutput.WriteAttributeString("class", group.ClassName);
					xmlOutput.WriteAttributeString("guid", group.Guid);
			}
			for (i=group.hierarchy.Count-1; i >= 0; i--)
			{
				BaseModNum = int.Parse(group.hierarchy[i].ToString().Length < 4 ? "0" : group.hierarchy[i].ToString().Substring(0,1));
				BaseClassNum = int.Parse(group.hierarchy[i].ToString().Length == 4 ? group.hierarchy[i].ToString().Substring(1,3) : group.hierarchy[i].ToString().Substring(0));
				modList.TryGetValue(BaseModNum, out dicClass);
				dicClass.TryGetValue(BaseClassNum, out hClass);
				WriteProjectGroup(group.hierarchy[i], hClass.ClassName, xmlOutput, group);
			}
			xmlOutput.WriteEndElement();	// rt
		}

		/***********************************************************************************************/
		/* We're passed in the name of the hierarchy to write. We write properties associated with it. */
		/***********************************************************************************************/
		private void WriteProjectGroup(int hierNum, string hierName, XmlTextWriter xmlOutput, RTClass group)
		{
			//write all the elements associated with this base class
			if (group.elementList.ContainsKey(hierNum))
			{
				xmlOutput.WriteStartElement(hierName);
				foreach (string s in group.elementList[hierNum])
				{
					string trimmed;
					if (s.StartsWith("<Run ") && (trimmed = s.TrimEnd()).EndsWith(">") && trimmed.Length != s.Length)
					{
						xmlOutput.WriteRaw(s.Substring(0, trimmed.Length - 1) + " xml:space=\"preserve\"" + s.Substring(trimmed.Length - 1));
					}
					else
						xmlOutput.WriteRaw(s);
				}
				xmlOutput.WriteEndElement();
			}
			else
			{
				xmlOutput.WriteRaw("<" + hierName + @"/>");
			}
		}

		private void WriteStartXMLElement(string NodeName, string NodeValue, XmlTextWriter xmlOutput)
		{
			xmlOutput.WriteStartElement(NodeName);
			xmlOutput.WriteString(NodeValue);
		}

		private void WriteXMLAttribute(string AttrName, string AttrValue, XmlTextWriter xmlOutput)
		{
			xmlOutput.WriteStartAttribute(AttrName);
			xmlOutput.WriteString(AttrValue);
			xmlOutput.WriteEndAttribute();
		}

		/***********************************************************************************************/
		/* We read in the model and table it up.  This is much faster than reading in an XMLdocument.  */
		/***********************************************************************************************/
		private void ProcessModel(XmlTextReader modFile)
		{
			string holdFieldName = "";
			int holdModNum = 0, holdFieldNum = 0;

			while (modFile.Read())
			{
				if (modFile.NodeType == XmlNodeType.Element)
				{
					holdNode = modFile.Name;

					if (holdNode == "EntireModel")		// get the version number
					{
						modFile.MoveToAttribute("version");
						holdVersion = modFile.Value;
					}

					else if (holdNode == "CellarModule")		// get the module number
					{
						modFile.MoveToAttribute("num");
						holdModNum = int.Parse(modFile.Value);
						Dictionary<int, Classes> dicClass = new Dictionary<int, Classes>();
						modList.Add(holdModNum, dicClass);
					}
					else if (holdNode == "class")
					{
						modList.TryGetValue(holdModNum, out dicClass);
						modFile.MoveToAttribute("num");
						holdClassNum = int.Parse(modFile.Value);
						modFile.MoveToAttribute("id");
						holdClassName = modFile.Value;

						Classes hClass = new Classes();
						hClass.ClassNum = holdClassNum;
						hClass.ClassName = holdClassName;
						hClass.ModNum = holdModNum;
						modFile.MoveToAttribute("base");
						hClass.BaseClassName = modFile.Value;

						classList.Add(holdClassName, hClass);
						dicClass.Add(holdClassNum, hClass);
					}
					else if (holdNode == "basic" || holdNode == "owning" || holdNode == "rel")
					{
						classList.TryGetValue(holdClassName, out hClass);
						modFile.MoveToAttribute("num");
						holdFieldNum = int.Parse(modFile.Value);
						modFile.MoveToAttribute("id");
						holdFieldName = modFile.Value;
						hClass.fields.Add(holdFieldName, holdFieldNum);
					}
				}
			}
			modFile.Close();
		}
	}
}