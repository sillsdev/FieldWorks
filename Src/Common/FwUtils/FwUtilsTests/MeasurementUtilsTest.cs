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
			Assert.That(MeasurementUtils.FormatMeasurement(2000, MsrSysType.Point), Is.EqualTo("2 pt"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ability to format positive measure value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FormatMeasurement_Positive_Centimeter()
		{
			Assert.That(MeasurementUtils.FormatMeasurement(255118, MsrSysType.Cm), Is.EqualTo("9 cm"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ability to format positive measure value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FormatMeasurement_Positive_Inches()
		{
			Assert.That(MeasurementUtils.FormatMeasurement(230400, MsrSysType.Inch), Is.EqualTo("3.2\""));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ability to format positive measure value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FormatMeasurement_Positive_Millimeters()
		{
			Assert.That(MeasurementUtils.FormatMeasurement(288000, MsrSysType.Mm), Is.EqualTo("101.6 mm"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ability to format negative measure value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FormatMeasurement_Negative_Point()
		{
			Assert.That(MeasurementUtils.FormatMeasurement(-28346, MsrSysType.Point), Is.EqualTo("-28.35 pt"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ability to format negative measure value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FormatMeasurement_Negative_Centimeter()
		{
			Assert.That(MeasurementUtils.FormatMeasurement(-255118, MsrSysType.Cm), Is.EqualTo("-9 cm"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ability to format negative measure value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FormatMeasurement_Negative_Inches()
		{
			Assert.That(MeasurementUtils.FormatMeasurement(-230400, MsrSysType.Inch), Is.EqualTo("-3.2\""));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ability to format negative measure value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FormatMeasurement_Negative_Millimeters()
		{
			Assert.That(MeasurementUtils.FormatMeasurement(-288000, MsrSysType.Mm), Is.EqualTo("-101.6 mm"));
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
			Assert.That((int)Math.Round(
				MeasurementUtils.ExtractMeasurementInMillipoints("2 pt", MsrSysType.Mm, -1)), Is.EqualTo(2000));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ability to extract positive measure value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExtractMeasurement_Positive_Centimeter()
		{
			Assert.That((int)Math.Round(
				MeasurementUtils.ExtractMeasurementInMillipoints("9 cm", MsrSysType.Point, -1)), Is.EqualTo(255118));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ability to extract positive measure value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExtractMeasurement_Positive_Inches()
		{
			Assert.That((int)Math.Round(
				MeasurementUtils.ExtractMeasurementInMillipoints("3.2\"", MsrSysType.Point, -1)), Is.EqualTo(230400));
			Assert.That((int)Math.Round(
				MeasurementUtils.ExtractMeasurementInMillipoints("0.05 in", MsrSysType.Point, -1)), Is.EqualTo(3600));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ability to extract positive measure value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExtractMeasurement_Positive_Millimeters()
		{
			Assert.That((int)Math.Round(
				MeasurementUtils.ExtractMeasurementInMillipoints("101.6 mm", MsrSysType.Point, -1)), Is.EqualTo(288000));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ability to extract negative measure value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExtractMeasurement_Negative_Point()
		{
			Assert.That((int)Math.Round(
				MeasurementUtils.ExtractMeasurementInMillipoints("-28.346 pt", MsrSysType.Inch, -1)), Is.EqualTo(-28346));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ability to extract negative measure value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExtractMeasurement_Negative_Centimeter()
		{
			Assert.That((int)Math.Round(
				MeasurementUtils.ExtractMeasurementInMillipoints("-9 cm", MsrSysType.Point, -1)), Is.EqualTo(-255118));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ability to extract negative measure value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExtractMeasurement_Negative_Inches()
		{
			Assert.That((int)Math.Round(
				MeasurementUtils.ExtractMeasurementInMillipoints("-3.2\"", MsrSysType.Point, -1)), Is.EqualTo(-230400));
			Assert.That((int)Math.Round(
				MeasurementUtils.ExtractMeasurementInMillipoints("-3.2 in", MsrSysType.Point, -1)), Is.EqualTo(-230400));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ability to extract negative measure value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExtractMeasurement_Negative_Millimeters()
		{
			Assert.That((int)Math.Round(
				MeasurementUtils.ExtractMeasurementInMillipoints("-101.6 mm", MsrSysType.Point, -1)), Is.EqualTo(-288000));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ability to parse measure values with missing or extra spaces.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExtractMeasurement_WeirdSpaces()
		{
			Assert.That((int)Math.Round(
				MeasurementUtils.ExtractMeasurementInMillipoints("101.6mm", MsrSysType.Point, -1)), Is.EqualTo(288000));
			Assert.That((int)Math.Round(
				MeasurementUtils.ExtractMeasurementInMillipoints(" 9 cm", MsrSysType.Point, -1)), Is.EqualTo(255118));
			Assert.That((int)Math.Round(
				MeasurementUtils.ExtractMeasurementInMillipoints("2 in ", MsrSysType.Point, -1)), Is.EqualTo(144000));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ability to parse measure values with no units specified.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExtractMeasurement_NoUnits()
		{
			Assert.That((int)Math.Round(
				MeasurementUtils.ExtractMeasurementInMillipoints("2", MsrSysType.Point, -1)), Is.EqualTo(2000));
			Assert.That((int)Math.Round(
				MeasurementUtils.ExtractMeasurementInMillipoints("101.6", MsrSysType.Mm, -1)), Is.EqualTo(288000));
			Assert.That((int)Math.Round(
				MeasurementUtils.ExtractMeasurementInMillipoints("9", MsrSysType.Cm, -1)), Is.EqualTo(255118));
			Assert.That((int)Math.Round(
				MeasurementUtils.ExtractMeasurementInMillipoints("2", MsrSysType.Inch, -1)), Is.EqualTo(144000));
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
			Assert.That(MeasurementUtils.ExtractMeasurementInMillipoints("--4\"", MsrSysType.Point, 999), Is.EqualTo(999));
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
			Assert.That(MeasurementUtils.ExtractMeasurementInMillipoints("4.5 mc", MsrSysType.Point, 999), Is.EqualTo(999));
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
			Assert.That(MeasurementUtils.ExtractMeasurementInMillipoints("4>4", MsrSysType.Point, 999), Is.EqualTo(999));
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
			Assert.That(MeasurementUtils.ExtractMeasurementInMillipoints("4.0.1", MsrSysType.Point, 999), Is.EqualTo(999));
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
			Assert.That(MeasurementUtils.ExtractMeasurementInMillipoints("4 1", MsrSysType.Point, 999), Is.EqualTo(999));
		}
		#endregion
	}
}
