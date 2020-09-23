// Copyright (c) 2017-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Linq;

namespace SIL.FieldWorks.Common.FwUtils
{
	public class GraphiteFontFeatures
	{
		public static string ConvertFontFeatureCodesToIds(string features)
		{
			// If the feature is empty or has already been converted just return
			if (string.IsNullOrEmpty(features) || !char.IsLetter(features[0]))
			{
				return features;
			}
			var feature = features.Split(',');
			foreach (var value in feature)
			{
				var keyValuePair = value.Split('=');
				var key = ConvertFontFeatureCodeToId(keyValuePair[0]);
				features = features.Replace(keyValuePair[0], key.ToString());
			}
			return features;
		}

		private static int ConvertFontFeatureCodeToId(string fontFeature)
		{
			fontFeature = new string(fontFeature.ToCharArray().Reverse().ToArray());
			return BitConverter.ToInt32(fontFeature.Select(Convert.ToByte).ToArray(), 0);
		}
	}
}
