import unittest
import ConvertFile

"""
This file contains test for the code in the ConvertFile module.

Run at command line to run all tests.
"""

class TestOutput:

	def __init__(self):
		self.output = [];
		self.nextLine = 0

	def Next(self):
		i = self.nextLine;
		self.nextLine += 1
		if i < len(self.output):
			return self.output[i]
		else:
			return None

	def write(self, line):
		self.output.append(line.strip())

class TestConversion(unittest.TestCase):

	def testKeyTerm(self):
		# the testData is bits and pieces of several actual entries to create a complete test
		# case
		testData = []
		testData.append('\\key Abba')
		testData.append('\\optional <table class="opt">' + \
						'<tr><td>Greek Term</td><td>Strong</td><td>Louw.Nida</td><td>Gloss</td></tr>' + \
						'<tr><td><font face="SIL Greek Trans">abba            </font></td><td>5     </td><td>12.12      </td><td>Father, transliterated from Aramaic</td></tr>' + \
						'<tr><td><font face="SIL Greek Trans"/></td><td/><td>35.16       </td><td>helper</td></tr>' + \
						'<tr><td colspan="4">See also MEDIATOR.</td></tr>' + \
						'</table><p>description of term</p>')
		testData.append('\\ref MRK 14:36')
		testData.append('\\rfnote Greek: <font face="SIL Greek Trans">abba</font>text of comment')
		testData.append('\\key1 Advocate')
		testData.append('\\keynote1  Advocate, one who speaks on behalf of another')
		testData.append('\\ref MAT 9:18')
		testData.append('\\rfnote Greek: <font face="SIL Greek Trans">archwn</font>')


		testOutput = TestOutput()
		ConvertFile.processKeyTerms(testData, testOutput)

		# print testOutput.output
		self.assertEqual('<keyterm name="Abba">',
						 testOutput.Next())
		self.assertEqual('<original_term term="abba" Strong="5" Louw.Nida="12.12" gloss="Father, transliterated from Aramaic" />',
						 testOutput.Next())
		self.assertEqual('<original_term term="abba" Strong="" Louw.Nida="35.16" gloss="helper" />',
						 testOutput.Next())
		self.assertEqual('<original_term term="" Strong="" Louw.Nida="" gloss="See also MEDIATOR." />',
						 testOutput.Next())
		self.assertEqual('<description>description of term</description>',
						 testOutput.Next())
		self.assertEqual('<ref location="MRK 14:36">',
						 testOutput.Next())
		self.assertEqual('<keyword>abba</keyword>',
						 testOutput.Next())
		self.assertEqual('<comment>text of comment</comment>',
						 testOutput.Next())
		self.assertEqual('</ref>',
						 testOutput.Next())
		self.assertEqual('<sense name="Advocate, one who speaks on behalf of another">',
						 testOutput.Next())
		self.assertEqual('<ref location="MAT 9:18">',
						 testOutput.Next())
		self.assertEqual('<keyword>archwn</keyword>',
						 testOutput.Next())
		self.assertEqual('</ref>',
						 testOutput.Next())
		self.assertEqual('</sense>',
						 testOutput.Next())
		self.assertEqual('</keyterm>',
						 testOutput.Next())
		self.assertEqual(None, testOutput.Next())

	def testNestedSensesTwoDeep(self):
		# the testData is bits and pieces of several actual entries to create a complete test
		# case
		testData = []
		testData.append('\\key Abba')
		testData.append('\\optional <table class="opt">' + \
						'<tr><td>Greek Term</td><td>Strong</td><td>Louw.Nida</td><td>Gloss</td></tr>' + \
						'<tr><td><font face="SIL Greek Trans">abba            </font></td><td>5     </td><td>12.12      </td><td>Father, transliterated from Aramaic</td></tr>' + \
						'</table>')
		testData.append('\\key1 Advocate')
		testData.append('\\keynote1  Advocate, one who speaks on behalf of another')
		testData.append('\\key2 Adultery')
		testData.append('\\keynote2  Masculine offenders')
		testData.append('\\ref LUK 18:11')
		testData.append('\\rfnote Greek: <font face="SIL Greek Trans">moichos</font>')


		testOutput = TestOutput()
		ConvertFile.processKeyTerms(testData, testOutput)

		# print testOutput.output
		self.assertEqual('<keyterm name="Abba">',
						 testOutput.Next())
		self.assertEqual('<original_term term="abba" Strong="5" Louw.Nida="12.12" gloss="Father, transliterated from Aramaic" />',
						 testOutput.Next())
		self.assertEqual('<sense name="Advocate, one who speaks on behalf of another">',
						 testOutput.Next())
		self.assertEqual('<sense name="Masculine offenders">',
						 testOutput.Next())
		self.assertEqual('<ref location="LUK 18:11">',
						 testOutput.Next())
		self.assertEqual('<keyword>moichos</keyword>',
						 testOutput.Next())
		self.assertEqual('</ref>',
						 testOutput.Next())
		self.assertEqual('</sense>',
						 testOutput.Next())
		self.assertEqual('</sense>',
						 testOutput.Next())
		self.assertEqual('</keyterm>',
						 testOutput.Next())
		self.assertEqual(None, testOutput.Next())

	def testNestedSensesThreeDeep(self):
		# the testData is bits and pieces of several actual entries to create a complete test
		# case
		testData = []
		testData.append('\\key Abba')
		testData.append('\\optional <table class="opt">' + \
						'<tr><td>Greek Term</td><td>Strong</td><td>Louw.Nida</td><td>Gloss</td></tr>' + \
						'<tr><td><font face="SIL Greek Trans">abba            </font></td><td>5     </td><td>12.12      </td><td>Father, transliterated from Aramaic</td></tr>' + \
						'</table>')
		testData.append('\\key1 Advocate')
		testData.append('\\keynote1  Advocate, one who speaks on behalf of another')
		testData.append('\\key2 Adultery')
		testData.append('\\keynote2  Masculine offenders')
		testData.append('\\key3 Believe')
		testData.append('\\keynote3  To believe that a statement is true')
		testData.append('\\ref MAT 8:13')
		testData.append('\\rfnote Greek: <font face="SIL Greek Trans">pisteuw (verb)</font>')


		testOutput = TestOutput()
		ConvertFile.processKeyTerms(testData, testOutput)

		# print testOutput.output
		self.assertEqual('<keyterm name="Abba">',
						 testOutput.Next())
		self.assertEqual('<original_term term="abba" Strong="5" Louw.Nida="12.12" gloss="Father, transliterated from Aramaic" />',
						 testOutput.Next())
		self.assertEqual('<sense name="Advocate, one who speaks on behalf of another">',
						 testOutput.Next())
		self.assertEqual('<sense name="Masculine offenders">',
						 testOutput.Next())
		self.assertEqual('<sense name="To believe that a statement is true">',
						 testOutput.Next())
		self.assertEqual('<ref location="MAT 8:13">',
						 testOutput.Next())
		self.assertEqual('<keyword>pisteuw (verb)</keyword>',
						 testOutput.Next())
		self.assertEqual('</ref>',
						 testOutput.Next())
		self.assertEqual('</sense>',
						 testOutput.Next())
		self.assertEqual('</sense>',
						 testOutput.Next())
		self.assertEqual('</sense>',
						 testOutput.Next())
		self.assertEqual('</keyterm>',
						 testOutput.Next())
		self.assertEqual(None, testOutput.Next())

	def testKeyTermReference(self):
		testData = []
		testData.append('\\key Abstinence from eating (see Fast)')

		testOutput = TestOutput()
		ConvertFile.processKeyTerms(testData, testOutput)

		# print testOutput.output
		self.assertEqual('<keyterm name="Abstinence_from_eating">',
						 testOutput.Next())
		self.assertEqual('<cross_reference key="Fast" />',
						 testOutput.Next())
		self.assertEqual('</keyterm>',
						 testOutput.Next())
		self.assertEqual(None, testOutput.Next())

if __name__ == '__main__':
	unittest.main()
