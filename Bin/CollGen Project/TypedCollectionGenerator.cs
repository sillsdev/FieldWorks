
namespace TypedCollectionBuilder
{
	using System;
	using System.CodeDom;
	using System.CodeDom.Compiler;
	using System.IO;
	using Microsoft.JScript;
	using System.Reflection;
	using System.Collections;
	using Microsoft.CSharp;
	using Microsoft.VisualBasic;


	/// <summary>
	///    Summary description for TypedCollectionGenerator.
	/// </summary>
	public class TypedCollectionGenerator
	{

		string fileName = null;
		string collectionTypeName = null;
		string lang = null;
		string path = null;
		string _extension = null;
		string nameSpace = null;
		string collectionTypeNameSpace = null;
		string authorName = null;
		bool readOnly = false;
		bool addValidation = false;
		bool addDispose = false;
		bool generateEnum = true;
		bool generateEnumAsNested = true;
		bool serializable = true;
		bool generateComments = true;
		bool addEditorAttribute = true;

		/// <summary>
		/// Default constructor for the class
		/// </summary>
		public TypedCollectionGenerator()
		{
		}

		public bool Serializable
		{
			get
			{
				return serializable;
			}
			set
			{
				serializable = value;
			}
		}

		public bool AddEditorAttribute
		{
			get { return addEditorAttribute; }
			set { addEditorAttribute = value; }
		}

		public bool GenerateComments
		{
			get
			{
				return generateComments;
			}
			set
			{
				generateComments = value;
			}
		}

		public bool GenerateEnum
		{
			get
			{
				return generateEnum;
			}
			set
			{
				generateEnum = value;
			}
		}

		public bool GenerateEnumAsNested
		{
			get
			{
				return generateEnumAsNested;
			}
			set
			{
				generateEnumAsNested = value;
			}
		}

		public string Path
		{
			get
			{
				return path;
			}
			set
			{
				path = value;
			}
		}

		public string FileName
		{
			get
			{
				return fileName;
			}
			set
			{
				fileName = value;
			}
		}

		public string Extension
		{
			get
			{
				return _extension;
			}
			set
			{
				_extension = value;
			}
		}

		public string NameSpace
		{
			get
			{
				return nameSpace;
			}
			set
			{
				nameSpace = value;
			}
		}
		public string CollectionTypeNameSpace
		{
			get
			{
				return collectionTypeNameSpace;
			}
			set
			{
				collectionTypeNameSpace = value;
			}
		}

		public string AuthorName
		{
			get
			{
				return authorName;
			}
			set
			{
				authorName = value;
			}
		}

		public bool AddValidation
		{
			get
			{
				return addValidation;
			}
			set
			{
				addValidation = value;
			}
		}

		public bool AddDispose
		{
			get
			{
				return addDispose;
			}
			set
			{
				addDispose = value;
			}
		}

		public bool ReadOnly
		{
			get
			{
				return readOnly;
			}
			set
			{
				readOnly = value;
			}
		}

		public string CollectionTypeName
		{
			get
			{
				return collectionTypeName;
			}
			set
			{
				collectionTypeName = value;
			}
		}
		public string Language
		{
			get
			{
				return lang;
			}
			set
			{
				lang = value;
			}
		}

		private void setDefaultPath()
		{
			path = Environment.CurrentDirectory;
		}

		public void Generate(string Path, string fileExt)
		{
			if (Path.Length > 0)
				path = Path;
			else
				this.setDefaultPath();

			Extension = fileExt;
			Generate();
		}

		public string GenerateToString()
		{
			StringWriter sw = new StringWriter();
			Generate(sw);
			return sw.ToString();
		}

