# Add the costCenter tag to resource groups

Adds or replaces the costCenter tag on resource groups that lack it.

| Property | Value |
| :-- | :-- |
| Name | `modify-costcenter-tag` |
| Type | Policy definition |
| Policy type | Custom |
| Mode | `All` |
| Category | Tags |
| Version | `2.0.0` |

## Navigation

- [Effect](#effect)
- [Resource types](#resource-types)
- [Parameters](#parameters)
- [Policy rule](#policy-rule)

## Effect

The effect is computed by an expression that resolves to one of `Modify`, `Disabled`:

```
[if(contains(parameters('enabledFeatures'), 'tags'), 'Modify', 'Disabled')]
```

Remediation needs these role assignments:

| Role | Role definition ID |
| :-- | :-- |
| Tag Contributor | `4a9ae827-6dc8-4573-8ac7-8239d42aa03f` |

Modify operations:

| Operation | Field |
| :-- | :-- |
| `addOrReplace` | `tags['costCenter']` |

## Resource types

| Resource type |
| :-- |
| `Microsoft.Resources/subscriptions/resourceGroups` |

## Parameters

| Name | Type | Required | Default | Allowed values | Description |
| :-- | :-- | :-- | :-- | :-- | :-- |
| `tagValue` | `String` | No | `unassigned` |  | Value written to the costCenter tag. |
| `enabledFeatures` | `Array` | No | See below |  | Include 'tags' to enable this policy. |

### `enabledFeatures`

- Default value:

```json
[
  "tags"
]
```

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
        "field": "tags['costCenter']",
        "exists": "false"
      }
    ]
  },
  "then": {
    "effect": "[if(contains(parameters('enabledFeatures'), 'tags'), 'Modify', 'Disabled')]",
    "details": {
      "roleDefinitionIds": [
        "/providers/Microsoft.Authorization/roleDefinitions/4a9ae827-6dc8-4573-8ac7-8239d42aa03f"
      ],
      "operations": [
        {
          "operation": "addOrReplace",
          "field": "tags['costCenter']",
          "value": "[parameters('tagValue')]"
        }
      ]
    }
  }
}
```
