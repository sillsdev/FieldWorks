// Copyright (c) 2007-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using ECInterfaces;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary />
	internal class EncoderInfo
	{
		/// <summary>The name of the encoding converter.</summary>
		public string m_name = string.Empty;
		/// <summary>The converter method, e.g. CC table, TecKit, etc.</summary>
		public ConverterType m_method;
		/// <summary>Name of the file containing the conversion table, etc.</summary>
		public string m_fileName = string.Empty;
		/// <summary>Type of conversion, e.g. from legacy to Unicode.</summary>
		public ConvType m_fromToType;

		/// <summary />
		/// <param name="name">The name of the encoding converter.</param>
		/// <param name="method">The method, e.g. CC table, TecKit, etc.</param>
		/// <param name="fileName">Name of the file containing the conversion table, etc.</param>
		/// <param name="fromToType">Type of conversion, e.g. from legacy to Unicode.</param>
		public EncoderInfo(string name, ConverterType method, string fileName, ConvType fromToType)
		{
			m_name = name;
			m_method = method;
			m_fileName = fileName;
			m_fromToType = fromToType;
		}
	}
}