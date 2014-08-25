// Copyright (c) 2009-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ServiceLocatorFactory.cs
// Responsibility: Randy Regnier
// Last reviewed: never

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.Practices.ServiceLocation;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.FDO.Application.Impl;
using SIL.FieldWorks.FDO.DomainImpl;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.FDO.Infrastructure.Impl;
using SIL.FieldWorks.FDO.DomainServices.DataMigration;
using SIL.Utils;
using StructureMap;
using StructureMap.Configuration.DSL;
using StructureMap.Pipeline;

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
		private readonly IFdoUI m_ui;
		private readonly IFdoDirectories m_dirs;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="backendProviderType">Type of backend provider to create.</param>
		/// <param name="ui">The UI service.</param>
		/// <param name="dirs">The directories service.</param>
		internal FdoServiceLocatorFactory(FDOBackendProviderType backendProviderType, IFdoUI ui, IFdoDirectories dirs)
		{
			m_backendProviderType = backendProviderType;
			m_ui = ui;
			m_dirs = dirs;
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
				case FDOBackendProviderType.kSharedXML:
					registry
						.For<IDataSetup>()
						.LifecycleIs(new SingletonLifecycle())
						.Use<SharedXMLBackendProvider>();
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
				.Use(() => new PalasoWritingSystemManager {TemplateFolder = m_dirs.TemplateDirectory});
			registry
				.For<ILgWritingSystemFactory>()
				.Use(c => (ILgWritingSystemFactory)c.GetInstance<IWritingSystemManager>());

			registry
				.For<IWritingSystemContainer>()
				.Use(c => c.GetInstance<ILangProjectRepository>().Singleton);

			registry
				.For<IFdoUI>()
				.Use(m_ui);

			registry
				.For<IFdoDirectories>()
				.Use(m_dirs);

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

			return new StructureMapServiceLocator(container);
		}

		#endregion
	}

	/// <summary>
	/// Implementation of StructureMapServiceLocator, with extra methods of IFdoServiceLocator.
	/// </summary>
	/// <remarks>This class used to be named StructureMapServiceLocatorWrapper, wrapping a class
	/// StructureMapServiceLocator implemented in StructureMapAdapter.dll. However, no one
	/// could remember where that originally came from, and it implements only two simple methods
	/// so that it seemed worth to remove the dll and implement the methods in here.</remarks>
	internal sealed class StructureMapServiceLocator : ServiceLocatorImplBase,
		IFdoServiceLocator, IServiceLocatorInternal, IDisposable
	{
		private Container m_container;
		private ILgCharacterPropertyEngine m_lgpe = LgIcuCharPropEngineClass.Create();

		/// <summary>
		/// Constructor
		/// </summary>
		internal StructureMapServiceLocator(Container container)
		{
			m_container = container;
		}

		#region Disposable stuff
		#if DEBUG
		/// <summary/>
		~StructureMapServiceLocator()
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
				if(m_container != null)
				{
					try
					{
						m_container.Dispose();
					}
					catch(InvalidComObjectException e) // Intermittantly the dispose of the container fails because a COM object has become invalid
					{
						// Display an indication of the failure, but don't crash, we made a good faith effort to dispose all our COM objects
						// and they probably were disposed. Also at this point we are probably shutting down, or wrapping up a unit test.
						Debug.WriteLine(String.Format(@"COM problem when disposing container in StructureMapServiceLocator: {0}", e.Message));
					}
				}
			}
			m_container = null;
			m_lgpe = null;
			IsDisposed = true;
		}
		#endregion

		#region Implementation of abstract methods
		/// <summary>
		///             When implemented by inheriting classes, this method will do the actual work of resolving
		///             the requested service instance.
		/// </summary>
		/// <param name="serviceType">Type of instance requested.</param>B
		/// <param name="key">Name of registered service you want. May be null.</param>
		/// <returns>
		/// The requested service instance.
		/// </returns>
		protected override object DoGetInstance(Type serviceType, string key)
		{
			if (string.IsNullOrEmpty(key))
			{
				return m_container.GetInstance(serviceType);
			}
			return m_container.GetInstance(serviceType, key);
		}

		/// <summary>
		///             When implemented by inheriting classes, this method will do the actual work of
		///             resolving all the requested service instances.
		/// </summary>
		/// <param name="serviceType">Type of service requested.</param>
		/// <returns>
		/// Sequence of service instance objects.
		/// </returns>
		protected override IEnumerable<object> DoGetAllInstances(Type serviceType)
		{
			foreach (object obj in m_container.GetAllInstances(serviceType))
			{
				yield return obj;
			}
		}

		#endregion

		#region Overrides of IServiceLocator implementation

		/// <summary>
		/// Get an instance of the given <typeparamref name="TService"/>.
		/// </summary>
		/// <typeparam name="TService">Type of object requested.</typeparam><exception cref="T:Microsoft.Practices.ServiceLocation.ActivationException">if there is are errors resolving
		///			 the service instance.</exception>
		/// <returns>
		/// The requested service instance.
		/// </returns>
		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification="See TODO-Linux comment")]
		public override TService GetInstance<TService>()
		{
			// IActionHandler is special - want to return the current one in use.
			if (typeof(TService) == typeof(IActionHandler))
				return (TService)ActionHandler;
			return base.GetInstance<TService>();
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
				return GetInstance<IUnitOfWorkService>();
			}
		}

		/// <summary>
		/// Shortcut
		/// </summary>
		public ICmObjectIdFactory CmObjectIdFactory
		{
			get
			{
				return GetInstance<ICmObjectIdFactory>();
			}
		}

		/// <summary>
		/// Shortcut
		/// </summary>
		public IDataSetup DataSetup
		{
			get
			{
				return GetInstance<IDataSetup>();
			}
		}

		/// <summary>
		/// Get the specified object instance; short for getting ICmObjectRepository and asking it to GetObject.
		/// </summary>
		public ICmObject GetObject(int hvo)
		{
			return GetInstance<ICmObjectRepository>().GetObject(hvo);
		}

		/// <summary>
		/// Answers true iff GetObject(hvo) will succeed; useful to avoid throwing and catching exceptions
		/// when possibly working with fake objects.
		/// </summary>
		public bool IsValidObjectId(int hvo)
		{
			return GetInstance<ICmObjectRepository>().IsValidObjectId(hvo);

		}


		/// <summary>
		/// Get the specified object instance; short for getting ICmObjectRepository and asking it to GetObject.
		/// </summary>
		public ICmObject GetObject(Guid guid)
		{
			return GetInstance<ICmObjectRepository>().GetObject(guid);
		}

		/// <summary>
		/// Get the specified object instance; short for getting ICmObjectRepository and asking it to GetObject.
		/// </summary>
		public ICmObject GetObject(ICmObjectId id)
		{
			return GetInstance<ICmObjectRepository>().GetObject(id);
		}

		/// <summary>
		/// Shortcut to the WS manager.
		/// </summary>
		public IWritingSystemManager WritingSystemManager
		{
			get
			{
				return GetInstance<IWritingSystemManager>();
			}
		}

		/// <summary>
		/// Shortcut to the WS container.
		/// </summary>
		public IWritingSystemContainer WritingSystems
		{
			get
			{
				return GetInstance<IWritingSystemContainer>();
			}
		}

		/// <summary>
		/// Shortcut.
		/// </summary>
		public ICmObjectRepository ObjectRepository
		{
			get
			{
				return GetInstance<ICmObjectRepository>();
			}
		}

		/// <summary>
		/// Shortcut.
		/// </summary>
		public ICmObjectIdFactory ObjectIdFactory
		{
			get
			{
				return GetInstance<ICmObjectIdFactory>();
			}
		}

		/// <summary>
		/// Shortcut.
		/// </summary>
		public IFwMetaDataCacheManaged MetaDataCache
		{
			get
			{
				return GetInstance<IFwMetaDataCacheManaged>();
			}
		}

		/// <summary>
		/// Shortcut to the writing system factory that gives meaning to writing systems.
		/// </summary>
		public ILgWritingSystemFactory WritingSystemFactory
		{
			get
			{
				return GetInstance<ILgWritingSystemFactory>();
			}
		}

		/// <summary>
		/// Shortcut to a service used in fluffing up surrogates.
		/// </summary>
		public LoadingServices LoadingServices
		{
			get
			{
				return GetInstance<LoadingServices>();
			}
		}

		/// <summary>
		/// Shortcut to the map used to find the one and only instance of FDO object for any given id.
		/// </summary>
		public IdentityMap IdentityMap
		{
			get
			{
				return GetInstance<IdentityMap>();
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