		private void Generate(TextWriter t)
		{
			CodeMemberMethod cm = null;
			CodeDomProvider codeProvider = null;
			ICodeGenerator cg = null;


			// Validate the selected language
			if (lang.ToUpper() == "VB" && (Extension == "" || Extension == null))
			{
				Extension = "vb";
			}
			else if (lang.ToUpper() == "CS" && (Extension == "" || Extension == null))
			{
				Extension = "cs";
			}
			else if (lang.ToUpper() == "JSCRIPT" && (Extension == "" || Extension == null))
			{
				Extension = "js";
			}
			else if (Extension == "" || Extension == null)
			{
				throw new ArgumentException("Invalid languge: Select VB, CS, JSCRIPT", "Language");
			}

			// Validate the selected language
			if (lang.ToUpper() == "VB")
			{
				codeProvider = new Microsoft.VisualBasic.VBCodeProvider();
			}
			else if (lang.ToUpper() == "CS")
			{
				codeProvider = new Microsoft.CSharp.CSharpCodeProvider();
			}
			else if (lang.ToUpper() == "JSCRIPT")
			{
				codeProvider = new Microsoft.JScript.JScriptCodeProvider();
			}
			else
			{
				throw new ArgumentException("Invalid languge: Select VB, CS, JSCRIPT", "Language");
			}

			cg = codeProvider.CreateGenerator();

			// Setup the temp collection type name
			string workCollectionTypeName = null;


			//	       if (null != collectionTypeNameSpace && collectionTypeNameSpace.Length > 0)
			//			workCollectionTypeName = collectionTypeNameSpace + "." + collectionTypeName;
			if (null != collectionTypeNameSpace && collectionTypeNameSpace.Length > 0)
				workCollectionTypeName = collectionTypeName;
			else
				workCollectionTypeName = collectionTypeName;

			//Create the class header =====================
			//Through the codedom we build an instance of a namespace object and
			//add our imports/using statements
			CodeNamespace cnamespace = new CodeNamespace(nameSpace);
			cnamespace.Imports.Add (new CodeNamespaceImport ("System") );
			cnamespace.Imports.Add (new CodeNamespaceImport ("System.Collections") );

			if (addEditorAttribute)
			{
				cnamespace.Imports.Add(new CodeNamespaceImport("System.ComponentModel")); // for EditorAttribute
				cnamespace.Imports.Add(new CodeNamespaceImport("System.ComponentModel.Design")); // for CollectionEditor
				cnamespace.Imports.Add(new CodeNamespaceImport("System.Drawing.Design")); // for UITypeEditor
			}

			if (collectionTypeNameSpace != null && collectionTypeNameSpace.Length > 0)
				cnamespace.Imports.Add(new CodeNamespaceImport(collectionTypeNameSpace));

			if (generateComments)
			{
				cnamespace.Comments.Add(new CodeCommentStatement("------------------------------------------------------------------------------", false));
				cnamespace.Comments.Add(new CodeCommentStatement("<copyright from='2002' to='2002' company='SIL International'>", false));
				cnamespace.Comments.Add(new CodeCommentStatement("   Copyright (c) 2002, SIL International. All Rights Reserved.   ", false));
				cnamespace.Comments.Add(new CodeCommentStatement("</copyright> ", false));
				cnamespace.Comments.Add(new CodeCommentStatement("", false));
				cnamespace.Comments.Add(new CodeCommentStatement("File: " + fileName + "." + Extension, false));
				cnamespace.Comments.Add(new CodeCommentStatement("Responsibility: " + authorName, false));
				cnamespace.Comments.Add(new CodeCommentStatement("Last reviewed: ", false));
				cnamespace.Comments.Add(new CodeCommentStatement("", false));
				cnamespace.Comments.Add(new CodeCommentStatement("Implementation of strongly-typed collection " + collectionTypeName + "Collection", false));
				cnamespace.Comments.Add(new CodeCommentStatement("", false));
				cnamespace.Comments.Add(new CodeCommentStatement("<remarks>Automatically created by CollectionGenerator</remarks>", false));
				cnamespace.Comments.Add(new CodeCommentStatement("------------------------------------------------------------------------------", false));
				cnamespace.Comments.Add(new CodeCommentStatement("", false));
			}

			//Create an instance of a CodeTypeDeclaration object from the codedom and
			//add it to the namespace
			CodeTypeDeclaration co = new CodeTypeDeclaration (collectionTypeName + "Collection");
			cnamespace.Types.Add(co);
			co.BaseTypes.Add(new CodeTypeReference("CollectionBase"));
			if (addDispose)
				co.BaseTypes.Add(new CodeTypeReference("IDisposable"));

			co.TypeAttributes  = TypeAttributes.Public;

			if (serializable)
			{
				co.CustomAttributes.Add(new CodeAttributeDeclaration("Serializable"));
			}

			if (addEditorAttribute)
			{
				co.CustomAttributes.Add(new CodeAttributeDeclaration("Editor", new CodeAttributeArgument[]
					{ new CodeAttributeArgument( new CodeTypeOfExpression("CollectionEditor")),
					  new CodeAttributeArgument( new CodeTypeOfExpression("UITypeEditor")) }));
			}

			string fullTypeReference = NameSpace + "." + CollectionTypeName;
			string collectionTypeReference = fullTypeReference + "Collection";
			if (generateComments)
			{
				co.Comments.Add(new CodeCommentStatement("<summary>", true));
				co.Comments.Add(new CodeCommentStatement("    <para>", true));
				co.Comments.Add(new CodeCommentStatement("      A collection that stores <see cref='" + fullTypeReference + "'/> objects.", true));
				co.Comments.Add(new CodeCommentStatement("   </para>", true));
				co.Comments.Add(new CodeCommentStatement("</summary>", true));
				co.Comments.Add(new CodeCommentStatement("<seealso cref='" + fullTypeReference +"Collection'/>", true));
			}

			/*===============================================*/
			/* Create the default constructor*/
			/*===============================================*/
			CodeConstructor cc = new CodeConstructor();
			cc.Attributes =  MemberAttributes.Public | MemberAttributes.Final;
			co.Members.Add(cc);

			if (generateComments)
			{
				cc.Comments.Add(new CodeCommentStatement("<summary>", true));
				cc.Comments.Add(new CodeCommentStatement("    <para>", true));
				cc.Comments.Add(new CodeCommentStatement("      Initializes a new instance of <see cref='" + collectionTypeReference + "'/>.", true));
				cc.Comments.Add(new CodeCommentStatement("   </para>", true));
				cc.Comments.Add(new CodeCommentStatement("</summary>", true));
			}


			/*===============================================*/
			/* Create the copy constructor*/
			/*===============================================*/
			cc = new CodeConstructor();
			cc.Attributes =  MemberAttributes.Public | MemberAttributes.Final;
			cc.Parameters.Add (new CodeParameterDeclarationExpression
				(workCollectionTypeName + "Collection", "value"));
			cc.Statements.Add (new CodeExpressionStatement(new CodeMethodInvokeExpression (new CodeThisReferenceExpression(), "AddRange", new CodeArgumentReferenceExpression("value"))));
			co.Members.Add(cc);

			if (generateComments)
			{
				cc.Comments.Add(new CodeCommentStatement("<summary>", true));
				cc.Comments.Add(new CodeCommentStatement("    <para>", true));
				cc.Comments.Add(new CodeCommentStatement("      Initializes a new instance of <see cref='" + collectionTypeReference + "'/> based on another <see cref='" + collectionTypeReference + "'/>.", true));
				cc.Comments.Add(new CodeCommentStatement("   </para>", true));
				cc.Comments.Add(new CodeCommentStatement("</summary>", true));
				cc.Comments.Add(new CodeCommentStatement("<param name='value'>", true));
				cc.Comments.Add(new CodeCommentStatement("      A <see cref='" + collectionTypeReference + "'/> from which the contents are copied", true));
				cc.Comments.Add(new CodeCommentStatement("</param>", true));
			}

			/*===============================================*/
			/* Create the array constructor*/
			/*===============================================*/
			cc = new CodeConstructor();
			cc.Parameters.Add (new CodeParameterDeclarationExpression
				(new CodeTypeReference(workCollectionTypeName, 1), "value"));
			cc.Attributes =  MemberAttributes.Public | MemberAttributes.Final;
			cc.Statements.Add (new CodeExpressionStatement(new CodeMethodInvokeExpression (new CodeThisReferenceExpression(), "AddRange", new CodeArgumentReferenceExpression("value"))));

			co.Members.Add(cc);

			if (generateComments)
			{
				cc.Comments.Add(new CodeCommentStatement("<summary>", true));
				cc.Comments.Add(new CodeCommentStatement("    <para>", true));
				cc.Comments.Add(new CodeCommentStatement("      Initializes a new instance of <see cref='" + collectionTypeReference + "'/> containing any array of <see cref='" + fullTypeReference + "'/> objects.", true));
				cc.Comments.Add(new CodeCommentStatement("   </para>", true));
				cc.Comments.Add(new CodeCommentStatement("</summary>", true));
				cc.Comments.Add(new CodeCommentStatement("<param name='value'>", true));
				cc.Comments.Add(new CodeCommentStatement("      A array of <see cref='" + fullTypeReference + "'/> objects with which to intialize the collection", true));
				cc.Comments.Add(new CodeCommentStatement("</param>", true));
			}


			/*===============================================*/
			/* Create and add the Dispose method             */
			/*===============================================*/
			if (addDispose)
			{
				cm = new CodeMemberMethod ();
				cm.Name = "Dispose";
				cm.ReturnType = new CodeTypeReference(typeof(void));
				cm.Attributes =  MemberAttributes.Public | MemberAttributes.Final;
				CodeIterationStatement disposeLoop =  new CodeIterationStatement(
					// Type t
					// dont know how to use this...new CodeVariableDeclarationStatement(workCollectionTypeName, "t"),
					// init expression
					new CodeVariableDeclarationStatement("System.Int32", "i", new CodePrimitiveExpression(0)),
					// test
					new CodeBinaryOperatorExpression(
						new CodeFieldReferenceExpression(null, "i"),
						CodeBinaryOperatorType.LessThan,
						new CodePropertyReferenceExpression(new CodePropertyReferenceExpression(null, "List"), "Count")
					),
					// increment
					new CodeAssignStatement(
						new CodeFieldReferenceExpression(null, "i"),
						new CodeBinaryOperatorExpression(new CodeFieldReferenceExpression(null, "i"),
						CodeBinaryOperatorType.Add,
						new CodePrimitiveExpression(1))
					),
					// statements: ((Type)List[i]).Dispose()
					new CodeStatement[] {	new CodeExpressionStatement
											(
												new CodeMethodInvokeExpression
												(
													new CodeCastExpression
													(
														workCollectionTypeName,
														new CodeIndexerExpression
														(
															new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "List"),
															new CodeArgumentReferenceExpression("i")
														)
													),
													"Dispose"
												)
											)
										}
					);
				cm.Statements.Add(disposeLoop);

				if (generateComments)
				{
					cm.Comments.Add(new CodeCommentStatement("<summary>", true));
					cm.Comments.Add(new CodeCommentStatement("Calls Dispose() on all items.", true));
					cm.Comments.Add(new CodeCommentStatement("</summary>", true));
				}

				co.Members.Add(cm);
			}

