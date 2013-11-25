/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: Mutex.h
Responsibility: Shon Katzenberger
Last reviewed:

	Wrapper for a mutual exclusion primitive.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef Mutex_H
#define Mutex_H 1

#ifndef WIN32
#include <pthread.h>
#include <errno.h>
#include <stdexcept>
#endif

// Use a synchronization primitive that pumps the message queue while waiting, since the
// mutex could be blocked in an STA thread. Blocking the STA thread without pumping the
// message queue can introduce deadlocks if another thread trys to marshal a call into
// the STA.
#define MSG_PUMP_MUTEX

/*----------------------------------------------------------------------------------------------
	Mutex - Wrapper for a mutual exclusion primitive for synchronization across multiple
	threads.

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
	Mutex()
	{
#ifdef WIN32
#ifdef MSG_PUMP_MUTEX
		m_mutex = CreateMutex(NULL, FALSE, NULL);
#else
		InitializeCriticalSection(&m_crit);
#endif
#else // !WIN32
		static pthread_mutex_t prototype = PTHREAD_RECURSIVE_MUTEX_INITIALIZER_NP;
		m_mutex = prototype;
#endif // !WIN32
	}

	~Mutex()
	{
#ifdef WIN32
#ifdef MSG_PUMP_MUTEX
		CloseHandle(m_mutex);
#else
		DeleteCriticalSection(&m_crit);
#endif
#else // !WIN32
		if (int errorNumber = pthread_mutex_destroy(&m_mutex))
		{
			const int bufLength = 1024;
			char errorMessage[bufLength];
			char *actualErrorMessage = strerror_r(errorNumber, errorMessage, bufLength);
			fprintf(stderr, "Mutex destroy error: %s\n", actualErrorMessage);
			throw std::logic_error(actualErrorMessage);
		}
#endif // !WIN32
	}

	void Lock()
	{
#ifdef WIN32
#ifdef MSG_PUMP_MUTEX
		DWORD index;
		CoWaitForMultipleHandles(0, INFINITE, 1, &m_mutex, &index);
#else
		EnterCriticalSection(&m_crit);
#endif
#else // !WIN32
		if (int errorNumber = pthread_mutex_lock(&m_mutex))
		{
			const int bufLength = 1024;
			char errorMessage[bufLength];
			char *actualErrorMessage = strerror_r(errorNumber, errorMessage, bufLength);
			fprintf(stderr, "Mutex lock error: %s\n", actualErrorMessage);
			if (EDEADLK == errorNumber)
				throw std::logic_error("EXCEPTION_POSSIBLE_DEADLOCK");
			else
				throw std::logic_error(actualErrorMessage);
		}
#endif // !WIN32
	}

	bool TryLock()
	{
#ifdef WIN32
#ifdef MSG_PUMP_MUTEX
		DWORD index;
		return CoWaitForMultipleHandles(0, 0, 1, &m_mutex, &index) == S_OK;
#else
		return TryEnterCriticalSection(&m_crit);
#endif
#else // !WIN32
		return (pthread_mutex_trylock(&m_mutex) == 0);
#endif
	}

	void Unlock()
	{
#ifdef WIN32
#ifdef MSG_PUMP_MUTEX
		ReleaseMutex(m_mutex);
#else
		LeaveCriticalSection(&m_crit);
#endif
#else // !WIN32
		if (int errorNumber = pthread_mutex_unlock(&m_mutex))
		{
			const int bufLength = 1024;
			char errorMessage[bufLength];
			char *actualErrorMessage = strerror_r(errorNumber, errorMessage, bufLength);
			fprintf(stderr, "Mutex unlock error: %s\n", actualErrorMessage);
			throw std::logic_error(actualErrorMessage);
		}
#endif // !WIN32
	}

protected:
#ifdef WIN32
#ifdef MSG_PUMP_MUTEX
	HANDLE m_mutex;
#else
	CRITICAL_SECTION m_crit;
#endif
#else // !WIN32
	pthread_mutex_t m_mutex;
#endif // !WIN32
};


/*----------------------------------------------------------------------------------------------
	MutexLock - Class that automatically calls Enter and Leave on a mutex when the object
	is created and destroyed.
	Hungarian: mtxl.
----------------------------------------------------------------------------------------------*/
class MutexLock
{
public:
	MutexLock(Mutex& mutx, bool tryLock = false) : m_mutx(mutx)
	{
		if (tryLock)
		{
			m_run = m_mutx.TryLock();
		}
		else
		{
			m_mutx.Lock();
			m_run = true;
		}
	}

	bool Run()
	{
		return m_run;
	}

	void Stop()
	{
		m_run = false;
	}

	~MutexLock()
	{
		m_mutx.Unlock();
	}

protected:
	Mutex& m_mutx;
	bool m_run;
};


/*----------------------------------------------------------------------------------------------
	Macro to create a MutexLock.
----------------------------------------------------------------------------------------------*/
#define LOCK(mutx) for (MutexLock lock(mutx); lock.Run(); lock.Stop())
#define TRY_LOCK(mutx) for (MutexLock lock(mutx, true); lock.Run(); lock.Stop())

#endif // !Mutex_H
