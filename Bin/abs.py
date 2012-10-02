#!/usr/bin/env python

#
#	$Id$
#
#	Create an absolute path from a relative path
#
#	MarkS - 2007-08-16
#
# abs.py
# Copyright (C) 2007 SIL International
#
# This library is free software; you can redistribute it and/or
# modify it under the terms of the GNU Lesser General Public
# License as published by the Free Software Foundation; either
# version 2.1 of the License, or (at your option) any later version.
#
# This library is distributed in the hope that it will be useful,
# but WITHOUT ANY WARRANTY; without even the implied warranty of
# MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
# Lesser General Public License for more details.
#
# You should have received a copy of the GNU Lesser General Public
# License along with this library; if not, write to the Free Software
# Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
#
# http://www.gnu.org/licenses/lgpl.html
#

import os
import sys

if len(sys.argv) != 2 or "--help" == sys.argv[1]:
	print "Usage:", sys.argv[0], "relative_path"
	sys.exit(1)

print os.path.abspath(sys.argv[1])
