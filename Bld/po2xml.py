#!/usr/bin/env python

#       po2xml.py
#
#       Reformat PO files into an XML message catalog
#
#       Neil Mayhew - 11 Mar 2005

import sys, re, string
import optparse, fileinput
from xml.sax.saxutils import XMLGenerator

def backslash_sub(m):
	c = m.group(1)
	return '\n' if c == 'n' else c

def unescape_c(s):
	return re.sub(r'\\(.)', backslash_sub, s)

class Message:
	def __init__(self, writer, roundtrip=False, indent="  "):
		self.writer = writer
		self.roundtrip = roundtrip
		self.indent = indent
		self.reset()

	def reset(self):
		self.msgid = []
		self.msgstr = []
		self.usrcomment = []
		self.dotcomment = []
		self.reference = []
		self.flags = []
		self.current = None

	def flush(self):
		if self.current != self.msgstr:
			return
		self.write()
		self.reset()

	def write(self):
		self.writer.startElement("msg", {})
		self.writer.ignorableWhitespace("\n")

		if self.roundtrip:
			# Support exact round-tripping using multiple <key> and <str> child elements
			for t in self.msgid:
				self.writeElement("key", t)
			for t in self.msgstr:
				self.writeElement("str", t)
		else:
			# Concatenate parts into single <key> and <str> child elements
			self.writeElement("key", string.join(self.msgid,  ''))
			self.writeElement("str", string.join(self.msgstr, ''))

		for t in self.usrcomment:
			self.writeElement("comment", t)
		for t in self.dotcomment:
			self.writeElement("info", t)
		for t in self.reference:
			self.writeElement("ref", t)
		for t in self.flags:
			self.writeElement("flags", t)

		self.writer.endElement("msg")
		self.writer.ignorableWhitespace("\n")

	def writeElement(self, name, data="", attrs={}):
		self.writer.ignorableWhitespace(self.indent)
		self.writer.startElement(name, attrs)
		self.writer.characters(data)
		self.writer.endElement(name)
		self.writer.ignorableWhitespace("\n")

def main():
	optparser = optparse.OptionParser(usage="Usage: %prog [options] POFILE ...")
	optparser.add_option("-r", "--roundtrip", help="generate round-tripable output",
		action="store_true", dest="roundtrip", default=False)
	optparser.add_option("-o", "--output", help="write output to XMLFILE", metavar="XMLFILE",
		action="store", dest="output", default="-")
	(options, args) = optparser.parse_args()

	if options.output == "-":
		output = sys.stdout
	else:
		output = open(options.output, "w")

	writer = XMLGenerator(output or sys.stdout, "utf-8")
	writer.startDocument()
	writer.processingInstruction("xml-stylesheet", 'type="text/xsl" href="format-html.xsl"')
	writer.ignorableWhitespace("\n")

	writer.startElement("messages", {})
	writer.ignorableWhitespace("\n")

	msg = Message(writer, options.roundtrip)

	for l in fileinput.input(args):
		# Continuation string?
		m = re.match(r'\s*"(.*)"', l)
		if m:
			assert(msg.current)
			msg.current.append(unescape_c(m.group(1)))
			continue
		else:
			msg.flush()

		m = re.match(r'(?s)msgid "(.*)"', l)
		if m:
			msg.msgid = [unescape_c(m.group(1))]
			msg.current = msg.msgid
		m = re.match(r'(?s)msgstr "(.*)"', l)
		if m:
			msg.msgstr = [unescape_c(m.group(1))]
			msg.current = msg.msgstr

		m = re.match(r'# \s*(.*)', l)
		if m:
			msg.usrcomment.append(m.group(1))
		m = re.match(r'#\.\s*(.*)', l)
		if m:
			msg.dotcomment.append(m.group(1))
		m = re.match(r'#:\s*(.*)', l)
		if m:
			msg.reference.append(m.group(1))
		m = re.match(r'#,\s*(.*)', l)
		if m:
			msg.flags.append(m.group(1))

	msg.flush()

	writer.endElement("messages")
	writer.ignorableWhitespace("\n")
	writer.endDocument()

if __name__ == "__main__":
	main()
