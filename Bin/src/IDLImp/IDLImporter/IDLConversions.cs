/// --------------------------------------------------------------------------------------------
#region /// Copyright (c) 2002-2007, SIL International. All Rights Reserved.
/// <copyright from='2002' to='2007' company='SIL International'>
///		Copyright (c) 2002-2007, SIL International. All Rights Reserved.
///
///		Distributable under the terms of either the Common Public License or the
///		GNU Lesser General Public License, as specified in the LICENSING.txt file.
/// </copyright>
#endregion
///
/// File: IDLConversions.cs
/// Responsibility: Eberhard Beilharz
/// Last reviewed:
///
/// <remarks>
/// Defines most of the conversions that will be performed on the interfaces of the IDL file.
/// </remarks>
/// --------------------------------------------------------------------------------------------

//#define DEBUG_IDLGRAMMAR

using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;

namespace SIL.FieldWorks.Tools
{
	#region XML type definition
	[System.Xml.Serialization.XmlTypeAttribute(Namespace="http://dummy.sil.org/IDLConversions.xsd")]
	public class ConversionEntry
	{
		public ConversionEntry()
		{}

		public ConversionEntry(string match, string replace)
		{
			Match = match;
			Replace = replace;
		}

		private string m_sAttribute;
		private string m_sAttrValueName;
		private string m_sAttrValue;
		private string m_sMatch;
		private string m_sReplace;
		private string m_sNewAttribute;
		private string m_sNewAttrValueName;
		private string m_sNewAttrValue;
		private bool m_fEnd = true;
		private Regex m_Regex;

		public string Attribute
		{
			get { return m_sAttribute; }
			set { m_sAttribute = value; }
		}

		[XmlIgnore]
		public string[] Attributes
		{
			get
			{
				if (m_sAttribute != null)
					return m_sAttribute.Split(',');
				return null;
			}
		}

		[XmlIgnore]
		public string AttrValueName
		{
			get { return m_sAttrValueName; }
		}

		public string AttrValue
		{
			get { return m_sAttrValue; }
			set
			{
				string[] parts = value.Split('=');
				if (parts.Length > 1)
				{
					m_sAttrValueName = parts[0];
					m_sAttrValue = parts[parts.Length-1];
				}
				else
					m_sAttrValue = value;
			}
		}

		public string Match
		{
			get { return m_sMatch; }
			set
			{
				m_sMatch = value;
				m_Regex = new Regex(m_sMatch);
			}
		}

		public string Replace
		{
			get { return m_sReplace; }
			set { m_sReplace = value; }
		}

		public string NewAttribute
		{
			get { return m_sNewAttribute; }
			set { m_sNewAttribute = value; }
		}

		[XmlIgnore]
		public string[] NewAttributes
		{
			get
			{
				if (m_sNewAttribute != null)
					return m_sNewAttribute.Split(',');
				return null;
			}
		}

		[XmlIgnore]
		public string NewAttrValueName
		{
			get { return m_sNewAttrValueName; }
		}

		public string NewAttrValue
		{
			get { return m_sNewAttrValue; }
			set
			{
				string[] parts = value.Split('=');
				if (parts.Length > 1)
				{
					m_sNewAttrValueName = parts[0];
					m_sNewAttrValue = parts[parts.Length-1];
				}
				else
					m_sNewAttrValue = value;
			}
		}

		public bool fEnd
		{
			get { return m_fEnd; }
			set { m_fEnd = value; }
		}

		[XmlIgnore]
		public Regex Regex
		{
			get { return m_Regex; }
		}
	}
	#endregion

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Defines most of the conversions that will be performed on the interfaces of the IDL file.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://dummy.sil.org/IDLConversions.xsd")]
	[System.Xml.Serialization.XmlRootAttribute(Namespace="http://dummy.sil.org/IDLConversions.xsd",
		IsNullable=false)]
	public class IDLConversions
	{
		#region Serialization
		public void Serialize(string fileName)
		{
			TextWriter textWriter = null;

			try
			{
				textWriter = new StreamWriter(fileName);
				XmlSerializer xmlSerializer = new XmlSerializer(typeof(IDLConversions));
				xmlSerializer.Serialize(textWriter, this);
			}
			finally
			{
				if (textWriter != null)
					textWriter.Close();
			}
		}

		public static IDLConversions Deserialize(string fileName)
		{
			XmlReader reader = null;
			IDLConversions ret = null;
			try
			{
				reader = new XmlTextReader(fileName);
				XmlSerializer xmlSerializer = new XmlSerializer(typeof(IDLConversions));
				ret = (IDLConversions)xmlSerializer.Deserialize(reader);

				if (ret.m_ParamNames != null)
				{
					s_ParamNames = new ConversionEntry[ret.m_ParamNames.Length];
					ret.m_ParamNames.CopyTo(s_ParamNames, 0);
				}
			}
			finally
			{
				if (reader != null)
					reader.Close();
			}

			return ret;
		}
		#endregion

		#region Variables
		[System.Xml.Serialization.XmlElementAttribute("ParamTypeConversion")]
		public ConversionEntry[] m_ParamTypes;