			/*===============================================*/
			/* Create and add the "this" property			   */
			/*===============================================*/
			if (lang != "JSCRIPT")
			{
				CodeMemberProperty  cp = new CodeMemberProperty ();
				cp.Name = "Item";
				cp.Attributes = MemberAttributes.Public | MemberAttributes.Final ;;
				cp.Type = new CodeTypeReference(workCollectionTypeName);
				cp.Parameters.Add (new CodeParameterDeclarationExpression ("System.Int32", "index"));
				cp.GetStatements.Add (new CodeMethodReturnStatement (new CodeCastExpression (workCollectionTypeName, new CodeIndexerExpression (new CodeTypeReferenceExpression ("List"), new CodeFieldReferenceExpression  (null, "index")))));
				cp.SetStatements.Add (new CodeAssignStatement (new CodeIndexerExpression (new CodeTypeReferenceExpression ("List"), new CodeFieldReferenceExpression  (null, "index")), new CodeFieldReferenceExpression  (null, "value")));
				co.Members.Add (cp);

				if (generateComments)
				{
					cp.Comments.Add(new CodeCommentStatement("<summary>", true));
					cp.Comments.Add(new CodeCommentStatement("<para>Represents the entry at the specified index of the <see cref='" + fullTypeReference + "'/>.</para>", true));
					cp.Comments.Add(new CodeCommentStatement("</summary>", true));
					cp.Comments.Add(new CodeCommentStatement("<param name='index'><para>The zero-based index of the entry to locate in the collection.</para></param>", true));
					cp.Comments.Add(new CodeCommentStatement("<value>", true));
					cp.Comments.Add(new CodeCommentStatement("   <para> The entry at the specified index of the collection.</para>", true));
					cp.Comments.Add(new CodeCommentStatement("</value>", true));
					cp.Comments.Add(new CodeCommentStatement("<exception cref='System.ArgumentOutOfRangeException'><paramref name='index'/> is outside the valid range of indexes for the collection.</exception>", true));
				}
			}

			/*===============================================*/
			/* ADD -> List.ADD()
			/*===============================================*/
			cm = new CodeMemberMethod ();
			cm.Name = "Add";
			cm.ReturnType = new CodeTypeReference(typeof(int));
			cm.Parameters.Add (new CodeParameterDeclarationExpression
				(workCollectionTypeName, "value"));
			cm.Attributes =  MemberAttributes.Public | MemberAttributes.Final;
			cm.Statements.Add (new CodeMethodReturnStatement (new CodeMethodInvokeExpression (new CodeTypeReferenceExpression ("List"), "Add", new CodeExpression [] {
																																										 new CodeFieldReferenceExpression  (null, "value")})));

