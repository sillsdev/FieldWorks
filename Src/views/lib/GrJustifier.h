/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 2003-2015 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: GrJustifier.h
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:
	A default justification agent for Graphite.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef GRJUSTIFIER_INCLUDED
#define GRJUSTIFIER_INCLUDED

/*----------------------------------------------------------------------------------------------
	Class: GrJustifier
	This class provides a basic justification/layout algorithm for applications that use
	Graphite.
----------------------------------------------------------------------------------------------*/
class GrJustifier : public gr::IGrJustifier
{
public:
	// Constructor:
	GrJustifier();
	~GrJustifier();

	virtual long IncRefCount(void)
	{
		return InterlockedIncrement(&m_cref);
	}
	virtual long DecRefCount(void)
	{
		long cref = InterlockedDecrement(&m_cref);
		if (cref == 0) {
			m_cref = 1;
			delete this;
		}
		return cref;
	}

	virtual gr::GrResult adjustGlyphWidths(gr::GraphiteProcess * pfgje,
		int iGlyphMin, int iGlyphLim,
		float dxCurrWidth, float dxDesiredWidth);

	//virtual GrResult suggestShrinkAndBreak(GraphiteProcess * pfgjwe,
	//	int iGlyphMin, int iGlyphLim, int dxsWidth, LgLineBreak lbPref, LgLineBreak lbMax,
	//	int * pdxShrink, LgLineBreak * plbToTry);

	// Return a Graphite-compatible justifier that can be stored in a Graphite segment.
	// TODO: remove
	//virtual void JustifierObject(IGrJustifier ** ppgjus)
	//{
	//	*ppgjus = this;
	//}

	// When a segment is being destroyed, there is nothing to do, since this is a pointer
	// to an object that is allocated elsewhere.
	// TODO: remove
	//virtual void DeleteJustifierPtr()
	//{
	//}

protected:
	long m_cref;

	gr::GraphiteProcess * m_pgreng;

	void DistributeRemainder(Vector<int> & vdxWidths, Vector<int> & vdxStretch,
		int dx, int iiMin, int iiLim,
		int * pdxStretchAchieved);
	int Lcm(Vector<int> & vnWeights, Vector<int> & vnMFactors);
	int PrimeFactors(int n, Vector<int> & vnPowersPerPrime);
	int NthPower(int nX, int nY);
};


#endif // !GRJUSTIFIER_INCLUDED
