# sed script to convert some character entities to an &amp; form so xmllint --format will not convert them to their equivalent whitespace.
s/\&\#32;/\&amp;\#32;/g
s/\&\#x20;/\&amp;\#x20;/g
s/\&\#10;/\&amp;\#x10;/g
s/\&\#x0a;/\&amp;\#x0a;/g


# s/\&\#\([0-9][0-9]+\);/\&amp;\#\1;/g  does not match (and I don't know why)
