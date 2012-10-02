'Filename: FWTesting.inc

'$INCLUDE 'RECORDER.INC'

'$include 'AfAppRes.inc'


'==================================================================================
'Constants
'==================================================================================

'Most control id constants come from resource include files in the C++ source code,
'but these ones don't seem to be found there.

const FWIDOKButton& = 1
const FWIDCancelButton& = 2

const FWIDMsgBoxOKButton& = 2
const FWIDMsgBoxStatic& = 65535
const FWIDMsgBoxYesButton& = 6   'Are these universal?
const FWIDMsgBoxNoButton& = 7

const FWIDMenuToolbar& = 31500

'Other constants global to FieldWorks

const FWDialogClass$ = "#32770"  'Don't know where this comes from
const FWHelpClass$ = "HH Parent"
const FWHelpButtonClass$ = "AfButton"

const FWErrorDlgTitle$ = "Error"
const FWPSDlgTitle$ = "Page Setup"
const FWFFDlgTitle$ = "Font"
const FWHADlgTitle$ = "About SIL FieldWorks Data Notebook"

'This html file just loads an ActiveX object that has to be loaded in order
'to be able to verify what page a given HTMLHelp window is displaying.
const FWHHEmbeddingPage$ = "/test/vt/FWGlobal/embedded-ie.html" 'under $(FWROOT)
'Spec file for menus, generic to all FW applications
const FWMenuFile$ = "Test\VT\FWGlobal\menus-generic.txt"

Global TimeoutMsec As Long  'to go with Global Timeout (seconds)


'==================================================================================
'Subs & Functions
'==================================================================================

'Perform initializations for testing FW apps
Sub FWInit()
	Timeout = 5
	TimeoutMsec = Timeout * 1000
	SetDefaultWaitTimeout(Timeout)
End Sub