			co.Members.Add(cm);

			if (generateComments)
			{
				cm.Comments.Add(new CodeCommentStatement("<summary>", true));
				cm.Comments.Add(new CodeCommentStatement("   <para>Adds a <see cref='" + fullTypeReference + "'/> with the specified value to the ", true));
				cm.Comments.Add(new CodeCommentStatement("   <see cref='" + collectionTypeReference + "'/> .</para>", true));
				cm.Comments.Add(new CodeCommentStatement("</summary>", true));
				cm.Comments.Add(new CodeCommentStatement("<param name='value'>The <see cref='" + fullTypeReference + "'/> to add.</param>", true));
				cm.Comments.Add(new CodeCommentStatement("<returns>", true));
				cm.Comments.Add(new CodeCommentStatement("   <para>The index at which the new element was inserted.</para>", true));
				cm.Comments.Add(new CodeCommentStatement("</returns>", true));
				cm.Comments.Add(new CodeCommentStatement("<seealso cref='" + collectionTypeReference + ".AddRange'/>", true));
			}

			/*===============================================*/
			/* ADDRANGE(<type>[]) -> List.ADD()
			/*===============================================*/
			cm = new CodeMemberMethod ();
			cm.Name = "AddRange";
			cm.ReturnType = new CodeTypeReference(typeof(void));
			cm.Parameters.Add (new CodeParameterDeclarationExpression
				(new CodeTypeReference(workCollectionTypeName, 1), "value"));
			cm.Attributes =  MemberAttributes.Public | MemberAttributes.Final;
			CodeIterationStatement loop = new CodeIterationStatement(
				// init expression
				new CodeVariableDeclarationStatement("System.Int32", "i", new CodePrimitiveExpression(0)),
				// test
				new CodeBinaryOperatorExpression(
				new CodeFieldReferenceExpression(null, "i"),
				CodeBinaryOperatorType.LessThan,
				new CodeFieldReferenceExpression(new CodeFieldReferenceExpression(null, "value"), "Length")
				),
				// increment
				new CodeAssignStatement(
				new CodeFieldReferenceExpression(null, "i"),
				new CodeBinaryOperatorExpression(new CodeFieldReferenceExpression(null, "i"),
				CodeBinaryOperatorType.Add,
				new CodePrimitiveExpression(1))
				),
				// statements
				new CodeStatement[] {
										new CodeExpressionStatement(
										new CodeMethodInvokeExpression(
										new CodeThisReferenceExpression(), "Add",
										new CodeExpression[] {
																 new CodeIndexerExpression(new CodeFieldReferenceExpression(null, "value"),
																 new CodeFieldReferenceExpression(null, "i")
																 )
															 }
										)
										)
									}
				);
			cm.Statements.Add (loop);

			co.Members.Add(cm);

			if (generateComments)
			{
				cm.Comments.Add(new CodeCommentStatement("<summary>", true));
				cm.Comments.Add(new CodeCommentStatement("<para>Copies the elements of an array to the end of the <see cref='" + collectionTypeReference + "'/>.</para>", true));
				cm.Comments.Add(new CodeCommentStatement("</summary>", true));
				cm.Comments.Add(new CodeCommentStatement("<param name='value'>", true));
				cm.Comments.Add(new CodeCommentStatement("   An array of type <see cref='" + fullTypeReference + "'/> containing the objects to add to the collection.", true));
				cm.Comments.Add(new CodeCommentStatement("</param>", true));
				cm.Comments.Add(new CodeCommentStatement("<returns>", true));
				cm.Comments.Add(new CodeCommentStatement("  <para>None.</para>", true));
				cm.Comments.Add(new CodeCommentStatement("</returns>", true));
				cm.Comments.Add(new CodeCommentStatement("<seealso cref='" + collectionTypeReference + ".Add'/>", true));
			}

			/*===============================================*/
			/* ADDRANGE(type-collection) -> List.ADD()
			/*===============================================*/
			cm = new CodeMemberMethod ();
			cm.Name = "AddRange";
			cm.ReturnType = new CodeTypeReference(typeof(void));
			cm.Parameters.Add (new CodeParameterDeclarationExpression
				(workCollectionTypeName + "Collection", "value"));
			cm.Attributes =  MemberAttributes.Public | MemberAttributes.Final;
			loop = new CodeIterationStatement(
				// init expression
				new CodeVariableDeclarationStatement("System.Int32", "i", new CodePrimitiveExpression(0)),
				// test
				new CodeBinaryOperatorExpression(
				new CodeFieldReferenceExpression(null, "i"),
				CodeBinaryOperatorType.LessThan,
				new CodePropertyReferenceExpression(new CodeFieldReferenceExpression(null, "value"), "Count")
				),
				// increment
				new CodeAssignStatement(
				new CodeFieldReferenceExpression(null, "i"),
				new CodeBinaryOperatorExpression(new CodeFieldReferenceExpression(null, "i"),
				CodeBinaryOperatorType.Add,
				new CodePrimitiveExpression(1))
				),
				// statements
				new CodeStatement[] {
										new CodeExpressionStatement(
										new CodeMethodInvokeExpression(
										new CodeThisReferenceExpression(), "Add",
										new CodeExpression[] {
																 new CodeIndexerExpression(new CodeFieldReferenceExpression(null, "value"),
																 new CodeFieldReferenceExpression(null, "i"))
															 }
										)
										)
									}
				);
			cm.Statements.Add (loop);

			co.Members.Add(cm);

