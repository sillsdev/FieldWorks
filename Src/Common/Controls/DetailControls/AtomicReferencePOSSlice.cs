using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework.DetailControls.Resources;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.LexText.Controls;
using XCore;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// Summary description for AtomicReferencePOSSlice.
	/// </summary>
	public class AtomicReferencePOSSlice : FieldSlice, IVwNotifyChange
	{
		/// <summary>
		/// Use this to do the Add/RemoveNotifications, since it can be used in the unmanged section of Dispose.
		/// (If m_sda is COM, that is.)
		/// Doing it there will be safer, since there was a risk of it not being removed
		/// in the mananged section, as when disposing was done by the Finalizer.
		/// </summary>
		private ISilDataAccess m_sda;
		private POSPopupTreeManager m_pOSPopupTreeManager;
		private IPartOfSpeech m_pos;
		private bool m_handlingMessage = false;
		protected TreeCombo m_tree;

		private TreeCombo Tree
		{
			get { return m_tree; }
		}

		private IPartOfSpeech POS
		{
			set { m_pos = value; }
			get
			{
				int posHvo = m_cache.DomainDataByFlid.get_ObjectProp(m_obj.Hvo, m_flid);
				if (posHvo == 0)
					m_pos = null;
				else if (m_pos == null || m_pos.Hvo != posHvo)
					m_pos = m_cache.ServiceLocator.GetInstance<IPartOfSpeechRepository>().GetObject(posHvo);
				return m_pos;
			}
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="obj">CmObject that is being displayed.</param>
		/// <param name="flid">The field identifier for the attribute we are displaying.</param>
		/// <param name="mediator"></param>
		/// <param name="propertyTable"></param>
		/// // cache, obj, flid, node, persistenceProvider, stringTbl
		public AtomicReferencePOSSlice(FdoCache cache, ICmObject obj, int flid, Mediator mediator, PropertyTable propertyTable)
			: base(new UserControl(), cache, obj, flid)
		{
			IVwStylesheet stylesheet = FontHeightAdjuster.StyleSheetFromPropertyTable(propertyTable);
			IWritingSystem defAnalWs = m_cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem;
			m_tree = new TreeCombo();
			m_tree.WritingSystemFactory = cache.WritingSystemFactory;
			m_tree.WritingSystemCode = defAnalWs.Handle;
			m_tree.Font = new Font(defAnalWs.DefaultFontName, 10);
			m_tree.StyleSheet = stylesheet;
			if (!Application.RenderWithVisualStyles)
				m_tree.HasBorder = false;
			// We embed the tree combo in a layer of UserControl, so it can have a fixed width
			// while the parent window control is, as usual, docked 'fill' to work with the splitter.
			m_tree.Dock = DockStyle.Left;
			m_tree.Width = 200;
			Control.Controls.Add(m_tree);
			if (m_pOSPopupTreeManager == null)
			{
				ICmPossibilityList list;
				int ws;
				if (obj is IReversalIndexEntry)
				{
					var rie = obj as IReversalIndexEntry;
					list = rie.ReversalIndex.PartsOfSpeechOA;
					ws = m_cache.ServiceLocator.WritingSystemManager.GetWsFromStr(rie.ReversalIndex.WritingSystem);
				}
				else
				{
					list = m_cache.LanguageProject.PartsOfSpeechOA;
					ws = m_cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle;
				}
				m_tree.WritingSystemCode = ws;
				m_pOSPopupTreeManager = new POSPopupTreeManager(m_tree, m_cache, list, ws, false, mediator, propertyTable, propertyTable.GetValue<Form>("window"));
				m_pOSPopupTreeManager.AfterSelect += m_pOSPopupTreeManager_AfterSelect;
			}
			try
			{
				m_handlingMessage = true;
				m_pOSPopupTreeManager.LoadPopupTree(POS == null ? 0 : POS.Hvo);
			}
			finally
			{
				m_handlingMessage = false;
			}
			Control.Height = m_tree.PreferredHeight;
					 // m_tree has sensible PreferredHeight once the text is set, UserControl does not.
					 // we need to set the Height after m_tree.Text has a value set to it.
		}

		#region IVwNotifyChange methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The dafault behavior is for change watchers to call DoEffectsOfPropChange if the
		/// data for the tag being watched has changed.
		/// </summary>
		/// <param name="hvo">The object that was changed</param>
		/// <param name="tag">The property of the object that was changed</param>
		/// <param name="ivMin">the starting character index where the change occurred</param>
		/// <param name="cvIns">the number of characters inserted</param>
		/// <param name="cvDel">the number of characters deleted</param>
		/// ------------------------------------------------------------------------------------
		public virtual void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			CheckDisposed();
			if (m_handlingMessage)
				return;

			if (hvo == m_obj.Hvo && tag == m_flid)
			{
				try
				{
					m_handlingMessage = true;
					var pos = POS;
					HvoTreeNode selNode = null;
					if (Tree.Tree != null)
						selNode = (Tree.Tree.SelectedNode as HvoTreeNode);
					if (selNode != null)
					{
						if (pos == null)
							Tree.Tree.SelectObj(0);
						else if (pos.Hvo != selNode.Hvo)
							Tree.Tree.SelectObj(pos.Hvo);
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
			m_sda = m_cache.DomainDataByFlid;
			m_sda.RemoveNotification(this);	// Just in case...
			m_sda.AddNotification(this);
		}

		#region IDisposable override

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			// m_sda COM object block removed due to crash in Finializer thread LT-6124

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_sda != null)
					m_sda.RemoveNotification(this);

				if (m_tree != null && m_tree.Parent == null)
					m_tree.Dispose();

				if (m_pOSPopupTreeManager != null)
				{
					m_pOSPopupTreeManager.AfterSelect -= new TreeViewEventHandler(m_pOSPopupTreeManager_AfterSelect);
					m_pOSPopupTreeManager.Dispose();
				}
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_sda = null;
			m_cache = null;
			m_tree = null;
			m_pOSPopupTreeManager = null;
			m_pos = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		private void m_pOSPopupTreeManager_AfterSelect(object sender, TreeViewEventArgs e)
		{
			// unless we get a mouse click or simulated mouse click (e.g. by ENTER or TAB),
			// do not treat as an actual selection.
			if (m_handlingMessage  || e.Action != TreeViewAction.ByMouse)
				return;

			int hvoPos = (e.Node as HvoTreeNode).Hvo;
			// if hvoPos is negative, then allow POSPopupTreeManager AfterSelect to handle it.
			if (hvoPos < 0)
				return;
			try
			{
				m_handlingMessage = true;
				UndoableUnitOfWorkHelper.Do(DetailControlsStrings.ksUndoSetCat, DetailControlsStrings.ksRedoSetCat, m_obj, () =>
				{
					m_cache.DomainDataByFlid.SetObjProp(m_obj.Hvo, m_flid, hvoPos);

					// Do some side effects for a couple of MSA classes.
					if (m_obj is IMoInflAffMsa)
					{
						var msa = m_obj as IMoInflAffMsa;
						if (hvoPos == 0)
						{
							msa.SlotsRC.Clear();
						}
						else if (msa.SlotsRC.Count > 0)
						{
							var allSlots = msa.PartOfSpeechRA.AllAffixSlots;
							if (msa.SlotsRC.All(slot => !allSlots.Contains(slot)))
								msa.SlotsRC.Clear();
						}
					}
					else if (m_obj is IMoDerivAffMsa)
					{
						var msa = m_obj as IMoDerivAffMsa;
						if (hvoPos > 0
							&& m_flid == MoDerivAffMsaTags.kflidFromPartOfSpeech
							&& msa.ToPartOfSpeechRA == null)
						{
							msa.ToPartOfSpeechRA = m_cache.ServiceLocator.GetInstance<IPartOfSpeechRepository>().GetObject(hvoPos);
						}
						else if (hvoPos > 0
							&& m_flid == MoDerivAffMsaTags.kflidToPartOfSpeech
							&& msa.FromPartOfSpeechRA == null)
						{
							msa.FromPartOfSpeechRA = m_cache.ServiceLocator.GetInstance<IPartOfSpeechRepository>().GetObject(hvoPos);
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
	/// <summary>
	/// This class shows the POS slice as being disabled.
	/// </summary>
	public class AutomicReferencePOSDisabledSlice : AtomicReferencePOSSlice
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="obj">CmObject that is being displayed.</param>
		/// <param name="flid">The field identifier for the attribute we are displaying.</param>
		/// <param name="mediator"></param>
		/// <param name="propertyTable"></param>
		/// // cache, obj, flid, node, persistenceProvider, stringTbl
		public AutomicReferencePOSDisabledSlice(FdoCache cache, ICmObject obj, int flid, Mediator mediator, PropertyTable propertyTable)
			: base(cache, obj, flid, mediator, propertyTable)
		{
			if (m_tree != null)
				m_tree.ForeColor = SystemColors.GrayText;
		}
	}
}
