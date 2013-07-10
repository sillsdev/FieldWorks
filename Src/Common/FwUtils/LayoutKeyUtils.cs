using System.Text.RegularExpressions;
using System.Xml;
using Palaso.Xml;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Class for utilities used to determine/search/build keys for layout configurations.
	/// Used by Inventory, LayoutCache, LayoutMerger and XmlDocConfigureDlg
	/// </summary>
	public class LayoutKeyUtils
	{
		/// <summary>
		/// This marks the beginning of a tag added to layout names (and param values) when a
		/// node in the tree is copied and has subnodes (the user duplicated part of a view).
		/// This tag will be a suffix on the duplicated node's 'param' attribute and any descendant
		/// layout's 'name' attribute (and their part refs' 'param' attributes).
		/// </summary>
		public const char kcMarkNodeCopy = '%';
		/// <summary>
		/// This marks the beginning of a tag added to layout names (and param values) when an
		/// entire top-level layout type is copied (i.e. this suffix is added when a user makes
		/// a named copy of an entire layout; e.g. 'My Stem-based Dictionary').
		/// </summary>
		public const char kcMarkLayoutCopy = '#';

		private const string NameAttr = "name";
		private const string LabelAttr = "label";
		private const string ParamAttr = "param";
		private const string LabelRegExString = @" \(\d\)";

		/// <summary>
		/// Look for signs of user named view or duplicated node in this user config key's name attribute.
		/// If found, return the extra part suffixed to the name as well as a new key array
		/// with the original (unmodified) name that should match the newMaster.
		/// </summary>
		/// <param name="keyAttributes"></param>
		/// <param name="keyVals">oldConfigured key values with (probably) suffixed material on the name</param>
		/// <param name="stdKeyVals">key values that should match the newMaster version</param>
		/// <returns>The extra part of the layout name after the standard name that is
		/// due to either (1) the user copying an entire view, or (2) the user duplicating an element of a view.</returns>
		public static string GetSuffixedPartOfNamedViewOrDuplicateNode(string[] keyAttributes, string[] keyVals,
			out string[] stdKeyVals)
		{
			stdKeyVals = keyVals.Clone() as string[];
			if (keyAttributes.Length > 2 && keyAttributes[2] == NameAttr && stdKeyVals.Length > 2)
			{
				var userModifiedName = stdKeyVals[2];
				var index = userModifiedName.IndexOfAny(new[] { kcMarkLayoutCopy, kcMarkNodeCopy });
				if (index > 0)
				{
					stdKeyVals[2] = userModifiedName.Substring(0, index);
					return userModifiedName.Substring(index);
				}
			}
			return string.Empty;
		}

		/// <summary>
		/// Look at the part ref label for a possible duplicated node suffix to put on the new label.
		/// Will be of the format "(x)" where x is a digit, except there could be more than one
		/// separated by spaces.
		/// </summary>
		/// <param name="partRefNode"></param>
		/// <returns></returns>
		public static string GetPossibleLabelSuffix(XmlNode partRefNode)
		{
			var label = partRefNode.GetOptionalStringAttribute(LabelAttr, string.Empty);
			if (string.IsNullOrEmpty(label))
				return string.Empty;
			var regexp = new Regex(LabelRegExString);
			var match = regexp.Match(label);
			// if there's a match, we want everything after the index
			return match.Success ? label.Substring(match.Index) : string.Empty;
		}

		/// <summary>
		/// Look at the part ref param attribute for a possible duplicated node suffix to copy to
		/// the new param attribute.
		/// Will be of the format "%0x" where x is a digit.
		/// </summary>
		/// <param name="partRefNode"></param>
		/// <returns></returns>
		public static string GetPossibleParamSuffix(XmlNode partRefNode)
		{
			var param = partRefNode.GetOptionalStringAttribute(ParamAttr, string.Empty);
			if (string.IsNullOrEmpty(param))
				return string.Empty;
			var index = param.IndexOf(kcMarkNodeCopy);
			if (index > 0)
				return param.Substring(index);
			return string.Empty;
		}
	}
}
