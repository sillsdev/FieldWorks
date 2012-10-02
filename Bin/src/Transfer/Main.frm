VERSION 5.00
Object = "{F9043C88-F6F2-101A-A3C9-08002B2F49FB}#1.2#0"; "comdlg32.ocx"
Begin VB.Form Main
   Caption         =   "Form1"
   ClientHeight    =   3195
   ClientLeft      =   165
   ClientTop       =   735
   ClientWidth     =   4680
   LinkTopic       =   "Form1"
   ScaleHeight     =   3195
   ScaleWidth      =   4680
   StartUpPosition =   3  'Windows Default
   Begin MSComDlg.CommonDialog dlg
	  Left            =   2400
	  Top             =   1680
	  _ExtentX        =   847
	  _ExtentY        =   847
	  _Version        =   393216
   End
   Begin VB.Menu FileMenu
	  Caption         =   "&File"
	  Begin VB.Menu TransferItem
		 Caption         =   "&Transfer..."
	  End
	  Begin VB.Menu ExitItem
		 Caption         =   "E&xit"
	  End
   End
End
Attribute VB_Name = "Main"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Option Explicit

Dim WithEvents m_db As Connection
Attribute m_db.VB_VarHelpID = -1
Dim m_fExecuting As Boolean
Dim m_sQueued As String

Dim m_lp As ICmDocument
Dim m_dm As CmDocManager
Dim m_dictClsName As Dictionary
Dim m_dictClsId As Dictionary

' m_rgfCreated(cookie\2) is true if object with that cookie was successfully added
' all cookies in Cellar 2 are odd, and they are allocated sequentially;
' 200000 was not enough for the Greek kb, so we'll try 600000.
Const kcfMax = 600000
Dim m_rgfCreated(kcfMax) As Boolean

Dim m_cobj As Long

#Const use_file = False
#Const no_async = True
#If use_file Then
Dim m_fn As Integer
#Else
Dim m_sCur As String
Const kcchMax = 4000
#End If

Const krgchHex = "0123456789ABCDEF"
Private Declare Sub CopyMem Lib "kernel32" Alias "RtlMoveMemory" (ByRef pvDst As Any, ByRef pvSrc As Any, ByVal cb As Long)

Private Declare Sub SleepEx Lib "kernel32" (ByVal dts As Long, ByVal fCanAlert As Long)

Private Sub ExitItem_Click()
	Unload Me
End Sub

Private Sub TransferItem_Click()
	Dim nErr As Long
	Dim sDb As String
	Dim sSvr As String
	Dim sSrc As String
	Dim rsCls As Recordset

	dlg.FileName = ""
	dlg.DefaultExt = "kb2"
	dlg.Filter = "Language Project (*.Kb2)|*.Kb2|All Files (*.*)|*.*"
	dlg.Flags = cdlOFNLongNames Or cdlOFNFileMustExist Or cdlOFNPathMustExist Or cdlOFNExplorer Or cdlOFNHideReadOnly

	dlg.CancelError = True
	On Error Resume Next
	dlg.ShowOpen
	nErr = Err
	On Error GoTo 0

	If nErr <> 0 Then Exit Sub
	sSrc = dlg.FileName

#If use_file Then
	Dim sDst As String

	dlg.FileName = ""
	dlg.DefaultExt = "Sql"
	dlg.Filter = "Sql Scripts (*.Sql)|*.Sql|All Files (*.*)|*.*"
	dlg.Flags = cdlOFNLongNames Or cdlOFNNoReadOnlyReturn Or cdlOFNOverwritePrompt Or cdlOFNExplorer Or cdlOFNHideReadOnly

	dlg.CancelError = True
	On Error Resume Next
	dlg.ShowSave
	nErr = Err
	On Error GoTo 0

	If nErr <> 0 Then Exit Sub
	sDst = dlg.FileName
