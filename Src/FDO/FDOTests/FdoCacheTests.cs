// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FdoCacheTests.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Reflection;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;

using NUnit.Framework;
using NMock;

using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Test.TestUtils;

namespace SIL.FieldWorks.FDO.FDOTests
{
	#region Dummy FdoCache
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// FdoCache for testing that exposes additional methods.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DummyFdoCache : FdoCache
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:DummyFdoCache"/> class.
		/// </summary>
		/// <param name="ode">The ode.</param>
		/// <param name="mdc">The MDC.</param>
		/// <param name="oleDbAccess">The OLE db access.</param>
		/// <param name="sda">The sda.</param>
		/// ------------------------------------------------------------------------------------
		public DummyFdoCache(IOleDbEncap ode, IFwMetaDataCache mdc, IVwOleDbDa oleDbAccess,
			ISilDataAccess sda): base(ode, mdc, oleDbAccess, sda)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Calls the process cellar module attribute.
		/// </summary>
		/// <param name="attr">The attr.</param>
		/// <returns><c>true</c> if ok, <c>false</c> if assembly can't be found.</returns>
		/// ------------------------------------------------------------------------------------
		public bool CallProcessCellarModuleAttribute(CellarModuleAttribute attr)
		{
			return base.ProcessCellarModuleAttribute(attr);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Calls the GetTypeInAssembly method.
		/// </summary>
		/// <param name="typeName">Name of the type.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public Type CallGetTypeInAssembly(string typeName)
		{
			return base.GetTypeInAssembly(typeName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Calls the InitializeModules method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CallInitializeModules()
		{
			InitializeModules();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes the cellar module attribute.
		/// </summary>
		/// <param name="attr">The attr.</param>
		/// ------------------------------------------------------------------------------------
		protected override bool ProcessCellarModuleAttribute(CellarModuleAttribute attr)
		{
			// for testing we ignore loading the types since we might be in the middle of a
			// build where not all of the assemblies containing the types are already built.
			m_ProcessCellarModuleAttributeCalled++;
			return true;
		}

		private int m_ProcessCellarModuleAttributeCalled;
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the number of calls to the ProcessCellarModuleAttribute method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int ProcessCellarModuleAttributeCalled
		{
			get { return m_ProcessCellarModuleAttributeCalled; }
			set { m_ProcessCellarModuleAttributeCalled = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the hashtable of hashtables used in the cache.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Dictionary<Guid, IDictionary> CacheHashTables
		{
			get { return m_hashtables; }
		}
	}
	#endregion

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for FdoCache
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class FdoCacheTests : BaseTest
	{
		#region Dummy StText
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private class DummyStText : StText
		{
		}
		#endregion

		private CacheBase m_cacheBase;
		private DummyFdoCache m_fdoCache;
		private DynamicMock m_mdc;
		private DynamicMock m_ode;
		private DynamicMock m_odde;
		private DynamicMock m_odc;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the test
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public void Setup()
		{
			CheckDisposed();
			m_ode = new DynamicMock(typeof(IOleDbEncap));
			m_odde = new DynamicMock(typeof(IVwOleDbDa));
			m_mdc = new DynamicMock(typeof(IFwMetaDataCache));
			m_odc = new DynamicMock(typeof(IOleDbCommand));
			m_odc.Ignore("ExecCommand");
			m_ode.SetupResult("CreateCommand", null,
				new string[] { "SIL.FieldWorks.Common.COMInterfaces.IOleDbCommand&"},
				new object[] { m_odc.MockInstance });

			m_cacheBase = new CacheBase((IFwMetaDataCache)m_mdc.MockInstance);

			if (m_fdoCache != null)
				m_fdoCache.Dispose();
			m_fdoCache = new DummyFdoCache((IOleDbEncap)m_ode.MockInstance,
				(IFwMetaDataCache)m_mdc.MockInstance, (IVwOleDbDa)m_odde.MockInstance,
				m_cacheBase);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Wipes out the old stuff after a test the test
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public void TearDown()
		{
			CheckDisposed();
			m_fdoCache.Dispose();
			m_cacheBase.Dispose();
			m_cacheBase = null;
			m_fdoCache = null;
			m_mdc = null;
			m_ode = null;
			m_odde = null;
			m_odc = null;
		}

		#region IDisposable override

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_cacheBase != null)
					m_cacheBase.Dispose();
				if (m_fdoCache != null)
					m_fdoCache.Dispose();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_fdoCache = null;
			m_cacheBase = null;
			m_mdc = null;
			m_ode = null;
			m_odde = null;
			m_odc = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		#region Tests related to splitting modules in different assemblies

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests initializing the cellar modules. We expect that ProcessCellarModuleAttribute
		/// currently gets called once (ScrFDO) because all other modules are in the FDO
		/// assembly and should be ignored.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InitializeModules()
		{
			m_fdoCache.ProcessCellarModuleAttributeCalled = 0;
			m_fdoCache.CallInitializeModules();

			Assert.AreEqual(1, m_fdoCache.ProcessCellarModuleAttributeCalled);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the processing of a cellar module attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ProcessCellarModuleAttribute()
		{
			CellarModuleAttribute attr = new CellarModuleAttribute("TestModule", "FdoTests");
			Assert.IsTrue(m_fdoCache.CallProcessCellarModuleAttribute(attr));
			Assert.AreEqual(GetType(),
				m_fdoCache.CallGetTypeInAssembly("SIL.FieldWorks.FDO.FDOTests.FdoCacheTests"));
			Assert.IsNull(m_fdoCache.CallGetTypeInAssembly("This.Type.Does.Not.Exist"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the processing of a cellar module attribute when the specified assembly can't
		/// be found. We expect this not to fail.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ProcessCellarModuleAttribute_AssemblyNotFound()
		{
			CellarModuleAttribute attr = new CellarModuleAttribute("TestModule", "DoesntExist");
			Assert.IsFalse(m_fdoCache.CallProcessCellarModuleAttribute(attr));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting the type for an type string.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetTypeInAssembly()
		{
			// Test original mapping
			Assert.AreEqual(typeof(StText),
				m_fdoCache.CallGetTypeInAssembly("SIL.FieldWorks.FDO.Cellar.StText"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests mapping a type to a different type.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MapTypes_Remapped()
		{
			// Remap type to a different type (derived from StText)
			m_fdoCache.MapType("SIL.FieldWorks.FDO.Cellar.StText", typeof(DummyStText));
			Assert.AreEqual(typeof(DummyStText),
				m_fdoCache.CallGetTypeInAssembly("SIL.FieldWorks.FDO.Cellar.StText"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests mapping a type to a different type if the remapped type isn't a subclass.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(ExceptionType=typeof(InvalidCastException))]
		public void MapTypes_NotSubclass()
		{
			// Remap type to a different type (derived from StText)
			m_fdoCache.MapType("SIL.FieldWorks.FDO.Cellar.StTxtPara", typeof(DummyStText));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that mapping the same two types twice doesn't crash.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MapTypes_ReMapSameType()
		{
			m_fdoCache.MapType("SIL.FieldWorks.FDO.Cellar.StText", typeof(DummyStText));
			m_fdoCache.MapType("SIL.FieldWorks.FDO.Cellar.StText", typeof(DummyStText));

			Assert.AreEqual(typeof(DummyStText),
				m_fdoCache.CallGetTypeInAssembly("SIL.FieldWorks.FDO.Cellar.StText"));
		}

		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the IsOwnerless method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ClassIsOwnerless()
		{
			CheckDisposed();
			m_mdc.ExpectAndReturn("GetClassName", "LgWritingSystem", (uint)LgWritingSystem.kClassId);

			// we need this line for testing FdoVector
			// m_cacheBase.SetObjProp(1000, (int)CmObjectFields.kflidCmObject_Owner, 0);

			m_cacheBase.CacheIntProp(1000, (int)CmObjectFields.kflidCmObject_Class,
				LgWritingSystem.kClassId);

			Assert.IsTrue(m_fdoCache.ClassIsOwnerless(1000));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the IsOwnerless method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ClassIsNotOwnerless()
		{
			CheckDisposed();
			m_mdc.ExpectAndReturn("GetClassName", "StText", (uint)StText.kClassId);

			m_cacheBase.CacheIntProp(1000, (int)CmObjectFields.kflidCmObject_Class,
				StText.kClassId);

			Assert.IsFalse(m_fdoCache.ClassIsOwnerless(1000));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private class DummyChangeWatcher : ChangeWatcher
		{
			public DummyChangeWatcher() : base(null, 0)
			{
			}

			protected override void DoEffectsOfPropChange(int hvo, int ivMin, int cvIns, int cvDel)
			{
				throw new Exception("The method or operation is not implemented.");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetHashtable method when the requested hashtable type has not previously
		/// been created.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetHashtable_CreateNew()
		{
			CheckDisposed();

			Guid hashTableGuid = Guid.NewGuid();
			Dictionary<int, string> dict =
				m_fdoCache.GetHashtable<int, string, DummyChangeWatcher>(hashTableGuid, 42);
			Assert.IsNotNull(dict);
			Assert.AreEqual(0, dict.Count);
			Assert.AreEqual(1, m_fdoCache.CacheHashTables.Count, "Expected one entry in cache hashtable");

			// Make sure the right kind of ChangeWatcher was created and registered
			bool fDummyCwFound = false;
			foreach (ChangeWatcher cw in m_fdoCache.ChangeWatchers)
			{
				if (cw is DummyChangeWatcher)
					fDummyCwFound = true;
			}
			Assert.IsTrue(fDummyCwFound, "No DummyChangeWatcher was found.");

			// Attempt to get the same hash table.
			dict = m_fdoCache.GetHashtable<int, string, DummyChangeWatcher>(hashTableGuid, 42);
			Assert.IsNotNull(dict);
			Assert.AreEqual(0, dict.Count);
			Assert.AreEqual(1, m_fdoCache.CacheHashTables.Count, "Expected one entry in cache hashtable");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the TryGetHashtable method when the Hashtable has not been created and then
		/// after it has been created.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TryGetHashtable()
		{
			CheckDisposed();
			Guid hashTableGuid = Guid.NewGuid();
			Dictionary<int, string> dict;
			Assert.IsFalse(m_fdoCache.TryGetHashtable<int, string>(hashTableGuid, out dict),
				"The hash table should not exist.");

			// Now add the hash table...
			dict = m_fdoCache.GetHashtable<int, string, DummyChangeWatcher>(hashTableGuid, 42);
			Assert.IsNotNull(dict);

			Dictionary<int, string> dictOut;
			// We expect TryGetHashTable to succeed now.
			Assert.IsTrue(m_fdoCache.TryGetHashtable<int, string>(hashTableGuid, out dictOut),
				"The hash table should exist.");
			Assert.AreEqual(dict, dictOut);
		}
	}
}
