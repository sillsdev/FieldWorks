VERSION 1.0 CLASS
BEGIN
  MultiUse = -1  'True
  Persistable = 0  'NotPersistable
  DataBindingBehavior = 0  'vbNone
  DataSourceBehavior  = 0  'vbNone
  MTSTransactionMode  = 0  'NotAnMTSObject
END
Attribute VB_Name = "Ldb"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = False
Attribute VB_Exposed = False
Option Explicit

' define class ID constants
Const kEntry = 5002
Const kMajorEntry = 5008
Const kMinorEntry = 5009
Const kSubEntry = 5020
Const kInflVar = 5007
Const kPhonVar = 5012

Const kMoDerAff = 5031
Const kMoDerStep = 5032
Const kMoInfAffix = 5038
Const kMoStem = 5001

Private m_db As Connection
' Private m_rsHdWrd As New Recordset
Private m_rsSense As New Recordset
Private m_ldid As Long
Private m_GlossEnc As Long
Private m_VernEnc As Long
Private m_rtfStr As String
Private m_HWHomographNo As Long
Private m_HWTxt As String

Public Property Let Conn(ByRef db As Connection)
	Set m_db = db
End Property

Public Property Let LexDBId(ByVal ldid As Long)
	m_ldid = ldid
End Property

Public Property Get LexDBId() As Long
	LexDBId = m_ldid
End Property

Public Property Let GlossEnc(ByVal nEnc)
	m_GlossEnc = nEnc
End Property

Public Property Let VernEnc(ByVal nEnc)
	m_VernEnc = nEnc
End Property

Public Function SetEthnologueCode(ByVal sEnc As String) As Boolean
	Dim rsCls As Recordset
	Dim sQuery As String

	Set rsCls = New Recordset

	' TODO: what if there is more than one lexical database assoicated with a language project
	sQuery = "select LexicalDatabase from LanguageProject where EthnologueCode = '" + sEnc + "'"
	rsCls.Open sQuery, m_db, adOpenForwardOnly, adLockReadOnly

	If rsCls.RecordCount = 0 Then
		SetEthnologueCode = False
	Else
		SetEthnologueCode = True
		m_ldid = rsCls!LexicalDatabase
	End If

	rsCls.Close
	Set rsCls = Nothing
End Function

Public Function DisplayLexEntry(ByVal nLexEntry As Long, ByVal HWTxt As String, ByVal HWHomographNo As Long, ByRef rtb As RichTextBox) As Boolean
	Dim nEntryId As Long
	Dim nLen As Integer

	m_HWTxt = HWTxt
	m_HWHomographNo = HWHomographNo

	m_rtfStr = "{\rtf1"

'    If Not GetHeadWordInfo(nLexEntry) Then
'        m_rtfStr = "{Missing headword}"
'    Else
'        Call DisplayHeadWord
'    End If
	Call DisplayHeadWord

	' determine the type of lexical entry
	Select Case GetClassId(m_db, nLexEntry)
		Case kMajorEntry, kSubEntry
			Call GetMajorEntry(nLexEntry)
			Call DisplayMajorEntry(nLexEntry)
			m_rsSense.Close
'            m_rsHdWrd.Close
		Case kInflVar, kPhonVar
			nEntryId = GetMinorEntry(nLexEntry)
'            m_rsHdWrd.Close
			Call GetHeadWordInfo(nEntryId)
			Call DisplayMinorEntry
'            m_rsHdWrd.Close
		Case Else
			Print 'Error !'
			' TODO: handle error
	End Select

	m_rtfStr = m_rtfStr + "}"
	rtb.TextRTF = m_rtfStr
End Function

Private Sub DisplayHeadWord()

	' head word text
'    m_rtfStr = m_rtfStr + "{\b " + m_rsHdWrd!txt
	m_rtfStr = m_rtfStr + "{\b " + m_HWTxt

	' homograph number
'    If m_rsHdWrd!HomographNumber <> 0 Then
	If m_HWHomographNo <> 0 Then
'        m_rtfStr = m_rtfStr + "}{\b\sub " + Trim(Str(m_rsHdWrd!HomographNumber))
		m_rtfStr = m_rtfStr + "}{\b\sub " + Trim(Str(m_HWHomographNo))
	End If
	m_rtfStr = m_rtfStr + "}  "

End Sub

