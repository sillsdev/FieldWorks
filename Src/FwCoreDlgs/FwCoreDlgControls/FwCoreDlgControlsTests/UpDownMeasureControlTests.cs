// Copyright (c) 2007-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: UpDownMeasureControlTests.cs
// Responsibility: TE Team

using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.FwCoreDlgControls;
using SIL.LCModel.Utils;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.FwCoreDlgControlsTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for the <see cref="UpDownMeasureControl"/> class
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	[SetCulture("en-US")]
	public class UpDownMeasureControlTests
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ability to set and get positive measure values.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetSetPositiveMeasureValue()
		{
			using (UpDownMeasureControl c = new UpDownMeasureControl())
			{
				c.MeasureType = MsrSysType.Point;
				c.MeasureMin = 0;
				c.MeasureMax = 10000;
				c.MeasureValue = 2000;
				Assert.That(c.MeasureValue, Is.EqualTo(2000));
				Assert.That(c.Text, Is.EqualTo("2 pt"));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ability to set and get negative measure values.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetSetNegativeMeasureValue()
		{
			using (UpDownMeasureControl c = new UpDownMeasureControl())
			{
				c.DisplayAbsoluteValues = false;
				c.MeasureType = MsrSysType.Point;
				c.MeasureMin = -30000;
				c.MeasureMax = 30000;
				c.MeasureValue = -2000;
				Assert.That(c.MeasureValue, Is.EqualTo(-2000));
				Assert.That(c.Text, Is.EqualTo("-2 pt"));
				c.DisplayAbsoluteValues = true;
				Assert.That(c.MeasureValue, Is.EqualTo(-2000));
				Assert.That(c.Text, Is.EqualTo("2 pt"));
				c.MeasureValue = 6000;
				Assert.That(c.MeasureValue, Is.EqualTo(6000));
				Assert.That(c.Text, Is.EqualTo("6 pt"));
				c.MeasureValue *= -1;
				Assert.That(c.MeasureValue, Is.EqualTo(-6000));
				Assert.That(c.Text, Is.EqualTo("6 pt"));
				c.Text = "-1 cm"; // this is illegal, so the value should not change
				Assert.That(c.MeasureValue, Is.EqualTo(-6000));
				Assert.That(c.Text, Is.EqualTo("6 pt"));
				c.Text = "1 cm";
				Assert.That(c.MeasureValue, Is.EqualTo(-28346));
				Assert.That(c.Text, Is.EqualTo("28.35 pt"));
				c.Text = "-1 in"; // this is illegal, so the value should not change
				Assert.That(c.MeasureValue, Is.EqualTo(-28346));
				Assert.That(c.Text, Is.EqualTo("28.35 pt"));
				c.Text = "1 in";
				Assert.That(c.MeasureValue, Is.EqualTo(-30000)); // Hit the minimum value
				Assert.That(c.Text, Is.EqualTo("30 pt"));
				c.DisplayAbsoluteValues = false;
				Assert.That(c.MeasureValue, Is.EqualTo(-30000));
				Assert.That(c.Text, Is.EqualTo("-30 pt"));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ability to set and get positive measure values using non-default units.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetSetMeasureValueWithUnits()
		{
			using (UpDownMeasureControl c = new UpDownMeasureControl())
			{
				c.MeasureType = MsrSysType.Point;
				c.MeasureMin = 0;
				c.MeasureMax = 1000000;
				c.Text = "9 cm";
				Assert.That(c.MeasureValue, Is.EqualTo(255118));
				Assert.That(c.Text, Is.EqualTo("255.12 pt"));

				c.MeasureType = MsrSysType.Cm;
				Assert.That(c.MeasureValue, Is.EqualTo(255118));
				Assert.That(c.Text, Is.EqualTo("9 cm"));
				c.Text = "4.5"; // i.e., 4.5 centimeters
				Assert.That(c.MeasureValue, Is.EqualTo(127559));
				Assert.That(c.Text, Is.EqualTo("4.5 cm"));

				c.MeasureType = MsrSysType.Point;
				Assert.That(c.MeasureValue, Is.EqualTo(127559));
				Assert.That(c.Text, Is.EqualTo("127.56 pt"));
				c.Text = "2 in";
				Assert.That(c.MeasureValue, Is.EqualTo(144000));
				Assert.That(c.Text, Is.EqualTo("144 pt"));

				c.MeasureType = MsrSysType.Inch;
				Assert.That(c.MeasureValue, Is.EqualTo(144000));
				Assert.That(c.Text, Is.EqualTo("2\""));
				c.Text = "3.2\"";
				Assert.That(c.MeasureValue, Is.EqualTo(230400));
				Assert.That(c.Text, Is.EqualTo("3.2\""));
				c.Text = "0.05in";
				Assert.That(c.MeasureValue, Is.EqualTo(3600));
				Assert.That(c.Text, Is.EqualTo("0.05\""));
				c.Text = "3.23";
				Assert.That(c.MeasureValue, Is.EqualTo(232560));
				Assert.That(c.Text, Is.EqualTo("3.23\""));

				c.MeasureType = MsrSysType.Point;
				Assert.That(c.MeasureValue, Is.EqualTo(232560));
				Assert.That(c.Text, Is.EqualTo("232.56 pt"));
				c.Text = "65 mm";
				Assert.That(c.MeasureValue, Is.EqualTo(184252));
				Assert.That(c.Text, Is.EqualTo("184.25 pt"));

				c.MeasureType = MsrSysType.Mm;
				Assert.That(c.MeasureValue, Is.EqualTo(184252));
				Assert.That(c.Text, Is.EqualTo("65 mm"));
				c.Text = "90.001";
				Assert.That(c.MeasureValue, Is.EqualTo(255121));
				Assert.That(c.Text, Is.EqualTo("90 mm"));
				c.Text = "4 \"";
				Assert.That(c.MeasureValue, Is.EqualTo(288000));
				Assert.That(c.Text, Is.EqualTo("101.6 mm"));

				c.MeasureType = MsrSysType.Point;
				Assert.That(c.MeasureValue, Is.EqualTo(288000));
				Assert.That(c.Text, Is.EqualTo("288 pt"));
				c.Text = "56.8 pt";
				Assert.That(c.MeasureValue, Is.EqualTo(56800));
				Assert.That(c.Text, Is.EqualTo("56.8 pt"));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ability to set unusual measure values (bogus units, extra spaces).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SetUnusualMeasureValues()
		{
			using (UpDownMeasureControl c = new UpDownMeasureControl())
			{
				c.MeasureType = MsrSysType.Point;
				c.MeasureMin = -1000000;
				c.MeasureMax = 1000000;
				// test weird spaces
				c.Text = " 9 cm";
				Assert.That(c.MeasureValue, Is.EqualTo(255118));
				Assert.That(c.Text, Is.EqualTo("255.12 pt"));
				c.Text = "20mm";
				Assert.That(c.MeasureValue, Is.EqualTo(56693));
				Assert.That(c.Text, Is.EqualTo("56.69 pt"));
				c.Text = "2 in ";
				Assert.That(c.MeasureValue, Is.EqualTo(144000));
				Assert.That(c.Text, Is.EqualTo("144 pt"));

				// Test bogus stuff
				c.Text = "--4"; // double negative
				Assert.That(c.MeasureValue, Is.EqualTo(144000));
				Assert.That(c.Text, Is.EqualTo("144 pt"));
				c.Text = "4.5 mc"; // bogus units
				Assert.That(c.MeasureValue, Is.EqualTo(144000));
				Assert.That(c.Text, Is.EqualTo("144 pt"));
				c.Text = "4>4"; // wrong decimal point symbol
				Assert.That(c.MeasureValue, Is.EqualTo(144000));
				Assert.That(c.Text, Is.EqualTo("144 pt"));
				c.Text = "4.0.1"; // too many decimal point symbols
				Assert.That(c.MeasureValue, Is.EqualTo(144000));
				Assert.That(c.Text, Is.EqualTo("144 pt"));
				c.Text = "4 1"; // internal space
				Assert.That(c.MeasureValue, Is.EqualTo(144000));
				Assert.That(c.Text, Is.EqualTo("144 pt"));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the up button
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UpButton()
		{
			using (UpDownMeasureControl c = new UpDownMeasureControl())
			{
				c.MeasureType = MsrSysType.Point;
				c.MeasureMin = -100000;
				c.MeasureMax = 100000;
				c.MeasureValue = 2000;
				c.UpButton();
				Assert.That(c.MeasureValue, Is.EqualTo(3000));
				Assert.That(c.Text, Is.EqualTo("3 pt"));
				c.MeasureValue = 2456;
				c.UpButton();
				Assert.That(c.MeasureValue, Is.EqualTo(3000));
				Assert.That(c.Text, Is.EqualTo("3 pt"));
				c.MeasureValue = 100000;
				c.UpButton();
				Assert.That(c.MeasureValue, Is.EqualTo(100000));
				Assert.That(c.Text, Is.EqualTo("100 pt"));
				c.MeasureValue = -3200;
				c.UpButton();
				Assert.That(c.MeasureValue, Is.EqualTo(-3000));
				Assert.That(c.Text, Is.EqualTo("-3 pt"));

				c.MeasureType = MsrSysType.Cm;
				c.Text = "2.8";
				c.UpButton();
				Assert.That(c.MeasureValue, Is.EqualTo(82205));
				Assert.That(c.Text, Is.EqualTo("2.9 cm"));
				c.Text = "2.85";
				c.UpButton();
				Assert.That(c.MeasureValue, Is.EqualTo(82205));
				Assert.That(c.Text, Is.EqualTo("2.9 cm"));
				c.Text = "3.5";
				c.UpButton();
				Assert.That(c.MeasureValue, Is.EqualTo(100000));
				Assert.That(c.Text, Is.EqualTo("3.53 cm"));
				c.Text = "-2";
				c.UpButton();
				Assert.That(c.MeasureValue, Is.EqualTo(-53858));
				Assert.That(c.Text, Is.EqualTo("-1.9 cm"));

				c.MeasureType = MsrSysType.Inch;
				c.Text = "1";
				c.UpButton();
				Assert.That(c.MeasureValue, Is.EqualTo(79200));
				Assert.That(c.Text, Is.EqualTo("1.1\""));
				c.Text = "1.009";
				c.UpButton();
				Assert.That(c.MeasureValue, Is.EqualTo(79200));
				Assert.That(c.Text, Is.EqualTo("1.1\""));
				c.Text = "1.3";
				c.UpButton();
				Assert.That(c.MeasureValue, Is.EqualTo(100000));
				Assert.That(c.Text, Is.EqualTo("1.39\""));
				c.Text = "-0.95";
				c.UpButton();
				Assert.That(c.MeasureValue, Is.EqualTo(-64800));
				Assert.That(c.Text, Is.EqualTo("-0.9\""));

				c.MeasureType = MsrSysType.Mm;
				c.Text = "2";
				c.UpButton();
				Assert.That(c.MeasureValue, Is.EqualTo(8504));
				Assert.That(c.Text, Is.EqualTo("3 mm"));
				c.Text = "2.72";
				c.UpButton();
				Assert.That(c.MeasureValue, Is.EqualTo(8504));
				Assert.That(c.Text, Is.EqualTo("3 mm"));
				c.Text = "35";
				c.UpButton();
				Assert.That(c.MeasureValue, Is.EqualTo(100000));
				Assert.That(c.Text, Is.EqualTo("35.28 mm"));
				c.Text = "0";
				c.UpButton();
				Assert.That(c.MeasureValue, Is.EqualTo(2835));
				Assert.That(c.Text, Is.EqualTo("1 mm"));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the down button
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DownButton()
		{
			using (UpDownMeasureControl c = new UpDownMeasureControl())
			{
				c.MeasureType = MsrSysType.Point;
				c.MeasureMin = -100000;
				c.MeasureMax = 100000;
				c.MeasureValue = 2000;
				c.DownButton();
				Assert.That(c.MeasureValue, Is.EqualTo(1000));
				Assert.That(c.Text, Is.EqualTo("1 pt"));
				c.MeasureValue = 2456;
				c.DownButton();
				Assert.That(c.MeasureValue, Is.EqualTo(2000));
				Assert.That(c.Text, Is.EqualTo("2 pt"));
				c.MeasureValue = -100000;
				c.DownButton();
				Assert.That(c.MeasureValue, Is.EqualTo(-100000));
				Assert.That(c.Text, Is.EqualTo("-100 pt"));
				c.MeasureValue = -3200;
				c.DownButton();
				Assert.That(c.MeasureValue, Is.EqualTo(-4000));
				Assert.That(c.Text, Is.EqualTo("-4 pt"));

				c.MeasureType = MsrSysType.Cm;
				c.Text = "2.8";
				c.DownButton();
				Assert.That(c.MeasureValue, Is.EqualTo(76535));
				Assert.That(c.Text, Is.EqualTo("2.7 cm"));
				c.Text = "2.85";
				c.DownButton();
				Assert.That(c.MeasureValue, Is.EqualTo(79370));
				Assert.That(c.Text, Is.EqualTo("2.8 cm"));
				c.Text = "-3.5";
				c.DownButton();
				Assert.That(c.MeasureValue, Is.EqualTo(-100000));
				Assert.That(c.Text, Is.EqualTo("-3.53 cm"));
				c.Text = "-2";
				c.DownButton();
				Assert.That(c.MeasureValue, Is.EqualTo(-59528));
				Assert.That(c.Text, Is.EqualTo("-2.1 cm"));

				c.MeasureType = MsrSysType.Inch;
				c.Text = "1";
				c.DownButton();
				Assert.That(c.MeasureValue, Is.EqualTo(64800));
				Assert.That(c.Text, Is.EqualTo("0.9\""));
				c.Text = "0.899";
				c.DownButton();
				Assert.That(c.MeasureValue, Is.EqualTo(57600));
				Assert.That(c.Text, Is.EqualTo("0.8\""));
				c.Text = "-1.3";
				c.DownButton();
				Assert.That(c.MeasureValue, Is.EqualTo(-100000));
				Assert.That(c.Text, Is.EqualTo("-1.39\""));
				c.Text = "-0.95";
				c.DownButton();
				Assert.That(c.MeasureValue, Is.EqualTo(-72000));
				Assert.That(c.Text, Is.EqualTo("-1\""));

				c.MeasureType = MsrSysType.Mm;
				c.Text = "2";
				c.DownButton();
				Assert.That(c.MeasureValue, Is.EqualTo(2835));
				Assert.That(c.Text, Is.EqualTo("1 mm"));
				c.Text = "2.72";
				c.DownButton();
				Assert.That(c.MeasureValue, Is.EqualTo(5669));
				Assert.That(c.Text, Is.EqualTo("2 mm"));
				c.Text = "-35";
				c.DownButton();
				Assert.That(c.MeasureValue, Is.EqualTo(-100000));
				Assert.That(c.Text, Is.EqualTo("-35.28 mm"));
				c.Text = "0";
				c.DownButton();
				Assert.That(c.MeasureValue, Is.EqualTo(-2835));
				Assert.That(c.Text, Is.EqualTo("-1 mm"));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ability to limit values based on the MeasureMax property.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MaxLimit()
		{
			using (UpDownMeasureControl c = new UpDownMeasureControl())
			{
				c.MeasureType = MsrSysType.Point;
				c.MeasureMin = -20;
				c.MeasureMax = 10000;
				c.MeasureValue = 20000;
				Assert.That(c.MeasureValue, Is.EqualTo(10000));
				Assert.That(c.Text, Is.EqualTo("10 pt"));
				c.MeasureMax = 1000;
				Assert.That(c.MeasureMin, Is.EqualTo(-20));
				Assert.That(c.MeasureValue, Is.EqualTo(1000));
				Assert.That(c.Text, Is.EqualTo("1 pt"));
				c.MeasureMax = -100;
				Assert.That(c.MeasureMin, Is.EqualTo(-100));
				Assert.That(c.MeasureValue, Is.EqualTo(-100));
				Assert.That(c.Text, Is.EqualTo("-0.1 pt"));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ability to limit values based on the MeasureMin property.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MinLimit()
		{
			using (UpDownMeasureControl c = new UpDownMeasureControl())
			{
				c.MeasureType = MsrSysType.Point;
				c.MeasureMin = -20;
				c.MeasureMax = 10000;
				c.MeasureValue = -50;
				Assert.That(c.MeasureValue, Is.EqualTo(-20));
				Assert.That(c.Text, Is.EqualTo("-0.02 pt"));
				c.MeasureMin = 0;
				Assert.That(c.MeasureMax, Is.EqualTo(10000));
				Assert.That(c.MeasureValue, Is.EqualTo(0));
				Assert.That(c.Text, Is.EqualTo("0 pt"));
				c.MeasureMin = 150000;
				Assert.That(c.MeasureMax, Is.EqualTo(150000));
				Assert.That(c.MeasureValue, Is.EqualTo(150000));
				Assert.That(c.Text, Is.EqualTo("150 pt"));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the down button decrements the underlying value (rather than the
		/// displayed value) when displaying absolute values.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DownButton_DisplayingAbsoluteValues()
		{
			using (UpDownMeasureControl c = new UpDownMeasureControl())
			{
				c.DisplayAbsoluteValues = true;
				c.MeasureType = MsrSysType.Point;
				c.MeasureMin = -30000;
				c.MeasureMax = 30000;
				c.MeasureValue = 0;
				Assert.That(c.MeasureValue, Is.EqualTo(0));
				Assert.That(c.Text, Is.EqualTo("0 pt"));
				c.DownButton();
				Assert.That(c.MeasureValue, Is.EqualTo(-1000));
				Assert.That(c.Text, Is.EqualTo("1 pt"));
				c.DownButton();
				Assert.That(c.MeasureValue, Is.EqualTo(-2000));
				Assert.That(c.Text, Is.EqualTo("2 pt"));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the up button when increment factor is > 1
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UpDownButtons_IncrementFactor()
		{
			using (UpDownMeasureControl c = new UpDownMeasureControl())
			{
				c.MeasureType = MsrSysType.Point;
				c.MeasureMin = -10000;
				c.MeasureMax = 10000;
				c.MeasureValue = 2000;
				c.MeasureIncrementFactor = 6;
				c.UpButton();
				Assert.That(c.MeasureValue, Is.EqualTo(6000));
				Assert.That(c.Text, Is.EqualTo("6 pt"));
				c.UpButton();
				Assert.That(c.MeasureValue, Is.EqualTo(10000));
				Assert.That(c.Text, Is.EqualTo("10 pt"));
				c.DownButton();
				Assert.That(c.MeasureValue, Is.EqualTo(6000));
				Assert.That(c.Text, Is.EqualTo("6 pt"));
				c.DownButton();
				Assert.That(c.MeasureValue, Is.EqualTo(0));
				Assert.That(c.Text, Is.EqualTo("0 pt"));
				c.DownButton();
				Assert.That(c.MeasureValue, Is.EqualTo(-6000));
				Assert.That(c.Text, Is.EqualTo("-6 pt"));
				c.DownButton();
				Assert.That(c.MeasureValue, Is.EqualTo(-10000));
				Assert.That(c.Text, Is.EqualTo("-10 pt"));
			}
		}
	}
}
