// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: Instructionator.cs
// Responsibility: LastufkaM
// Last reviewed:
//
// <remarks>
//    This class will eventually replace xmlInstructionBuilder.
// </remarks>

using System;
using System.Xml;
using System.Collections;
using System.Reflection;

namespace GuiTestDriver
{
	public class Instructionator
	{
		static ArrayList m_actPrototypes = null;
		static ArrayList m_pasPrototypes = null;
		static ArrayList m_susPrototypes = null;

		const string prefix = "GuiTestDriver.";

		/// <summary>
		/// Build a dynamic instruction tree by parsing test script elements.
		/// The TestState must already be created.
		/// </summary>
		public Instructionator() { }

		public static bool initialize(string insFileName)
		{
			// read the instruction file into prototype classes
			XmlElement docEl = XmlFiler.getDocumentElement(insFileName, "instructions", false);
			if (docEl == null)
			{
				Logger.getOnly().fail("Instruction prototype file " + insFileName + " not found or empty");
				return false;
			}
			XmlNodeList xnActive = XmlFiler.selectNodes(docEl, "active");
			if (xnActive == null)
			{
				Logger.getOnly().fail("Instruction prototype file " + insFileName + " contains no active instructions");
				return false;
			}
			m_actPrototypes = listChildren(xnActive[0]);
			if (m_actPrototypes == null)
			{
				Logger.getOnly().fail("Instruction prototype file " + insFileName + " contains not one active instruction!");
				return false;
			}

			XmlNodeList xnPassive = XmlFiler.selectNodes(docEl, "passive");
			if (xnPassive != null) m_pasPrototypes = listChildren(xnPassive[0]);

			XmlNodeList xnSuspended = XmlFiler.selectNodes(docEl, "suspended");
			if (xnSuspended != null) m_susPrototypes = listChildren(xnSuspended[0]);
			return true;
		}