Private Function GetMinorEntry(ByVal nLexEntry As Long) As Long
	Dim sQuery As String
	Dim rsCls As New Recordset

	sQuery = "select MainEntryOrSense as id from LexMinorEntry where id = " & Str(nLexEntry)
	rsCls.Open sQuery, m_db, adOpenForwardOnly, adLockReadOnly

	If rsCls.RecordCount < 1 Then
		GetMinorEntry = -1
	Else
		GetMinorEntry = rsCls!id
	End If

End Function

Private Sub DisplayMinorEntry()

	m_rtfStr = m_rtfStr + "{ (var of }"
	Call DisplayHeadWord
	m_rtfStr = m_rtfStr + "{)}"

End Sub

Private Sub DisplayMajorEntry(ByVal nLexEntry As Long)
	Dim sQuery As String
	Dim sPosA As String
	Dim nLexId As Long
	Dim bDone As Boolean
	Dim rsCls As New Recordset
	Dim rsCls2 As New Recordset

	Dim nPrevDepth As Integer
	Dim aSensNum() As Integer
	Dim sSensNum As String
	Dim i As Integer

	' loop through each sense and display its information
	With m_rsSense

	If .RecordCount < 1 Then
		Exit Sub
	End If

	ReDim aSensNum(10)
	aSensNum(0) = 0
	.MoveFirst
	While Not .EOF()

		' build the sense number
		If !depth <= nPrevDepth Then
			aSensNum(!depth) = aSensNum(!depth) + 1
		ElseIf !depth > nPrevDepth Then
			If !depth > UBound(aSensNum) Then
				ReDim Preserve aSensNum(UBound(aSensNum) + 10)
			End If
			aSensNum(!depth) = 1
		End If
		sSensNum = Str(aSensNum(0))
		For i = 1 To !depth
			sSensNum = sSensNum & "." & Trim(Str(aSensNum(i)))
		Next i
		nPrevDepth = !depth

		m_rtfStr = m_rtfStr + "{\b " + sSensNum + "}"

		sQuery = "select  ls.morphosyntaxinfo msi, lgs.txt TxtGloss, lgs.fmt FmtGloss, lds.txt TxtDef, lds.fmt FmtDef " + _
				"from   lexSense ls" + _
				"           left outer join LexSense_Gloss lgs on lgs.obj = ls.id and lgs.enc = " + Str(m_GlossEnc) + _
				"           left outer join LexSense_Definition lds on lds.obj = ls.id and lds.enc = " + Str(m_GlossEnc) + _
				"Where  ls.id = " + Str(!sensId)

		rsCls.Open sQuery, m_db, adOpenForwardOnly, adLockReadOnly

		If rsCls.RecordCount > 0 Then
			rsCls.MoveFirst

			' display sense information
			If rsCls!msi <> "" Then
				 sPosA = GetSenseInfo(rsCls!msi)
				 If sPosA <> "" Then
					m_rtfStr = m_rtfStr + "{ \i " + sPosA + "} "
				 End If
			End If

			' display the gloss and definition
			m_rtfStr = m_rtfStr + "{"
			With rsCls
				If !TxtGloss <> "" Then m_rtfStr = m_rtfStr + " '" + !TxtGloss + "'"
				If !TxtDef <> "" Then m_rtfStr = m_rtfStr + " " + !TxtDef
			End With
			m_rtfStr = m_rtfStr + "}"

			' display the example sentences with translations
			If rsCls!msi <> "" Then

