# Contribute to Azure Policy Docs

Use this guide to prepare a change, open a pull request, and understand how your contribution reaches users.

## How your contribution is released

This repository uses [semantic-release](https://github.com/semantic-release/semantic-release) to select versions and publish packages from `main`. Your PR title and change description help determine the release version and tell users what's new.

- Write a Conventional Commit PR title, add tests, and describe the user-facing changes.
- Let the automated checks validate your title and code.
- After your PR is merged and the release checks pass, automation publishes any required release.

You don't need to update package versions, add changelog entries, push release tags, or configure publishing credentials. `AzurePolicyDocs.Core` and `AzurePolicyDocs.Cli` share one version, even when a change affects only one package.

> [!IMPORTANT]
> A merged feature, fix, performance improvement, or breaking change can reach users in the next automated release. Describe compatibility changes and migration steps in your PR.

## Prerequisites

- A GitHub account and Git.
- A stable .NET 10 SDK compatible with [global.json](global.json): 10.0.303 or a later 10.0 feature band.
- Node.js 24.10.0 or later in the 24.x release line, with npm, to validate release tooling and commit messages. CI uses the latest 24.x version through [.node-version](.node-version). The tools also support Node.js 22.14.0 or later in the 22.x line.

Node.js is a development dependency. You don't need it to build the C# projects or run the published CLI. Contributors don't need a NuGet API key or release permissions.

## Set up your development environment

1. Fork the repository if you don't have write access, then clone your fork. If you have write access, clone the repository directly.
1. Create a working branch from the latest `main`.
1. From the repository root, restore the tools and dependencies:

   ```powershell
   dotnet tool restore
   dotnet restore AzurePolicyDocs.sln
   npm ci
   ```

Use `npm ci` for normal development. When you intentionally update release dependencies, use `npm install` and include both [package.json](package.json) and [package-lock.json](package-lock.json) in your PR.

## Make and validate your change

1. Keep the change focused. Add a regression test for a bug fix and tests for new behavior.
1. Update the relevant documentation and samples when the CLI, template model, or rendered output changes.
1. Format the C# and MSBuild files, then run the checks from the repository root:

   ```powershell
   dotnet csharpier format .
   dotnet csharpier check .
   dotnet build AzurePolicyDocs.sln --configuration Release
   dotnet test AzurePolicyDocs.sln --configuration Release --no-build
   dotnet pack AzurePolicyDocs.sln --configuration Release --no-build --output ./artifacts
   npm test
   ```

1. For CLI changes, check the rendered sample:

   ```powershell
   dotnet run --project src/AzurePolicyDocs.Cli --configuration Release --no-build -- generate ./samples/policyDefinitions/allowed-locations.json --stdout
   ```

The .NET build treats warnings as errors. CSharpier uses [.editorconfig](.editorconfig) and [.config/dotnet-tools.json](.config/dotnet-tools.json). Keep generated build output and installed dependencies out of commits.

`npm test` checks version selection, release notes, PR-title rules, and the NuGet publishing steps. Integration tests use temporary local Git repositories. They don't publish packages or contact GitHub.

Local packages use `0.0.0-development`. This placeholder in [Directory.Build.props](Directory.Build.props) is not the latest released version. The release process overrides `Version` when it builds and packs both packages.

## Choose a Conventional Commit title

Use this format for your PR title and for commits that you intend to keep on `main`:

```text
type(optional-scope): short description
```

Use a lowercase type, a concise lowercase description, and no final period. Keep the header within 100 characters. A scope such as `cli`, `core`, or `templates` is optional and doesn't affect the version.

| Change | Example title | Release impact |
| :-- | :-- | :-- |
| Bug fix | `fix(cli): preserve filesystem roots` | Patch, such as 1.2.3 to 1.2.4 |
| Performance improvement | `perf(core): reduce model allocations` | Patch |
| New feature | `feat(templates): add a parameter summary` | Minor, such as 1.2.3 to 1.3.0 |
| Breaking change | `feat(cli)!: remove the legacy output option` | Major, such as 1.2.3 to 2.0.0 |
| Documentation | `docs: clarify installation requirements` | No release by itself |
| Tests, refactoring, or formatting | `test: cover null models`, `refactor(core): simplify parsing`, `style: format source files` | No release by itself |
| Build, CI, or maintenance | `build: update development tools`, `ci: update build checks`, `chore: update repository settings` | No release by itself |

The highest impact among all commits since the last release wins. A docs-only merge doesn't trigger a release by itself, but a later run can still release earlier unreleased features or fixes. Versions come from Git tags, not from PR labels or the development package version.

To check a title locally:

```powershell
'fix(cli): preserve filesystem roots' | npm run lint:commit
```

### Describe breaking changes

Add `!` before the colon when users must change their code, templates, scripts, or runtime. For example, removing an option, renaming a public model property, or raising the minimum supported runtime can be a breaking change.

Explain the migration in the PR description and include a `BREAKING CHANGE:` footer that can be copied into the final squash commit:

```text
feat(cli)!: remove the legacy output option

BREAKING CHANGE: --output is no longer supported. Use --outfile for one
document or --outdir for multiple documents.
```

Either `!` or a `BREAKING CHANGE:` footer triggers a major release, regardless of the commit type. Use both to make the impact clear and give users migration instructions. A footer written only in the PR description isn't enough if it is omitted from the squash commit.

Breaking changes also trigger a major release from a `0.x` baseline. For example, a breaking change after `v0.1.0` produces `1.0.0`, not `0.2.0`.

## Open a pull request

1. Push your working branch and open a PR targeting `main`.
1. Give the PR a Conventional Commit title. The **Conventional PR title** check runs again when you edit the title.
1. Describe the problem, the change, and how you tested it. Link related issues and include migration steps for breaking changes.
1. Wait for the Windows build, Linux build, release tooling tests, and PR-title check to pass. Fork PRs run without publishing secrets.
1. Address review feedback. The maintainer uses **Squash and merge** and reviews the final commit message before merging.

Individual work-in-progress commits don't need to be rewritten when the PR is squash merged. The final squash commit does need to follow the convention because semantic-release reads Git commit messages, not PR titles directly. Avoid release-skip and CI-skip markers, which can hide required changes or checks.

Don't add a version bump or a new section to [CHANGELOG.md](CHANGELOG.md). Describe the user-facing change in the title and commit body instead. New release notes are generated in [GitHub Releases](https://github.com/Gijsreyn/azure-policy-docs/releases). The changelog preserves notes written before automation.

## Write documentation for the reader

Follow the [Microsoft Learn style and voice guidance](https://learn.microsoft.com/contribute/content/style-quick-start). Use sentence-case headings, short sentences, active voice, and numbered steps for tasks. Address the reader as "you". Explain what a change means for users, and provide a working example when it helps.

For more context, see [docs/architecture.md](docs/architecture.md), [docs/template-model.md](docs/template-model.md), and [docs/custom-templates.md](docs/custom-templates.md).







