VERSION 1.0 CLASS
BEGIN
  MultiUse = -1  'True
  Persistable = 0  'NotPersistable
  DataBindingBehavior = 0  'vbNone
  DataSourceBehavior  = 0  'vbNone
  MTSTransactionMode  = 0  'NotAnMTSObject
END
Attribute VB_Name = "TestInterlinVc"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = False
Attribute VB_Exposed = False
Implements IVwViewConstructor

Private Sub IVwViewConstructor_Display(ByVal pvwenv As Views.IVwEnv, ByVal hvo As Long, ByVal frag As Long)
	If frag = 101 Then
		'sequence of interlin words
		pvwenv.OpenParagraph
		pvwenv.AddObjVecItems 201, Me, 102
		pvwenv.CloseParagraph
		Exit Sub
	End If
	If frag = 102 Then
		'one interlinear bundle
		pvwenv.IntProperty(ktptMarginLeading, ktpvMilliPoint) = 10000
		pvwenv.OpenInnerPile
		pvwenv.AddStringProp 202 'word
		Dim sda As ISilDataAccess
		Set sda = pvwenv.DataAccess
		Dim nSpellStatus As Long
		nSpellStatus = sda.IntProp(hvo, 204)
		Dim red As Long
		red = 255 '*256 for blue, *256 again green
		If nSpellStatus = 0 Then
			'incorrect...red squiggle underline
			pvwenv.IntProperty(ktptUnderColor, ktpvDefault) = red
			pvwenv.IntProperty(ktptUnderline, ktpvEnum) = kuntSquiggle
		End If

		' This bit of the display (it will be the property containing the
		' inner pile) needs to be regenerated if this property changes,
		' since it could affect the underlining.
		pvwenv.NoteDependency hvo, 204, 1

		pvwenv.AddStringProp 203 'annotation
		pvwenv.CloseInnerPile
		Exit Sub
	End If
End Sub

Private Function IVwViewConstructor_DisplayVariant(ByVal pvwenv As Views.IVwEnv, ByVal v As Variant, ByVal frag As Long) As FwKernelLib.ITsString

End Function

Private Sub IVwViewConstructor_DisplayVec(ByVal pvwenv As Views.IVwEnv, ByVal hvo As Long, ByVal tag As Long, ByVal frag As Long)

End Sub

Private Sub IVwViewConstructor_DoHotLinkAction(ByVal bstrGuid As String, ByVal hvoOwner As Long, ByVal tag As Long, ByVal ptss As FwKernelLib.ITsString, ByVal ichObj As Long)

End Sub

Private Function IVwViewConstructor_EstimateHeight(ByVal hvo As Long, ByVal frag As Long, ByVal dxAvailWidth As Long) As Long

End Function

Private Function IVwViewConstructor_GetStrForGuid(ByVal bstrGuid As String) As FwKernelLib.ITsString

End Function

Private Sub IVwViewConstructor_LoadDataFor(ByVal pvwenv As Views.IVwEnv, rghvo As Long, ByVal chvo As Long, ByVal hvoParent As Long, ByVal tag As Long, ByVal frag As Long, ByVal ihvoMin As Long)

End Sub

Private Function IVwViewConstructor_UpdateProp(ByVal psda As Views.ISilDataAccess, ByVal hvo As Long, ByVal tag As Long, ByVal frag As Long, ByVal ptssVal As FwKernelLib.ITsString) As FwKernelLib.ITsString

End Function
