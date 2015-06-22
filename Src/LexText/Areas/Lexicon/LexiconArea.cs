// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using SIL.FieldWorks.Common.Framework;
using XCore;

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
			//MessageBoxUtils.Show(Form.ActiveForm, "Implement lexicon area Deactivate method.", "Need to implement", MessageBoxButtons.OK);
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
			//MessageBoxUtils.Show(Form.ActiveForm, "Implement lexicon area Activate method.", "Need to implement", MessageBoxButtons.OK);
		}

		/// <summary>
		/// Do whatever might be needed to get ready for a refresh.
		/// </summary>
		/// <remarks>
		/// One might expect this method to pass this call into the area's current tool.
		/// </remarks>
		public void PrepareToRefresh()
		{
			//MessageBoxUtils.Show(Form.ActiveForm, "Implement lexicon area PrepareToRefresh method.", "Need to implement", MessageBoxButtons.OK);
		}

		/// <summary>
		/// Finish the refresh.
		/// </summary>
		/// <remarks>
		/// One might expect this method to pass this call into the area's current tool.
		/// </remarks>
		public void FinishRefresh()
		{
			//MessageBoxUtils.Show(Form.ActiveForm, "Implement lexicon area FinishRefresh method.", "Need to implement", MessageBoxButtons.OK);
		}

		/// <summary>
		/// The properties are about to be saved, so make sure they are all current.
		/// Add new ones, as needed.
		/// </summary>
		/// <param name="propertyTable">The table that is about to be persisted.</param>
		public void EnsurePropertiesAreCurrent(PropertyTable propertyTable)
		{
			//MessageBoxUtils.Show(Form.ActiveForm, "Implement lexicon area EnsurePropertiesAreCurrent method.", "Need to implement", MessageBoxButtons.OK);
		}

		#endregion
	}
}
