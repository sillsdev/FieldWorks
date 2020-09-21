#!/usr/bin/perl

# This script is used to download the dependencies to build libpalaso, liblcm and chorus
# libraries locally for debugging FLEx. It find the locations to these local repos by parsing
# the LibraryDevelopment.properties file.

# Run: perl DownloadLibraryDependencies.pl
# Note: you will need to install XML::Simple to run this script.
# XML::Simple is included in Strawberry Perl on windows (http://strawberryperl.com/)
# XML::Simple installation can be done on linux using the CPAN shell:
#   shell> perl -MCPAN -e shell
#   cpan> install XML::Simple

# After this script terminates, do the following to build libraries locally
#   Windows: build /t:LocalLibrary /p:Platform=<x86|x64>
#   Linux: msbuild /t:LocalLibrary
use XML::Simple;

my $osName = "$^O";
my $extension = "win.sh";
if ($osName eq "linux") {
	$extension = "mono.sh";
}
my $xml = new XML::Simple;
my $data = $xml->XMLin("LibraryDevelopment.properties");
my $palasoArtifactsDir = "$data->{PropertyGroup}->{'PalasoArtifactsDir'}->{content}";
my $chorusArtifactsDir = "$data->{PropertyGroup}->{'ChorusArtifactsDir'}->{content}";
my $useLocal = "$data->{PropertyGroup}->{'UseLocalLibraries'}";
print "Downloading libpalaso dependencies...\n";

# Look for "output" in the path and take everything before it
if (not $palasoArtifactsDir =~ m/output/) {
	die "Error: Expected to find 'output' in the libpalaso artifacts path. Check the LibraryDevelopment.properties file.\n";
}
my $palasoBase = "$`";
# Replace backslashes with forward slashes
$palasoBase =~ s/\\/\//g;
system($palasoBase . "build/buildupdate.$extension");
print "Finished downloading libpalaso dependencies.\n";
print "Downloading chorus dependencies...\n";
if (not $chorusArtifactsDir =~ m/output/) {
	die "Error: Expected to find 'output' in the chorus artifacts path. Check the LibraryDevelopment.properties file.\n";
}
my $chorusBase = "$`";
$chorusBase =~ s/\\/\//g;
system($chorusBase . "build/buildupdate.$extension");
print "Finished downloading chorus dependencies.\n";
if ($useLocal ne "Y") {
	print "\nWARNING: FieldWorks is not set to build using local libraries. Edit the LibraryDevelopment.properties file to change this.\n";
}