			if (generateComments)
			{
				cm.Comments.Add(new CodeCommentStatement("<summary>", true));
				cm.Comments.Add(new CodeCommentStatement("    <para>", true));
				cm.Comments.Add(new CodeCommentStatement("      Adds the contents of another <see cref='" + collectionTypeReference + "'/> to the end of the collection.", true));
				cm.Comments.Add(new CodeCommentStatement("   </para>", true));
				cm.Comments.Add(new CodeCommentStatement("</summary>", true));
				cm.Comments.Add(new CodeCommentStatement("<param name='value'>", true));
				cm.Comments.Add(new CodeCommentStatement("   A <see cref='" + fullTypeReference + "Collection'/> containing the objects to add to the collection.", true));
				cm.Comments.Add(new CodeCommentStatement("</param>", true));
				cm.Comments.Add(new CodeCommentStatement("<returns>", true));
				cm.Comments.Add(new CodeCommentStatement("  <para>None.</para>", true));
				cm.Comments.Add(new CodeCommentStatement("</returns>", true));
				cm.Comments.Add(new CodeCommentStatement("<seealso cref='" + collectionTypeReference + ".Add'/>", true));

			}


			/*====================================================*/
			/* Contains -> List.Contains							*/
			/*====================================================*/
			cm = new CodeMemberMethod ();
			cm.Name = "Contains";
			cm.ReturnType = new CodeTypeReference(typeof(bool));
			cm.Parameters.Add (new CodeParameterDeclarationExpression (workCollectionTypeName, "value"));
			cm.Attributes = MemberAttributes.Public | MemberAttributes.Final ;
			cm.Statements.Add (new CodeMethodReturnStatement (new CodeMethodInvokeExpression (new CodeTypeReferenceExpression ("List"), "Contains", new CodeExpression [] {
																																											  new CodeFieldReferenceExpression  (null, "value")})));
			co.Members.Add (cm);

			if (generateComments)
			{
				cm.Comments.Add(new CodeCommentStatement("<summary>", true));
				cm.Comments.Add(new CodeCommentStatement("<para>Gets a value indicating whether the ", true));
				cm.Comments.Add(new CodeCommentStatement("   <see cref='" + collectionTypeReference + "'/> contains the specified <see cref='" + fullTypeReference + "'/>.</para>", true));
				cm.Comments.Add(new CodeCommentStatement("</summary>", true));
				cm.Comments.Add(new CodeCommentStatement("<param name='value'>The <see cref='" + fullTypeReference + "'/> to locate.</param>", true));
				cm.Comments.Add(new CodeCommentStatement("<returns>", true));
				cm.Comments.Add(new CodeCommentStatement("<para><see langword='true'/> if the <see cref='" + fullTypeReference + "'/> is contained in the collection; ", true));
				cm.Comments.Add(new CodeCommentStatement("  otherwise, <see langword='false'/>.</para>", true));
				cm.Comments.Add(new CodeCommentStatement("</returns>", true));
				cm.Comments.Add(new CodeCommentStatement("<seealso cref='" + collectionTypeReference + ".IndexOf'/>", true));
			}

			/*====================================================*/
			/* CopyTo -> List.CopyTo()							*/
			/*====================================================*/
			cm = new CodeMemberMethod ();
			cm.Name = "CopyTo";
			cm.ReturnType = new CodeTypeReference(typeof(void));
			cm.Parameters.Add (new CodeParameterDeclarationExpression (new CodeTypeReference(workCollectionTypeName, 1), "array"));
			cm.Parameters.Add (new CodeParameterDeclarationExpression ("System.Int32", "index"));
			cm.Attributes = MemberAttributes.Public | MemberAttributes.Final ;
			cm.Statements.Add (new CodeExpressionStatement(new CodeMethodInvokeExpression (new CodeTypeReferenceExpression ("List"), "CopyTo", new CodeExpression [] {
																																										 new CodeFieldReferenceExpression  (null, "array"), new CodeFieldReferenceExpression  (null, "index")})));
			co.Members.Add (cm);

			if (generateComments)
			{
				cm.Comments.Add(new CodeCommentStatement("<summary>", true));
				cm.Comments.Add(new CodeCommentStatement("<para>Copies the <see cref='" + collectionTypeReference + "'/> values to a one-dimensional <see cref='System.Array'/> instance at the ", true));
				cm.Comments.Add(new CodeCommentStatement("   specified index.</para>", true));
				cm.Comments.Add(new CodeCommentStatement("</summary>", true));
				cm.Comments.Add(new CodeCommentStatement("<param name='array'><para>The one-dimensional <see cref='System.Array'/> that is the destination of the values copied from <see cref='" + collectionTypeReference + "'/> .</para></param>", true));
				cm.Comments.Add(new CodeCommentStatement("<param name='index'>The index in <paramref name='array'/> where copying begins.</param>", true));
				cm.Comments.Add(new CodeCommentStatement("<returns>", true));
				cm.Comments.Add(new CodeCommentStatement("  <para>None.</para>", true));
				cm.Comments.Add(new CodeCommentStatement("</returns>", true));
				cm.Comments.Add(new CodeCommentStatement("<exception cref='System.ArgumentException'><para><paramref name='array'/> is multidimensional.</para> <para>-or-</para> <para>The number of elements in the <see cref='" + collectionTypeReference + "'/> is greater than the available space between <paramref name='arrayIndex'/> and the end of <paramref name='array'/>.</para></exception>", true));
				cm.Comments.Add(new CodeCommentStatement("<exception cref='System.ArgumentNullException'><paramref name='array'/> is <see langword='null'/>. </exception>", true));
				cm.Comments.Add(new CodeCommentStatement("<exception cref='System.ArgumentOutOfRangeException'><paramref name='arrayIndex'/> is less than <paramref name='array'/>'s lowbound. </exception>", true));
				cm.Comments.Add(new CodeCommentStatement("<seealso cref='System.Array'/>", true));
			}

