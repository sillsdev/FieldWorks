// Copyright (c) 2004-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Windows.Forms;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// This interface specifies a component that allows the user to enter specifications
	/// on how to change a column, and displays the current choice.
	/// </summary>
	public interface IBulkEditSpecControl
	{
		/// <summary>Get/Set the property table.</summary>
		IPropertyTable PropertyTable { get; set; }

		/// <summary>Retrieve the control that does the work.</summary>
		Control Control { get; }

		/// <summary>Get or set the cache. Client promises to set this immediately after creation.</summary>
		LcmCache Cache { get; set; }

		/// <summary>
		/// The decorator cache that understands the special properties used to control the checkbox and preview.
		/// Client promises to set this immediately after creation.
		/// </summary>
		XMLViewsDataCache DataAccess { get; set; }

		/// <summary>
		/// Set a stylesheet. Should be done soon after creation, before reading Control.
		/// </summary>
		IVwStylesheet Stylesheet { set; }

		/// <summary>Invoked when the command is to be executed. The argument contains an array of
		/// the HVOs of the items to which the change should be done (those visible and checked).</summary>
		void DoIt(IEnumerable<int> itemsToChange, ProgressState state);

		/// <summary>
		/// This is called when the preview button is clicked. The control is passed
		/// the list of currently active (filtered and checked) items. It should cache
		/// tagEnabled to zero for any objects that can't be
		/// modified. For ones that can, it should set the string property tagMadeUpFieldIdentifier
		/// to the value to show in the 'modified' fields.
		/// </summary>
		void FakeDoit(IEnumerable<int> itemsToChange, int tagMadeUpFieldIdentifier, int tagEnabled, ProgressState state);

		/// <summary>
		/// True if the editor can set a value that will make the field 'clear'.
		/// </summary>
		bool CanClearField { get; }

		/// <summary>
		/// Select a value that will make the field 'clear' (used by BE Delete tab).
		/// </summary>
		void SetClearField();

		/// <summary>
		/// the field paths starting from the RootObject to the value/object that we're bulkEditing
		/// </summary>
		List<int> FieldPath { get; }

		/// <summary>
		/// Returns the Suggest button if our target is Semantic Domains, otherwise null.
		/// </summary>
		Button SuggestButton { get; }

		/// <summary />
		event FwSelectionChangedEventHandler ValueChanged;

		/// <summary>
		/// Tells SemanticDomainChooserBEditControl to make suggestions and then call FakeDoIt
		/// </summary>
		void MakeSuggestions(IEnumerable<int> itemsToChange, int tagMadeUpFieldIdentifier, int tagEnabled, ProgressState state);
	}
}