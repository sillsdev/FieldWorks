#if !WIN32
#include <iostream>

// This file gets included from several link_check targets on Linux.
// The purpose is to check that there are going to be no undefined symbols in the .so
// Otherwise you don't find out until you try to load the .so into the application when
// creating a COM object
int main (int argc, char const* argv[])
{
	std::cout << "Dummy main called\n";
}
#endif
