// Copyright (c) 2007-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: MeasurementUtilsTest.cs
// Responsibility: TE Team

using System;
using NUnit.Framework;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// MeasurementUtilsTests class
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	[SetCulture("en-US")]
	public class MeasurementUtilsTests
	{
		#region FormatMeasurement tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ability to format positive measure value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FormatMeasurement_Positive_Point()
		{
			Assert.AreEqual("2 pt", MeasurementUtils.FormatMeasurement(2000, MsrSysType.Point));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ability to format positive measure value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FormatMeasurement_Positive_Centimeter()
		{
			Assert.AreEqual("9 cm", MeasurementUtils.FormatMeasurement(255118, MsrSysType.Cm));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ability to format positive measure value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FormatMeasurement_Positive_Inches()
		{
			Assert.AreEqual("3.2\"", MeasurementUtils.FormatMeasurement(230400, MsrSysType.Inch));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ability to format positive measure value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FormatMeasurement_Positive_Millimeters()
		{
			Assert.AreEqual("101.6 mm", MeasurementUtils.FormatMeasurement(288000, MsrSysType.Mm));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ability to format negative measure value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FormatMeasurement_Negative_Point()
		{
			Assert.AreEqual("-28.35 pt", MeasurementUtils.FormatMeasurement(-28346, MsrSysType.Point));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ability to format negative measure value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FormatMeasurement_Negative_Centimeter()
		{
			Assert.AreEqual("-9 cm", MeasurementUtils.FormatMeasurement(-255118, MsrSysType.Cm));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ability to format negative measure value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FormatMeasurement_Negative_Inches()
		{
			Assert.AreEqual("-3.2\"", MeasurementUtils.FormatMeasurement(-230400, MsrSysType.Inch));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ability to format negative measure value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FormatMeasurement_Negative_Millimeters()
		{
			Assert.AreEqual("-101.6 mm", MeasurementUtils.FormatMeasurement(-288000, MsrSysType.Mm));
		}
		#endregion

		#region ExtractMeasurementInMillipoints tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ability to extract positive measure value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExtractMeasurement_Positive_Point()
		{
			Assert.AreEqual(2000, (int)Math.Round(
				MeasurementUtils.ExtractMeasurementInMillipoints("2 pt", MsrSysType.Mm, -1)));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ability to extract positive measure value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExtractMeasurement_Positive_Centimeter()
		{
			Assert.AreEqual(255118, (int)Math.Round(
				MeasurementUtils.ExtractMeasurementInMillipoints("9 cm", MsrSysType.Point, -1)));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ability to extract positive measure value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExtractMeasurement_Positive_Inches()
		{
			Assert.AreEqual(230400, (int)Math.Round(
				MeasurementUtils.ExtractMeasurementInMillipoints("3.2\"", MsrSysType.Point, -1)));
			Assert.AreEqual(3600, (int)Math.Round(
				MeasurementUtils.ExtractMeasurementInMillipoints("0.05 in", MsrSysType.Point, -1)));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ability to extract positive measure value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExtractMeasurement_Positive_Millimeters()
		{
			Assert.AreEqual(288000, (int)Math.Round(
				MeasurementUtils.ExtractMeasurementInMillipoints("101.6 mm", MsrSysType.Point, -1)));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ability to extract negative measure value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExtractMeasurement_Negative_Point()
		{
			Assert.AreEqual(-28346, (int)Math.Round(
				MeasurementUtils.ExtractMeasurementInMillipoints("-28.346 pt", MsrSysType.Inch, -1)));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ability to extract negative measure value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExtractMeasurement_Negative_Centimeter()
		{
			Assert.AreEqual(-255118, (int)Math.Round(
				MeasurementUtils.ExtractMeasurementInMillipoints("-9 cm", MsrSysType.Point, -1)));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ability to extract negative measure value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExtractMeasurement_Negative_Inches()
		{
			Assert.AreEqual(-230400, (int)Math.Round(
				MeasurementUtils.ExtractMeasurementInMillipoints("-3.2\"", MsrSysType.Point, -1)));
			Assert.AreEqual(-230400, (int)Math.Round(
				MeasurementUtils.ExtractMeasurementInMillipoints("-3.2 in", MsrSysType.Point, -1)));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ability to extract negative measure value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExtractMeasurement_Negative_Millimeters()
		{
			Assert.AreEqual(-288000, (int)Math.Round(
				MeasurementUtils.ExtractMeasurementInMillipoints("-101.6 mm", MsrSysType.Point, -1)));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ability to parse measure values with missing or extra spaces.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExtractMeasurement_WeirdSpaces()
		{
			Assert.AreEqual(288000, (int)Math.Round(
				MeasurementUtils.ExtractMeasurementInMillipoints("101.6mm", MsrSysType.Point, -1)));
			Assert.AreEqual(255118, (int)Math.Round(
				MeasurementUtils.ExtractMeasurementInMillipoints(" 9 cm", MsrSysType.Point, -1)));
			Assert.AreEqual(144000, (int)Math.Round(
				MeasurementUtils.ExtractMeasurementInMillipoints("2 in ", MsrSysType.Point, -1)));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ability to parse measure values with no units specified.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExtractMeasurement_NoUnits()
		{
			Assert.AreEqual(2000, (int)Math.Round(
				MeasurementUtils.ExtractMeasurementInMillipoints("2", MsrSysType.Point, -1)));
			Assert.AreEqual(288000, (int)Math.Round(
				MeasurementUtils.ExtractMeasurementInMillipoints("101.6", MsrSysType.Mm, -1)));
			Assert.AreEqual(255118, (int)Math.Round(
				MeasurementUtils.ExtractMeasurementInMillipoints("9", MsrSysType.Cm, -1)));
			Assert.AreEqual(144000, (int)Math.Round(
				MeasurementUtils.ExtractMeasurementInMillipoints("2", MsrSysType.Inch, -1)));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ability to parse bogus measure strings.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExtractMeasurement_Bogus_DoubleNegative()
		{
			// double negative
			Assert.AreEqual(999,
				MeasurementUtils.ExtractMeasurementInMillipoints("--4\"", MsrSysType.Point, 999));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ability to parse bogus measure strings.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExtractMeasurement_Bogus_Units()
		{
			// bogus units
			Assert.AreEqual(999,
				MeasurementUtils.ExtractMeasurementInMillipoints("4.5 mc", MsrSysType.Point, 999));
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ability to parse bogus measure strings.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExtractMeasurement_Bogus_WrongDecimalPointSymbol()
		{
			// wrong decimal point symbol
			Assert.AreEqual(999,
				MeasurementUtils.ExtractMeasurementInMillipoints("4>4", MsrSysType.Point, 999));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ability to parse bogus measure strings.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExtractMeasurement_Bogus_TooManyDecimalPointSymbols()
		{
			// too many decimal point symbols
			Assert.AreEqual(999,
				MeasurementUtils.ExtractMeasurementInMillipoints("4.0.1", MsrSysType.Point, 999));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ability to parse bogus measure strings.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExtractMeasurement_Bogus_InternalSpace()
		{
			// internal space
			Assert.AreEqual(999,
				MeasurementUtils.ExtractMeasurementInMillipoints("4 1", MsrSysType.Point, 999));
		}
		#endregion
	}
}
