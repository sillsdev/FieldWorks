VERSION 5.00
Begin VB.Form DataSource
   BorderStyle     =   3  'Fixed Dialog
   Caption         =   "Open Database Source"
   ClientHeight    =   1884
   ClientLeft      =   2760
   ClientTop       =   3756
   ClientWidth     =   6036
   LinkTopic       =   "Form1"
   MaxButton       =   0   'False
   MinButton       =   0   'False
   ScaleHeight     =   1884
   ScaleWidth      =   6036
   ShowInTaskbar   =   0   'False
   Begin VB.TextBox db
	  Height          =   375
	  Left            =   240
	  TabIndex        =   1
	  Text            =   "Text2"
	  Top             =   1200
	  Width           =   4095
   End
   Begin VB.TextBox server
	  Height          =   375
	  Left            =   240
	  TabIndex        =   0
	  Text            =   "Text1"
	  Top             =   360
	  Width           =   4095
   End
   Begin VB.CommandButton CancelButton
	  Cancel          =   -1  'True
	  Caption         =   "Cancel"
	  Height          =   375
	  Left            =   4680
	  TabIndex        =   4
	  Top             =   1080
	  Width           =   1215
   End
   Begin VB.CommandButton OKButton
	  Caption         =   "OK"
	  Default         =   -1  'True
	  Height          =   375
	  Left            =   4680
	  TabIndex        =   2
	  Top             =   480
	  Width           =   1215
   End
   Begin VB.Label Label1
	  Caption         =   "Server Machine"
	  Height          =   255
	  Index           =   1
	  Left            =   240
	  TabIndex        =   5
	  Top             =   120
	  Width           =   2175
   End
   Begin VB.Label Label2
	  Caption         =   "Database"
	  Height          =   255
	  Index           =   0
	  Left            =   240
	  TabIndex        =   3
	  Top             =   960
	  Width           =   2175
   End
End
Attribute VB_Name = "DataSource"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False

Option Explicit

Private Sub CancelButton_Click()
	server.Text = ""
	db.Text = ""
	DataSource.Hide
End Sub

Private Sub OKButton_Click()
	DataSource.Hide
End Sub
