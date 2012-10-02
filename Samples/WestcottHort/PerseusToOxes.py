##------------------------------------------------------
## PerseusToOsis.py
## Convert the Perseus NT to OXES format
## Responsibility: Steve Miller
## Last Reviewed: Never
##
##  Usage:
##      -- You'll need Python version 3 or later.
##      -- Change the hard coded directories to your preference below.
##
##  Known Issues:
##      -- Quote marks aren't quite right. Also, The source text doesn't
##          distinguish between OT quotes and dialog quotes.
##      -- Paragraph breaks aren't quite right.
##      -- The Creative Commons attribution probably belongs in Rightslong,
##          but it hasn't been hooked up yet.
##      -- The Contributor tags are hooked up, but don't make it into the text in FW.
##      -- Input and output files are hard coded.
##
##  Notes:
##      See ReadMe.rf
##------------------------------------------------------

import string
import time
import datetime
from time import strftime
import os

class PerseusToOsis:

	debugging = False #( Set to false for production

	perseus_file_name = "C:\\FwBEP\Samples\\WestcottHort\\nt_gk.xml"
	oxes_file_name = "C:\\FwBEP\Samples\\WestcottHort\\nt_gk.oxes"
	perseus = open(perseus_file_name, "r")
	oxes = open(oxes_file_name, encoding='utf-8', mode="w")

	book_title = '' #( build the book title in this string
	book_abbreviation = ''
	book_english = ''
	chapter = '0'
	verse = '0'
	milestone_number = '0'
	quote_level = 0
	diacritic_markers = '()\\/=|+'

	more_milestone_info = False
	more_quote_info = False

	start_paragraph = False
	start_chapter = False
	start_verse = False
	start_trgroup = False
	start_quote = False
	start_greek = False

	close_book = False
	close_section = False
	close_paragraph = False
	close_chapter = False
	close_verse = False
	close_quote = False
	close_greek = False

	is_file_header_info = True
	is_title = False
	is_book = False
	is_milestone = False
	is_note = False
	is_upper = False
	is_pb = False
	is_citation1 = False
	is_citation2 = False
	is_unspecified_quote = False
	is_div0 = False
	is_div2 = False
	is_end = False

	add_psili = False #( smooth breather
	add_dasia = False #( rough breather
	add_varia = False #( an accent marker
	add_oxia = False #( another accent marker
	add_perispomeni = False #( circumflex
	add_ypogegrammeni = False #( subscript iota for small letters
	add_prosgegrammeni = False #( subscript iota for capitol letters
	add_dialytika = False #( double dot above

	def convert(self):
		self.write_file_header()
		self.process_source()
		self.write_file_footer()
		self.cleanup()

	#=================================( Methods to Process Source Text )======================#

	#------------------------------------------------------------------------
	# Method: process_source
	# Summary: Read and process the source text
	#------------------------------------------------------------------------

	def process_source(self):
		for full_line in self.perseus.readlines():
			if self.debugging:
				self.oxes.write('\nfull_line: ' + full_line) # no need for an extra line feed!

			line = full_line.strip()
			line = line.replace('>', '> ')

			for word in line.split():
				if '&ulcrop;' in word: #REVIEW (Steve Miller): So far I can't find a purprose for left or right croppings.
					pos = word.find('&ulcrop;')
					word = word[:pos] + word[pos + 8:]
				if '&urcrop;' in word: #REVIEW (Steve Miller): So far I can't find a purprose for left or right croppings.
					pos = word.find('&urcrop;')
					word = word[:pos] + word[pos + 8:]
				if '&mdash;' in word:
					pos = word.find('&mdash;')
					word = word[:pos] + '&' + word[pos + 7:] #( Currently using the & as a marker to turn it into an em dash below.
				if '&lt;*&gt;' in word:  #REVIEW (Steve Miller): I don't know wh <*> was put in the text
					pos = word.find('&lt;*&gt;')
					word = word[:pos] + word[pos + 9:]

				if 'div0' in word or self.is_div0:
					self.is_div0 = '-->' not in word
				elif word == '<div1':
					self.is_book = True
				elif word == '</div1>':
					pass
				elif 'div2' in word or self.is_div2:
					#( This marks a section at the end of Mark that I'm not familiar with. For now I'm not bringing it over.
					self.is_div2 = '/div2' not in word
				elif self.is_book:
					self.process_book(word)
				elif self.is_note:
					if '-->' in word:
						if '</l>' in word:
							self.process_citation_end('')
						self.is_note = False
				elif word == '<!--':
					#REVIEW (Steve Miller): I currently can't see anything more to do with notes. We might want to do something with them eventually.
					if self.is_unspecified_quote:
						self.write_paragraph_start()
						self.is_unspecified_quote = False
					self.is_note = True
				elif '<l>' in word:
					if self.is_unspecified_quote:
						self.write_paragraph_start()
						self.is_unspecified_quote = False
					self.process_citation1_start(word)
				elif '<l' == word:
					self.is_citation2 = True
				elif self.is_citation2 and 'indent' in word:
					self.process_citation2_start(word)
				elif '</l>' in word:
					if 'verse' in word: #( Still processing a verse milestone
						pos = word.rfind('</l>')
						self.process_citation_end(word[pos:])
						self.process_milestone(word[:pos])
					elif word[:2] == '/>': #( still finishing up previous milestone
						self.process_citation_end(word[2:])
						self.process_milestone(word[2:])
					else:
						self.process_citation_end(word)
				#( Notes have to precede milestones, because some notes are stuck between milestone markers. See Mt. 1:10-11
				#( Ditto for citations.
				elif word == '<milestone':
					self.is_milestone = True
				elif self.is_milestone:
					self.process_milestone(word)
				elif self.is_title or '<head>' in word:
					self.is_file_header_info = False
					title_ready = self.process_title(word)
					if (title_ready):
						self.write_book_start()
				elif '<quote>' in word:
					if 'milestone' in word:
						self.is_milestone = True
						self.process_quote_start('')
					else:
						self.process_quote_start(word)
				elif '</quote>' in word:
					self.process_quote_end(word)
				elif '<q' == word or 'unspecified' in word:
					self.is_unspecified_quote = True
				elif '</q>' in word:
					pos = word.find('</q>')
					if pos > 0:
						self.process_quote_end(word[:pos])
					#( The case of "elif '<!' in word" is handled above
					elif len(word) > 4:
						if self.is_unspecified_quote:
							self.process_quote_end(' ' + word[:4])
						else:
							self.write_greek_word(' ' + word[:4])
					self.write_paragraph_start()
					self.is_unspecified_quote = False
				elif self.is_unspecified_quote:
					#( The case of <l> being in the next word is handled above.
					if word[0] == '>':
						word = word[1:]
					if '&ulcrop;' in word:
						word = word[word.find('&ulcrop;') + 8:]
					if '&urcrop;' in word:
						word = word[:word.find('&urcrop;')]

					if '<quote>' in word:
						self.is_unspecified_quote = False #( We want to keep the flag on otherwise for an end quote
					self.process_quote_start(word)
				elif self.is_file_header_info:
					pass
				elif self.is_pb:
					if '>' in word:
						self.is_pb = False
				elif '<pb' in word: #( Apparently a paragraph break, but placed badly. See John 7:1.
					pos = word.rfind('<pb')
					if pos > 0:
						word = word[:pos]
						self.write_greek_word(' ' + word)
					##self.write_paragraph_start()
					self.is_pb = True
				elif '<p>' == word:
					#REVIEW (Steve Miller): This may well be a good place to put in section.
					pass
				elif '</p>' in word:
					pos = word.rfind('</p>')
					if pos > 0:
						word = word[:pos]
						self.write_greek_word(' ' + word)
						self.write_greek_line_end()
					self.write_paragraph_start()
				elif '&top;' in word:
					pos = word.rfind('<&top;>')
					if pos > 0:
						word = word[:pos]
						self.write_greek_word(' ' + word)
				elif '</body>' in word or self.is_end:
					self.is_end = True
				else:
					if 'milestone' in word:
						word = word[:word.find('<milestone')]
						self.is_milestone = True

					if self.start_greek:
						self.write_greek_line_start()
						self.write_greek_word(word)
					else:
						self.write_greek_word(' ' + word)

	#------------------------------------------------------------------------
	# Method: process_book
	# Summary: Find both the English name and the abberviation of the book
	# Parameter: word = the individual word being processed
	# Example line: <div1 type="Book" n="Matthew">
	#------------------------------------------------------------------------

	def process_book(self, word):
		if 'n="' in word:
			if '">' in word: #( Such as: n="Matthew">
				self.book_english = word[word.find('n="') + 3:word.rfind('">')]
				self.book_abbreviation = self.get_book_abbreviation(self.book_english)
				self.is_book = False
			else: #( Such as the first half of: n="I Peter">
				self.book_english = word[word.find('n="') + 3:]
		elif '">' in word:
			self.book_english += ' ' + word[:word.rfind('">')]
			self.book_abbreviation = self.get_book_abbreviation(self.book_english)
			self.is_book = False

	#------------------------------------------------------------------------
	# Method: process_title
	# Summary: Extract the title of the book from the line
	# Parameter: word = the individual word being processed
	# Example line: <head>*k*a*t*a *m*a*q*q*a*i*o*n</head>
	#------------------------------------------------------------------------

	def process_title(self, word):
		pos = word.find('<head>')
		if (pos != -1):
			word = word[pos + 6:]
			self.is_title = True

		pos = word.rfind('</head>')
		if (pos == -1):
			self.book_title += ' ' + word
			title_ready = False
		else:
			word = word[:pos]
			self.book_title += ' ' + word
			self.is_title = False
			title_ready = True

		return title_ready

	#------------------------------------------------------------------------
	# Method: process_milestone
	# Summary: Three types of milestones exist: chapters, verses, and
	#   paragraphs. Each type is processed a little differently:
	#       -- Chapter milestones are always followed by a verse or paragraph milestone.
	#       -- Verse milestones are always followed by a paragraph milestone or Greek text.
	#       -- Paragraph milestones are always followed by Greek text.
	#   However, working word by word, we don't always know which milestone we are
	#   working with until we process a couple more words.
	# Parameter: word = the individual word being processed
	#
	# General Example lines:
	#   <milestone n="1" unit="chapter"/><milestone n="1" unit="verse"/>
	#   <milestone unit="para"
	#   <milestone n="2"
	#   <milestone unit="para"/>*)abraa\m e)ge/nnhsen to\n *)isaa/k,
	#   <milestone
	#   <milestone n="4" unit="verse"/>
	#   a(gi/ou. <milestone
	#   n="2" unit="chapter"/><milestone n="1" unit="verse"/>
	# Chapter Examples:
	#   <milestone n="1" unit="chapter"/><milestone n="1" unit="verse"/>
	#   n="2" unit="chapter"/><milestone n="1" unit="verse"/>
	#   unit="chapter"/><milestone n="1" unit="verse"/>
	# Verse Examples:
	#   <milestone n="1" unit="chapter"/><milestone n="1" unit="verse"/>
	#   unit="verse"/>
	#   <milestone n="4" unit="verse"/>
	#   n="7" unit="verse"/>
	#   unit="verse"
	#   n="25" unit="verse"
	# Paragraph Examples:
	#   <milestone unit="para"
	#   />*)isaa\k de\ e)ge/nnhsen to\n *)iakw/b,
	#   unit="para"/>*fare\s de\ e)ge/nnhsen to\n *(esrw/m,
	#------------------------------------------------------------------------

	def process_milestone(self, word):
		if (self.close_greek):
			self.write_greek_line_end()

		greek_word = ''
		is_chapter = 'chapter' in word
		is_verse = 'verse' in word
		is_paragraph = 'para' in word
		close_bracket_pos = word.find('>')
		has_close_bracket = close_bracket_pos != -1

		if ('n="' in word):
			self.set_milestone_number(word)

		if (is_chapter):
			self.chapter = self.milestone_number
			self.start_chapter = True
			if ('<milestone' in word):
				has_close_bracket = False #( Don't want to trigger end of milestone yet.

		if (is_verse):
			self.verse = self.milestone_number
			self.start_verse = True

		if (is_paragraph):
			self.start_paragraph = True

		if (has_close_bracket):
			self.is_milestone = False
			self.start_greek = True
			if (len(word) > close_bracket_pos + 1):
				self.write_greek_line_start()
				greek_word = word[close_bracket_pos + 1:]
				if '<quote>' in greek_word:
					self.process_quote_start(greek_word)
				else:
					self.write_greek_word(greek_word)

	#------------------------------------------------------------------------
	# Method: process_quote_start, process_quote_end
	# Summary: Process quotations.
	# Parameter: word = the individual word being processed
	#
	# OXES has markers for both <wordsOfJesus> and <otPassage>. See, for example,
	# Mt. 4:4. The W-H text doesn't appear to have any particular marker for either,
	# and so I had no way of marking those for OXES. The W-H appears to use the
	# quote marker strictly for quotation marks. See, for example, Mt. 1:23.
	#
	# However, both have markers for levels of "poetic" literature such as OT quotes
	# or the Lord's prayer.
	#
	# General examples:
	#   <quote>
	#   <l>*)idou\ h( parqe/nos e)n gastri\ e(/cei kai\ te/cetai ui(o/n,</l>
	#   <l>kai\ kale/sousin to\ o)/noma au)tou= *)emmanouh/l:</l>
	#   </quote>
	#   <milestone unit="para"
	#   />o(/ e)stin meqermhneuo/menon <quote>*meq' h(mw=n o( qeo/s</quote>. <milestone n="24"
	#   <quote>
	#   <quote>*)ec *ai)gu/ptou e)ka/lesa to\n ui(o/n mou</quote>. <milestone
	#   o( de\ a)pokriqei\s ei)=pen *ge/graptai <quote>*ou)k e)p' a)/rtw| mo/nw|
	#   />maka/rioi <quote>oi( penqou=ntes,</quote> o(/ti au)toi\ <quote>paraklhqh/sontai.</quote>
	#------------------------------------------------------------------------

	def process_quote_start(self, word):
		if self.start_paragraph:
			self.write_paragraph_start()
			self.write_greek_line_start()
		word = word[word.find('<quote>') + 7:]
		if len(word) > 0:
			end_quote = '</quote>' in word
			if end_quote:
				word = word[:word.rfind('</quote>')]
			self.write_quote_start()
			self.write_greek_word(word)
			if end_quote:
				self.write_quote_end()
		else:
			self.start_quote = True

	#-------------------------------------------------------------------

	def process_quote_end(self, word):
		word = word[:word.rfind('</quote>')]
		self.close_quote = True
		if len(word) > 0:
			self.write_greek_word(' ' + word)
			#TODO (Steve Miller): The quote end really ought to be written even if the word isn't there. But doing so now messes up paragraphing.
			self.write_quote_end()

	#-------------------------------------------------------------------
	# Method: process_citiation1, process_citiation2, process_citation_end
	# Summary: Process citations. These are typically OT quotes, but can
	#   be things like the Lord's prayer.
	# Parameter: snippet = the "word" containing the milestone number
	#-------------------------------------------------------------------

	def process_citation1_start(self, word):
		if self.close_greek:
			self.write_greek_line_end()

		self.is_citation1 = True
		self.write_citation1_start()
		if word[:3] == '<l>':
			word = word[3:] #( strip off <l>
		end_pos = word.find('</l>')
		if end_pos > 0:
			word = word[:end_pos]

		quote_pos = word.find('<quote>')
		has_quote = False
		if quote_pos >= 0:
			word = word[:quote_pos] + word[quote_pos + 7:]
			has_quote = True

		self.write_greek_line_start()
		if self.is_unspecified_quote:
			self.process_quote_start(word)
			self.is_unspecified_quote = False
		else:
			if has_quote:
				self.process_quote_start(word)
			else:
				self.write_greek_word(word)

		if end_pos > 0:
			self.process_citation_end('')

	def process_citation2_start(self, word):
		if self.close_greek:
			self.write_greek_line_end()
		if self.close_paragraph:
			self.write_paragraph_end()

		self.write_citation2_start()

		word = word[word.find('>') + 1:]
		end_pos = word.find('</l>')
		if end_pos > 0:
			word = word[:end_pos]
		if 'milestone' in word:
		   word = word[:word.find('<milestone')]
		   self.is_milestone = True

		self.write_greek_line_start()
		if len(word) > 0:
			self.write_greek_word(word)
		if end_pos > 0:
			self.process_citation_end('')

	def process_citation_end(self, word):
		self.is_citation1 = False
		self.is_citation2 = False

		if self.close_quote:
			self.write_quote_end()
		word =  word[:word.rfind('</l>')]
		if len(word) > 0:
			if 'quote' in word:
				self.process_quote_end(word)
			else:
				self.write_greek_word(' ' + word)
			self.write_greek_line_end()
		self.write_citation_end()

		self.start_greek = True
		self.start_paragraph = True

	#-------------------------------------------------------------------
	# Method: set_milestone_number
	# Summary: Finds the number of the milestone, whether for chapter or verse
	# Parameter: snippet = the "word" containing the milestone number
	#-------------------------------------------------------------------

	def set_milestone_number(self, snippet):
		pos1 = snippet.find('n="') + 3
		pos2 = snippet.find('"', pos1 + 1)
		self.milestone_number = snippet[pos1:pos2]

	#=================================( Methods to Write New Text )======================#

	#------------------------------------------------------------------------
	# Method: write_file_header
	# Summary: Write out the header to the target file
	#------------------------------------------------------------------------

	def write_file_header(self):
		today = datetime.date.today()
		computername = os.getenv('COMPUTERNAME')

		#--( Header information )--#
		self.oxes.write("<?xml version=\"1.0\" encoding=\"utf-8\"?>\n")
		self.oxes.write("<oxes xmlns=\"http://www.wycliffe.net/scripture/namespace/version_1.1.2\">\n")
		self.oxes.write('\t' + '<oxesText type=\"Wycliffe-1.1.2\" oxesIDWork=\"WBT.grc\" xml:lang=\"grc\" canonical=\"true\">\n')
		self.oxes.write('\t' + '\t' + "<header>\n")
		self.oxes.write('\t' + '\t' + '\t' + "<revisionDesc resp=\"mil\">\n")
		self.oxes.write('\t' + '\t' + '\t' + '\t' + "<date>" + today.strftime("%y.%m.%d") + "</date>\n")
		self.oxes.write('\t' + '\t' + '\t' + '\t' + "<para xml:lang=\"en\">Converted by " + computername + " on " + today.strftime("%d %B %Y at %I %M %p") + "</para>\n")
		self.oxes.write('\t' + '\t' + '\t' + "</revisionDesc>\n")
		self.oxes.write('\t' + '\t' + '\t' + "<work oxesWork=\"WBT.grc\">\n")
		self.oxes.write('\t' + '\t' + '\t' + '\t' + "<titleGroup>\n")
		self.oxes.write('\t' + '\t' + '\t' + '\t' + '\t' + "<title type=\"main\">\n")
		self.oxes.write('\t' + '\t' + '\t' + '\t' + '\t' + '\t' + "<trGroup>\n")
		self.oxes.write('\t' + '\t' + '\t' + '\t' + '\t' + '\t' + '\t' + "<tr>The New Testament in the original Greek</tr>\n")
		self.oxes.write('\t' + '\t' + '\t' + '\t' + '\t' + '\t' + "</trGroup>\n")
		self.oxes.write('\t' + '\t' + '\t' + '\t' + '\t' + "</title>\n")
		self.oxes.write('\t' + '\t' + '\t' + '\t' + "</titleGroup>\n")
		self.oxes.write('\t' + '\t' + '\t' + '\t' + '<contributor role=\"Author\" ID=\"wfb\">Brooke Foss Westcott, D.D.</contributor>\n')
		self.oxes.write('\t' + '\t' + '\t' + '\t' + '<contributor role=\"Author\" ID=\"fjah\">Fenton John Anthony Hort, D.D.</contributor>\n')
		self.oxes.write('\t' + '\t' + '\t' + '\t' + '<contributor role=\"Publisher\" ID=\"hb\">Harper &amp; Brothers, Franklin Square, New York</contributor>\n')
		# TODO: The Creative Common attribution probably ought to go in <rightsLong>
		self.oxes.write('\t' + '\t' + '\t' + '\t' + '<contributor role=\"Compiler\" ID=\"pdl\">Perseus Digital Library. This work is distributed and licensed under the Creative Commons Attribution-Noncommercial-Share Alike 3.0 United States License. http://creativecommons.org/licenses/by-nc-sa/3.0/us/</contributor>\n')
		self.oxes.write('\t' + '\t' + '\t' + '\t' + '<contributor role=\"Programmer\" ID=\"sil\">SIL International</contributor>\n')
		self.oxes.write('\t' + '\t' + '\t' + "</work>\n")

