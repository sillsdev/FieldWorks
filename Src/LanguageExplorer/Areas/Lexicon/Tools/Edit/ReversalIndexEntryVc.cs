// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;

namespace LanguageExplorer.Areas.Lexicon.Tools.Edit
{
	/// <summary />
	internal class ReversalIndexEntryVc : FwBaseVc, IDisposable
	{
		private List<IReversalIndex> m_usedIndices;
		ITsTextProps m_ttpLabel; // Props to use for ws name labels.

		/// <summary />
		public ReversalIndexEntryVc(List<IReversalIndex> usedIndices, LcmCache cache)
		{
			Cache = cache;
			m_usedIndices = usedIndices;
			m_ttpLabel = WritingSystemServices.AbbreviationTextProperties;
		}

		#region Disposable stuff
		/// <summary/>
		~ReversalIndexEntryVc()
		{
			Dispose(false);
		}

		/// <summary>
		/// Throw if the IsDisposed property is true
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
			{
				throw new ObjectDisposedException(GetType().ToString(), "This object is being used after it has been disposed: this is an Error.");
			}
		}

		/// <summary/>
		public bool IsDisposed { get; private set; }

		/// <summary/>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary/>
		protected virtual void Dispose(bool fDisposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType() + " *******");
			if (IsDisposed)
			{
				return; // No need to run it more than once.
			}
			if (fDisposing)
			{
				// Dispose managed resources here.
				m_usedIndices?.Clear();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_cache = null;
			m_usedIndices = null;
			m_ttpLabel = null;
			IsDisposed = true;
		}
		#endregion

		/// <summary />
		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			CheckDisposed();

