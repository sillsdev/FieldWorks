---
applyTo: "**/*.cs"
name: "dispose.instructions"
description: "IDisposable patterns and debugging tips for FieldWorks"
---

# Dispose Patterns

## Purpose & Scope

This document describes how to properly implement and debug IDisposable patterns in FieldWorks. Improper disposal handling has historically caused hanging tests and application shutdown issues.

## The Problem

Failing to call `Dispose()` on disposable objects can cause:

1. **Non-deterministic finalization** - Objects finalize in random order during GC, causing problems on some machines but not others
2. **Resource leaks** - Adding code that requires disposal to an existing IDisposable class, assuming Dispose is being called
3. **Hanging tests** - COM objects holding references to managed objects that aren't released
4. **Application won't exit** - Background threads or resources preventing clean shutdown

## Rules

### 1. Dispose Owned Objects

If a class **owns** disposable member variables, it MUST:
- Implement `IDisposable`
- Call `Dispose()` on owned members in its `Dispose(bool)` method

### 2. Avoid Unnecessary IDisposable

Try to avoid implementing IDisposable unless truly necessary.

### 3. Single Owner

Each disposable object should have **exactly one owner** responsible for disposing it. Other classes should only hold references.

### 4. Document Ownership

Add comments for methods/constructors that accept disposable parameters:

```csharp
/// <summary>
/// Creates a new handler.
/// </summary>
/// <param name="stream">The stream to use. This class takes ownership and will dispose it.</param>
public Handler(Stream stream) { ... }

/// <summary>
/// Sets the cache.
/// </summary>
/// <param name="cache">The cache to reference. Caller retains ownership.</param>
public void SetCache(ICache cache) { ... }
```

### 5. Use Container Collections

For objects that need to stay around without dedicated member variables:
- Use `System.ComponentModel.Container` (if deriving from Component)
- Use `SIL.Utils.DisposableObjectsSet`

### 6. COM Object Caution

Be careful when passing IDisposable managed objects to COM:
- If COM stores the reference, release the COM object when disposing the managed object
- Failure to do this can cause deadlocks during GC

## Implementation Pattern

```csharp
public class MyClass : IDisposable
{
    private static int s_count;
    private int m_number;
    private Stream m_ownedStream;

    public MyClass()
    {
        m_number = ++s_count;
        Debug.WriteLine($"Creating {GetType()} number {m_number}");
    }

#if DEBUG
    ~MyClass()
    {
        Dispose(false);
    }
#endif

    public bool IsDisposed { get; private set; }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        Debug.WriteLineIf(!disposing,
            $"***** Missing Dispose() call for {GetType()}. *****");
        Debug.WriteLine($"Disposing {GetType()} number {m_number}");

        if (disposing)
        {
            // Dispose managed resources
            m_ownedStream?.Dispose();
        }

        // Release unmanaged resources (if any)

        IsDisposed = true;
    }
}
```

## Detecting Missing Dispose Calls

The "Missing Dispose" debug message appears when an object is finalized instead of disposed:

```
***** Missing Dispose() call for MyClass. *****
```

### What To Do

1. **During tests**: Find and fix the missing Dispose call before checking in
2. **During application run**: Verify your changes properly dispose objects. Some messages during application run are acceptable, but investigate new ones.

**DO NOT** comment out the "Missing Dispose" line - it helps find real problems.

## Debugging Tips

### Finding the Specific Instance

Add instance tracking to help identify which object wasn't disposed:

```csharp
private static int s_count;
private int m_number;

public MyClass()
{
    m_number = ++s_count;
    Debug.WriteLine($"Creating {GetType()} number {m_number}");
}
```

Then set a conditional breakpoint when `m_number` matches the undisposed instance.

### Finding Creation Location

Add a stack trace capture to find where objects are created:

```csharp
private string m_creationStackTrace = Environment.StackTrace;

protected virtual void Dispose(bool disposing)
{
    Debug.WriteLineIf(!disposing,
        $"Object creation path:{Environment.NewLine}{m_creationStackTrace}");
    // ... rest of dispose
}
```

### Using undisposed-fody

For automated detection, see [undisposed-fody](https://github.com/ermshiperete/undisposed-fody).

### Common Cause: Double Assignment

Missing dispose often occurs when a variable is assigned twice:

```csharp
m_window = new XWindow();  // First assignment
m_window = new XWindow();  // Second assignment - first window not disposed!
```

A single `m_window.Dispose()` only disposes the second instance.

### Running Tests from Command Line

If missing dispose errors don't appear in the IDE:

```bash
dotnet test Output/Debug/xWorksTests.dll --filter "FullyQualifiedName~SIL.FieldWorks.XWorks"
```

## See Also

- [managed.instructions.md](managed.instructions.md) - C# development guidelines
- [testing.instructions.md](testing.instructions.md) - Testing guidelines
