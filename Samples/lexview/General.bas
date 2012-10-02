Attribute VB_Name = "General"
Public Function GetClassId(ByRef db As Connection, ByVal objId As Long)
	Dim rsCls As Recordset

	Set rsCls = New Recordset

	' determine the type of lexical entry
	sQuery = "select Class$ Class " + _
	"From CmObject " + _
	"where id = " + Str(objId)
	rsCls.Open sQuery, db, adOpenStatic, adLockReadOnly

	If rsCls.RecordCount > 0 Then
		GetClassId = rsCls!Class
	Else
		GetClassId = -1       ' make sure class IDs are not negative
	End If

	rsCls.Close
	Set rsCls = Nothing

End Function

Public Function RemoveSpaces()

End Function
