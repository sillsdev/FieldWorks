'//-------------------------------------------------------------------------------------------------
'// <copyright file="wix.rc" company="Microsoft">
'//    Copyright (c) Microsoft Corporation.  All rights reserved.
'//
'//    The use and distribution terms for this software are covered by the
'//    Common Public License 1.0 (http://opensource.org/licenses/cpl.php)
'//    which can be found in the file CPL.TXT at the root of this distribution.
'//    By using this software in any fashion, you are agreeing to be bound by
'//    the terms of this license.
'//
'//    You must not remove this notice, or any other, from this software.
'// </copyright>
'//
'// <summary>
'// Takes an XML file and compacts it by removing all unnecessary whitespace.
'// </summary>
'//-------------------------------------------------------------------------------------------------

Option Explicit
If 2 > WScript.Arguments.Count Then
	WScript.Echo "Must specify xml document to flatten and where to write output"
	WScript.Quit 2
End If

Dim xmlDoc : Set xmlDoc = WScript.CreateObject("Microsoft.XMLDOM")
xmlDoc.preserveWhiteSpace = False
xmlDoc.async = False
xmlDoc.validateOnParse = False

Dim fso : Set fso = WScript.CreateObject("Scripting.FileSystemObject")
Dim output : Set output = fso.CreateTextFile(WScript.Arguments(1), -1, 0)

Dim loaded : loaded = xmlDoc.load(WScript.Arguments(0))
If Not loaded Then
	Dim pe :  Set pe = xmlDoc.parseError
	Dim sErr : sErr = "Failed to load XML file: " & pe.url & vbCrLf & "   " & pe.errorCode & " - " & pe.reason & vbCrLf & "   Line:" & pe.line & ", Character: " & pe.linepos
	WScript.Echo sErr
	WScript.Quit 1
End If

output.Write "<?xml version='1.0'?>"
Dim node
For Each node in xmlDoc.childNodes
	DisplayNode output, node
Next
WScript.Quit 0

Function DisplayNode(output, node)
	If 1 <> node.nodeType Then
		DisplayNode = ""
		Exit Function
	End If
	If "annotation" = node.baseName Then ' skip annotation blocks
		DisplayNode = ""
		Exit Function
	End If

	Dim textNode : Set textNode = node.selectSingleNode("text()")

	output.Write "<" & node.nodeName
	Dim attrib
	For Each attrib in node.attributes
		output.Write " " & attrib.Name & "='" & Replace(Replace(Replace(attrib.Value, "'", "&apos;"), ">", "&gt;"), "<", "&lt;") & "'"
	Next

	If Not textNode Is Nothing Or 0 < node.childNodes.Length Then
		output.Write ">"

		If Not textNode is Nothing Then output.Write textNode.nodeValue

		Dim child
		For Each child In node.childNodes
			DisplayNode output, child
		Next
		output.Write "</" & node.nodeName & ">"
	Else
		output.Write "/>"
	End If

	Set DisplayNode = output
End Function
