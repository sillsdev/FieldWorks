/*----------------------------------------------------------------------------------------------
Class: VwColor
Description: Stores a color in a format compatible with Cairo
Author: Andrew Palmowski (Aug 18, 2006)
----------------------------------------------------------------------------------------------*/
#include "BasicTypes.h"
#include "ExtendedTypes.h"

#ifndef VWCOLOR_H_
#define VWCOLOR_H_

class VwColor
{
public:
	VwColor();
	VwColor(int nRGB);
	VwColor(COLORREF nRGB);

	bool operator==(const VwColor& other);

	double m_red;
	double m_green;
	double m_blue;
	bool   m_transparent;
protected:
	void CreateRGB(COLORREF nRGB);
};

#endif /*VWCOLOR_H_*/