			/*===================================================*/
			/* IndexOf -> List.IndexOf()						   */
			/*===================================================*/
			cm = new CodeMemberMethod ();
			cm.Name = "IndexOf";
			cm.ReturnType = new CodeTypeReference(typeof(int));
			cm.Parameters.Add (new CodeParameterDeclarationExpression (workCollectionTypeName, "value"));
			cm.Attributes = MemberAttributes.Public | MemberAttributes.Final ;
			cm.Statements.Add (new CodeMethodReturnStatement (new CodeMethodInvokeExpression (new CodeTypeReferenceExpression ("List"), "IndexOf", new CodeExpression [] {
																																											 new CodeFieldReferenceExpression  (null, "value")})));
			co.Members.Add (cm);

			if (generateComments)
			{
				cm.Comments.Add(new CodeCommentStatement("<summary>", true));
				cm.Comments.Add(new CodeCommentStatement("   <para>Returns the index of a <see cref='" + fullTypeReference + "'/> in ", true));
				cm.Comments.Add(new CodeCommentStatement("      the <see cref='" + collectionTypeReference + "'/> .</para>", true));
				cm.Comments.Add(new CodeCommentStatement("</summary>", true));
				cm.Comments.Add(new CodeCommentStatement("<param name='value'>The <see cref='" + fullTypeReference + "'/> to locate.</param>", true));
				cm.Comments.Add(new CodeCommentStatement("<returns>", true));
				cm.Comments.Add(new CodeCommentStatement("<para>The index of the <see cref='" + fullTypeReference + "'/> of <paramref name='value'/> in the ", true));
				cm.Comments.Add(new CodeCommentStatement("<see cref='" + collectionTypeReference + "'/>, if found; otherwise, -1.</para>", true));
				cm.Comments.Add(new CodeCommentStatement("</returns>", true));
				cm.Comments.Add(new CodeCommentStatement("<seealso cref='" + collectionTypeReference + ".Contains'/>", true));
			}

			/*================================================*/
			/* Insert -> List.Insert()							*/
			/*================================================*/
			cm = new CodeMemberMethod ();
			cm.Name = "Insert";
			cm.ReturnType = new CodeTypeReference(typeof(void));
			cm.Parameters.Add (new CodeParameterDeclarationExpression ("System.Int32", "index"));
			cm.Parameters.Add (new CodeParameterDeclarationExpression (workCollectionTypeName, "value"));
			cm.Attributes = MemberAttributes.Public | MemberAttributes.Final ;
			cm.Statements.Add (new CodeExpressionStatement(new CodeMethodInvokeExpression (new CodeTypeReferenceExpression ("List"), "Insert", new CodeExpression [] {
																																										 new CodeFieldReferenceExpression  (null, "index"), new CodeFieldReferenceExpression  (null, "value")})));
			co.Members.Add (cm);

			if (generateComments)
			{
				cm.Comments.Add(new CodeCommentStatement("<summary>", true));
				cm.Comments.Add(new CodeCommentStatement("<para>Inserts a <see cref='" + fullTypeReference + "'/> into the <see cref='" + collectionTypeReference + "'/> at the specified index.</para>", true));
				cm.Comments.Add(new CodeCommentStatement("</summary>", true));
				cm.Comments.Add(new CodeCommentStatement("<param name='index'>The zero-based index where <paramref name='value'/> should be inserted.</param>", true));
				cm.Comments.Add(new CodeCommentStatement("<param name=' value'>The <see cref='" + fullTypeReference + "'/> to insert.</param>", true));
				cm.Comments.Add(new CodeCommentStatement("<returns><para>None.</para></returns>", true));
				cm.Comments.Add(new CodeCommentStatement("<seealso cref='" + collectionTypeReference + ".Add'/>", true));
			}


			if (generateEnum)
			{
				/*=====================================================*/
				/* GetEnumerator										 */
				/*=====================================================*/
				cm = new CodeMemberMethod ();
				cm.Name = "GetEnumerator";
				cm.ReturnType = new CodeTypeReference(collectionTypeName + "Enumerator");
				cm.Attributes = MemberAttributes.Public | MemberAttributes.Final | MemberAttributes.New;
				//		return new CustomerEnumerator(this);

				cm.Statements.Add ( new CodeMethodReturnStatement ( new CodeObjectCreateExpression(collectionTypeName + "Enumerator",
					new CodeExpression[] { new CodeThisReferenceExpression() })));

				co.Members.Add (cm);

				if (generateComments)
				{
					cm.Comments.Add(new CodeCommentStatement("<summary>", true));
					cm.Comments.Add(new CodeCommentStatement("   <para>Returns an enumerator that can iterate through ", true));
					cm.Comments.Add(new CodeCommentStatement("      the <see cref='" + collectionTypeReference + "'/> .</para>", true));
					cm.Comments.Add(new CodeCommentStatement("</summary>", true));
					cm.Comments.Add(new CodeCommentStatement("<returns><para>None.</para></returns>", true));
					cm.Comments.Add(new CodeCommentStatement("<seealso cref='System.Collections.IEnumerator'/>", true));
				}


			}

			/*====================================================*/
			/* Remove -> List.Remove()							*/
			/*====================================================*/
			cm = new CodeMemberMethod ();
			cm.Name = "Remove";
			cm.ReturnType = new CodeTypeReference(typeof(void));
			cm.Parameters.Add (new CodeParameterDeclarationExpression (workCollectionTypeName, "value"));
			cm.Attributes = MemberAttributes.Public | MemberAttributes.Final ;
			cm.Statements.Add (new CodeExpressionStatement(new CodeMethodInvokeExpression (new CodeTypeReferenceExpression ("List"), "Remove", new CodeExpression [] {
																																										 new CodeFieldReferenceExpression  (null, "value")})));
			co.Members.Add (cm);

