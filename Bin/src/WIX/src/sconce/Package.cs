//-------------------------------------------------------------------------------------------------
// <copyright file="Package.cs" company="Microsoft">
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
// Core implementation for a generic Visual Studio package.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure
{
	using System;
	using System.Collections;
	using System.ComponentModel.Design;
	using System.Diagnostics;
	using System.Globalization;
	using System.Runtime.InteropServices;
	using System.Threading;
	using Microsoft.VisualStudio.OLE.Interop;
	using Microsoft.VisualStudio.Shell.Interop;
	using Microsoft.Win32;

	using IServiceProvider = System.IServiceProvider;
	using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
	using ResId = ResourceId;

	/// <summary>
	/// Implements and/or provides all of the required interfaces and services to allow a
	/// generic project to be integrated into the Visual Studio environment.
	/// </summary>
	[Guid("DB56A6EE-F553-41D4-AE35-38D7D490B230")]
	public class Package :
		IDisposable,
		IVsPackage,
		IVsInstalledProduct,
		IOleServiceProvider,
		IOleCommandTarget,
		IServiceContainer,
		IServiceProvider
	{
		#region Member Variables
		//==========================================================================================
		// Member Variables
		//==========================================================================================

		private static readonly Type classType = typeof(Package);

		private static Package instance;
		private static Guid packageTypeGuid = Guid.Empty;
		private static Version productVersion;

		private bool closed;
		private PackageContext context;
		private ProjectFactory projectFactory;
		private uint projectCookie;
		private Hashtable services;
		#endregion

		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		/// <summary>
		/// Initializes a new instance of the <see cref="Package"/> class.
		/// </summary>
		public Package()
		{
			Tracer.WriteLineIf(classType, Tracer.ConstructorMethodName, Tracer.Level.Warning, Instance != null, "There is more than one Package instance. There should only be one.");
			instance = this;
		}

		/// <summary>
		/// Finalizer for the <see cref="Package"/> class.
		/// </summary>
		~Package()
		{
			Tracer.Fail("Dispose should have been called explicitly.");
			this.Dispose(false);
		}
		#endregion

		#region Properties
		//==========================================================================================
		// Properties
		//==========================================================================================

		/// <summary>
		/// Gets the one and only one instance of this package.
		/// </summary>
		public static Package Instance
		{
			get { return instance; }
		}

		/// <summary>
		/// Gets the resource identifier for the package icon shown in the Visual Studio About box.
		/// </summary>
		public virtual int AboutBoxIconResourceId
		{
			get { return (int)ResId.IDI_PACKAGE; }
		}

		/// <summary>
		/// Gets a flag indicating whether the package has been closed by Visual Studio.
		/// </summary>
		public bool Closed
		{
			get { return this.closed; }
		}

		/// <summary>
		/// Gets the context associated with this package.
		/// </summary>
		public PackageContext Context
		{
			get { return this.context; }
		}

		/// <summary>
		/// Gets the official name for the product that is shown in the Visual Studio About box.
		/// </summary>
		public virtual string OfficialName
		{
			get { return this.Context.NativeResources.GetString(ResId.IDS_OFFICIALNAME); }
		}

		/// <summary>
		/// Gets the GUID of the package that is registered with Visual Studio.
		/// </summary>
		public Guid PackageTypeGuid
		{
			get
			{
				if (packageTypeGuid == Guid.Empty)
				{
					// Read the GuidAttribute from this object, but don't look for
					// inherited attributes. Each package should have a distinct
					// GUID that is registered with Visual Studio.
					object[] attributes = this.GetType().GetCustomAttributes(typeof(GuidAttribute), false);
					if (attributes == null || attributes.Length == 0)
					{
						Tracer.Fail("{0} needs to define a GuidAttribute on the class.", this.GetType().FullName);
					}
					else
					{
						packageTypeGuid = new Guid(((GuidAttribute)attributes[0]).Value);
					}
				}
				return packageTypeGuid;
			}
		}

		/// <summary>
		/// Gets the product details string that appears in the Visual Studio About box.
		/// </summary>
		public virtual string ProductDetails
		{
			get { return this.Context.NativeResources.GetString(ResId.IDS_PRODUCTDETAILS); }
		}

		/// <summary>
		/// Gets the product ID string that is shown in the Visual Studio About box.
		/// </summary>
		public virtual string ProductId
		{
			get { return this.Context.NativeResources.GetString(ResId.IDS_PID); }
		}

		/// <summary>
		/// Gets the version of the product.
		/// </summary>
		public virtual Version ProductVersion
		{
			get
			{
				if (productVersion == null)
				{
					productVersion = Instance.GetType().Assembly.GetName().Version;
				}
				return productVersion;
			}
		}

		/// <summary>
		/// Gets the GUID for the project type that should be registered with Visual Studio.
		/// </summary>
		public virtual Guid ProjectTypeGuid
		{
			get { return typeof(Project).GUID; }
		}

		/// <summary>
		/// Gets the resource identifier of the bitmap to use for the Visual Studio splash screen.
		/// </summary>
		public virtual int SplashBitmapResourceId
		{
			get { return (int)ResId.IDB_SPLASH_IMAGE; }
		}
		#endregion

		#region Methods
		//==========================================================================================
		// Methods
		//==========================================================================================

		/// <summary>
		/// Disposes of managed and native resources.
		/// </summary>
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Gets the service object of the specified type.
		/// </summary>
		/// <param name="serviceType">An object that specifies the type of service object to get.</param>
		/// <returns>
		/// <para>A service object of type <paramref name="serviceType"/>.</para>
		/// <para>-or-</para>
		/// <para><see langword="null"/> if there is no service object of type
		/// <paramref name="serviceType"/>.</para>
		/// </returns>
		public object GetService(Type serviceType)
		{
			Tracer.VerifyNonNullArgument(serviceType, "serviceType");
			Tracer.WriteLineVerbose(classType, "GetService", "ServiceType = {0}", serviceType.FullName);

			// Check for the special services that we provide.
			if (serviceType == typeof(IServiceContainer) || serviceType == Instance.GetType())
			{
				return this;
			}

			object value = null;

			// Check our proferred services list to see if we can return this service.
			if (this.services != null)
			{
				value = this.services[serviceType];
				if (value is ProfferedService)
				{
					value = ((ProfferedService)value).Instance;
				}

				// If we have a callback then we have to attempt to create the service
				// by calling the callback method.
				if (value is ServiceCreatorCallback)
				{
					// In case the callback method tries to recursively call back into
					// us, we'll just null out the entry for this service now. That
					// way this method will just fail instead of allowing for a stack
					// overflow exception.
					this.services[serviceType] = null;

					// Let the callback create the service.
					Tracer.WriteLineVerbose(classType, "GetService", "Creating the {0} service via a callback.", serviceType.FullName);
					value = ((ServiceCreatorCallback)value)(this, serviceType);

					// Verify that the callback gave us a valid service.
					if (value != null && !value.GetType().IsCOMObject && !serviceType.IsAssignableFrom(value.GetType()))
					{
						// Well, the callback was naughty and didn't give us what we wanted, but
						// let's not punish our caller by raising an exception. We'll just fail
						// by nulling the value.
						Tracer.Fail("The service creator callback returned the object '{0}' that doesn't implement the requested type '{1}'", value.GetType().FullName, serviceType.FullName);
						value = null;
					}

					// Set the created service in our proferred service list.
					this.services[serviceType] = value;
				}
			}

			// Let our parent provider handle the rest of the types.
			if (value == null && this.Context.ServiceProvider != null)
			{
				value = this.Context.ServiceProvider.GetService(serviceType);
			}

			return value;
		}

		#region IVsPackage Members
		/// <summary>
		/// Initializes the VSPackage with a back pointer to the environment. This is the entry point
		/// for the Visual Studio package.
		/// </summary>
		/// <param name="sp">
		/// Pointer to the <see cref="IOleServiceProvider"/> interface through which the
		/// VSPackage can query for services.
		/// </param>
		/// <returns>An HRESULT indicating the result of the call.</returns>
		int IVsPackage.SetSite(IOleServiceProvider sp)
		{
			if (this.Closed)
			{
				Tracer.Fail("We shouldn't have been called if we're being unloaded.");
				return NativeMethods.E_UNEXPECTED;
			}

			try
			{
				if (sp != null)
				{
					// If SetSite has been called more than once, it's an error.
					if (this.Context != null)
					{
						string message = this.Context.NativeResources.GetString(ResId.IDS_E_SITEALREADYSET, this.GetType().FullName);
						Tracer.Fail(message);
						throw new InvalidOperationException(message);
					}

					// Initialize the ServiceProvider and ourself.
					ServiceProvider contextServiceProvider = new ServiceProvider(sp);
					this.context = this.CreatePackageContext(contextServiceProvider);
					Tracer.Initialize(this.context);
					this.Initialize();
				}
				else if (this.Context != null && this.Context.ServiceProvider != null)
				{
					this.Dispose(true);
				}
			}
			catch (Exception e)
			{
				Tracer.Fail("Unexpected exception: {0}\n{1}", e.Message, e);
				throw;
			}

			return NativeMethods.S_OK;
		}

		int IVsPackage.QueryClose(out int pfCanClose)
		{
			pfCanClose = 1;
			return NativeMethods.S_OK;
		}

		int IVsPackage.Close()
		{
			// We only want to call Dispose once, so we use this flag to indicate
			// that we're closing.
			if (!this.Closed)
			{
				this.Dispose();
			}
			this.closed = true;

			// Write the summary section to the trace log.
			Tracer.WriteSummary();
			return NativeMethods.S_OK;
		}

		int IVsPackage.GetAutomationObject(string pszPropName, out object ppDisp)
		{
			ppDisp = null;
			if (this.Closed)
			{
				return NativeMethods.E_UNEXPECTED;
			}
			return NativeMethods.S_OK;
		}

		int IVsPackage.CreateTool(ref Guid rguidPersistenceSlot)
		{
			if (this.Closed)
			{
				return NativeMethods.E_UNEXPECTED;
			}
			return NativeMethods.S_OK;
		}

		int IVsPackage.ResetDefaults(uint grfFlags)
		{
			if (this.Closed)
			{
				return NativeMethods.E_UNEXPECTED;
			}
			return NativeMethods.S_OK;
		}

		int IVsPackage.GetPropertyPage(ref Guid rguidPage, VSPROPSHEETPAGE[] ppage)
		{
			if (this.Closed)
			{
				return NativeMethods.E_UNEXPECTED;
			}
			return NativeMethods.S_OK;
		}
		#endregion

		#region IVsInstalledProduct Members
		int IVsInstalledProduct.IdBmpSplash(out uint pIdBmp)
		{
			pIdBmp = unchecked((uint)this.SplashBitmapResourceId);
			return NativeMethods.S_OK;
		}

		int IVsInstalledProduct.ProductDetails(out string pbstrProductDetails)
		{
			pbstrProductDetails = this.ProductDetails;
			return NativeMethods.S_OK;
		}

		int IVsInstalledProduct.IdIcoLogoForAboutbox(out uint pIdIco)
		{
			pIdIco = unchecked((uint)this.AboutBoxIconResourceId);
			return NativeMethods.S_OK;
		}

		int IVsInstalledProduct.ProductID(out string pbstrPID)
		{
			pbstrPID = this.ProductId;
			return NativeMethods.S_OK;
		}

		int IVsInstalledProduct.OfficialName(out string pbstrName)
		{
			pbstrName = this.OfficialName;
			return NativeMethods.S_OK;
		}
		#endregion

		#region IOleCommandTarget Members
		/// <summary>
		/// Executes a specified command or displays help for a command.
		/// </summary>
		/// <param name="guidGroup">
		/// Unique identifier of the command group; can be <see langword="null"/> to specify
		/// the standard group.
		/// </param>
		/// <param name="nCmdId">
		/// The command to be executed. This command must belong to the group specified with
		/// <paramref name="guidGroup"/>.
		/// </param>
		/// <param name="nCmdExecOpt">
		/// Values taken from the <b>OLECMDEXECOPT</b> enumeration, which describe how the
		/// object should execute the command.
		/// </param>
		/// <param name="vIn">
		/// Pointer to a <b>VARIANTARG</b> structure containing input arguments. Can be <see langword="null"/>.
		/// </param>
		/// <param name="vOut">
		/// Pointer to a <b>VARIANTARG</b> structure to receive command output. Can be <see langword="null"/>.
		/// </param>
		/// <returns>
		///     This method supports the standard return values E_FAIL and E_UNEXPECTED, as well
		///     as the following:
		///     S_OK
		///         The command was executed successfully.
		///     OLECMDERR_E_UNKNOWNGROUP
		///         <paramref name="guidGroup"/> is not <see langword="null"/> but does not
		///         specify a recognized command group.
		///     OLECMDERR_E_NOTSUPPORTED
		///         <paramref name="nCmdID"/> is not a valid command in the group identified by
		///         <paramref name="guidGroup"/>.
		///     OLECMDERR_E_DISABLED
		///         The command identified by <paramref name="nCmdID"/> is currently disabled
		///         and cannot be executed.
		///     OLECMDERR_E_NOHELP
		///         The caller has asked for help on the command identified by
		///         <paramref name="nCmdID"/>, but no help is available.
		///     OLECMDERR_E_CANCELED
		///         The user canceled the execution of the command.
		/// </returns>
		int IOleCommandTarget.Exec(ref Guid guidGroup, uint nCmdId, uint nCmdExecOpt, IntPtr vIn, IntPtr vOut)
		{
			return NativeMethods.OLECMDERR_E_NOTSUPPORTED;
		}

		/// <summary>
		/// Queries the object for the status of one or more commands generated by user interface events.
		/// </summary>
		/// <param name="guidGroup">
		/// Unique identifier of the command group; can be <see langword="null"/> to specify
		/// the standard group. All the commands that are passed in the <paramref name="oleCmd"/>
		/// array must belong to the group specified by <paramref name="guidGroup"/>.
		/// </param>
		/// <param name="cCmds">The number of commands in the <paramref name="oleCmd"/> array.</param>
		/// <param name="oleCmd">
		/// A caller-allocated array of <see cref="OLECMD"/> structures that indicate the
		/// commands for which the caller needs status information. This method fills the
		/// <c>cmdf</c> member of each structure with values taken from the <b>OLECMDF</b>
		/// enumeration.
		/// </param>
		/// <param name="oleText">
		/// Pointer to an <b>OLECMDTEXT</b> structure in which to return name and/or status
		/// information of a single command. Can be <see langword="null"/> to indicate that
		/// the caller does not need this information.
		/// </param>
		/// <returns>
		///     <para>This method supports the standard return values E_FAIL and E_UNEXPECTED,
		///     as well as the following:</para>
		///     S_OK
		///         The command status as any optional strings were returned successfully.
		///     E_POINTER
		///         <paramref name="oleCmd"/> is <see langword="null"/>.
		///     OLECMDERR_E_UNKNOWNGROUP
		///         <paramref name="guidGroup"/> is not <see langword="null"/> but does not
		///         specify a recognized command group.
		/// </returns>
		int IOleCommandTarget.QueryStatus(ref Guid guidGroup, uint cCmds, OLECMD[] oleCmd, IntPtr oleText)
		{
			if (oleCmd == null)
			{
				throw new ArgumentNullException("oleCmd");
			}
			return NativeMethods.E_UNEXPECTED;
		}
		#endregion

		#region IOleServiceProvider Members
		/// <summary>
		/// Acts as the factory method for any services exposed through an implementation of
		/// <see cref="IOleServiceProvider"/>.
		/// </summary>
		/// <param name="sid">Unique identifier of the service (an SID).</param>
		/// <param name="iid">Unique identifier of the interface the caller wishes to receive for the service.</param>
		/// <param name="ppvObj">
		/// Address of the caller-allocated variable to receive the interface pointer of the
		/// service on successful return from this function. The caller becomes responsible
		/// for calling <b>Release</b> through this interface pointer when the service is no
		/// longer needed.
		/// </param>
		/// <returns>
		///     Returns one of the following values:
		///     <list type="table">
		///     <item>
		///         <term>S_OK</term>
		///         <description>The service was successfully created or retrieved. The caller is
		///         responsible for calling ((IUnknown *)*ppv)->Release();.</description>
		///     </item>
		///     <item>
		///         <term>E_OUTOFMEMORY</term>
		///         <description>There is insufficient memory to create the service.</description>
		///     </item>
		///     <item>
		///         <term>E_UNEXPECTED</term>
		///         <description>An unknown error occurred.</description>
		///     </item>
		///     <item>
		///         <term>E_NOINTERFACE</term>
		///         <description>The service exists, but the interface requested does not exist
		///         on that service.</description>
		///     </item>
		///     </list>
		/// </returns>
		int IOleServiceProvider.QueryService(ref Guid sid, ref Guid iid, out IntPtr ppvObj)
		{
			Tracer.WriteLineVerbose(classType, "IOleServiceProvider.QueryService", "Querying for service {0}", sid.ToString("B"));
			ppvObj = IntPtr.Zero;
			int hr = NativeMethods.S_OK;
			object service = null;

			// See if we can find the service in our proffered services list.
			if (this.services != null)
			{
				foreach (Type serviceType in this.services.Keys)
				{
					if (serviceType.GUID == sid)
					{
						service = GetService(serviceType);
						break;
					}
				}
			}

			if (service == null)
			{
				Tracer.WriteLineVerbose(classType, "IOleServiceProvider.QueryService", "Could not find service {0}", sid.ToString("B"));
				return NativeMethods.E_NOINTERFACE;
			}

			// If the caller requested an IID other than IUnknown, then we have to
			// query for that interface. Otherwise, we can just return the IUnknown.
			if (iid == NativeMethods.IID_IUnknown)
			{
				ppvObj = Marshal.GetIUnknownForObject(service);
			}
			else
			{
				IntPtr pUnk = Marshal.GetIUnknownForObject(service);
				hr = Marshal.QueryInterface(pUnk, ref iid, out ppvObj);
				Marshal.Release(pUnk);
			}

			return hr;
		}
		#endregion

		#region IServiceContainer Members
		/// <summary>
		/// Adds the specified service to the service container.
		/// </summary>
		/// <param name="serviceType">The type of service to add.</param>
		/// <param name="serviceInstance">
		/// An instance of the service type to add. This object must implement or inherit from
		/// the type indicated by the <paramref name="serviceType"/> parameter.
		/// </param>
		void IServiceContainer.AddService(Type serviceType, object serviceInstance)
		{
			((IServiceContainer)this).AddService(serviceType, serviceInstance, false);
		}

		/// <summary>
		/// Adds the specified service to the service container, and optionally promotes the
		/// service to parent service containers.
		/// </summary>
		/// <param name="serviceType">The type of service to add.</param>
		/// <param name="serviceInstance">
		/// An instance of the service type to add. This object must implement or inherit from
		/// the type indicated by the <paramref name="serviceType"/> parameter.
		/// </param>
		/// <param name="promote">
		/// <see langword="true"/> to promote this request to any parent service containers;
		/// otherwise, <see langword="false"/>.
		/// </param>
		void IServiceContainer.AddService(Type serviceType, object serviceInstance, bool promote)
		{
			Tracer.VerifyNonNullArgument(serviceType, "serviceType");
			Tracer.VerifyNonNullArgument(serviceInstance, "serviceInstance");

			this.AddServiceHelper(serviceType, serviceInstance, promote);
		}

		/// <summary>
		/// Adds the specified service to the service container.
		/// </summary>
		/// <param name="serviceType">The type of service to add.</param>
		/// <param name="callback">
		/// A callback object that is used to create the service. This allows a service to be
		/// declared as available, but delays the creation of the object until the service is
		/// requested.
		/// </param>
		void IServiceContainer.AddService(Type serviceType, ServiceCreatorCallback callback)
		{
			((IServiceContainer)this).AddService(serviceType, callback, false);
		}

		/// <summary>
		/// Adds the specified service to the service container, and optionally promotes the
		/// service to parent service containers.
		/// </summary>
		/// <param name="serviceType">The type of service to add.</param>
		/// <param name="callback">
		/// A callback object that is used to create the service. This allows a service to be
		/// declared as available, but delays the creation of the object until the service is
		/// requested.
		/// </param>
		/// <param name="promote">
		/// <see langword="true"/> to promote this request to any parent service containers;
		/// otherwise, <see langword="false"/>.
		/// </param>
		void IServiceContainer.AddService(Type serviceType, ServiceCreatorCallback callback, bool promote)
		{
			Tracer.VerifyNonNullArgument(serviceType, "serviceType");
			Tracer.VerifyNonNullArgument(callback, "callback");

			this.AddServiceHelper(serviceType, callback, promote);
		}

		/// <summary>
		/// Removes the specified service type from the service container.
		/// </summary>
		/// <param name="serviceType">The type of service to remove.</param>
		void IServiceContainer.RemoveService(Type serviceType)
		{
			((IServiceContainer)this).RemoveService(serviceType, false);
		}

		/// <summary>
		/// Removes the specified service type from the service container, and optionally
		/// promotes the service to parent service containers.
		/// </summary>
		/// <param name="serviceType">The type of service to remove.</param>
		/// <param name="promote">
		/// <see langword="true"/> to promote this request to any parent service containers;
		/// otherwise, <see langword="false"/>.
		/// </param>
		void IServiceContainer.RemoveService(Type serviceType, bool promote)
		{
			Tracer.VerifyNonNullArgument(serviceType, "serviceType");

			if (this.services != null)
			{
				object value = this.services[serviceType];
				if (value != null)
				{
					this.services.Remove(serviceType);

					// If we registered this service with VS, then we need to revoke it.
					if (value is ProfferedService)
					{
						ProfferedService service = (ProfferedService)value;
						if (service.Cookie != 0)
						{
							IProfferService ps = (IProfferService)GetService(typeof(IProfferService));
							if (ps != null)
							{
								int hr = ps.RevokeService(service.Cookie);
								Tracer.Assert(NativeMethods.Succeeded(hr), "Failed to unregister service {0}.", service.GetType().FullName);
							}
							service.Cookie = 0;
						}
						value = service.Instance;
					}

					if (value is IDisposable)
					{
						((IDisposable)value).Dispose();
					}
				}
			}
		}
		#endregion

		#region IServiceProvider Members
		/// <summary>
		/// Gets the service object of the specified type.
		/// </summary>
		/// <param name="serviceType">An object that specifies the type of service object to get.</param>
		/// <returns>
		/// <para>A service object of type <paramref name="serviceType"/>.</para>
		/// <para>-or-</para>
		/// <para><see langword="null"/> if there is no service object of type
		/// <paramref name="serviceType"/>.</para>
		/// </returns>
		object IServiceProvider.GetService(Type serviceType)
		{
			return this.GetService(serviceType);
		}
		#endregion

		/// <summary>
		/// Provides a way for subclasses to create a new type-specific <see cref="PackageContext"/> object.
		/// </summary>
		/// <param name="serviceProvider">The <see cref="ServiceProvider"/> instance to use for getting services from the environment.</param>
		/// <returns>A new <see cref="PackageContext"/> object.</returns>
		protected virtual PackageContext CreatePackageContext(ServiceProvider serviceProvider)
		{
			return new PackageContext(serviceProvider);
		}

		/// <summary>
		/// Provides a way for subclasses to create a new type-specific project factory.
		/// </summary>
		/// <returns>A new <see cref="ProjectFactory"/> object.</returns>
		protected virtual ProjectFactory CreateProjectFactory()
		{
			return new ProjectFactory(this);
		}

		/// <summary>
		/// Cleans up managed and native resources.
		/// </summary>
		/// <param name="disposing">Indicates whether this is being called from the finalizer or from <see cref="Dispose()"/>.</param>
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				// Unregister our project types.
				if (this.projectCookie != 0)
				{
					IVsRegisterProjectTypes regProjTypes = (IVsRegisterProjectTypes)this.GetService(typeof(IVsRegisterProjectTypes));
					if (regProjTypes != null)
					{
						int hr = regProjTypes.UnregisterProjectType(this.projectCookie);
						this.projectCookie = 0;
						Tracer.Assert(NativeMethods.Succeeded(hr), "Cannot unregister the project type {0}.", this.projectCookie);
					}
				}

				// Revoke all proffered services that we contain.
				if (this.services != null)
				{
					IProfferService ps = (IProfferService)this.GetService(typeof(IProfferService));
					Hashtable services = this.services;
					this.services = null;

					foreach (object service in this.services.Values)
					{
						if (service is ProfferedService)
						{
							ProfferedService proffered = (ProfferedService)service;
							if (proffered.Cookie != 0 && ps != null)
							{
								// Unregister the proffered service from the system.
								int hr = ps.RevokeService(proffered.Cookie);
								Tracer.Assert(NativeMethods.Succeeded(hr), "Failed to unregister service {0}.", service.GetType().FullName);
							}
						}

						// Dispose the service if possible.
						if (service is IDisposable)
						{
							((IDisposable)service).Dispose();
						}
					}
				}

				if (this.context != null)
				{
					this.context.Dispose();
					this.context = null;
				}
			}
		}

		/// <summary>
		/// Adds the specified service to the service container, and optionally promotes the
		/// service to parent service containers.
		/// </summary>
		/// <param name="serviceType">The type of service to add.</param>
		/// <param name="serviceInstanceOrCallback">
		/// <para>An instance of the service type to add. This object must implement or inherit
		/// from the type indicated by the <paramref name="serviceType"/> parameter.</para>
		/// <para>- or -</para>
		/// <para>A callback object that is used to create the service. This allows a service
		/// to be declared as available, but delays the creation of the object until the
		/// service is requested.</para>
		/// </param>
		/// <param name="promote">
		/// <see langword="true"/> to promote this request to any parent service containers;
		/// otherwise, <see langword="false"/>.
		/// </param>
		private void AddServiceHelper(Type serviceType, object serviceInstanceOrCallback, bool promote)
		{
			Tracer.Assert(serviceType != null && serviceInstanceOrCallback != null, "Shouldn't have null parameters.");

			// Create the services table if necessary.
			if (this.services == null)
			{
				this.services = new Hashtable();
			}

			bool isCallback = (serviceInstanceOrCallback is ServiceCreatorCallback);
			Type serviceInstanceType = serviceInstanceOrCallback.GetType();
			if (!isCallback && !serviceInstanceType.IsCOMObject && !serviceType.IsAssignableFrom(serviceInstanceType))
			{
				string message = this.Context.NativeResources.GetString(ResId.IDS_E_INVALIDSERVICEINSTANCE, serviceType.FullName);
				Tracer.Fail(message);
				throw new ArgumentException(message);
			}

			// Disallow the addition of duplicate services.
			if (this.services.ContainsKey(serviceType))
			{
				string message = this.Context.NativeResources.GetString(ResId.IDS_E_DUPLICATESERVICE, serviceType.FullName);
				Tracer.Fail(message);
				throw new InvalidOperationException(message);
			}

			if (promote)
			{
				// If we're promoting, we need to store this guy in a promoted service
				// object because we need to manage additional state.  We attempt
				// to proffer at this time if we have a service provider.  If we don't,
				// we will proffer when we get one.
				ProfferedService service = new ProfferedService();
				service.Instance = serviceInstanceOrCallback;
				if (isCallback)
				{
					this.services[serviceType] = service;
				}

				if (this.Context.ServiceProvider != null)
				{
					IProfferService ps = (IProfferService)GetService(typeof(IProfferService));
					if (ps != null)
					{
						uint cookie;
						Guid serviceGuid = serviceType.GUID;
						int hr = ps.ProfferService(ref serviceGuid, this, out cookie);
						service.Cookie = cookie;
						if (NativeMethods.Failed(hr))
						{
							string message = this.Context.NativeResources.GetString(ResId.IDS_E_FAILEDTOPROFFERSERVICE, serviceType.FullName);
							Tracer.Fail(message);
							throw new COMException(message, hr);
						}
					}
				}
			}

			if (!isCallback || (isCallback && !promote))
			{
				this.services[serviceType] = serviceInstanceOrCallback;
			}
		}

		/// <summary>
		/// Initializes this package.
		/// </summary>
		private void Initialize()
		{
			int hr = NativeMethods.S_OK;

			// If we have any services to proffer, let's do it now.
			if (this.services != null)
			{
				IProfferService ps = (IProfferService)this.GetService(typeof(IProfferService));
				Tracer.Assert(ps != null, "We have services to proffer, but can't get an instance of IProfferService.");
				if (ps != null)
				{
					foreach (DictionaryEntry entry in this.services)
					{
						ProfferedService service = entry.Value as ProfferedService;
						if (service != null)
						{
							Type serviceType = (Type)entry.Key;
							Guid serviceGuid = serviceType.GUID;
							uint cookie;
							hr = ps.ProfferService(ref serviceGuid, this, out cookie);
							service.Cookie = cookie;
							if (NativeMethods.Failed(hr))
							{
								string message = this.Context.NativeResources.GetString(ResId.IDS_E_FAILEDTOPROFFERSERVICE, serviceType.FullName);
								Tracer.Fail(message);
								throw new COMException(message, hr);
							}
						}
					}
				}
			}

			// Create the Project Factory and register our project types.
			Tracer.WriteLineInformation(classType, "Initialize", "Creating the project factory and registering our project types.");
			IVsRegisterProjectTypes regProjTypes = (IVsRegisterProjectTypes)this.Context.ServiceProvider.GetServiceOrThrow(typeof(SVsRegisterProjectTypes), typeof(IVsRegisterProjectTypes), classType, "Initialize");
			this.projectFactory = this.CreateProjectFactory();
			Guid projectGuid = this.ProjectTypeGuid;
			hr = regProjTypes.RegisterProjectType(ref projectGuid, this.projectFactory, out this.projectCookie);
			if (NativeMethods.Succeeded(hr))
			{
				Tracer.WriteLine(classType, "Initialize", Tracer.Level.Information, "Successfully registered our project types.");
			}
			else
			{
				Tracer.Fail("Failed to register the Wix Project type. HRESULT = 0x{0}", hr.ToString("x"));
			}
		}
		#endregion

		#region Classes
		//==========================================================================================
		// Classes
		//==========================================================================================

		/// <summary>
		/// Contains a service that is being promoted to Visual Studio.
		/// </summary>
		private sealed class ProfferedService
		{
			public object Instance;
			public uint Cookie;
		}
		#endregion
	}
}