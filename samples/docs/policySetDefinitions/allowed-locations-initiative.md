# Allowed Locations Initiative

This initiative contains the policies necessary to limit Azure region deployments for all resources and resource groups.

| Property | Value |
| :-- | :-- |
| Name | `e14e5d7c-9551-4ae2-b8fa-b5d6b9b3c677` |
| Type | Policy set definition |
| Category | Org Governance |
| Version | `1.0.0` |
| Policies | 2 |

## Navigation

- [Parameters](#parameters)
- [Policy definitions](#policy-definitions)

## Parameters

| Name | Type | Required | Default | Allowed values | Description |
| :-- | :-- | :-- | :-- | :-- | :-- |
| `AllowedLocations` | `Array` | No | See below |  |  |

### `AllowedLocations`

- Default value:

```json
[
  "centralus",
  "eastus",
  "eastus2",
  "southcentralus"
]
```

## Policy definitions

| Reference ID | Policy definition | Built-in | Version | Groups |
| :-- | :-- | :-- | :-- | :-- |
| `allowed-locations-resources` | `e56962a6-4747-49cd-b67b-bf8b01975c4c` | Yes |  |  |
| `allowed-locations-resource-groups` | `e765b5de-1225-4ba3-bd56-1ac6695af988` | Yes |  |  |

### `allowed-locations-resources`

- Policy definition ID: `/providers/Microsoft.Authorization/policyDefinitions/e56962a6-4747-49cd-b67b-bf8b01975c4c`

| Parameter | Value |
| :-- | :-- |
| `listOfAllowedLocations` | `[parameters('AllowedLocations')]` |

### `allowed-locations-resource-groups`

- Policy definition ID: `/providers/Microsoft.Authorization/policyDefinitions/e765b5de-1225-4ba3-bd56-1ac6695af988`

| Parameter | Value |
| :-- | :-- |
| `listOfAllowedLocations` | `[parameters('AllowedLocations')]` |
