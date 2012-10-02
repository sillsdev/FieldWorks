/*
 *    LocaleIndex.h
 *
 *    Declaration of a class that supports retrieval of Language (ISO 639-1 (2 letter) code)
 *    and Country (ISO 3166-1 alpha-2 (2 letter) code) for a given Locale ID.
 *
 *    Andrew Weaver - 2007-05-09
 *
 *    $Id$
 */

#ifndef LOCALEINDEX_H
#define LOCALEINDEX_H

#include <map>
#include <string>

class LocaleIndex
{
public:
	struct LocaleInfo
	{
		LocaleInfo(std::string l = std::string(), std::string c = std::string())
			: language(l), country(c)
		{
		}

		bool operator == (const LocaleInfo& other)
		{
			return language == other.language && country == other.country;
		}

		std::string language;  // ISO 639-1 (2 letter) language code
		std::string country;   // ISO 3166-1 alpha-2 (2 letter) country code
	};

	typedef int LCID;
	std::string GetLanguage(LCID localeID);
	std::string GetCountry(LCID localeID);

	static LocaleIndex& Instance();

private:
	LocaleIndex();

	typedef std::map<LCID, LocaleInfo> LocaleMap;
	LocaleMap localeMap;
};

#endif //LOCALEINDEX_H
