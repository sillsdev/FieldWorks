// --------------------------------------------------------------------------------------------
// Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
//
// File: SemanticDomainsSimpleListChooser.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.ObjectModel;
using System.Windows.Forms;
using System.Xml;
using System.Collections.Generic;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Framework.DetailControls.Resources;
using SIL.Utils;
using SIL.FieldWorks.FDO;
using XCore;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	class SemanticDomainsSimpleListChooser : SimpleListChooser
	{
		/// <summary>
		/// Constructor for use with designer
		/// </summary>
		public SemanticDomainsSimpleListChooser()
			: base()
		{
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// constructor for use with adding a new value
		/// </summary>
		/// <param name="persistProvider">The persist provider.</param>
		/// <param name="labels">The labels.</param>
		/// <param name="fieldName">the user-readable name of the field that is being edited</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// ------------------------------------------------------------------------------------
		public SemanticDomainsSimpleListChooser(IPersistenceProvider persistProvider,
			IEnumerable<ObjectLabel> labels, string fieldName, IHelpTopicProvider helpTopicProvider)
			: base(persistProvider, labels, fieldName, helpTopicProvider)
		{
			m_textSearch.TextChanged += m_textSearch_TextChanged;
			m_originalLabels = labels;
			//m_textSearch.KeyDown += m_textSearch_KeyDown;
		}


		/// <summary>
		/// When the user types in this text box seach through the Semantic Domains to find matches
		/// and display them in the TreeView.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		protected void m_textSearch_TextChanged(object sender, EventArgs e)
		{
			if (String.IsNullOrEmpty(m_textSearch.Tss.Text))
			{
				myTimer.Stop();
				myTimer.Enabled = false;

			} else if (myTimerTickSet == false)
			{
				// Sets the timer interval to 2 seconds.
				myTimer.Interval = 2000;
				myTimer.Start();
				myTimer.Enabled = true;
				myTimerTickSet = true;
				myTimer.Tick += new EventHandler(TimerEventProcessor);
			}
			else
			{
				//myTimer.Tick += new EventHandler(TimerEventProcessor);
				myTimer.Stop();
				myTimer.Enabled = true;
			}

			//if (m_skipCheck)
			//    return;
			//int selStart = m_tbForm.SelectionStart;
			//int selLen = m_tbForm.SelectionLength;
			//int addToSelection;
			//string fixedText = AdjustText(out addToSelection);
			//int selLocation = fixedText.Length;
			//ResetMatches(fixedText);
			//// Even if AdjustText didn't move the selection, it may have changed the text,
			//// which has a side effect in a text box of selecting all of it. We don't want that here,
			//// so reset the selection to what it ought to be.
			//selStart = Math.Min(Math.Max(selStart + addToSelection, 0), fixedText.Length);
			//if (selLen + selStart > fixedText.Length)
			//    selLen = fixedText.Length - selStart;
			//if (m_tbForm.SelectionStart != selStart || m_tbForm.SelectionLength != selLen)
			//    m_tbForm.Select(selStart, selLen);
		}

		private IEnumerable<ICmSemanticDomain> GetSemanticDomainList(ITsString searchString)
		{
			var returnvalues = new Collection<ICmSemanticDomain>();
			var semDomRepo = Cache.ServiceLocator.GetInstance<ICmSemanticDomainRepository>();
			var wsMgr = Cache.ServiceLocator.WritingSystemManager;
			var userWs = wsMgr.UserWs;

			int i = 0;
			foreach (var semD in semDomRepo.AllInstances())
			{
				var hvo = semD.Hvo;
				if (semD.ShortNameTSS.Text.StartsWith(searchString.Text))
				{
					returnvalues.Add(semD);
				}
			}
			return returnvalues;
		}
		private Timer myTimer = new Timer();
		private bool myTimerTickSet = false;

		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		protected void m_textSearch_KeyDown(object sender, EventArgs e)
		{
			//switch (e.KeyCode)
			//{
			//    case Keys.Up:
			//        m_matchingObjectsBrowser.SelectPrevious();
			//        e.Handled = true;
			//        m_tbForm.Select();
			//        break;
			//    case Keys.Down:
			//        m_matchingObjectsBrowser.SelectNext();
			//        e.Handled = true;
			//        m_tbForm.Select();
			//        break;
			//}
		}

		private void TimerEventProcessor(object sender, EventArgs eventArgs)
		{
			myTimer.Tick -= TimerEventProcessor;
			myTimerTickSet = false;
			ChangeSemDomSelection();
		}

		private void ChangeSemDomSelection()
		{
			IEnumerable<ObjectLabel> labels = new List<ObjectLabel>();

			//Gordon will write a method that will return IEnumerable<ICmSemanticDomain>
			//based on the search string we give it.
			var searchString = m_textSearch.Tss;
			if (!string.IsNullOrEmpty(searchString.Text))
			{
				var semDomRepo = Cache.ServiceLocator.GetInstance<ICmSemanticDomainRepository>();
				//var semDomainsToShow = GetSemanticDomainList(searchString);
				var semDomainsToShow = semDomRepo.FindDomainsThatMatch(searchString.Text);
				labels = ObjectLabel.CreateObjectLabels(m_cache, semDomainsToShow, "", DisplayWs);
				var savedCursor = this.Cursor;
				LoadTree(labels, null, false);
				this.Cursor = savedCursor;
			}
			else
			{
				var savedCursor = this.Cursor;
				LoadTree(m_originalLabels, null, false);
				this.Cursor = savedCursor;
			}
		}
	}
}
