// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// An item to show in the chooser which is a command the user can select, rather than a simple choice.
	/// </summary>
	public abstract class ChooserCommand
	{
		/// <summary />
		protected IPropertyTable m_propertyTable;
		/// <summary />
		protected IPublisher m_publisher;
		/// <summary />
		protected ISubscriber m_subscriber;

#if WhenFigureOutWhatThisShouldBe
		protected string m_sHelp;
#endif

		/// <summary>
		/// Initializes a new instance of the class.
		/// </summary>
		protected ChooserCommand(LcmCache cache, bool fCloseBeforeExecuting, string sLabel, IPropertyTable propertyTable, IPublisher publisher, ISubscriber subscriber)
		{
			Cache = cache;
			ShouldCloseBeforeExecuting = fCloseBeforeExecuting;
			Label = sLabel + "  ";  // Extra spaces are just a hack to keep the label from being truncated - I have no idea why it is being truncated
			m_propertyTable = propertyTable;
			m_publisher = publisher;
			m_subscriber = subscriber;
		}

		/// <summary>
		/// Indicates whether or not the chooser dialog should close before executing the link command
		/// </summary>
		public LcmCache Cache { get; set; }

		/// <summary>
		/// Indicates whether or not the chooser dialog should close before executing the link command
		/// </summary>
		public bool ShouldCloseBeforeExecuting { get; set; }

		/// <summary>
		/// The entire text of the label that will appear in the chooser
		/// </summary>
		public string Label { get; set; }

		/// <summary>
		/// Executes this instance.
		/// </summary>
		public virtual ObjectLabel Execute()
		{
			return null;
		}
	}
}
