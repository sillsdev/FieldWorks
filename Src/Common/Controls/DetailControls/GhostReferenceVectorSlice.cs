using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Framework.DetailControls.Resources;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.Utils;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// This class is used (e.g., in the Info tab of Texts/Words) where a Reference Vector Slice would normally appear,
	/// except that the object that has the property does not yet exist. In fact, we do not want to create the object
	/// until the user runs the chooser and clicks OK.
	/// </summary>
	public class GhostReferenceVectorSlice : FieldSlice
	{
		public GhostReferenceVectorSlice(FdoCache cache, ICmObject obj, XmlNode configNode)
			: base(new GhostReferenceVectorLauncher(), cache, obj, GetFieldId(cache, configNode))
		{
		}

		protected override void UpdateDisplayFromDatabase()
		{
		}

		private static int GetFieldId(FdoCache cache, XmlNode configurationParameters)
		{
			return cache.MetaDataCacheAccessor.GetFieldId(XmlUtils.GetManditoryAttributeValue(configurationParameters, "ghostClass"),
				XmlUtils.GetManditoryAttributeValue(configurationParameters, "ghostField"), true);
		}

		public override void FinishInit()
		{
			base.FinishInit();

			((GhostReferenceVectorLauncher)Control).InitializeFlexComponent(PropertyTable, Publisher, Subscriber);
			((GhostReferenceVectorLauncher)Control).Initialize(m_cache, m_obj, m_flid, m_fieldName, m_persistenceProvider, DisplayNameProperty, BestWsName);
		}

		// Copied from ReferenceVectorSlice for initializing GhostReferenceVectorLauncher...may not be used.
		protected string BestWsName
		{
			get
			{
				XmlNode parameters = ConfigurationNode.SelectSingleNode("deParams");
				if (parameters == null)
					return "analysis";

				return XmlUtils.GetOptionalAttributeValue(parameters, "ws", "analysis");
			}
		}

		// Copied from ReferenceVectorSlice for initializing GhostReferenceVectorLauncher...may not be used.
		protected string DisplayNameProperty
		{
			get
			{
				XmlNode parameters = ConfigurationNode.SelectSingleNode("deParams");
				if (parameters == null)
					return "";

				return XmlUtils.GetOptionalAttributeValue(parameters, "displayProperty", "");
			}
		}
	}

	class GhostReferenceVectorLauncher: ButtonLauncher
	{
		// We want to emulate what ReferenceLauncher does, but without the object being created
		// until the user clicks OK in the simple list chooser.
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="FindForm() returns a reference")]
		protected override void HandleChooser()
		{
			// YAGNI: may eventually need to make configurable how it comes up with the list of candidates.
			// Currently this is used only for properties of a ghost notebook record.
			var candidateList = (ICmPossibilityList) ReferenceTargetServices.RnGenericRecReferenceTargetOwner(m_cache, m_flid);
			var candidates = candidateList == null ? null : candidateList.PossibilitiesOS;
			// YAGNI: see ReferenceLauncher implementation of this method for a possible approach to
			// making the choice of writing system configurable.
			var labels = ObjectLabel.CreateObjectLabels(m_cache, candidates,
				m_displayNameProperty, "analysis vernacular");
			var chooser = new SimpleListChooser(m_persistProvider,
				labels,
				m_fieldName,
				m_cache,
				new ICmObject[0],
				PropertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider"));
			chooser.SetHelpTopic(Slice.GetChooserHelpTopicID());

			chooser.SetObjectAndFlid(0, m_flid);
			if (Slice.ConfigurationNode != null)
			{
				// Review JohnT: can any of this be made relevant without an object?
				//    // Handle the default case ("owner") for text parameters.

				//    // This (old approach) works only if
				//    // all of the list items are owned by the same object as the first one in the
				//    // list.  (Later elements can be owned by elements owned by that first owner,
				//    // if you know what I mean.)
				//    //if (candidates.Count != 0)
				//    //    chooser.TextParamHvo = m_cache.GetOwnerOfObject((int)candidates[0]);
				//    // JohnT: this approach depends on a new FDO method.
				//    ICmObject referenceTargetOwner = m_obj.ReferenceTargetOwner(m_flid);
				//    if (referenceTargetOwner != null)
				//        chooser.TextParamHvo = referenceTargetOwner.Hvo;
				//    chooser.SetHelpTopic(Slice.GetChooserHelpTopicID());
				chooser.InitializeExtras(Slice.ConfigurationNode, PropertyTable);
			}
			var res = chooser.ShowDialog(FindForm());
			if (DialogResult.Cancel == res)
				return;

			if (chooser.HandleAnyJump())
				return;

			if (chooser.ChosenObjects != null && chooser.ChosenObjects.Any())
			{
				UndoableUnitOfWorkHelper.Do(string.Format(DetailControlsStrings.ksUndoSet, m_fieldName),
				string.Format(DetailControlsStrings.ksRedoSet, m_fieldName), m_obj,
					() =>
					{
						// YAGNI: creating the real object may eventually need to be configurable,
						// perhaps by indicating in the configuration node what class of object to create
						// and so forth, or perhaps by just putting a "doWhat" attribute on the configuration node
						// and making a switch here to control what is done. For now this slice is only used
						// in one situation, where we need to create a notebook record, associate the current object
						// with it, and add the values to it.
						((IText)m_obj).AssociateWithNotebook(false);
						IRnGenericRec notebookRec;
						DataTree.NotebookRecordRefersToThisText(m_obj as IText, out notebookRec);
						var recHvo = notebookRec.Hvo;
						var values = (from obj in chooser.ChosenObjects select obj.Hvo).ToArray();
						var listFlid = m_flid;
						if (m_flid == RnGenericRecTags.kflidParticipants)
						{
							var defaultRoledParticipant = notebookRec.MakeDefaultRoledParticipant();
							recHvo = defaultRoledParticipant.Hvo;
							listFlid = RnRoledParticTags.kflidParticipants;
						}
						m_cache.DomainDataByFlid.Replace(recHvo, listFlid, 0, 0, values, values.Length);
						// We don't do anything about updating the display because creating the real object
						// will typically destroy this slice altogether and replace it with a real one.
					});
				// Structure has changed drastically, start over.
				var index = Slice.IndexInContainer;
				var dataTree = Slice.ContainingDataTree;
				dataTree.RefreshList(false); // Slice will be destroyed!!
				if (index <= dataTree.Slices.Count - 1)
					dataTree.CurrentSlice = dataTree.FieldAt(index);
			}
		}
	}
}
