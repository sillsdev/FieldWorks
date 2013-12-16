// Copyright (c) 2007-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: PrinterUtils.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Printing;
using System.Text;

namespace SIL.Utils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Methods to support use of the printers.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class PrinterUtils
	{
		#region Constants
		/// <summary>Conversion factor from hundredths of inches to millipoints</summary>
		private const int kCentiInchToMilliPoints = 720;
		private const float kDefaultDpiX = 300.0f;
		private const float kDefaultDpiY = 300.0f;
		#endregion

		#region Member variables
		private static PrinterResolution s_defaultPrinterResolution = null;
		private static PaperSize s_defaultpaperSize = null;
		private static bool s_fTriedToGetDefautSettings = false;
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the default page settings from the default printer on the macine. If no printer
		/// is installed, set them to null.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static void TryCreateDefaultPageSettings()
		{
			if (!s_fTriedToGetDefautSettings)
			{
				try
				{
					PrinterSettings defaultPrintSettings = new PrinterSettings();
					Debug.Assert(defaultPrintSettings.IsDefaultPrinter);
					// ENHANCE (TE-5917): Detect when the default printer changes and update
					// accordinly.
					PageSettings pageSettings = defaultPrintSettings.DefaultPageSettings;
					s_defaultpaperSize = pageSettings.PaperSize;
					s_defaultPrinterResolution = pageSettings.PrinterResolution;
				}
				catch
				{
					// Printer is not installed or maybe there is a problem with the driver.
					s_defaultpaperSize = null;
					s_defaultPrinterResolution = null;
				}
				s_fTriedToGetDefautSettings = true;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the default printer DPI. If no printer is installed, this will return
		/// 300.0f for both the x and y DPI
		/// </summary>
		/// <param name="dpiX">The dpi X.</param>
		/// <param name="dpiY">The dpi Y.</param>
		/// ------------------------------------------------------------------------------------
		public static void GetDefaultPrinterDPI(out float dpiX, out float dpiY)
		{
			TryCreateDefaultPageSettings();
			// JohnT: it appears that when the default printer is not connected, it's possible to get a complete
			// garbage printer resolution, e.g., (-3, -1) for my laptop. Don't use such garbage, it produces Asserts
			// because the resultion gets corrected up to zero and then divided by.
			if (s_defaultPrinterResolution != null && s_defaultPrinterResolution.X > 0 &&
				s_defaultPrinterResolution.Y > 0)
			{
				dpiX = s_defaultPrinterResolution.X;
				dpiY = s_defaultPrinterResolution.Y;
				return;
			}
			// if there are no printers set up then we will get an invalid printer exception
			// In which case (JohnT), we want a default printer resolution, not an invalid
			// one just created but without valid values.
			dpiX = kDefaultDpiX;
			dpiY = kDefaultDpiY;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the default paper size from the default printer in millipoints.
		/// </summary>
		/// <param name="mptPaperHeight">Height of the default paper in millipoints.</param>
		/// <param name="mptPaperWidth">Width of the default paper in millipoints.</param>
		/// ------------------------------------------------------------------------------------
		public static void GetDefaultPaperSizeInMp(out int mptPaperHeight, out int mptPaperWidth)
		{
			TryCreateDefaultPageSettings();
			if (s_defaultpaperSize == null)
			{
				// Printer is not installed. Set default paper size to A4.
				mptPaperHeight = 1169 * kCentiInchToMilliPoints;
				mptPaperWidth = 827 * kCentiInchToMilliPoints;
				return;
			}

			mptPaperHeight = s_defaultpaperSize.Height * kCentiInchToMilliPoints;
			mptPaperWidth = s_defaultpaperSize.Width * kCentiInchToMilliPoints;
		}
	}
}
