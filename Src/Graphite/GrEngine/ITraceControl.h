/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2015 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

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
