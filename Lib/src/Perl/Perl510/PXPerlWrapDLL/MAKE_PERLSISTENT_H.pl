$/ = 65536;

print "Creating perlsistent header file... ";
open OUT, ">perlsistent.h" or die $!;
print OUT "// Generated on ", scalar gmtime, "\n\nstatic const unsigned char perlsistent_top[] = {\n\t";
open IN, "perlsistent_top.pl" or die $!;
binmode IN;
my $buf = <IN>;
my @chars = split(//, $buf);
push @chars, chr(0);
my $i = 0;
my $count = 0;
for my $char (@chars)
{
  $count++;
  printf OUT "0x%02X, ", ord($char);
  $i++;
  $i == 16 and $i = 0, print OUT "\n\t";

}
print OUT "\n};\n\nstatic const size_t perlsistent_top_size = $count;\n\n\n";
close IN;


print OUT "static const unsigned char perlsistent_bot[] = {\n\t";
open IN, "perlsistent_bot.pl" or die $!;
binmode IN;
$buf = <IN>;
@chars = split(//, $buf);
push @chars, chr(0);
$i = 0;
$count = 0;
for my $char (@chars)
{
  $count++;
  printf OUT "0x%02X, ", ord($char);
  $i++;
  $i == 16 and $i = 0, print OUT "\n\t";

}
print OUT "\n};\n\nstatic const size_t perlsistent_bot_size = $count;\n";
close IN;

close OUT;
print "ok\n";
