rem ..\..\..\artifacts\Debug\pg -D phonenv.parser
..\..\..\artifacts\Debug\pg phonenv.parser
if exist hab.tmp del hab.tmp > nul
ren phonenv.parser.cs hab.tmp
gawk -f phonprs.awk < hab.tmp > hab.cs
if exist phonenv.parser.cs del phonenv.parser.cs > nul
ren hab.cs phonenv.parser.cs
