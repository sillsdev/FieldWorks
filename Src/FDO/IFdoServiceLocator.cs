using Microsoft.Practices.ServiceLocation;
using System;

using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.Infrastructure;

namespace SIL.FieldWorks.FDO
{
	/// <summary>
	/// This interface defines FDO extensions to IServiceLocator, mainly shortcuts for particular
	/// GetService() calls.
	/// </summary>
	public interface IFdoServiceLocator : IServiceLocator
	{
		/// <summary>
		/// Shortcut to the IActionHandler instance.
		/// </summary>
		IActionHandler ActionHandler { get; }

		/// <summary>
		/// Shortcut to the ICmObjectIdFactory instance.
		/// </summary>
		ICmObjectIdFactory CmObjectIdFactory { get; }

		/// <summary>
		/// Shortcut to the IDataSetup instance.
		/// </summary>
		IDataSetup DataSetup { get; }

		/// <summary>
		/// Get the specified object instance; short for getting ICmObjectRepository and asking it to GetObject.
		/// </summary>
		ICmObject GetObject(int hvo);

		/// <summary>
		/// Answers true iff GetObject(hvo) will succeed; useful to avoid throwing and catching exceptions
		/// when possibly working with fake objects.
		/// </summary>
		bool IsValidObjectId(int hvo);

		/// <summary>
		/// Get the specified object instance; short for getting ICmObjectRepository and asking it to GetObject.
		/// </summary>
		ICmObject GetObject(Guid guid);

		/// <summary>
		/// Get the specified object instance; short for getting ICmObjectRepository and asking it to GetObject.
		/// </summary>
		ICmObject GetObject(ICmObjectId id);

		/// <summary>
		/// Shortcut to the WS manager.
		/// </summary>
		WritingSystemManager WritingSystemManager { get; }

		/// <summary>
		/// Gets the writing system container.
		/// </summary>
		/// <value>The writing system container.</value>
		IWritingSystemContainer WritingSystems { get; }

		/// <summary>
		/// The place to get CmObjects.
		/// </summary>
		ICmObjectRepository ObjectRepository { get; }

		/// <summary>
		/// The thing that knows how to make ICmObjectIds.
		/// </summary>
		ICmObjectIdFactory ObjectIdFactory { get; }

		/// <summary>
		/// Shortcut to the meta data cache that gives information about the properties of objects.
		/// </summary>
		IFwMetaDataCacheManaged MetaDataCache { get;  }

		/// <summary>
		/// Shortcut to the writing system factory that gives meaning to writing systems.
		/// </summary>
		ILgWritingSystemFactory WritingSystemFactory { get; }

		/// <summary>
		/// Shortcut to the Unicode character property engine.
		/// </summary>
		ILgCharacterPropertyEngine UnicodeCharProps { get; }
	}

	/// <summary>
	/// A further interface typically implemented by service locator, for services that should stay
	/// internal to FDO.
	/// </summary>
	internal interface IServiceLocatorInternal
	{
		Infrastructure.Impl.IdentityMap IdentityMap { get; }
		LoadingServices LoadingServices { get; }
		IUnitOfWorkService UnitOfWorkService { get; }
	}
}
