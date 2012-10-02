#!/usr/bin/env python
import os
import os.path
import re
import sys

"""
This script extracts all of the Win32 dialog definitions from the file whose name is
provided as its first command line argument and also from all of the included .rc files.

(A proposed solution involved use of the C pre-processor to produce a single file containing
all of the dialog definitions. The command to achieve this is as follows:

	$ gcc -E -x c WorldPad.rc > all_dialogs.rc

Unfortunately, the human readable symbols identifying the dialogs, and used to name the output
files, were converted to relatively meaningless numeric values by the pre-processor.)
"""

class ResourceFile(object):
	"""
	Defines a .rc Resource File
	"""
	def __init__(self, infile):
		self.ifile = open(infile, 'rU')
		self.data = [line.rstrip() for line in self.ifile.readlines()]
		self.ifile.close()
		# Locate the dialogs defined in the .rc file
		self.locate_dialogs(self.ifile.name, self.data)
		self.iter = iter(self.dialogs)

	def __iter__(self):
		return self

	def next(self):
		try:
			return self.iter.next()
		except StopIteration:
			raise

	def locate_dialogs(self, filename, data):
		"""
		Locate all Win32 dialog definitions (start & end line) in the data lines supplied.
		"""
		self.dialogs = []
		dialog_found = False
		for i, line in enumerate(data):
			# TODO: Would match be better?
			decl = re.search(r"^krid(.*?)\sDIALOG|DIALOGEX\s", line)
			if decl:  # Does the current line mark the beginning of a dialog definition?
				dialog_found = True
				dialog_name = decl.group(1)
				print 'Dialog definition "%s" found in file: %s at line %d' % (dialog_name, os.path.basename(filename), i + 1)
				start = i
			elif dialog_found:  # Are we currently processing a dialog definition?
				# TODO: Would match be better?
				term = re.search(r"^\s?END\s?$", line)
				if term:  # Does the current line mark the end of a dialog definition?
					dialog_found = False
					print 'Dialog definition "%s" ended at line %d' % (dialog_name, i + 1)
					dialog = Dialog(self.data, start, i, dialog_name, filename)
					self.dialogs.append(dialog)


class Dialog(object):
	"""
	Defines an individual dialog in a .rc Resource File
	"""
	controls = [
			'CHECKBOX',
			'COMBOBOX',
			'CONTROL',
			'CTEXT',
			'DEFPUSHBUTTON',
			'EDITTEXT',
			'GROUPBOX',
			'ICON',
			'LISTBOX',
			'LTEXT',
			'PUSHBUTTON',
			'RTEXT',
			]

	def __init__(self, data, start, end, name, filename):
		self.name = name
		self.filename = filename
		self.statements = self.clean(data[start: end + 1])

	def clean(self, data):
		"""
		Removes blank lines and combines multi-line statements into a single line
		"""
		cntl_stmt = ''
		statements = []
		for line in data:
			# TODO: Would search be better?
			blank = re.match(r"^\s*$", line)
			if blank:  # Is the current line blank?
				continue
			# TODO: Would match be better?
			term = re.search(r"^\s?END\s?$", line)
			if term:  # Does the current line mark the end of a dialog definition?
				if cntl_stmt != '':
					statements.append(cntl_stmt)  # Copy a 'pending' statement
				statements.append(line)
			else:
				# TODO: Would search be better?
				cntl = re.match(r"(\s+([A-Z]+)\s+.+)\s*", line)
				if cntl and cntl.group(2) in Dialog.controls:  # Is this the first line defining a control
					if cntl_stmt != '':
						statements.append(cntl_stmt)  # Copy a 'pending' statement
					cntl_stmt = cntl.group(1)
				elif cntl_stmt != '':
					cont = re.match(r"\s+(([^\/\/])(.+))\s*", line)
					if cont:  # Is this a continuation of a control definition?
						if cntl_stmt.endswith(','):
							cntl_stmt = cntl_stmt + cont.group(1)
						else:
							cntl_stmt = cntl_stmt + ' ' + cont.group(1)
					else:  # A first/only comment line
						statements.append(cntl_stmt)  # Copy a 'pending' statement
						statements.append(line)
						cntl_stmt = ''
				else:  # A prolog statement (i.e. up to an including 'BEGIN') or
					   # second/subsequent comment line
					statements.append(line)
		return statements

	def write(self, outdir, comment):
		"""
		Writes out the entire dialog definition
		"""
		ofile = open(os.path.join(outdir, self.name + '.rc'), 'w')
		if comment:
			ofile.write('// Extracted from %s%s' % (os.path.basename(self.filename), os.linesep))
		for statement in self.statements:
			ofile.write('%s%s' % (statement, os.linesep))
		ofile.close()


def process_included_file(path, outdir):
	"""
	Process a 'child' .rc file.
	"""
	if os.path.exists(path):
		# Extract those dialogs defined in the included .rc file
		rf = ResourceFile(path)
		for dlg in rf:
			dlg.write(outdir, True)
	else:
		print 'Error: Included file "%s" does not exist!' % (path)

def main():
	# Validate command line arguments
	if len(sys.argv) < 2:
		print 'Error: Input filename argument is missing'
		sys.exit(1)
	infile = sys.argv[1]
	if not os.path.exists(infile):
		print 'Error: Input file does not exist'
		sys.exit(1)
	if not os.path.isfile(infile):
		print 'Error: Argument 1 must be a filename'
		sys.exit(1)
	if len(sys.argv) < 3:
		print 'Error: Output directory name argument is missing'
		sys.exit(1)
	outdir = sys.argv[2]
	if not os.path.exists(outdir):
		try:
			os.makedirs(outdir)
		except Exception, e:
			print 'Error: Unable to create output directory [%s]' % (e)
			sys.exit(1)

	rf = ResourceFile(infile)
	for dlg in rf:
		dlg.write(outdir, True)

	# TODO: Avoid reading entire file twice! (see ResourceFile.__init__())
	ifile = open(infile, 'rU')
	data = [line.rstrip() for line in ifile.readlines()]
	ifile.close()

	# Ensure output directory is an absolute path before changing the current directory
	# to that of the 'parent' .rc file (to simplify resolving of #include statement paths)
	if not os.path.isabs(outdir):
		outdir = os.path.abspath(outdir)
	if not os.path.isabs(infile):
		infile = os.path.abspath(infile)
	os.chdir(os.path.dirname(infile))

	# Search for all #include statements that refer to additional .rc files
	for line in data:
		# TODO: Would search be better?
		incl = re.match(r"^#include\s+\"(.*\.rc)\".*", line)
		if incl:
			process_included_file(incl.group(1).replace('\\', os.sep), outdir)

if __name__ == '__main__':
	main()
