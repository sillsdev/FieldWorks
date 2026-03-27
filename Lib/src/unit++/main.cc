// Copyright (C) 2001 Claus Dræby
// Terms of use are in the file COPYING
#include "main.h"
#include <algorithm>
#include <signal.h>
#include <stdio.h>
#include <stdlib.h>
#if defined(WIN32) || defined(WIN64)
#define WINDOWS_LEAN_AND_MEAN
#include <Windows.h>
#include <crtdbg.h>
#endif
using namespace std;
using namespace unitpp;

#if defined(WIN32) || defined(WIN64)
namespace
{
	void TerminateOnSigAbrt(int)
	{
		_exit(3);
	}

	typedef HRESULT (WINAPI * PfnWerGetFlags)(HANDLE, PDWORD);
	typedef HRESULT (WINAPI * PfnWerSetFlags)(DWORD);

	const DWORD kWerFaultReportingNoUi = 0x00000004;
	const DWORD kWerFaultReportingAlwaysShowUi = 0x00000010;

	void ConfigureWindowsErrorReportingUi()
	{
		DWORD errorMode = GetErrorMode();
		errorMode |= SEM_FAILCRITICALERRORS;
		errorMode |= SEM_NOGPFAULTERRORBOX;
		errorMode |= SEM_NOOPENFILEERRORBOX;
		SetErrorMode(errorMode);

		HMODULE hWer = LoadLibraryA("wer.dll");
		if (!hWer)
			return;

		PfnWerGetFlags pfnWerGetFlags = reinterpret_cast<PfnWerGetFlags>(
			GetProcAddress(hWer, "WerGetFlags")
		);
		PfnWerSetFlags pfnWerSetFlags = reinterpret_cast<PfnWerSetFlags>(
			GetProcAddress(hWer, "WerSetFlags")
		);

		if (pfnWerSetFlags)
		{
			DWORD flags = 0;
			if (pfnWerGetFlags)
				pfnWerGetFlags(GetCurrentProcess(), &flags);

			flags |= kWerFaultReportingNoUi;
			flags &= ~kWerFaultReportingAlwaysShowUi;
			pfnWerSetFlags(flags);
		}

		FreeLibrary(hWer);
	}

	void ConfigureCrtReportUi()
	{
		_set_error_mode(_OUT_TO_STDERR);

		_CrtSetReportMode(_CRT_WARN, _CRTDBG_MODE_FILE);
		_CrtSetReportFile(_CRT_WARN, _CRTDBG_FILE_STDERR);
		_CrtSetReportMode(_CRT_ERROR, _CRTDBG_MODE_FILE);
		_CrtSetReportFile(_CRT_ERROR, _CRTDBG_FILE_STDERR);
		_CrtSetReportMode(_CRT_ASSERT, _CRTDBG_MODE_FILE);
		_CrtSetReportFile(_CRT_ASSERT, _CRTDBG_FILE_STDERR);

		_set_abort_behavior(0, _WRITE_ABORT_MSG | _CALL_REPORTFAULT);
	}

	void SuppressInteractiveCrashUi()
	{
		ConfigureWindowsErrorReportingUi();
		ConfigureCrtReportUi();
	}
}
#endif

bool unitpp::verbose = false;
int unitpp::verbose_lvl = 0;
bool unitpp::line_fmt = false;
bool unitpp::pedantic = false;
bool unitpp::exit_on_error = false;

test_runner* runner = 0;

test_runner::~test_runner()
{
}

void unitpp::set_tester(test_runner* tr)
{
	runner = tr;
}

int main(int argc, const char* argv[])
{
#if defined(WIN32) || defined(WIN64)
	SuppressInteractiveCrashUi();
#endif
    printf("DEBUG: unit++ main start\n"); fflush(stdout);
	options().add("v", new options_utils::opt_flag(verbose));
	options().alias("verbose", "v");
	options().add("V", new options_utils::opt_int(verbose_lvl, 1));
	options().alias("verbose-lvl", "V");
	options().add("l", new options_utils::opt_flag(line_fmt));
	options().alias("line", "l");
	options().add("p", new options_utils::opt_flag(pedantic));
	options().alias("pedantic", "p");
	options().add("e", new options_utils::opt_flag(exit_on_error));
	options().alias("exit_on_error", "e");
	if (!options().parse(argc, argv))
		options().usage();
	plain_runner plain;
	if (!runner)
		runner = &plain;

	int retval = 0;

	try {
    	printf("DEBUG: Calling GlobalSetup\n"); fflush(stdout);
		GlobalSetup(verbose);
		printf("DEBUG: Returned from GlobalSetup\n"); fflush(stdout);
 	}
	catch (const std::exception& e) {
		fprintf(stderr, "GlobalSetup threw std::exception: %s\n", e.what());
		fflush(stderr);
		return 1;
	}
	catch (...) {
		fprintf(stderr, "GlobalSetup threw an unknown exception\n");
		fflush(stderr);
		return 1;
	}

	retval = runner->run_tests(argc, argv) ? 0 : 1;
	signal(SIGABRT, TerminateOnSigAbrt);

	try {
    	printf("DEBUG: Calling GlobalTeardown\n"); fflush(stdout);
		GlobalTeardown();
 	}
	catch (const std::exception& e) {
		fprintf(stderr, "GlobalTeardown threw std::exception: %s\n", e.what());
		fflush(stderr);
		retval = 1;
	}
	catch (...) {
		fprintf(stderr, "GlobalTeardown threw an unknown exception\n");
		fflush(stderr);
		retval = 1;
	}
	printf("DEBUG: unit++ main end (retval=%d)\n", retval); fflush(stdout);
	return retval;
}

namespace unitpp {
options_utils::optmap& options()
{
	static options_utils::optmap opts("[ testids... ]");
	return opts;
}

bool plain_runner::run_tests(int argc, const char** argv)
{
	bool res = true;
	if (options().n() < argc)
		for (int i = options().n(); i < argc; ++i)
			res = res && run_test(argv[i]);
	else
		res = run_test();
	return res;
}

bool plain_runner::run_test(const string& id)
{
	test* tp = suite::main().find(id);
	if (!tp) {
		return false;
	}
	return run_test(tp);
}
bool plain_runner::run_test(test* tp)
{
	if (verbose)
		verbose_lvl = 1;
	tester tst(cout, verbose_lvl, line_fmt);
	try {
		tp->visit(&tst);
	} catch(...) {
		if (!exit_on_error) // Sanity check
			throw;
	}
	tst.summary();
	res_cnt res(tst.res_tests());
	return res.n_err() == 0 && res.n_fail() == 0;
}

}
