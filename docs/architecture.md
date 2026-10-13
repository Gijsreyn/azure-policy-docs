# Architecture

The solution has two projects. `AzurePolicyDocs.Cli` is the command-line tool, and `AzurePolicyDocs.Core` is the library that does the work. The CLI depends on Core, never the other way around. Core depends only on [Scriban](https://github.com/scriban/scriban), the template engine, and the .NET base class library. A third project, `AzurePolicyDocs.Tests`, tests both.

## Data flow

A policy file goes through three steps: it is parsed, turned into a model, and rendered to text.

![Data flow](media/AzurePolicyDocsDataFlow.png)

The diagram shortens the class names. These are the full names and what each step does:

1. **Parse.** `PolicyDocumentParser` in `Parsing/` reads the JSON file and returns a `PolicyDocument`. It holds the kind (policy definition or policy set definition), the name, the whole JSON object, and its `properties` object.
2. **Build the model.** `PolicyDocumentationModelBuilder` in `Model/` picks out everything a documentation page needs and stores it in a `PolicyDocumentationModel`, a set of C# records. For a policy definition it uses three helpers in `Analysis/`:
   - `PolicyEffectAnalyzer` reads `then.effect` and works out which effects the policy can have. The effect is a fixed value, a parameter, or an expression.
   - `PolicyRuleAnalyzer` goes through the conditions of the rule and collects the resource types and field aliases they mention.
   - `RoleDefinitionCatalog` looks up the names of common built-in roles, so a page can say "Contributor" instead of showing only a GUID.
3. **Render.** `PolicyDocumentationRenderer` in `Rendering/` produces the final text. It first asks `PolicyDocumentationScriptModelFactory` to copy the records into a `ScriptObject`, the object a Scriban template reads from, with camelCase keys such as `displayName`. Then it runs the template and cleans up the line endings (see [Output rules](#output-rules)).

The CLI is a thin layer around this flow. It adds:

- **Finding the input files.** One file, or every file that matches a glob pattern such as `Definitions/**/*.json*`.
- **Deciding where the output goes.** Every output path is worked out and checked before anything is written.
- **Reading custom values.** A value passed with `--custom-template-value owner=Platform` is available in the template as `custom.owner`.
- **The catalog.** `azurepolicydocs catalog` writes one index page that links to every policy. It reuses `BuildModel` for every file and renders the result with a different template.

## Why the template model is built by hand

Scriban can read .NET objects directly and use their property names in templates. That is less code, but it makes the C# property names something every template depends on. Rename a property in C#, and each template that uses the old name breaks, with nothing to warn you.

`PolicyDocumentationScriptModelFactory` adds every key by hand instead. The names a template can use are listed in [template-model.md](template-model.md) and change only when someone changes them on purpose. That list is part of the tool's public interface: renaming a key is a breaking change for anyone with a custom template. The C# records behind it can be renamed and reorganized freely.

Bicep made the same choice for `bicep docs generate`. The overall design here (model builder, script model factory, template loader, output normalization) follows Bicep's on purpose.

## Why the parser accepts imperfect files

The parser is not strict, and that is on purpose. Real policy files often differ from the format Azure documents:

- Exported files do not agree on upper and lower case in property names.
- Enterprise Policy as Code (EPAC) uses `.jsonc` files, which can contain comments and trailing commas. Strict JSON allows neither.
- Some repositories store only the content of `properties`, often called the property bag (`displayName`, `policyRule`, and so on), without the surrounding `name`, `type`, and `properties` wrapper.

`PolicyDocumentParser` accepts all of these. The tool's job is to document the files people actually have, not to judge whether those files are correct. Checking that a policy is valid is left to Azure and to the Azure Policy Linter.

The parser does stop with a clear message when a file is not valid JSON, or when it is neither a policy definition nor a policy set definition.

## Why resource types and aliases are only a summary

Each page lists the resource types a policy targets and the field aliases it uses. These lists give a reader a quick overview, but they are worked out with simple rules and can be incomplete.

The reason is how aliases are written. An alias such as `Microsoft.Storage/storageAccounts/supportsHttpsTrafficOnly` joins a resource type and a property path into one string. Here the type is `Microsoft.Storage/storageAccounts` and the property is `supportsHttpsTrafficOnly`, but nothing in the string marks where one ends and the other begins. Counting slashes does not help, because a type can have more parts, as in `Microsoft.Sql/servers/databases`. Only the metadata of the Azure resource providers has the answer, and the tool does not call Azure.

So the tool does not guess:

- A resource type is listed only when the rule compares the `type` field directly, as in `{ "field": "type", "equals": "Microsoft.Storage/storageAccounts" }`.
- Aliases are listed separately, exactly as they are written in the rule.

A policy that targets resources only through aliases therefore shows no resource types. The full policy rule is always included on the page, so a reader can check both lists against the rule itself.

## Output rules

- All output paths are worked out and checked before the first file is written. A conflict, such as two inputs that would write the same file, stops the run before anything on disk changes.
- If rendering fails, an existing output file is left as it was.
- Rendered text always uses `\n` line endings and ends with exactly one newline. The same input gives the same file on every platform, and diffs show only real changes.
- Files are written as UTF-8 without a byte order mark (BOM).

## Tests

`src/AzurePolicyDocs.Tests` uses xUnit and FluentAssertions.

- Parser and model builder tests run against sample policy files (fixtures) under `Fixtures/` and check the exact result, so a change in behavior shows up as a failing test.
- Renderer tests render the fixtures with the built-in templates and compare the output to stored expected files under `Fixtures/Expected/`, often called golden files. When you change a template on purpose, regenerate the golden file with `azurepolicydocs generate <fixture> --stdout` and review the diff.
- CLI tests call `Program.RunAsync` directly inside the test process. They capture output in string writers and write files to temporary directories, so they cover argument validation, output paths, and exit codes without starting a separate process.

## Adding a field to the model

1. Add it to the record in `Model/PolicyDocumentationModel.cs` and fill it in `PolicyDocumentationModelBuilder`.
2. Expose it with a camelCase key in `PolicyDocumentationScriptModelFactory`. Until you do, no template can see the field.
3. Document it in `docs/template-model.md`.
4. Use it in the built-in template if it belongs there, regenerate the golden files, and add a model builder test.

## Adding an output format

Only the built-in templates know about Markdown. The rest of Core works with plain data. A new output format is therefore a new template: pass it with `--template`, or embed it next to the existing templates and select it in `PolicyDocumentationRenderer`.