#End If

	sSvr = InputBox("Enter the server name:", "Server", "(local)")
	If sSvr = "" Then Exit Sub

	sDb = InputBox("Enter the database name:", "Database", "Tuwali")
	If sDb = "" Then Exit Sub

	Set m_db = New Connection
	m_db.CommandTimeout = 6000 ' Need bigger timout for Greek: Default 30 sec is too small.
	' m_db.Open "Provider=SQLOLEDB.1;Integrated Security=SSPI;Initial Catalog=" & sDb & ";Data Source=" & sSvr
	m_db.Open "Provider=SQLOLEDB.1;Uid=sa;Pwd=;Initial Catalog=" & sDb & ";Data Source=" & sSvr

	Set m_dm = New CmDocManager
	Set m_lp = m_dm.LoadDocument(sSrc)

	Set rsCls = New Recordset
	rsCls.Open "select * from [Class$]", m_db, adOpenStatic, adLockReadOnly

	' Build the class dictionaries.
	Set m_dictClsName = New Dictionary
	Set m_dictClsId = New Dictionary
	While Not rsCls.EOF
		Dim cd As ClassData
		Dim sCls As String
		Dim Id As Long

		Set cd = New ClassData
		sCls = rsCls!Name
		Id = rsCls!Id
		cd.Init Id, rsCls!base, sCls, m_db
		m_dictClsName.Add sCls, cd
		m_dictClsId.Add Id, cd
		rsCls.MoveNext
	Wend
	rsCls.Close
	Set rsCls = Nothing

	Dim d As Date
	Dim d2 As Date

	Dim i As Long

	For i = 0 To kcfMax
		m_rgfCreated(i) = False
	Next

#If use_file Then
	m_fn = FreeFile
	On Error Resume Next
	Open sDst For Output Access Write As #m_fn
	If Err Then
		MsgBox "Opening file '" & sDst & "' failed."
		Exit Sub
	End If
	On Error GoTo 0
	Exec "set nocount on"
	Exec "begin tran"
#End If

	d = Now
	m_cobj = 0
	CreateObjects m_lp
#If use_file Then
#ElseIf no_async Then
	If Len(m_sCur) > 0 Then
		m_db.Execute "set nocount on begin tran " & m_sCur & " commit tran set nocount off"
		m_sCur = ""
	End If
#Else
	While m_fExecuting
		DoEvents
		SleepEx 1000, 1
	Wend
#End If
	Debug.Print m_cobj
	d2 = Now
	Debug.Print (d2 - d) * 24 * 3600
	Beep

	d = Now
	m_cobj = 0
	UpdateObjects m_lp
#If use_file Then
#ElseIf no_async Then
	If Len(m_sCur) > 0 Then
		m_db.Execute "set nocount on begin tran " & m_sCur & " commit tran set nocount off"
		m_sCur = ""
	End If
#Else
	While m_fExecuting
		DoEvents
		SleepEx 1000, 1
	Wend
#End If
	Debug.Print m_cobj
	d2 = Now
	Debug.Print (d2 - d) * 24 * 3600

#If use_file Then
	Exec "set nocount off"
	Exec "commit tran"
	Close #m_fn
#End If
	Beep
End Sub

