/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 2026 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef VWRENDERTRACE_INCLUDED
#define VWRENDERTRACE_INCLUDED

#include <cstdio>
#include <windows.h>

/*----------------------------------------------------------------------------------------------
	Render trace timing infrastructure for Views engine performance analysis.

	Usage:
		#ifdef TRACING_RENDER
		VwRenderTraceTimer timer("Layout");
		// ... do layout work ...
		timer.Stop(); // or let destructor handle it
		#endif

	Output format (compatible with RenderTraceParser):
		[RENDER] Stage=Layout Duration=123.45ms Context=VwRootBox

	Enable by defining TRACING_RENDER before including this header.
	The output goes to OutputDebugString and optionally to a file.

	Hungarian: rtt (render trace timer)
----------------------------------------------------------------------------------------------*/

// Uncomment to enable render tracing globally for debug builds
// #define TRACING_RENDER

#ifdef TRACING_RENDER

// File output for render trace (optional - set to NULL to disable file output)
// Can be opened via VwRenderTrace::OpenTraceFile()
extern FILE * g_fpRenderTrace;

/*----------------------------------------------------------------------------------------------
Class: VwRenderTrace
Description: Static helper for render trace configuration and output.
----------------------------------------------------------------------------------------------*/
class VwRenderTrace
{
public:
	// Open a trace file for persistent logging
	static bool OpenTraceFile(const wchar_t * pszPath)
	{
		if (g_fpRenderTrace)
			CloseTraceFile();

		_wfopen_s(&g_fpRenderTrace, pszPath, L"a");
		return g_fpRenderTrace != NULL;
	}

	// Close the trace file
	static void CloseTraceFile()
	{
		if (g_fpRenderTrace)
		{
			fclose(g_fpRenderTrace);
			g_fpRenderTrace = NULL;
		}
	}

	// Write a trace message
	static void Write(const char * pszFormat, ...)
	{
		char szBuffer[1024];
		va_list args;
		va_start(args, pszFormat);
		vsprintf_s(szBuffer, sizeof(szBuffer), pszFormat, args);
		va_end(args);

		::OutputDebugStringA(szBuffer);
		if (g_fpRenderTrace)
		{
			fputs(szBuffer, g_fpRenderTrace);
			fflush(g_fpRenderTrace);
		}
	}

	// Check if tracing is enabled at runtime
	static bool IsEnabled()
	{
		// Could check environment variable or registry here
		return true;
	}
};

/*----------------------------------------------------------------------------------------------
Class: VwRenderTraceTimer
Description: RAII timer for automatic stage duration measurement.
Hungarian: rtt
----------------------------------------------------------------------------------------------*/
class VwRenderTraceTimer
{
public:
	VwRenderTraceTimer(const char * pszStageName, const char * pszContext = NULL)
		: m_pszStageName(pszStageName)
		, m_pszContext(pszContext)
		, m_fStopped(false)
	{
		if (VwRenderTrace::IsEnabled())
		{
			QueryPerformanceCounter(&m_liStart);
		}
	}

	~VwRenderTraceTimer()
	{
		if (!m_fStopped && VwRenderTrace::IsEnabled())
		{
			Stop();
		}
	}

	void Stop()
	{
		if (m_fStopped || !VwRenderTrace::IsEnabled())
			return;

		m_fStopped = true;

		LARGE_INTEGER liEnd, liFreq;
		QueryPerformanceCounter(&liEnd);
		QueryPerformanceFrequency(&liFreq);

		double durationMs = static_cast<double>(liEnd.QuadPart - m_liStart.QuadPart) * 1000.0
			/ static_cast<double>(liFreq.QuadPart);

		if (m_pszContext)
		{
			VwRenderTrace::Write("[RENDER] Stage=%s Duration=%.3fms Context=%s\r\n",
				m_pszStageName, durationMs, m_pszContext);
		}
		else
		{
			VwRenderTrace::Write("[RENDER] Stage=%s Duration=%.3fms\r\n",
				m_pszStageName, durationMs);
		}
	}

private:
	const char * m_pszStageName;
	const char * m_pszContext;
	LARGE_INTEGER m_liStart;
	bool m_fStopped;
};

// Global trace file pointer
FILE * g_fpRenderTrace = NULL;

// Convenience macros for conditional tracing
#define RENDER_TRACE_TIMER(name) VwRenderTraceTimer __rtt_##__LINE__(name)
#define RENDER_TRACE_TIMER_CTX(name, ctx) VwRenderTraceTimer __rtt_##__LINE__(name, ctx)
#define RENDER_TRACE_MSG(fmt, ...) VwRenderTrace::Write(fmt, ##__VA_ARGS__)

#else // !TRACING_RENDER

// No-op macros when tracing is disabled
#define RENDER_TRACE_TIMER(name) ((void)0)
#define RENDER_TRACE_TIMER_CTX(name, ctx) ((void)0)
#define RENDER_TRACE_MSG(fmt, ...) ((void)0)

#endif // TRACING_RENDER

#endif // VWRENDERTRACE_INCLUDED
