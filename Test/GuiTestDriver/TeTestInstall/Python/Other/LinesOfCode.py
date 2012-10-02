#!/usr/bin/env python

import fileinput

#----------------------------------------------------------------------

totalLines = 0
totalCommentLines = 0

class CountLines:
	def __init__(self, string):
		self.name = string
		self.projectFile = 'C:\\Documents and Settings\\JonesT\\My Documents\\Komodo Projects\\' + string
		#self.projectFile = 'C:\\Documents and Settings\\JonesT\\Desktop\\' + string # 728 lines

	totalLines = 0
	totalCommentLines = 0

	#----------------------------------------------------------------------

	def count(self):
		temp = self.projectFile
		print self.name
		commentLines = 0
		lines = 0
		blanklines = 0

		for line in fileinput.FileInput(temp):
			#if "REM" in line:
			if "#" in line:
				commentLines += 1
			else:
				lines += 1
			if line.startswith('\n'):
				blanklines += 1

		lines_of_code = lines - blanklines
		lines_of_code_Nbr = "%i" %lines_of_code
		print 'Lines of Code = ' + lines_of_code_Nbr

		commentLines_Nbr = "%i" %commentLines
		self.totalCommentLines += commentLines
		print 'Comments      = ' + commentLines_Nbr
		print ' '
		self.totalLines += lines_of_code

	#----------------------------------------------------------------------

aCountLines = CountLines('Variables.py')
aCountLines.count()
totalCommentLines += aCountLines.totalCommentLines
totalLines += aCountLines.totalLines

aCountLines = CountLines('Reporter.py')
aCountLines.count()
totalCommentLines += aCountLines.totalCommentLines
totalLines += aCountLines.totalLines

aCountLines = CountLines('AutoTests.py')
aCountLines.count()
totalCommentLines += aCountLines.totalCommentLines
totalLines += aCountLines.totalLines

aCountLines = CountLines('TestPlan.py')
aCountLines.count()
totalCommentLines += aCountLines.totalCommentLines
totalLines += aCountLines.totalLines

aCountLines = CountLines('TestCase.py')
aCountLines.count()
totalCommentLines += aCountLines.totalCommentLines
totalLines += aCountLines.totalLines

aCountLines = CountLines('FieldWorks.py')
aCountLines.count()
totalCommentLines += aCountLines.totalCommentLines
totalLines += aCountLines.totalLines

#aCountLines = CountLines('TE AutoTests.bat')
#aCountLines.count()
#totalLines += aCountLines.totalLines

totalLines_Nbr = "%i" %totalLines
print 'Total Lines of Code => ' + totalLines_Nbr

totalCommentLines_Nbr = "%i" %totalCommentLines
print 'Total Comment Lines => ' + totalCommentLines_Nbr
