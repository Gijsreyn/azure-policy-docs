<!-- markdownlint-configure-file {"MD024": {"siblings_only": true}} -->

# Changelog

All notable changes to this project are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the project uses
[Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added

- Pinned CSharpier local tool with shared EditorConfig settings and required formatting checks in the build and release workflows.
- Regression tests for glob splitting, filesystem roots, ordinal input ordering, and null rendering models.

### Changed

- Target .NET 10 and its default C# 14 language version, and select the .NET 10.0.303 SDK with stable 10.0 feature-band roll-forward. The library and CLI now require .NET 10 or later.
- Use span-based glob parsing and direct ordinal ordering of matched paths.

### Fixed

- Preserve the filesystem root when resolving patterns such as `/**/*.json`.
- Report null rendering models with `ArgumentNullException` instead of an incidental null-reference failure.

## [0.1.0] - 2026-10-09

### Added

- `policydocs generate`: Markdown documentation for policy definitions and policy set definitions, from a
  single file or a glob pattern, written beside the input, under an output directory, to an exact path,
  or to stdout.
- `policydocs catalog`: one index page that lists every matched policy and links to its documentation.
- `policydocs template export`: prints a built-in Scriban template as the starting point for a custom one.
- Built-in templates for policy definitions, policy set definitions, and the catalog.
- Custom Scriban templates with `include` support and custom values from the command line or JSON files.
- Effect analysis (literal, parameter, or expression), targeted resource types, field aliases, remediation
  roles with friendly names for common built-in roles, and modify operations.
- JSON with comments and trailing commas, as used by Enterprise Policy as Code `.jsonc` files.
- `AzurePolicyDocs.Core` library package for building models and rendering without the CLI.
