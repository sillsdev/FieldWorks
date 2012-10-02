/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: VwNotifier.h
Responsibility: John Thomson
Last reviewed: Not yet.

Description:
	VwNotifier classes represent the relationship between boxes and the properties and objects
	which they display. Specifically, one notifier keeps track of everything that is
	displayed as part of one occurrence of one view of one object.

	The abstract notifier class keeps track of the depth of embedding of the view of its main
	object within the entire view hierarchy, and also of the object itself. It also tracks
	a "key" box, its "first covering box", the first box which contains all the contents of
	the notifier. This key is used to file the notifier in the root box of the display.

	The main VwNotifier class adds information about a sequence of properties of the object,
	indicating a range of boxes for each, and how to regenerate them.

	VwMissingNotifier covers the case that the attempt
	to display an object does not generate any boxes at all. In this case, we point
	a notifier at the first available containing box, and regenerate at that level if anything
	changes on the invisible object.

	VwPropListNotifier is used to record additional dependencies which require display update.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef VWNOTIFIER_INCLUDED
#define VWNOTIFIER_INCLUDED

// Used to return relationship between two notifiers (VwNotifier::HowRelated)
// Hungarian: vnr
enum VwNoteRel
{
	kvnrSame,  // argument is the same notifier
	kvnrAncestor, // argument is an ancestor of the recipient
	kvnrDescendant, // argument is a descendent of the recipient
	kvnrBefore, // argument is logically before the recipient
	kvnrAfter,  // argument is logically after the recipient
};

// Notifier Properties used to document how a particular property was inserted in the view.
// Hungarian: vnp
// ENHANCE JohnT: are the ones for non-property Add methods useful?
enum VwNoteProps
{
	kvnpNone, // Default, not a property
	kvnpObjProp, // Property set with AddObjProp
	kvnpObjVec, // Property set with AddObjVec
	kvnpObjVecItems, // Property set with AddObjVecItems (often LazyVecItems is similar)
	kvnpObj, // non-property inserted with AddObj
	// ENHANCE JohnT: Should AddProp be changed to AddVariant and kvnpProp to kvnpVariant?
	kvnpProp, // Property set with AddProp.
	// ENHANCE JohnT: Do we need ones for AddDerivedProp and AddMultiProp when implemented?
	kvnpStringProp,	// Property set with AddStringProp
	kvnpIntProp, // Property set with AddIntProp
	kvnpTimeProp, // Property set with AddTimeProp
	kvnpStringAltMember, // Property set with AddStringAltMember
	kvnpStringAlt, // Property set with AddStringAlt
	kvnpStringAltSeq, // Property set with AddStringAltSeq
	kvnpString, // String added with AddString (not a property)
	kvnpWindow, // Something inserted with AddWindow (not a property?)
	kvnpSeparatorBar, // Inserted with AddSeparatorBar
	kvnpLazyVecItems, // Property set with AddLazyVecItems (very like ObjVecItems)
	kvnpUnicodeProp, // PropertySet with AddUnicodeProp.
	kvnpIntPropPic, // Property set with AddIntPropPic.
	kvnpPropTypeMask = 0x7f, // space for 128 property types
	kvnpEditable = 0x80,  // bit for editability, orthogonal to others.
};

// Used for return result of EditableSubstringAt.
// These are ordered by desirability.
enum VwEditPropVal
{
	kvepvNone, // No object property at the indicated point
	kvepvNonStringProp, // the box was found in the indicated property, but no particular string prop
	kvepvReadOnly, // A non-editable property covers the point.
	kvepvEditable, // A (text-)editable property at the indicated point
};

class VwNotifier;
DEFINE_COM_PTR(VwNotifier);

/*----------------------------------------------------------------------------------------------
Class: VwAbstractNotifier
Description:
Hungarian: vanote
----------------------------------------------------------------------------------------------*/
class VwAbstractNotifier : public IVwNotifyChange
{
public:
	// Constructors/destructors/etc.
	VwAbstractNotifier(int ihvoProp);
	virtual ~VwAbstractNotifier();

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

	// IVwNotifyChange methods.
	STDMETHOD(PropChanged)(HVO obj, int tag, int ivMin, int cvIns, int cvDel);