Sub FWVerifyHelp(sHHTitle as string, sPageTitle as string)
	'Test: press Help button on current dialog;
	' should open Help window to right place.
	'The Help button is a custom control, so we must treat it specially.
	WButtonSetClass(FWHelpButtonClass)
	WButtonClick(_id(kctidHelp))
	WButtonSetClass("Button")
	WaitUntilIdle(TimeoutMsec)

	dim hwnd as long

	'Find HTMLHelp window
	hwnd = WFndWndC("+" + sHHTitle, FWHelpClass, FW_FOCUS, Timeout)
	if hwnd < 1 then fail "Can't find Help window."

	'We have to prepare the embedded browser so it can interact with VT.
	'Do this by visiting a supplied HTML page.
	WSysMenu(hwnd)
	WMenuSelect("&Jump to URL...")
	WaitUntilIdle(TimeoutMsec)

	dim fwroot as string, filepath as string
	fwroot = ENVIRON("FWROOT")
	filepath = fwroot + FWHHEmbeddingPage
	if fwroot = "" or not exists(filepath) then
		filepath = "c:/temp" + FWHHEmbeddingPage
		if not exists(filepath) then
			fail "Required file $(FWROOT)"; FWHHEmbeddingPage; " not found."
		end if
	end if

	WEditSetText(_id(&h6f), "file:///" + filepath)
	WButtonClick(_id(1)) 'press OK
	WaitUntilIdle(TimeoutMsec)

	hwnd = WFndWndC("+" + sHHTitle, FWHelpClass, FW_FOCUS, Timeout)
	if hwnd < 1 then fail "Can't find Help window."
	WToolbarButtonClk(_id(&h03ee), "Back") 'click the Back button

	''LogBrowserTitles

	dim sBC as string 'browser context
	dim sFC as string 'frame context

	sBC = WebFndBrowser(sPageTitle, FWEB_ACTIVATE)
	if sBC = "0" or sBC = "" then
		fail "Couldn't find embedded help browser titled """; sPageTitle; """"
	end if

End Sub


'Press keystrokes "key" and verify that focus goes to control "ID".
'Careful, this will invoke command buttons and radio buttons.

Sub FWTestKbdShortcut(key as string, id as long)
	Dim info As INFO
	dim hwnd as long
	dim s as string

	'Verify that pressing Alt+Key sets focus to control ID
	'key should be in lower case (or we could convert it)

	Play "%(" + key + ")"
	WaitUntilIdle(TimeoutMsec) 'shouldn't take any time really

	WGetInfo(WGetFocus(), info) 'Find the id of the ctrl that now has focus

	if info.id <> id then
		Fail "After Alt+"; key; ", control id "; info.id; " had focus instead of "; id
	end if

	if info.class = "Edit" then
		s = _hwnd(info.hwnd)
		if WEditSelStart(s) <> 1 or WEditSelLen(s) <> len(info.Text) then
			Fail "Control id "; info.id; " didn't select all text when it received focus."
		end if
	end if

End Sub


'Test tab sequence.  Assumes that the last control is an Button (use WButtonSetClass first if nec)

Sub FWTestTabSeq (nCtrls as integer, ctrlIDs() as long)
	Dim info As INFO
	dim i as integer
	dim s as string
	dim hwnd as long

	'Set focus to last control (must be a button)
	'Unfortunately I can't find a straightforward way to set focus to any control,
	' or even to a button specifically, by ID.  Hence the following kludge.
	hwnd = WButtonFind(_id(ctrlIDs(nCtrls)))
	if hwnd = 0 then fail "Couldn't find last control #"; ctrlIDs(nCtrls)
	WGetInfo(hwnd, info)
	WButtonSetFocus(info.Text)

	'Verify that pressing Tab repeatedly cycles through all
	'controls in order.

	for i = 1 to nCtrls
		Play "{Tab}"
		WaitUntilIdle(TimeoutMsec) 'shouldn't take any time really

		WGetInfo(WGetFocus(), info) 'Find the id of the ctrl that now has focus

		if info.id <> ctrlIDs(i) then
			Fail i; "th control in tab sequence has id "; info.id; " instead of "; ctrlIDs(i)
		end if

		if info.class = "Edit" then
			s = _hwnd(info.hwnd)
			if WEditSelStart(s) <> 1 or WEditSelLen(s) <> len(info.Text) then
				Fail "Control id "; info.id; " didn't select all text when it received focus."
			end if
		end if

	next i

End Sub


'Return non-zero if str1 and str2 are two equivalent measurements, e.g. 1.0" and 1.00",
'2.50cm and 2.5cm, etc.

Function FWEquiv(s1 as string, s2 as string) as integer
	dim l1 as integer, l2 as integer
	dim stmp as string

	if s1 = s2 then
		FWEquiv = 1
		Exit Function
	end if

	l1 = len(s1)
	l2 = len(s2)

	'remove units and trailing zeroes
	while l1 > 0 and l2 > 0 and right$(s1, 1) = right$(s2, 1)
		s1 = left$(s1, l1 - 1)
		s2 = left$(s2, l2 - 1)
		l1 = l1 - 1
		l2 = l2 - 1
	wend

	if l1 > 0 and l2 > 0 and FWPrettyClose(val(s1), val(s2)) then
		FWEquiv = 1
	else
		FWEquiv = 0
	end if

end function


'Return non-zero if v1 and v2 are practically equal
Function FWPrettyClose(v1 as single, v2 as single) as integer
	dim diff as single
	if v1 = 0 then
		if v2 = 0 then
			FWPrettyClose = 1
			exit function
		else
			diff = abs((v1 - v2)/v2)
		end if
	else
		diff = abs((v1 - v2)/v1)
	end if
	if diff > 0.001 then FWPrettyClose = 0 else FWPrettyClose = 1
end function


' Sub: FWVerifyText
'      Based on RecVerifyText in VT's recorder.inc, but gives fuller diagnostics
'
' Desc: This sub will verify that the value of some text in a control
'       is equal to the text specified by the 'szItem$' parameter.
'
'       If 'szItem' [nItem?] is non-NULL [non-zero?], then the text to be verified belongs
'       to an item in the control, and not the control itself.
'
'       A Fail is raised if the text of 'nItem' doesn't match 'szItem$'.

'TODO LarsH: maybe use FWEquiv(A, B) = 0 instead of A <> B

Sub FWVerifyText(szControl$, szItem$, nItem&, dwType&)
	Dim szTemp As String
	dim hwnd as long

	Select Case dwType&
		Case V_WINDOW
			If GetText(WGetActWnd(0)) <> szItem$ Then
				Fail "The active window does not have the caption """ + szItem$ + """."
			End If

		Case V_BUTTON
			hWnd = WButtonFind(szControl$)

			If hWnd Then
				'If szItem has two special characters, remove one.

				If Left$(szItem, 1) = Mid$(szItem, 2, 1) Then
					Select Case Left$(szItem, 1)
						Case "#", "@", "~", "=", "+"
							szTemp$ = String$(256, 0)
							szTemp$ = Mid$(szItem$, 2)
							szItem$ = szTemp$
					End Select
				End If

				If GetText(hWnd) <> szItem$ Then
					Fail "The text of button """ + szControl$ + """ is not """ + szItem$ + """."
				End If
			Else
				Fail "Unable to find the button with ordinal: " + szControl$
			End If

		Case V_CHECK
			hWnd = WCheckFind(szControl$)

			If hWnd Then
				If GetText(hWnd) <> szItem$ Then
					Fail "The text of check box """ + szControl$ + """ is not """ + szItem$ + """."
				End If
			Else
				Fail "Unable to find the check box with ordinal: " + szControl$
			End If

		Case V_EDIT
			If nItem = 0 Then
				hWnd = WEditFind(szControl$)
				if hwnd = 0 then Fail "Couldn't find edit box " + szControl$
				szTemp = EditText(szControl$)
				If FWEquiv(szTemp, szItem) = 0 Then
					Fail "The edit box """ + szControl$ + """ contains """ + szTemp$ + """ instead of """ + szItem$ + """."
				End If
			Else
				hWnd = WEditFind(szControl$)

				If hWnd Then
					If LabelText(hWnd) <> szItem$ Then
						Fail "The label associated with the edit box is not """ + szItem$ + """."
					End If
				Else
					Fail "Unable to find the edit box with ordinal: " + szControl$
				End If
			End If

		Case V_LIST
			If nItem <> 0 Then
				If ListItemText(szControl$, nItem&) <> szItem$ Then
					Fail "Item ";nItem&;" in the list box """ + szControl$ + """ is not """ + szItem$ + """."
				End If
			Else
				hWnd = WListFind(szControl$)

				If hWnd Then
					If LabelText(hWnd) <> szItem$ Then
						Fail "The label associated with the list box is not """ + szItem$ + """."
					End If
				Else
					Fail "Unable to find the list box with ordinal: " + szControl$
				End If
			End If

		Case V_COMBO
			If nItem& > 0 Then
				If ComboItemText(szControl$, nItem&) <> szItem$ Then
					Fail "Item ";nItem&;" in the combo box """ + szControl$ + """ is not """ + szItem$ + """."
				End If
			ElseIf nItem = 0 Then
				szTemp = ComboText(szControl$)
				If szTemp <> szItem$ Then
					Fail "The combo box """ + szControl$ + """ contains """ + sztemp + """ instead of """ + szItem$ + """."
				End If
			Else
				hWnd = WComboFind(szControl$)

				If hWnd Then
					If LabelText(hWnd) <> szItem$ Then
						Fail "The label associated with the combo box is not """ + szItem$ + """."
					End If
				Else
					Fail "Unable to find the combo box with ordinal: " + szControl$
				End If
			End If

		Case V_LISTVIEW
			If nItem <> 0 Then
				If ViewItemText(szControl$, "@" + LTrim$(Str$(nItem&)), Null) <> szItem$ Then
					Fail "Item ";nItem&;" in the listview """ + szControl$ + """ is not """ + szItem$ + """."
				End If
			Else
				hWnd = WViewFind(szControl$)

				If hWnd Then
					If LabelText(hWnd) <> szItem$ Then
						Fail "The label associated with the list view is not """ + szItem$ + """."
					End If
				Else
					Fail "Unable to find the list view control with ordinal: " + szControl$
				End If
			End If

		Case V_HEADER
			If HeaderItemText(szControl$, "@" + LTrim$(Str$(nItem&))) <> szItem$ Then
				Fail "Item ";nItem&;" in the header control """ + szControl$ + """ is not """ + szItem$ + """."
			End If

		Case V_TREEVIEW
			If nItem <> 0 Then
				If TreeItemText(szControl$, "@" + LTrim$(Str$(nItem&))) <> szItem$ Then
					Fail "Item ";nItem&;" in the tree control """ + szControl$ + """ is not """ + szItem$ + """."
				End If
			Else
				hWnd = WTreeFind(szControl$)

				If hWnd Then
					If LabelText(hWnd) <> szItem$ Then
						Fail "The label associated with the tree view is not """ + szItem$ + """."
					End If
				Else
					Fail "Unable to find the tree view control with ordinal: " + szControl$
				End If
			End If

		Case V_TOOLBAR
			If ToolbarButtonText(szControl$, "@" + LTrim$(Str$(nItem&))) <> szItem$ Then
				Fail "Button ";nItem&;" in the toolbar """ + szControl$ + """ is not """ + szItem$ + """."
			End If

		Case V_TAB
			If TabItemText(szControl$, "@" + LTrim$(Str$(nItem&))) <> szItem$ Then
				Fail "Item ";nItem&;" in the tab control """ + szControl$ + """ is not """ + szItem$ + """."
			End If

		Case V_STATUS
			If StatusItemText(szControl$, "@" + LTrim$(Str$(nItem&))) <> szItem$ Then
				Fail "Item ";nItem&;" in the status bar """ + szControl$ + """ is not """ + szItem$ + """."
			End If

		Case V_MENU
			If RecPullDownPopups(szItem$) Then
				If RecGetMenuItem(szItem$) <> MenuText("@" + LTrim$(Str$(nItem))) Then
					WMenuEnd()
					Fail "The text of menu item """;nItem&;""" is not """ + szItem$ + "."
				End If

				WMenuEnd()
			Else
				Fail "A menu pop-up did not exist - unable to verify menu item."
			End If

		Case V_STATIC
			If StaticText(szControl$) <> szItem$ Then
				Fail "The static control """ + szControl$ + """ does not contain the text """ + szItem$ + """."
			End If

		Case V_DESKTOP
			If DesktopItemText("@" + LTrim$(Str$(nItem&))) <> szItem$ Then
				Fail "Item ";nItem&;" on the desktop is not """ + szItem$ + """."
			End If

		Case V_SLIDER
			hWnd = WSliderFind(szControl$)

			If hWnd Then
				If LabelText(hWnd) <> szItem$ Then
					Fail "The label associated with the slider control is not """ + szControl$ + """."
				End If
			Else
				Fail "Unable to find slider control with ordinal: " + szControl$
			End If

		Case V_OPTION
			hWnd = WOptionFind(szControl$)

			If hWnd Then
				If GetText(hWnd) <> szItem$ Then
					Fail "The text of option button """ + szControl$ + """ is not """ + szItem$ + """."
				End If
			Else
				Fail "Unable to find the option button with ordinal: " + szControl$
			End If
		Case V_REBAR
			If WRebarBandText(szControl$, nItem&) <> szItem$ Then
				Fail "Band ";nItem&;" in the rebar control """ + szControl$ + """ is not """ + szItem$ + """."
			End If

		Case Else
			Fail "Improper call to verification function!"
	End Select

End Sub


'Read in a menu specification from a file, and verify that the active app's main menus
'conform to it.  Verifies the order of items as well as text captions.  It will *not* detect
'a problem if a menu has extra items on the end of it.
'Submenus are tested, but not dynamic menu items.  We will not verify pictures or checkmarks,
'only strings.  Accepts but doesn't yet use information about accelerators (e.g. Ctrl+S).

'TODO LarsH: detect items that are in the app's menus (at the end) but not in the spec
' However don't give errors on dynamic items.  Maybe put special notation in the spec for
' "dynamic items go here"?

'TODO LarsH: verify that the menu spec doesn't have duplicate hotkeys, e.g. "&Print" and "&Page Setup"
'on the same menu.

'TODO LarsH: ignore *indented* blank lines and indented comments

'TODO LarsH: make a way to test non-dynamic items that occur on a menu after dynamic items; e.g.
'  the View - Overlays submenu has some dynamic items followed by ---, Show All, ---, &Configure...

'TODO LarsH: notate and verify presence of checkmarks on menu items.
'  There does not seem to be a way to do this in VT.  WMenuChecked() only tells
'  whether an item is checked, not whether it's checkable.

Sub FWVerifyMenus(fname as string)
	'open menu file, from $(fwroot) directory
	dim infile%
	dim dir$, s$, failure$

	infile = FreeFile
	if not exists(fname) then
		dir = Environ("fwroot")
		if dir = "" then
			fail "Menu file """; fname; """ not found and no FWROOT setting"
			exit sub
		else
			s = dir + "\" + fname
			if not exists(s) then
				fail "Couldn't find menu file """; fname; """ or """; s ;""""
				exit sub
			else
				fname = s
			end if
		end if
	end if

	err = 0
	on local error goto ErrOpen
	Open fname for input as #infile

 ErrOpen:
	if err then
		fail "Couldn't open menu file """; fname; """: "; err; Error$()
		exit sub
	end if
	on local error goto 0

	failure = ProcessMenuFile(infile, fname)

	close #infile

	if not (failure = "") then
		fail failure
	end if
end sub


'Private fn used by FWVerifyMenus

function ProcessMenuFile(infile as integer, fname as string) as string
	dim linenum%
	dim item(4) 'item(i) = index of last matched item on level i of menu (not counting separators)
	dim sep(4)  'index of last processed separator on level i
	dim length(4) 'number of items, incl separators, on level i menu
	dim dynam(4) 'boolean: true if the previous item on level i was "*"
	dim altkey$(4) 'alt-keys of menus
	dim tmpItem%, tmpSep%
	dim level%, lastLevel%, dyn%
	dim menupath$, menucapn$, s$
	dim currTop$, curr2nd$, curr3rd$ 'E.g. View\Toolbars\Standard
	dim mi as MENUINFO

	linenum = 1
	lastLevel = 0

	ProcessMenuFile = ""

	'We go through the menu file line by line, comparing the actual GUI menu with it.
	'The notion of the "current position" in the GUI menu is indicated by item(level)+sep(level).
	while not (EOF(infile))

		err = 0
		on local error goto ErrRead
		line input #infile, s

	 ErrRead:
		if err then
			Log #0, "Err = "; err
			ProcessMenuFile = "Couldn't read from menu file """ + s + """: " + Error$()
			exit sub
		end if
		on local error goto 0

		'Log #0, "Input: "; s

		if IgnoreableLine(s) = TRUE then goto NextLine  'ignore blank lines & comments
		'Log #0, "Processing: "; s

		select case left(s, 1)
			case "'"	'ignore comments

			case "	"	'beginning with tab(s)
						'Here we process non-top-level menu items & separators
				dim i%, j%
				dim accel$
				accel = ""

				for i = 1 to len(s)
					if mid$(s, i, 1) <> "	" then
						j = instr(i, s, chr$(9)) 'look for tab separating caption from accelerator
						if j > 0 then
							accel = mid$(s, j + 1)
							s = left$(s, j - 1)	'remove accelerator from s
						end if
						exit for
					end if
				next

				'Log #0, "=> ["; s; "] ("; accel; ")"

				'Find indentation level and set menupath (e.g. "&Filters") and menucapn (e.g. "&No Filter")
				if mid$(s, 2, 1) = "	" then
					if mid$(s, 3, 1) = "	" then
						menucapn = mid$(s, 4)
						level = 4
						menupath = curr2nd + "\" + curr3rd
					else
						curr3rd = mid$(s, 3)
						menucapn = curr3rd
						level = 3
						menupath = curr2nd
					end if
				else
					curr2nd = mid$(s, 2)
					menucapn = curr2nd
					level = 2
					menupath = ""
				end if

				if level > lastLevel then 'initialize counters for new level
					item(level) = 0
					sep(level) = 0
					dynam(level) = false
				end if

				'Log #0, "menupath = ["; menupath; "].  menucapn = ["; menucapn; "]"

				'If changed levels, start over with opening the parent menu
				if level <> lastLevel then
					Play "{Esc}{Esc}{Esc}{Esc}" 'close all open menus (we could be more efficient about this but...)

					if (asc(altkey$(1)) <> 0) then
						'This will also test that the alt-keys work
						Play altkey$(1)
					else
						WToolbarButtonClk(_id(FWIDMenuToolbar), _ord(item(1)))  'open top-level menu
					end if

					if menupath <> "" then WMenuSelect(menupath) 'will give error if not found

					'This must be done after the menu is open
					if level > lastLevel then length(level) = MenuFullCount()
				end if

				select case menucapn
					case "*"	'Wildcard: zero or more dynamic menu items
						dynam(level) = TRUE

					case "-"	'Separator
						sep(level) = sep(level) + 1

						if dynam(level) = TRUE then
							'Look for a separator anywhere after the current point.
							'MenuFindSep() will update tmpItem and tmpSep if it succeeds.
							'These tmp variables are necessary because modifying call-by-reference
							'parameters doesn't work with array elements.
							tmpItem = item(level)
							tmpSep = sep(level)
							if MenuFindSep(tmpItem, tmpSep, length(level)) = True then
								item(level) = tmpItem
								sep(level) = tmpSep
							else
								fail fname; " line "; linenum; ": there should be a separator at or after "; currTop; _
									"\"; menupath; "\"; _ord(item(level) + sep(level))
							end if
							'We can't yet set dynam(level) = 0 because the separator we found might actually be
							'part of dynamic stuff rather than the static separator we were looking for.
						else
							'Look for a separator at current point
							if not WMenuSeparator(item(level) + sep(level)) then
								fail fname; " line "; linenum; ": menu item "; currTop; _
									"\"; menupath; "\"; _ord(item(level) + sep(level)); " should be a separator."
							end if
						end if

					case else	'a specific menu item
						altkey$(level) = FWGetAltKey(menucapn)

						item(level) = item(level) + 1

											'Tried using MenuFullText here; but it doesn't work for me.

						'If this item follows a *, we should just look for it by caption.
						if dynam(level) = TRUE then
							'Look for this item anywhere after the current point.
							'MenuFindItem() will update tmpItem and tmpSep if it succeeds.
							'These tmp variables are necessary because modifying call-by-reference
							'parameters doesn't work with array elements.
							tmpItem = item(level)-1 'Unincrement item because following separators will use unincremented item base
							tmpSep = sep(level)
							Log #0, fname; " line "; linenum; " MenuFindItem("; tmpItem; ", "; tmpSep; ", "; _
								length(level); ", "; menucapn; ")"
							if MenuFindItem(tmpItem, tmpSep, length(level), menucapn) = True then
								item(level) = tmpItem
								sep(level) = tmpSep
							else
								fail fname; " line "; linenum; ": there should be an item called " + menucapn + _
									" at or after "; currTop; "\"; menupath; "\"; _ord(item(level) + sep(level))
							end if

							dynam(level) = FALSE
						else
							s = MenuText(_ord(item(level)))
							'if not WMenuExists(menucapn) then
							if s <> menucapn then
								fail fname; " line "; linenum; ": menu item "; currTop; _
									"\"; menupath; "\"; _ord(item(level)); " was ["; s; "] instead of ["; menucapn; "]"
							else
								'Log #0, "Found "; currTop; "\"; menupath; "\"; menucapn
							end if
						end if

				end select

			case else 'top-level menu item
				'assume no separators and no dynamic items in menu bar itself.
				level = 1
				item(1) = item(1) + 1
				if lastLevel < 1 then item(1) = 1
				currTop = s
				Play "{Esc}{Esc}{Esc}{Esc}" 'close all open menus
				'if not WToolbarButtonExists(_id(FWIDMenuToolbar), currTop) then
				s = ToolbarButtonText(_id(FWIDMenuToolbar), _ord(item(1)))
				if not s = currTop then
					fail fname; " line "; linenum; ": menu item "; _ord(item(1)); " was ["; s; "] instead of ["; currTop; "]"
				else
					'Log #0, "Found "; currTop
				end if

				'Look for Alt key accelerator
				altkey$(1) = FWGetAltKey(s)

		end select

	 NextLine:
		linenum = linenum + 1
		lastLevel = level
	wend
end function

'Private util fn
'Strips off comments, and returns true if there is nothing else on the line.
function IgnoreableLine(s as string) as integer
	IgnoreableLine = TRUE
	if s = "" then exit function

	dim i%, lastnonwhite%
	lastnonwhite = 0

	for i = 1 to len(s)
		select case mid$(s, i, 1)
			case "'"	'comment
				s = left$(s, lastnonwhite)
				exit function
			case "	"	'tab
			case " "	'space
				'keep looping
			case else
				IgnoreableLine = FALSE  'non-comment character
				lastnonwhite = i
		end select
	next i

	'nothing but whitespace; treat as blank.
	exit function
end function


'Compute number of items in currently active menu, including separators
function MenuFullCount() as integer
	dim i%, cItems%, limMaxIndex%
	dim dummy%

	cItems = WMenuCount()
	limMaxIndex = cItems + cItems 'assume no more than one separator per item
	for i = 1 to limMaxIndex
		on local error goto wmserr
		dummy% = WMenuSeparator(i)
'		if WMenuSeparator(i) <> False then
'			Log #0, "Item #"; i; " is a separator."
'		else
'			Log #0, "Item #"; i; " isn't."
'		end if
		on local error goto 0

	next i

endofloop:
	'Log #0, "Done; i = "; i
	MenuFullCount = i-1
	exit function

wmserr:
	'log #0, "err: "; err; " error$: "; error$
	if err = 107 then 'Menu item out of range
		resume endofloop
	else
		on local error goto 0
		resume
	end if

end function

'Starting after ITEM items and SEP separators, find another separator.
'Return False if not found.  Otherwise, ITEM is
'updated to account for items passed over.

function MenuFindSep(item%, sep%, lim%) as integer
	dim olditem%
	olditem = item

	'Log #0, "> MenuFindSep: item="; item; " sep="; sep

	while item + sep <= lim
		if WMenuSeparator(item + sep) then
			MenuFindSep = True
			'Log #0, "< MenuFindSep: item="; item; " sep="; sep
			exit function
		else
			item = item + 1
		end if
	wend

	item = olditem 'for the sake of error reporting, restore the old item value (?)
	MenuFindSep = False
end function


'Starting after ITEM items and SEP separators, find item with specified caption.
'Return False if not found.  Otherwise, SEP and ITEM are updated to
'account for seps and items passed over.

function MenuFindItem(item%, sep%, lim%, caption$) as integer
	dim olditem%
	olditem = item

	'Log #0, "In MenuFindItem loop: item="; item; ", sep="; sep
	while item + sep < lim
		if WMenuSeparator(item + sep + 1) then
			sep = sep + 1
		else
			item = item + 1 'should this go after or before the "if MenuText()"?##
			'Log #0, "Testing item #"; item; ": "; MenuText(_ord(item)) '##
			if MenuText(_ord(item)) = caption then
				MenuFindItem = True
				exit function
			end if
		end if
		'Log #0, "In MenuFindItem loop: item="; item; ", sep="; sep
	wend

	item = olditem 'for the sake of error reporting, restore the old item value (?)
	MenuFindItem = False
end function


function FWGetAltKey(s$) as string
	dim i%

	for i = 1 to len(s)
		if mid$(s, i, 1) = "&" then
			FWGetAltKey = "%(" + mid$(s, i+1, 1) + ")"
			i = len(s) 'end loop
		end if
	next i

	FWGetAltKey = chr$(0)
end function
