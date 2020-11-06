// Copyright (c) 2003-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Runtime.InteropServices;
using NUnit.Framework;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.Utils;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Test the C++ VwCacheDa implementation of the IVwCacheDa interface (this should really be
	/// done in C++)
	/// </summary>
	[TestFixture]
	public class IVwCacheDaCppTests
	{
		// NB: m_ISilDataAccess and m_IVwCacheDa are exactly the same object.
		// they could be C# or C++, depending on if the main is is IVwCacheDaCppTests
		// or IVwCacheDaCSharpTests, however.
		/// <summary>The ISilDataAccess object</summary>
		protected ISilDataAccess m_ISilDataAccess;
		/// <summary>The IVwCacheDa object</summary>
		protected IVwCacheDa m_IVwCacheDa;

		/// <summary>
		/// Setup done before each test.
		/// </summary>
		[SetUp]
		public void TestSetup()
		{
			var cda = VwCacheDaClass.Create();
			cda.TsStrFactory = TsStringUtils.TsStrFactory;
			m_ISilDataAccess = cda;
			ILgWritingSystemFactory wsf = new WritingSystemManager();
			m_ISilDataAccess.WritingSystemFactory = wsf;
			m_IVwCacheDa = cda;
		}

		/// <summary />
		[TearDown]
		public void TestTeardown()
		{
		}

		/// <summary>
		/// Test setting/getting an object property
		/// </summary>
		[Test]
		public void ObjectProp()
		{
			var hvo = m_ISilDataAccess.get_ObjectProp(1000, 2000);
			Assert.AreEqual(0, hvo);

			m_IVwCacheDa.CacheObjProp(1000, 2000, 7777);
			hvo = m_ISilDataAccess.get_ObjectProp(1000, 2000);
			Assert.AreEqual(7777, hvo);

			m_IVwCacheDa.CacheObjProp(1000, 2000, 8888);
			hvo = m_ISilDataAccess.get_ObjectProp(1000, 2000);
			Assert.AreEqual(8888, hvo);
		}

		/// <summary>
		/// Test getting/setting a vector of hvos
		/// </summary>
		[Test]
		public void VecProp()
		{
			// test VecProp
			using (var arrayPtr = MarshalEx.ArrayToNative<int>(10))
			{
				m_ISilDataAccess.VecProp(1001, 2001, 10, out var chvo, arrayPtr);
				Assert.AreEqual(0, chvo);

				chvo = m_ISilDataAccess.get_VecSize(1001, 2001);
				Assert.AreEqual(0, chvo);

				var rgHvo = new[] { 33, 44, 55 };
				m_IVwCacheDa.CacheVecProp(1001, 2001, rgHvo, rgHvo.Length);
				m_ISilDataAccess.VecProp(1001, 2001, 10, out chvo, arrayPtr);
				var rgHvoNew = MarshalEx.NativeToArray<int>(arrayPtr, chvo);
				Assert.AreEqual(rgHvo.Length, rgHvoNew.Length);
				for (var i = 0; i < rgHvoNew.Length; i++)
				{
					Assert.AreEqual(rgHvo[i], rgHvoNew[i]);
				}

				var rgHvo2 = new[] { 66, 77, 88, 99 };
				m_IVwCacheDa.CacheVecProp(1001, 2001, rgHvo2, rgHvo2.Length);
				m_ISilDataAccess.VecProp(1001, 2001, 10, out chvo, arrayPtr);
				rgHvoNew = MarshalEx.NativeToArray<int>(arrayPtr, chvo);
				Assert.AreEqual(rgHvo2.Length, rgHvoNew.Length);
				for (var i = 0; i < rgHvoNew.Length; i++)
				{
					Assert.AreEqual(rgHvo2[i], rgHvoNew[i]);
				}

				Exception ex = null;
				try
				{
					m_ISilDataAccess.VecProp(1001, 2001, 2, out chvo, arrayPtr);
				}
				catch (Exception e)
				{
					ex = e;
				}
				Assert.IsNotNull(ex);
				Assert.AreEqual(typeof(ArgumentException), ex.GetType());

				// test VecItem
				var hvo = m_ISilDataAccess.get_VecItem(1001, 2001, 2);
				Assert.AreEqual(88, hvo);

				ex = null;
				try
				{
					m_ISilDataAccess.get_VecItem(1001, 2001, 10);
				}
				catch (Exception e)
				{
					ex = e;
				}
				Assert.IsNotNull(ex);
				Assert.AreEqual(typeof(ArgumentException), ex.GetType());

				// test Vector size
				chvo = m_ISilDataAccess.get_VecSize(1001, 2001);
				Assert.AreEqual(rgHvo2.Length, chvo);
			}
		}

		/// <summary>
		/// Test getting/setting binary data
		/// </summary>
		[Test]
		public void BinaryProp()
		{
			using (var arrayPtr = MarshalEx.ArrayToNative<int>(10))
			{
				m_ISilDataAccess.BinaryPropRgb(1112, 2221, ArrayPtr.Null, 0, out var chvo);
				Assert.AreEqual(0, chvo);

				var prgb = new byte[] { 3, 4, 5 };
				m_IVwCacheDa.CacheBinaryProp(1112, 2221, prgb, prgb.Length);
				m_ISilDataAccess.BinaryPropRgb(1112, 2221, arrayPtr, 10, out chvo);
				var prgbNew = MarshalEx.NativeToArray<byte>(arrayPtr, chvo);
				Assert.AreEqual(prgb.Length, prgbNew.Length);
				for (var i = 0; i < prgbNew.Length; i++)
				{
					Assert.AreEqual(prgb[i], prgbNew[i]);
				}

				var prgb2 = new byte[] { 6, 7, 8, 9 };
				m_IVwCacheDa.CacheBinaryProp(1112, 2221, prgb2, prgb2.Length);
				m_ISilDataAccess.BinaryPropRgb(1112, 2221, arrayPtr, 10, out chvo);
				prgbNew = MarshalEx.NativeToArray<byte>(arrayPtr, chvo);
				Assert.AreEqual(prgb2.Length, prgbNew.Length);
				for (var i = 0; i < prgbNew.Length; i++)
				{
					Assert.AreEqual(prgb2[i], prgbNew[i]);
				}
			}
		}

		/// <summary>
		/// Test getting/setting binary data
		/// </summary>
		[Test]
		public void BinaryProp_BufferToSmall()
		{
			var prgb2 = new byte[] { 6, 7, 8, 9 };
			m_IVwCacheDa.CacheBinaryProp(1112, 2221, prgb2, prgb2.Length);
			using (var arrayPtr = MarshalEx.ArrayToNative<int>(10))
			{
				Assert.That(() => m_ISilDataAccess.BinaryPropRgb(1112, 2221, arrayPtr, 2, out _),
					Throws.Exception.TypeOf<COMException>());
			}
		}

		/// <summary>
		/// Tests getting/setting a Guid property
		/// </summary>
		[Test]
		public void GuidProp()
		{
			var guidNew = m_ISilDataAccess.get_GuidProp(1113, 2223);
			Assert.AreEqual(Guid.Empty, guidNew);

			var guid = new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11);
			m_IVwCacheDa.CacheGuidProp(1113, 2223, guid);
			guidNew = m_ISilDataAccess.get_GuidProp(1113, 2223);
			Assert.AreEqual(guid, guidNew);

			var guid2 = new Guid(10, 12, 13, 14, 15, 16, 17, 18, 19, 110, 111);
			m_IVwCacheDa.CacheGuidProp(1113, 2223, guid2);
			guidNew = m_ISilDataAccess.get_GuidProp(1113, 2223);
			Assert.AreEqual(guid2, guidNew);
		}

		/// <summary>
		/// Tests getting/setting a Int64 property
		/// </summary>
		[Test]
		public void Int64Prop()
		{
			var valNew = m_ISilDataAccess.get_Int64Prop(1114, 2224);
			Assert.AreEqual(0, valNew);

			m_IVwCacheDa.CacheInt64Prop(1114, 2224, long.MaxValue);
			valNew = m_ISilDataAccess.get_Int64Prop(1114, 2224);
			Assert.AreEqual(long.MaxValue, valNew);

			m_IVwCacheDa.CacheInt64Prop(1114, 2224, long.MinValue);
			valNew = m_ISilDataAccess.get_Int64Prop(1114, 2224);
			Assert.AreEqual(long.MinValue, valNew);
		}

		/// <summary>
		/// Tests getting/setting a int property
		/// </summary>
		[Test]
		public void IntProp()
		{
			var valNew = m_ISilDataAccess.get_IntProp(1115, 2225);
			Assert.AreEqual(0, valNew);

			valNew = m_IVwCacheDa.get_CachedIntProp(1115, 2225, out var f);
			Assert.AreEqual(false, f);
			Assert.AreEqual(0, valNew);

			m_IVwCacheDa.CacheIntProp(1115, 2225, int.MaxValue);
			valNew = m_ISilDataAccess.get_IntProp(1115, 2225);
			Assert.AreEqual(int.MaxValue, valNew);
			valNew = m_IVwCacheDa.get_CachedIntProp(1115, 2225, out f);
			Assert.AreEqual(true, f);
			Assert.AreEqual(int.MaxValue, valNew);

			m_IVwCacheDa.CacheIntProp(1115, 2225, int.MinValue);
			valNew = m_ISilDataAccess.get_IntProp(1115, 2225);
			Assert.AreEqual(int.MinValue, valNew);
			valNew = m_IVwCacheDa.get_CachedIntProp(1115, 2225, out f);
			Assert.AreEqual(true, f);
			Assert.AreEqual(int.MinValue, valNew);
		}

		/// <summary>
		/// Tests getting/setting a Time property
		/// </summary>
		[Test]
		public void TimeProp()
		{
			var valNew = m_ISilDataAccess.get_TimeProp(1116, 2226);
			Assert.AreEqual(0, valNew);

			m_IVwCacheDa.CacheTimeProp(1116, 2226, DateTime.MaxValue.Ticks);
			valNew = m_ISilDataAccess.get_TimeProp(1116, 2226);
			Assert.AreEqual(DateTime.MaxValue.Ticks, valNew);

			m_IVwCacheDa.CacheTimeProp(1116, 2226, DateTime.MinValue.Ticks);
			valNew = m_ISilDataAccess.get_TimeProp(1116, 2226);
			Assert.AreEqual(DateTime.MinValue.Ticks, valNew);
		}

		/// <summary>
		/// Tests getting/setting the unicode prop
		/// </summary>
		[Test]
		public void UnicodeProp()
		{
			var strNew = m_ISilDataAccess.get_UnicodeProp(1119, 2229);
			Assert.IsNull(strNew);

			var str = "UnicodeTest";
			m_IVwCacheDa.CacheUnicodeProp(1119, 2229, str, str.Length);
			strNew = m_ISilDataAccess.get_UnicodeProp(1119, 2229);
			Assert.AreEqual(str, strNew);

			str = "SecondUnicodeTest";
			m_IVwCacheDa.CacheUnicodeProp(1119, 2229, str, str.Length);
			strNew = m_ISilDataAccess.get_UnicodeProp(1119, 2229);
			Assert.AreEqual(str, strNew);
		}

		/// <summary>
		/// Tests getting/setting the unknown property
		/// </summary>
		[Test]
		public void UnknownProp()
		{
			var obj = m_ISilDataAccess.get_UnknownProp(1120, 2220);
			Assert.IsNull(obj);

			var propsBldr = TsStringUtils.MakePropsBldr();
			var ttp = propsBldr.GetTextProps();
			m_IVwCacheDa.CacheUnknown(1120, 2220, ttp);
			obj = m_ISilDataAccess.get_UnknownProp(1120, 2220);
			Assert.AreEqual(ttp, obj);
		}

		/// <summary>
		/// Test that the cache does not cache guid properties (in its internal hash table of
		/// object guids) that are not object guids. To do this, two guid properties are cached
		/// with one being specified as an object guid (i.e. CmObjectFields.kflidCmObject_Guid)
		/// and the other being specified as not an object guid (i.e. dummyFlid).
		/// </summary>
		[Test]
		public void TestCacheGuidProp_ForNonCmObjectGuid()
		{
			const int objFlid = (int)CmObjectFields.kflidCmObject_Guid;
			const int nonObjFlid = 9005; //CmFilterTags.kflidApp
			const int objHvo1 = 1124;
			const int objHvo2 = 1125;
			var guid = Guid.NewGuid();

			// Cache the guids in this order. When this test failed, caching the
			// guid for the hvo objHvo2 clobbered the cached guid for object objHvo1
			// so there was no longer longer a guid in the cache for object objHvo1.
			m_IVwCacheDa.CacheGuidProp(objHvo1, objFlid, guid);
			m_IVwCacheDa.CacheGuidProp(objHvo2, nonObjFlid, guid);

			// Make sure the correct hvo is returned when
			// trying to create an object from the guid.
			Assert.AreEqual(objHvo1, m_ISilDataAccess.get_ObjFromGuid(guid));

			m_IVwCacheDa.ClearAllData();

			// Now cache the guids in the reverse order from the order done above.
			m_IVwCacheDa.CacheGuidProp(objHvo2, nonObjFlid, guid);
			m_IVwCacheDa.CacheGuidProp(objHvo1, objFlid, guid);

			// Make sure the same flid is returned when the caching is reversed.
			Assert.AreEqual(objHvo1, m_ISilDataAccess.get_ObjFromGuid(guid));
		}

		/// <summary>
		/// Test that the cache does not save guid properties in its internal hash table of
		/// object guids when the guid property is not an object guid. To do this, two guid
		/// properties are sent to the cache using the SetGuid method. One is an object guid
		/// (i.e. CmObjectFields.kflidCmObject_Guid) and the other is not (i.e. dummyFlid).
		/// </summary>
		[Test]
		public void TestSetGuid_ForNonCmObjectGuid()
		{
			const int objFlid = (int)CmObjectFields.kflidCmObject_Guid;
			const int nonObjFlid = 9005; //CmFilterTags.kflidApp
			const int objHvo1 = 1124;
			const int objHvo2 = 1125;
			var guid = Guid.NewGuid();

			// Save the guids. When this test failed, the saved guid for object objHvo2
			// clobbered the saved guid for object objHvo1 so there was no longer longer
			// a guid in the object cache for object objHvo1.
			m_ISilDataAccess.SetGuid(objHvo1, objFlid, guid);
			m_ISilDataAccess.SetGuid(objHvo2, nonObjFlid, guid);

			// Make sure the correct hvo is returned when
			// trying to create an object from the guid.
			Assert.AreEqual(objHvo1, m_ISilDataAccess.get_ObjFromGuid(guid));

			m_IVwCacheDa.ClearAllData();

			// Now save the guids in the reverse order from the order saved above.
			m_ISilDataAccess.SetGuid(objHvo2, nonObjFlid, guid);
			m_ISilDataAccess.SetGuid(objHvo1, objFlid, guid);

			// Make sure the same flid is returned when the saving is reversed.
			Assert.AreEqual(objHvo1, m_ISilDataAccess.get_ObjFromGuid(guid));
		}

		/// <summary>
		/// Test that the cache does not remove guid properties (from its internal
		/// hash table of object guids) that are not object guids.
		/// </summary>
		[Test]
		public void TestRemoveObjRef_ForNonCmObjectGuid()
		{
			const int objFlid = (int)CmObjectFields.kflidCmObject_Guid;
			const int nonObjFlid = 9005; //CmFilterTags.kflidApp
			const int objHvo1 = 1124;
			const int objHvo2 = 1125;
			var guid = Guid.NewGuid();

			// Cache the guids in this order. When this test failed, caching
			// the guid for objHvo2 clobbered the cached guid for object objHvo1
			// so there was no longer longer a guid in the cache for object objHvo1.
			m_IVwCacheDa.CacheGuidProp(objHvo1, objFlid, guid);
			m_IVwCacheDa.CacheGuidProp(objHvo2, nonObjFlid, guid);

			// Remove the object reference for objHvo2 and make sure it doesn't
			// remove the ability to get the object for objHvo1 using the same guid
			// that was a property for object objHvo2.
			m_ISilDataAccess.RemoveObjRefs(objHvo2);
			Assert.AreEqual(objHvo1, m_ISilDataAccess.get_ObjFromGuid(guid));
		}
	}
}