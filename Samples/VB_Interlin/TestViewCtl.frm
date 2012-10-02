VERSION 5.00
Object = "{F7DC6325-7613-4DB8-AC71-23B2D206C418}#1.0#0"; "ViewCtl.dll"
Begin VB.Form Form1
   Caption         =   "Form1"
   ClientHeight    =   3195
   ClientLeft      =   60
   ClientTop       =   345
   ClientWidth     =   4815
   LinkTopic       =   "Form1"
   ScaleHeight     =   3195
   ScaleWidth      =   4815
   StartUpPosition =   3  'Windows Default
   Begin VB.ComboBox TagCombo
	  Height          =   315
	  Left            =   3720
	  TabIndex        =   1
	  Text            =   "TagCombo"
	  Top             =   2280
	  Visible         =   0   'False
	  Width           =   735
   End
   Begin VIEWCTLLibCtl.SilView SilView1
	  Height          =   2415
	  Left            =   360
	  OleObjectBlob   =   "TestViewCtl.frx":0000
	  TabIndex        =   2
	  Top             =   480
	  Width           =   3135
   End
   Begin VB.CommandButton Outline
	  Caption         =   "Choose"
	  Height          =   375
	  Left            =   3720
	  TabIndex        =   0
	  Top             =   600
	  Width           =   855
   End
End
Attribute VB_Name = "Form1"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Option Explicit
Dim m_scn As SelChangeNotifier

Private Sub Form_Load()
	Dim rootb As IVwRootBox
	Set rootb = SilView1.RootBox
	Dim vda As IVwCacheDa
	Set vda = New VwCacheDa
	Set rootb.DataAccess = vda
	Set m_scn = New SelChangeNotifier
	' Review JohnT: do we need to call the RemoveSelChngListener
	' when we close the form?
	rootb.AddSelChngListener m_scn

	' identifiers for four (for now) words
	Dim cword As Integer
	cword = 4
	Dim hvoWords() As Long
	ReDim hvoWords(cword)
	Dim i As Integer
	For i = 0 To cword - 1
		hvoWords(i) = i + 1
	Next i

	' Make word/ann pairs for each word object
	Dim strWords() As String
	ReDim strWords(cword)
	Dim strAnn() As String
	ReDim strAnn(cword)
	strWords(0) = "This"
	strAnn(0) = "det"
	strWords(1) = "is"
	strAnn(1) = "verb"
	strWords(2) = "interlinear"
	strAnn(2) = "adj"
	strWords(3) = "text"
	strAnn(3) = "noun"
	Dim tsf As ITsStrFactory
	Set tsf = New TsStrFactory
	Dim tss As ITsString
	For i = 0 To cword - 1
		Set tss = tsf.MakeString(strWords(i), 0)
		vda.CacheStringProp hvoWords(i), 202, tss
		Set tss = tsf.MakeString(strAnn(i), 0)
		vda.CacheStringProp hvoWords(i), 203, tss
		'property 204 indicates the correctness status
		'by default all are assumed incorrect
		vda.CacheIntProp hvoWords(i), 204, 0
	Next i

	' Make the sentence have the sequence of words
	Dim hvoSent As Long
	hvoSent = 1001
	vda.CacheVecProp hvoSent, 201, hvoWords(0), cword

	Dim tivc As TestInterlinVc
	Set tivc = New TestInterlinVc
	rootb.SetRootObject hvoSent, tivc, 101, Nothing

	'Give the combo box we use for pop-ups some items.
	TagCombo.AddItem ("Det")
	TagCombo.AddItem ("Noun")
	TagCombo.AddItem ("Verb")
	TagCombo.AddItem ("Adj")
	TagCombo.AddItem ("Conj")

End Sub

