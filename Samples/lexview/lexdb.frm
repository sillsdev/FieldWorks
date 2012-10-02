VERSION 5.00
Object = "{3B7C8863-D78F-101B-B9B5-04021C009402}#1.2#0"; "RICHTX32.OCX"
Object = "{F9043C88-F6F2-101A-A3C9-08002B2F49FB}#1.2#0"; "comdlg32.ocx"
Object = "{831FDD16-0C5C-11D2-A9FC-0000F8754DA1}#2.0#0"; "MSCOMCTL.OCX"
Begin VB.Form LexDB
   Caption         =   "Lexical Database"
   ClientHeight    =   7935
   ClientLeft      =   165
   ClientTop       =   735
   ClientWidth     =   11415
   BeginProperty Font
	  Name            =   "Times New Roman"
	  Size            =   9.75
	  Charset         =   0
	  Weight          =   400
	  Underline       =   0   'False
	  Italic          =   0   'False
	  Strikethrough   =   0   'False
   EndProperty
   LinkTopic       =   "Form1"
   ScaleHeight     =   7935
   ScaleWidth      =   11415
   StartUpPosition =   3  'Windows Default
   Begin VB.Timer Timer1
	  Left            =   10440
	  Top             =   0
   End
   Begin MSComctlLib.StatusBar StatusBar
	  Align           =   2  'Align Bottom
	  Height          =   255
	  Left            =   0
	  TabIndex        =   5
	  Top             =   7680
	  Width           =   11415
	  _ExtentX        =   20135
	  _ExtentY        =   450
	  _Version        =   393216
	  BeginProperty Panels {8E3867A5-8586-11D1-B16A-00C0F0283628}
		 NumPanels       =   1
		 BeginProperty Panel1 {8E3867AB-8586-11D1-B16A-00C0F0283628}
		 EndProperty
	  EndProperty
   End
   Begin RichTextLib.RichTextBox rtfTextBox
	  Height          =   6255
	  Left            =   120
	  TabIndex        =   4
	  Top             =   1320
	  Width           =   11175
	  _ExtentX        =   19711
	  _ExtentY        =   11033
	  _Version        =   393217
	  Enabled         =   -1  'True
	  TextRTF         =   $"lexdb.frx":0000
	  BeginProperty Font {0BE35203-8F91-11CE-9DE3-00AA004BB851}
		 Name            =   "Times New Roman"
		 Size            =   12
		 Charset         =   0
		 Weight          =   400
		 Underline       =   0   'False
		 Italic          =   0   'False
		 Strikethrough   =   0   'False
	  EndProperty
   End
   Begin MSComDlg.CommonDialog ComDlg
	  Left            =   10920
	  Top             =   0
	  _ExtentX        =   847
	  _ExtentY        =   847
	  _Version        =   393216
   End
   Begin VB.HScrollBar HScroll1
	  Height          =   255
	  Left            =   3600
	  TabIndex        =   1
	  Top             =   720
	  Width           =   3735
   End
   Begin VB.TextBox LexEntry
	  BeginProperty Font
		 Name            =   "MS Sans Serif"
		 Size            =   9.75
		 Charset         =   0
		 Weight          =   400
		 Underline       =   0   'False
		 Italic          =   0   'False
		 Strikethrough   =   0   'False
	  EndProperty
	  Height          =   285
	  Left            =   3600
	  TabIndex        =   0
	  Top             =   120
	  Width           =   3735
   End
   Begin VB.Label Label2
	  Caption         =   "Dictionary Entry:"
	  BeginProperty Font
		 Name            =   "MS Sans Serif"
		 Size            =   9.75
		 Charset         =   0
		 Weight          =   400
		 Underline       =   0   'False
		 Italic          =   0   'False
		 Strikethrough   =   0   'False
	  EndProperty
	  Height          =   255
	  Left            =   120
	  TabIndex        =   3
	  Top             =   840
	  Width           =   2535
   End
   Begin VB.Label Le
	  Alignment       =   1  'Right Justify
	  Caption         =   "Lexical Entry:"
	  BeginProperty Font
		 Name            =   "MS Sans Serif"
		 Size            =   9.75
		 Charset         =   0
		 Weight          =   400
		 Underline       =   0   'False
		 Italic          =   0   'False
		 Strikethrough   =   0   'False
	  EndProperty
	  Height          =   255
	  Left            =   2040
	  TabIndex        =   2
	  Top             =   120
	  Width           =   1455
   End
   Begin VB.Menu FileMenu
	  Caption         =   "&File"
	  Begin VB.Menu OpenCmd
		 Caption         =   "&Open..."
		 Shortcut        =   ^O
	  End
	  Begin VB.Menu sep1
		 Caption         =   "-"
	  End
	  Begin VB.Menu ExitCmd
		 Caption         =   "E&xit"
		 Shortcut        =   ^Q
	  End
   End
End
Attribute VB_Name = "LexDB"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Option Explicit

Dim m_ldb As Ldb
Dim m_db As Connection

Dim m_lpid As Long
Dim m_ldid As Long
Dim m_encAn As Long
Dim m_encVer As Long

