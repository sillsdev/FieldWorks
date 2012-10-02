/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfCmd.h
Responsibility: Shon Katzenberger
Last reviewed:

-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef AfCmd_H
#define AfCmd_H 1


/*----------------------------------------------------------------------------------------------
	Forward declarations.
----------------------------------------------------------------------------------------------*/
class Cmd;
class CmdState;
class CmdHandler;
class CmdExec;
class CmdHndIter;


/*----------------------------------------------------------------------------------------------
	Specifies which targets a command handler will see commands for.
----------------------------------------------------------------------------------------------*/
enum CmdMapMask
{
	kfcmmNil = 0,
	// The window that is processing the message from FWndProc will only use a command handler
	// that was attached by the same window.
	kfcmmThis = 1,
	// The command is being processed by a non-window class.
	kfcmmNobody = 2,
	// The window that is processing the message from FWndProc will only use a command handler
	// that was attached by a child of the window.
	kfcmmChild = 4,
	// The window that is processing the message from FWndProc will use any command handler
	// with the correct cid.
	kfcmmOthers = 8,

	kgrfcmmAll = kfcmmThis | kfcmmNobody | kfcmmChild | kfcmmOthers,
};


/*----------------------------------------------------------------------------------------------
	Command enable / disable options.
----------------------------------------------------------------------------------------------*/
enum CmdEnableState
{
	kfcesNil     = 0x0000,
	kfcesDisable = 0x0001, // Set to disabled.
	kfcesEnable  = 0x0002, // Set to enabled.
	kfcesUncheck = 0x0004, // Set to un-checked.
	kfcesCheck   = 0x0008, // Set to checked.
	kfcesBullet  = 0x0010, // Set to bulleted.
	kfcesText    = 0x0020, // Set the text.

	kgrfcesMark	= kfcesUncheck | kfcesCheck | kfcesBullet,
};


/*----------------------------------------------------------------------------------------------
	Functions for defining command maps.
----------------------------------------------------------------------------------------------*/
#define CMD_MAP_DEC(cls) \
	private: \
		static CmdMapEntry s_rgcme[]; \
	protected: \
		static CmdMap s_cmdm; \
		virtual CmdMap * GetCmdMap(void) { return &s_cmdm; }

// For defining the command map in a .cpp file.
#define BEGIN_CMD_MAP_BASE(cls) \
	cls::CmdMap cls::s_cmdm = { NULL, cls::s_rgcme }; \
	cls::CmdMapEntry cls::s_rgcme[] = {
#define BEGIN_CMD_MAP(cls) \
	cls::CmdMap cls::s_cmdm = { &(cls::SuperClass::s_cmdm), cls::s_rgcme }; \
	cls::CmdMapEntry cls::s_rgcme[] = {

#define ON_CID(cid, pfncmd, pfneds, grfcmm) \
	{ cid, (PfnCmd)pfncmd, (PfnCms)pfneds, grfcmm },

// Use this if the command should only be used if it is being processed by the same window.
#define ON_CID_ME(cid, pfncmd, pfneds) \
	{ cid, (PfnCmd)pfncmd, (PfnCms)pfneds, kfcmmThis },

// Use this if the command should only be used if it is being processed by the same window
// or a non-window class.
#define ON_CID_GEN(cid, pfncmd, pfneds) \
	{ cid, (PfnCmd)pfncmd, (PfnCms)pfneds, kfcmmThis | kfcmmNobody },

// Use this if the command should only be used if it is being processed by the same window,
// a non-window class, or if it is a child of the window being processed.
// Note that with multiple frame windows open, this will make sure it is processed by the same
// frame window.
#define ON_CID_CHILD(cid, pfncmd, pfneds) \
	{ cid, (PfnCmd)pfncmd, (PfnCms)pfneds, kfcmmThis | kfcmmNobody | kfcmmChild },

// Use this if the command should be processed by any class.
// Note that when multiple frame windows are open, the command may be processed by a different
// frame window from which it was initiated.
#define ON_CID_ALL(cid, pfncmd, pfneds) \
	{ cid, (PfnCmd)pfncmd, (PfnCms)pfneds, kgrfcmmAll },

#define END_CMD_MAP(pfncmdDef, pfnedsDef, grfcmm) \
	{ kcidNil, (PfnCmd)pfncmdDef, (PfnCms)pfnedsDef, grfcmm } };
#define END_CMD_MAP_NIL() \
	{ kcidNil, NULL, NULL, kfcmmNil } };


