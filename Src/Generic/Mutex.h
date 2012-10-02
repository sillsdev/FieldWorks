/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: Mutex.h
Responsibility: Shon Katzenberger
Last reviewed:

	Wrapper for a critical section.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef Mutex_H
#define Mutex_H 1

/*----------------------------------------------------------------------------------------------
	Mutex - Wrapper for a Win32 critical section.

	Care must be taken not to Enter or Leave the Mutex before the constructor is envoked or
	after the destructor is invoked. If you create a global Mutex and have global objects
	reference the global Mutex, make sure you don't reference the Mutex during init or exit
	time.

	Generally you should create a static MutexLock to Enter and Leave the Mutex. This will
	ensure that Enter and Leave are balanced appropriately.

	Hungarian: mutx.
----------------------------------------------------------------------------------------------*/
class Mutex
{
public:
	Mutex(void)
	{
		InitializeCriticalSection(&m_crit);
	}

	~Mutex(void)
	{
		DeleteCriticalSection(&m_crit);
	}

	void Enter(void)
	{
		EnterCriticalSection(&m_crit);
	}

	void Leave(void)
	{
		LeaveCriticalSection(&m_crit);
	}

protected:
	CRITICAL_SECTION m_crit;
};


/*----------------------------------------------------------------------------------------------
	MutexLock - Class that automatically calls Enter and Leave on a mutex when the object
	is created and destroyed.
	Hungarian: mtxl.
----------------------------------------------------------------------------------------------*/
class MutexLock
{
public:
	MutexLock(Mutex & mutx)
	{
		m_pmutx = &mutx;
		m_pmutx->Enter();
	}
	~MutexLock(void)
	{
		m_pmutx->Leave();
	}

protected:
	Mutex * m_pmutx;
};


/*----------------------------------------------------------------------------------------------
	Macro to create a MutexLock.
----------------------------------------------------------------------------------------------*/
#define LockMutex(mutx) MutexLock _mtxl_##__LINE__(mutx)

#endif // !Mutex_H
