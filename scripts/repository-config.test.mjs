import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { test } from "node:test";

const read = (path) => readFileSync(new URL(`../${path}`, import.meta.url), "utf8")
    .replaceAll("\r\n", "\n");

test("CodeQL scans every PR to main and maintains a scheduled main baseline", () => {
    const workflow = read(".github/workflows/codeql.yml");
    assert.match(workflow, /\n  pull_request:\n    branches: \[main\]/);
    assert.match(workflow, /\n  push:\n    branches: \[main\]/);
    assert.match(workflow, /\n  schedule:\n    - cron:/);
    assert.match(workflow, /\n  workflow_dispatch:/);
    assert.doesNotMatch(workflow, /\n\s+(?:paths|paths-ignore|pull_request_target):/);
    assert.match(workflow, /\npermissions:\n  contents: read\n/);
    assert.match(workflow, /\n      security-events: write\n/);
    assert.doesNotMatch(workflow, /(?:contents|pull-requests|id-token): write|secrets\./);
});

test("CodeQL observes a full C# build with the repository SDK before analysis", () => {
    const workflow = read(".github/workflows/codeql.yml");
    const setup = workflow.indexOf("uses: actions/setup-dotnet@");
    const init = workflow.indexOf("uses: github/codeql-action/init@v4");
    const build = workflow.indexOf("run: dotnet build AzurePolicyDocs.sln");
    const analyze = workflow.indexOf("uses: github/codeql-action/analyze@v4");
    assert.ok(setup >= 0 && setup < init && init < build && build < analyze);
    assert.match(workflow, /global-json-file: global.json/);
    assert.match(workflow, /languages: csharp\n\s+build-mode: manual/);
    assert.match(workflow, /queries: security-and-quality/);
    assert.match(workflow, /dotnet build .*--no-incremental.*-p:UseSharedCompilation=false/);
    assert.match(workflow, /category: \/language:csharp/);
});

test("PR builds still execute .NET tests on Windows and Linux before packing", () => {
    const workflow = read(".github/workflows/build.yml");
    assert.match(workflow, /\n  pull_request:\n    branches: \[main\]/);
    assert.match(workflow, /os: \[ubuntu-latest, windows-latest\]/);
    const build = workflow.indexOf("run: dotnet build AzurePolicyDocs.sln");
    const tests = workflow.indexOf("run: dotnet test AzurePolicyDocs.sln");
    const pack = workflow.indexOf("run: dotnet pack AzurePolicyDocs.sln");
    assert.ok(build >= 0 && build < tests && tests < pack);
    assert.match(workflow, /azurepolicydocs generate .*--stdout/);
});

test("packable projects use package-specific READMEs at the NuGet package root", () => {
    const targets = read("Directory.Build.targets");
    assert.ok(targets.includes('Condition="\'$(IsPackable)\' == \'true\'"'));
    assert.ok(targets.includes('Include="$(MSBuildThisFileDirectory)docs/packages/$(PackageId)/README.md"'));
    assert.ok(targets.includes('PackagePath="\\"'));
    assert.match(read("Directory.Build.props"), /<PackageReadmeFile>README\.md<\/PackageReadmeFile>/);

    for (const id of ["AzurePolicyDocs.Core", "AzurePolicyDocs.Cli"]) {
        assert.ok(read(`src/${id}/${id}.csproj`).includes(`<PackageId>${id}</PackageId>`));
        const readme = read(`docs/packages/${id}/README.md`);
        assert.ok(readme.startsWith(`# ${id}\n`));
        assert.notEqual(readme, read("README.md"));
        for (const [, link] of readme.matchAll(/\]\(([^)]+)\)/g)) {
            assert.match(link, /^https:\/\//, `${id} must use NuGet-compatible documentation links`);
        }
    }
});

test("the CLI package guide documents every declared option", () => {
    const options = read("src/AzurePolicyDocs.Cli/OptionNames.cs");
    const guide = read("docs/packages/AzurePolicyDocs.Cli/README.md");
    const names = [...options.matchAll(/"(--[a-z-]+)"/g)].map((match) => match[1]);
    assert.ok(names.length > 0);
    for (const option of [...names, "--kind"]) {
        assert.ok(guide.includes(`| \`${option}`), `Missing option table entry for ${option}`);
    }
});

test("the repository overview links to workflows and both NuGet package guides", () => {
    const readme = read("README.md");
    for (const workflow of ["build", "codeql", "release"]) {
        assert.ok(readme.includes(`/actions/workflows/${workflow}.yml/badge.svg`));
    }
    for (const id of ["AzurePolicyDocs.Core", "AzurePolicyDocs.Cli"]) {
        assert.ok(readme.includes(`https://img.shields.io/nuget/v/${id}`));
        assert.ok(readme.includes(`https://www.nuget.org/packages/${id}`));
        assert.ok(readme.includes(`](docs/packages/${id}/README.md)`));
    }
});
