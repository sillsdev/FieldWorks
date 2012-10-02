
:: **  Create "XmlFromCsv2.xml" by applying the xsl stylesheet to "XmlFromCsv.xml" **

@ECHO OFF

SET USER=%1

"C:\Documents and Settings\%USER%\Desktop\AutoTests Scripts\MSXSL.EXE" XmlFromCsv.xml XmlFromCsv.xsl -o XmlFromCsv2.xml