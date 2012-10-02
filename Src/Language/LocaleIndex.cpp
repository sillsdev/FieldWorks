/*
 *    LocaleIndex.cpp
 *
 *    Implementation of a class that supports retrieval of Language (ISO 639-1 (2 letter) code)
 *    and Country (ISO 3166-1 alpha-2 (2 letter) code) for a given Locale ID.
 *
 *    Andrew Weaver - 2007-05-09
 *
 *    $Id$
 */

//:>********************************************************************************************
//:>	Include files
//:>********************************************************************************************

#include <cassert>
#include <unicode/locid.h>
#include <iostream>

#include "LocaleIndex.h"

/**
 * Get a displayable representation of a LocaleInfo instance (for debugging purposes).
 * @return std::ostream
 */
std::ostream& operator << (std::ostream& s, const LocaleIndex::LocaleInfo& p)
{
	return s << p.language << ", " << p.country;
}

//:>********************************************************************************************
//:>	LocaleIndex Constructors/Destructor
//:>********************************************************************************************

/**
 * Construct the singleton instance of the LocaleIndex class, storing the locale
 * information obtained from ICU in a Map.
 */
LocaleIndex::LocaleIndex()
{
	std::cout << "LocaleIndex::LocaleIndex()" << std::endl;

	// Obtain the array of locales from ICU
	int numOfLocales = 0;
	const Locale* locales = Locale::getAvailableLocales(numOfLocales);

	/*std::cout << "# locales: " << numOfLocales << std::endl;*/

	// Store the required locale info in the locale map
	for (int i = 0; i < numOfLocales; i++)
	{
		LocaleInfo newInfo(locales[i].getLanguage(), locales[i].getCountry());

		LCID localeID = locales[i].getLCID();
		if (localeID == 0)
			continue;  // Discard all locales with an localeID of zero

		LocaleMap::const_iterator iter = localeMap.find(localeID);
		if (iter == localeMap.end())  // localeID not found in map
		{
			localeMap[localeID] = newInfo;  // Insert a new map entry
		}
		else  // localeID found in map
		{
			const LocaleInfo& mapInfo = (*iter).second;
			bool sameLanguage = mapInfo.language == newInfo.language;
			bool sameCountry  = mapInfo.country  == newInfo.country;
			assert(sameLanguage && (mapInfo.country.empty() || sameCountry));
			if (!(sameLanguage && (mapInfo.country.empty() || sameCountry)))
			{
				// TODO: Should we be logging this error?
				std::cout << "New entry (0x" << std::hex << localeID << std::dec
					<< ", " << newInfo
					<< ") conflicts with (0x" << std::hex << localeID << std::dec
					<< ", " << mapInfo
					<< ") already in map!" << std::endl;
			}
		}
	}

	/*for (LocaleMap::const_iterator p = localeMap.begin();
		p != localeMap.end(); p++)
	{
		std::cout << "0x" << std::hex << (*p).first << std::dec << " = " << (*p).second << std::endl;
	}

	std::cout << "# entries: " << localeMap.size() << std::endl;*/
}

//:>********************************************************************************************
//:>	Other LocaleIndex methods
//:>********************************************************************************************

/**
 * Get a reference to the singleton instance of the LocaleIndex class.
 * @return LocaleIndex
 */
LocaleIndex& LocaleIndex::Instance()
{
	std::cout << "LocaleIndex::Instance()" << std::endl;

	static LocaleIndex instance;
	return instance;
}

/**
 * Get the Language (ISO 639-1 (2 letter) code) corresponding to the supplied Locale ID.
 * @param localeID input Locale ID
 * @return std::string (Language)
 */
std::string LocaleIndex::GetLanguage(LCID localeID)
{
	std::cout << "LocaleIndex::GetLanguage()" << std::endl;

	LocaleMap::const_iterator iter = localeMap.find(localeID);
	if (iter == localeMap.end())
		return std::string();
	return (*iter).second.language;
}

/**
 * Get the Country (ISO 3166-1 alpha-2 (2 letter) code) corresponding to the supplied Locale ID.
 * @param localeID input Locale ID
 * @return std::string (Country)
 */
std::string LocaleIndex::GetCountry(LCID localeID)
{
	std::cout << "LocaleIndex::GetCountry()" << std::endl;

	LocaleMap::const_iterator iter = localeMap.find(localeID);
	if (iter == localeMap.end())
		return std::string();
	return (*iter).second.country;
}
