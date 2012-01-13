#!/bin/bash
###################################################

#
# Headless FieldWorks Build Script
#
# Original author: MarkS 2009-08
#

echo BUILD SCRIPT BEING USED:
cat "$0"
# Note that (false) does not quit the shell with 'set -e'. So    (false) || false    is needed.

# Check for required programs
REQUIRED_PROGRAMS="Xvfb Xephyr metacity"
for program in $REQUIRED_PROGRAMS
do
  if ! { which $program > /dev/null; }; then
	echo Error: FieldWorks build requires missing program \"$program\" to be installed.
	exit 1
  fi
done

# Get ready to build
. environ
export AssertUiEnabled=false  # bypass assert message boxes for headless build
# Set environment variable to allow building on CI build agents without having to create
# /var/lib/fieldworks directory with correct permissions.
export FW_CommonAppData=$WORKSPACE/var/lib/fieldworks

# start ibus daemon just in case it's not yet running
/usr/bin/ibus-daemon --xim -d

# Set up a headless X server to run the graphical unit tests inside
# Avoid DISPLAY collisions with concurrent builds
let rand1=$RANDOM%50+20
let rand2=$RANDOM%50+20
# Run the tests inside Xephyr, and run Xephyr inside Xvfb.
export Xvfb_DISPLAY=:$rand1
while [ -e /tmp/.X${Xvfb_DISPLAY}-lock ]; do  # Don't use an X display already in use
  export Xvfb_DISPLAY=:$rand1
done
Xvfb -reset -terminate -screen 0 1280x1024x24 $Xvfb_DISPLAY & export Xvfb_PID=$!; sleep 3s
export Xephyr_DISPLAY=:$rand2
while [ -e /tmp/.X${Xephyr_DISPLAY}-lock ]; do  # Don't use an X display already in use
  export Xephyr_DISPLAY=:$rand2
done
DISPLAY=$Xvfb_DISPLAY Xephyr $Xephyr_DISPLAY -reset -terminate -screen 1280x1024 & export Xephyr_PID=$!; sleep 3s
export DISPLAY=$Xephyr_DISPLAY; metacity & sleep 3s
echo FieldWorks build using DISPLAY of $DISPLAY
# Upon exit, kill off Xvfb and Xephyr. This may not be necessary if Hudson cleans up whatever we start.
trap "{ echo Killing off Xvfb \(pid $Xvfb_PID\) and Xephyr \(pid $Xephyr_PID\) ...; kill $Xephyr_PID || (sleep 10s; kill -9 $Xephyr_PID); sleep 3s; kill $Xvfb_PID || (sleep 10s; kill -9 $Xvfb_PID); }" EXIT $EXIT_STATUS

# Build
(cd Bld && ../Bin/nant/bin/nant $1 remakefw-jenkins)
EXIT_STATUS=$?
echo "FieldWorks build finished - exit status: $EXIT_STATUS"
#(cd Bld && ../Bin/nant/bin/nant linux-smoketest)
exit $EXIT_STATUS
###################################################
