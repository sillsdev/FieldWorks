#!/bin/sh

grep -rI GetColValue "$@" |
	sed '
		s/^\([^:]*\):.*\(GetColValue\)/\1	\2/
		'
