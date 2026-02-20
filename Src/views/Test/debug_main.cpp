// Debug version of unit++ main.cc
#include "../../../Lib/src/unit++/main.h"
#include <algorithm>
#include <iostream>
#include <stdio.h>

using namespace std;
using namespace unitpp;

// Redefine unitpp globals
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
    printf("DEBUG: main start\n"); fflush(stdout);
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

    printf("DEBUG: Calling GlobalSetup\n"); fflush(stdout);
	GlobalSetup(verbose);
    printf("DEBUG: Returned from GlobalSetup\n"); fflush(stdout);

	int retval = runner->run_tests(argc, argv) ? 0 : 1;

    printf("DEBUG: Calling GlobalTeardown\n"); fflush(stdout);
	GlobalTeardown();
    printf("DEBUG: main end\n"); fflush(stdout);
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
    printf("DEBUG: run_tests start\n"); fflush(stdout);
	bool res = true;
	if (options().n() < argc)
		for (int i = options().n(); i < argc; ++i)
			res = res && run_test(argv[i]);
	else
		res = run_test();
    printf("DEBUG: run_tests end\n"); fflush(stdout);
	return res;
}

bool plain_runner::run_test(const string& id)
{
    printf("DEBUG: run_test(id='%s') start\n", id.c_str()); fflush(stdout);
	test* tp = suite::main().find(id);
	if (!tp) {
        printf("DEBUG: Test not found: %s\n", id.c_str()); fflush(stdout);
		return false;
	}
    printf("DEBUG: Found test, calling run_test(tp)\n"); fflush(stdout);
	return run_test(tp);
}
bool plain_runner::run_test(test* tp)
{
    printf("DEBUG: run_test(tp) start\n"); fflush(stdout);
	if (verbose)
		verbose_lvl = 1;
	tester tst(cout, verbose_lvl, line_fmt);
	try {
        printf("DEBUG: Calling tp->visit(&tst)\n"); fflush(stdout);
		tp->visit(&tst);
        printf("DEBUG: Returned from tp->visit(&tst)\n"); fflush(stdout);
	} catch(...) {
        printf("DEBUG: Caught exception in run_test\n"); fflush(stdout);
		if (!exit_on_error) // Sanity check
			throw;
	}
	tst.summary();
	res_cnt res(tst.res_tests());
	return res.n_err() == 0 && res.n_fail() == 0;
}

}
