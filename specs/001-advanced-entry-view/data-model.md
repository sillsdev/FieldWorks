# Data Model (Phase 1)

This models detached DTOs used by the Avalonia.PropertyGrid host (Option A). These mirror LCM entities but are not bound to live LCModel objects during edit.

## EntryModel
- Id: (none until save)
- LexicalForms: Dictionary<WritingSystemId, string> (Required: at least one non-empty)
- CitationForm: Optional<string>
- MorphType: Enum or PossibilityRef (Required)
- PartOfSpeech: PossibilityRef (Optional, project-dependent)
- Senses: List<SenseModel>
- Pronunciations: List<PronunciationModel>
- Variants: List<VariantModel>
- Template: TemplateProfileModel (applied on create)

Validation rules
- At least one LexicalForm filled in a valid project WS
- MorphType must be selected
- No duplicate variants referencing self

## SenseModel
- Gloss: Dictionary<WritingSystemId, string>
- Definition: Dictionary<WritingSystemId, string>
- Examples: List<ExampleModel>
- SemanticDomain: PossibilityRef (Optional)

Validation rules
- At least one of Gloss or Definition should be non-empty

## ExampleModel
- Text: Dictionary<WritingSystemId, string>
- Translation: Dictionary<WritingSystemId, string>

## PronunciationModel
- Phonetic: string (WS-aware input; project default WS)
- AudioRef: Optional<Uri or FilePath>

## VariantModel
- VariantType: Enum/PossibilityRef
- TargetEntryRef: Optional<EntryRef>
- Form: Optional<string> (if not referencing an entry)

## TemplateProfileModel
- PresetMorphType: Optional
- PresetPOS: Optional
- DefaultSenseSkeleton: Optional (fields to pre-seed)
- VisibilityRules: Map<PropertyPath, VisibilityPolicy>

## Cross-cutting
- WritingSystemId: from project WS registry
- PossibilityRef: Id + display path (tree path)
- EntryRef: Id + headword preview

## Relationships
- EntryModel contains Senses, Pronunciations, Variants
- SenseModel contains Examples

## Mapping contracts
- EntryMapper: EntryModel → LexEntry (create entry, set forms, add children)
- SenseMapper, PronunciationMapper, VariantMapper: materialize sub-objects and attach
- Reverse mapping (for preview): LexEntry → EntryModel (subset) when needed
