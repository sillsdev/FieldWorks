using System.Windows.Forms;

namespace SIL.CoreImpl
{
	/// <summary>
	/// Interface for classes wishing to receive sequential messages.
	/// </summary>
	public interface IReceiveSequentialMessages
	{
		/// <summary>
		/// Minimal implementation is base.WndProc(ref m);
		/// </summary>
		/// <param name="m"></param>
		void OriginalWndProc(ref Message m);

		/// <summary>
		/// Minimal implementation is nothing, if you don't override OnPaint.
		/// </summary>
		/// <param name="e"></param>
		void OriginalOnPaint(PaintEventArgs e);

		/// <summary>
		/// Get the actual message sequencer. (This is not used by the sequencer, itself, but is very
		/// useful for helper classes handling controls which may need to interact with the sequencer
		/// if their control has one.)
		/// </summary>
		MessageSequencer Sequencer
		{
			get;
		}
	}
}