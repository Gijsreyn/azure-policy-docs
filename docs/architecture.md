# Architecture

Two projects, one direction of dependency: `AzurePolicyDocs.Cli` depends on `AzurePolicyDocs.Core`; Core depends on nothing but Scriban and the base class library. Tests cover both.

## Data flow

```text
file path
  │  PolicyDocumentParser              Parsing/
  ▼
PolicyDocument (kind, name, root, properties)
  │  PolicyDocumentationModelBuilder   Model/
  │    ├─ PolicyEffectAnalyzer         Analysis/   then.effect → literal | parameter | expression
  │    ├─ PolicyRuleAnalyzer           Analysis/   if → resource types, field aliases
  │    └─ RoleDefinitionCatalog        Analysis/   role GUIDs → names
  ▼
PolicyDocumentationModel (records)
  │  PolicyDocumentationScriptModelFactory   Rendering/   records → ScriptObject with camelCase keys
  │  PolicyDocumentationRenderer             Rendering/   Scriban template → normalized text
  ▼
string
```

The CLI adds input resolution (one file or a glob), output resolution and validation, custom-value parsing, and the catalog, which reuses `BuildModel` for every file and renders a different template.

## Why the model is built by hand

Scriban can reflect over .NET objects, but then a property rename in C# silently changes the template contract. `PolicyDocumentationScriptModelFactory` adds every key explicitly. The template model is therefore a documented, versioned surface ([template-model.md](template-model.md)), and the C# records can change freely behind it. This is the same choice Bicep made for `bicep docs generate`, and the overall design (model builder, script model factory, template loader, output normalization) deliberately mirrors it.

## Why parsing is lenient

Policy JSON in the wild is messy: exports mix property casing, EPAC files carry comments and trailing commas, and some repositories store only the property bag. `PolicyDocumentParser` accepts all of that because the tool documents what people have, not what they should have. Validation is a job for Azure and for the Azure Policy Linter.

## Why analysis is heuristic

Resource types and aliases are convenience summaries. Without resource provider metadata there is no exact way to tell where a resource type ends and a property path begins inside an alias, so the tool only lists types from explicit `type` conditions and lists aliases separately. The full policy rule is always rendered, so a reader can check the summary against the source.

## Output rules

- Outputs are resolved and validated for the whole run before the first write.
- A render failure never overwrites an existing output file.
- Rendered text uses `\n` and ends with exactly one newline, so files are identical on every platform and diff cleanly.
- Writes use UTF-8 without a byte order mark.

## Tests

`src/AzurePolicyDocs.Tests` uses xUnit and FluentAssertions.

- Parser and model builder tests pin behavior on fixtures under `Fixtures/`.
- Renderer tests compare built-in template output to golden files under `Fixtures/Expected/`. When you change a template on purpose, regenerate the golden file with `policydocs generate <fixture> --stdout` and review the diff.
- CLI tests call `Program.RunAsync` in-process with string writers and temporary directories, so they exercise argument validation, output resolution, and exit codes without spawning processes.

## Adding a field to the model

1. Add it to the record in `Model/PolicyDocumentationModel.cs` and populate it in `PolicyDocumentationModelBuilder`.
2. Expose it with a camelCase key in `PolicyDocumentationScriptModelFactory`.
3. Document it in `docs/template-model.md`.
4. Use it in the built-in template if it belongs there, regenerate the golden files, and add a model builder test.

## Adding an output format

Nothing in Core is Markdown-specific except the built-in templates. A new format is a new template, either passed with `--template` or embedded next to the existing ones and selected in `PolicyDocumentationRenderer`.
