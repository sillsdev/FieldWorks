#!/bin/sh
mkdir x
cd x
tar xzf ../fieldworks-enchant_1.6.1.orig.tar.gz
cd fieldworks-enchant-1.6.1
patch -p0 -b < "../../Enchant patch.patch"
patch -p1 -b < ../../fieldworks-enchant-1.6.1/debian/patches/Force-case-sensitive.patch
patch -p1 -b < ../../fieldworks-enchant-1.6.1/debian/patches/Fix-stat-bug.patch
