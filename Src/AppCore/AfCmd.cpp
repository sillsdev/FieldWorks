/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfCmd.cpp
Responsibility: Shon Katzenberger
Last reviewed:

-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE

/*----------------------------------------------------------------------------------------------
	The generic CmdHandler command map.
----------------------------------------------------------------------------------------------*/
BEGIN_CMD_MAP_BASE(CmdHandler)

END_CMD_MAP_NIL()


/*----------------------------------------------------------------------------------------------
	Find a command map entry for the given command.
	Return true if an entry was found, and ppcme is set to the entry.
----------------------------------------------------------------------------------------------*/
bool CmdHandler::FindCmdMapEntry(int cid, uint grfcmm, CmdMapEntry ** ppcme)
{
	AssertObj(this);
	AssertPtr(ppcme);
	Assert(cid != kcidNil);

	CmdMap * pcm;
	CmdMapEntry * pcmeT;
	CmdMapEntry *pcmeDef = NULL;

	// Process the linked list of command maps for each class.
	for (pcm = GetCmdMap(); pcm; pcm = pcm->m_pcmBase)
	{
		// Process the command entries for each map.
		for (pcmeT = pcm->m_prgcme; pcmeT->m_cid != kcidNil; pcmeT++)
		{
			// If it matches the cid and flags, return it.
			if (pcmeT->m_cid == cid && (pcmeT->m_grfcmm & grfcmm))
			{
				*ppcme = pcmeT;
				return true;
			}
		}

		// Check for a default function.
		if (!pcmeDef && pcmeT->m_pfncmd && (pcmeT->m_grfcmm & grfcmm))
			pcmeDef = pcmeT;
	}

	// No specific one found, return the default one.
	if (pcmeDef)
	{
		*ppcme = pcmeDef;
		return true;
	}

	return false;
}


/*----------------------------------------------------------------------------------------------
	Returns true iff the command was completely handled and shouldn't be dispatched any further.
----------------------------------------------------------------------------------------------*/
bool CmdHandler::FDoCmd(Cmd * pcmd)
{
	AssertObj(this);
	AssertObj(pcmd);

	if (pcmd->m_cid == 111) // paste
	{
		int i = 1; i++;
	}

	CmdPtr qcmd = pcmd;
	CmdMapEntry * pcme;
	uint grfcmm;

	if (!pcmd->m_qcmh)
		grfcmm = kfcmmNobody;
	else if (this == pcmd->m_qcmh)
		grfcmm = kfcmmThis;
	else if (pcmd->m_qcmh->Hwnd() && m_hwnd &&::IsChild(pcmd->m_qcmh->Hwnd(), m_hwnd))
		grfcmm = kfcmmChild;
	else
		grfcmm = kfcmmOthers;

	if (!FindCmdMapEntry(pcmd->m_cid, grfcmm, &pcme) || !pcme->m_pfncmd)
		return false;

	return (this->*pcme->m_pfncmd)(pcmd);
}


/*----------------------------------------------------------------------------------------------
	Returns true iff the command state was set and shouldn't be dispatched any further.
	fMayExec is set to true if the command has a method that can be executed.
	Return true if the command is enabled.
----------------------------------------------------------------------------------------------*/
bool CmdHandler::FSetCmdState(CmdState & cms, bool & fMayExec)
{
	AssertObj(this);

	CmdMapEntry * pcme;
	uint grfcmm;
	CmdHandler * pcmh = cms.Target();

	fMayExec = false;

	if (!pcmh)
		// Set nobody if we don't have a target set in the CmdHandler.
		grfcmm = kfcmmNobody;
	else if (this == pcmh)
		// Set this if the target of the CmdHandler is self.
		grfcmm = kfcmmThis;
	else if (m_hwnd && pcmh->Hwnd() && ::IsChild(pcmh->Hwnd(), m_hwnd))
		// Set this if self is a child of the target of the CmdHandler.
		grfcmm = kfcmmChild;
	else
		// Set others if the target is different from self.
		grfcmm = kfcmmOthers;

	// Try to find a command entry that matches the cid and flags.
	if (!FindCmdMapEntry(cms.Cid(), grfcmm, &pcme))
		return false;

	// If found, check whether we have an enable method defined.
	fMayExec = pcme->m_pfncmd != NULL;
	if (!pcme->m_pfncms)
		return false;

	// Execute the enable method and return the results.
	return (this->*pcme->m_pfncms)(cms);
}


