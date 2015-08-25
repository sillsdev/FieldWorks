// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Drawing;
using System.Windows.Forms;
using SIL.CoreImpl;
using SIL.FieldWorks.Resources;

namespace LanguageExplorer.Areas.Grammar.Tools.AdhocCoprohibEdit
{
	/// <summary>
	/// ITool implementation for the "AdhocCoprohibitionRuleEdit" tool in the "grammar" area.
	/// </summary>
	/// <remarks>
	/// This is the comment from the data model file about an "AdhocCoprohibition" (actual class "MoAdhocProhib":
	///
	/// This abstract class is intended to capture co-occurrence restrictions between morphemes or allomorphs which cannot be captured
	/// using morphosyntactic or phonological restrictions. Linguistically speaking, this is perhaps as bad a kludge as you can imagine.
	/// We allow it here for reasons of stealth-to-wealth work, with the understanding that the program will twist the user's arm into
	/// getting rid of these later on (perhaps by flagging each of these constraints with a Warning). The technique is borrowed from
	/// AMPLE's ad hoc pairs. Note however that, Aronoff (1976: 53) gives as an example of a negative constraint the fact that the
	/// suffix -ness does not attach to adjectives of the form X-ate, X-ant, or X-ent. So maybe this isn't such a kludge after all.
	/// On the other hand, Aronoff's examples are largely statistical generalizations, that is, tendencies - as opposed to hard constraints.
	///
	/// It may be that we should also have positive cooccurrence constraints. Aronoff (1976: 63) lists a number of "forms of the base"
	/// which are compatible with the English prefix un-, among them X-en (where -en is the past participle suffix), X-ing, and X-able,
	/// which would be examples of positive constraints. However, un- also attaches to a good many monomorphemic stems (roots, e.g. unhappy),
	/// so it may be that this is not a real generalization.
	///
	/// In addition to the attributes below, there should probably be an attribute to point to analyses which are ruled out by these constraints.
	/// These could be either grammatical words for which the parser would generate an incorrect analysis if it were not for this constraint,
	/// or ungrammatical words which the user has supplied, and which would be parsed if not for this constraint. It may even be desirable to
	/// allow individual constraints to be turned off in the parsing of such examples, in order to verify that the constraint works, and that
	/// it is (still) needed. However, the need for such an attr is probably more general than this class; see my email of 18 Jan 2000.
	/// </remarks>
	internal sealed class AdhocCoprohibitionRuleEditTool : ITool
	{
		#region Implementation of IPropertyTableProvider

		/// <summary>
		/// Placement in the IPropertyTableProvider interface lets FwApp call IPropertyTable.DoStuff.
		/// </summary>
		public IPropertyTable PropertyTable { get; private set; }

		#endregion

		#region Implementation of IPublisherProvider

		/// <summary>
		/// Get the IPublisher.
		/// </summary>
		public IPublisher Publisher { get; private set; }

		#endregion

		#region Implementation of ISubscriberProvider

		/// <summary>
		/// Get the ISubscriber.
		/// </summary>
		public ISubscriber Subscriber { get; private set; }

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="propertyTable">Interface to a property table.</param>
		/// <param name="publisher">Interface to the publisher.</param>
		/// <param name="subscriber">Interface to the subscriber.</param>
		public void InitializeFlexComponent(IPropertyTable propertyTable, IPublisher publisher, ISubscriber subscriber)
		{
			FlexComponentCheckingService.CheckInitializationValues(propertyTable, publisher, subscriber, PropertyTable, Publisher, Subscriber);

			PropertyTable = propertyTable;
			Publisher = publisher;
			Subscriber = subscriber;
		}

		#endregion

		#region Implementation of IMajorFlexComponent

		/// <summary>
		/// Deactivate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the outgoing component, when the user switches to a component.
		/// </remarks>
		public void Deactivate(ICollapsingSplitContainer mainCollapsingSplitContainer, MenuStrip menuStrip, ToolStripContainer toolStripContainer,
			StatusBar statusbar)
		{
		}

		/// <summary>
		/// Activate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the component that is becoming active.
		/// </remarks>
		public void Activate(ICollapsingSplitContainer mainCollapsingSplitContainer, MenuStrip menuStrip, ToolStripContainer toolStripContainer,
			StatusBar statusbar)
		{
		}

		/// <summary>
		/// Do whatever might be needed to get ready for a refresh.
		/// </summary>
		public void PrepareToRefresh()
		{
		}

		/// <summary>
		/// Finish the refresh.
		/// </summary>
		public void FinishRefresh()
		{
		}

		/// <summary>
		/// The properties are about to be saved, so make sure they are all current.
		/// Add new ones, as needed.
		/// </summary>
		public void EnsurePropertiesAreCurrent()
		{
		}

		#endregion

		#region Implementation of IMajorFlexUiComponent

		/// <summary>
		/// Get the internal name of the component.
		/// </summary>
		/// <remarks>NB: This is the machine friendly name, not the user friendly name.</remarks>
		public string MachineName
		{
			get { return "AdhocCoprohibitionRuleEdit"; }
		}

		/// <summary>
		/// User-visible localizable component name.
		/// </summary>
		public string UiName
		{
			get { return "Ad hoc Rules"; }
		}

		#endregion

		#region Implementation of ITool

		/// <summary>
		/// Get the area machine name the tool is for.
		/// </summary>
		public string AreaMachineName
		{
			get { return "grammar"; }
		}

		/// <summary>
		/// Get the image for the area.
		/// </summary>
		public Image Icon
		{
			get
			{
				var image = Images.SideBySideView;
				image.MakeTransparent(Color.Magenta);
				return image;
			}
		}

		#endregion
	}
}
