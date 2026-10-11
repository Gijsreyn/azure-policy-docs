# Template model

Templates receive two root objects: `policy` and `custom`. The catalog template receives `catalog` and `custom`. Property names are camelCase and fixed; they are built by hand in `PolicyDocumentationScriptModelFactory` and do not follow the C# record names, so a refactor in the library never changes what a template sees.

Missing values are `null`. Collections are never `null`; an empty collection has `size` 0.

## `policy`

| Field | Type | Description |
| :-- | :-- | :-- |
| `kind` | string | `policyDefinition` or `policySetDefinition`. |
| `name` | string | The resource `name`, or the file name without extension when the file holds only the property bag. |
| `displayName` | string or null | `properties.displayName`. |
| `description` | string or null | `properties.description`, trimmed. |
| `policyType` | string or null | `Custom`, `BuiltIn`, or `Static`. |
| `mode` | string or null | `properties.mode`. Policy definitions only. |
| `version` | string or null | `properties.version`, falling back to `metadata.version`. |
| `category` | string or null | `metadata.category`. |
| `preview` | bool | `metadata.preview`, accepting `true` or `"true"`. |
| `deprecated` | bool | `metadata.deprecated`, accepting `true` or `"true"`. |
| `path` | string | Full path of the input file. The built-in templates don't render it, so generated files stay portable. |
| `custom` | object | The same object as the root `custom`. |
| `metadata` | object or null | The whole `metadata` object converted to script values, so `policy.metadata.owner` works for any property an author added. |
| `parameters` | array of parameter | Declared parameters in declaration order. |
| `effect` | effect or null | Policy definitions only. Null when the rule has no `then.effect`. |
| `effectDetails` | effect details or null | Policy definitions only. Null when the rule has no `then.details`. |
| `resourceTypes` | array of string | Resource types from positive `type` conditions (`equals`, `in`, `like`, `match`, `matchInsensitively`, `contains`) under `if`, sorted. Expressions are excluded. |
| `fieldAliases` | array of string | Field aliases (fields containing `/`, excluding `tags[...]`) referenced under `if` and the existence condition, sorted. |
| `policyRuleJson` | string or null | The `policyRule` pretty-printed with two-space indentation and relaxed escaping. |
| `policyRuleFence` | string or null | A backtick fence longer than any backtick run inside `policyRuleJson`. |
| `policyDefinitions` | array of member | Policy set definitions only. |
| `policyCount` | int | Number of members. 0 for policy definitions. |
| `groups` | array of group | `policyDefinitionGroups`. Policy set definitions only. |

### parameter

| Field | Type | Description |
| :-- | :-- | :-- |
| `name` | string | Parameter name. |
| `type` | string | `type` as declared, for example `String` or `Array`. `string` when missing. |
| `displayName` | string or null | `metadata.displayName`. |
| `description` | string or null | `metadata.description`. |
| `required` | bool | True when the parameter has no `defaultValue`. |
| `defaultValue` | string or null | Single-line form: strings as-is (`""` when empty), everything else as compact JSON. |
| `defaultValueJson` | string or null | Pretty-printed JSON. Contains `\n` for non-empty arrays and objects; use that to decide between inline and block rendering. |
| `defaultValueFence` | string or null | Fence for `defaultValueJson`. |
| `allowedValues` | array of string | Each value in single-line form. |
| `strongType` | string or null | `metadata.strongType`. |
| `assignPermissions` | bool | `metadata.assignPermissions`. |
| `deprecated` | bool | `metadata.deprecated`. |
| `effect` | bool | True for the parameter that `then.effect` references, or for a parameter named `effect` when the rule has no parameter reference. |

### effect

| Field | Type | Description |
| :-- | :-- | :-- |
| `raw` | string | `then.effect` as written. |
| `kind` | string | `literal`, `parameter`, or `expression`. |
| `parameterName` | string or null | For `parameter`: the referenced parameter. |
| `defaultEffect` | string or null | For `literal`: the value. For `parameter`: the parameter's default. Null for `expression`. |
| `allowedEffects` | array of string | For `literal`: the value. For `parameter`: the parameter's `allowedValues`. For `expression`: the known effect names quoted inside the expression, in order of appearance. |

### effect details

| Field | Type | Description |
| :-- | :-- | :-- |
| `resourceType` | string or null | `details.type`, the related resource an existence effect looks for. |
| `roleDefinitions` | array of role | `details.roleDefinitionIds` resolved to `id`, `guid`, and `name` (null when the GUID is not a common built-in role). |
| `hasExistenceCondition` | bool | Whether `details.existenceCondition` is present. |
| `hasDeployment` | bool | Whether `details.deployment` is present. |
| `modifyOperations` | array of operation | `details.operations`, each with `operation` and `field`. |

### member (policy set definitions)

| Field | Type | Description |
| :-- | :-- | :-- |
| `referenceId` | string | `policyDefinitionReferenceId`, falling back to the policy name or `policy-<index>`. |
| `policyDefinitionId` | string or null | Full resource ID when given. |
| `policyDefinitionName` | string or null | EPAC's short name for custom definitions. |
| `name` | string | `policyDefinitionName`, else the last segment of `policyDefinitionId`, else the reference ID. |
| `builtIn` | bool | True when the ID starts with `/providers/Microsoft.Authorization/policyDefinitions/`. |
| `definitionVersion` | string or null | Pinned version such as `1.*.*`. |
| `groupNames` | array of string | Groups the member belongs to. |
| `parameters` | array of `{ name, value }` | Parameter values passed to the member, with the `{ "value": ... }` wrapper removed and the value in single-line form. |

### group (policy set definitions)

| Field | Type | Description |
| :-- | :-- | :-- |
| `name` | string | Group name, referenced by members' `groupNames`. |
| `displayName`, `category`, `description`, `additionalMetadataId` | string or null | As declared. |
| `memberCount` | int | Number of members that reference the group. |

## `custom`

A flat object of string values. Values come from `--custom-template-value key=value` and `--custom-template-value-file-path file.json`, applied in command-line order; the last occurrence of a key wins. Library callers pass them through `PolicyDocumentationGenerationOptions.CustomValues`.

## `catalog`

| Field | Type | Description |
| :-- | :-- | :-- |
| `custom` | object | Same as the root `custom`. |
| `policyDefinitions` | array of entry | Sorted by category, then display name, then path. |
| `policySetDefinitions` | array of entry | Same ordering. |

### entry

| Field | Type | Description |
| :-- | :-- | :-- |
| `kind`, `name`, `displayName`, `description`, `category`, `mode`, `version` | | As on `policy`. |
| `defaultEffect` | string or null | As on `effect`. |
| `allowedEffects` | array of string | As on `effect`. |
| `policyCount` | int | Members of a policy set, 0 for a policy definition. |
| `path` | string | Source file, relative to the catalog's directory, with forward slashes. |
| `documentationPath` | string or null | The generated document for this entry, relative to the catalog's directory. |

## Rendering guarantees

- Line endings in the output are `\n`; the output ends with exactly one `\n`.
- Loops are limited to 100,000 iterations and strings are not truncated.
- `include "name"` resolves against the include root. Missing includes fail the render with the include name in the message.
