// Copyright (c) 2010-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace SIL.FieldWorks
{
	///<summary>
	/// Class to find out some details about the current FW installation.
	///</summary>
	public static class WindowsInstallerQuery
	{
		private const string InstallerProductCode         = "{8E80F1ED-826A-46d5-A59A-D8A203F2F0D9}";
		private const string InstalledProductNameProperty = "InstalledProductName";
		private const string TeFeatureName                = "TE";

		private const int ErrorMoreData       = 234;
		private const int ErrorUnknownProduct = 1605;
		private const int ErrorUnknownFeature = 1606;

		[DllImport("msi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern Int32 MsiGetProductInfo(string product, string property,
			StringBuilder valueBuf, ref Int32 cchValueBuf);

		[DllImport("msi.dll", CharSet = CharSet.Unicode)]
		internal static extern uint MsiOpenProduct(string szProduct, out int hProduct);

		[DllImport("msi.dll", CharSet = CharSet.Unicode)]
		internal static extern uint MsiGetFeatureInfo(int hProduct, string szFeature, out uint lpAttributes, StringBuilder lpTitleBuf, ref uint cchTitleBuf, StringBuilder lpHelpBuf, ref uint cchHelpBuf);

		/// <summary>
		/// Check the installer status to see if FW is installed on the user's machine.
		/// If not, it can be assumed we are running on a developer's machine.
		/// </summary>
		/// <returns>True if this is an installed version</returns>
		public static bool IsThisInstalled()
		{
			string productName;

			var status = GetProductInfo(InstalledProductNameProperty, out productName);

			return status != ErrorUnknownProduct;
		}

		/// <summary>
		/// Check the installer status to see if we are running a BTE version of FW.
		/// If the product is not installed then we assume this is a developer build
		/// and just say it's BTE anyway.
		/// </summary>
		/// <returns>True if this is a BTE version</returns>
		public static bool IsThisBTE()
		{
			string productName;

			var status = GetProductInfo(InstalledProductNameProperty, out productName);

			if (status == ErrorUnknownProduct)
				return true; // Assume it's BTE if we can't find installation information

			return productName.EndsWith("BTE");
		}

		private static Int32 GetProductInfo(string propertyName, out string propertyValue)
		{
			var sbBuffer = new StringBuilder();
			var len = sbBuffer.Capacity;
			sbBuffer.Length = 0;

			var status = MsiGetProductInfo(InstallerProductCode, propertyName, sbBuffer, ref len);
			if (status == ErrorMoreData)
			{
				len++;
				sbBuffer.EnsureCapacity(len);
				status = MsiGetProductInfo(InstallerProductCode, InstalledProductNameProperty, sbBuffer, ref len);
			}

			propertyValue = sbBuffer.ToString();

			return status;
		}
	}
}