	####
}

sub parse
{
	my($packagename, $script) = @_;

	my $ret = 1;

	{
		local @_ = ();

		$verbose and print STDERR "[Perlsistent] compiling $packagename\n";

		defined $Cache{$packagename} and
			delete_package($packagename), undef $Cache{$packagename};

		my $eval = qq{package $packagename; sub handler {{ local \@_ = (); $script; }}};

		{
			my($packagename, $script);
			eval $eval;
			$@ and $ret = 0, print STDERR $@;
		}

		$Cache{$packagename} = 1;
	}

	return $ret;
}

sub run
{
	my($packagename) = @_;

	my $ret = 1;

	{
		local @_ = ();

		$verbose and print STDERR "[Perlsistent] running $packagename\n";

		if ($Cache{$packagename} == 1)
		{
			eval {
				$packagename->handler();
			};
			$@ and $ret = 0, print STDERR $@;
		}
	}

	return $ret;
}

sub clean
{
	my($packagename) = @_;
	$verbose and print STDERR "[Perlsistent] cleaning $packagename\n";
	defined $Cache{$packagename} and
		delete_package($packagename), undef $Cache{$packagename};
}

sub evalcode
{
	my($packagename, $code) = @_;

	{
		local @_ = ();

		$verbose and print STDERR "[Perlsistent] evaluating in $packagename\n";

		my $eval = qq{package $packagename; $code;};

		{
			my($packagename, $code);
			return eval $eval;
		}
	}
}

sub test
{
	return 1;
}

1;
#EOF
