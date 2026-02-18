"""FieldWorks test exclusion tooling.

This package centralizes the shared helpers used by the
`audit_test_exclusions.py`, `convert_test_exclusions.py`, and
`validate_test_exclusions.py` CLIs. Modules inside this package purposely
avoid external dependencies so they can run on any FieldWorks developer
machine without additional installs.
"""

from __future__ import annotations

__all__ = [
    "__version__",
]

# Bump the version when we ship user-facing changes to the scripts. The
# string is informational only and does not map to a published package.
__version__ = "0.2.0-dev"