			if (generateComments)
			{
				cm.Comments.Add(new CodeCommentStatement("<summary>", true));
				cm.Comments.Add(new CodeCommentStatement("   <para> Removes a specific <see cref='" + fullTypeReference + "'/> from the ", true));
				cm.Comments.Add(new CodeCommentStatement("   <see cref='" + collectionTypeReference + "'/> .</para>", true));
				cm.Comments.Add(new CodeCommentStatement("</summary>", true));
				cm.Comments.Add(new CodeCommentStatement("<param name='value'>The <see cref='" + fullTypeReference + "'/> to remove from the <see cref='" + collectionTypeReference + "'/> .</param>", true));
				cm.Comments.Add(new CodeCommentStatement("<returns><para>None.</para></returns>", true));
				cm.Comments.Add(new CodeCommentStatement("<exception cref='System.ArgumentException'><paramref name='value'/> is not found in the Collection. </exception>", true));
			}

			/*==================================================================*/
			/* End Typed Collection Methods									  */
			/*==================================================================*/



			/*=================================================================================*/
			/* Start Semi-Events													 		     */
			/*=================================================================================*/
			if (AddValidation)
			{
				// Generate the protected override void OnRemove
				cm = new CodeMemberMethod ();
				cm.Name = "OnSet";
				cm.ReturnType = new CodeTypeReference(typeof(void));
				cm.Parameters.Add (new CodeParameterDeclarationExpression ("System.Int32", "index"));
				cm.Parameters.Add (new CodeParameterDeclarationExpression ("System.Object", "oldValue"));
				cm.Parameters.Add (new CodeParameterDeclarationExpression ("System.Object", "newValue"));
				cm.Attributes =  MemberAttributes.Family | MemberAttributes.Override | MemberAttributes.VTableMask ;
				cm.Statements.Add(new CodeCommentStatement("TODO: Add code here to handle an existing value within"));
				cm.Statements.Add(new CodeCommentStatement("      the collection be replaced with a new value"));
				co.Members.Add(cm);
				//-----------------------------------------------


				// Generate the protected override void OnRemove
				cm = new CodeMemberMethod ();
				cm.Name = "OnInsert";
				cm.ReturnType = new CodeTypeReference(typeof(void));
				cm.Parameters.Add (new CodeParameterDeclarationExpression ("System.Int32", "index"));
				cm.Parameters.Add (new CodeParameterDeclarationExpression ("System.Object", "value"));
				cm.Attributes =  MemberAttributes.Family | MemberAttributes.Override | MemberAttributes.VTableMask ;
				co.Members.Add(cm);
				//-----------------------------------------------

				// Generate the protected override void OnClear
				cm = new CodeMemberMethod ();
				cm.Name = "OnClear";
				cm.ReturnType = new CodeTypeReference(typeof(void));
				cm.Attributes =  MemberAttributes.Family | MemberAttributes.Override | MemberAttributes.VTableMask ;
				co.Members.Add(cm);
				//-----------------------------------------------

				// Generate the protected override void OnRemove
				cm = new CodeMemberMethod ();
				cm.Name = "OnRemove";
				cm.ReturnType = new CodeTypeReference(typeof(void));
				cm.Parameters.Add (new CodeParameterDeclarationExpression ("System.Int32", "index"));
				cm.Parameters.Add (new CodeParameterDeclarationExpression ("System.Object", "value"));
				cm.Attributes =  MemberAttributes.Family | MemberAttributes.Override | MemberAttributes.VTableMask ;
				co.Members.Add(cm);
				//-----------------------------------------------

				// Generate the protected override void OnValidate
				cm = new CodeMemberMethod ();
				cm.Name = "OnValidate";
				cm.ReturnType = new CodeTypeReference(typeof(void));
				cm.Parameters.Add (new CodeParameterDeclarationExpression ("System.Object", "value"));
				cm.Attributes =  MemberAttributes.Family | MemberAttributes.Override | MemberAttributes.VTableMask ;
				co.Members.Add(cm);
				//-----------------------------------------------
			}
			/*======================================================================*/
			/* End semi-events													  */
			/*======================================================================*/

			if (generateEnum)
			{

				/*======================================================================*/
				/* Custom Enumerator													  */
				/*======================================================================*/
				CodeTypeDeclaration eco = new CodeTypeDeclaration (collectionTypeName + "Enumerator");

				if (generateEnumAsNested)
				{
					co.Members.Add(eco);
				}
				else
				{
					cnamespace.Types.Add(eco);
				}

				//commented out since the codeDOM doesn't support explicit interface
				//implementation currently for C#

				eco.BaseTypes.Add(new CodeTypeReference(typeof(object)));
				eco.BaseTypes.Add(new CodeTypeReference("IEnumerator"));
				eco.TypeAttributes = TypeAttributes.Public;


				// Generate the method level variables

				CodeMemberField cf = new CodeMemberField();
				cf.Name = "baseEnumerator";
				cf.Attributes = MemberAttributes.Private | MemberAttributes.Final;
				cf.Type = new CodeTypeReference("IEnumerator");
				eco.Members.Add(cf);

				cf = new CodeMemberField();
				cf.Name = "temp";
				cf.Attributes = MemberAttributes.Private | MemberAttributes.Final;
				cf.Type = new CodeTypeReference("IEnumerable");
				eco.Members.Add(cf);


				//-----------------------------------------------------
				// generate the internal construcot
				cc = new CodeConstructor();
				// cc.Attributes = MemberAttributes.Assembly;
				cc.Attributes = MemberAttributes.Public;
				cc.Name = collectionTypeName + "Enumerator";
				cc.Parameters.Add(new  CodeParameterDeclarationExpression (collectionTypeName + "Collection", "mappings"));
				cc.Statements.Add(new CodeAssignStatement(
					new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), "temp"),
					new CodeCastExpression ("IEnumerable", new CodePropertyReferenceExpression(null, "mappings"))));

