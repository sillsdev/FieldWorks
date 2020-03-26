// Copyright (c) 2007-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using ECInterfaces;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary />
	internal sealed class EncoderInfo
	{
		/// <summary>The name of the encoding converter.</summary>
		internal string Name { get; }

		/// <summary>The converter method, e.g. CC table, TecKit, etc.</summary>
		internal ConverterType Method { get; }

		/// <summary>Name of the file containing the conversion table, etc.</summary>
		internal string FileName { get; }

		/// <summary>Type of conversion, e.g. from legacy to Unicode.</summary>
		internal ConvType FromToType { get; }

		/// <summary />
		/// <param name="name">The name of the encoding converter.</param>
		/// <param name="method">The method, e.g. CC table, TecKit, etc.</param>
		/// <param name="fileName">Name of the file containing the conversion table, etc.</param>
		/// <param name="fromToType">Type of conversion, e.g. from legacy to Unicode.</param>
		internal EncoderInfo(string name, ConverterType method, string fileName, ConvType fromToType)
		{
			Name = name;
			Method = method;
			FileName = fileName;
			FromToType = fromToType;
		}
	}
}