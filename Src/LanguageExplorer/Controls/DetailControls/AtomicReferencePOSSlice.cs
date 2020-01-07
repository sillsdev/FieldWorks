// Copyright (c) 2005-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using LanguageExplorer.Controls.DetailControls.Resources;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary />
	internal class AtomicReferencePOSSlice : FieldSlice, IVwNotifyChange
	{
		/// <summary>
		/// Use this to do the Add/RemoveNotifications, since it can be used in the unmanaged section of Dispose.
		/// (If m_sda is COM, that is.)
		/// Doing it there will be safer, since there was a risk of it not being removed
		/// in the managed section, as when disposing was done by the Finalizer.
		/// </summary>
		private ISilDataAccess m_sda;
		private POSPopupTreeManager m_pOSPopupTreeManager;
		private IPartOfSpeech m_pos;
		private bool m_handlingMessage;

		protected TreeCombo Tree { get; set; }

		private IPartOfSpeech POS
		{
			get
			{
				var posHvo = Cache.DomainDataByFlid.get_ObjectProp(MyCmObject.Hvo, m_flid);
				if (posHvo == 0)
				{
					m_pos = null;
				}
				else if (m_pos == null || m_pos.Hvo != posHvo)
				{
					m_pos = Cache.ServiceLocator.GetInstance<IPartOfSpeechRepository>().GetObject(posHvo);
				}
				return m_pos;
			}
		}

		/// <summary />
		public AtomicReferencePOSSlice(LcmCache cache, ICmObject obj, int flid, FlexComponentParameters flexComponentParameters)
			: base(new UserControl(), cache, obj, flid)
		{
			IVwStylesheet stylesheet = FwUtils.StyleSheetFromPropertyTable(flexComponentParameters.PropertyTable);
			var defAnalWs = Cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem;
			Tree = new TreeCombo
			{
				WritingSystemFactory = cache.WritingSystemFactory,
				WritingSystemCode = defAnalWs.Handle,
				Font = new Font(defAnalWs.DefaultFontName, 10),
				StyleSheet = stylesheet
			};
			if (!Application.RenderWithVisualStyles)
			{
				Tree.HasBorder = false;
			}
			// We embed the tree combo in a layer of UserControl, so it can have a fixed width
			// while the parent window control is, as usual, docked 'fill' to work with the splitter.
			Tree.Dock = DockStyle.Left;
			Tree.Width = 200;
			Control.Controls.Add(Tree);
			if (m_pOSPopupTreeManager == null)
			{
				ICmPossibilityList list;
				int ws;
				var rie = obj as IReversalIndexEntry;
				if (rie != null)
				{
					list = rie.ReversalIndex.PartsOfSpeechOA;
					ws = Cache.ServiceLocator.WritingSystemManager.GetWsFromStr(rie.ReversalIndex.WritingSystem);
				}
				else
				{
					list = Cache.LanguageProject.PartsOfSpeechOA;
					ws = Cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle;
				}
				Tree.WritingSystemCode = ws;
				m_pOSPopupTreeManager = new POSPopupTreeManager(Tree, Cache, list, ws, false, flexComponentParameters, flexComponentParameters.PropertyTable.GetValue<Form>(FwUtils.window));
				m_pOSPopupTreeManager.AfterSelect += m_pOSPopupTreeManager_AfterSelect;
			}
			try
			{
				m_handlingMessage = true;
				m_pOSPopupTreeManager.LoadPopupTree(POS?.Hvo ?? 0);
			}
			finally
			{
				m_handlingMessage = false;
			}
			// m_tree has sensible PreferredHeight once the text is set, UserControl does not.
			// we need to set the Height after m_tree.Text has a value set to it.
			Control.Height = Tree.PreferredHeight;
		}

		#region IVwNotifyChange methods
		/// <summary>
		/// The dafault behavior is for change watchers to call DoEffectsOfPropChange if the
		/// data for the tag being watched has changed.
		/// </summary>
		public virtual void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			if (m_handlingMessage)
			{
				return;
			}
			if (hvo == MyCmObject.Hvo && tag == m_flid)
			{
				try
				{
					m_handlingMessage = true;
					var pos = POS;
					HvoTreeNode selNode = null;
					if (Tree.Tree != null)
					{
						selNode = (Tree.Tree.SelectedNode as HvoTreeNode);
					}
					if (selNode != null)
					{
						if (pos == null)
						{
							Tree.Tree.SelectObj(0);
						}
						else if (pos.Hvo != selNode.Hvo)
						{
							Tree.Tree.SelectObj(pos.Hvo);
						}
					}
				}
				finally
				{
					m_handlingMessage = false;
				}
			}
		}
		#endregion

		protected override void UpdateDisplayFromDatabase()
		{
			m_sda = Cache.DomainDataByFlid;
			m_sda.RemoveNotification(this); // Just in case...
			m_sda.AddNotification(this);
		}

		#region IDisposable override

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				// Dispose managed resources here.
				m_sda?.RemoveNotification(this);

				if (Tree != null && Tree.Parent == null)
				{
					Tree.Dispose();
				}

				if (m_pOSPopupTreeManager != null)
				{
					m_pOSPopupTreeManager.AfterSelect -= m_pOSPopupTreeManager_AfterSelect;
					m_pOSPopupTreeManager.Dispose();
				}
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_sda = null;
			Cache = null;
			Tree = null;
			m_pOSPopupTreeManager = null;
			m_pos = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		private void m_pOSPopupTreeManager_AfterSelect(object sender, TreeViewEventArgs e)
		{
			// unless we get a mouse click or simulated mouse click (e.g. by ENTER or TAB),
			// do not treat as an actual selection.
			if (m_handlingMessage || e.Action != TreeViewAction.ByMouse)
			{
				return;
			}
			var hvoPos = (e.Node as HvoTreeNode).Hvo;
			// if hvoPos is negative, then allow POSPopupTreeManager AfterSelect to handle it.
			if (hvoPos < 0)
			{
				return;
			}
			try
			{
				m_handlingMessage = true;
				UndoableUnitOfWorkHelper.Do(DetailControlsStrings.ksUndoSetCat, DetailControlsStrings.ksRedoSetCat, MyCmObject, () =>
				{
					Cache.DomainDataByFlid.SetObjProp(MyCmObject.Hvo, m_flid, hvoPos);
					// Do some side effects for a couple of MSA classes.
					if (MyCmObject is IMoInflAffMsa)
					{
						var msa = (IMoInflAffMsa)MyCmObject;
						if (hvoPos == 0)
						{
							msa.SlotsRC.Clear();
						}
						else if (msa.SlotsRC.Count > 0)
						{
							var allSlots = msa.PartOfSpeechRA.AllAffixSlots;
							if (msa.SlotsRC.All(slot => !allSlots.Contains(slot)))
							{
								msa.SlotsRC.Clear();
							}
						}
					}
					else if (MyCmObject is IMoDerivAffMsa)
					{
						var msa = (IMoDerivAffMsa)MyCmObject;
						if (hvoPos > 0 && m_flid == MoDerivAffMsaTags.kflidFromPartOfSpeech && msa.ToPartOfSpeechRA == null)
						{
							msa.ToPartOfSpeechRA = Cache.ServiceLocator.GetInstance<IPartOfSpeechRepository>().GetObject(hvoPos);
						}
						else if (hvoPos > 0 && m_flid == MoDerivAffMsaTags.kflidToPartOfSpeech && msa.FromPartOfSpeechRA == null)
						{
							msa.FromPartOfSpeechRA = Cache.ServiceLocator.GetInstance<IPartOfSpeechRepository>().GetObject(hvoPos);
						}
					}
				});
			}
			finally
			{
				m_handlingMessage = false;
			}
		}
	}
}