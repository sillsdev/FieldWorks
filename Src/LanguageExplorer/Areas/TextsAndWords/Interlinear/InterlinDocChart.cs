// Copyright (c) 2010-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	/// <summary>
	/// Dummy subclass so we can design the Chart tab.
	/// </summary>
	public class InterlinDocChart : UserControl, IInterlinConfigurable, ISetupLineChoices
	{
		public LcmCache Cache { get; set; }
		public IPropertyTable PropertyTable { get; set; }
		public InterlinVc Vc { get; protected set; }
		public IVwRootBox Rootb { get; set; }

		private void InitializeComponent()
		{
			SuspendLayout();
			AccessibleName = "InterlinDocChart";
			Name = "InterlinDocChart";
			ResumeLayout(false);
		}

		public InterlinDocChart()
		{
			InitializeComponent();
		}

		public virtual void SetRoot(int hvo)
		{
			throw new NotSupportedException();
		}

		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + " ******");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				Vc.Dispose();
			}

			base.Dispose(disposing);
		}

		public bool ForEditing { get; set; }

		public virtual InterlinLineChoices SetupLineChoices(string lineConfigPropName, InterlinMode mode)
		{
			throw new NotSupportedException();
		}
	}
}