' This query includes the abbreviation for the translation type
'                        sQuery = "select ab.txt type, les.id Id, lets.txt vernTxt, lets.fmt vernFmt, lts.txt transTxt, lts.fmt transFmt " + _
'                                "from lexsense_examples let " + _
'                                "    join lexexamplesentence les on let.Dst = les.id " + _
'                                "    left outer join lexexamplesentence_example lets on let.Dst = lets.obj and lets.enc = " + Str(m_VernEnc) + _
'                                "    left outer join lexexamplesentence_translations ltt on ltt.Src = les.id " + _
'                                "    left outer join lptranslation_translation lts on ltt.Dst = lts.obj and lts.enc = " + Str(m_GlossEnc) + _
'                                "    left outer join lptranslation lt on lt.id = ltt.Dst " + _
'                                "    left outer join lppossibility lp on lt.type = lp.id " + _
'                                "    left outer join cmpossibility_abbreviation ab on lp.id = ab.obj and ab.enc = " + Str(m_GlossEnc) + _
'                                "where   let.Src = " + Str(m_rsSense!sensId) + _
'                                "order by let.ord "
'
				sQuery = "select les.id Id, coalesce(lets.txt, '') vernTxt, lets.fmt vernFmt, " + _
						"   coalesce(lts.txt, '') transTxt, lts.fmt transFmt " + _
						"from lexsense_examples let " + _
						"    join lexexamplesentence les on let.Dst = les.id " + _
						"    left outer join lexexamplesentence_example lets on let.Dst = lets.obj and lets.enc = " + Str(m_VernEnc) + _
						"    left outer join lexexamplesentence_translations ltt on ltt.Src = les.id " + _
						"    left outer join lptranslation_translation lts on ltt.Dst = lts.obj and lts.enc = " + Str(m_GlossEnc) + _
						"where   let.Src = " + Str(m_rsSense!sensId) + _
						"order by let.ord "
				rsCls2.Open sQuery, m_db, adOpenForwardOnly, adLockReadOnly

				If rsCls2.RecordCount > 0 Then
					rsCls2.MoveFirst
					nLexId = rsCls2!id
					While Not rsCls2.EOF()
						' build the vernacular example text
						m_rtfStr = m_rtfStr + "{ \i " + rsCls2!vernTxt + "}"
						bDone = False
						Do While Not bDone
' This displays the translation type - see the above commented out query
'                                    m_rtfStr = m_rtfStr + "{  " + rsCls2!Type + "}"
							m_rtfStr = m_rtfStr + "{  " + rsCls2!transTxt + "}"

							rsCls2.MoveNext
							If rsCls2.EOF Then
								bDone = True
							ElseIf rsCls2!id <> nLexId Then
								m_rtfStr = m_rtfStr + "{,}"
								bDone = True
							End If
						Loop
					Wend
				End If

				rsCls2.Close
			End If
		End If

		rsCls.Close

		.MoveNext
	Wend

	End With

End Sub

Private Function GetHeadWordInfo(ByVal nLexEntry As Long) As Boolean
	Dim sQuery As String
	Dim nGlossEnc As Long
	Dim sEnc As String
	Dim rsHdWrd As New Recordset

	GetHeadWordInfo = True

	nGlossEnc = 0
	sQuery = "select le.id, " + _
			"   le.HomographNumber," + _
			"   coalesce(mmt.prefix, '')+coalesce(mfs.txt, '')+coalesce(mmt.postfix, '') as txt, " + _
			"   mfs.fmt " + _
			"from lexicaldatabase_entries let" + _
			"   join lexentry le on let.Dst = le.id " + _
			"   join lexentry_forms lft on le.id = lft.Src " + _
			"   join MoForm mf on lft.Dst = mf.id " + _
			"   left outer join MoForm_Form mfs on mf.id = mfs.obj and mfs.enc = " + Str(m_VernEnc) + _
			"   left outer join MoMorphType mmt on mf.morphtype = mmt.id " + _
			"where let.Src = " + Str(m_ldid) + _
			"   and le.id = " + Str(nLexEntry) + _
			"   and le.IsIncludedAsHeadword = 0 " + _
			"   and mfs.enc = " + Str(m_VernEnc)

	rsHdWrd.Open sQuery, m_db, adOpenForwardOnly, adLockReadOnly

	If rsHdWrd.RecordCount > 0 Then
		rsHdWrd.MoveFirst
		m_HWTxt = rsHdWrd!txt
		m_HWHomographNo = rsHdWrd!HomographNumber
	Else
		GetHeadWordInfo = False
	End If

End Function

Private Function GetMajorEntry(ByVal nLexEntry As Long) As Boolean
	Dim cmd As Command
	Dim prmLexEntry As Parameter
	Dim nClassId As Long

	Set cmd = New Command

	cmd.ActiveConnection = m_db
	cmd.CommandType = adCmdStoredProc
	cmd.CommandText = "getSensesInEntry"

	Set prmLexEntry = New Parameter
	prmLexEntry.Type = adInteger
	prmLexEntry.Direction = adParamInput
	prmLexEntry.Value = nLexEntry
	cmd.Parameters.Append prmLexEntry

	Set m_rsSense = cmd.Execute()

End Function

