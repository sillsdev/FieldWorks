// Copyright (C) 2001 Claus Dræby
// Terms of use are in the file COPYING
#ifndef __TEST_FW_H_
#define __TEST_FW_H_
#include <vector>
#include <string>
#include <map>
#include <iostream>
#include <sstream>
#include "optmap.h"
/**
 * The unitpp name space holds all the stuff needed to use the unit++ testing
 * framework.
 *
 * The normal way to make a test is like this:
 *
\begin{verbatim}
#include<unit++/unit++.h>
using namespace unitpp;
// use anonymous namespace so all test classes can be named Test
namespace {
class Test : public suite {
	void test1()
	{
		// do test stuff
		assert_true("message", exp1); // exp1 should be true
		assert_eq("another msg", 123456, exp2); // exp2 should be 123456
		// ...
	}
	void test2()
	{
		// do something that provokes exception out_of_range
	}
	void test_xyzzy()
	{
		assert_false("message", expr1); // an expression expected to yield false
	}
	void test_except2()
	{
		// do something expected to generate an out_of_range exception
	}
public:
	Test() : suite("appropriate name for test suite")
	{
		// add the suite to the global test suite
		suite::main().add("id", this);
		// any setup you need
		add("id1", testcase(this, "Test 1", &Test::test1));
		// make a testcase from the method
		testcase tc(this, "Test 2", &Test::test2);
		// add a testcase that expects the exception
		add("id2", exception_case<out_of_range>(tc));
		// A macro for adding a test case:
		//   requires class to be named Test and test case to be named test_<id>
		//   hence this adds the function Test::test_xyzzy under id xyzzy:
		member_test(xyzzy);
		// Macro for adding exception test cases:
		//   requirements as above
		exception_member_test(out_of_range, except2);
	}
} * theTest = new Test();  // by new, since testcase claims ownership
}
\end{verbatim}
 *
 * In order to make an executable test, simply link the above code against
 * libunit++, something like
 *
 * #g++ -o test++ mytest.cc -L <location of libunit++> -lunit++#
 *
 * This will generate a test called #test++# and the standard behaviour for a
 * test. Note that most shells have #test# defined as a shell builtin which
 * makes it a moderately bad name for a program, since it is rather hard to
 * get executed, hence #test++#.
 * @see main
 */
