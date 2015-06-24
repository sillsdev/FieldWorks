using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SIL.FieldWorks.FwCoreDlgControls;

namespace SIL.FieldWorks.XWorks
{
	public interface IDictionaryDetailsView : IDisposable
	{
		/// <summary>
		/// Tell the controller that the Style selection was changed.
		/// </summary>
		event EventHandler StyleSelectionChanged;

		/// <summary>
		/// Tell the controller the Style button was clicked.
		/// </summary>
		event EventHandler StyleButtonClick;

		/// <summary>
		/// Tell the controller that the Before Text was changed.
		/// </summary>
		event EventHandler BeforeTextChanged;

		/// <summary>
		/// Tell the controller that the Between Text was changed.
		/// </summary>
		event EventHandler BetweenTextChanged;

		/// <summary>
		/// Tell the controller that the After Text was changed.
		/// </summary>
		event EventHandler AfterTextChanged;

		string BeforeText { get; set; }

		string BetweenText { get; set; }

		string AfterText { get; set; }

		string Style { get; }

		bool StylesVisible { set; }

		bool SurroundingCharsVisible { set; }

		UserControl OptionsView { set; }

		bool Visible { get; set; }

		Control TopLevelControl { get; }

		bool IsDisposed { get; }

		void SetStyles(List<StyleComboItem> styles, string selectedStyle, bool usingParaStyles);

		void SuspendLayout();

		void ResumeLayout();
	}
}
