// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: UpDownMeasureControlTests.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.FwCoreDlgControls;
using SIL.Utils;
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
				Assert.AreEqual(2000, c.MeasureValue);
				Assert.AreEqual("2 pt", c.Text);
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
				Assert.AreEqual(-2000, c.MeasureValue);
				Assert.AreEqual("-2 pt", c.Text);
				c.DisplayAbsoluteValues = true;
				Assert.AreEqual(-2000, c.MeasureValue);
				Assert.AreEqual("2 pt", c.Text);
				c.MeasureValue = 6000;
				Assert.AreEqual(6000, c.MeasureValue);
				Assert.AreEqual("6 pt", c.Text);
				c.MeasureValue *= -1;
				Assert.AreEqual(-6000, c.MeasureValue);
				Assert.AreEqual("6 pt", c.Text);
				c.Text = "-1 cm"; // this is illegal, so the value should not change
				Assert.AreEqual(-6000, c.MeasureValue);
				Assert.AreEqual("6 pt", c.Text);
				c.Text = "1 cm";
				Assert.AreEqual(-28346, c.MeasureValue);
				Assert.AreEqual("28.35 pt", c.Text);
				c.Text = "-1 in"; // this is illegal, so the value should not change
				Assert.AreEqual(-28346, c.MeasureValue);
				Assert.AreEqual("28.35 pt", c.Text);
				c.Text = "1 in";
				Assert.AreEqual(-30000, c.MeasureValue); // Hit the minimum value
				Assert.AreEqual("30 pt", c.Text);
				c.DisplayAbsoluteValues = false;
				Assert.AreEqual(-30000, c.MeasureValue);
				Assert.AreEqual("-30 pt", c.Text);
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
				Assert.AreEqual(255118, c.MeasureValue);
				Assert.AreEqual("255.12 pt", c.Text);

				c.MeasureType = MsrSysType.Cm;
				Assert.AreEqual(255118, c.MeasureValue);
				Assert.AreEqual("9 cm", c.Text);
				c.Text = "4.5"; // i.e., 4.5 centimeters
				Assert.AreEqual(127559, c.MeasureValue);
				Assert.AreEqual("4.5 cm", c.Text);

				c.MeasureType = MsrSysType.Point;
				Assert.AreEqual(127559, c.MeasureValue);
				Assert.AreEqual("127.56 pt", c.Text);
				c.Text = "2 in";
				Assert.AreEqual(144000, c.MeasureValue);
				Assert.AreEqual("144 pt", c.Text);

				c.MeasureType = MsrSysType.Inch;
				Assert.AreEqual(144000, c.MeasureValue);
				Assert.AreEqual("2\"", c.Text);
				c.Text = "3.2\"";
				Assert.AreEqual(230400, c.MeasureValue);
				Assert.AreEqual("3.2\"", c.Text);
				c.Text = "0.05in";
				Assert.AreEqual(3600, c.MeasureValue);
				Assert.AreEqual("0.05\"", c.Text);
				c.Text = "3.23";
				Assert.AreEqual(232560, c.MeasureValue);
				Assert.AreEqual("3.23\"", c.Text);

				c.MeasureType = MsrSysType.Point;
				Assert.AreEqual(232560, c.MeasureValue);
				Assert.AreEqual("232.56 pt", c.Text);
				c.Text = "65 mm";
				Assert.AreEqual(184252, c.MeasureValue);
				Assert.AreEqual("184.25 pt", c.Text);

				c.MeasureType = MsrSysType.Mm;
				Assert.AreEqual(184252, c.MeasureValue);
				Assert.AreEqual("65 mm", c.Text);
				c.Text = "90.001";
				Assert.AreEqual(255121, c.MeasureValue);
				Assert.AreEqual("90 mm", c.Text);
				c.Text = "4 \"";
				Assert.AreEqual(288000, c.MeasureValue);
				Assert.AreEqual("101.6 mm", c.Text);

				c.MeasureType = MsrSysType.Point;
				Assert.AreEqual(288000, c.MeasureValue);
				Assert.AreEqual("288 pt", c.Text);
				c.Text = "56.8 pt";
				Assert.AreEqual(56800, c.MeasureValue);
				Assert.AreEqual("56.8 pt", c.Text);
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
				Assert.AreEqual(255118, c.MeasureValue);
				Assert.AreEqual("255.12 pt", c.Text);
				c.Text = "20mm";
				Assert.AreEqual(56693, c.MeasureValue);
				Assert.AreEqual("56.69 pt", c.Text);
				c.Text = "2 in ";
				Assert.AreEqual(144000, c.MeasureValue);
				Assert.AreEqual("144 pt", c.Text);

				// Test bogus stuff
				c.Text = "--4"; // double negative
				Assert.AreEqual(144000, c.MeasureValue);
				Assert.AreEqual("144 pt", c.Text);
				c.Text = "4.5 mc"; // bogus units
				Assert.AreEqual(144000, c.MeasureValue);
				Assert.AreEqual("144 pt", c.Text);
				c.Text = "4>4"; // wrong decimal point symbol
				Assert.AreEqual(144000, c.MeasureValue);
				Assert.AreEqual("144 pt", c.Text);
				c.Text = "4.0.1"; // too many decimal point symbols
				Assert.AreEqual(144000, c.MeasureValue);
				Assert.AreEqual("144 pt", c.Text);
				c.Text = "4 1"; // internal space
				Assert.AreEqual(144000, c.MeasureValue);
				Assert.AreEqual("144 pt", c.Text);
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
				Assert.AreEqual(3000, c.MeasureValue);
				Assert.AreEqual("3 pt", c.Text);
				c.MeasureValue = 2456;
				c.UpButton();
				Assert.AreEqual(3000, c.MeasureValue);
				Assert.AreEqual("3 pt", c.Text);
				c.MeasureValue = 100000;
				c.UpButton();
				Assert.AreEqual(100000, c.MeasureValue);
				Assert.AreEqual("100 pt", c.Text);
				c.MeasureValue = -3200;
				c.UpButton();
				Assert.AreEqual(-3000, c.MeasureValue);
				Assert.AreEqual("-3 pt", c.Text);

				c.MeasureType = MsrSysType.Cm;
				c.Text = "2.8";
				c.UpButton();
				Assert.AreEqual(82205, c.MeasureValue);
				Assert.AreEqual("2.9 cm", c.Text);
				c.Text = "2.85";
				c.UpButton();
				Assert.AreEqual(82205, c.MeasureValue);
				Assert.AreEqual("2.9 cm", c.Text);
				c.Text = "3.5";
				c.UpButton();
				Assert.AreEqual(100000, c.MeasureValue);
				Assert.AreEqual("3.53 cm", c.Text);
				c.Text = "-2";
				c.UpButton();
				Assert.AreEqual(-53858, c.MeasureValue);
				Assert.AreEqual("-1.9 cm", c.Text);

				c.MeasureType = MsrSysType.Inch;
				c.Text = "1";
				c.UpButton();
				Assert.AreEqual(79200, c.MeasureValue);
				Assert.AreEqual("1.1\"", c.Text);
				c.Text = "1.009";
				c.UpButton();
				Assert.AreEqual(79200, c.MeasureValue);
				Assert.AreEqual("1.1\"", c.Text);
				c.Text = "1.3";
				c.UpButton();
				Assert.AreEqual(100000, c.MeasureValue);
				Assert.AreEqual("1.39\"", c.Text);
				c.Text = "-0.95";
				c.UpButton();
				Assert.AreEqual(-64800, c.MeasureValue);
				Assert.AreEqual("-0.9\"", c.Text);

				c.MeasureType = MsrSysType.Mm;
				c.Text = "2";
				c.UpButton();
				Assert.AreEqual(8504, c.MeasureValue);
				Assert.AreEqual("3 mm", c.Text);
				c.Text = "2.72";
				c.UpButton();
				Assert.AreEqual(8504, c.MeasureValue);
				Assert.AreEqual("3 mm", c.Text);
				c.Text = "35";
				c.UpButton();
				Assert.AreEqual(100000, c.MeasureValue);
				Assert.AreEqual("35.28 mm", c.Text);
				c.Text = "0";
				c.UpButton();
				Assert.AreEqual(2835, c.MeasureValue);
				Assert.AreEqual("1 mm", c.Text);
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
				Assert.AreEqual(1000, c.MeasureValue);
				Assert.AreEqual("1 pt", c.Text);
				c.MeasureValue = 2456;
				c.DownButton();
				Assert.AreEqual(2000, c.MeasureValue);
				Assert.AreEqual("2 pt", c.Text);
				c.MeasureValue = -100000;
				c.DownButton();
				Assert.AreEqual(-100000, c.MeasureValue);
				Assert.AreEqual("-100 pt", c.Text);
				c.MeasureValue = -3200;
				c.DownButton();
				Assert.AreEqual(-4000, c.MeasureValue);
				Assert.AreEqual("-4 pt", c.Text);

				c.MeasureType = MsrSysType.Cm;
				c.Text = "2.8";
				c.DownButton();
				Assert.AreEqual(76535, c.MeasureValue);
				Assert.AreEqual("2.7 cm", c.Text);
				c.Text = "2.85";
				c.DownButton();
				Assert.AreEqual(79370, c.MeasureValue);
				Assert.AreEqual("2.8 cm", c.Text);
				c.Text = "-3.5";
				c.DownButton();
				Assert.AreEqual(-100000, c.MeasureValue);
				Assert.AreEqual("-3.53 cm", c.Text);
				c.Text = "-2";
				c.DownButton();
				Assert.AreEqual(-59528, c.MeasureValue);
				Assert.AreEqual("-2.1 cm", c.Text);

				c.MeasureType = MsrSysType.Inch;
				c.Text = "1";
				c.DownButton();
				Assert.AreEqual(64800, c.MeasureValue);
				Assert.AreEqual("0.9\"", c.Text);
				c.Text = "0.899";
				c.DownButton();
				Assert.AreEqual(57600, c.MeasureValue);
				Assert.AreEqual("0.8\"", c.Text);
				c.Text = "-1.3";
				c.DownButton();
				Assert.AreEqual(-100000, c.MeasureValue);
				Assert.AreEqual("-1.39\"", c.Text);
				c.Text = "-0.95";
				c.DownButton();
				Assert.AreEqual(-72000, c.MeasureValue);
				Assert.AreEqual("-1\"", c.Text);

				c.MeasureType = MsrSysType.Mm;
				c.Text = "2";
				c.DownButton();
				Assert.AreEqual(2835, c.MeasureValue);
				Assert.AreEqual("1 mm", c.Text);
				c.Text = "2.72";
				c.DownButton();
				Assert.AreEqual(5669, c.MeasureValue);
				Assert.AreEqual("2 mm", c.Text);
				c.Text = "-35";
				c.DownButton();
				Assert.AreEqual(-100000, c.MeasureValue);
				Assert.AreEqual("-35.28 mm", c.Text);
				c.Text = "0";
				c.DownButton();
				Assert.AreEqual(-2835, c.MeasureValue);
				Assert.AreEqual("-1 mm", c.Text);
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
				Assert.AreEqual(10000, c.MeasureValue);
				Assert.AreEqual("10 pt", c.Text);
				c.MeasureMax = 1000;
				Assert.AreEqual(-20, c.MeasureMin);
				Assert.AreEqual(1000, c.MeasureValue);
				Assert.AreEqual("1 pt", c.Text);
				c.MeasureMax = -100;
				Assert.AreEqual(-100, c.MeasureMin);
				Assert.AreEqual(-100, c.MeasureValue);
				Assert.AreEqual("-0.1 pt", c.Text);
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
				Assert.AreEqual(-20, c.MeasureValue);
				Assert.AreEqual("-0.02 pt", c.Text);
				c.MeasureMin = 0;
				Assert.AreEqual(10000, c.MeasureMax);
				Assert.AreEqual(0, c.MeasureValue);
				Assert.AreEqual("0 pt", c.Text);
				c.MeasureMin = 150000;
				Assert.AreEqual(150000, c.MeasureMax);
				Assert.AreEqual(150000, c.MeasureValue);
				Assert.AreEqual("150 pt", c.Text);
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
				Assert.AreEqual(0, c.MeasureValue);
				Assert.AreEqual("0 pt", c.Text);
				c.DownButton();
				Assert.AreEqual(-1000, c.MeasureValue);
				Assert.AreEqual("1 pt", c.Text);
				c.DownButton();
				Assert.AreEqual(-2000, c.MeasureValue);
				Assert.AreEqual("2 pt", c.Text);
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
				Assert.AreEqual(6000, c.MeasureValue);
				Assert.AreEqual("6 pt", c.Text);
				c.UpButton();
				Assert.AreEqual(10000, c.MeasureValue);
				Assert.AreEqual("10 pt", c.Text);
				c.DownButton();
				Assert.AreEqual(6000, c.MeasureValue);
				Assert.AreEqual("6 pt", c.Text);
				c.DownButton();
				Assert.AreEqual(0, c.MeasureValue);
				Assert.AreEqual("0 pt", c.Text);
				c.DownButton();
				Assert.AreEqual(-6000, c.MeasureValue);
				Assert.AreEqual("-6 pt", c.Text);
				c.DownButton();
				Assert.AreEqual(-10000, c.MeasureValue);
				Assert.AreEqual("-10 pt", c.Text);
			}
		}
	}
}