/*----------------------------------------------------------------------------------------------
	Add the pcmh to the command handler list.
----------------------------------------------------------------------------------------------*/
void CmdExec::AddCmdHandler(CmdHandler * pcmh, int cmhl, uint grfcmm)
{
	AssertObj(this);
	AssertObj(pcmh);

	int iche;
	CmdHndEntry che;

	FFindCmhl(cmhl, &iche);

	che.m_cmhl = cmhl;
	che.m_grfcmm = grfcmm;
	che.m_qcmh = pcmh;

	m_vche.Insert(iche, che);

	// Update the active handler iterators.
	CmdHndIter * pchi;

	for (pchi = m_pchiFirst; pchi; pchi = pchi->m_pobjNext)
	{
		if (pchi->m_icheNext > iche)
			pchi->m_icheNext++;
	}
}


/*----------------------------------------------------------------------------------------------
	Remove the command handler at the given handler level.
----------------------------------------------------------------------------------------------*/
void CmdExec::RemoveCmdHandler(CmdHandler * pcmh, int cmhl)
{
	AssertObj(this);
	AssertObj(pcmh);

	int iche;

	if (!FFindCmhl(cmhl, &iche))
		return;

	for (; iche < m_vche.Size(); iche++)
	{
		if (iche >= m_vche.Size() || m_vche[iche].m_cmhl != cmhl)
			return;
		if (m_vche[iche].m_qcmh == pcmh)
			break;
	}

	// Delete it.
	m_vche.Delete(iche);

	// Update the active handler iterators.
	CmdHndIter * pchi;

	for (pchi = m_pchiFirst; pchi; pchi = pchi->m_pobjNext)
	{
		if (pchi->m_icheNext > iche)
			pchi->m_icheNext--;
	}
}


void CmdExec::BuryCmdHandler(CmdHandler * pcmh)
{
	// TODO ShonK: implement.
	Assert(false);
}

/*----------------------------------------------------------------------------------------------
	Clear out the whole list. This is done from AfApp::CleanUp() ONLY! It removes reference
	counts on the command handlers which in some cases seem to be able, on W98, to cause objects
	referenced by the command handlers (often AfWnd subclasses) to hang around until after
	their DLLs are unloaded, with disastrous results.
	Also clears the command handler vector.
	Enhance JohnT: are there intended uses of this where we would be doing this too soon?
----------------------------------------------------------------------------------------------*/
void CmdExec::_BuryAllHandlers()
{
	m_vche.Clear();
	m_vqcmd.Clear();
	m_pchiFirst = NULL;
}


/*----------------------------------------------------------------------------------------------
	Look for the first command handler with the given handler level value.
----------------------------------------------------------------------------------------------*/
bool CmdExec::FFindCmhl(int cmhl, int * piche)
{
	int ivMin, ivLim;

	for (ivMin = 0, ivLim = m_vche.Size(); ivMin < ivLim; )
	{
		int ivMid = (ivMin + ivLim) / 2;
		if (m_vche[ivMid].m_cmhl < cmhl)
			ivMin = ivMid + 1;
		else
			ivLim = ivMid;
	}

	*piche = ivMin;
	return ivMin < m_vche.Size() && m_vche[ivMin].m_cmhl == cmhl;
}


/***********************************************************************************************
	Command queue and dispatching methods.
***********************************************************************************************/


/*----------------------------------------------------------------------------------------------
	This dispatches the command immediately (without putting it in the command queue).
	Returns true iff someone processed the command fully.
----------------------------------------------------------------------------------------------*/
bool CmdExec::FDispatchCmd(Cmd * pcmd)
{
	AssertObj(this);
	AssertObj(pcmd);

	if (pcmd->m_cid >= kcidMenuItemDynMin && pcmd->m_cid < kcidMenuItemDynLim)
	{
		// We are in one of the expanded menu items, so change the command ID to
		// the old ID of the dummy menu item, which is what the handler is looking for.
		//   m_rgn[1] holds the menu handle.
		//   m_rgn[2] holds the index of the selected item.
		int idOld;
		pcmd->m_rgn[0] = AfMenuMgr::kmaDoCommand;
		AfApp::Papp()->GetMenuMgr()->GetLastExpMenuInfo((HMENU *)&pcmd->m_rgn[1],
			&idOld);
		pcmd->m_rgn[2] = pcmd->m_cid - kcidMenuItemDynMin;
		pcmd->m_cid = idOld;
	}

	CmdPtr qcmd = pcmd; // Keep a reference count on the command.
	const CmdHndEntry * pche;
	CmdHndIter chi(this);

	// Pipe it through the command handlers, then to the target.
	Assert(!chi.m_icheNext);
	while (chi.m_icheNext < m_vche.Size())
	{
		pche = &m_vche[chi.m_icheNext++];
		if (!pcmd->m_qcmh)
		{
			if (!(pche->m_grfcmm & kfcmmNobody))
				continue;
		}
		else if (pche->m_qcmh == pcmd->m_qcmh)
		{
			if (!(pche->m_grfcmm & kfcmmThis))
				continue;
		}
		else if (pcmd->m_qcmh->Hwnd() && pche->m_qcmh->Hwnd()
			&& ::IsChild(pcmd->m_qcmh->Hwnd(), pche->m_qcmh->Hwnd()))
		{
			if (!(pche->m_grfcmm & kfcmmChild))
				continue;
		}
		else if (!(pche->m_grfcmm & kfcmmOthers))
			continue;

		if (pche->m_qcmh->FDoCmd(pcmd))
			return true;
	}

	if (pcmd->m_qcmh && pcmd->m_qcmh->FDoCmd(pcmd))
		return true;

	return false;
}


