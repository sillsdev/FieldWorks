/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: LgUnicodeCollater.h
Responsibility: John Thomson
Last reviewed: Not yet.

Description:	This file contains the class definition the Unicode TR10 Collating Engine.  It
				contains methods to produce sort keys and do direct string comparisons of these
				keys.  As of now, this engine generates sort keys with full decompositions and
				takes into consideration ignorable characters as flagged by fVariant.  It also
				establishes collating elements for expansions in default table.
				ENHANCE: This engine does not implement reordering, contractions or direction.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef LGUNICODECOLLATER_INCLUDED
#define LGUNICODECOLLATER_INCLUDED

/*---------------------------------------------------------------------------------------------
Class: CollatingElement
Description: This class defines the bit field that makes up a collating element.  It contains
			 the three weights of an element along with flags to flag certain properties.  Note
			 that with expansion elements uWeight1 and uWeight2 contain an index into an array
			 of sequences of characters, and uWeight3 contains how many characters are in the
			 expansion.
Hungarian: colel
---------------------------------------------------------------------------------------------*/
class CollatingElement
{
public:
	uint uWeight1 : 16;  //uWeight2 and uWeight2 will either be weights or an index into
	uint uWeight2 : 8;	 //the array of multiples
	uint uWeight3 : 5;	 //Either a weight or a count of expansion chars for an element
	uint fMultiple : 1;	 //Flags whether an element has an expansion
	uint fVariant : 1;	 //Flags whether an element is ignorable when this option is set.
	uint fReorder : 1;   //Flags whether the position of an element has changed from the
						 //standard sequence.

	bool Multiple() const
	{
		return fMultiple;
	}
	void Multiple(bool fNew)
	{
		fMultiple = fNew ? 1 : 0;
	}
	bool Variant() const
	{
		return fVariant;
	}
	void Variant(bool fNew)
	{
		fVariant = fNew ? 1 : 0;
	}
	bool Reorder() const
	{
		return fReorder;
	}
	void Reorder(bool fNew)
	{
		fReorder = fNew ? 1 : 0;
	}

	// The index to look for multiple collating elements when the primary
	// one has fMultiple set is found by concatenating two weight fields
	int MultipleIndex() const
	{
		return (uWeight2 << 16) | uWeight1;
	}

	void MultipleIndex(int icolel)
	{
		Assert((icolel >> 24) == 0);
		uWeight1 = (uint)(icolel & 0x0000FFFF);
		uWeight2 = (uint)((icolel >> 16) & 0x000000FF);
	}
};

/*----------------------------------------------------------------------------------------------
Class: LgUnicodeCollater
Description:  The implementation of the Unicode TR10 collating engine.
			ENHANCE:  This engine does not support reordering, contractions or direction
Hungarian: luc
----------------------------------------------------------------------------------------------*/
class LgUnicodeCollater :
	public ILgCollatingEngine,
	public ISimpleInit
{
public:
	// Static methods
	static void CreateCom(IUnknown *punkOuter, REFIID iid, void ** ppv);

	// Constructors/destructors/etc.
	LgUnicodeCollater();
	virtual ~LgUnicodeCollater();

	//IUnknown methods
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(ULONG, AddRef)(void)
	{
		return InterlockedIncrement(&m_cref);
	}
	STDMETHOD_(ULONG, Release)(void)
	{
		long cref = InterlockedDecrement(&m_cref);
		if (cref == 0) {
			m_cref = 1;
			delete this;
		}
		return cref;
	}

	// ISimpleInit Methods
	STDMETHOD(InitNew)(const BYTE * prgb, int cb);
	STDMETHOD(get_InitializationData)(BSTR * pbstr);

	// ILgCollatingEngine Methods
	STDMETHOD(get_SortKey)(BSTR bstrValue, LgCollatingOptions colopt, BSTR * pbstrKey);
	STDMETHOD(SortKeyRgch)(const OLECHAR * prgch, int cchIn, LgCollatingOptions colopt,
		int cchMaxOut, OLECHAR * pchKey, int * pcchOut);
	STDMETHOD(Compare)(BSTR bstrValue1, BSTR bstrValue2, LgCollatingOptions colopt,
		int * pnVal);
	STDMETHOD(get_WritingSystemFactory)(ILgWritingSystemFactory ** pwsf);
	STDMETHOD(putref_WritingSystemFactory)(ILgWritingSystemFactory * pwsf);
	STDMETHOD(get_SortKeyVariant)(BSTR bstrValue, LgCollatingOptions colopt, VARIANT * psaKey);
	STDMETHOD(CompareVariant)(VARIANT saValue1, VARIANT saValue2, LgCollatingOptions colopt,
		int * pnVal);
	STDMETHOD(Open)(BSTR bstrLocale);
	STDMETHOD(Close)();

	// Member variable access

	// Other public methods

protected:
	// Member variables
	long m_cref;

	//Pointer to access decomposition property methods in character property engine
	//this pointer is initialized to null in the constructor.
	ILgCharacterPropertyEnginePtr m_qcpe;

	ILgWritingSystemFactoryPtr m_qwsf;

	//Count of how many collating elements are in the main array of collating elements.
	static int g_ccolel;

	//Pointer to main array of collating elements obtained from LgUnicodeCollateInit.h.
	static const CollatingElement * g_prgcolel;

	//Pointer to array of sequences of multiple collating elements.
	static const CollatingElement * g_prgcolelMultiple;

	//Pointer to an array of indexes into the main collating element array.  The index is
	//of the first collating element on each page.  -1 indicates that (U, 0x20, 2) works
	//for the entire page.
	static const short * g_prgicolelPage;

	//Pointer to an array of counts of collating elements in the main collating element
	//array.  Each array index contains the number of collating elements on each page - 1.  A count
	//of 255 indicates a full page of 256 collating elements.  Any other count indicates
	//that if the lsb > g_prgccolelPage[msb] + 1, (U, 0x20, 2) works for that element.
	static const byte * g_prgccolelPage;


	// Static methods

	// Constructors/destructors/etc.

	// Other protected methods
	virtual int FindColel(OLECHAR ch);
	bool PackWeights(OLECHAR *&pchKey, int &cchOut, int cchMaxOut, int nWeight, bool &fEven);

};
#endif  //LGUNICODECOLLATER_INCLUDED
