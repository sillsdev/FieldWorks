@echo off

Rem Concatenate all the TOC, index and project files to make editing easier and to avoid a lot of copying.

del TestHelp.hhp
copy all.hhp + Apps\all.hhp + Bugs\all.hhp + Building\all.hhp + Milestone\all.hhp + Source\all.hhp + Starting\all.hhp + TCL\all.hhp + Using\all.hhp + Writing\all.hhp + XML\all.hhp + WorldPad\all.hhp + DN\all.hhp + FW\all.hhp + TLE\all.hhp + StdCtrls\all.hhp TestHelp.hhp

del TestHelp.hhc
copy  all.hhc + Apps\all.hhc + Bugs\all.hhc + Building\all.hhc + Milestone\all.hhc + Source\all.hhc + Starting\all.hhc + TCL\all.hhc + Using\all.hhc + Writing\all.hhc + XML\all.hhc + WorldPad\all.hhc + DN\all.hhc + FW\all.hhc + TLE\all.hhc + StdCtrls\all.hhc + end.hh TestHelp.hhc

del TestHelp.hhk
copy  all.hhk + Apps\all.hhk + Bugs\all.hhk + Building\all.hhk + Milestone\all.hhk + Source\all.hhk + Starting\all.hhk + TCL\all.hhk + Using\all.hhk + Writing\all.hhk + XML\all.hhk + WorldPad\all.hhk + DN\all.hhk + FW\all.hhk + TLE\all.hhk + StdCtrls\all.hhk + end.hh TestHelp.hhk
