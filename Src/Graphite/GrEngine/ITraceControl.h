/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: ITraceControl.h
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:
	Defines an interface that allows initializing an engine with a flag or set of flags
	controlling trace options.
----------------------------------------------------------------------------------------------*/

#pragma once

#ifndef INTFTRACECTRL_INCLUDED
#define INTFTRACECTRL_INCLUDED

//:End Ignore

interface __declspec(uuid("B9CA9701-19F9-11d4-9273-00400543A57C")) ITraceControl;

#define IID_ITraceControl __uuidof(ITraceControl)

interface ITraceControl : public IUnknown
{
	STDMETHOD(SetTracing)(int nOptions) = 0;
	STDMETHOD(GetTracing)(int * pnOptions) = 0;
};


#endif // !INTFTRACECTRL_INCLUDED
