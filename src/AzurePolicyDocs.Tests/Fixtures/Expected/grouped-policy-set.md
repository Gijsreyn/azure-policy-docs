# Security baseline

Groups the controls every subscription must meet.

| Property | Value |
| :-- | :-- |
| Name | `security-baseline` |
| Type | Policy set definition |
| Policy type | Custom |
| Category | Security |
| Version | `3.0.0` |
| Policies | 4 |

## Navigation

- [Parameters](#parameters)
- [Policy definitions](#policy-definitions)
- [Groups](#groups)

## Parameters

| Name | Type | Required | Default | Allowed values | Description |
| :-- | :-- | :-- | :-- | :-- | :-- |
| `storageEffect` | `String` | No | `Audit` | `Audit`, `Deny`, `Disabled` | Effect for the storage policies. |
| `allowedLocations` | `Array` | Yes |  |  | Regions that may be used. |

### `allowedLocations`

- Strong type: `location`

## Policy definitions

| Reference ID | Policy definition | Built-in | Version | Groups |
| :-- | :-- | :-- | :-- | :-- |
| `storage-https` | `404c3081-a854-4457-ae30-26a93ef643f9` | Yes | `1.*.*` | `Storage` |
| `allowed-locations` | `e56962a6-4747-49cd-b67b-bf8b01975c4c` | Yes |  | `Location`, `Storage` |
| `custom-tagging` | `rg-require-environment-tag` | No |  |  |
| `custom-no-reference-id` | `custom-no-reference-id` | No |  |  |

### `storage-https`

- Policy definition ID: `/providers/Microsoft.Authorization/policyDefinitions/404c3081-a854-4457-ae30-26a93ef643f9`

| Parameter | Value |
| :-- | :-- |
| `effect` | `[parameters('storageEffect')]` |

### `allowed-locations`

- Policy definition ID: `/providers/Microsoft.Authorization/policyDefinitions/e56962a6-4747-49cd-b67b-bf8b01975c4c`

| Parameter | Value |
| :-- | :-- |
| `listOfAllowedLocations` | `[parameters('allowedLocations')]` |

### `custom-tagging`

| Parameter | Value |
| :-- | :-- |
| `tagName` | `environment` |
| `effect` | `Audit` |

### `custom-no-reference-id`

- Policy definition ID: `/providers/Microsoft.Management/managementGroups/contoso/providers/Microsoft.Authorization/policyDefinitions/custom-no-reference-id`

## Groups

| Name | Display name | Category | Policies | Description |
| :-- | :-- | :-- | :-- | :-- |
| `Storage` | Storage controls | Data protection | 2 | Controls for storage accounts. |
| `Location` | Location controls | Governance | 1 |  |
| `Unused` | No members |  | 0 |  |
