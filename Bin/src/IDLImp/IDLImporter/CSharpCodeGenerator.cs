/// --------------------------------------------------------------------------------------------
#region /// Copyright (c) 2007, SIL International. All Rights Reserved.
/// <copyright from='2007' to='2007' company='SIL International'>
///		Copyright (c) 2007, SIL International. All Rights Reserved.
///
///		Distributable under the terms of either the Common Public License or the
///		GNU Lesser General Public License, as specified in the LICENSING.txt file.
/// </copyright>
#endregion
///
/// File: CSharpCodeGenerator.cs
/// Responsibility: TE Team
/// Last reviewed:
///
/// <remarks>
/// Basically Microsoft's CSharp code generator as found in Microsoft.CSharp.CSharpCodeGenerator
/// enhanced with some missing pieces and without the compiler support.
/// </remarks>
/// --------------------------------------------------------------------------------------------
using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;

namespace SIL.FieldWorks.Tools
{

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Basically Microsoft's CSharp code generator as found in Microsoft.CSharp.CSharpCodeGenerator
	/// enhanced with some missing pieces and without the compiler support. It would have been
	/// nice to override Microsoft's implementation, but unfortunately that's internal to
	/// System.dll and most methods are private.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class CSharpCodeGenerator : ICodeGenerator
	{
		#region Member variables
		private static Dictionary<string, int> systemTypes;
		private CodeTypeDeclaration currentClass;
		private CodeTypeMember currentMember;
		private bool generatingForLoop;
		private bool inNestedBinary;
		private static readonly List<string> keywords;
		private const GeneratorSupport LanguageSupport = (GeneratorSupport.DeclareIndexerProperties | GeneratorSupport.GenericTypeDeclaration | GeneratorSupport.GenericTypeReference | GeneratorSupport.PartialTypes | GeneratorSupport.Resources | GeneratorSupport.Win32Resources | GeneratorSupport.ComplexExpressions | GeneratorSupport.PublicStaticMembers | GeneratorSupport.MultipleInterfaceMembers | GeneratorSupport.NestedTypes | GeneratorSupport.ChainedConstructorArguments | GeneratorSupport.ReferenceParameters | GeneratorSupport.ParameterAttributes | GeneratorSupport.AssemblyAttributes | GeneratorSupport.DeclareEvents | GeneratorSupport.DeclareInterfaces | GeneratorSupport.DeclareDelegates | GeneratorSupport.DeclareEnums | GeneratorSupport.DeclareValueTypes | GeneratorSupport.ReturnTypeAttributes | GeneratorSupport.TryCatchStatements | GeneratorSupport.StaticConstructors | GeneratorSupport.MultidimensionalArrays | GeneratorSupport.GotoStatements | GeneratorSupport.EntryPointMethod | GeneratorSupport.ArraysOfArrays);
		private const int MaxLineLength = 80;
		private CodeGeneratorOptions options;
		private IndentedTextWriter output;
		private const int ParameterMultilineThreshold = 15;
		#endregion

		static CSharpCodeGenerator()
		{
			keywords = new List<string>();
			keywords.AddRange(new string[] { "as", "do", "if", "in", "is" });
			keywords.AddRange(new string[] { "for", "int", "new", "out", "ref", "try" });
			keywords.AddRange(new string[] { "base", "bool", "byte", "case", "char", "else", "enum", "goto", "lock", "long", "null", "this", "true", "uint", "void" });
			keywords.AddRange(new string[] { "break", "catch", "class", "const", "event", "false", "fixed", "float", "sbyte", "short", "throw", "ulong", "using", "where", "while", "yield" });
			keywords.AddRange(new string[] { "double", "extern", "object", "params", "public", "return", "sealed", "sizeof", "static", "string", "struct", "switch", "typeof", "unsafe", "ushort" });
			keywords.AddRange(new string[] { "checked", "decimal", "default", "finally", "foreach", "partial", "private", "virtual" });
			keywords.AddRange(new string[] { "abstract", "continue", "delegate", "explicit", "implicit", "internal", "operator", "override", "readonly", "volatile" });
			keywords.AddRange(new string[] { "__arglist", "__makeref", "__reftype", "interface", "namespace", "protected", "unchecked" });
			keywords.AddRange(new string[] { "__refvalue", "stackalloc" });
		}

		private void AppendEscapedChar(StringBuilder b, char value)
		{
			if (b == null)
			{
				this.Output.Write(@"\u");
				int num1 = value;
				this.Output.Write(num1.ToString("X4", CultureInfo.InvariantCulture));
			}
			else
			{
				b.Append(@"\u");
				int num1 = value;
				b.Append(num1.ToString("X4", CultureInfo.InvariantCulture));
			}
		}

		private void ContinueOnNewLine(string st)
		{
			this.Output.WriteLine(st);
		}

		public string CreateEscapedIdentifier(string name)
		{
			if (!CSharpCodeGenerator.IsKeyword(name) && !CSharpCodeGenerator.IsPrefixTwoUnderscore(name))
			{
				return name;
			}
			return ("@" + name);
		}

		public string CreateValidIdentifier(string name)
		{
			if (CSharpCodeGenerator.IsPrefixTwoUnderscore(name))
			{
				name = "_" + name;
			}
			while (CSharpCodeGenerator.IsKeyword(name))
			{
				name = "_" + name;
			}
			return name;
		}

		private void GenerateArgumentReferenceExpression(CodeArgumentReferenceExpression e)
		{
			this.OutputIdentifier(e.ParameterName);
		}

		private void GenerateArrayCreateExpression(CodeArrayCreateExpression e)
		{
			this.Output.Write("new ");
			CodeExpressionCollection collection1 = e.Initializers;
			if (collection1.Count > 0)
			{
				this.OutputType(e.CreateType);
				if (e.CreateType.ArrayRank == 0)
				{
					this.Output.Write("[]");
				}
				this.Output.WriteLine(" {");
				this.Indent++;
				this.OutputExpressionList(collection1, true);
				this.Indent--;
				this.Output.Write("}");
			}
			else
			{
				this.Output.Write(this.GetBaseTypeOutput(e.CreateType));
				this.Output.Write("[");
				if (e.SizeExpression != null)
				{
					this.GenerateExpression(e.SizeExpression);
				}
				else
				{
					this.Output.Write(e.Size);
				}
				this.Output.Write("]");
			}
		}

		private void GenerateArrayIndexerExpression(CodeArrayIndexerExpression e)
		{
			this.GenerateExpression(e.TargetObject);
			this.Output.Write("[");
			bool flag1 = true;
			foreach (CodeExpression expression1 in e.Indices)
			{
				if (flag1)
				{
					flag1 = false;
				}
				else
				{
					this.Output.Write(", ");
				}
				this.GenerateExpression(expression1);
			}
			this.Output.Write("]");
		}

		private void GenerateAssignStatement(CodeAssignStatement e)
		{
			this.GenerateExpression(e.Left);
			this.Output.Write(" = ");
			this.GenerateExpression(e.Right);
			if (!this.generatingForLoop)
			{
				this.Output.WriteLine(";");
			}
		}

		private void GenerateAttachEventStatement(CodeAttachEventStatement e)
		{
			this.GenerateEventReferenceExpression(e.Event);
			this.Output.Write(" += ");
			this.GenerateExpression(e.Listener);
			this.Output.WriteLine(";");
		}

		private void GenerateAttributeDeclarationsEnd(CodeAttributeDeclarationCollection attributes)
		{
			this.Output.Write("]");
		}

		private void GenerateAttributeDeclarationsStart(CodeAttributeDeclarationCollection attributes)
		{
			this.Output.Write("[");
		}

		private void GenerateAttributes(CodeAttributeDeclarationCollection attributes)
		{
			this.GenerateAttributes(attributes, null, false);
		}

		private void GenerateAttributes(CodeAttributeDeclarationCollection attributes, string prefix)
		{
			this.GenerateAttributes(attributes, prefix, false);
		}

		private void GenerateAttributes(CodeAttributeDeclarationCollection attributes, string prefix, bool inLine)
		{
			if (attributes.Count != 0)
			{
				IEnumerator enumerator1 = attributes.GetEnumerator();
				bool flag1 = false;
				while (enumerator1.MoveNext())
				{
					CodeAttributeDeclaration declaration1 = (CodeAttributeDeclaration) enumerator1.Current;
					if (declaration1.Name.Equals("system.paramarrayattribute", StringComparison.OrdinalIgnoreCase))
					{
						flag1 = true;
					}
					else
					{
						this.GenerateAttributeDeclarationsStart(attributes);
						if (prefix != null)
						{
							this.Output.Write(prefix);
						}
						if (declaration1.AttributeType != null)
						{
							this.Output.Write(this.GetTypeOutput(declaration1.AttributeType));
						}
						this.Output.Write("(");
						bool flag2 = true;
						foreach (CodeAttributeArgument argument1 in declaration1.Arguments)
						{
							if (flag2)
							{
								flag2 = false;
							}
							else
							{
								this.Output.Write(", ");
							}
							this.OutputAttributeArgument(argument1);
						}
						this.Output.Write(")");
						this.GenerateAttributeDeclarationsEnd(attributes);
						if (inLine)
						{
							this.Output.Write(" ");
							continue;
						}
						this.Output.WriteLine();
					}
				}
				if (flag1)
				{
					if (prefix != null)
					{
						this.Output.Write(prefix);
					}
					this.Output.Write("params");
					if (inLine)
					{
						this.Output.Write(" ");
					}
					else
					{
						this.Output.WriteLine();
					}
				}
			}
		}

		private void GenerateBaseReferenceExpression(CodeBaseReferenceExpression e)
		{
			this.Output.Write("base");
		}

		private void GenerateBinaryOperatorExpression(CodeBinaryOperatorExpression e)
		{
			bool flag1 = false;
			this.Output.Write("(");
			this.GenerateExpression(e.Left);
			this.Output.Write(" ");
			if ((e.Left is CodeBinaryOperatorExpression) || (e.Right is CodeBinaryOperatorExpression))
			{
				if (!this.inNestedBinary)
				{
					flag1 = true;
					this.inNestedBinary = true;
					this.Indent += 3;
				}
				this.ContinueOnNewLine("");
			}
			this.OutputOperator(e.Operator);
			this.Output.Write(" ");
			this.GenerateExpression(e.Right);
			this.Output.Write(")");
			if (flag1)
			{
				this.Indent -= 3;
				this.inNestedBinary = false;
			}
		}

		private void GenerateCastExpression(CodeCastExpression e)
		{
			this.Output.Write("((");
			this.OutputType(e.TargetType);
			this.Output.Write(")(");
			this.GenerateExpression(e.Expression);
			this.Output.Write("))");
		}

		private void GenerateChecksumPragma(CodeChecksumPragma checksumPragma)
		{
			this.Output.Write("#pragma checksum \"");
			this.Output.Write(checksumPragma.FileName);
			this.Output.Write("\" \"");
			this.Output.Write(checksumPragma.ChecksumAlgorithmId.ToString("B", CultureInfo.InvariantCulture));
			this.Output.Write("\" \"");
			if (checksumPragma.ChecksumData != null)
			{
				foreach (byte num1 in checksumPragma.ChecksumData)
				{
					this.Output.Write(num1.ToString("X2", CultureInfo.InvariantCulture));
				}
			}
			this.Output.WriteLine("\"");
		}

		public void GenerateCodeFromMember(CodeTypeMember member, TextWriter writer, CodeGeneratorOptions options)
		{
			if (this.output != null)
			{
				throw new InvalidOperationException("CodeGenReentrance");
			}
			this.options = (options == null) ? new CodeGeneratorOptions() : options;
			this.output = new IndentedTextWriter(writer, this.options.IndentString);
			try
			{
				CodeTypeDeclaration declaration1 = new CodeTypeDeclaration();
				this.currentClass = declaration1;
				this.GenerateTypeMember(member, declaration1);
			}
			finally
			{
				this.currentClass = null;
				this.output = null;
				this.options = null;
			}
		}

		private void GenerateCodeRegionDirective(CodeRegionDirective regionDirective)
		{
			if (regionDirective.RegionMode == CodeRegionMode.Start)
			{
				this.Output.Write("#region ");
				this.Output.WriteLine(regionDirective.RegionText);
			}
			else if (regionDirective.RegionMode == CodeRegionMode.End)
			{
				this.Output.WriteLine("#endregion");
			}
		}

		private void GenerateComment(CodeComment e)
		{
			string text1 = e.DocComment ? "///" : "//";
			this.Output.Write(text1);
			this.Output.Write(" ");
			string text2 = e.Text;
			for (int num1 = 0; num1 < text2.Length; num1++)
			{
				if (text2[num1] != '\0')
				{
					this.Output.Write(text2[num1]);
					if (text2[num1] == '\r')
					{
						if ((num1 < (text2.Length - 1)) && (text2[num1 + 1] == '\n'))
						{
							this.Output.Write('\n');
							num1++;
						}
						OutputTabs((IndentedTextWriter)this.Output);
						this.Output.Write(text1);
					}
					else if (text2[num1] == '\n')
					{
						OutputTabs((IndentedTextWriter)this.Output);
						this.Output.Write(text1);
					}
					else if (((text2[num1] == '\u2028') || (text2[num1] == '\u2029')) || (text2[num1] == '\x0085'))
					{
						this.Output.Write(text1);
					}
				}
			}
			this.Output.WriteLine();
		}

		private void OutputTabs(IndentedTextWriter writer)
		{
			for (int i = 0; i < writer.Indent; i++)
				writer.Write(IndentedTextWriter.DefaultTabString);
		}

		private string IndentationString(int indent)
		{
			string tab = IndentedTextWriter.DefaultTabString;
			StringBuilder bldr = new StringBuilder(indent * tab.Length);
			for (int i = 0; i < indent; i++)
				bldr.Append(tab);
			return bldr.ToString();
		}

		private void GenerateCommentStatement(CodeCommentStatement e)
		{
			this.GenerateComment(e.Comment);
		}

		private void GenerateCommentStatements(CodeCommentStatementCollection e)
		{
			foreach (CodeCommentStatement statement1 in e)
			{
				this.GenerateCommentStatement(statement1);
			}
		}

		private void GenerateCompileUnit(CodeCompileUnit e)
		{
			this.GenerateCompileUnitStart(e);
			this.GenerateNamespaces(e);
			this.GenerateCompileUnitEnd(e);
		}

		private void GenerateCompileUnitEnd(CodeCompileUnit e)
		{
			if (e.EndDirectives.Count > 0)
			{
				this.GenerateDirectives(e.EndDirectives);
			}
		}

		private void GenerateCompileUnitStart(CodeCompileUnit e)
		{
			if (e.StartDirectives.Count > 0)
			{
				this.GenerateDirectives(e.StartDirectives);
			}
			this.Output.WriteLine("//------------------------------------------------------------------------------");
			this.Output.Write("// <");
			this.Output.WriteLine("AutoGen_Comment_Line1");
			this.Output.Write("//     ");
			this.Output.WriteLine("AutoGen_Comment_Line2");
			this.Output.Write("//     ");
			this.Output.Write("AutoGen_Comment_Line3");
			this.Output.WriteLine(Environment.Version.ToString());
			this.Output.WriteLine("//");
			this.Output.Write("//     ");
			this.Output.WriteLine("AutoGen_Comment_Line4");
			this.Output.Write("//     ");
			this.Output.WriteLine("AutoGen_Comment_Line5");
			this.Output.Write("// </");
			this.Output.WriteLine("AutoGen_Comment_Line1");
			this.Output.WriteLine("//------------------------------------------------------------------------------");
			this.Output.WriteLine("");
			SortedList list1 = new SortedList(StringComparer.Ordinal);
			foreach (CodeNamespace namespace1 in e.Namespaces)
			{
				if (string.IsNullOrEmpty(namespace1.Name))
				{
					namespace1.UserData["GenerateImports"] = false;
					foreach (CodeNamespaceImport import1 in namespace1.Imports)
					{
						if (!list1.Contains(import1.Namespace))
						{
							list1.Add(import1.Namespace, import1.Namespace);
						}
					}
				}
			}
			foreach (string text1 in list1.Keys)
			{
				this.Output.Write("using ");
				this.OutputIdentifier(text1);
				this.Output.WriteLine(";");
			}
			if (list1.Keys.Count > 0)
			{
				this.Output.WriteLine("");
			}
			if (e.AssemblyCustomAttributes.Count > 0)
			{
				this.GenerateAttributes(e.AssemblyCustomAttributes, "assembly: ");
				this.Output.WriteLine("");
			}
		}

		private void GenerateConditionStatement(CodeConditionStatement e)
		{
			this.Output.Write("if (");
			this.GenerateExpression(e.Condition);
			this.Output.Write(")");
			this.OutputStartingBrace();
			this.Indent++;
			this.GenerateStatements(e.TrueStatements);
			this.Indent--;
			if (e.FalseStatements.Count > 0)
			{
				this.Output.Write("}");
				if (this.Options.ElseOnClosing)
				{
					this.Output.Write(" ");
				}
				else
				{
					this.Output.WriteLine("");
				}
				this.Output.Write("else");
				this.OutputStartingBrace();
				this.Indent++;
				this.GenerateStatements(e.FalseStatements);
				this.Indent--;
			}
			this.Output.WriteLine("}");
		}

		private void GenerateConstructor(CodeConstructor e, CodeTypeDeclaration c)
		{
			if (this.IsCurrentClass || this.IsCurrentStruct)
			{
				if (e.CustomAttributes.Count > 0)
				{
					this.GenerateAttributes(e.CustomAttributes);
				}
				this.OutputMemberAccessModifier(e.Attributes);
				this.OutputIdentifier(this.CurrentTypeName);
				this.Output.Write("(");
				this.OutputParameters(e.Parameters);
				this.Output.Write(")");
				CodeExpressionCollection collection1 = e.BaseConstructorArgs;
				CodeExpressionCollection collection2 = e.ChainedConstructorArgs;
				if (collection1.Count > 0)
				{
					this.Output.WriteLine(" : ");
					this.Indent++;
					this.Indent++;
					this.Output.Write("base(");
					this.OutputExpressionList(collection1);
					this.Output.Write(")");
					this.Indent--;
					this.Indent--;
				}
				if (collection2.Count > 0)
				{
					this.Output.WriteLine(" : ");
					this.Indent++;
					this.Indent++;
					this.Output.Write("this(");
					this.OutputExpressionList(collection2);
					this.Output.Write(")");
					this.Indent--;
					this.Indent--;
				}
				this.OutputStartingBrace();
				this.Indent++;
				this.GenerateStatements(e.Statements);
				this.Indent--;
				this.Output.WriteLine("}");
			}
		}

		private void GenerateConstructors(CodeTypeDeclaration e)
		{
			IEnumerator enumerator1 = e.Members.GetEnumerator();
			while (enumerator1.MoveNext())
			{
				if (enumerator1.Current is CodeConstructor)
				{
					this.currentMember = (CodeTypeMember) enumerator1.Current;
					if (this.options.BlankLinesBetweenMembers)
					{
						this.Output.WriteLine();
					}
					if (this.currentMember.StartDirectives.Count > 0)
					{
						this.GenerateDirectives(this.currentMember.StartDirectives);
					}
					this.GenerateCommentStatements(this.currentMember.Comments);
					CodeConstructor constructor1 = (CodeConstructor) enumerator1.Current;
					if (constructor1.LinePragma != null)
					{
						this.GenerateLinePragmaStart(constructor1.LinePragma);
					}
					this.GenerateConstructor(constructor1, e);
					if (constructor1.LinePragma != null)
					{
						this.GenerateLinePragmaEnd(constructor1.LinePragma);
					}
					if (this.currentMember.EndDirectives.Count > 0)
					{
						this.GenerateDirectives(this.currentMember.EndDirectives);
					}
				}
			}
		}

		private void GenerateDecimalValue(decimal d)
		{
			this.Output.Write(d.ToString(CultureInfo.InvariantCulture));
			this.Output.Write('m');
		}

		private void GenerateDefaultValueExpression(CodeDefaultValueExpression e)
		{
			this.Output.Write("default(");
			this.OutputType(e.Type);
			this.Output.Write(")");
		}

		private void GenerateDelegateCreateExpression(CodeDelegateCreateExpression e)
		{
			this.Output.Write("new ");
			this.OutputType(e.DelegateType);
			this.Output.Write("(");
			this.GenerateExpression(e.TargetObject);
			this.Output.Write(".");
			this.OutputIdentifier(e.MethodName);
			this.Output.Write(")");
		}

		private void GenerateDelegateInvokeExpression(CodeDelegateInvokeExpression e)
		{
			if (e.TargetObject != null)
			{
				this.GenerateExpression(e.TargetObject);
			}
			this.Output.Write("(");
			this.OutputExpressionList(e.Parameters);
			this.Output.Write(")");
		}

		private void GenerateDirectionExpression(CodeDirectionExpression e)
		{
			this.OutputDirection(e.Direction);
			this.GenerateExpression(e.Expression);
		}

		private void GenerateDirectives(CodeDirectiveCollection directives)
		{
			for (int num1 = 0; num1 < directives.Count; num1++)
			{
				CodeDirective directive1 = directives[num1];
				if (directive1 is CodeChecksumPragma)
				{
					this.GenerateChecksumPragma((CodeChecksumPragma) directive1);
				}
				else if (directive1 is CodeRegionDirective)
				{
					this.GenerateCodeRegionDirective((CodeRegionDirective) directive1);
				}
			}
		}

		private void GenerateDoubleValue(double d)
		{
			if (double.IsNaN(d))
			{
				this.Output.Write("double.NaN");
			}
			else if (double.IsNegativeInfinity(d))
			{
				this.Output.Write("double.NegativeInfinity");
			}
			else if (double.IsPositiveInfinity(d))
			{
				this.Output.Write("double.PositiveInfinity");
			}
			else
			{
				this.Output.Write(d.ToString("R", CultureInfo.InvariantCulture));
			}
		}

		private void GenerateEntryPointMethod(CodeEntryPointMethod e, CodeTypeDeclaration c)
		{
			if (e.CustomAttributes.Count > 0)
			{
				this.GenerateAttributes(e.CustomAttributes);
			}
			this.Output.Write("public static ");
			this.OutputType(e.ReturnType);
			this.Output.Write(" Main()");
			this.OutputStartingBrace();
			this.Indent++;
			this.GenerateStatements(e.Statements);
			this.Indent--;
			this.Output.WriteLine("}");
		}

		private void GenerateEvent(CodeMemberEvent e, CodeTypeDeclaration c)
		{
			if (!this.IsCurrentDelegate && !this.IsCurrentEnum)
			{
				if (e.CustomAttributes.Count > 0)
				{
					this.GenerateAttributes(e.CustomAttributes);
				}
				if (e.PrivateImplementationType == null)
				{
					this.OutputMemberAccessModifier(e.Attributes);
				}
				this.Output.Write("event ");
				string text1 = e.Name;
				if (e.PrivateImplementationType != null)
				{
					text1 = e.PrivateImplementationType.BaseType + "." + text1;
				}
				this.OutputTypeNamePair(e.Type, text1);
				this.Output.WriteLine(";");
			}
		}

		private void GenerateEventReferenceExpression(CodeEventReferenceExpression e)
		{
			if (e.TargetObject != null)
			{
				this.GenerateExpression(e.TargetObject);
				this.Output.Write(".");
			}
			this.OutputIdentifier(e.EventName);
		}

		private void GenerateEvents(CodeTypeDeclaration e)
		{
			IEnumerator enumerator1 = e.Members.GetEnumerator();
			while (enumerator1.MoveNext())
			{
				if (enumerator1.Current is CodeMemberEvent)
				{
					this.currentMember = (CodeTypeMember) enumerator1.Current;
					if (this.options.BlankLinesBetweenMembers)
					{
						this.Output.WriteLine();
					}
					if (this.currentMember.StartDirectives.Count > 0)
					{
						this.GenerateDirectives(this.currentMember.StartDirectives);
					}
					this.GenerateCommentStatements(this.currentMember.Comments);
					CodeMemberEvent event1 = (CodeMemberEvent) enumerator1.Current;
					if (event1.LinePragma != null)
					{
						this.GenerateLinePragmaStart(event1.LinePragma);
					}
					this.GenerateEvent(event1, e);
					if (event1.LinePragma != null)
					{
						this.GenerateLinePragmaEnd(event1.LinePragma);
					}
					if (this.currentMember.EndDirectives.Count > 0)
					{
						this.GenerateDirectives(this.currentMember.EndDirectives);
					}
				}
			}
		}

		private void GenerateExpression(CodeExpression e)
		{
			if (e is CodeArrayCreateExpression)
			{
				this.GenerateArrayCreateExpression((CodeArrayCreateExpression) e);
			}
			else if (e is CodeBaseReferenceExpression)
			{
				this.GenerateBaseReferenceExpression((CodeBaseReferenceExpression) e);
			}
			else if (e is CodeBinaryOperatorExpression)
			{
				this.GenerateBinaryOperatorExpression((CodeBinaryOperatorExpression) e);
			}
			else if (e is CodeCastExpression)
			{
				this.GenerateCastExpression((CodeCastExpression) e);
			}
			else if (e is CodeDelegateCreateExpression)
			{
				this.GenerateDelegateCreateExpression((CodeDelegateCreateExpression) e);
			}
			else if (e is CodeFieldReferenceExpression)
			{
				this.GenerateFieldReferenceExpression((CodeFieldReferenceExpression) e);
			}
			else if (e is CodeArgumentReferenceExpression)
			{
				this.GenerateArgumentReferenceExpression((CodeArgumentReferenceExpression) e);
			}
			else if (e is CodeVariableReferenceExpression)
			{
				this.GenerateVariableReferenceExpression((CodeVariableReferenceExpression) e);
			}
			else if (e is CodeIndexerExpression)
			{
				this.GenerateIndexerExpression((CodeIndexerExpression) e);
			}
			else if (e is CodeArrayIndexerExpression)
			{
				this.GenerateArrayIndexerExpression((CodeArrayIndexerExpression) e);
			}
			else if (e is CodeSnippetExpression)
			{
				this.GenerateSnippetExpression((CodeSnippetExpression) e);
			}
			else if (e is CodeMethodInvokeExpression)
			{
				this.GenerateMethodInvokeExpression((CodeMethodInvokeExpression) e);
			}
			else if (e is CodeMethodReferenceExpression)
			{
				this.GenerateMethodReferenceExpression((CodeMethodReferenceExpression) e);
			}
			else if (e is CodeEventReferenceExpression)
			{
				this.GenerateEventReferenceExpression((CodeEventReferenceExpression) e);
			}
			else if (e is CodeDelegateInvokeExpression)
			{
				this.GenerateDelegateInvokeExpression((CodeDelegateInvokeExpression) e);
			}
			else if (e is CodeObjectCreateExpression)
			{
				this.GenerateObjectCreateExpression((CodeObjectCreateExpression) e);
			}
			else if (e is CodeParameterDeclarationExpression)
			{
				this.GenerateParameterDeclarationExpression((CodeParameterDeclarationExpression) e);
			}
			else if (e is CodeDirectionExpression)
			{
				this.GenerateDirectionExpression((CodeDirectionExpression) e);
			}
			else if (e is CodePrimitiveExpression)
			{
				this.GeneratePrimitiveExpression((CodePrimitiveExpression) e);
			}
			else if (e is CodePropertyReferenceExpression)
			{
				this.GeneratePropertyReferenceExpression((CodePropertyReferenceExpression) e);
			}
			else if (e is CodePropertySetValueReferenceExpression)
			{
				this.GeneratePropertySetValueReferenceExpression((CodePropertySetValueReferenceExpression) e);
			}
			else if (e is CodeThisReferenceExpression)
			{
				this.GenerateThisReferenceExpression((CodeThisReferenceExpression) e);
			}
			else if (e is CodeTypeReferenceExpression)
			{
				this.GenerateTypeReferenceExpression((CodeTypeReferenceExpression) e);
			}
			else if (e is CodeTypeOfExpression)
			{
				this.GenerateTypeOfExpression((CodeTypeOfExpression) e);
			}
			else if (e is CodeDefaultValueExpression)
			{
				this.GenerateDefaultValueExpression((CodeDefaultValueExpression) e);
			}
			else
			{
				if (e == null)
				{
					throw new ArgumentNullException("e");
				}
				throw new ArgumentException("InvalidElementType: " + e, e.GetType().FullName);
			}
		}

		private void GenerateExpressionStatement(CodeExpressionStatement e)
		{
			this.GenerateExpression(e.Expression);
			if (!this.generatingForLoop)
			{
				this.Output.WriteLine(";");
			}
		}

		private void GenerateField(CodeMemberField e)
		{
			if (!this.IsCurrentDelegate && !this.IsCurrentInterface)
			{
				if (this.IsCurrentEnum)
				{
					if (e.CustomAttributes.Count > 0)
					{
						this.GenerateAttributes(e.CustomAttributes);
					}
					this.OutputIdentifier(e.Name);
					if (e.InitExpression != null)
					{
						this.Output.Write(" = ");
						this.GenerateExpression(e.InitExpression);
					}
					this.Output.WriteLine(",");
				}
				else
				{
					if (e.CustomAttributes.Count > 0)
					{
						this.GenerateAttributes(e.CustomAttributes);
					}
					this.OutputMemberAccessModifier(e.Attributes);
					this.OutputVTableModifier(e.Attributes);
					this.OutputFieldScopeModifier(e.Attributes);
					this.OutputTypeNamePair(e.Type, e.Name);
					if (e.InitExpression != null)
					{
						this.Output.Write(" = ");
						this.GenerateExpression(e.InitExpression);
					}
					this.Output.WriteLine(";");
				}
			}
		}

		private void GenerateFieldReferenceExpression(CodeFieldReferenceExpression e)
		{
			if (e.TargetObject != null)
			{
				this.GenerateExpression(e.TargetObject);
				this.Output.Write(".");
			}
			this.OutputIdentifier(e.FieldName);
		}

		private void GenerateFields(CodeTypeDeclaration e)
		{
			IEnumerator enumerator1 = e.Members.GetEnumerator();
			while (enumerator1.MoveNext())
			{
				if (enumerator1.Current is CodeMemberField)
				{
					this.currentMember = (CodeTypeMember) enumerator1.Current;
					if (this.options.BlankLinesBetweenMembers)
					{
						this.Output.WriteLine();
					}
					if (this.currentMember.StartDirectives.Count > 0)
					{
						this.GenerateDirectives(this.currentMember.StartDirectives);
					}
					this.GenerateCommentStatements(this.currentMember.Comments);
					CodeMemberField field1 = (CodeMemberField) enumerator1.Current;
					if (field1.LinePragma != null)
					{
						this.GenerateLinePragmaStart(field1.LinePragma);
					}
					this.GenerateField(field1);
					if (field1.LinePragma != null)
					{
						this.GenerateLinePragmaEnd(field1.LinePragma);
					}
					if (this.currentMember.EndDirectives.Count > 0)
					{
						this.GenerateDirectives(this.currentMember.EndDirectives);
					}
				}
			}
		}

		private void GenerateGotoStatement(CodeGotoStatement e)
		{
			this.Output.Write("goto ");
			this.Output.Write(e.Label);
			this.Output.WriteLine(";");
		}

		private void GenerateIndexerExpression(CodeIndexerExpression e)
		{
			this.GenerateExpression(e.TargetObject);
			this.Output.Write("[");
			bool flag1 = true;
			foreach (CodeExpression expression1 in e.Indices)
			{
				if (flag1)
				{
					flag1 = false;
				}
				else
				{
					this.Output.Write(", ");
				}
				this.GenerateExpression(expression1);
			}
			this.Output.Write("]");
		}

		private void GenerateIterationStatement(CodeIterationStatement e)
		{
			this.generatingForLoop = true;
			this.Output.Write("for (");
			this.GenerateStatement(e.InitStatement);
			this.Output.Write("; ");
			this.GenerateExpression(e.TestExpression);
			this.Output.Write("; ");
			this.GenerateStatement(e.IncrementStatement);
			this.Output.Write(")");
			this.OutputStartingBrace();
			this.generatingForLoop = false;
			this.Indent++;
			this.GenerateStatements(e.Statements);
			this.Indent--;
			this.Output.WriteLine("}");
		}

		private void GenerateLabeledStatement(CodeLabeledStatement e)
		{
			this.Indent--;
			this.Output.Write(e.Label);
			this.Output.WriteLine(":");
			this.Indent++;
			if (e.Statement != null)
			{
				this.GenerateStatement(e.Statement);
			}
		}

		private void GenerateLinePragmaEnd(CodeLinePragma e)
		{
			this.Output.WriteLine();
			this.Output.WriteLine("#line default");
			this.Output.WriteLine("#line hidden");
		}

		private void GenerateLinePragmaStart(CodeLinePragma e)
		{
			this.Output.WriteLine("");
			this.Output.Write("#line ");
			this.Output.Write(e.LineNumber);
			this.Output.Write(" \"");
			this.Output.Write(e.FileName);
			this.Output.Write("\"");
			this.Output.WriteLine("");
		}

		private void GenerateMethod(CodeMemberMethod e, CodeTypeDeclaration c)
		{
			if (this.IsCurrentClass || this.IsCurrentStruct || this.IsCurrentInterface)
			{
				if (e.CustomAttributes.Count > 0)
				{
					this.GenerateAttributes(e.CustomAttributes);
				}
				if (e.ReturnTypeCustomAttributes.Count > 0)
				{
					this.GenerateAttributes(e.ReturnTypeCustomAttributes, "return: ");
				}
				if (!this.IsCurrentInterface)
				{
					if (e.PrivateImplementationType == null)
					{
						this.OutputMemberAccessModifier(e.Attributes);
						this.OutputVTableModifier(e.Attributes);
						this.OutputMemberScopeModifier(e.Attributes);

						if (IsExtern(e.UserData))
							this.Output.Write("extern ");
					}
				}
				else
				{
					this.OutputVTableModifier(e.Attributes);
				}
				this.OutputType(e.ReturnType);
				this.Output.Write(" ");
				if (e.PrivateImplementationType != null)
				{
					this.Output.Write(e.PrivateImplementationType.BaseType);
					this.Output.Write(".");
				}
				this.OutputIdentifier(e.Name);
				this.OutputTypeParameters(e.TypeParameters);
				this.Output.Write("(");
				this.OutputParameters(e.Parameters);
				this.Output.Write(")");
				this.OutputTypeParameterConstraints(e.TypeParameters);
				if (!this.IsCurrentInterface && ((e.Attributes & MemberAttributes.ScopeMask) != MemberAttributes.Abstract) && !IsExtern(e.UserData))
				{
					this.OutputStartingBrace();
					this.Indent++;
					this.GenerateStatements(e.Statements);
					this.Indent--;
					this.Output.WriteLine("}");
				}
				else
				{
					this.Output.WriteLine(";");
				}
			}
		}

		private void GenerateMethodInvokeExpression(CodeMethodInvokeExpression e)
		{
			this.GenerateMethodReferenceExpression(e.Method);
			this.Output.Write("(");
			this.OutputExpressionList(e.Parameters);
			this.Output.Write(")");
		}

		private void GenerateMethodReferenceExpression(CodeMethodReferenceExpression e)
		{
			if (e.TargetObject != null)
			{
				if (e.TargetObject is CodeBinaryOperatorExpression)
				{
					this.Output.Write("(");
					this.GenerateExpression(e.TargetObject);
					this.Output.Write(")");
				}
				else
				{
					this.GenerateExpression(e.TargetObject);
				}
				this.Output.Write(".");
			}
			this.OutputIdentifier(e.MethodName);
			if (e.TypeArguments.Count > 0)
			{
				this.Output.Write(this.GetTypeArgumentsOutput(e.TypeArguments));
			}
		}

		private void GenerateMethodReturnStatement(CodeMethodReturnStatement e)
		{
			this.Output.Write("return");
			if (e.Expression != null)
			{
				this.Output.Write(" ");
				this.GenerateExpression(e.Expression);
			}
			this.Output.WriteLine(";");
		}

		private void GenerateMethods(CodeTypeDeclaration e)
		{
			IEnumerator enumerator1 = e.Members.GetEnumerator();
			while (enumerator1.MoveNext())
			{
				if (((enumerator1.Current is CodeMemberMethod) && !(enumerator1.Current is CodeTypeConstructor)) && !(enumerator1.Current is CodeConstructor))
				{
					this.currentMember = (CodeTypeMember) enumerator1.Current;
					if (this.options.BlankLinesBetweenMembers)
					{
						this.Output.WriteLine();
					}
					if (this.currentMember.StartDirectives.Count > 0)
					{
						this.GenerateDirectives(this.currentMember.StartDirectives);
					}
					this.GenerateCommentStatements(this.currentMember.Comments);
					CodeMemberMethod method1 = (CodeMemberMethod) enumerator1.Current;
					if (method1.LinePragma != null)
					{
						this.GenerateLinePragmaStart(method1.LinePragma);
					}
					if (enumerator1.Current is CodeEntryPointMethod)
					{
						this.GenerateEntryPointMethod((CodeEntryPointMethod) enumerator1.Current, e);
					}
					else
					{
						this.GenerateMethod(method1, e);
					}
					if (method1.LinePragma != null)
					{
						this.GenerateLinePragmaEnd(method1.LinePragma);
					}
					if (this.currentMember.EndDirectives.Count > 0)
					{
						this.GenerateDirectives(this.currentMember.EndDirectives);
					}
				}
			}
		}

		private void GenerateNamespace(CodeNamespace e)
		{
			this.GenerateCommentStatements(e.Comments);
			this.GenerateNamespaceStart(e);
			if (this.GetUserData(e, "GenerateImports", true))
			{
				this.GenerateNamespaceImports(e);
			}
			this.Output.WriteLine("");
			this.GenerateTypes(e);
			this.GenerateNamespaceEnd(e);
		}

		private void GenerateNamespaceEnd(CodeNamespace e)
		{
			if ((e.Name != null) && (e.Name.Length > 0))
			{
				this.Indent--;
				this.Output.WriteLine("}");
			}
		}

		private void GenerateNamespaceImport(CodeNamespaceImport e)
		{
			this.Output.Write("using ");
			this.OutputIdentifier(e.Namespace);
			this.Output.WriteLine(";");
		}

		private void GenerateNamespaceImports(CodeNamespace e)
		{
			foreach (CodeNamespaceImport import1 in e.Imports)
			{
				if (import1.LinePragma != null)
				{
					this.GenerateLinePragmaStart(import1.LinePragma);
				}
				this.GenerateNamespaceImport(import1);
				if (import1.LinePragma != null)
				{
					this.GenerateLinePragmaEnd(import1.LinePragma);
				}
			}
		}

		private void GenerateNamespaces(CodeCompileUnit e)
		{
			foreach (CodeNamespace namespace1 in e.Namespaces)
			{
				((ICodeGenerator)this).GenerateCodeFromNamespace(namespace1, this.output.InnerWriter, this.options);
			}
		}

		private void GenerateNamespaceStart(CodeNamespace e)
		{
			if ((e.Name != null) && (e.Name.Length > 0))
			{
				this.Output.Write("namespace ");
				string[] textArray1 = e.Name.Split(new char[] { '.' });
				this.OutputIdentifier(textArray1[0]);
				for (int num1 = 1; num1 < textArray1.Length; num1++)
				{
					this.Output.Write(".");
					this.OutputIdentifier(textArray1[num1]);
				}
				this.OutputStartingBrace();
				this.Indent++;
			}
		}

		private void GenerateNestedTypes(CodeTypeDeclaration e)
		{
			IEnumerator enumerator1 = e.Members.GetEnumerator();
			while (enumerator1.MoveNext())
			{
				if (enumerator1.Current is CodeTypeDeclaration)
				{
					if (this.options.BlankLinesBetweenMembers)
					{
						this.Output.WriteLine();
					}
					CodeTypeDeclaration declaration1 = (CodeTypeDeclaration) enumerator1.Current;
					((ICodeGenerator) this).GenerateCodeFromType(declaration1, this.output.InnerWriter, this.options);
				}
			}
		}

		private void GenerateObjectCreateExpression(CodeObjectCreateExpression e)
		{
			this.Output.Write("new ");
			this.OutputType(e.CreateType);
			this.Output.Write("(");
			this.OutputExpressionList(e.Parameters);
			this.Output.Write(")");
		}

		private void GenerateParameterDeclarationExpression(CodeParameterDeclarationExpression e)
		{
			if (e.CustomAttributes.Count > 0)
			{
				this.GenerateAttributes(e.CustomAttributes, null, true);
			}
			this.OutputDirection(e.Direction);
			this.OutputTypeNamePair(e.Type, e.Name);
		}

		private void GeneratePrimitiveChar(char c)
		{
			this.Output.Write('\'');
			switch (c)
			{
				case '\x0084':
				case '\x0085':
				case '\u2028':
				case '\u2029':
					this.AppendEscapedChar(null, c);
					break;

				case '\'':
					this.Output.Write(@"\'");
					break;

				case '\\':
					this.Output.Write(@"\\");
					break;

				case '\t':
					this.Output.Write(@"\t");
					break;

				case '\n':
					this.Output.Write(@"\n");
					break;

				case '\r':
					this.Output.Write(@"\r");
					break;

				case '"':
					this.Output.Write("\\\"");
					break;

				case '\0':
					this.Output.Write(@"\0");
					break;

				default:
					if (char.IsSurrogate(c))
					{
						this.AppendEscapedChar(null, c);
					}
					else
					{
						this.Output.Write(c);
					}
					break;
			}
			this.Output.Write('\'');
		}

		private void GeneratePrimitiveExpression(CodePrimitiveExpression e)
		{
			if (e.Value is char)
			{
				this.GeneratePrimitiveChar((char) e.Value);
			}
			else if (e.Value is sbyte)
			{
				sbyte num1 = (sbyte) e.Value;
				this.Output.Write(num1.ToString(CultureInfo.InvariantCulture));
			}
			else if (e.Value is ushort)
			{
				ushort num2 = (ushort) e.Value;
				this.Output.Write(num2.ToString(CultureInfo.InvariantCulture));
			}
			else if (e.Value is uint)
			{
				uint num3 = (uint) e.Value;
				this.Output.Write(num3.ToString(CultureInfo.InvariantCulture));
				this.Output.Write("u");
			}
			else if (e.Value is ulong)
			{
				ulong num4 = (ulong) e.Value;
				this.Output.Write(num4.ToString(CultureInfo.InvariantCulture));
				this.Output.Write("ul");
			}
			else
			{
				this.GeneratePrimitiveExpressionBase(e);
			}
		}

		private void GeneratePrimitiveExpressionBase(CodePrimitiveExpression e)
		{
			if (e.Value == null)
			{
				this.Output.Write(this.NullToken);
			}
			else if (e.Value is string)
			{
				this.Output.Write(this.QuoteSnippetString((string) e.Value));
			}
			else if (e.Value is char)
			{
				this.Output.Write("'" + e.Value.ToString() + "'");
			}
			else if (e.Value is byte)
			{
				byte num1 = (byte) e.Value;
				this.Output.Write(num1.ToString(CultureInfo.InvariantCulture));
			}
			else if (e.Value is short)
			{
				short num2 = (short) e.Value;
				this.Output.Write(num2.ToString(CultureInfo.InvariantCulture));
			}
			else if (e.Value is int)
			{
				int num3 = (int) e.Value;
				this.Output.Write(num3.ToString(CultureInfo.InvariantCulture));
			}
			else if (e.Value is long)
			{
				long num4 = (long) e.Value;
				this.Output.Write(num4.ToString(CultureInfo.InvariantCulture));
			}
			else if (e.Value is float)
			{
				this.GenerateSingleFloatValue((float) e.Value);
			}
			else if (e.Value is double)
			{
				this.GenerateDoubleValue((double) e.Value);
			}
			else if (e.Value is decimal)
			{
				this.GenerateDecimalValue((decimal) e.Value);
			}
			else
			{
				if (!(e.Value is bool))
				{
					throw new ArgumentException("InvalidPrimitiveType", e.Value.GetType().ToString() );
				}
				if ((bool) e.Value)
				{
					this.Output.Write("true");
				}
				else
				{
					this.Output.Write("false");
				}
			}
		}

		private void GenerateProperties(CodeTypeDeclaration e)
		{
			IEnumerator enumerator1 = e.Members.GetEnumerator();
			while (enumerator1.MoveNext())
			{
				if (enumerator1.Current is CodeMemberProperty)
				{
					this.currentMember = (CodeTypeMember) enumerator1.Current;
					if (this.options.BlankLinesBetweenMembers)
					{
						this.Output.WriteLine();
					}
					if (this.currentMember.StartDirectives.Count > 0)
					{
						this.GenerateDirectives(this.currentMember.StartDirectives);
					}
					this.GenerateCommentStatements(this.currentMember.Comments);
					CodeMemberProperty property1 = (CodeMemberProperty) enumerator1.Current;
					if (property1.LinePragma != null)
					{
						this.GenerateLinePragmaStart(property1.LinePragma);
					}
					this.GenerateProperty(property1, e);
					if (property1.LinePragma != null)
					{
						this.GenerateLinePragmaEnd(property1.LinePragma);
					}
					if (this.currentMember.EndDirectives.Count > 0)
					{
						this.GenerateDirectives(this.currentMember.EndDirectives);
					}
				}
			}
		}

		private void GenerateProperty(CodeMemberProperty e, CodeTypeDeclaration c)
		{
			if ((this.IsCurrentClass || this.IsCurrentStruct) || this.IsCurrentInterface)
			{
				if (e.CustomAttributes.Count > 0)
				{
					this.GenerateAttributes(e.CustomAttributes);
				}
				if (!this.IsCurrentInterface)
				{
					if (e.PrivateImplementationType == null)
					{
						this.OutputMemberAccessModifier(e.Attributes);
						this.OutputVTableModifier(e.Attributes);
						this.OutputMemberScopeModifier(e.Attributes);

						if (IsExtern(e.UserData))
							Output.Write("extern ");
					}
				}
				else
				{
					this.OutputVTableModifier(e.Attributes);
				}
				this.OutputType(e.Type);
				this.Output.Write(" ");
				if ((e.PrivateImplementationType != null) && !this.IsCurrentInterface)
				{
					this.Output.Write(e.PrivateImplementationType.BaseType);
					this.Output.Write(".");
				}
				if ((e.Parameters.Count > 0) && (string.Compare(e.Name, "Item", StringComparison.OrdinalIgnoreCase) == 0))
				{
					this.Output.Write("this[");
					this.OutputParameters(e.Parameters);
					this.Output.Write("]");
				}
				else
				{
					this.OutputIdentifier(e.Name);
				}
				this.OutputStartingBrace();
				this.Indent++;
				if (e.HasGet)
				{
					if (this.IsCurrentInterface ||
						(e.Attributes & MemberAttributes.ScopeMask) == MemberAttributes.Abstract ||
						IsExtern(e.UserData))
					{
						if (e.UserData.Contains("get_attrs"))
						{
							CodeAttributeDeclarationCollection coll =
								e.UserData["get_attrs"] as CodeAttributeDeclarationCollection;
							if (coll != null && coll.Count > 0)
								GenerateAttributes(coll);
						}

						this.Output.WriteLine("get;");
					}
					else
					{
						this.Output.Write("get");
						this.OutputStartingBrace();
						this.Indent++;
						this.GenerateStatements(e.GetStatements);
						this.Indent--;
						this.Output.WriteLine("}");
					}
				}
				if (e.HasSet)
				{
					if (this.IsCurrentInterface || ((e.Attributes & MemberAttributes.ScopeMask) == MemberAttributes.Abstract) || IsExtern(e.UserData))
					{
						if (e.UserData.Contains("set_attrs"))
						{
							CodeAttributeDeclarationCollection coll =
								e.UserData["set_attrs"] as CodeAttributeDeclarationCollection;
							if (coll != null && coll.Count > 0)
								GenerateAttributes(coll);
						}

						this.Output.WriteLine("set;");
					}
					else
					{
						this.Output.Write("set");
						this.OutputStartingBrace();
						this.Indent++;
						this.GenerateStatements(e.SetStatements);
						this.Indent--;
						this.Output.WriteLine("}");
					}
				}
				this.Indent--;
				this.Output.WriteLine("}");
			}
		}

		private void GeneratePropertyReferenceExpression(CodePropertyReferenceExpression e)
		{
			if (e.TargetObject != null)
			{
				this.GenerateExpression(e.TargetObject);
				this.Output.Write(".");
			}
			this.OutputIdentifier(e.PropertyName);
		}

		private void GeneratePropertySetValueReferenceExpression(CodePropertySetValueReferenceExpression e)
		{
			this.Output.Write("value");
		}

		private void GenerateRemoveEventStatement(CodeRemoveEventStatement e)
		{
			this.GenerateEventReferenceExpression(e.Event);
			this.Output.Write(" -= ");
			this.GenerateExpression(e.Listener);
			this.Output.WriteLine(";");
		}

		private void GenerateSingleFloatValue(float s)
		{
			if (float.IsNaN(s))
			{
				this.Output.Write("float.NaN");
			}
			else if (float.IsNegativeInfinity(s))
			{
				this.Output.Write("float.NegativeInfinity");
			}
			else if (float.IsPositiveInfinity(s))
			{
				this.Output.Write("float.PositiveInfinity");
			}
			else
			{
				this.Output.Write(s.ToString(CultureInfo.InvariantCulture));
				this.Output.Write('F');
			}
		}

		private void GenerateSnippetCompileUnit(CodeSnippetCompileUnit e)
		{
			this.GenerateDirectives(e.StartDirectives);
			if (e.LinePragma != null)
			{
				this.GenerateLinePragmaStart(e.LinePragma);
			}
			this.Output.WriteLine(e.Value);
			if (e.LinePragma != null)
			{
				this.GenerateLinePragmaEnd(e.LinePragma);
			}
			if (e.EndDirectives.Count > 0)
			{
				this.GenerateDirectives(e.EndDirectives);
			}
		}

		private void GenerateSnippetExpression(CodeSnippetExpression e)
		{
			this.Output.Write(e.Value);
		}

		private void GenerateSnippetMember(CodeSnippetTypeMember e)
		{
			this.Output.Write(e.Text);
		}

		private void GenerateSnippetMembers(CodeTypeDeclaration e)
		{
			IEnumerator enumerator1 = e.Members.GetEnumerator();
			bool flag1 = false;
			while (enumerator1.MoveNext())
			{
				if (enumerator1.Current is CodeSnippetTypeMember)
				{
					flag1 = true;
					this.currentMember = (CodeTypeMember) enumerator1.Current;
					if (this.options.BlankLinesBetweenMembers)
					{
						this.Output.WriteLine();
					}
					if (this.currentMember.StartDirectives.Count > 0)
					{
						this.GenerateDirectives(this.currentMember.StartDirectives);
					}
					this.GenerateCommentStatements(this.currentMember.Comments);
					CodeSnippetTypeMember member1 = (CodeSnippetTypeMember) enumerator1.Current;
					if (member1.LinePragma != null)
					{
						this.GenerateLinePragmaStart(member1.LinePragma);
					}
					int num1 = this.Indent;
					this.Indent = 0;
					this.GenerateSnippetMember(member1);
					this.Indent = num1;
					if (member1.LinePragma != null)
					{
						this.GenerateLinePragmaEnd(member1.LinePragma);
					}
					if (this.currentMember.EndDirectives.Count > 0)
					{
						this.GenerateDirectives(this.currentMember.EndDirectives);
					}
				}
			}
			if (flag1)
			{
				this.Output.WriteLine();
			}
		}

		private void GenerateSnippetStatement(CodeSnippetStatement e)
		{
			this.Output.WriteLine(e.Value);
		}

		private void GenerateStatement(CodeStatement e)
		{
			if (e.StartDirectives.Count > 0)
			{
				this.GenerateDirectives(e.StartDirectives);
			}
			if (e.LinePragma != null)
			{
				this.GenerateLinePragmaStart(e.LinePragma);
			}
			if (e is CodeCommentStatement)
			{
				this.GenerateCommentStatement((CodeCommentStatement) e);
			}
			else if (e is CodeMethodReturnStatement)
			{
				this.GenerateMethodReturnStatement((CodeMethodReturnStatement) e);
			}
			else if (e is CodeConditionStatement)
			{
				this.GenerateConditionStatement((CodeConditionStatement) e);
			}
			else if (e is CodeTryCatchFinallyStatement)
			{
				this.GenerateTryCatchFinallyStatement((CodeTryCatchFinallyStatement) e);
			}
			else if (e is CodeAssignStatement)
			{
				this.GenerateAssignStatement((CodeAssignStatement) e);
			}
			else if (e is CodeExpressionStatement)
			{
				this.GenerateExpressionStatement((CodeExpressionStatement) e);
			}
			else if (e is CodeIterationStatement)
			{
				this.GenerateIterationStatement((CodeIterationStatement) e);
			}
			else if (e is CodeThrowExceptionStatement)
			{
				this.GenerateThrowExceptionStatement((CodeThrowExceptionStatement) e);
			}
			else if (e is CodeSnippetStatement)
			{
				int num1 = this.Indent;
				this.Indent = 0;
				this.GenerateSnippetStatement((CodeSnippetStatement) e);
				this.Indent = num1;
			}
			else if (e is CodeVariableDeclarationStatement)
			{
				this.GenerateVariableDeclarationStatement((CodeVariableDeclarationStatement) e);
			}
			else if (e is CodeAttachEventStatement)
			{
				this.GenerateAttachEventStatement((CodeAttachEventStatement) e);
			}
			else if (e is CodeRemoveEventStatement)
			{
				this.GenerateRemoveEventStatement((CodeRemoveEventStatement) e);
			}
			else if (e is CodeGotoStatement)
			{
				this.GenerateGotoStatement((CodeGotoStatement) e);
			}
			else
			{
				if (!(e is CodeLabeledStatement))
				{
					throw new ArgumentException("InvalidElementType" + e, e.GetType().FullName );
				}
				this.GenerateLabeledStatement((CodeLabeledStatement) e);
			}
			if (e.LinePragma != null)
			{
				this.GenerateLinePragmaEnd(e.LinePragma);
			}
			if (e.EndDirectives.Count > 0)
			{
				this.GenerateDirectives(e.EndDirectives);
			}
		}

		private void GenerateStatements(CodeStatementCollection stms)
		{
			IEnumerator enumerator1 = stms.GetEnumerator();
			while (enumerator1.MoveNext())
			{
				((ICodeGenerator)this).GenerateCodeFromStatement((CodeStatement) enumerator1.Current, this.output.InnerWriter, this.options);
			}
		}

		private void GenerateThisReferenceExpression(CodeThisReferenceExpression e)
		{
			this.Output.Write("this");
		}

		private void GenerateThrowExceptionStatement(CodeThrowExceptionStatement e)
		{
			this.Output.Write("throw");
			if (e.ToThrow != null)
			{
				this.Output.Write(" ");
				this.GenerateExpression(e.ToThrow);
			}
			this.Output.WriteLine(";");
		}

		private void GenerateTryCatchFinallyStatement(CodeTryCatchFinallyStatement e)
		{
			this.Output.Write("try");
			this.OutputStartingBrace();
			this.Indent++;
			this.GenerateStatements(e.TryStatements);
			this.Indent--;
			CodeCatchClauseCollection collection1 = e.CatchClauses;
			if (collection1.Count > 0)
			{
				IEnumerator enumerator1 = collection1.GetEnumerator();
				while (enumerator1.MoveNext())
				{
					this.Output.Write("}");
					if (this.Options.ElseOnClosing)
					{
						this.Output.Write(" ");
					}
					else
					{
						this.Output.WriteLine("");
					}
					CodeCatchClause clause1 = (CodeCatchClause) enumerator1.Current;
					this.Output.Write("catch (");
					this.OutputType(clause1.CatchExceptionType);
					this.Output.Write(" ");
					this.OutputIdentifier(clause1.LocalName);
					this.Output.Write(")");
					this.OutputStartingBrace();
					this.Indent++;
					this.GenerateStatements(clause1.Statements);
					this.Indent--;
				}
			}
			CodeStatementCollection collection2 = e.FinallyStatements;
			if (collection2.Count > 0)
			{
				this.Output.Write("}");
				if (this.Options.ElseOnClosing)
				{
					this.Output.Write(" ");
				}
				else
				{
					this.Output.WriteLine("");
				}
				this.Output.Write("finally");
				this.OutputStartingBrace();
				this.Indent++;
				this.GenerateStatements(collection2);
				this.Indent--;
			}
			this.Output.WriteLine("}");
		}

		private void GenerateType(CodeTypeDeclaration e)
		{
			this.currentClass = e;
			if (e.StartDirectives.Count > 0)
			{
				this.GenerateDirectives(e.StartDirectives);
			}
			this.GenerateCommentStatements(e.Comments);
			if (e.LinePragma != null)
			{
				this.GenerateLinePragmaStart(e.LinePragma);
			}
			this.GenerateTypeStart(e);
			if (this.Options.VerbatimOrder)
			{
				foreach (CodeTypeMember member1 in e.Members)
				{
					this.GenerateTypeMember(member1, e);
				}
			}
			else
			{
				this.GenerateFields(e);
				this.GenerateSnippetMembers(e);
				this.GenerateTypeConstructors(e);
				this.GenerateConstructors(e);
				this.GenerateProperties(e);
				this.GenerateEvents(e);
				this.GenerateMethods(e);
				this.GenerateNestedTypes(e);
			}
			this.currentClass = e;
			this.GenerateTypeEnd(e);
			if (e.LinePragma != null)
			{
				this.GenerateLinePragmaEnd(e.LinePragma);
			}
			if (e.EndDirectives.Count > 0)
			{
				this.GenerateDirectives(e.EndDirectives);
			}
		}

		private void GenerateTypeConstructor(CodeTypeConstructor e)
		{
			if (this.IsCurrentClass || this.IsCurrentStruct)
			{
				if (e.CustomAttributes.Count > 0)
				{
					this.GenerateAttributes(e.CustomAttributes);
				}
				this.Output.Write("static ");
				this.Output.Write(this.CurrentTypeName);
				this.Output.Write("()");
				this.OutputStartingBrace();
				this.Indent++;
				this.GenerateStatements(e.Statements);
				this.Indent--;
				this.Output.WriteLine("}");
			}
		}

		private void GenerateTypeConstructors(CodeTypeDeclaration e)
		{
			IEnumerator enumerator1 = e.Members.GetEnumerator();
			while (enumerator1.MoveNext())
			{
				if (enumerator1.Current is CodeTypeConstructor)
				{
					this.currentMember = (CodeTypeMember) enumerator1.Current;
					if (this.options.BlankLinesBetweenMembers)
					{
						this.Output.WriteLine();
					}
					if (this.currentMember.StartDirectives.Count > 0)
					{
						this.GenerateDirectives(this.currentMember.StartDirectives);
					}
					this.GenerateCommentStatements(this.currentMember.Comments);
					CodeTypeConstructor constructor1 = (CodeTypeConstructor) enumerator1.Current;
					if (constructor1.LinePragma != null)
					{
						this.GenerateLinePragmaStart(constructor1.LinePragma);
					}
					this.GenerateTypeConstructor(constructor1);
					if (constructor1.LinePragma != null)
					{
						this.GenerateLinePragmaEnd(constructor1.LinePragma);
					}
					if (this.currentMember.EndDirectives.Count > 0)
					{
						this.GenerateDirectives(this.currentMember.EndDirectives);
					}
				}
			}
		}

		private void GenerateTypeEnd(CodeTypeDeclaration e)
		{
			if (!this.IsCurrentDelegate)
			{
				this.Indent--;
				this.Output.WriteLine("}");
			}
		}

		private void GenerateTypeMember(CodeTypeMember member, CodeTypeDeclaration declaredType)
		{
			if (this.options.BlankLinesBetweenMembers)
			{
				this.Output.WriteLine();
			}
			if (member is CodeTypeDeclaration)
			{
				((ICodeGenerator)this).GenerateCodeFromType((CodeTypeDeclaration)member, this.output.InnerWriter, this.options);
				this.currentClass = declaredType;
			}
			else
			{
				if (member.StartDirectives.Count > 0)
				{
					this.GenerateDirectives(member.StartDirectives);
				}
				this.GenerateCommentStatements(member.Comments);
				if (member.LinePragma != null)
				{
					this.GenerateLinePragmaStart(member.LinePragma);
				}
				if (member is CodeMemberField)
				{
					this.GenerateField((CodeMemberField) member);
				}
				else if (member is CodeMemberProperty)
				{
					this.GenerateProperty((CodeMemberProperty) member, declaredType);
				}
				else if (member is CodeMemberMethod)
				{
					if (member is CodeConstructor)
					{
						this.GenerateConstructor((CodeConstructor) member, declaredType);
					}
					else if (member is CodeTypeConstructor)
					{
						this.GenerateTypeConstructor((CodeTypeConstructor) member);
					}
					else if (member is CodeEntryPointMethod)
					{
						this.GenerateEntryPointMethod((CodeEntryPointMethod) member, declaredType);
					}
					else
					{
						this.GenerateMethod((CodeMemberMethod) member, declaredType);
					}
				}
				else if (member is CodeMemberEvent)
				{
					this.GenerateEvent((CodeMemberEvent) member, declaredType);
				}
				else if (member is CodeSnippetTypeMember)
				{
					int num1 = this.Indent;
					this.Indent = 0;
					this.GenerateSnippetMember((CodeSnippetTypeMember) member);
					this.Indent = num1;
					this.Output.WriteLine();
				}
				if (member.LinePragma != null)
				{
					this.GenerateLinePragmaEnd(member.LinePragma);
				}
				if (member.EndDirectives.Count > 0)
				{
					this.GenerateDirectives(member.EndDirectives);
				}
			}
		}

		private void GenerateTypeOfExpression(CodeTypeOfExpression e)
		{
			this.Output.Write("typeof(");
			this.OutputType(e.Type);
			this.Output.Write(")");
		}

		private void GenerateTypeReferenceExpression(CodeTypeReferenceExpression e)
		{
			this.OutputType(e.Type);
		}

		private void GenerateTypes(CodeNamespace e)
		{
			foreach (CodeTypeDeclaration declaration1 in e.Types)
			{
				if (this.options.BlankLinesBetweenMembers)
				{
					this.Output.WriteLine();
				}
				((ICodeGenerator)this).GenerateCodeFromType(declaration1, this.output.InnerWriter, this.options);
			}
		}

		private void GenerateTypeStart(CodeTypeDeclaration e)
		{
			if (e.CustomAttributes.Count > 0)
			{
				this.GenerateAttributes(e.CustomAttributes);
			}
			if (!this.IsCurrentDelegate)
			{
				this.OutputTypeAttributes(e);
				this.OutputIdentifier(e.Name);
				this.OutputTypeParameters(e.TypeParameters);
				bool flag1 = true;
				foreach (CodeTypeReference reference1 in e.BaseTypes)
				{
					if (flag1)
					{
						this.Output.Write(" : ");
						flag1 = false;
					}
					else
					{
						this.Output.Write(", ");
					}
					this.OutputType(reference1);
				}
				this.OutputTypeParameterConstraints(e.TypeParameters);
				this.OutputStartingBrace();
				this.Indent++;
			}
			else
			{
				switch ((e.TypeAttributes & TypeAttributes.NestedFamORAssem))
				{
					case TypeAttributes.Public:
						this.Output.Write("public ");
						break;
				}
				CodeTypeDelegate delegate1 = (CodeTypeDelegate) e;
				this.Output.Write("delegate ");
				this.OutputType(delegate1.ReturnType);
				this.Output.Write(" ");
				this.OutputIdentifier(e.Name);
				this.Output.Write("(");
				this.OutputParameters(delegate1.Parameters);
				this.Output.WriteLine(");");
			}
		}

		private void GenerateVariableDeclarationStatement(CodeVariableDeclarationStatement e)
		{
			this.OutputTypeNamePair(e.Type, e.Name);
			if (e.InitExpression != null)
			{
				this.Output.Write(" = ");
				this.GenerateExpression(e.InitExpression);
			}
			if (!this.generatingForLoop)
			{
				this.Output.WriteLine(";");
			}
		}

		private void GenerateVariableReferenceExpression(CodeVariableReferenceExpression e)
		{
			this.OutputIdentifier(e.VariableName);
		}

		private string GetBaseTypeOutput(CodeTypeReference typeRef)
		{
			string text1 = typeRef.BaseType;
			if (text1.Length == 0)
			{
				return "void";
			}
			string text2 = text1.ToLower(CultureInfo.InvariantCulture);
			string text4 = text2;
			if (text4 != null)
			{
				int num5;
				if (systemTypes == null)
				{
					systemTypes = new Dictionary<string, int>(0x10);
					systemTypes.Add("system.int16", 0);
					systemTypes.Add("system.int32", 1);
					systemTypes.Add("system.int64", 2);
					systemTypes.Add("system.string", 3);
					systemTypes.Add("system.object", 4);
					systemTypes.Add("system.boolean", 5);
					systemTypes.Add("system.void", 6);
					systemTypes.Add("system.char", 7);
					systemTypes.Add("system.byte", 8);
					systemTypes.Add("system.uint16", 9);
					systemTypes.Add("system.uint32", 10);
					systemTypes.Add("system.uint64", 11);
					systemTypes.Add("system.sbyte", 12);
					systemTypes.Add("system.single", 13);
					systemTypes.Add("system.double", 14);
					systemTypes.Add("system.decimal", 15);
				}
				if (systemTypes.TryGetValue(text4, out num5))
				{
					switch (num5)
					{
						case 0:
							return "short";

						case 1:
							return "int";

						case 2:
							return "long";

						case 3:
							return "string";

						case 4:
							return "object";

						case 5:
							return "bool";

						case 6:
							return "void";

						case 7:
							return "char";

						case 8:
							return "byte";

						case 9:
							return "ushort";

						case 10:
							return "uint";

						case 11:
							return "ulong";

						case 12:
							return "sbyte";

						case 13:
							return "float";

						case 14:
							return "double";

						case 15:
							return "decimal";
					}
				}
			}
			StringBuilder builder1 = new StringBuilder(text1.Length + 10);
			if (typeRef.Options == CodeTypeReferenceOptions.GlobalReference)
			{
				builder1.Append("global::");
			}
			string text3 = typeRef.BaseType;
			int num1 = 0;
			int num2 = 0;
			for (int num3 = 0; num3 < text3.Length; num3++)
			{
				switch (text3[num3])
				{
					case '+':
					case '.':
						builder1.Append(this.CreateEscapedIdentifier(text3.Substring(num1, num3 - num1)));
						builder1.Append('.');
						num3++;
						num1 = num3;
						goto Label_0357;

					case '`':
						break;

					default:
						goto Label_0357;
				}
				builder1.Append(this.CreateEscapedIdentifier(text3.Substring(num1, num3 - num1)));
				num3++;
				int num4 = 0;
				while (((num3 < text3.Length) && (text3[num3] >= '0')) && (text3[num3] <= '9'))
				{
					num4 = (num4 * 10) + (text3[num3] - '0');
					num3++;
				}
				this.GetTypeArgumentsOutput(typeRef.TypeArguments, num2, num4, builder1);
				num2 += num4;
				if ((num3 < text3.Length) && ((text3[num3] == '+') || (text3[num3] == '.')))
				{
					builder1.Append('.');
					num3++;
				}
				num1 = num3;
			Label_0357:;
			}
			if (num1 < text3.Length)
			{
				builder1.Append(this.CreateEscapedIdentifier(text3.Substring(num1)));
			}
			return builder1.ToString();
		}

		private string GetResponseFileCmdArgs(CompilerParameters options, string cmdArgs)
		{
			string text1 = options.TempFiles.AddExtension("cmdline");
			Stream stream1 = new FileStream(text1, FileMode.Create, FileAccess.Write, FileShare.Read);
			try
			{
				using (StreamWriter writer1 = new StreamWriter(stream1, Encoding.UTF8))
				{
					writer1.Write(cmdArgs);
					writer1.Flush();
				}
			}
			finally
			{
				stream1.Close();
			}
			return ("/noconfig /fullpaths @\"" + text1 + "\"");
		}

		private string GetTypeArgumentsOutput(CodeTypeReferenceCollection typeArguments)
		{
			StringBuilder builder1 = new StringBuilder(0x80);
			this.GetTypeArgumentsOutput(typeArguments, 0, typeArguments.Count, builder1);
			return builder1.ToString();
		}

		private void GetTypeArgumentsOutput(CodeTypeReferenceCollection typeArguments, int start, int length, StringBuilder sb)
		{
			sb.Append('<');
			bool flag1 = true;
			for (int num1 = start; num1 < (start + length); num1++)
			{
				if (flag1)
				{
					flag1 = false;
				}
				else
				{
					sb.Append(", ");
				}
				if (num1 < typeArguments.Count)
				{
					sb.Append(this.GetTypeOutput(typeArguments[num1]));
				}
			}
			sb.Append('>');
		}

		public string GetTypeOutput(CodeTypeReference typeRef)
		{
			string text1 = string.Empty;
			CodeTypeReference reference1 = typeRef;
			while (reference1.ArrayElementType != null)
			{
				reference1 = reference1.ArrayElementType;
			}
			text1 = text1 + this.GetBaseTypeOutput(reference1);
			while ((typeRef != null) && (typeRef.ArrayRank > 0))
			{
				char[] chArray1 = new char[typeRef.ArrayRank + 1];
				chArray1[0] = '[';
				chArray1[typeRef.ArrayRank] = ']';
				for (int num1 = 1; num1 < typeRef.ArrayRank; num1++)
				{
					chArray1[num1] = ',';
				}
				text1 = text1 + new string(chArray1);
				typeRef = typeRef.ArrayElementType;
			}
			return text1;
		}

		private bool GetUserData(CodeObject e, string property, bool defaultValue)
		{
			object obj1 = e.UserData[property];
			if ((obj1 != null) && (obj1 is bool))
			{
				return (bool) obj1;
			}
			return defaultValue;
		}

		private static bool IsKeyword(string value)
		{
			return keywords.Contains(value);
		}

		private static bool IsPrefixTwoUnderscore(string value)
		{
			if ((value.Length >= 3) && ((value[0] == '_') && (value[1] == '_')))
			{
				return (value[2] != '_');
			}
			return false;
		}

		public bool IsValidIdentifier(string value)
		{
			if ((value == null) || (value.Length == 0))
			{
				return false;
			}
			if (value.Length > 0x200)
			{
				return false;
			}
			if (value[0] != '@')
			{
				if (CSharpCodeGenerator.IsKeyword(value))
				{
					return false;
				}
			}
			else
			{
				value = value.Substring(1);
			}
			return CodeGenerator.IsValidLanguageIndependentIdentifier(value);
		}

		private static string JoinStringArray(string[] sa, string separator)
		{
			if ((sa == null) || (sa.Length == 0))
			{
				return string.Empty;
			}
			if (sa.Length == 1)
			{
				return ("\"" + sa[0] + "\"");
			}
			StringBuilder builder1 = new StringBuilder();
			for (int num1 = 0; num1 < (sa.Length - 1); num1++)
			{
				builder1.Append("\"");
				builder1.Append(sa[num1]);
				builder1.Append("\"");
				builder1.Append(separator);
			}
			builder1.Append("\"");
			builder1.Append(sa[sa.Length - 1]);
			builder1.Append("\"");
			return builder1.ToString();
		}

		private void OutputAttributeArgument(CodeAttributeArgument arg)
		{
			if ((arg.Name != null) && (arg.Name.Length > 0))
			{
				this.OutputIdentifier(arg.Name);
				this.Output.Write("=");
			}
			((ICodeGenerator)this).GenerateCodeFromExpression(arg.Value, this.output.InnerWriter, this.options);
		}

		private void OutputDirection(FieldDirection dir)
		{
			switch (dir)
			{
				case FieldDirection.In:
					return;

				case FieldDirection.Out:
					this.Output.Write("out ");
					return;

				case FieldDirection.Ref:
					this.Output.Write("ref ");
					return;
			}
		}

		private void OutputExpressionList(CodeExpressionCollection expressions)
		{
			this.OutputExpressionList(expressions, false);
		}

		private void OutputExpressionList(CodeExpressionCollection expressions, bool newlineBetweenItems)
		{
			bool flag1 = true;
			IEnumerator enumerator1 = expressions.GetEnumerator();
			this.Indent++;
			while (enumerator1.MoveNext())
			{
				if (flag1)
				{
					flag1 = false;
				}
				else if (newlineBetweenItems)
				{
					this.ContinueOnNewLine(",");
				}
				else
				{
					this.Output.Write(", ");
				}
				((ICodeGenerator)this).GenerateCodeFromExpression((CodeExpression)enumerator1.Current, this.output.InnerWriter, this.options);
			}
			this.Indent--;
		}

		private void OutputFieldScopeModifier(MemberAttributes attributes)
		{
			switch ((attributes & MemberAttributes.ScopeMask))
			{
				case MemberAttributes.Final:
				case MemberAttributes.Override:
					return;

				case MemberAttributes.Static:
					this.Output.Write("static ");
					return;

				case MemberAttributes.Const:
					this.Output.Write("const ");
					return;
			}
		}

		private void OutputIdentifier(string ident)
		{
			this.Output.Write(this.CreateEscapedIdentifier(ident));
		}

		private void OutputMemberAccessModifier(MemberAttributes attributes)
		{
			MemberAttributes attributes1 = attributes & MemberAttributes.AccessMask;
			if (attributes1 <= MemberAttributes.Family)
			{
				if (attributes1 != MemberAttributes.Assembly)
				{
					if (attributes1 != MemberAttributes.FamilyAndAssembly)
					{
						if (attributes1 == MemberAttributes.Family)
						{
							this.Output.Write("protected ");
						}
						return;
					}
					this.Output.Write("internal ");
					return;
				}
			}
			else
			{
				switch (attributes1)
				{
					case MemberAttributes.FamilyOrAssembly:
						this.Output.Write("protected internal ");
						return;

					case MemberAttributes.Private:
						this.Output.Write("private ");
						return;

					case MemberAttributes.Public:
						this.Output.Write("public ");
						return;
				}
				return;
			}
			this.Output.Write("internal ");
		}

		private void OutputMemberScopeModifier(MemberAttributes attributes)
		{
			switch ((attributes & MemberAttributes.ScopeMask))
			{
				case MemberAttributes.Abstract:
					this.Output.Write("abstract ");
					return;

				case MemberAttributes.Final:
					this.Output.Write("");
					return;

				case MemberAttributes.Static:
					this.Output.Write("static ");
					return;

				case MemberAttributes.Override:
					this.Output.Write("override ");
					return;
			}
			switch ((attributes & MemberAttributes.AccessMask))
			{
				case MemberAttributes.Assembly:
				case MemberAttributes.Family:
				case MemberAttributes.Public:
					this.Output.Write("virtual ");
					break;
			}
		}

		private void OutputOperator(CodeBinaryOperatorType op)
		{
			switch (op)
			{
				case CodeBinaryOperatorType.Add:
					this.Output.Write("+");
					return;

				case CodeBinaryOperatorType.Subtract:
					this.Output.Write("-");
					return;

				case CodeBinaryOperatorType.Multiply:
					this.Output.Write("*");
					return;

				case CodeBinaryOperatorType.Divide:
					this.Output.Write("/");
					return;

				case CodeBinaryOperatorType.Modulus:
					this.Output.Write("%");
					return;

				case CodeBinaryOperatorType.Assign:
					this.Output.Write("=");
					return;

				case CodeBinaryOperatorType.IdentityInequality:
					this.Output.Write("!=");
					return;

				case CodeBinaryOperatorType.IdentityEquality:
					this.Output.Write("==");
					return;

				case CodeBinaryOperatorType.ValueEquality:
					this.Output.Write("==");
					return;

				case CodeBinaryOperatorType.BitwiseOr:
					this.Output.Write("|");
					return;

				case CodeBinaryOperatorType.BitwiseAnd:
					this.Output.Write("&");
					return;

				case CodeBinaryOperatorType.BooleanOr:
					this.Output.Write("||");
					return;

				case CodeBinaryOperatorType.BooleanAnd:
					this.Output.Write("&&");
					return;

				case CodeBinaryOperatorType.LessThan:
					this.Output.Write("<");
					return;

				case CodeBinaryOperatorType.LessThanOrEqual:
					this.Output.Write("<=");
					return;

				case CodeBinaryOperatorType.GreaterThan:
					this.Output.Write(">");
					return;

				case CodeBinaryOperatorType.GreaterThanOrEqual:
					this.Output.Write(">=");
					return;
			}
		}

		private void OutputParameters(CodeParameterDeclarationExpressionCollection parameters)
		{
			bool flag1 = true;
			bool flag2 = parameters.Count > 15;
			if (flag2)
			{
				this.Indent += 3;
			}
			foreach (CodeParameterDeclarationExpression expression1 in parameters)
			{
				if (flag1)
				{
					flag1 = false;
				}
				else
				{
					this.Output.Write(", ");
				}
				if (flag2)
				{
					this.ContinueOnNewLine("");
				}
				this.GenerateExpression(expression1);
			}
			if (flag2)
			{
				this.Indent -= 3;
			}
		}

		private void OutputStartingBrace()
		{
			if (this.Options.BracingStyle == "C")
			{
				this.Output.WriteLine("");
				this.Output.WriteLine("{");
			}
			else
			{
				this.Output.WriteLine(" {");
			}
		}

		private void OutputType(CodeTypeReference typeRef)
		{
			this.Output.Write(this.GetTypeOutput(typeRef));
		}

		private void OutputTypeAttributes(CodeTypeDeclaration e)
		{
			if ((e.Attributes & MemberAttributes.New) != ((MemberAttributes) 0))
			{
				this.Output.Write("new ");
			}
			TypeAttributes attributes1 = e.TypeAttributes;
			switch ((attributes1 & TypeAttributes.NestedFamORAssem))
			{
				case TypeAttributes.AutoLayout:
				case TypeAttributes.NestedAssembly:
				case TypeAttributes.NestedFamANDAssem:
					this.Output.Write("internal ");
					break;

				case TypeAttributes.Public:
				case TypeAttributes.NestedPublic:
					this.Output.Write("public ");
					break;

				case TypeAttributes.NestedPrivate:
					this.Output.Write("private ");
					break;

				case TypeAttributes.NestedFamily:
					this.Output.Write("protected ");
					break;

				case TypeAttributes.NestedFamORAssem:
					this.Output.Write("protected internal ");
					break;
			}
			// Add missing "static" feature
			if (e.UserData.Contains("static"))
			{
				e.UserData.Remove("static");
				Output.Write("static ");
			}
			if (e.IsStruct)
			{
				if (e.IsPartial)
				{
					this.Output.Write("partial ");
				}
				this.Output.Write("struct ");
			}
			else if (e.IsEnum)
			{
				this.Output.Write("enum ");
			}
			else
			{
				TypeAttributes attributes3 = attributes1 & TypeAttributes.Interface;
				if (attributes3 != TypeAttributes.AutoLayout)
				{
					if (attributes3 != TypeAttributes.Interface)
					{
						return;
					}
				}
				else
				{
					if ((attributes1 & TypeAttributes.Sealed) == TypeAttributes.Sealed)
					{
						this.Output.Write("sealed ");
					}
					if ((attributes1 & TypeAttributes.Abstract) == TypeAttributes.Abstract)
					{
						this.Output.Write("abstract ");
					}
					if (e.IsPartial)
					{
						this.Output.Write("partial ");
					}
					this.Output.Write("class ");
					return;
				}
				if (e.IsPartial)
				{
					this.Output.Write("partial ");
				}
				this.Output.Write("interface ");
			}
		}

		private void OutputTypeNamePair(CodeTypeReference typeRef, string name)
		{
			this.OutputType(typeRef);
			this.Output.Write(" ");
			this.OutputIdentifier(name);
		}

		private void OutputTypeParameterConstraints(CodeTypeParameterCollection typeParameters)
		{
			if (typeParameters.Count != 0)
			{
				for (int num1 = 0; num1 < typeParameters.Count; num1++)
				{
					this.Output.WriteLine();
					this.Indent++;
					bool flag1 = true;
					if (typeParameters[num1].Constraints.Count > 0)
					{
						foreach (CodeTypeReference reference1 in typeParameters[num1].Constraints)
						{
							if (flag1)
							{
								this.Output.Write("where ");
								this.Output.Write(typeParameters[num1].Name);
								this.Output.Write(" : ");
								flag1 = false;
							}
							else
							{
								this.Output.Write(", ");
							}
							this.OutputType(reference1);
						}
					}
					if (typeParameters[num1].HasConstructorConstraint)
					{
						if (flag1)
						{
							this.Output.Write("where ");
							this.Output.Write(typeParameters[num1].Name);
							this.Output.Write(" : new()");
						}
						else
						{
							this.Output.Write(", new ()");
						}
					}
					this.Indent--;
				}
			}
		}

		private void OutputTypeParameters(CodeTypeParameterCollection typeParameters)
		{
			if (typeParameters.Count != 0)
			{
				this.Output.Write('<');
				bool flag1 = true;
				for (int num1 = 0; num1 < typeParameters.Count; num1++)
				{
					if (flag1)
					{
						flag1 = false;
					}
					else
					{
						this.Output.Write(", ");
					}
					if (typeParameters[num1].CustomAttributes.Count > 0)
					{
						this.GenerateAttributes(typeParameters[num1].CustomAttributes, null, true);
						this.Output.Write(' ');
					}
					this.Output.Write(typeParameters[num1].Name);
				}
				this.Output.Write('>');
			}
		}

		private void OutputVTableModifier(MemberAttributes attributes)
		{
			MemberAttributes attributes1 = attributes & MemberAttributes.VTableMask;
			if (attributes1 == MemberAttributes.New)
			{
				this.Output.Write("new ");
			}
		}

		private string QuoteSnippetString(string value)
		{
			if (((value.Length >= 0x100) && (value.Length <= 0x5dc)) && (value.IndexOf('\0') == -1))
			{
				return this.QuoteSnippetStringVerbatimStyle(value);
			}
			return this.QuoteSnippetStringCStyle(value);
		}

		private string QuoteSnippetStringCStyle(string value)
		{
			StringBuilder builder1 = new StringBuilder(value.Length + 5);
			builder1.Append("\"");
			for (int num1 = 0; num1 < value.Length; num1++)
			{
				switch (value[num1])
				{
					case '\u2028':
					case '\u2029':
						this.AppendEscapedChar(builder1, value[num1]);
						break;

					case '\\':
						builder1.Append(@"\\");
						break;

					case '\'':
						builder1.Append(@"\'");
						break;

					case '\t':
						builder1.Append(@"\t");
						break;

					case '\n':
						builder1.Append(@"\n");
						break;

					case '\r':
						builder1.Append(@"\r");
						break;

					case '"':
						builder1.Append("\\\"");
						break;

					case '\0':
						builder1.Append(@"\0");
						break;

					default:
						builder1.Append(value[num1]);
						break;
				}
				if ((num1 > 0) && ((num1 % 80) == 0))
				{
					if ((char.IsHighSurrogate(value[num1]) && (num1 < (value.Length - 1))) && char.IsLowSurrogate(value[num1 + 1]))
					{
						builder1.Append(value[++num1]);
					}
					builder1.Append("\" +\r\n");
					builder1.Append(IndentationString(Indent + 1));
					builder1.Append('"');
				}
			}
			builder1.Append("\"");
			return builder1.ToString();
		}

		private string QuoteSnippetStringVerbatimStyle(string value)
		{
			StringBuilder builder1 = new StringBuilder(value.Length + 5);
			builder1.Append("@\"");
			for (int num1 = 0; num1 < value.Length; num1++)
			{
				if (value[num1] == '"')
				{
					builder1.Append("\"\"");
				}
				else
				{
					builder1.Append(value[num1]);
				}
			}
			builder1.Append("\"");
			return builder1.ToString();
		}

		private void ResolveReferencedAssemblies(CompilerParameters options, CodeCompileUnit e)
		{
			if (e.ReferencedAssemblies.Count > 0)
			{
				foreach (string text1 in e.ReferencedAssemblies)
				{
					if (!options.ReferencedAssemblies.Contains(text1))
					{
						options.ReferencedAssemblies.Add(text1);
					}
				}
			}
		}

		public bool Supports(GeneratorSupport support)
		{
			return ((support & (GeneratorSupport.DeclareIndexerProperties | GeneratorSupport.GenericTypeDeclaration | GeneratorSupport.GenericTypeReference | GeneratorSupport.PartialTypes | GeneratorSupport.Resources | GeneratorSupport.Win32Resources | GeneratorSupport.ComplexExpressions | GeneratorSupport.PublicStaticMembers | GeneratorSupport.MultipleInterfaceMembers | GeneratorSupport.NestedTypes | GeneratorSupport.ChainedConstructorArguments | GeneratorSupport.ReferenceParameters | GeneratorSupport.ParameterAttributes | GeneratorSupport.AssemblyAttributes | GeneratorSupport.DeclareEvents | GeneratorSupport.DeclareInterfaces | GeneratorSupport.DeclareDelegates | GeneratorSupport.DeclareEnums | GeneratorSupport.DeclareValueTypes | GeneratorSupport.ReturnTypeAttributes | GeneratorSupport.TryCatchStatements | GeneratorSupport.StaticConstructors | GeneratorSupport.MultidimensionalArrays | GeneratorSupport.GotoStatements | GeneratorSupport.EntryPointMethod | GeneratorSupport.ArraysOfArrays)) == support);
		}

		void ICodeGenerator.GenerateCodeFromCompileUnit(CodeCompileUnit e, TextWriter w, CodeGeneratorOptions o)
		{
			bool flag1 = false;
			if ((this.output != null) && (w != this.output.InnerWriter))
			{
				throw new InvalidOperationException("CodeGenOutputWriter");
			}
			if (this.output == null)
			{
				flag1 = true;
				this.options = (o == null) ? new CodeGeneratorOptions() : o;
				this.output = new IndentedTextWriter(w, this.options.IndentString);
			}
			try
			{
				if (e is CodeSnippetCompileUnit)
				{
					this.GenerateSnippetCompileUnit((CodeSnippetCompileUnit) e);
				}
				else
				{
					this.GenerateCompileUnit(e);
				}
			}
			finally
			{
				if (flag1)
				{
					this.output = null;
					this.options = null;
				}
			}
		}

		void ICodeGenerator.GenerateCodeFromExpression(CodeExpression e, TextWriter w, CodeGeneratorOptions o)
		{
			bool flag1 = false;
			if ((this.output != null) && (w != this.output.InnerWriter))
			{
				throw new InvalidOperationException("CodeGenOutputWriter");
			}
			if (this.output == null)
			{
				flag1 = true;
				this.options = (o == null) ? new CodeGeneratorOptions() : o;
				this.output = new IndentedTextWriter(w, this.options.IndentString);
			}
			try
			{
				this.GenerateExpression(e);
			}
			finally
			{
				if (flag1)
				{
					this.output = null;
					this.options = null;
				}
			}
		}

		void ICodeGenerator.GenerateCodeFromNamespace(CodeNamespace e, TextWriter w, CodeGeneratorOptions o)
		{
			bool flag1 = false;
			if ((this.output != null) && (w != this.output.InnerWriter))
			{
				throw new InvalidOperationException("CodeGenOutputWriter");
			}
			if (this.output == null)
			{
				flag1 = true;
				this.options = (o == null) ? new CodeGeneratorOptions() : o;
				this.output = new IndentedTextWriter(w, this.options.IndentString);
			}
			try
			{
				this.GenerateNamespace(e);
			}
			finally
			{
				if (flag1)
				{
					this.output = null;
					this.options = null;
				}
			}
		}

		void ICodeGenerator.GenerateCodeFromStatement(CodeStatement e, TextWriter w, CodeGeneratorOptions o)
		{
			bool flag1 = false;
			if ((this.output != null) && (w != this.output.InnerWriter))
			{
				throw new InvalidOperationException("CodeGenOutputWriter");
			}
			if (this.output == null)
			{
				flag1 = true;
				this.options = (o == null) ? new CodeGeneratorOptions() : o;
				this.output = new IndentedTextWriter(w, this.options.IndentString);
			}
			try
			{
				this.GenerateStatement(e);
			}
			finally
			{
				if (flag1)
				{
					this.output = null;
					this.options = null;
				}
			}
		}

		void ICodeGenerator.GenerateCodeFromType(CodeTypeDeclaration e, TextWriter w, CodeGeneratorOptions o)
		{
			bool flag1 = false;
			if ((this.output != null) && (w != this.output.InnerWriter))
			{
				throw new InvalidOperationException("CodeGenOutputWriter");
			}
			if (this.output == null)
			{
				flag1 = true;
				this.options = (o == null) ? new CodeGeneratorOptions() : o;
				this.output = new IndentedTextWriter(w, this.options.IndentString);
			}
			try
			{
				this.GenerateType(e);
			}
			finally
			{
				if (flag1)
				{
					this.output = null;
					this.options = null;
				}
			}
		}

		public void ValidateIdentifier(string value)
		{
			if (!this.IsValidIdentifier(value))
			{
				throw new ArgumentException("InvalidIdentifier", value);
			}
		}


		private string CurrentTypeName
		{
			get
			{
				if (this.currentClass != null)
				{
					return this.currentClass.Name;
				}
				return "<% unknown %>";
			}
		}

		private string FileExtension
		{
			get
			{
				return ".cs";
			}
		}

		private int Indent
		{
			get
			{
				return this.output.Indent;
			}
			set
			{
				this.output.Indent = value;
			}
		}

		private bool IsCurrentClass
		{
			get
			{
				if ((this.currentClass != null) && !(this.currentClass is CodeTypeDelegate))
				{
					return this.currentClass.IsClass;
				}
				return false;
			}
		}

		private bool IsCurrentDelegate
		{
			get
			{
				if ((this.currentClass != null) && (this.currentClass is CodeTypeDelegate))
				{
					return true;
				}
				return false;
			}
		}

		private bool IsCurrentEnum
		{
			get
			{
				if ((this.currentClass != null) && !(this.currentClass is CodeTypeDelegate))
				{
					return this.currentClass.IsEnum;
				}
				return false;
			}
		}

		private bool IsCurrentInterface
		{
			get
			{
				if ((this.currentClass != null) && !(this.currentClass is CodeTypeDelegate))
				{
					return this.currentClass.IsInterface;
				}
				return false;
			}
		}

		private bool IsCurrentStruct
		{
			get
			{
				if ((this.currentClass != null) && !(this.currentClass is CodeTypeDelegate))
				{
					return this.currentClass.IsStruct;
				}
				return false;
			}
		}

		// Deal with extern declaration.
		private bool IsExtern(IDictionary userData)
		{
			if (userData == null)
				return false;

			return userData.Contains("extern");
		}

		private string NullToken
		{
			get
			{
				return "null";
			}
		}

		private CodeGeneratorOptions Options
		{
			get
			{
				return this.options;
			}
		}

		private TextWriter Output
		{
			get
			{
				return this.output;
			}
		}
	}
}
