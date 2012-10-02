/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: VwEnv.h
Responsibility: John Thomson
Last reviewed: Not yet.

Description:
	The main interface for constructing views. A hierarchy of objects
	and properties is mapped onto a hierarchy of "flow objects", parts
	of the visible display.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef VWENV_INCLUDED
#define VWENV_INCLUDED

// This seems to be needed so we can Assign to VwEnvPtr now that it is not a DeclareCoClass.
class __declspec(uuid("A4C6D3CC-12A7-49fb-A838-51E7970C1C3D")) VwEnv;

/*----------------------------------------------------------------------------------------------
Class: VwEnv
Description:
	The main class. See overall file comment.
Hungarian: vwenv
----------------------------------------------------------------------------------------------*/
class VwEnv : public IVwEnv
{
	friend class VwLazyBox;
	friend class VwNotifier;
public:
	// Static methods
	// For now, it does not look as if we needed to CoCreateInstance of these.
	// static void CreateCom(IUnknown *punkOuter, REFIID iid, void ** ppv);

	// Constructors/destructors/etc.
	VwEnv();
	virtual ~VwEnv();

	// IUnknown methods.
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(UCOMINT32, AddRef)(void)
	{
		return InterlockedIncrement(&m_cref);
	}
	STDMETHOD_(UCOMINT32, Release)(void)
	{
		long cref = InterlockedDecrement(&m_cref);
		if (cref == 0)
		{
			m_cref = 1;
			delete this;
		}
		return cref;
	}

	// Delimiting objects and properties within the display
	STDMETHOD(AddObjProp)(int tag, IVwViewConstructor * pvwvc, int frag);
	STDMETHOD(AddObjVec)(int tag, IVwViewConstructor * pvwvc, int frag);
	STDMETHOD(AddObjVecItems)(int tag, IVwViewConstructor * pvwvc, int frag);
	STDMETHOD(AddReversedObjVecItems)(int tag, IVwViewConstructor * pvwvc, int frag);
	STDMETHOD(AddObj)(HVO hvo, IVwViewConstructor * pvwvc, int frag);
	STDMETHOD(AddLazyVecItems)(int tag, IVwViewConstructor * pvwvc, int frag);
	STDMETHOD(AddLazyItems)(HVO * prghvo, int chvo, IVwViewConstructor * pvwvc, int frag);
	STDMETHOD(AddProp)(int tag, IVwViewConstructor * pvwvc, int frag);
	STDMETHOD(AddDerivedProp)(int * prgtag, int ctag, IVwViewConstructor * pvwvc, int frag);
	STDMETHOD(NoteDependency)(HVO * prghvo, PropTag * prgtag, int chvo);
	STDMETHOD(NoteStringValDependency)(HVO hvo, PropTag tag, int ws, ITsString * ptssVal);
	STDMETHOD(AddMultiProp)(HVO * prghvo, int * prgtag, int chvo, IVwViewConstructor * pvwvc, int frag);
	STDMETHOD(StartDependency)(HVO * prghvo, int * prgtag, int chvo);
	STDMETHOD(EndDependency)();

	STDMETHOD(CurrentObject)(HVO * phvo);
	STDMETHOD(get_OpenObject)(HVO * phvoRet);
	STDMETHOD(get_EmbeddingLevel)(int * pchvo);
	STDMETHOD(GetOuterObject)(int ichvoLevel, HVO * phvo, int * ptag, int * pihvo);
	STDMETHOD(get_DataAccess)(ISilDataAccess ** ppsda);

	// Inserting basic object displays into the view.
	STDMETHOD(AddStringProp)(int tag, IVwViewConstructor * pvwvc);
	STDMETHOD(AddUnicodeProp)(int tag, int ws, IVwViewConstructor * pvwvc);
	STDMETHOD(AddIntProp)(int tag);
	STDMETHOD(AddIntPropPic)(int tag, IVwViewConstructor * pvc, int frag, int nMin, int nMax);
	STDMETHOD(AddStringAltMember)(int tag, int ws, IVwViewConstructor * pvwvc);
	STDMETHOD(AddStringAlt)(int tag);
	STDMETHOD(AddStringAltSeq)(int tag, int * prgenc, int cws);
	STDMETHOD(AddString)(ITsString* pss);
	STDMETHOD(AddTimeProp)(int tag, DWORD flags);

