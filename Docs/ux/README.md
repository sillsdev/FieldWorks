# FieldWorks UX guide

This guide states how the FieldWorks user interface should look and behave. It
covers new and changed Avalonia UI now, and it is written as the guide for all
FieldWorks UI, since every screen is moving to Avalonia. It is authoritative
for view-wide UX decisions: where code and this guide disagree, either the code
is a bug or the guide needs a recorded change.

The rules state intent. Measurements (colors, sizes, spacing) live in theme
tokens, not here; [winforms-theme-baseline.md](winforms-theme-baseline.md)
records the WinForms values they started from, so divergence can be measured.
Skills and other agent instructions keep the mechanics of following a rule and
link to the rule here.

Many rules come from long-standing WinForms behavior that users already rely
on. A rule that deliberately departs from WinForms carries a **Needs team
consensus** note until the team agrees to it.

Differences in a single converted screen, such as a field that works
differently in one dialog, are recorded in that conversion's Jira issue, not
here. This guide holds decisions that apply across views; its
[decision register](#decision-register) lists decisions that are provisional
or temporary.

Sections: [Universal](#universal) | [Dialogs](#dialogs) |
[Browse views](#browse-views) | [Detail views](#detail-views) |
[Interlinear text](#interlinear-text) | [Decision register](#decision-register)
| [Appendix: terms and code](#appendix-terms-and-where-they-live-in-code)

## Universal

### Stealth to wealth

**Stealth to wealth.** FieldWorks reveals capability as a user's data and skill
grow. A new user with an empty project should see a small, calm interface.
Fields, columns, commands and options appear when the data needs them or when
the user asks for them.

- Prefer hiding until relevant over disabling. Show what applies to the current
  tool and record. *(The exception is a fixed set of controls such as dialog
  tabs: there, show what doesn't apply as disabled, so the layout doesn't
  shift.)*
- Everything hidden must be reachable by a visible, discoverable route (Show
  Hidden Fields, the column chooser, Configure…). Never rely on hover alone or
  on a secret shortcut.
- Data the user has entered is never hidden by default. A field that holds data
  shows.
- Allow quick, rough work now and refinement later. Don't force a user to
  complete an analysis before moving on.

### Writing system drives input and direction

**The writing system drives input and direction.** Wherever the user edits data
text, the active keyboard (and input method) follows the writing system at the
cursor. When focus moves from data text to an interface control such as a dialog
or toolbar box, the user's default keyboard returns. Text direction and
alignment come from the writing system: right-to-left data reads and aligns
right-to-left in every view, mixed-direction text displays in the correct order,
and sequences the user reads in order (such as morphemes) follow the writing
system's direction. Interface text (menus, labels, buttons) follows the
interface language's direction, not the data's.

### Command labels

**Commands say what they do.** A command label starts with a verb (Insert,
Delete, Move, Show, Configure…) and names its object ("Insert Sense", "Delete
Example"). A label ends in "…" when the command needs more input before it acts
(opens a dialog or asks for confirmation); a command that acts immediately has
no "…". Dialog titles never end in "…". Commands that move the user to another
tool use one form: "Show in {tool}". The "…" is part of the localized string,
never appended in code. Every top-level menu has an access key, and menu
commands should have one too; in a long menu where keys would collide, give them
first to the most frequently used commands.

### Menu structure

**Menus keep their shape.** Users rely on muscle memory, so the new UI keeps the
WinForms menu order: the main menu (File, Send/Receive, Edit, View, Data,
Insert, Format, Tools, Window, Help) and every existing context menu keep their
items in the same order and grouping, especially in high-use places such as the
detail-view row menus and the browse-view menus. Areas add commands to the
existing menus, never new top-level menus. A new context menu follows the
WinForms pattern: text editing first (Cut, Copy, Paste) where it applies, then
navigation ("Show in…"), then commands that act on the item, with structural and
destructive commands (Move, Delete) last, and separators between groups. A set
of related commands appears the same way, flat or as a submenu, wherever it
occurs.

### Keyboard shortcuts

**Shortcuts are kept, not reinvented.** The new UI keeps every WinForms shortcut
with its meaning in that area; users' hands know them. A new shortcut must not
reuse a key that already has a meaning in the same area, and a modifier keeps
one meaning across a family of commands (in interlinear, Shift means "move
without approving"). Every shortcut is shown next to its command in the menu.
Developer and support tools (such as Inspect entry or the nightly update
channel) may stay behind a key combination, because they aren't user features; a
user feature always has a visible command.

### Status bar

**Tell the user when the view isn't showing everything.** Whenever a filter, a
non-default sort, or another mode changes what the user sees, the status bar
says so, and each kind of state keeps its own distinct indicator. These
indicators disappear when the view returns to its default, so their presence
always means something. The status bar shows the current record's position
("3/120") or "No Records". If the user asks to go to something the current
filter hides, ask before turning the filter off; never silently fail to show it.

### Empty and missing values

**Empty looks empty; missing looks missing.** A field with no value shows a
muted hint saying what goes there (for example "Type to add Gloss"), never
placeholder characters that could be mistaken for data. Where the user needs to
see that an expected value is missing (an unanalyzed word's gloss, an empty
chart cell), use one missing-value marker everywhere ("***"), drawn so it can't
be mistaken for typed text. Hint text and markers are never saved as data, and
they disappear as soon as the user types.

### Clickable items

**Clickable things look and feel clickable, the same way everywhere.** Anything
that navigates or runs a command when clicked gets one shared, recognizable
"clickable" treatment, used consistently across the product. A link style is one
way to do it; whatever the treatment is, it comes from the theme, and every
clickable item uses it. The pointer confirms what a click will do (for example,
a hand over clickable items and a text pointer over editable text). A disabled
command looks disabled rather than vanishing. A view that the user is working
against from elsewhere (a preview, a find target) keeps its selection visible
when it loses focus; other views hide it.

### Data text font

**Data text is drawn as the user configured it.** Text the user entered or that
belongs to their project (entries, glosses, list items, example sentences) is
drawn in its writing system's font and the project's styles, wherever it
appears: detail views, browse cells, choosers, dialogs, menus listing data. Only
interface text (labels, buttons, menus, captions) uses the interface font. Never
draw data text in the interface font to make a screen look uniform; for scripts
that need a specific font, the wrong font can mean missing or broken characters.

### Theme values

**Every color, size and spacing comes from the theme.** Screens use named theme
values (tokens) for color, spacing, sizing and type, never one-off numbers. When
something needs a look the theme doesn't have, add a named token for its purpose
(for example "approved guess background"), not a raw value at the point of use.
The same purpose always uses the same token, so one meaning has one look
everywhere, and a theme change reaches every screen.
Color tokens are defined per theme variant and referenced so they update when
the variant changes (in Avalonia, `ThemeDictionaries` and `DynamicResource`),
so a dark theme stays possible later. FieldWorks tokens alias Semi.Avalonia's
keys (ADR 0001) and never redefine a Semi key; a component that needs different
values uses its own token or control theme.

### Localizable text

**Every word the user reads can be translated.** All interface text, including
labels, tooltips, messages, accessible names and the "…" on commands, comes from
localizable resources, never from strings written in code. Messages are whole
sentences with placeholders ("Delete {0}?"), not fragments joined in code,
because word order differs between languages. Layouts leave room for longer
translations. Technical detail about errors and exceptions is not covered by
this rule: it may appear untranslated in the error report dialogs and in logs
meant for the development team.

### Keyboard and assistive access

**Everything works without a mouse and can be read by assistive tools.** Every
control the user can act on can be reached and operated from the keyboard, in a
sensible Tab order that follows the visual reading order, and the focused item
is always visibly marked and never fully hidden behind other content. Every
such control has an accessible name in the
interface language (usually its visible label) and a stable automation id that
does not change with language or layout, so screen readers and automated tests
can find it. Automation ids are never built from translated text, are unique
within their window, and start with the name of their dialog or view
(`AddNewSense.Ok`). Icon-only buttons always have a tooltip that matches their
accessible name. Text and the parts of controls the user must see meet WCAG 2.2
AA contrast: 4.5:1 for text (3:1 for large text), and 3:1 for control borders,
icons and focus marks. No automated check catches contrast, so check new colors
when adding them. Buttons, check boxes, links and other controls are at least
24 DIP high or spaced so that a 24 DIP target around each does not overlap its
neighbors (WCAG 2.5.8). Rows in dense data views follow the view's own row
height.

### Views remember their state

**Views come back the way the user left them.** When a user switches away from a
tool and returns, or reopens the project, each view shows what it showed before:
the same record or item selected and scrolled into view, the same choices of
what to show and how (columns, sort, filter, expanded sections, the line choices
in an interlinear text, the last kind of bulk change used). Tools that work on
the same records share the current record, so switching tools during a task
keeps the user's place; how each tool shows those records belongs to that tool.
These settings are kept per project and per user. Anything remembered can be
returned to its default from where it was set (Show All in a filter, the default
sort, the default columns).

### Long-running work

**Long work never freezes the window.** Work that takes more than a moment says
in words what it is doing. When the amount of work is known, show specific
progress ("Parsing word 120 of 500" with a bar that fills); when it isn't, show
an indeterminate bar. Offer Cancel when stopping is safe, and say when the work
has finished or failed rather than just letting the bar disappear. Reuse the
existing progress classes (`ProgressDialogWithTask` / `IThreadedProgress` for
blocking work, `StatusBarProgressHandler` for the status bar) rather than
writing a new one.

## Dialogs

### Dialog buttons

**Dialog buttons sit in one place and behave one way.** Buttons that close the
dialog form one group at the bottom right, in this order: the confirming button,
named for its action where that's clearer than OK (Delete, Merge, Create); any
alternative ways to confirm (Create and Edit); Cancel; Apply, if the dialog has
it; then Help when a help topic exists. Buttons that act without closing the
dialog (Browse, Restore Defaults, Select All, More) do not join that group:
place them next to the content they affect, or at the bottom left. Enter
activates the confirming button and Esc cancels, except in a confirmation that
deletes data or can't be undone: there Cancel has the focus and no button is
the default, so Enter does not delete. A dialog that only shows
information, or applies changes immediately, offers Close instead of OK and
Cancel, and Esc closes it. Buttons share one standard size from the theme.

### Dialog windows

**Dialogs are compact, owned windows.** A dialog belongs to the window that
opened it: it opens centered on that window, stays in front of it, has no
minimize or maximize button, and doesn't appear separately in the taskbar. It
shows the FieldWorks icon. Layout is compact, matching the density users know
from FieldWorks rather than roomy default spacing. A dialog whose content can
grow (lists, trees, previews, long text) is resizable, has a minimum size at
which every control stays visible and usable, and remembers its size and
position, always reopening fully on screen; a dialog with fixed content is not
resizable. Every screen is checked at 100% and 150% display scaling before it
ships.

### Modal and modeless windows

**A converted window blocks the rest of FieldWorks exactly when its WinForms
version did.** In WinForms almost every dialog is modal: a dialog that asks for
input, offers a choice, confirms an action or changes settings blocks the window
that opened it until it closes, and its result takes effect then. A window is
modeless only when the user works with it alongside the main window: a tool
that acts on the content behind it (Find and Replace, find in an HTML view), or
a companion window kept open during work (Try A Word, parser reports). Opening
a modeless tool again brings the open one forward instead of starting a second.
Progress windows follow *Long-running work*. An Avalonia window that changes
its modality from the WinForms one needs a decision register entry.

### Checking input

> **Needs team consensus** (UX-6). Intentional departure from WinForms;
> modernization.

**Show problems where they are, and never lose the user's input.** When
something the user entered can't be accepted, say so next to that field, in
plain words that say how to fix it, and keep the dialog open with everything the
user typed intact. Check a field when the user leaves it or presses the
confirming button, not on every keystroke. Disable the confirming button only
when the reason is obvious from what's on screen (for example, nothing is
selected yet in a chooser); otherwise leave it enabled and explain the problem
when it is pressed. Use a separate message box only to confirm a consequential
action (such as deleting) or for a problem that isn't about any one field.

### Help

**Help is one keystroke away and goes to the right page.** A dialog with a help
topic shows a Help button, and F1 opens the same topic. Help opens the page for
this dialog (or the field, from a detail-view row), never a generic start page.
A dialog with no help topic shows no Help button rather than a disabled one. The
title-bar "?" button is not used.

### Advanced options

> **Needs team consensus** (UX-7). Replaces five WinForms patterns with one.

**Advanced options use one pattern.** A dialog that has options most users don't
need opens showing only the common ones, with a single "More options" expander
that reveals the rest in place. The expander looks and behaves the same in every
dialog, and the dialog remembers whether the user left it open. If any advanced
option is already set to something other than its default, the advanced section
opens automatically, so a user never has a hidden setting affecting their work.
Wizards mark optional steps as optional and let the user finish without them.

### Choosers

**Choosers work the same everywhere.** Choosing from a list (a category, a
semantic domain, a variant type) always uses the same chooser: a searchable list
or tree, with the current value visible, OK enabled once something is chosen,
and its size and position remembered. A chooser that picks from a user-editable
list offers a link to edit that list, always worded "Edit the {list name} list".
In a field that holds several items, the chooser adds items; items already in
the field are shown but can't be picked again, and items are removed from the
field itself, not from the chooser.

## Browse views

### Browse view criteria

**Browse view criteria.** A browse view is a table of records, one per row,
chosen when seeing many records side by side helps the user. Choose the layout
by the task:

- **Hierarchical records with little to compare** (lists, categories): no browse
  view; the records appear as a tree in the sidebar beside the detail view.
- **Pick a record, then edit it:** a narrow browse view beside the detail view,
  showing only the few columns needed to find the record. Full editing happens
  in the detail view.
- **Compare or survey many records across many fields:** a full-width browse
  view. Simple text fields can be corrected in place in their cells, so the user
  can fix what they spot without leaving the table; anything more involved is
  edited in the detail view. The full-width browse tool and its editing tool
  share one list of records, so the current record, sort and filter carry across
  when the user switches.
- **Enter many records quickly:** a browse view whose cells are the entry form
  (Collect Words).
- **Change many records at once:** a full-width Bulk Edit tool. Its cells are
  read-only; changes go through the checkbox column and the bulk edit bar, so a
  whole-list change is always deliberate.
- **Read a flowing document or a text:** a document view, not a browse view.

In-place editing in a cell uses the same editor, validation and undo as that
field in the detail view.

### Columns

**Columns start few and grow on request.** A browse view opens with a small
default set of columns: just enough to recognize and find the record, and for a
narrow browse view beside a detail view, fewer still. All other columns stay
available through the column chooser: frequently wanted columns appear directly
in its quick menu, and the rest are behind "More Column Choices…". The user's
choice of columns, their order and their widths are remembered for each tool in
each project. A column that shows text in one writing system displays that
writing system's data. When the user switches it to a different writing system,
the header says which one, using its abbreviation. Every header is a localized
label, and a shorter localized form may be used where the full label doesn't
fit.

### Sorting

**Sorting is done from the column headers.** Clicking a column header sorts by
that column, and clicking it again reverses the order. Shift+click adds the
column as a further sort key, and each sorted header shows its direction and its
rank. Sort options that only make sense for language data, such as sorting from
the end of the word or by length, sit in the header's context menu. Text columns
sort by their writing system's collation, not by the order of the character
codes. The sort is remembered with the list of records, so it survives restarts
and is shared by every tool that uses that list.

### Filter bar

**Lists of records can be filtered from the columns.** A browse view that shows
a tool's main list of records has a filter bar under the headers, with one
filter cell per column; lists inside choosers and dialogs don't, since they have
their own search box. Every filter cell offers the same menu in the same order:
Show All, then Blanks and Non-blanks, then options that suit the column's kind
of data (for example a list chooser for a list field, or a range for a number),
then "Filter for…" to type a pattern. Typed patterns are matched in the column's
writing system. Filters in several columns combine: a record shows only if it
passes all of them. Like the sort, the filter is remembered with the list of
records.

### Bulk Edit

**Bulk changes are previewed, deliberate and undoable.** Changes to many records
at once happen only in a Bulk Edit tool, never as a side effect of an ordinary
browse or detail view. The user first narrows the records by filtering and
checking rows, then chooses the change; a change applies only to the checked
rows that pass the current filter. Kinds of change that don't apply to the
chosen field are shown as unavailable rather than hidden. Every change except
delete can be previewed in the table before it's applied; the preview is clearly
marked as not yet saved and can be cleared. The command that applies the change
names what it will do. Deleting records asks for confirmation and says how many
records will be deleted. A whole bulk change, including a delete, is one undo
step; if some change can't be undone, the confirmation says so before anything
happens.

### Checkboxes

**Checkboxes.** Where the user checks rows to choose what an action affects,
rows start checked when the action normally applies to the whole list, and
unchecked when the user is picking a few items out of many.

### Current record and keyboard

**The current record is always visible and easy to move.** The browse view marks
the current record's row so it stands out at a glance, using a marker that
doesn't hide the text being edited in an editable list. Whenever the current
record changes, by any route, its row is scrolled into view. In a list used to
pick records, the arrow keys move to the previous or next record while focus
stays in the list, so the user can step through records and watch the detail
view change; Home, End, Page Up and Page Down move to the first record, the last
record or a page at a time. The record navigation commands (First, Previous,
Next, Last) are always available from the menu and keep their WinForms
shortcuts.

## Detail views

### What a detail view is

**What a detail view is.** A detail view edits one record: the record currently
selected in the tool's list of records, whether that list is shown as a browse
view beside it, a tree in the sidebar, or not at all. It is the full editing
form for that record, laid out as rows: each row has a label on the left and the
field's value on the right, and related rows are grouped under section headers
(Senses, Allomorphs, Etymology). Owned records, such as the senses of an entry
or the examples of a sense, appear nested inside it rather than in a separate
window. Selecting a different record in the list replaces the detail view's
contents; there is no Save step. Every change is kept as the user makes it, and
it can be undone.

### Field visibility by area

**Initial field visibility is predetermined by area: show what will matter, and
let the user choose.** Each area decides which fields its detail views show at
first, aiming to show the fields a user needs to make a usable record there,
plus every field that already holds data. Fields that only some users need
appear when they hold data, or when the user turns on Show Hidden Fields for
that tool. Show Hidden Fields is off by default, is found in the same place in
every tool, and is remembered per tool. A field is never hidden in a way the
user can't undo: anything hidden can be shown again, either with Show Hidden
Fields or by changing that field's visibility. When a tool is converted, its
fields keep their WinForms visibility; how much an area hides is that area's
decision, not something to make uniform.

### Field control

**Users have field-by-field control of visibility and location.** Every row in a
detail view offers the same field commands in the same place at the end of its
context menu: set the field's visibility (always visible, hidden unless it has
data, or normally hidden), move the field up or down among its neighbors, and
Help when a topic exists. These choices apply to that field in that tool
everywhere it appears, not just to the current record, and are remembered. The
wording of the visibility choices is the same everywhere, so the user learns it
once. A user who has rearranged or hidden fields can return a tool's form to its
defaults.

### Sections and expansion

**Sections organize the form and can be folded away.** Groups of related rows
sit under a section header that stands out from ordinary rows, with clear space
above it, so the user can scan the form by section. A section or nested record
that can be expanded shows whether it is expanded, starts expanded, and can be
collapsed or expanded by mouse or keyboard. Collapsing something applies to that
record only, and the record remembers it. The current row's section header is
where that section's commands appear.

### Labels

**Labels line up, stay readable, and can be narrowed.** All rows in a detail
view share one label column, and nested rows indent their labels to show the
nesting. The user can drag the column narrower or wider for all rows at once,
and the width is remembered per tool. When the column gets too narrow for full
labels, each label switches to a short localized abbreviation, and its tooltip
always gives the full label. Field labels use Title Case ("Lexeme Form",
"Grammatical Info.").

### Fields in several writing systems

**Each writing system gets its own line.** A field that holds text in several
writing systems shows one line per writing system, each starting with that
writing system's abbreviation, so the user always knows which language they're
typing in. Each line follows its writing system's direction and font. By default
the field shows the project's active writing systems that apply to it; a writing
system that already holds data in the field always shows, and at least one
always shows. From the field's menu the user can show all of them for now, or
choose which ones the field shows from then on. Where a field mixes vernacular
and analysis writing systems, vernacular comes first.

### Saving and undo

> **Needs team consensus** (UX-8). Last sentence: field-level problem display
> replaces WinForms message boxes.

**Edits save themselves, one undoable step per field.** There is no Save button
in a detail view. A change to a field is committed when the user leaves that
field, moves to another record or tool, or runs a command, and becomes its own
undo step, named for the field ("Undo change Gloss"), so Undo reverses exactly
one field at a time. While the user is typing in a field, Undo works within the
text first. If a value can't be accepted, the problem is shown at that field in
plain words, the user's typing is kept, and focus stays there until it's fixed
or the change is undone, as in *Checking input* for dialogs. A command that
can't be undone says so before it runs.

### Tab and focus

**Tab moves through the fields in order, and focus lands where the user
expects.** Tab and Shift+Tab move from field to field in the order they appear,
including into nested records, and through each writing system's line in a
multilingual field. A field that holds several items (references, environments)
is one Tab stop: Tab lands on its first item, the arrow keys move between items
(following the row's direction), Home and End jump to the ends, and items that
can be reordered move with Ctrl+arrow. The field's add and configure buttons are
their own stops after the items. Expand and collapse markers, labels and the
row's menu button aren't Tab stops. At the first and last field, Tab carries on
into the rest of the window as it does elsewhere. Opening a record puts the
cursor in its first editable field. After a command that changes the form
(inserting a sense, for example), the cursor goes to the new item's first field;
after any other refresh it stays in the field the user was in.

### Current row

**The user can always see where they are, without the form shouting.** The field
being edited is clearly marked by its edit box's focus treatment, and the
current item in a field that holds several items is marked as selected. Rows are
not filled or highlighted as a whole; the form stays calm so the data reads
easily. When the current field is inside a collapsed section, the section's
header shows that it contains it. The current row's section shows its commands,
as in *Sections and expansion*.

## Interlinear text

### Analysis trust colors

**Colors tell the user how much to trust an analysis.** In every view of
interlinear text, an analysis is shaded to show where it came from: one color
for a guess based on the user's earlier approvals, another for a machine guess,
another where several approved analyses could apply, and no shading once the
user has approved it. The same meaning has the same color on every interlinear
surface (text, the word being analyzed, choices, concordance), and these colors
aren't used for anything else, such as selection or highlighting elsewhere in
the product. Shading is never the only signal: the analysis's state can also be
read from its tooltip or by assistive tools.

### Analysis commits

**Analysis work is never lost, and approving is always a choice.** Whatever the
user types while analyzing a word is kept when they move to another word, by any
route. Approving an analysis, which turns a guess into the user's own analysis,
is a separate, deliberate action. Every command that moves to another word comes
in a pair: the plain form approves and moves; the same keys with Shift move
without approving. Each commit is one undo step. While analyzing a word, Tab
moves between its lines, and the same key opens the list of choices on every
line.

### The word being analyzed

**It's always clear which word is being analyzed.** The word the user is working
on is set apart from the surrounding text, so it's clear at a glance which word
the user's typing and choices apply to. Within it, it's clear which lines the
user can type in and which only display information. Every line that offers a
list of choices shows that it does, in the same way. Where several approved
analyses could apply, the user can see how many.

### Interlinear lines

**Each interlinear task starts with the lines it needs, and the user can change
them.** A new user glossing a text sees only a few lines (the word, its gloss,
its category, a free translation); morpheme-by-morpheme analysis lines appear by
default only where that work is done. The user can show, hide and add lines, and
choose which writing systems each line shows; those choices are remembered
separately for each task. However the user makes these changes, the result
always keeps this shape:

- At least one line always shows, and a view where the user analyzes words
  always shows the word itself in the default vernacular writing system.
- Lines keep a fixed order by kind: word lines first, then the morpheme lines
  together as one block, then the lines about the whole sentence (free
  translation, literal translation, notes). Only the writing-system lines of the
  same field can be reordered among themselves, and they stay together.
- The same line can't appear twice in the same writing system.

Each line's label says which line it is and, where it matters, which writing
system it shows.

## Decision register

Each entry has one status:

- **Adopted**: decided; change it only by adding a new entry that supersedes it.
- **Provisional**: in use, but expected to be revisited; it names the trigger
  for revisiting.
- **Temporary**: a known gap or compromise; it names the condition that ends
  it.

A superseded entry stays in the table, marked as superseded and pointing to the
entry that replaces it.

| ID | Decision | Status | Revisit trigger / end condition | Decided |
| --- | --- | --- | --- | --- |
| UX-1 | Empty or appendable sequences in detail views show a "Type to add {item}" row as an extra in-place way to add; insert commands stay in the menus and section commands. | Adopted | — | 2026-08-11, LT-22687 |
| UX-2 | Sections that offer commands show them on the section header at all times (see *Sections and expansion*). | Adopted | — | 2026-08-11, LT-22687 |
| UX-3 | Section commands appear as one "Field options" link per section that opens a menu (WinForms shows one link per command), and that link is a Tab stop. | Provisional | LT-22693 | 2026-08-11, LT-22687 |
| UX-4 | Edit boxes in detail views have a visible border. | Provisional | A Jira issue or user-testing report asks to remove the border | 2026-08-11, LT-22687 |
| UX-5 | Tab stops at the first and last detail-view row instead of carrying on into the rest of the window. | Temporary | The whole window is Avalonia (LT-22688) | 2026-09-22, commit 23965cd67 |
| UX-6 | Input problems show next to the field, in plain words, keeping the user's input; WinForms message boxes are not used for them (see *Checking input*). | Provisional | Team consensus on LT-22687 | 2026-10-23, LT-22687 |
| UX-7 | Advanced options use one "More options" expander, replacing five WinForms patterns (see *Advanced options*). | Provisional | Team consensus on LT-22687 | 2026-10-23, LT-22687 |
| UX-8 | Detail views show field problems at the field, as in UX-6, not in message boxes (see *Saving and undo*). | Provisional | Team consensus on LT-22687 | 2026-10-23, LT-22687 |

## Appendix: terms and where they live in code

The guide uses terms from the user's point of view. This table connects them to
code, for developers and AI agents.

| Guide term | WinForms code | Avalonia code |
|---|---|---|
| list of records (shared by tools) | `RecordClerk` / `RecordList` (xWorks) | same `RecordClerk`, used by the Avalonia `RecordEditView` host |
| browse view | `BrowseViewer`, `XmlBrowseViewBase` | — |
| detail view | `DataTree` (DetailControls), `RecordEditView` | `DataTree` (FwAvalonia/Detail), `DetailComposer`, `DetailModel`, `RecordEditView.Avalonia` |
| row / field (detail view) | `Slice` | detail row (`DetailComposer` output) |
| section, section header | heavy `SummarySlice`, `header="true"` parts | section header row |
| section commands | `hotlinks=` on a part, `SummaryCommandControl` | `CreateHotlinkStrip` ("Field options") |
| field visibility | part `visibility` (`always` / `ifdata` / `never`) and layout overrides | same layouts, read by `DetailComposer` |
| "Type to add ..." row | `GhostStringSlice` | ghost row |
| filter bar | `FilterBar` | — |
| bulk edit bar | `BulkEditBar` | — |
| the word being analyzed | focus box: `SandboxBase`, `FocusBoxController` | — |
| interlinear lines | `InterlinLineChoices` | — |
| theme values | system colors and literals | design tokens (ADR 0001/0002) |

A "—" means the area isn't converted yet; the converting change fills in the
cell.