Sub CreateObjects(ByVal obj As ICmObject)
	Dim tag As Long
	Dim sCls As String
	Dim cd As ClassData
	Dim objT As ICmObject
	Dim vobj As ICmVector
	Dim cobj As Long
	Dim iobj As Long
	Dim idObj As Long
	Dim idCls As Long
	Dim ifd As Long
	Dim fd As FldData
	Dim a()
	Dim sCmd As String
	Dim cpt As Long
	Dim sName As String

	sCls = obj.ClassName

	' Fix CM class name changes
	If Left(sCls, 1) <> "W" And Right(sCls, 4) = "List" Then
	' If sCls = "PartOfSpeechList" Or sCls = "LpPossibilityList" Or sCls = "LpLocalPossibilityList" Or sCls = "MoMorphTypeList" Or sCls = "LexSubentryTypeList" Or sCls = "ParticipantList" Or sCls = "LocationList" Then
		sCls = "CmPossibilityList"
	ElseIf Right(sCls, 2) = "ty" Then
	'ElseIf sCls = "LpPossibility" Or sCls = "LpLocalPossibility" Then
		sCls = "CmPossibility"
	ElseIf sCls = "StParagraph" Then
		sCls = "StTxtPara"
	ElseIf sCls = "LpEncodingPrx" Then
		sCls = "LgEncoding"
	End If

	If Not m_dictClsName.Exists(sCls) Then
		' Debug.Print "Create Missing class: " & sCls & " (" & obj.Cookie & ")"
		Exit Sub
	End If

	Set cd = m_dictClsName.Item(sCls)

	idObj = obj.Cookie \ 2
	m_cobj = m_cobj + 1
	m_rgfCreated(idObj) = True

	sCmd = "execute Create_" & sCls & " " & idObj & ", null"
	Exec sCmd

	Do
		a = cd.Fields.Items
		For ifd = 0 To UBound(a)
			Set fd = a(ifd)
			cpt = fd.cpt

			Select Case cpt
			Case kcptOwningPointer

				' Change RnResearchNbk.People to old Participants
				sName = fd.Name
				If sName = "People" Then
					sName = "Participants"
				ElseIf sName = "DateCreated" Then
					sName = "TimeCreated"
				ElseIf sName = "DateModified" Then
					sName = "TimeModified"
				End If

				tag = obj.PropTagBstr(sName)
				If cpt <> obj.PropType(tag) Then GoTo NextField
				Set objT = obj.ObjectProp(tag)
				If Not objT Is Nothing Then CreateObjects objT
			Case kcptOwningVector
				tag = obj.PropTagBstr(fd.Name)
				If cpt <> obj.PropType(tag) Then GoTo NextField
				Set vobj = obj.ObjectVecProp(tag)
				cobj = vobj.Size
				If cobj > 0 Then
					For iobj = 0 To cobj - 1
						Set objT = vobj.ItemUnknown(iobj)
						CreateObjects objT
					Next
				End If
			End Select
NextField:
		Next
		idCls = cd.base
		If idCls = cd.Id Then Exit Do
		Set cd = m_dictClsId.Item(idCls)
	Loop
End Sub