	void Close(void);

	// Member variable access.

	// Returns an uncounted reference to it.
	HVO Object()
	{
		return m_hvo;
	}

	void SetObject(HVO wobj);

	// The box used to look it up in the root box.
	VwBox * KeyBox()
	{
		return m_pboxKey;
	}

	void _KeyBox(VwBox* pbox)
	{
		Assert(pbox == NULL || dynamic_cast<VwBox *>(pbox));
		m_pboxKey = pbox;
	}

	int Level() const;

	VwNoteRel HowRelated(VwAbstractNotifier * panoteArg, int * piprop);

	// Other public methods.

	// The first box (at or after index *pibox, which is updated to the place to start searching
	// for more)
	virtual VwBox * FirstBox(int * pibox = 0) = 0;
	virtual VwBox * LastBox() = 0;
	virtual VwBox * FirstCoveringBox()
	{
		return FirstBox();
	}

	virtual VwBox* LastCoveringBox()
	{
		return LastBox();
	}
	virtual VwBox * LastTopLevelBox() {return LastBox();}

	bool GetBoxForProp(int tag, int& ipropStartInOut, VwBox ** ppboxRet);

	virtual void ReplaceBoxes(VwBox * pboxOldFirst, VwBox * pboxOldLast,
		VwBox * pboxNewFirst, VwBox * pboxNewLast, NotifierVec & vpanoteDel);

	int ObjectIndex()
	{
		return m_ihvoProp;
	}

	void SetObjectIndex(int iunk)
	{
		m_ihvoProp = iunk;
	}

	// The index of the property this is part of in its parent notifier; or, in the whole root
	// box, if it is a top-level notifier.
	int PropIndex()
	{
		return m_ipropParent;
	}

	void SetPropIndex(int iprop)
	{
		m_ipropParent = iprop;
	}

	VwNotifier * Parent()
	{
		return m_qnoteParent;
	}

	void SetParent(VwNotifier * pnote)
	{
		m_qnoteParent = pnote;
	}

	virtual VwEditPropVal EditableSubstringAt(VwParagraphBox *pvpbox, int ichMin, int ichLim, bool fAssocBefore,
		HVO * phvo, int * ptag, int * pichMin, int * pichLim,
		IVwViewConstructor ** ppvvc, int * pfrag, int *piprop, VwNoteProps * pvnp,
		int * pitssProp, ITsString ** pptssProp)
	{
		return kvepvNone; // a reasonable default.
	}

	bool HasAncestor(VwAbstractNotifier * panote, VwAbstractNotifier ** ppanoteChild);

	virtual void FixOffsets(VwBox * pbox, int ditss)
	{
	}
	virtual void AdjustForStringRep(VwParagraphBox * pvpbox, int itssMin, int itssLim,
		int ditss, int levMin);
	virtual void AddToMap(ObjNoteMap & mmhvoqnote);
#ifdef DEBUG
	virtual void AssertValid();
#endif
protected:
	long m_cref;

	// Index of this object in its containing property.
	int m_ihvoProp;

	// The firstCoveringBox under which this box is registered in the NotifierMap of its root.
	VwBox * m_pboxKey;

	// The cookie for which this organizes the view.
	HVO m_hvo;

	// The next outer notifier.
	VwNotifierPtr m_qnoteParent;

	// index of the property of pnoteParent that this is part of.
	int m_ipropParent;

	// Interface used to access data
	ISilDataAccessPtr * m_qsda;
};

DEFINE_COM_PTR(VwAbstractNotifier);

/*----------------------------------------------------------------------------------------------
Class: VwNotifier
Description: VwNotifier manages a list of properties of the main object that are displayed
	sequentially.
Hungarian: vnote
----------------------------------------------------------------------------------------------*/
class VwNotifier : public VwAbstractNotifier
{
public:
	// Static methods
	static VwNotifier * Create(int ihvoProp, int cprop);

	// Constructor is private. Use Create.
	virtual ~VwNotifier();

	// Member variable access
	virtual VwBox * LastBox()
	{
		return m_pboxLast;
	}

	void SetLastBox(VwBox * pbox)
	{
		m_pboxLast = pbox;
	}

	// IVwNotifyChange methods

