/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: ActionHandler.h
Responsibility: John Thomson
Last reviewed: never

Description:
	This file contains class declarations for the ActionHandler class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef ActionHandler_INCLUDED
#define ActionHandler_INCLUDED

class ActionHandler;

/*----------------------------------------------------------------------------------------------
	Cross-Reference: ${IActionHandler}

	@h3{Hungarian: acth}
----------------------------------------------------------------------------------------------*/
class ActionHandler : public IActionHandler
{
public:
	ActionHandler();
	~ActionHandler();

	STDMETHOD_(UCOMINT32, AddRef)(void);
	static void CreateCom(IUnknown * punkCtl, REFIID riid, void ** ppv);
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(UCOMINT32, Release)(void);

	STDMETHOD(BeginUndoTask)(BSTR bstrUndo, BSTR bstrRedo);
	STDMETHOD(EndUndoTask)();
	STDMETHOD(ContinueUndoTask)();
	STDMETHOD(EndOuterUndoTask)();
	STDMETHOD(BreakUndoTask)(BSTR bstrUndo, BSTR bstrRedo);
	STDMETHOD(BeginNonUndoableTask)();
	STDMETHOD(EndNonUndoableTask)();

	STDMETHOD(StartSeq)(BSTR bstrUndo, BSTR bstrRedo, IUndoAction * puact);
	STDMETHOD(GetUndoText)(BSTR * pbstrUndo);
	STDMETHOD(GetUndoTextN)(int iAct, BSTR * pbstrUndo);
	STDMETHOD(GetRedoText)(BSTR * pbstrRedo);
	STDMETHOD(GetRedoTextN)(int iAct, BSTR * pbstrRedo);
	STDMETHOD(AddAction)(IUndoAction * puact);
	STDMETHOD(CanUndo)(ComBool * pfCanUndo);
	STDMETHOD(CanRedo)(ComBool * pfCanRedo);
	STDMETHOD(Undo)(UndoResult * pures);
	STDMETHOD(Redo)(UndoResult * pures);
	STDMETHOD(CreateMarkIfNeeded)(ComBool fCreateMark);
	STDMETHOD(Rollback)(int nDepth);
	STDMETHOD(get_CurrentDepth)(int * pnDepth);
	STDMETHOD(Commit)();
	STDMETHOD(Close)();
	STDMETHOD(Mark)(int * phMark);
	STDMETHOD(CollapseToMark)(int hMark, BSTR bstrUndo, BSTR bstrRedo, ComBool * pf);
	STDMETHOD(DiscardToMark)(int hMark);
	STDMETHOD(get_TopMarkHandle)(int * phMark);
	STDMETHOD(get_TasksSinceMark)(ComBool fUndo, ComBool * pf);
	STDMETHOD(get_UndoableActionCount)(int * pcAct);
	STDMETHOD(get_UndoableSequenceCount)(int * pcAct);
	STDMETHOD(get_RedoableSequenceCount)(int * pcAct);

	STDMETHOD(get_UndoGrouper)(IUndoGrouper ** ppundg);
	STDMETHOD(put_UndoGrouper)(IUndoGrouper * pundg);

	STDMETHOD(get_IsUndoOrRedoInProgress)(ComBool * pfInProgress);
	STDMETHOD(get_SuppressSelections)(ComBool * pfSupressSel);

protected:
	int m_cref;

	// Depth of nesting of the tasks.
	int m_nDepth;

	// Labels for next task.
	StrUni m_stuNextUndo;
	StrUni m_stuNextRedo;
	// True if we have done at least one BeginUndoTask but have not yet put any undo-actions
	// on the stack.
	// JT: This flag is set in BeginUndoTask. It's real purpose is so we can postpone
	// recording the start of a new action sequence (done in CleanupRedoActions, believe
	// it or not) until there is at least one action to put in the sequence. Otherwise,
	// we might unnecessarily clean out the Redo stack for a 'task' that turns out to
	// have no substance.
	bool m_fStartedNext;

	// True to create a mark when you add an action, if there isn't one
	// False otherwise
	bool m_fCreateMarkIfNeeded;

	// True if it currently makes sense to Continue the current task. Calling Undo or Redo
	// definitely means this is not sensible, so a ContinueUndoTask following one of these
	// gets converted into an independent task.
	bool m_fCanContinueTask;

	// True if an action was added to the current task that changed the data.
	bool m_fDataChangeAction;

	// Index to the current action in the action vector m_vquact, ie, the most recent action
	// added and/or the next action to undo.
	int m_iuactCurr;
	// Vector of actions.
	Vector<IUndoActionPtr> m_vquact;

	// Index to the current action sequence given in the vector m_viSeqStart.
	// JT: This is the sequence that will be Undone if the user currently selects Undo.
	// It is initialized to -1, a state that indicates there is currently nothing that
	// can be undone.
	// It is also the sequence that will be added to by new actions.
	int m_iCurrSeq;
	// Vector of action sequences.
	// JT: I gather each item is an index into m_vquact, of the first action in a given
	// sequence (where a sequence is defined as the stuff from one outer BeginUndoTask
	// to the corresponding EndUndoTask, that is, typically what seems to the user like
	// a single indivisible undoable action). Some of these sequences may be currently
	// in the 'undone' state.
	Vector<int> m_viSeqStart;
	// Undo text that goes with the action sequence.
	Vector<StrUni> m_vstuUndo;
	// Redo text that goes with the action sequence.
	Vector<StrUni> m_vstuRedo;

	// For marking the stack with a range of temporary undo-tasks that are private to a
	// data-entry field editor (AfDeFieldEditor).
	Vector<int> m_viMarks;

	// keep a ref to the undo interface if given
	IUndoGrouperPtr m_qundg;

	// True for the duration of an Undo/Redo operation. Used to suppress
	// recording any new actions.
	bool m_fUndoOrRedoInProgress;

	// private methods:
	void AddActionAux(IUndoAction * puact);
	void CleanUpRedoActions(bool fForce);
	void CleanUpEmptyTasks();
	void EmptyStack();
	void CleanUpMarks();

	HRESULT CallUndo(UndoResult * pures, bool & fRedoable, bool fForDataChange);
	HRESULT CallRedo(UndoResult * pures, bool fForDataChange, int iSeqToRedo, int iLastRedoAct);

};
DEFINE_COM_PTR(ActionHandler);


#endif // ActionHandler_INCLUDED