Sub UpdateObjects(ByVal obj As ICmObject)
	Dim sCls As String
	Dim cd As ClassData
	Dim idObj As Long
	Dim a()
	Dim sCmd As String
	Dim ifd As Long
	Dim val As Variant
	Dim fd As FldData
	Dim cpt As Long
	Dim fldt As Long
	Dim sFld As String
	Dim fOrdered As Boolean
	Dim otss As TextServicesLib.ITsString
	Dim csa As ICmStringAlt
	Dim ctss As Long
	Dim itss As Long
	Dim enc As Long
	Dim objT As ICmObject
	Dim idDst As Long
	Dim vobj As ICmVector
	Dim cobj As Long
	Dim sIns As String
	Dim sPre As String
	Dim iobj As Long
	Dim iT As Long
	Dim idCls As Long
	Dim hobj As Long

	sCls = obj.ClassName

	' Fix CM class name changes
	If Left(sCls, 1) <> "W" And Right(sCls, 4) = "List" Then
	' If sCls = "PartOfSpeechList" Or sCls = "LpPossibilityList" Or sCls = "LpLocalPossibilityList" Or sCls = "MoMorphTypeList" Or sCls = "LexSubentryTypeList" Or sCls = "ParticipantList" Or sCls = "LocationList" Then
		sCls = "CmPossibilityList"
	ElseIf Right(sCls, 2) = "ty" Then
	'ElseIf sCls = "LpPossibility" Or sCls = "LpLocalPossibility" Then
		sCls = "CmPossibility"
	ElseIf sCls = "StParagraph" Then
		sCls = "StTxtPara"
	ElseIf sCls = "LpEncodingPrx" Then
		sCls = "LgEncoding"
	End If

	If Not m_dictClsName.Exists(sCls) Then
		' Debug.Print "Update Missing class: " & sCls & " (" & obj.Cookie & ")"
		Exit Sub
	End If
	Set cd = m_dictClsName.Item(sCls)

	idObj = obj.Cookie \ 2
	m_cobj = m_cobj + 1

	Do
		a = cd.Fields.Items
		sCmd = ""
		For ifd = 0 To UBound(a)
			val = Null
			Set fd = a(ifd)
			cpt = fd.cpt
			fldt = fd.fldt
			sFld = fd.Name

			' Skip new fields
			If fd.Id = 4001003 Then
				GoTo NextField2     'RnResearchNbk.Encoding
			ElseIf fd.Id = 7005 Then
				GoTo NextField2    'CmPossibility.SourceItem
			ElseIf fd.Id = 8009 Then
				GoTo NextField2    'CmPossibilityList.SourceList
			End If
			' Change RnResearchNbk.People to old name
			If sFld = "People" Then
				sFld = "Participants"
			ElseIf sFld = "DateCreated" Then
				sFld = "TimeCreated"
			ElseIf sFld = "DateModified" Then
				sFld = "TimeModified"
			End If

			tag = obj.PropTagBstr(sFld)

			fOrdered = fldt >= 27

			' If the field is not the expected type skip it
			If cpt <> obj.PropType(tag) Then
				GoTo NextField2
			End If

			Select Case cpt
			Case kcptInteger
				val = obj.IntegerProp(tag)
			Case kcptBoolean
				If obj.BooleanProp(tag) Then
					val = 1
				Else
					val = 0
				End If
			Case kcptString
				Set otss = obj.StringProp(tag)
				sCmd = sCmd & ",[" & fd.Name & "_Fmt]=" & StringFormat(0, otss)
				val = otss.Text
			Case kcptMultiString
				Set csa = obj.MultiStringProp(tag)
				ctss = csa.StringCount
				For itss = 0 To ctss - 1
					Set otss = csa.GetStringFromIndex(itss, enc)
					Dim sCmd2 As String
					sCmd2 = "exec Set_" & cd.Name & "_" & fd.Name & " " & idObj & "," & enc & "," & Literalize(otss.Text, kcptString) & "," & StringFormat(enc, otss)
					Exec sCmd2
				Next
			Case kcptUnicode
				val = obj.UnicodeProp(tag)
			Case kcptOwningPointer
				Set objT = obj.ObjectProp(tag)
				If Not objT Is Nothing Then
					idDst = objT.Cookie \ 2
					If m_rgfCreated(idDst) Then
						UpdateObjects objT
						sCmd2 = "update CmObject set Owner$ = " & idObj & ", OwnFlid$ = " & fd.Id & ", OwnOrd$ = 0 where Id = " & idDst
						Exec sCmd2
					End If
				End If
			Case kcptReferencePointer
				Set objT = obj.ObjectProp(tag)
				If Not objT Is Nothing Then
					idDst = objT.Cookie \ 2
					If m_rgfCreated(idDst) Then
						val = idDst
					End If
				End If
			Case kcptOwningVector
				Set vobj = obj.ObjectVecProp(tag)
				cobj = vobj.Size
				If cobj > 0 Then
					sIns = ""
					If fOrdered Then
						sPre = "update CmObject set Owner$ = " & idObj & ", OwnFlid$ = " & fd.Id & ", OwnOrd$ = "
					Else
						sPre = "update CmObject set Owner$ = " & idObj & ", OwnFlid$ = " & fd.Id
					End If

					For iobj = 0 To cobj - 1
						Set objT = vobj.ItemUnknown(iobj)
						idDst = objT.Cookie \ 2
						If m_rgfCreated(idDst) Then
							UpdateObjects objT
							sIns = sIns & sPre
							If fOrdered Then sIns = sIns & iobj
							sIns = sIns & " where id = " & idDst & vbCrLf
						End If
					Next
					If sIns <> "" Then Exec sIns
				End If
			Case kcptReferenceVector
				Set vobj = obj.ObjectVecProp(tag)
				cobj = vobj.Size
				If cobj > 0 Then
					sIns = ""
					If fOrdered Then
						sPre = "insert into [" & cd.Name & "_" & sFld & "] ([Src], [Dst], [Ord]) values(" & idObj & ", "
					Else
						sPre = "insert into [" & cd.Name & "_" & sFld & "] ([Src], [Dst]) values(" & idObj & ", "
					End If

					For iobj = 0 To cobj - 1
						Set objT = vobj.ItemUnknown(iobj)
						hobj = objT.Cookie
						idDst = hobj \ 2
						If m_rgfCreated(idDst) Then
							If fOrdered Then
								sIns = sIns & sPre & idDst & ", " & iobj
							Else
								For iT = 0 To iobj - 1
									If hobj = vobj.ItemCookie(iT) Then
										Debug.Print "Skipping", m_lp.GetCookieFromObject(obj), obj.ClassName, obj.PropName(tag), hobj, vobj.Size, iT
										GoTo LSkip
									End If
								Next
								sIns = sIns & sPre & idDst
							End If
							sIns = sIns & ")" & vbCrLf
