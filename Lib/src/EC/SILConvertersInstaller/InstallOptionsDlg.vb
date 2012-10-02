Imports System.IO
Imports System.Collections.Generic
Imports Microsoft.Win32
Imports System.Threading
Imports System.Windows.Forms
Imports System.Reflection
Imports ECInterfaces
Imports SilEncConverters31
Imports System.Configuration.Install    ' for InstallException
Imports System.Diagnostics              ' for ProcessStartInfo
Imports System.Security                 ' for SecurityException
Imports System.Runtime.InteropServices  ' for DllImport
Imports System.Text                     ' for StringBuilder

Public Class InstallOptionsDlg
	Inherits Form

	Public Const cstrInstallerKey As String = "SOFTWARE\SIL\SilEncConverters31\Installer"
	Public Const cstrInstallFromDirKey As String = "InstallerPath"
	Public Const cstrInstallShowToolbipKey As String = "ShowToolTips"
	Public Const strStopMyself As String = "StopMyself"
	Public Const strCheckAllLabel As String = "&Check All"
	Public Const strClearAllLabel As String = "&Clear All"
	Public Const strDefaultLable As String = "Show &Currently Installed"
	Public Const cstrDefInstructions As String = "Click the OK or Apply buttons to install checked items and uninstall unchecked items"

	Public Const strMyExeName As String = "SetupSC.exe"
	Const cstrTargetDirDef As String = "\SIL"
	Dim m_sSrcDir, m_sTargetDir As String
	Dim m_mySavedState As ArrayList = New ArrayList()
	Friend WithEvents TableLayoutPanel1 As System.Windows.Forms.TableLayoutPanel
	Friend WithEvents HelpProvider As System.Windows.Forms.HelpProvider
	Friend WithEvents ButtonOK As System.Windows.Forms.Button
	Friend WithEvents TimerTooltip As System.Windows.Forms.Timer
	Public myToolTips As New ToolTip()

#If Not DontUseHklmRegistry Then
	Public Sub New()
#Else
	Public Sub New(ByVal bUninstall As Boolean)
#End If
		MyBase.New()

		Dim strCaption As String = cstrProgramCaption

		Dim key As RegistryKey
