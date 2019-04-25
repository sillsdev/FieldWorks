// Copyright (c) 2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using SIL.LCModel.Core.WritingSystems;
using SIL.WritingSystems;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary/>
	public class AdvancedScriptRegionVariantModel
	{
		private FwWritingSystemSetupModel _model;
		private ScriptListItem _currentScriptListItem;
		private ScriptListItem _script;
		private RegionListItem _region;
		private readonly RegionSubtag _privateUseQMRegion = new RegionSubtag("QM", "Private Use Region");
		private readonly VariantListItem _noneVariantListItem = new VariantListItem(null);
		private readonly ScriptListItem _noneScriptListItem = new ScriptListItem(null);
		private readonly RegionListItem _noneRegionListItem = new RegionListItem(null);

		private delegate void ChangeCodeDelegate();
		private event ChangeCodeDelegate ChangeCode = delegate { }; // add empty delegate!

		private void ChangeCodeValue()
		{
			Code = IetfLanguageTag.Create(CurrentWs.Language, Script, Region, GetCurrentVariants());
		}

		private IEnumerable<VariantSubtag> GetCurrentVariants()
		{
			// TODO: Remove StandardVariant if it is no longer applicable
			var variants = string.IsNullOrEmpty(StandardVariant) ? string.Empty : StandardVariant;
			if (!string.IsNullOrEmpty(OtherVariants))
			{
				variants += "-";
				variants += OtherVariants;
			}

			IEnumerable<VariantSubtag> varaintList;
			if (IetfLanguageTag.TryGetVariantSubtags(variants, out varaintList))
			{
				return varaintList;
			}
			return null;
		}

		private CoreWritingSystemDefinition CurrentWs
		{
			get { return _model.WorkingList[_model.CurrentWritingSystemIndex].WorkingWs; }
		}

		/// <summary/>
		public string Abbreviation
		{
			get { return _model.CurrentWsSetupModel.CurrentAbbreviation; }
			set { _model.CurrentWsSetupModel.CurrentAbbreviation = value; }
		}

		/// <summary/>
		public AdvancedScriptRegionVariantModel(FwWritingSystemSetupModel model)
		{
			_model = model;
			Code = _model.CurrentWsSetupModel.CurrentLanguageTag;
			ChangeCode = ChangeCodeValue;
		}

		/// <summary/>
		public string Code {
			get { return CurrentWs.LanguageTag; }
			set
			{
				ChangeCode -= ChangeCodeValue;
				LanguageSubtag language;
				ScriptSubtag script;
				RegionSubtag region;
				IEnumerable<VariantSubtag> variants;
				if (IetfLanguageTag.TryGetSubtags(value, out language, out script, out region, out variants))
				{
					if (script != null)
					{
						var registeredScripts = GetScripts();
						var selectedScript = registeredScripts.FirstOrDefault(s => s.Code == script.Code);
						Script = selectedScript ?? new ScriptListItem(script);
						CurrentWs.Script = Script;
					}
					else
					{
						Script = _noneScriptListItem; // rely on set side effects to adjust CurrentWs
					}
					if (region != null)
					{
						var registeredRegions = GetRegions();
						var selectedRegion = registeredRegions.FirstOrDefault(r => r.Code == region.Code);
						Region = selectedRegion ?? new RegionListItem(region);
						CurrentWs.Region = Region;
					}
					else
					{
						Region = _noneRegionListItem; // rely on set side effects to adjust CurrentWs
					}
					CurrentWs.Variants.Clear();
					CurrentWs.Variants.AddRange(variants);
				}
				ChangeCode += ChangeCodeValue;
			}
		}

		/// <summary/>
		public ScriptListItem Script
		{
			get { return _script; }
			set
			{
				_script = value;
				// We don't want to lose the ScriptCode and ScriptRegion if the user picks "Qaaa" and already have a custom script set
				// otherwise we do want to set them
				if (value.Code != "Qaaa" || ScriptCode == null || StandardSubtags.RegisteredScripts.Contains(ScriptCode))
				{
					ScriptCode = value.Code;
					ScriptName = value.Name;
					ChangeCode?.Invoke();
				}
			}
		}

		/// <summary/>
		public string ScriptName
		{
			get { return CurrentWs.Script?.Name; }
			set
			{
				if (CurrentWs.Script != null)
				{
					CurrentWs.Script = new ScriptSubtag(CurrentWs.Script, value);
				}
				else if (value != null)
				{
					throw new ArgumentException("Can not set the name on a null script, set script Code first");
				}
			}
		}

		/// <summary/>
		public string ScriptCode
		{
			get { return CurrentWs?.Script?.Code; }
			set
			{
				if (value == null)
				{
					CurrentWs.Script = null;
					return;
				}
				if (_script.IsPrivateUse)
				{
					var updatedTag = new ScriptSubtag(value, ScriptName);
					CurrentWs.Script = updatedTag;
					_script = new ScriptListItem(updatedTag);
					return;
				}
				ScriptSubtag registeredScript = null;
				if (StandardSubtags.RegisteredScripts.TryGet(value, out registeredScript))
				{
					CurrentWs.Script = registeredScript;
				}
				else
				{
					var updatedTag = new ScriptSubtag(value, ScriptName);
					CurrentWs.Script = updatedTag;
				}
			}
		}

		/// <summary>
		/// If the user selects Qaaa we want to let them edit the 'Code' to customize it.
		/// If the Script is private use, but the code doesn't match any registered private use scripts then they have already customized
		/// and we want to allow them to edit it.
		/// </summary>
		public bool EnableScriptCode => Script != null && (Script.Code == "Qaaa" || Script.IsPrivateUse && !StandardSubtags.IsPrivateUseScriptCode(ScriptCode));

		/// <summary/>
		public string RegionName
		{
			get { return CurrentWs.Region?.Name; }
			set
			{
				if (CurrentWs.Region != null)
				{
					CurrentWs.Region = new RegionSubtag(CurrentWs.Region, value);
				}
				else if (value != null)
				{
					throw new ArgumentException("Can not set the name on a null region, set RegionCode first");
				}
			}
		}

		/// <summary/>
		public RegionListItem Region
		{
			get { return _region; }
			set
			{
				// We don't want to lose the RegionCode and RegionName if the user picks "QM" and already have a custom region set
				// otherwise we do want to set them
				_region = value;
				if (value.Code != "QM" || RegionCode == null || StandardSubtags.IsValidRegisteredVariantCode(value.Code))
				{
					RegionCode = value.Code;
					RegionName = value.Name;
					ChangeCode?.Invoke();
				}
			}
		}

		/// <summary/>
		public bool EnableRegionCode => Region != null && (Region.Code == "QM" || Region.IsPrivateUse && !StandardSubtags.IsPrivateUseRegionCode(RegionCode));

		/// <summary/>
		public string RegionCode
		{
			get { return CurrentWs?.Region?.Code; }
			set
			{
				if (value == null)
				{
					CurrentWs.Region = null;
					return;
				}
				if (_region.IsPrivateUse)
				{
					var updatedTag = new RegionSubtag(value, RegionName);
					CurrentWs.Region = updatedTag;
					// Update the region with this code if necessary. (The change may have originated there.)
					_region = new RegionListItem(updatedTag);
					return;
				}
				RegionSubtag registeredRegion = null;
				if (StandardSubtags.RegisteredRegions.TryGet(value, out registeredRegion))
				{
					CurrentWs.Region = registeredRegion;
				}
				else
				{
					var updatedTag = new RegionSubtag(value, RegionName);
					CurrentWs.Region = updatedTag;
				}
			}
		}

		/// <summary/>
		public string StandardVariant
		{
			get
			{
				var firstVariant = CurrentWs.Variants.FirstOrDefault();
				if (firstVariant != null && !firstVariant.IsPrivateUse)
				{
					return firstVariant.Code;
				}

				return null;
			}
			set
			{
				var firstVariant = CurrentWs.Variants.FirstOrDefault();
				if (firstVariant != null && !firstVariant.IsPrivateUse)
				{
					CurrentWs.Variants.RemoveAt(0);
				}
				// if the user selected None we will not add anything otherwise add the selected standard variant
				if (!string.IsNullOrEmpty(value))
				{
					CurrentWs.Variants.Insert(0, StandardSubtags.RegisteredVariants[value]);
				}
			}
		}

		/// <summary/>
		public string OtherVariants
		{
			// return other variants including the first one if it is private use
			get
			{
				var firstVariant = CurrentWs.Variants.FirstOrDefault();
				IEnumerable<VariantSubtag> otherVariants;
				if (firstVariant != null && !firstVariant.IsPrivateUse)
				{
					otherVariants = CurrentWs.Variants.Skip(1);
				}
				else
				{
					otherVariants = CurrentWs.Variants;
				}

				var otherVariantCodes = string.Empty;

				bool foundPrivateUse = false;
				foreach (var variant in otherVariants)
				{
					if (!foundPrivateUse && variant.IsPrivateUse)
					{
						otherVariantCodes += "x-";
						foundPrivateUse = true;
					}

					otherVariantCodes += variant.Code + "-";
				}

				return otherVariantCodes.TrimEnd('-');
			}
			set
			{
				IEnumerable<VariantSubtag> otherVariants;
				IetfLanguageTag.TryGetVariantSubtags(value, out otherVariants);
				var otherVariantList = new List<VariantSubtag>(otherVariants);
				var firstVariant = CurrentWs.Variants.FirstOrDefault();
				if (firstVariant != null && !firstVariant.IsPrivateUse)
				{
					otherVariantList.Insert(0, firstVariant);
				}

				CurrentWs.Variants.Clear();
				CurrentWs.Variants.AddRange(otherVariantList);

			}
		}

		/// <summary/>
		public IEnumerable<VariantListItem> GetStandardVariants()
		{
			yield return _noneVariantListItem;
			foreach (var variant in StandardSubtags.RegisteredVariants)
			{
				if (variant.IsDeprecated)
				{
					continue;
				}
				if (!variant.Prefixes.Any())
				{
					yield return new VariantListItem(variant);
				}
				else if (variant.Prefixes.All(p => Code.EndsWith(p)))
				{
					yield return new VariantListItem(variant);
				}
			}
		}

		/// <summary/>
		public IEnumerable<ScriptListItem> GetScripts()
		{
			yield return _noneScriptListItem;
			// If the model represents a private use script code add it first in the list
			if (CurrentWs?.Script != null && CurrentWs.Script.IsPrivateUse)
			{
				yield return new ScriptListItem(CurrentWs.Script);
			}
			foreach (var script in StandardSubtags.RegisteredScripts)
			{
				if (script.IsDeprecated)
				{
					continue;
				}
				yield return new ScriptListItem(script);
			}
		}

		/// <summary/>
		public IEnumerable<RegionListItem> GetRegions()
		{
			yield return _noneRegionListItem;
			// If the model represents a private use region code add it first in the list
			if (CurrentWs?.Region != null && CurrentWs.Region.IsPrivateUse)
			{
				yield return new RegionListItem(CurrentWs.Region);
			}
			foreach (var region in StandardSubtags.RegisteredRegions)
			{
				if (region.IsDeprecated)
				{
					continue;
				}
				yield return new RegionListItem(region);
			}
			yield return new RegionListItem(_privateUseQMRegion);
		}


		/// <summary/>
		public class ScriptListItem : IEquatable<ScriptListItem>
		{
			private readonly ScriptSubtag _script;

			/// <summary/>
			public ScriptListItem(ScriptSubtag script)
			{
				_script = script;
			}

			/// <summary/>
			public string Name => _script?.Name;

			/// <summary/>
			public bool IsPrivateUse => _script != null && _script.IsPrivateUse;

			/// <summary/>
			public string Code => _script?.Code;

			/// <summary/>
			public string Label => _script == null ? "None" : string.Format("{0} ({1})", _script.Name, _script.Code);

			/// <summary/>
			public bool Equals(ScriptListItem other)
			{
				if (ReferenceEquals(null, other)) return false;
				if (ReferenceEquals(this, other)) return true;
				if (_script == null != (other._script == null))
					return false;
				if (_script == null && other._script == null)
					return true;
				return _script.IsPrivateUse == other.IsPrivateUse && _script.Code == other.Code && _script.Name == other.Name;
			}

			/// <summary/>
			public override bool Equals(object obj)
			{
				if (ReferenceEquals(null, obj)) return false;
				if (ReferenceEquals(this, obj)) return true;
				if (obj.GetType() != this.GetType()) return false;
				return Equals((ScriptListItem)obj);
			}

			/// <summary/>
			public override int GetHashCode()
			{
				return (_script != null ? _script.GetHashCode() : 0);
			}

			/// <summary>Allow cast of a ScriptListItem to a ScriptSubtag</summary>
			public static implicit operator ScriptSubtag(ScriptListItem item)
			{
				return item?._script;
			}
		}

		/// <summary/>
		public class RegionListItem : IEquatable<RegionListItem>
		{
			private RegionSubtag _region;

			/// <summary/>
			public RegionListItem(RegionSubtag region)
			{
				_region = region;
			}

			/// <summary/>
			public string Name => _region?.Name;

			/// <summary/>
			public bool IsPrivateUse => _region != null && _region.IsPrivateUse;

			/// <summary/>
			public string Code => _region?.Code;

			/// <summary/>
			public string Label => _region == null ? "None" : string.Format("{0} ({1})", _region.Name, _region.Code);

			/// <summary>Allow cast of a RegionListItem to a RegionSubtag</summary>
			public static implicit operator RegionSubtag(RegionListItem item)
			{
				return item?._region;
			}

			/// <summary/>
			public bool Equals(RegionListItem other)
			{
				if (ReferenceEquals(null, other)) return false;
				if (ReferenceEquals(this, other)) return true;
				return Equals(_region, other._region);
			}

			/// <summary/>
			public override bool Equals(object obj)
			{
				if (ReferenceEquals(null, obj)) return false;
				if (ReferenceEquals(this, obj)) return true;
				if (obj.GetType() != this.GetType()) return false;
				return Equals((RegionListItem)obj);
			}

			/// <summary/>
			public override int GetHashCode()
			{
				return (_region != null ? _region.GetHashCode() : 0);
			}
		}

		/// <summary/>
		public class VariantListItem
		{
			private VariantSubtag _variant;

			/// <summary/>
			public VariantListItem(VariantSubtag variant)
			{
				_variant = variant;
			}

			/// <summary/>
			public string Code => _variant?.Code;

			/// <summary/>
			public string Name => _variant == null ? "None" : _variant.Name;

			/// <summary/>
			public override string ToString()
			{
				return Code;
			}
		}

		/// <summary/>
		public bool ValidateOtherVariants(string otherVariants)
		{
			IEnumerable<VariantSubtag> tags;
			return !otherVariants.EndsWith("-") && IetfLanguageTag.TryGetVariantSubtags(otherVariants, out tags);
		}

		/// <summary/>
		public bool ValidateScriptCode(string scriptCode)
		{
			return IetfLanguageTag.IsValidScriptCode(scriptCode);
		}

		/// <summary/>
		public bool ValidateRegionCode(string regionCode)
		{
			return IetfLanguageTag.IsValidRegionCode(regionCode);
		}

		/// <summary/>
		public bool ValidateIetfCode(string text)
		{
			string language;
			string script;
			string region;
			string variant;
			if (IetfLanguageTag.TryGetParts(_model.CurrentWsSetupModel?.CurrentLanguageTag, out language, out script, out region,
				out variant))
			{

				return text.StartsWith(language) && IetfLanguageTag.IsValid(text);
			}
			throw new ApplicationException("Invalid code stored in the model");
		}
	}
}
