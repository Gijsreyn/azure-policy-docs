# Azure Policy Docs

`policydocs` is a .NET command-line tool that turns Azure Policy definition and policy set definition JSON into Markdown. It reads the same files you already keep in source control, including the `.jsonc` files that Enterprise Policy as Code (EPAC) uses, and renders them with a [Scriban](https://github.com/scriban/scriban) template. The built-in templates produce a page per policy with its effect, parameters, targeted resource types, remediation roles, and the full policy rule. Bring your own template when your repository needs a different layout.

The command shape follows `bicep docs generate`: one file or a glob pattern in, Markdown beside the input or under an output directory out, custom values passed on the command line.

```text
policydocs generate ./Definitions/policyDefinitions/allowed-locations.json
policydocs generate --pattern "./Definitions/**/*.json*" --outdir ./docs/policies
policydocs catalog  --pattern "./Definitions/**/*.json*" --outfile ./docs/policies/README.md --docs-dir ./docs/policies
```

Rendered examples for every file under [samples/](samples/) are in [samples/docs/](samples/docs/).

## Install

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later. The tool rolls forward to newer runtimes. The `AzurePolicyDocs.Core` library targets .NET 10.

```powershell
dotnet tool install --global AzurePolicyDocs.Cli
policydocs --help
```

To render from your own code instead, reference the `AzurePolicyDocs.Core` package and use `PolicyDocumentationGenerator`.

## Commands

### `policydocs generate`

Generates one Markdown document per input.

```text
policydocs generate [<inputFile>] [options]
```

| Option | Description |
| :-- | :-- |
| `<inputFile>` | A policy definition or policy set definition file. Either this or `--pattern` is required. |
| `--pattern <glob>` | Generate for every matching file, for example `Definitions/**/*.json*`. The longest literal directory before the first wildcard is the pattern root. |
| `--outdir <dir>` | Write beneath this directory. With `--pattern`, directories under the pattern root are recreated so same-named files stay apart. |
| `--outfile <path>` | Write one document to exactly this path. Single input only. |
| `--stdout` | Print one document and write nothing. Single input only. |
| `--template <file>` | Use a custom Scriban template instead of the built-in one. |
| `--include-root <dir>` | Directory that `include` statements in the template resolve against. Defaults to the input file's directory. |
| `--custom-template-value key=value` | Expose a value to the template as `custom.key`. Repeatable. |
| `--custom-template-value-file-path <file>` | Load values from a JSON object whose values are strings. Repeatable. Later occurrences of either option win. |

Without an output option, the document is written beside the input as `<input name>.md`, so `allowed-locations.json` becomes `allowed-locations.md`. Outputs are resolved and validated before anything is written: an output can't overwrite its input, can't end in `.json` or `.jsonc`, and two inputs can't claim the same output.

With `--pattern`, a file that fails to parse is reported on stderr and skipped. The other files are still written and the command exits with 1.

### `policydocs catalog`

Writes one index page that lists every matched policy and links to its document.

```text
policydocs catalog --pattern <glob> [--outfile <path> | --stdout] [--docs-dir <dir>] [template options]
```

The catalog defaults to `README.md` in the pattern root. Links point to a `.md` file beside each source file, or into `--docs-dir` when you generated the documents with `--outdir`. Set `custom.title` and `custom.description` to change the heading.

### `policydocs template export`

Prints a built-in template, which is the easiest way to start a custom one.

```text
policydocs template export --kind policyDefinition --outfile ./templates/policy.scriban
policydocs template export --kind policySetDefinition
policydocs template export --kind catalog
```

## What the built-in templates document

For a policy definition:

- Display name, description, name, policy type, mode, category, version, and preview or deprecated flags.
- The effect: a fixed value, the parameter that sets it with its default and allowed values, or the expression that computes it and the effects it can resolve to.
- For `deployIfNotExists`, `auditIfNotExists`, and `modify`: the related resource type, whether an existence condition and a deployment are present, the role assignments remediation needs (with names for common built-in roles), and modify operations.
- Resource types targeted by explicit `type` conditions, and the field aliases the rule references.
- Parameters in declaration order with type, required flag, default, allowed values, description, strong type, and assign-permissions flag. Multi-line defaults get their own code block.
- The complete policy rule as JSON.

For a policy set definition: the same header, its parameters, every referenced policy definition with reference ID, built-in or custom, pinned version, groups, and parameter values, and the policy definition groups with member counts.

The full template model is in [docs/template-model.md](docs/template-model.md). Writing a custom template is covered in [docs/custom-templates.md](docs/custom-templates.md).

## Custom templates in short

```scriban
# {{ policy.displayName ?? policy.name }}

{{ policy.description }}

{{ for parameter in policy.parameters }}
- `{{ parameter.name }}` ({{ parameter.type }}){{ if parameter.required }}, required{{ end }}: {{ parameter.description }}
{{ end }}

Owner: {{ custom.owner }}
```

```powershell
policydocs generate .\policy.json --template .\templates\short.scriban --custom-template-value owner="Platform team"
```

Includes work relative to the input file's directory unless you pass `--include-root`. Rendered output always uses `\n` line endings and ends with exactly one newline.

## Input formats

- A full resource payload with `name`, `type`, and `properties`, as exported from Azure or kept in EPAC and the `Azure/azure-policy` repository.
- Only the property bag (`displayName`, `policyRule`, ...). The name then falls back to the file name.
- Policy set definitions in either shape, including EPAC's `policyDefinitionName` references.
- Comments and trailing commas are accepted.

Property names are matched case-insensitively.

## Exit codes

| Code | Meaning |
| --: | :-- |
| 0 | Every input rendered and every output was written. An unmatched `--pattern` also returns 0, with a warning on stderr. |
| 1 | Invalid arguments, a template that failed to parse or render, an input that could not be read or is not a policy, or an output that could not be written. |

## Limitations

- Policy assignments and exemptions are not documented. EPAC assignment files use their own tree format, so they are a separate model.
- Resource types are collected from explicit `type` conditions only. A rule that targets a resource purely through aliases shows no resource types; the aliases are listed instead.
- Effect expressions are summarized by the effect literals they contain. The expression itself is always shown.
- Friendly role names cover common built-in roles. Other roles show their GUID.

## Build from source

Install a stable .NET 10 SDK matching [global.json](global.json) (10.0.303 or a later 10.0 feature band). All projects target `net10.0` and use its default language version, C# 14, instead of a floating `latest` language version.

```powershell
dotnet build AzurePolicyDocs.sln
dotnet test AzurePolicyDocs.sln
dotnet run --project src/AzurePolicyDocs.Cli -- generate ./samples/policyDefinitions/allowed-locations.json --stdout
```

To try the packed tool locally:

```powershell
dotnet pack src/AzurePolicyDocs.Cli/AzurePolicyDocs.Cli.csproj --configuration Release --output ./artifacts
dotnet tool install --global AzurePolicyDocs.Cli --add-source ./artifacts
```

### Formatting

[CSharpier](https://csharpier.com/docs/CLI) is pinned in [.config/dotnet-tools.json](.config/dotnet-tools.json) as a development-only local tool. Runtime roll-forward lets it run with the .NET 10 SDK without requiring a separate .NET 8 runtime.

Run these commands from the repository root:

```powershell
dotnet tool restore
dotnet csharpier format .
dotnet csharpier check .
```

CSharpier uses [.editorconfig](.editorconfig) for LF line endings, a 100-column print width, four-space C# indentation, and two-space XML/MSBuild indentation. It respects [.gitignore](.gitignore), so build outputs, packages, and test results are excluded. Templates and golden Markdown files are not reformatted.

The [build workflow](.github/workflows/build.yml) checks formatting on both Windows and Linux for pull requests, pushes to `main`, and manual runs. The [release workflow](.github/workflows/release.yml) checks it before building or publishing. Unformatted files fail the workflow; CI never rewrites or commits code.

The SDK and language settings follow Microsoft Learn's [upgrade guidance](https://learn.microsoft.com/dotnet/core/install/upgrade) and [C# language version guidance](https://learn.microsoft.com/dotnet/csharp/language-reference/configure-language-version).

## Releasing

Pushing a tag such as `v0.2.0` runs [release.yml](.github/workflows/release.yml), which checks formatting, builds with that version, runs the tests, packs both packages, pushes them to NuGet.org with the `NUGET_API_KEY` repository secret, and creates a GitHub release. Before the first release, replace the author and repository placeholders in [Directory.Build.props](Directory.Build.props) and [LICENSE](LICENSE).

## License

MIT. See [LICENSE](LICENSE).