#If Not DontUseHklmRegistry Then
#Else
		If (bUninstall) Then
			strCaption = "SIL Converters Uninstall"
			' first see if the user wants to clear out the repository
			Dim res As DialogResult
			res = MessageBox.Show("Do you want to remove all of your existing converters?", strCaption, MessageBoxButtons.YesNoCancel)
			If (res = Windows.Forms.DialogResult.Yes) Then
				key = Registry.LocalMachine.OpenSubKey(cstrInstallerKey & "\" & checkListBoxMapsTables.mySubDir, True)
				If (Not key Is Nothing) Then
					For Each strMapName As String In key.GetSubKeyNames()
						Dim keyMapName As RegistryKey = key.OpenSubKey(strMapName)
						If (Not keyMapName Is Nothing) Then
							For Each strFilename As String In keyMapName.GetValueNames
								DeleteFile(strFilename)
							Next
							key.DeleteSubKeyTree(strMapName)
						End If
					Next
				End If

				ClearRepository()
			ElseIf (res = Windows.Forms.DialogResult.Cancel) Then
				Throw New InstallException(strStopMyself)
			End If

			' we're done, so since this is "New", we have to use a different mechanism to
			' prevent the "ShowDialog" in the caller from happening...
			Throw New InstallException(strStopMyself)
		Else
#End If

			Dim sPathModule As String = System.Reflection.Assembly.GetExecutingAssembly.GetModules()(0).FullyQualifiedName
			Dim nIndex As Integer = sPathModule.LastIndexOf("\")
			m_sSrcDir = sPathModule.Substring(0, nIndex + 1)

			' the user *must* have a usable EncConverters object to use at this point
			' or we can't continue. If it's not installed (as indicated by a reg key),
			' then we must quit.
			key = Registry.LocalMachine.OpenSubKey("SOFTWARE\SIL\SilEncConverters31")
			If (Not key Is Nothing) Then
				Dim sRootVal As String = key.GetValue("RootDir")
				If (Not String.IsNullOrEmpty(sRootVal)) Then

					' Jira EC-7: retain tooltip state info across invocations
					Dim bShowTooltip As Boolean
					Dim keyLastTooltipState As RegistryKey = Registry.LocalMachine.OpenSubKey(cstrInstallerKey)
					If (Not keyLastTooltipState Is Nothing) Then
						bShowTooltip = (keyLastTooltipState.GetValue(cstrInstallShowToolbipKey, "True") = "True")
					End If

					' Now we're ready to initialize the dialog
					InitializeComponent()

					CheckBoxShowTooltip.Checked = bShowTooltip

					'Add any initialization after the InitializeComponent() call
					m_sTargetDir = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + cstrTargetDirDef
					Directory.CreateDirectory(m_sTargetDir)
					m_sTargetDir += "\"

					' initialize the tooltips
					CreateMyToolTip()
					Exit Sub
				End If
			End If

			' means we didn't have a good EncConverters' install
			Throw New InstallException("Oops... Cannot continue without the EncConverters repository installed")
#If Not DontUseHklmRegistry Then
#Else
		End If
#End If
	End Sub

	Protected Sub LaunchProgram(ByVal strProgram As String, ByVal strArguments As String)
		Try
			Dim myProcess As Process = New Process

			myProcess.StartInfo.FileName = strProgram
			myProcess.StartInfo.Arguments = strArguments
			myProcess.Start()

		Catch
		End Try
	End Sub

	Public Sub UpdateToolTip(ByVal ctrl As Control, ByVal sTip As String)
		myToolTips.SetToolTip(ctrl, sTip)
	End Sub

	Private Sub CreateMyToolTip()
		' Create the ToolTip and associate with the Form container.
		' Set up the delays for the ToolTip.
		myToolTips.AutoPopDelay = 30000
		myToolTips.InitialDelay = 1000
		myToolTips.ReshowDelay = 500
		' Force the ToolTip text to be displayed whether or not the form is active.
		myToolTips.ShowAlways = True

		' Set up the ToolTip text for the Button and Checkbox.
		UpdateToolTip(Me.checkListBoxMapsTables, "MapsTables silly")

	End Sub

	Private Shadows Sub Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
		' to try/catch on these as if EncCnvtrs didn't get installed (or isn't working), the
		' MapsTables, at least, will fail to work and will throw an exception.
		Try

			Me.HelpProvider.SetHelpString(Me.CheckBoxSetClearAll, My.Resources.CheckBoxSetClearAllHelp)
			Me.HelpProvider.SetHelpString(Me.checkListBoxMapsTables, My.Resources.checkListBoxMapsTablesHelp)

			InitListBoxes()

			Me.myInstallButton.Enabled = False

		Catch ex As FileNotFoundException
			MessageBox.Show("Did you forget to install the Repository?" & Chr(10) & Chr(10) & ex.ToString, cstrProgramCaption)
		End Try
	End Sub

	Private Sub InitListBoxes()
		Me.checkListBoxMapsTables.InitializeComponent(m_sSrcDir, m_sTargetDir)
		Me.checkListBoxMapsTables.ContextMenu = ContextMenuPopup
	End Sub
#Region " Windows Form Designer generated code "

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
	Friend WithEvents myInstallButton As Button
	Friend WithEvents myCancelButton As Button
	' Friend WithEvents Label1 As Label
	Friend WithEvents checkListBoxMapsTables As SILConvertersInstaller.CheckListBoxMapsTables
	Friend WithEvents myStatusBar As StatusBar
	' Friend WithEvents checkListBoxEngines As SILConvertersInstaller.CheckListBoxEngines
	Friend WithEvents ContextMenuPopup As System.Windows.Forms.ContextMenu
	Friend WithEvents MenuItemSelectAll As System.Windows.Forms.MenuItem
	Friend WithEvents MenuItemClearAll As System.Windows.Forms.MenuItem
	Friend WithEvents CheckBoxSetClearAll As System.Windows.Forms.CheckBox
	Friend WithEvents MenuItemTestItem As System.Windows.Forms.MenuItem
	Friend WithEvents MenuItem2 As System.Windows.Forms.MenuItem
	Friend WithEvents CheckBoxShowTooltip As System.Windows.Forms.CheckBox
	<System.Diagnostics.DebuggerStepThrough()> Private Sub InitializeComponent()
		Me.components = New System.ComponentModel.Container
		Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(InstallOptionsDlg))
		Me.myInstallButton = New System.Windows.Forms.Button
		Me.myCancelButton = New System.Windows.Forms.Button
		Me.myStatusBar = New System.Windows.Forms.StatusBar
		Me.ContextMenuPopup = New System.Windows.Forms.ContextMenu
		Me.MenuItemTestItem = New System.Windows.Forms.MenuItem
		Me.MenuItem2 = New System.Windows.Forms.MenuItem
		Me.MenuItemSelectAll = New System.Windows.Forms.MenuItem
		Me.MenuItemClearAll = New System.Windows.Forms.MenuItem
		Me.CheckBoxSetClearAll = New System.Windows.Forms.CheckBox
		Me.CheckBoxShowTooltip = New System.Windows.Forms.CheckBox
		Me.TableLayoutPanel1 = New System.Windows.Forms.TableLayoutPanel
		Me.checkListBoxMapsTables = New SILConvertersInstaller.CheckListBoxMapsTables
		Me.ButtonOK = New System.Windows.Forms.Button
		Me.HelpProvider = New System.Windows.Forms.HelpProvider
		Me.TimerTooltip = New System.Windows.Forms.Timer(Me.components)
		Me.TableLayoutPanel1.SuspendLayout()
		Me.SuspendLayout()
		'
		'myInstallButton
		'
		Me.myInstallButton.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
		Me.myInstallButton.Enabled = False
		Me.HelpProvider.SetHelpString(Me.myInstallButton, "Click this button to install the checked items and uninstall the unchecked items")
		Me.myInstallButton.Location = New System.Drawing.Point(275, 409)
		Me.myInstallButton.Name = "myInstallButton"
		Me.HelpProvider.SetShowHelp(Me.myInstallButton, True)
		Me.myInstallButton.Size = New System.Drawing.Size(75, 23)
		Me.myInstallButton.TabIndex = 0
		Me.myInstallButton.Text = "Apply"
		'
		'myCancelButton
		'
		Me.myCancelButton.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
		Me.myCancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel
		Me.HelpProvider.SetHelpString(Me.myCancelButton, "Click this button to dismiss the dialog")
		Me.myCancelButton.Location = New System.Drawing.Point(194, 409)
		Me.myCancelButton.Name = "myCancelButton"
		Me.HelpProvider.SetShowHelp(Me.myCancelButton, True)
		Me.myCancelButton.Size = New System.Drawing.Size(75, 23)
		Me.myCancelButton.TabIndex = 1
		Me.myCancelButton.Text = "Cancel"
		'
		'myStatusBar
		'
		Me.myStatusBar.Location = New System.Drawing.Point(0, 435)
		Me.myStatusBar.Name = "myStatusBar"
		Me.myStatusBar.Size = New System.Drawing.Size(465, 22)
		Me.myStatusBar.TabIndex = 12
		Me.myStatusBar.Text = "Click the OK or Apply buttons to install checked items and uninstall unchecked it" & _
			"ems"
		'
		'ContextMenuPopup
		'
		Me.ContextMenuPopup.MenuItems.AddRange(New System.Windows.Forms.MenuItem() {Me.MenuItemTestItem, Me.MenuItem2, Me.MenuItemSelectAll, Me.MenuItemClearAll})
		'
		'MenuItemTestItem
		'
		Me.MenuItemTestItem.Index = 0
		Me.MenuItemTestItem.Text = "&Test Converter"
		'
		'MenuItem2
		'
		Me.MenuItem2.Index = 1
		Me.MenuItem2.Text = "-"
		'
		'MenuItemSelectAll
		'
		Me.MenuItemSelectAll.Index = 2
		Me.MenuItemSelectAll.Text = "&Select All"
		'
		'MenuItemClearAll
		'
		Me.MenuItemClearAll.Index = 3
		Me.MenuItemClearAll.Text = "&Clear All"
		'
		'CheckBoxSetClearAll
		'
		Me.HelpProvider.SetHelpString(Me.CheckBoxSetClearAll, "")
		Me.CheckBoxSetClearAll.Location = New System.Drawing.Point(5, 3)
		Me.CheckBoxSetClearAll.Margin = New System.Windows.Forms.Padding(5, 3, 3, 3)
		Me.CheckBoxSetClearAll.Name = "CheckBoxSetClearAll"
		Me.HelpProvider.SetShowHelp(Me.CheckBoxSetClearAll, True)
		Me.CheckBoxSetClearAll.Size = New System.Drawing.Size(150, 21)
		Me.CheckBoxSetClearAll.TabIndex = 15
		Me.CheckBoxSetClearAll.Text = "&Check All"
		Me.CheckBoxSetClearAll.ThreeState = True
		'
		'CheckBoxShowTooltip
		'
		Me.CheckBoxShowTooltip.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
		Me.CheckBoxShowTooltip.Checked = True
		Me.CheckBoxShowTooltip.CheckState = System.Windows.Forms.CheckState.Checked
		Me.HelpProvider.SetHelpString(Me.CheckBoxShowTooltip, "Uncheck this box to turn off the converter tooltips")
		Me.CheckBoxShowTooltip.Location = New System.Drawing.Point(362, 3)
		Me.CheckBoxShowTooltip.Name = "CheckBoxShowTooltip"
		Me.HelpProvider.SetShowHelp(Me.CheckBoxShowTooltip, True)
		Me.CheckBoxShowTooltip.Size = New System.Drawing.Size(100, 21)
		Me.CheckBoxShowTooltip.TabIndex = 16
		Me.CheckBoxShowTooltip.Text = "&Show ToolTips"
		'
		'TableLayoutPanel1
		'
		Me.TableLayoutPanel1.ColumnCount = 3
		Me.TableLayoutPanel1.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50.0!))
		Me.TableLayoutPanel1.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle)
		Me.TableLayoutPanel1.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50.00001!))
		Me.TableLayoutPanel1.Controls.Add(Me.CheckBoxSetClearAll, 0, 0)
		Me.TableLayoutPanel1.Controls.Add(Me.myCancelButton, 1, 2)
		Me.TableLayoutPanel1.Controls.Add(Me.checkListBoxMapsTables, 0, 1)
		Me.TableLayoutPanel1.Controls.Add(Me.CheckBoxShowTooltip, 2, 0)
		Me.TableLayoutPanel1.Controls.Add(Me.myInstallButton, 2, 2)
		Me.TableLayoutPanel1.Controls.Add(Me.ButtonOK, 0, 2)
		Me.TableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill
		Me.TableLayoutPanel1.Location = New System.Drawing.Point(0, 0)
		Me.TableLayoutPanel1.Name = "TableLayoutPanel1"
		Me.TableLayoutPanel1.RowCount = 3
		Me.TableLayoutPanel1.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 9.039548!))
		Me.TableLayoutPanel1.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 90.96045!))
		Me.TableLayoutPanel1.RowStyles.Add(New System.Windows.Forms.RowStyle)
		Me.TableLayoutPanel1.Size = New System.Drawing.Size(465, 435)
		Me.TableLayoutPanel1.TabIndex = 17
		'
		'checkListBoxMapsTables
		'
		Me.checkListBoxMapsTables.CheckOnClick = True
		Me.TableLayoutPanel1.SetColumnSpan(Me.checkListBoxMapsTables, 3)
		Me.checkListBoxMapsTables.Dock = System.Windows.Forms.DockStyle.Fill
		Me.checkListBoxMapsTables.Location = New System.Drawing.Point(3, 39)
		Me.checkListBoxMapsTables.Name = "checkListBoxMapsTables"
		Me.checkListBoxMapsTables.Size = New System.Drawing.Size(459, 349)
		Me.checkListBoxMapsTables.TabIndex = 9
		Me.checkListBoxMapsTables.ThreeDCheckBoxes = True
		'
		'ButtonOK
		'
		Me.ButtonOK.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
		Me.HelpProvider.SetHelpString(Me.ButtonOK, "Click this button to install the checked items and uninstall the unchecked items " & _
				"and then dismiss the dialog")
		Me.ButtonOK.Location = New System.Drawing.Point(113, 409)
		Me.ButtonOK.Name = "ButtonOK"
		Me.HelpProvider.SetShowHelp(Me.ButtonOK, True)
		Me.ButtonOK.Size = New System.Drawing.Size(75, 23)
		Me.ButtonOK.TabIndex = 17
		Me.ButtonOK.Text = "OK"
		Me.ButtonOK.UseVisualStyleBackColor = True
		'
		'TimerTooltip
		'
		Me.TimerTooltip.Interval = 500
		'
		'InstallOptionsDlg
		'
		Me.AcceptButton = Me.ButtonOK
		Me.AutoScaleBaseSize = New System.Drawing.Size(5, 13)
		Me.CancelButton = Me.myCancelButton
		Me.ClientSize = New System.Drawing.Size(465, 457)
		Me.Controls.Add(Me.TableLayoutPanel1)
		Me.Controls.Add(Me.myStatusBar)
		Me.HelpButton = True
		Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
		Me.MaximizeBox = False
		Me.MinimizeBox = False
		Me.Name = "InstallOptionsDlg"
		Me.Text = "Converter Installer"
		Me.TableLayoutPanel1.ResumeLayout(False)
		Me.ResumeLayout(False)

	End Sub

	Private Sub InstallButton_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles myInstallButton.Click
		Try
			Me.checkListBoxMapsTables.InstallSelected(Me.myStatusBar, m_mySavedState)

		Catch ex As InstallException
			MessageBox.Show(ex.Message, cstrProgramCaption)
		End Try

		Try
			' reinitialize
			Me.Cursor = Cursors.WaitCursor
			' Thread.Sleep(2000)  ' give it time to reset
			InitListBoxes() ' try this, but just close the app if it fails

			Me.myInstallButton.Enabled = False
			Me.myStatusBar.Text = "Converter Installation complete. Click 'Cancel' to close."

		Catch ex As Exception
			DialogResult = Windows.Forms.DialogResult.OK
			Close()
		End Try
		Me.Cursor = Cursors.Default

	End Sub

	Private Sub ButtonOK_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ButtonOK.Click

		Try
			Me.checkListBoxMapsTables.InstallSelected(Me.myStatusBar, m_mySavedState)
			Me.myStatusBar.Text = "Converter Installation complete. Click 'Cancel' to close."

		Catch ex As InstallException
			MessageBox.Show(ex.Message, cstrProgramCaption)
		End Try

		Me.Cursor = Cursors.Default
		Close()

	End Sub

