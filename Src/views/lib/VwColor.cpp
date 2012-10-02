#include "VwColor.h"
#include "BasicTypes.h"

/*----------------------------------------------------------------------------------------------
	Instantiates a new Color object, setting all values to 0
----------------------------------------------------------------------------------------------*/
VwColor::VwColor()
{
	m_red = 0;
	m_green = 0;
	m_blue = 0;
	m_transparent = false;
}
/*----------------------------------------------------------------------------------------------
	Instantiates a new Color object, setting values according to nRGB
	Arguments:
		nRGB			RGB color value
----------------------------------------------------------------------------------------------*/
VwColor::VwColor(int nRGB) {
	CreateRGB((COLORREF)nRGB);
}

/*----------------------------------------------------------------------------------------------
	Instantiates a new Color object, setting values according to nRGB
	Arguments:
		nRGB			RGB color value
----------------------------------------------------------------------------------------------*/
VwColor::VwColor(COLORREF nRGB) {
	CreateRGB(nRGB);
}

void VwColor::CreateRGB(COLORREF nRGB) {
	m_blue  = ((nRGB >> 16) & 0xFF)	/ 255.0;
	m_green = ((nRGB >> 8)  & 0xFF) / 255.0;
	m_red   = (nRGB         & 0xFF)	/ 255.0;
	m_transparent = false;
}

/*----------------------------------------------------------------------------------------------
	Comparison operator, checks to see if red, green, blue and transparency values are identical
	Arguments:
		nRGB			RGB color value
----------------------------------------------------------------------------------------------*/
bool VwColor::operator==(const VwColor& other) {
	return other.m_red == m_red && other.m_green == m_green && other.m_blue == m_blue
		&& other.m_transparent == m_transparent;
}
