// Copyright (c) 2006-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Utils;

namespace LanguageExplorer.TestUtilities.Tests
{
	/// <summary>
	/// Tests the two main interfaces (ISilDataAccess and IVwCacheDa) for RealDataCache.
	/// </summary>
	[TestFixture]
	public sealed class RealDataCacheISilDataAccessTests
	{
		/// <summary />
		private RealDataCache m_realDataCache;

		/// <summary />
		private ISilDataAccess SilDataAccess => m_realDataCache;

		/// <summary />
		private IVwCacheDa VwCacheDa => m_realDataCache;

		/// <summary>
		/// If a test overrides this, it should call this base implementation.
		/// </summary>
		[OneTimeSetUp]
		public void FixtureSetup()
		{
			m_realDataCache = new RealDataCache
			{
				MetaDataCache = MetaDataCache.CreateMetaDataCache(MDCTestUtils.GetPathToTestFile("TestModel.xml"))
			};
		}

		/// <summary/>
		[OneTimeTearDown]
		public void FixtureTearDown()
		{
			m_realDataCache.Dispose();
		}

		/// <summary>
		/// Shuts down the LCM cache
		/// </summary>
		/// <remarks>This method is called after each test</remarks>
		[TearDown]
		public void Exit()
		{
			VwCacheDa.ClearAllData();
		}

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
			Assert.AreEqual(hvoObj, SilDataAccess.get_ObjectProp(hvo, tag),
				"Wrong hvoObj in cache.");
		}

