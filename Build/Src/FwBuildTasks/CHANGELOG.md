# Change Log

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/)
and this project adheres to [Semantic Versioning](http://semver.org/).

<!-- Available types of changes:
### Added
### Changed
### Fixed
### Deprecated
### Removed
### Security
-->

## [Unreleased]

### Changed

- Build for .Net Framework v4.6.1

## [10.0.0] - 2021-03-05

### Changed

- Create nuget packages
- Projects that require access to the classes defined in FwBuildTasks should import the
  `SIL.FwBuildTasks.Lib` nuget package, all others `SIL.FwBuildTasks`.
- Build for Netstandard 2.0

### Fixed

- `RegFree` task - don't fail if `Platform` is not specified. In this case just use the
  bitness of the currently running process.
