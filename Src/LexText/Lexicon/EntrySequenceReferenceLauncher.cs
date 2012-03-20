using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Xml;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.LexText.Controls;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.XWorks.LexEd
{
	public class EntrySequenceReferenceLauncher : VectorReferenceLauncher
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
		}

		/// <summary>
		/// Override method to handle launching of a chooser for selecting lexical entries or senses.
		/// </summary>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="FindForm() returns a reference")]
		protected override void HandleChooser()
		{
			Debug.Assert(m_flid == LexEntryRefTags.kflidComponentLexemes ||
				m_flid == LexEntryRefTags.kflidPrimaryLexemes);
			if (m_flid == LexEntryRefTags.kflidComponentLexemes)
			{
				using (LinkEntryOrSenseDlg dlg = new LinkEntryOrSenseDlg())
				{
					ILexEntry le = null;
					if (m_obj.ClassID == LexEntryTags.kClassId)
					{
						// filter this entry from the list.
						le = m_obj as ILexEntry;
					}
					else
					{
						// assume the owner is the entry (e.g. owner of LexEntryRef)
						le = m_obj.OwnerOfClass<ILexEntry>();
					}
					dlg.SetDlgInfo(m_cache, m_mediator, le);
					String str = ShowHelp.RemoveSpaces(this.Slice.Label);
					dlg.SetHelpTopic("khtpChooseLexicalEntryOrSense-" + str);
					if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
						AddItem(dlg.SelectedObject);
				}
			}
			else if (m_flid == LexEntryRefTags.kflidPrimaryLexemes)
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
				var labels = ObjectLabel.CreateObjectLabels(m_cache, ler.ComponentLexemesRS.Cast<ICmObject>(),
					m_displayNameProperty, displayWs);
				using (ReallySimpleListChooser chooser = new ReallySimpleListChooser(null,
					labels, "PrimaryLexemes", m_cache, ler.PrimaryLexemesRS.Cast<ICmObject>(),
					false, m_mediator.HelpTopicProvider))

				{
					chooser.SetObjectAndFlid(m_obj.Hvo, m_flid);	// may set TextParamHvo
					chooser.Text = LexEdStrings.ksChooseWhereToShowSubentry;
					chooser.SetHelpTopic(Slice.GetChooserHelpTopicID());
					chooser.InitializeExtras(null,Mediator);
					chooser.AddLink(LexEdStrings.ksAddAComponent, ReallySimpleListChooser.LinkType.kDialogLink,
						new AddComponentChooserCommand(m_cache, false, null, m_mediator, m_obj, FindForm()));
					DialogResult res = chooser.ShowDialog();
					if (DialogResult.Cancel == res)
						return;
					if (chooser.ChosenObjects != null)
						SetItems(chooser.ChosenObjects);
				}
			}
		}

		public override void AddItem(ICmObject obj)
		{
			CheckDisposed();

			var lexemes = new HashSet<ICmObject>();
			ILexEntryRef ler = m_obj as ILexEntryRef;
			if (m_flid == LexEntryRefTags.kflidComponentLexemes)
				lexemes.UnionWith(ler.ComponentLexemesRS);
			else if (m_flid == LexEntryRefTags.kflidPrimaryLexemes)
				lexemes.UnionWith(ler.PrimaryLexemesRS);
			// don't add a duplicate items.
			if (!lexemes.Contains(obj))
			{
				lexemes.Add(obj);
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
					le = m_ler.OwnerOfClass<ILexEntry>();
					dlg.SetDlgInfo(m_cache, m_mediator, le);
					dlg.SetHelpTopic("khtpChooseLexicalEntryOrSense");
					if (dlg.ShowDialog(m_form) == DialogResult.OK)
					{
						ICmObject obj = dlg.SelectedObject;
						if (obj != null)
						{
							if (!m_ler.ComponentLexemesRS.Contains(obj))
							{
								UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(
									LexEdStrings.ksUndoCreatingEntry,
									LexEdStrings.ksRedoCreatingEntry,
									Cache.ActionHandlerAccessor,
									() => { m_ler.ComponentLexemesRS.Add(obj); } );
							}
						}
					}
				}
			}
			return result;
		}
	}
}