#End Region

	Private Sub SetAll()
		Dim i As Integer
		For i = 0 To Me.checkListBoxMapsTables.Items.Count - 1
			Me.checkListBoxMapsTables.SetItemCheckState(i, CheckState.Checked)
		Next
	End Sub
	Private Sub ClearAll()
		For Each index As Integer In Me.checkListBoxMapsTables.CheckedIndices
			Me.checkListBoxMapsTables.SetItemCheckState(index, CheckState.Unchecked)
		Next
	End Sub
	Private Sub SetStateIfNotInstalled(ByVal state As CheckState)
		Dim aECs As New EncConverters
		Dim i As Integer
		For i = 0 To Me.checkListBoxMapsTables.Items.Count - 1
			If (Me.checkListBoxMapsTables.GetItemCheckState(i) <> CheckState.Indeterminate) Then
				If (aECs.Item(Me.checkListBoxMapsTables.Items.Item(i)) Is Nothing) Then
					Me.checkListBoxMapsTables.SetItemCheckState(i, state)
				Else
					Me.checkListBoxMapsTables.SetItemCheckState(i, CheckState.Indeterminate)
				End If
			End If
		Next
	End Sub
	Private Sub MenuItemSelectAll_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MenuItemSelectAll.Click
		SetAll()
	End Sub

	Private Sub MenuItemClearAll_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MenuItemClearAll.Click
		ClearAll()
	End Sub

	Private Sub ClearRepository()
		Dim aECs As New EncConverters
		aECs.Clear()
	End Sub

	Private Sub CheckBoxSetClearAll_CheckStateChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles CheckBoxSetClearAll.CheckStateChanged
		If (Me.CheckBoxSetClearAll.CheckState = CheckState.Checked) Then
			Me.CheckBoxSetClearAll.Text = strDefaultLable
			SetStateIfNotInstalled(CheckState.Checked)
		ElseIf (Me.CheckBoxSetClearAll.CheckState = CheckState.Indeterminate) Then
			Me.CheckBoxSetClearAll.Text = strClearAllLabel
			SetStateIfNotInstalled(CheckState.Unchecked)
		Else
			Me.CheckBoxSetClearAll.Text = strCheckAllLabel
			ClearAll()
		End If
	End Sub

	Private Sub MenuItemTestItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MenuItemTestItem.Click
		Me.checkListBoxMapsTables.TestConverter()
	End Sub

	Private Sub CheckBoxShowTooltip_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles CheckBoxShowTooltip.CheckedChanged
		Me.myToolTips.Active = Me.CheckBoxShowTooltip.Checked
		Try
			Dim keyLastTooltipState As RegistryKey = Registry.LocalMachine.CreateSubKey(cstrInstallerKey)
			If (Not keyLastTooltipState Is Nothing) Then
				keyLastTooltipState.SetValue(cstrInstallShowToolbipKey, CheckBoxShowTooltip.Checked)
			End If
		Catch ' it might fail on some systems. this isn't that important that we even need to bother the user about it.
		End Try
	End Sub

	Private Sub TimerTooltip_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TimerTooltip.Tick
		Me.checkListBoxMapsTables.OnTooltipTimerTick()
	End Sub
