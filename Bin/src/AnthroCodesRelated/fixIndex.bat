REM First place a copy index*.htm files in this directory (* = letter NOT Index.htm or IndexFrame.htm)
REM Converts HTML to XHTML
call tidy -f err.txt -asxhtml -utf8 -m index*.htm

REM Note the above conversion does not update file dates

REM Add entity to XHTML files so that XSL processor can work.
call cc -t AddEnt.cct -o Index*.out Index*.tdy

REM Fix anchors on Index*.htm
call msxsl IndexA.out FixTheIndexPages.xsl -o IndexA.fix
call msxsl IndexB.out FixTheIndexPages.xsl -o IndexB.fix
call msxsl IndexC.out FixTheIndexPages.xsl -o IndexC.fix
call msxsl IndexD.out FixTheIndexPages.xsl -o IndexD.fix
call msxsl IndexE.out FixTheIndexPages.xsl -o IndexE.fix
call msxsl IndexF.out FixTheIndexPages.xsl -o IndexF.fix
call msxsl IndexG.out FixTheIndexPages.xsl -o IndexG.fix
call msxsl IndexH.out FixTheIndexPages.xsl -o IndexH.fix
call msxsl IndexI.out FixTheIndexPages.xsl -o IndexI.fix
call msxsl IndexJ.out FixTheIndexPages.xsl -o IndexJ.fix
call msxsl IndexK.out FixTheIndexPages.xsl -o IndexK.fix
call msxsl IndexL.out FixTheIndexPages.xsl -o IndexL.fix
call msxsl IndexM.out FixTheIndexPages.xsl -o IndexM.fix
call msxsl IndexN.out FixTheIndexPages.xsl -o IndexN.fix
call msxsl IndexO.out FixTheIndexPages.xsl -o IndexO.fix
call msxsl IndexP.out FixTheIndexPages.xsl -o IndexP.fix
call msxsl IndexQ.out FixTheIndexPages.xsl -o IndexQ.fix
call msxsl IndexR.out FixTheIndexPages.xsl -o IndexR.fix
call msxsl IndexS.out FixTheIndexPages.xsl -o IndexS.fix
call msxsl IndexT.out FixTheIndexPages.xsl -o IndexT.fix
call msxsl IndexU.out FixTheIndexPages.xsl -o IndexU.fix
call msxsl IndexV.out FixTheIndexPages.xsl -o IndexV.fix
call msxsl IndexW.out FixTheIndexPages.xsl -o IndexW.fix
call msxsl IndexX.out FixTheIndexPages.xsl -o IndexX.fix
call msxsl IndexY.out FixTheIndexPages.xsl -o IndexY.fix
call msxsl IndexZ.out FixTheIndexPages.xsl -o IndexZ.fix

ren *.htm *.tdy
ren *.fix *.htm

REM copy *.htm files to Help compile directory.
