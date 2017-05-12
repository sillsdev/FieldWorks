BEGIN { XmlSummary = "	/// <summary/>";
}
/using System;using Tools;/ {print "using System.Text;";}
/public enum SyntaxErrType/ { print XmlSummary; }
/unknown,$/ { print XmlSummary; }
/missingOpeningParen,$/ { print XmlSummary; }
/missingClosingParen,$/ { print XmlSummary; }
/missingOpeningSquareBracket,$/ { print XmlSummary; }
/missingClosingSquareBracket,$/ { print XmlSummary; }
/public void ResetNaturalClasses\(string\[\] saSegments\)/ { print XmlSummary; }
/public void ResetSegments\(string\[\] saSegments\)/ { print XmlSummary; }
/public void ResetSortedList\(ref System.Collections.SortedList list, string\[\] saContents\)/ { print XmlSummary; }
/public bool IsValidClass\(string sClass\)/ { print XmlSummary; }
/public bool IsValidSegment\(string sSegment, ref int iPos\)/ { print XmlSummary; }
/public void CreateErrorMessage\(string sType, string sItem, int pos\)/ { print XmlSummary; }
/public void ThrowError\(int iPos\)/ { print XmlSummary; }
/public string Input/ { print XmlSummary; }
/public bool Success/ { print XmlSummary; }
/public string ErrorMessage/ { print XmlSummary; }
/public int Position/ { print XmlSummary; }
/public SyntaxErrType SyntaxErrorType/ { print XmlSummary; }

/./ { print $0}
