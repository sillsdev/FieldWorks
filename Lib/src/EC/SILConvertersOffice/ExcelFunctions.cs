using System;
using System.Collections.Generic;
using System.Text;
using ECInterfaces;
using SilEncConverters40;
using System.Runtime.InteropServices;               // for ClassInterface attribute (for VBA)
using Microsoft.Win32;                              // for Registry

namespace SILConvertersOffice
{
	[ClassInterface(ClassInterfaceType.AutoDual), ComVisible(true)]
	public class ConvertFunctions
	{
		protected double result;
		protected string strResult;

		public ConvertFunctions()
		{
		}

		/// <summary>
		/// Use this method to use a system converter ('ConverterName') to convert the value in a
		/// cell ('Input'). You can also optionally specify the direction of conversion (True or False)
		/// and the output normalization form.
		/// </summary>
		/// <param name="Input">cell reference to convert</param>
		/// <param name="ConverterName">friendly name of converter from repository</param>
		/// <param name="Forward">TRUE for forward conversion (default), or FALSE for reverse (for bi-directional converters only)</param>
		/// <returns>the converted string</returns>
		public string ConvertString
			(
			string Input,
			object ConverterName,
			[Optional, DefaultParameterValue(true)] object Forward
			// can't really do this anyway..., [Optional, DefaultParameterValue(NormalizeFlags.None)] object NormalFormOutput
			)
		{
			IEncConverter aEC = GetEncConverters[ConverterName];
			if( !(Forward is System.Reflection.Missing))
				aEC.DirectionForward = (bool)Forward;
			/*
			if( !(NormalFormOutput is System.Reflection.Missing))
				aEC.NormalizeOutput = (NormalizeFlags)NormalFormOutput;
			*/
			strResult = aEC.Convert(Input);
			return strResult;
		}

		// override object.Equals (so we can make it COMVisible = false (so it doesn't show up in Excel)
		[ComVisible(false)]
		public override bool Equals(object obj)
		{
			return base.Equals(obj);
		}

		// override object.GetHashCode (so we can make it COMVisible = false (so it doesn't show up in Excel)
		[ComVisible(false)]
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		// override object.ToString (so we can make it COMVisible = false (so it doesn't show up in Excel)
		[ComVisible(false)]
		public override string ToString()
		{
			return base.ToString();
		}

		// override object.GetHashCode (so we can make it COMVisible = false (so it doesn't show up in Excel)
		// this doesn't work for GetType (apparently) since it was "sealed"
		/*
		[ComVisible(false)]
		protected new Type GetType()
		{
			return base.GetType();
		}
		*/

		private static EncConverters m_aECs = null;

		protected static EncConverters GetEncConverters
		{
			get
			{
				if (m_aECs == null)
					m_aECs = new EncConverters();
				return m_aECs;
			}
		}

		[ComRegisterFunctionAttribute]
		public static void RegisterFunction(Type type)
		{
			Registry.ClassesRoot.CreateSubKey(GetSubKeyName(type));
		}

		[ComUnregisterFunctionAttribute]
		public static void UnregisterFunction(Type type)
		{
			Registry.ClassesRoot.DeleteSubKey(GetSubKeyName(type), false);
		}

		private static string GetSubKeyName(Type type)
		{
			string s = @"CLSID\{" + type.GUID.ToString().ToUpper() + @"}\Programmable";
			return s;
		}
	}
}
