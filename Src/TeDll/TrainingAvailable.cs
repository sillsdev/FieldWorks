using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using SIL.Utils;

namespace SIL.FieldWorks.TE
{
	/// <summary>
	/// Show a Training Available message
	/// </summary>
	public partial class TrainingAvailable : Form, IFWDisposable
	{
		/// <summary>
		///
		/// </summary>
		public TrainingAvailable()
		{
			InitializeComponent();
		}

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		/// When the window is created, make sure it comes forward.
		/// </summary>
		protected override void OnShown(EventArgs e)
		{
			base.OnShown(e);
			Activate();
		}
	}
}