Private Function GetSenseInfo(ByVal nMsi As Long) As String
	Dim sQuery As String
	Dim rsCls As Recordset

	Set rsCls = New Recordset

	Select Case GetClassId(m_db, nMsi)
		Case kMoDerAff
			sQuery = "select  posa.Txt txt, posa.fmt fmt " + _
					"from    MoDerivationalAffixMsi amsi " + _
					"        left outer join PartOfSpeech pos on amsi.FromPartOfSpeech = pos.id " + _
					"        left outer join CmPossibility_Abbreviation posa on posa.obj = pos.id " + _
					"Where amsi.id = " + Str(nMsi) + _
					"    and enc = " + Str(m_GlossEnc)
		Case kMoDerStep
			sQuery = "select  posa.Txt txt, posa.fmt fmt " + _
					"from    MoDerivationalStepMsi dmsi " + _
					"        left outer join PartOfSpeech pos on dmsi.PartOfSpeech = pos.id " + _
					"        left outer join CmPossibility_Abbreviation posa on posa.obj = pos.id " + _
					"Where dmsi.id = " + Str(nMsi) + _
					"    and enc = " + Str(m_GlossEnc)
		Case kMoInfAffix
			sQuery = ""
			' TODO:
		Case kMoStem
			sQuery = "select  posa.Txt txt, posa.fmt fmt " + _
					"from    MoStemMsi smsi " + _
					"        left outer join PartOfSpeech pos on smsi.PartOfSpeech = pos.id " + _
					"        left outer join CmPossibility_Abbreviation posa on posa.obj = pos.id " + _
					"Where smsi.id = " + Str(nMsi) + _
					"    and enc = " + Str(m_GlossEnc)
		Case Else
			' TODO: handle error
	End Select

	rsCls.Open sQuery, m_db, adOpenForwardOnly, adLockReadOnly

	GetSenseInfo = rsCls!txt

	rsCls.Close
	Set rsCls = Nothing
End Function

Public Function AddGetSenseProc() As Boolean
	Dim sQuery As String
	Dim rsCls As New Recordset

	On Error GoTo Failed

	' determine if the procedure exists
	sQuery = "select * from sysobjects where id = object_id('dbo.GetSensesInEntry') "
	rsCls.Open sQuery, m_db, adOpenForwardOnly, adLockReadOnly

	If rsCls.RecordCount = 0 Then
		rsCls.Close

		' add the stored procedure
		sQuery = "create procedure dbo.GetSensesInEntry " & _
		" @EntryId as integer " & _
		"as " & _
		"declare @nCurDepth int,@rowCnt int,@str varchar(100),@SenseId int " & _
		" set @nCurDepth = 0 " & _
		" create table #lexSenses (ownrId int,sensId int,ord int,depth int,sensNum nvarchar(1000)) " & _
		" set nocount on " & _
		" insert into #lexSenses " & _
		" select mjet.Src,mjet.Dst,mjet.ord,@nCurDepth,replicate(' ',5-len(convert(nvarchar(10),mjet.ord+1)))+convert(nvarchar(10),mjet.ord+1) " & _
		" from lexMajorEntry_Senses mjet " & _
		" where mjet.Src = @EntryId " & _
		" set @rowCnt = @@rowcount " & _
		" while @rowCnt > 0 begin " & _
		"  set @nCurDepth = @nCurDepth + 1 " & _
		"  insert into #lexSenses " & _
		"  select ls.sensId, lst.Dst, lst.ord, @nCurDepth, sensNum+'.'+replicate(' ', 5-len(convert(nvarchar(10), lst.ord+1)))+convert(nvarchar(10), lst.ord+1) " & _
		"  from #lexSenses ls join lexSense_Senses lst on ls.sensId = lst.Src " & _
		"  where depth = @nCurDepth - 1 " & _
		"  set @rowCnt = @@rowcount " & _
		" end " & _
		" set nocount off " & _
		" select * from #lexSenses order by sensNum " & _
		" drop table #lexSenses "
		rsCls.Open sQuery, m_db, adOpenForwardOnly, adLockReadOnly
	End If

	' because ADO does not handle the "The command(s) completed successfully." comming back from SQL-Server
	'    check to see if the record set is still open, if it is then close it
	If rsCls.State = adStateOpen Then
		rsCls.Close
	End If

	AddGetSenseProc = True
	Exit Function

Failed:
	MsgBox ("Unable to add stored procedure GetSensesInEntry()")
	AddGetSenseProc = False
End Function
