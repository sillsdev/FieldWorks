#!/usr/bin/env python

import os
import Reporter
import time
#from ctypes import *
#user32 = windll.user32

#----------------------------------------------------------------------

class FieldWorks:

	#EnumWindowsProc = WINFUNCTYPE(c_int, c_int, c_int)
	#
	#def GetHandles(title, parent=None):
	#    # Returns handles to windows with matching titles
	#    hwnds = []
	#
	#    def EnumCB(hwnd, lparam, match=title.lower(), hwnds=hwnds):
	#        title = c_buffer(' ' * 256)
	#        user32.GetWindowTextA(hwnd, title, 255)
	#        if title.value.lower() == match:
	#            hwnds.append(hwnd)
	#
	#    if parent is not None:
	#        user32.EnumChildWindows(parent, EnumWindowsProc(EnumCB), 0)
	#    else:
	#        user32.EnumWindows(EnumWindowsProc(EnumCB), 0)
	#    return hwnds

	#----------------------------------------------------------------------

	def cleanupFiles(self):

		# Must delete the SetupFW file before we can copy over it
		if os.path.isfile('C:\\SetupFW.msi'):
			os.system('del C:\\SetupFW.msi')

		if os.path.isfile('C:\\Documents and Settings\\All Users\\Application Data\\SIL\\FieldWorks\\Data\\MALVI OTHERSF IMPORT TEST.bak'):
			os.system('del C:\\"Documents and Settings"\\"All Users"\\"Application Data"\\SIL\\FieldWorks\\Data\\"MALVI OTHERSF IMPORT TEST".bak')
		if os.path.isfile('C:\\Documents and Settings\\All Users\\Application Data\\SIL\\FieldWorks\\Data\\MALVI OTHERSF IMPORT TEST.mdf'):
			os.system('del C:\\"Documents and Settings"\\"All Users"\\"Application Data"\\SIL\\FieldWorks\\Data\\"MALVI OTHERSF IMPORT TEST".mdf')
		if os.path.isfile('C:\\Documents and Settings\\All Users\\Application Data\\SIL\\FieldWorks\\Data\\MALVI OTHERSF IMPORT TEST_log.ldf'):
			os.system('del C:\\"Documents and Settings"\\"All Users"\\"Application Data"\\SIL\\FieldWorks\\Data\\"MALVI OTHERSF IMPORT TEST_log".ldf')

		if os.path.isfile('C:\\Documents and Settings\\All Users\\Application Data\\SIL\\FieldWorks\\Data\\MALAY PARATEXT 5 IMPORT TEST.bak'):
			os.system('del C:\\"Documents and Settings"\\"All Users"\\"Application Data"\\SIL\\FieldWorks\\Data\\"MALAY PARATEXT 5 IMPORT TEST".bak')
		if os.path.isfile('C:\\Documents and Settings\\All Users\\Application Data\\SIL\\FieldWorks\\Data\\MALAY PARATEXT 5 IMPORT TEST.mdf'):
			os.system('del C:\\"Documents and Settings"\\"All Users"\\"Application Data"\\SIL\\FieldWorks\\Data\\"MALAY PARATEXT 5 IMPORT TEST".mdf')
		if os.path.isfile('C:\\Documents and Settings\\All Users\\Application Data\\SIL\\FieldWorks\\Data\\MALAY PARATEXT 5 IMPORT TEST_log.ldf'):
			os.system('del C:\\"Documents and Settings"\\"All Users"\\"Application Data"\\SIL\\FieldWorks\\Data\\"MALAY PARATEXT 5 IMPORT TEST_log".ldf')

		if os.path.isfile('C:\\Documents and Settings\\All Users\\Application Data\\SIL\\FieldWorks\\Data\\MALAY PARATEXT 6 IMPORT TEST.bak'):
			os.system('del C:\\"Documents and Settings"\\"All Users"\\"Application Data"\\SIL\\FieldWorks\\Data\\"MALAY PARATEXT 6 IMPORT TEST".bak')
		if os.path.isfile('C:\\Documents and Settings\\All Users\\Application Data\\SIL\\FieldWorks\\Data\\MALAY PARATEXT 6 IMPORT TEST.mdf'):
			os.system('del C:\\"Documents and Settings"\\"All Users"\\"Application Data"\\SIL\\FieldWorks\\Data\\"MALAY PARATEXT 6 IMPORT TEST".mdf')
		if os.path.isfile('C:\\Documents and Settings\\All Users\\Application Data\\SIL\\FieldWorks\\Data\\MALAY PARATEXT 6 IMPORT TEST_log.ldf'):
			os.system('del C:\\"Documents and Settings"\\"All Users"\\"Application Data"\\SIL\\FieldWorks\\Data\\"MALAY PARATEXT 6 IMPORT TEST_log".ldf')

	def reinstall(self):

		self.uninstall()

		Reporter.print_to_log_file('Deleting unwanted project files')
		self.cleanupFiles()

		self.install()

	def uninstall(self):

		Reporter.print_to_log_file('Uninstalling current FieldWorks')
		os.system('msiexec /x c:\setupfw.msi /qb')

	def install(self):

		Reporter.print_to_log_file('Getting the latest FieldWorks installer')
		os.system('copy \\\\jar-file\\SilLangSoft\\FW_6_0\\FieldWorks\\SetupFW.msi c:\\')

		Reporter.print_to_log_file('Installing latest FieldWorks')

		os.system('msiexec /i c:\setupfw.msi /qb')

		#for handle in GetHandles('SQL Failure'):
		#    for childHandle in GetHandles('ok', handle):
		#        user32.SendMessageA(childHandle, 0x00F5, 0, 0) # 0x00F5 = BM_CLICK

		os.system('path=%path%;C:\\"Program Files"\\"Common Files"\\SIL')

		Reporter.print_to_log_file('Finished installing FieldWorks')

	#----------------------------------------------------------------------
