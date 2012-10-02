using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;   // for the class attributes
using ECInterfaces;                     // for IEncConverter

namespace SilEncConverters31
{
	public class CpEncConverterConfig : EncConverterConfig
	{
		public CpEncConverterConfig()
			: base
			(
			typeof(CpEncConverter).FullName,
			CpEncConverter.strDisplayName,
			CpEncConverter.strHtmlFilename,
			ProcessTypeFlags.CodePageConversion
			)
			{
			}

		public override bool Configure
		(
		IEncConverters aECs,
		string strFriendlyName,
		ConvType eConversionType,
		string strLhsEncodingID,
		string strRhsEncodingID
		)
		{
			CpAutoConfigDialog form = new CpAutoConfigDialog(aECs, m_strDisplayName, m_strFriendlyName,
				m_strConverterID, m_eConversionType, m_strLhsEncodingID, m_strRhsEncodingID,
				m_lProcessType, m_bIsInRepository);

			return base.Configure(form);
		}

		public override void DisplayTestPage
			(
			IEncConverters aECs,
			string strFriendlyName,
			string strConverterIdentifier,
			ConvType eConversionType,
			string strTestData
			)
		{
			InitializeFromThis(ref strFriendlyName, ref strConverterIdentifier,
				ref eConversionType, ref strTestData);

			CpAutoConfigDialog form = new CpAutoConfigDialog(aECs, strFriendlyName,
				strConverterIdentifier, eConversionType, strTestData);

			base.DisplayTestPage(form);
		}
	}
}
