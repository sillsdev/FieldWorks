rem ..\..\Bin\CSharpLexYaccTool\pg -D phonenv.parser
..\..\..\Bin\CSharpLexYaccTool\pg phonenv.parser
if exist hab.tmp del hab.tmp > nul
ren phonenv.parser.cs hab.tmp
..\..\..\Bin\gawk -f phonprs.awk < hab.tmp > hab.cs
if exist phonenv.parser.cs del phonenv.parser.cs > nul
ren hab.cs phonenv.parser.cs