		[System.Xml.Serialization.XmlElementAttribute("ParamNameConversion")]
		public ConversionEntry[] m_ParamNames; // only here so that we can serialize it.

		private CodeNamespace m_Namespace;

		private static ConversionEntry[] s_ParamNames;
		private static Dictionary<CodeFieldReferenceExpression, string> s_NeedsAdjustment =
			new Dictionary<CodeFieldReferenceExpression, string>();
		private static Dictionary<string, string> s_EnumMemberMapping =
			new Dictionary<string, string>();
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the namespace.
		/// </summary>
		/// <value>The namespace.</value>
		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public CodeNamespace Namespace
		{
			get { return m_Namespace; }
			set { m_Namespace = value; }
		}

		#region General conversion methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the function declaration, i.e. look at the attributes and change the return
		/// value and other stuff
		/// </summary>
		/// <param name="member">Contains information about the function</param>
		/// <param name="rt">Specified return value</param>
		/// <param name="types">The types.</param>
		/// <param name="attributes">The attributes.</param>
		/// <returns>Function or property description</returns>
		/// ------------------------------------------------------------------------------------
		public CodeTypeMember HandleFunction_dcl(CodeMemberMethod member, CodeTypeReference rt,
			CodeTypeMemberCollection types, Hashtable attributes)
		{
			bool fPreserveSig = false;
			CodeTypeMember memberRet = member;

			if (attributes["custom"] != null)
			{
				CodeAttributeArgument arg = (CodeAttributeArgument)attributes["custom"];
				if (arg.Name == "842883D3-DC67-45cf-B968-E763D37A7A19")
				{
					if (arg.Value != null && arg.Value.ToString() != "false")
					{	// preserve signature
						fPreserveSig = true;
						member.ReturnType = rt;
						memberRet.CustomAttributes.Add(new CodeAttributeDeclaration("PreserveSig"));
					}

					attributes.Remove("custom");
				}
			}

			if (attributes["propget"] != null || attributes["propput"] != null || attributes["propputref"] != null)
			{
				if (member.Parameters.Count == 1)
				{
					// normal property - deal with it the .NET way (get/set)
					CodeMemberProperty property = new CodeMemberProperty();
					property.Attributes = memberRet.Attributes;
					property.Comments.AddRange(memberRet.Comments);
					property.CustomAttributes.AddRange(memberRet.CustomAttributes);
					property.EndDirectives.AddRange(memberRet.EndDirectives);
					property.LinePragma = memberRet.LinePragma;
					property.Name = memberRet.Name;
					property.StartDirectives.AddRange(memberRet.StartDirectives);
					foreach (object key in memberRet.UserData.Keys)
						property.UserData.Add(key, memberRet.UserData[key]);

					if (attributes["propget"] != null)
					{
						property.HasGet = true;
						attributes.Remove("propget");
					}
					if (attributes["propput"] != null || attributes["propputref"] != null)
					{
						property.HasSet = true;
						Trace.Assert(attributes["propput"] == null || attributes["propputref"] == null);
						attributes.Remove("propput");
						attributes.Remove("propputref");
					}
					property.Type = member.Parameters[0].Type;
					memberRet = property;
				}
				else
				{
					// parameter with multiple parameters - can't use get/set the .NET way
					if (attributes["propget"] != null)
					{
						memberRet.Name = "get_" + memberRet.Name;
						attributes.Remove("propget");
					}
					if (attributes["propput"] != null)
					{
						int iPropSet = IndexOfMember(types, "set_" + member.Name);
						if (iPropSet > -1)
							memberRet.Name = "let_" + memberRet.Name;
						else
							memberRet.Name = "set_" + memberRet.Name;
						attributes.Remove("propput");
					}
					if (attributes["propputref"] != null)
					{
						int iPropSet = IndexOfMember(types, "set_" + member.Name);
						if (iPropSet > -1)
							memberRet.Name = "let_" + memberRet.Name;
						else
							memberRet.Name = "set_" + memberRet.Name;
						attributes.Remove("propputref");
					}
				}
			}

			if (!fPreserveSig)
			{
				CodeParameterDeclarationExpression retParam = GetReturnType(member.Parameters);
				member.ReturnType = retParam.Type;
				member.CustomAttributes.AddRange(retParam.CustomAttributes);
				for (int i = 0; i < member.CustomAttributes.Count; i++)
				{
					member.CustomAttributes[i].Name = "return: " + member.CustomAttributes[i].Name;
				}
			}


			// Needs to come after putting "return:" in front!
			if (attributes["local"] != null)
			{
				member.CustomAttributes.Add(new CodeAttributeDeclaration("Obsolete",
					new CodeAttributeArgument(new CodePrimitiveExpression(
					"Can't call COM method marked with [local] attribute in IDL file"))));
				attributes.Remove("local");
			}
			if (attributes["restricted"] != null)
			{
				member.CustomAttributes.Add(new CodeAttributeDeclaration("TypeLibFunc",
					new CodeAttributeArgument(new CodeSnippetExpression("TypeLibFuncFlags.FRestricted"))));
				attributes.Remove("restricted");
			}
			if (attributes["warning"] != null)
			{
				member.Comments.Add(new CodeCommentStatement(string.Format("<remarks>{0}</remarks>",
					(string)attributes["warning"]), true));
				attributes.Remove("warning");
			}

			/// Add the attributes
			foreach (DictionaryEntry entry in attributes)
			{
				if (entry.Value is CodeAttributeArgument)
					memberRet.CustomAttributes.Add(new CodeAttributeDeclaration((string)entry.Key,
						(CodeAttributeArgument)entry.Value));
				else
					memberRet.CustomAttributes.Add(new CodeAttributeDeclaration((string)entry.Key));
			}
			attributes.Clear();

			return memberRet;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the return type.
		/// </summary>
		/// <param name="parameters">The parameters.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private CodeParameterDeclarationExpression GetReturnType(
			CodeParameterDeclarationExpressionCollection parameters)
		{
			CodeParameterDeclarationExpression retType = new CodeParameterDeclarationExpression(typeof(void),
				"return");
			foreach (CodeParameterDeclarationExpression exp in parameters)
			{
				if (exp.UserData["retval"] != null && (bool)exp.UserData["retval"] &&
					exp.Type.ArrayRank <=0)
				{	/// Marshalling arrays as return value doesn't work!
					retType = exp;
					parameters.Remove(exp);
					break;
				}
			}

			return retType;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the index of a member method/property by name
		/// </summary>
		/// <param name="types">member collection</param>
		/// <param name="name">name to look for</param>
		/// <returns>index of member if found, otherwise -1</returns>
		/// ------------------------------------------------------------------------------------
		private int IndexOfMember(CodeTypeMemberCollection types, string name)
		{
			foreach (CodeTypeMember member in types)
			{
				if (member.Name == name)
					return types.IndexOf(member);
			}

			return -1;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles all base classes. For IUnknown and IDispatch we set an attribute
		/// instead of adding it to the list of base clases
		/// </summary>
		/// <param name="type">Type description</param>
		/// <param name="nameSpace">The name space.</param>
		/// <param name="attributes">The attributes.</param>
		/// ------------------------------------------------------------------------------------
		public void HandleInterface(CodeTypeDeclaration type, CodeNamespace nameSpace,
			IDictionary attributes)
		{
			type.CustomAttributes.Add(new CodeAttributeDeclaration("ComImport"));

			CodeAttributeDeclarationCollection toRemove = new CodeAttributeDeclarationCollection();

			if (attributes["dual"] != null)
			{
				type.UserData.Add("InterfaceType", "ComInterfaceType.InterfaceIsDual");
				attributes.Remove("dual");
			}

			if (attributes["Guid"] != null)
				m_Namespace.UserData.Add(type.Name + "Guid", attributes["Guid"]);

			StringCollection superClasses = (StringCollection)type.UserData["inherits"];
			// Prepare to remove redundant superclasses
			Dictionary<string, StringCollection> allBases = new Dictionary<string, StringCollection>();
			foreach (string str in superClasses)
				allBases[str] = AllBases(str, nameSpace);
			foreach (string str in superClasses)
			{
				switch (str)
				{
					case "IUnknown":
					case "IDispatch":
						if (type.UserData["InterfaceType"] != null)
						{	/// we had a interface spec previously
							type.UserData["InterfaceType"] = "ComInterfaceType.InterfaceIsDual";
						}
						else
							type.UserData.Add("InterfaceType", "ComInterfaceType.InterfaceIs" + str);
						break;
					default:
					{
						if (type.BaseTypes.Count > 0)
						{
							Console.WriteLine("Error: only one base class supported (interface {0})!",
								type.Name);
						}
						else
						{
							bool fRedundant = false;
							foreach (string other in superClasses)
							{
								// Is this base class contained in another?
								if (other != str && allBases[other].Contains(str))
								{
									fRedundant = true;
									break;
								}
							}
							if (fRedundant)
								break;

							string interfaceType;
							CodeTypeMemberCollection tmpColl = GetBaseMembers(str, nameSpace,
								out interfaceType);
							if (tmpColl != null)
							{
								tmpColl.AddRange(type.Members);
								type.Members.Clear();
								type.Members.AddRange(tmpColl);
								if (type.UserData["InterfaceType"] == null)
									type.UserData.Add("InterfaceType", interfaceType);
							}
							type.BaseTypes.Add(new CodeTypeReference(str));
						}
						break;
					}
				}
			}

			if (type.UserData["InterfaceType"] != null)
			{
				if ((string)type.UserData["InterfaceType"] == "ComInterfaceType.InterfaceIsDual")
					type.UserData.Remove("InterfaceType");
				else
					type.CustomAttributes.Add(new CodeAttributeDeclaration("InterfaceType",
						new CodeAttributeArgument(new CodeSnippetExpression(
						(string)type.UserData["InterfaceType"]))));
			}

			foreach (CodeAttributeDeclaration attr in toRemove)
				type.CustomAttributes.Remove(attr);

			AddAttributesToType(type, attributes);
			attributes.Clear();

#if DEBUG_IDLGRAMMAR
			foreach (CodeTypeMember member in type.Members)
			{
				System.Diagnostics.Debug.WriteLine(string.Format("member={0}.{1}", type.Name,
					member.Name));
			}
#endif
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets alls base class names.
		/// </summary>
		/// <param name="typeName">Name of the type.</param>
		/// <param name="nameSpace">The namespace.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private StringCollection AllBases(string typeName, CodeNamespace nameSpace)
		{
			CodeTypeDeclaration type = (CodeTypeDeclaration)nameSpace.UserData[typeName];
			if (type == null)
			{
				//System.Console.WriteLine("Type missing for {0}", typeName);
				return new StringCollection();
			}
			StringCollection directBases = (StringCollection)type.UserData["inherits"];
			if (directBases == null)
			{
				//System.Console.WriteLine("Bases missing for {0}", typeName);
				return new StringCollection();
			}
			StringCollection result = new StringCollection();
			// This is astonishingly ugly, but I couldn't easily find a better way to do it
			String[] tmp;
			tmp = new String[directBases.Count];
			directBases.CopyTo(tmp, 0);
			result.AddRange(tmp);
			foreach (string _base in directBases)
			{
				StringCollection theseBases = AllBases(_base, nameSpace);
				tmp = new String[theseBases.Count];
				theseBases.CopyTo(tmp, 0);
				result.AddRange(tmp);
			}
			return result;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the type of the attributes to.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <param name="attributes">The attributes.</param>
		/// ------------------------------------------------------------------------------------
		private static void AddAttributesToType(CodeTypeDeclaration type, IDictionary attributes)
		{
			/// Add the attributes
			foreach (DictionaryEntry entry in attributes)
			{
				if (entry.Value is CodeAttributeArgument)
					type.CustomAttributes.Add(new CodeAttributeDeclaration((string)entry.Key,
						(CodeAttributeArgument)entry.Value));
				else
					type.CustomAttributes.Add(new CodeAttributeDeclaration((string)entry.Key));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the co class interface.
		/// </summary>
		/// <param name="type">The type (coClass interface declaration).</param>
		/// <param name="nameSpace">The name space.</param>
		/// <param name="attributes">The attributes.</param>
		/// ------------------------------------------------------------------------------------
		public void HandleCoClassInterface(CodeTypeDeclaration type, CodeNamespace nameSpace,
			IDictionary attributes)
		{
			// Add a start region
			type.StartDirectives.Add(new CodeRegionDirective(CodeRegionMode.Start,
				type.Name + " CoClass definitions"));

			type.CustomAttributes.Add(new CodeAttributeDeclaration("ComImport"));
			type.CustomAttributes.Add(new CodeAttributeDeclaration("CoClass",
				new CodeAttributeArgument(new CodeTypeOfExpression(GetCoClassObjectName(type)))));

			// we have to change the GUID: the interface we're defining here is just a synonym
			// for the first interface this coclass implements. This means we have to replace
			// the GUID with the GUID of the first interface (i.e. base class).
			Hashtable attributesCopy = new Hashtable(attributes);
			CodeAttributeArgument guid =
				m_Namespace.UserData[type.BaseTypes[0].BaseType + "Guid"] as CodeAttributeArgument;
			attributesCopy["Guid"] = guid;

			AddAttributesToType(type, attributesCopy);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Declares the co class object.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <param name="nameSpace">The name space.</param>
		/// <param name="attributes">The attributes.</param>
		/// <returns>The type declaration for the CoClass object. The CoClass object gets
		/// named after the interface it implements with a leading underscore and "Class"
		/// appended.</returns>
		/// ------------------------------------------------------------------------------------
		public CodeTypeDeclaration DeclareCoClassObject(CodeTypeDeclaration type,
			CodeNamespace nameSpace, IDictionary attributes)
		{
			string coClassName = GetCoClassObjectName(type);
			CodeTypeDeclaration coClass = new CodeTypeDeclaration(coClassName);
			coClass.IsClass = true;
			coClass.TypeAttributes = TypeAttributes.NestedAssembly;
			coClass.CustomAttributes.Add(new CodeAttributeDeclaration("ComImport"));
			coClass.CustomAttributes.Add(new CodeAttributeDeclaration("ClassInterface",
				new CodeAttributeArgument(new CodeSnippetExpression("ClassInterfaceType.None"))));
			coClass.CustomAttributes.Add(new CodeAttributeDeclaration("TypeLibType",
				new CodeAttributeArgument(new CodeSnippetExpression("TypeLibTypeFlags.FCanCreate"))));
			coClass.BaseTypes.Add(type.BaseTypes[0]);
			coClass.BaseTypes.Add(new CodeTypeReference(type.Name));
			string interfaceType;
			foreach (CodeTypeReference baseType in type.BaseTypes)
			{
				CodeTypeMemberCollection tmpColl = GetBaseMembers(baseType.BaseType, nameSpace,
					out interfaceType);
				if (tmpColl != null)
				{
					// adjust attributes
					foreach (CodeTypeMember member in tmpColl)
					{
						//member.Attributes &= ~MemberAttributes.New;
						member.Attributes = MemberAttributes.Public;
						member.UserData.Add("extern", true);

						CodeAttributeDeclaration methodImplAttr =
							new CodeAttributeDeclaration("MethodImpl",
								new CodeAttributeArgument(
								new CodeSnippetExpression("MethodImplOptions.InternalCall")),
								new CodeAttributeArgument("MethodCodeType",
								new CodeSnippetExpression("MethodCodeType.Runtime")));

						if (member is CodeMemberProperty)
						{
							// for a property the attribute must be on the get/set, not on
							// the enclosing property. The default C# code generator doesn't
							// suport this, so we handle this in our custom code generator.
							CodeMemberProperty prop = member as CodeMemberProperty;
							CodeAttributeDeclarationCollection attrColl =
								new CodeAttributeDeclarationCollection(
								new CodeAttributeDeclaration[] { methodImplAttr });
							if (prop.HasGet)
								prop.UserData["get_attrs"] = attrColl;
							if (prop.HasSet)
								prop.UserData["set_attrs"] = attrColl;
						}
						else
						{
							member.CustomAttributes.Add(methodImplAttr);
						}
					}
					tmpColl.AddRange(coClass.Members);
					coClass.Members.Clear();
					coClass.Members.AddRange(tmpColl);
				}
			}

			// Add a region around this class
			coClass.StartDirectives.Add(new CodeRegionDirective(CodeRegionMode.Start,
				"Private " + coClassName + " class"));
			coClass.EndDirectives.Add(new CodeRegionDirective(CodeRegionMode.End, coClassName));

			AddAttributesToType(coClass, attributes);

			return coClass;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Declares the coclass creator class.
		/// </summary>
		/// <param name="type">The type name.</param>
		/// <param name="nameSpace">The name space.</param>
		/// <param name="attributes">The attributes.</param>
		/// <returns>coclass creator class declaration.</returns>
		/// ------------------------------------------------------------------------------------
		public CodeTypeDeclaration DeclareCoClassCreator(CodeTypeDeclaration type,
			CodeNamespace nameSpace, IDictionary attributes)
		{
			CodeTypeDeclaration coClassCreator = new CodeTypeDeclaration(type.Name + "Class");
			coClassCreator.TypeAttributes = TypeAttributes.Public;
			coClassCreator.Attributes = MemberAttributes.Static;

			// .NET 2.0 allows static classes, but unfortunately the C# code generator
			// doesn't have a way to generate code that way directly yet, so we add a userdata
			// property and deal with that in our custom code generator.
			coClassCreator.UserData.Add("static", true);

			// add delegate declaration
			string delegateName = coClassCreator.Name + "Delegate";

			CodeTypeReference returnType = new CodeTypeReference(type.Name);
			CodeTypeDelegate deleg = new CodeTypeDelegate(delegateName);
			deleg.ReturnType = returnType;
			// There's no way to cause the code generator to put the keyword "private" in front -
			// all we can do is ommit "public".
			deleg.TypeAttributes = TypeAttributes.NestedPrivate;
			coClassCreator.Members.Add(deleg);

			// add the Create() method declaration
			CodeMemberMethod createMethod = new CodeMemberMethod();
			createMethod.Attributes = MemberAttributes.Static | MemberAttributes.Public;
			createMethod.Name = "Create";
			createMethod.ReturnType = returnType;

			// Now add this code:
			// if (Application.OpenForms.Count > 0)
			//{
			//    Form form = Application.OpenForms[0];
			//    if (form.InvokeRequired)
			//    {
			//        return (ITsPropsBldr)form.Invoke(new CreateTsPropsBldrClassDelegate(Create));
			//    }
			//}
			CodeConditionStatement ifStatement = new CodeConditionStatement();
			// ENHANCE: this doesn't work very well if we ever want to generate in a language
			// other then C#.
			// if (Application.OpenForms.Count > 0)
			ifStatement.Condition = new CodeBinaryOperatorExpression(
					new CodeSnippetExpression("Application.OpenForms.Count"),
					CodeBinaryOperatorType.GreaterThan,
					new CodePrimitiveExpression(0));

			ifStatement.TrueStatements.Add(
				new CodeSnippetExpression("Form form = Application.OpenForms[0]"));
			CodeConditionStatement nestedIf = new CodeConditionStatement();
			nestedIf.Condition = new CodeSnippetExpression("form.InvokeRequired");
			nestedIf.TrueStatements.Add(new CodeMethodReturnStatement(
				new CodeSnippetExpression(
				string.Format("({0})form.Invoke(new {1}(Create))",
				returnType.BaseType, delegateName))));
			ifStatement.TrueStatements.Add(nestedIf);
			createMethod.Statements.Add(ifStatement);
			createMethod.Statements.Add(new CodeMethodReturnStatement(
				new CodeObjectCreateExpression(GetCoClassObjectName(type))));

			coClassCreator.Members.Add(createMethod);

			coClassCreator.EndDirectives.Add(new CodeRegionDirective(CodeRegionMode.End, string.Empty));

			Dictionary<string, IdhCommentProcessor.CommentInfo> childMethods =
				new Dictionary<string, IdhCommentProcessor.CommentInfo>();

			childMethods.Add("Create", new IdhCommentProcessor.CommentInfo("Creates a new " +
				type.Name + " object", null, 0));
			IDLImporter.s_MoreComments.Add(coClassCreator.Name,
				new IdhCommentProcessor.CommentInfo("Helper class used to create a new instance of the "
				+ type.Name + " COM object", childMethods, 0));
			return coClassCreator;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the co class object.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private static string GetCoClassObjectName(CodeTypeDeclaration type)
		{
			return string.Format("_{0}Class", type.Name);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a copy of the collection of the methods of the base type
		/// </summary>
		/// <param name="typeName">The name of the base type</param>
		/// <param name="nameSpace">The namespace which defines the base types</param>
		/// <param name="interfaceType">[out] Returns the interface type of the base type</param>
		/// <returns>A copy of the collection of the methods</returns>
		/// ------------------------------------------------------------------------------------
		private CodeTypeMemberCollection GetBaseMembers(string typeName,
			CodeNamespace nameSpace, out string interfaceType)
		{
			interfaceType = null;
			if (nameSpace.UserData[typeName] == null)
			{
				System.Console.WriteLine("Error: base type {0} not found!", typeName);
				return null;
			}
			else
			{
				CodeTypeDeclaration type = (CodeTypeDeclaration)nameSpace.UserData[typeName];
				interfaceType = (string)type.UserData["InterfaceType"];
				CodeTypeMemberCollection coll = new CodeTypeMemberCollection();

				/// All base class members must be preceded by new
				foreach (CodeTypeMember member in type.Members)
				{
					CodeTypeMember newMember;
					if (member is CodeMemberMethod)
					{
						newMember = new CodeMemberMethod();
					}
					else if (member is CodeMemberProperty)
						newMember = new CodeMemberProperty();
					else
					{
						Console.WriteLine("Unhandled member type: {0}", member.GetType());
						continue;
					}

					newMember.Attributes = member.Attributes | MemberAttributes.New;
					newMember.Attributes = newMember.Attributes & ~MemberAttributes.AccessMask |
						MemberAttributes.Public;
					newMember.Comments.AddRange(member.Comments);
					newMember.CustomAttributes.AddRange(member.CustomAttributes);
					newMember.Name = member.Name;
					if (member is CodeMemberMethod)
					{
						((CodeMemberMethod)newMember).ImplementationTypes.AddRange(((CodeMemberMethod)member).ImplementationTypes);
						((CodeMemberMethod)newMember).Parameters.AddRange(((CodeMemberMethod)member).Parameters);
						((CodeMemberMethod)newMember).ReturnType = ((CodeMemberMethod)member).ReturnType;
						((CodeMemberMethod)newMember).ReturnTypeCustomAttributes.AddRange(((CodeMemberMethod)member).ReturnTypeCustomAttributes);
					}
					else if (member is CodeMemberProperty)
					{
						((CodeMemberProperty)newMember).ImplementationTypes.AddRange(((CodeMemberProperty)member).ImplementationTypes);
						((CodeMemberProperty)newMember).Type = ((CodeMemberProperty)member).Type;
						((CodeMemberProperty)newMember).HasGet = ((CodeMemberProperty)member).HasGet;
						((CodeMemberProperty)newMember).HasSet = ((CodeMemberProperty)member).HasSet;
					}
					foreach (DictionaryEntry entry in member.UserData)
						newMember.UserData.Add(entry.Key, entry.Value);

					coll.Add(newMember);
				}

				return coll;
			}
		}

		#endregion

		#region Conversions based on XML configuration file
		/// <summary>
		/// Make conversions based on attributes
		/// </summary>
		/// <param name="type">Type of parameter</param>
		/// <param name="sParameter">original parameter string</param>
		/// <param name="param">parameter description</param>
		/// <returns></returns>
		public CodeTypeReference ConvertParamType(string sOriginalParameter,
			CodeParameterDeclarationExpression param, IDictionary attributes)
		{
			CodeTypeReference type = new CodeTypeReference(string.Empty);
			string sParameter = sOriginalParameter;

			if (m_ParamTypes != null)
			{
				for (int i = 0; i < m_ParamTypes.Length; i++)
				{
					ConversionEntry entry = m_ParamTypes[i];
					if (entry.Regex.IsMatch(sParameter))
					{
						bool fMatch = false;
						if (entry.Attributes != null)
						{
							int iAttribute = 0;
							foreach(string attr in entry.Attributes)
							{
								iAttribute++;
								string attribute = attr.Trim();
								bool fNegate = false;
								if (attribute[0] == '~')
								{
									fNegate = true;
									attribute = attribute.Substring(1);
								}

								if (attributes[attribute] != null)
								{
									object rawArg = attributes[attribute];
									CodeAttributeArgument arg;

									if (rawArg.GetType() != typeof(CodeAttributeArgument))
										arg = new CodeAttributeArgument(new CodePrimitiveExpression(rawArg));
									else
										arg = (CodeAttributeArgument)rawArg;

									/// We test the attribute value only for the first attribute!
									if (entry.AttrValue == null || (iAttribute <= 1
										&& entry.AttrValue == (string)((CodePrimitiveExpression)arg.Value).Value))
									{
										if (entry.AttrValueName == null || entry.AttrValueName == arg.Name)
										{
											if (!fNegate && (iAttribute <= 1 || fMatch == true))
												fMatch = true;
											else
											{
												fMatch = false;
												break;
											}
										}
									}
								}
								else if (fNegate && (iAttribute <= 1 || fMatch == true))
								{ /// attribute not found, and we don't want to have it, so it's a match
									fMatch = true;
								}
								else
								{
									fMatch = false;
									break;
								}
							}
						}
						else
							fMatch = true;

						if (fMatch)
						{
							sParameter = entry.Regex.Replace(sParameter, entry.Replace);

							if (entry.NewAttributes != null)
							{
								int iAttribute = 0;
								foreach(string attr in entry.NewAttributes)
								{
									iAttribute++;
									string attribute = attr.Trim();

									if (attribute[0] == '-')
									{
										attributes.Remove(attribute.Substring(1));
									}
									else if (iAttribute == 1 && param != null)
									{
										// we only deal with one attribute to add
										if (entry.NewAttrValue == null)
										{
											// attribute without value
											param.CustomAttributes.Add(new CodeAttributeDeclaration(
												attribute));
										}
										else
										{
											// attribute with value
											CodeAttributeArgument arg;
											if (entry.NewAttrValueName == null)
											{
												// attribute with unnamed value
												arg = new CodeAttributeArgument(
													new CodeSnippetExpression(entry.NewAttrValue));
											}
											else
											{
												// attribute with named value
												arg = new CodeAttributeArgument(entry.NewAttrValueName,
													new CodeSnippetExpression(entry.NewAttrValue));
											}

											param.CustomAttributes.Add(new CodeAttributeDeclaration(
												attribute, arg));
										}
									}
								}
							}

							if (entry.fEnd)
								break;
						}
					}
				}
			}

			/// Remove the parameter name from the end
			Regex regex = new Regex("\\s+[^\\s]+[^\\w]*$");
			type.BaseType = regex.Replace(sParameter.TrimStart(null), "");

			Regex regexArray = new Regex("\\[\\s*\\]\\s*$");
			if (regexArray.IsMatch(type.BaseType))
			{
				type.BaseType = regexArray.Replace(type.BaseType, "");
				CodeTypeReference tmpType = new CodeTypeReference(type, 1);
				type = tmpType;
			}

			if (param != null)
				HandleInOut(param, attributes);

			// Put size_is to UserData so that we can deal with it later when we have all
			// parameters
			string varName = (string)attributes["size_is"];
			if (varName != null && varName.Length > 0 && param != null)
				param.UserData.Add("size_is", varName);
			attributes.Remove("size_is");
			attributes.Remove("string");

			if (attributes["retval"] != null)
			{
				if (param != null)
					param.UserData.Add("retval", attributes["retval"]);
				attributes.Remove("retval");
			}

			if (attributes["IsArray"] != null)
				attributes.Remove("IsArray");

			/// Add the attributes
			if (param != null)
			{
				foreach (DictionaryEntry entry in attributes)
				{
					if (entry.Value is CodeAttributeArgument)
						param.CustomAttributes.Add(new CodeAttributeDeclaration((string)entry.Key,
							(CodeAttributeArgument)entry.Value));
					else
						param.CustomAttributes.Add(new CodeAttributeDeclaration((string)entry.Key));
				}
			}
			attributes.Clear();

			return type;
		}

		public static string ConvertParamName(string input)
		{
			string strRet = input;
			if (s_ParamNames != null)
			{
				for (int i = 0; i < s_ParamNames.Length; i++)
				{
					ConversionEntry entry = s_ParamNames[i];
					if (entry.Regex.IsMatch(input))
						strRet = entry.Regex.Replace(strRet, entry.Replace);
				}
			}

			return strRet.TrimStart(null);
		}

		#endregion

		#region Helper methods

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle [in][out] attributes and set direction accordingly
		/// </summary>
		/// <param name="param">parameter declaration</param>
		/// <param name="attributes">list of attributes</param>
		/// ------------------------------------------------------------------------------------
		private void HandleInOut(CodeParameterDeclarationExpression param, IDictionary attributes)
		{
			if (attributes["out"] != null && (bool)attributes["out"])
			{
				if (attributes["in"] != null && (bool)attributes["in"])
				{
					param.Direction = FieldDirection.Ref;
				}
				else
					param.Direction = FieldDirection.Out;
			}
			else
				param.Direction = FieldDirection.In;

			attributes.Remove("out");
			attributes.Remove("in");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// For a size_is attribute in IDL we have to add a SizeParmIndex to the code which
		/// contains the index of the paramater that specifies the size of the array.
		/// </summary>
		/// <param name="method">The method we're dealing with.</param>
		/// <param name="attributes">list of attributes</param>
		/// ------------------------------------------------------------------------------------
		public void HandleSizeIs(CodeMemberMethod method, IDictionary attributes)
		{
			// loop through all parameters and look if they have a size_is attribute
			foreach(CodeParameterDeclarationExpression param in method.Parameters)
			{
				if (param.UserData.Contains("size_is"))
				{
					string attributeParamName = "SizeParamIndex";
					string varName = ConvertParamName((string)param.UserData["size_is"]);
					int nValue;
					if (int.TryParse(varName, out nValue))
					{
						// We have a fixed length, so use that
						attributeParamName = "SizeConst";
					}
					else
					{
						// now search for the parameter named varName that contains the size
						// of the array
						int i;
						for (i = 0; i < method.Parameters.Count; i++)
						{
							if (method.Parameters[i].Name == varName)
							{
								nValue = i;
								break;
							}
						}
						if (i == method.Parameters.Count && !attributes.Contains("restricted"))
						{
							// if it's a restricted method we don't care
							Console.WriteLine("Internal error: couldn't find MarshalAs " +
								"attribute for parameter {0} of method {1}", param.Name, method.Name);
							attributes.Add("warning",
								string.Format("NOTE: This method probably doesn't work since it caused " +
								"an error on IDL import for parameter {0}", param.Name));
						}
					}

					// we found the parameter, now find the attribute
					foreach (CodeAttributeDeclaration attribute in param.CustomAttributes)
					{
						if (attribute.Name == "MarshalAs")
						{
							attribute.Arguments.Add(new CodeAttributeArgument(
								attributeParamName, new CodePrimitiveExpression(nValue)));
						}
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates an enum member.
		/// </summary>
		/// <param name="enumName">Name of the enumeration.</param>
		/// <param name="name">The name.</param>
		/// <param name="value">The value.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static CodeMemberField CreateEnumMember(string enumName, string name,
			string value)
		{
			CodeMemberField member = new CodeMemberField();
			member.Name = name;
			if (s_EnumMemberMapping.ContainsKey(name))
				throw new ApplicationException(string.Format("{0} is defined in both {1} and {2}",
					name, enumName, s_EnumMemberMapping[name]));

			if (enumName != string.Empty)
				s_EnumMemberMapping.Add(name, enumName);

			if (value != string.Empty)
			{
				int val;
				if (int.TryParse(value, out val) || (value.StartsWith("0x") &&
					int.TryParse(value.Substring(2), NumberStyles.HexNumber, null, out val)))
					member.InitExpression = new CodePrimitiveExpression(val);
				else
				{
					CodeFieldReferenceExpression fieldRef = new CodeFieldReferenceExpression();
					member.InitExpression = fieldRef;

					// The value might be a reference to an enum member defined in another
					// enumeration. While this is fine in C++/IDL, we need to add the enum name
					// to it.
					fieldRef.FieldName = ResolveReferences(enumName, value, fieldRef, false);
				}
			}
			return member;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Resolves references in the value.
		/// </summary>
		/// <param name="enumName">The name of the enumeration.</param>
		/// <param name="value">The value.</param>
		/// <param name="fieldRef">The field ref.</param>
		/// <param name="fFinal"><c>true</c> to process string even if we can't find potential
		/// reference, <c>false</c> to add it to a list for later processing.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private static string ResolveReferences(string enumName, string value,
			CodeFieldReferenceExpression fieldRef, bool fFinal)
		{
			StringBuilder bldr = new StringBuilder(value);
			Regex regex = new Regex(@"\w+");

			MatchCollection matches = regex.Matches(value);
			for (int i = matches.Count; i > 0; i--)
			{
				Match match = matches[i-1];
				string refMember = match.Value;
				if (s_EnumMemberMapping.ContainsKey(refMember))
				{
					// need to do this only if it's defined in a different enumeration
					if (s_EnumMemberMapping[refMember] != enumName)
					{
						bldr.Remove(match.Index, match.Length);
						bldr.Insert(match.Index, string.Format("{0}.{1}", s_EnumMemberMapping[refMember],
							refMember));
					}
				}
				else if (!fFinal)
				{
					// maybe it's referencing a type that we haven't processed yet, so try it
					// again later
					s_NeedsAdjustment.Add(fieldRef, enumName);
					break;
				}
				// otherwise just leave it as it is
			}
			return bldr.ToString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adjusts the references in enums that we couldn't resolve earlier because they
		/// referenced something that came later in the file.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void AdjustReferencesInEnums()
		{
			foreach (CodeFieldReferenceExpression fieldRef in s_NeedsAdjustment.Keys)
			{
				fieldRef.FieldName = ResolveReferences(s_NeedsAdjustment[fieldRef],
					fieldRef.FieldName, fieldRef, true);
			}
		}
		#endregion
	}

}
