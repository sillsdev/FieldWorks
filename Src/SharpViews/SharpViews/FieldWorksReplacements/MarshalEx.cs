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
// File: MarshalEx.cs
// Responsibility: Eberhard Beilharz
// Last reviewed:
//
// <remarks>
// Implements helper methods for marshaling of COM objects
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SIL.FieldWorks.Common.COMInterfaces
{
	/// <summary>
	/// Helper methods for marshalling of COM objects
	/// </summary>
	public static class MarshalEx
	{
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Calculates the size of an object of <paramref name="type"/>
		/// </summary>
		/// <param name="type">type</param>
		/// <returns>Size of type</returns>
		/// -----------------------------------------------------------------------------------
		static private int SizeOf(Type type)
		{
			if (type.IsInterface)
				return IntPtr.Size;
			else
				return Marshal.SizeOf(type);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Allocates memory for <paramref name="nMaxSize"/> elements and returns a pointer
		/// suitable to be passed as array to a COM method.
		/// </summary>
		/// <param name="nMaxSize">Max. number of elements in the array</param>
		/// <param name="elementType">Type of the elements in the array</param>
		/// <returns>Pointer to array</returns>
		/// <remarks>Use this method for an empty array that is passed by
		/// reference.</remarks>
		/// -----------------------------------------------------------------------------------
		static public ArrayPtr ArrayToNative(int nMaxSize, Type elementType)
		{
			return new ArrayPtr(nMaxSize * SizeOf(elementType));
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Re-Allocates memory for <paramref name="nMaxSize"/> elements and returns a pointer suitable to be
		/// passed as array to a COM method.
		/// </summary>
		/// <param name="arrayPtr">Pointer with previously allocated memory</param>
		/// <param name="nMaxSize">Max. number of elements in the array</param>
		/// <param name="elementType">Type of the elements in the array</param>
		/// -----------------------------------------------------------------------------------
		static public void ArrayToNative(ref ArrayPtr arrayPtr, int nMaxSize, Type elementType)
		{
			arrayPtr.Resize(nMaxSize * SizeOf(elementType));
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Converts a managed array to a pointer to an unmanaged array that can
		/// be passed to a COM method.
		/// </summary>
		/// <param name="array">Managed array</param>
		/// <returns>Pointer to unmanaged array</returns>
		/// <remarks>This method is only necessary for [out] or [in,out] arrays. For
		/// [in] arrays the .NET marshalling works.</remarks>
		/// -----------------------------------------------------------------------------------
		static public ArrayPtr ArrayToNative(Array array)
		{
			Type elementType = array.GetType().GetElementType();
			int elemSize = SizeOf(elementType);
			ArrayPtr unmanagedObj = new ArrayPtr(array.Length * elemSize);
			CopyElements(unmanagedObj, array);

			return unmanagedObj;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Converts a managed array to a pointer to an unmanaged array that can
		/// be passed to a COM method.
		/// </summary>
		/// <param name="unmanagedObj">Unamanged array pointer</param>
		/// <param name="nMaxSize">Maximum size of the array</param>
		/// <param name="array">Managed array</param>
		/// <remarks>This method is only necessary for [out] or [in,out] arrays. For
		/// [in] arrays the .NET marshalling works.</remarks>
		/// -----------------------------------------------------------------------------------
		static public void ArrayToNative(ArrayPtr unmanagedObj, int nMaxSize, Array array)
		{
			Debug.Assert(array.Length <= nMaxSize);
			CopyElements(unmanagedObj, array);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copy the elements of the <paramref name="array"/> to the unamanged pointer.
		/// </summary>
		/// <param name="unmanagedObj">Unmanaged array pointer</param>
		/// <param name="array">Managed array</param>
		/// ------------------------------------------------------------------------------------
		static private void CopyElements(ArrayPtr unmanagedObj, Array array)
		{
			Type elementType = array.GetType().GetElementType();
			int elemSize = SizeOf(elementType);

			IntPtr current = (IntPtr)unmanagedObj;
			if (elementType.IsValueType)
			{
				foreach (object obj in array)
				{
					Marshal.StructureToPtr(obj, current, true);
					current = (IntPtr)((ulong)current + (ulong)elemSize);
				}
			}
			else
			{
				int i = 0;
				foreach(object obj in array)
				{
					if (obj == null)
						Marshal.WriteIntPtr(current, i * elemSize, IntPtr.Zero);
					else
						Marshal.WriteIntPtr(current, i * elemSize, Marshal.GetIUnknownForObject(obj));
					i++;
				}
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Converts an unmanaged array from a COM method to a managed array
		/// </summary>
		/// <param name="nativeData">Pointer to unmanaged array</param>
		/// <param name="cElem">Number of elements in unmanaged array</param>
		/// <param name="elementType">Type of elements in array</param>
		/// <returns>Managed array</returns>
		/// <remarks>This method is only necessary for [out] or [in,out] arrays. For
		/// [in] arrays the .NET marshalling works.</remarks>
		/// -----------------------------------------------------------------------------------
		static public Array NativeToArray(ArrayPtr nativeData, int cElem, Type elementType)
		{
			int elemSize = SizeOf(elementType);
			IntPtr current = (IntPtr)nativeData;

			if (elementType.IsValueType)
			{
				// For value types, we want to use Array, so that there won't be boxing
				// of IntPtr
				Array array = Array.CreateInstance(elementType, cElem);

				for (int i = 0; i < cElem; i++)
				{
					// for a value type array, the C++ type is type*
					try
					{
						array.SetValue(Marshal.PtrToStructure(current, elementType), i);
					}
					catch (Exception ex)
					{
						// We've had bizarre reports of a null reference inside PtrToStructure.
						// Report everything that remotely might be relevant.
						if (elementType == null)
							throw new Exception("PtrToStructure threw an exception. elementType is null.", ex);
						if (array == null)
							throw new Exception("PtrToStructure threw an exception. array is null.", ex);
						throw new Exception(
							"PtrToStructure threw an exception. elementType = " + elementType
							+ " current = " + current + " cElem = " + cElem + " i = " + i,
							ex);
					}
					current = (IntPtr)((ulong)current + (ulong)elemSize);
				}

				return array;
			}
			else
			{
				// For reference types, we can't use Array, because there we have
				// problems with calling SetValue() with an interface
				object[] array = (object[])Array.CreateInstance(elementType, cElem);

				for (int i = 0; i < cElem; i++)
				{
					// for a reference type array the C++ type is type**
					IntPtr punk = Marshal.ReadIntPtr(current, i * elemSize);
					if (punk == IntPtr.Zero)
						array[i] = null;
					else
					{
						array[i] = Marshal.GetObjectForIUnknown(punk);
						Marshal.Release(punk);
					}
				}

				return array;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Allocates memory for <paramref name="nMaxSize"/> characters in a string and
		/// returns a pointer suitable to be passed as array to a COM method.
		/// </summary>
		/// <param name="nMaxSize">Max. number of elements in the array</param>
		/// <param name="fUnicode"><c>true</c> to allocate an array for wchar, otherwise
		/// allocate array for 8bit characters.</param>
		/// <returns>Pointer to array</returns>
		/// <remarks>Use this method for an empty array that is passed by
		/// reference.</remarks>
		/// ------------------------------------------------------------------------------------
		static public ArrayPtr StringToNative(int nMaxSize, bool fUnicode)
		{
			// Marshal.SizeOf(typeof(char)) always returns 1, so we have to deal with
			// unmanaged wchar ourself...
			// System.Char is 16bit
			if (fUnicode)
				return new ArrayPtr(nMaxSize * 2);
			else
				return new ArrayPtr(nMaxSize * SizeOf(typeof(char)));
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Copies the characters in the string to the unmanaged array.
		/// </summary>
		/// <param name="arrayPtr">Unmanaged array</param>
		/// <param name="nMaxSize">Max. number of elements in the array</param>
		/// <param name="str">Managed string</param>
		/// <param name="fUnicode"><c>true</c> to allocate an array for wchar, otherwise
		/// allocate array for 8bit characters.</param>
		/// ------------------------------------------------------------------------------------
		static public void StringToNative(ArrayPtr arrayPtr, int nMaxSize, string str,
			bool fUnicode)
		{
			Debug.Assert(str.Length < nMaxSize);
			// Marshal.SizeOf(typeof(char)) always returns 1, so we have to deal with
			// unmanaged wchar ourself...
			// System.Char is 16bit
			if (fUnicode)
			{
				int elemSize = SizeOf(typeof(Int16));
				IntPtr current = (IntPtr)arrayPtr;
				for (int i = 0; i < str.Length; i++)
				{
					Marshal.WriteInt16(current, i * elemSize, (short)str[i]);
				}
				Marshal.WriteInt16(current, str.Length * elemSize, 0);
			}
			else
			{
				int elemSize = SizeOf(typeof(byte));
				IntPtr current = (IntPtr)arrayPtr;
				for (int i = 0; i < str.Length; i++)
				{
					Marshal.WriteByte(current, i * elemSize, (byte)str[i]);
				}
				Marshal.WriteByte(current, str.Length * elemSize, 0);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Converts an unmanaged character array to a managed string
		/// </summary>
		/// <param name="nativeData">Pointer to unmanaged array</param>
		/// <param name="cElem">Number of elements in unmanaged array</param>
		/// <param name="fUnicode"><c>true</c> to convert array to an Unicode, otherwise
		/// convert to Ansi.</param>
		/// <returns>Managed string</returns>
		/// <remarks>This method is only necessary for [out] or [in,out] arrays. For
		/// [in] arrays the .NET marshalling works.</remarks>
		/// ------------------------------------------------------------------------------------
		static public string NativeToString(ArrayPtr nativeData, int cElem, bool fUnicode)
		{
			if (fUnicode)
				return Marshal.PtrToStringUni((IntPtr)nativeData);
			else
				return Marshal.PtrToStringAnsi((IntPtr)nativeData);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Converts a ushort[] array of Unicode characters to a string
		/// </summary>
		/// <param name="array">Array of Unicode characters</param>
		/// <returns>String</returns>
		/// ------------------------------------------------------------------------------------
		static public string UShortToString(ushort[] array)
		{
			int nLen = Math.Max(0, Math.Min(array.Length, Array.IndexOf(array, (ushort)0)));
			char[] aString = new char[nLen];
			Array.Copy(array, aString, nLen);
			return new string(aString);
		}

		/// <summary>
		/// Converts a string to a ushort[] array of Unicode characters.
		/// </summary>
		/// <param name="str">The string.</param>
		/// <param name="array">The array of Unicode characters.</param>
		/// <returns></returns>
		static public void StringToUShort(string str, ushort[] array)
		{
			int len = Math.Min(str.Length, array.Length - 1);
			Array.Copy(str.ToCharArray(), array, len);
			array[len] = 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Converts a uint array into a byte array.
		/// Takes no account of endian issues.
		/// </summary>
		/// <param name="array">A uint array</param>
		/// <returns>A byte array</returns>
		/// ------------------------------------------------------------------------------------
		static public byte[] UIntArrayToByteArray(uint[] array)
		{
			byte[] ret = new byte[array.Length * sizeof(uint)];
			for(int i=0 ; i<array.Length ; ++i)
			{
				ret[(i * sizeof(uint)) + 0] = (byte)array[i];
				ret[(i * sizeof(uint)) + 1] = (byte)(array[i] >> 8);
				ret[(i * sizeof(uint)) + 2] = (byte)(array[i] >> 16);
				ret[(i * sizeof(uint)) + 3] = (byte)(array[i] >> 24);
			}

			return ret;
		}
	}
}
