/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2015 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: GrPseudoMap.h
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:
	The GrPseudoMap class.
----------------------------------------------------------------------------------------------*/
#ifdef _MSC_VER
#pragma once
#endif
#ifndef GR_PSEUDOMAP_INCLUDED
#define GR_PSEUDOMAP_INCLUDED

//:End Ignore

namespace gr
{

/*----------------------------------------------------------------------------------------------
	A mapping between a Unicode value and a pseudo-glyph.

	Hungarian: psd
----------------------------------------------------------------------------------------------*/

class GrPseudoMap
{
public:
	unsigned int Unicode()	{ return m_nUnicode; }
	gid16 PseudoGlyph()	{ return m_chwPseudo; }

	void SetUnicode(int n)			{ m_nUnicode = n; }
	void SetPseudoGlyph(gid16 chw)	{ m_chwPseudo = chw; }

protected:
	//	Instance variables:
	unsigned int m_nUnicode;
	gid16 m_chwPseudo;
};

} // namespace gr

#endif // !GR_PSEUDOMAP_INCLUDED