LSkip:
						End If
					Next
					If sIns <> "" Then Exec sIns
				End If
			End Select

			If Not IsNull(val) Then
				sCmd = sCmd & ",[" & fd.Name & "]=" & Literalize(val, cpt)
			End If
NextField2:
		Next
		If sCmd <> "" Then
			sCmd = "update [" & cd.Name & "] set " & Mid(sCmd, 2) & " where [ID]=" & idObj
			Exec sCmd
		End If
		idCls = cd.base
		If idCls = cd.Id Then Exit Do
		Set cd = m_dictClsId.Item(idCls)
	Loop
End Sub

Private Function Literalize(val As Variant, cpt As Long) As String
	Dim sT As String
	Dim sRes As String
	Dim ich As Long

	Select Case VarType(val)
	Case 1
		Literalize = "Null"
	Case 8
		sT = val
		sRes = "'"
		Do
			ich = InStr(1, sT, "'")
			If ich <= 0 Then
				sRes = sRes & sT & "'"
				Exit Do
			End If
			sRes = sRes & Left(sT, ich - 1) & "''"
			sT = Mid(sT, ich + 1)
		Loop
		Literalize = sRes
	Case Else
		Literalize = val
	End Select
End Function

' Answer the format string for the given (old-style) TsString
' in a form suitable to be embedded in an SQL update command.
' The string returned here would typically be inserted in something like
' "update LexSense set _Gloss_Fmt = " & StringFormat(otss)
Private Function StringFormat(ByVal enc As Long, ByVal tss As TextServicesLib.ITsString) As String
	StringFormat = "0x01000000" & Hexify(tss.Length * 2) & "01000000"
#If False Then
	Dim ttpOld As TextServicesLib.ITsTextProps
	Dim ttpNew As FwKernelLib.ITsTextProps
	Dim crun As Long
	Dim irun As Long
	Dim bstrResult As String
	bstrResult = "0x"
	Dim httpPrev As Long
	httpPrev = -1
	Dim ttpOldPrev As TextServicesLib.ITsTextProps
	Dim ttpNewPrev As FwKernelLib.ITsTextProps

	crun = tss.RunCount
	bstrResult = bstrResult & Hexify(crun)
	For irun = 0 To crun - 1
		Set ttpOld = tss.Properties(irun)
		Set ttpNew = CopyTextProps(ttpOld)
		Dim ichLim As Long
		ichLim = tss.LimOfRun(irun)
		bstrResult = bstrResult & Hexify(ichLim * 2)
		Dim http As Long
		http = m_ool.GetCookieFromTextProps(ttpNew)
		bstrResult = bstrResult & Hexify(http)
		If httpPrev = http Then
			Dim stopHere As Long
			stopHere = 0
		End If
		httpPrev = http
		Set ttpOldPrev = ttpOld
		Set ttpNewPrev = ttpNew
	Next irun
	StringFormat = bstrResult