	STDMETHOD(AddWindow)(IVwEmbeddedWindow* pew, int nAscentTwips, ComBool fJustifyRight, ComBool fAutoShow);
	STDMETHOD(AddSeparatorBar)();
	STDMETHOD(AddSimpleRect)(int rgb, int dmpWidth, int dmpHeight, int dmpBaselineOffset);
	STDMETHOD(AddPictureWithCaption)(IPicture * ppict, PropTag tag, ITsTextProps * pttpCaption,
		HVO hvoCmFile, int ws, int dxmpWidth, int dympHeight, IVwViewConstructor * pvwvc);
	STDMETHOD(AddPicture)(IPicture * ppict, PropTag tag, int dxmpWidth, int dympHeight);

	STDMETHOD(SetParagraphMark)(VwBoundaryMark boundaryMark);
	// Delimit layout flow objects.
	STDMETHOD(OpenDiv)();
	STDMETHOD(CloseDiv)();
	STDMETHOD(OpenParagraph)();
	STDMETHOD(OpenTaggedPara)();
	STDMETHOD(OpenMappedPara)();
	STDMETHOD(OpenMappedTaggedPara)();
	STDMETHOD(OpenConcPara)(int ichMinItem, int ichLimItem, VwConcParaOpts cpoFlags, int dmpAlign);
	STDMETHOD(OpenOverridePara)(int cOverrideProperties, DispPropOverride *prgOverrideProperties);
	STDMETHOD(CloseParagraph)();
	STDMETHOD(OpenInnerPile)();
	STDMETHOD(CloseInnerPile)();
	STDMETHOD(OpenSpan)();
	STDMETHOD(CloseSpan)();
	STDMETHOD(OpenTable)(int cCols, VwLength vwlenWidth, int twBorder,
		VwAlignment vaAlign, VwFramePosition vfpFrame, VwRule vrlRule,
		int twSpacing, int twPadding, ComBool fSelectOneCol);
	STDMETHOD(CloseTable)();
	STDMETHOD(OpenTableRow)();
	STDMETHOD(CloseTableRow)();
	STDMETHOD(OpenTableCell)(int nRowSpan, int nColSpan);
	STDMETHOD(CloseTableCell)();
	STDMETHOD(OpenTableHeaderCell)(int nRowSpan, int nColSpan);
	STDMETHOD(CloseTableHeaderCell)();
	STDMETHOD(MakeColumns)(int nColSpan, VwLength vlWidth);
	STDMETHOD(MakeColumnGroup)(int nColSpan, VwLength vlWidth);
	STDMETHOD(OpenTableHeader)();
	STDMETHOD(CloseTableHeader)();
	STDMETHOD(OpenTableFooter)();
	STDMETHOD(CloseTableFooter)();
	STDMETHOD(OpenTableBody)();
	STDMETHOD(CloseTableBody)();

	// Visual properties.
	STDMETHOD(put_IntProperty)(int sp, int pv, int nValue);
	STDMETHOD(put_StringProperty)(int sp, BSTR bstrValue);
	STDMETHOD(put_Props)(ITsTextProps * pttp);

	// Info and measurement
	STDMETHOD(get_StringWidth)(ITsString * ptss, ITsTextProps * pttp, int * dxs, int * dys);

	void Initialize(IVwGraphics * pvg, VwRootBox * pzrootb, IVwViewConstructor * pvc);
	void InitEmbedded(IVwGraphics * pvg, VwMoveablePileBox * pmpbox);
	void InitRegenerate(IVwGraphics * pvg, VwRootBox * pzrootb,
		VwGroupBox * pgboxContainer, VwPropertyStore * pzvps, HVO hvo,
		BuildVec * pvbldrec, int iprop);
	void GetRegenerateInfo(VwBox ** ppbox, NotifierVec & vpanoteIncomplete, bool fForErrorRecovery = false);
	void SetParentOfTopNotifiers(VwNotifier * pnote);

