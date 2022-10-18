<<<<<<< HEAD:Src/LanguageExplorer/Impls/UserInterfaceChooser.Designer.cs
// Copyright (c) 2007-2020 SIL International
||||||| f013144d5:Src/Common/Controls/Widgets/UserInterfaceChooser.Designer.cs
// Copyright (c) 2015 SIL International
=======
// Copyright (c) 2007-2021 SIL International
>>>>>>> develop:Src/Common/Controls/Widgets/UserInterfaceChooser.Designer.cs
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

<<<<<<< HEAD:Src/LanguageExplorer/Impls/UserInterfaceChooser.Designer.cs
using System.Diagnostics;

namespace LanguageExplorer.Impls
||||||| f013144d5:Src/Common/Controls/Widgets/UserInterfaceChooser.Designer.cs
using SIL.PlatformUtilities;

namespace SIL.FieldWorks.Common.Widgets
=======
namespace SIL.FieldWorks.Common.Widgets
>>>>>>> develop:Src/Common/Controls/Widgets/UserInterfaceChooser.Designer.cs
{
	partial class UserInterfaceChooser
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				components?.Dispose();
				m_foreColorBrush?.Dispose();
			}
			m_foreColorBrush = null;

			base.Dispose(disposing);
		}

		#region Component Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.SuspendLayout();
			//
			// UserInterfaceChooser
			//
			this.AccessibleName = "UserInterfaceChooser";
			this.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.FormattingEnabled = true;
			this.Size = new System.Drawing.Size(195, 21);
			this.Sorted = true;
			this.ResumeLayout(false);

		}

		#endregion
	}
}
