// --------------------------------------------------------------------------------------------
// <copyright from='2011' to='2011' company='SIL International'>
// 	Copyright (c) 2011, SIL International. All Rights Reserved.
//
// 	Distributable under the terms of either the Common Public License or the
// 	GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
// --------------------------------------------------------------------------------------------
#if __MonoCS__
using System;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.Common.Keyboarding.Linux
{
	/// <summary>
	/// This class represents an ICU locale.
	/// </summary>
	public class IcuLocale
	{
		/// <summary>
		/// Gets or sets the identifier of the ICU locale.
		/// </summary>
		public string Id { get; private set; }

		/// <summary>
		/// Gets the 3-letter ISO country code
		/// </summary>
		public string CountryCode { get; private set; }

		/// <summary>
		/// Gets the 3-letter ISO 639-3 language code
		/// </summary>
		public string LanguageCode { get; private set; }

		/// <summary>
		/// Gets the Windows locale identifier, or 0 if none defined.
		/// </summary>
		public int LCID { get; private set; }

		/// <summary>
		/// Gets the country and language codes concatenated so that they can be used as a key.
		/// </summary>
		public string LanguageCountry
		{
			get
			{
				if (string.IsNullOrEmpty(CountryCode) && string.IsNullOrEmpty(LanguageCode))
					return string.Empty;
				return LanguageCode + "_" + CountryCode;
			}
		}

		/// <summary>
		/// Initializes a new instance of the
		/// <see cref="T:SIL.FieldWorks.Common.Keyboarding.Linux.IcuLocale"/> class.
		/// </summary>
		public IcuLocale(string localeId)
		{
			Id = localeId;
			CountryCode = Icu.GetISO3Country(localeId);
			LanguageCode = Icu.GetISO3Language(localeId);
			LCID = Icu.GetLCID(localeId);
		}
	}
}
#endif
