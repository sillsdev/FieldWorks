// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2002, SIL International. All Rights Reserved.
// <copyright from='2002' to='2002' company='SIL International'>
//		Copyright (c) 2002, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ISilDataAccessTests.cs
// Responsibility: TE Team
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
using SIL.FieldWorks.Test.TestUtils;

namespace SIL.FieldWorks.FDO.FDOTests.Cache
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Test the ISilDataAccess interface
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ISilDataAccessTests : BaseTest
	{
		/// <summary>The ISilDataAccess object</summary>
		protected ISilDataAccess m_ISilDataAccess;

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
				if (m_ISilDataAccess != null)
				{
					if (m_ISilDataAccess.WritingSystemFactory != null)
						m_ISilDataAccess.WritingSystemFactory.Shutdown();
					(m_ISilDataAccess as IDisposable).Dispose();
				}
			}
			// Dispose unmanaged resources here, whether disposing is true or false.
			m_ISilDataAccess = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the Prop method.
		/// </summary>
		/// <param name="hvo">HVO part of the key</param>
		/// <param name="tag">tag part of the key</param>
		/// <param name="expValue">Expected value</param>
		/// <param name="type">The field type of <paramref name="expValue"/></param>
		/// <remarks>The C++ implementation only supports 32 and 64bit integers and
		/// (non-multilingual) strings.</remarks>
		/// ------------------------------------------------------------------------------------
		protected virtual void CheckProp(int hvo, int tag, object expValue,
			CellarModuleDefns type)
		{
			object val = m_ISilDataAccess.get_Prop(hvo, tag);
			if (type == CellarModuleDefns.kcptTime
				|| type == CellarModuleDefns.kcptInteger
				|| type == CellarModuleDefns.kcptString)
				Assert.AreEqual(expValue, val);
			else
				Assert.AreEqual(null, val);
		}

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
			Assert.IsFalse(m_ISilDataAccess.IsDirty());

			m_ISilDataAccess.SetObjProp(1000, 2000, 7777);
			hvo = m_ISilDataAccess.get_ObjectProp(1000, 2000);
			Assert.AreEqual(7777, hvo);
			Assert.IsTrue(m_ISilDataAccess.IsDirty());

			m_ISilDataAccess.SetObjProp(1000, 2000, 8888);
			hvo = m_ISilDataAccess.get_ObjectProp(1000, 2000);
			Assert.AreEqual(8888, hvo);
			Assert.IsTrue(m_ISilDataAccess.IsDirty());

			CheckProp(1000, 2000, 8888, CellarModuleDefns.kcptOwningAtom);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test getting/setting a vector of hvos
		/// </summary>
		/// <remarks>There is no SetVecProp method on ISilDataAccess. However, we include
		/// this test here (and use IVwCacheDa to add to the cache) so that we can call
		/// <see cref="CheckProp"/>.</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VecProp()
		{
			CheckDisposed();
			if (!(m_ISilDataAccess is IVwCacheDa))
				return; // nothing we can test right here

			int[] rgHvo = new int[] { 33, 44, 55 };
			((IVwCacheDa)m_ISilDataAccess).CacheVecProp(1001, 2001, rgHvo, rgHvo.Length);

			CheckProp(1001, 2001, rgHvo, CellarModuleDefns.kcptOwningCollection);
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
				m_ISilDataAccess.BinaryPropRgb(1112, 2221, arrayPtr, 10, out chvo);
				Assert.AreEqual(0, chvo);
				Assert.IsFalse(m_ISilDataAccess.IsDirty());

				byte[] prgb = new byte[] { 3, 4, 5 };
				m_ISilDataAccess.SetBinary(1112, 2221, prgb, prgb.Length);
				m_ISilDataAccess.BinaryPropRgb(1112, 2221, ArrayPtr.Null, 0, out chvo);
				Assert.AreEqual(prgb.Length, chvo);

				m_ISilDataAccess.BinaryPropRgb(1112, 2221, arrayPtr, 10, out chvo);
				byte[] prgbNew = (byte[])MarshalEx.NativeToArray(arrayPtr, chvo, typeof(byte));
				Assert.AreEqual(prgb.Length, prgbNew.Length);
				for (int i = 0; i < prgbNew.Length; i++)
					Assert.AreEqual(prgb[i], prgbNew[i]);
				Assert.IsTrue(m_ISilDataAccess.IsDirty());

				byte[] prgb2 = new byte[] { 6, 7, 8, 9 };
				m_ISilDataAccess.SetBinary(1112, 2221, prgb2, prgb2.Length);
				m_ISilDataAccess.BinaryPropRgb(1112, 2221, arrayPtr, 10, out chvo);
				prgbNew = (byte[])MarshalEx.NativeToArray(arrayPtr, chvo, typeof(byte));
				Assert.AreEqual(prgb2.Length, prgbNew.Length);
				for (int i = 0; i < prgbNew.Length; i++)
					Assert.AreEqual(prgb2[i], prgbNew[i]);
				Assert.IsTrue(m_ISilDataAccess.IsDirty());

				Exception ex = null;
				try
				{
					m_ISilDataAccess.BinaryPropRgb(1112, 2221, arrayPtr, 2, out chvo);
				}
				catch (Exception e)
				{
					ex = e;
				}
				Assert.AreEqual(typeof(System.Runtime.InteropServices.COMException),
					ex.GetType());
				Assert.IsTrue(m_ISilDataAccess.IsDirty());

				CheckProp(1112, 2221, prgb2, CellarModuleDefns.kcptBinary);
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
			Assert.IsFalse(m_ISilDataAccess.IsDirty());

			Guid guid = new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11);
			m_ISilDataAccess.SetGuid(1113, 2223, guid);
			guidNew = m_ISilDataAccess.get_GuidProp(1113, 2223);
			Assert.AreEqual(guid, guidNew);
			Assert.IsTrue(m_ISilDataAccess.IsDirty());

			Guid guid2 = new Guid(10, 12, 13, 14, 15, 16, 17, 18, 19, 110, 111);
			m_ISilDataAccess.SetGuid(1113, 2223, guid2);
			guidNew = m_ISilDataAccess.get_GuidProp(1113, 2223);
			Assert.AreEqual(guid2, guidNew);
			Assert.IsTrue(m_ISilDataAccess.IsDirty());

			CheckProp(1113, 2223, guid2, CellarModuleDefns.kcptGuid);
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
			Assert.IsFalse(m_ISilDataAccess.IsDirty());

			m_ISilDataAccess.SetInt64(1114, 2224, long.MaxValue);
			valNew = m_ISilDataAccess.get_Int64Prop(1114, 2224);
			Assert.AreEqual(long.MaxValue, valNew);
			Assert.IsTrue(m_ISilDataAccess.IsDirty());

			m_ISilDataAccess.SetInt64(1114, 2224, long.MinValue);
			valNew = m_ISilDataAccess.get_Int64Prop(1114, 2224);
			Assert.AreEqual(long.MinValue, valNew);
			Assert.IsTrue(m_ISilDataAccess.IsDirty());

			CheckProp(1114, 2224, long.MinValue, CellarModuleDefns.kcptTime);
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
			Assert.IsFalse(m_ISilDataAccess.IsDirty());

			m_ISilDataAccess.SetInt(1115, 2225, int.MaxValue);
			valNew = m_ISilDataAccess.get_IntProp(1115, 2225);
			Assert.AreEqual(int.MaxValue, valNew);
			Assert.IsTrue(m_ISilDataAccess.IsDirty());

			m_ISilDataAccess.SetInt(1115, 2225, int.MinValue);
			valNew = m_ISilDataAccess.get_IntProp(1115, 2225);
			Assert.AreEqual(int.MinValue, valNew);
			Assert.IsTrue(m_ISilDataAccess.IsDirty());

			CheckProp(1115, 2225, int.MinValue, CellarModuleDefns.kcptInteger);
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
			Assert.IsFalse(m_ISilDataAccess.IsDirty());

			m_ISilDataAccess.SetTime(1116, 2226, DateTime.MaxValue.Ticks);
			valNew = m_ISilDataAccess.get_TimeProp(1116, 2226);
			Assert.AreEqual(DateTime.MaxValue.Ticks, valNew);
			Assert.IsTrue(m_ISilDataAccess.IsDirty());

			m_ISilDataAccess.SetTime(1116, 2226, DateTime.MinValue.Ticks);
			valNew = m_ISilDataAccess.get_TimeProp(1116, 2226);
			Assert.AreEqual(DateTime.MinValue.Ticks, valNew);
			Assert.IsTrue(m_ISilDataAccess.IsDirty());

			CheckProp(1116, 2226, DateTime.MinValue.Ticks, CellarModuleDefns.kcptTime);
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
			Assert.AreEqual(0, tsStringNew.Length);
			Assert.IsFalse(m_ISilDataAccess.IsDirty());

			ITsPropsBldr propsBldr = TsPropsBldrClass.Create();
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			propsBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "Verse");
			strBldr.Replace(0, 0, "Test", propsBldr.GetTextProps());
			ITsString tsString = strBldr.GetString();
			m_ISilDataAccess.SetMultiStringAlt(1117, 2227, 7, tsString);
			tsStringNew = m_ISilDataAccess.get_MultiStringAlt(1117, 2227, 7);
			Assert.AreEqual(tsString, tsStringNew);
			Assert.IsTrue(m_ISilDataAccess.IsDirty());

			strBldr.Replace(0, 0, "SecondTest", propsBldr.GetTextProps());
			tsString = strBldr.GetString();
			m_ISilDataAccess.SetMultiStringAlt(1117, 2227, 7, tsString);
			tsStringNew = m_ISilDataAccess.get_MultiStringAlt(1117, 2227, 7);
			Assert.AreEqual(tsString, tsStringNew);
			Assert.IsTrue(m_ISilDataAccess.IsDirty());

			tsStringNew = m_ISilDataAccess.get_MultiStringAlt(1117, 2227, 8);
			Assert.AreEqual(0, tsStringNew.Length);
			Assert.IsTrue(m_ISilDataAccess.IsDirty());

			CheckProp(1117, 2227, tsString, CellarModuleDefns.kcptMultiString);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting/setting String properties and StringFields properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void StringProp()
		{
			CheckDisposed();
			// Test StringProp
			ITsString tsStringNew = m_ISilDataAccess.get_StringProp(1118, 2228);
			Assert.AreEqual(0, tsStringNew.Length);
			Assert.IsFalse(m_ISilDataAccess.IsDirty());

			ITsPropsBldr propsBldr = TsPropsBldrClass.Create();
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			propsBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "Verse");
			strBldr.Replace(0, 0, "StringPropTest", propsBldr.GetTextProps());
			ITsString tsString = strBldr.GetString();
			m_ISilDataAccess.SetString(1118, 2228, tsString);
			tsStringNew = m_ISilDataAccess.get_StringProp(1118, 2228);
			Assert.AreEqual(tsString, tsStringNew);
			Assert.IsTrue(m_ISilDataAccess.IsDirty());

			strBldr.Replace(0, 0, "Second", propsBldr.GetTextProps());
			tsString = strBldr.GetString();
			m_ISilDataAccess.SetString(1118, 2228, tsString);
			tsStringNew = m_ISilDataAccess.get_StringProp(1118, 2228);
			Assert.AreEqual(tsString, tsStringNew);
			Assert.IsTrue(m_ISilDataAccess.IsDirty());

			CheckProp(1118, 2228, tsString, CellarModuleDefns.kcptString);
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
			Assert.IsFalse(m_ISilDataAccess.IsDirty());

			string str = "UnicodeTest";
			m_ISilDataAccess.SetUnicode(1119, 2229, str, str.Length);
			strNew = m_ISilDataAccess.get_UnicodeProp(1119, 2229);
			Assert.AreEqual(str, strNew);
			Assert.IsTrue(m_ISilDataAccess.IsDirty());

			str = "SecondUnicodeTest";
			m_ISilDataAccess.SetUnicode(1119, 2229, str, str.Length);
			strNew = m_ISilDataAccess.get_UnicodeProp(1119, 2229);
			Assert.AreEqual(str, strNew);
			Assert.IsTrue(m_ISilDataAccess.IsDirty());

			str = "ThirdUnicodeTest";
			m_ISilDataAccess.set_UnicodeProp(1119, 2229, str);
			int cch;
			using (ArrayPtr arrayPtr = MarshalEx.StringToNative(100, true))
			{
				m_ISilDataAccess.UnicodePropRgch(1119, 2229, arrayPtr, 100, out cch);
				strNew = MarshalEx.NativeToString(arrayPtr, cch, true);
				Assert.AreEqual(str, strNew);
				Assert.AreEqual(str.Length, cch);
				Assert.IsTrue(m_ISilDataAccess.IsDirty());

				m_ISilDataAccess.UnicodePropRgch(1119, 2229, ArrayPtr.Null, 0, out cch);
				Assert.AreEqual(str.Length, cch);

				CheckProp(1119, 2229, str, CellarModuleDefns.kcptUnicode);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting/setting the unicode prop
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(COMException))]
		public void UnicodeProp_BufferToSmall()
		{
			CheckDisposed();
			string str = "ThirdUnicodeTest";
			m_ISilDataAccess.set_UnicodeProp(1119, 2229, str);
			int cch;
			using (ArrayPtr arrayPtr = MarshalEx.StringToNative(100, true))
			{
				m_ISilDataAccess.UnicodePropRgch(1119, 2229, arrayPtr, 1, out cch);
			}
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
			Assert.IsFalse(m_ISilDataAccess.IsDirty());

			ITsPropsBldr propsBldr = TsPropsBldrClass.Create();
			ITsTextProps ttp = propsBldr.GetTextProps();
			m_ISilDataAccess.SetUnknown(1120, 2220, ttp);
			obj = m_ISilDataAccess.get_UnknownProp(1120, 2220);
			Assert.AreEqual(ttp, obj);
			Assert.IsTrue(m_ISilDataAccess.IsDirty());
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
				bool fExpected = false;
				switch (cpt)
				{
					case CellarModuleDefns.kcptNil:
						try
						{
							Assert.IsFalse(m_ISilDataAccess.get_IsPropInCache(hvo, tag,
								(int)cpt, 0));
						}
						catch(ArgumentException)
						{
						}
						continue;
					case CellarModuleDefns.kcptBoolean:
					case CellarModuleDefns.kcptInteger:
					case CellarModuleDefns.kcptNumeric:
						fExpected = ((int)expValues[5] != 0);
						break;
					case CellarModuleDefns.kcptFloat:
						// Never cached so far
						// TODO: We expect this to fail the test for VwCacheDa because the existing
						// implementation fails to set the return value to false. Need to fix this
						// at line 520 of VwCacheDa.cpp.
						break;
					case CellarModuleDefns.kcptTime:
						fExpected = (expValues[4] is long || (int)expValues[4] != 0);
						break;
					case CellarModuleDefns.kcptGuid:
						fExpected = ((Guid)expValues[3] != Guid.Empty);
						break;
					case CellarModuleDefns.kcptImage:
					case CellarModuleDefns.kcptGenDate:
						// Never cached so far
						// TODO: We expect this to fail the test for VwCacheDa because the existing
						// implementation fails to set the return value to false. Need to fix this
						// at line 535 of VwCacheDa.cpp.
						break;
					case CellarModuleDefns.kcptBinary:
						fExpected = (expValues[2] is byte[]);
							break;
					case CellarModuleDefns.kcptMultiString:
					case CellarModuleDefns.kcptMultiBigString:
					case CellarModuleDefns.kcptMultiUnicode:
					case CellarModuleDefns.kcptMultiBigUnicode:
						fExpected = (expValues[6] != null);
						break;
					case CellarModuleDefns.kcptString:
					case CellarModuleDefns.kcptBigString:
						fExpected = (expValues[7] != null);
						break;
					case CellarModuleDefns.kcptUnicode:
					case CellarModuleDefns.kcptBigUnicode:
						fExpected = (expValues[8] != null);
						break;
					case CellarModuleDefns.kcptOwningAtom:
					case CellarModuleDefns.kcptReferenceAtom:
						fExpected = ((int)expValues[0] != 0);
						break;
					case CellarModuleDefns.kcptOwningCollection:
					case CellarModuleDefns.kcptReferenceCollection:
					case CellarModuleDefns.kcptOwningSequence:
					case CellarModuleDefns.kcptReferenceSequence:
						fExpected = (expValues[1] is int[]);
							break;
					default:
						continue;
				}
				Assert.AreEqual(fExpected, m_ISilDataAccess.get_IsPropInCache(hvo, tag, (int)cpt, 12345),
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
			m_ISilDataAccess.SetObjProp(1121, 2221, 7777);
			VerifyCache(1121, 2221,
				new object[] { 7777, 0, 0, Guid.Empty, 0, 0, null, null, null, null});

			byte[] prgb = new byte[] { 3, 4, 5 };
			m_ISilDataAccess.SetBinary(1123, 2223, prgb, prgb.Length);
			VerifyCache(1123, 2223,
				new object[] { 0, 0, prgb, Guid.Empty, 0, 0, null, null, null, null });

			Guid guid = new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11);
			m_ISilDataAccess.SetGuid(1124, 2224, guid);
			VerifyCache(1124, 2224,
				new object[] { 0, 0, 0, guid, 0, 0, null, null, null, null });

			m_ISilDataAccess.SetInt64(1125, 2225, 123456789);
			VerifyCache(1125, 2225,
				new object[] { 0, 0, 0, Guid.Empty, 123456789, 0, null, null, null, null });

			// TimeProp uses the same cache as Int64
			long ticks = DateTime.Now.Ticks;
			m_ISilDataAccess.SetTime(1127, 2227, ticks);
			VerifyCache(1127, 2227,
				new object[] { 0, 0, 0, Guid.Empty, ticks, 0, null, null, null, null });

			m_ISilDataAccess.SetInt(1126, 2226, 987654);
			VerifyCache(1126, 2226,
				new object[] { 0, 0, 0, Guid.Empty, 0, 987654, null, null, null, null });

			ITsPropsBldr propsBldr = TsPropsBldrClass.Create();
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			propsBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "Verse");
			strBldr.Replace(0, 0, "KeyTestMulti", propsBldr.GetTextProps());
			ITsString tsString = strBldr.GetString();
			m_ISilDataAccess.SetMultiStringAlt(1128, 2228, 12345, tsString);
			VerifyCache(1128, 2228,
				new object[] { 0, 0, 0, Guid.Empty, 0, 0, tsString.Text, null, null, null });

			strBldr.Replace(0, 0, "String", propsBldr.GetTextProps());
			tsString = strBldr.GetString();
			m_ISilDataAccess.SetString(1129, 2229, tsString);
			VerifyCache(1129, 2229,
				new object[] { 0, 0, 0, Guid.Empty, 0, 0, null, tsString.Text, null, null });

			string str = "KeyTestUnicode";
			m_ISilDataAccess.SetUnicode(1130, 2230, str, str.Length);
			VerifyCache(1130, 2230,
				new object[] { 0, 0, 0, Guid.Empty, 0, 0, null, null, str, null });

			ITsTextProps ttp = propsBldr.GetTextProps();
			m_ISilDataAccess.SetUnknown(1131, 2230, ttp);
			VerifyCache(1131, 2230,
				new object[] { 0, 0, 0, Guid.Empty, 0, 0, null, null, null, ttp});

		}
	}

	#region Test the C++ VwCacheDa class (ISilDataAccess part)
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Test the C++ VwCacheDa class (ISilDataAccess part)
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	//[TestFixture]
	//[Ignore("Usually we don't need to test the C++ code here")]
	public class ISilDataAccessCppTests : ISilDataAccessTests
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
		}

	}
	#endregion

	#region Test the C# CacheBase class (ISilDataAccess part)
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Test the C# CacheBase class (ISilDataAccess part)
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class ISilDataAccessCSharpTests : ISilDataAccessTests
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
			if (m_ISilDataAccess != null && (m_ISilDataAccess is IDisposable))
				(m_ISilDataAccess as IDisposable).Dispose();
			m_ISilDataAccess = (ISilDataAccess)new CacheBase(null);
			// It must have a writing system factory: for testing, use the memory based one.
			ILgWritingSystemFactory wsf = LgWritingSystemFactoryClass.Create();
			// For these tests we don't need to run InstallLanguage.
			wsf.BypassInstall = true;
			m_ISilDataAccess.WritingSystemFactory = wsf;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the Prop method.
		/// </summary>
		/// <param name="hvo">HVO part of the key</param>
		/// <param name="tag">tag part of the key</param>
		/// <param name="expValue">Expected value</param>
		/// <param name="type">The field type of <paramref name="expValue"/></param>
		/// ------------------------------------------------------------------------------------
		protected override void CheckProp(int hvo, int tag, object expValue,
			CellarModuleDefns type)
		{
			object val = m_ISilDataAccess.get_Prop(hvo, tag);
			if (type == CellarModuleDefns.kcptMultiString)
				// Multistring props need an additional encoding, so get_Prop always returns
				// null for kcptMultiString
				Assert.AreEqual(null, val);
			else
				Assert.AreEqual(expValue, val);
		}
	}
	#endregion
}
