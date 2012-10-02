using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;   // for the class attributes
using ECInterfaces;                     // for IEncConverter

namespace SilEncConverters40
{
	public class FallbackEncConverterConfig : EncConverterConfig
	{
		public FallbackEncConverterConfig()
			: base
			(
			typeof(FallbackEncConverter).FullName,
			FallbackEncConverter.strDisplayName,
			FallbackEncConverter.strHtmlFilename,
			ProcessTypeFlags.DontKnow
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
			FallbackAutoConfigDialog form = new FallbackAutoConfigDialog(aECs, m_strDisplayName, m_strFriendlyName,
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

			FallbackAutoConfigDialog form = new FallbackAutoConfigDialog(aECs, strFriendlyName,
				strConverterIdentifier, eConversionType, strTestData);

			base.DisplayTestPage(form);
		}
	}
}
