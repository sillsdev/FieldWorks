// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ChooserCommand.cs
// Responsibility: Andy Black
// Last reviewed:
//
// <remarks>
// </remarks>

using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary>
	/// An item to show in the chooser which is a command the user can select, rather than a simple choice.
	/// </summary>
	public abstract class ChooserCommand
	{
		/// <summary />
		protected bool m_fShouldCloseBeforeExecuting;
		/// <summary />
		protected string m_sLabel;
		/// <summary />
		protected LcmCache m_cache;
		/// <summary />
		protected IPropertyTable m_propertyTable;
		/// <summary />
		protected IPublisher m_publisher;
		/// <summary />
		protected ISubscriber m_subscriber;

#if WhenFigureOutWhatThisShouldBe
		protected string m_sHelp;
#endif

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:ChooserCommand"/> class.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="fCloseBeforeExecuting">if set to <c>true</c> [f close before executing].</param>
		/// <param name="sLabel">The s label.</param>
		/// <param name="propertyTable"></param>
		/// <param name="publisher"></param>
		/// <param name="subscriber"></param>
		/// ------------------------------------------------------------------------------------
		protected ChooserCommand(LcmCache cache, bool fCloseBeforeExecuting, string sLabel, IPropertyTable propertyTable, IPublisher publisher, ISubscriber subscriber)
		{
			m_cache = cache;
			m_fShouldCloseBeforeExecuting = fCloseBeforeExecuting;
			m_sLabel = sLabel + "  ";  // Extra spaces are just a hack to keep the label from being truncated - I have no idea why it is being truncated
			m_propertyTable = propertyTable;
			m_publisher = publisher;
			m_subscriber = subscriber;
		}

		//properties
		/// <summary>
		/// Indicates whether or not the chooser dialog should close before executing the link command
		/// </summary>
		public LcmCache Cache
		{
			get
			{
				return m_cache;
			}
			set
			{
				m_cache = value;
			}
		}

		/// <summary>
		/// Indicates whether or not the chooser dialog should close before executing the link command
		/// </summary>
		public bool ShouldCloseBeforeExecuting
		{
			get
			{
				return m_fShouldCloseBeforeExecuting;
			}
			set
			{
				m_fShouldCloseBeforeExecuting = value;
			}
		}
		/// <summary>
		/// The entire text of the label that will appear in the chooser
		/// </summary>
		public string Label
		{
			get
			{
				return m_sLabel;
			}
			set
			{
				m_sLabel = value;
			}
		}

		//methods

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Executes this instance.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public virtual ObjectLabel Execute()
		{
			return null;
		}

	}
}