End Class

#Region " SIL Converters CheckListBox (base class)"
Public Class ConvertersCheckListBox
	Inherits CheckedListBox

	Protected m_sSrcDir As String
	Protected m_sTargetDir As String
	Protected m_sDefDescription As String
	Protected m_sLastToolTip As String
	Protected m_mapLbItems2Tooltips As New Dictionary(Of String, String)
	Protected m_hashOriginallyInstalled As New Hashtable

	Public Sub InitializeComponent(ByVal sSrcDir As String, ByVal sTargetDir As String, _
					ByVal sDefDescription As String)
		m_sSrcDir = sSrcDir
		m_sTargetDir = sTargetDir
		m_sDefDescription = sDefDescription

		' in case this isn't the first time thru here...
		Items.Clear()
		m_hashOriginallyInstalled.Clear()
		m_mapLbItems2Tooltips.Clear()
	End Sub

	Protected Sub InitializeCheckBoxList(ByVal arItems As ArrayList, ByVal bStartCheckedState As CheckState)
		Dim aItem As String
		For Each aItem In arItems
			Items.Add(aItem, bStartCheckedState)
		Next
	End Sub

	Public Sub New()
		MyBase.New()
	End Sub

	Protected m_ptLastMove As New Point(0, 0)

	Public Function LastPointClicked() As Integer
		LastPointClicked = IndexFromPoint(m_ptLastMove)
	End Function

	Protected m_nLastTooltipDisplayedIndex As Integer = ListBox.NoMatches

	Protected Overrides Sub OnMouseMove(ByVal e As MouseEventArgs)
		MyBase.OnMouseMove(e)
		m_ptLastMove = New Point(e.X, e.Y)
		Dim nIndex As Integer = LastPointClicked()
		If (nIndex <> m_nLastTooltipDisplayedIndex) Then
			m_nLastTooltipDisplayedIndex = nIndex
			Dim myForm As InstallOptionsDlg = TopLevelControl()
			myForm.myToolTips.Hide(Me)
			If (nIndex <> ListBox.NoMatches) Then
				myForm.TimerTooltip.Stop()
				myForm.TimerTooltip.Start()
			End If
		End If
	End Sub

	Public Sub OnTooltipTimerTick()
		Dim myForm As InstallOptionsDlg = TopLevelControl()
		Try
			If (m_nLastTooltipDisplayedIndex <> ListBox.NoMatches) Then
				Dim sDesc As String = m_sDefDescription
				Dim strKey As String = Items.Item(m_nLastTooltipDisplayedIndex)
				If (m_mapLbItems2Tooltips.TryGetValue(strKey, sDesc)) Then
					myForm.UpdateToolTip(Me, sDesc)
				End If
				If (sDesc <> m_sLastToolTip) Then
					m_sLastToolTip = sDesc
				End If
			End If
		Finally
			myForm.TimerTooltip.Stop()
		End Try
	End Sub
End Class
#End Region

