# Public IP addresses are not allowed

| Property | Value |
| :-- | :-- |
| Name | `hard-coded-effect` |
| Type | Policy definition |
| Mode | `All` |

## Navigation

- [Effect](#effect)
- [Resource types](#resource-types)
- [Parameters](#parameters)
- [Policy rule](#policy-rule)

## Effect

The effect is fixed to `Deny`.

## Resource types

| Resource type |
| :-- |
| `Microsoft.Network/publicIPAddresses` |

## Parameters

_This policy definition declares no parameters._

## Policy rule

```json
{
  "if": {
    "field": "type",
    "like": "Microsoft.Network/publicIPAddresses"
  },
  "then": {
    "effect": "Deny"
  }
}
```
