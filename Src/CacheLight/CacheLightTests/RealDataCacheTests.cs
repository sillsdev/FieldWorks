using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic; // Needed for KeyNotFoundException.
using System.Xml;
using System.IO;

using NUnit.Framework;
using SIL.FieldWorks.CacheLight;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.CacheLightTests
{
	/// <summary>
	/// Tests the two main interfaces (ISilDataAccess and IVwCacheDa) for RealDataCache.
	/// </summary>
	public class RealDataCacheBase : IFWDisposable
	{
		/// <summary></summary>
		private RealDataCache m_realDataCache;

		/// <summary>
		///
		/// </summary>
		protected ISilDataAccess SilDataAccess
		{
			get { return m_realDataCache as ISilDataAccess; }
		}

		/// <summary>
		///
		/// </summary>
		protected IVwCacheDa VwCacheDa
		{
			get { return m_realDataCache as IVwCacheDa; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If a test overrides this, it should call this base implementation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public virtual void FixtureSetup()
		{
			CheckDisposed();

			string modelDir = DirectoryFinder.FwSourceDirectory;
			modelDir = modelDir.Substring(0, modelDir.LastIndexOf('\\'));
			modelDir = Path.Combine(modelDir, @"Output\XMI");
			m_realDataCache = new RealDataCache();
			m_realDataCache.MetaDataCache = MetaDataCache.CreateMetaDataCache(Path.Combine(modelDir, "xmi2cellar3.xml"));
		}

		#region IDisposable & Co. implementation

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		/// True, if the object has been disposed.
		/// </summary>
		private bool m_isDisposed = false;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual bool IsDisposed
		{
			get { return m_isDisposed; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		~RealDataCacheBase()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Subclasses should override the Dispose method with the parameter
		/// and call the base method to tear down a test fisture class.
		/// </summary>
		/// <remarks>Must not be virtual.</remarks>
		/// ------------------------------------------------------------------------------------
		[TestFixtureTearDown]
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

		/// ------------------------------------------------------------------------------------
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
		/// ------------------------------------------------------------------------------------
		protected virtual void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_realDataCache != null)
					m_realDataCache.Dispose();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_realDataCache = null;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the FDO cache and open database
		/// </summary>
		/// <remarks>This method is called before each test</remarks>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public virtual void Initialize()
		{
			CheckDisposed();

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shuts down the FDO cache
		/// </summary>
		/// <remarks>This method is called after each test</remarks>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public virtual void Exit()
		{
			CheckDisposed();

			VwCacheDa.ClearAllData();
		}
	}

	/// <summary>
	/// Tests the IVwCacheDa methods for RealDataCache.
	/// Since IVwCacheDa is so short on 'getters', I have to use
	/// the 'getters' from the ISilDataAccess interface,
	/// in order tobe able to test these at all. :-(
	/// </summary>
	public class RealDataCacheIVwCacheDaTests : RealDataCacheBase
	{
	}

	/// <summary>
	/// Tests the two main interfaces (ISilDataAccess and IVwCacheDa) for RealDataCache.
	/// </summary>
	[TestFixture]
	public class RealDataCacheISilDataAccessTests : RealDataCacheBase
	{
		/// <summary>
		/// Test Int Property get/set.
		/// </summary>
		[Test]
		public void ObjPropTest()
		{
			CheckDisposed();

			// Set class first, or it will throw an exception.
			int hvo = 1;
			uint clidAnal = SilDataAccess.MetaDataCache.GetClassId("WfiAnalysis");
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class, (int)clidAnal);
			// Set class of POS object.
			int hvoObj = 3;
			uint clidPOS = SilDataAccess.MetaDataCache.GetClassId("PartOfSpeech");
			SilDataAccess.SetInt(hvoObj, (int)CmObjectFields.kflidCmObject_Class, (int)clidPOS);

			// Now set its 'category' property.
			int tag = (int)SilDataAccess.MetaDataCache.GetFieldId("WfiAnalysis", "Category", false);
			SilDataAccess.SetObjProp(hvo, tag, hvoObj);
			int hvoObj2 = SilDataAccess.get_ObjectProp(hvo, tag);
			Assert.AreEqual(hvoObj, hvoObj2, "Wrong hvoObj in cache.");
		}
		/// <summary>
		/// Test Int Property get, when no set has been done.
		/// </summary>
		[Test]
		[ExpectedException(typeof(KeyNotFoundException))]
		public void ObjPropKNTTest()
		{
			CheckDisposed();

			int hvo = 1;
			// Set class first, or it will throw the exception in the wrong place.
			uint clid = SilDataAccess.MetaDataCache.GetClassId("MoAdhocProhib");
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class, (int)clid);

			//int tag = (int)SilDataAccess.MetaDataCache.GetFieldId("MoAdhocProhib", "Adjacency", false);
			int hvoObj2 = SilDataAccess.get_ObjectProp(hvo, (int)CmObjectFields.kflidCmObject_Owner);
		}

		/// <summary>
		/// Test Int Property get/set.
		/// </summary>
		[Test]
		public void IntPropTest()
		{
			CheckDisposed();

			int hvo = 1;
			uint clid = SilDataAccess.MetaDataCache.GetClassId("PartOfSpeech");
			int tag = (int)CmObjectFields.kflidCmObject_Class;
			SilDataAccess.SetInt(hvo, tag, (int)clid);
			int clid1 = SilDataAccess.get_IntProp(hvo, tag);
			Assert.AreEqual((int)clid, clid1, "Wrong clid in cache.");

			// See if the int is there via another method.
			// It should be there.
			bool isInCache = false;
			int clid2 = VwCacheDa.get_CachedIntProp(hvo, tag, out isInCache);
			Assert.IsTrue(isInCache, "Int not in cache.");
			Assert.AreEqual(clid1, clid2, "Clids are not the same.");

			// See if the int is there via another method.
			// It should not be there.
			isInCache = true;
			int ownerHvo = VwCacheDa.get_CachedIntProp(hvo, (int)CmObjectFields.kflidCmObject_Owner, out isInCache);
			Assert.IsFalse(isInCache, "Int is in cache.");
			Assert.AreEqual(0, ownerHvo, "Wrong owner.");
		}
		/// <summary>
		/// Test Int Property get, when no set has been done.
		/// </summary>
		[Test]
		[ExpectedException(typeof(KeyNotFoundException))]
		public void IntPropKNTTest()
		{
			CheckDisposed();

			int hvo = 1;
			// Set class first, or it will throw the exception in the wrong place.
			uint clid = SilDataAccess.MetaDataCache.GetClassId("MoAdhocProhib");
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class, (int)clid);

			int tag = (int)SilDataAccess.MetaDataCache.GetFieldId("MoAdhocProhib", "Adjacency", false);
			int clid1 = SilDataAccess.get_IntProp(hvo, tag);
		}

		/// <summary>
		/// Test Guid Property get/set.
		/// </summary>
		[Test]
		public void GuidPropTest()
		{
			CheckDisposed();

			// Set class first, or it will throw an exception.
			int hvo = 1;
			uint clidAnal = SilDataAccess.MetaDataCache.GetClassId("WfiAnalysis");
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class, (int)clidAnal);

			// Now set its 'Guid' property.
			int tag = (int)CmObjectFields.kflidCmObject_Guid;
			Guid uid = Guid.NewGuid();
			SilDataAccess.SetGuid(hvo, tag, uid);
			Guid uid2 = SilDataAccess.get_GuidProp(hvo, tag);
			Assert.AreEqual(uid, uid2, "Wrong uid in cache.");

			// Test the reverse method.
			int hvo2 = SilDataAccess.get_ObjFromGuid(uid2);
			Assert.AreEqual(hvo, hvo2, "Wrong hvo in cache for Guid.");
		}
		/// <summary>
		/// Test Guid Property get, when no set has been done.
		/// </summary>
		[Test]
		[ExpectedException(typeof(KeyNotFoundException))]
		public void GuidPropKNTTest()
		{
			CheckDisposed();

			int hvo = 1;
			// Set class first, or it will throw the exception in the wrong place.
			uint clidLe = SilDataAccess.MetaDataCache.GetClassId("LexEntry");
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class, (int)clidLe);
			int tag = (int)CmObjectFields.kflidCmObject_Guid;

			Guid uid = SilDataAccess.get_GuidProp(hvo, tag);
		}

		/// <summary>
		/// Test Bool Property get/set.
		/// </summary>
		[Test]
		public void BoolPropTest()
		{
			// Set class first, or it will throw an exception.
			int hvo = 1;
			uint clidLe = SilDataAccess.MetaDataCache.GetClassId("LexEntry");
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class, (int)clidLe);

			// Now set its 'ExcludeAsHeadword' property.
			int tag = (int)SilDataAccess.MetaDataCache.GetFieldId("LexEntry", "ExcludeAsHeadword", false);
			bool excludeOriginal = true;
			SilDataAccess.SetBoolean(hvo, tag, excludeOriginal);
			bool excludeOriginal1 = SilDataAccess.get_BooleanProp(hvo, tag);
			Assert.AreEqual(excludeOriginal, excludeOriginal1, "Wrong bool in cache.");
		}
		/// <summary>
		/// Test Guid Property get, when no set has been done.
		/// </summary>
		[Test]
		[ExpectedException(typeof(KeyNotFoundException))]
		public void BoolPropKNTTest()
		{
			CheckDisposed();

			int hvo = 1;
			// Set class first, or it will throw the exception in the wrong place.
			uint clidLe = SilDataAccess.MetaDataCache.GetClassId("WfiAnalysis");
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class, (int)clidLe);
			int tag = (int)SilDataAccess.MetaDataCache.GetFieldId("WfiAnalysis", "Category", false);

			bool excludeOriginal = SilDataAccess.get_BooleanProp(hvo, tag);
		}

		/// <summary>
		/// Test Unicode Property get/set.
		/// </summary>
		[Test]
		public void UnicodePropTest()
		{
			// Set class first, or it will throw an exception.
			int hvo = 1;
			uint clidAnal = SilDataAccess.MetaDataCache.GetClassId("LangProject");
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class, (int)clidAnal);

			// Set its 'EthnologueCode' property, using the 'BSTR' method.
			int tag = (int)SilDataAccess.MetaDataCache.GetFieldId("LangProject", "EthnologueCode", false);
			string ec = "ZPI";
			SilDataAccess.set_UnicodeProp(hvo, tag, ec);
			string ec2 = SilDataAccess.get_UnicodeProp(hvo, tag);
			Assert.AreEqual(ec, ec2, "Wrong Unicode string in cache.");

			// Set its 'EthnologueCode' property, using non-bstr method.
			string ecNew = "ZPR";
			SilDataAccess.SetUnicode(hvo, tag, ecNew, ecNew.Length);
			int len;
			SilDataAccess.UnicodePropRgch(hvo, tag, null, 0, out len);
			Assert.AreEqual(ecNew.Length, len);
			using (ArrayPtr arrayPtr = MarshalEx.StringToNative(len + 1, true))
			{
				int cch;
				SilDataAccess.UnicodePropRgch(hvo, tag, arrayPtr, len + 1, out cch);
				string ecNew2 = MarshalEx.NativeToString(arrayPtr, cch, true);
				Assert.AreEqual(ecNew, ecNew2);
				Assert.AreEqual(ecNew2.Length, cch);
				Assert.IsTrue(SilDataAccess.IsDirty());
			}
		}

		/// <summary>
		/// Test Unicode Property get, when no set has been done.
		/// </summary>
		[Test]
		[ExpectedException(typeof(KeyNotFoundException))]
		public void UnicodePropKNTTest()
		{
			CheckDisposed();

			int hvo = 1;
			// Set class first, or it will throw the exception in the wrong place.
			uint clid = SilDataAccess.MetaDataCache.GetClassId("LangProject");
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class, (int)clid);

			int tag = (int)SilDataAccess.MetaDataCache.GetFieldId("LangProject", "EthnologueCode", false);
			string ec = SilDataAccess.get_UnicodeProp(hvo, tag);
		}

		/// <summary>
		/// Test Unicode Property get, when no set has been done.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void UnicodePropWrongLengthTest()
		{
			CheckDisposed();

			// Set class first, or it will throw an exception.
			int hvo = 1;
			uint clidAnal = SilDataAccess.MetaDataCache.GetClassId("LangProject");
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class, (int)clidAnal);

			// Set its 'EthnologueCode' property, using the 'BSTR' method.
			int tag = (int)SilDataAccess.MetaDataCache.GetFieldId("LangProject", "EthnologueCode", false);
			string ec = "ZPI";
			SilDataAccess.set_UnicodeProp(hvo, tag, ec);
			string ec2 = SilDataAccess.get_UnicodeProp(hvo, tag);
			Assert.AreEqual(ec, ec2, "Wrong Unicode string in cache.");

			// Set its 'EthnologueCode' property, using non-bstr method.
			string ecNew = "ZPR";
			SilDataAccess.SetUnicode(hvo, tag, ecNew, ecNew.Length);
			int len;
			SilDataAccess.UnicodePropRgch(hvo, tag, null, 0, out len);
			Assert.AreEqual(ecNew.Length, len);
			using (ArrayPtr arrayPtr = MarshalEx.StringToNative(len, true))
			{
				int cch;
				// Should throw the exception here.
				SilDataAccess.UnicodePropRgch(hvo, tag, arrayPtr, len, out cch);
			}
		}

		/// <summary>
		/// Test In64 Property get/set.
		/// </summary>
		[Test]
		public void Int64PropTest()
		{
			CheckDisposed();

			// CmPerson->DateOfBirth:GenDate
			// First, set up class id.
			int hvo = 1;
			uint clid = SilDataAccess.MetaDataCache.GetClassId("CmPerson");
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class, (int)clid);

			long dob = long.MinValue; // Use 'Adam' :-)
			int tag = (int)SilDataAccess.MetaDataCache.GetFieldId("CmPerson", "DateOfBirth", false);
			SilDataAccess.SetInt64(hvo, tag, dob);
			long dob2 = SilDataAccess.get_Int64Prop(hvo, tag);
			Assert.AreEqual(dob, dob2, "Wrong DOB in cache.");
		}
		/// <summary>
		/// Test In64 Property get, when no set has been done.
		/// </summary>
		[Test]
		[ExpectedException(typeof(KeyNotFoundException))]
		public void In64PropKNTTest()
		{
			CheckDisposed();

			int hvo = 1;
			// Set class first, or it will throw the exception in the wrong place.
			uint clidLe = SilDataAccess.MetaDataCache.GetClassId("CmPerson");
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class, (int)clidLe);
			int tag = (int)SilDataAccess.MetaDataCache.GetFieldId("CmPerson", "DateOfBirth", false);
			long dob = SilDataAccess.get_Int64Prop(hvo, tag);
		}

		/// <summary>
		/// Test Time Property get/set.
		/// </summary>
		[Test]
		public void TimePropTest()
		{
			CheckDisposed();

			// LexEntry->DateCreated
			// First, set up class id.
			int hvo = 2;
			uint clid = SilDataAccess.MetaDataCache.GetClassId("LexEntry");
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class, (int)clid);

			long doc = long.MinValue;
			int tag = (int)SilDataAccess.MetaDataCache.GetFieldId("LexEntry", "DateCreated", false);
			SilDataAccess.SetTime(hvo, tag, doc);
			long doc2 = SilDataAccess.get_TimeProp(hvo, tag);
			Assert.AreEqual(doc, doc2, "Wrong creation in cache.");
		}
		/// <summary>
		/// Test Time Property get, when no set has been done.
		/// </summary>
		[Test]
		[ExpectedException(typeof(KeyNotFoundException))]
		public void TimePropKNTTest()
		{
			CheckDisposed();

			int hvo = 1;
			// Set class first, or it will throw the exception in the wrong place.
			uint clid = SilDataAccess.MetaDataCache.GetClassId("LexEntry");
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class, (int)clid);

			int tag = (int)SilDataAccess.MetaDataCache.GetFieldId("LexEntry", "DateCreated", false);
			long dob = SilDataAccess.get_TimeProp(hvo, tag);
		}

		/// <summary>
		/// Test IUnknown Property get/set.
		/// </summary>
		[Test]
		public void UnkPropTest()
		{
			CheckDisposed();

			// First, set up class id.
			int hvo = 1;
			uint clid = SilDataAccess.MetaDataCache.GetClassId("StPara");
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class, (int)clid);

			ITsPropsBldr tsPropsBuilder = TsPropsBldrClass.Create();
			ITsTextProps props = tsPropsBuilder.GetTextProps();
			int tag = (int)SilDataAccess.MetaDataCache.GetFieldId("StPara", "StyleRules", false);
			SilDataAccess.SetUnknown(hvo, tag, props);
			ITsTextProps props2 = (ITsTextProps)SilDataAccess.get_UnknownProp(hvo, tag);
			Assert.AreEqual(props, props2, "Wrong text props in cache.");
		}

		/// <summary>
		/// Test Time Property get, when no set has been done.
		/// </summary>
		[Test]
		[ExpectedException(typeof(KeyNotFoundException))]
		public void UnkPropKNTTest()
		{
			CheckDisposed();

			int hvo = 1;
			// Set class first, or it will throw the exception in the wrong place.
			uint clid = SilDataAccess.MetaDataCache.GetClassId("StPara");
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class, (int)clid);

			int tag = (int)SilDataAccess.MetaDataCache.GetFieldId("StPara", "StyleRules", false);
			object unk = SilDataAccess.get_UnknownProp(hvo, tag);
		}

		/// <summary>
		/// Test Time Property get, when no set has been done.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void UnkPropMisMatchedFlidTest()
		{
			CheckDisposed();

			// First, set up class id.
			int hvo = 1;
			uint clid = SilDataAccess.MetaDataCache.GetClassId("StPara");
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class, (int)clid);

			ITsPropsBldr tsPropsBuilder = TsPropsBldrClass.Create();
			ITsTextProps props = tsPropsBuilder.GetTextProps();
			int tag = (int)SilDataAccess.MetaDataCache.GetFieldId("StPara", "StyleName", false);
			SilDataAccess.SetUnknown(hvo, tag, props);
		}

		/// <summary>
		/// Test Time Property get, when no set has been done.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void UnkPropWrongInterfaceTest()
		{
			CheckDisposed();

			// First, set up class id.
			int hvo = 1;
			uint clid = SilDataAccess.MetaDataCache.GetClassId("StPara");
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class, (int)clid);

			ITsPropsBldr tsPropsBuilder = TsPropsBldrClass.Create();
			int tag = (int)SilDataAccess.MetaDataCache.GetFieldId("StPara", "StyleRules", false);
			SilDataAccess.SetUnknown(hvo, tag, tsPropsBuilder);
		}

		/// <summary>
		/// Test Binary Property get/set.
		/// </summary>
		[Test]
		public void BinaryPropTest()
		{
			CheckDisposed();

			// ScrImportSet::ImportSettings:Binary
			// Set class first, or it will throw an exception.
			int hvo = 1;
			uint clidAnal = SilDataAccess.MetaDataCache.GetClassId("ScrImportSet");
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class, (int)clidAnal);
			int tag = (int)SilDataAccess.MetaDataCache.GetFieldId("ScrImportSet", "ImportSettings", false);

			using (ArrayPtr arrayPtr = MarshalEx.ArrayToNative(3, typeof(int)))
			{
				int chvo;
				byte[] prgb = new byte[] { 3, 4, 5 };
				SilDataAccess.SetBinary(hvo, tag, prgb, prgb.Length);
				SilDataAccess.BinaryPropRgb(hvo, tag, arrayPtr, 3, out chvo);
				byte[] prgbNew = (byte[])MarshalEx.NativeToArray(arrayPtr, chvo, typeof(byte));
				Assert.AreEqual(prgb.Length, prgbNew.Length);
				for (int i = 0; i < prgbNew.Length; i++)
					Assert.AreEqual(prgb[i], prgbNew[i]);
			}
		}

		/// <summary>
		/// Test Binary Property get, when no set has been done.
		/// </summary>
		[Test]
		[ExpectedException(typeof(KeyNotFoundException))]
		public void BinaryPropKNTTest()
		{
			CheckDisposed();

			int hvo = 1;
			// Set class first, or it will throw the exception in the wrong place.
			uint clid = SilDataAccess.MetaDataCache.GetClassId("ScrImportSet");
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class, (int)clid);
			int tag = (int)SilDataAccess.MetaDataCache.GetFieldId("ScrImportSet", "ImportSettings", false);
			//using (ArrayPtr arrayPtr = MarshalEx.ArrayToNative(2, typeof(int)))
			//{
				int chvo;
				SilDataAccess.BinaryPropRgb(hvo, tag, ArrayPtr.Null, 0, out chvo);
			//	byte[] prgbNew = (byte[])MarshalEx.NativeToArray(arrayPtr, chvo, typeof(byte));
			//	Assert.AreEqual(prgb.Length, prgbNew.Length);
			//	for (int i = 0; i < prgbNew.Length; i++)
			//		Assert.AreEqual(prgb[i], prgbNew[i]);
			//}
		}

		/// <summary>
		/// Test Binary Property get, when no set has been done.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void BinaryPropWrongLengthTest()
		{
			CheckDisposed();

			int hvo = 1;
			uint clid = SilDataAccess.MetaDataCache.GetClassId("ScrImportSet");
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class, (int)clid);
			int tag = (int)SilDataAccess.MetaDataCache.GetFieldId("ScrImportSet", "ImportSettings", false);
			using (ArrayPtr arrayPtr = MarshalEx.ArrayToNative(2, typeof(int)))
			{
				byte[] prgb = new byte[] { 3, 4, 5 };
				int chvo;
				SilDataAccess.SetBinary(hvo, tag, prgb, prgb.Length);
				SilDataAccess.BinaryPropRgb(hvo, tag, arrayPtr, 2, out chvo);
			}
		}

		/// <summary>
		/// Test String Property get/set.
		/// </summary>
		[Test]
		public void StringPropTest()
		{
			CheckDisposed();

			// Set class first, or it will throw an exception.
			int hvo = 1;
			uint clid = SilDataAccess.MetaDataCache.GetClassId("PhEnvironment");
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class, (int)clid);
			int tag = (int)SilDataAccess.MetaDataCache.GetFieldId("PhEnvironment", "StringRepresentation", false);

			ITsPropsBldr propsBldr = TsPropsBldrClass.Create();
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			propsBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "Verse");
			strBldr.Replace(0, 0, "/ a _", propsBldr.GetTextProps());
			ITsString tsString = strBldr.GetString();
			SilDataAccess.SetString(hvo, tag, tsString);

			ITsString tsStringNew = SilDataAccess.get_StringProp(hvo, tag);
			Assert.AreEqual(tsString, tsStringNew);
		}

		/// <summary>
		/// Test CacheStringFields method.
		/// </summary>
		[Test]
		public void StringFieldsTest()
		{
			CheckDisposed();

			// Set class first, or it will throw an exception.
			int hvo = 1;
			uint clid = SilDataAccess.MetaDataCache.GetClassId("PhEnvironment");
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class, (int)clid);
			int tag = (int)SilDataAccess.MetaDataCache.GetFieldId("PhEnvironment", "StringRepresentation", false);

			ITsPropsBldr propsBldr = TsPropsBldrClass.Create();
			propsBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "Verse");
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			// Test StringFields (which are basically the same, except that the
			// format of the parameters is different)
			strBldr.Replace(0, 0, "Third", propsBldr.GetTextProps());
			ITsString tsString = strBldr.GetString();
			int cbFmt;
			byte[] rgbFmt;
			using (ArrayPtr arrayPtr = MarshalEx.ArrayToNative(1000, typeof(byte)))
			{
				cbFmt = tsString.SerializeFmtRgb(arrayPtr, 1000);
				rgbFmt = (byte[])MarshalEx.NativeToArray(arrayPtr, cbFmt, typeof(byte));
			}
			VwCacheDa.CacheStringFields(hvo, tag, tsString.Text,
				tsString.Length, rgbFmt, cbFmt);
			strBldr.Replace(0, 5, "Fourth", propsBldr.GetTextProps());
			tsString = strBldr.GetString();

			VwCacheDa.CacheStringFields(hvo, tag, tsString.Text,
				tsString.Length, rgbFmt, cbFmt);

			ITsString tsStringNew = SilDataAccess.get_StringProp(hvo, tag);

			Assert.AreEqual(tsString.Text, tsStringNew.Text);
		}

		/// <summary>
		/// Test String Property get, when no set has been done.
		/// </summary>
		[Test]
		[ExpectedException(typeof(KeyNotFoundException))]
		public void StringPropKNTTest()
		{
			CheckDisposed();

			// Set class first, or it will throw an exception.
			int hvo = 1;
			uint clid = SilDataAccess.MetaDataCache.GetClassId("PhEnvironment");
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class, (int)clid);
			int tag = (int)SilDataAccess.MetaDataCache.GetFieldId("PhEnvironment", "StringRepresentation", false);

			ITsString tsStringNew = SilDataAccess.get_StringProp(hvo, tag);
		}

		/// <summary>
		/// Test CacheStringFields method.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void StringPropWrongLengthTextTest()
		{
			CheckDisposed();

			// Set class first, or it will throw an exception.
			int hvo = 1;
			uint clid = SilDataAccess.MetaDataCache.GetClassId("PhEnvironment");
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class, (int)clid);
			int tag = (int)SilDataAccess.MetaDataCache.GetFieldId("PhEnvironment", "StringRepresentation", false);

			ITsPropsBldr propsBldr = TsPropsBldrClass.Create();
			propsBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "Verse");
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			// Test StringFields (which are basically the same, except that the
			// format of the parameters is different)
			strBldr.Replace(0, 0, "Third", propsBldr.GetTextProps());
			ITsString tsString = strBldr.GetString();
			int cbFmt;
			byte[] rgbFmt;
			using (ArrayPtr arrayPtr = MarshalEx.ArrayToNative(1000, typeof(byte)))
			{
				cbFmt = tsString.SerializeFmtRgb(arrayPtr, 1000);
				rgbFmt = (byte[])MarshalEx.NativeToArray(arrayPtr, cbFmt, typeof(byte));
			}
			VwCacheDa.CacheStringFields(hvo, tag, tsString.Text,
				tsString.Length - 1, rgbFmt, cbFmt);
		}

		/// <summary>
		/// Test CacheStringFields method.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void StringPropWrongLengthFmtTest()
		{
			CheckDisposed();

			// Set class first, or it will throw an exception.
			int hvo = 1;
			uint clid = SilDataAccess.MetaDataCache.GetClassId("PhEnvironment");
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class, (int)clid);
			int tag = (int)SilDataAccess.MetaDataCache.GetFieldId("PhEnvironment", "StringRepresentation", false);

			ITsPropsBldr propsBldr = TsPropsBldrClass.Create();
			propsBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "Verse");
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			// Test StringFields (which are basically the same, except that the
			// format of the parameters is different)
			strBldr.Replace(0, 0, "Third", propsBldr.GetTextProps());
			ITsString tsString = strBldr.GetString();
			int cbFmt;
			byte[] rgbFmt;
			using (ArrayPtr arrayPtr = MarshalEx.ArrayToNative(1000, typeof(byte)))
			{
				cbFmt = tsString.SerializeFmtRgb(arrayPtr, 1000);
				rgbFmt = (byte[])MarshalEx.NativeToArray(arrayPtr, cbFmt, typeof(byte));
			}
			VwCacheDa.CacheStringFields(hvo, tag, tsString.Text,
				tsString.Length, rgbFmt, cbFmt - 1);
		}

		/// <summary>
		/// Test MultiString Property get/set.
		/// </summary>
		[Test]
		public void MultiStringPropTest()
		{
			CheckDisposed();

			// Set class first, or it will throw an exception.
			int hvo = 1;
			uint clid = SilDataAccess.MetaDataCache.GetClassId("CmPossibility");
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class, (int)clid);
			int tag = (int)SilDataAccess.MetaDataCache.GetFieldId("CmPossibility", "Name", false);

			ITsStrFactory tsf = TsStrFactoryClass.Create();
			ITsString tss = tsf.MakeString("Verb", 1);
			SilDataAccess.SetMultiStringAlt(hvo, tag, 1, tss);

			ITsString tssNew = SilDataAccess.get_MultiStringAlt(hvo, tag, 1);
			Assert.AreEqual(tss, tssNew);
		}

		/// <summary>
		/// Test get_MultiStringProp method.
		/// </summary>
		[Test]
		public void AllMultiStringPropTest()
		{
			CheckDisposed();

			// Set class first, or it will throw an exception.
			int hvo = 1;
			uint clid = SilDataAccess.MetaDataCache.GetClassId("CmPossibility");
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class, (int)clid);
			int tag = (int)SilDataAccess.MetaDataCache.GetFieldId("CmPossibility", "Name", false);

			ITsStrFactory tsf = TsStrFactoryClass.Create();
			ITsString tss = tsf.MakeString("Verb", 1);
			SilDataAccess.SetMultiStringAlt(hvo, tag, 1, tss);
			tss = tsf.MakeString("Verbo", 2);
			SilDataAccess.SetMultiStringAlt(hvo, tag, 2, tss);

			ITsMultiString tsms = SilDataAccess.get_MultiStringProp(hvo, tag);
			Assert.AreEqual(tsms.StringCount, 2);
		}

		/// <summary>
		/// Test setting a Ws of zero method.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void MultiString0WSTest()
		{
			CheckDisposed();

			// Set class first, or it will throw an exception.
			int hvo = 1;
			uint clid = SilDataAccess.MetaDataCache.GetClassId("CmPossibility");
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class, (int)clid);
			int tag = (int)SilDataAccess.MetaDataCache.GetFieldId("CmPossibility", "Name", false);

			ITsStrFactory tsf = TsStrFactoryClass.Create();
			ITsString tss = tsf.MakeString("Verb", 1);
			SilDataAccess.SetMultiStringAlt(hvo, tag, 0, tss);
		}

		/// <summary>
		/// Test setting a Ws of zero method.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void MultiStringNegativeWSTest()
		{
			CheckDisposed();

			// Set class first, or it will throw an exception.
			int hvo = 1;
			uint clid = SilDataAccess.MetaDataCache.GetClassId("CmPossibility");
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class, (int)clid);
			int tag = (int)SilDataAccess.MetaDataCache.GetFieldId("CmPossibility", "Name", false);

			ITsStrFactory tsf = TsStrFactoryClass.Create();
			ITsString tss = tsf.MakeString("Verb", 1);
			SilDataAccess.SetMultiStringAlt(hvo, tag, -1, tss);
		}
	}
}