#Region " MapsTables CheckedListBox "
Public Class CheckListBoxMapsTables
	Inherits ConvertersCheckListBox

	Private Const myExt As String = ".vbs"
	Private Const myFilter As String = "*.xml"
	Public Const mySubDir As String = "MapsTables"

	Private m_hashAddinDependencies As New Hashtable
	Private m_hashAutoAdds As New Hashtable
	Private m_hashScriptAdds As New Hashtable
	Private m_hashFilesToInstall As New Hashtable

	Public Sub TestConverter()
		Dim nIndex As Integer = LastPointClicked()
		Dim strFriendlyName As String = Me.Items(nIndex)
		Dim strConverterIdentifier As String = Nothing
		Dim strTestData As String = Nothing
		Dim strImplType As String = Nothing
		Dim eConvType As ConvType
		Dim aECs As New EncConverters

		Dim aConfigurator As IEncConverterConfig = Nothing

		Dim strKey As String = m_sSrcDir + strFriendlyName
		If (m_hashAutoAdds.ContainsKey(strKey)) Then
			Dim aRow As SILConvertersInstallerDetails.autoInstallRow = m_hashAutoAdds(strKey)

			strConverterIdentifier = aRow.converterSpec
			If (Not aRow.IsIsConverterSpecAFileNull) Then
				If (aRow.IsConverterSpecAFile) Then
					strConverterIdentifier = m_sSrcDir + strConverterIdentifier
				End If
			End If

			eConvType = XmlConvTypeEnum(aRow.conversionType)

			If (Not aRow.IssampleDataNull) Then
				strTestData = aRow.sampleData
			End If

			strImplType = aRow.implementType

		ElseIf (m_hashScriptAdds.ContainsKey(strKey)) Then
			Dim aRow As SILConvertersInstallerDetails.scriptInstallRow = m_hashScriptAdds(strKey)

			If (Not aRow.IsconverterSpecNull) Then
				strConverterIdentifier = aRow.converterSpec

				If ((Not aRow.IsIsConverterSpecAFileNull) And aRow.IsConverterSpecAFile) Then
					strConverterIdentifier = m_sSrcDir + strConverterIdentifier
				End If
			End If

			If (Not aRow.IsconversionTypeNull) Then
				eConvType = XmlConvTypeEnum(aRow.conversionType)
			End If

			If (Not aRow.IssampleDataNull) Then
				strTestData = aRow.sampleData
			End If

			strImplType = aRow.implementType

		End If

		' compound converters that aren't in the repository can't do test mode
		Dim bIsACompoundConverterAndNotInRepository As Boolean = _
		(((strImplType = EncConverters.strTypeSILcomp) Or (strImplType = EncConverters.strTypeSILfallback)) _
		And (aECs(strFriendlyName) Is Nothing))

		If (bIsACompoundConverterAndNotInRepository) Then
			MessageBox.Show("Compound converters (and all its embedded steps) must be installed before they can be tested.", cstrProgramCaption)
		Else
			' looking in the repository if no impl type or if it's already in the repository
			Dim aEC As IEncConverter = Nothing
			If (aECs.ContainsKey(strFriendlyName)) Then
				aEC = aECs(strFriendlyName)
			ElseIf (Not String.IsNullOrEmpty(strImplType)) Then
				aEC = aECs.NewEncConverterByImplementationType(strImplType)
			End If

			If (Not aEC Is Nothing) Then
				aConfigurator = aEC.Configurator
			End If

			If (aConfigurator Is Nothing) Then
				MessageBox.Show("This converter type doesn't support the test feature.", cstrProgramCaption)
			Else
				Try
					aConfigurator.DisplayTestPage(aECs, strFriendlyName, strConverterIdentifier, eConvType, strTestData)
				Catch ex As Exception
					MessageBox.Show(String.Format("Unable to test converter.{0}{0}{1}", Environment.NewLine, ex.Message), cstrProgramCaption)
				End Try

			End If
		End If
	End Sub

	Public Sub New()
		MyBase.New()
	End Sub

	Protected Function FormatTabbedTip(ByVal strFormat As String, ByVal strValue As String) As String
		FormatTabbedTip = Environment.NewLine & "    " & String.Format(strFormat, strValue)
	End Function
	Protected Function FormatTabbedTip2(ByVal strFormat As String, ByVal strValue As String, ByVal strValue2 As String) As String
		FormatTabbedTip2 = Environment.NewLine & "    " & String.Format(strFormat, strValue, strValue2)
	End Function
	Protected Sub HandleListFixup(ByVal strMapName As String, ByVal strImplTypeIn As String, _
	ByVal strDir As String, ByRef strFriendlyName As String, ByRef strImplTypeOut As String, _
	ByRef strComboKey As String, ByRef aFriendlyNames As Hashtable)

		strImplTypeOut = strImplTypeIn
		If (aFriendlyNames.ContainsKey(strMapName)) Then
			strFriendlyName = EncConverters.BuildConverterSpecNameEx(strMapName, strImplTypeOut)
		Else
			strFriendlyName = strMapName
		End If

		strComboKey = strDir + strFriendlyName

	End Sub
	Protected Sub GetXMLDetails(ByVal sDir As String, ByVal aFile As String, ByRef aECs As EncConverters, ByRef aFriendlyNames As Hashtable)

		Dim aDetailsFile As SILConvertersInstallerDetails = New SILConvertersInstallerDetails
		Try
			aDetailsFile.ReadXml(sDir & mySubDir & "\" & aFile)

			Dim state As CheckState = CheckState.Indeterminate

			' first determine all the names in this xml file (by going thru the full list of script and
			' auto elements
			Dim lstMapNames As List(Of String) = New List(Of String)
			Dim aAutoRow As SILConvertersInstallerDetails.autoInstallRow
			For Each aAutoRow In aDetailsFile.autoInstall
				Dim strMapName As String = aAutoRow.mappingName
				If (lstMapNames.Contains(strMapName)) Then
					aFriendlyNames.Add(strMapName, aAutoRow.implementType)
				Else
					lstMapNames.Add(strMapName)
				End If
			Next

			Dim aScriptRow As SILConvertersInstallerDetails.scriptInstallRow
			For Each aScriptRow In aDetailsFile.scriptInstall
				Dim strMapName As String = aScriptRow.mappingName
				If (lstMapNames.Contains(strMapName)) Then
					aFriendlyNames.Add(strMapName, aScriptRow.implementType)
				Else
					lstMapNames.Add(strMapName)
				End If
			Next
			' now the 'lstMapNames' variable has the full list of mapping names (so we can know if there
			'   are two different "spec"s for the same mapping name (and therefore use the ImplType as well)
			'   (e.g. "Annapurna<>UNICODE (SIL.cc)" and "Annapurna<>UNICODE (SIL.tec)"

			' next go thru the MapTable rows and setup what we'll need to do installation later
			Dim aMapTable As SILConvertersInstallerDetails.MapTableRow
			For Each aMapTable In aDetailsFile.MapTable
				' set the map from this display name to the file name prefix to copy
				' key it from both the sDir and the display name (in case we're installing it
				' again and then we want the details from the src xml file)
				Dim stateNew As CheckState = state
				Dim strDescription As String = aMapTable.description

				' see if this map entry has files to copy (put them in an ArrayList to be
				' copied later only if this converter map is going to be installed)
				Dim aListFiles As New ArrayList
				If (aMapTable.GetFilesToInstallRows().Length() > 0) Then
					Dim aFileCopyRow, aFileCopyRows() As SILConvertersInstallerDetails.FileToInstallRow
					aFileCopyRows = aMapTable.GetFilesToInstallRows(0).GetFileToInstallRows()
					For Each aFileCopyRow In aFileCopyRows
						aListFiles.Add(aFileCopyRow.filename)
						strDescription += FormatTabbedTip("Filename to install: '{0}'", aFileCopyRow.filename)
					Next
				End If

				' there are two ways to install a converter: either an 'auto install' entry
				' (easy to do, but only for simply 'AddConversionMap' details at this point)...
				Dim sComboKey As String = Nothing
				Dim sFriendlyName As String = Nothing
				Dim strImplType As String = Nothing
				Dim bUsingExtendedFriendlyName As Boolean = False
				Dim aAutoInstallRows() As SILConvertersInstallerDetails.autoInstallRow = aMapTable.GetautoInstallRows()
				If (aAutoInstallRows.Length > 0) Then
					Dim aRow As SILConvertersInstallerDetails.autoInstallRow = aAutoInstallRows(0)
					HandleListFixup(aRow.mappingName, aRow.implementType, sDir, sFriendlyName, _
					strImplType, sComboKey, aFriendlyNames)
					m_hashAutoAdds(sComboKey) = aRow

					' add some info to the tool tip
					strDescription += FormatTabbedTip("Converter Identifier: '{0}'", aRow.converterSpec)
					strDescription += FormatTabbedTip("Conversion Type: '{0}'", aRow.conversionType.ToString())
					strDescription += FormatTabbedTip("Implementation Type: '{0}'", strImplType)
				End If

				' ... or a script install
				Dim aScriptInstallRows() As SILConvertersInstallerDetails.scriptInstallRow = aMapTable.GetscriptInstallRows()
				If (aScriptInstallRows.Length > 0) Then
					Dim aRow As SILConvertersInstallerDetails.scriptInstallRow = aScriptInstallRows(0)
					HandleListFixup(aRow.mappingName, aRow.implementType, sDir, sFriendlyName, _
					strImplType, sComboKey, aFriendlyNames)
					m_hashScriptAdds(sComboKey) = aRow

					' add some info to the tool tip
					strDescription += FormatTabbedTip("Implementation Type: '{0}'", strImplType)
				End If

				m_hashFilesToInstall(sComboKey) = aListFiles

				' now update the checked state based on what's really in the repository
				aFriendlyNames.Add(sFriendlyName, strImplType)
				'Dim strMapName As String = sFriendlyName
				'If (bUsingExtendedFriendlyName) Then
				'strMapName = EncConverters.BuildConverterSpecNameEx(sFriendlyName, strImplType)
				'End If
				If (aECs(sFriendlyName) Is Nothing) Then
					stateNew = CheckState.Unchecked
				End If

				' add the 'name' as the checkbox entry (unless it's already there)
				If (FindStringExact(sFriendlyName) = -1) Then
					Dim nIndex As Integer = Items.Add(sFriendlyName, stateNew)

					' indicate that it wasn't originally installed so we know not to do the uninstall
					' if the user just unchecks it without it having originally been installed.
					If (stateNew = CheckState.Unchecked) Then
						m_hashOriginallyInstalled.Add(sFriendlyName, aFile)
					End If

					Debug.Assert(Not m_mapLbItems2Tooltips.ContainsKey(sFriendlyName))
					m_mapLbItems2Tooltips(sFriendlyName) = strDescription
				End If
			Next aMapTable

		Catch e As Exception
			MessageBox.Show(e.Message, cstrProgramCaption)
		End Try
	End Sub

