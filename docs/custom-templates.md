# Custom templates

The built-in Markdown is a sensible default, not the only output. Any [Scriban](https://github.com/scriban/scriban/blob/master/doc/language.md) template can read the model described in [template-model.md](template-model.md) and produce Markdown, AsciiDoc, HTML, CSV, or anything else that is text.

## Start from the built-in template

```powershell
azurepolicydocs template export --kind policyDefinition --outfile ./templates/policy.scriban
azurepolicydocs generate ./Definitions/policyDefinitions/allowed-locations.json --template ./templates/policy.scriban --stdout
```

The same template is used for every input of a `generate` run. When a pattern mixes policy definitions and policy sets, branch on `policy.kind`:

```scriban
{{ if policy.kind == "policySetDefinition" }}
{{ include "_policy-set.scriban" }}
{{ else }}
{{ include "_policy.scriban" }}
{{ end }}
```

## Includes

`include` paths resolve against the include root, which is the input file's directory unless you pass `--include-root`. Keep shared fragments in one directory and point the root at it:

```powershell
azurepolicydocs generate --pattern "./Definitions/**/*.json*" --outdir ./docs --template ./templates/policy.scriban --include-root ./templates
```

## Custom values

Values are strings and live under `custom`:

```powershell
azurepolicydocs generate ./policy.json --stdout `
  --custom-template-value owner="Platform team" `
  --custom-template-value-file-path ./docs/values.json
```

```json
{
  "owner": "Platform team",
  "supportUrl": "https://contoso.example/support"
}
```

Inline values and value files are applied in command-line order. The last occurrence of a key wins. Both `custom.owner` and `policy.custom.owner` read the same value.

## Whitespace control

Scriban prints everything outside `{{ }}` verbatim, including the newline after a control statement. Use `{{~` and `~}}` to strip whitespace on that side of a tag. The built-in templates put every control statement on its own line with `{{~ ... ~}}` and write blank lines explicitly, which keeps the output readable without trial and error.

```scriban
{{~ for parameter in policy.parameters ~}}
- `{{ parameter.name }}`
{{~ end ~}}
```

## Markdown tables

A table cell breaks on `|` and on line breaks. The built-in templates define `md_cell` for this and you can copy it:

```scriban
{{~ func md_cell(value) ~}}
{{~ if value == null ~}}
{{~ ret "" ~}}
{{~ end ~}}
{{~ ret (value | string.replace "\r\n" " " | string.replace "\n" " " | string.replace "|" "\\|") ~}}
{{~ end ~}}
```

## Code fences

`policyRuleJson` and `defaultValueJson` may contain backticks. `policyRuleFence` and `defaultValueFence` are always longer than the longest run inside the content:

```scriban
{{ policy.policyRuleFence }}json
{{ policy.policyRuleJson }}
{{ policy.policyRuleFence }}
```

## A compact example

[samples/templates/compact.scriban](../samples/templates/compact.scriban) renders a single bullet list per policy and includes a header fragment. Try it:

```powershell
azurepolicydocs generate ./samples/policyDefinitions/allowed-locations.json --stdout `
  --template ./samples/templates/compact.scriban --include-root ./samples/templates `
  --custom-template-value owner="Platform team"
```

## Failure behavior

A template that fails to parse stops the run before any file is written and is reported once, not per input. A template that fails while rendering one input reports that input and continues with the others; the exit code is 1. Existing output files are never overwritten by a failed render.
