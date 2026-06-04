# MCP Driver Selection

FieldWorks is currently a WinForms application. Use WinForms MCP as the default
driver for fresh live-app automation, and keep WinApp MCP as the UIA3 fallback
and visible-desktop diagnostic path.

## Default: WinForms MCP UIA2

Use WinForms MCP when:

- launching FieldWorks yourself for verification or screenshot evidence;
- the task can run in the background on a hidden desktop;
- the target is a WinForms menu, dialog, button, tab, text box, combo box, or
  standard control;
- you need `render_form` to preview a `.Designer.cs` surface;
- foreground-window stealing would disrupt the developer.

Preferred tools:

- process: `winforms_launch_app`, `winforms_attach_to_process`,
  `winforms_close_app`, `winforms_get_process_status`;
- discovery: `winforms_get_element_tree`, `winforms_find_element`,
  `winforms_wait_for_element`, `winforms_element_exists`;
- interaction: `winforms_click_menu_item`, `winforms_click_element`,
  `winforms_type_text`, `winforms_set_value`, `winforms_select_item`;
- evidence: `winforms_take_screenshot`, `winforms_render_form`.

Headless-safe rule: prefer UIA pattern operations. Avoid `winforms_send_keys`,
drag/drop, and double-click against headless processes because those rely on
input simulation on the visible desktop.

## Fallback: WinApp MCP UIA3

Use WinApp MCP when:

- WinForms MCP is not configured in the current agent client;
- the task is about foreground, focus, taskbar, window ordering, or another
  visible desktop behavior;
- a control, popup, or non-WinForms surface is missing or unreliable through
  WinForms MCP;
- you need UIA3 behavior for comparison or troubleshooting.

Useful tools include `mcp_winapp_launch_app`, `mcp_winapp_attach_to_app`,
`mcp_winapp_get_snapshot`, `mcp_winapp_find_elements`,
`mcp_winapp_click_element`, `mcp_winapp_invoke_element`,
`mcp_winapp_select_option`, and `mcp_winapp_take_screenshot`.

## Dynamic Choice Procedure

1. Decide whether the task requires visible desktop behavior.
2. If not, choose WinForms MCP and launch FieldWorks headless.
3. Keep the returned process ID for screenshots and cleanup.
4. Inspect the tree before interacting.
5. If an element cannot be found or invoked through WinForms MCP, switch to
   WinApp MCP for that route and record the fallback in the navigation file.
6. If the task needs human-visible screenshots or focus behavior, start with
   WinApp MCP.

## Recording Findings

When updating a navigation path, record:

- which driver was verified;
- whether the process was launched headless or attached visibly;
- stable automation IDs/names;
- the successful tool sequence;
- any driver-specific failure and the fallback that worked.