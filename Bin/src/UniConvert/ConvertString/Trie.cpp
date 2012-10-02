/*----------------------------------------------------------------------------------------------
Copyright 2000, SIL International. All rights reserved.

File: Trie.cpp
Responsibility: Darrell Zook
Last reviewed:

	Implementation file for TrieLevel and TrieElement.
----------------------------------------------------------------------------------------------*/
#include "Main.h"
#pragma hdrstop
#undef THIS_FILE
DEFINE_THIS_FILE

/***********************************************************************************************
	TrieLevel methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
TrieLevel::TrieLevel()
{
	m_cte = 0;
	m_prgte = NULL;
}

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
TrieLevel::~TrieLevel()
{
	for (int ite = 0; ite < m_cte; ite++)
		delete m_prgte[ite];
	delete m_prgte;
}

/*----------------------------------------------------------------------------------------------
	Parameters:
		pszKey: 16-bit key string to be added to the trie
		pvInfo: Pointer to the data to be stored with the key
----------------------------------------------------------------------------------------------*/
HRESULT TrieLevel::AddKey(OLECHAR * pszKey, void * pvInfo)
{
	AssertPtr(pszKey);
	Assert(pvInfo);

	int iPos;
	if (S_FALSE == FindElement(*pszKey, iPos))
	{
		TrieElement ** ppteT = (TrieElement **)realloc(m_prgte, sizeof(TrieElement *) * ++m_cte);
		if (!ppteT)
			return WarnHr(E_OUTOFMEMORY);
		m_prgte = ppteT;
		if (iPos == -1)
			iPos = 0;
		else
			memmove(&m_prgte[iPos + 1], &m_prgte[iPos], sizeof(TrieElement *) * (m_cte - iPos - 1));
		try
		{
			m_prgte[iPos] = NewObj TrieElement(*pszKey, NULL);
		}
		catch (...)
		{
			return WarnHr(E_OUTOFMEMORY);
		}
	}
	if (pszKey[1] != 0)
		return m_prgte[iPos]->AddKey(pszKey + 1, pvInfo);
	if (m_prgte[iPos]->GetInfo())
		return WarnHr(E_UNEXPECTED);
	m_prgte[iPos]->SetInfo(pvInfo);
	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Parameters:
		chElement: Character to search for in the current trie level
		iPos: Will contain the position of the element if it is found, or
				the position it should be added to

	This function will return S_FALSE if the element was not found.
	Otherwise it will return S_OK.
----------------------------------------------------------------------------------------------*/
HRESULT TrieLevel::FindElement(OLECHAR chElement, int & iPos)
{
	int iteLower = 0;
	int iteUpper = m_cte - 1;
	int iteTry = 0;

	while (iteLower <= iteUpper)
	{
		iteTry = (iteLower + iteUpper) >> 1;
		if (chElement < m_prgte[iteTry]->GetElement())
			iteUpper = iteTry - 1;
		else if (chElement > m_prgte[iteTry]->GetElement())
			iteLower = iteTry + 1;
		else
		{
			iPos = iteTry;
			return S_OK;
		}
	}

	iPos = m_prgte ? iteLower : -1;
	return S_FALSE;
}

/*----------------------------------------------------------------------------------------------
	Parameters:
		prgchKey: 8-bit key string to find
		pcchKey: Will contain the length of the matched key if there is one.
			This should be initialized to 0 before calling FindKey.
		ppvInfo: Will contain the data that was associated with the key or
			NULL if the key could not be found.

	This method returns S_OK if the key was found or S_FALSE if it was not found.
----------------------------------------------------------------------------------------------*/
HRESULT TrieLevel::FindKey(char * prgchKey, int * pcchKey, void ** ppvInfo)
{
	AssertPtr(prgchKey);
	AssertPtr(pcchKey);
	Assert(ppvInfo);

	*ppvInfo = NULL;
	int iPos;
	if (0 == *prgchKey || FindElement((BYTE)*prgchKey, iPos) == S_FALSE)
		return S_FALSE;

	(*pcchKey)++;

	HRESULT hr = m_prgte[iPos]->FindKey(prgchKey + 1, pcchKey, ppvInfo);
	if (hr != S_OK)
	{
		if (!(*ppvInfo) && m_prgte[iPos]->GetInfo())
		{
			// This should only get set once at the level where the key was matched.
			*ppvInfo = m_prgte[iPos]->GetInfo();
			return S_OK;
		}
		return hr;
	}
	return hr;
}

/*----------------------------------------------------------------------------------------------
	Parameters:
		prgchKey: 16-bit key string to find
		pnKeyLength: Will contain the length of the matched key if there is one.
			This should be initialized to 0 before calling FindKey.
		ppvInfo: Will contain the data that was associated with the key or
			NULL if the key could not be found.

	This method returns S_OK if the key was found or S_FALSE if it was not found.
----------------------------------------------------------------------------------------------*/
HRESULT TrieLevel::FindKey(OLECHAR * prgchKey, int * pcchKey, void ** ppvInfo)
{
	AssertPtr(prgchKey);
	AssertPtr(pcchKey);
	Assert(ppvInfo);

	*ppvInfo = NULL;
	int iPos;
	if (0 == *prgchKey || FindElement(*prgchKey, iPos) == S_FALSE)
		return S_FALSE;

	(*pcchKey)++;

	HRESULT hr = m_prgte[iPos]->FindKey(prgchKey + 1, pcchKey, ppvInfo);
	if (hr != S_OK)
	{
		if (!(*ppvInfo) && m_prgte[iPos]->GetInfo())
		{
			// This should only get set once at the level where the key was matched.
			*ppvInfo = m_prgte[iPos]->GetInfo();
			return S_OK;
		}
		return hr;
	}
	return hr;
}


/***********************************************************************************************
	TrieElement methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
TrieElement::TrieElement(OLECHAR chElement, void * pvInfo)
{
	m_chElement = chElement;
	m_pvInfo = pvInfo;
	m_ptl = NULL;
}

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
TrieElement::~TrieElement()
{
	if (m_pvInfo)
		delete m_pvInfo;
	if (m_ptl)
		delete m_ptl;
}

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
HRESULT TrieElement::AddKey(OLECHAR * pszKey, void * pvInfo)
{
	if (!m_ptl)
	{
		try
		{
			m_ptl = NewObj TrieLevel;
		}
		catch (...)
		{
			return WarnHr(E_OUTOFMEMORY);
		}
	}

	return m_ptl->AddKey(pszKey, pvInfo);
}

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
HRESULT TrieElement::FindKey(char * prgchKey, int * pcchKey, void ** ppvInfo)
{
	if (m_ptl)
		return m_ptl->FindKey(prgchKey, pcchKey, ppvInfo);
	return S_FALSE;
}

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
HRESULT TrieElement::FindKey(OLECHAR * prgchKey, int * pcchKey, void ** ppvInfo)
{
	if (m_ptl)
		return m_ptl->FindKey(prgchKey, pcchKey, ppvInfo);
	return S_FALSE;
}