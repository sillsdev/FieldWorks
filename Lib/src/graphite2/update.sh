#!/bin/bash

# Script used to update the Graphite2 library in the FW source tree
# It expects to find a checkout of the graphite2 tree in a directory "graphitedev"
# alongside the fwrepo directory that is to be updated.
# This script should be run from the "graphite2" directory.
# Expect error messages from the copy commands if this is not found!

# copy the source and headers
cp -R ../../../../../graphitedev/src/* src
cp -R ../../../../../graphitedev/include/* include

# create objects list for Windows
sed -n -e 's|\$(_NS)_SOURCES =|GR2_OBJECTS =|p' -e 's|\$(\$(_NS)_BASE)/src/\([a-zA-Z_]*\)\.cpp|\$(INT_DIR)\\\1.obj|p' -e 's|\$(\$(_NS)_BASE)/src/\$(\$(_NS)_MACHINE)\(.*\)\.cpp|\$(INT_DIR)\\call\1.obj|p' src/files.mk > src/files.mk.win

# record the upstream changeset that was used
CHANGESET=$(cd ../../../../../graphitedev/ && hg log | head -n 1 | cut -d : -f 1,3 | sed -e 's/:/ /')
echo "This directory contains the Graphite2 library from http://hg.palaso.org/graphitedev" > README
echo "Current version derived from upstream" $CHANGESET >> README
echo "See update.sh for update procedure." >> README

# summarize what's been touched
echo Updated to $CHANGESET.
echo Here is what changed in the graphite2 directory:
echo

git status -s .