	static void IntToTsString(int nVal, ITsStrFactory ** pptsf, ISilDataAccess * psda, ITsString ** pptss);
	static void GetStringFactory(ITsStrFactory ** pptsf);
	static void TimeToTsString(int64 nTime, DWORD flags, ISilDataAccess * psda, ITsString ** pptss);
	void AddObjVecItemRange(int tag, IVwViewConstructor * pvvc, int frag,
		int ihvoMin, int ihvoLim);

	void Cleanup();
//	void MakeEmbeddedBoxes(VwParagraphBox * pvpbox, ITsString * ptss, IVwViewConstructor * pvvc);

protected:

	// Member variables
	// int m_nStylesheetID; // arbitrary number, we just store it
	// int m_nStyleMode; // another
	long m_cref;
	IVwGraphicsPtr m_qvg;

	// The view under construction.
	VwRootBoxPtr m_qrootbox;
	// and its data object
	ISilDataAccessPtr m_qsda;

	// true if an object, rather than an property, was last opened; used to automatically open
	// kNotAnAttr if an object is opened when expecting a property, and to report error if a
	// property is opened when expecting an object.
	bool m_fObjectOpen;

	// Box that new boxes will currently be added to.
	VwGroupBox * m_pgboxCurr;

	// Vector of open boxes.
	GroupBoxVec m_vpgboxOpen;

	// For making strings out of ints, etc.
	ITsStrFactoryPtr m_qtsf;

	// Style property handling.
	// Each box gets assigned a VwPropertyStore object from which it can read properties.
	// At any time, m_zvps is THE property store that will be assigned to a newly created box.
	// When a grouping flow object is opened, we create a default style for boxes inside it
	// by resetting uninheritable properties, thus producing a new style object from the
	// one assigned to the outer flow object. (If the same style is used for other outer flow
	// objects, the  internal style will be reused also.)
	// When we close a grouping flow object, we return to the top level internal style of the
	// containing flow object. We keep track of these on a stack, parallel to the open flow
	// object stack. If we have n boxes open, and close one, we will make the properties at
	// m_vpzvpsInitialStyles [n-1] current.
	// When we invoke a style, we create a new current properties object by applying that
	// modification to the current one.
	VwPropertyStorePtr m_qzvps;

	typedef Vector<VwPropertyStore *> PropsVec;
	PropsVec m_vpzvpsInitialStyles;

	// True if resetting properties for a table row.
	int m_fTableRow;

	/*------------------------------------------------------------------------------------------
	Class: NotifierRec
	Description: Info used to create notifiers. A notifier record contains the info we need for
		one item in the notifier: the tag of the property, and a pointer to the box, and if it
		is a string box, which might contain multiple underlying strings, the index of the first
		char. Also the view constructor, fragment, and current style to use in regenerating,
		and the VwNoteProps flag indicating what construction method was used.
	Hungarian: noterec
	------------------------------------------------------------------------------------------*/
	class NotifierRec
	{
	public:
		VwBox * pbox;
		int tag;
		int itssStart;
		int frag;
		IVwViewConstructorPtr qvvc;
		VwPropertyStore * pzvps;
		int noteProp;
		VwNoteProps vnp;

		NotifierRec()
		{
			Assert(pbox == NULL);
			// ktagGapInAttrs is a useful default for tag. See CloseProp.
			tag = ktagGapInAttrs;
			itssStart = -1;
			noteProp = 0;
			vnp = kvnpNone;
		}
	};

	// A vector keeps track of the NotiferRecs being accumulated for the current object,
	// until we have all of it and can build a more compact data structure in the notifier.
	// Hungarian: noterecvec
	typedef Vector<NotifierRec *> NotifierRecVec;
	NotifierRecVec m_noterecvecCurr;

