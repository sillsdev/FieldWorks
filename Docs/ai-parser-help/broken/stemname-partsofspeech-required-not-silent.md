---
title: "A StemName's partsOfSpeech is required by the loader — omitting it crashes, it doesn't silently reject everything"
implements: src/SIL.Machine.Morphology.HermitCrab/XmlLanguageLoader.cs, src/SIL.Machine.Morphology.HermitCrab/StemName.cs, src/SIL.Machine.Morphology.HermitCrab/HermitCrabInput.dtd
category: loader
symptom: crash
grammar_visible: yes
---

## What it is

A `<StemName>` element's `partsOfSpeech` attribute is declared `#REQUIRED` in the HC-XML DTD:

```
<!ELEMENT StemName (Name, Regions) >
<!ATTLIST StemName
  id ID #REQUIRED
  partsOfSpeech IDREFS #REQUIRED
>
```

(`HermitCrabInput.dtd:94-99`). This is worth checking against source rather than assuming, because
the failure mode for omitting it is a crash, not — as one might guess from the compiled `StemName`
class alone — a silent "empty POS constraint that matches nothing." The compiled `StemName` class
(`StemName.cs`) doesn't even have a `PartsOfSpeech` property; part of speech is folded into each
region's feature structure at load time, and the loader never tolerates a missing value for it.

## The mechanism

`XmlLanguageLoader.LoadStemName` reads and immediately uses the attribute with no null-check:

```csharp
private void LoadStemName(XElement stemNameElem)
{
    var posIDs = (string)stemNameElem.Attribute("partsOfSpeech");
    FeatureSymbol[] pos = posIDs.Split(' ').Select(id => _posFeature.PossibleSymbols[id]).ToArray();
    ...
    foreach (XElement regionElem in stemNameElem.Elements("Regions").Elements("Region"))
    {
        var fs = new FeatureStruct();
        fs.AddValue(_posFeature, pos);
        ...
    }
}
```

(`XmlLanguageLoader.cs:323-342`). `posIDs` is a plain nullable string cast from the XML attribute
with no fallback. If the attribute is truly absent, `posIDs` is `null` and `posIDs.Split(' ')` throws
a `NullReferenceException` immediately during loading — before any word is ever parsed or generated,
and before the stem name's regions are even built.

In practice this null case is also caught earlier by XML validation on most runtimes:
`XmlLanguageLoader.Load` sets `ValidationType = Type.GetType("Mono.Runtime") == null ?
ValidationType.DTD : ValidationType.None` (`XmlLanguageLoader.cs:212`). On a non-Mono .NET runtime,
the reader validates the document against the DTD as it parses, and a `StemName` element missing its
`#REQUIRED` `partsOfSpeech` attribute fails validation with an explicit error naming the missing
attribute — before `LoadStemName` ever runs. Only on Mono (`ValidationType.None`, no DTD validation)
would an omitted attribute reach `LoadStemName` at all, where it produces the `NullReferenceException`
described above instead. Either way, the outcome is a loud failure at load time, not a silently
unusable stem name.

A related but distinct trap: if `partsOfSpeech` is present but contains an ID that isn't a POS the
grammar declared, `_posFeature.PossibleSymbols[id]` throws a `KeyNotFoundException` (or equivalent) —
also a crash, also at load time, also not silent.

## Why this is worth documenting anyway

Even though the failure is loud rather than silent, the *error message itself* doesn't say "this
stem name is unusable" in language a grammar author would immediately connect to their FLEx-side
edit — a DTD validation error or a bare `NullReferenceException`/`KeyNotFoundException` stack trace
from deep in `XmlLanguageLoader` gives no hint that the fix is "add a `partsOfSpeech` attribute to
this `StemName` element" unless the reader already knows this loader code. The practical failure
mode is "the grammar fails to load at all" (not "some words silently fail to parse"), which is an
important distinction from the "rejects everything at runtime" framing this trap is sometimes
described with — the actual failure happens at grammar-load time, for the whole grammar, not
per-word at parse time.

## Fix

Always give every `<StemName>` an explicit `partsOfSpeech` listing every POS the stem name should
apply to. If a load fails with a `NullReferenceException` or DTD validation error mentioning
`StemName`, check for a missing or misspelled `partsOfSpeech` attribute value first — the loader
provides no default and no graceful degradation for it.
