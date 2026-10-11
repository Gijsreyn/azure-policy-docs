import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { test } from "node:test";
import { analyzeCommits } from "@semantic-release/commit-analyzer";
import { generateNotes } from "@semantic-release/release-notes-generator";
import load from "@commitlint/load";
import lint from "@commitlint/lint";
import config from "../release.config.mjs";

const [, analyzerOptions] = config.plugins[0];
const [, notesOptions] = config.plugins[1];
const context = {
    cwd: process.cwd(),
    logger: { log() {} },
    options: { repositoryUrl: "https://github.com/Gijsreyn/azure-policy-docs.git" },
};

for (const [message, expected] of [
    ["fix(cli): preserve filesystem roots", "patch"],
    ["perf(core): reduce allocations", "patch"],
    ["feat(templates): add a parameter table", "minor"],
    ["feat(cli)!: remove an option", "major"],
    ["fix(core): change the model\n\nBREAKING CHANGE: rename the effect property", "major"],
    ["refactor(core)!: change the public API", "major"],
    ["docs: clarify installation", null],
    ["test: cover null models", null],
    ["ci: update the build workflow", null],
    ["build: update development tools", null],
    ["chore(deps-dev): update release tools", null],
    ["fix(deps): update runtime dependencies", "patch"],
    ["Initial setup", null],
]) {
    test(`${message.split("\n")[0]} selects ${expected ?? "no release"}`, async () => {
        assert.equal(await analyzeCommits(analyzerOptions, {
            ...context,
            commits: [{ hash: "a".repeat(40), message }],
        }), expected);
    });
}

test("the highest impact across unreleased commits wins", async () => {
    const commits = ["fix: correct output", "feat: add a template", "docs!: remove support"]
        .map((message, index) => ({ hash: String(index).repeat(40), message }));
    assert.equal(await analyzeCommits(analyzerOptions, { ...context, commits }), "major");
});

test("release notes include features, fixes, and breaking changes", async () => {
    const notes = await generateNotes(notesOptions, {
        ...context,
        commits: [
            { hash: "a".repeat(40), message: "feat(core): add a table" },
            { hash: "b".repeat(40), message: "fix(cli): preserve roots" },
            { hash: "c".repeat(40), message: "feat(cli)!: remove legacy input\n\nBREAKING CHANGE: use the input option" },
        ],
        lastRelease: { version: "0.1.0", gitTag: "v0.1.0" },
        nextRelease: { version: "1.0.0", gitTag: "v1.0.0" },
    });
    assert.match(notes, /add a table/);
    assert.match(notes, /preserve roots/);
    assert.match(notes, /BREAKING CHANGE/);
    assert.match(notes, /use the input option/);
    assert.match(notes, /compare\/v0\.1\.0\.\.\.v1\.0\.0/);
});

test("PR titles accept conventional and breaking titles but reject plain titles", async () => {
    const loaded = await load({}, { cwd: context.cwd });
    for (const [title, expected] of [
        ["fix(cli): preserve roots", true],
        ["feat(core)!: rename a property", true],
        ["docs: clarify setup", true],
        ["update the docs", false],
        ["feature: add a template", false],
    ]) {
        const result = await lint(title, loaded.rules, {
            parserOpts: loaded.parserPreset?.parserOpts,
        });
        assert.equal(result.valid, expected, title);
    }
});

test("only main releases, NuGet publishes before GitHub, and npm is disabled", () => {
    assert.deepEqual(config.branches, ["main"]);
    assert.equal(config.tagFormat, "v${version}");
    const names = config.plugins.map((plugin) => Array.isArray(plugin) ? plugin[0] : plugin);
    assert.deepEqual(names, [
        "@semantic-release/commit-analyzer",
        "@semantic-release/release-notes-generator",
        "./scripts/nuget-release.mjs",
        "@semantic-release/github",
    ]);
    const github = config.plugins[3][1];
    assert.equal(github.successCommentCondition, false);
    assert.equal(github.failCommentCondition, false);
    assert.equal(github.releasedLabels, false);
    assert.equal(github.assets[0].path, "artifacts/release/*.nupkg");
});

test("release authentication uses a temporary NuGet key and skips previews", () => {
    const workflow = readFileSync(new URL("../.github/workflows/release.yml", import.meta.url), "utf8")
        .replaceAll("\r\n", "\n");
    const publishJob = workflow.slice(workflow.indexOf("\n  publish:"));
    const loginStep = publishJob.match(/      - name: Authenticate to NuGet.org with Trusted Publishing\n([\s\S]*?)(?=\n      - name:)/)?.[1];

    assert.ok(loginStep, "The NuGet login step must precede the release step");
    assert.match(publishJob, /    permissions:\n      contents: write\n      id-token: write/);
    assert.equal((workflow.match(/id-token: write/g) ?? []).length, 1);
    assert.match(loginStep, /if: github\.event_name != 'workflow_dispatch' \|\| !inputs\.dry-run/);
    assert.match(loginStep, /id: nuget-login\n/);
    assert.match(loginStep, /uses: NuGet\/login@v1\n/);
    assert.match(loginStep, /user: \$\{\{ secrets\.NUGET_USER \}\}/);
    assert.match(publishJob, /NUGET_API_KEY: \$\{\{ steps\.nuget-login\.outputs\.NUGET_API_KEY \}\}/);
    assert.doesNotMatch(workflow, /secrets\.NUGET_API_KEY/);
    assert.ok(publishJob.indexOf("run: npm ci") < publishJob.indexOf("id: nuget-login"));
});