	// When regenerating a single property, we don't have records about the previous
	// properties in m_noterecvecCurr. This causes a problem when we come to construct
	// notifiers for the objects in the property, because m_noterecvecCurr.Size() is not
	// an accurate indication of the property index in the parent object. The first variable
	// here holds the necessary correction, and the other indicates the stack level at which
	// it is needed. Both are zero until altered by InitRegenerate.
	int m_ipropAdjust;
	int m_insiAdjustLev;

	// We also need to keep track of the current object for which the notifier is being built
	HVO m_hvoCurr;

	// Number of objs added to the current property, so far.
	int m_chvoProp;

	/*------------------------------------------------------------------------------------------
	struct: NotifierStackItem
	Description: We need to keep a NotifierRec for each currently open object, as well as to
		keep track of all the objects. This stack does so. This information is also used in
		providing context information.
	Hungarian: nsi
	------------------------------------------------------------------------------------------*/
	struct NotifierStackItem
	{
		HVO hvo;

		// Tracks the number of objects that were so far in the current property when we did the
		// OpenObject that pushed this stack item. The count includes the open object.
		int chvoProp;

		NotifierRecVec noterecvec;
		// Saves size of m_vpanoteIncomplete at OpenObject
		int inoteIncomplete;
	};

	// Hungarian: nsivec
	typedef Vector<NotifierStackItem *> NotifierDataVec; // Hungarian nsivec
	NotifierDataVec m_nsivecStack;

	// Number of items on stack, in normal build; but main purpose is to set the level of
	// embedding for building notifiers.
	int m_cnsi;

	// It is possible we could open several properties of several nested objects before we
	// actually make a box, which becomes the first box of each of those properties. Therefore,
	// we keep a vector of NotifierRecs which are "waiting" for a box. When we actually make a
	// box, it "satisfies" all the notifier recs in this vector, and empties it. We also put a
	// dummy entry here when we close a property (and clear it when we close an object) so as to
	// tag parts of the object display that aren't part of a property.
	NotifierRecVec m_noterecvecOpenProps;

	// When we create a notifier, we have not yet created its parent notifier. Keep it in this
	// list until we do, then fill in its parent slot.
	NotifierVec m_vpanoteIncomplete;

	// Data structures used in regenerating part of a display.

	// While we regenerate part of the contents of a box, we save its previous first and
	// last boxes, and also a record of exactly which box it is.
	VwBox * m_pboxSaveFirst;
	VwBox * m_pboxSaveLast;
	VwGroupBox * m_pboxSaveContainer;

	static int GetUserWs(ISilDataAccess * psda);

	// General flow object handling
	void OpenFlowObject(VwGroupBox * pgbox);
	void CloseFlowObject();
	void ResetStyles();

	// Delimiting objects and properties
public:
	void OpenObject(HVO hvo);
	void CloseObject();
protected:
	void OpenProp(int tag, IVwViewConstructor * pvvc, int frag, VwNoteProps vnp,
		int chvoPrev = 0);
	void CloseProp();

	// Routines to allocate and free NotifierRecs.	If needed for performance, we can make a
	// cache of them as they will be heavily reused during the view building process. Make sure
	// it returns one with box null and prop tag kNotAnAttr.
	NotifierRec * GetNotifierRec()
	{
		return NewObj NotifierRec();
	}

	void FreeNotifierRec(NotifierRec * pnoterec)
	{
		delete pnoterec;
	}

	// Adding boxes and managing strings
	void AddBox(VwBox * pbox);
	void AddLeafBox(VwBox * pbox);

	// Other protected methods
	virtual VwParagraphBox * MakeParagraphBox(VwSourceType vst = kvstNormal);
	virtual VwDivBox * MakeDivBox();
	virtual VwPropertyStore * MakePropertyStore();

private:
	friend class VwRootBox; // so it can call OpenObject etc.
};
DEFINE_COM_PTR(VwEnv);
DEFINE_COM_PTR(IPicture);
#endif  //VWENV_INCLUDE
