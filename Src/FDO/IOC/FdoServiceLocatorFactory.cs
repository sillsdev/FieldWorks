// --------------------------------------------------------------------------------------------
// Copyright (C) 2009 SIL International. All rights reserved.
//
// Distributable under the terms of either the Common Public License or the
// GNU Lesser General Public License, as specified in the LICENSING.txt file.
//
// File: ServiceLocatorFactory.cs
// Responsibility: Randy Regnier
// Last reviewed: never
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using Microsoft.Practices.ServiceLocation;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.FDO.Application.Impl;
using SIL.FieldWorks.FDO.DomainImpl;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.FDO.Infrastructure.Impl;
using SIL.FieldWorks.FDO.DomainServices.DataMigration;
using StructureMap;
using StructureMap.Configuration.DSL;
using StructureMap.Pipeline;
using StructureMap.ServiceLocatorAdapter;

namespace SIL.FieldWorks.FDO.IOC
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Factory for hard-wired FDO Common Service Locator.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal sealed partial class FdoServiceLocatorFactory : IServiceLocatorBootstrapper
	{
		private readonly FDOBackendProviderType m_backendProviderType;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="backendProviderType">Type of backend provider to create.</param>
		/// ------------------------------------------------------------------------------------
		internal FdoServiceLocatorFactory(FDOBackendProviderType backendProviderType)
		{
			m_backendProviderType = backendProviderType;
		}

		#region Implementation of IServiceLocatorBootstrapper

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create an IServiceLocator instance.
		/// </summary>
		/// <returns>An IServiceLocator instance.</returns>
		/// ------------------------------------------------------------------------------------
		public IServiceLocator CreateServiceLocator()
		{
			// NOTE: When creating an object through IServiceLocator.GetInstance the caller has
			// to call Dispose() on the newly created object, unless it's a singleton
			// (registered with LifecycleIs(new SingletonLifecycle())) in which case
			// the Registry will dispose the object.
			var registry = new Registry();

			// NB: Default is:
			// .CacheBy(InstanceScope.PerRequest);

			// Add data migration manager. (new one per request)
			registry
				.For<IDataMigrationManager>()
				.Use<FdoDataMigrationManager>();

			// Add FdoCache
			registry
				.For<HomographConfiguration>()
				.LifecycleIs(new SingletonLifecycle())
				.Use<HomographConfiguration>();

			// Add FdoCache
			registry
				.For<FdoCache>()
				.LifecycleIs(new SingletonLifecycle())
				.Use<FdoCache>();
			// Add IParagraphCounterRepository
			registry
				.For<IParagraphCounterRepository>()
				.LifecycleIs(new SingletonLifecycle())
				.Use<ParagraphCounterRepository>();

			// Add IFilteredScrBookRepository
			registry
				.For<IFilteredScrBookRepository>()
				.LifecycleIs(new SingletonLifecycle())
				.Use<FilteredScrBookRepository>();

			// Add ITsStrFactory
			registry
				.For<ITsStrFactory>()
				.LifecycleIs(new SingletonLifecycle())
				.Use(c => TsStrFactoryClass.Create());

			// Add MDC
			registry
				.For<IFwMetaDataCacheManaged>()
				.LifecycleIs(new SingletonLifecycle())
				.Use<FdoMetaDataCache>();
			// Register its other interface.
			registry
				.For<IFwMetaDataCacheManagedInternal>()
				.Use(c => (IFwMetaDataCacheManagedInternal)c.GetInstance<IFwMetaDataCacheManaged>());

			// Add Virtuals
			registry
				.For<Virtuals>()
				.LifecycleIs(new SingletonLifecycle())
				.Use<Virtuals>();

			// Add IdentityMap
			registry
				.For<IdentityMap>()
				.LifecycleIs(new SingletonLifecycle())
				.Use<IdentityMap>();
			// No. This makes a second instance of IdentityMap,
			// which is probably not desirable.
			//registry
			//	.For<ICmObjectIdFactory>()
			//	.LifecycleIs(new SingletonLifecycle())
			//	.Use<IdentityMap>();
			// Register IdentityMap's other interface.
			registry
				.For<ICmObjectIdFactory>()
				.Use(c => (ICmObjectIdFactory)c.GetInstance<IdentityMap>());
			registry
				.For<ICmObjectRepositoryInternal>()
				.Use(c => (ICmObjectRepositoryInternal)c.GetInstance<ICmObjectRepository>());

			// Add surrogate factory (internal);
			registry
				.For<ICmObjectSurrogateFactory>()
				.LifecycleIs(new SingletonLifecycle())
				.Use<CmObjectSurrogateFactory>();

			// Add surrogate repository (internal);
			registry
				.For<ICmObjectSurrogateRepository>()
				.LifecycleIs(new SingletonLifecycle())
				.Use<CmObjectSurrogateRepository>();

			// Add BEP.
			switch (m_backendProviderType)
			{
				default:
					throw new InvalidOperationException(Strings.ksInvalidBackendProviderType);
				case FDOBackendProviderType.kXML:
					registry
						.For<IDataSetup>()
						.LifecycleIs(new SingletonLifecycle())
						.Use<XMLBackendProvider>();
					break;
				case FDOBackendProviderType.kDb4oClientServer:
					registry
						.For<IDataSetup>()
						.LifecycleIs(new SingletonLifecycle())
						.Use<Db4oClientServerBackendProvider>();
					break;
				case FDOBackendProviderType.kMemoryOnly:
					registry
						.For<IDataSetup>()
						.LifecycleIs(new SingletonLifecycle())
						.Use<MemoryOnlyBackendProvider>();
					break;
			}
			// Register two additional interfaces of the BEP, which are injected into other services.
			registry
				.For<IDataStorer>()
				.Use(c => (IDataStorer)c.GetInstance<IDataSetup>());
			registry
				.For<IDataReader>()
				.Use(c => (IDataReader)c.GetInstance<IDataSetup>());

			// Add Mediator
			registry
				.For<IUnitOfWorkService>()
				.LifecycleIs(new SingletonLifecycle())
				.Use<UnitOfWorkService>();
			// Register additional interfaces for the UnitOfWorkService.
			registry
				.For<ISilDataAccessHelperInternal>()
				.Use(c => (ISilDataAccessHelperInternal)c.GetInstance<IUnitOfWorkService>());
			registry
				.For<IActionHandler>()
				.Use(c => ((UnitOfWorkService)c.GetInstance<IUnitOfWorkService>()).ActiveUndoStack);
			registry
				.For<IWorkerThreadReadHandler>()
				.Use(c => (IWorkerThreadReadHandler)c.GetInstance<IUnitOfWorkService>());
			registry
				.For<IUndoStackManager>()
				.Use(c => (IUndoStackManager)c.GetInstance<IUnitOfWorkService>());

			// Add generated factories.
			AddFactories(registry);

			// Add generated Repositories
			AddRepositories(registry);

			// Add IAnalysisRepository
			registry
				.For<IAnalysisRepository>()
				.LifecycleIs(new SingletonLifecycle())
				.Use<AnalysisRepository>();

			// Add ReferenceAdjusterService
			registry
				.For<IReferenceAdjuster>()
				.LifecycleIs(new SingletonLifecycle())
				.Use<ReferenceAdjusterService>();

			// Add SDA
			registry
				.For<ISilDataAccessManaged>()
				.LifecycleIs(new SingletonLifecycle())
				.Use<DomainDataByFlid>();

			// Add loader helper
			registry
				.For<LoadingServices>()
				.LifecycleIs(new SingletonLifecycle())
				.Use<LoadingServices>();

			// Add writing system manager
			registry
				.For<IWritingSystemManager>()
				.LifecycleIs(new SingletonLifecycle())
				.Use(() => new PalasoWritingSystemManager());
			registry
				.For<ILgWritingSystemFactory>()
				.Use(c => (ILgWritingSystemFactory)c.GetInstance<IWritingSystemManager>());

			registry
				.For<IWritingSystemContainer>()
				.Use(c => c.GetInstance<ILangProjectRepository>().Singleton);

			// =================================================================================
			// Don't add COM object to the registry. StructureMap does not properly release COM
			// objects when the container is disposed, it will crash when the container is
			// disposed.
			// =================================================================================

			var container = new Container(registry);
			// Do this once after something is added, to make sure
			// the entire set of objects can be created.
			// After it proves ok, then block the line again,
			// and let SM create them 'on demand'.
			//container.AssertConfigurationIsValid();

			return new StructureMapServiceLocatorWrapper(new StructureMapServiceLocator(container), container);
		}

		#endregion
	}

	/// <summary>
	/// Wrapper for StructureMapServiceLocator,
	/// which adds the extra methods of IFdoServiceLocator.
	/// </summary>
	internal sealed class StructureMapServiceLocatorWrapper : IFdoServiceLocator, IServiceLocatorInternal, IDisposable
	{
		private StructureMapServiceLocator m_baseServiceLocator;
		private Container m_container;
		private ILgCharacterPropertyEngine m_lgpe = LgIcuCharPropEngineClass.Create();

		/// <summary>
		/// Constructor
		/// </summary>
		internal StructureMapServiceLocatorWrapper(StructureMapServiceLocator baseServiceLocator, Container container)
		{
			if (baseServiceLocator == null) throw new ArgumentNullException("baseServiceLocator");

			m_baseServiceLocator = baseServiceLocator;
			m_container = container;
		}

		#region Disposable stuff
		#if DEBUG
		/// <summary/>
		~StructureMapServiceLocatorWrapper()
		{
			Dispose(false);
		}
		#endif

		/// <summary/>
		public bool IsDisposed
		{
			get;
			private set;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Releases all resources
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary/>
		private void Dispose(bool fDisposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType() + " *******");
			if (fDisposing && !IsDisposed)
			{
				// dispose managed and unmanaged objects
				if (m_container != null)
					m_container.Dispose();
			}
			m_baseServiceLocator = null;
			m_container = null;
			m_lgpe = null;
			IsDisposed = true;
		}
		#endregion

		#region Implementation of IServiceProvider

		/// <summary>
		/// Gets the service object of the specified type.
		/// </summary>
		/// <returns>
		/// A service object of type <paramref name="serviceType"/>.
		///					 -or-
		///				 null if there is no service object of type <paramref name="serviceType"/>.
		/// </returns>
		/// <param name="serviceType">An object that specifies the type of service object to get.
		///				 </param><filterpriority>2</filterpriority>
		public object GetService(Type serviceType)
		{
			return m_baseServiceLocator.GetService(serviceType);
		}

		#endregion

		#region Implementation of IServiceLocator

		/// <summary>
		/// Get an instance of the given <paramref name="serviceType"/>.
		/// </summary>
		/// <param name="serviceType">Type of object requested.</param><exception cref="T:Microsoft.Practices.ServiceLocation.ActivationException">if there is an error resolving
		///			 the service instance.</exception>
		/// <returns>
		/// The requested service instance.
		/// </returns>
		public object GetInstance(Type serviceType)
		{
			return m_baseServiceLocator.GetInstance(serviceType);
		}

		/// <summary>
		/// Get an instance of the given named <paramref name="serviceType"/>.
		/// </summary>
		/// <param name="serviceType">Type of object requested.</param><param name="key">Name the object was registered with.</param><exception cref="T:Microsoft.Practices.ServiceLocation.ActivationException">if there is an error resolving
		///			 the service instance.</exception>
		/// <returns>
		/// The requested service instance.
		/// </returns>
		public object GetInstance(Type serviceType, string key)
		{
			return m_baseServiceLocator.GetInstance(serviceType, key);
		}

		/// <summary>
		/// Get all instances of the given <paramref name="serviceType"/> currently
		///			 registered in the container.
		/// </summary>
		/// <param name="serviceType">Type of object requested.</param><exception cref="T:Microsoft.Practices.ServiceLocation.ActivationException">if there is are errors resolving
		///			 the service instance.</exception>
		/// <returns>
		/// A sequence of instances of the requested <paramref name="serviceType"/>.
		/// </returns>
		public IEnumerable<object> GetAllInstances(Type serviceType)
		{
			return m_baseServiceLocator.GetAllInstances(serviceType);
		}

		/// <summary>
		/// Get an instance of the given <typeparamref name="TService"/>.
		/// </summary>
		/// <typeparam name="TService">Type of object requested.</typeparam><exception cref="T:Microsoft.Practices.ServiceLocation.ActivationException">if there is are errors resolving
		///			 the service instance.</exception>
		/// <returns>
		/// The requested service instance.
		/// </returns>
		public TService GetInstance<TService>()
		{
			// IActionHandler is special - want to return the current one in use.
			if (typeof(TService) == typeof(IActionHandler))
				return (TService)ActionHandler;
			return m_baseServiceLocator.GetInstance<TService>();
		}

		/// <summary>
		/// Get an instance of the given named <typeparamref name="TService"/>.
		/// </summary>
		/// <typeparam name="TService">Type of object requested.</typeparam><param name="key">Name the object was registered with.</param><exception cref="T:Microsoft.Practices.ServiceLocation.ActivationException">if there is are errors resolving
		///			 the service instance.</exception>
		/// <returns>
		/// The requested service instance.
		/// </returns>
		public TService GetInstance<TService>(string key)
		{
			return m_baseServiceLocator.GetInstance<TService>(key);
		}

		/// <summary>
		/// Get all instances of the given <typeparamref name="TService"/> currently
		///			 registered in the container.
		/// </summary>
		/// <typeparam name="TService">Type of object requested.</typeparam><exception cref="T:Microsoft.Practices.ServiceLocation.ActivationException">if there is are errors resolving
		///			 the service instance.</exception>
		/// <returns>
		/// A sequence of instances of the requested <typeparamref name="TService"/>.
		/// </returns>
		public IEnumerable<TService> GetAllInstances<TService>()
		{
			return m_baseServiceLocator.GetAllInstances<TService>();
		}

		#endregion

		#region Implementation of IFdoServiceLocator

		/// <summary>
		/// Shortcut. Don't try to cache this locally, it can change from one call to another!
		/// </summary>
		public IActionHandler ActionHandler
		{
			get { return ((UnitOfWorkService) UnitOfWorkService).ActiveUndoStack; }
		}

		/// <summary>
		/// Shortcut
		/// </summary>
		public IUnitOfWorkService UnitOfWorkService
		{
			get
			{
				return m_baseServiceLocator.GetInstance<IUnitOfWorkService>();
			}
		}

		/// <summary>
		/// Shortcut
		/// </summary>
		public ICmObjectIdFactory CmObjectIdFactory
		{
			get
			{
				return m_baseServiceLocator.GetInstance<ICmObjectIdFactory>();
			}
		}

		/// <summary>
		/// Shortcut
		/// </summary>
		public IDataSetup DataSetup
		{
			get
			{
				return m_baseServiceLocator.GetInstance<IDataSetup>();
			}
		}

		/// <summary>
		/// Get the specified object instance; short for getting ICmObjectRepository and asking it to GetObject.
		/// </summary>
		public ICmObject GetObject(int hvo)
		{
			return m_baseServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo);
		}

		/// <summary>
		/// Answers true iff GetObject(hvo) will succeed; useful to avoid throwing and catching exceptions
		/// when possibly working with fake objects.
		/// </summary>
		public bool IsValidObjectId(int hvo)
		{
			return m_baseServiceLocator.GetInstance<ICmObjectRepository>().IsValidObjectId(hvo);

		}


		/// <summary>
		/// Get the specified object instance; short for getting ICmObjectRepository and asking it to GetObject.
		/// </summary>
		public ICmObject GetObject(Guid guid)
		{
			return m_baseServiceLocator.GetInstance<ICmObjectRepository>().GetObject(guid);
		}

		/// <summary>
		/// Get the specified object instance; short for getting ICmObjectRepository and asking it to GetObject.
		/// </summary>
		public ICmObject GetObject(ICmObjectId id)
		{
			return m_baseServiceLocator.GetInstance<ICmObjectRepository>().GetObject(id);
		}

		/// <summary>
		/// Shortcut to the WS manager.
		/// </summary>
		public IWritingSystemManager WritingSystemManager
		{
			get
			{
				return m_baseServiceLocator.GetInstance<IWritingSystemManager>();
			}
		}

		/// <summary>
		/// Shortcut to the WS container.
		/// </summary>
		public IWritingSystemContainer WritingSystems
		{
			get
			{
				return m_baseServiceLocator.GetInstance<IWritingSystemContainer>();
			}
		}

		/// <summary>
		/// Shortcut.
		/// </summary>
		public ICmObjectRepository ObjectRepository
		{
			get
			{
				return m_baseServiceLocator.GetInstance<ICmObjectRepository>();
			}
		}

		/// <summary>
		/// Shortcut.
		/// </summary>
		public ICmObjectIdFactory ObjectIdFactory
		{
			get
			{
				return m_baseServiceLocator.GetInstance<ICmObjectIdFactory>();
			}
		}

		/// <summary>
		/// Shortcut.
		/// </summary>
		public IFwMetaDataCacheManaged MetaDataCache
		{
			get
			{
				return m_baseServiceLocator.GetInstance<IFwMetaDataCacheManaged>();
			}
		}

		/// <summary>
		/// Shortcut to the writing system factory that gives meaning to writing systems.
		/// </summary>
		public ILgWritingSystemFactory WritingSystemFactory
		{
			get
			{
				return m_baseServiceLocator.GetInstance<ILgWritingSystemFactory>();
			}
		}

		/// <summary>
		/// Shortcut to a service used in fluffing up surrogates.
		/// </summary>
		public LoadingServices LoadingServices
		{
			get
			{
				return m_baseServiceLocator.GetInstance<LoadingServices>();
			}
		}

		/// <summary>
		/// Shortcut to the map used to find the one and only instance of FDO object for any given id.
		/// </summary>
		public IdentityMap IdentityMap
		{
			get
			{
				return m_baseServiceLocator.GetInstance<IdentityMap>();
			}
		}

		/// <summary>
		/// Shortcut to the Unicode character property engine.
		/// </summary>
		public ILgCharacterPropertyEngine UnicodeCharProps
		{
			get
			{
				return m_lgpe; // m_baseServiceLocator.GetInstance<ILgCharacterPropertyEngine>();
			}
		}
		#endregion
	}
}
