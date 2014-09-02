using System.Xml.Linq;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// Base class common to all parser trace processing
	/// </summary>
	public abstract class ParserTrace
	{
		private static ParserTraceUITransform s_traceTransform;
		protected static ParserTraceUITransform ParseTransform
		{
			get
			{
				if (s_traceTransform == null)
					s_traceTransform = new ParserTraceUITransform("FormatXAmpleParse");
				return s_traceTransform;
			}
		}

		public WordGrammarDebugger WordGrammarDebugger { get; protected set; }

		/// <summary>
		/// Create an HTML page of the results
		/// </summary>
		/// <param name="result">XML of the parse trace output</param>
		/// <param name="isTrace"></param>
		/// <returns>URL of the resulting HTML page</returns>
		public abstract string CreateResultPage(XDocument result, bool isTrace);
	}
}
