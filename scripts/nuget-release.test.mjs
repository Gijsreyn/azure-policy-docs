import assert from "node:assert/strict";
import childProcess from "node:child_process";
import { existsSync, mkdirSync, mkdtempSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { resolve } from "node:path";
import { test } from "node:test";
import { prepare, publish, verifyRelease } from "./nuget-release.mjs";

function fixture(t) {
    const cwd = mkdtempSync(resolve(tmpdir(), "policydocs-release-"));
    t.after(() => rmSync(cwd, { recursive: true, force: true }));
    return {
        cwd,
        env: { NUGET_API_KEY: "test-only-key" },
        options: { dryRun: false },
        nextRelease: { version: "1.2.3" },
        logger: { log() {} },
    };
}

function packages(context) {
    const directory = resolve(context.cwd, "artifacts/release");
    mkdirSync(directory, { recursive: true });
    for (const id of ["AzurePolicyDocs.Core", "AzurePolicyDocs.Cli"]) {
        writeFileSync(resolve(directory, `${id}.${context.nextRelease.version}.nupkg`), "package fixture");
    }
}

test("verifyRelease requires a key only for a real release", (t) => {
    const context = fixture(t);
    context.env = {};
    assert.throws(() => verifyRelease({}, context), /NUGET_API_KEY/);
    context.env.NUGET_API_KEY = "   ";
    assert.throws(() => verifyRelease({}, context), /NUGET_API_KEY/);
    context.options.dryRun = true;
    assert.doesNotThrow(() => verifyRelease({}, context));
});

test("a preview never builds, removes artifacts, or publishes", (t) => {
    const context = fixture(t);
    context.options.dryRun = true;
    packages(context);
    const spawn = t.mock.method(childProcess, "spawnSync", () => assert.fail("Must not run dotnet"));
    prepare({}, context);
    publish({}, context);
    assert.equal(spawn.mock.callCount(), 0);
    assert.ok(existsSync(resolve(context.cwd, "artifacts/release/AzurePolicyDocs.Core.1.2.3.nupkg")));
});

for (const version of ["", "v1.2.3", "1.2.3-preview.1", "../other", "1.2.3; echo unsafe"]) {
    test(`rejects invalid stable version ${JSON.stringify(version)}`, (t) => {
        const context = fixture(t);
        context.nextRelease.version = version;
        assert.throws(() => verifyRelease({}, context), /stable/);
        assert.throws(() => prepare({}, context), /stable/);
        assert.throws(() => publish({}, context), /stable/);
    });
}

test("prepare builds and tests versioned assemblies before packing both packages", (t) => {
    const context = fixture(t);
    packages(context);
    const stale = resolve(context.cwd, "artifacts/release/Old.0.1.0.nupkg");
    const unrelated = resolve(context.cwd, "artifacts/keep.txt");
    writeFileSync(stale, "stale");
    writeFileSync(unrelated, "keep");
    const spawn = t.mock.method(childProcess, "spawnSync", (_command, args) => {
        if (args[0] === "pack") packages(context);
        return { status: 0 };
    });

    prepare({}, context);

    const calls = spawn.mock.calls.map((call) => call.arguments);
    assert.deepEqual(calls.map(([, args]) => args[0]), ["build", "test", "pack"]);
    for (const index of [0, 2]) {
        assert.ok(calls[index][1].includes("-p:Version=1.2.3"));
        assert.ok(calls[index][1].includes("-p:ContinuousIntegrationBuild=true"));
    }
    assert.ok(!calls[0][1].includes("--no-build"));
    assert.ok(calls[1][1].includes("--no-build"));
    assert.ok(calls[2][1].includes("--no-build"));
    for (const [command, , options] of calls) {
        assert.equal(command, "dotnet");
        assert.equal(options.cwd, context.cwd);
        assert.equal(options.shell, false);
    }
    assert.equal(existsSync(stale), false);
    assert.equal(readFileSync(unrelated, "utf8"), "keep");
});

for (const failedStep of ["build", "test", "pack"]) {
    test(`prepare stops when dotnet ${failedStep} fails`, (t) => {
        const context = fixture(t);
        const spawn = t.mock.method(childProcess, "spawnSync", (_command, args) => ({
            status: args[0] === failedStep ? 1 : 0,
        }));
        assert.throws(() => prepare({}, context), new RegExp(`dotnet ${failedStep} failed`));
        assert.equal(spawn.mock.calls.at(-1).arguments[1][0], failedStep);
    });
}

test("prepare fails if pack produces no packages", (t) => {
    const context = fixture(t);
    t.mock.method(childProcess, "spawnSync", () => ({ status: 0 }));
    assert.throws(() => prepare({}, context), /exactly the Core and CLI/);
});

test("publish pushes only this version's Core and CLI packages in order", (t) => {
    const context = fixture(t);
    packages(context);
    const spawn = t.mock.method(childProcess, "spawnSync", () => ({ status: 0 }));
    const result = publish({}, context);
    const calls = spawn.mock.calls.map((call) => call.arguments[1]);
    assert.equal(calls.length, 2);
    assert.match(calls[0][2], /AzurePolicyDocs\.Core\.1\.2\.3\.nupkg$/);
    assert.match(calls[1][2], /AzurePolicyDocs\.Cli\.1\.2\.3\.nupkg$/);
    for (const args of calls) {
        assert.deepEqual(args.slice(0, 2), ["nuget", "push"]);
        assert.ok(args.includes("--skip-duplicate"));
        assert.ok(args.includes("https://api.nuget.org/v3/index.json"));
        assert.equal(args[args.indexOf("--api-key") + 1], "test-only-key");
    }
    assert.equal(result.url, "https://www.nuget.org/packages/AzurePolicyDocs.Cli/1.2.3");
});

for (const issue of ["missing", "stale"]) {
    test(`publish refuses ${issue} packages before pushing anything`, (t) => {
        const context = fixture(t);
        packages(context);
        if (issue === "missing") {
            rmSync(resolve(context.cwd, "artifacts/release/AzurePolicyDocs.Cli.1.2.3.nupkg"));
        } else {
            writeFileSync(resolve(context.cwd, "artifacts/release/AzurePolicyDocs.Cli.0.1.0.nupkg"), "stale");
        }
        const spawn = t.mock.method(childProcess, "spawnSync", () => assert.fail("Must not push"));
        assert.throws(() => publish({}, context), /exactly the Core and CLI/);
        assert.equal(spawn.mock.callCount(), 0);
    });
}

test("publish stops on failure and never exposes secret-bearing spawn errors", (t) => {
    const context = fixture(t);
    packages(context);
    const spawn = t.mock.method(childProcess, "spawnSync", () => ({
        status: null,
        error: new Error(`spawn failed: ${context.env.NUGET_API_KEY}`),
    }));
    assert.throws(() => publish({}, context), (error) => {
        assert.match(error.message, /dotnet nuget failed/);
        assert.ok(!error.message.includes(context.env.NUGET_API_KEY));
        return true;
    });
    assert.equal(spawn.mock.callCount(), 1);
});
