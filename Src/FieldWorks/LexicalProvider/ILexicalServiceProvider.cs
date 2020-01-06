// Copyright (c) 2011-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ServiceModel;

namespace SIL.FieldWorks.LexicalProvider
{
	/// <summary>
	/// Provides a service contract for getting a lexical provider from an application.
	/// WARNING: Paratext contains its own identical definition of these interfaces.
	/// Any change must be coordinated (both in corresponding source files and in terms
	/// of product release schedules.
	/// </summary>
	[ServiceContract]
	public interface ILexicalServiceProvider
	{
		/// <summary>
		/// Gets the location for the provider for the specified project and
		/// provider type. If the providerType is not supported, return null for the Uri.
		/// </summary>
		[OperationContract]
		Uri GetProviderLocation(string projhandle, string providerType);

		/// <summary>
		/// Gets the version of the specified provider that the server supports. If the
		/// providerType is not supported, return 0 for the version.
		/// </summary>
		[OperationContract]
		int GetSupportedVersion(string providerType);

		/// <summary>
		/// Unlike a normal ping method that gets a response, we just use this ping method
		/// to determine if the service provider is actually valid since no exception is
		/// thrown until a method is called.
		/// </summary>
		[OperationContract]
		void Ping();
	}
}