	STDMETHOD(PropChanged)(HVO hvo, int tag, int ivMin, int cvIns, int cvDel);

	// Accessing the arrays allocated at the end of the object.

	// Note: logically it would be a little cleaner to make each of these functions call the one
	// before it. However, the last would then involve deeply nested inline functions, which is
	// a performance problem.
	// The box pointers array is actually part of the object.
	VwBox ** Boxes()
	{
		return m_rgpbox;
	}

	// The tags array starts right after boxes, one array of pointers along.
	int * Tags()
	{
		return (PropTag *) (m_rgpbox + m_cprop);
	}

	// The string indices come after tags, after one array of pointers and one of tags.
	int * StringIndexes()
	{
		return (int *) (((char *) m_rgpbox) + m_cprop * (sizeof(VwBox *) + sizeof(PropTag)));
	}

	// The fragment values, after one array of pointers, one of tags, and one of ints.
	int * Fragments()
	{
		return (int *) (((char *) m_rgpbox) + m_cprop * (sizeof(VwBox *) + sizeof(PropTag)
			+ sizeof(int)));
	}

	// The view constructors, after one pointer, one tag, and two int arrays.
	IVwViewConstructor ** Constructors()
	{
		return (IVwViewConstructor **) (((char *) m_rgpbox) + m_cprop * (sizeof(VwBox *) +
			sizeof(PropTag) + 2 * sizeof(int)));
	}

	// The styles, after two pointer, one tag and two int arrays. I cheat a little by assuming
	// pointers are all the same size.
	VwPropertyStore ** Styles()
	{
		return (VwPropertyStore **) (((char *) m_rgpbox) + m_cprop * (2 * sizeof(VwBox *) +
			sizeof(PropTag) + 2 * sizeof(int)));
	}

	// The Flags, after three pointers, one tag, and two int arrays.
	VwNoteProps * Flags()
	{
		return (VwNoteProps *) (((char *) m_rgpbox) + m_cprop * (3 * sizeof(VwBox *) +
			sizeof(PropTag) + 2 * sizeof(int)));
	}

	virtual VwEditPropVal EditableSubstringAt(VwParagraphBox *pvpbox, int ichMin, int ichLim, bool fAssocBefore,
		HVO * phvo, int * ptag, int * pichMin, int * pichLim,
		IVwViewConstructor ** ppvvc, int * pfrag, int *piprop, VwNoteProps * pvnp,
		int * pitssProp, ITsString ** pptssProp);

	// ENHANCE JohnT: could more of these be protected?
	virtual VwBox * FirstBox(int * pibox = 0);
	VwGroupBox * ContainingBox();
	virtual VwBox * FirstCoveringBox();
	virtual VwBox * LastCoveringBox();
	virtual VwBox * LastTopLevelBox();

	void GetBuildRecsFor(int tag, BuildVec &bldvec, VwNotifier ** ppanote);

	void GetPropForSubNotifier(VwNotifier * pnote, VwBox ** ppboxFirstProp,
		int * pitssFirstProp, int * ptag, int * piprop);
	int PropCount(int tag, VwBox * pboxLim = NULL, int itss = -1);

	void ReplaceBoxSeq(int iprop, int itssMin,  int itssLim, VwBox * pboxFirstOld1,
		VwBox * pboxLastOld1, VwBox * pboxFirstNew1, VwBox * pboxLastNew1, NotifierVec & vpanoteNew,
		IVwGraphics * pvg, bool fNeedLayout = true);
	void _SetLastBox();
	bool GetBoxForProp(int tag, int& iattrStartInOut, VwBox ** ppboxRet);
	void DeleteBoxes(VwBox * pboxFirst, VwBox * pboxLast, int chvoStopLevel1,
		VwBox * pboxReplaceFirst, NotifierVec & vpanoteDel, BoxSet * pboxsetDeleted);

	virtual void ReplaceBoxes(VwBox * pboxOldFirst, VwBox * pboxOldLast,
		VwBox * pboxNewFirst, VwBox * pboxNewLast, NotifierVec & vpanoteDel);
	virtual void FixOffsets(VwBox * pbox, int ditss);

