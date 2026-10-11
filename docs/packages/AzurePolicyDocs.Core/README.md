# AzurePolicyDocs.Core

Build documentation models from Azure Policy definitions and policy set definitions, then render them as Markdown or another text format with Scriban templates. Use this .NET library to add policy documentation to your own tools, build pipelines, or applications without launching a separate CLI process.

The library reads local JSON. It doesn't connect to Azure or require Azure credentials.

## Install

Targets .NET 10. Add the package to a compatible .NET project:

```powershell
dotnet add package AzurePolicyDocs.Core
```

Need a ready-to-use command instead? Install [AzurePolicyDocs.Cli](https://www.nuget.org/packages/AzurePolicyDocs.Cli).

## What you can do

- Generate Markdown for individual policy definitions and initiatives (policy set definitions).
- Inspect typed models for metadata, parameters, effects, explicit resource types, field aliases, remediation roles, modify operations, and policy references.
- Parse full resource payloads or property bags, including JSON with comments and trailing commas. EPAC policy definitions and policy set definitions are supported.
- Customize output with Scriban templates, file includes, and your own string values.
- Build a catalog that groups policies and initiatives and links to their documentation.
- Use the parsing, model-building, and rendering APIs independently when you need a different output format.

## Generate Markdown from a file

```csharp
using AzurePolicyDocs;

var generator = new PolicyDocumentationGenerator();
string markdown = generator.Generate("./policies/allowed-locations.json");

File.WriteAllText("./allowed-locations.md", markdown);
```

The same call works for a policy set definition. The generator selects the built-in template from the document kind. `Generate` returns text; your application controls where and how to write it.

## Parse JSON and inspect the model

```csharp
using AzurePolicyDocs;

var generator = new PolicyDocumentationGenerator();
string json = File.ReadAllText("./policies/allowed-locations.json");
var model = generator.BuildModel(json, path: "allowed-locations.json");

Console.WriteLine(model.DisplayName ?? model.Name);
Console.WriteLine(model.Effect?.DefaultEffect);

foreach (var parameter in model.Parameters)
{
    Console.WriteLine($"{parameter.Name}: {parameter.Type}");
}

string markdown = generator.Render(model);
```

For in-memory JSON, `path` supplies a name fallback and context for errors and relative includes. It doesn't need to identify an existing file. Use `BuildModel(path)` instead to read directly from disk.

## Customize rendering

```csharp
using AzurePolicyDocs;
using AzurePolicyDocs.Rendering;

var generator = new PolicyDocumentationGenerator();
var options = new PolicyDocumentationGenerationOptions(
    TemplateFile: Path.GetFullPath("./templates/policy.scriban"),
    IncludeRoot: Path.GetFullPath("./templates"),
    CustomValues: new Dictionary<string, string>
    {
        ["owner"] = "Platform team",
    }
);

string markdown = generator.Generate("./policies/allowed-locations.json", options);
Console.Write(markdown);
```

The custom template can read `policy` and `custom`:

```scriban
# {{ policy.displayName ?? policy.name }}

{{ policy.description }}

Owner: {{ custom.owner }}
```

`TemplateFile`, `IncludeRoot`, and `CustomValues` are optional. Without a custom template, the embedded Markdown template is used. For individual policies, includes default to the source file's directory. For catalogs, they default to the current working directory. Values in the rendering options override values already stored in the model.

To start from an existing template, call `PolicyDocumentationRenderer.GetBuiltInTemplateSource` with a `PolicyDocumentKind`, or `GetBuiltInCatalogTemplateSource` for the catalog.

## Build a catalog

```csharp
using AzurePolicyDocs;
using AzurePolicyDocs.Model;

var generator = new PolicyDocumentationGenerator();
var model = generator.BuildModel("./policies/allowed-locations.json");
var entries = new[]
{
    (
        Model: model,
        Path: "policies/allowed-locations.json",
        DocumentationPath: (string?)"allowed-locations.md"
    ),
};

var catalog = PolicyCatalogModel.Create(
    entries,
    new Dictionary<string, string> { ["title"] = "Policy catalog" }
);

File.WriteAllText("./README.md", generator.RenderCatalog(catalog));
```

Add one entry per policy or initiative. Supply source and documentation links relative to the catalog's intended location. The library sorts entries by category and display name, but doesn't discover files or write the linked documents for you.

## Behavior and limitations

- Property names are matched case-insensitively. Missing resource names fall back to the input file name.
- Rendered text uses LF line endings and ends with exactly one newline.
- Parsing and template failures are reported as `PolicyDocumentationException`. Handle that exception at your application's boundary.
- Policy assignments and exemptions aren't supported.
- Resource types are inferred only from explicit `type` conditions. Effect expressions are summarized, not evaluated by Azure. Friendly role names cover common built-in roles, not every role.
- This is a documentation library, not a policy deployment or validation engine.

## Documentation and support

- [Template model](https://github.com/Gijsreyn/azure-policy-docs/blob/main/docs/template-model.md)
- [Custom templates](https://github.com/Gijsreyn/azure-policy-docs/blob/main/docs/custom-templates.md)
- [Rendered examples](https://github.com/Gijsreyn/azure-policy-docs/tree/main/samples/docs)
- [Source and issue tracker](https://github.com/Gijsreyn/azure-policy-docs)
- [MIT license](https://github.com/Gijsreyn/azure-policy-docs/blob/main/LICENSE)