Const ksegMax = 100
Dim m_rgseg(ksegMax) As Long
Dim m_fSettingWord As Boolean
Dim m_sRoot As String

Private Type LexEntry
	id As Long
	sLexEntry As String
	HomographNumber As Long
End Type

Dim m_rglexe() As LexEntry ' Dynamic array
Dim m_clexeMax As Long ' Actual size of WrdForms array.
Dim m_clexe As Long ' Number of active WrdForms.

Dim m_fKeyDown As Boolean
Dim m_fClick As Boolean

Private Sub OpenCmd_Click()
	Dim sQuery As String
	Dim sSvr As String
	Dim sDb As String
	Dim rsLexEntries As Recordset
	Dim rsT As Recordset

	'Get server and database from user
	sSvr = "Daffy"
	sDb = "Tuwali"
	Load DataSource
	DataSource.server.Text = sSvr
	DataSource.db.Text = sDb
	DataSource.Show vbModal, Me
	sSvr = DataSource.server.Text
	sDb = DataSource.db.Text
	Unload DataSource
	If sSvr = "" Or sDb = "" Then Exit Sub

	' Set the caption of the application
	Caption = "Lexical Database [\\" & sSvr & "\" & sDb & "]"

	Set m_db = Nothing
	m_clexe = 0

	On Error GoTo Failed

	' open the connection to the database
	Set m_db = New Connection
	m_db.Open "Provider=SQLOLEDB.1;Initial Catalog=" & sDb & ";Data Source=" & sSvr, "sa", ""
	m_db.CursorLocation = adUseClient

	Set m_ldb = New Ldb
	m_ldb.Conn = m_db

	' Get the first language project.
	Set rsT = New Recordset
	rsT.Open "select top 1 id lpid, ldb.Dst ldid from LanguageProject lp " & _
		"join LanguageProject_LexicalDatabase ldb on ldb.Src = lp.id", m_db, adOpenStatic, adLockReadOnly
	' rsT.Open "select top 1 id as lpid, LexicalDatabase as ldid from LanguageProject", m_db, adOpenForwardOnly, adLockReadOnly
	m_lpid = rsT.Fields("lpid").Value
	m_ldid = rsT.Fields("ldid").Value
	Debug.Print "LanguageProject = " & m_lpid
	Debug.Print "LexicalDatabase = " & m_ldid
	rsT.Close

	' Get the encodings
	rsT.Open _
		"select top 1 lev.Encoding venc, lea.Encoding aenc " & _
		"from LanguageProject_CurrentVernacularEncs ve " & _
		"join LanguageProject_CurrentAnalysisEncs ae on ve.src = ae.src " & _
		"join LgEncoding lev on ve.dst = lev.id " & _
		"join LgEncoding lea on ae.dst = lea.id " & _
		"where ae.src = " & m_lpid, _
		m_db, adOpenForwardOnly, adLockReadOnly
	m_encAn = rsT.Fields("aenc").Value
	Debug.Print "Analysis enc = " & m_encAn
	m_encVer = rsT.Fields("venc").Value
	Debug.Print "Vernacular enc = " & m_encVer
	rsT.Close

	m_ldb.LexDBId = m_ldid
	m_ldb.GlossEnc = m_encAn
	m_ldb.VernEnc = m_encVer
	If Not m_ldb.AddGetSenseProc() Then
		GoTo Failed
	End If

	Set rsLexEntries = New Recordset
	sQuery = "select  le.id, " & _
			"   le.HomographNumber," & _
			"   convert(nvarchar(50), coalesce(mmt.prefix, '') + coalesce(mfs.Txt, '') + coalesce(mmt.postfix, '')) as txt " & _
			"from    lexicaldatabase_entries let " & _
			"    join lexentry le on let.Dst = le.id " & _
			"    join lexentry_forms lft on le.id = lft.Src " & _
			"    join MoForm mf on lft.Dst = mf.id " & _
			"    left outer join MoForm_Form mfs on mf.id = mfs.obj and mfs.enc = " + Str(m_encVer) & _
			"    left outer join MoMorphType mmt on mf.morphtype = mmt.id " & _
			"where  let.Src = " + Str(m_ldb.LexDBId) & _
			"        and le.IsIncludedAsHeadword = 0 "
	rsLexEntries.Open sQuery, m_db, adOpenStatic, adLockReadOnly

	' Copy the record set to an internal array.
	While Not rsLexEntries.EOF
		If m_clexe >= m_clexeMax Then
			If m_clexeMax = 0 Then
				m_clexeMax = 100
				ReDim m_rglexe(m_clexeMax)
			Else
				m_clexeMax = 2 * m_clexeMax
				ReDim Preserve m_rglexe(m_clexeMax)
			End If
		End If
		m_rglexe(m_clexe).id = rsLexEntries.Fields!id
		m_rglexe(m_clexe).sLexEntry = rsLexEntries!txt
		If rsLexEntries!HomographNumber > 0 Then m_rglexe(m_clexe).sLexEntry = m_rglexe(m_clexe).sLexEntry & Trim(Str(rsLexEntries!HomographNumber))
		m_rglexe(m_clexe).HomographNumber = rsLexEntries!HomographNumber
		m_clexe = m_clexe + 1
		rsLexEntries.MoveNext
	Wend

	' Update scrolling information.
	If m_clexe = 0 Then
		HScroll1.Enabled = False
	Else
		HScroll1.Enabled = True
		HScroll1.Max = m_clexe - 1
		HScroll1.Min = 0
		HScroll1.LargeChange = m_clexe / 25
		If HScroll1.LargeChange = 0 Then HScroll1.LargeChange = 1
		HScroll1.Value = 0
	End If

	FillControls True
	Exit Sub

