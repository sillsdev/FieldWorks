// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using Microsoft.Practices.ServiceLocation;

namespace SIL.Utils
{
	/// <summary>
	/// Create an IServiceLocator.
	/// </summary>
	/// <remarks>
	/// This should be implemented by a class that interacts with one of the many IoC systems,
	/// which implements the IServiceLocator interface (newly defined and managed at):
	/// http://www.codeplex.com/CommonServiceLocator
	///
	/// In this case, it will allow for swapping between IoC systems (i.e., a Strategy) without
	/// affecting an application. That is, an application will just use the IServiceLocator
	/// methods to get at its services.
	/// </remarks>
	public interface IServiceLocatorBootstrapper
	{
		/// <summary>
		/// Create an IServiceLocator instance.
		/// </summary>
		/// <returns>An IServiceLocator instance.</returns>
		IServiceLocator CreateServiceLocator();
	}
}