// Copyright (c) 2009-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: GetServicesFromFWClass.cs
// Responsibility: GordonM
//
// <remarks>
// Used by both Application.Impl.DomainDataByFlid and by DomainServices.CopyObject (so far).
// </remarks>

using System;
using System.Collections.Generic;			// for Dictionary
using System.Reflection;					// for GetExecutingAssembly()
using SIL.FieldWorks.FDO.Infrastructure;	// for IFwMetaDataCacheManaged

namespace SIL.FieldWorks.FDO.DomainServices
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Gives the CSharp interface type for a service of the specified class.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal static class GetServicesFromFWClass
	{
		/// <summary></summary>
		private static readonly Dictionary<int, Type> s_classIdToFactType = new Dictionary<int, Type>();
		/// <summary></summary>
		private static readonly Dictionary<int, Type> s_classIdToRepoType = new Dictionary<int, Type>();

		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// Gives the CSharp interface type for the factory that creates objects of the specified class.
		/// </summary>
		/// ----------------------------------------------------------------------------------------
		internal static Type GetFactoryTypeFromFWClassID(IFwMetaDataCacheManaged mdc, int classId)
		{
			// Abstract classes have no factory.
			if (mdc.GetAbstract(classId))
				throw new ArgumentException("No factory for abstract classes", "classId");

			// if the class ID is cached then return the type now.
			Type result;
			if (s_classIdToFactType.TryGetValue(classId, out result))
				return result;

			// Find the class name of this object
			var sClassName = mdc.GetClassName(classId);
			// find the Type for the factory for this class.
			var fullTypeName = string.Format("SIL.FieldWorks.FDO.I{0}Factory", sClassName);
			result = Assembly.GetExecutingAssembly().GetType(fullTypeName, true);
			s_classIdToFactType.Add(classId, result); // Store for next time.
			return result;
		}

		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// Gives the CSharp interface type for the repository that creates objects of the specified class.
		/// </summary>
		/// ----------------------------------------------------------------------------------------
		internal static Type GetRepositoryTypeFromFWClassID(IFwMetaDataCacheManaged mdc, int classId)
		{
			// if the class ID is cached then return the type now.
			Type result;
			if (s_classIdToRepoType.TryGetValue(classId, out result))
				return result;

			// Find the class name of this object
			var sClassName = mdc.GetClassName(classId);
			// find the Type for the repository for this class.
			var fullTypeName = string.Format("SIL.FieldWorks.FDO.I{0}Repository", sClassName);
			result = Assembly.GetExecutingAssembly().GetType(fullTypeName, true);
			s_classIdToRepoType.Add(classId, result); // Store for next time.
			return result;
		}

	}
}