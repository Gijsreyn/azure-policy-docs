# AzurePolicyDocs.Cli

Generate Markdown documentation for Azure Policy definitions and initiatives from the command line. The `azurepolicydocs` .NET tool reads local JSON or JSONC files, renders a page per policy, and creates a linked catalog. No Azure sign-in or connection is required.

## Install

Requires the .NET 10 SDK or later to install. The tool targets .NET 10 and can roll forward to newer runtimes.

```powershell
dotnet tool install --global AzurePolicyDocs.Cli
azurepolicydocs --help
```

To update an existing installation:

```powershell
dotnet tool update --global AzurePolicyDocs.Cli
```

For a repository-local tool installation:

```powershell
dotnet new tool-manifest
dotnet tool install AzurePolicyDocs.Cli
dotnet tool run azurepolicydocs --help
```

Create a tool manifest only if your repository doesn't already have one. After cloning a repository with a committed manifest, run `dotnet tool restore`.

Need to generate documentation from C# instead? Use [AzurePolicyDocs.Core](https://www.nuget.org/packages/AzurePolicyDocs.Core).

## Quick start

```powershell
azurepolicydocs generate ./Definitions/policyDefinitions/allowed-locations.json
azurepolicydocs generate --pattern "./Definitions/**/*.json*" --outdir ./docs/policies
azurepolicydocs catalog --pattern "./Definitions/**/*.json*" --outfile ./docs/policies/README.md --docs-dir ./docs/policies
```

The first command writes Markdown beside its input. The second generates documents under the output directory while preserving the input directory structure. The third creates an index linking to those documents. Quote glob patterns so the tool, rather than your shell, expands them.

## Generate policy documentation

```text
azurepolicydocs generate [<inputFile>] [options]
```

| Argument or option | Description                                                                                                    |
|:-------------------|:---------------------------------------------------------------------------------------------------------------|
| `<inputFile>`      | One policy definition or policy set definition. Specify either this argument or `--pattern`, not both.         |
| `--pattern <glob>` | Generate for every matching file. The longest literal directory before the first wildcard is the pattern root. |
| `--outdir <dir>`   | Write beneath this directory, preserving directories under the pattern root.                                   |
| `--outfile <path>` | Write one document to exactly this path. Cannot be used with `--pattern`.                                      |
| `--stdout`         | Print one document without writing a file. Cannot be used with `--pattern`.                                    |

Without an output option, the tool writes a Markdown file beside each input, using the same base name. `--outdir`, `--outfile`, and `--stdout` are mutually exclusive. Output paths can't overwrite policy sources, end in a JSON or JSONC extension, or collide with another generated output.

The shared template options below also apply to `generate`.

## Generate a catalog

```text
azurepolicydocs catalog --pattern <glob> [options]
```

| Option             | Description                                                                                                  |
|:-------------------|:-------------------------------------------------------------------------------------------------------------|
| `--pattern <glob>` | Required. Select policies and initiatives to list.                                                           |
| `--outfile <path>` | Write the catalog here. Defaults to a README in the pattern root.                                            |
| `--stdout`         | Print the catalog without writing a file. Cannot be combined with `--outfile`.                               |
| `--docs-dir <dir>` | Link to documents generated with `generate --outdir`. Otherwise, link to a Markdown file beside each source. |

The catalog groups policy definitions and policy sets, then sorts by category and display name. It doesn't generate the linked documents; run `generate` first. Source and documentation links are relative to the catalog's output directory, or to the current working directory with `--stdout`.

The shared template options also apply to `catalog`. Set `custom.title` and `custom.description` to customize the catalog heading:

```powershell
azurepolicydocs catalog --pattern "./Definitions/**/*.json*" --stdout --custom-template-value "title=Platform policies" --custom-template-value "description=Policies maintained by the platform team"
```

## Shared template options

These options work with both `generate` and `catalog`.

| Option                                     | Description                                                                                                                                                                                                   |
|:-------------------------------------------|:--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `--template <file>`                        | Use a custom Scriban template instead of the embedded template.                                                                                                                                               |
| `--include-root <dir>`                     | Resolve template includes beneath this directory. For `generate`, defaults to each input file's directory. For `catalog`, defaults to the output directory, or the current working directory with `--stdout`. |
| `--custom-template-value key=value`        | Supply a string available as `custom.key`. Repeat for multiple values.                                                                                                                                        |
| `--custom-template-value-file-path <file>` | Load a JSON object whose values are strings. Repeat for multiple files.                                                                                                                                       |

Inline values and value files are processed in command-line order. The last occurrence of a key wins. Quote the entire `key=value` argument when the value contains spaces.

```powershell
azurepolicydocs generate ./policy.json --stdout --template ./templates/policy.scriban --include-root ./templates --custom-template-value "owner=Platform team" --custom-template-value-file-path ./values.json
```

For example, a custom-values file can contain:

```json
{
  "supportUrl": "https://contoso.example/support"
}
```

The template can read `{{ custom.owner }}` and `{{ custom.supportUrl }}`.

## Export a built-in template

```text
azurepolicydocs template export --kind <kind> [--outfile <path>]
```

| Option | Description |
| :-- | :-- |
| `--kind <kind>` | Required. One of `policyDefinition`, `policySetDefinition`, or `catalog`. |
| `--outfile <path>` | Write the template to this path instead of stdout. |

```powershell
azurepolicydocs template export --kind policyDefinition --outfile ./templates/policy.scriban
azurepolicydocs template export --kind policySetDefinition
azurepolicydocs template export --kind catalog
```

## Help and version

Use `--help` on any command to list its arguments and options. Use `azurepolicydocs --version` to print the installed version.

## Supported inputs and output

- Full resource payloads and property bags are accepted, including EPAC policy definitions and policy sets.
- JSON comments, trailing commas, and case-insensitive property names are supported.
- Policy pages include metadata, effects, parameters, explicit resource types, field aliases, remediation details, and the full policy rule.
- Initiative pages include parameters, policy references and their values, groups, and member counts.
- Output uses UTF-8 without a byte order mark, LF line endings, and one final newline.
- Policy assignments and exemptions aren't supported. The tool documents policies; it doesn't deploy them or validate them against Azure.

## Exit codes

| Code | Meaning                                                                                                                                                  |
|-----:|:---------------------------------------------------------------------------------------------------------------------------------------------------------|
|    0 | Successful execution. An unmatched `generate --pattern` also returns 0 with a warning on stderr; an unmatched catalog pattern produces an empty catalog. |
|    1 | Invalid arguments, input or output errors, or a template that fails to parse or render.                                                                  |

In batch generation, input or per-file rendering failures are reported on stderr and other valid inputs continue. The final exit code is 1 if any input failed. A template syntax error stops the run before files are written. Failed rendering doesn't overwrite an existing document.

## Documentation and support

- [Custom templates](https://github.com/Gijsreyn/azure-policy-docs/blob/main/docs/custom-templates.md)
- [Template model](https://github.com/Gijsreyn/azure-policy-docs/blob/main/docs/template-model.md)
- [Rendered examples](https://github.com/Gijsreyn/azure-policy-docs/tree/main/samples/docs)
- [Source and issue tracker](https://github.com/Gijsreyn/azure-policy-docs)
- [MIT license](https://github.com/Gijsreyn/azure-policy-docs/blob/main/LICENSE)
