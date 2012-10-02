// --------------------------------------------------------------------------------------------
// <copyright from='2003' to='2003' company='SIL International'>
//    Copyright (c) 2003, SIL International. All Rights Reserved.
// </copyright>
//
// File: WordLevel.cs
// Responsibility: RandyR
// Last reviewed:
//
// <remarks>
// Implementation of WordRecord.
// </remarks>
//
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace SIL.WordWorks.GAFAWS
{
	/// ---------------------------------------------------------------------------------------
	/// <summary>
	/// A word record.
	/// </summary>
	/// ---------------------------------------------------------------------------------------
	public sealed class WordRecord
	{
		/// <summary>
		/// Collection of prefixes.
		/// </summary>
		[XmlArrayItemAttribute(IsNullable=false)]
		public List<Affix> Prefixes;

		/// <summary>
		/// The stem.
		/// </summary>
		public Stem Stem;

		/// <summary>
		/// Collection of suffixes.
		/// </summary>
		[XmlArrayItemAttribute(IsNullable=false)]
		public List<Affix> Suffixes;

		/// <summary>
		/// Model-specific data.
		/// </summary>
		public Other Other;

		/// <summary>
		/// Word record ID.
		/// </summary>
		[XmlAttributeAttribute(DataType="ID")]
		public string WRID;

		/// <summary>
		/// Constructor.
		/// </summary>
		public WordRecord()
		{
			//Prefixes = new List<Affix>();
			//Stem = new Stem();
			//Suffixes = new List<Affix>();
		}
	}
}