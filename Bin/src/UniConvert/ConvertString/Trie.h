/*----------------------------------------------------------------------------------------------
Copyright 1999, SIL International. All rights reserved.

File: Trie.h
Responsibility: Darrell Zook
Last reviewed: Never

	Main header file for TrieLevel and TrieElement.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef _TRIE_H_
#define _TRUE_H_

class TrieLevel;
class TrieElement;

class TrieLevel
{
public:
	TrieLevel();
	~TrieLevel();
	HRESULT AddKey(OLECHAR * pszKey, void * pvInfo);
	HRESULT FindElement(OLECHAR chElement, int & iPos);
	HRESULT FindKey(char * prgchKey, int * pcchKey, void ** ppvInfo);
	HRESULT FindKey(OLECHAR * prgchKey, int * pcchKey, void ** ppvInfo);

protected:
	int m_cte;
	TrieElement ** m_prgte;
};

class TrieElement
{
public:
	TrieElement(OLECHAR chElement, void * pvInfo);
	~TrieElement();

	HRESULT AddKey(OLECHAR * pszKey, void * pvInfo);
	HRESULT FindKey(char * prgchKey, int * pcchKey, void ** ppvInfo);
	HRESULT FindKey(OLECHAR * prgchKey, int * pcchKey, void ** ppvInfo);

	OLECHAR GetElement(void)
		{ return m_chElement; }
	void * GetInfo()
		{ return m_pvInfo; }
	void SetInfo(void * pvInfo)
		{ m_pvInfo = pvInfo; }

protected:
	OLECHAR m_chElement;
	TrieLevel * m_ptl;
	void * m_pvInfo;
};

#endif // _TRIE_H_