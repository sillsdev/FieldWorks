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
// File: DummyClass.cs
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
	/// Used for non-Cellar classes
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyClass: IClass
	{
		#region IClass Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the abbreviation.
		/// </summary>
		/// <value>The abbreviation.</value>
		/// ------------------------------------------------------------------------------------
		public string Abbreviation
		{
			get { return string.Empty; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the base class.
		/// </summary>
		/// <value>The base class.</value>
		/// ------------------------------------------------------------------------------------
		public Class BaseClass
		{
			get { return null; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the base class.
		/// </summary>
		/// <value>The name of the base class.</value>
		/// ------------------------------------------------------------------------------------
		public string BaseClassName
		{
			get { return string.Empty; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the depth.
		/// </summary>
		/// <value>The depth.</value>
		/// ------------------------------------------------------------------------------------
		public int Depth
		{
			get { return 0; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this instance is abstract.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is abstract; otherwise, <c>false</c>.
		/// </value>
		/// ------------------------------------------------------------------------------------
		public bool IsAbstract
		{
			get { return false; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this instance is hand generated.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is hand generated; otherwise, <c>false</c>.
		/// </value>
		/// ------------------------------------------------------------------------------------
		public bool IsHandGenerated
		{
			get { return false; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the is ownerless.
		/// </summary>
		/// <value>The is ownerless.</value>
		/// ------------------------------------------------------------------------------------
		public string IsOwnerless
		{
			get { return "true"; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the number.
		/// </summary>
		/// <value>The number.</value>
		/// ------------------------------------------------------------------------------------
		public int Number
		{
			get { return 0; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the properties.
		/// </summary>
		/// <value>The properties.</value>
		/// ------------------------------------------------------------------------------------
		public StringKeyCollection<Property> Properties
		{
			get { return new StringKeyCollection<Property>(); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the sub classes.
		/// </summary>
		/// <value>The sub classes.</value>
		/// ------------------------------------------------------------------------------------
		public StringKeyCollection<Class> SubClasses
		{
			get { return new StringKeyCollection<Class>(); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the relative qualified signature.
		/// </summary>
		/// <param name="desiredModule">The desired module.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public string GetRelativeQualifiedSignature(CellarModule desiredModule)
		{
			return string.Empty;
		}

		#endregion
	}
}
