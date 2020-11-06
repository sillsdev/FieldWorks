// Copyright (c) 2019-2020 SIL International
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
	internal sealed class AdvancedScriptRegionVariantModel
	{
		private FwWritingSystemSetupModel _model;
		private ScriptListItem _currentScriptListItem;
		private ScriptListItem _script;
		private RegionListItem _region;
		private static readonly ScriptSubtag _privateUseQaaaScript = new ScriptSubtag("Qaaa", "Private Use Script"); // TODO (Hasso): 2021.12: ensure helps list this
		private static readonly RegionSubtag _privateUseQMRegion = new RegionSubtag("QM", "Private Use Region");
		private static readonly VariantListItem _noneVariantListItem = new VariantListItem(null);
		private static readonly ScriptListItem _noneScriptListItem = new ScriptListItem(null);
		private static readonly RegionListItem _noneRegionListItem = new RegionListItem(null);
		private delegate void ChangeCodeDelegate();
		private event ChangeCodeDelegate ChangeCode;

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

			return IetfLanguageTag.TryGetVariantSubtags(variants, out var variantList) ? variantList : null;
		}

		private CoreWritingSystemDefinition CurrentWs => _model.WorkingList[_model.CurrentWritingSystemIndex].WorkingWs;

		/// <summary/>
		public string Abbreviation
		{
			get => _model.CurrentWsSetupModel.CurrentAbbreviation;
			set => _model.CurrentWsSetupModel.CurrentAbbreviation = value;
		}

		/// <summary/>
		public AdvancedScriptRegionVariantModel(FwWritingSystemSetupModel model)
		{
			_model = model;
			Code = _model.CurrentWsSetupModel.CurrentLanguageTag;
			ChangeCode = ChangeCodeValue;
		}

		/// <summary/>
		internal string Code
		{
			get => CurrentWs.LanguageTag;
			set
			{
				ChangeCode -= ChangeCodeValue;
				if (IetfLanguageTag.TryGetSubtags(value, out _, out var script, out var region, out var variants))
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
		internal ScriptListItem Script
		{
			get => _script;
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
		internal string ScriptName
		{
			get => CurrentWs.Script?.Name;
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
		internal string ScriptCode
		{
			get => CurrentWs?.Script?.Code;
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
				if (StandardSubtags.RegisteredScripts.TryGet(value, out var registeredScript))
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
		internal bool EnableScriptCode => Script != null && (Script.Code == "Qaaa" || Script.IsPrivateUse && !StandardSubtags.IsPrivateUseScriptCode(ScriptCode));

		/// <summary/>
		internal string RegionName
		{
			get => CurrentWs.Region?.Name;
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
		internal RegionListItem Region
		{
			get => _region;
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
		internal bool EnableRegionCode => Region != null && (Region.Code == "QM" || Region.IsPrivateUse && !StandardSubtags.IsPrivateUseRegionCode(RegionCode));

		/// <summary/>
		internal string RegionCode
		{
			get => CurrentWs?.Region?.Code;
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
				CurrentWs.Region = StandardSubtags.RegisteredRegions.TryGet(value, out var registeredRegion) ? registeredRegion : new RegionSubtag(value, RegionName);
			}
		}

		/// <summary/>
		internal string StandardVariant
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
		internal string OtherVariants
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

				var foundPrivateUse = false;
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
				IetfLanguageTag.TryGetVariantSubtags(value, out var otherVariants);
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
		internal IEnumerable<VariantListItem> GetStandardVariants()
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
				else if (variant.Prefixes.All(p => Code.EndsWith(p) || Code.StartsWith(p)))
				{
					yield return new VariantListItem(variant);
				}
			}
		}

		/// <summary/>
		internal IEnumerable<ScriptListItem> GetScripts()
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
			yield return new ScriptListItem(_privateUseQaaaScript);
		}

		/// <summary/>
		internal IEnumerable<RegionListItem> GetRegions()
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
		public bool ValidateOtherVariants(string otherVariants)
		{
			return !otherVariants.EndsWith("-") && IetfLanguageTag.TryGetVariantSubtags(otherVariants, out _);
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
			return IetfLanguageTag.TryGetParts(_model.CurrentWsSetupModel?.CurrentLanguageTag, out var language, out _, out _, out _)
				? text.StartsWith(language) && IetfLanguageTag.IsValid(text) : throw new ApplicationException("Invalid code stored in the model");
		}
	}
}
