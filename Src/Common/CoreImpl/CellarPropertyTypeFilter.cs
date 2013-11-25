// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: CellarPropertyTypeFilter.cs
// Responsibility: TE Team

using System;

namespace SIL.CoreImpl
{
	/// <summary>
	/// Types of fields that can be stored in a meta-data cache. These values are 1 shifted
	/// to the left the number of bits equal to the corresponding CellarPropertyType value.
	/// </summary>
	[Flags]
	public enum CellarPropertyTypeFilter : int
	{
		/// <summary> </summary>
		OwningAtomic = 1 << CellarPropertyType.OwningAtomic, // 8388608
		/// <summary> </summary>
		ReferenceAtomic = 1 << CellarPropertyType.ReferenceAtomic, // 16777216
		/// <summary> </summary>
		OwningCollection = 1 << CellarPropertyType.OwningCollection, //33554432
		/// <summary> </summary>
		ReferenceCollection = 1 << CellarPropertyType.ReferenceCollection, //67108864
		/// <summary> </summary>
		OwningSequence = 1 << CellarPropertyType.OwningSequence, //134217728
		/// <summary> </summary>
		ReferenceSequence = 1 << CellarPropertyType.ReferenceSequence, //268435456

		/// <summary> </summary>
		AllOwning = OwningAtomic | OwningCollection | OwningSequence, //176160768
		/// <summary> </summary>
		AllReference = ReferenceAtomic | ReferenceCollection | ReferenceSequence, //352321536
		/// <summary> </summary>
		All = AllOwning | AllReference, // 528482304

		/// <summary> </summary>
		MultiString = 1 << CellarPropertyType.MultiString,
		/// <summary> </summary>
		MultiUnicode = 1 << CellarPropertyType.MultiUnicode,

		/// <summary>All atomic types</summary>
		AllAtomic = OwningAtomic | ReferenceAtomic,
		/// <summary>All collection and sequence types</summary>
		AllVector = OwningCollection | OwningSequence | ReferenceCollection | ReferenceSequence,

		/// <summary> </summary>
		String = 1 << CellarPropertyType.String,
		/// <summary> </summary>
		Unicode = 1 << CellarPropertyType.Unicode,

		/// <summary> All multilingual string types</summary>
		AllMulti = MultiString | MultiUnicode,
		/// <summary> All non-multilingual string types</summary>
		AllSimpleString = String | Unicode,
		/// <summary> All string types, plain and multilingual</summary>
		AllString = AllMulti | AllSimpleString,

		/// <summary></summary>
		Boolean = 1 << CellarPropertyType.Boolean,
		/// <summary></summary>
		GenDate = 1 << CellarPropertyType.GenDate,
		/// <summary></summary>
		Guid = 1 << CellarPropertyType.Guid,
		/// <summary>Float is not used in the model, as of 23 March 2013.</summary>
		Float = 1 << CellarPropertyType.Float,
		/// <summary>Numeric is not used in the model, as of 23 March 2013.</summary>
		Numeric = 1 << CellarPropertyType.Numeric,
		/// <summary></summary>
		Integer = 1 << CellarPropertyType.Integer,
		/// <summary></summary>
		Time = 1 << CellarPropertyType.Time,
		/// <summary></summary>
		AllBasic = Boolean | GenDate | Guid | Float | Numeric | Integer | Time,

		///// <summary>special virtual bits</summary>
		//kcptVirtualBit = 0xe0,
		/// <summary>virtual bit mask to strip virtual bit</summary>
		VirtualMask = 0x1f,
	}
}