// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: CellarModuleAttribute.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;

namespace SIL.FieldWorks.FDO
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Attribute that is used to register Cellar modules in FDO and tell FDO in which assembly
	/// the module is implemented.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[AttributeUsage(AttributeTargets.Assembly, AllowMultiple=true)]
	public class CellarModuleAttribute: Attribute
	{
		private string m_ModuleName;
		private string m_Location;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:CellarModuleAttribute"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public CellarModuleAttribute()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:CellarModuleAttribute"/> class.
		/// </summary>
		/// <param name="moduleName">The name of the cellar module.</param>
		/// <param name="location">The location.</param>
		/// ------------------------------------------------------------------------------------
		public CellarModuleAttribute(string moduleName, string location)
		{
			ModuleName = moduleName;
			Location = location;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the name.
		/// </summary>
		/// <value>The name.</value>
		/// ------------------------------------------------------------------------------------
		public string ModuleName
		{
			get { return m_ModuleName; }
			set { m_ModuleName = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the location.
		/// </summary>
		/// <value>The location.</value>
		/// ------------------------------------------------------------------------------------
		public string Location
		{
			get { return m_Location; }
			set { m_Location = value; }
		}
	}
}