#End If
End Function

' Answer the hexadecimal form of a long as 8 characters,
' where the first two represent the low byte, and so forth
Private Function Hexify(ByVal n As Long) As String
	Dim rgbT(3) As Byte
	Dim sRes As String
	Dim ib As Long

	CopyMem rgbT(0), n, 4

	For ib = 0 To 3
		sRes = sRes & Mid(krgchHex, rgbT(ib) \ 16 + 1, 1) & Mid(krgchHex, rgbT(ib) Mod 16 + 1, 1)
	Next
	Hexify = sRes
End Function

#If False Then
Private Function CopyTextProps(ttp As TextServicesLib.ITsTextProps) As FwKernelLib.ITsTextProps
	Dim tpb As FwKernelLib.TsPropsBldr
	Set tpb = New FwKernelLib.TsPropsBldr
	Dim cenc As Long
	Dim enc As Long
	Dim ws As Long
	Dim ttpt As Long
	Dim nVar As Long
	Dim nVal As Long
	Dim bstrVal As String
	Dim ienc As Long
	Dim ip As Long

	cenc = ttp.EncodingCount
	For ienc = 0 To cenc - 1
		ttp.GetEncoding ienc, enc, ws
		tpb.PushEncoding enc, ws
	Next ienc
	Dim cpInt As Long
	cpInt = ttp.IntPropCount
	For ip = 0 To cpInt - 1
		ttp.GetIntProp ip, ttpt, nVar, nVal
		tpb.SetIntPropValues ttpt, nVar, nVal
	Next ip
	Dim cpStr As Long
	cpStr = ttp.StringPropCount
	For ip = 0 To cpStr - 1
		ttp.GetStringProp ip, ttpt, nVar, bstrVal
		tpb.SetStrPropValues ttpt, nVar, bstrVal
	Next ip
	Set CopyTextProps = tpb.GetTextProps
End Function
#End If

Private Sub Exec(ByRef sCmd As String)
#If use_file Then
	Print #m_fn, sCmd
#ElseIf no_async Then
	m_sCur = m_sCur & sCmd & vbCrLf
	If Len(m_sCur) > kcchMax Then
		m_db.Execute "set nocount on begin tran " & m_sCur & " commit tran set nocount off"
		m_sCur = ""
	End If
#Else
	Dim tsSleep As Long

	m_sCur = m_sCur & sCmd & vbCrLf
	If Len(m_sCur) > kcchMax Then
		m_sQueued = m_sQueued & m_sCur
		m_sCur = ""
		If m_fExecuting Then
			DoEvents
			tsSleep = 10
			While m_fExecuting And Len(m_sQueued) > 8 * kcchMax
				SleepEx tsSleep, 1
				DoEvents
				If tsSleep < 1000 Then tsSleep = tsSleep * 2
			Wend
		Else
			SubmitBatch
		End If
	End If
#End If
End Sub

#If Not use_file And Not no_async Then
Private Sub m_db_ExecuteComplete(ByVal RecordsAffected As Long, ByVal pError As ADODB.Error, adStatus As ADODB.EventStatusEnum, ByVal pCommand As ADODB.Command, ByVal pRecordset As ADODB.Recordset, ByVal pConnection As ADODB.Connection)
	If m_fExecuting Then
		m_fExecuting = False
		SubmitBatch
	End If
End Sub

Private Sub SubmitBatch()
	Debug.Assert Not m_fExecuting
	If Len(m_sQueued) > 0 Then
		Debug.Print Len(m_sQueued)
		m_fExecuting = True
		m_db.Execute "set nocount on begin tran " & m_sQueued & " commit tran set nocount off", , adAsyncExecute Or adExecuteNoRecords Or adCmdText
		m_sQueued = ""
	End If
End Sub
#End If
