//-------------------------------------------------------------------------------------------------
// <copyright file="GenerateMessageFiles.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
//
//    The use and distribution terms for this software are covered by the
//    Common Public License 1.0 (http://opensource.org/licenses/cpl.php)
//    which can be found in the file CPL.TXT at the root of this distribution.
//    By using this software in any fashion, you are agreeing to be bound by
//    the terms of this license.
//
//    You must not remove this notice, or any other, from this software.
// </copyright>
//
// <summary>
// Message files generation class.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.MsgGen
{
	using System;
	using System.CodeDom;
	using System.Collections;
	using System.Globalization;
	using System.Resources;
	using System.Xml;

	/// <summary>
	/// Message files generation class.
	/// </summary>
	public class GenerateMessageFiles
	{
		/// <summary>
		/// Generate the message files.
		/// </summary>
		/// <param name="messagesDoc">Input Xml document containing message definitions.</param>
		/// <param name="codeCompileUnit">CodeDom container.</param>
		/// <param name="resourceWriter">Writer for default resource file.</param>
		public static void Generate(XmlDocument messagesDoc, CodeCompileUnit codeCompileUnit, ResourceWriter resourceWriter)
		{
			Hashtable usedNumbers = new Hashtable();

			if (null == messagesDoc)
			{
				throw new ArgumentNullException("messagesDoc");
			}

			if (null == codeCompileUnit)
			{
				throw new ArgumentNullException("codeCompileUnit");
			}

			if (null == resourceWriter)
			{
				throw new ArgumentNullException("resourceWriter");
			}

			string namespaceAttr = messagesDoc.DocumentElement.GetAttribute("Namespace");
			string resourcesAttr = messagesDoc.DocumentElement.GetAttribute("Resources");

			// namespace
			CodeNamespace messagesNamespace = new CodeNamespace(namespaceAttr);
			codeCompileUnit.Namespaces.Add(messagesNamespace);

			// imports
			messagesNamespace.Imports.Add(new CodeNamespaceImport("System"));
			messagesNamespace.Imports.Add(new CodeNamespaceImport("System.Reflection"));
			messagesNamespace.Imports.Add(new CodeNamespaceImport("System.Resources"));
			if (namespaceAttr != "Microsoft.Tools.WindowsInstallerXml")
			{
				messagesNamespace.Imports.Add(new CodeNamespaceImport("Microsoft.Tools.WindowsInstallerXml"));
			}

			foreach (XmlElement classElement in messagesDoc.DocumentElement.ChildNodes)
			{
				int index = -1;
				string className = classElement.GetAttribute("Name");
				string baseContainerName = classElement.GetAttribute("BaseContainerName");
				string containerName = classElement.GetAttribute("ContainerName");
				string levelType = classElement.GetAttribute("LevelType");
				string startNumber = classElement.GetAttribute("StartNumber");

				// automatically generate message numbers
				if (0 < startNumber.Length)
				{
					index = Convert.ToInt32(startNumber, CultureInfo.InvariantCulture);
				}

				// message container class
				messagesNamespace.Types.Add(CreateContainer(namespaceAttr, baseContainerName, containerName, resourcesAttr));

				// class
				CodeTypeDeclaration messagesClass = new CodeTypeDeclaration(className);
				messagesNamespace.Types.Add(messagesClass);

				// messages
				foreach (XmlElement messageElement in classElement.ChildNodes)
				{
					int number;
					string id = messageElement.GetAttribute("Id");
					string level = messageElement.GetAttribute("Level");
					string numberString = messageElement.GetAttribute("Number");
					bool sourceLineNumbers = true;

					// determine the message number (and ensure it was set properly)
					if (0 > index) // manually specified message number
					{
						if (0 < numberString.Length)
						{
							number = Convert.ToInt32(numberString, CultureInfo.InvariantCulture);
						}
						else
						{
							throw new ApplicationException(String.Format("Message number must be assigned for {0} '{1}'.", containerName, id));
						}
					}
					else // automatically generated message number
					{
						if (0 == numberString.Length)
						{
							number = index++;
						}
						else
						{
							throw new ApplicationException(String.Format("Message number cannot be assigned for {0} '{1}' since it will be automatically generated.", containerName, id));
						}
					}

					// check for message number collisions
					if (usedNumbers.Contains(number))
					{
						throw new ApplicationException(String.Format("Collision detected between two or more messages with number '{0}'.", number));
					}
					usedNumbers.Add(number, null);

					if ("no" == messageElement.GetAttribute("SourceLineNumbers"))
					{
						sourceLineNumbers = false;
					}

					int instanceCount = 0;
					foreach (XmlElement instanceElement in messageElement.ChildNodes)
					{
						string formatString = instanceElement.InnerText.Trim();
						string resourceName = String.Concat(className, "_", id, "_", (++instanceCount).ToString());

						// create a resource
						resourceWriter.AddResource(resourceName, formatString);

						// create method
						CodeMemberMethod method = new CodeMemberMethod();
						method.ReturnType = new CodeTypeReference(containerName);
						method.Attributes = MemberAttributes.Public | MemberAttributes.Static;
						messagesClass.Members.Add(method);

						// method name
						method.Name = id;

						// return statement
						CodeMethodReturnStatement stmt = new CodeMethodReturnStatement();
						method.Statements.Add(stmt);

						// return statement expression
						CodeObjectCreateExpression expr = new CodeObjectCreateExpression();
						stmt.Expression = expr;

						// new struct
						expr.CreateType = new CodeTypeReference(containerName);

						// optionally have sourceLineNumbers as the first parameter
						if (sourceLineNumbers)
						{
							// sourceLineNumbers parameter
							expr.Parameters.Add(new CodeArgumentReferenceExpression("sourceLineNumbers"));
						}
						else
						{
							expr.Parameters.Add(new CodePrimitiveExpression(null));
						}

						// message number parameter
						expr.Parameters.Add(new CodePrimitiveExpression(number));

						// message level parameter (optional)
						if (string.Empty != levelType)
						{
							if (string.Empty != level)
							{
								expr.Parameters.Add(new CodeCastExpression(typeof(int), new CodeArgumentReferenceExpression(String.Concat(levelType, ".", level))));
							}
							else
							{
								expr.Parameters.Add(new CodeCastExpression(typeof(int), new CodeArgumentReferenceExpression("level")));
							}
						}
						else
						{
							expr.Parameters.Add(new CodePrimitiveExpression(-1));
						}

						// resource name parameter
						expr.Parameters.Add(new CodePrimitiveExpression(resourceName));

						// optionally have sourceLineNumbers as the first parameter
						if (sourceLineNumbers)
						{
							method.Parameters.Add(new CodeParameterDeclarationExpression("SourceLineNumberCollection", "sourceLineNumbers"));
						}

						// message level parameter (optional)
						if (string.Empty != levelType && string.Empty == level)
						{
							method.Parameters.Add(new CodeParameterDeclarationExpression(levelType, "level"));
						}

						foreach (XmlNode parameterNode in instanceElement.ChildNodes)
						{
							XmlElement parameterElement;

							if (null != (parameterElement = parameterNode as XmlElement))
							{
								string type = parameterElement.GetAttribute("Type");
								string name = parameterElement.GetAttribute("Name");

								// method parameter
								method.Parameters.Add(new CodeParameterDeclarationExpression(type, name));

								// String.Format parameter
								expr.Parameters.Add(new CodeArgumentReferenceExpression(name));
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// Create message container class.
		/// </summary>
		/// <param name="namespaceName">Namespace to use for resources stream.</param>
		/// <param name="baseContainerName">Name of the base message container class.</param>
		/// <param name="containerName">Name of the message container class.</param>
		/// <param name="resourcesName">Name of the resources stream (will get namespace prepended).</param>
		/// <returns>Message container class CodeDom object.</returns>
		private static CodeTypeDeclaration CreateContainer(string namespaceName, string baseContainerName, string containerName, string resourcesName)
		{
			CodeTypeDeclaration messageContainer = new CodeTypeDeclaration();

			messageContainer.Name = containerName;
			messageContainer.BaseTypes.Add(new CodeTypeReference(baseContainerName));

			// constructor
			CodeConstructor constructor = new CodeConstructor();
			constructor.Attributes = MemberAttributes.Public;
			constructor.ReturnType = null;
			messageContainer.Members.Add(constructor);

			CodeMemberField resourceManager = new CodeMemberField();
			resourceManager.Attributes = MemberAttributes.Private | MemberAttributes.Static;
			resourceManager.Name = "resourceManager";
			resourceManager.Type = new CodeTypeReference("ResourceManager");
			resourceManager.InitExpression = new CodeObjectCreateExpression("ResourceManager", new CodeSnippetExpression(String.Format("\"{0}.{1}\"", namespaceName, resourcesName)), new CodeSnippetExpression("Assembly.GetExecutingAssembly()"));
			messageContainer.Members.Add(resourceManager);

			// constructor parameters
			constructor.Parameters.Add(new CodeParameterDeclarationExpression("SourceLineNumberCollection", "sourceLineNumbers"));
			constructor.Parameters.Add(new CodeParameterDeclarationExpression(typeof(int), "id"));
			constructor.Parameters.Add(new CodeParameterDeclarationExpression(typeof(int), "level"));
			constructor.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "resourceName"));
			CodeParameterDeclarationExpression messageArgsParam = new CodeParameterDeclarationExpression("params object[]", "messageArgs");
			constructor.Parameters.Add(messageArgsParam);

			constructor.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("sourceLineNumbers"));
			constructor.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("id"));
			constructor.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("level"));
			constructor.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("resourceName"));
			constructor.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("messageArgs"));

			CodeMemberProperty resourceManagerProperty = new CodeMemberProperty();
			resourceManagerProperty.Attributes = MemberAttributes.Public | MemberAttributes.Override;
			resourceManagerProperty.Name = "ResourceManager";
			resourceManagerProperty.Type = new CodeTypeReference("ResourceManager");
			CodeFieldReferenceExpression resourceManagerReference = new CodeFieldReferenceExpression();
			resourceManagerReference.FieldName = "resourceManager";
			resourceManagerProperty.GetStatements.Add(new CodeMethodReturnStatement(resourceManagerReference));
			messageContainer.Members.Add(resourceManagerProperty);

			return messageContainer;
		}
	}
}
