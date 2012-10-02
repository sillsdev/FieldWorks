using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;   // for the class attributes
using ECInterfaces;                     // for IEncConverter

namespace SilEncConverters31
{
	public class CmpdEncConverterConfig : EncConverterConfig
	{
		public CmpdEncConverterConfig()
			: base
			(
			typeof(CmpdEncConverter).FullName,
			CmpdEncConverter.strDisplayName,
			CmpdEncConverter.strHtmlFilename,
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
			CmpdAutoConfigDialog form = new CmpdAutoConfigDialog(aECs, m_strDisplayName, m_strFriendlyName,
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

			CmpdAutoConfigDialog form = new CmpdAutoConfigDialog(aECs, strFriendlyName,
				strConverterIdentifier, eConversionType, strTestData);

			base.DisplayTestPage(form);
		}
	}
}
