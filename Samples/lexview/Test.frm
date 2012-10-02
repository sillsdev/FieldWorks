VERSION 5.00
Object = "{3B7C8863-D78F-101B-B9B5-04021C009402}#1.2#0"; "RICHTX32.OCX"
Begin VB.Form Form1
   Caption         =   "Form1"
   ClientHeight    =   6510
   ClientLeft      =   60
   ClientTop       =   345
   ClientWidth     =   7710
   LinkTopic       =   "Form1"
   ScaleHeight     =   6510
   ScaleWidth      =   7710
   StartUpPosition =   3  'Windows Default
   Begin RichTextLib.RichTextBox RichTextBox1
	  Height          =   5055
	  Left            =   240
	  TabIndex        =   0
	  Top             =   1080
	  Width           =   7215
	  _ExtentX        =   12726
	  _ExtentY        =   8916
	  _Version        =   393217
	  Enabled         =   -1  'True
	  TextRTF         =   $"Test.frx":0000
   End
End
Attribute VB_Name = "Form1"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Option Explicit

Dim m_ldb As ldb
Dim m_PossList As PossList

Dim fwdb As FwDatabase

Private Sub Form_Load()

	Set fwdb = New FwDatabase
	fwdb.Open "ac-zookk", "TestLangProj"

	Set m_ldb = New ldb
	m_ldb.Conn = fwdb
	' !!! pick the first language project
	Call m_ldb.SetEthnologueCode("FRN")

	m_ldb.GlossEnc = 740664001  ' will come from the language project DB in the future
	m_ldb.VernEnc = 931905001   ' will come from the language project DB in the future


	Call m_ldb.DisplayLexEntry(657, Me.RichTextBox1)
End Sub

Private Sub Form_Unload(Cancel As Integer)
	Call m_ldb.DestroyLdb
End Sub
