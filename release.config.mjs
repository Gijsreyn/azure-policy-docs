export default {
    branches: ["main"],
    tagFormat: "v${version}",
    plugins: [
        ["@semantic-release/commit-analyzer", { preset: "conventionalcommits" }],
        ["@semantic-release/release-notes-generator", { preset: "conventionalcommits" }],
        "./scripts/nuget-release.mjs",
        [
            "@semantic-release/github",
            {
                assets: [{ path: "artifacts/release/*.nupkg", label: "NuGet package" }],
                successCommentCondition: false,
                failCommentCondition: false,
                releasedLabels: false,
            },
        ],
    ],
};
