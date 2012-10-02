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
// File: IVwCacheDaTests.cs
// Responsibility: Eberhard Beilharz
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Runtime.InteropServices;
using System.Diagnostics;

using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Test.TestUtils;

namespace SIL.FieldWorks.FDO.FDOTests.Cache
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Test the IVwCacheDa interface
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class IVwCacheDaTests : BaseTest
	{
		// NB: m_ISilDataAccess and m_IVwCacheDa are exactly the same object.
		// they could be C# or C++, depeding on if the main is is IVwCacheDaCppTests
		// or IVwCacheDaCSharpTests, however.
		/// <summary>The ISilDataAccess object</summary>
		protected ISilDataAccess m_ISilDataAccess;
		/// <summary>The IVwCacheDa object</summary>
		protected IVwCacheDa m_IVwCacheDa;

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

			// NB: m_ISilDataAccess and m_IVwCacheDa are the same exact object.
			switch (GetType().Name)
			{
				default:
					throw new InvalidOperationException("Class of obejct not recognized.");
				case "IVwCacheDaCppTests":
					// Not really relevant, as of 12/7.06, since the C++ class was not being used.
					// It's TextFixture attribute was commented out.
					if (m_ISilDataAccess != null)
					{
						if (m_ISilDataAccess.WritingSystemFactory != null)
							m_ISilDataAccess.WritingSystemFactory.Shutdown();
						if (Marshal.IsComObject(m_ISilDataAccess))
						{
							int n = Marshal.ReleaseComObject(m_ISilDataAccess);
							Debug.Assert(n == 0);
						}
					}
					break;
				case "IVwCacheDaCSharpTests":
					if (disposing)
					{
						// Dispose managed resources here.
						if (m_IVwCacheDa != null) // It has to be IDisposable if it is C# && m_IVwCacheDa is IDisposable)
						{
							if (m_ISilDataAccess.WritingSystemFactory != null)
								m_ISilDataAccess.WritingSystemFactory.Shutdown();
							(m_IVwCacheDa as IDisposable).Dispose();
						}
					}
					break;
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_IVwCacheDa = null;
			m_ISilDataAccess = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test setting/getting an object property
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ObjectProp()
		{
			CheckDisposed();
			int hvo = m_ISilDataAccess.get_ObjectProp(1000, 2000);
			Assert.AreEqual(0, hvo);

			m_IVwCacheDa.CacheObjProp(1000, 2000, 7777);
			hvo = m_ISilDataAccess.get_ObjectProp(1000, 2000);
			Assert.AreEqual(7777, hvo);

			m_IVwCacheDa.CacheObjProp(1000, 2000, 8888);
			hvo = m_ISilDataAccess.get_ObjectProp(1000, 2000);
			Assert.AreEqual(8888, hvo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test getting/setting a vector of hvos
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VecProp()
		{
			CheckDisposed();
			// test VecProp
			using (ArrayPtr arrayPtr = MarshalEx.ArrayToNative(10, typeof(int)))
			{
				int chvo = 99;
				m_ISilDataAccess.VecProp(1001, 2001, 10, out chvo, arrayPtr);
				Assert.AreEqual(0, chvo);

				chvo = m_ISilDataAccess.get_VecSize(1001, 2001);
				Assert.AreEqual(0, chvo);

				int[] rgHvo = new int[] { 33, 44, 55 };
				m_IVwCacheDa.CacheVecProp(1001, 2001, rgHvo, rgHvo.Length);
				m_ISilDataAccess.VecProp(1001, 2001, 10, out chvo, arrayPtr);
				int[] rgHvoNew = (int[])MarshalEx.NativeToArray(arrayPtr, chvo, typeof(int));
				Assert.AreEqual(rgHvo.Length, rgHvoNew.Length);
				for (int i = 0; i < rgHvoNew.Length; i++)
					Assert.AreEqual(rgHvo[i], rgHvoNew[i]);

				int[] rgHvo2 = new int[] { 66, 77, 88, 99 };
				m_IVwCacheDa.CacheVecProp(1001, 2001, rgHvo2, rgHvo2.Length);
				m_ISilDataAccess.VecProp(1001, 2001, 10, out chvo, arrayPtr);
				rgHvoNew = (int[])MarshalEx.NativeToArray(arrayPtr, chvo, typeof(int));
				Assert.AreEqual(rgHvo2.Length, rgHvoNew.Length);
				for (int i = 0; i < rgHvoNew.Length; i++)
					Assert.AreEqual(rgHvo2[i], rgHvoNew[i]);

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
				int hvo = m_ISilDataAccess.get_VecItem(1001, 2001, 2);
				Assert.AreEqual(88, hvo);

				ex = null;
				try
				{
					hvo = m_ISilDataAccess.get_VecItem(1001, 2001, 10);
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test getting/setting binary data
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BinaryProp()
		{
			CheckDisposed();
			using (ArrayPtr arrayPtr = MarshalEx.ArrayToNative(10, typeof(int)))
			{
				int chvo = 99;
				m_ISilDataAccess.BinaryPropRgb(1112, 2221, ArrayPtr.Null, 0, out chvo);
				Assert.AreEqual(0, chvo);

				byte[] prgb = new byte[] { 3, 4, 5 };
				m_IVwCacheDa.CacheBinaryProp(1112, 2221, prgb, prgb.Length);
				m_ISilDataAccess.BinaryPropRgb(1112, 2221, arrayPtr, 10, out chvo);
				byte[] prgbNew = (byte[])MarshalEx.NativeToArray(arrayPtr, chvo, typeof(byte));
				Assert.AreEqual(prgb.Length, prgbNew.Length);
				for (int i = 0; i < prgbNew.Length; i++)
					Assert.AreEqual(prgb[i], prgbNew[i]);

				byte[] prgb2 = new byte[] { 6, 7, 8, 9 };
				m_IVwCacheDa.CacheBinaryProp(1112, 2221, prgb2, prgb2.Length);
				m_ISilDataAccess.BinaryPropRgb(1112, 2221, arrayPtr, 10, out chvo);
				prgbNew = (byte[])MarshalEx.NativeToArray(arrayPtr, chvo, typeof(byte));
				Assert.AreEqual(prgb2.Length, prgbNew.Length);
				for (int i = 0; i < prgbNew.Length; i++)
					Assert.AreEqual(prgb2[i], prgbNew[i]);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test getting/setting binary data
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(COMException))]
		public void BinaryProp_BufferToSmall()
		{
			CheckDisposed();
			byte[] prgb2 = new byte[] { 6, 7, 8, 9 };
			m_IVwCacheDa.CacheBinaryProp(1112, 2221, prgb2, prgb2.Length);
			using (ArrayPtr arrayPtr = MarshalEx.ArrayToNative(10, typeof(int)))
			{
				int chvo;
				m_ISilDataAccess.BinaryPropRgb(1112, 2221, arrayPtr, 2, out chvo);
			}
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting/setting a Guid property
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GuidProp()
		{
			CheckDisposed();
			Guid guidNew = m_ISilDataAccess.get_GuidProp(1113, 2223);
			Assert.AreEqual(Guid.Empty, guidNew);

			Guid guid = new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11);
			m_IVwCacheDa.CacheGuidProp(1113, 2223, guid);
			guidNew = m_ISilDataAccess.get_GuidProp(1113, 2223);
			Assert.AreEqual(guid, guidNew);

			Guid guid2 = new Guid(10, 12, 13, 14, 15, 16, 17, 18, 19, 110, 111);
			m_IVwCacheDa.CacheGuidProp(1113, 2223, guid2);
			guidNew = m_ISilDataAccess.get_GuidProp(1113, 2223);
			Assert.AreEqual(guid2, guidNew);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting/setting a Int64 property
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Int64Prop()
		{
			CheckDisposed();
			long valNew = m_ISilDataAccess.get_Int64Prop(1114, 2224);
			Assert.AreEqual(0, valNew);

			m_IVwCacheDa.CacheInt64Prop(1114, 2224, long.MaxValue);
			valNew = m_ISilDataAccess.get_Int64Prop(1114, 2224);
			Assert.AreEqual(long.MaxValue, valNew);

			m_IVwCacheDa.CacheInt64Prop(1114, 2224, long.MinValue);
			valNew = m_ISilDataAccess.get_Int64Prop(1114, 2224);
			Assert.AreEqual(long.MinValue, valNew);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting/setting a int property
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IntProp()
		{
			CheckDisposed();
			int valNew = m_ISilDataAccess.get_IntProp(1115, 2225);
			Assert.AreEqual(0, valNew);

			bool f;
			valNew = m_IVwCacheDa.get_CachedIntProp(1115, 2225, out f);
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting/setting a Time property
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TimeProp()
		{
			CheckDisposed();
			long valNew = m_ISilDataAccess.get_TimeProp(1116, 2226);
			Assert.AreEqual(0, valNew);

			m_IVwCacheDa.CacheTimeProp(1116, 2226, DateTime.MaxValue.Ticks);
			valNew = m_ISilDataAccess.get_TimeProp(1116, 2226);
			Assert.AreEqual(DateTime.MaxValue.Ticks, valNew);

			m_IVwCacheDa.CacheTimeProp(1116, 2226, DateTime.MinValue.Ticks);
			valNew = m_ISilDataAccess.get_TimeProp(1116, 2226);
			Assert.AreEqual(DateTime.MinValue.Ticks, valNew);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting/setting the MultiStringAlt property
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MultiStringAlt()
		{
			CheckDisposed();
			ITsString tsStringNew = m_ISilDataAccess.get_MultiStringAlt(1117, 2227, 7);
			Assert.IsNotNull(tsStringNew);
			Assert.AreEqual(0, tsStringNew.Length);

			ITsPropsBldr propsBldr = TsPropsBldrClass.Create();
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			propsBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "Verse");
			strBldr.Replace(0, 0, "Test", propsBldr.GetTextProps());
			ITsString tsString = strBldr.GetString();
			m_IVwCacheDa.CacheStringAlt(1117, 2227, 7, tsString);
			tsStringNew = m_ISilDataAccess.get_MultiStringAlt(1117, 2227, 7);
			Assert.AreEqual(tsString, tsStringNew);

			strBldr.Replace(0, 0, "SecondTest", propsBldr.GetTextProps());
			tsString = strBldr.GetString();
			m_IVwCacheDa.CacheStringAlt(1117, 2227, 7, tsString);
			tsStringNew = m_ISilDataAccess.get_MultiStringAlt(1117, 2227, 7);
			Assert.AreEqual(tsString, tsStringNew);

			tsStringNew = m_ISilDataAccess.get_MultiStringAlt(1117, 2227, 8);
			Assert.IsNotNull(tsStringNew);
			Assert.AreEqual(0, tsStringNew.Length);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting a string that is not in the cache
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void StringProp_EmptyString()
		{
			CheckDisposed();
			// Test StringProp
			ITsString tsStringNew = m_ISilDataAccess.get_StringProp(1118, 2228);
			Assert.AreEqual(0, tsStringNew.Length);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting/setting String properties and StringFields properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void StringProp_SimpleString()
		{
			CheckDisposed();
			// Test StringProp
			ITsPropsBldr propsBldr = TsPropsBldrClass.Create();
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			propsBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "Verse");
			strBldr.Replace(0, 0, "StringPropTest", propsBldr.GetTextProps());
			ITsString tsString = strBldr.GetString();
			m_IVwCacheDa.CacheStringProp(1118, 2228, tsString);

			ITsString tsStringNew = m_ISilDataAccess.get_StringProp(1118, 2228);

			Assert.AreEqual(tsString, tsStringNew);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests replacing a string in the cache and retrieveing it again
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void StringProp_ReplaceStringInCache()
		{
			CheckDisposed();
			ITsPropsBldr propsBldr = TsPropsBldrClass.Create();
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			propsBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "Verse");
			strBldr.Replace(0, 0, "StringPropTest", propsBldr.GetTextProps());
			ITsString tsString = strBldr.GetString();
			m_IVwCacheDa.CacheStringProp(1118, 2228, tsString);
			strBldr.Replace(0, 0, "Second", propsBldr.GetTextProps());
			tsString = strBldr.GetString();
			m_IVwCacheDa.CacheStringProp(1118, 2228, tsString);
			ITsString tsStringNew = m_ISilDataAccess.get_StringProp(1118, 2228);
			Assert.AreEqual(tsString, tsStringNew);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting/setting StringFields properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void StringFields_Simple()
		{
			CheckDisposed();
			ITsPropsBldr propsBldr = TsPropsBldrClass.Create();
			propsBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "Verse");
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			// Test StringFields (which are basically the same, except that the
			// format of the parameters is different)
			strBldr.Replace(0, 0, "Third", propsBldr.GetTextProps());
			ITsString tsString = strBldr.GetString();
			using (ArrayPtr arrayPtr = MarshalEx.ArrayToNative(1000, typeof(byte)))
			{
				int cbFmt = tsString.SerializeFmtRgb(arrayPtr, 1000);
				byte[] rgbFmt = (byte[])MarshalEx.NativeToArray(arrayPtr, cbFmt, typeof(byte));
				m_IVwCacheDa.CacheStringFields(1118, 2228, tsString.Text,
					tsString.Length, rgbFmt, cbFmt);
				ITsString tsStringNew = m_ISilDataAccess.get_StringProp(1118, 2228);
				Assert.AreEqual(tsString.Text, tsStringNew.Text);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests replacinng a StringField property and getting it again
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void StringFields_Replace()
		{
			CheckDisposed();
			ITsPropsBldr propsBldr = TsPropsBldrClass.Create();
			propsBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "Verse");
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			// Test StringFields (which are basically the same, except that the
			// format of the parameters is different)
			strBldr.Replace(0, 0, "Third", propsBldr.GetTextProps());
			ITsString tsString = strBldr.GetString();
			using (ArrayPtr arrayPtr = MarshalEx.ArrayToNative(1000, typeof(byte)))
			{
				int cbFmt = tsString.SerializeFmtRgb(arrayPtr, 1000);
				byte[] rgbFmt = (byte[])MarshalEx.NativeToArray(arrayPtr, cbFmt, typeof(byte));
				m_IVwCacheDa.CacheStringFields(1118, 2228, tsString.Text,
					tsString.Length, rgbFmt, cbFmt);
				strBldr.Replace(0, 5, "Fourth", propsBldr.GetTextProps());
				tsString = strBldr.GetString();

				m_IVwCacheDa.CacheStringFields(1118, 2228, tsString.Text,
					tsString.Length, rgbFmt, cbFmt);

				ITsString tsStringNew = m_ISilDataAccess.get_StringProp(1118, 2228);

				Assert.AreEqual(tsString.Text, tsStringNew.Text);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting/setting the unicode prop
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UnicodeProp()
		{
			CheckDisposed();
			string strNew = m_ISilDataAccess.get_UnicodeProp(1119, 2229);
			Assert.IsNull(strNew);

			string str = "UnicodeTest";
			m_IVwCacheDa.CacheUnicodeProp(1119, 2229, str, str.Length);
			strNew = m_ISilDataAccess.get_UnicodeProp(1119, 2229);
			Assert.AreEqual(str, strNew);

			str = "SecondUnicodeTest";
			m_IVwCacheDa.CacheUnicodeProp(1119, 2229, str, str.Length);
			strNew = m_ISilDataAccess.get_UnicodeProp(1119, 2229);
			Assert.AreEqual(str, strNew);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting/setting the unknown property
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UnknownProp()
		{
			CheckDisposed();
			object obj = m_ISilDataAccess.get_UnknownProp(1120, 2220);
			Assert.IsNull(obj);

			ITsPropsBldr propsBldr = TsPropsBldrClass.Create();
			ITsTextProps ttp = propsBldr.GetTextProps();
			m_IVwCacheDa.CacheUnknown(1120, 2220, ttp);
			obj = m_ISilDataAccess.get_UnknownProp(1120, 2220);
			Assert.AreEqual(ttp, obj);
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
			Assert.AreEqual(expValues[0], hvoVal);

			int chvo = 99;
			using (ArrayPtr arrayPtr = MarshalEx.ArrayToNative(10, typeof(int)))
			{
				m_ISilDataAccess.VecProp(hvo, tag, 10, out chvo, arrayPtr);
				if (expValues[1] is int[])
					Assert.AreEqual(((int[])expValues[1]).Length, chvo);
				else
					Assert.AreEqual(expValues[1], chvo);

				m_ISilDataAccess.BinaryPropRgb(hvo, tag, arrayPtr, 10, out chvo);
				if (expValues[2] is byte[])
					Assert.AreEqual(((byte[])expValues[2]).Length, chvo);
				else
					Assert.AreEqual(expValues[2], chvo);

				Guid guidNew = m_ISilDataAccess.get_GuidProp(hvo, tag);
				Assert.AreEqual(expValues[3], guidNew);

				long valLong = m_ISilDataAccess.get_Int64Prop(hvo, tag);
				Assert.AreEqual(expValues[4], valLong);

				// Int64 and TimeProp use the same cache
				valLong = m_ISilDataAccess.get_TimeProp(hvo, tag);
				Assert.AreEqual(expValues[4], valLong);

				int valInt = m_ISilDataAccess.get_IntProp(hvo, tag);
				Assert.AreEqual(expValues[5], valInt);

				ITsString tsStringNew = m_ISilDataAccess.get_MultiStringAlt(hvo, tag, 12345);
				Assert.AreEqual(expValues[6], tsStringNew.Text);

				tsStringNew = m_ISilDataAccess.get_StringProp(hvo, tag);
				Assert.AreEqual(expValues[7], tsStringNew.Text);

				string strNew = m_ISilDataAccess.get_UnicodeProp(hvo, tag);
				Assert.AreEqual(expValues[8], strNew);

				object obj = m_ISilDataAccess.get_UnknownProp(hvo, tag);
				Assert.AreEqual(expValues[9], obj);

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
			for (CellarModuleDefns cpt = CellarModuleDefns.kcptNil;
				cpt <= CellarModuleDefns.kcptReferenceSequence; cpt++)
			{
				bool flag = false;
				switch (cpt)
				{
					case CellarModuleDefns.kcptNil:
						try
						{
							Assert.IsFalse(m_ISilDataAccess.get_IsPropInCache(hvo, tag,
								(int)cpt, 0));
						}
						catch (ArgumentException)
						{
						}
						continue;
					case CellarModuleDefns.kcptBoolean:
					case CellarModuleDefns.kcptInteger:
					case CellarModuleDefns.kcptNumeric:
						flag = ((int)expValues[5] != 0);
						break;
					case CellarModuleDefns.kcptFloat:
						// Never cached so far
						// TODO: We expect this to fail the test for VwCacheDa because the existing
						// implementation fails to set the return value to false. Need to fix this
						// at line 520 of VwCacheDa.cpp.
						break;
					case CellarModuleDefns.kcptTime:
						flag = (expValues[4] is long || (int)expValues[4] != 0);
						break;
					case CellarModuleDefns.kcptGuid:
						flag = ((Guid)expValues[3] != Guid.Empty);
						break;
					case CellarModuleDefns.kcptImage:
					case CellarModuleDefns.kcptGenDate:
						// Never cached so far
						// TODO: We expect this to fail the test for VwCacheDa because the existing
						// implementation fails to set the return value to false. Need to fix this
						// at line 535 of VwCacheDa.cpp.
						break;
					case CellarModuleDefns.kcptBinary:
						flag = (expValues[2] is byte[]);
						break;
					case CellarModuleDefns.kcptMultiString:
					case CellarModuleDefns.kcptMultiBigString:
					case CellarModuleDefns.kcptMultiUnicode:
					case CellarModuleDefns.kcptMultiBigUnicode:
						flag = (expValues[6] != null);
						break;
					case CellarModuleDefns.kcptString:
					case CellarModuleDefns.kcptBigString:
						flag = (expValues[7] != null);
						break;
					case CellarModuleDefns.kcptUnicode:
					case CellarModuleDefns.kcptBigUnicode:
						flag = (expValues[8] != null);
						break;
					case CellarModuleDefns.kcptOwningAtom:
					case CellarModuleDefns.kcptReferenceAtom:
						flag = ((int)expValues[0] != 0);
						break;
					case CellarModuleDefns.kcptOwningCollection:
					case CellarModuleDefns.kcptReferenceCollection:
					case CellarModuleDefns.kcptOwningSequence:
					case CellarModuleDefns.kcptReferenceSequence:
						flag = (expValues[1] is int[]);
						break;
					default:
						continue;
				}
				Assert.AreEqual(flag, m_ISilDataAccess.get_IsPropInCache(hvo, tag, (int)cpt, 12345),
					string.Format("IsPropInCache for property type '{0}' failed;", cpt));
			}
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that the cache returns only object types that we put in, and the IsPropInCache
		/// method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void KeyCheck()
		{
			CheckDisposed();
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

			ITsPropsBldr propsBldr = TsPropsBldrClass.Create();
			ITsStrBldr strBldr = TsStrBldrClass.Create();
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
			CheckDisposed();

			int objFlid = (int)CmObjectFields.kflidCmObject_Guid;
			int nonObjFlid = (int)SIL.FieldWorks.FDO.Cellar.CmFilter.CmFilterTags.kflidApp;
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
			Assert.AreEqual(objHvo1, m_ISilDataAccess.get_ObjFromGuid(guid));

			m_IVwCacheDa.ClearAllData();

			// Now cache the guids in the reverse order from the order done above.
			m_IVwCacheDa.CacheGuidProp(objHvo2, nonObjFlid, guid);
			m_IVwCacheDa.CacheGuidProp(objHvo1, objFlid, guid);

			// Make sure the same flid is returned when the caching is reversed.
			Assert.AreEqual(objHvo1, m_ISilDataAccess.get_ObjFromGuid(guid));
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
			CheckDisposed();

			int objFlid = (int)CmObjectFields.kflidCmObject_Guid;
			int nonObjFlid = (int)SIL.FieldWorks.FDO.Cellar.CmFilter.CmFilterTags.kflidApp;
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
			Assert.AreEqual(objHvo1, m_ISilDataAccess.get_ObjFromGuid(guid));

			m_IVwCacheDa.ClearAllData();

			// Now save the guids in the reverse order from the order saved above.
			m_ISilDataAccess.SetGuid(objHvo2, nonObjFlid, guid);
			m_ISilDataAccess.SetGuid(objHvo1, objFlid, guid);

			// Make sure the same flid is returned when the saving is reversed.
			Assert.AreEqual(objHvo1, m_ISilDataAccess.get_ObjFromGuid(guid));
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
			CheckDisposed();

			int objFlid = (int)CmObjectFields.kflidCmObject_Guid;
			int nonObjFlid = (int)SIL.FieldWorks.FDO.Cellar.CmFilter.CmFilterTags.kflidApp;
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
			Assert.AreEqual(objHvo1, m_ISilDataAccess.get_ObjFromGuid(guid));
		}
	}

	#region Test the C++ VwCacheDa class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Test the C++ VwCacheDa class (IVwCacheDa part).
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	//[Ignore("Usually we don't need to test the C++ code here")]
	public class IVwCacheDaCppTests : IVwCacheDaTests
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a new <see cref="VwCacheDaClass"/> object that creates a cache.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public void SetUp()
		{
			CheckDisposed();
			m_ISilDataAccess = VwCacheDaClass.Create();
			// It must have a writing system factory: for testing, use the memory based one.
			ILgWritingSystemFactory wsf = LgWritingSystemFactoryClass.Create();
			// For these tests we don't need to run InstallLanguage.
			wsf.BypassInstall = true;
			m_ISilDataAccess.WritingSystemFactory = wsf;
			m_IVwCacheDa = (IVwCacheDa)m_ISilDataAccess;
		}

	}
	#endregion

	#region Test the C# IVwCacheDa
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Test the C# CacheBase class (IVwCacheDa part)
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class IVwCacheDaCSharpTests : IVwCacheDaTests
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a new <see cref="CacheBase"/> object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public void SetUp()
		{
			CheckDisposed();
			if (m_IVwCacheDa != null)
				(m_IVwCacheDa as IDisposable).Dispose();

			m_IVwCacheDa = (IVwCacheDa)new CacheBase(null);
			m_ISilDataAccess = (ISilDataAccess)m_IVwCacheDa;
			// It must have a writing system factory: for testing, use the memory based one.
			ILgWritingSystemFactory wsf = LgWritingSystemFactoryClass.Create();
			// For these tests we don't need to run InstallLanguage.
			wsf.BypassInstall = true;
			m_ISilDataAccess.WritingSystemFactory = wsf;
		}
	}
	#endregion
}