			var da = vwenv.DataAccess;
			switch (frag)
			{
				default:
				{
					Debug.Assert(false, "Unrecognized fragment.");
					break;
				}
				case ReversalIndexEntrySliceView.kFragMainObject:
				{
					// The hvo here is for the sense.

					// We use a table to display
					// encodings in column one and the strings in column two.
					// The table uses 100% of the available width.
					VwLength vlTable;
					vlTable.nVal = 10000;
					vlTable.unit = VwUnit.kunPercent100;
					// The width of the writing system column is determined from the width of the
					// longest one which will be displayed.
					var dxsMax = 0; // Max width required.
					var sda = vwenv.DataAccess;
					foreach (var idx in m_usedIndices)
					{
						var wsHandle = m_cache.ServiceLocator.WritingSystemManager.GetWsFromStr(idx.WritingSystem);
						int dxs;    // Width of displayed string.
						int dys;    // Height of displayed string (not used here).
						vwenv.get_StringWidth(sda.get_StringProp(wsHandle, ReversalEntryDataAccess.kflidWsAbbr),
							m_ttpLabel,
							out dxs,
							out dys);
						dxsMax = Math.Max(dxsMax, dxs);
					}
					VwLength vlColWs; // 5-pt space plus max label width.
					vlColWs.nVal = dxsMax + 5000;
					vlColWs.unit = VwUnit.kunPoint1000;
					// Enhance JohnT: possibly allow for right-to-left UI by reversing columns?
					// The Main column is relative and uses the rest of the space.
					VwLength vlColMain;
					vlColMain.nVal = 1;
					vlColMain.unit = VwUnit.kunRelative;

					vwenv.OpenTable(2, // Two columns.
						vlTable, // Table uses 100% of available width.
						0, // Border thickness.
						VwAlignment.kvaLeft, // Default alignment.
						VwFramePosition.kvfpVoid, // No border.
						VwRule.kvrlNone, // No rules between cells.
						0, // No forced space between cells.
						0, // No padding inside cells.
						false);
					// Specify column widths. The first argument is the number of columns,
					// not a column index. The writing system column only occurs at all if its
					// width is non-zero.
					vwenv.MakeColumns(1, vlColWs);
					vwenv.MakeColumns(1, vlColMain);

					vwenv.OpenTableBody();
					// Do vector of rows. Each row essentially is a reversal index, but shows other information.
					vwenv.AddObjVec(ReversalIndexEntrySliceView.kFlidIndices, this, ReversalIndexEntrySliceView.kFragIndices);
					vwenv.CloseTableBody();
					vwenv.CloseTable();
					break;
				}
				case ReversalIndexEntrySliceView.kFragIndexMain:
				{
					// First cell has writing system abbreviation displayed using m_ttpLabel.
					var wsHvo = 0;
					foreach (var idx in m_usedIndices)
					{
						if (idx.Hvo == hvo)
						{
							wsHvo = m_cache.ServiceLocator.WritingSystemManager.GetWsFromStr(idx.WritingSystem);
							break;
						}
					}
					Debug.Assert(wsHvo > 0, "Could not find writing system.");

					var wsOldDefault = DefaultWs;
					DefaultWs = wsHvo;

					// Cell 1 shows the ws abbreviation.
					vwenv.OpenTableCell(1, 1);
					vwenv.Props = m_ttpLabel;
					vwenv.AddObj(wsHvo, this, ReversalIndexEntrySliceView.kFragWsAbbr);
					vwenv.CloseTableCell();

					// Second cell has the contents for the reversal entries.
					vwenv.OpenTableCell(1, 1);
					// This displays the field flush right for RTL data, but gets arrow keys to
					// behave reasonably.  See comments on LT-5287.
					var wsObj = m_cache.ServiceLocator.WritingSystemManager.Get(DefaultWs);
					if (wsObj != null && wsObj.RightToLeftScript)
					{
						vwenv.set_IntProperty((int)FwTextPropType.ktptRightToLeft, (int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvForceOn);
					}
					vwenv.OpenParagraph();
					// Do vector of entries in the second column.
					vwenv.AddObjVec(ReversalIndexEntrySliceView.kFlidEntries, this, ReversalIndexEntrySliceView.kFragEntries);
					vwenv.CloseParagraph();
					vwenv.CloseTableCell();

					DefaultWs = wsOldDefault;
					break;
				}
				case ReversalIndexEntrySliceView.kFragEntryForm:
				{
					vwenv.AddStringAltMember(ReversalIndexEntryTags.kflidReversalForm, DefaultWs, this);
					var hvoCurrent = vwenv.CurrentObject();
					if (hvoCurrent > 0)
					{
						var rie = m_cache.ServiceLocator.GetInstance<IReversalIndexEntryRepository>().GetObject(hvoCurrent);
						Debug.Assert(rie != null);
						var rgWs = WritingSystemServices.GetReversalIndexWritingSystems(m_cache, rie.Hvo, false);
						var wsAnal = m_cache.DefaultAnalWs;
						var tisb = TsStringUtils.MakeIncStrBldr();
						tisb.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault, wsAnal);
						tisb.SetIntPropValues((int)FwTextPropType.ktptEditable,
							(int)FwTextPropVar.ktpvEnum,
							(int)TptEditable.ktptNotEditable);
						tisb.Append(" [");
						var cstr = 0;
						ITsTextProps ttpBase = null;
						ITsTextProps ttpLabel = null;
						foreach (var writingSystemDefinition in rgWs)
						{
							var ws = writingSystemDefinition.Handle;
							if (ws == DefaultWs)
							{
								continue;
							}
							var sForm = rie.ReversalForm.get_String(ws).Text;
							if (string.IsNullOrEmpty(sForm))
							{
								continue;
							}

							if (cstr > 0)
							{
								tisb.Append(", ");
							}
							++cstr;
							var sWs = writingSystemDefinition.Abbreviation;
							if (!string.IsNullOrEmpty(sWs))
							{
								if (ttpBase == null)
								{
									var tpbLabel = m_ttpLabel.GetBldr();
									tpbLabel.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault, wsAnal);
									ttpLabel = tpbLabel.GetTextProps();
									// We have to totally replace the properties set by ttpLabel.  The
									// simplest way is to create another ITsString with the simple base
									// property of only the default analysis writing system.
									var tpbBase = TsStringUtils.MakePropsBldr();
									tpbBase.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault, wsAnal);
									ttpBase = tpbBase.GetTextProps();
								}
								var tssWs = TsStringUtils.MakeString(sWs, ttpLabel);
								tisb.AppendTsString(tssWs);
								var tssSpace = TsStringUtils.MakeString(" ", ttpBase);
								tisb.AppendTsString(tssSpace);
							}
							tisb.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault, ws);
							tisb.Append(sForm);
							tisb.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault, wsAnal);
						}
						if (cstr > 0)
						{
							tisb.Append("]");
							var tss = tisb.GetString();
							vwenv.AddString(tss);
						}
					}
					break;
				}
				case ReversalIndexEntrySliceView.kFragWsAbbr:
				{
					vwenv.AddString(da.get_StringProp(hvo, ReversalEntryDataAccess.kflidWsAbbr));
					break;
				}
			}
		}

		/// <summary />
		public override void DisplayVec(IVwEnv vwenv, int hvo, int tag, int frag)
		{
			CheckDisposed();

			var da = vwenv.DataAccess;
			switch (frag)
			{
				default:
				{
					Debug.Assert(false, "Unrecognized fragment.");
					break;
				}
				case ReversalIndexEntrySliceView.kFragIndices:
				{
					// hvo here is the sense.
					var countRows = da.get_VecSize(hvo, tag);
					Debug.Assert(countRows == m_usedIndices.Count, "Mismatched number of indices.");
					for (var i = 0; i < countRows; ++i)
					{
						vwenv.OpenTableRow();

						var idxHvo = da.get_VecItem(hvo, tag, i);
						vwenv.AddObj(idxHvo, this, ReversalIndexEntrySliceView.kFragIndexMain);

						vwenv.CloseTableRow();
					}
					break;
				}
				case ReversalIndexEntrySliceView.kFragEntries:
				{
					var wsHvo = 0;
					foreach (var idx in m_usedIndices)
					{
						if (idx.Hvo == hvo)
						{
							wsHvo = m_cache.ServiceLocator.WritingSystemManager.GetWsFromStr(idx.WritingSystem);
							break;
						}
					}
					Debug.Assert(wsHvo > 0, "Could not find writing system.");
					var wsOldDefault = DefaultWs;
					DefaultWs = wsHvo;

					// hvo here is a reversal index.
					var countEntries = da.get_VecSize(hvo, ReversalIndexEntrySliceView.kFlidEntries);
					for (var j = 0; j < countEntries; ++j)
					{
						if (j != 0)
						{
							vwenv.AddSeparatorBar();
						}
						var entryHvo = da.get_VecItem(hvo, ReversalIndexEntrySliceView.kFlidEntries, j);
						vwenv.AddObj(entryHvo, this, ReversalIndexEntrySliceView.kFragEntryForm);
					}

					DefaultWs = wsOldDefault;
					break;
				}
			}
		}
	}
}