				cc.Statements.Add(new CodeAssignStatement(
					new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), "baseEnumerator"),
					new CodeMethodInvokeExpression (new CodePropertyReferenceExpression(null, "temp"), "GetEnumerator")));

				eco.Members.Add(cc);



				/*===============================================*/
				/* MoveNext
				/*===============================================*/
				cm = new CodeMemberMethod();
				cm.Name = "MoveNext";
				cm.ReturnType = new CodeTypeReference(typeof(bool));
				cm.Attributes = MemberAttributes.Public | MemberAttributes.Final ;

				cm.Statements.Add(new CodeMethodReturnStatement(new CodeMethodInvokeExpression (new CodePropertyReferenceExpression(null, "baseEnumerator"), "MoveNext")));

				eco.Members.Add(cm);


				/*===============================================*/
				/* IEnumerable.MoveNext
				/*===============================================*/
				cm = new CodeMemberMethod();
				cm.Name = "MoveNext";
				cm.PrivateImplementationType = new CodeTypeReference("IEnumerator");
				cm.ReturnType = new CodeTypeReference(typeof(bool));
				cm.Attributes = MemberAttributes.Public | MemberAttributes.Final ;

				cm.Statements.Add(new CodeMethodReturnStatement(new CodeMethodInvokeExpression (new CodePropertyReferenceExpression(null, "baseEnumerator"), "MoveNext")));

				eco.Members.Add(cm);

				/*===============================================*/
				/* Create and add the
				/*===============================================*/

				//commented out since the codeDOM doesn't support explicit
				//interface implementation for C# at this time.

				/*===============================================*/
				/* Current
				/*===============================================*/
				CodeMemberProperty cp1 = new CodeMemberProperty();
				cp1 = new CodeMemberProperty();
				cp1.Name = "Current";
				cp1.Type = new CodeTypeReference(collectionTypeName);
				cp1.Attributes = MemberAttributes.Final | MemberAttributes.Public;
				cp1.GetStatements.Add (new CodeMethodReturnStatement (new CodeCastExpression (workCollectionTypeName, new CodePropertyReferenceExpression (new CodePropertyReferenceExpression(null, "baseEnumerator"), "Current"))));

				eco.Members.Add(cp1);

				/*===============================================*/
				/* IEnumerator.Current
				/*===============================================*/
				cp1 = new CodeMemberProperty();
				cp1 = new CodeMemberProperty();
				cp1.Name = "Current";
				cp1.PrivateImplementationType = new CodeTypeReference("IEnumerator");
				cp1.Type = new CodeTypeReference(typeof(object));
				cp1.Attributes = MemberAttributes.Final | MemberAttributes.Public;
				cp1.GetStatements.Add (new CodeMethodReturnStatement (new CodePropertyReferenceExpression (new CodePropertyReferenceExpression(null, "baseEnumerator"), "Current")));

				eco.Members.Add(cp1);


				/*===============================================*/
				/* Reset
				/*===============================================*/
				cm = new CodeMemberMethod();
				cm.Name = "Reset";
				cm.ReturnType = new CodeTypeReference(typeof(void));
				cm.Attributes = MemberAttributes.Public | MemberAttributes.Final ;

				CodeBinaryOperatorExpression s1 = new CodeBinaryOperatorExpression(new CodePropertyReferenceExpression( null, "index"), CodeBinaryOperatorType.Assign , new CodePropertyReferenceExpression(null, "-1"));

				//cm.Statements.Add(new CodeAssignStatement(new CodeFieldReferenceExpression(null, "index"), new CodeFieldReferenceExpression(null, "-1") ));  //replace s1
				cm.Statements.Add(new CodeExpressionStatement(new CodeMethodInvokeExpression(new CodePropertyReferenceExpression(null, "baseEnumerator"), "Reset")));
				eco.Members.Add(cm);

				/*===============================================*/
				/* IEnumerator.Reset
				/*===============================================*/
				cm = new CodeMemberMethod();
				cm.Name = "Reset";
				cm.PrivateImplementationType = new CodeTypeReference("IEnumerator");
				cm.ReturnType = new CodeTypeReference(typeof(void));
				cm.Attributes = MemberAttributes.Public | MemberAttributes.Final ;

				s1 = new CodeBinaryOperatorExpression(new CodePropertyReferenceExpression( null, "index"), CodeBinaryOperatorType.Assign , new CodePropertyReferenceExpression(null, "-1"));

				//cm.Statements.Add(new CodeAssignStatement(new CodeFieldReferenceExpression(null, "index"), new CodeFieldReferenceExpression(null, "-1") ));  //replace s1
				cm.Statements.Add(new CodeExpressionStatement(new CodeMethodInvokeExpression(new CodePropertyReferenceExpression(null, "baseEnumerator"), "Reset")));
				eco.Members.Add(cm);
			}

			/*======================================*/
			/* Spit the source into the file		  */
			/*======================================*/
			CodeGeneratorOptions cgo = new CodeGeneratorOptions();
			cgo.BracingStyle = "C";
			cgo.IndentString = "\t";
			cg.GenerateCodeFromNamespace (cnamespace, t, cgo);


		}

		public void Generate()
		{
			TextWriter t = null;
			string workFile = null;


			// Validate the selected language
			if (lang.ToUpper() == "VB" && (Extension == "" || Extension == null))
			{
				Extension = "vb";
			}
			else if (lang.ToUpper() == "CS" && (Extension == "" || Extension == null))
			{
				Extension = "cs";
			}
			else if (lang.ToUpper() == "JSCRIPT" && (Extension == "" || Extension == null))
			{
				Extension = "js";
			}
			else if (Extension == "" || Extension == null)
			{
				throw new ArgumentException("Invalid languge: Select VB, CS, JSCRIPT", "Language");
			}

			// Build the file name
			workFile = System.IO.Path.Combine(path, fileName + "." + _extension);

			// Open the file on disk
			t = new StreamWriter (new FileStream (workFile, FileMode.Create));

			try
			{
				Generate(t);
			}
			finally
			{
				t.Close();
			}
		}
	}
}