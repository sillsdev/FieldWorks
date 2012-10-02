VERSION 1.0 CLASS
BEGIN
  MultiUse = -1  'True
  Persistable = 0  'NotPersistable
  DataBindingBehavior = 0  'vbNone
  DataSourceBehavior  = 0  'vbNone
  MTSTransactionMode  = 0  'NotAnMTSObject
END
Attribute VB_Name = "FldData"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = False
Attribute VB_Exposed = False
Option Explicit

Dim m_id As Long
Dim m_cls As Long
Dim m_fldt As Long
Dim m_cpt As Long
Dim m_sName As String

Public Sub Init(Id As Long, cls As Long, fldt As Long, sName As String)
	m_id = Id
	m_cls = cls
	m_fldt = fldt

	m_cpt = kcptNil
	Select Case fldt
	Case kcptBoolean
		m_cpt = kcptBoolean
	Case kcptInteger
		m_cpt = kcptInteger

	Case kcptString, kcptBigString
		m_cpt = kcptString
	Case kcptMultiString, kcptMultiUnicode, kcptMultiBigString, kcptMultiBigUnicode
		m_cpt = kcptMultiString
	Case kcptUnicode, kcptBigUnicode
		m_cpt = kcptUnicode

	Case kcptOwningAtom
		m_cpt = kcptOwningPointer
	Case kcptReferenceAtom
		m_cpt = kcptReferencePointer
	Case kcptOwningCollection, kcptOwningSequence
		m_cpt = kcptOwningVector
	Case kcptReferenceCollection, kcptReferenceSequence
		m_cpt = kcptReferenceVector
	End Select

	m_sName = sName
End Sub

Public Property Get fldt() As Long
	fldt = m_fldt
End Property

Public Property Get cpt() As Long
	cpt = m_cpt
End Property

Public Property Get Name() As String
	Name = m_sName
End Property
Public Property Get Id() As Long
	Id = m_id
End Property
