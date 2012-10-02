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
// File: CmResource.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.FDO.Cellar
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// CmResource manages version numbers for resources in the project that are loaded from
	/// XML files.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class CmResource
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a resource matching the specified name from the Owning object from a collection
		/// of resources specified by the field id.
		/// </summary>
		/// <param name="cache">database</param>
		/// <param name="hvoOwner">id of the owning object</param>
		/// <param name="flid">field id in the owning object</param>
		/// <param name="name">name of the specified resource</param>
		/// <returns>resource with the specified name</returns>
		/// ------------------------------------------------------------------------------------
		public static CmResource GetResource(FdoCache cache, int hvoOwner, int flid, string name)
		{
			FdoOwningCollection<ICmResource> resources =
				new FdoOwningCollection<ICmResource>(cache, hvoOwner, flid);
			foreach (CmResource resource in resources)
			{
				if (resource.Name.Equals(name))
					return resource;
			}
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets a resource matching the specified name from the Owning object from a collection
		/// of resources specified by the field id.
		/// </summary>
		/// <param name="cache">database</param>
		/// <param name="hvoOwner">id of the owning object</param>
		/// <param name="flid">field id in the owning object</param>
		/// <param name="name">name of the specified resource</param>
		/// <param name="newVersion">new version number for the resource</param>
		/// ------------------------------------------------------------------------------------
		public static void SetResource(FdoCache cache, int hvoOwner, int flid, string name,
			Guid newVersion)
		{
			CmResource resource = GetResource(cache, hvoOwner, flid, name);
			if (resource == null)
			{
				// Resource does not exist yet. Add it to the collection.
				FdoOwningCollection<ICmResource> resources =
					new FdoOwningCollection<ICmResource>(cache, hvoOwner, flid);
				CmResource newResource = new CmResource();
				resources.Add(newResource);
				newResource.Name = name;
				newResource.Version = newVersion;
				return;
			}

			resource.Version = newVersion;
		}
	}
}
