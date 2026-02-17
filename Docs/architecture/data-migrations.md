# Data Migrations Guide

This document describes the principles for creating and maintaining data migrations in FieldWorks. Data migrations are essential for evolving the data model while preserving existing user data.

> **Note**: Data migration code lives in the [liblcm](https://github.com/sillsdev/liblcm) repository, not in FieldWorks. This guide covers the conceptual process; see liblcm for implementation details.

## Overview

Data migrations transform data from one model version to another. Every change to the FieldWorks data model requires a corresponding data migration.

## Critical Requirements

### For Every Data Migration in FieldWorks:

#### a) FLEx Bridge Metadata Cache Migration

**There MUST be a corresponding metadata cache migration in FLEx Bridge.**

If you don't create this, you won't be able to use FLEx Bridge Send/Receive with projects that have been migrated. Contact the FLEx Bridge team if you're unsure how to do this.

#### b) Initialize C# Value Type Properties

**Any new CmObjects MUST have all C# value type data properties added for the new instance.**

The current data types that must have explicit property elements are:
- `int`
- `bool`
- `Guid`
- `DateTime`
- `GenDate` (stored as int)

## Creating a Data Migration

### Step 1: Update the Model Version

In `Src/FDO/MasterFieldWorksModel.xml`:

1. **Change the version number** (e.g., 7000065 to 7000066):
   ```xml
   <EntireModel xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
                version="7000066"
                xsi:noNamespaceSchemaLocation="MasterFieldWorksModel.xsd">
   ```

2. **Add a change history comment** near the top with:
   - Date of the change
   - Model version number
   - Short description of what changed

3. **If the model structure changed**, add the model changes (e.g., new properties, renamed fields)

### Step 2: Update Related Files

#### HandGenerated.xml (if needed)

For special cases, update `Src/FDO/FDOGenerate/HandGenerated.xml`.

#### NewLangProj.fwdata

Update `DistFiles/Templates/NewLangProj.fwdata`:
- Change the version number at the top of the file
- **Don't forget this step!** You will regret it.

> **Tip**: Create a new project in FLEx with debugging attached, stop after the DM runs but before anything else is added, and copy the resulting fwdata file.

#### FdoDataMigrationManager.cs

In `Src/FDO/DomainServices/DataMigration/FdoDataMigrationManager.cs`, add a line like:

```csharp
m_individualMigrations.Add(7000066, new DataMigration7000066());
```

> **Note**: Some migrations are "do-nothing" deals that serve purposes other than actual data transformation. Those share a common implementation that only increments the version number.

### Step 3: Create Migration Files

Create three new files (copy from a previous version as a template):

#### Test Data File
`Src/FDO/FDOTests/TestData/DataMigration7000066Tests.xml`

A stripped-down project containing the pertinent objects needed to test the migration. Include only the minimum data needed to make tests pass.

#### Test Class
`Src/FDO/FDOTests/DataMigrationTests/DataMigration7000066Tests.cs`

Tests that demonstrate the migration works correctly:

```csharp
/// <summary>
/// Test the migration from version 7000065 to 7000066.
/// </summary>
[TestFixture]
public class DataMigration7000066Tests : DataMigrationTestsBase
{
    [Test]
    public void PerformMigration()
    {
        // Arrange
        var dtoRepos = CreateDtoRepository(7000065);

        // Act
        m_dataMigrationManager.PerformMigration(dtoRepos, 7000066);

        // Assert
        Assert.AreEqual(7000066, dtoRepos.CurrentModelVersion);
        // Add specific assertions for your migration
    }
}
```

#### Migration Implementation
`Src/FDO/DomainServices/DataMigration/DataMigration7000066.cs`

The code that performs the actual migration:

```csharp
internal class DataMigration7000066 : IDataMigration
{
    public void PerformMigration(IDomainObjectDTORepository domainObjectDtoRepository)
    {
        // Step 1: Verify version
        DataMigrationServices.CheckVersionNumber(domainObjectDtoRepository, 7000065);

        // Step 2: Perform the migration
        // ... your migration code here ...

        // Step 3: Update version
        DataMigrationServices.IncrementVersionNumber(domainObjectDtoRepository);
    }
}
```

## Migration Code Guidelines

### Always Check Version First

The first step in every migration must verify the current version number. Migrations must never be applied out of order.

### Use Minimal Test Data

Test data files don't need a complete projection of all properties. Include only what's necessary to make tests pass.

### Finding Affected Code

Search for the previous version number to find all places that need updating:

```
Find all "7000065", Match case, Whole word, Subfolders
Look in: Src/FDO
File types: *.cs; *.xml
```

## Files Changed in a Typical Migration

| File | Change Required |
|------|-----------------|
| `MasterFieldWorksModel.xml` | Update version, add change history, add model changes |
| `HandGenerated.xml` | Update if needed for special cases |
| `NewLangProj.fwdata` | Update version number |
| `FdoDataMigrationManager.cs` | Add migration registration |
| `DataMigration7000066.cs` | **NEW** - migration implementation |
| `DataMigration7000066Tests.cs` | **NEW** - migration tests |
| `DataMigration7000066Tests.xml` | **NEW** - test data |

## Testing Your Migration

1. Run the migration tests to verify the code works
2. Create a new language project to verify `NewLangProj.fwdata` is correct
3. Open an existing project to verify the migration runs correctly
4. Test with FLEx Bridge Send/Receive if applicable

## See Also

- [Dependencies on Other Repos](dependencies.md) - Related repository information
- [Commit message guidelines](../../.github/commit-guidelines.md) - CI-enforced commit rules
