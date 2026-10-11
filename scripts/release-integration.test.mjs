import assert from "node:assert/strict";
import { execFileSync } from "node:child_process";
import { mkdtempSync, rmSync } from "node:fs";
import { createRequire } from "node:module";
import { tmpdir } from "node:os";
import { resolve } from "node:path";
import { test } from "node:test";
import { pathToFileURL } from "node:url";
import config from "../release.config.mjs";

const require = createRequire(import.meta.url);

function repository(t) {
    const directory = mkdtempSync(resolve(tmpdir(), "azurepolicydocs-semantic-"));
    t.after(() => rmSync(directory, { recursive: true, force: true }));
    const remote = resolve(directory, "remote.git");
    const cwd = resolve(directory, "work");
    // Use only a local bare remote. Never read credentials or contact GitHub/NuGet.
    const env = Object.fromEntries(Object.entries(process.env).filter(([key]) =>
        !/^(GIT_|GH_|GITHUB_|NUGET_|CI$|TF_BUILD$|BUILD_|NODE_TEST_)/i.test(key)));
    Object.assign(env, { GIT_CONFIG_NOSYSTEM: "1", GIT_CONFIG_GLOBAL: process.platform === "win32" ? "NUL" : "/dev/null" });
    function git(...args) {
        return execFileSync("git", args, { cwd: directory, env, encoding: "utf8", stdio: ["ignore", "pipe", "pipe"] }).trim();
    }
    git("init", "--bare", "--initial-branch=main", remote);
    git("clone", remote, cwd);
    git("-C", cwd, "checkout", "-b", "main");
    git("-C", cwd, "config", "user.name", "Release test");
    git("-C", cwd, "config", "user.email", "release-test@example.invalid");
    git("-C", cwd, "config", "commit.gpgsign", "false");
    git("-C", cwd, "commit", "--allow-empty", "-m", "chore: initial setup");
    return { cwd, env, git, remote };
}

async function preview(repo) {
    const options = {
        ...config,
        ci: false,
        dryRun: true,
        repositoryUrl: repo.remote.replaceAll("\\", "/"),
        // Keep the real analyzer and NuGet verification; omit the network-only GitHub plugin.
        plugins: [
            [require.resolve("@semantic-release/commit-analyzer"), config.plugins[0][1]],
            resolve(import.meta.dirname, "nuget-release.mjs"),
        ],
    };
    // semantic-release hooks stdout. Isolate it from the Node test runner's reporter.
    const code = `
        import release from ${JSON.stringify(pathToFileURL(require.resolve("semantic-release")).href)};
        import { Writable } from "node:stream";
        const sink = new Writable({ write(chunk, encoding, callback) { callback(); } });
        const result = await release(${JSON.stringify(options)}, {
            cwd: ${JSON.stringify(repo.cwd)}, env: process.env, stdout: sink, stderr: sink,
        });
        process.stdout.write(JSON.stringify(result));
    `;
    return JSON.parse(execFileSync(process.execPath, ["--input-type=module", "-e", code], {
        cwd: repo.cwd, env: repo.env, encoding: "utf8", stdio: ["ignore", "pipe", "pipe"],
    }));
}

for (const [baseline, message, expected] of [
    [null, "feat: add documentation generation", "1.0.0"],
    ["0.1.0", "fix: preserve roots", "0.1.1"],
    ["0.1.0", "feat: add a template", "0.2.0"],
    ["0.1.0", "feat!: remove legacy input", "1.0.0"],
    ["0.1.0", "docs: clarify setup", null],
]) {
    test(`isolated preview from ${baseline ?? "no tags"} selects ${expected ?? "no release"}`, async (t) => {
        const repo = repository(t);
        if (baseline) repo.git("-C", repo.cwd, "tag", `v${baseline}`);
        repo.git("-C", repo.cwd, "commit", "--allow-empty", "-m", message);
        repo.git("-C", repo.cwd, "push", "origin", "main", "--tags");
        const before = repo.git("--git-dir", repo.remote, "show-ref");
        const result = await preview(repo);
        if (expected) {
            assert.equal(result.nextRelease.version, expected);
        } else {
            assert.equal(result, false);
        }
        assert.equal(repo.git("--git-dir", repo.remote, "show-ref"), before);
    });
}