/*----------------------------------------------------------------------------------------------
	Command Handler base class.
----------------------------------------------------------------------------------------------*/
class CmdHandler : public GenRefObj
{
public:
	virtual bool FDoCmd(Cmd * pcmd);
	virtual bool FSetCmdState(CmdState & cms, bool & fMayExec);

	HWND Hwnd(void)
	{
		return m_hwnd;
	}

#ifdef DEBUG
	bool AssertValid(void)
	{
		GenRefObj::AssertValid();
		AssertPtr(this);
		return true;
	}
#endif // DEBUG

protected:
	// Command function.
	typedef bool (CmdHandler::*PfnCmd)(Cmd * pcmd);

	// Command enabler function.
	typedef bool (CmdHandler::*PfnCms)(CmdState & cms);

	// Command Map Entry.
	struct CmdMapEntry
	{
		int m_cid;
		PfnCmd m_pfncmd;
		PfnCms m_pfncms;
		uint m_grfcmm;
	};

	// Command Map.
	struct CmdMap
	{
		CmdMap * m_pcmBase;
		CmdMapEntry * m_prgcme;
	};

	virtual bool FindCmdMapEntry(int cid, uint grfcmm, CmdMapEntry ** ppcme);

	// This is used in AfWnd and not applicable in AfApp. However, we are defining it here
	// so that ON_CID_CHILD can be used to determine if windows are in a parent relationship.
	// When not used, this is simply NULL.
	HWND m_hwnd;

	CMD_MAP_DEC(CmdHandler);
};

typedef GenSmartPtr<CmdHandler> CmdHandlerPtr;


/*----------------------------------------------------------------------------------------------
	Command object.
	Hungarian: cmd.
----------------------------------------------------------------------------------------------*/
const int kcnCmd = 4;

class Cmd : public GenRefObj
{
public:
	Cmd(void)
	{
		m_cid = 0;
		ClearItems(m_rgn, kcnCmd);
	}

	Cmd(int cid, CmdHandler * pcmh = NULL, int n0 = 0, int n1 = 0, int n2 = 0, int n3 = 0)
	{
		m_cid = cid;
		m_qcmh = pcmh;
		m_rgn[0] = n0;
		m_rgn[1] = n1;
		m_rgn[2] = n2;
		m_rgn[3] = n3;
	}

#ifdef DEBUG
	bool AssertValid(void)
	{
		GenRefObj::AssertValid();
		AssertPtr(this);
		AssertPtrN(m_qcmh);
		return true;
	}
#endif // DEBUG

	// The target of the command - may be null.
	CmdHandlerPtr m_qcmh;

	// The command id.
	int m_cid;

	// The parameters.
	int m_rgn[kcnCmd];
};


typedef GenSmartPtr<Cmd> CmdPtr;


/*----------------------------------------------------------------------------------------------
	For setting UI for a command.
	Hungarian: cms.
----------------------------------------------------------------------------------------------*/
class CmdState
{
public:
	CmdState(int cid = 0, CmdHandler * pcmh = NULL, Pcsz pszText = NULL,
		HWND hwndToolbar = NULL)
	{
		AssertObjN(pcmh);
		AssertPszN(pszText);
		Init(cid, pcmh, pszText, hwndToolbar);
	}

	void Init(int cid, CmdHandler * pcmh, Pcsz pszText, HWND hwndToolbar = NULL)
	{
		AssertObjN(pcmh);
		AssertPszN(pszText);
		m_cid = cid;
		m_qcmh = pcmh;
		m_grfces = 0;
		m_strb.Assign(pszText);
		m_fToolbar = hwndToolbar != NULL;
		m_hwndToolbar = hwndToolbar;
	}

	int Cid(void)
	{
		return m_cid;
	}

	CmdHandler * Target(void)
	{
		return m_qcmh;
	}

	virtual void SetState(uint grfces)
	{
		m_grfces = (m_grfces & kfcesText) | (grfces & ~kfcesText);
	}

	virtual void Enable(bool fEnable = true)
	{
		if (fEnable)
			m_grfces = (m_grfces & ~kfcesDisable) | kfcesEnable;
		else
			m_grfces = (m_grfces & ~kfcesEnable) | kfcesDisable;
	}

	virtual void SetCheck(bool fCheck = true)
	{
		if (fCheck)
			m_grfces = (m_grfces & ~(kfcesUncheck | kfcesBullet)) | kfcesCheck;
		else
			m_grfces = (m_grfces & ~(kfcesCheck | kfcesBullet)) | kfcesUncheck;
	}

