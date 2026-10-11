# Resource groups should have an 'environment' tag

Audits or denies resource groups that are created or updated without the required tag. Use the tagName parameter to require a different tag.

| Property | Value |
| :-- | :-- |
| Name | `rg-require-environment-tag` |
| Type | Policy definition |
| Policy type | Custom |
| Mode | `All` |
| Category | Tags |
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

| Resource type |
| :-- |
| `Microsoft.Resources/subscriptions/resourceGroups` |

## Parameters

| Name | Type | Required | Default | Allowed values | Description |
| :-- | :-- | :-- | :-- | :-- | :-- |
| `tagName` | `String` | No | `environment` |  | Name of the tag that every resource group must carry. |
| `effect` | `String` | No | `Audit` | `Audit`, `Deny`, `Disabled` | Enable or disable the execution of the policy. |

## Policy rule

```json
{
  "if": {
    "allOf": [
      {
        "field": "type",
        "equals": "Microsoft.Resources/subscriptions/resourceGroups"
      },
      {
        "field": "[concat('tags[', parameters('tagName'), ']')]",
        "exists": "false"
      }
    ]
  },
  "then": {
    "effect": "[parameters('effect')]"
  }
}
```
