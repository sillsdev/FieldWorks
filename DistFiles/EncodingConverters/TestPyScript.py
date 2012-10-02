import unicodedata

# Legacy to Legacy
def ToUpper(s):
	if not isinstance(s, str):
		raise ValueError('Input Data is not a Legacy string! (%s)' % s)
	else:
		return str.upper(s)

# Legacy to Legacy
def ToLower(s):
	if not isinstance(s, str):
		raise ValueError('Input Data is not a Legacy string! (%s)' % s)
	else:
		return str.lower(s)

# Legacy to Unicode (the default function is named "Convert")
def Convert(s):
	return unicode(s)

# Unicode to Unicode
def NFC(u):
	if not isinstance(u, unicode):
		raise UnicodeError(u'Input Data is not a Unicode string! (%s)' % u)
	else:
		return unicodedata.normalize('NFC',u)

# Unicode to Unicode
def NFD(u):
	if not isinstance(u, unicode):
		raise UnicodeError(u'Input Data is not a Unicode string! (%s)' % u)
	else:
		return unicodedata.normalize('NFD',u)

# Unicode to Legacy
def ToHex(u):
	if not isinstance(u, unicode):
		raise UnicodeError(u'Input Data is not a Unicode string! (%s)' % u)
	else:
		r = ''
		for ch in u:
			r += 'u%04x ' % ord(ch)
		return r

# Unicode to Legacy
def UnicodeName(u):
	if not isinstance(u, unicode):
		raise UnicodeError(u'Input Data is not a Unicode string! (%s)' % u)
	else:
		r = u''
		for ch in u:
			r += '%s; ' % unicodedata.name(ch)
		return r

# Unicode to Unicode (w/ addl fixed parameter being passed)
# notice that the data to be converted is always last
def OneAddlParam(sLang, u):
	if not isinstance(u, unicode):
		raise UnicodeError(u'Input Data is not a Unicode string! (%s)' % u)
	else:
		if sLang == u'Italian':
			u += u'o'
		return u

# Unicode to Unicode (w/ 2 addl fixed parameter being passed)
# notice that the data to be converted is always last
def TwoAddlParams(sLang, sOther, u):
	if not isinstance(u, unicode):
		raise UnicodeError(u'Input Data is not a Unicode string! (%s)' % u)
	else:
		if sLang == u'Italian':
			if sOther == u'schmaboogle':
				u += u'o'
		return u

def ProcessChinese(uI):
	return uI

def ChangeLanguage(sLang, uI):
	if not isinstance(uI, unicode):
		raise UnicodeError(u'Input Data is not a Unicode string! (%s)' % uI)
	else:
		if sLang == u'Chinese':
			# do some Chinese processing result in uO
			uO = ProcessChinese(uI)
		return uO

import pyclbr

# this is unnecessary for running the script via EncConverters (which just runs the functions), but is
# useful for testing that the functions work in PythonWin.
if __name__ == '__main__':
	print s
	s = ToUpper('asdf')
	print s
	s = ToLower(s)
	print s
	u = ToHex(u'asdg')
	print u
	u = Convert('asdg')
	print u
	u = UnicodeName(u'\N{greek small letter alpha with oxia}')
	print u
	u = UnicodeName(NFD(u'\N{greek small letter alpha with oxia}'))
	print u
	u = OneAddlParam(u'Italian', u'Bonjourn')
	print u
