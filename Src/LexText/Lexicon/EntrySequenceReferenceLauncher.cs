using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.Xml;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.LexText.Controls;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.XWorks.LexEd
{
	public class EntrySequenceReferenceLauncher : VectorReferenceLauncher, IVwNotifyChange
	{
		/// <summary>
		/// Use this to do the Add/RemoveNotifications, since it can be used in the unmanged section of Dispose.
		/// (If m_sda is COM, that is.)
		/// Doing it there will be safer, since there was a risk of it not being removed
		/// in the mananged section, as when disposing was done by the Finalizer.
		/// </summary>
		private ISilDataAccess m_sda;

		private System.ComponentModel.IContainer components = null;

		public EntrySequenceReferenceLauncher()
		{
			// This call is required by the Windows Form Designer.
			InitializeComponent();

			// TODO: Add any initialization after the InitializeComponent call
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if( disposing )
			{
				// Do this first, before setting m_fDisposing to true.
				if (m_sda != null)
					m_sda.RemoveNotification(this);
				if (components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		public override void Initialize(FdoCache cache, ICmObject obj, int flid, string fieldName, IPersistenceProvider persistProvider, XCore.Mediator mediator, string displayNameProperty, string displayWs)
		{
			base.Initialize(cache, obj, flid, fieldName, persistProvider, mediator, displayNameProperty, displayWs);
			m_sda = m_cache.MainCacheAccessor;
			m_sda.AddNotification(this);
		}

		/// <summary>
		/// Override method to handle launching of a chooser for selecting lexical entries or senses.
		/// </summary>
		protected override void HandleChooser()
		{
			Debug.Assert(m_flid == (int)LexEntryRef.LexEntryRefTags.kflidComponentLexemes ||
				m_flid == (int)LexEntryRef.LexEntryRefTags.kflidPrimaryLexemes);
			if (m_flid == (int)LexEntryRef.LexEntryRefTags.kflidComponentLexemes)
			{
				using (LinkEntryOrSenseDlg dlg = new LinkEntryOrSenseDlg())
				{
					ILexEntry le = null;
					if (m_obj.ClassID == LexEntry.kclsidLexEntry)
					{
						// filter this entry from the list.
						le = m_obj as ILexEntry;
					}
					else
					{
						// assume the owner is the entry (e.g. owner of LexEntryRef)
						int hvoEntry = m_cache.GetOwnerOfObjectOfClass(m_obj.Hvo, LexEntry.kclsidLexEntry);
						if (hvoEntry != 0)
							le = LexEntry.CreateFromDBObject(m_cache, hvoEntry);
					}
					dlg.SetDlgInfo(m_cache, m_mediator, le);
					String str = ShowHelp.RemoveSpaces(this.Slice.Label);
					dlg.SetHelpTopic("khtpChooseLexicalEntryOrSense-" + str);
					if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
						AddItem(dlg.SelectedID);
				}
			}
			else if (m_flid == (int)LexEntryRef.LexEntryRefTags.kflidPrimaryLexemes)
			{
				string displayWs = "analysis vernacular";
				if (m_configurationNode != null)
				{
					XmlNode node = m_configurationNode.SelectSingleNode("deParams");
					if (node != null)
						displayWs = XmlUtils.GetAttributeValue(node, "ws", "analysis vernacular").ToLower();
				}
				ILexEntryRef ler = m_obj as ILexEntryRef;
				Debug.Assert(ler != null);
				List<int> candidates = new List<int>();
				candidates.AddRange(ler.ComponentLexemesRS.HvoArray);
				ObjectLabelCollection labels = new ObjectLabelCollection(m_cache, candidates,
					m_displayNameProperty, displayWs);
				using (ReallySimpleListChooser chooser = new ReallySimpleListChooser(null,
					labels, "PrimaryLexemes", m_cache, ler.PrimaryLexemesRS.HvoArray, false))

				{
					chooser.SetObjectAndFlid(m_obj.Hvo, m_flid);	// may set TextParamHvo

					chooser.Text = "Choose where to show subentry";

					//chooser.ReplaceTreeView(Mediator, "WordformsBrowseView");

					chooser.InitializeExtras(null,Mediator);
					chooser.AddLink("Add a Component...", ReallySimpleListChooser.LinkType.kDialogLink,
						new AddComponentChooserCommand(m_cache, false, null, m_mediator, m_obj, FindForm()));
					DialogResult res = chooser.ShowDialog();
					if (DialogResult.Cancel == res)
						return;
					if (chooser.ChosenHvos != null)
						SetItems(chooser.ChosenHvos);
				}
			}
		}

		public override void AddItem(int hvoNew)
		{
			CheckDisposed();

			List<int> lexemes = new List<int>();
			ILexEntryRef ler = m_obj as ILexEntryRef;
			if (m_flid == (int)LexEntryRef.LexEntryRefTags.kflidComponentLexemes)
				lexemes.AddRange(ler.ComponentLexemesRS.HvoArray);
			else if (m_flid == (int)LexEntryRef.LexEntryRefTags.kflidPrimaryLexemes)
				lexemes.AddRange(ler.PrimaryLexemesRS.HvoArray);
			// don't add a duplicate items.
			if (!lexemes.Contains(hvoNew))
			{
				lexemes.Add(hvoNew);
				SetItems(lexemes);
			}
		}

		#region Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			components = new System.ComponentModel.Container();
		}
		#endregion

		#region IVwNotifyChange Members

		/// <summary>
		/// If a reference is removed from the ComponentLexeme list, it must also be removed
		/// from the PrimaryLexeme list (if it exists there as well).
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="ivMin"></param>
		/// <param name="cvIns"></param>
		/// <param name="cvDel"></param>
		public void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			if (hvo != m_obj.Hvo ||
				m_obj.ClassID != LexEntryRef.kclsidLexEntryRef ||
				tag != (int)LexEntryRef.LexEntryRefTags.kflidComponentLexemes)
			{
				return;
			}
			ILexEntryRef ler = m_obj as ILexEntryRef;
			FdoReferenceSequence<ICmObject> rgobjComponent = ler.ComponentLexemesRS;
			if (cvDel > cvIns)
			{
				int[] rghvoPrimary = ler.PrimaryLexemesRS.HvoArray;
				for (int i = 0; i < rghvoPrimary.Length; ++i)
				{
					if (!rgobjComponent.Contains(rghvoPrimary[i]))
						ler.PrimaryLexemesRS.Remove(rghvoPrimary[i]);
				}
			}
			else if (cvIns > cvDel && ler.ComponentLexemesRS.Count == 1 && ler.PrimaryLexemesRS.Count == 0)
			{
				ler.PrimaryLexemesRS.Append(ler.ComponentLexemesRS[0]);
			}
		}

		#endregion
	}

	internal class AddComponentChooserCommand : ChooserCommand
	{
		private ILexEntryRef m_ler;
		private Form m_form;

		public AddComponentChooserCommand(FdoCache cache, bool fCloseBeforeExecuting,
			string sLabel, XCore.Mediator mediator, ICmObject obj,
			Form form)
			: base(cache, fCloseBeforeExecuting, sLabel, mediator)
		{
			m_ler = obj as ILexEntryRef;
			m_form = form;
		}

		public override ObjectLabel Execute()
		{
			ObjectLabel result = null;
			if (m_ler != null)
			{
				using (LinkEntryOrSenseDlg dlg = new LinkEntryOrSenseDlg())
				{
					ILexEntry le = null;
					// assume the owner is the entry (e.g. owner of LexEntryRef)
					int hvoEntry = m_cache.GetOwnerOfObjectOfClass(m_ler.Hvo, LexEntry.kclsidLexEntry);
					if (hvoEntry != 0)
						le = LexEntry.CreateFromDBObject(m_cache, hvoEntry);
					dlg.SetDlgInfo(m_cache, m_mediator, le);
					dlg.SetHelpTopic("khtpChooseLexicalEntryOrSense");
					if (dlg.ShowDialog(m_form) == DialogResult.OK)
					{
						int hvo = dlg.SelectedID;
						if (hvo != 0 && !m_ler.ComponentLexemesRS.Contains(hvo))
							m_ler.ComponentLexemesRS.Append(hvo);
					}
				}
			}
			return result;
		}
	}
}
