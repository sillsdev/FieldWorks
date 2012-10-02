using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;   // for the class attributes
using ECInterfaces;                     // for IEncConverter

namespace SilEncConverters31
{
	public class AdaptItEncConverterConfig : EncConverterConfig
	{
		public AdaptItEncConverterConfig()
			: base
			(
			typeof(AdaptItEncConverter).FullName,
			AdaptItEncConverter.strDisplayName,
			AdaptItEncConverter.strHtmlFilename,
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
			AdaptItLookupAutoConfigDialog form = new AdaptItLookupAutoConfigDialog(aECs, m_strDisplayName, m_strFriendlyName,
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

			AdaptItLookupAutoConfigDialog form = new AdaptItLookupAutoConfigDialog(aECs, strFriendlyName,
				strConverterIdentifier, eConversionType, strTestData);

			base.DisplayTestPage(form);
		}
	}
}
