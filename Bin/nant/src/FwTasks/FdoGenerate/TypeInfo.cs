// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2006' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: TypeInfo.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;

namespace SIL.FieldWorks.FDO.FdoGenerate
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class TypeInfo
	{
		/// <summary></summary>
		public string ColumnType;
		/// <summary></summary>
		public string GetSetMethod;
		/// <summary></summary>
		public string CSharpType;
		/// <summary></summary>
		public string CSharpTypeAlt;
		/// <summary></summary>
		public string Hungarian;
		/// <summary></summary>
		public string RetrievalType;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:TypeInfo"/> class.
		/// </summary>
		/// <param name="columnType">Type of the column.</param>
		/// <param name="getsetMeth">The getset meth.</param>
		/// <param name="cSharpType">Type of the c sharp.</param>
		/// <param name="hung">The hung.</param>
		/// ------------------------------------------------------------------------------------
		public TypeInfo(string columnType, string getsetMeth, string cSharpType, string hung)
			: this(columnType, string.Empty, getsetMeth, cSharpType, string.Empty, hung)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:TypeInfo"/> class.
		/// </summary>
		/// <param name="columnType">Type of the column.</param>
		/// <param name="retrievalType">Type of the retrieval.</param>
		/// <param name="getsetMeth">The getset meth.</param>
		/// <param name="cSharpType">Type of the c sharp.</param>
		/// <param name="cSharpTypeAlt">Type of the c sharp.</param>
		/// <param name="hung">The hung.</param>
		/// ------------------------------------------------------------------------------------
		public TypeInfo(string columnType, string retrievalType, string getsetMeth,
			string cSharpType, string cSharpTypeAlt, string hung)
		{
			ColumnType = columnType;
			RetrievalType = retrievalType;
			GetSetMethod = getsetMeth;
			CSharpType = cSharpType;
			CSharpTypeAlt = cSharpTypeAlt;
			Hungarian = hung;
		}

		private static Dictionary<string, TypeInfo> s_TypeInfos;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the type infos.
		/// </summary>
		/// <value>The type infos.</value>
		/// ------------------------------------------------------------------------------------
		public static Dictionary<string, TypeInfo> TypeInfos
		{
			get
			{
				if (s_TypeInfos == null)
				{
					s_TypeInfos = new Dictionary<string, TypeInfo>();
					s_TypeInfos.Add("Integer", new TypeInfo("koctInt", "IntProperty", "int", "i"));
					// Review: are all binaries formats?
					// we have from the idh:  // koctTtp: Column is binary data representing a TsTextProps
					// s_TypeInfos.Add("Binary", new TypeInfo("koctFmt", "FormatProperty",
					// "StringFormat", "fmt"));
					// Review: what cSharpType? StStyle:Rules is a varbinary[8000]
					s_TypeInfos.Add("TextPropBinary", new TypeInfo("koctTtp", "Unknown",
						"ITsTextProps", "bin"));
					s_TypeInfos.Add("Binary", new TypeInfo("koctBinary", "BinaryProperty", "byte[]", "bin"));
					// Review: is this right, are bools treated in the db as ints?
					s_TypeInfos.Add("Boolean", new TypeInfo("koctInt", "BoolProperty", "bool", "b"));
					s_TypeInfos.Add("Guid", new TypeInfo("koctGuid", "GuidProperty", "Guid", "guid"));
					s_TypeInfos.Add("String", new TypeInfo("koctString", "UNUSED", "TsStringAccessor",
						"tss"));
					s_TypeInfos.Add("Unicode", new TypeInfo("koctUnicode", "UnicodeProperty", "string",
						"s"));
					// hungarian ?
					s_TypeInfos.Add("Time", new TypeInfo("koctTime", "TimeProperty", "DateTime", "dt"));
					// Review: columnType? hung ? how should we rep in C#?
					s_TypeInfos.Add("GenDate", new TypeInfo("koctInt64", "GenDateProperty", "int", "gd"));
					s_TypeInfos.Add("Image", new TypeInfo("???", "special", "???", "???",
						string.Empty, "image"));
					s_TypeInfos.Add("MultiString", new TypeInfo(string.Empty, "special", "UNUSED",
						"MultiStringAccessor", "ITsString", "ms"));
					s_TypeInfos.Add("MultiUnicode", new TypeInfo(string.Empty, "special", "UNUSED",
						"MultiUnicodeAccessor", "string", "mu"));
					// owning and ref vectors treated the same for now
					s_TypeInfos.Add("vector", new TypeInfo("koctObjVec", "VectorProperty", "int[]", "ivec"));
					// REVIEW(John): should these really be ints, and thus separate from hvoAtomicOwning,
					// which I know need GetObjProperty ?
					// basic rels get treated as with this signature instead of whatever class they point to
					// TODO JohnH(RandyR): If it is an HVO, then it needs to use GetObjProperty.
					// Note that I changed the method to a bogus one, to see if this was being used,
					// and it looks like it is not. Perhaps you should remove it.
					s_TypeInfos.Add("hvo", new TypeInfo("koctInt", "ReallyBadIntProperty", "int", "hvo"));
					// basic rels get treated as with this signature instead of whatever class they
					// point to
					s_TypeInfos.Add("hvoAtomicOwning", new TypeInfo("koctObjOwn", "ObjProperty",
						"int", "hvo"));
					s_TypeInfos.Add("hvoAtomicReference", new TypeInfo("koctObj", "ObjProperty",
						"int", "hvo"));
					// TODO:    Float    Image    Int64    Numeric
				}

				return s_TypeInfos;
			}
		}
	}

}