##        TODO: Apparently rightsLong hasn't been developed in TE yet.
##        self.oxes.write('\t' + '\t' + '\t' + '<rightsLong>\n')
##        self.oxes.write('\t' + '\t' + '\t' + '\t' + '<trGroup>\n')
##        self.oxes.write('\t' + '\t' + '\t' + '\t' + '\t' + '<tr>')
##        self.oxes.write('This work is licensed under a Creative Commons Attribution-Noncommercial-Share Alike 3.0 United States License. http://creativecommons.org/licenses/by-nc-sa/3.0/us/')
##        self.oxes.write('</tr>\n')
##        self.oxes.write('\t' + '\t' + '\t' + '\t' + '</trGroup>\n')
##        self.oxes.write('\t' + '\t' + '\t' + '</rightsLong>\n')

		self.oxes.write('\t' + '\t' + "</header>\n")

		#--( Title Page )--#
		self.oxes.write('\t' + '\t' + "<titlePage>\n")
		self.oxes.write('\t' + '\t' + '\t' + "<titleGroup>\n")
		self.oxes.write('\t' + '\t' + '\t' + '\t' + "<title type=\"main\">\n")
		self.oxes.write('\t' + '\t' + '\t' + '\t' + '\t' + "<trGroup>\n")
		self.oxes.write('\t' + '\t' + '\t' + '\t' + '\t' + '\t' + "<tr>The New Testament in the original Greek</tr>\n")
		self.oxes.write('\t' + '\t' + '\t' + '\t' + '\t' + "</trGroup>\n")
		self.oxes.write('\t' + '\t' + '\t' + '\t' + "</title>\n")
		self.oxes.write('\t' + '\t' + '\t' + "</titleGroup>\n")
		self.oxes.write('\t' + '\t' + "</titlePage>\n")

		#--( Start the Canon Section )--#
		self.oxes.write('\t' + '\t' + "<canon ID=\"nt\">\n")

	#------------------------------------------------------------------------
	# Method: write_file_header
	# Summary: Write out the block for the book (of the Bible) title
	#------------------------------------------------------------------------

	def write_book_start(self):
		if self.close_book:
			self.write_book_end()

		self.oxes.write('\t' + '\t' + '\t' + '<book ID="' + self.book_abbreviation + '">\n')
		self.oxes.write('\t' + '\t' + '\t' + '\t' + '<titleGroup short="' + self.book_english + '">\n')
		self.oxes.write('\t' + '\t' + '\t' + '\t' + '\t' + '<title type="main">\n')
		self.oxes.write('\t' + '\t' + '\t' + '\t' + '\t' + '\t' + '<trGroup>\n')

		self.oxes.write('\t' + '\t' + '\t' + '\t' + '\t' + '\t' + '\t' + '<tr>')
		self.write_greek_word(self.book_title)
		self.oxes.write('</tr>\n')

		self.oxes.write('\t' + '\t' + '\t' + '\t' + '\t' + '\t' + '</trGroup>\n')
		self.oxes.write('\t' + '\t' + '\t' + '\t' + '\t' + '</title>\n')
		self.oxes.write('\t' + '\t' + '\t' + '\t' + '</titleGroup>\n')
		self.book_title = ''
		self.close_book = True

		self.write_section_start()

	def write_section_start(self):
		if self.close_section:
			self.write_section_end()

		self.oxes.write('\t' + '\t' + '\t' + '\t'+ '<section>\n')

		#( If we had section headers, this might become a different method.
		self.oxes.write('\t' + '\t' + '\t' + '\t' + '\t' + '<sectionHead>\n')
		self.oxes.write('\t' + '\t' + '\t' + '\t' + '\t' + '\t' + '<trGroup>\n')
		self.oxes.write('\t' + '\t' + '\t' + '\t' + '\t' + '\t' + '\t' + '<tr>')
		self.oxes.write(' ')
		self.oxes.write('</tr>\n')
		self.oxes.write('\t' + '\t' + '\t' + '\t' + '\t' + '\t' + '</trGroup>\n')
		self.oxes.write('\t' + '\t' + '\t' + '\t' + '\t' + '</sectionHead>\n')

		self.close_section = True

	def write_paragraph_start(self):
		if self.close_greek:
			self.write_greek_line_end()
		if self.close_paragraph:
			self.write_paragraph_end()

		self.oxes.write('\t' + '\t' + '\t' + '\t' + '\t' + '<p>\n')
		self.close_paragraph = True
		self.start_paragraph = False

	def write_chapter_start(self):
		if self.close_verse:
			self.write_verse_end()
		if self.close_chapter:
			self.write_chapter_end()

		self.oxes.write('\t' + '\t' + '\t' + '\t' + '\t' + '\t' + '<chapterStart ID="' + self.book_abbreviation +
						'.' + self.chapter + '" n="' + self.chapter + '" />\n')
		self.start_chapter = False
		self.close_chapter = True

	def write_verse_start(self):
		if (self.close_verse):
			self.write_verse_end()

		self.oxes.write('\t' + '\t' + '\t' + '\t' + '\t' + '\t' + '<verseStart ID="' + self.book_abbreviation +
						'.' + self.chapter + '.' + self.verse + '" n="' + self.verse + '" />\n')
		self.start_verse = False
		self.close_verse = True

	def write_citation1_start(self):
		if self.close_paragraph:
			self.write_paragraph_end()
		self.start_paragraph = False
		self.oxes.write('\t' + '\t' + '\t' + '\t' + '\t' + '<l level="1" type="citation">\n')

	def write_citation2_start(self):
		self.start_paragraph = False
		self.oxes.write('\t' + '\t' + '\t' + '\t' + '\t' + '<l level="2" type="citation">\n')

	def write_trgroup_start(self):
		self.oxes.write('\t' +  '\t' '\t' + '\t' + '\t' + '\t' + '<trGroup>\n')

	def write_quote_start(self):
		self.quote_level += 1
		if (self.quote_level % 1 == 0):
			self.oxes.write('\N{LEFT DOUBLE QUOTATION MARK}')
		else:
			self.oxes.write('\N{LEFT SINGLE QUOTATION MARK}')
		self.start_quote = False

	def write_greek_line_start(self):
		self.start_greek = False

		if self.start_paragraph:
			self.write_paragraph_start()
		if self.start_chapter:
			self.write_chapter_start()
		if self.start_verse:
			self.write_verse_start()
		self.write_trgroup_start()

		self.oxes.write('\t' + '\t' + '\t' + '\t' + '\t' + '\t' + '\t<tr>')
		self.close_greek = True

	def write_greek_line_end(self):
		self.oxes.write('</tr>\n')
		self.close_greek = False
		self.write_trgroup_end()

	def write_trgroup_end(self):
		self.oxes.write('\t' + '\t' + '\t'  '\t' + '\t' + '\t' + '</trGroup>\n')

	def write_citation_end(self):
		if (self.close_greek):
			self.write_greek_line_end()
		self.oxes.write('\t' + '\t' + '\t' + '\t' + '\t' + '</l>\n')
		self.start_paragraph = True

	def write_verse_end(self):
		verse = str(int(self.verse) - 1) #( self.verse will already be incremented for the next verse
		self.oxes.write('\t' + '\t' + '\t' + '\t' + '\t' + '\t' + '<verseEnd ID="' + self.book_abbreviation +
			'.' + self.chapter + '.' + verse + '" />\n')
		self.close_verse = False

	def write_chapter_end(self):
		chapter = str(int(self.chapter) - 1) #( self.verse will already be incremented for the next chapter
		self.oxes.write('\t' + '\t' + '\t' + '\t' + '\t' + '\t' + '<chapterEnd ID="' + self.book_abbreviation +
			'.' + chapter + '" />\n')
		self.close_chapter = False

	def write_paragraph_end(self):
		self.oxes.write('\t' + '\t' + '\t' + '\t' + '\t' + '</p>\n')
		self.close_paragraph = False

	def write_section_end(self):
		if self.close_paragraph:
			self.write_paragraph_end()
		self.oxes.write('\t' + '\t' + '\t' + '\t' + '</section>\n')
		self.close_section = False

	def write_book_end(self):
		if self.close_greek:
			self.write_greek_line_end()
		if self.close_paragraph:
			self.write_paragraph_end()
		if self.close_section:
			self.write_section_end()
		self.oxes.write('\t' + '\t' + '\t' + '</book>\n')
		self.close_book = False

	def write_quote_end(self):
		# TODO (Steve Miller): This doesn't put the end quote in the right place when </l> is followed by </quote>. See Mt. 1:23
		if (self.quote_level % 1 == 0):
			self.oxes.write('\N{RIGHT DOUBLE QUOTATION MARK}')
		else:
			self.oxes.write('\N{RIGHT SINGLE QUOTATION MARK}')
		self.quote_level -= 1
		self.close_quote = False

	def write_file_footer(self):
		if self.close_greek:
			self.write_greek_line_end()
		if self.close_paragraph:
			self.write_paragraph_end()
		if self.close_section:
			self.write_section_end()
		self.write_book_end()
		self.oxes.write('\t' + '\t' + '</canon>\n')
		self.oxes.write('\t' + '</oxesText>\n')
		self.oxes.write('</oxes>\n')

	#-------------------------------------------------------------------
	# Method: write_greek_word
	# Summary: One of the main engines of the program, this loops through
	#   each character of the word and processes it for writing
	#-------------------------------------------------------------------

	def write_greek_word(self, word):
		if self.start_quote:
			self.write_quote_start()

		char = ''
		go_write = False
		for c in word:
			if c in ' []':
				self.oxes.write(c)
			elif c == '*':
				self.is_upper = True
			elif self.is_diacritic(c):
				self.set_diacritic(c)
			else:
				if self.is_upper:
					self.write_greek_char(c, False)
				else:
					if go_write:
						self.write_greek_char(char, c == '.' or c == ',')
					char = c
					go_write = True
		if char != ' ':
			self.write_greek_char(char, True)

	def is_diacritic(self, char):
		return char in self.diacritic_markers

	def set_diacritic(self, char):
		if (char == ')'):
			self.add_psili = True
		elif (char == '('):
			self.add_dasia = True
		elif (char == '\\'):
			self.add_varia = True
		elif (char == '/'):
			self.add_oxia = True
		elif (char == '='):
			self.add_perispomeni = True
		elif (char == '|'):
			if (self.is_upper):
				self.add_prosgegrammeni = True
			else:
				self.add_ypogegrammeni = True
		elif (char == '+'):
			self.add_dialytika = True

	#-------------------------------------------------------------------
	# Method: write_greek_char
	# Summary: Writes a Greek letter with appropriate diacritics
	# Parameters:
	#   char = Roman character to tranliterate to Greek
	#   is_final_character = True if final character in word, for sigma
	#
	# Unicode:
	#   The Greek chart starts at 0370
	#   The Greek extended chart starts at 1F00
	#
	# Diacritics:
	#   psili = smooth breather, which can be combined with other diacritics
	#   dasia = rough breather, which can be combined with other diacritics
	#   varia = left-leaning accent mark
	#   oxia = right-leaning accent mark
	#   perispomeni = eh...
	#   add_dialytika = double dot above
	#
	# Sample sentence from Mt. 1:1:
	#   *b*i*b*l*o*s gene/sews *)ihsou= *xristou= ui(ou= *dauei\d ui(ou= *)abraam.
	#       -- The * designates a capitol letter is following in *b*i*b*l*o*s
	#       -- The / designates an accent mark for the letter before in gene/sews
	#       -- The ) designates a smooth breather (psili) for the letter following in *)ihsou=
	#       And so forth. I think the Perseus code replaces an * with a capitol form of the small
	#       letter that follows the *, then applies the accents and breathers the program encounters
	#       following the small letter.
	#-------------------------------------------------------------------

	def write_greek_char(self, char, is_final_character):

		if (self.is_upper):
			c = char.upper()
		else:
			c = char.lower()

		if c == ' ':
			self.oxes.write(' ')
		elif (self.is_upper):
			if (c == 'A'):
				if (self.add_prosgegrammeni):
					if (self.add_psili):
						if (self.add_varia):
							self.oxes.write('\N{GREEK CAPITAL LETTER ALPHA WITH PSILI AND VARIA AND PROSGEGRAMMENI}')
						elif (self.add_oxia):
							self.oxes.write('\N{GREEK CAPITAL LETTER ALPHA WITH PSILI AND OXIA AND PROSGEGRAMMENI}')
						elif (self.add_perispomeni):
							self.oxes.write('\N{GREEK CAPITAL LETTER ALPHA WITH PSILI AND PERISPOMENI AND PROSGEGRAMMENI}')
						else:
							self.oxes.write('\N{GREEK CAPITAL LETTER ALPHA WITH PSILI AND PROSGEGRAMMENI}')
					elif (self.add_dasia):
						if (self.add_varia):
							self.oxes.write('\N{GREEK CAPITAL LETTER ALPHA WITH DASIA AND VARIA AND PROSGEGRAMMENI}')
						elif (self.add_oxia):
							self.oxes.write('\N{GREEK CAPITAL LETTER ALPHA WITH DASIA AND OXIA AND PROSGEGRAMMENI}')
						elif (self.add_perispomeni):
							self.oxes.write('\N{GREEK CAPITAL LETTER ALPHA WITH DASIA AND PERISPOMENI AND PROSGEGRAMMENI}')
						else:
							self.oxes.write('\N{GREEK CAPITAL LETTER ALPHA WITH DASIA AND PROSGEGRAMMENI}')
					else:
						self.oxes.write('\N{GREEK CAPITAL LETTER ALPHA WITH PROSGEGRAMMENI}')
				else:
					if (self.add_psili):
						if (self.add_varia):
							self.oxes.write('\N{GREEK CAPITAL LETTER ALPHA WITH PSILI AND VARIA}')
						elif (self.add_oxia):
							self.oxes.write('\N{GREEK CAPITAL LETTER ALPHA WITH PSILI AND OXIA}')
						elif (self.add_perispomeni):
							self.oxes.write('\N{GREEK CAPITAL LETTER ALPHA WITH PSILI AND PERISPOMENI}')
						else:
							self.oxes.write('\N{GREEK CAPITAL LETTER ALPHA WITH PSILI}')
					elif (self.add_dasia):
						if (self.add_varia):
							self.oxes.write('\N{GREEK CAPITAL LETTER ALPHA WITH DASIA AND VARIA}')
						elif (self.add_oxia):
							self.oxes.write('\N{GREEK CAPITAL LETTER ALPHA WITH DASIA AND OXIA}')
						elif (self.add_perispomeni):
							self.oxes.write('\N{GREEK CAPITAL LETTER ALPHA WITH DASIA AND PERISPOMENI}')
						else:
							self.oxes.write('\N{GREEK CAPITAL LETTER ALPHA WITH DASIA}')
					else:
						self.oxes.write('\N{GREEK CAPITAL LETTER ALPHA}')
			elif (c == 'B'):
				self.oxes.write('\N{GREEK CAPITAL LETTER BETA}')
			elif (c == 'G'):
				self.oxes.write('\N{GREEK CAPITAL LETTER GAMMA}')
			elif (c == 'D'):
				self.oxes.write('\N{GREEK CAPITAL LETTER DELTA}')
			elif (c == 'E'):
				if (self.add_psili):
					if (self.add_varia):
						self.oxes.write('\N{GREEK CAPITAL LETTER EPSILON WITH PSILI AND VARIA}')
					elif (self.add_oxia):
						self.oxes.write('\N{GREEK CAPITAL LETTER EPSILON WITH PSILI AND OXIA}')
					else:
						self.oxes.write('\N{GREEK CAPITAL LETTER EPSILON WITH PSILI}')
				elif (self.add_dasia):
					if (self.add_varia):
						self.oxes.write('\N{GREEK CAPITAL LETTER EPSILON WITH DASIA AND VARIA}')
					elif (self.add_oxia):
						self.oxes.write('\N{GREEK CAPITAL LETTER EPSILON WITH DASIA AND OXIA}')
					else:
						self.oxes.write('\N{GREEK CAPITAL LETTER EPSILON WITH DASIA}')
				else:
					self.oxes.write('\N{GREEK CAPITAL LETTER EPSILON}')
			elif (c == 'Z'):
				self.oxes.write('\N{GREEK CAPITAL LETTER ZETA}')
			elif (c == 'H'):
				if (self.add_prosgegrammeni):
					if (self.add_psili):
						if (self.add_varia):
							self.oxes.write('\N{GREEK CAPITAL LETTER ETA WITH PSILI AND VARIA AND PROSGEGRAMMENI}')
						elif (self.add_oxia):
							self.oxes.write('\N{GREEK CAPITAL LETTER ETA WITH PSILI AND OXIA AND PROSGEGRAMMENI}')
						elif (self.add_perispomeni):
							self.oxes.write('\N{GREEK CAPITAL LETTER ETA WITH PSILI AND PERISPOMENI AND PROSGEGRAMMENI}')
						else:
							self.oxes.write('\N{GREEK CAPITAL LETTER ETA WITH PSILI AND PROSGEGRAMMENI}')
					elif (self.add_dasia):
						if (self.add_varia):
							self.oxes.write('\N{GREEK CAPITAL LETTER ETA WITH DASIA AND VARIA AND PROSGEGRAMMENI}')
						elif (self.add_oxia):
							self.oxes.write('\N{GREEK CAPITAL LETTER ETA WITH DASIA AND OXIA AND PROSGEGRAMMENI}')
						elif (self.add_perispomeni):
							self.oxes.write('\N{GREEK CAPITAL LETTER ETA WITH DASIA AND PERISPOMENI AND PROSGEGRAMMENI}')
						else:
							self.oxes.write('\N{GREEK CAPITAL LETTER ETA WITH DASIA AND PROSGEGRAMMENI}')
					else:
						self.oxes.write('\N{GREEK CAPITAL LETTER ETA WITH PROSGEGRAMMENI}')
				else:
					if (self.add_psili):
						if (self.add_varia):
							self.oxes.write('\N{GREEK CAPITAL LETTER ETA WITH PSILI AND VARIA}')
						elif (self.add_oxia):
							self.oxes.write('\N{GREEK CAPITAL LETTER ETA WITH PSILI AND OXIA}')
						elif (self.add_perispomeni):
							self.oxes.write('\N{GREEK CAPITAL LETTER ETA WITH PSILI AND PERISPOMENI}')
						else:
							self.oxes.write('\N{GREEK CAPITAL LETTER ETA WITH PSILI}')
					elif (self.add_dasia):
						if (self.add_varia):
							self.oxes.write('\N{GREEK CAPITAL LETTER ETA WITH DASIA AND VARIA}')
						elif (self.add_oxia):
							self.oxes.write('\N{GREEK CAPITAL LETTER ETA WITH DASIA AND OXIA}')
						elif (self.add_perispomeni):
							self.oxes.write('\N{GREEK CAPITAL LETTER ETA WITH DASIA AND PERISPOMENI}')
						else:
							self.oxes.write('\N{GREEK CAPITAL LETTER ETA WITH DASIA}')
					else:
						self.oxes.write('\N{GREEK CAPITAL LETTER ETA}')
			elif (c == 'Q'):
				self.oxes.write('\N{GREEK CAPITAL LETTER THETA}')
			elif (c == 'I'):
				if (self.add_psili):
					if (self.add_varia):
						self.oxes.write('\N{GREEK CAPITAL LETTER IOTA WITH PSILI AND VARIA}')
					elif (self.add_oxia):
						self.oxes.write('\N{GREEK CAPITAL LETTER IOTA WITH PSILI AND OXIA}')
					else:
						self.oxes.write('\N{GREEK CAPITAL LETTER IOTA WITH PSILI}')
				elif (self.add_dasia):
					if (self.add_varia):
						self.oxes.write('\N{GREEK CAPITAL LETTER IOTA WITH DASIA AND VARIA}')
					elif (self.add_oxia):
						self.oxes.write('\N{GREEK CAPITAL LETTER IOTA WITH DASIA AND OXIA}')
					else:
						self.oxes.write('\N{GREEK CAPITAL LETTER IOTA WITH DASIA}')
				else:
					if (self.add_dialytika):
						self.oxes.write('\N{GREEK CAPITAL LETTER IOTA WITH DIALYTIKA}')
					else:
						self.oxes.write('\N{GREEK CAPITAL LETTER IOTA}')
			elif (c == 'K'):
				self.oxes.write('\N{GREEK CAPITAL LETTER KAPPA}')
			elif (c == 'L'):
				self.oxes.write('\N{GREEK CAPITAL LETTER LAMDA}')
			elif (c == 'M'):
				self.oxes.write('\N{GREEK CAPITAL LETTER MU}')
			elif (c == 'N'):
				self.oxes.write('\N{GREEK CAPITAL LETTER NU}')
			elif (c == 'C'):
				self.oxes.write('\N{GREEK CAPITAL LETTER XI}')
			elif (c == 'O'):
				if (self.add_psili):
					if (self.add_varia):
						self.oxes.write('\N{GREEK CAPITAL LETTER OMICRON WITH PSILI AND VARIA}')
					elif (self.add_oxia):
						self.oxes.write('\N{GREEK CAPITAL LETTER OMICRON WITH PSILI AND OXIA}')
					else:
						self.oxes.write('\N{GREEK CAPITAL LETTER OMICRON WITH PSILI}')
				elif (self.add_dasia):
					if (self.add_varia):
						self.oxes.write('\N{GREEK CAPITAL LETTER OMICRON WITH DASIA AND VARIA}')
					elif (self.add_oxia):
						self.oxes.write('\N{GREEK CAPITAL LETTER OMICRON WITH DASIA AND OXIA}')
					else:
						self.oxes.write('\N{GREEK CAPITAL LETTER OMICRON WITH DASIA}')
				else:
					self.oxes.write('\N{GREEK CAPITAL LETTER OMICRON}')
			elif (c == 'P'):
				self.oxes.write('\N{GREEK CAPITAL LETTER PI}')
			elif (c == 'R'):
				if (self.add_dasia):
					self.oxes.write('\N{GREEK CAPITAL LETTER RHO WITH DASIA}')
				else:
					self.oxes.write('\N{GREEK CAPITAL LETTER RHO}')
			elif (c == 'S'):
				self.oxes.write('\N{GREEK CAPITAL LETTER SIGMA}')
			elif (c == 'T'):
				self.oxes.write('\N{GREEK CAPITAL LETTER TAU}')
			elif (c == 'U'):
				if (self.add_psili):
					if (self.add_varia):
						self.oxes.write('\N{GREEK CAPITAL LETTER EPSILON WITH PSILI AND VARIA}')
					elif (self.add_oxia):
						self.oxes.write('\N{GREEK CAPITAL LETTER EPSILON WITH PSILI AND OXIA}')
					else:
						self.oxes.write('\N{GREEK CAPITAL LETTER EPSILON WITH PSILI}')
				elif (self.add_dasia):
					if (self.add_varia):
						self.oxes.write('\N{GREEK CAPITAL LETTER UPSILON WITH DASIA AND VARIA}')
					elif (self.add_oxia):
						self.oxes.write('\N{GREEK CAPITAL LETTER UPSILON WITH DASIA AND OXIA}')
					else:
						self.oxes.write('\N{GREEK CAPITAL LETTER UPSILON WITH DASIA}')
				else:
					if (self.add_dialytika):
						self.oxes.write('\N{GREEK CAPITAL LETTER UPSILON WITH DIALYTIKA}')
					else:
						self.oxes.write('\N{GREEK CAPITAL LETTER UPSILON}')
			elif (c == 'F'):
				self.oxes.write('\N{GREEK CAPITAL LETTER PHI}')
			elif (c == 'X'):
				self.oxes.write('\N{GREEK CAPITAL LETTER CHI}')
			elif (c == 'Y'):
				self.oxes.write('\N{GREEK CAPITAL LETTER PSI}')
			elif (c == 'W'):
				if (self.add_prosgegrammeni):
					if (self.add_psili):
						if (self.add_varia):
							self.oxes.write('\N{GREEK CAPITAL LETTER OMEGA WITH PSILI AND VARIA AND PROSGEGRAMMENI}')
						elif (self.add_oxia):
							self.oxes.write('\N{GREEK CAPITAL LETTER OMEGA WITH PSILI AND OXIA AND PROSGEGRAMMENI}')
						elif (self.add_perispomeni):
							self.oxes.write('\N{GREEK CAPITAL LETTER OMEGA WITH PSILI AND PERISPOMENI AND PROSGEGRAMMENI}')
						else:
							self.oxes.write('\N{GREEK CAPITAL LETTER OMEGA WITH PSILI AND PROSGEGRAMMENI}')
					elif (self.add_dasia):
						if (self.add_varia):
							self.oxes.write('\N{GREEK CAPITAL LETTER OMEGA WITH DASIA AND VARIA AND PROSGEGRAMMENI}')
						elif (self.add_oxia):
							self.oxes.write('\N{GREEK CAPITAL LETTER OMEGA WITH DASIA AND OXIA AND PROSGEGRAMMENI}')
						elif (self.add_perispomeni):
							self.oxes.write('\N{GREEK CAPITAL LETTER OMEGA WITH DASIA AND PERISPOMENI AND PROSGEGRAMMENI}')
						else:
							self.oxes.write('\N{GREEK CAPITAL LETTER OMEGA WITH DASIA AND PROSGEGRAMMENI}')
					else:
						self.oxes.write('\N{GREEK CAPITAL LETTER OMEGA WITH PROSGEGRAMMENI}')
				else:
					if (self.add_psili):
						if (self.add_varia):
							self.oxes.write('\N{GREEK CAPITAL LETTER OMEGA WITH PSILI AND VARIA}')
						elif (self.add_oxia):
							self.oxes.write('\N{GREEK CAPITAL LETTER OMEGA WITH PSILI AND OXIA}')
						elif (self.add_perispomeni):
							self.oxes.write('\N{GREEK CAPITAL LETTER OMEGA WITH PSILI AND PERISPOMENI}')
						else:
							self.oxes.write('\N{GREEK CAPITAL LETTER OMEGA WITH PSILI}')
					elif (self.add_dasia):
						if (self.add_varia):
							self.oxes.write('\N{GREEK CAPITAL LETTER OMEGA WITH DASIA AND VARIA}')
						elif (self.add_oxia):
							self.oxes.write('\N{GREEK CAPITAL LETTER OMEGA WITH DASIA AND OXIA}')
						elif (self.add_perispomeni):
							self.oxes.write('\N{GREEK CAPITAL LETTER OMEGA WITH DASIA AND PERISPOMENI}')
						else:
							self.oxes.write('\N{GREEK CAPITAL LETTER OMEGA WITH DASIA}')
					else:
						self.oxes.write('\N{GREEK CAPITAL LETTER OMEGA}')
			else:
				self.oxes.write('\nCaptital letter: ' + c + '\n')

		#--( Small Letters )--#
		else: #( elif (not self.is_upper):
			if (c == 'a'):
				if (self.add_ypogegrammeni):
					if (self.add_psili):
						if (self.add_varia):
							self.oxes.write('\N{GREEK SMALL LETTER ALPHA WITH PSILI AND VARIA AND YPOGEGRAMMENI}')
						elif (self.add_oxia):
							self.oxes.write('\N{GREEK SMALL LETTER ALPHA WITH PSILI AND OXIA AND YPOGEGRAMMENI}')
						elif (self.add_perispomeni):
							self.oxes.write('\N{GREEK SMALL LETTER ALPHA WITH PSILI AND PERISPOMENI AND YPOGEGRAMMENI}')
						else:
							self.oxes.write('\N{GREEK SMALL LETTER ALPHA WITH PSILI AND YPOGEGRAMMENI}')
					elif (self.add_dasia):
						if (self.add_varia):
							self.oxes.write('\N{GREEK SMALL LETTER ALPHA WITH DASIA AND VARIA AND YPOGEGRAMMENI}')
						elif (self.add_oxia):
							self.oxes.write('\N{GREEK SMALL LETTER ALPHA WITH DASIA AND OXIA AND YPOGEGRAMMENI}')
						elif (self.add_perispomeni):
							self.oxes.write('\N{GREEK SMALL LETTER ALPHA WITH DASIA AND PERISPOMENI AND YPOGEGRAMMENI}')
						else:
							self.oxes.write('\N{GREEK SMALL LETTER ALPHA WITH DASIA AND YPOGEGRAMMENI}')
					else:
						if (self.add_varia):
							self.oxes.write('\N{GREEK SMALL LETTER ALPHA WITH VARIA AND YPOGEGRAMMENI}')
						elif (self.add_oxia):
							self.oxes.write('\N{GREEK SMALL LETTER ALPHA WITH OXIA AND YPOGEGRAMMENI}')
						elif (self.add_perispomeni):
							self.oxes.write('\N{GREEK SMALL LETTER ALPHA WITH PERISPOMENI AND YPOGEGRAMMENI}')
						else:
							self.oxes.write('\N{GREEK SMALL LETTER ALPHA WITH YPOGEGRAMMENI}')
				else:
					if (self.add_psili):
						if (self.add_varia):
							self.oxes.write('\N{GREEK SMALL LETTER ALPHA WITH PSILI AND VARIA}')
						elif (self.add_oxia):
							self.oxes.write('\N{GREEK SMALL LETTER ALPHA WITH PSILI AND OXIA}')
						elif (self.add_perispomeni):
							self.oxes.write('\N{GREEK SMALL LETTER ALPHA WITH PSILI AND PERISPOMENI}')
						else:
							self.oxes.write('\N{GREEK SMALL LETTER ALPHA WITH PSILI}')
					elif (self.add_dasia):
						if (self.add_varia):
							self.oxes.write('\N{GREEK SMALL LETTER ALPHA WITH DASIA AND VARIA}')
						elif (self.add_oxia):
							self.oxes.write('\N{GREEK SMALL LETTER ALPHA WITH DASIA AND OXIA}')
						elif (self.add_perispomeni):
							self.oxes.write('\N{GREEK SMALL LETTER ALPHA WITH DASIA AND PERISPOMENI}')
						else:
							self.oxes.write('\N{GREEK SMALL LETTER ALPHA WITH DASIA}')
					else:
						if (self.add_varia):
							self.oxes.write('\N{GREEK SMALL LETTER ALPHA WITH VARIA}')
						elif (self.add_oxia):
							self.oxes.write('\N{GREEK SMALL LETTER ALPHA WITH OXIA}')
						elif (self.add_perispomeni):
							self.oxes.write('\N{GREEK SMALL LETTER ALPHA WITH PERISPOMENI}')
						else:
							self.oxes.write('\N{GREEK SMALL LETTER ALPHA}')
			elif (c == 'b'):
				self.oxes.write('\N{GREEK SMALL LETTER BETA}')
			elif (c == 'g'):
				self.oxes.write('\N{GREEK SMALL LETTER GAMMA}')
			elif (c == 'd'):
				self.oxes.write('\N{GREEK SMALL LETTER DELTA}')
			elif (c == 'e'):
				if (self.add_psili):
					if (self.add_varia):
						self.oxes.write('\N{GREEK SMALL LETTER EPSILON WITH PSILI AND VARIA}')
					elif (self.add_oxia):
						self.oxes.write('\N{GREEK SMALL LETTER EPSILON WITH PSILI AND OXIA}')
					else:
						self.oxes.write('\N{GREEK SMALL LETTER EPSILON WITH PSILI}')
				elif (self.add_dasia):
					if (self.add_varia):
						self.oxes.write('\N{GREEK SMALL LETTER EPSILON WITH DASIA AND VARIA}')
					elif (self.add_oxia):
						self.oxes.write('\N{GREEK SMALL LETTER EPSILON WITH DASIA AND OXIA}')
					else:
						self.oxes.write('\N{GREEK SMALL LETTER EPSILON WITH DASIA}')
				else:
					if (self.add_varia):
						self.oxes.write('\N{GREEK SMALL LETTER EPSILON WITH VARIA}')
					elif (self.add_oxia):
						self.oxes.write('\N{GREEK SMALL LETTER EPSILON WITH OXIA}')
					else:
						self.oxes.write('\N{GREEK SMALL LETTER EPSILON}')
			elif (c == 'z'):
				self.oxes.write('\N{GREEK SMALL LETTER ZETA}')
			elif (c == 'h'):
				if (self.add_ypogegrammeni):
					if (self.add_psili):
						if (self.add_varia):
							self.oxes.write('\N{GREEK SMALL LETTER ETA WITH PSILI AND VARIA AND YPOGEGRAMMENI}')
						elif (self.add_oxia):
							self.oxes.write('\N{GREEK SMALL LETTER ETA WITH PSILI AND OXIA AND YPOGEGRAMMENI}')
						elif (self.add_perispomeni):
							self.oxes.write('\N{GREEK SMALL LETTER ETA WITH PSILI AND PERISPOMENI AND YPOGEGRAMMENI}')
						else:
							self.oxes.write('\N{GREEK SMALL LETTER ETA WITH PSILI AND YPOGEGRAMMENI}')
					elif (self.add_dasia):
						if (self.add_varia):
							self.oxes.write('\N{GREEK SMALL LETTER ETA WITH DASIA AND VARIA AND YPOGEGRAMMENI}')
						elif (self.add_oxia):
							self.oxes.write('\N{GREEK SMALL LETTER ETA WITH DASIA AND OXIA AND YPOGEGRAMMENI}')
						elif (self.add_perispomeni):
							self.oxes.write('\N{GREEK SMALL LETTER ETA WITH DASIA AND PERISPOMENI AND YPOGEGRAMMENI}')
						else:
							self.oxes.write('\N{GREEK SMALL LETTER ETA WITH DASIA AND YPOGEGRAMMENI}')
					else:
						if (self.add_varia):
							self.oxes.write('\N{GREEK SMALL LETTER ETA WITH VARIA AND YPOGEGRAMMENI}')
						elif (self.add_oxia):
							self.oxes.write('\N{GREEK SMALL LETTER ETA WITH OXIA AND YPOGEGRAMMENI}')
						elif (self.add_perispomeni):
							self.oxes.write('\N{GREEK SMALL LETTER ETA WITH PERISPOMENI AND YPOGEGRAMMENI}')
						else:
							self.oxes.write('\N{GREEK SMALL LETTER ETA WITH YPOGEGRAMMENI}')
				else:
					if (self.add_psili):
						if (self.add_varia):
							self.oxes.write('\N{GREEK SMALL LETTER ETA WITH PSILI AND VARIA}')
						elif (self.add_oxia):
							self.oxes.write('\N{GREEK SMALL LETTER ETA WITH PSILI AND OXIA}')
						elif (self.add_perispomeni):
							self.oxes.write('\N{GREEK SMALL LETTER ETA WITH PSILI AND PERISPOMENI}')
						else:
							self.oxes.write('\N{GREEK SMALL LETTER ETA WITH PSILI}')
					elif (self.add_dasia):
						if (self.add_varia):
							self.oxes.write('\N{GREEK SMALL LETTER ETA WITH DASIA AND VARIA}')
						elif (self.add_oxia):
							self.oxes.write('\N{GREEK SMALL LETTER ETA WITH DASIA AND OXIA}')
						elif (self.add_perispomeni):
							self.oxes.write('\N{GREEK SMALL LETTER ETA WITH DASIA AND PERISPOMENI}')
						else:
							self.oxes.write('\N{GREEK SMALL LETTER ETA WITH DASIA}')
					else:
						if (self.add_varia):
							self.oxes.write('\N{GREEK SMALL LETTER ETA WITH VARIA}')
						elif (self.add_oxia):
							self.oxes.write('\N{GREEK SMALL LETTER ETA WITH OXIA}')
						elif (self.add_perispomeni):
							self.oxes.write('\N{GREEK SMALL LETTER ETA WITH PERISPOMENI}')
						else:
							self.oxes.write('\N{GREEK SMALL LETTER ETA}')
			elif (c == 'q'):
				self.oxes.write('\N{GREEK SMALL LETTER THETA}')
			elif (c == 'i'):
				if (self.add_psili):
					if (self.add_varia):
						self.oxes.write('\N{GREEK SMALL LETTER IOTA WITH PSILI AND VARIA}')
					elif (self.add_oxia):
						self.oxes.write('\N{GREEK SMALL LETTER IOTA WITH PSILI AND OXIA}')
					elif (self.add_perispomeni):
						self.oxes.write('\N{GREEK SMALL LETTER IOTA WITH PSILI AND PERISPOMENI}')
					else:
						self.oxes.write('\N{GREEK SMALL LETTER IOTA WITH PSILI}')
				elif (self.add_dasia):
					if (self.add_varia):
						self.oxes.write('\N{GREEK SMALL LETTER IOTA WITH DASIA AND VARIA}')
					elif (self.add_oxia):
						self.oxes.write('\N{GREEK SMALL LETTER IOTA WITH DASIA AND OXIA}')
					elif (self.add_perispomeni):
						self.oxes.write('\N{GREEK SMALL LETTER IOTA WITH DASIA AND PERISPOMENI}')
					else:
						self.oxes.write('\N{GREEK SMALL LETTER IOTA WITH DASIA}')
				else:
					if (self.add_varia):
						self.oxes.write('\N{GREEK SMALL LETTER IOTA WITH VARIA}')
					elif (self.add_oxia):
						self.oxes.write('\N{GREEK SMALL LETTER IOTA WITH OXIA}')
					elif (self.add_perispomeni):
						if (self.add_dialytika):
							self.oxes.write('\N{GREEK SMALL LETTER IOTA WITH DIALYTIKA AND PERISPOMENI}')
						else:
							self.oxes.write('\N{GREEK SMALL LETTER IOTA WITH PERISPOMENI}')
					else:
						if (self.add_dialytika):
							self.oxes.write('\N{GREEK SMALL LETTER IOTA WITH DIALYTIKA}')
						else:
							self.oxes.write('\N{GREEK SMALL LETTER IOTA}')
			elif (c == 'k'):
				self.oxes.write('\N{GREEK SMALL LETTER KAPPA}')
			elif (c == 'l'):
				self.oxes.write('\N{GREEK SMALL LETTER LAMDA}')
			elif (c == 'm'):
				self.oxes.write('\N{GREEK SMALL LETTER MU}')
			elif (c == 'n'):
				self.oxes.write('\N{GREEK SMALL LETTER NU}')
			elif (c == 'c'):
				self.oxes.write('\N{GREEK SMALL LETTER XI}')
			elif (c == 'o'):
				if (self.add_psili):
					if (self.add_varia):
						self.oxes.write('\N{GREEK SMALL LETTER OMICRON WITH PSILI AND VARIA}')
					elif (self.add_oxia):
						self.oxes.write('\N{GREEK SMALL LETTER OMICRON WITH PSILI AND OXIA}')
					else:
						self.oxes.write('\N{GREEK SMALL LETTER OMICRON WITH PSILI}')
				elif (self.add_dasia):
					if (self.add_varia):
						self.oxes.write('\N{GREEK SMALL LETTER OMICRON WITH DASIA AND VARIA}')
					elif (self.add_oxia):
						self.oxes.write('\N{GREEK SMALL LETTER OMICRON WITH DASIA AND OXIA}')
					else:
						self.oxes.write('\N{GREEK SMALL LETTER OMICRON WITH DASIA}')
				else:
					if (self.add_varia):
						self.oxes.write('\N{GREEK SMALL LETTER OMICRON WITH VARIA}')
					elif (self.add_oxia):
						self.oxes.write('\N{GREEK SMALL LETTER OMICRON WITH OXIA}')
					else:
						self.oxes.write('\N{GREEK SMALL LETTER OMICRON}')
			elif (c == 'p'):
				self.oxes.write('\N{GREEK SMALL LETTER PI}')
			elif (c == 'r'):
				if (self.add_psili):
					self.oxes.write('\N{GREEK SMALL LETTER RHO WITH PSILI}')
				elif (self.add_dasia):
					self.oxes.write('\N{GREEK SMALL LETTER RHO WITH DASIA}')
				else:
					self.oxes.write('\N{GREEK SMALL LETTER RHO}')
			elif (c == 's'):
				if is_final_character:
					self.oxes.write('\N{GREEK SMALL LETTER FINAL SIGMA}')
				else:
					self.oxes.write('\N{GREEK SMALL LETTER SIGMA}')
			elif (c == 't'):
				self.oxes.write('\N{GREEK SMALL LETTER TAU}')
			elif (c == 'u'):
				if (self.add_psili):
					if (self.add_varia):
						self.oxes.write('\N{GREEK SMALL LETTER UPSILON WITH PSILI AND VARIA}')
					elif (self.add_oxia):
						self.oxes.write('\N{GREEK SMALL LETTER UPSILON WITH PSILI AND OXIA}')
					elif (self.add_perispomeni):
						self.oxes.write('\N{GREEK SMALL LETTER UPSILON WITH PSILI AND PERISPOMENI}')
					else:
						self.oxes.write('\N{GREEK SMALL LETTER UPSILON WITH PSILI}')
				elif (self.add_dasia):
					if (self.add_varia):
						self.oxes.write('\N{GREEK SMALL LETTER UPSILON WITH DASIA AND VARIA}')
					elif (self.add_oxia):
						self.oxes.write('\N{GREEK SMALL LETTER UPSILON WITH DASIA AND OXIA}')
					elif (self.add_perispomeni):
						self.oxes.write('\N{GREEK SMALL LETTER UPSILON WITH DASIA AND PERISPOMENI}')
					else:
						self.oxes.write('\N{GREEK SMALL LETTER UPSILON WITH DASIA}')
				else:
					if (self.add_varia):
						self.oxes.write('\N{GREEK SMALL LETTER UPSILON WITH VARIA}')
					elif (self.add_oxia):
						self.oxes.write('\N{GREEK SMALL LETTER UPSILON WITH OXIA}')
					elif (self.add_perispomeni):
						if (self.add_dialytika):
							self.oxes.write('\N{GREEK SMALL LETTER UPSILON WITH DIALYTIKA AND PERISPOMENI}')
						else:
							self.oxes.write('\N{GREEK SMALL LETTER UPSILON WITH PERISPOMENI}')
					else:
						if (self.add_dialytika):
							self.oxes.write('\N{GREEK SMALL LETTER UPSILON WITH DIALYTIKA}')
						else:
							self.oxes.write('\N{GREEK SMALL LETTER UPSILON}')
			elif (c == 'f'):
				self.oxes.write('\N{GREEK SMALL LETTER PHI}')
			elif (c == 'x'):
				self.oxes.write('\N{GREEK SMALL LETTER CHI}')
			elif (c == 'y'):
				self.oxes.write('\N{GREEK SMALL LETTER PSI}')
			elif (c == 'w'):
				if (self.add_ypogegrammeni):
					if (self.add_psili):
						if (self.add_varia):
							self.oxes.write('\N{GREEK SMALL LETTER OMEGA WITH PSILI AND VARIA AND YPOGEGRAMMENI}')
						elif (self.add_oxia):
							self.oxes.write('\N{GREEK SMALL LETTER OMEGA WITH PSILI AND OXIA AND YPOGEGRAMMENI}')
						elif (self.add_perispomeni):
							self.oxes.write('\N{GREEK SMALL LETTER OMEGA WITH PSILI AND PERISPOMENI AND YPOGEGRAMMENI}')
						else:
							self.oxes.write('\N{GREEK SMALL LETTER OMEGA WITH PSILI AND YPOGEGRAMMENI}')
					elif (self.add_dasia):
						if (self.add_varia):
							self.oxes.write('\N{GREEK SMALL LETTER OMEGA WITH DASIA AND VARIA AND YPOGEGRAMMENI}')
						elif (self.add_oxia):
							self.oxes.write('\N{GREEK SMALL LETTER OMEGA WITH DASIA AND OXIA AND YPOGEGRAMMENI}')
						elif (self.add_perispomeni):
							self.oxes.write('\N{GREEK SMALL LETTER OMEGA WITH DASIA AND PERISPOMENI AND YPOGEGRAMMENI}')
						else:
							self.oxes.write('\N{GREEK SMALL LETTER OMEGA WITH DASIA AND YPOGEGRAMMENI}')
					else:
						if (self.add_varia):
							self.oxes.write('\N{GREEK SMALL LETTER OMEGA WITH VARIA AND YPOGEGRAMMENI}')
						elif (self.add_oxia):
							self.oxes.write('\N{GREEK SMALL LETTER OMEGA WITH OXIA AND YPOGEGRAMMENI}')
						elif (self.add_perispomeni):
							self.oxes.write('\N{GREEK SMALL LETTER OMEGA WITH PERISPOMENI AND YPOGEGRAMMENI}')
						else:
							self.oxes.write('\N{GREEK SMALL LETTER OMEGA WITH YPOGEGRAMMENI}')
				else:
					if (self.add_psili):
						if (self.add_varia):
							self.oxes.write('\N{GREEK SMALL LETTER OMEGA WITH PSILI AND VARIA}')
						elif (self.add_oxia):
							self.oxes.write('\N{GREEK SMALL LETTER OMEGA WITH PSILI AND OXIA}')
						elif (self.add_perispomeni):
							self.oxes.write('\N{GREEK SMALL LETTER OMEGA WITH PSILI AND PERISPOMENI}')
						else:
							self.oxes.write('\N{GREEK SMALL LETTER OMEGA WITH PSILI}')
					elif (self.add_dasia):
						if (self.add_varia):
							self.oxes.write('\N{GREEK SMALL LETTER OMEGA WITH DASIA AND VARIA}')
						elif (self.add_oxia):
							self.oxes.write('\N{GREEK SMALL LETTER OMEGA WITH DASIA AND OXIA}')
						elif (self.add_perispomeni):
							self.oxes.write('\N{GREEK SMALL LETTER OMEGA WITH DASIA AND PERISPOMENI}')
						else:
							self.oxes.write('\N{GREEK SMALL LETTER OMEGA WITH DASIA}')
					else:
						if (self.add_varia):
							self.oxes.write('\N{GREEK SMALL LETTER OMEGA WITH VARIA}')
						elif (self.add_oxia):
							self.oxes.write('\N{GREEK SMALL LETTER OMEGA WITH OXIA}')
						elif (self.add_perispomeni):
							self.oxes.write('\N{GREEK SMALL LETTER OMEGA WITH PERISPOMENI}')
						else:
							self.oxes.write('\N{GREEK SMALL LETTER OMEGA}')
			elif (c == "'"):
				#( The Perseus library uses a Greek Koronis here, which is supposedly the
				#( diacritic merging two words into one. But if they're doing it, hey,
				#( who am I to say any different?
				self.oxes.write('\N{GREEK KORONIS}')
			elif (c == '&'):
				self.oxes.write('\N{EM DASH}')
			else:
				self.oxes.write(c)

		#( Reset diacritics
		self.is_upper = False
		self.add_psili = False #( smooth breather
		self.add_dasia = False #( rough breather
		self.add_varia = False #( an accent marker
		self.add_oxia = False #( another accent marker
		self.add_perispomeni = False #( circumflex
		self.add_ypogegrammeni = False #( subscript iota for small letters
		self.add_prosgegrammeni = False #( subscript iota for capitol letters
		self.add_dialytika = False #( double dot above

	def get_book_abbreviation(self, book_name):
		#( Codes came from ScrBookRef.xml
		if (book_name == "Matthew"):
			return "MAT"
		elif (book_name == "Mark"):
			return "MRK"
		elif (book_name == "Luke"):
			return "LUK"
		elif (book_name == "John"):
			return "JHN"
		elif (book_name == "Acts"):
			return "ACT"
		elif (book_name == "Romans"):
			return "ROM"
		elif (book_name == "I Corinthians"):
			return "1CO"
		elif (book_name == "II Corinthians"):
			return "2CO"
		elif (book_name == "Galatians"):
			return "GAL"
		elif (book_name == "Ephesians"):
			return "EPH"
		elif (book_name == "Philippians"):
			return "PHP"
		elif (book_name == "Colossians"):
			return "COL"
		elif (book_name == "I Thessalonians"):
			return "1TH"
		elif (book_name == "II Thessalonians"):
			return "2TH"
		elif (book_name == "I Timothy"):
			return "1TI"
		elif (book_name == "II Timothy"):
			return "2TI"
		elif (book_name == "Titus"):
			return "TIT"
		elif (book_name == "Philemon"):
			return "PHM"
		elif (book_name == "Hebrews"):
			return "HEB"
		elif (book_name == "James"):
			return "JAS"
		elif (book_name == "I Peter"):
			return "1PE"
		elif (book_name == "II Peter"):
			return "2PE"
		elif (book_name == "I John"):
			return "1JN"
		elif (book_name == "II John"):
			return "2JN"
		elif (book_name == "III John"):
			return "3JN"
		elif (book_name == "Jude"):
			return "JUD"
		elif (book_name == "Revelation"):
			return "REV"
		else:
			return book_name #( For typos or whatever

	def cleanup(self):
		self.perseus.close()
		self.oxes.close()

#==( Execute the conversion )==#

p2o = PerseusToOsis()
p2o.convert()
