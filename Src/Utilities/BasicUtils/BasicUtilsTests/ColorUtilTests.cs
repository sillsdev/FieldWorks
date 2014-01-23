// Copyright (c) 2006-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ColorUtilTests.cs
// Responsibility: TE Team

using System;
using System.Drawing;
using NUnit.Framework;

namespace SIL.Utils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Test the ColorUtil class.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class ColorUtilTests // can't derive from BaseTest because of dependencies
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests conversion of a BGR color (alpha channel not set) to a System.Drawing.Color
		/// (opaque).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ConvertBGRtoColor()
		{
			uint expectedColor = 0xFFFEDCBA;
			Assert.AreEqual(Color.FromArgb((int)expectedColor), ColorUtil.ConvertBGRtoColor(0xBADCFE));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests conversion of the special transparent BGR color value to a
		/// System.Drawing.Color having the KnownColor Transparent.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ConvertTransparentBGRtoColor()
		{
			Assert.AreEqual(Color.FromKnownColor(KnownColor.Transparent),
				ColorUtil.ConvertBGRtoColor(0xC0000000));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests conversion of a System.Drawing.Color (opaque) to a BGR color (alpha channel
		/// not set).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ConvertColortoBGR()
		{
			uint color = 0xFFFEDCBA;
			Assert.AreEqual(0xBADCFE, ColorUtil.ConvertColorToBGR(Color.FromArgb((int)color)));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests conversion of a System.Drawing.Color having the KnownColor Transparent to the
		/// special transparent BGR color value
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ConvertTransparentColortoBGR()
		{
			Assert.AreEqual(0xC0000000,
				ColorUtil.ConvertColorToBGR(Color.FromKnownColor(KnownColor.Transparent)));
		}
	}
}