		/// <summary>
		/// Test Int Property get, when no set has been done.
		/// </summary>
		[Test]
		public void ObjPropKNTTest()
		{
			const int hvo = 1;
			// Set class first, or it will throw the exception in the wrong place.
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class,
				SilDataAccess.MetaDataCache.GetClassId("ClassC"));
			Assert.That(
				() =>
				{
					SilDataAccess.get_ObjectProp(hvo, (int)CmObjectFields.kflidCmObject_Owner);
				}, Throws.TypeOf<KeyNotFoundException>());
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
			var clid2 = VwCacheDa.get_CachedIntProp(hvo, tag, out var isInCache);
			Assert.IsTrue(isInCache, "Int not in cache.");
			Assert.AreEqual(clid1, clid2, "Clids are not the same.");
			// See if the int is there via another method.
			// It should not be there.
			var ownerHvo = VwCacheDa.get_CachedIntProp(hvo,
				(int)CmObjectFields.kflidCmObject_Owner, out isInCache);
			Assert.IsFalse(isInCache, "Int is in cache.");
			Assert.AreEqual(0, ownerHvo, "Wrong owner.");
		}

		/// <summary>
		/// Test Int Property get, when no set has been done.
		/// </summary>
		[Test]
		public void IntPropKNTTest()
		{
			const int hvo = 1;
			// Set class first, or it will throw the exception in the wrong place.
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class,
				SilDataAccess.MetaDataCache.GetClassId("ClassC"));
			Assert.That(
				() =>
				{
					SilDataAccess.get_IntProp(hvo,
						SilDataAccess.MetaDataCache.GetFieldId("ClassC", "IntProp2", false));
				}, Throws.TypeOf<KeyNotFoundException>());
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
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class,
				SilDataAccess.MetaDataCache.GetClassId("ClassA"));
			// Now set its 'Guid' property.
			const int tag = (int)CmObjectFields.kflidCmObject_Guid;
			var uid = Guid.NewGuid();
			SilDataAccess.SetGuid(hvo, tag, uid);
			var uid2 = SilDataAccess.get_GuidProp(hvo, tag);
			Assert.AreEqual(uid, uid2, "Wrong uid in cache.");

			// Test the reverse method.
			Assert.AreEqual(hvo, SilDataAccess.get_ObjFromGuid(uid2),
				"Wrong hvo in cache for Guid.");
		}

		/// <summary>
		/// Test Guid Property get, when no set has been done.
		/// </summary>
		[Test]
		public void GuidPropKNTTest()
		{
			const int hvo = 1;
			// Set class first, or it will throw the exception in the wrong place.
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class,
				SilDataAccess.MetaDataCache.GetClassId("ClassD"));
			Assert.That(
				() =>
				{
					SilDataAccess.get_GuidProp(hvo, (int)CmObjectFields.kflidCmObject_Guid);
				}, Throws.TypeOf<KeyNotFoundException>());
		}

		/// <summary>
		/// Test Bool Property get/set.
		/// </summary>
		[Test]
		public void BoolPropTest()
		{
			// Set class first, or it will throw an exception.
			const int hvo = 1;
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class,
				SilDataAccess.MetaDataCache.GetClassId("ClassD"));
			// Now set its 'BoolProp3' property.
			var tag = SilDataAccess.MetaDataCache.GetFieldId("ClassD", "BoolProp3", false);
			const bool excludeOriginal = true;
			SilDataAccess.SetBoolean(hvo, tag, excludeOriginal);
			Assert.AreEqual(excludeOriginal, SilDataAccess.get_BooleanProp(hvo, tag),
				"Wrong bool in cache.");
		}

		/// <summary>
		/// Test Guid Property get, when no set has been done.
		/// </summary>
		[Test]
		public void BoolPropKNTTest()
		{
			const int hvo = 1;
			// Set class first, or it will throw the exception in the wrong place.
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class,
				SilDataAccess.MetaDataCache.GetClassId("ClassA"));
			Assert.That(
				() =>
				{
					SilDataAccess.get_BooleanProp(hvo,
						SilDataAccess.MetaDataCache.GetFieldId("ClassA", "Prop1", false));
				}, Throws.TypeOf<KeyNotFoundException>());
		}

		/// <summary>
		/// Test Unicode Property get/set.
		/// </summary>
		[Test]
		public void UnicodePropTest()
		{
			// Set class first, or it will throw an exception.
			const int hvo = 1;
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class,
				SilDataAccess.MetaDataCache.GetClassId("ClassE"));
			// Set its 'UnicodeProp4' property, using the 'BSTR' method.
			var tag = SilDataAccess.MetaDataCache.GetFieldId("ClassE", "UnicodeProp4", false);
			const string ec = "ZPI";
			SilDataAccess.set_UnicodeProp(hvo, tag, ec);
			Assert.AreEqual(ec, SilDataAccess.get_UnicodeProp(hvo, tag),
				"Wrong Unicode string in cache.");
			// Set its 'UnicodeProp4' property, using non-bstr method.
			const string ecNew = "ZPR";
			SilDataAccess.SetUnicode(hvo, tag, ecNew, ecNew.Length);
			SilDataAccess.UnicodePropRgch(hvo, tag, null, 0, out var len);
			Assert.AreEqual(ecNew.Length, len);
			using (var arrayPtr = MarshalEx.StringToNative(len + 1, true))
			{
				SilDataAccess.UnicodePropRgch(hvo, tag, arrayPtr, len + 1, out var cch);
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
		public void UnicodePropKNTTest()
		{
			const int hvo = 1;
			// Set class first, or it will throw the exception in the wrong place.
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class,
				SilDataAccess.MetaDataCache.GetClassId("ClassE"));
			Assert.That(
				() =>
				{
					SilDataAccess.get_UnicodeProp(hvo,
						SilDataAccess.MetaDataCache.GetFieldId("ClassE", "UnicodeProp4", false));
				}, Throws.TypeOf<KeyNotFoundException>());
		}

		/// <summary>
		/// Test Unicode Property get, when no set has been done.
		/// </summary>
		[Test]
		public void UnicodePropWrongLengthTest()
		{
			// Set class first, or it will throw an exception.
			const int hvo = 1;
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class,
				SilDataAccess.MetaDataCache.GetClassId("ClassE"));
			// Set its 'UnicodeProp4' property, using the 'BSTR' method.
			var tag = SilDataAccess.MetaDataCache.GetFieldId("ClassE", "UnicodeProp4", false);
			const string ec = "ZPI";
			SilDataAccess.set_UnicodeProp(hvo, tag, ec);
			Assert.AreEqual(ec, SilDataAccess.get_UnicodeProp(hvo, tag),
				"Wrong Unicode string in cache.");
			// Set its 'UnicodeProp4' property, using non-bstr method.
			const string ecNew = "ZPR";
			SilDataAccess.SetUnicode(hvo, tag, ecNew, ecNew.Length);
			SilDataAccess.UnicodePropRgch(hvo, tag, null, 0, out var len);
			Assert.AreEqual(ecNew.Length, len);
			using (var arrayPtr = MarshalEx.StringToNative(len, true))
			{
				Assert.That(
					() => { SilDataAccess.UnicodePropRgch(hvo, tag, arrayPtr, len, out _); },
					Throws.TypeOf<ArgumentException>());
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
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class,
				SilDataAccess.MetaDataCache.GetClassId("ClassF"));
			const long dob = long.MinValue;
			var tag = SilDataAccess.MetaDataCache.GetFieldId("ClassF", "Int64Prop5", false);
			SilDataAccess.SetInt64(hvo, tag, dob);
			Assert.AreEqual(dob, SilDataAccess.get_Int64Prop(hvo, tag), "Wrong DOB in cache.");
		}

		/// <summary>
		/// Test In64 Property get, when no set has been done.
		/// </summary>
		[Test]
		public void In64PropKNTTest()
		{
			const int hvo = 1;
			// Set class first, or it will throw the exception in the wrong place.
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class,
				SilDataAccess.MetaDataCache.GetClassId("ClassF"));
			Assert.That(
				() =>
				{
					SilDataAccess.get_Int64Prop(hvo,
						SilDataAccess.MetaDataCache.GetFieldId("ClassF", "Int64Prop5", false));
				}, Throws.TypeOf<KeyNotFoundException>());
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
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class,
				SilDataAccess.MetaDataCache.GetClassId("ClassD"));
			const long doc = long.MinValue;
			var tag = SilDataAccess.MetaDataCache.GetFieldId("ClassD", "TimeProp6", false);
			SilDataAccess.SetTime(hvo, tag, doc);
			Assert.AreEqual(doc, SilDataAccess.get_TimeProp(hvo, tag),
				"Wrong creation in cache.");
		}

		/// <summary>
		/// Test Time Property get, when no set has been done.
		/// </summary>
		[Test]
		public void TimePropKNTTest()
		{
			const int hvo = 1;
			// Set class first, or it will throw the exception in the wrong place.
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class,
				SilDataAccess.MetaDataCache.GetClassId("ClassD"));
			Assert.That(
				() =>
				{
					SilDataAccess.get_TimeProp(hvo,
						SilDataAccess.MetaDataCache.GetFieldId("ClassD", "TimeProp6", false));
				}, Throws.TypeOf<KeyNotFoundException>());
		}

		/// <summary>
		/// Test IUnknown Property get/set.
		/// </summary>
		[Test]
		public void UnkPropTest()
		{
			// First, set up class id.
			const int hvo = 1;
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class,
				SilDataAccess.MetaDataCache.GetClassId("ClassG"));
			var tsPropsBuilder = TsStringUtils.MakePropsBldr();
			var props = tsPropsBuilder.GetTextProps();
			var tag = SilDataAccess.MetaDataCache.GetFieldId("ClassG", "TextPropsProp7", false);
			SilDataAccess.SetUnknown(hvo, tag, props);
			Assert.AreEqual(props, (ITsTextProps)SilDataAccess.get_UnknownProp(hvo, tag),
				"Wrong text props in cache.");
		}

		/// <summary>
		/// Test Time Property get, when no set has been done.
		/// </summary>
		[Test]
		public void UnkPropKNTTest()
		{
			const int hvo = 1;
			// Set class first, or it will throw the exception in the wrong place.
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class,
				SilDataAccess.MetaDataCache.GetClassId("ClassG"));
			Assert.That(
				() =>
				{
					SilDataAccess.get_UnknownProp(hvo,
						SilDataAccess.MetaDataCache.GetFieldId("ClassG", "TextPropsProp7",
							false));
				}, Throws.TypeOf<KeyNotFoundException>());
		}

		/// <summary>
		/// Test Time Property get, when no set has been done.
		/// </summary>
		[Test]
		public void UnkPropMisMatchedFlidTest()
		{
			// First, set up class id.
			const int hvo = 1;
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class,
				SilDataAccess.MetaDataCache.GetClassId("ClassE"));
			var tsPropsBuilder = TsStringUtils.MakePropsBldr();
			Assert.That(
				() =>
				{
					SilDataAccess.SetUnknown(hvo,
						SilDataAccess.MetaDataCache.GetFieldId("ClassE", "UnicodeProp4", false),
						tsPropsBuilder.GetTextProps());
				}, Throws.TypeOf<ArgumentException>());
		}

		/// <summary>
		/// Test Time Property get, when no set has been done.
		/// </summary>
		[Test]
		public void UnkPropWrongInterfaceTest()
		{
			// First, set up class id.
			const int hvo = 1;
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class,
				SilDataAccess.MetaDataCache.GetClassId("ClassG"));
			var tsPropsBuilder = TsStringUtils.MakePropsBldr();
			Assert.That(
				() =>
				{
					SilDataAccess.SetUnknown(hvo,
						SilDataAccess.MetaDataCache.GetFieldId("ClassG", "TextPropsProp7", false),
						tsPropsBuilder);
				}, Throws.TypeOf<ArgumentException>());
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
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class,
				SilDataAccess.MetaDataCache.GetClassId("ClassH"));
			var tag = SilDataAccess.MetaDataCache.GetFieldId("ClassH", "BinaryProp8", false);

			using (var arrayPtr = MarshalEx.ArrayToNative<int>(3))
			{
				var prgb = new byte[] { 3, 4, 5 };
				SilDataAccess.SetBinary(hvo, tag, prgb, prgb.Length);
				SilDataAccess.BinaryPropRgb(hvo, tag, arrayPtr, 3, out var chvo);
				var prgbNew = MarshalEx.NativeToArray<byte>(arrayPtr, chvo);
				Assert.AreEqual(prgb.Length, prgbNew.Length);
				for (var i = 0; i < prgbNew.Length; i++)
				{
					Assert.AreEqual(prgb[i], prgbNew[i]);
				}
			}
		}

		/// <summary>
		/// Test Binary Property get, when no set has been done.
		/// </summary>
		[Test]
		public void BinaryPropKNTTest()
		{
			const int hvo = 1;
			// Set class first, or it will throw the exception in the wrong place.
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class,
				SilDataAccess.MetaDataCache.GetClassId("ClassI"));
			Assert.That(
				() =>
				{
					SilDataAccess.BinaryPropRgb(hvo,
						SilDataAccess.MetaDataCache.GetFieldId("ClassI", "BinaryProp9", false),
						ArrayPtr.Null, 0, out _);
				}, Throws.TypeOf<KeyNotFoundException>());
		}

		/// <summary>
		/// Test Binary Property get, when no set has been done.
		/// </summary>
		[Test]
		public void BinaryPropWrongLengthTest()
		{
			const int hvo = 1;
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class,
				SilDataAccess.MetaDataCache.GetClassId("ClassI"));
			var tag = SilDataAccess.MetaDataCache.GetFieldId("ClassI", "BinaryProp9", false);
			using (var arrayPtr = MarshalEx.ArrayToNative<int>(2))
			{
				var prgb = new byte[] { 3, 4, 5 };
				SilDataAccess.SetBinary(hvo, tag, prgb, prgb.Length);
				Assert.That(() => { SilDataAccess.BinaryPropRgb(hvo, tag, arrayPtr, 2, out _); },
					Throws.TypeOf<ArgumentException>());
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
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class,
				SilDataAccess.MetaDataCache.GetClassId("ClassJ"));
			var tag = SilDataAccess.MetaDataCache.GetFieldId("ClassJ", "StringProp10", false);

			var tsString = TsStringUtils.MakeString("/ a _", 42, "Verse");
			SilDataAccess.SetString(hvo, tag, tsString);

			Assert.AreEqual(tsString, SilDataAccess.get_StringProp(hvo, tag));
		}

		/// <summary>
		/// Test String Property get, when no set has been done.
		/// </summary>
		[Test]
		public void StringPropKNTTest()
		{
			// Set class first, or it will throw an exception.
			const int hvo = 1;
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class,
				SilDataAccess.MetaDataCache.GetClassId("ClassJ"));
			Assert.That(
				() =>
				{
					SilDataAccess.get_StringProp(hvo,
						SilDataAccess.MetaDataCache.GetFieldId("ClassJ", "StringProp10", false));
				}, Throws.TypeOf<KeyNotFoundException>());
		}

		/// <summary>
		/// Test MultiString Property get/set.
		/// </summary>
		[Test]
		public void MultiStringPropTest()
		{
			// Set class first, or it will throw an exception.
			const int hvo = 1;
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class,
				SilDataAccess.MetaDataCache.GetClassId("ClassK"));
			var tag = SilDataAccess.MetaDataCache.GetFieldId("ClassK", "MultiStringProp11",
				false);

			var tss = TsStringUtils.MakeString("Verb", 1);
			SilDataAccess.SetMultiStringAlt(hvo, tag, 1, tss);

			Assert.AreEqual(tss, SilDataAccess.get_MultiStringAlt(hvo, tag, 1));
		}

		/// <summary>
		/// Test get_MultiStringProp method.
		/// </summary>
		[Test]
		public void AllMultiStringPropTest()
		{
			// Set class first, or it will throw an exception.
			const int hvo = 1;
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class,
				SilDataAccess.MetaDataCache.GetClassId("ClassK"));
			var tag = SilDataAccess.MetaDataCache.GetFieldId("ClassK", "MultiStringProp11",
				false);

			var tss = TsStringUtils.MakeString("Verb", 1);
			SilDataAccess.SetMultiStringAlt(hvo, tag, 1, tss);
			tss = TsStringUtils.MakeString("Verbo", 2);
			SilDataAccess.SetMultiStringAlt(hvo, tag, 2, tss);

			Assert.AreEqual(SilDataAccess.get_MultiStringProp(hvo, tag).StringCount, 2);
		}

		/// <summary>
		/// Test setting a Ws of zero method.
		/// </summary>
		[Test]
		public void MultiString0WSTest()
		{
			// Set class first, or it will throw an exception.
			const int hvo = 1;
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class,
				SilDataAccess.MetaDataCache.GetClassId("ClassK"));
			Assert.That(
				() =>
				{
					SilDataAccess.SetMultiStringAlt(hvo,
						SilDataAccess.MetaDataCache.GetFieldId("ClassK", "MultiStringProp11",
							false), 0, TsStringUtils.MakeString("Verb", 1));
				}, Throws.TypeOf<ArgumentException>());
		}

		/// <summary>
		/// Test setting a Ws of zero method.
		/// </summary>
		[Test]
		public void MultiStringNegativeWSTest()
		{
			// Set class first, or it will throw an exception.
			const int hvo = 1;
			SilDataAccess.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class,
				SilDataAccess.MetaDataCache.GetClassId("ClassK"));
			Assert.That(
				() =>
				{
					SilDataAccess.SetMultiStringAlt(hvo,
						SilDataAccess.MetaDataCache.GetFieldId("ClassK", "MultiStringProp11",
							false), -1, TsStringUtils.MakeString("Verb", 1));
				}, Throws.TypeOf<ArgumentException>());
		}

		/// <summary>
		/// Test the MakeNewObject method when creating an unowned object
		/// </summary>
		[Test]
		public void MakeNewObjectTest_UnownedObject()
		{
			var hvoNew = SilDataAccess.MakeNewObject(1, 0, -1, 0);
			Assert.IsTrue(SilDataAccess.get_IsValidObject(hvoNew));
			Assert.AreEqual(1,
				SilDataAccess.get_ObjectProp(hvoNew, (int)CmObjectFields.kflidCmObject_Class));
		}

		/// <summary>
		/// Test the MakeNewObject method when creating an atomic owned object
		/// </summary>
		[Test]
		public void MakeNewObjectTest_OwnedObjectAtomic()
		{
			var hvoOwner = SilDataAccess.MakeNewObject(1, 0, -1, 0);
			var clid = SilDataAccess.MetaDataCache.GetClassId("ClassA");
			var flid = SilDataAccess.MetaDataCache.GetFieldId2(1, "AtomicProp97", false);
			var hvoNew = SilDataAccess.MakeNewObject(clid, hvoOwner, flid, -2);
			Assert.IsTrue(SilDataAccess.get_IsValidObject(hvoNew));
			Assert.AreEqual(flid,
				SilDataAccess.get_ObjectProp(hvoNew, (int)CmObjectFields.kflidCmObject_OwnFlid));
			Assert.AreEqual(hvoOwner,
				SilDataAccess.get_ObjectProp(hvoNew, (int)CmObjectFields.kflidCmObject_Owner));
			Assert.AreEqual(clid,
				SilDataAccess.get_ObjectProp(hvoNew, (int)CmObjectFields.kflidCmObject_Class));
			Assert.AreEqual(hvoNew, SilDataAccess.get_ObjectProp(hvoOwner, flid));
		}

		/// <summary>
		/// Test the MakeNewObject method when creating an object owned in a sequence
		/// </summary>
		[Test]
		public void MakeNewObjectTest_OwnedObjectSequence()
		{
			var hvoOwner = SilDataAccess.MakeNewObject(1, 0, -1, 0);
			var clid = SilDataAccess.MetaDataCache.GetClassId("ClassB");
			var flid = SilDataAccess.MetaDataCache.GetFieldId2(1, "SequenceProp98", false);
			var hvoNewObjects = new int[5];
			hvoNewObjects[0] = SilDataAccess.MakeNewObject(clid, hvoOwner, flid, 1);
			hvoNewObjects[2] = SilDataAccess.MakeNewObject(clid, hvoOwner, flid, 1);
			hvoNewObjects[1] = SilDataAccess.MakeNewObject(clid, hvoOwner, flid, 1);
			hvoNewObjects[3] = SilDataAccess.MakeNewObject(clid, hvoOwner, flid, 10);
			hvoNewObjects[4] = SilDataAccess.MakeNewObject(clid, hvoOwner, flid, 0);
			Assert.AreEqual(5, SilDataAccess.get_VecSize(hvoOwner, flid));
			var prevOwnOrd = -1;
			for (var i = 0; i < 5; i++)
			{
				var hvoNew = SilDataAccess.get_VecItem(hvoOwner, flid, i);
				Assert.IsTrue(SilDataAccess.get_IsValidObject(hvoNew));
				Assert.AreEqual(flid,
					SilDataAccess.get_ObjectProp(hvoNew,
						(int)CmObjectFields.kflidCmObject_OwnFlid));
				Assert.AreEqual(hvoOwner,
					SilDataAccess.get_ObjectProp(hvoNew,
						(int)CmObjectFields.kflidCmObject_Owner));
				var ownOrd =
					SilDataAccess.get_ObjectProp(hvoNew,
						(int)CmObjectFields.kflidCmObject_OwnOrd);
				Assert.IsTrue(prevOwnOrd < ownOrd);
				prevOwnOrd = ownOrd;
				Assert.AreEqual(clid,
					SilDataAccess.get_ObjectProp(hvoNew,
						(int)CmObjectFields.kflidCmObject_Class));
			}
		}

		/// <summary>
		/// Test the MakeNewObject method when creating an object owned in a collection
		/// </summary>
		[Test]
		public void MakeNewObjectTest_OwnedObjectCollection()
		{
			var hvoOwner = SilDataAccess.MakeNewObject(1, 0, -1, 0);
			var clid = SilDataAccess.MetaDataCache.GetClassId("ClassC");
			var flid = SilDataAccess.MetaDataCache.GetFieldId2(1, "CollectionProp99", false);
			var hvoNew = SilDataAccess.MakeNewObject(clid, hvoOwner, flid, -1);
			Assert.IsTrue(SilDataAccess.get_IsValidObject(hvoNew));
			Assert.AreEqual(flid,
				SilDataAccess.get_ObjectProp(hvoNew, (int)CmObjectFields.kflidCmObject_OwnFlid));
			Assert.AreEqual(hvoOwner,
				SilDataAccess.get_ObjectProp(hvoNew, (int)CmObjectFields.kflidCmObject_Owner));
			Assert.AreEqual(clid,
				SilDataAccess.get_ObjectProp(hvoNew, (int)CmObjectFields.kflidCmObject_Class));
			Assert.AreEqual(1, SilDataAccess.get_VecSize(hvoOwner, flid));
			Assert.AreEqual(hvoNew, SilDataAccess.get_VecItem(hvoOwner, flid, 0));
		}
	}
}