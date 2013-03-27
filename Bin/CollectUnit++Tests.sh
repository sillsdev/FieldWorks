#!/bin/sh
# Usage: CollectUnit++Tests.sh <module> <filename1> <filename2>...<outputfile>


BUILD_ROOT=$(dirname $0)

mono --debug $BUILD_ROOT/CollectCppUnitTests.exe $*
