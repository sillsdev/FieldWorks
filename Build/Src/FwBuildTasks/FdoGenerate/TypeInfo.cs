// Copyright (c) 2006-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using System.Collections.Generic;

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
		public string CSharpType;
		/// <summary></summary>
		public string CSharpTypeAlt;
		/// <summary></summary>
		public string Hungarian;
		/// <summary></summary>
		public string RetrievalType;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="TypeInfo"/> class.
		/// </summary>
		/// <param name="cSharpType">Type of the c sharp.</param>
		/// <param name="hung">The hung.</param>
		/// ------------------------------------------------------------------------------------
		public TypeInfo(string cSharpType, string hung)
			: this(string.Empty, cSharpType, string.Empty, hung)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="TypeInfo"/> class.
		/// </summary>
		/// <param name="retrievalType">Type of the retrieval.</param>
		/// <param name="cSharpType">Type of the c sharp.</param>
		/// <param name="cSharpTypeAlt">Type of the c sharp.</param>
		/// <param name="hung">The hung.</param>
		/// ------------------------------------------------------------------------------------
		public TypeInfo(string retrievalType, string cSharpType,
			string cSharpTypeAlt, string hung)
		{
			RetrievalType = retrievalType;
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
					s_TypeInfos = new Dictionary<string, TypeInfo>
									{
										{"Integer", new TypeInfo("int", "i")},
										{"TextPropBinary", new TypeInfo("ITsTextProps", "bin")},
										{"Binary", new TypeInfo("byte[]", "bin")},
										{"Boolean", new TypeInfo("bool", "b")},
										{"Guid", new TypeInfo("Guid", "guid")},
										{"String", new TypeInfo("TsStringAccessor", "tss")},
										{"Unicode", new TypeInfo("string", "s")},
										{"Time", new TypeInfo("DateTime", "dt")},
										{"GenDate", new TypeInfo("GenDate", "gd")},
										{"Image", new TypeInfo("???", "???", string.Empty, "image")},
										{"MultiString", new TypeInfo("UNUSED",
																		"MultiStringAccessor", "ITsString", "ms")
										},
										{"MultiUnicode", new TypeInfo("UNUSED",
																		 "MultiUnicodeAccessor", "string", "mu")
										},
										{"vector", new TypeInfo("int[]", "ivec")},
										{"hvoAtomicOwning", new TypeInfo("int", "hvo")},
										{"hvoAtomicReference", new TypeInfo("int", "hvo")}
									};
					// owning and ref vectors treated the same for now
					// basic rels get treated as with this signature instead of whatever class they
					// point to
					// TODO:    Float    Int64    Numeric
				}

				return s_TypeInfos;
			}
		}
	}

}
