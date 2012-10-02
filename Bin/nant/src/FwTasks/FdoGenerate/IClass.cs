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
// File: IClass.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
namespace SIL.FieldWorks.FDO.FdoGenerate
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IClass
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the abbreviation.
		/// </summary>
		/// <value>The abbreviation.</value>
		/// ------------------------------------------------------------------------------------
		string Abbreviation { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the base class.
		/// </summary>
		/// <value>The base class.</value>
		/// ------------------------------------------------------------------------------------
		Class BaseClass { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the base class.
		/// </summary>
		/// <value>The name of the base class.</value>
		/// ------------------------------------------------------------------------------------
		string BaseClassName { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the depth.
		/// </summary>
		/// <value>The depth.</value>
		/// ------------------------------------------------------------------------------------
		int Depth { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this instance is abstract.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is abstract; otherwise, <c>false</c>.
		/// </value>
		/// ------------------------------------------------------------------------------------
		bool IsAbstract { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the is ownerless.
		/// </summary>
		/// <value>The is ownerless.</value>
		/// ------------------------------------------------------------------------------------
		string IsOwnerless { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the number.
		/// </summary>
		/// <value>The number.</value>
		/// ------------------------------------------------------------------------------------
		int Number { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the properties.
		/// </summary>
		/// <value>The properties.</value>
		/// ------------------------------------------------------------------------------------
		StringKeyCollection<Property> Properties { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the sub classes.
		/// </summary>
		/// <value>The sub classes.</value>
		/// ------------------------------------------------------------------------------------
		StringKeyCollection<Class> SubClasses { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the relative qualified signature.
		/// </summary>
		/// <param name="desiredModule">The desired module.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		string GetRelativeQualifiedSignature(CellarModule desiredModule);
	}
}
