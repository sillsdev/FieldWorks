using System;

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary>
	/// Interface of methods used to communicate with IBus.
	/// </summary>
	public interface IIBusCommunicator : IDisposable
	{
		/// <summary>
		/// Returns true if connected to IBus.
		/// </summary>
		bool Connected { get; }

		/// <summary>
		/// Focus in on IBus input context.
		/// </summary>
		void FocusIn();

		/// <summary>
		/// Focus out of IBus input context.
		/// </summary>
		void FocusOut();

		/// <summary>
		/// Sets the cursor location of IBus input context.
		/// </summary>
		void SetCursorLocation(int x, int y, int width, int height);

		/// <summary>
		/// Send a key Event to the ibus current input context.
		/// returns true if key event is handled by ibus.
		/// </summary>
		bool ProcessKeyEvent(uint keyval, uint keycode, uint state);

		/// <summary>
		/// Reset current input context.
		/// </summary>
		void Reset();

		/// <summary>
		/// Create an input context of given name.
		/// </summary>
		void CreateInputContext(string name);

		/// <summary></summary>
		event Action<string> CommitText;

		/// <summary></summary>
		event Action<string, uint, bool> UpdatePreeditText;

		/// <summary></summary>
		event Action HidePreeditText;

		/// <summary></summary>
		event Action<uint, uint, uint> ForwardKeyEvent;
	}
}