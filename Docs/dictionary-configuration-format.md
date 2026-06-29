# The Dictionary Configuration File Format

This document describes the `.fwdictconfig` file that controls how FieldWorks Language
Explorer (FLEx) lays out a dictionary or reversal-index view, and shows how a technical
user can make changes by editing the file directly — including changes the **Configure
Dictionary** dialog cannot make on its own.

It is written for people who are comfortable editing XML in a text editor and who already
understand FLEx concepts such as writing systems, paragraph vs. character styles, and the
entry/sense structure of a dictionary.

## When you would edit the file by hand

Almost everything in a configuration can be set from **Tools ▸ Configure ▸ Dictionary**.
Hand-editing is for the changes the dialog does not expose. This guide walks through two such
edits — equally common, and each impossible from the dialog alone:

- **Paragraph display on a writing-system field** — render each writing system of a field as
  its own styled paragraph instead of inline.
- **Grouping fields under a heading** — collect several unrelated fields into one
  organizational block.

The principle is the same in both: the dialog can adjust a node that already has the
structure in question, but it cannot *add* that structure. Hand-editing adds it — and once it
is there, the dialog reads and preserves it.

## Where the files live

There are two locations.

**Shipped defaults (read-only templates).** These are installed with FieldWorks under the
program's `Language Explorer/DefaultConfigurations/` folder, e.g.
`DefaultConfigurations/Dictionary/Lexeme.fwdictconfig`. FLEx treats these as factory
templates and never writes to them: when you customize a configuration in the dialog, FLEx
saves your changes to a project copy instead of back to the shipped file. They are also
replaced every time FieldWorks is installed or upgraded.

**Project copies (the editable ones).** When you customize a configuration, FLEx writes a
copy into your project folder under:

```
<ProjectFolder>/ConfigurationSettings/Dictionary/
<ProjectFolder>/ConfigurationSettings/ReversalIndex/
```

These project copies are the files you edit. Don't edit a shipped default:
the program will not persist UI changes there, your edits would be lost on the next
upgrade, and the default only serves as the template that gets cloned into new projects —
it has no effect on a project that already has its own copy.

## Before you edit: work on a named copy

1. Open **Configure Dictionary**, click **Manage Configurations…**, select the
   configuration closest to what you want, and click **Copy**. Give it a distinct name
   (e.g. `Lexeme — paragraphs`). This writes a new file into
   `<ProjectFolder>/ConfigurationSettings/Dictionary/`.
2. Close FLEx (so your edits are not competing with an in-memory copy), open that file in a
   text editor, and confirm the root element reads `version="26"` (the current version).
3. Make a backup copy of the file before editing.
4. Edit, save, reopen FLEx, and check the result.

**Edit a named copy rather than a customized factory configuration.** In Manage
Configurations, a configuration that is a customized version of a shipped default shows a
**Reset** button, while a separately-named copy shows a **Remove** button. Reset reloads
the factory defaults over your file and discards every hand edit; Remove deletes the file
outright. Working on a clearly-named copy means the only destructive action available is an
explicit Remove — there is no Reset that would silently restore factory content over your
work.

**Leave the `version` attribute unchanged.** A copy from a current FLEx is already at the
current version, so the migration FLEx runs on upgrades skips it. See *Appendix: configuration
migration on upgrade* for the mechanics.

## Anatomy of the file

The root element is `DictionaryConfiguration`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<DictionaryConfiguration name="Lexeme-based (complex forms as main entries)" allPublications="true" isRootBased="false" version="26" lastModified="2025-02-26">
	<ConfigurationItem name="Main Entry" isEnabled="true" style="Dictionary-Normal" styleType="paragraph" field="LexEntry" cssClassNameOverride="entry">
		...
	</ConfigurationItem>
