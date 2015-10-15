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

using System;
using System.Collections;
using System.Windows.Forms;

using SIL.FieldWorks.FDO;
using XCore;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary>
	/// An item to show in the chooser which is a command the user can select, rather than a simple choice.
	/// </summary>
	public abstract class ChooserCommand
	{
		/// <summary></summary>
		protected bool m_fShouldCloseBeforeExecuting;
		/// <summary></summary>
		protected string m_sLabel;
		/// <summary></summary>
		protected FdoCache m_cache;
		/// <summary></summary>
		protected Mediator m_mediator;
		/// <summary></summary>
		protected PropertyTable m_propertyTable;

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
		/// <param name="mediator">The mediator.</param>
		/// <param name="propertyTable"></param>
		/// ------------------------------------------------------------------------------------
		protected ChooserCommand(FdoCache cache, bool fCloseBeforeExecuting, string sLabel,
			Mediator mediator, PropertyTable propertyTable)
		{
			m_cache = cache;
			m_fShouldCloseBeforeExecuting = fCloseBeforeExecuting;
			m_sLabel = sLabel + "  ";  // Extra spaces are just a hack to keep the label from being truncated - I have no idea why it is being truncated
			m_mediator = mediator;
			m_propertyTable = propertyTable;
		}

		//properties
		/// <summary>
		/// Indicates whether or not the chooser dialog should close before executing the link command
		/// </summary>
		public FdoCache Cache
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
		/// Access the XCore.Mediator.
		/// </summary>
		public XCore.Mediator Mediator
		{
			get { return m_mediator; }
			set { m_mediator = value; }
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
