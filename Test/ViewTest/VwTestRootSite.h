/*----------------------------------------------------------------------------------------------
Copyright (c) 2000-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: ViewTestRootSite.h
Responsibility: Luke Ulrich
Last reviewed: never

Description:
	Dummy Test Site for Testing Views
----------------------------------------------------------------------------------------------*/
#ifndef VWTESTROOTSITE
#define VWTESTROOTSITE

/*----------------------------------------------------------------------------------------------
Class: VwTestRootSite
Description:
	The main class. See overall file comment.
Hungarian: trs
----------------------------------------------------------------------------------------------*/
class VwTestRootSite : public IVwRootSite
{
public:
	VwTestRootSite();
	~VwTestRootSite();

	// IUnknown methods.
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

	// IVwRootSite methods.
	STDMETHOD(InvalidateRect)(IVwRootBox * pRoot, int twLeft, int twTop, int twWidth,
		int twHeight);
	STDMETHOD(GetGraphics)(IVwGraphics ** ppvg, RECT * prcSrcRoot, RECT * prcDstRoot);
	STDMETHOD(ReleaseGraphics)(IVwGraphics * pvg);
	STDMETHOD(GetAvailWidth)(int * ptwWidth);
	STDMETHOD(SizeChanged)();
	STDMETHOD(DoUpdates)();
	STDMETHOD(PropChanged)(HVO hvo, int tag, int ivMin, int cvIns, int cvDel);

	// Set the graphics parameters
	void SetVgObject(IVwGraphics *pvg);
	void SetSrcRoot(RECT rcSrcRoot);
	void SetDstRoot(RECT rcDstRoot);
	void SetAvailWidth(IVwRootBoxPtr ptb, int iwidth);
protected:
	long m_cref;
	IVwGraphics * m_pvg;
	RECT m_rcSrcRoot;
	RECT m_rcDstRoot;
	int m_twWidth;
};

DEFINE_COM_PTR(VwTestRootSite);
#endif