Imports System.IO

Public Class AppFileListDlg
	Inherits System.Windows.Forms.Form

#Region " Windows Form Designer generated code "

	Public Sub New(ByVal sDirName As String)
		MyBase.New()

		'This call is required by the Windows Form Designer.
		InitializeComponent()

		'Add any initialization after the InitializeComponent() call
		Me.LabelAppName.Text = String.Format(Me.LabelAppName.Text, sDirName)
		If (Directory.Exists(sDirName)) Then
			RecurseSubDirs(sDirName)
		Else
			ListBoxFiles.Items.Add(sDirName)
		End If
	End Sub

	Protected Sub RecurseSubDirs(ByVal sDirName As String)
		ListBoxFiles.Items.AddRange(Directory.GetFiles(sDirName))

		For Each sDirName In Directory.GetDirectories(sDirName)
			RecurseSubDirs(sDirName)
		Next
	End Sub

	'Form overrides dispose to clean up the component list.
	Protected Overloads Overrides Sub Dispose(ByVal disposing As Boolean)
		If disposing Then
			If Not (components Is Nothing) Then
				components.Dispose()
			End If
		End If
		MyBase.Dispose(disposing)
	End Sub

	'Required by the Windows Form Designer
	Private components As System.ComponentModel.IContainer

	'NOTE: The following procedure is required by the Windows Form Designer
	'It can be modified using the Windows Form Designer.
	'Do not modify it using the code editor.
	Friend WithEvents myCancelButton As System.Windows.Forms.Button
	Friend WithEvents LabelAppName As System.Windows.Forms.Label
	Friend WithEvents ListBoxFiles As System.Windows.Forms.ListBox
	<System.Diagnostics.DebuggerStepThrough()> Private Sub InitializeComponent()
		Me.LabelAppName = New System.Windows.Forms.Label()
		Me.ListBoxFiles = New System.Windows.Forms.ListBox()
		Me.myCancelButton = New System.Windows.Forms.Button()
		Me.SuspendLayout()
		'
		'LabelAppName
		'
		Me.LabelAppName.Location = New System.Drawing.Point(16, 16)
		Me.LabelAppName.Name = "LabelAppName"
		Me.LabelAppName.Size = New System.Drawing.Size(480, 40)
		Me.LabelAppName.TabIndex = 0
		Me.LabelAppName.Text = "Files in the {0} application"
		'
		'ListBoxFiles
		'
		Me.ListBoxFiles.HorizontalScrollbar = True
		Me.ListBoxFiles.Location = New System.Drawing.Point(16, 56)
		Me.ListBoxFiles.Name = "ListBoxFiles"
		Me.ListBoxFiles.Size = New System.Drawing.Size(480, 238)
		Me.ListBoxFiles.TabIndex = 1
		'
		'myCancelButton
		'
		Me.myCancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel
		Me.myCancelButton.Location = New System.Drawing.Point(219, 312)
		Me.myCancelButton.Name = "myCancelButton"
		Me.myCancelButton.TabIndex = 2
		Me.myCancelButton.Text = "OK"
		'
		'AppFileListDlg
		'
		Me.AutoScaleBaseSize = New System.Drawing.Size(5, 13)
		Me.CancelButton = Me.myCancelButton
		Me.ClientSize = New System.Drawing.Size(512, 350)
		Me.Controls.AddRange(New System.Windows.Forms.Control() {Me.myCancelButton, Me.ListBoxFiles, Me.LabelAppName})
		Me.Name = "AppFileListDlg"
		Me.Text = "SIL Converter Application File List"
		Me.ResumeLayout(False)

	End Sub

#End Region

End Class
