/*-----------------------------------------------------------------------------------*//*:Ignore
Copyright 2000, SIL International. All rights reserved.

File: RnDeFeRoleParts.h
Responsibility: Ken Zook
Last reviewed: never

Description:
	Defines the RnDeFeRoleParts class for displaying RnRoledPartic.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef RNDEFE_ROLEPARTS_INCLUDED
#define RNDEFE_ROLEPARTS_INCLUDED 1

// Used as a placeholder for a RnRoledPartic hvo when the object does not exist.
enum {khvoDummyRnRoledPartic = -1};

/*----------------------------------------------------------------------------------------------
	This class provides functionality for a field editor to display a RnRoledParticipant on a
	single line, with the name of the role as the label, and the participants as a tags-type
	list. The field may display a +/- expansion box if it is the first one. The first one is
	always the one with an empty role. It may also have khvoDummyRnRoledPartic as the
	object id if it is a place holder for a real RnRoledParticipant. Otherwise, m_hvoObj is
	set to the hvo of the RnRoledParticipant, and m_flid is set to
	RnRoledPartic_Participants. The indent of each field after the first one will be
	one greater than the first field.
	@h3{Hungarian: derp}
----------------------------------------------------------------------------------------------*/
class RnDeFeRoleParts : public AfDeFeTags
{
public:
	typedef AfDeFeTags SuperClass;

	RnDeFeRoleParts();
	~RnDeFeRoleParts();

	virtual void Init(HVO pssl, HVO hvoOwner, int flid, HVO pssRole, bool fHier,
		PossNameType pnt = kpntName);
	virtual bool SaveEdit();

	virtual DeTreeState GetExpansion()
	{
		return m_dtsExpanded;
	}

	// @return Object id of outer object.
	virtual HVO GetOwner()
	{
		return m_hvoOwner;
	}

	// @return field id on GetOwner() that holds the intermediate object.
	virtual int GetOwnerFlid()
	{
		return m_flidOwner;
	}

	void SetExpansion(DeTreeState dts)
	{
		Assert((uint)dts < (uint)kdtsLim);
		m_dtsExpanded = dts;
	}
	virtual void SaveCursorInfo();
	virtual void RestoreCursor(Vector<HVO> & vhvo, Vector<int> & vflid, int ichCur);
	virtual void ListChanged(int nAction, HVO hvoPssl, HVO hvoSrc, HVO hvoDst, int ipssSrc,
		int ipssDst);
	virtual void OnReleasePtr();

protected:
	DeTreeState m_dtsExpanded; // The current state of the tree node.
	// Note: m_hvoObj holds the id for the RnRoledPartic held in this field, and
	// m_flid holds flidRnRoledPartic_Participants.
	HVO m_hvoOwner; // The object id of the owner of the RnRoledPartic for this field.
	int m_flidOwner; // The flid in m_hvoOwner holding the RnRoledPartic.
	HVO m_pssRole; // The id of the CmPossibility item we are displaying in the label.
	HVO m_psslRole; // The id of the Role possibility list. (0 if m_pssRole is 0).
};

#endif // RNDEFE_ROLEPARTS_INCLUDED
