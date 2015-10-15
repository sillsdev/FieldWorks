// Copyright (c) 2004-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;

namespace SIL.FieldWorks.Resources
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Non-static methods and fields that are accessed by ResourceHelper.
	/// </summary>
	/// <remarks>The non-static methods and fields are in a separate class so that clients can
	/// use ResourceHelper without the need for a reference to Windows.Forms if all they need is
	/// to get some strings.</remarks>
	/// ----------------------------------------------------------------------------------------
	public partial class ResourceHelperImpl : Form
	{
		#region Construction and destruction
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor for ResourceHelper
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ResourceHelperImpl()
		{
			InitializeComponent();
		}

		/// <summary>
		/// Dispose static member variables
		/// </summary>
		protected internal virtual void DisposeStaticMembers()
		{
			if (ResourceHelper.s_stringResources != null)
				ResourceHelper.s_stringResources.ReleaseAllResources();
			ResourceHelper.s_stringResources = null;
			if (ResourceHelper.s_helpResources != null)
				ResourceHelper.s_helpResources.ReleaseAllResources();
			ResourceHelper.s_helpResources = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				if (components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose(disposing);
		}
		#endregion

		protected static ResourceHelperImpl Helper
		{
			get { return ResourceHelper.Helper; }
			set { ResourceHelper.Helper = value; }
		}
	}
}
