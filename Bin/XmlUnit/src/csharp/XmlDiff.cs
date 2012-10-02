namespace XmlUnit {
	using System;
	using System.IO;
	using System.Xml;
	using System.Xml.Schema;

	public class XmlDiff {
		private readonly XmlReader _controlReader;
		private readonly XmlReader _testReader;
		private readonly DiffConfiguration _diffConfiguration;
		private DiffResult _diffResult;

		public XmlDiff(XmlInput control, XmlInput test,
					   DiffConfiguration diffConfiguration) {
			_diffConfiguration =  diffConfiguration;
			_controlReader = CreateXmlReader(control);
			if (control.Equals(test)) {
				_testReader = _controlReader;
			} else {
				_testReader = CreateXmlReader(test);
			}
		}

		public XmlDiff(XmlInput control, XmlInput test)
			: this(control, test, new DiffConfiguration()) {
		}

		public XmlDiff(TextReader control, TextReader test)
			: this(new XmlInput(control), new XmlInput(test)) {
		}

		public XmlDiff(string control, string test)
			: this(new XmlInput(control), new XmlInput(test)) {
		}

		private XmlReader CreateXmlReader(XmlInput forInput) {
			XmlReader xmlReader = forInput.CreateXmlReader();

			if (xmlReader is XmlTextReader) {
				((XmlTextReader) xmlReader ).WhitespaceHandling = _diffConfiguration.WhitespaceHandling;
			}

			if (_diffConfiguration.UseValidatingParser) {
				XmlValidatingReader validatingReader = new XmlValidatingReader(xmlReader);
				return validatingReader;
			}

			return xmlReader;
		}

		public DiffResult Compare() {
			if (_diffResult == null) {
				_diffResult = new DiffResult();
				if (!_controlReader.Equals(_testReader)) {
					Compare(_diffResult);
				}
			}
			return _diffResult;
		}

		private void Compare(DiffResult result) {
			bool controlRead, testRead;
			try {
				do {
					controlRead = _controlReader.Read();
					testRead = _testReader.Read();
					Compare(result, ref controlRead, ref testRead);
				} while (controlRead && testRead) ;
			} catch (FlowControlException e) {
				Console.Out.WriteLine(e.Message);
			}
		}

		private void Compare(DiffResult result, ref bool controlRead, ref bool testRead) {
			if (controlRead) {
				if(testRead) {
					CompareNodes(result);
					CheckEmptyOrAtEndElement(result, ref controlRead, ref testRead);
				} else {
					DifferenceFound(DifferenceType.CHILD_NODELIST_LENGTH_ID, result);
				}
			}
		}

		private void CompareNodes(DiffResult result) {
			XmlNodeType controlNodeType = _controlReader.NodeType;
			XmlNodeType testNodeType = _testReader.NodeType;
			if (!controlNodeType.Equals(testNodeType)) {
				CheckNodeTypes(controlNodeType, testNodeType, result);
			} else if (controlNodeType == XmlNodeType.Element) {
				CompareElements(result);
			} else if (controlNodeType == XmlNodeType.Text) {
				CompareText(result);
			}
		}

		private void CheckNodeTypes(XmlNodeType controlNodeType, XmlNodeType testNodeType, DiffResult result) {
			XmlReader readerToAdvance = null;
			if (controlNodeType.Equals(XmlNodeType.XmlDeclaration)) {
				readerToAdvance = _controlReader;
			} else if (testNodeType.Equals(XmlNodeType.XmlDeclaration)) {
				readerToAdvance = _testReader;
			}

			if (readerToAdvance != null) {
				DifferenceFound(DifferenceType.HAS_XML_DECLARATION_PREFIX_ID,
								controlNodeType, testNodeType, result);
				readerToAdvance.Read();
				CompareNodes(result);
			} else {
				DifferenceFound(DifferenceType.NODE_TYPE_ID, controlNodeType,
								testNodeType, result);
			}
		}

		private void CompareElements(DiffResult result) {
			string controlTagName = _controlReader.Name;
			string testTagName = _testReader.Name;
			if (!String.Equals(controlTagName, testTagName)) {
				DifferenceFound(DifferenceType.ELEMENT_TAG_NAME_ID, result);
			} else {
				int controlAttributeCount = _controlReader.AttributeCount;
				int testAttributeCount = _testReader.AttributeCount;
				if (controlAttributeCount != testAttributeCount) {
					DifferenceFound(DifferenceType.ELEMENT_NUM_ATTRIBUTES_ID, result);
				} else {
					CompareAttributes(result, controlAttributeCount);
				}
			}
		}

		private void CompareAttributes(DiffResult result, int controlAttributeCount) {
			string controlAttrValue, controlAttrName;
			string testAttrValue, testAttrName;

			_controlReader.MoveToFirstAttribute();
			_testReader.MoveToFirstAttribute();
			for (int i=0; i < controlAttributeCount; ++i) {

				controlAttrName = _controlReader.Name;
				testAttrName = _testReader.Name;

				controlAttrValue = _controlReader.Value;
				testAttrValue = _testReader.Value;

				if (!String.Equals(controlAttrName, testAttrName)) {
					DifferenceFound(DifferenceType.ATTR_SEQUENCE_ID, result);

					if (!_testReader.MoveToAttribute(controlAttrName)) {
						DifferenceFound(DifferenceType.ATTR_NAME_NOT_FOUND_ID, result);
					}
					testAttrValue = _testReader.Value;
				}

				if (!String.Equals(controlAttrValue, testAttrValue)) {
					DifferenceFound(DifferenceType.ATTR_VALUE_ID, result);
				}

				_controlReader.MoveToNextAttribute();
				_testReader.MoveToNextAttribute();
			}
		}

		private void CompareText(DiffResult result) {
			string controlText = _controlReader.Value;
			string testText = _testReader.Value;
			if (!String.Equals(controlText, testText)) {
				DifferenceFound(DifferenceType.TEXT_VALUE_ID, result);
			}
		}

		private void DifferenceFound(DifferenceType differenceType, DiffResult result) {
			DifferenceFound(new Difference(differenceType), result);
		}

		private void DifferenceFound(Difference difference, DiffResult result) {
			result.DifferenceFound(this, difference);
			if (!ContinueComparison(difference)) {
				throw new FlowControlException(difference);
			}
		}

		private void DifferenceFound(DifferenceType differenceType,
									 XmlNodeType controlNodeType,
									 XmlNodeType testNodeType,
									 DiffResult result) {
			DifferenceFound(new Difference(differenceType, controlNodeType, testNodeType),
							result);
		}

		private bool ContinueComparison(Difference afterDifference) {
			return !afterDifference.MajorDifference;
		}

		private void CheckEmptyOrAtEndElement(DiffResult result,
											  ref bool controlRead, ref bool testRead) {
			if (_controlReader.IsEmptyElement) {
				if (!_testReader.IsEmptyElement) {
					CheckEndElement(_testReader, ref testRead, result);
				}
			} else {
				if (_testReader.IsEmptyElement) {
					CheckEndElement(_controlReader, ref controlRead, result);
				}
			}
		}

		private void CheckEndElement(XmlReader reader, ref bool readResult, DiffResult result) {
			readResult = reader.Read();
			if (!readResult || reader.NodeType != XmlNodeType.EndElement) {
				DifferenceFound(DifferenceType.CHILD_NODELIST_LENGTH_ID, result);
			}
		}

		private class FlowControlException : ApplicationException {
			public FlowControlException(Difference cause) : base(cause.ToString()) {
			}
		}

		public string OptionalDescription {
			get {
				return _diffConfiguration.Description;
			}
		}
	}
}