namespace unitpp {

extern int verbose_lvl;

class visitor;
/**
 * The heart of a test system: A test. The test is meant as a base class for
 * the tests that a client want performed. This means that all tests are to
 * be pointers dynamically allocated. However, the test system takes
 * responsibilities for freeing them again.
 *
 * The function call overload mechanism is used for the executable part of
 * the test.
 */
class test {
	std::string name_;
	char const* file_;
	unsigned int line_;
public:
	/// A test just needs a name
	test(const std::string& name, char const* f = "", unsigned int l = 0) : name_(name), file_(f), line_(l) {}
	virtual ~test() {}
	/// The execution of the test
	virtual void operator()() = 0;
	virtual void visit(visitor*);
	virtual test* get_child(const std::string&) { return 0; }
	std::string name() const { return name_; }
	char const* file() const { return file_; }
	unsigned int line() const { return line_; }
	/// Initialize before executing a test.  The default method does nothing.
	virtual void Setup() {}
	/// Cleanup after executing a test.  The default method does nothing.
	virtual void Teardown() {}
	/// Initialize before running a test suite.  The default method does nothing.
	virtual void SuiteSetup() {}
	/// Cleanup after running a test suite.  The default method does nothing.
	virtual void SuiteTeardown() {}
	virtual bool IsVisitingSuite() { return false; }
	// this calls Teardown() and possibly SuiteTeardown().
	virtual void cleanup() {}
};

/**
 * A test that is implemented by a member function.
 */
template<typename C>
class test_mfun : public test {
public:
	typedef void (C::*mfp)();
	/// An object, a name, and a pointer to a member function.
	test_mfun(C* par, const std::string& name, mfp fp, char const* file, unsigned int line)
		: test(name, file, line), par(par), fp(fp)
	{}
	/// Executed by invoking the function in the object.
	virtual void operator()()
	{
		// In case we're running an isolated test out of a suite.
		if (!par->IsVisitingSuite())
			par->SuiteSetup();
		par->Setup();
		try
		{
			(par->*fp)();
		}
		catch (...)
		{
			cleanup();
			throw;			// pass on the exception
		}
		cleanup();
	}
	// This is a separate method for use by exception_test::operator()
	virtual void cleanup()
	{
		par->Teardown();
		if (!par->IsVisitingSuite())
			par->SuiteTeardown();
	}
private:
	C* par;
	mfp fp;
};

/**
 * A ref counted reference to a test. This is what test suites are composed
 * of, and what ensures destruction.
 */
class testcase {
	std::size_t* cnt;
	test* tst;
	void dec_cnt();
public:
	/// Simply wrap -- and own -- a test.
	testcase(test* t);
	/// Keep the ref count
	testcase(const testcase& tr);
	/**
	 * Make a testcase from a class and a member function.
	 *
	 * The normal usage is inside some test suite class Test:
	 *
	 * #add("id", testcase(this, "Testing this and that", &Test::test))#
	 *
	 * to make a test that invokes the test method on the instance of the
	 * suite class.
	 * \Ref{test_mfun}
	 */
	template<typename C>
		testcase(C* par, const std::string& name, typename test_mfun<C>::mfp fp, char const* file = "", unsigned int line = 0)
		: cnt(new size_t(1)), tst(new test_mfun<C>(par, name, fp, file, line))
		{ }
	~testcase();
	/// Assignment that maintains reference count.
	testcase& operator=(const testcase&);
	void visit(visitor* vp) const { tst->visit(vp); }
	operator test& () { return *tst; }
	operator const test& () const { return *tst; }
	test * Test() { return tst; }
};

/**
 * A wrapper class for the testcase class that succedes if the correct
 * exception is generated.
 */
template<typename E>
class exception_test : public test {
public:
	/**
	 * The constructor needs a testcase to wrap. This exception_test will
	 * fail unless the wrapped testcase generates the exception.
	 *
	 * The name of the exception_test is copied from the wrapped test.
	 */
	exception_test(const testcase& tc)
		: test(static_cast<const test&>(tc).name(),
			   static_cast<const test&>(tc).file(),
			   static_cast<const test&>(tc).line())
		, tc(tc) {}
	/// Runs the wrapped test, and fails unless the correct exception is thrown.
	virtual void operator()();
private:
	testcase tc;
};
/**
 * Generate a testcase that expects a specific exception from the testcase it
 * wraps. It can be used something like
 *
 * #testcase tc(this, "Test name", &Test::test);#
 *
 * #add("ex", exception_case<out_of_range>(tc));#
 *
 * The name of the exception_case is copied from the wrapped testcase, and
 * the exception_case will execute the tc test case and report a failure
 * unless the #out_of_range# exception is generated.
 */
template<typename E>
testcase exception_case(const testcase& tc)
{
	return testcase(new exception_test<E>(tc));
}

/**
 * Splits the string by char c. Each c will generate a new element in the
 * vector, including leading and trailing c.
 */
extern std::vector<std::string> vectorize(const std::string& str, char c);

/**
 * A suite is a test that happens to be a collection of tests. This is an
 * implementation of the Composite pattern.
 */
class suite : public test {
	std::vector<std::string> ids;
	std::vector<testcase> tests;
public:
	/// Make an empty test suite.
	suite(const std::string& name) : test(name) { fVisitingSuite = false; }
	virtual ~suite() {};
	/// Add a testcase to the suite.
	void add(const std::string& id, const testcase& t);
	/**
	 * Get a child with the specified id.
	 * @return 0 if not found.
	 */
	virtual test* get_child(const std::string& id);
	/// An empty implementation.
	virtual void operator()() {}
	/// Allow a visitor to visit a suite node of the test tree.
	void visit(visitor*);
	/// Get a reference to the main test suite that the main program will run.
	static suite& main();
	// Splits the string by dots, and use each id to find a suite or test.
	test* find(const std::string& id);
	virtual bool IsVisitingSuite() { return fVisitingSuite; }
private:
	bool fVisitingSuite;
};

/**
 * The visitor class is a base class for classes that wants to participate in
 * the visitor pattern with the test hierarchi.
 *
 * This is a slightly extended visitor pattern implementation, intended for
 * collaboration with the Composite pattern. The aggregate node (here the
 * suite node) is visited twice, before and after the children are visited.
 * This allows different algorithms to be implemented.
 */
class visitor {
public:
	virtual ~visitor() {}
	/// Visit a test case, that is not a suite.
	virtual void visit(test&) = 0;
	/// Visit a suite node before the children are visited.
	virtual void visit(suite&) {};
	/**
	 * Visit a suite after the children are visited
	 */
	virtual void visit(suite&, int dummy) = 0; // post childs
};

/// The basic for all failed assert statements.
class assertion_error : public std::exception
{
	char const* file_;
	unsigned int line_;
	std::string msg;
public:
	/// An assertion error with the given message.
	assertion_error(char const* file, unsigned int line, const std::string& msg)
	: file_(file), line_(line), msg(msg) {}
	///
	std::string message() const { return msg; }
	virtual ~assertion_error() throw () {}
	/**
	 * The virtual method used for operator<<.
	 */
	virtual void out(std::ostream& os) const;
	char const* file() { return file_; }
	unsigned int line() { return line_; }
};
/**
 * This exception represents a failed comparison between two values.
 * Both the expected and the actually value are kept.
 */
class assert_value_error : public assertion_error
{
	std::string exp;
	std::string got;
public:
	/// Construct by message, expected and gotten.
	assert_value_error(const char* f, unsigned int l, const std::string& msg, std::string const& exp, std::string const& got)
	: assertion_error(f, l, msg), exp(exp), got(got)
	{
	}
	virtual ~assert_value_error() throw () {}
	virtual void out(std::ostream& os) const
	{
		os << message() << " [expected: `" << exp << "' got: `" << got << "']";
	}
};
/// The test was not succesful.
inline void assert_fail_f(const char* f, unsigned int l, const std::string& msg)
{
	throw assertion_error(f, l, msg);
}
template<typename E>
void exception_test<E>::operator()()
{
	try {
		(static_cast<test&>(tc))();
		assert_fail_f(file(), line(), "unexpected lack of exception");
	} catch (E& ) {
		// fine!
	}
}
/// Assert that the assertion is true, that is fail #if (!assertion) ...#
template<class A> inline void assert_true_f(char const* f, unsigned int l, const std::string& msg, A assertion)
{
	if (verbose_lvl > 2)
		std::cerr << "assert_true: " << f << ':' << l << std::endl;
	if (!assertion)
		throw assertion_error(f, l, msg);
}
/// Assert that the assertion is false, that is fail #if (assertion) ...#
template<class A> inline void assert_false_f(char const* f, unsigned int l, const std::string& msg, A assertion)
{
	if (verbose_lvl > 2)
		std::cerr << "assert_false: " << f << ':' << l << std::endl;
	if (assertion)
		throw assertion_error(f, l, msg);
}
#define member_testcase(func) testcase(this, #func, &Test::test_##func, __FILE__, __LINE__)
#define assert_true(m, a) assert_true_f(__FILE__, __LINE__, m, a)
#define assert_false(m, a) assert_false_f(__FILE__, __LINE__, m, a)
#define assert_fail(m) assert_fail_f(__FILE__, __LINE__, m)
#define assert_eq(m, e, g) assert_eq_f(__FILE__, __LINE__, m, e, g)
#define member_test(func) add(#func, member_testcase(func))
#define exception_member_test(excep, func) add(#func, exception_case<excep>(member_testcase(func)))

/// Assert that the two arguments are equal in the #==# sense.
template<class T1, class T2>
	inline void assert_eq_f(char const* f, unsigned int l, const std::string& msg, T1 exp, T2 got)
{
	if (verbose_lvl > 2)
		std::cerr << "assert_eq: " << f << ':' << l << std::endl;
	if (!(exp == got)) {
		std::ostringstream oexp; oexp << exp;
		std::ostringstream ogot; ogot << got;
		throw assert_value_error(f, l, msg, oexp.str(), ogot.str());
	}
}
/*
 * Put an assertion error to a stream, using the out method. The out method
 * is virtual.
 */
inline std::ostream& operator<<(std::ostream& os, const unitpp::assertion_error& a)
{
	a.out(os);
	return os;
}

/**
 * The singleton instance of the option handler of main.
 *
 * This allows a test to add its own flags to the resulting test program, in
 * the following way.
 *
 * #bool x_flg = false;#
 * #unitpp::options().add("x", new options_utils::opt_flag(x_flg));#
 *
 * If a -x is now given to the resulting test it will set the #x_flg#
 * variable;
 */
options_utils::optmap& options();

/**
 * An instance of this class hooks the GUI code into the test executable.
 * Hence, make a global variable of class gui_hook to allow the -g option to
 * a test.
 *
 * If the library is compiled without GUI support, it is still legal to
 * create an instance of gui_hook, but it will not add the -g option.
 */
class gui_hook {
public:
	gui_hook();
};

/**
 *  This function performs global initialization before any tests are run.
 *  It is called from main().  Programmers can supply their own implementation,
 *  or rely on the default null implementation in the library.
 */
void GlobalSetup(bool verbose);
/**
 *  This function performs global cleanup after all tests have been run.
 *  It is called from main().  Programmers can supply their own implementation,
 *  or rely on the default null implementation in the library.
 */
void GlobalTeardown();
}

#endif
