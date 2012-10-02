//-------------------------------------------------------------------------------------------------
// <copyright file="ServiceProvider.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
//
//    The use and distribution terms for this software are covered by the
//    Common Public License 1.0 (http://opensource.org/licenses/cpl.php)
//    which can be found in the file CPL.TXT at the root of this distribution.
//    By using this software in any fashion, you are agreeing to be bound by
//    the terms of this license.
//
//    You must not remove this notice, or any other, from this software.
// </copyright>
//
// <summary>
// Wraps the IServiceProvider, IOleServiceProvider, and IObjectWithSite functionality in one class.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure
{
	using System;
	using System.Runtime.InteropServices;
	using Microsoft.VisualStudio.OLE.Interop;
	using Microsoft.VisualStudio.Shell.Interop;

	using IServiceProvider = System.IServiceProvider;
	using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

	using ResId = ResourceId;

	/// <summary>
	/// Allows querying for services by type and by GUID. Uses an OLE service provider
	/// (<see cref="IOleServiceProvider"/>) to implement the <see cref="System.IServiceProvider"/> interface.
	/// </summary>
	public sealed class ServiceProvider : IDisposable, IServiceProvider, IObjectWithSite
	{
		#region Member Variables
		//==========================================================================================
		// Member Variables
		//==========================================================================================

		private static readonly Type classType = typeof(ServiceProvider);

		private IOleServiceProvider oleServiceProvider;
		#endregion

		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		/// <summary>
		/// Initializes a new instance of the <see cref="ServiceProvider"/> class.
		/// </summary>
		public ServiceProvider(IOleServiceProvider oleServiceProvider)
		{
			Tracer.VerifyNonNullArgument(oleServiceProvider, "oleServiceProvider");
			this.oleServiceProvider = oleServiceProvider;
		}
		#endregion

		#region Properties
		//==========================================================================================
		// Properties
		//==========================================================================================

		public IOleServiceProvider OleServiceProvider
		{
			get { return this.oleServiceProvider; }
		}
		#endregion

		#region Methods
		//==========================================================================================
		// Methods
		//==========================================================================================

		public void Dispose()
		{
			if (this.oleServiceProvider != null)
			{
				this.oleServiceProvider = null;
			}
		}

		/// <summary>
		/// Gets the service object of the specified GUID.
		/// </summary>
		/// <param name="guid">A GUID that specifies the type of service object to get.</param>
		/// <returns>
		/// A service object of the type specified by <paramref name="guid"/>, or
		/// <see langword="null"/> if there is no service object with the corresponding GUID.
		/// </returns>
		public object GetService(Guid guid)
		{
			// If we've already been disposed, disallow any service requests.
			if (this.oleServiceProvider == null)
			{
				return null;
			}

			if (guid == Guid.Empty)
			{
				return null;
			}

			// We provide a couple of services inherently.
			if (guid == NativeMethods.IID_IServiceProvider || guid == NativeMethods.IID_IObjectWithSite)
			{
				return this;
			}
			if (guid == NativeMethods.IID_IOleServiceProvider)
			{
				return this.oleServiceProvider;
			}

			// Let the OLE service provider have a chance at fulfilling the request.
			Guid guidIUnknown = NativeMethods.IID_IUnknown;
			IntPtr unknownPointer = IntPtr.Zero;
			object service = null;

			try
			{
				int hr = this.oleServiceProvider.QueryService(ref guid, ref guidIUnknown, out unknownPointer);
				if (NativeMethods.Failed(hr) || unknownPointer == IntPtr.Zero)
				{
					return null;
				}

				// At this point we know that the OLE service provider successfully fulfilled the
				// request, so we have to marshal the pointer to an IUnknown object and release
				// a reference because QueryService already added a ref.
				service = Marshal.GetObjectForIUnknown(unknownPointer);
			}
			finally
			{
				if (unknownPointer != IntPtr.Zero)
				{
					Marshal.Release(unknownPointer);
				}
			}

			return service;
		}

		/// <summary>
		/// Gets the service object of the specified type.
		/// </summary>
		/// <param name="serviceType">An object that specifies the type of service object to get.</param>
		/// <returns>
		/// A service object of type <paramref name="serviceType"/>, or <see langword="null"/>
		/// if there is no service object of type <paramref name="serviceType"/>.
		/// </returns>
		public object GetService(Type serviceType)
		{
			Tracer.VerifyNonNullArgument(serviceType, "serviceType");
			Tracer.WriteLineVerbose(classType, "GetService", "ServiceType = {0}", serviceType.FullName);
			object service = this.GetService(serviceType.GUID);
			return service;
		}

		/// <summary>
		/// Gets the service object of the specified type or throws an <see cref="InvalidOperationException"/>
		/// informing the user that a critical service is missing and to try to repair the Visual Studio installation.
		/// </summary>
		/// <param name="serviceType">An object that specifies the type of service object to get.</param>
		/// <param name="interfaceType">The interface to get from <paramref name="serviceType"/>.</param>
		/// <param name="classType">The type of the class containing the method.</param>
		/// <param name="methodName">The name of the method that is being entered.</param>
		/// <returns>
		/// A service object of type <paramref name="serviceType"/>, or <see langword="null"/>
		/// if there is no service object of type <paramref name="serviceType"/>.
		/// </returns>
		public object GetServiceOrThrow(Type serviceType, Type interfaceType, Type classType, string methodName)
		{
			object service = this.GetService(serviceType);
			if (service == null)
			{
				this.ThrowFailedServiceException(classType, methodName, serviceType);
			}

			Guid interfaceGuid = interfaceType.GUID;
			IntPtr punknown = IntPtr.Zero;
			IntPtr pinterface = IntPtr.Zero;

			try
			{
				punknown = Marshal.GetIUnknownForObject(service);
				Marshal.QueryInterface(punknown, ref interfaceGuid, out pinterface);

				if (pinterface == IntPtr.Zero)
				{
					this.ThrowFailedServiceException(classType, methodName, interfaceType);
				}
			}
			finally
			{
				if (pinterface != IntPtr.Zero)
				{
					Marshal.Release(pinterface);
				}

				if (punknown != IntPtr.Zero)
				{
					Marshal.Release(punknown);
				}
			}

			return service;
		}

		/// <summary>
		/// Gets an <see cref="IVsShell"/> interface pointer from the environment and throws if it can't get it.
		/// </summary>
		/// <param name="classType">The type of the class containing the method.</param>
		/// <param name="methodName">The name of the method that is being entered.</param>
		public IVsShell GetVsShell(Type classType, string methodName)
		{
			return (IVsShell)this.GetServiceOrThrow(typeof(SVsShell), typeof(IVsShell), classType, methodName);
		}

		/// <summary>
		/// Gets an <see cref="IVsSolution"/> interface pointer from the environment and throws if it can't get it.
		/// </summary>
		/// <param name="classType">The type of the class containing the method.</param>
		/// <param name="methodName">The name of the method that is being entered.</param>
		public IVsSolution GetVsSolution(Type classType, string methodName)
		{
			return (IVsSolution)this.GetServiceOrThrow(typeof(SVsSolution), typeof(IVsSolution), classType, methodName);
		}

		/// <summary>
		/// Gets an <see cref="IVsUIShell"/> interface pointer from the environment and throws if it can't get it.
		/// </summary>
		/// <param name="classType">The type of the class containing the method.</param>
		/// <param name="methodName">The name of the method that is being entered.</param>
		public IVsUIShell GetVsUIShell(Type classType, string methodName)
		{
			return (IVsUIShell)this.GetServiceOrThrow(typeof(SVsUIShell), typeof(IVsUIShell), classType, methodName);
		}

		/// <summary>
		/// Throws an <see cref="InvalidOperationException"/> informing the user that a critical service is missing and to
		/// try to repair the Visual Studio installation.
		/// </summary>
		/// <param name="classType">The type of the class containing the method.</param>
		/// <param name="methodName">The name of the method that is being entered.</param>
		/// <param name="serviceType">An object that specifies the type of service object that was requested but failed.</param>
		/// <remarks>Also logs a failure message in the trace log and asserts in debug mode.</remarks>
		public void ThrowFailedServiceException(Type classType, string methodName, Type serviceType)
		{
			string serviceTypeName = serviceType.Name;
			Tracer.Fail("Cannot get an instance of {0}", serviceTypeName);
			throw new InvalidOperationException(SconceStrings.ErrorMissingService(serviceTypeName));
		}

		/// <summary>
		/// Retrieves the last site set with <see cref="IObjectWithSite.SetSite"/>.
		/// If there's no known site, the method throws a COM failure code.
		/// </summary>
		/// <param name="riid">The IID of the interface pointer that should be returned.</param>
		/// <param name="ppvSite">
		/// Address of pointer variable that receives the interface pointer requested in
		/// <paramref name="interfaceId"/>. The return value contains the requested
		/// interface pointer to the site last seen in <see cref="IObjectWithSite.SetSite"/>.
		/// The specific interface returned depends on the <paramref name="interfaceId"/>
		/// argument — in essence, the two arguments act identically to those in
		/// <b>IUnknown::QueryInterface</b>. If the appropriate interface pointer is
		/// available, the object must call <b>IUnknown::AddRef</b> on that pointer before
		/// returning successfully. If no site is available, or the requested interface is
		/// not supported, this method must return <see langword="null"/>.
		/// </param>
		void IObjectWithSite.GetSite(ref Guid riid, out IntPtr ppvSite)
		{
			object site = this.GetService(riid);
			if (site == null)
			{
				// There is a site but it does not support the interface requested by interfaceId.
				Marshal.ThrowExceptionForHR(NativeMethods.E_NOINTERFACE);
			}

			// We have to convert from our object to a pointer to the interface for COM.
			IntPtr pUnknown = Marshal.GetIUnknownForObject(site);
			int hr = Marshal.QueryInterface(pUnknown, ref riid, out ppvSite);
			Marshal.Release(pUnknown);
			NativeMethods.ThrowOnFailure(hr);
		}

		/// <summary>
		/// Provides the site's <b>IUnknown</b> pointer to the object. The object should
		/// hold onto this pointer, calling <b>IUnknown::AddRef</b> in doing so. If the
		/// object already has a site, it should call that existing site's <b>IUnknown::Release</b>,
		/// save the new site pointer, and call the new site's <b>IUnknown::AddRef</b>.
		/// </summary>
		/// <param name="pUnkSite">
		/// Pointer to the <b>IUnknown</b> interface pointer of the site managing this object.
		/// If <see langword="null"/>, the object should call <b>IUnknown::Release</b> on any
		/// existing site at which point the object no longer knows its site.
		/// </param>
		void IObjectWithSite.SetSite(object pUnkSite)
		{
			if (pUnkSite is IOleServiceProvider)
			{
				this.oleServiceProvider = (IOleServiceProvider)pUnkSite;
			}
		}
		#endregion
	}
}
