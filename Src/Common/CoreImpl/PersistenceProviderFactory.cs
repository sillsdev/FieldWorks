// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.CoreImpl.Impls;
using SIL.Utils;

namespace SIL.CoreImpl
{
	/// <summary>
	/// Factory that creates an instance of IPersistenceProvider
	/// </summary>
	public static class PersistenceProviderFactory
	{
		/// <summary>
		/// Create an instance of IPersistenceProvider
		/// </summary>
		/// <param name="propertyTable">The property table to use for persistence.</param>
		/// <param name="context">The persistence context</param>
		/// <returns></returns>
		public static IPersistenceProvider CreatePersistenceProvider(IPropertyTable propertyTable, string context)
		{
			return new PersistenceProvider(propertyTable, context);
		}

		/// <summary>
		/// Create an instance of IPersistenceProvider
		/// </summary>
		/// <param name="propertyTable">The property table to use for persistence.</param>
		/// <returns></returns>
		public static IPersistenceProvider CreatePersistenceProvider(IPropertyTable propertyTable)
		{
			return CreatePersistenceProvider(propertyTable, "Default");
		}
	}
}