header {
/// --------------------------------------------------------------------------------------------
#region /// Copyright (c) 2002, SIL International. All Rights Reserved.
/// <copyright from='2002' to='2002' company='SIL International'>
///		Copyright (c) 2002, SIL International. All Rights Reserved.
///
///		Distributable under the terms of either the Common Public License or the
///		GNU Lesser General Public License, as specified in the LICENSING.txt file.
/// </copyright>
#endregion
///
/// File: idl.g
/// Responsibility: Eberhard Beilharz
/// Last reviewed:
///
/// <remarks>
/// Defines the (partial) IDL grammar and some actions. It needs to be compiled with the ANTL
/// tool
/// </remarks>
/// --------------------------------------------------------------------------------------------

//#define DEBUG_IDLGRAMMAR

using System.Diagnostics;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
}

options {
	language = "CSharp";
	namespace  =  "SIL.FieldWorks.Tools";
}

// TODO: the MIDL compiler accepts keywords as identifiers (e.g. char string[3]); our grammar doesn't

/**
 *  This is a complete parser for the IDL language as defined
 *  by the CORBA 2.0 specification.  It will allow those who
 *  need an IDL parser to get up-and-running very quickly.
 *  Though IDL's syntax is very similar to C++, it is also
 *  much simpler, due in large part to the fact that it is
 *  a declarative-only language.
 *
 *  Some things that are not included are: Symbol table construction
 *  (it is not necessary for parsing, btw) and preprocessing (for
 *  IDL compiler #pragma directives). You can use just about any
 *  C or C++ preprocessor, but there is an interesting semantic
 *  issue if you are going to generate code: In C, #include is
 *  a literal include, in IDL, #include is more like Java's import:
 *  It adds definitions to the scope of the parse, but included
 *  definitions are not generated.
 *
 *  Jim Coker, jcoker@magelang.com
 */
class IDLParser extends Parser;
options {
	exportVocab=IDL;
	buildAST = true;	// uses CommonAST by default
}

tokens {
	V1_ENUM = "v1_enum";
	INT3264 = "__int3264";
	INT64 = "__int64";
}

{
	private CodeNamespace m_Namespace = null;
	private IDLConversions m_Conv = null;
}

specification [CodeNamespace cnamespace, IDLConversions conv]
	{
		m_Namespace = cnamespace;
		m_Conv = conv;
	}
	:   (definition)+ EOF
	;
	exception
	catch [RecognitionException ex]
	{
		reportError(ex);
		return;
	}


definition
	{
		Hashtable attributes = new Hashtable();
		CodeTypeMember type = null;
		CodeTypeDeclaration decl = null;
	}
	:   ( type=type_dcl SEMI!
			{
			#if DEBUG_IDLGRAMMAR
				System.Diagnostics.Debug.WriteLine(string.Format("\nType declaration found {0}\n\n", type != null ? type.Name : "<empty>"));
			#endif
				if (type != null && type is CodeTypeDeclaration)
				{
					m_Namespace.Types.Add((CodeTypeDeclaration)type);
					m_Namespace.UserData.Add(type.Name, type);
				}
			}
		| c:const_dcl SEMI!
			{
			#if DEBUG_IDLGRAMMAR
				System.Diagnostics.Debug.WriteLine(string.Format("\nConstant found {0}\n\n", c_AST != null ? c_AST.ToStringList() : "<null>"));
			#endif
			}
		| e:except_dcl SEMI!
			{
			#if DEBUG_IDLGRAMMAR
				System.Diagnostics.Debug.WriteLine(string.Format("\nException declaration found {0}\n\n", e_AST != null ? e_AST.ToStringList() : "<null>"));
			#endif
			}
		| (LBRACKET attribute_list[attributes] RBRACKET)?
			(l:library
				{
					#if DEBUG_IDLGRAMMAR
					System.Diagnostics.Debug.WriteLine(string.Format("\nLibrary found {0}\n\n", l_AST != null ? l_AST.ToStringList() : "<null>"));
					#endif
				}
			| decl=interf
				{
					if (!(bool)decl.UserData["IsPartial"])
					{
						#if DEBUG_IDLGRAMMAR
						System.Diagnostics.Debug.WriteLine(string.Format("\nInterface declaration found {0}\n\n", decl.Name));
						#endif
						m_Conv.HandleInterface(decl, m_Namespace, attributes);
						m_Namespace.Types.Add(decl);
						m_Namespace.UserData.Add(decl.Name, decl);
					}
				}
			| decl=coclass
				{
					if (!(bool)decl.UserData["IsPartial"])
					{
						#if DEBUG_IDLGRAMMAR
						System.Diagnostics.Debug.WriteLine(string.Format("\nCoclass declaration found {0}\n\n", decl.Name));
						#endif
						m_Conv.HandleCoClassInterface(decl, m_Namespace, attributes);
						// Add coclass interface
						m_Namespace.Types.Add(decl);
						// Add coclass object
						m_Namespace.Types.Add(m_Conv.DeclareCoClassObject(decl, m_Namespace, attributes));
						// Add coclass creator object
						m_Namespace.Types.Add(m_Conv.DeclareCoClassCreator(decl, m_Namespace, attributes));
						attributes.Clear();
					}
				}
			)
		| m:module SEMI!
			{
			#if DEBUG_IDLGRAMMAR
				System.Diagnostics.Debug.WriteLine(string.Format("\nModule found {0}\n\n", m_AST != null ? m_AST.ToStringList() : "<null>"));
			#endif
			}
		| import SEMI!
		| importlib SEMI!
		| cpp_quote!
		| mi:midl_pragma_warning!
			{
			#if DEBUG_IDLGRAMMAR
				System.Diagnostics.Debug.WriteLine(string.Format("\nMIDL pragma found {0}\n\n", mi_AST != null ? mi_AST.ToStringList() : "<null>"));
			#endif
			}
		)
	;