		/// <summary>
		/// Read instruction file.
		/// Interpret script nodes according to tne instruction file.
		/// Make the class but don't process its child instructions if any.
		/// Don't execute the instruction.
		/// </summary>
		/// <param name="xn">The XML repersentation of the instruction to be checked</param>
		/// <param name="con">The current context object</param>
		/// <returns>Returns an unexecuted instruction or null</returns>
		static public Instruction MakeShell(XmlNode xn, Context con)
		{
			Logger.getOnly().isNotNull(xn, "Instruction can't be created from nothing!");
			if (m_actPrototypes == null) {
				Logger.getOnly().fail("Can not create: No instruction prototypes loaded.");
				return null;
			}

			Instruction FluffedInstruction = null;

			// number the context if it doesn't have one yet to avoid depth-first numbering.
			if (con != null && con.Number == -1) con.Number = TestState.getOnly().IncInstructionCount;

			// figure out what to do with this node
			switch (xn.Name)
			{
			case "#comment": // ignore comments, etc..
			case "#significant-whitespace":
			case "#whitespace":
			case "#text":
				break;
			default: // Find the instruction prototype based on node name.
				InsPrototype prototype = findPrototype(xn.Name, m_actPrototypes);
				if (prototype != null)
				{
					var AtValues = new ArrayList();
					ArrayList atts = prototype.Attributes;

					Logger.getOnly().startList("Instruction " + prototype.Name);

					XmlAttributeCollection atNodes = xn.Attributes;
					if (atNodes != null && atts != null)
					{
						foreach (XmlAttribute atx in atNodes)
						{ // find each attribute in the prototype
							string atValue = null;
							foreach (Attribute at in atts)
							{
								if (at.Name == atx.Name)
								{ // its this one
									atValue = XmlFiler.getAttribute(xn, at.Name);
									if (atValue != null && at.Name != "log")
									{ // log is dealt with in AddInstruction()
										var atVar = new Attribute(at.Name, atValue, at.Value);
										AtValues.Add(atVar);
										Logger.getOnly().listItem(" " + atx.Name + "=" + atValue);
										break;
									}
								}
							}
							if (atValue == null)
							{ // This attribute is not expected: make it a variable
								// Add it as a var instruction so it gets bound at the right time
								// Use <var id="atx.Name" set="atValue"/>
								var var = new Var();
								var.Id = atx.Name;
								var.Set = XmlFiler.getAttribute(xn, atx.Name);
								// Add the var to the growing list of instructions
								AddInstruction(xn, var, con);
								Logger.getOnly().paragraph("Added <var id=\"" + var.Id + "\" set=\"" + var.Set + "\"/>");
							}
						}
					}

					Logger.getOnly().endList();

					// Create the instruction using prototype.Name, AtValues.Name and AtValues.Value
					string protoName = XmlNameToCName(prototype.Name);
					string protoNameQ = prefix + protoName;
					Assembly assem = Assembly.GetExecutingAssembly();
					// All instruction classes must have empty constructors
					Object protoInstruction = null;
					try { protoInstruction = assem.CreateInstance(protoNameQ, true,
								   BindingFlags.CreateInstance, null, null, null, null);}
					catch (Exception e) { Logger.getOnly().fail("Instruction " + protoName + " not created: " + e.Message); }
					Logger.getOnly().isNotNull(protoInstruction, "Instruction " + protoName + " is DOA");
					FluffedInstruction = (Instruction)protoInstruction;
					foreach (Attribute at in AtValues)
					{ // Call each setter to set the instruction properties.
						int number = 0;
						UInt32 unsigned = 0;
						string primative = "string";
						if (at.Type == "msec" || at.Type == "int" || at.Type.Contains("[0-10]"))
						{
							try
							{
								number = Convert.ToInt32(at.Value);
								primative = "int";
							}
							catch (FormatException) { }
						}
						if (at.Type == "m-sec" || at.Type == "hz")
						{
							try
							{
								unsigned = Convert.ToUInt32(at.Value, 10);
								primative = "UInt32";
							}
							catch (FormatException) { }
						}
						if (primative == "string" && at.Type.Contains("|"))
						{
							string[] enumList = makeEnumList(at.Type);
							foreach (string value in enumList)
							{
								if (value == at.Value)
								{
									primative = value;
									break;
								}
							}
							if (primative == "string")
								Logger.getOnly().fail("Invalid enum {" + at.Value + "} for " + protoNameQ + "." + at.Name + "(" + at.Type + ")");
						}
						string propName = XmlNameToCName(at.Name);
						string propNameQ = protoNameQ + "." + XmlNameToCName(at.Name);
						PropertyInfo pi = null;
						MethodInfo m = null;
						try
						{
							if (primative == "int")
							{
								pi = assem.GetType(protoNameQ).GetProperty(propName, typeof(int));
								m = pi.GetSetMethod();
								m.Invoke(protoInstruction, new Object[] { number });
							}
							else if (primative == "UInt32")
							{
								pi = assem.GetType(protoNameQ).GetProperty(propName, typeof(UInt32));
								m = pi.GetSetMethod();
								m.Invoke(protoInstruction, new Object[] { unsigned });
							}
							else
							{
								Type t = assem.GetType(protoNameQ);
								pi = t.GetProperty(propName, typeof(string));
								m = pi.GetSetMethod();
								m.Invoke(protoInstruction, new Object[] { at.Value });
							}
						}
						catch
						{ Logger.getOnly().fail(" Can't find setter: " + protoNameQ + "." + propName + "(" + at.Type + ") using value {" + at.Value + "}"); }
						if (at.Name == "id" && protoName != "Var")
							TestState.getOnly().AddNamedInstruction(at.Value, FluffedInstruction);
					} // end of process attributes
					// Call the finishCreation method
					FluffedInstruction.finishCreation(xn, con);
					//if (prototype.Name != "if" &&
					//	prototype.Name != "then" && prototype.Name != "else")
					// Add the instruction to the growing list of instructions
					AddInstruction(xn, FluffedInstruction, con);
				}
				else
				{
					bool unknown = true;
					if (m_pasPrototypes != null)
					{
						InsPrototype IgnoredPrototype = findPrototype(xn.Name, m_pasPrototypes);
						if (IgnoredPrototype != null) unknown = false;
					}
					if (unknown) Logger.getOnly().fail("Can't make <" + xn.Name + "> instruction");
				}
				break;
			}
			return FluffedInstruction;
		}

		/// <summary>
		/// Adds an instruction to the growing tree, sets the log level
		/// and records it to the log.
		/// </summary>
		/// <param name="xn">An element node</param>
		/// <param name="ins">The instruction to be added</param>
		/// <param name="con">The current context object</param>
		static private void AddInstruction(XmlNode xn, Instruction ins, Context con)
		{	// Vars put themselves on ts, making sure there is only one
			ins.Element = (XmlElement)xn;
			// If not a new one, make sure the model node is propagated to each child context
			if (ins is Context && con.ModelNode != null)
				((Context)ins).ModelNode = con.ModelNode;
			ins.Parent = con;
			con.Add(ins);
			string logLevel = XmlFiler.getAttribute(xn, "log");
			if (logLevel != null && "all" == logLevel) ins.Log = 1;
			else if (logLevel != null && "time" == logLevel) ins.Log = 2;
			else ins.Log = Convert.ToInt32(logLevel);
			// add one to the instruction count, then assign it to the instruction
			// A context might already have a number
			//if (ins.Number == -1) ins.Number = TestState.getOnly().IncInstructionCount;
			//Logger.getOnly().mark(ins); // log the progress of interpretation
		}

