// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using SIL.CoreImpl;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.FDO;

namespace LexiconAreaPlugin
{
	/// <summary>
	/// IArea implementation for the main, and thus only required, Area: "lexicon".
	/// </summary>
	public sealed class LexiconArea : IArea
	{
		#region Implementation of IMajorFlexUiComponent

		/// <summary>
		/// Get the internal name of the component.
		/// </summary>
		/// <remarks>NB: This is the machine friendly name, not the user friendly name.</remarks>
		public string Name { get { return "lexicon"; } }

		/// <summary>
		/// Deactivate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the outgoing Area, when the user switches to a new Area.
		/// </remarks>
		public void Deactivate()
		{
#if RANDYTODO
			// Implement and call Deactivate() on current tool in area.
			//MessageBoxUtils.Show(Form.ActiveForm, "Implement lexicon area Deactivate method.", "Need to implement", MessageBoxButtons.OK);
#endif
		}

		/// <summary>
		/// Activate the area.
		/// </summary>
		/// <remarks>
		/// This is called on the area that is becoming active.
		///
		/// One of its tools will become active.
		/// </remarks>
		public void Activate()
		{
#if RANDYTODO
			// Implement and call Activate() on current/default tool in area.
			//MessageBoxUtils.Show(Form.ActiveForm, "Implement lexicon area Activate method.", "Need to implement", MessageBoxButtons.OK);
#endif
		}

		/// <summary>
		/// Do whatever might be needed to get ready for a refresh.
		/// </summary>
		/// <remarks>
		/// One might expect this method to pass this call into the area's current tool.
		/// </remarks>
		public void PrepareToRefresh()
		{
#if RANDYTODO
			// Implement and call PrepareToRefresh() on current tool in area.
			//MessageBoxUtils.Show(Form.ActiveForm, "Implement lexicon area PrepareToRefresh method.", "Need to implement", MessageBoxButtons.OK);
#endif
		}

		/// <summary>
		/// Finish the refresh.
		/// </summary>
		/// <remarks>
		/// One might expect this method to pass this call into the area's current tool.
		/// </remarks>
		public void FinishRefresh()
		{
#if RANDYTODO
			// Implement and call FinishRefresh() on current tool in area.
			//MessageBoxUtils.Show(Form.ActiveForm, "Implement lexicon area FinishRefresh method.", "Need to implement", MessageBoxButtons.OK);
#endif
		}

		/// <summary>
		/// The properties are about to be saved, so make sure they are all current.
		/// Add new ones, as needed.
		/// </summary>
		/// <param name="fdoServiceLocator">The main system service locator.</param>
		/// <param name="propertyTable">The table that is about to be persisted.</param>
		public void EnsurePropertiesAreCurrent(IFdoServiceLocator fdoServiceLocator, PropertyTable propertyTable)
		{
#if RANDYTODO
			// Implement and call EnsurePropertiesAreCurrent() on current tool in area.
			//MessageBoxUtils.Show(Form.ActiveForm, "Implement lexicon area EnsurePropertiesAreCurrent method.", "Need to implement", MessageBoxButtons.OK);
#endif
		}

		#endregion
	}
}
