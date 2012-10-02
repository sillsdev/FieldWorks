#!/usr/bin/env python

#       $Id: po2xml.py,v 1.8 2006/02/03 22:14:37 mayhewn Exp $
#
#       Reformat PO files into an XML message catalog
#
#       Neil Mayhew - 11 Mar 2005

import sys, fileinput, re, string

def backslash_sub(m):
	c = m.group(1)
	if c == 'n':
		return '\n'
	else:
		return c

def unescape_c(s):
	s = re.sub(r'\\(.)', backslash_sub, s)
	return s

def escape_xml(s):
	s = re.sub('&', '&amp;', s)
	s = re.sub('<', '&lt;', s)
	s = re.sub('>', '&gt;', s)
	s = re.sub("'", '&apos;', s)
	s = re.sub('"', '&quot;', s)
	s = re.sub('\n', '&#10;', s)
	return s

class Message:
	def __init__(self, roundtrip):
		self.roundtrip = roundtrip
		self.reset()

	def reset(self):
		self.msgid = []
		self.msgstr = []
		self.usrcomment = []
		self.dotcomment = []
		self.reference = []
		self.flags = []
		self.current = None

	def flush(self, output):
		if self.current != self.msgstr:
			return
		output.write(str(self))
		self.reset()

	def __str__(self):
		s = '<msg>\n'
		if self.roundtrip:
			# Support exact round-tripping using multiple <key> and <str> child elements
			for t in self.msgid:
				s += '  <key>%s</key>\n' % escape_xml(t)
			for t in self.msgstr:
				s += '  <str>%s</str>\n' % escape_xml(t)
		else:
			# Concatenate parts into single <key> and <str> child elements
			s += '  <key>%s</key>\n' % escape_xml(string.join(self.msgid, ''))
			s += '  <str>%s</str>\n' % escape_xml(string.join(self.msgstr, ''))
		for t in self.usrcomment:
			s += '  <comment>%s</comment>\n' % escape_xml(t)
		for t in self.dotcomment:
			s += '  <info>%s</info>\n' % escape_xml(t)
		for t in self.reference:
			s += '  <ref>%s</ref>\n' % escape_xml(t)
		for t in self.flags:
			s += '  <flags>%s</flags>\n' % escape_xml(t)
		s += '</msg>\n'
		return s

def main():
	output = sys.stdout
	roundtrip = False

	msg = Message(roundtrip)

	output.write('<?xml version="1.0"?>\n')
	output.write('<?xml-stylesheet type="text/xsl" href="format-html.xsl"?>\n')
	output.write('<messages>\n')

	for l in fileinput.input():
		# Continuation string?
		m = re.match(r'\s*"(.*)"', l)
		if m:
			assert(msg.current)
			msg.current.append(unescape_c(m.group(1)))
			continue
		else:
			msg.flush(output)

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

	msg.flush(output)

	output.write('</messages>\n')

if __name__ == "__main__":
	main()
