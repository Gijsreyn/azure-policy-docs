# Summary

Describe the problem and how this change helps users. Link related issues.

## Release impact

- Use a Conventional Commit PR title, such as `fix(cli): preserve filesystem roots`.
- For a breaking change, add `!` to the title and describe the migration. Preserve a `BREAKING CHANGE:` footer in the squash commit.
- Don't bump package versions or add a changelog entry.

## Validation

- [ ] Added or updated tests for the changed behavior.
- [ ] Ran formatting checks, the Release build, and the .NET tests.
- [ ] Ran `npm test` for release tooling changes.
- [ ] Updated the relevant documentation and samples.

See the [contribution guide](../CONTRIBUTING.md) for commit conventions and validation steps.
