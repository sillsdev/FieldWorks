// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2008, SIL International. All Rights Reserved.
// <copyright from='2008' to='2008' company='SIL International'>
//		Copyright (c) 2008, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: UserFunctions.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;

using NAnt.Core;
using NAnt.Core.Types;
using NAnt.Core.Attributes;
using System.Security.Principal;

namespace SIL.FieldWorks.Build.Tasks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[FunctionSet("user", "User")]
	public class UserFunctions: FunctionSetBase
	{
		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="UserFunctions"/> class.
		/// </summary>
		/// <param name="project">The project.</param>
		/// <param name="properties">The properties.</param>
		/// ------------------------------------------------------------------------------------
		public UserFunctions(Project project, PropertyDictionary properties)
			: base(project, properties)
		{
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the current user is an administrator.
		/// </summary>
		/// <returns><c>true</c> if current user has administrator privileges, otherwise
		/// <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		[Function("is-admin")]
		public bool IsAdmin()
		{
			WindowsIdentity id = WindowsIdentity.GetCurrent();
			WindowsPrincipal p = new WindowsPrincipal(id);
			return p.IsInRole(WindowsBuiltInRole.Administrator);
		}
	}
}
