####################################################
#
#     Perl Persistent Interpreter
#         Core Script File
#
#                     Copyright (C) PixiGreg
#
####################################################
#
#    >>> Be careful modifying this file ! <<<
#
####################################################
#         include all needed libs here






####################################################


BEGIN
{
	# so you can put all .pm file under lib dir or site/lib dir
	push @INC, 'lib', 'site/lib';

	use strict;
	use warnings 'all';
	use Symbol qw(delete_package);
	use PerlIO::scalar;
	#use Win32;

	our %Cache;

	our $_stream_stderr;
	our $_stream_stdout;

	our $testing = 0;

	close STDOUT;
	close STDERR;
	open(STDERR, '+>:perlio:scalar', \$_stream_stderr) or die "Can't open STDERR: $!";
	open(STDOUT, '+>:perlio:scalar', \$_stream_stdout) or die "Can't open STDOUT: $!";
	select STDERR; $| = 1;
	select STDOUT; $| = 1;
}

END
{
	close STDOUT;
	close STDERR;
}

sub empty_outputs
{
	seek STDOUT, 0, 0;
	seek STDERR, 0, 0;
}

sub empty_stderr
{
	seek STDERR, 0, 0;
}

sub empty_stdout
{
	seek STDOUT, 0, 0;
}

sub get_stderr
{
	my $stderr;
	my $pos = tell STDERR;
	seek STDERR, 0, 0;
	read STDERR, $stderr, $pos;
	seek STDERR, $pos, 0;
	return $stderr;
}

sub get_stdout
{
	my $stdout;
	my $pos = tell STDOUT;
	seek STDOUT, 0, 0;
	read STDOUT, $stdout, $pos;
	seek STDOUT, $pos, 0;
	return $stdout;
}

sub compile
{
	my($packagename, $script, $empty) = @_;

	$empty and &empty_outputs;

	# clean if a script is already compiled with same name
	defined $Cache{$packagename} and
		delete_package($packagename), undef $Cache{$packagename};

	# wrap the code into a subroutine inside our unique package
	my $eval = qq{package $packagename; sub handler { $script; }};
	{
		# hide our variables within this block
		my($packagename, $script);
		eval $eval;
		if ($@)
		{
			warn $@;
			return;
		}
	}

	$Cache{$packagename} = 1;
}

sub execute
{
	my($packagename, $empty) = @_;

	$empty and &empty_outputs;

	if (defined $Cache{$packagename})
	{
		eval { $packagename->handler(); };
		warn $@ if ($@);
	}
}

sub clean
{
	my $packagename = shift;
	defined $Cache{$packagename} and
		delete_package($packagename), undef $Cache{$packagename};
}

sub test
{
	1;
}

1;
