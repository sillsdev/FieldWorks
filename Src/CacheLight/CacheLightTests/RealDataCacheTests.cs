// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic; // Needed for KeyNotFoundException.
using System.IO;
using NUnit.Framework;
using SIL.FieldWorks.CacheLight;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Test.TestUtils;
using SIL.Utils;
using System.Text;
using SIL.CoreImpl;

namespace SIL.FieldWorks.CacheLightTests
{
	/// <summary>
	/// Tests the two main interfaces (ISilDataAccess and IVwCacheDa) for RealDataCache.
	/// </summary>
	public class RealDataCacheBase
	{
		/// <summary></summary>
		private RealDataCache m_realDataCache;

		/// <summary>
		///
		/// </summary>
		protected ISilDataAccess SilDataAccess
		{
			get { return m_realDataCache; }
		}

		/// <summary>
		///
		/// </summary>
		protected IVwCacheDa VwCacheDa
		{
			get { return m_realDataCache; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If a test overrides this, it should call this base implementation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public virtual void FixtureSetup()
		{
			FileUtils.Manager.SetFileAdapter(new MockFileOS());
			using (TextWriter fw = FileUtils.OpenFileForWrite("TestModel.xsd", Encoding.UTF8))
				fw.Write(Properties.Resources.TestModel_xsd);
			using (TextWriter fw = FileUtils.OpenFileForWrite("TestModel.xml", Encoding.UTF8))
				fw.Write(Properties.Resources.TestModel_xml);

			m_realDataCache = new RealDataCache
			{
				MetaDataCache = MetaDataCache.CreateMetaDataCache("TestModel.xml")
			};
		}

		/// <summary/>
		[TestFixtureTearDown]
		public virtual void FixtureTearDown()
		{
			FileUtils.Manager.Reset();
					m_realDataCache.Dispose();
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
			// Set class first, or it will throw an exception.
			const int hvo = 1;
			var clidAnal = SilDataAccess.MetaDataCache.GetClassId("ClassA");
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class, clidAnal);
			// Set class of POS object.
			const int hvoObj = 3;
			var clidPOS = SilDataAccess.MetaDataCache.GetClassId("ClassB");
			SilDataAccess.SetInt(hvoObj, (int)CmObjectFields.kflidCmObject_Class, clidPOS);

			// Now set its 'Prop1' property.
			var tag = SilDataAccess.MetaDataCache.GetFieldId("ClassA", "Prop1", false);
			SilDataAccess.SetObjProp(hvo, tag, hvoObj);
			var hvoObj2 = SilDataAccess.get_ObjectProp(hvo, tag);
			Assert.AreEqual(hvoObj, hvoObj2, "Wrong hvoObj in cache.");
		}
		/// <summary>
		/// Test Int Property get, when no set has been done.
		/// </summary>
		[Test]
		[ExpectedException(typeof(KeyNotFoundException))]
		public void ObjPropKNTTest()
		{
			const int hvo = 1;
			// Set class first, or it will throw the exception in the wrong place.
			var clid = SilDataAccess.MetaDataCache.GetClassId("ClassC");
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class, clid);

			SilDataAccess.get_ObjectProp(hvo, (int)CmObjectFields.kflidCmObject_Owner);
		}

		/// <summary>
		/// Test Int Property get/set.
		/// </summary>
		[Test]
		public void IntPropTest()
		{
			const int hvo = 1;
			var clid = SilDataAccess.MetaDataCache.GetClassId("ClassB");
			const int tag = (int)CmObjectFields.kflidCmObject_Class;
			SilDataAccess.SetInt(hvo, tag, clid);
			var clid1 = SilDataAccess.get_IntProp(hvo, tag);
			Assert.AreEqual(clid, clid1, "Wrong clid in cache.");

			// See if the int is there via another method.
			// It should be there.
			bool isInCache;
			var clid2 = VwCacheDa.get_CachedIntProp(hvo, tag, out isInCache);
			Assert.IsTrue(isInCache, "Int not in cache.");
			Assert.AreEqual(clid1, clid2, "Clids are not the same.");

			// See if the int is there via another method.
			// It should not be there.
			var ownerHvo = VwCacheDa.get_CachedIntProp(hvo, (int)CmObjectFields.kflidCmObject_Owner, out isInCache);
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
			const int hvo = 1;
			// Set class first, or it will throw the exception in the wrong place.
			var clid = SilDataAccess.MetaDataCache.GetClassId("ClassC");
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class, clid);