	static VwNotifier * NextNotifierAt(int lev, VwBox * pboxStart, int itssStart);
	VwNotifier * NextNotifier();
	VwNotifier * FindChild(PropTag tag, int ipropTag, int ihvoTarget, int ich);
	VwNotifier * FindChild(int iprop, int ihvoTarget, int ich);
	int TagOccurrence(int tag, int ipropTag);

	int CProps()
	{
		return m_cprop;
	}
	int LastStringIndex()
	{
		return m_itssLimSubString;
	}
	void AdjustForStringRep(VwParagraphBox * pvpbox, int itssMin, int itssLim,
		int ditss, int levMin);

	VwBox * GetLimOfProp(int ipropTarget, int * pitssLim = NULL);
	int PropInfoFromBox(VwBox * pbox);
#ifdef DEBUG
	void AssertValid();
#endif
protected:
	// Types of property update we may do. Also used as return type from AdjustReplacedStuff
	// and UpdateLazyProp
	enum UpdateType
	{
		kutNormal, // Normal update, replace whole property, old and new values not empty.
		kutInsItems, // Pure insert into non-empty prop: nothing to delete.
		kutDelItems, // Pure delete, leaving non-empty prop: nothing to replace it with.
		// This last value is used when AdjustReplacedStuff could not handle this case by
		// replacement of a subset of the items in the property. The caller should fall back
		// to replacing the whole property.
		kutFail,
		kutDone, // Lazy prop entirely updated by modifying LazyBox.
	};

	// Member variables
	// This variable points to the very last box in the containing box's list which is part of
	// this object. This allows us to replace just the display of this object, omitting any
	// separators between it and a subsequent object.
	VwBox * m_pboxLast;

	// If m_pboxLast is a para box, indicates the last string that is part of this object.
	int m_itssLimSubString;

	// Number of property records in the object.
	int m_cprop;

	// NOTE: memory is always allocated for seven following arrays, logically like this:
	// VwBox m_rgpbox[m_cprop];
	// int m_rgtag[m_cprop];
	// int m_rgitssStart[m_cprop];
	// int m_frag[m_cprop];
	// VwViewConstructor * m_pvvc[m_cprop];
	// VwPropertyStore * m_pzvps[m_cprop];
	// VwNoteProps * m_vnp[m_cprop]

	// One entry in each of these arrays gives info about one of the properties of the object
	// being displayed:
	//	- the first box used to display it (or NULL if nothing shows)
	//	- the tag of the property
	//	- if the box is a para box, the index of the first character that belongs to
	//			this property
	//	- a pointer to the view constructor that should be used if this property needs
	//			to be regenerated
	//	- the 'frag' identifier that needs to be passed to the constructor
	//	- the styles that should be current when invoking the view constructor
	//	- a flag byte indicates which method was used to add the property to the VwEnv.

	// If the property in question is a basic property added directly without a view
	// constructor, the view constructor is null. In this case, the m_vnp entry indicates how
	// to display it.

	// The exact limits of a property may be indicated in several ways.
	// If a paragraph was the current flow object when the property was opened (and, necessarily,
	// also when it was closed), then the box for the property is the paragraph, and the itss
	// for the property indicates where it starts. (The starting point might be either a string
	// or an embedded box.) The itss where it ends is found in one of three ways:
	//  - If the next non-empty property is in the same (paragraph) box, its string
	//		index is a lim for the current property;
	//  - If the next non-empty property has a different box pointer, then the limit is the
	//		end of the paragraph.
	//  - If this is the last property of the notifier, m_itssLimSubString gives the limit.

	// If a paragraph was not the current flow object when the property was opened and closed,
	// then the box for the property is the first box that belongs to the
	// property, and the last box for the property is found in one of three ways:
	//  - If the next non-empty property is in the same linked list of boxes as the first one,
	//		then the property ends with the box before that.
	//  - If the next non-empty property is not in the same linked list as the first one,
	//		then the property ends at the end of the linked list.
	//	- If this property is the last non-empty one, m_pboxLast points to the last box.

	// So that there is no ambiguity about which approach is being used, the string index is
	// set to -1 if we are using the second approach.

	// To make debugging a little easier we make the first of these variables real with a dummy
	// size
	VwBox * m_rgpbox[1];

	// Static methods

