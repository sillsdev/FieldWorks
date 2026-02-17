# VwDrawRootBuffered Buffering Fix

## Problem
The VwDrawRootBuffered class had a critical bug in its GDI resource management that caused:
1. **Resource leaks**: The new bitmap created for double buffering was never deleted
2. **Visual artifacts**: The old bitmap handle was deleted instead of being properly restored
3. **Incorrect cleanup**: The destructor was left cleaning up a DC with a corrupted bitmap state

## Root Cause
The bug was in the bitmap selection and cleanup code:

```cpp
// WRONG - Old buggy code:
HBITMAP hbmpOld = AfGdi::SelectObjectBitmap(m_hdcMem, hbmp);
fSuccess = AfGdi::DeleteObjectBitmap(hbmpOld);  // BUG: Deleting the OLD bitmap!
Assert(fSuccess);
// ... draw and blit ...
// BUG: Never cleanup the NEW bitmap (hbmp)
```

When `SelectObjectBitmap` is called, it:
1. Selects the new bitmap (`hbmp`) into the DC
2. Returns the **old** bitmap that was previously in the DC (typically a stock 1x1 monochrome bitmap)

The bug was deleting this old bitmap immediately, which is wrong because:
- Stock GDI objects shouldn't be deleted (may cause issues)
- The new bitmap (`hbmp`) was never deleted, causing a resource leak
- The DC was left with no valid bitmap to restore to

## Solution

### DrawTheRoot (with ReDrawLastDraw support)
Keeps the bitmap cached in `m_hdcMem` for potential `ReDrawLastDraw` calls:

```cpp
// Clean up any previous cached bitmap and DC
if (m_hdcMem)
{
    HBITMAP hbmpCached = (HBITMAP)::GetCurrentObject(m_hdcMem, OBJ_BITMAP);
    if (hbmpCached)
        AfGdi::DeleteObjectBitmap(hbmpCached);  // Delete the previous cached bitmap
    AfGdi::DeleteDC(m_hdcMem);
    m_hdcMem = 0;
}

// Create new memory DC and bitmap
m_hdcMem = AfGdi::CreateCompatibleDC(hdc);
HBITMAP hbmp = AfGdi::CreateCompatibleBitmap(hdc, width, height);
HBITMAP hbmpOld = AfGdi::SelectObjectBitmap(m_hdcMem, hbmp);
// Don't delete hbmpOld - it's the stock bitmap from the new DC

// ... draw to m_hdcMem ...

// Blit to screen (bitmap stays in m_hdcMem for ReDrawLastDraw)
::BitBlt(hdc, ..., m_hdcMem, ...);
```

### DrawTheRootRotated (local resources)
Uses local DC/bitmap since rotation makes caching impractical:

```cpp
// Create local memory DC and bitmap
HDC hdcMem = AfGdi::CreateCompatibleDC(hdc);
HBITMAP hbmp = AfGdi::CreateCompatibleBitmap(hdc, width, height);
HBITMAP hbmpOld = AfGdi::SelectObjectBitmap(hdcMem, hbmp);
// Don't delete hbmpOld - it's the stock bitmap from the new DC

// ... draw to hdcMem ...

// Blit rotated to screen
::PlgBlt(hdc, ..., hdcMem, ...);

// Clean up local resources
AfGdi::SelectObjectBitmap(hdcMem, hbmpOld, AfGdi::OLD); // Restore stock bitmap
AfGdi::DeleteObjectBitmap(hbmp);  // Delete our custom bitmap
AfGdi::DeleteDC(hdcMem);
```

### DrawTheRootAt (already correct)
This method already had the correct pattern - it was used as a reference for the fix.

## Key Points

1. **Stock bitmap handling**: When a new DC is created, it comes with a default 1x1 stock bitmap. When we select our custom bitmap, the stock bitmap is returned but we **do not** delete it. Stock GDI objects should not be deleted by applications. Instead:
   - For cached DCs (`DrawTheRoot`): Leave the custom bitmap selected; it will be deleted on next draw or in destructor
   - For local DCs (`DrawTheRootRotated`, `DrawTheRootAt`): Restore the stock bitmap before deleting the DC

2. **Resource ownership**: 
   - `DrawTheRoot`: Keeps the custom bitmap selected in `m_hdcMem` for caching
   - `DrawTheRootRotated`: Uses local DC, restores stock bitmap, deletes custom bitmap and DC
   - `DrawTheRootAt`: Uses local DC, restores stock bitmap, deletes custom bitmap and DC

3. **Exception safety**: Added proper cleanup in catch blocks for `DrawTheRootRotated` to prevent leaks on exceptions. The stock bitmap is restored and the custom bitmap is deleted before rethrowing.

4. **ReDrawLastDraw**: This optimization re-blits the cached bitmap when the form is disabled, avoiding a full redraw. It requires `m_hdcMem` to be persistent with a valid custom bitmap selected.

## Testing
The fix should be tested by:
1. Running FieldWorks applications and checking for visual artifacts during text rendering
2. Monitoring GDI object counts to ensure no resource leaks
3. Testing the disabled form scenario (which uses ReDrawLastDraw)
4. Testing rotated views if any exist in the application

## References
- VwRootBox.cpp: Implementation of VwDrawRootBuffered
- VwRootBox.h: Class declaration
- ManagedVwDrawRootBuffered/VwDrawRootBuffered.cs: Managed version (reference implementation)
- SimpleRootSite.cs: Uses ReDrawLastDraw for disabled form optimization
