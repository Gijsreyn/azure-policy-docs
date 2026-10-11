# Resources should be deployed to allowed locations

Audits or denies resources deployed outside the allowed locations. Resource groups and global resources are not evaluated.

| Property | Value |
| :-- | :-- |
| Name | `allowed-locations` |
| Type | Policy definition |
| Policy type | Custom |
| Mode | `Indexed` |
| Category | General |
| Version | `1.0.0` |

## Navigation

- [Effect](#effect)
- [Resource types](#resource-types)
- [Parameters](#parameters)
- [Policy rule](#policy-rule)

## Effect

The effect is set by the `effect` parameter.

- Default: `Audit`
- Allowed: `Audit`, `Deny`, `Disabled`

## Resource types

_The policy rule has no explicit `type` condition._

## Parameters

| Name | Type | Required | Default | Allowed values | Description |
| :-- | :-- | :-- | :-- | :-- | :-- |
| `allowedLocations` | `Array` | Yes |  |  | The list of locations that resources can be deployed to. |
| `effect` | `String` | No | `Audit` | `Audit`, `Deny`, `Disabled` | Enable or disable the execution of the policy. |

### `allowedLocations`

- Strong type: `location`

## Policy rule

```json
{
  "if": {
    "allOf": [
      {
        "field": "type",
        "notEquals": "Microsoft.Resources/subscriptions/resourceGroups"
      },
      {
        "field": "location",
        "notIn": "[parameters('allowedLocations')]"
      },
      {
        "field": "location",
        "notEquals": "global"
      }
    ]
  },
  "then": {
    "effect": "[parameters('effect')]"
  }
}
```