#Const UseToStringForDescription = True

	Public Shadows Sub InitializeComponent(ByVal sSrcDir As String, ByVal sTargetDir As String)
		' we expect the maps/tables to be in the "MapsTables" sub-folder
		' sTargetDir = sTargetDir & mySubDir
		If (Not Directory.Exists(sTargetDir & mySubDir)) Then
			Directory.CreateDirectory(sTargetDir & mySubDir)
		End If

		MyBase.InitializeComponent(sSrcDir, sTargetDir, "CC Tables, TECkit maps, etc.")

		' in case this isnt' the first time thru here.
		m_hashAddinDependencies.Clear()
		m_hashAutoAdds.Clear()
		m_hashScriptAdds.Clear()
		m_hashFilesToInstall.Clear()

		' first get the info from the xml files (in the source)
		Dim aFile As String = Dir(m_sSrcDir & mySubDir & "\" & myFilter)
		If (Len(aFile) <= 0) Then
			m_sSrcDir = TestingDir()
			aFile = Dir(m_sSrcDir & mySubDir & "\" & myFilter)
			If (aFile = "") Then
				Exit Sub
			End If
		End If

		' get a repository so we can talk to it (to see what's already installed and what's not)
		Dim aECs As New EncConverters
		Dim aFriendlyNames As New Hashtable ' keep track of names we're adding
		While aFile <> ""
			GetXMLDetails(m_sSrcDir, aFile, aECs, aFriendlyNames)

			' get the next one
			aFile = Dir()
		End While

		' put all the rest of the converters in the repository into the listbox
		For Each aEC As IEncConverter In aECs.Values
			Try
				Dim strFriendlyName As String = aEC.Name ' EncConverters.BuildConverterSpecNameEx(aEC.Name, aEC.ImplementType)
#If UseToStringForDescription Then
				Dim strDescription As String = aEC.ToString()
				If (aFriendlyNames.Contains(strFriendlyName)) Then
					m_mapLbItems2Tooltips.Remove(strFriendlyName)
				Else
					Items.Add(strFriendlyName, CheckState.Indeterminate)
					aFriendlyNames.Add(strFriendlyName, aEC.ImplementType)
				End If
#Else
				Dim strDescription As String
				If (aFriendlyNames.Contains(strFriendlyName)) Then
					strDescription = m_mapLbItems2Tooltips(strFriendlyName)
					m_mapLbItems2Tooltips.Remove(strFriendlyName)
					If (aEC.ImplementType = "SIL.comp") Then
						strDescription += FormatTabbedTip("Converter Identifier: '{0}'", aEC.ConverterIdentifier)
					End If
				Else
					Dim nIndex As Integer = Items.Add(strFriendlyName, CheckState.Indeterminate)
					aFriendlyNames.Add(strFriendlyName, aEC.ImplementType)
					strDescription = String.Format("A '{0}' converter of implementation type '{1}'", aEC.ConversionType.ToString(), aEC.ImplementType)
					' add CR + TAB to the beginning of each of the rest
					strDescription += FormatTabbedTip("Converter Identifier: '{0}'", aEC.ConverterIdentifier)
				End If

				If ((Len(aEC.LeftEncodingID) > 0) And (Len(aEC.RightEncodingID) > 0)) Then
					strDescription += FormatTabbedTip2("For going between the '{0}' and '{1}' encodings", aEC.LeftEncodingID, aEC.RightEncodingID)
				ElseIf (Len(aEC.LeftEncodingID) > 0) Then
					strDescription += FormatTabbedTip("For converting from the '{0}' encoding", aEC.LeftEncodingID)
				ElseIf (Len(aEC.RightEncodingID) > 0) Then
					strDescription += FormatTabbedTip("For converting to the '{0}' encoding", aEC.RightEncodingID)
				End If

				Dim aAttrsKeys As String() = aEC.AttributeKeys
				If (Not aAttrsKeys Is Nothing) Then
					For Each aAttrsKey As String In aEC.AttributeKeys
						strDescription += FormatTabbedTip2("'{0}' = '{1}'", aAttrsKey, aEC.AttributeValue(aAttrsKey))
					Next
				End If
#End If

				' if a Configurator is defined, then give it's display name (at the top)
				Dim aECConfig As IEncConverterConfig = aEC.Configurator
				If (Not aECConfig Is Nothing) Then
					strDescription = aECConfig.ConfiguratorDisplayName + Environment.NewLine + strDescription
				End If

				m_mapLbItems2Tooltips(strFriendlyName) = strDescription
			Catch ex As Exception
				' these are just "nicities", so don't bomb out if something fails.
			End Try
		Next

		' finally, put any conversions which EncConverters *doesn't* support (e.g. Martin's converters) into the listbox
		For Each strFriendlyName As String In aECs.Mappings
			If (Not aFriendlyNames.Contains(strFriendlyName)) Then
				Dim nIndex As Integer = Items.Add(strFriendlyName, CheckState.Indeterminate)
				Dim strDescription As String = String.Format("An unrecognized converter named '{0}'", strFriendlyName)
				Debug.Assert(Not m_mapLbItems2Tooltips.ContainsKey(strFriendlyName))
				m_mapLbItems2Tooltips(strFriendlyName) = strDescription
			End If
		Next
	End Sub

	Public Sub InstallSelected(ByRef myStatusBar As StatusBar, ByRef mySavedState As ArrayList)

		' first copy the relevent files to the targetdir (notice that we're not keeping
		' track of these file copies in 'mySavedState' because we don't want them uninstalled
		' during uninstall (or the user will lose his/her converters whenever EC is
		' reinstalled, since it requires a prior uninstall)
		Dim aECs As New EncConverters
		Dim i As Integer
		For i = 0 To Me.Items.Count - 1
			Dim aItem As String = Me.Items.Item(i)
			If (Me.GetItemCheckState(i) = CheckState.Unchecked) Then
				If (Not m_hashOriginallyInstalled.ContainsKey(aItem)) Then
					' this means that we're supposed to uninstall it! Just see if any of these
					' files are in the target dir and remove it.
					myStatusBar.Text = String.Format("Uninstalling '{0}'...", aItem)
					myStatusBar.Refresh()
					Dim sComboKey As String = m_sSrcDir & aItem
					If (m_hashFilesToInstall.ContainsKey(sComboKey)) Then
						Dim sFilename As String
						Dim aFileList As ArrayList = m_hashFilesToInstall(sComboKey)
						For Each sFilename In aFileList
							Dim strFullyQualifiedName As String = m_sTargetDir & sFilename
							DeleteFile(strFullyQualifiedName)
#If Not DontUseHklmRegistry Then
#Else
							RemRegValue(mySubDir & "\" & aItem, strFullyQualifiedName)
#End If
						Next
#If Not DontUseHklmRegistry Then
#Else
						RemRegKey(mySubDir, aItem)
#End If

					End If

					' if the user added some 'automatic' adds for this converter, then remove it.
					If (m_hashAutoAdds.ContainsKey(sComboKey)) Then
						Dim aAutoAdd As SILConvertersInstallerDetails.autoInstallRow = m_hashAutoAdds(sComboKey)
						aECs.Remove(aECs.BuildConverterSpecName(aAutoAdd.mappingName, aAutoAdd.implementType))
					ElseIf (m_hashScriptAdds.ContainsKey(sComboKey)) Then
						Dim aScriptAdd As SILConvertersInstallerDetails.scriptInstallRow = m_hashScriptAdds(sComboKey)
						aECs.Remove(aECs.BuildConverterSpecName(aScriptAdd.mappingName, aScriptAdd.implementType))
					Else
						' otherwise, it may be a converter EC knows nothing about.
						aECs.Remove(aItem)
					End If
				End If
			ElseIf (Me.GetItemCheckState(i) = CheckState.Checked) Then
				' installing
				myStatusBar.Text = String.Format("Installing '{0}'...", aItem)
				myStatusBar.Refresh()
				Dim sComboKey As String = m_sSrcDir & aItem
				If (m_hashFilesToInstall.ContainsKey(sComboKey)) Then
					Dim sFilename As String
					Dim aFileList As ArrayList = m_hashFilesToInstall(sComboKey)
					For Each sFilename In aFileList
						Dim strFullyQualifiedName As String = m_sTargetDir & sFilename
						CopyFile(m_sSrcDir & sFilename, strFullyQualifiedName)
#If Not DontUseHklmRegistry Then
#Else
						SILConverterInstallerModule.AddRegValue(mySubDir & "\" & aItem, strFullyQualifiedName)
#End If
					Next
				End If

				' if the user wanted to do some automatic adds, then we'll call EC for them.
				If (m_hashAutoAdds.ContainsKey(sComboKey)) Then
					Dim aAutoAdd As SILConvertersInstallerDetails.autoInstallRow = m_hashAutoAdds(sComboKey)
					Dim sLeftEncoding, sRightEncoding, sProcessType, sConverterSpec As String
					If (aAutoAdd.IsleftEncodingNull) Then
						sLeftEncoding = Nothing
					Else
						sLeftEncoding = aAutoAdd.leftEncoding
					End If
					If (aAutoAdd.IsrightEncodingNull) Then
						sRightEncoding = Nothing
					Else
						sRightEncoding = aAutoAdd.rightEncoding
					End If
					If (aAutoAdd.IsprocessTypeNull) Then
						sProcessType = Nothing
					Else
						sProcessType = aAutoAdd.processType
					End If
					sConverterSpec = aAutoAdd.converterSpec
					If Not aAutoAdd.IsIsConverterSpecAFileNull Then
						If (aAutoAdd.IsConverterSpecAFile) Then
							sConverterSpec = m_sTargetDir & sConverterSpec
						End If
					End If

					Try
						aECs.AddConversionMap(aAutoAdd.mappingName, sConverterSpec, _
							XmlConvTypeEnum(aAutoAdd.conversionType), aAutoAdd.implementType, _
							sLeftEncoding, sRightEncoding, XmlProcessTypeEnum(sProcessType))
					Catch ex As Exception
						Dim strErrorMsg As String = String.Format("Unable to add the '{1}' converter!{0}(specification: {2}){0}{0}Reason:{0}{0}   {3}{0}{0}Do you have the proper transduction engine installed for this type?", _
							Environment.NewLine, aAutoAdd.mappingName, sConverterSpec, ex.Message)
						Dim strDisplayName As String = aECs.GetImplementationDisplayName(aAutoAdd.implementType)
						If (Not strDisplayName Is Nothing) Then
							strErrorMsg += String.Format(" (i.e. here the '{0}' transduction engine)", strDisplayName)
						End If
						MessageBox.Show(strErrorMsg, cstrProgramCaption)
					End Try

					' the other way to do adds is with a script file
				ElseIf (m_hashScriptAdds.ContainsKey(sComboKey)) Then

					Dim aScriptAdd As SILConvertersInstallerDetails.scriptInstallRow = m_hashScriptAdds(sComboKey)
					Dim sTargetPath As String = m_sSrcDir & aScriptAdd.filename
					If (File.Exists(sTargetPath)) Then
						DoCmdLine("wscript", """" & sTargetPath & """ """ & m_sTargetDir & """")
						System.Windows.Forms.Application.DoEvents()
					End If

				End If
			End If
		Next i

	End Sub

	' convert the string from the xml file into the correct enum value used by EncCnvtrs
	Function XmlConvTypeEnum(ByVal sConvType As String) As ConvType
		If (sConvType = "LegacyToFromUnicode") Then
			XmlConvTypeEnum = ConvType.Legacy_to_from_Unicode
		ElseIf (sConvType = "LegacyToFromLegacy") Then
			XmlConvTypeEnum = ConvType.Legacy_to_from_Legacy
		ElseIf (sConvType = "LegacyToUnicode") Then
			XmlConvTypeEnum = ConvType.Legacy_to_Unicode
		ElseIf (sConvType = "LegacyToLegacy") Then
			XmlConvTypeEnum = ConvType.Legacy_to_Legacy
		ElseIf (sConvType = "UnicodeToFromUnicode") Then
			XmlConvTypeEnum = ConvType.Unicode_to_from_Unicode
		ElseIf (sConvType = "UnicodeToFromLegacy") Then
			XmlConvTypeEnum = ConvType.Unicode_to_from_Legacy
		ElseIf (sConvType = "UnicodeToUnicode") Then
			XmlConvTypeEnum = ConvType.Unicode_to_Unicode
		ElseIf (sConvType = "UnicodeToLegacy") Then
			XmlConvTypeEnum = ConvType.Unicode_to_Legacy
		Else
			Throw New InstallException("Unknown conversionType: " & sConvType & " encountered")
		End If
	End Function

	' convert the string from the xml file into the correct enum value used by EncCnvtrs
	Function XmlProcessTypeEnum(ByVal sProcessType As String) As ProcessTypeFlags
		If (sProcessType = "UnicodeEncodingConversion") Then
			XmlProcessTypeEnum = ProcessTypeFlags.UnicodeEncodingConversion
		ElseIf (sProcessType = "Transliteration") Then
			XmlProcessTypeEnum = ProcessTypeFlags.Transliteration
		ElseIf (sProcessType = "ICUTransliteration") Then
			XmlProcessTypeEnum = (ProcessTypeFlags.ICUTransliteration + ProcessTypeFlags.Transliteration)
		ElseIf (sProcessType = "ICUConverter") Then
			XmlProcessTypeEnum = (ProcessTypeFlags.ICUConverter + ProcessTypeFlags.UnicodeEncodingConversion)
		ElseIf (sProcessType = "CodePageConversion") Then
			XmlProcessTypeEnum = ProcessTypeFlags.CodePageConversion
		ElseIf (sProcessType = "NonUnicodeEncodingConversion") Then
			XmlProcessTypeEnum = ProcessTypeFlags.NonUnicodeEncodingConversion
		Else    '  (sProcessType = "DontKnow") Then
			XmlProcessTypeEnum = ProcessTypeFlags.DontKnow
		End If
	End Function

	Protected Overrides Sub OnItemCheck(ByVal ice As System.Windows.Forms.ItemCheckEventArgs)
		MyBase.OnItemCheck(ice)
		Dim myForm As InstallOptionsDlg = TopLevelControl()
		myForm.myInstallButton.Enabled = True
		myForm.myStatusBar.Text = InstallOptionsDlg.cstrDefInstructions

	End Sub

End Class
#End Region

Module SILConverterInstallerModule
	Public Const cstrProgramCaption As String = "SIL Converters Installer"
	Sub Main()
		Try
			Dim args As String() = GetCommandLineArgs()
#If Not DontUseHklmRegistry Then
			Dim myDlg As InstallOptionsDlg = New InstallOptionsDlg()
#Else
			Dim bUninstall As Boolean = False
			If (args.Length > 0) Then
				If (((args(0) = "/u") Or (args(0) = "-u"))) Then
					bUninstall = True
				End If
			End If
			Dim myDlg As InstallOptionsDlg = New InstallOptionsDlg(bUninstall)
#End If
			myDlg.ShowDialog()

		Catch aExcept As ThreadAbortException
			' this means we're starting ourself again, so just exit
		Catch aExcept As InstallException
			If (aExcept.Message <> InstallOptionsDlg.strStopMyself) Then
				' New might throw if the repository isn't already installed.
				MessageBox.Show(aExcept.Message, cstrProgramCaption)
			End If
		Catch aExcept As SecurityException
			MessageBox.Show(aExcept.Message, cstrProgramCaption)
		Catch aExcept As Exception
			MessageBox.Show(aExcept.Message, cstrProgramCaption)
		End Try
	End Sub
	Function GetCommandLineArgs() As String()
		' Declare variables.
		Dim separators As String = " "
		Dim commands As String = Microsoft.VisualBasic.Command()
		Dim args() As String = commands.Split(separators.ToCharArray)
		Return args
	End Function

	Public Function DoCmdLine(ByVal sCmdPath As String, ByVal sArgs As String) As Integer
		Dim Si As ProcessStartInfo = New ProcessStartInfo(sCmdPath, sArgs)
		Si.UseShellExecute = False
		Si.CreateNoWindow = True
		Try
			Dim P As Process = Process.Start(Si)
			P.WaitForExit()
			DoCmdLine = P.ExitCode
		Catch e As Exception
			Throw New InstallException(e.Message)
		End Try
	End Function

	Public Sub DeleteFile(ByVal sFilename As String)
		Try
			If (File.Exists(sFilename)) Then
				File.Delete(sFilename)
			End If
		Catch
		End Try
	End Sub

	Public Sub CopyFile(ByVal sSrc As String, ByVal sDst As String)
		Try
			Dim strDirectory As String = Path.GetDirectoryName(sDst)
			If (Not Directory.Exists(strDirectory)) Then
				Directory.CreateDirectory(strDirectory)
			End If
			FileCopy(sSrc, sDst)
		Catch
		End Try
	End Sub

	Public Sub AddRegValue(ByVal strSubKeyName As String, ByVal str As String)
		Try
			' add a registry key so we can gracefully uninstall these on main uninstall
			Dim key As RegistryKey = Registry.LocalMachine.CreateSubKey(InstallOptionsDlg.cstrInstallerKey & "\" & strSubKeyName)

			If ((Not key Is Nothing) And (Not str Is Nothing)) Then
				key.SetValue(str, "")
			End If
		Catch ex As UnauthorizedAccessException
			MessageBox.Show("If this is Windows XP, you need to run this program as an Administrator." & Chr(10) & Chr(10) & ex.ToString, cstrProgramCaption)
		End Try
	End Sub

	Public Sub RemRegValue(ByVal strSubKeyName As String, ByVal strPath As String)
		' add a registry key so we can gracefully uninstall these on main uninstall
		Dim key As RegistryKey = Registry.LocalMachine.OpenSubKey(InstallOptionsDlg.cstrInstallerKey & "\" & strSubKeyName, True)

		If ((Not key Is Nothing) And (Not strPath Is Nothing)) Then
			Try
				key.DeleteValue(strPath)
			Catch ex As Exception
			End Try
		End If
	End Sub

	Public Sub RemRegKey(ByVal strSubKeyName As String, ByVal strSubKeyNameToDelete As String)
		' add a registry key so we can gracefully uninstall these on main uninstall
		Dim key As RegistryKey = Registry.LocalMachine.OpenSubKey(InstallOptionsDlg.cstrInstallerKey & "\" & strSubKeyName, True)

		If ((Not key Is Nothing) And (Not strSubKeyNameToDelete Is Nothing)) Then
			Try
				key.DeleteSubKeyTree(strSubKeyNameToDelete)
			Catch ex As Exception
			End Try
		End If
	End Sub

	Private m_strTestingDir As String
	Public Function TestingDir() As String
		If (m_strTestingDir Is Nothing) Then
			' m_strTestingDir = Environment.GetFolderPath(Environment.SpecialFolder.Personal) & "\My Downloads\SILConverters21\"
			' m_strTestingDir = "F:\SrcSubversion\Distribution Source\Base Package\SILConverters22\"
			Dim key As RegistryKey = Registry.LocalMachine.OpenSubKey(InstallOptionsDlg.cstrInstallerKey)
			If (Not key Is Nothing) Then
				m_strTestingDir = key.GetValue("TestingPath")
			End If
		End If
		TestingDir = m_strTestingDir
	End Function
End Module
