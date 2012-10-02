import sys
import re
import elementtree.ElementTree

"""
This file contains classes to convert a Key Terms markup file (like those
used by Paratext) into an XML format for Translation Editor.

Run from the command line and supply an input file containing a Key Terms
markup file and an output file for the generated XML.
"""

class OriginalTerm:

	prevTerm = None

	def __init__(self, row):
		fields = row.findall('td')
		self.term = ''
		self.strongs = ''
		self.louw_nida = ''
		self.gloss = ''
		if len(fields) == 4:
			word = fields[0].find('font')
			if word != None:
				if word.text != None:
					self.term = word.text.strip()
				elif OriginalTerm.prevTerm != None:
					self.term = OriginalTerm.prevTerm
			elif fields[0].text != None:
				self.term = fields[0].text.strip()
			elif OriginalTerm.prevTerm != None:
				self.term = OriginalTerm.prevTerm
			OriginalTerm.prevTerm = self.term

			if fields[1].text != None:
				self.strongs = fields[1].text.strip()

			if fields[2].text != None:
				self.louw_nida = fields[2].text.strip()

			if fields[3].text != None:
				self.gloss = fixText(fields[3].text.strip())
		elif len(fields) == 1 and fields[0].text != None:
			self.gloss = fixText(fields[0].text.strip())

	def write(self, outFile):
		line = '<original_term term="' + self.term + \
			   '" Strong="' + self.strongs + \
			   '" Louw.Nida="' + self.louw_nida + \
			   '" gloss="' + self.gloss + '" />\n'
		outFile.write(line)

class Reference:

	def __init__(self, scrRef):
		self.reference = scrRef
		self.keyword = None
		self.comment = None

	def processNote(self, note):
		s1 = re.search('<font[^/]*>', note)
		s2 = re.search('</font>', note)
		if s1 != None and s2 != None:
			self.keyword = note[s1.end():s2.start()].strip()
			if s2.end() < len(note):
				self.comment = fixText(note[s2.end():])
		else:
			self.keyword = note

	def write(self, outFile):
		outFile.write('<ref location="' + self.reference + '">\n')
		if self.keyword != None:
			outFile.write('<keyword>' + self.keyword + '</keyword>\n')
		else:
			outFile.write('<keyword>no_keyword</keyword>\n')
		if self.comment != None:
			outFile.write('<comment>' + self.comment + '</comment>\n')
		outFile.write('</ref>\n')

class Sense:

	def __init__(self, parent, level, name):
		self.parent = parent
		self.level = level
		self.name = name
		self.references = []
		self.subSenses = []

	def wrapup(self):
		pass

	def write(self, outFile):
		outFile.write('<sense name="' + self.name + '">\n')
		for sense in self.subSenses:
			sense.write(outFile)
		for ref in self.references:
			ref.write(outFile)
		outFile.write('</sense>\n')

class KeyTerm:

	def __init__(self, newTerm):
		self.level = 0
		i = newTerm.find('(see ')
		if i == -1:
			self.term = newTerm.replace(' ', '_')
			self.crossRef = None
		else:
			self.term = newTerm[0:i].strip().replace(' ', '_')
			self.crossRef = newTerm[i + 5:-1].strip().replace(' ', '_')
		self.originalTerms = []
		self.description = None
		self.currentRef = None
		self.references = []
		self.currentSubSense = None
		self.subSenses = []


	def wrapup(self):
		self.finishRef()
		if self.currentSubSense != None:
			self.currentSubSense.wrapup()
			self.currentSubSense.parent.subSenses.append(self.currentSubSense)

	def write(self, outFile):
		outFile.write('<keyterm name="' + self.term + '">\n')
		if self.crossRef != None:
			outFile.write('<cross_reference key="' + self.crossRef + '" />\n')
		for word in self.originalTerms:
			word.write(outFile)
		if self.description:
			outFile.write('<description>' + self.description + '</description>\n')
		for ref in self.references:
			ref.write(outFile)
		for sense in self.subSenses:
			sense.write(outFile)
		outFile.write('</keyterm>\n')

	def processTable(self, tableString):
		table = elementtree.ElementTree.fromstring(tableString)
		firstRow = True
		for row in table.getiterator('tr'):
			if not firstRow:
				self.originalTerms.append(OriginalTerm(row))
			firstRow = False

	def processOptionalLine(self, line):
		result = re.search('<table.*</table>', line)
		if result:
			self.processTable(line[result.start():result.end()])
			if result.end() < len(line):
				self.description = fixText(line[result.end():])

	def finishRef(self):
		if self.currentRef != None:
			if self.currentSubSense == None:
				self.references.append(self.currentRef)
			else:
				self.currentSubSense.references.append(self.currentRef)


	def startNewRef(self, ref):
		self.finishRef()
		self.currentRef = Reference(ref)

	def startSubSense(self, level, name):
		self.finishRef()
		if self.currentSubSense != None:
			self.currentSubSense.parent.subSenses.append(self.currentSubSense)
			parent = self.currentSubSense
			while parent.level >= level:
				parent = parent.parent
		else:
			parent = self
		self.currentSubSense = Sense(parent, level, name)
		self.currentRef = None

def processKeyTerms(inFile, outFile):
	currentTerm = None
	for line in inFile:
		line = line.strip()
		if line[0:5] == '\\key ':
			if currentTerm:
				currentTerm.wrapup()
				currentTerm.write(outFile)

			currentTerm = KeyTerm(line[5:])

		elif line[0:10] == '\\optional ':
			currentTerm.processOptionalLine(line[10:])

		elif line[0:5] == '\\ref ':
			currentTerm.startNewRef(line[5:].strip())

		elif line[0:8] == '\\rfnote ':
			currentTerm.currentRef.processNote(line[8:])

		elif line[0:6] == '\\key1 ':
			currentTerm.startSubSense(1, line[6:].strip())

		elif line[0:10] == '\\keynote1 ':
			currentTerm.currentSubSense.name = fixText(line[10:].strip())

		elif line[0:6] == '\\key2 ':
			currentTerm.startSubSense(2, line[6:].strip())

		elif line[0:10] == '\\keynote2 ':
			currentTerm.currentSubSense.name = fixText(line[10:].strip())

		elif line[0:6] == '\\key3 ':
			currentTerm.startSubSense(3, line[6:].strip())

		elif line[0:10] == '\\keynote3 ':
			currentTerm.currentSubSense.name = fixText(line[10:].strip())

	if currentTerm:
		currentTerm.wrapup()
		currentTerm.write(outFile)

def fixText(text):
	newText = text.replace('<font face="SIL Greek Trans">', "'")
	newText = newText.replace('</font>', "'")
	newText = newText.replace('<p>', '')
	newText = newText.replace('</p>', '')
	newText = newText.replace('"', "'")
	newText = newText.replace(chr(0x91), "'")
	newText = newText.replace(chr(0x92), "'")
	return newText

def main():
	inFile = file(sys.argv[1], 'r')
	outFile = file(sys.argv[2], 'w')
	outFile.write('<?xml version="1.0" encoding="utf-8"?>\n')
	outFile.write('<!DOCTYPE keyterms SYSTEM "KeyTerms.dtd">\n\n')

	outFile.write('<keyterms>\n')
	processKeyTerms(inFile, outFile)
	outFile.write('</keyterms>\n')
	inFile.close()
	outFile.close()

if __name__ == '__main__':
	main()
