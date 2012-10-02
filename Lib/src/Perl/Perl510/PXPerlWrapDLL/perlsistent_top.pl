# Perl Persistent Interpreter. Core Script File.
# Copyright (C) PixiGreg.
#################################################

my $verbose = 0; # this was for testing purposes

our %Cache; # the package names cache

BEGIN
{
	# disable stdout/stderr buffering
	$|++;
	select STDERR;
	$|++;
	select STDOUT;

	use strict;
	use warnings 'all';
	use Symbol qw(delete_package);

	srand(time);

	# Run time modules added below by PXPerlWrap
	####
