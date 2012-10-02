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
	public partial class AdaptItGuesserAutoConfigDialog : SilEncConverters31.AdaptItAutoConfigDialog
	{
		protected string cstrDefaultFriendNamePrefix = "Target Word Guesser for ";

		public AdaptItGuesserAutoConfigDialog
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
			AdaptItGuesserEncConverter.strHtmlFilename,
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

		public AdaptItGuesserAutoConfigDialog
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
			get { return typeof(AdaptItGuesserEncConverter).FullName; }
		}

		protected override string ImplType
		{
			get { return EncConverters.strTypeSILadaptitGuesser; }
		}

		protected override string DefaultFriendlyName
		{
			get
			{
				System.Diagnostics.Debug.Assert(!String.IsNullOrEmpty(m_strXmlTitle));
				return cstrDefaultFriendNamePrefix + ProjectNameFromConverterSpec;
			}
		}
	}
}
