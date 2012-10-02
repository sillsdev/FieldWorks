// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: CellarPropertyType.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.CoreImpl
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Types of properties in "Cellar" database. The underlying values still come from
	/// CmTypes.h, but that file can be retired if/when these values are no longer needed in
	/// C++ code.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public enum CellarPropertyType : int
	{
		/// <summary>
		///
		/// </summary>
		Nil = CellarModuleDefns.kcptNil,
		/// <summary>
		///
		/// </summary>
		Boolean = CellarModuleDefns.kcptBoolean,
		/// <summary>
		///
		/// </summary>
		Integer = CellarModuleDefns.kcptInteger,
		/// <summary>
		///
		/// </summary>
		Numeric = CellarModuleDefns.kcptNumeric,
		/// <summary>
		///
		/// </summary>
		Float = CellarModuleDefns.kcptFloat,
		/// <summary>
		///
		/// </summary>
		Time = CellarModuleDefns.kcptTime,
		/// <summary>
		///
		/// </summary>
		Guid = CellarModuleDefns.kcptGuid,
		/// <summary>
		///
		/// </summary>
		Image = CellarModuleDefns.kcptImage,
		/// <summary>
		///
		/// </summary>
		GenDate = CellarModuleDefns.kcptGenDate,
		/// <summary>
		///
		/// </summary>
		Binary = CellarModuleDefns.kcptBinary,

		/// <summary>
		///
		/// </summary>
		String = CellarModuleDefns.kcptString,
		/// <summary>
		///
		/// </summary>
		MultiString = CellarModuleDefns.kcptMultiString,
		/// <summary>
		///
		/// </summary>
		Unicode = CellarModuleDefns.kcptUnicode,
		/// <summary>
		///
		/// </summary>
		MultiUnicode = CellarModuleDefns.kcptMultiUnicode,
		/// <summary>
		///
		/// </summary>
		BigString = CellarModuleDefns.kcptBigString,
		/// <summary>
		///
		/// </summary>
		MultiBigString = CellarModuleDefns.kcptMultiBigString,
		/// <summary>
		///
		/// </summary>
		BigUnicode = CellarModuleDefns.kcptBigUnicode,
		/// <summary>
		///
		/// </summary>
		MultiBigUnicode = CellarModuleDefns.kcptMultiBigUnicode,

		/// <summary>
		///
		/// </summary>
		OwningAtomic = CellarModuleDefns.kcptOwningAtom, // 23
		/// <summary>
		///
		/// </summary>
		ReferenceAtomic = CellarModuleDefns.kcptReferenceAtom, // 24
		/// <summary>
		///
		/// </summary>
		OwningCollection = CellarModuleDefns.kcptOwningCollection, // 25
		/// <summary>
		///
		/// </summary>
		ReferenceCollection = CellarModuleDefns.kcptReferenceCollection, // 26
		/// <summary>
		///
		/// </summary>
		OwningSequence = CellarModuleDefns.kcptOwningSequence, // 27
		/// <summary>
		///
		/// </summary>
		ReferenceSequence = CellarModuleDefns.kcptReferenceSequence, // 28

		/// <summary>The first object type (non-basic)</summary>
		MinObj = 23,
	}

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
		/// <summary> </summary>
		MultiBigString = 1 << CellarPropertyType.MultiBigString,
		/// <summary> </summary>
		MultiBigUnicode = 1 << CellarPropertyType.MultiBigUnicode,

		/// <summary>All atomic types</summary>
		AllAtomic = OwningAtomic | ReferenceAtomic,
		/// <summary>All collection and sequence types</summary>
		AllVector = OwningCollection | OwningSequence | ReferenceCollection | ReferenceSequence,

		/// <summary> </summary>
		String = 1 << CellarPropertyType.String,
		/// <summary> </summary>
		Unicode = 1 << CellarPropertyType.Unicode,
		/// <summary> </summary>
		BigString = 1 << CellarPropertyType.BigString,
		/// <summary> </summary>
		BigUnicode = 1 << CellarPropertyType.BigUnicode,

		/// <summary> All multilingual string types</summary>
		AllMulti = MultiString | MultiUnicode | MultiBigString | MultiBigUnicode,
		/// <summary> All non-multilingual string types</summary>
		AllSimpleString = String | Unicode | BigString | BigUnicode,
		/// <summary> All string types, plain and multilingual</summary>
		AllString = AllMulti | AllSimpleString,

		/// <summary></summary>
		Integer = 1 << CellarPropertyType.Integer,
		/// <summary></summary>
		GenDate = 1 << CellarPropertyType.GenDate,

		///// <summary>special virtual bits</summary>
		//kcptVirtualBit = 0xe0,
		/// <summary>virtual bit mask to strip virtual bit</summary>
		VirtualMask = 0x1f,
	}
}