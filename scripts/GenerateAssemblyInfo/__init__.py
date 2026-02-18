"""FieldWorks GenerateAssemblyInfo convergence package.

This namespace hosts the audit/convert/validate automation that:
1. Scans every managed .csproj to detect CommonAssemblyInfoTemplate usage.
2. Applies scripted fixes (template linking, GenerateAssemblyInfo toggles, file restoration).
3. Validates repository compliance and emits review-ready artifacts under Output/GenerateAssemblyInfo/.

All entry points follow the CLI patterns documented in specs/002-convergence-generate-assembly-info/quickstart.md.
"""
