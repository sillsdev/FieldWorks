REM This is a simple batch file to apply Model update transforms to a copy of TestLangProj.xml.
@echo off
attrib -R testlangproj.xml
call msxsl "TestLangProj.xml" %1 -xe -o "TestLangProjnew.xml"
del TestLangProj.bak
ren TestLangProj.xml TestLangProj.bak
ren TestLangProjnew.xml TestLangProj.xml