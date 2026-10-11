export default {
    extends: ["@commitlint/config-conventional"],
    rules: {
        // A breaking change can use either ! or a BREAKING CHANGE footer.
        "breaking-change-exclamation-mark": [0],
    },
};

