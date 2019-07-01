// Copyright (c) 2015-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using LanguageExplorer.UtilityTools;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.Language;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Areas.Lexicon.Tools.ReversalIndexes
{
	internal sealed class SortReversalSubEntries : IUtility
	{
		private UtilityDlg m_dlg;

		/// <summary />
		internal SortReversalSubEntries(UtilityDlg utilityDlg)
		{
			if (utilityDlg == null)
			{
				throw new ArgumentNullException(nameof(utilityDlg));
			}
			m_dlg = utilityDlg;
		}

		/// <summary />
		public string Label => LanguageExplorerResources.SortReversalSubentries_Label;

		/// <summary>
		/// This is what is actually shown in the dialog as the ID of the task.
		/// </summary>
		public override string ToString()
		{
			return Label;
		}

		/// <summary />
		public void OnSelection()
		{
			m_dlg.WhenDescription = LanguageExplorerResources.ksWhenToSortReversalSubentries;
			m_dlg.WhatDescription = LanguageExplorerResources.ksWhatIsSortReversalSubentries;
			m_dlg.RedoDescription = LanguageExplorerResources.ksWarningSortReversalSubentries;
		}

		/// <summary />
		public void Process()
		{
			var cache = m_dlg.PropertyTable.GetValue<LcmCache>(FwUtils.cache);
			NonUndoableUnitOfWorkHelper.DoSomehow(cache.ActionHandlerAccessor, () =>
			{
				SortReversalSubEntriesInPlace(cache);
				MessageBox.Show(m_dlg, LanguageExplorerResources.SortReversalSubEntries_CompletedContent, LanguageExplorerResources.SortReversalSubEntries_CompletedTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
			});
		}

		/// <summary />
		internal static void SortReversalSubEntriesInPlace(LcmCache cache)
		{
			var allReversalIndexes = cache.ServiceLocator.GetInstance<IReversalIndexRepository>().AllInstances();
			foreach (var reversalIndex in allReversalIndexes)
			{
				using (var comp = new ReversalSubEntryIcuComparer(cache, reversalIndex.WritingSystem))
				{
					foreach (var reversalIndexEntry in reversalIndex.EntriesOC.Where(rie => rie.SubentriesOS.Count > 1))
					{
						var subEntryArray = reversalIndexEntry.SubentriesOS.ToArray();
						Array.Sort(subEntryArray, comp);
						for (var i = 0; i < subEntryArray.Length; ++i)
						{
							reversalIndexEntry.SubentriesOS.Insert(i, subEntryArray[i]);
						}
					}
				}
			}
		}

		/// <summary />
		private sealed class ReversalSubEntryIcuComparer : IComparer<IReversalIndexEntry>, IDisposable
		{
			private readonly int m_ws;
			private readonly ManagedLgIcuCollator m_collator;

			/// <summary />
			public ReversalSubEntryIcuComparer(LcmCache cache, string ws)
			{
				m_collator = new ManagedLgIcuCollator();
				m_ws = cache.WritingSystemFactory.GetWsFromStr(ws);
				m_collator.Open(ws);
			}

			/// <summary />
			public int Compare(IReversalIndexEntry x, IReversalIndexEntry y)
			{
				var xString = x.ReversalForm.get_String(m_ws);
				var yString = y.ReversalForm.get_String(m_ws);
				return m_collator.Compare(xString.Text, yString.Text, LgCollatingOptions.fcoIgnoreCase);
			}

			#region disposal
			~ReversalSubEntryIcuComparer() { Dispose(false); }

			/// <summary />
			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			/// <summary />
			private void Dispose(bool disposing)
			{
				Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
				if (disposing)
				{
					m_collator?.Dispose();
				}
			}
			#endregion disposal
		}
	}
}