module
	: "module"^ identifier LBRACE d:definition_list RBRACE
	;

import
	: "import"^ string_literal (COMMA string_literal)*
			{
			#if DEBUG_IDLGRAMMAR
				System.Diagnostics.Debug.WriteLine(#import.ToStringList());
			#endif
			}
	;

definition_list
	:   (definition)+
	;


library
	: "library"^ identifier LBRACE (definition)+ RBRACE SEMI!
	;

coclass returns [CodeTypeDeclaration type]
	{
		Hashtable attributes = new Hashtable();
		type = new CodeTypeDeclaration();
		type.IsInterface = true;
		type.UserData.Add("IsPartial", false); // The isPartial property is a .Net 2.0 feature
	}
	: "coclass"^ name:identifier { type.Name = name_AST.getText(); }
		LBRACE
			(
				(LBRACKET attribute_list[attributes] RBRACKET)? interf_declr[type.BaseTypes] (SEMI!)?
			)*
		RBRACE
	;

attribute_list [IDictionary attributes]
	: attribute[attributes] (COMMA attribute[attributes])*
	;

attribute [IDictionary attributes]
	: "uuid" uuid:uuid_literal
		{ attributes.Add("Guid", new CodeAttributeArgument(new CodePrimitiveExpression(uuid_AST.ToStringList().Replace(" ", "")))); }
	| "version" LPAREN (~RPAREN)+ RPAREN
	| "async_uuid" uuid_literal
	| "local"
		{ attributes["local"] = true;}
	| "object"
	| "pointer_default" LPAREN ptr_attr RPAREN
	| "endpoint" LPAREN string_literal (COMMA string_literal)* RPAREN
	| "odl"
	| "optimize" LPAREN string_literal RPAREN
	| "proxy"
	/* type lib specific */
	| "aggregatable"
	| "appobject"
	| "bindable"
	| "control"
	| "custom" LPAREN name:string_literal COMMA value:non_rparen RPAREN
		{
			attributes.Add("custom", new CodeAttributeArgument(name_AST.getText(),
				new CodePrimitiveExpression(value_AST.getText())));
		}
	| "default"
	| "defaultbind"
	| "defaultcollelem"
	| "defaultvtable"
	| "displaybind"
	| "dllname" LPAREN string_literal RPAREN
	| "dual"
		{ attributes.Add("dual", new CodeAttributeArgument()); }
	| "entry" LPAREN (~RPAREN)+ RPAREN
	| "helpcontext" LPAREN integer_literal RPAREN
	| "helpfile" LPAREN string_literal RPAREN
	| "helpstring" LPAREN string_literal RPAREN
	| "helpstringdll" LPAREN string_literal RPAREN
	| "hidden"
	| "id" LPAREN ( integer_literal | identifier ) RPAREN
	| "idempotent"
	| "immediatebind"
	| "lcid" LPAREN integer_literal RPAREN
	| "licensed"
	| "message"
	| "nonbrowsable"
	| "noncreatable"
	| "nonextensible"
	| "oleautomation"
	| "restricted"
		{ attributes.Add("restricted", new CodeAttributeArgument()); }
	;

non_rparen
	: (~RPAREN)+
	;

lib_definition
	{
		Hashtable attributes = new Hashtable();
		CodeTypeMember ignored;
	}
	: (LBRACKET attribute_list[attributes] RBRACKET)? ignored=interf
	| ignored=type_dcl SEMI!
	| const_dcl SEMI!
	| importlib SEMI!
	| import SEMI!
	//| tagged_declarator SEMI!
	;

importlib
	: "importlib"^ LPAREN str:string_literal RPAREN
			{
			#if DEBUG_IDLGRAMMAR
				System.Diagnostics.Debug.WriteLine("importlib " + str_AST.getText());
			#endif
			}
	;

interf returns [CodeTypeDeclaration type]
	{
		bool fForwardDeclaration = true;
		StringCollection inherits;
		type = new CodeTypeDeclaration();
		type.IsInterface = true;
	}
	: ("interface"^ | "dispinterface"^) name:identifier inherits=inheritance_spec
		(LBRACE! (fForwardDeclaration=interface_body[type])*  RBRACE!)?
		{
			/// we don't treat a forward declaration as real declaration
			type.Name = name_AST.getText();
			type.UserData.Add("IsPartial", fForwardDeclaration);
			type.UserData.Add("inherits", inherits);
		}
	;

interf_declr [CodeTypeReferenceCollection baseTypes]
	: ("interface"^ | "dispinterface"^) name:identifier
	{
		baseTypes.Add(name_AST.getText());
	}
	;

interface_body [CodeTypeDeclaration type] returns [bool fForwardDeclaration]
	{
		CodeTypeMember member = null;
		CodeTypeMember ignored;
		fForwardDeclaration = false;
	}
	: ignored=type_dcl SEMI
	| const_dcl SEMI
	| except_dcl SEMI
	| attr_dcl SEMI
	| cpp_quote!
	| member=function_dcl[type.Members] SEMI
		{
			if (member != null)
				type.Members.Add(member);
		}
	;


inheritance_spec returns [StringCollection coll]
	{ coll = new StringCollection(); }
	: (COLON scoped_name_list[coll])?
	;

scoped_name_list [StringCollection coll]
	: name1:scoped_name
			{ coll.Add(name1_AST.getText()); }
		(COMMA name2:scoped_name
			{ coll.Add(name2_AST.getText()); }
		)*
	;

scoped_name
	: (SCOPEOP)? identifier (SCOPEOP identifier)*
	;

const_dcl
	{ string ignored; }
	: "const" const_type identifier ASSIGN ignored=const_exp
	;

const_type
	: base_type_spec
	| string_type
	| scoped_name (STAR)*
	;

/*   EXPRESSIONS   */

const_exp returns [string s]
	{ s = string.Empty; }
	:   s=or_expr
	;

or_expr returns [string s]
	{
		StringBuilder bldr = new StringBuilder();
		string expr = string.Empty;
		s = string.Empty;
	}
	: expr=xor_expr
		{ bldr.Append(expr); }
	  ( op:OR expr=xor_expr
			{
				bldr.Append(#op.getText());
				bldr.Append(expr);
			}
		)*
		{ s = bldr.ToString(); }
	;

xor_expr returns [string s]
	{
		StringBuilder bldr = new StringBuilder();
		string expr = string.Empty;
		s = string.Empty;
	}
	: expr=and_expr
		{ bldr.Append(expr); }
	  ( op:XOR expr=and_expr
			{
				bldr.Append(#op.getText());
				bldr.Append(expr);
			}
		)*
		{ s = bldr.ToString(); }
	;

and_expr returns [string s]
	{
		StringBuilder bldr = new StringBuilder();
		string expr = string.Empty;
		s = string.Empty;
	}
	: expr=shift_expr
		{ bldr.Append(expr); }
	  ( op:AND expr=shift_expr
			{
				bldr.Append(#op.getText());
				bldr.Append(expr);
			}
		)*
		{ s = bldr.ToString(); }
	;

shift_expr returns [string s]
	{
		StringBuilder bldr = new StringBuilder();
		string expr = string.Empty;
		s = string.Empty;
	}
	: expr=add_expr
		{ bldr.Append(expr); }
	  ( op:shift_op expr=add_expr
			{
				bldr.Append(#op.getText());
				bldr.Append(expr);
			}
		)*
		{ s = bldr.ToString(); }
	;

shift_op
	: LSHIFT
	| RSHIFT
	;

add_expr returns [string s]
	{
		StringBuilder bldr = new StringBuilder();
		string expr = string.Empty;
		s = string.Empty;
	}
	: expr=mult_expr
		{ bldr.Append(expr); }
	  ( op:add_op expr=mult_expr
			{
				bldr.Append(#op.getText());
				bldr.Append(expr);
			}
		)*
		{ s = bldr.ToString(); }
	;

add_op
	: PLUS
	| MINUS
	;

mult_expr returns [string s]
	{
		StringBuilder bldr = new StringBuilder();
		string expr = string.Empty;
		s = string.Empty;
	}
	: expr=unary_expr
		{ bldr.Append(expr); }
	  ( op:mult_op expr=unary_expr
			{
				bldr.Append(#op.getText());
				bldr.Append(expr);
			}
		)*
		{ s = bldr.ToString(); }
	;

mult_op
	: STAR
	| DIV
	| MOD
	;

unary_expr returns [string s]
	{
		string p = string.Empty;
		s = string.Empty;
	}
	: (u:unary_operator)? p=primary_expr
		{
			if (#u != null)
				s = #u.getText() + p;
			else
				s = p;
		}
	;

unary_operator
	: MINUS
	| PLUS
	| TILDE
	;

// Node of type TPrimaryExp serves to avoid inf. recursion on tree parse
primary_expr returns [string s]
	{
		string c;
		s = string.Empty;
	}
	: sn:scoped_name
		{ s = #sn.getText(); }
	| l:literal
		{ s = #l.getText(); }
	| LPAREN c=const_exp RPAREN
		{ s = "(" + c + ")"; }
	;

literal
	: (string_literal)=> string_literal
	| character_literal
	| floating_pt_or_integer_literal
	| boolean_literal
	;

boolean_literal
	: "TRUE"
	| "true"
	| "FALSE"
	| "false"
	;

positive_int_const
	{ string s; }
	: s=const_exp
	;


type_dcl returns [CodeTypeMember type]
	{
		type = null;
		string ignored;
		Hashtable attributes = new Hashtable();
	}
	: "typedef" (LBRACKET type_attributes RBRACKET)? type=type_declarator
	| type=struct_type
	| union_type
	| type=enum_type
	| /* empty */
	| "native" ignored=declarator[attributes]
	;

type_attributes
	: type_attribute (COMMA type_attribute)*
	;

type_attribute
	: "context_handle"
	| "handle"
	| "pipe" /* TODO: element_type pipe_declarator */
	| V1_ENUM
	| "transmit_as" LPAREN simple_type_spec RPAREN
	| "wire_marshal" LPAREN simple_type_spec RPAREN
	| "represent_as" LPAREN simple_type_spec RPAREN
	| "user_marshal" LPAREN simple_type_spec RPAREN
	| "public"
	/* the following 3 rules are not in the MS MIDL spec */
	| string_type
	| "switch_type" LPAREN switch_type_spec RPAREN
	| ptr_attr
	;

type_declarator returns [CodeTypeMember type]
	{
		type = null;
		string name;
		Hashtable attributes = new Hashtable();
	}
	: type=type_spec name=declarator_list[attributes]
		{
		#if DEBUG_IDLGRAMMAR
			System.Diagnostics.Debug.WriteLine(string.Format("typespec: {0}; {1}", type != null ? type.Name : "<empty>", name));
		#endif
			if (type.Name == string.Empty)
				type.Name = name;
		}
	;

type_spec  returns [CodeTypeMember type]
	{ type = null; }
	: ("const")?
		( s:simple_type_spec
			{
				type = new CodeMemberField();
				((CodeMemberField)type).Type = m_Conv.ConvertParamType(#s.getText(), null, new Hashtable());
				type.Attributes = (type.Attributes & ~MemberAttributes.AccessMask) | MemberAttributes.Public;
			}
		| type=constr_type_spec)
	;

simple_type_spec
	:   base_type_spec
	|   template_type_spec
	|   scoped_name (STAR)*
	;

base_type_spec
	: (  ("signed" | "unsigned")?
			( integer_type
			| char_type)
		| boolean_type
		| floating_pt_type
		| "octet"
		| "any"
		| "void"
		| "byte"
		| "wchar_t"
	) (STAR)*
	| "handle_t"
	;

attr_vars
	: attr_var (COMMA attr_var)*
	;

attr_var
	: (((STAR)? identifier) | INT | HEX)?
	;

ptr_attr
	: "ref"
	| "unique"
	| "ptr"
	;

integer_type
	:  ("small" | "short" | "long" | "int" | "hyper" | INT3264 | INT64) ("int")?
	;

char_type
	: "char"
	;

floating_pt_type
	: "float"
	| "double"
	;

boolean_type
	: "boolean"
	;

template_type_spec
	: sequence_type
	| string_type
	;

constr_type_spec returns [CodeTypeDeclaration type]
	{ type = null; }
	: type=struct_type
	| union_type
	| type=enum_type
	;

declarator_list [IDictionary attributes] returns [string s]
	{
		string ignored;
		s = string.Empty;
	}
	: s=declarator[attributes] (COMMA (STAR)* ignored=declarator[attributes])*
	;

declarator [IDictionary attributes] returns [string s]
	{
		s = string.Empty;
		string f = string.Empty;
		List<string> arraySize = new List<string>();
	}
	: i:identifier (f=fixed_array_size { arraySize.Add(f); })*
		{
		#if DEBUG_IDLGRAMMAR
			System.Diagnostics.Debug.WriteLine("declarator: " + #i.getText());
		#endif
			s = #i.getText();

			if (arraySize.Count > 0)
				attributes.Add("IsArray", arraySize);
		}
	;

struct_type returns [CodeTypeDeclaration type]
	{
		type = new CodeTypeDeclaration();
		type.IsStruct = true;
		// Add attribute: [StructLayout(LayoutKind.Sequential, Pack=4)]
		type.CustomAttributes.Add(new CodeAttributeDeclaration("StructLayout",
			new CodeAttributeArgument(new CodeVariableReferenceExpression("LayoutKind.Sequential")),
			new CodeAttributeArgument("Pack", new CodePrimitiveExpression(4))));
	}
	: "struct" (name:identifier)? LBRACE (member[type.Members])+ RBRACE (STAR)*
		{
			if (#name != null)
			{
				#if DEBUG_IDLGRAMMAR
				System.Console.WriteLine(string.Format("struct {0}", #name.getText()));
				#endif
				type.Name = #name.getText();
			}
		}
	;

member [CodeTypeMemberCollection members]
	{
		Hashtable attributes = new Hashtable();
		CodeTypeMember type;
		string name;
	}
	:   (field_attribute_list[attributes])? type=type_spec name=declarator_list[attributes] SEMI
		{
			if (type != null && name != string.Empty)
			{
				if (attributes["IsArray"] != null)
				{
					List<string> arraySizes = (List<string>)attributes["IsArray"];
					if (arraySizes.Count > 1)
					{
						Console.WriteLine(string.Format("Can't handle multi dimensional arrays: {0}",
							name));
					}

					if (arraySizes.Count == 1)
					{
						// Add attribute: [MarshalAs(UnmanagedType.ByValArray, SizeConst=x)]
						int val;
						if (int.TryParse(arraySizes[0], out val))
						{
							if (type is CodeMemberField)
								((CodeMemberField)type).Type.ArrayRank = 1;
							else
								Console.WriteLine(string.Format("Unhandled type: {0}", type.GetType()));

							type.CustomAttributes.Add(new CodeAttributeDeclaration("MarshalAs",
								new CodeAttributeArgument(
									new CodeSnippetExpression("UnmanagedType.ByValArray")),
								new CodeAttributeArgument("SizeConst",
									new CodePrimitiveExpression(val))));
						}
						else
						{
							Console.WriteLine(string.Format("Can't handle array dimension spec: '{0}' for {1}",
								arraySizes[0], name));
						}
					}
					attributes.Remove("IsArray");
				}

				type.Name = name;
				members.Add(type);
			}
		}
	;

// TODO
union_type
	: "union" name:identifier
			( "switch" LPAREN switch_type_spec identifier RPAREN (identifier)?
					LBRACE switch_body RBRACE
			| LBRACE n_e_case_list RBRACE)?
		{
			if (#name != null)
			{
				#if DEBUG_IDLGRAMMAR
				System.Console.WriteLine(string.Format("union {0}", #name.getText()));
				#endif
				CodeTypeDeclaration type = new CodeTypeDeclaration();
				type.IsStruct = true; // IsUnion does not exist
				type.Name = #name.getText();
				m_Namespace.UserData[type.Name] = type;
			}
		}
	;

switch_type_spec
	{ CodeTypeDeclaration type; }
	: ("signed" | "unsigned")?
		( integer_type
		| char_type)
	| boolean_type
	| type=enum_type
	| scoped_name
	;

switch_body
	:   case_stmt_list
	;

/* for non-encapsulated union */
n_e_case_list
	: (n_e_case_stmt)+
	;

n_e_case_stmt
	: n_e_case_label (unnamed_element_spec)? SEMI
	;

n_e_case_label
	{ string ignored; }
	: LBRACKET
		( "case" LPAREN ignored=const_exp RPAREN
		| "default")
	  RBRACKET
	;

/* for encapsulated union */
case_stmt_list
	:  (case_stmt)+
	;

case_stmt
	:   case_label_list unnamed_element_spec SEMI
	;

case_label_list
	:   (case_label)+
	;


case_label
	{ string ignored; }
	:   "case" ignored=const_exp COLON
	|   "default" COLON
	;

unnamed_element_spec
	{
		Hashtable attributes = new Hashtable();
		CodeTypeMember type;
		string ignored;
	}
	:   (field_attribute_list[attributes])? type=type_spec (ignored=declarator[attributes])?
	;

element_spec
	{
		Hashtable attributes = new Hashtable();
		CodeTypeMember type;
		string ignored;
	}
	:   (field_attribute_list[attributes])? type=type_spec ignored=declarator[attributes]
	;

enum_type returns [CodeTypeDeclaration type]
	{
		type = new CodeTypeDeclaration();
		type.IsEnum = true;
		string name = string.Empty;
	}
	:   "enum" (name:identifier { if (#name != null) name = #name.getText();} )?
			LBRACE enumerator_list[name, type.Members] RBRACE
		{
			if (#name != null)
			{
				#if DEBUG_IDLGRAMMAR
				System.Console.WriteLine(string.Format("enum {0}", #name.getText()));
				#endif
				type.Name = #name.getText();
			}
		}
	;

enumerator_list [string enumName, CodeTypeMemberCollection members]
	{
		string s1 = string.Empty;
		string s2 = string.Empty;
		string expr1 = string.Empty;
		string expr2 = string.Empty;
	}
	:    s1=enumerator[ref expr1]
			(COMMA s2=enumerator[ref expr2]
				{
					members.Add(IDLConversions.CreateEnumMember(enumName, s1, expr1));
					s1 = s2;
					expr1 = expr2;
				}
			)*
		{
			if (s1 != string.Empty)
				members.Add(IDLConversions.CreateEnumMember(enumName, s1, expr1));
		}
	;

enumerator [ref string e] returns [string s]
	{
		s = string.Empty;
		e = string.Empty;
	}
	:   name:identifier (ASSIGN e=const_exp)?
		{
			s = #name.getText();
		}
	| /* empty */
	;

	//enumerator_value ((OR | PLUS | AND | STAR | DIV) enumerator_value)*)?
enumerator_value
	: (MINUS)? (INT | HEX | identifier)
	;

sequence_type
	:   "sequence"
		 LT_ simple_type_spec opt_pos_int GT
	;

opt_pos_int
	:    (COMMA positive_int_const)?
	;

string_type
	:   "string" (STAR)*
	;

fixed_array_size returns [string s]
	{ s = string.Empty; }
	:   LBRACKET (bounds:array_bounds)? RBRACKET
		{
			if (#bounds != null)
				s = #bounds.getText();
		}
	;

array_bounds
	: array_bound (RANGE array_bound)?
	| STAR
	;

array_bound
	: positive_int_const
	;


attr_dcl
	{
		string ignored;
		Hashtable attributes = new Hashtable();
	}
	: ("readonly")? "attribute" param_type_spec ignored=declarator_list[attributes]
	;

except_dcl
	{ CodeTypeMemberCollection ignored = new CodeTypeMemberCollection(); }
	:   "exception"
		identifier
		 LBRACE (member[ignored])* RBRACE
	;

function_dcl [CodeTypeMemberCollection types] returns [CodeTypeMember memberRet]
	{
		CodeParameterDeclarationExpressionCollection pars;
		CodeMemberMethod member = new CodeMemberMethod();
		Hashtable funcAttributes = new Hashtable();
		memberRet = member;
	}
	: (function_attribute_list[funcAttributes])? rt:param_type_spec ("const")? name:identifier pars=parameter_dcls ("const")?
		{
			member.Name = #name.getText();
			member.Parameters.AddRange(pars);

			if (#rt == null)
				memberRet = null;
			else
			{
				CodeParameterDeclarationExpression param = new CodeParameterDeclarationExpression();
				Hashtable attributes = new Hashtable();
				param.Type = m_Conv.ConvertParamType(#rt.ToString(), param, attributes);

				m_Conv.HandleSizeIs(member, funcAttributes);
				memberRet = m_Conv.HandleFunction_dcl(member, param.Type, types, funcAttributes);
			}
		}
	;

function_attribute_list [IDictionary attributes]
	: LBRACKET (function_attribute[attributes] (COMMA function_attribute[attributes])*)? RBRACKET
	;

function_attribute [IDictionary attributes]
	: "callback"
	| "broadcast"
	| ptr_attr
	| string_type
	| "ignore"
	| "context_handle"
	| "propget"
		{
			attributes.Add("propget", new CodeAttributeArgument());
		}
	| "propput"
		{
			attributes.Add("propput", new CodeAttributeArgument());
		}
	| "propputref"
		{
			attributes.Add("propputref", new CodeAttributeArgument());
		}
	| "uidefault"
	| "usesgetlasterror"
	| "vararg"
	| attribute[attributes]
	;

parameter_dcls returns [CodeParameterDeclarationExpressionCollection paramColl]
	{
		paramColl = new CodeParameterDeclarationExpressionCollection();
		CodeParameterDeclarationExpression param;
	}
	: LPAREN! (param=param_dcl
		{ paramColl.Add(param); }
		(COMMA! param=param_dcl
		{ paramColl.Add(param); }
		)*)? RPAREN!
	;

param_dcl returns [CodeParameterDeclarationExpression param]
	{
		param = new CodeParameterDeclarationExpression();
		Hashtable attributes = new Hashtable();
		string name = string.Empty;
	}
	: (LBRACKET param_attributes[attributes] RBRACKET)? ("const")? strType:param_type_spec ("const")? (name=declarator[attributes])?
		{
			string str = null;
			if (#strType != null && name != string.Empty)
			{
				str = #strType.ToStringList();
				param.Name = IDLConversions.ConvertParamName(name);
				param.Type = m_Conv.ConvertParamType(str, param, attributes);
			}
		}
	;

param_attributes [IDictionary param]
	: param_attribute[param] (COMMA param_attribute[param])*
	;

param_attribute [IDictionary attributes]
	: "in"
		{ attributes["in"] = true;}
	| "out"
		{ attributes["out"] = true;}
	| "retval"
		{ attributes["retval"] = true; }
	| "defaultvalue" LPAREN (~RPAREN)+ RPAREN
	| "optional"
	| "readonly"
	| "requestedit"
	| "iid_is" LPAREN attr_vars RPAREN
	| "range" LPAREN integer_literal COMMA integer_literal RPAREN
	| field_attribute[attributes]
	;

field_attribute_list [IDictionary attributes]
	: LBRACKET field_attribute[attributes] (COMMA field_attribute[attributes])* RBRACKET
	;

field_attribute [IDictionary attributes]
	{ string val; }
	: "ignore"
	| "size_is" LPAREN (STAR)* val=const_exp RPAREN
		{ attributes.Add("size_is", val);}
	| "max_is" LPAREN attr_vars RPAREN
	| "length_is" LPAREN attr_vars RPAREN
	| "first_is" LPAREN attr_vars RPAREN
	| "last_is" LPAREN attr_vars RPAREN
	| "switch_is" LPAREN attr_var RPAREN
	| "source"
	| string_type
		{ attributes.Add("string", new CodeAttributeArgument());}
	| ptr_attr
	| attribute[attributes]
	;

raises_expr
	{ StringCollection ignored = new StringCollection(); }
	:   "raises" LPAREN scoped_name_list[ignored] RPAREN
	;

context_expr
	:   "context" LPAREN string_literal_list RPAREN
	;

string_literal_list
	:    string_literal (COMMA! string_literal)*
	;

uuid_literal
	: LPAREN! value:non_rparen RPAREN!
	;

param_type_spec
	: (base_type_spec
		| string_type
		| scoped_name (STAR)* // TODO
		| "SAFEARRAY" LPAREN base_type_spec RPAREN
		)
	;

integer_literal
	: INT
	| OCTAL
	| HEX
	;

string_literal
	: ("L")? (STRING_LITERAL)+
	;

character_literal
	: ("L")? CHAR_LITERAL
	;

floating_pt_or_integer_literal
	: INT (FLOAT)?
	| OCTAL
	| HEX
	;

identifier
	: IDENT
	;

cpp_quote
	: "cpp_quote" LPAREN string_literal RPAREN
	;

midl_pragma_warning
	: "midl_pragma_warning" LPAREN (~RPAREN)* RPAREN
	;

/* IDL LEXICAL RULES  */
class IDLLexer extends Lexer;
options {
	exportVocab=IDL;
	k=4;
}

SEMI
options {
  paraphrase = ";";
}
	:	';'
	;

QUESTION
options {
  paraphrase = "?";
}
	:	'?'
	;

LPAREN
options {
  paraphrase = "(";
}
	:	'('
	;

RPAREN
options {
  paraphrase = ")";
}
	:	')'
	;

LBRACKET
options {
  paraphrase = "[";
}
	:	'['
	;

RBRACKET
options {
  paraphrase = "]";
}
	:	']'
	;

LBRACE
options {
  paraphrase = "{";
}
	:	'{'
	;

RBRACE
options {
  paraphrase = "}";
}
	:	'}'
	;

OR
options {
  paraphrase = "|";
}
	:	'|'
	;

XOR
options {
  paraphrase = "^";
}
	:	'^'
	;

AND
options {
  paraphrase = "&";
}
	:	'&'
	;

COLON
options {
  paraphrase = ":";
}
	:	':'
	;

COMMA
options {
  paraphrase = ",";
}
	: ','
	;

DOT
options {
  paraphrase = ".";
}
	: '.'
	;

RANGE
options {
	paraphrase = "..";
}
	: ".."
	;

ASSIGN
options {
  paraphrase = "=";
}
	:	'='
	;

NOT
options {
  paraphrase = "!";
}
	:	'!'
	;

LT_
options {
  paraphrase = "<";
}
	:	'<'
	;

LSHIFT
options {
  paraphrase = "<<";
}
	: "<<"
	;

GT
options {
  paraphrase = ">";
}
	:	'>'
	;

RSHIFT
options {
  paraphrase = ">>";
}
	: ">>"
	;

DIV
options {
  paraphrase = "/";
}
	:	'/'
	;

PLUS
options {
  paraphrase = "+";
}
	:	'+'
	;

MINUS
options {
  paraphrase = "-";
}
	:	'-'
	;

TILDE
options {
  paraphrase = "~";
}
	:	'~'
	;

STAR
options {
  paraphrase = "*";
}
	:	'*'
	;

MOD
options {
  paraphrase = "%";
}
	:	'%'
	;


SCOPEOP
options {
  paraphrase = "::";
}
	:  	"::"
	;

QUOTE
options {
	paraphrase = "double quotes";
}
	: '"'
	;

WS_
options {
  paraphrase = "white space";
}
	:	(' '
	|	'\t'
	|	'\n'  { newline(); }
	|	'\r')
		{ $setType(Token.SKIP); }
	;


PREPROC_DIRECTIVE
options {
  paraphrase = "a preprocessor directive";
}

	:
	'#'
	(~'\n')* '\n'
	{ newline(); $setType(Token.SKIP); }
	;


SL_COMMENT
options {
  paraphrase = "a comment";
}

	:
	"//"
	(~'\n')* '\n'
	{ $setType(Token.SKIP); newline(); }
	;

ML_COMMENT
options {
  paraphrase = "a comment";
}
	:
	"/*"
	(
			STRING_LITERAL
		|	CHAR_LITERAL
		|	'\n' { newline(); }
		|	'*' ~'/'
		|	~'*'
	)*
	"*/"
	{ $setType(Token.SKIP);  }
	;

CHAR_LITERAL
options {
  paraphrase = "a character literal";
}
	:
	'\''
	( ESC | ~'\'' )
	'\''
	;

STRING_LITERAL
options {
  paraphrase = "a string literal";
}
	:
	'"'!
	(ESC|~'"')*
	'"'!
	;


protected
ESC
options {
  paraphrase = "an escape sequence";
}
	:	'\\'
		(	'n'
		|	't'
		|	'v'
		|	'b'
		|	'r'
		|	'f'
		|	'a'
		|	'\\'
		|	'?'
		|	'\''
		|	'"'
		|	('0' | '1' | '2' | '3')
			(
				/* Since a digit can occur in a string literal,
				 * which can follow an ESC reference, ANTLR
				 * does not know if you want to match the digit
				 * here (greedy) or in string literal.
				 * The same applies for the next two decisions
				 * with the warnWhenFollowAmbig option.
				 */
				options {
					warnWhenFollowAmbig = false;
				}
			:	OCTDIGIT
				(
					options {
						warnWhenFollowAmbig = false;
					}
				:	OCTDIGIT
				)?
			)?
		|   'x' HEXDIGIT
			(
				options {
					warnWhenFollowAmbig = false;
				}
			:	HEXDIGIT
			)?
		)
	;

protected
VOCAB
options {
  paraphrase = "an escaped character value";
}
	:	'\3'..'\377'
	;

protected
DIGIT
options {
  paraphrase = "a digit";
}
	:	'0'..'9'
	;

protected
OCTDIGIT
options {
  paraphrase = "an octal digit";
}
	:	'0'..'7'
	;

protected
HEXDIGIT
options {
  paraphrase = "a hexadecimal digit";
}
	:	('0'..'9' | 'a'..'f' | 'A'..'F')
	;


/* octal literals are detected by checkOctal */

HEX
options {
  paraphrase = "a hexadecimal value value";
}

	:    ("0x" | "0X") (HEXDIGIT)+
	;

INT
options {
  paraphrase = "an integer value";
}
	: (DIGIT)+                  // base-10
	;

FLOAT
options {
  paraphrase = "an floating point value";
}
	: '.' (DIGIT)+ (('e' | 'E') ('+' | '-')? (DIGIT)+)?
	;

IDENT
options {
  testLiterals = true;
  paraphrase = "an identifer";
}
	: ('a'..'z'|'A'..'Z'|'_') ('a'..'z'|'A'..'Z'|'_'|'0'..'9')*
	;
