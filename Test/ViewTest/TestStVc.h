/*----------------------------------------------------------------------------------------------
Copyright 2000, SIL International. All rights reserved.

File: TestStVc.h
Responsibility: John Thomson
Last reviewed: never

Description:
	This class provides a view constructor for a standard view of a structured text.
	As of 6/20/2000 this is an exact copy of the WorldPad structured text view constructor.
	We make a copy here so that (1) subsequent changes to WorldPad won't break the test; and
	(2) because it lives in a project main directory that we can't use for building another
	project.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef RN_TST_ST_VC_INCLUDED
#define RN_TST_ST_VC_INCLUDED 1

class TestStVc;
typedef GenSmartPtr<TestStVc> TestStVcPtr;

// Enumeration to provide fragment identifiers
enum
{
	kfrText, // The whole text
	kfrPara, // one paragraph
	kftBody, // The string that is the body of a paragraph
};

/*----------------------------------------------------------------------------------------------
	The main customizeable document view constructor class.
	Hungarian: rdw.
----------------------------------------------------------------------------------------------*/
class TestStVc : public VwBaseVc
{
public:
	typedef VwBaseVc SuperClass;

	TestStVc();
	~TestStVc();
	STDMETHOD(Display)(IVwEnv * pvwenv, HVO hvo, int frag);

protected:
};

#endif // RN_TST_ST_VC_INCLUDED
