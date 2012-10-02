using System;
using System.Windows.Forms;
using ECInterfaces;
using Microsoft.Win32;          // for RegistryKey

namespace ClipboardEC
{
	/// <summary>
	/// Summary description for ProcTypeMenuItem.
	/// </summary>
	public class ProcTypeMenuItem : System.Windows.Forms.ToolStripMenuItem
	{
		private ProcessTypeFlags m_eProcessType;
		private FormClipboardEncConverter   m_myParent;

		public ProcTypeMenuItem()
		{
		}

		public void InitializeComponent(ProcessTypeFlags eProcessType, FormClipboardEncConverter myParent)
		{
			m_eProcessType = eProcessType;
			m_myParent = myParent;
			this.Click += new EventHandler(ProcTypeMenuItem_Click);

			this.Checked = !((m_eProcessType & m_myParent.ProcessTypeFilter) == 0);
		}

		private void ProcTypeMenuItem_Click(object sender, EventArgs e)
		{
			m_myParent.showAllTransductionTypesToolStripMenuItem.Checked = false;
			this.Checked = !this.Checked;
			if( this.Checked )
			{
				m_myParent.ProcessTypeFilter |= m_eProcessType;
			}
			else
			{
				m_myParent.ProcessTypeFilter &= ~m_eProcessType;
			}
			m_myParent.UpdateFilteringIndication();

			// add that to the registry also, so we remember it.
			RegistryKey keyLastDebugState = Registry.CurrentUser.CreateSubKey(FormClipboardEncConverter.cstrProjectMemoryKey);
			keyLastDebugState.SetValue(FormClipboardEncConverter.cstrProjectTransTypeFilterLastState, (Int32)m_myParent.ProcessTypeFilter);
		}
	}

	public class ImplTypeMenuItem : System.Windows.Forms.ToolStripMenuItem
	{
		private string m_strImplType;
		private FormClipboardEncConverter   m_myParent;

		public ImplTypeMenuItem(string strImplType, string strDisplayName, FormClipboardEncConverter myParent)
		{
			m_strImplType = strImplType;
			m_myParent = myParent;
			this.Click +=new EventHandler(ImplTypeMenuItem_Click);
			this.Text = strDisplayName;
			// this.RadioCheck = true;
		}

		private void ImplTypeMenuItem_Click(object sender, EventArgs e)
		{
			foreach (ToolStripMenuItem aMenuItem in m_myParent.byImplementationTypeToolStripMenuItem.DropDownItems)
				aMenuItem.Checked = false;
			this.Checked = true;
			m_myParent.ImplTypeFilter = m_strImplType;
			m_myParent.UpdateFilteringIndication();

			// add that to the registry also, so we remember it.
			RegistryKey keyLastDebugState = Registry.CurrentUser.CreateSubKey(FormClipboardEncConverter.cstrProjectMemoryKey);
			keyLastDebugState.SetValue(FormClipboardEncConverter.cstrProjectImplTypeFilterLastState, ((m_strImplType == null) ? "" : m_strImplType));
		}
	}

	public class EncodingFilterMenuItem : System.Windows.Forms.ToolStripMenuItem
	{
		private FormClipboardEncConverter   m_myParent;

		public EncodingFilterMenuItem(string strEncodingFilter, FormClipboardEncConverter myParent)
		{
			m_myParent = myParent;
			this.Click +=new EventHandler(EncodingFilterMenuItem_Click);
			this.Text = strEncodingFilter;
			// this.RadioCheck = true;
		}

		private void EncodingFilterMenuItem_Click(object sender, EventArgs e)
		{
			foreach (ToolStripMenuItem aMenuItem in m_myParent.byEncodingToolStripMenuItem.DropDownItems)
				aMenuItem.Checked = false;
			this.Checked = true;

			// if this is the top menu item (which indicates: filtering off), then clear out the filter string
			string strEncodingID = null;
			if( this.Text != FormClipboardEncConverter.cstrProjectEncodingFilterOffDisplayString )
				strEncodingID = this.Text;

			m_myParent.EncodingFilter = strEncodingID;
			m_myParent.UpdateFilteringIndication();

			// add that to the registry also, so we remember it.
			RegistryKey keyLastDebugState = Registry.CurrentUser.CreateSubKey(FormClipboardEncConverter.cstrProjectMemoryKey);
			keyLastDebugState.SetValue(FormClipboardEncConverter.cstrProjectEncodingFilterLastState, ((strEncodingID == null) ? "" : strEncodingID));
		}
	}
}
