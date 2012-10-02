VERSION 1.0 CLASS
BEGIN
  MultiUse = -1  'True
  Persistable = 0  'NotPersistable
  DataBindingBehavior = 0  'vbNone
  DataSourceBehavior  = 0  'vbNone
  MTSTransactionMode  = 0  'NotAnMTSObject
END
Attribute VB_Name = "ClassData"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = False
Attribute VB_Exposed = False
Option Explicit

Dim m_id As Long
Dim m_base As Long
Dim m_sName As String
Dim m_dictFld As Dictionary

Public Sub Init(Id As Long, base As Long, sName As String, ByVal db As Connection)
	m_id = Id
	m_base = base
	m_sName = sName

	Set m_dictFld = New Dictionary

	Dim rs As New Recordset
	rs.Open "select * from [Field$] where [Class]=" & Id, db, adOpenStatic, adLockReadOnly
	While Not rs.EOF
		Dim fd As FldData
		Dim sFld As String

		Set fd = New FldData
		sFld = rs!Name
		fd.Init rs!Id, rs!Class, rs!Type, sFld
		m_dictFld.Add sFld, fd
		rs.MoveNext
	Wend
	rs.Close
End Sub

Public Property Get Fields() As Dictionary
	Set Fields = m_dictFld
End Property

Public Property Get Id() As Long
	Id = m_id
End Property

Public Property Get base() As Long
	base = m_base
End Property

Public Property Get Name() As String
	Name = m_sName
End Property
