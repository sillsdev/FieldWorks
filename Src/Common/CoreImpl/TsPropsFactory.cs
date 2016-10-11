// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.CoreImpl
{
	/// <summary>
	/// This class is a factory for creating <see cref="TsTextProps"/>.
	/// </summary>
	public class TsPropsFactory : ITsPropsFactory
	{
		/// <summary>
		/// Creates a text properties object using the specified writing system, old writing system, and a character
		/// style. The writing system value may be zero which means no writing system is specified. The style may be null.
		/// If the writing system is zero, then the old writing system must also be zero. An exception will be thrown otherwise.
		/// </summary>
		public ITsTextProps MakeProps(string bstrStyle, int ws, int ows)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Creates a text properties object using the specified writing system, old writing system, and a character
		/// style. The writing system value may be zero which means no writing system is specified. The style may be null.
		/// If the writing system is zero, then the old writing system must also be zero. An exception will be thrown otherwise.
		/// This method is only used by Views.
		/// </summary>
		public ITsTextProps MakePropsRgch(string rgchStyle, int cch, int ws, int ows)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Creates an empty text properties builder.
		/// </summary>
		public ITsPropsBldr GetPropsBldr()
		{
			return new TsPropsBldr();
		}
	}
}
