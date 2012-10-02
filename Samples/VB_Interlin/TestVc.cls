VERSION 1.0 CLASS
BEGIN
  MultiUse = -1  'True
  Persistable = 0  'NotPersistable
  DataBindingBehavior = 0  'vbNone
  DataSourceBehavior  = 0  'vbNone
  MTSTransactionMode  = 0  'NotAnMTSObject
END
Attribute VB_Name = "TestVc"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = False
Attribute VB_Exposed = False
Implements IVwViewConstructor

Private Sub IVwViewConstructor_Display(ByVal pvwenv As Views.IVwEnv, ByVal hvo As Long, ByVal frag As Long)
	If frag = 101 Then
		pvwenv.AddStringProp 201
	End If
End Sub

Private Function IVwViewConstructor_DisplayVariant(ByVal pvwenv As Views.IVwEnv, ByVal v As Variant, ByVal frag As Long) As FwKernelLib.ITsString

End Function

Private Sub IVwViewConstructor_DisplayVec(ByVal pvwenv As Views.IVwEnv, ByVal hvo As Long, ByVal tag As Long, ByVal frag As Long)

End Sub

Private Sub IVwViewConstructor_DoHotLinkAction(ByVal bstrGuid As String, ByVal hvoOwner As Long, ByVal tag As Long, ByVal ptss As FwKernelLib.ITsString, ByVal ichObj As Long)

End Sub

Private Function IVwViewConstructor_EstimateHeight(ByVal pvwenv As Views.IVwEnv, ByVal hvo As Long, ByVal frag As Long, ByVal dxAvailWidth As Long) As Long

End Function

Private Function IVwViewConstructor_GetStrForGuid(ByVal bstrGuid As String) As FwKernelLib.ITsString

End Function

Private Sub IVwViewConstructor_LoadDataFor(ByVal pvwenv As Views.IVwEnv, ByVal hvo As Long, ByVal frag As Long)

End Sub

Private Function IVwViewConstructor_UpdateProp(ByVal psda As Views.ISilDataAccess, ByVal hvo As Long, ByVal tag As Long, ByVal frag As Long, ByVal ptssVal As FwKernelLib.ITsString) As FwKernelLib.ITsString

End Function
