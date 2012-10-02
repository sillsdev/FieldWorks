# sed script to convert some character entities from an &amp; form so xmllint --format will not convert them to their equivalent whitespace.
s/\&amp;\#32;/\&\#32;/g
s/\&amp;\#x20;/\&\#x20;/g
s/\&amp;\#x10;/\&\#10;/g
s/\&amp;\#x0a;/\&\#x0a;/g
