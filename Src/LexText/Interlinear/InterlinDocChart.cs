// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using XCore;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// Dummy subclass so we can design the Chart tab.
	/// </summary>
	public class InterlinDocChart : UserControl, IInterlinearTabControl, IInterlinConfigurable, ISetupLineChoices
	{
		public LcmCache Cache { get; set; }
		public PropertyTable PropertyTable { get; set; }
		public InterlinVc Vc { get; protected set; }
		public IVwRootBox Rootb { get; set; }

		private void InitializeComponent()
		{
			this.SuspendLayout();
			//
			// InterlinDocChart
			//
			this.AccessibleName = "InterlinDocChart";
			this.Name = "InterlinDocChart";
			this.ResumeLayout(false);
		}

		public InterlinDocChart()
		{
			InitializeComponent();
		}

		public virtual void SetRoot(int hvo)
		{
			throw new System.NotImplementedException();
		}

		public virtual bool OnConfigureInterlinear(object argument)
		{
			throw new System.NotImplementedException();
		}

		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + " ******");
			Vc.Dispose();
			base.Dispose(disposing);
		}

		public bool ForEditing { get; set; }

		public virtual InterlinLineChoices SetupLineChoices(string lineConfigPropName, InterlinLineChoices.InterlinMode mode)
		{
			throw new System.NotImplementedException();
		}

	}
}