			var tag = SilDataAccess.MetaDataCache.GetFieldId("ClassC", "IntProp2", false);
			SilDataAccess.get_IntProp(hvo, tag);
		}

		/// <summary>
		/// Test Guid Property get/set.
		/// </summary>
		[Test]
		public void GuidPropTest()
		{
			// Set class first, or it will throw an exception.
			const int hvo = 1;
			var clidAnal = SilDataAccess.MetaDataCache.GetClassId("ClassA");
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class, clidAnal);

			// Now set its 'Guid' property.
			const int tag = (int)CmObjectFields.kflidCmObject_Guid;
			var uid = Guid.NewGuid();
			SilDataAccess.SetGuid(hvo, tag, uid);
			var uid2 = SilDataAccess.get_GuidProp(hvo, tag);
			Assert.AreEqual(uid, uid2, "Wrong uid in cache.");

			// Test the reverse method.
			var hvo2 = SilDataAccess.get_ObjFromGuid(uid2);
			Assert.AreEqual(hvo, hvo2, "Wrong hvo in cache for Guid.");
		}
		/// <summary>
		/// Test Guid Property get, when no set has been done.
		/// </summary>
		[Test]
		[ExpectedException(typeof(KeyNotFoundException))]
		public void GuidPropKNTTest()
		{
			const int hvo = 1;
			// Set class first, or it will throw the exception in the wrong place.
			var clidLe = SilDataAccess.MetaDataCache.GetClassId("ClassD");
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class, clidLe);
			const int tag = (int)CmObjectFields.kflidCmObject_Guid;

			SilDataAccess.get_GuidProp(hvo, tag);
		}

		/// <summary>
		/// Test Bool Property get/set.
		/// </summary>
		[Test]
		public void BoolPropTest()
		{
			// Set class first, or it will throw an exception.
			const int hvo = 1;
			var clidLe = SilDataAccess.MetaDataCache.GetClassId("ClassD");
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class, clidLe);

			// Now set its 'BoolProp3' property.
			var tag = SilDataAccess.MetaDataCache.GetFieldId("ClassD", "BoolProp3", false);
			const bool excludeOriginal = true;
			SilDataAccess.SetBoolean(hvo, tag, excludeOriginal);
			var excludeOriginal1 = SilDataAccess.get_BooleanProp(hvo, tag);
			Assert.AreEqual(excludeOriginal, excludeOriginal1, "Wrong bool in cache.");
		}
		/// <summary>
		/// Test Guid Property get, when no set has been done.
		/// </summary>
		[Test]
		[ExpectedException(typeof(KeyNotFoundException))]
		public void BoolPropKNTTest()
		{
			const int hvo = 1;
			// Set class first, or it will throw the exception in the wrong place.
			var clidLe = SilDataAccess.MetaDataCache.GetClassId("ClassA");
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class, clidLe);
			var tag = SilDataAccess.MetaDataCache.GetFieldId("ClassA", "Prop1", false);
			SilDataAccess.get_BooleanProp(hvo, tag);
		}

		/// <summary>
		/// Test Unicode Property get/set.
		/// </summary>
		[Test]
		public void UnicodePropTest()
		{
			// Set class first, or it will throw an exception.
			const int hvo = 1;
			var clidAnal = SilDataAccess.MetaDataCache.GetClassId("ClassE");
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class, clidAnal);

			// Set its 'UnicodeProp4' property, using the 'BSTR' method.
			var tag = SilDataAccess.MetaDataCache.GetFieldId("ClassE", "UnicodeProp4", false);
			const string ec = "ZPI";
			SilDataAccess.set_UnicodeProp(hvo, tag, ec);
			var ec2 = SilDataAccess.get_UnicodeProp(hvo, tag);
			Assert.AreEqual(ec, ec2, "Wrong Unicode string in cache.");

			// Set its 'UnicodeProp4' property, using non-bstr method.
			const string ecNew = "ZPR";
			SilDataAccess.SetUnicode(hvo, tag, ecNew, ecNew.Length);
			int len;
			SilDataAccess.UnicodePropRgch(hvo, tag, null, 0, out len);
			Assert.AreEqual(ecNew.Length, len);
			using (var arrayPtr = MarshalEx.StringToNative(len + 1, true))
			{
				int cch;
				SilDataAccess.UnicodePropRgch(hvo, tag, arrayPtr, len + 1, out cch);
				var ecNew2 = MarshalEx.NativeToString(arrayPtr, cch, true);
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
			const int hvo = 1;
			// Set class first, or it will throw the exception in the wrong place.
			var clid = SilDataAccess.MetaDataCache.GetClassId("ClassE");
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class, clid);

			var tag = SilDataAccess.MetaDataCache.GetFieldId("ClassE", "UnicodeProp4", false);
			SilDataAccess.get_UnicodeProp(hvo, tag);
		}

		/// <summary>
		/// Test Unicode Property get, when no set has been done.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void UnicodePropWrongLengthTest()
		{
			// Set class first, or it will throw an exception.
			const int hvo = 1;
			var clidAnal = SilDataAccess.MetaDataCache.GetClassId("ClassE");
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class, clidAnal);

			// Set its 'UnicodeProp4' property, using the 'BSTR' method.
			var tag = SilDataAccess.MetaDataCache.GetFieldId("ClassE", "UnicodeProp4", false);
			const string ec = "ZPI";
			SilDataAccess.set_UnicodeProp(hvo, tag, ec);
			var ec2 = SilDataAccess.get_UnicodeProp(hvo, tag);
			Assert.AreEqual(ec, ec2, "Wrong Unicode string in cache.");

			// Set its 'UnicodeProp4' property, using non-bstr method.
			const string ecNew = "ZPR";
			SilDataAccess.SetUnicode(hvo, tag, ecNew, ecNew.Length);
			int len;
			SilDataAccess.UnicodePropRgch(hvo, tag, null, 0, out len);
			Assert.AreEqual(ecNew.Length, len);
			using (var arrayPtr = MarshalEx.StringToNative(len, true))
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
			// ClassF->Int64Prop5:GenDate
			// First, set up class id.
			const int hvo = 1;
			var clid = SilDataAccess.MetaDataCache.GetClassId("ClassF");
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class, clid);

			const long dob = long.MinValue;
			var tag = SilDataAccess.MetaDataCache.GetFieldId("ClassF", "Int64Prop5", false);
			SilDataAccess.SetInt64(hvo, tag, dob);
			var dob2 = SilDataAccess.get_Int64Prop(hvo, tag);
			Assert.AreEqual(dob, dob2, "Wrong DOB in cache.");
		}
		/// <summary>
		/// Test In64 Property get, when no set has been done.
		/// </summary>
		[Test]
		[ExpectedException(typeof(KeyNotFoundException))]
		public void In64PropKNTTest()
		{
			const int hvo = 1;
			// Set class first, or it will throw the exception in the wrong place.
			var clidLe = SilDataAccess.MetaDataCache.GetClassId("ClassF");
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class, clidLe);
			var tag = SilDataAccess.MetaDataCache.GetFieldId("ClassF", "Int64Prop5", false);
			SilDataAccess.get_Int64Prop(hvo, tag);
		}

		/// <summary>
		/// Test Time Property get/set.
		/// </summary>
		[Test]
		public void TimePropTest()
		{
			// ClassD->TimeProp6
			// First, set up class id.
			const int hvo = 2;
			var clid = SilDataAccess.MetaDataCache.GetClassId("ClassD");
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class, clid);

			const long doc = long.MinValue;
			var tag = SilDataAccess.MetaDataCache.GetFieldId("ClassD", "TimeProp6", false);
			SilDataAccess.SetTime(hvo, tag, doc);
			var doc2 = SilDataAccess.get_TimeProp(hvo, tag);
			Assert.AreEqual(doc, doc2, "Wrong creation in cache.");
		}
		/// <summary>
		/// Test Time Property get, when no set has been done.
		/// </summary>
		[Test]
		[ExpectedException(typeof(KeyNotFoundException))]
		public void TimePropKNTTest()
		{
			const int hvo = 1;
			// Set class first, or it will throw the exception in the wrong place.
			var clid = SilDataAccess.MetaDataCache.GetClassId("ClassD");
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class, clid);

			var tag = SilDataAccess.MetaDataCache.GetFieldId("ClassD", "TimeProp6", false);
			SilDataAccess.get_TimeProp(hvo, tag);
		}

		/// <summary>
		/// Test IUnknown Property get/set.
		/// </summary>
		[Test]
		public void UnkPropTest()
		{
			// First, set up class id.
			const int hvo = 1;
			var clid = SilDataAccess.MetaDataCache.GetClassId("ClassG");
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class, clid);

			var tsPropsBuilder = TsPropsBldrClass.Create();
			var props = tsPropsBuilder.GetTextProps();
			var tag = SilDataAccess.MetaDataCache.GetFieldId("ClassG", "TextPropsProp7", false);
			SilDataAccess.SetUnknown(hvo, tag, props);
			var props2 = (ITsTextProps)SilDataAccess.get_UnknownProp(hvo, tag);
			Assert.AreEqual(props, props2, "Wrong text props in cache.");
		}

		/// <summary>
		/// Test Time Property get, when no set has been done.
		/// </summary>
		[Test]
		[ExpectedException(typeof(KeyNotFoundException))]
		public void UnkPropKNTTest()
		{
			const int hvo = 1;
			// Set class first, or it will throw the exception in the wrong place.
			var clid = SilDataAccess.MetaDataCache.GetClassId("ClassG");
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class, clid);

			var tag = SilDataAccess.MetaDataCache.GetFieldId("ClassG", "TextPropsProp7", false);
			SilDataAccess.get_UnknownProp(hvo, tag);
		}

		/// <summary>
		/// Test Time Property get, when no set has been done.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void UnkPropMisMatchedFlidTest()
		{
			// First, set up class id.
			const int hvo = 1;
			var clid = SilDataAccess.MetaDataCache.GetClassId("ClassE");
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class, clid);

			var tsPropsBuilder = TsPropsBldrClass.Create();
			var props = tsPropsBuilder.GetTextProps();
			var tag = SilDataAccess.MetaDataCache.GetFieldId("ClassE", "UnicodeProp4", false);
			SilDataAccess.SetUnknown(hvo, tag, props);
		}

		/// <summary>
		/// Test Time Property get, when no set has been done.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void UnkPropWrongInterfaceTest()
		{
			// First, set up class id.
			const int hvo = 1;
			var clid = SilDataAccess.MetaDataCache.GetClassId("ClassG");
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class, clid);

			var tsPropsBuilder = TsPropsBldrClass.Create();
			var tag = SilDataAccess.MetaDataCache.GetFieldId("ClassG", "TextPropsProp7", false);
			SilDataAccess.SetUnknown(hvo, tag, tsPropsBuilder);
		}

		/// <summary>
		/// Test Binary Property get/set.
		/// </summary>
		[Test]
		public void BinaryPropTest()
		{
			// ClassH::BinaryProp8:Binary
			// Set class first, or it will throw an exception.
			const int hvo = 1;
			var clidAnal = SilDataAccess.MetaDataCache.GetClassId("ClassH");
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class, clidAnal);
			var tag = SilDataAccess.MetaDataCache.GetFieldId("ClassH", "BinaryProp8", false);

			using (var arrayPtr = MarshalEx.ArrayToNative<int>(3))
			{
				int chvo;
				var prgb = new byte[] { 3, 4, 5 };
				SilDataAccess.SetBinary(hvo, tag, prgb, prgb.Length);
				SilDataAccess.BinaryPropRgb(hvo, tag, arrayPtr, 3, out chvo);
				var prgbNew = MarshalEx.NativeToArray<byte>(arrayPtr, chvo);
				Assert.AreEqual(prgb.Length, prgbNew.Length);
				for (var i = 0; i < prgbNew.Length; i++)
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
			const int hvo = 1;
			// Set class first, or it will throw the exception in the wrong place.
			var clid = SilDataAccess.MetaDataCache.GetClassId("ClassI");
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class, clid);
			var tag = SilDataAccess.MetaDataCache.GetFieldId("ClassI", "BinaryProp9", false);
			int chvo;
			SilDataAccess.BinaryPropRgb(hvo, tag, ArrayPtr.Null, 0, out chvo);
		}

		/// <summary>
		/// Test Binary Property get, when no set has been done.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void BinaryPropWrongLengthTest()
		{
			const int hvo = 1;
			var clid = SilDataAccess.MetaDataCache.GetClassId("ClassI");
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class, clid);
			var tag = SilDataAccess.MetaDataCache.GetFieldId("ClassI", "BinaryProp9", false);
			using (var arrayPtr = MarshalEx.ArrayToNative<int>(2))
			{
				var prgb = new byte[] { 3, 4, 5 };
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
			// Set class first, or it will throw an exception.
			const int hvo = 1;
			var clid = SilDataAccess.MetaDataCache.GetClassId("ClassJ");
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class, clid);
			var tag = SilDataAccess.MetaDataCache.GetFieldId("ClassJ", "StringProp10", false);

			var tsString = TsStringUtils.MakeTss("/ a _", 42, "Verse");
			SilDataAccess.SetString(hvo, tag, tsString);

			var tsStringNew = SilDataAccess.get_StringProp(hvo, tag);
			Assert.AreEqual(tsString, tsStringNew);
		}

		/// <summary>
		/// Test String Property get, when no set has been done.
		/// </summary>
		[Test]
		[ExpectedException(typeof(KeyNotFoundException))]
		public void StringPropKNTTest()
		{
			// Set class first, or it will throw an exception.
			const int hvo = 1;
			var clid = SilDataAccess.MetaDataCache.GetClassId("ClassJ");
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class, clid);
			var tag = SilDataAccess.MetaDataCache.GetFieldId("ClassJ", "StringProp10", false);
			SilDataAccess.get_StringProp(hvo, tag);
		}

		/// <summary>
		/// Test MultiString Property get/set.
		/// </summary>
		[Test]
		public void MultiStringPropTest()
		{
			// Set class first, or it will throw an exception.
			const int hvo = 1;
			var clid = SilDataAccess.MetaDataCache.GetClassId("ClassK");
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class, clid);
			var tag = SilDataAccess.MetaDataCache.GetFieldId("ClassK", "MultiStringProp11", false);

			var tsf = TsStrFactoryClass.Create();
			var tss = tsf.MakeString("Verb", 1);
			SilDataAccess.SetMultiStringAlt(hvo, tag, 1, tss);

			var tssNew = SilDataAccess.get_MultiStringAlt(hvo, tag, 1);
			Assert.AreEqual(tss, tssNew);
		}

		/// <summary>
		/// Test get_MultiStringProp method.
		/// </summary>
		[Test]
		public void AllMultiStringPropTest()
		{
			// Set class first, or it will throw an exception.
			const int hvo = 1;
			var clid = SilDataAccess.MetaDataCache.GetClassId("ClassK");
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class, clid);
			var tag = SilDataAccess.MetaDataCache.GetFieldId("ClassK", "MultiStringProp11", false);

			var tsf = TsStrFactoryClass.Create();
			var tss = tsf.MakeString("Verb", 1);
			SilDataAccess.SetMultiStringAlt(hvo, tag, 1, tss);
			tss = tsf.MakeString("Verbo", 2);
			SilDataAccess.SetMultiStringAlt(hvo, tag, 2, tss);

			var tsms = SilDataAccess.get_MultiStringProp(hvo, tag);
			Assert.AreEqual(tsms.StringCount, 2);

			// TsMultiString's are required to be created and released by same thread
			System.Runtime.InteropServices.Marshal.ReleaseComObject(tsms);
		}

		/// <summary>
		/// Test setting a Ws of zero method.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void MultiString0WSTest()
		{
			// Set class first, or it will throw an exception.
			const int hvo = 1;
			var clid = SilDataAccess.MetaDataCache.GetClassId("ClassK");
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class, clid);
			var tag = SilDataAccess.MetaDataCache.GetFieldId("ClassK", "MultiStringProp11", false);

			var tsf = TsStrFactoryClass.Create();
			var tss = tsf.MakeString("Verb", 1);
			SilDataAccess.SetMultiStringAlt(hvo, tag, 0, tss);
		}

		/// <summary>
		/// Test setting a Ws of zero method.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void MultiStringNegativeWSTest()
		{
			// Set class first, or it will throw an exception.
			const int hvo = 1;
			var clid = SilDataAccess.MetaDataCache.GetClassId("ClassK");
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class, clid);
			var tag = SilDataAccess.MetaDataCache.GetFieldId("ClassK", "MultiStringProp11", false);

			var tsf = TsStrFactoryClass.Create();
			var tss = tsf.MakeString("Verb", 1);
			SilDataAccess.SetMultiStringAlt(hvo, tag, -1, tss);
		}

		/// <summary>
		/// Test the MakeNewObject method when creating an unowned object
		/// </summary>
		[Test]
		public void MakeNewObjectTest_UnownedObject()
		{
			int hvoNew = SilDataAccess.MakeNewObject(1, 0, -1, 0);
			Assert.IsTrue(SilDataAccess.get_IsValidObject(hvoNew));
			Assert.AreEqual(1, SilDataAccess.get_ObjectProp(hvoNew, (int)CmObjectFields.kflidCmObject_Class));
		}

		/// <summary>
		/// Test the MakeNewObject method when creating an atomic owned object
		/// </summary>
		[Test]
		public void MakeNewObjectTest_OwnedObjectAtomic()
		{
			int hvoOwner = SilDataAccess.MakeNewObject(1, 0, -1, 0);
			var clid = SilDataAccess.MetaDataCache.GetClassId("ClassA");
			var flid = SilDataAccess.MetaDataCache.GetFieldId2(1, "AtomicProp97", false);
			int hvoNew = SilDataAccess.MakeNewObject(clid, hvoOwner, flid, -2);
			Assert.IsTrue(SilDataAccess.get_IsValidObject(hvoNew));
			Assert.AreEqual(flid, SilDataAccess.get_ObjectProp(hvoNew, (int)CmObjectFields.kflidCmObject_OwnFlid));
			Assert.AreEqual(hvoOwner, SilDataAccess.get_ObjectProp(hvoNew, (int)CmObjectFields.kflidCmObject_Owner));
			Assert.AreEqual(clid, SilDataAccess.get_ObjectProp(hvoNew, (int)CmObjectFields.kflidCmObject_Class));
			Assert.AreEqual(hvoNew, SilDataAccess.get_ObjectProp(hvoOwner, flid));
		}

		/// <summary>
		/// Test the MakeNewObject method when creating an object owned in a sequence
		/// </summary>
		[Test]
		public void MakeNewObjectTest_OwnedObjectSequence()
		{
			int hvoOwner = SilDataAccess.MakeNewObject(1, 0, -1, 0);
			var clid = SilDataAccess.MetaDataCache.GetClassId("ClassB");
			var flid = SilDataAccess.MetaDataCache.GetFieldId2(1, "SequenceProp98", false);
			int[] hvoNewObjects = new int[5];
			hvoNewObjects[0] = SilDataAccess.MakeNewObject(clid, hvoOwner, flid, 1);
			hvoNewObjects[2] = SilDataAccess.MakeNewObject(clid, hvoOwner, flid, 1);
			hvoNewObjects[1] = SilDataAccess.MakeNewObject(clid, hvoOwner, flid, 1);
			hvoNewObjects[3] = SilDataAccess.MakeNewObject(clid, hvoOwner, flid, 10);
			hvoNewObjects[4] = SilDataAccess.MakeNewObject(clid, hvoOwner, flid, 0);
			Assert.AreEqual(5, SilDataAccess.get_VecSize(hvoOwner, flid));
			int prevOwnOrd = -1;
			for (int i = 0; i < 5; i++)
			{
				int hvoNew = SilDataAccess.get_VecItem(hvoOwner, flid, i);
				Assert.IsTrue(SilDataAccess.get_IsValidObject(hvoNew));
				Assert.AreEqual(flid, SilDataAccess.get_ObjectProp(hvoNew, (int)CmObjectFields.kflidCmObject_OwnFlid));
				Assert.AreEqual(hvoOwner, SilDataAccess.get_ObjectProp(hvoNew, (int)CmObjectFields.kflidCmObject_Owner));
				int ownOrd = SilDataAccess.get_ObjectProp(hvoNew, (int)CmObjectFields.kflidCmObject_OwnOrd);
				Assert.IsTrue(prevOwnOrd < ownOrd);
				prevOwnOrd = ownOrd;
				Assert.AreEqual(clid, SilDataAccess.get_ObjectProp(hvoNew, (int)CmObjectFields.kflidCmObject_Class));
			}
		}

		/// <summary>
		/// Test the MakeNewObject method when creating an object owned in a collection
		/// </summary>
		[Test]
		public void MakeNewObjectTest_OwnedObjectCollection()
		{
			int hvoOwner = SilDataAccess.MakeNewObject(1, 0, -1, 0);
			var clid = SilDataAccess.MetaDataCache.GetClassId("ClassC");
			var flid = SilDataAccess.MetaDataCache.GetFieldId2(1, "CollectionProp99", false);
			int hvoNew = SilDataAccess.MakeNewObject(clid, hvoOwner, flid, -1);
			Assert.IsTrue(SilDataAccess.get_IsValidObject(hvoNew));
			Assert.AreEqual(flid, SilDataAccess.get_ObjectProp(hvoNew, (int)CmObjectFields.kflidCmObject_OwnFlid));
			Assert.AreEqual(hvoOwner, SilDataAccess.get_ObjectProp(hvoNew, (int)CmObjectFields.kflidCmObject_Owner));
			Assert.AreEqual(clid, SilDataAccess.get_ObjectProp(hvoNew, (int)CmObjectFields.kflidCmObject_Class));
			Assert.AreEqual(1, SilDataAccess.get_VecSize(hvoOwner, flid));
			Assert.AreEqual(hvoNew, SilDataAccess.get_VecItem(hvoOwner, flid, 0));
		}
	}
}