Failed:
	Set m_db = Nothing
	m_clexe = 0

	MsgBox "Loading Database " & sDb & "From Server " & sSvr & " Failed"
	Exit Sub
End Sub

Private Sub ExitCmd_Click()
	Unload Me
	End
End Sub
Private Sub HScroll1_Change()
	FillControls False
	Timer1.Enabled = False
	Timer1.Interval = 100
	Timer1.Enabled = True
End Sub

'Private Sub HScroll1_Scroll()
'    HScroll1_Change
'End Sub

Private Sub HScroll1_Scroll()
	StatusBar.Panels(1).Text = Str(HScroll1.Value + 1) & " of " & Str(m_clexe)
	HScroll1_Change
End Sub

' Look up the word form in context and displays the segment in the middle listbox.
Private Sub FillControls(fAll As Boolean)
	If m_db Is Nothing Then Exit Sub

	Dim id As Long
	Dim sLex As String
	Dim ilexe As Long
	Dim nHGNo As Long
	Dim nHGNoLen As Integer

	If Not HScroll1.Enabled Then
		LexEntry.Text = ""
		rtfTextBox.TextRTF = ""
	End If

	' Get the lex entry based on the scroll index value
	ilexe = HScroll1.Value
	id = m_rglexe(ilexe).id
	sLex = m_rglexe(ilexe).sLexEntry
	nHGNo = m_rglexe(ilexe).HomographNumber

	' Set the lex entry field
	If Not m_fSettingWord Then
		m_fSettingWord = True
		LexEntry.Text = sLex
		m_fSettingWord = False
	End If

	' Only continue if we need to fill in the segments at this point
	If Not fAll Then Exit Sub

	' if the Homograph Number is greater than 0 strip off the number from the end of the
	' lexical entry string

	If nHGNo > 0 Then
		nHGNoLen = (Log(nHGNo) \ Log(10)) + 1
		sLex = Left(sLex, Len(sLex) - nHGNoLen)
	End If

	Call m_ldb.DisplayLexEntry(id, sLex, nHGNo, rtfTextBox)

	StatusBar.Panels(1).Text = Str(ilexe + 1) & " of " & Str(m_clexe)
End Sub

' This method sets up the lexeme completion which is handled in LexEntry_Changed.
Private Sub LexEntry_KeyDown(KeyCode As Integer, Shift As Integer)
	If KeyCode <> 8 Then Exit Sub

	Dim cch As Integer
	Dim s As String

	cch = Len(m_sRoot)
	If cch <= 0 Then Exit Sub
	s = LexEntry.Text
	If cch < Len(s) And m_sRoot = Left(s, cch) And LexEntry.SelStart = cch And LexEntry.SelLength = Len(s) - cch Then
		m_fSettingWord = True
		LexEntry.Text = m_sRoot
		LexEntry.SelStart = cch
		LexEntry.SelLength = 0
		m_fSettingWord = False
	End If
End Sub

' This method handles lexeme completion. It looks up the first lexeme that can be interpreted from
' the users key strokes and selects the portion added so the next key stroke replaces it.
Private Sub LexEntry_Change()
	If m_fSettingWord Then Exit Sub
	If m_db Is Nothing Then Exit Sub
	If m_clexe = 0 Then Exit Sub

	Dim ilexeMin As Long
	Dim ilexeLim As Long
	Dim ilexeT As Long
	Dim sCur As String
	Dim sT As String

	ilexeMin = 0
	ilexeLim = m_clexe - 1
	sCur = LexEntry.Text
	While (ilexeMin < ilexeLim)
		ilexeT = (ilexeMin + ilexeLim) \ 2
		sT = m_rglexe(ilexeT).sLexEntry
		If (StrComp(sT, sCur, vbTextCompare) < 0) Then
			ilexeMin = ilexeT + 1
		Else
			ilexeLim = ilexeT
		End If
	Wend

	m_fSettingWord = True

	sT = m_rglexe(ilexeMin).sLexEntry

	If Len(sCur) <= Len(sT) And sCur = Left(sT, Len(sCur)) Then
		m_sRoot = sCur
		LexEntry.Text = sT
		LexEntry.SelStart = Len(sCur)
		LexEntry.SelLength = Len(sT) - Len(sCur)
	Else
		m_sRoot = ""
	End If

	HScroll1.Value = ilexeMin
	m_fSettingWord = False
End Sub


Private Sub Timer1_Timer()
	Timer1.Enabled = False
	FillControls True
End Sub
