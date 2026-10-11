# Inherit Tag from Resource Group with dynamic notScope

Resources inherit a tag from the resource group. It allows for resource groups and resource types to be excluded from this.

| Property | Value |
| :-- | :-- |
| Name | `5cc2cbfc-e306-4ec6-a141-eea3c79bb2ae` |
| Type | Policy definition |
| Policy type | Custom |
| Mode | `Indexed` |
| Category | Tags |
| Version | `1.0.1` |

## Navigation

- [Effect](#effect)
- [Resource types](#resource-types)
- [Parameters](#parameters)
- [Policy rule](#policy-rule)

## Effect

The effect is set by the `effect` parameter.

- Default: `Modify`
- Allowed: `Disabled`, `Audit`, `Deny`, `Modify`

Remediation needs these role assignments:

| Role | Role definition ID |
| :-- | :-- |
| Contributor | `b24988ac-6180-42a0-ab88-20f7382dd24c` |

Modify operations:

| Operation | Field |
| :-- | :-- |
| `addOrReplace` | `[concat('tags[', parameters('tagName'), ']')]` |

## Resource types

_The policy rule has no explicit `type` condition._

## Parameters

| Name | Type | Required | Default | Allowed values | Description |
| :-- | :-- | :-- | :-- | :-- | :-- |
| `effect` | `string` | No | `Modify` | `Disabled`, `Audit`, `Deny`, `Modify` | Enable or disable the execution of the policy |
| `tagName` | `String` | Yes |  |  | Name of the tag, such as 'environment' |
| `excludedRG` | `Array` | No | See below |  | This array contains all excluded RGs from this policy |
| `excludedResourceTypes` | `Array` | No | See below |  | Exclude certain resource types from this policy |

### `excludedRG`

- Default value:

```json
[
  "synapseworkspace-managedrg-*",
  "databricks*",
  "managed-rg-*",
  "LogAnalyticsDefaultR*",
  "DefaultResourceGroup*",
  "NetworkWatcherRG*",
  "Orca-Security*"
]
```

### `excludedResourceTypes`

- Default value:

```json
[
  "Microsoft.Network/firewallPolicies",
  "Microsoft.Media/mediaservices/liveevents",
  "Microsoft.Sql/virtualclusters",
  "Microsoft.Logic/integrationServiceEnvironments/managedApis",
  "Microsoft.ManagedIdentity/userAssignedIdentities",
  "microsoft.sql/servers/databases",
  "microsoft.sql/managedinstances/databases",
  "microsoft.compute/virtualmachines/extensions",
  "Microsoft.OperationsManagement/solutions"
]
```

## Policy rule

```json
{
  "if": {
    "allOf": [
      {
        "count": {
          "value": "[parameters('excludedResourceTypes')]",
          "name": "excludedResourceTypes",
          "where": {
            "field": "type",
            "like": "[current('excludedResourceTypes')]"
          }
        },
        "equals": 0
      },
      {
        "field": "[concat('tags[', parameters('tagName'), ']')]",
        "notEquals": "[resourceGroup().tags[parameters('tagName')]]"
      },
      {
        "value": "[resourceGroup().tags[parameters('tagName')]]",
        "notEquals": ""
      },
      {
        "count": {
          "value": "[parameters('excludedRG')]",
          "name": "excludedRG",
          "where": {
            "value": "[resourceGroup().name]",
            "like": "[current('excludedRG')]"
          }
        },
        "equals": 0
      }
    ]
  },
  "then": {
    "effect": "[parameters('effect')]",
    "details": {
      "roleDefinitionIds": [
        "/providers/microsoft.authorization/roleDefinitions/b24988ac-6180-42a0-ab88-20f7382dd24c"
      ],
      "operations": [
        {
          "operation": "addOrReplace",
          "field": "[concat('tags[', parameters('tagName'), ']')]",
          "value": "[resourceGroup().tags[parameters('tagName')]]"
        }
      ]
    }
  }
}
```
