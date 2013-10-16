namespace SIL.HermitCrab
{
	/// <summary>
	/// This interface represents an output format for HC objects.
	/// </summary>
	public interface IOutput
	{
		TraceManager TraceManager { get; }

		/// <summary>
		/// Morphes the specified word and writes the results to the underlying stream.
		/// </summary>
		/// <param name="morpher">The morpher.</param>
		/// <param name="word">The word.</param>
		/// <param name="prettyPrint">if set to <c>true</c> the results will be formatted for human readability.</param>
		/// <param name="printTraceInputs">if set to <c>true</c> the inputs to rules in the trace will be written out.</param>
		void MorphAndLookupWord(Morpher morpher, string word, bool prettyPrint, bool printTraceInputs);

		/// <summary>
		/// Writes the specified word synthesis record.
		/// </summary>
		/// <param name="ws">The word synthesis record.</param>
		/// <param name="prettyPrint">if set to <c>true</c> the results will be formatted for human readability.</param>
		void Write(WordSynthesis ws, bool prettyPrint);

		/// <summary>
		/// Writes the specified load exception.
		/// </summary>
		/// <param name="le">The load exception.</param>
		void Write(LoadException le);

		/// <summary>
		/// Writes the specified morph exception.
		/// </summary>
		/// <param name="me">The morph exception.</param>
		void Write(MorphException me);

		/// <summary>
		/// Flushes the current buffer to the underlying stream.
		/// </summary>
		void Flush();


		/// <summary>
		/// Closes the underlying stream.
		/// </summary>
		void Close();
	}
}
