import childProcess from "node:child_process";
import { mkdirSync, readdirSync, rmSync } from "node:fs";
import { resolve } from "node:path";

const packageIds = ["AzurePolicyDocs.Core", "AzurePolicyDocs.Cli"];
const source = "https://api.nuget.org/v3/index.json";

function releaseFiles(context) {
    const version = context.nextRelease.version;
    if (!/^\d+\.\d+\.\d+$/.test(version)) {
        throw new Error("NuGet releases require a stable major.minor.patch version.");
    }

    const directory = resolve(context.cwd, "artifacts/release");
    return {
        directory,
        names: packageIds.map((id) => `${id}.${version}.nupkg`),
    };
}

function assertPackages({ directory, names }) {
    const actual = readdirSync(directory).filter((name) => name.endsWith(".nupkg")).sort();
    if (JSON.stringify(actual) !== JSON.stringify([...names].sort())) {
        throw new Error("Expected exactly the Core and CLI NuGet packages for this release version.");
    }
}

function runDotnet(args, context) {
    context.logger.log(`Running dotnet ${args[0]}.`);
    const result = childProcess.spawnSync("dotnet", args, {
        cwd: context.cwd,
        env: context.env,
        stdio: "inherit",
        shell: false,
    });
    if (result.error || result.status !== 0) {
        // Do not include spawn arguments or its error object: publish arguments contain a secret.
        throw new Error(`dotnet ${args[0]} failed (exit code ${result.status ?? "unavailable"}).`);
    }
}

// verifyRelease runs only when commits warrant a release, and before any tag is created.
export function verifyRelease(_pluginConfig, context) {
    releaseFiles(context);
    if (!context.options.dryRun && !context.env.NUGET_API_KEY?.trim()) {
        throw new Error(
            "NuGet/login did not provide NUGET_API_KEY. Check the NuGet.org Trusted Publishing policy, " +
            "the NUGET_USER Actions secret, and id-token: write permission."
        );
    }
}

export function prepare(_pluginConfig, context) {
    if (context.options.dryRun) return;

    const files = releaseFiles(context);
    // Only this dedicated generated directory is cleared, never other build artifacts.
    rmSync(files.directory, { recursive: true, force: true });
    mkdirSync(files.directory, { recursive: true });

    const versionArgs = [
        `-p:Version=${context.nextRelease.version}`,
        "-p:ContinuousIntegrationBuild=true",
    ];
    // Rebuild with the selected version instead of reusing the unversioned CI assemblies.
    runDotnet(["build", "AzurePolicyDocs.sln", "--configuration", "Release", ...versionArgs], context);
    runDotnet([
        "test", "AzurePolicyDocs.sln", "--configuration", "Release", "--no-build",
        "--logger", "trx", "--results-directory", "TestResults/release",
    ], context);
    runDotnet([
        "pack", "AzurePolicyDocs.sln", "--configuration", "Release", "--no-build",
        ...versionArgs, "--output", files.directory,
    ], context);
    assertPackages(files);
}

export function publish(_pluginConfig, context) {
    if (context.options.dryRun) return;

    verifyRelease(_pluginConfig, context);
    const files = releaseFiles(context);
    assertPackages(files);
    // Publish Core first. Stop immediately if either push fails.
    for (const name of files.names) {
        runDotnet([
            "nuget", "push", resolve(files.directory, name),
            "--api-key", context.env.NUGET_API_KEY,
            "--source", source, "--skip-duplicate",
        ], context);
    }

    return {
        name: "NuGet.org packages",
        url: `https://www.nuget.org/packages/AzurePolicyDocs.Cli/${context.nextRelease.version}`,
    };
}