	// Constructors/destructors/etc.

	// Other protected methods


	/*------------------------------------------------------------------------------------------
	struct: PropBoxRec
	Description: A list of these records is used to find occurrences of a particular property
		within a notifier. (A given property could possibly be displayed more than one time,
		though this is unusual.) Note that we deliberately use first and last, not min and lim,
		because there may be no appropriate lim box following the last one of the prop.
	Hungarian: pbrec
	------------------------------------------------------------------------------------------*/
	struct PropBoxRec
	{
		int		iprop;      // index in props recorded in this notifier
		VwBox *	pboxFirst;  // first and last boxes of the prop
		VwBox *	pboxLast;
		int		itssMin;	// Range of strings in prop, if first and last boxes are
		int		itssLim;	// text ones
	};

	// Hungarian: vpbrec
	typedef Vector<PropBoxRec> PropBoxList;
	void GetPropOccurrences(int tag, int ws, PropBoxList& vpbrec);


	// protected methods
	void LimitsOfPropAt(int iprop, VwBox ** ppboxFirst, VwBox ** ppboxLast,
		int * pichMin, int * pichLim);
	bool OkayToReplace(PropBoxRec& pbrec, VwBox* pboxNewFirst);
	UpdateType AdjustReplacedStuff(VwBox * & pboxFirst, VwBox * & pboxLast, int & itssFirst,
		int & itssLast, int iprop, int ihvoMin, int chvoIns, int chvoDel,
		ISilDataAccess * psda, HVO hvo, int tag);
	void InsertBoxes(int iprop, int itssMin,  int itssLim, VwBox * pboxFirstOld,
		VwBox * pboxLastOld, VwBox * pboxFirstNew1, VwBox * pboxLastNew, NotifierVec & vpanoteNew,
		IVwGraphics * pvg);
	void InsertIntoFixMap(FixupMap & fixmap, VwBox * pboxNext, VwBox * pboxPrev,
		VwGroupBox * pgboxContainer);
	UpdateType UpdateLazyProp(VwBox * & pboxFirst, VwBox * & pboxLast,
		int iprop, int ihvoMin1, int chvoIns, int chvoDel1, VwBox * & pboxNew,
		VwPropertyStore * pzvps, IVwViewConstructor * pvc, PropTag tag , int frag);
	VwAbstractNotifier * FindChildOrLazy(int iprop, int ihvoTarget, VwLazyBox ** pplzb,
		VwNotifier ** ppnotePrev, VwLazyBox ** pplzbPrev);

private:
	// Private because a VwNotifier must be constructed using the static create method, which
	// handles the trick of storing the extra arrays in the same storage allocation block.
	VwNotifier(int ihvoProp, int cprop);
	bool operator=(const VwNotifier&);
};

/*----------------------------------------------------------------------------------------------
Class: VwRegenNotifier
Description: This is a baseclass for two kinds of notifiers both of which record that a higher-
level box needs to be regenerated if something changes.
Hungarian: vmnote
----------------------------------------------------------------------------------------------*/
class VwRegenNotifier : public VwAbstractNotifier
{
	typedef VwAbstractNotifier SuperClass;
public:
	virtual void ReplaceBoxes(VwBox * pboxOldFirst, VwBox * pboxOldLast,
		VwBox * pboxNewFirst, VwBox * pboxNewLast, NotifierVec & vpanoteDel);
protected:
	void Regenerate();

protected:
	// Member variables
	int m_cprop; // Number of properties monitored.
protected:
	// Proptected because a VwRegenNotifier must be constructed using the static create method
	// of one of its subclasses.
	// which handles the trick of storing the extra arrays in the same storage allocation block.
	VwRegenNotifier(int ihvoProp, int cprop);
	bool operator=(const VwRegenNotifier&);

	// This kind of notifier only deals with one box. It functions as key, first,
	// and last.
	virtual VwBox * FirstBox(int * pibox = 0)
	{
		return KeyBox();
	}
	virtual VwBox * LastBox()
	{
		return KeyBox();
	}
};

DEFINE_COM_PTR(VwRegenNotifier);