Public Sub SelChange()
	Dim rootb As IVwRootBox
	Set rootb = SilView1.RootBox
	Dim sel As IVwSelection
	Set sel = rootb.Selection
	If sel Is Nothing Then Exit Sub

	'Now figure out what is selected
	'All these are values returned.
	Dim tssSel As ITsString
	Dim ichSel As Long
	Dim fAssocPrev As Boolean
	Dim hvoSel As Long
	Dim tagSel As Long
	Dim encSel As Long
	sel.TextSelInfo False, tssSel, ichSel, fAssocPrev, hvoSel, tagSel, encSel
	' If not in the annotation property no special behavior
	If tagSel <> 203 Then Exit Sub
	Dim strSel As String
	strSel = tssSel.Text
	TagCombo.Text = strSel
	 'Get a bounding rectangle for the selection
	'relative to the SilView1 window
	Dim rcLoc As tagRECT
	rootb.Selection.GetParaLocation rcLoc

	'rcLoc is in pixels relative to top left of SilView1.
	'Top, Left, etc. of TagCombo are in twips, so convert
	'Todo JohnH (JohnT): work out an official way to get
	'the device resolution
	Dim dxpInch As Integer ' H pixels per inch
	dxpInch = 96
	Dim dypInch As Integer ' V pixels per inch
	dypInch = 96
	Dim dztwInch As Integer ' pixels per inch (H or V)
	dztwInch = 20 * 72

	rcLoc.Left = rcLoc.Left * dztwInch / dxpInch
	rcLoc.Right = rcLoc.Right * dztwInch / dxpInch
	rcLoc.Top = rcLoc.Top * dztwInch / dypInch
	rcLoc.bottom = rcLoc.bottom * dztwInch / dypInch

	TagCombo.Visible = False
	TagCombo.Left = SilView1.Left + rcLoc.Left
	' Extra 200 allows combo to use up extra space between
	' bundles and makes text more visible
	' (But it isn't really enough--try for a number that
	' lets the word be read, even if it overlaps the next
	' word.)
	TagCombo.Width = rcLoc.Right - rcLoc.Left + 500
	TagCombo.Top = SilView1.Top + rcLoc.Top
	'This property is read-only...we don't seem to be able
	' to change the height?
	'TagCombo.Height = rcLoc.Bottom - rcLoc.Top

	'Now all its properties are correct and it's in the right place,
	'make it visible.
	TagCombo.Visible = True
End Sub

Private Sub TagCombo_Click()
	Dim rootb As IVwRootBox
	Set rootb = SilView1.RootBox
	Dim sda As ISilDataAccess
	Set sda = rootb.DataAccess
	Dim sel As IVwSelection
	Set sel = rootb.Selection
	If sel Is Nothing Then
		' Hide it to indicate the user needs to select
		TagCombo.Visible = False
		Exit Sub
	End If

	' All these are values returned.
	Dim tssSel As ITsString
	Dim ichSel As Long
	Dim fAssocPrev As Boolean
	Dim hvoSel As Long
	Dim tagSel As Long
	Dim encSel As Long
	sel.TextSelInfo False, tssSel, ichSel, fAssocPrev, hvoSel, tagSel, encSel

	' The user has chosen something, assume correct.
	' The string is potentially for an undo menu item.
	sda.SetInt hvoSel, 204, 1
	Dim strNew As String
	strNew = TagCombo.List(TagCombo.ListIndex)
	If tssSel.Text <> strNew Then
		Dim tsf As ITsStrFactory
		Set tsf = New TsStrFactory
		Dim tss As ITsString
		Set tss = tsf.MakeString(strNew, 0)
		' Part of the same undo item, so pass an empty string.
		sda.SetString hvoSel, 203, tss
	End If
	' Broadcast that the annotation and the spelling status changed
	' Actually just broadcasting that the status changed forces
	' a redraw that includes the annotation, but in general we
	' should do both in case something else depends on it.
	sda.PropChanged Nothing, kpctNotifyAll, hvoSel, 203, 0, 0, 0
	sda.PropChanged Nothing, kpctNotifyAll, hvoSel, 204, 0, 0, 0
	' And hide the combo so we can see the result
	' (Also because we have destroyed the selection, so using it
	' again won't work)
	TagCombo.Visible = False
End Sub