</DictionaryConfiguration>
```

Each displayed element is a `ConfigurationItem`. Items nest: a `ConfigurationItem` contains
child `ConfigurationItem` elements for its sub-fields, and optionally one options element.
The attributes you will touch most often are:

| Attribute | Meaning |
| --- | --- |
| `name` | The label shown in the dialog's tree. |
| `field` | The FieldWorks field this node displays (e.g. `DefinitionOrGloss`). Do not invent values. |
| `isEnabled` | Whether the node is shown (`true`/`false`). |
| `before` / `between` / `after` | Literal text inserted before the content, between repeated items, and after the content. |
| `style` | The name of the FLEx style applied to this node. Must be a style that exists in the project. |
| `styleType` | `character` or `paragraph` — whether `style` is a character or paragraph style. Omitted means the default for the node. |
| `cssClassNameOverride` | An alternate CSS class name for export. |

Writing-system fields carry a writing-system options element as a child:

```xml
<WritingSystemOptions writingSystemType="analysis" displayWSAbreviation="false">
	<Option id="all analysis" isEnabled="true"/>
</WritingSystemOptions>
```

`writingSystemType` is one of `vernacular`, `analysis`, `both`, `pronunciation`, or
`reversal`. Each `Option` names a writing system (`id`) and whether it is shown.

## Walkthrough: paragraph display on a writing-system field

A field that displays in one or more writing systems carries one of two option types:

- `WritingSystemOptions` — the ordinary type; the writing systems are shown inline.
- `WritingSystemAndParaOptions` — a superset that adds the attribute `displayInParagraph`.
  When `true`, each writing system renders as its own paragraph and the owning node carries a
  paragraph style.

The dialog shows a *"display in paragraphs"* checkbox **only on nodes that already carry
`WritingSystemAndParaOptions`** — in the shipped configurations these are the
analysis-writing-system note fields (Grammar Note, Discourse Note, General Note, and so on)
and Encyclopedic Info. It cannot switch a plainly-configured field to the paragraph-capable
type; that switch is the hand edit.

**Definition (or Gloss)** is one such field — just a "for instance"; the same two edits work
on any node with writing-system options. As shipped in `Lexeme.fwdictconfig` it uses plain
writing-system options, displayed inline:

```xml
<ConfigurationItem name="Definition (or Gloss)" between=" " after="" isEnabled="true" field="DefinitionOrGloss">
	<WritingSystemOptions writingSystemType="analysis" displayWSAbreviation="false">
		<Option id="all analysis" isEnabled="true"/>
	</WritingSystemOptions>
</ConfigurationItem>
```

To display each analysis writing system as its own styled paragraph, make **two** changes:

```xml
<ConfigurationItem name="Definition (or Gloss)" between=" " after="" isEnabled="true" style="Dictionary-Normal" styleType="paragraph" field="DefinitionOrGloss">
	<WritingSystemAndParaOptions writingSystemType="analysis" displayWSAbreviation="false" displayInParagraph="true">
		<Option id="all analysis" isEnabled="true"/>
	</WritingSystemAndParaOptions>
</ConfigurationItem>
```

1. **Change the options element** from `WritingSystemOptions` to
   `WritingSystemAndParaOptions` (both the opening and closing tag) and add
   `displayInParagraph="true"`. The `writingSystemType`, `displayWSAbreviation`, and the
   `<Option>` children stay exactly as they were.
2. **Add a paragraph style to the `ConfigurationItem`**: `styleType="paragraph"` plus a
   `style` that names a **paragraph** style which already exists in the project. Here
   `Dictionary-Normal` is used because it ships as a paragraph style; in practice you would
   usually create a dedicated paragraph style in **Format ▸ Styles** (for example
   `Dictionary-Definition`) and name it here.

For the exact shipped shape, open `Root.fwdictconfig` and find `Grammar Note`: it already
carries `<WritingSystemAndParaOptions … displayInParagraph="false">`, which is what the
dialog's checkbox toggles. Toggling it there also assigns a paragraph style automatically;
when editing by hand you set the `style` yourself.

Save the file, reopen FLEx, and open the dictionary view. Each analysis writing system of
the definition should now appear on its own line in the chosen paragraph style. Open
Configure Dictionary and select the Definition node: the "display in paragraphs" checkbox is
now present and checked, confirming the dialog has accepted your edit.

## Walkthrough: grouping fields under a heading

A *grouping node* collects several sibling fields under one heading, purely for organization;
the model describes it as grouping "nodes which are not related in the model." It holds no
data of its own — each child still reads its own field from the same entry or sense the
group's siblings use — so the group is just a wrapper that can carry its own paragraph style
and an (unpublished) description. The dialog can move nodes into and out of
an existing group and edit a group's description, but it cannot create a group; that is a
hand edit.

A grouping node is an ordinary `ConfigurationItem` with a `<GroupingOptions>` child and the
grouped items nested inside it. This is the shipped example — **References Section** in
`Hybrid.fwdictconfig`, which gathers the component-reference fields into one styled block:

```xml
<ConfigurationItem name="References Section" isEnabled="true" before=" " after=" " styleType="paragraph" style="Block Quote" field="ReferencesSection">
	<GroupingOptions displayGroupInParagraph="true" />
	<ConfigurationItem name="Component References" ...> ... </ConfigurationItem>
	<!-- more grouped items -->
