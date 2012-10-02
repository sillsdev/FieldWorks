#python defaults to ascii, so switch the console to unicode
import sys, codecs
sys.stdout = codecs.getwriter('utf-8')(sys.stdout)

#this is the python.net piece that interfaces us to the .net
#FieldWorks libraries
import clr
from CLR.System.Reflection import Assembly

#FDO (FieldWorks Data Objects) is the
#Object-Relational-Mapper layer of FieldWorks
fdo = Assembly.LoadWithPartialName("FDO")
from CLR.SIL.FieldWorks.FDO import FdoCache

#open up a language project to work on
db = FdoCache.Create("TestLangProj")
lp = db.LangProject
vern = lp.DefaultVernacularWritingSystem
analysisWs = lp.DefaultAnalysisWritingSystem
lexicon = lp.LexDbOA
for e in lexicon.EntriesOC :
	print "\lx " + e.LexemeFormOA.Form.GetAlternative(vern)
	for sense in e.SensesOS :
		print "\ge " + sense.Gloss.GetAlternative(analysisWs)
		if sense.MorphoSyntaxAnalysisRA <> None:
			print "\pos "   + sense.MorphoSyntaxAnalysisRA.InterlinearAbbr
	print
