using ECInterfaces;

namespace SilEncConverters40
{
	class TechHindiSiteConfig : EncConverterConfig
	{
		public TechHindiSiteConfig()
			: base
			(
			typeof(TechHindiSiteEncConverter).FullName,
			TechHindiSiteEncConverter.CstrDisplayName,
			TechHindiSiteEncConverter.CstrHtmlFilename,
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
			TechHindiSiteAutoConfigDialog form = new TechHindiSiteAutoConfigDialog(aECs, m_strDisplayName,
				m_strFriendlyName, m_strConverterID, m_eConversionType, m_strLhsEncodingID,
				m_strRhsEncodingID, m_lProcessType, m_bIsInRepository);

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

			TechHindiSiteAutoConfigDialog form = new TechHindiSiteAutoConfigDialog(aECs, strFriendlyName,
				strConverterIdentifier, eConversionType, strTestData);

			base.DisplayTestPage(form);
		}
	}
}