	virtual void SetBullet(bool fBullet = true)
	{
		if (fBullet)
			m_grfces = (m_grfces & ~(kfcesUncheck | kfcesCheck)) | kfcesBullet;
		else
			m_grfces = (m_grfces & ~(kfcesCheck | kfcesBullet)) | kfcesUncheck;
	}

	virtual void SetText(const achar * prgch, int cch)
	{
		m_strb.Assign(prgch, cch);
		m_grfces |= kfcesText;
	}

	void SetText(Pcsz psz)
	{
		AssertPszN(psz);
		SetText(psz, StrLen(psz));
	}

	virtual int Grfces(void)
	{
		return m_grfces;
	}

	virtual Pcsz Text(void)
	{
		return m_strb.Chars();
	}

	virtual int GetExpMenuItemIndex(void)
	{
		Assert(!m_fToolbar);
		return m_iitemExp;
	}

	virtual HWND GetToolbar()
	{
		Assert(m_fToolbar);
		return m_hwndToolbar;
	}

	bool IsFromToolbar()
	{
		return m_fToolbar;
	}

protected:
	int m_cid;
	CmdHandlerPtr m_qcmh;
	uint m_grfces;
	StrAppBuf m_strb;
	bool m_fToolbar; // This will be true when setting the state of toolbar buttons.
	union
	{
		int m_iitemExp; // This is only used for expanded menu items.
		HWND m_hwndToolbar; // This will be used for setting the state of toolbar buttons.
	};

	friend class AfMenuMgr;
};


/*----------------------------------------------------------------------------------------------
	Command dispatcher. We will probably have one of these for each active thread.
----------------------------------------------------------------------------------------------*/
class CmdExec : public GenRefObj
{
public:
	/*------------------------------------------------------------------------------------------
		Command handler management.
	------------------------------------------------------------------------------------------*/

	// Add a command handler to the command handler list.
	// NOTE: Any window that calls this needs a corresponding RemoveCmdHandler to release it!
	virtual void AddCmdHandler(CmdHandler * pcmh, int cmhl, uint grfcmm = kfcmmNobody);

	// Remove a command handler to the command handler list.
	virtual void RemoveCmdHandler(CmdHandler * pcmh, int cmhl);

	// Remove a command handler from all internal structures (so it can die in peace).
	virtual void BuryCmdHandler(CmdHandler * pcmh);

	// Clean everything out (used only when shutting down application).
	void _BuryAllHandlers();

	/*------------------------------------------------------------------------------------------
		Command management.
	------------------------------------------------------------------------------------------*/

	// Get the next command from the queue and dispatch it.
	// Returns true iff a command was handled.
	virtual bool FDispatchNextCmd(void);

	// Dispatch the given command immediately.
	virtual bool FDispatchCmd(Cmd * pcmd);

	// Get state of the command.
	virtual bool FSetCmdState(CmdState & cms);

	// Enqueue means post the command to the back of the queue. Push means post it to the front
	// of the queue.
	virtual void EnqueueCmd(Cmd * pcmd);
	virtual void PushCmd(Cmd * pcmd);

	// Remove any commands from the queue that match these values.
	// REVIEW ShonK: How should we specify wildcards?
	virtual void FlushCid(int cid, CmdHandler * pcmh = NULL, int n0 = 0, int n1 = 0,
		int n2 = 0, int n3 = 0);

protected:
	// The command handler list.
	struct CmdHndEntry
	{
		// Handler priority.
		CmdHandlerPtr m_qcmh;
		int m_cmhl;
		int m_grfcmm;
	};
	Vector<CmdHndEntry> m_vche;

	// The command queue.
	Vector<CmdPtr> m_vqcmd;

	// An iterator for the command handler list. This is automatically updated when the
	// command handler list changes.
	class CmdHndIter : public LLBase<CmdHndIter>
	{
	public:
		CmdHndIter(CmdExec * pcex) : LLBase<CmdHndIter>(&pcex->m_pchiFirst)
		{
			m_icheNext = 0;
		}

		int m_icheNext;

		friend class CmdExec;
	};

	// Linked list of active command handler iterators.
	friend class CmdHndIter;
	CmdHndIter * m_pchiFirst;

	bool FFindCmhl(int cmhl, int * piche);
};

typedef GenSmartPtr<CmdExec> CmdExecPtr;


#endif // !AfCmd_H
