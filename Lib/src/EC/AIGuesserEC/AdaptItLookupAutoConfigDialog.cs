using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using ECInterfaces;                     // for IEncConverter

namespace SilEncConverters31
{
	public partial class AdaptItLookupAutoConfigDialog : SilEncConverters31.AdaptItAutoConfigDialog
	{
		protected string cstrDefaultFriendNamePrefix = "Lookup in ";

		public AdaptItLookupAutoConfigDialog
			(
			IEncConverters aECs,
			string strDisplayName,
			string strFriendlyName,
			string strConverterIdentifier,
			ConvType eConversionType,
			string strLhsEncodingId,
			string strRhsEncodingId,
			int lProcessTypeFlags,
			bool bIsInRepository
			)
		{
			base.Initialize
			(
			aECs,
			AdaptItEncConverter.strHtmlFilename,
			strDisplayName,
			strFriendlyName,
			strConverterIdentifier,
			eConversionType,
			strLhsEncodingId,
			strRhsEncodingId,
			lProcessTypeFlags,
			bIsInRepository
			);
		}

		public AdaptItLookupAutoConfigDialog
			(
			IEncConverters aECs,
			string strFriendlyName,
			string strConverterIdentifier,
			ConvType eConversionType,
			string strTestData
			)
		{
			base.Initialize
			(
			aECs,
			strFriendlyName,
			strConverterIdentifier,
			eConversionType,
			strTestData
			);
		}

		protected override string ProgID
		{
			get { return typeof(AdaptItEncConverter).FullName; }
		}

		protected override string ImplType
		{
			get { return EncConverters.strTypeSILadaptit; }
		}

		protected override string DefaultFriendlyName
		{
			get
			{
				System.Diagnostics.Debug.Assert(!String.IsNullOrEmpty(ProjectNameFromConverterSpec));
				return cstrDefaultFriendNamePrefix + ProjectNameFromConverterSpec;
			}
		}
	}
}