		/// <summary>
		/// Select the nodes from the GUI Model xPath select in the script context
		/// con.
		/// </summary>
		/// <param name="con">The script context who's model to use</param>
		/// <param name="select">Gui Model xPath to model nodes</param>
		/// <param name="source">Text to identify the source instruction in asserts</param>
		/// <returns>A list of model nodes matching the query</returns>
		static public XmlNodeList selectNodes(Context con, string select, string source)
		{
			XmlNodeList nodes = null;
			// try to dereference variables in the select expression
			string evalSelect = Utilities.evalExpr(select);
			if (con.ModelNode != null)
			{ // Search from the current model context
				XmlNode current = con.ModelNode.ModelNode;
				Logger.getOnly().isNotNull(current, source + " context model node is null, parent may not have @select");
				XmlNamespaceManager nsmgr = new XmlNamespaceManager(current.OwnerDocument.NameTable);
				//m_log.paragraph("Selecting from model context=" + current.Name);
				nodes = current.SelectNodes(evalSelect, nsmgr);
			}
			else
			{ // Search from the model root
				OnApplication apCon = null;
				if (typeof(OnApplication).IsInstanceOfType(con))
					apCon = (OnApplication)con;
				else
					apCon = (OnApplication)con.Ancestor(typeof(OnApplication));
				Logger.getOnly().isNotNull(apCon, "No on-application context found for " + source + "select to get a model from");
				XmlElement root = apCon.ModelRoot;
				Logger.getOnly().isNotNull(root, "GUI model of " + source + "select has no root");
				// preprocess select for special variables like $name; if there is a model node
				//m_log.paragraph("Selecting from model context=" + root.Name);
				nodes = root.SelectNodes(evalSelect);
			}
			return nodes;
		}

		/// <summary>
		/// Init cap and remove - from the names to get the property name.
		/// The first char after a - is capitalized too.
		/// </summary>
		/// <param name="name">Name of attribute or element</param>
		/// <returns>Condensed name</returns>
		static public string XmlNameToCName(string name)
		{
			string newName = null;
			bool foundDash = true;
			foreach (Char c in name)
			{
				if (c == '-') foundDash = true;
				else
				{
					if (foundDash) newName += Char.ToUpper(c);
					else newName += c;
					foundDash = false;
				}
			}
			return newName;
		}

		/// <summary>
		/// Tokenizes the enum expression delimited by "|".
		/// </summary>
		/// <param name="enumExpression">the enum expression delimited by "|"</param>
		/// <returns>ArrayList of valid enum values</returns>
		static public string [] makeEnumList(string enumExpression)
		{
			return enumExpression.Split(new char [] {'|'});
		}

		/// <summary>
		/// Puts the children of an instruction group into an arraylist of prototypes.
		/// </summary>
		/// <param name="xn">Instruction group XML node</param>
		/// <returns>ArrayList of the prototypes or null</returns>
		static private ArrayList listChildren(XmlNode xn)
		{
			ArrayList childList = null;
			XmlNodeList children = xn.ChildNodes;
			foreach (XmlNode xnChild in children)
			{
				switch (xnChild.Name)
				{
				case "#comment": // ignore comments, etc..
				case "#significant-whitespace":
				case "#whitespace":
				case "#text":
					break;
				default:
					if (childList == null) childList = new ArrayList(30);
					childList.Add(new InsPrototype(xnChild));
					break;
				}
			}
			return childList;
		}

		/// <summary>
		/// Finds a prototype by name from a set of targets.
		/// </summary>
		/// <param name="name">The name of the prototype saught</param>
		/// <param name="targets">The list of possible prototypes</param>
		/// <returns>The prototype found or null</returns>
		static private InsPrototype findPrototype(string name, ArrayList targets)
		{
			InsPrototype prototype = null;
			foreach (InsPrototype proto in targets)
			{
				//Logger.getOnly().paragraph(proto.Name + "=?" + name);
				if (proto.Name == name)
				{
					prototype = proto;
					break;
				}
			}
			return prototype;
		}
	}
}
