copy %1 %1.bak
%2\sed -f %2\XmlFormatter\MapCharRefsBefore.sed %1 > %1.Before
%2\XmlFormatter\xmllint --format %1.Before > %1.After
%2\sed -f %2\XmlFormatter\MapCharRefsAfter.sed %1.After > %1
if exist %1.Before del %1.Before
if exist %1.After del %1.After