/*----------------------------------------------------------------------------------------------
Class: VwMissingNotifier
Description: This is used when the display of a particular object generates nothing at all: not
a single box, even an empty string. This means there is nothing to hook to as an indication of
where to regenerate. What we do is to point this special notifier at the lowest-level
containing box. Then, if any of the relevant properties changes, we find the lowest-level
real notifier that governs that box, and regenerate the appropriate property.
Hungarian: vmnote
----------------------------------------------------------------------------------------------*/
class VwMissingNotifier : public VwRegenNotifier
{
public:
	// Static methods
	static VwMissingNotifier * Create(int ihvoProp, int cprop);

	// Constructor is private. Use Create.
	virtual ~VwMissingNotifier();

	// IVwNotifyChange methods

	STDMETHOD(PropChanged)(HVO hvo, int tag, int ivMin, int cvIns, int cvDel);

	// Member variable access.

	// The tags array starts in an actual member variable.
	PropTag * Tags()
	{
		return m_rgtag;
	}

protected:
	PropTag m_rgtag[1]; // Actually size m_cprop

private:
	// Private because a VwMissingNotifier must be constructed using the static create method,
	// which handles the trick of storing the extra arrays in the same storage allocation block.
	VwMissingNotifier(int ihvoProp, int cprop)
		:VwRegenNotifier(ihvoProp, cprop)
	{
	}
	bool operator=(const VwMissingNotifier&);
};

DEFINE_COM_PTR(VwMissingNotifier);

/*----------------------------------------------------------------------------------------------
Class: VwPropListNotifier
Description: This is used when the view constructor explicitly says that regeneration must
be done if any of a list of properties changes. It is initialized with a list of (obj, prop)
pairs to monitor.
Hungarian: vmnote
----------------------------------------------------------------------------------------------*/
class VwPropListNotifier : public VwRegenNotifier
{
public:
	// Static methods
	static VwPropListNotifier * Create(int ihvoProp, int cprop);

	// Constructor is private. Use Create.
	virtual ~VwPropListNotifier();

	// IVwNotifyChange methods

	STDMETHOD(PropChanged)(HVO hvo, int tag, int ivMin, int cvIns, int cvDel);

	// Member variable access.

	// The tags array starts in an actual member variable.
	PropTag * Tags()
	{
		return m_rgtag;
	}

	HVO * Objects()
	{
		return (HVO *)(m_rgtag + m_cprop);
	}
	virtual void AddToMap(ObjNoteMap & mmhvoqnote);

protected:
	PropTag m_rgtag[1]; // Actually size m_cprop
	// followed by the following, allocated by the Create method:
	// HVO m_rghvo[m_cprop].

private:
	// Private because a VwPropListNotifier must be constructed using the static create method,
	// which handles the trick of storing the extra arrays in the same storage allocation block.
	VwPropListNotifier(int ihvoProp, int cprop)
		:VwRegenNotifier(ihvoProp, cprop)
	{
	}
	bool operator=(const VwPropListNotifier&);
};

DEFINE_COM_PTR(VwPropListNotifier);

/*----------------------------------------------------------------------------------------------
Class: VwPropListNotifier
Description: This is used when the view constructor explicitly says that regeneration must
be done if there is a change in whether a string equality test is true. This is extensively
used in views that test whether a string property is empty, and it can be very expensive to
regnerate with a PropList notifier every keystroke, so we only want to do it if the actual
result of evaluating the condition changes.
Hungarian: svnote
----------------------------------------------------------------------------------------------*/
class VwStringValueNotifier : public VwRegenNotifier
{
public:
	// Constructor and destructor
	VwStringValueNotifier(HVO hvo, int tag, int ws, ITsString * ptssVal, ISilDataAccess * psda);
	virtual ~VwStringValueNotifier();

	// IVwNotifyChange methods

	STDMETHOD(PropChanged)(HVO hvo, int tag, int ivMin, int cvIns, int cvDel);

	bool EvalString(ISilDataAccess * psda);

	// Member variable access.

protected:
	PropTag m_tag;
	int m_ws;
	ITsStringPtr m_qtssVal;
	bool m_fWasInitiallyEqual;

private:

	bool operator=(const VwPropListNotifier&);
};

DEFINE_COM_PTR(VwStringValueNotifier);
#endif  //VWNOTIFIER_INCLUDED