</ConfigurationItem>
```

Three things about the grouping node itself: `<GroupingOptions>` carries
`displayGroupInParagraph` (whether the whole group renders as its own paragraph) and may hold
descriptive text as its element content (a note to yourself — it is not published); the
`field` value (`ReferencesSection`) is **not** a real model field but just a unique
identifier for the group, so pick a distinctive name no sibling uses; and `styleType`/`style`
on the `ConfigurationItem` style the group as a whole.

The note fields — Grammar Note, Phonology Note, Semantics Note, Sociolinguistics Note,
Discourse Note, and the rest — ship as separate, adjacent siblings; this is just a "for
instance." To group them, wrap the note `ConfigurationItem`s in a new grouping node *in
place*, so their fields still resolve against the same sense:

```xml
<ConfigurationItem name="Notes" isEnabled="true" styleType="paragraph" style="Block Quote" field="NotesGroup">
	<GroupingOptions displayGroupInParagraph="true">Editorial notes, shown together</GroupingOptions>
	<ConfigurationItem name="Grammar Note" before=" " between=" " isEnabled="true" field="GrammarNote">
		<WritingSystemAndParaOptions writingSystemType="analysis" displayWSAbreviation="false" displayInParagraph="false">
			<Option id="all analysis" isEnabled="true"/>
		</WritingSystemAndParaOptions>
	</ConfigurationItem>
	<ConfigurationItem name="Phonology Note" before=" " between=" " isEnabled="true" field="PhonologyNote">
		<WritingSystemAndParaOptions writingSystemType="analysis" displayWSAbreviation="false" displayInParagraph="false">
			<Option id="all analysis" isEnabled="true"/>
		</WritingSystemAndParaOptions>
	</ConfigurationItem>
