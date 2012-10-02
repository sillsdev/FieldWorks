#!/bin/sh
# Usage: CollectUnit++Tests.sh <module> <filename1> <filename2>...
MODULE=$1
shift

BUILD_ROOT=$(dirname $0)

for filename in $*
do
	gawk -v module=$MODULE -v SHORTFILENAME="$(basename $filename)" -f $BUILD_ROOT/CollectUnit++Tests.awk $filename
done
