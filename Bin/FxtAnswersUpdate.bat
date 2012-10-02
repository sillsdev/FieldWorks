REM Run this after any model change which causes FXT tests to fail.

p4 edit %fwroot%\Src\FXT\FxtDll\FxtDllTests\ExpectedResults\TLP*.*

%fwroot%/output/debug/fxt TestLangProj %fwroot%/DistFiles\Language Explorer\Configuration\Grammar\FXTs\m3parser.fxt %fwroot%/src/FXT\FxtDll\FxtDllTests\ExpectedResults/tlpParser.xml -parserDump
%fwroot%/output/debug/fxt TestLangProj "%fwroot%/DistFiles\Language Explorer\Export Templates\RootBasedMdf.xml" %fwroot%/src/FXT\FxtDll\FxtDllTests\ExpectedResults/TLPRootBasedMDF.sfm
%fwroot%/output/debug/fxt TestLangProj %fwroot%/src/FXT\FxtDll\FxtDllTests\simpleGuids.fxt %fwroot%/src/FXT\FxtDll\FxtDllTests\ExpectedResults/tlpSimpleGuidsAnswer.xml -guids
%fwroot%/output/debug/fxt TestLangProj %fwroot%/DistFiles\Language Explorer\Configuration\Grammar\FXTs\m3sketchgen.fxt %fwroot%/src/FXT\FxtDll\FxtDllTests\ExpectedResults/tlpsketchgen.xml -guids
%fwroot%/output/debug/fxt TestLangProj "%fwroot%/DistFiles\Language Explorer\Export Templates\mdf.xml" %fwroot%/src/FXT\FxtDll\FxtDllTests\ExpectedResults/TLPStandardFormatMDF.sfm -guids
%fwroot%/output/debug/fxt TestLangProj %fwroot%\src\FXT\FxtDll\FxtDllTests\WebPageSample.xhtml %fwroot%/src/FXT\FxtDll\FxtDllTests\ExpectedResults/tlpWebPageSampleAnswer.xhtml

REM Reverting any unchanged lists
p4 revert -a -c default %fwroot%/src/FXT\FxtDll\FxtDllTests\ExpectedResults/TLPParser.xml
p4 revert -a -c default %fwroot%/src/FXT\FxtDll\FxtDllTests\ExpectedResults/TLPRootBasedMDF.sfm
p4 revert -a -c default %fwroot%/src/FXT\FxtDll\FxtDllTests\ExpectedResults/TLPSimpleGuidsAnswer.xml
p4 revert -a -c default %fwroot%/src/FXT\FxtDll\FxtDllTests\ExpectedResults/TLPSketchGen.xml
p4 revert -a -c default %fwroot%/src/FXT\FxtDll\FxtDllTests\ExpectedResults/TLPStandardFormatMDF.sfm
p4 revert -a -c default %fwroot%/src/FXT\FxtDll\FxtDllTests\ExpectedResults/TLPWebPageSampleAnswer.xhtml

REM Done. Any changed fxt "answer files" files should now be listed under your default p4 changelist.
