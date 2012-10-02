namespace XmlUnit {
	using System.Xml;

	public class DiffConfiguration {
		public static readonly WhitespaceHandling DEFAULT_WHITESPACE_HANDLING = WhitespaceHandling.All;
		public static readonly string DEFAULT_DESCRIPTION = "XmlDiff";
		public static readonly bool DEFAULT_USE_VALIDATING_PARSER = true;

		private readonly string _description;
		private readonly bool _useValidatingParser;
		private readonly WhitespaceHandling _whitespaceHandling;

		public DiffConfiguration(string description,
								 bool useValidatingParser,
								 WhitespaceHandling whitespaceHandling) {
			_description = description;
			_useValidatingParser = useValidatingParser;
			_whitespaceHandling = whitespaceHandling;
		}

		public DiffConfiguration(string description,
								 WhitespaceHandling whitespaceHandling)
		: this (description,
				DEFAULT_USE_VALIDATING_PARSER,
				whitespaceHandling) {}

		public DiffConfiguration(WhitespaceHandling whitespaceHandling)
		: this(DEFAULT_DESCRIPTION,
			   DEFAULT_USE_VALIDATING_PARSER,
			   whitespaceHandling) {}

		public DiffConfiguration(string description)
		: this(description,
			   DEFAULT_USE_VALIDATING_PARSER,
			   DEFAULT_WHITESPACE_HANDLING) {}

		public DiffConfiguration(bool useValidatingParser)
		: this(DEFAULT_DESCRIPTION,
			   useValidatingParser,
			   DEFAULT_WHITESPACE_HANDLING) {
		}

		public DiffConfiguration()
		: this(DEFAULT_DESCRIPTION,
			   DEFAULT_USE_VALIDATING_PARSER,
			   DEFAULT_WHITESPACE_HANDLING) {}

		public string Description {
			get {
				return _description;
			}
		}

		public bool UseValidatingParser {
			get {
				return _useValidatingParser;
			}
		}

		public WhitespaceHandling WhitespaceHandling {
			get {
				return _whitespaceHandling;
			}
		}
	}
}