</ConfigurationItem>
```

Keep each grouped `ConfigurationItem` exactly as it was; you are only nesting it inside the
wrapper. Any node can be brought into the group this way, as long as it sits at the same
level as the group — that is, it must be one of the group's siblings.

Save the file and reopen FLEx: the grouped fields now display together as one block, and in
Configure Dictionary they appear nested under the **Notes** group, which you can select to
edit its description or paragraph setting.

## What FLEx does when it opens your file

Every time FLEx loads a project configuration — not only during a version migration — it
reconciles the file against the live project. Two of these steps directly affect
hand-editing:

- **Styles are validated.** If a node's `style` names a style that does not exist in the
  project's stylesheet, FLEx silently clears the `style` attribute as it loads the file.
  **This is the most common reason a hand-added paragraph style "doesn't take":** create the
  paragraph style in **Format ▸ Styles** first, and spell its name exactly as it appears
  there.
- **Writing-system and list options are refreshed.** Each writing-system node's `<Option>`
  list is updated against the project's current writing systems, and custom-field and
  type-list nodes are added or removed to match the project. Reference only writing systems
  that exist in the project, since this list is reconciled to the project each time the file
  loads.

## Practical notes

- **The dialog rewrites the whole file.** If you later make any change through Configure
  Dictionary, FLEx re-serializes the entire configuration. XML **comments are not
  preserved**, attribute order may change, and whitespace is normalized. Keep your notes
  outside the file.
- **No schema validation on load.** The file is deserialized directly; it is not validated
  against `DictionaryConfiguration.xsd`. Unknown elements and attributes are silently
  ignored (so a typo in an element or attribute name fails quietly rather than with an
  error), while malformed XML makes the whole configuration fail to load. Edit carefully and
  keep your backup.
- **Validate by hand against the schema.** The schema lives with the program at
  `Language Explorer/Configuration/DictionaryConfiguration.xsd`. You can validate your file
  against it with any XML tool before loading it in FLEx.

## Appendix: configuration migration on upgrade

On startup FLEx migrates configurations whose `version` is **older** than the current
version (`26`). The migration scans only the project copies in
`<ProjectFolder>/ConfigurationSettings/Dictionary` and `ConfigurationSettings/ReversalIndex`,
and touches only files whose `version` is below `26`. When it runs, it loads the matching
shipped default and copies the default's values for attributes such as `before`, `between`,
`after`, `style`, `styleType`, and `cssClassNameOverride` onto matching nodes, then rewrites
the file — which can overwrite or relocate a hand-set paragraph style.

A copy made by a current FLEx is already at `version="26"`, so the migrator skips it
entirely. This is the reason for working on a current-version copy and leaving the `version`
attribute unchanged: do not lower it (you would invite a migration pass that may overwrite
your changes) and do not raise it (a future FLEx expects to migrate from the real version).

## Appendix: quick reference

### `DictionaryConfiguration` (root) attributes

| Attribute | Meaning |
| --- | --- |
| `name` | Display label of the configuration. |
| `version` | Schema version. Currently `26`. Leave unchanged. |
| `lastModified` | Date FLEx last saved the file. |
| `allPublications` | Whether the configuration applies to all publications. |
| `isRootBased` | Root-based (`true`) vs. lexeme-based (`false`) layout. |
| `writingSystem` | For reversal configurations, the reversal writing system. |

### `ConfigurationItem` attributes

`name`, `field`, `subField`, `isEnabled`, `before`, `between`, `after`, `style`,
`styleType`, `cssClassNameOverride`, `nameSuffix`, `isDuplicate`, `isCustomField`,
`hideCustomFields`. (See the table under *Anatomy of the file* for the common ones.)

`styleType` values: `default`, `character`, `paragraph`.

### Options elements (children of a `ConfigurationItem`)

| Element | Key attributes | Notes |
| --- | --- | --- |
| `WritingSystemOptions` | `writingSystemType`, `displayWSAbreviation` | Inline writing-system display. |
| `WritingSystemAndParaOptions` | `writingSystemType`, `displayWSAbreviation`, **`displayInParagraph`** | Adds per-writing-system paragraph display. See *Walkthrough: paragraph display on a writing-system field*. |
| `ListTypeOptions` | `list` (`none`/`minor`/`complex`/`variant`/`sense`/`entry`/`note`) | A selectable list of items. |
| `ComplexFormOptions` | `list`, **`displayEachComplexFormInParagraph`** | List options that can show each item in its own paragraph. |
| `SenseOptions` | `numberingStyle`, `numberBefore`, `numberAfter`, `numberSingleSense`, `showSingleGramInfoFirst`, `displayEachSenseInParagraph`, `displayFirstSenseInline` | Sense numbering and layout. |
| `PictureOptions` | `minimumHeight`/`Width`, `maximumHeight`/`Width`, `pictureLocation`, `stackPictures` | Picture sizing and placement. |
| `GroupingOptions` | `displayGroupInParagraph` | Wraps unrelated sibling nodes under one heading; its element text is an unpublished description. See *Walkthrough: grouping fields under a heading*. |

`writingSystemType` values: `vernacular`, `analysis`, `both`, `pronunciation`, `reversal`.

Each list-style options element contains `<Option id="…" isEnabled="true|false"/>` children
identifying the writing systems or list items, in display order.
