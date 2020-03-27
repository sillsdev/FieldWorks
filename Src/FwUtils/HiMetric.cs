// Copyright (c) 2009-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Facilitates the conversion of pixels to hiMetrics and vice versa.
	/// HiMetric is 1/1000 of a cm.
	/// </summary>
	internal sealed class HiMetric
	{
		// definition taken from C++ views code
		private const double HIMETRIC_INCH = 2540;

		/// <summary />
		internal HiMetric(int pixels, int dpi)
		{
			Value = (int)Math.Round(pixels*HIMETRIC_INCH / dpi);
		}

		/// <summary />
		internal HiMetric(int hiMetrix)
		{
			Value = hiMetrix;
		}

		/// <summary>
		/// Gets the value in pixels for a given dpi
		/// </summary>
		/// <returns>returns the value in pixels for a given dpi</returns>
		internal int GetPixels(int dpi)
		{
			return (int)Math.Round(dpi * (double)Value / HIMETRIC_INCH);
		}

		/// <summary>
		/// Gets the value in HiMetric.
		/// </summary>
		internal int Value { get; }
	}
}
