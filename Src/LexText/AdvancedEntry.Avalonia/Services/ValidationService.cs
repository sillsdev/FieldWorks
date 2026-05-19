using System;
using System.Collections.Generic;
using SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.Layout.Compilation;
using SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.Presentation;
using SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.Staging;

namespace SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.Services;

public sealed record ValidationError(string Path, string Message);

public sealed class ValidationService
{
	public IReadOnlyList<ValidationError> Validate(
		PresentationLayout layout,
		StagedEntryState state
	)
	{
		var errors = new List<ValidationError>();
		ValidateNodes(layout.Children, state.Root, pathPrefix: layout.RootClass);
		return errors;

		void ValidateNodes(
			IReadOnlyList<PresentationNode> nodes,
			StagedObjectState owner,
			string pathPrefix
		)
		{
			foreach (var node in nodes)
			{
				switch (node)
				{
					case PresentationSection section:
						ValidateNodes(section.Children, owner, pathPrefix);
						break;
					case PresentationField field:
						if (field.IsRequired)
						{
							owner.Fields.TryGetValue(field.Field, out var value);
							if (string.IsNullOrWhiteSpace(value))
								errors.Add(
									new ValidationError(
										$"{pathPrefix}.{field.Field}",
										$"Required field '{field.Label ?? field.Field}' is empty."
									)
								);
						}
						break;
					case PresentationObject obj:
						{
							var childClass =
								FieldClassMap.GetItemClass(owner.ClassName, obj.Field, obj.Ghost) ?? "Unknown";
							var child = owner.GetOrCreateObject(obj.Field, childClass);
							ValidateNodes(obj.Children, child, $"{pathPrefix}.{obj.Field}");
							break;
						}
					case PresentationSequence seq:
						{
							var itemClass =
								FieldClassMap.GetItemClass(owner.ClassName, seq.Field, seq.Ghost) ?? "Unknown";
							var stagedSeq = owner.GetOrCreateSequence(seq.Field, itemClass);
							for (var i = 0; i < stagedSeq.Count; i++)
							{
								// Validation must not force UI/editor materialization.
								// If a sequence item isn't materialized in staged state yet, skip it.
								if (!stagedSeq.TryGetItem(i, out var item) || item is null)
									continue;

								ValidateNodes(
									seq.ItemTemplate,
									item,
									$"{pathPrefix}.{seq.Field}[{i}]"
								);
							}
							break;
						}
					default:
						break;
				}
			}
		}
	}
}