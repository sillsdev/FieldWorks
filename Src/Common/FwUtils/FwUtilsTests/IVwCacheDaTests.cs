// Copyright (c) 2003-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Runtime.InteropServices;
using NUnit.Framework;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel.Utils;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Test the C++ VwCacheDa implementation of the IVwCacheDa interface (this should really be
	/// done in C++)
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class IVwCacheDaCppTests
	{
		// NB: m_ISilDataAccess and m_IVwCacheDa are exactly the same object; however,
		// they could be C# or C++, depending on if the main is is IVwCacheDaCppTests
		// or IVwCacheDaCSharpTests.
		/// <summary>The ISilDataAccess object</summary>
		protected ISilDataAccess m_ISilDataAccess;
		/// <summary>The IVwCacheDa object</summary>
		protected IVwCacheDa m_IVwCacheDa;

		/// <summary>
		/// One-time cleanup after all tests in this fixture complete.
		/// Forces GC to run and wait for finalizers to prevent crashes during VSTest cleanup.
		/// </summary>
		[OneTimeTearDown]
		public void FixtureTearDown()
		{
			// Force garbage collection and wait for finalizers to complete.
			// This ensures COM objects are released while native DLLs are still loaded.
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Setup done before each test.
		/// </summary>
		/// ------------------------------------------------------------------------------------
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

		/// <summary/>
		[TearDown]
		public void TestTeardown()
		{
			// Release COM objects to prevent access violations during VSTest cleanup.
			// The native VwCacheDa must be released before the process exits, otherwise
			// the CLR finalizer thread may try to release it after native DLLs are unloaded.
			if (m_IVwCacheDa != null)
			{
				// Clear any cached data first
				m_IVwCacheDa.ClearAllData();

				// Release the COM object reference
				Marshal.ReleaseComObject(m_IVwCacheDa);
				m_IVwCacheDa = null;
				m_ISilDataAccess = null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test setting/getting an object property
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ObjectProp()
		{
			int hvo = m_ISilDataAccess.get_ObjectProp(1000, 2000);
			Assert.That(hvo, Is.EqualTo(0));

			m_IVwCacheDa.CacheObjProp(1000, 2000, 7777);
			hvo = m_ISilDataAccess.get_ObjectProp(1000, 2000);
			Assert.That(hvo, Is.EqualTo(7777));

			m_IVwCacheDa.CacheObjProp(1000, 2000, 8888);
			hvo = m_ISilDataAccess.get_ObjectProp(1000, 2000);
			Assert.That(hvo, Is.EqualTo(8888));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test getting/setting a vector of hvos
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VecProp()
		{
			// test VecProp
			using (ArrayPtr arrayPtr = MarshalEx.ArrayToNative<int>(10))
			{
				int chvo = 99;
				m_ISilDataAccess.VecProp(1001, 2001, 10, out chvo, arrayPtr);
				Assert.That(chvo, Is.EqualTo(0));

				chvo = m_ISilDataAccess.get_VecSize(1001, 2001);
				Assert.That(chvo, Is.EqualTo(0));

				int[] rgHvo = new int[] { 33, 44, 55 };
				m_IVwCacheDa.CacheVecProp(1001, 2001, rgHvo, rgHvo.Length);
				m_ISilDataAccess.VecProp(1001, 2001, 10, out chvo, arrayPtr);
				int[] rgHvoNew = MarshalEx.NativeToArray<int>(arrayPtr, chvo);
				Assert.That(rgHvoNew.Length, Is.EqualTo(rgHvo.Length));
				for (int i = 0; i < rgHvoNew.Length; i++)
					Assert.That(rgHvoNew[i], Is.EqualTo(rgHvo[i]));

				int[] rgHvo2 = new int[] { 66, 77, 88, 99 };
				m_IVwCacheDa.CacheVecProp(1001, 2001, rgHvo2, rgHvo2.Length);
				m_ISilDataAccess.VecProp(1001, 2001, 10, out chvo, arrayPtr);
				rgHvoNew = MarshalEx.NativeToArray<int>(arrayPtr, chvo);
				Assert.That(rgHvoNew.Length, Is.EqualTo(rgHvo2.Length));
				for (int i = 0; i < rgHvoNew.Length; i++)
					Assert.That(rgHvoNew[i], Is.EqualTo(rgHvo2[i]));

				Exception ex = null;
				try
				{
					m_ISilDataAccess.VecProp(1001, 2001, 2, out chvo, arrayPtr);
				}
				catch (Exception e)
				{
					ex = e;
				}
				Assert.That(ex, Is.Not.Null);
				Assert.That(ex.GetType(), Is.EqualTo(typeof(ArgumentException)));

				// test VecItem
				int hvo = m_ISilDataAccess.get_VecItem(1001, 2001, 2);
				Assert.That(hvo, Is.EqualTo(88));

				ex = null;
				try
				{
					hvo = m_ISilDataAccess.get_VecItem(1001, 2001, 10);
				}
				catch (Exception e)
				{
					ex = e;
				}
				Assert.That(ex, Is.Not.Null);
				Assert.That(ex.GetType(), Is.EqualTo(typeof(ArgumentException)));

				// test Vector size
				chvo = m_ISilDataAccess.get_VecSize(1001, 2001);
				Assert.That(chvo, Is.EqualTo(rgHvo2.Length));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test getting/setting binary data
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BinaryProp()
		{
			using (ArrayPtr arrayPtr = MarshalEx.ArrayToNative<int>(10))
			{
				int chvo = 99;
				m_ISilDataAccess.BinaryPropRgb(1112, 2221, ArrayPtr.Null, 0, out chvo);
				Assert.That(chvo, Is.EqualTo(0));

				byte[] prgb = new byte[] { 3, 4, 5 };
				m_IVwCacheDa.CacheBinaryProp(1112, 2221, prgb, prgb.Length);
				m_ISilDataAccess.BinaryPropRgb(1112, 2221, arrayPtr, 10, out chvo);
				byte[] prgbNew = MarshalEx.NativeToArray<byte>(arrayPtr, chvo);
				Assert.That(prgbNew.Length, Is.EqualTo(prgb.Length));
				for (int i = 0; i < prgbNew.Length; i++)
					Assert.That(prgbNew[i], Is.EqualTo(prgb[i]));

				byte[] prgb2 = new byte[] { 6, 7, 8, 9 };
				m_IVwCacheDa.CacheBinaryProp(1112, 2221, prgb2, prgb2.Length);
				m_ISilDataAccess.BinaryPropRgb(1112, 2221, arrayPtr, 10, out chvo);
				prgbNew = MarshalEx.NativeToArray<byte>(arrayPtr, chvo);
				Assert.That(prgbNew.Length, Is.EqualTo(prgb2.Length));
				for (int i = 0; i < prgbNew.Length; i++)
					Assert.That(prgbNew[i], Is.EqualTo(prgb2[i]));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test getting/setting binary data
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BinaryProp_BufferToSmall()
		{
			Assert.That(() =>
			{
				byte[] prgb2 = new byte[] { 6, 7, 8, 9 };
				m_IVwCacheDa.CacheBinaryProp(1112, 2221, prgb2, prgb2.Length);
				using (ArrayPtr arrayPtr = MarshalEx.ArrayToNative<int>(10))
				{
					int chvo;
					m_ISilDataAccess.BinaryPropRgb(1112, 2221, arrayPtr, 2, out chvo);
				}
			}, Throws.TypeOf<COMException>());
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting/setting a Guid property
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GuidProp()
		{
			Guid guidNew = m_ISilDataAccess.get_GuidProp(1113, 2223);
			Assert.That(guidNew, Is.EqualTo(Guid.Empty));

			Guid guid = new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11);
			m_IVwCacheDa.CacheGuidProp(1113, 2223, guid);
			guidNew = m_ISilDataAccess.get_GuidProp(1113, 2223);
			Assert.That(guidNew, Is.EqualTo(guid));

			Guid guid2 = new Guid(10, 12, 13, 14, 15, 16, 17, 18, 19, 110, 111);
			m_IVwCacheDa.CacheGuidProp(1113, 2223, guid2);
			guidNew = m_ISilDataAccess.get_GuidProp(1113, 2223);
			Assert.That(guidNew, Is.EqualTo(guid2));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting/setting a Int64 property
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Int64Prop()
		{
			long valNew = m_ISilDataAccess.get_Int64Prop(1114, 2224);
			Assert.That(valNew, Is.EqualTo(0));

			m_IVwCacheDa.CacheInt64Prop(1114, 2224, long.MaxValue);
			valNew = m_ISilDataAccess.get_Int64Prop(1114, 2224);
			Assert.That(valNew, Is.EqualTo(long.MaxValue));

			m_IVwCacheDa.CacheInt64Prop(1114, 2224, long.MinValue);
			valNew = m_ISilDataAccess.get_Int64Prop(1114, 2224);
			Assert.That(valNew, Is.EqualTo(long.MinValue));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting/setting a int property
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IntProp()
		{
			int valNew = m_ISilDataAccess.get_IntProp(1115, 2225);
			Assert.That(valNew, Is.EqualTo(0));

			bool f;
			valNew = m_IVwCacheDa.get_CachedIntProp(1115, 2225, out f);
			Assert.That(f, Is.EqualTo(false));
			Assert.That(valNew, Is.EqualTo(0));

			m_IVwCacheDa.CacheIntProp(1115, 2225, int.MaxValue);
			valNew = m_ISilDataAccess.get_IntProp(1115, 2225);
			Assert.That(valNew, Is.EqualTo(int.MaxValue));
			valNew = m_IVwCacheDa.get_CachedIntProp(1115, 2225, out f);
			Assert.That(f, Is.EqualTo(true));
			Assert.That(valNew, Is.EqualTo(int.MaxValue));

			m_IVwCacheDa.CacheIntProp(1115, 2225, int.MinValue);
			valNew = m_ISilDataAccess.get_IntProp(1115, 2225);
			Assert.That(valNew, Is.EqualTo(int.MinValue));
			valNew = m_IVwCacheDa.get_CachedIntProp(1115, 2225, out f);
			Assert.That(f, Is.EqualTo(true));
			Assert.That(valNew, Is.EqualTo(int.MinValue));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting/setting a Time property
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TimeProp()
		{
			long valNew = m_ISilDataAccess.get_TimeProp(1116, 2226);
			Assert.That(valNew, Is.EqualTo(0));

			m_IVwCacheDa.CacheTimeProp(1116, 2226, DateTime.MaxValue.Ticks);
			valNew = m_ISilDataAccess.get_TimeProp(1116, 2226);
			Assert.That(valNew, Is.EqualTo(DateTime.MaxValue.Ticks));

			m_IVwCacheDa.CacheTimeProp(1116, 2226, DateTime.MinValue.Ticks);
			valNew = m_ISilDataAccess.get_TimeProp(1116, 2226);
			Assert.That(valNew, Is.EqualTo(DateTime.MinValue.Ticks));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting/setting the MultiStringAlt property
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("Writing System 'missing' problem that I decline to track down just yet.")]
		public void MultiStringAlt()
		{
			ITsString tsStringNew = m_ISilDataAccess.get_MultiStringAlt(1117, 2227, 7);
			Assert.That(tsStringNew, Is.Not.Null);
			Assert.That(tsStringNew.Length, Is.EqualTo(0));

			ITsPropsBldr propsBldr = TsStringUtils.MakePropsBldr();
			ITsStrBldr strBldr = TsStringUtils.MakeStrBldr();
			propsBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "Verse");
			strBldr.Replace(0, 0, "Test", propsBldr.GetTextProps());
			ITsString tsString = strBldr.GetString();
			m_IVwCacheDa.CacheStringAlt(1117, 2227, 7, tsString);
			tsStringNew = m_ISilDataAccess.get_MultiStringAlt(1117, 2227, 7);
			Assert.That(tsStringNew, Is.EqualTo(tsString));

			strBldr.Replace(0, 0, "SecondTest", propsBldr.GetTextProps());
			tsString = strBldr.GetString();
			m_IVwCacheDa.CacheStringAlt(1117, 2227, 7, tsString);
			tsStringNew = m_ISilDataAccess.get_MultiStringAlt(1117, 2227, 7);
			Assert.That(tsStringNew, Is.EqualTo(tsString));

			tsStringNew = m_ISilDataAccess.get_MultiStringAlt(1117, 2227, 8);
			Assert.That(tsStringNew, Is.Not.Null);
			Assert.That(tsStringNew.Length, Is.EqualTo(0));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting a string that is not in the cache
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("Writing System 'missing' problem that I decline to track down just yet.")]
		public void StringProp_EmptyString()
		{
			// Test StringProp
			ITsString tsStringNew = m_ISilDataAccess.get_StringProp(1118, 2228);
			Assert.That(tsStringNew.Length, Is.EqualTo(0));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting/setting String properties and StringFields properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("Writing System 'missing' problem that I decline to track down just yet.")]
		public void StringProp_SimpleString()
		{
			// Test StringProp
			ITsPropsBldr propsBldr = TsStringUtils.MakePropsBldr();
			ITsStrBldr strBldr = TsStringUtils.MakeStrBldr();
			propsBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "Verse");
			strBldr.Replace(0, 0, "StringPropTest", propsBldr.GetTextProps());
			ITsString tsString = strBldr.GetString();
			m_IVwCacheDa.CacheStringProp(1118, 2228, tsString);

			ITsString tsStringNew = m_ISilDataAccess.get_StringProp(1118, 2228);

			Assert.That(tsStringNew, Is.EqualTo(tsString));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests replacing a string in the cache and retrieveing it again
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("Writing System 'missing' problem that I decline to track down just yet.")]
		public void StringProp_ReplaceStringInCache()
		{
			ITsPropsBldr propsBldr = TsStringUtils.MakePropsBldr();
			ITsStrBldr strBldr = TsStringUtils.MakeStrBldr();
			propsBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "Verse");
			strBldr.Replace(0, 0, "StringPropTest", propsBldr.GetTextProps());
			ITsString tsString = strBldr.GetString();
			m_IVwCacheDa.CacheStringProp(1118, 2228, tsString);
			strBldr.Replace(0, 0, "Second", propsBldr.GetTextProps());
			tsString = strBldr.GetString();
			m_IVwCacheDa.CacheStringProp(1118, 2228, tsString);
			ITsString tsStringNew = m_ISilDataAccess.get_StringProp(1118, 2228);
			Assert.That(tsStringNew, Is.EqualTo(tsString));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting/setting the unicode prop
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UnicodeProp()
		{
			string strNew = m_ISilDataAccess.get_UnicodeProp(1119, 2229);
			Assert.That(strNew, Is.Null);

			string str = "UnicodeTest";
			m_IVwCacheDa.CacheUnicodeProp(1119, 2229, str, str.Length);
			strNew = m_ISilDataAccess.get_UnicodeProp(1119, 2229);
			Assert.That(strNew, Is.EqualTo(str));

			str = "SecondUnicodeTest";
			m_IVwCacheDa.CacheUnicodeProp(1119, 2229, str, str.Length);
			strNew = m_ISilDataAccess.get_UnicodeProp(1119, 2229);
			Assert.That(strNew, Is.EqualTo(str));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting/setting the unknown property
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UnknownProp()
		{
			object obj = m_ISilDataAccess.get_UnknownProp(1120, 2220);
			Assert.That(obj, Is.Null);

			ITsPropsBldr propsBldr = TsStringUtils.MakePropsBldr();
			ITsTextProps ttp = propsBldr.GetTextProps();
			m_IVwCacheDa.CacheUnknown(1120, 2220, ttp);
			obj = m_ISilDataAccess.get_UnknownProp(1120, 2220);
			Assert.That(obj, Is.EqualTo(ttp));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieve properties for all data types and verify that we only get the expected
		/// values, i.e. the expected data type that we put in the cache.
		/// </summary>
		/// <param name="hvo">HVO part of the key</param>
		/// <param name="tag">tag part of the key</param>
		/// <param name="expValues">Expected values</param>
		/// ------------------------------------------------------------------------------------
		private void VerifyCache(int hvo, int tag, object[] expValues)
		{
			int hvoVal = m_ISilDataAccess.get_ObjectProp(hvo, tag);
			Assert.That(hvoVal, Is.EqualTo(expValues[0]));

			int chvo = 99;
			using (ArrayPtr arrayPtr = MarshalEx.ArrayToNative<int>(10))
			{
				m_ISilDataAccess.VecProp(hvo, tag, 10, out chvo, arrayPtr);
				if (expValues[1] is int[])
					Assert.That(chvo, Is.EqualTo(((int[])expValues[1]).Length));
				else
					Assert.That(chvo, Is.EqualTo(expValues[1]));

				m_ISilDataAccess.BinaryPropRgb(hvo, tag, arrayPtr, 10, out chvo);
				if (expValues[2] is byte[])
					Assert.That(chvo, Is.EqualTo(((byte[])expValues[2]).Length));
				else
					Assert.That(chvo, Is.EqualTo(expValues[2]));

				Guid guidNew = m_ISilDataAccess.get_GuidProp(hvo, tag);
				Assert.That(guidNew, Is.EqualTo(expValues[3]));

				long valLong = m_ISilDataAccess.get_Int64Prop(hvo, tag);
				Assert.That(valLong, Is.EqualTo(expValues[4]));

				// Int64 and TimeProp use the same cache
				valLong = m_ISilDataAccess.get_TimeProp(hvo, tag);
				Assert.That(valLong, Is.EqualTo(expValues[4]));

				int valInt = m_ISilDataAccess.get_IntProp(hvo, tag);
				Assert.That(valInt, Is.EqualTo(expValues[5]));

				ITsString tsStringNew = m_ISilDataAccess.get_MultiStringAlt(hvo, tag, 12345);
				Assert.That(tsStringNew.Text, Is.EqualTo(expValues[6]));

				tsStringNew = m_ISilDataAccess.get_StringProp(hvo, tag);
				Assert.That(tsStringNew.Text, Is.EqualTo(expValues[7]));

				string strNew = m_ISilDataAccess.get_UnicodeProp(hvo, tag);
				Assert.That(strNew, Is.EqualTo(expValues[8]));

				object obj = m_ISilDataAccess.get_UnknownProp(hvo, tag);
				Assert.That(obj, Is.EqualTo(expValues[9]));

				CheckIsPropInCache(hvo, tag, expValues);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check the IsPropInCache method.
		/// </summary>
		/// <param name="hvo">HVO part of the key</param>
		/// <param name="tag">tag part of the key</param>
		/// <param name="expValues">Expected values</param>
		/// ------------------------------------------------------------------------------------
		private void CheckIsPropInCache(int hvo, int tag, object[] expValues)
		{
			for (CellarPropertyType cpt = CellarPropertyType.Nil;
				cpt <= CellarPropertyType.ReferenceSequence; cpt++)
			{
				bool flag = false;
				switch (cpt)
				{
					case CellarPropertyType.Nil:
						try
						{
							Assert.That(m_ISilDataAccess.get_IsPropInCache(hvo, tag,
								(int)cpt, 0), Is.False);
						}
						catch (ArgumentException)
						{
						}
						continue;
					case CellarPropertyType.Boolean:
					case CellarPropertyType.Integer:
					case CellarPropertyType.Numeric:
						flag = ((int)expValues[5] != 0);
						break;
					case CellarPropertyType.Float:
						// Never cached so far
						// TODO: We expect this to fail the test for VwCacheDa because the existing
						// implementation fails to set the return value to false. Need to fix this
						// at line 520 of VwCacheDa.cpp.
						break;
					case CellarPropertyType.Time:
						flag = (expValues[4] is long || (int)expValues[4] != 0);
						break;
					case CellarPropertyType.Guid:
						flag = ((Guid)expValues[3] != Guid.Empty);
						break;
					case CellarPropertyType.Image:
					case CellarPropertyType.GenDate:
						// Never cached so far
						// TODO: We expect this to fail the test for VwCacheDa because the existing
						// implementation fails to set the return value to false. Need to fix this
						// at line 535 of VwCacheDa.cpp.
						break;
					case CellarPropertyType.Binary:
						flag = (expValues[2] is byte[]);
						break;
					case CellarPropertyType.MultiString:
					case CellarPropertyType.MultiUnicode:
						flag = (expValues[6] != null);
						break;
					case CellarPropertyType.String:
						flag = (expValues[7] != null);
						break;
					case CellarPropertyType.Unicode:
						flag = (expValues[8] != null);
						break;
					case CellarPropertyType.OwningAtomic:
					case CellarPropertyType.ReferenceAtomic:
						flag = ((int)expValues[0] != 0);
						break;
					case CellarPropertyType.OwningCollection:
					case CellarPropertyType.ReferenceCollection:
					case CellarPropertyType.OwningSequence:
					case CellarPropertyType.ReferenceSequence:
						flag = (expValues[1] is int[]);
						break;
					default:
						continue;
				}
				Assert.That(m_ISilDataAccess.get_IsPropInCache(hvo, tag, (int)cpt, 12345), Is.EqualTo(flag), $"IsPropInCache for property type '{cpt}' failed;");
			}
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that the cache returns only object types that we put in, and the IsPropInCache
		/// method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("Writing System 'missing' problem that I decline to track down just yet.")]
		public void KeyCheck()
		{
			m_IVwCacheDa.CacheObjProp(1121, 2221, 7777);
			VerifyCache(1121, 2221,
				new object[] { 7777, 0, 0, Guid.Empty, 0, 0, null, null, null, null });

			int[] rgHvo = new int[] { 33, 44, 55 };
			m_IVwCacheDa.CacheVecProp(1122, 2222, rgHvo, rgHvo.Length);
			VerifyCache(1122, 2222,
				new object[] { 0, rgHvo, 0, Guid.Empty, 0, 0, null, null, null, null });

			byte[] prgb = new byte[] { 3, 4, 5 };
			m_IVwCacheDa.CacheBinaryProp(1123, 2223, prgb, prgb.Length);
			VerifyCache(1123, 2223,
				new object[] { 0, 0, prgb, Guid.Empty, 0, 0, null, null, null, null });

			Guid guid = new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11);
			m_IVwCacheDa.CacheGuidProp(1124, 2224, guid);
			VerifyCache(1124, 2224,
				new object[] { 0, 0, 0, guid, 0, 0, null, null, null, null });

			m_IVwCacheDa.CacheInt64Prop(1125, 2225, 123456789);
			VerifyCache(1125, 2225,
				new object[] { 0, 0, 0, Guid.Empty, 123456789, 0, null, null, null, null });

			// TimeProp uses the same cache as Int64
			long ticks = DateTime.Now.Ticks;
			m_IVwCacheDa.CacheTimeProp(1127, 2227, ticks);
			VerifyCache(1127, 2227,
				new object[] { 0, 0, 0, Guid.Empty, ticks, 0, null, null, null, null });

			m_IVwCacheDa.CacheIntProp(1126, 2226, 987654);
			VerifyCache(1126, 2226,
				new object[] { 0, 0, 0, Guid.Empty, 0, 987654, null, null, null, null });

			ITsPropsBldr propsBldr = TsStringUtils.MakePropsBldr();
			ITsStrBldr strBldr = TsStringUtils.MakeStrBldr();
			propsBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "Verse");
			strBldr.Replace(0, 0, "KeyTestMulti", propsBldr.GetTextProps());
			ITsString tsString = strBldr.GetString();
			m_IVwCacheDa.CacheStringAlt(1128, 2228, 12345, tsString);
			VerifyCache(1128, 2228,
				new object[] { 0, 0, 0, Guid.Empty, 0, 0, tsString.Text, null, null, null });

			strBldr.Replace(0, 0, "String", propsBldr.GetTextProps());
			tsString = strBldr.GetString();
			m_IVwCacheDa.CacheStringProp(1129, 2229, tsString);
			VerifyCache(1129, 2229,
				new object[] { 0, 0, 0, Guid.Empty, 0, 0, null, tsString.Text, null, null });

			string str = "KeyTestUnicode";
			m_IVwCacheDa.CacheUnicodeProp(1130, 2230, str, str.Length);
			VerifyCache(1130, 2230,
				new object[] { 0, 0, 0, Guid.Empty, 0, 0, null, null, str, null });

			ITsTextProps ttp = propsBldr.GetTextProps();
			m_IVwCacheDa.CacheUnknown(1131, 2230, ttp);
			VerifyCache(1131, 2230,
				new object[] { 0, 0, 0, Guid.Empty, 0, 0, null, null, null, ttp});
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that the cache does not cache guid properties (in its internal hash table of
		/// object guids) that are not object guids. To do this, two guid properties are cached
		/// with one being specified as an object guid (i.e. CmObjectFields.kflidCmObject_Guid)
		/// and the other being specified as not an object guid (i.e. dummyFlid).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestCacheGuidProp_ForNonCmObjectGuid()
		{
			int objFlid = (int)CmObjectFields.kflidCmObject_Guid;
			int nonObjFlid = 9005; //CmFilterTags.kflidApp
			int objHvo1 = 1124;
			int objHvo2 = 1125;
			Guid guid = Guid.NewGuid();

			// Cache the guids in this order. When this test failed, caching the
			// guid for the hvo objHvo2 clobbered the cached guid for object objHvo1
			// so there was no longer longer a guid in the cache for object objHvo1.
			m_IVwCacheDa.CacheGuidProp(objHvo1, objFlid, guid);
			m_IVwCacheDa.CacheGuidProp(objHvo2, nonObjFlid, guid);

			// Make sure the correct hvo is returned when
			// trying to create an object from the guid.
			Assert.That(m_ISilDataAccess.get_ObjFromGuid(guid), Is.EqualTo(objHvo1));

			m_IVwCacheDa.ClearAllData();

			// Now cache the guids in the reverse order from the order done above.
			m_IVwCacheDa.CacheGuidProp(objHvo2, nonObjFlid, guid);
			m_IVwCacheDa.CacheGuidProp(objHvo1, objFlid, guid);

			// Make sure the same flid is returned when the caching is reversed.
			Assert.That(m_ISilDataAccess.get_ObjFromGuid(guid), Is.EqualTo(objHvo1));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that the cache does not save guid properties in its internal hash table of
		/// object guids when the guid property is not an object guid. To do this, two guid
		/// properties are sent to the cache using the SetGuid method. One is an object guid
		/// (i.e. CmObjectFields.kflidCmObject_Guid) and the other is not (i.e. dummyFlid).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestSetGuid_ForNonCmObjectGuid()
		{
			int objFlid = (int)CmObjectFields.kflidCmObject_Guid;
			int nonObjFlid = 9005; //CmFilterTags.kflidApp
			int objHvo1 = 1124;
			int objHvo2 = 1125;
			Guid guid = Guid.NewGuid();

			// Save the guids. When this test failed, the saved guid for object objHvo2
			// clobbered the saved guid for object objHvo1 so there was no longer longer
			// a guid in the object cache for object objHvo1.
			m_ISilDataAccess.SetGuid(objHvo1, objFlid, guid);
			m_ISilDataAccess.SetGuid(objHvo2, nonObjFlid, guid);

			// Make sure the correct hvo is returned when
			// trying to create an object from the guid.
			Assert.That(m_ISilDataAccess.get_ObjFromGuid(guid), Is.EqualTo(objHvo1));

			m_IVwCacheDa.ClearAllData();

			// Now save the guids in the reverse order from the order saved above.
			m_ISilDataAccess.SetGuid(objHvo2, nonObjFlid, guid);
			m_ISilDataAccess.SetGuid(objHvo1, objFlid, guid);

			// Make sure the same flid is returned when the saving is reversed.
			Assert.That(m_ISilDataAccess.get_ObjFromGuid(guid), Is.EqualTo(objHvo1));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that the cache does not remove guid properties (from its internal
		/// hash table of object guids) that are not object guids.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestRemoveObjRef_ForNonCmObjectGuid()
		{
			int objFlid = (int)CmObjectFields.kflidCmObject_Guid;
			int nonObjFlid = 9005; //CmFilterTags.kflidApp
			int objHvo1 = 1124;
			int objHvo2 = 1125;
			Guid guid = Guid.NewGuid();

			// Cache the guids in this order. When this test failed, caching
			// the guid for objHvo2 clobbered the cached guid for object objHvo1
			// so there was no longer longer a guid in the cache for object objHvo1.
			m_IVwCacheDa.CacheGuidProp(objHvo1, objFlid, guid);
			m_IVwCacheDa.CacheGuidProp(objHvo2, nonObjFlid, guid);

			// Remove the object reference for objHvo2 and make sure it doesn't
			// remove the ability to get the object for objHvo1 using the same guid
			// that was a property for object objHvo2.
			m_ISilDataAccess.RemoveObjRefs(objHvo2);
			Assert.That(m_ISilDataAccess.get_ObjFromGuid(guid), Is.EqualTo(objHvo1));
		}
	}
}