/*----------------------------------------------------------------------------------------------
	Get the current state of the command (whether it should be enabled, disabled, checked, etc).
----------------------------------------------------------------------------------------------*/
bool CmdExec::FSetCmdState(CmdState & cms)
{
	AssertObj(this);

	bool fMayExec;
	bool fSomeMayExec = false;
	const CmdHndEntry * pche;
	CmdHndIter chi(this);
	CmdHandlerPtr qcmh = cms.Target();

	// Pipe it through the command handlers, then to the target.
	Assert(!chi.m_icheNext);
	// Process each CmdHndEntry in the vector.
	while (chi.m_icheNext < m_vche.Size())
	{
		pche = &m_vche[chi.m_icheNext++];
		if (!qcmh)
		{
			// If we don't have a CmdState target, and nobody is clear, skip this one..
			if (!(pche->m_grfcmm & kfcmmNobody))
				continue;
		}
		else if (pche->m_qcmh == qcmh)
		{
			// If the CmdState target matches the CmdHndEntry and this is clear, skip this one..
			if (!(pche->m_grfcmm & kfcmmThis))
				continue;
		}
		else if (qcmh->Hwnd() && pche->m_qcmh->Hwnd()
			&& ::IsChild(qcmh->Hwnd(), pche->m_qcmh->Hwnd()))
		{
			if (!(pche->m_grfcmm & kfcmmChild))
				continue;
		}
		else if (!(pche->m_grfcmm & kfcmmOthers))
			// Or if others is clear, skip this one.
			continue;

		// We have something worth checking.
		fMayExec = false;
		// Execute the desired enable method and return true, if enabled.
		if (pche->m_qcmh->FSetCmdState(cms, fMayExec))
			return true;
		if (fMayExec)
			fSomeMayExec = true;
	}

	// None of the CmdHndEntry items matched the request, so try the target of the CmdState.
	// Return true if it was enabled.
	fMayExec = false;
	if (qcmh && qcmh->FSetCmdState(cms, fMayExec))
		return true;
	if (fMayExec)
		fSomeMayExec = true;

	// If anyone can execute this command, enable it; otherwise, disable.
	// (JohnT added this, because otherwise, if a command is disabled at some
	// point because there is temporarily no command handler for it, it can
	// never get enabled again unless it has a specific enabler function.
	// If I've understood right, we don't get here at all if a specific enabler
	// function has done the job definitively, because the enabler returns true.
	// Originally, Shon just had it disable if there is no handler, but not
	// enable if there is.)
	cms.Enable(fSomeMayExec);

	return false;
}


/*----------------------------------------------------------------------------------------------
	Dispatch the next command. Returns true iff a command was dispatched.
----------------------------------------------------------------------------------------------*/
bool CmdExec::FDispatchNextCmd(void)
{
	CmdPtr qcmd;

	if (!m_vqcmd.Pop(AddrOf(qcmd)))
		return false;

	FDispatchCmd(qcmd);

	return true;
}


/*----------------------------------------------------------------------------------------------
	Push the command onto the front of the command queue.
----------------------------------------------------------------------------------------------*/
void CmdExec::PushCmd(Cmd * pcmd)
{
	m_vqcmd.Push(pcmd);
}


/*----------------------------------------------------------------------------------------------
	Put the command on the back of the command queue.
----------------------------------------------------------------------------------------------*/
void CmdExec::EnqueueCmd(Cmd * pcmd)
{
	m_vqcmd.Insert(0, pcmd);
}


/*----------------------------------------------------------------------------------------------
	Remove matching commands from the queue.
----------------------------------------------------------------------------------------------*/
void CmdExec::FlushCid(int cid, CmdHandler * pcmh, int n0, int n1, int n2, int n3)
{
	int iqcmd;

	// Must walk backwards!
	for (iqcmd = m_vqcmd.Size(); --iqcmd >= 0; )
	{
		Cmd * pcmd = m_vqcmd[iqcmd];
		if (pcmd->m_cid == cid && pcmd->m_qcmh == pcmh && pcmd->m_rgn[0] == n0 &&
			pcmd->m_rgn[1] == n1 && pcmd->m_rgn[2] == n2 && pcmd->m_rgn[3] == n3)
		{
			m_vqcmd.Delete(iqcmd);
		}
	}
}
