'Filename: DNtesting.inc

'$include '..\FWGlobal\FWtesting.inc'

'==================================================================================
'Constants
'==================================================================================

const DNExpTitle$ = "DEB-Debug - Data Notebook"
const DNExpTitleSuffix$ = " - Data Notebook"
const DNMainClass$ = "RnMainWnd"
const DNHHTitle$ = "Data Notebook User Helps"

const DN_START_DELAY = 30 'How many seconds for DN to start up
'DN does currently sometimes take longer than this, especially if MSDE server
' has to start up too.  However spec says 30 seconds.


'==================================================================================
'Subs & Functions
'==================================================================================

'Start the Data Notebook app and wait till it's ready
function StartDN(useExisting%, checkSplash%) as long
	'If useExisting is zero, close any existing DN app(s) first.
	'If checkSplash is non-zero, verify splash screen (not functional)

	dim hwnd as long

	'Minimize the Visual Test window.
	If GetHandle(GH_HWNDCLIENT) Then WMinWnd(GetHandle(GH_HWNDCLIENT))

	' Check the resolution that this script was recorded on.
	'CheckResolution (1280, 1024)  'Is this a good thing?


	if useExisting = 0 then
		'First check if DN is already running; if so, close it.
		dim count%
		count = 0

		do
			hwnd = FindDNWindow(False)
			if hwnd > 0 then
				if count > 10 then
					fail "Couldn't close pre-existing Data Notebooks"
				end if
				'Close the window
				Play "{esc}{esc}{esc}%(f)x" 'Alt+F X = File-Exit
				WaitUntilIdle(TimeoutMsec)
			end if
			count = count + 1
		loop while hwnd > 0
	else
		'First check if DN is already running; if so, use it.
		hwnd = FindDNWindow(False)
		if hwnd > 0 then
			CurrentWindow = hwnd
			exit sub
		end if
	end if

	'Use this if we want to start it from the desktop; but this requires that
	'the icon be visible. :-(
	'WDesktopItemDblClk("FieldWorks", VK_LBUTTON)

	if false then
		'Start it from the task bar:
		WTaskbarStartClk()
		'Can we avoid assuming English here?
		WToolbarButtonClk("@2", "&Programs")
		WToolbarButtonClk("@1", "FieldWorks")
		'WToolbarButtonClk("@1", "Data Notebook")
		'Problem: there is a "Data Notebook" folder, then a "Data Notebook" program item.
		'So we have to use the ordinal number instead of the string.
		WToolbarButtonClk("@1", _ord(5))  '5th button (counting submenus too)
		'Note, maybe this can fail if "hidden menus" are active(?)
	else
		'start it from the desktop icon
		WDesktopItemDblClk("Data Notebook")
	end if

	'Wait for DN to start.
	hwnd = WFndWndC(DNExpTitleSuffix, DNMainClass, _
		FW_FOCUS OR FW_IGNOREFILE_LEFT, DN_START_DELAY)
	StartDN = hwnd
	if hwnd > 0 then
		CurrentWindow = hwnd
	else
		Fail "FieldWorks Data Notebook didn't start within "; DN_START_DELAY; " seconds."
	end if

	'if checkSplash > 0 then
		' The following doesn't work.  Apparently the splash screen doesn't respond to messages
		' (it's busy?)
		''Check for the splash screen.
		'hwnd = WFndWndC("(Splash)", "Static", FW_FOCUS, 3)
		'
		'if hwnd <= 0 then
		'	Fail "Splash screen didn't appear (fast enough)"
		'end if

		'OK, the splash screen appeared.
		'TODO LarsH: can we verify anything on the splash screen, such as
		'the SIL logo, app title, status mesg, etc.?
	'end if
end sub

function FindDNWindow(giveErr%, extraFlags& = 0) as long
	dim hwnd as long
	hwnd = WFndWndC(DnExpTitleSuffix, DNMainClass, _
		FW_FOCUS OR FW_IGNOREFILE_LEFT OR extraFlags, Timeout)
	FindDNWindow = hwnd
	if hwnd <= 0 and giveErr = True then
		fail "Couldn't find Data Notebook window."
	end if
end function
