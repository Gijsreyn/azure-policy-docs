# Deploy diagnostic settings for storage accounts | preview

Deploys a diagnostic setting that routes storage account logs and metrics to a Log Analytics workspace.
The second line exercises multi-line descriptions.

| Property | Value |
| :-- | :-- |
| Name | `deploy-storage-diagnostics` |
| Type | Policy definition |
| Policy type | Custom |
| Mode | `Indexed` |
| Category | Monitoring |
| Version | `1.2.0` |
| Preview | Yes |

## Navigation

- [Effect](#effect)
- [Resource types](#resource-types)
- [Parameters](#parameters)
- [Field aliases](#field-aliases)
- [Policy rule](#policy-rule)

## Effect

The effect is set by the `effect` parameter.

- Default: `DeployIfNotExists`
- Allowed: `DeployIfNotExists`, `AuditIfNotExists`, `Disabled`

Related resource type: `Microsoft.Insights/diagnosticSettings`, checked with an existence condition, deployed from a template when missing.

Remediation needs these role assignments:

| Role | Role definition ID |
| :-- | :-- |
| Contributor | `b24988ac-6180-42a0-ab88-20f7382dd24c` |
| Unknown role | `00000000-1111-2222-3333-444444444444` |

## Resource types

| Resource type |
| :-- |
| `Microsoft.Storage/*` |
| `Microsoft.Storage/storageAccounts` |
| `Microsoft.Storage/storageAccounts/blobServices` |

## Parameters

| Name | Type | Required | Default | Allowed values | Description |
| :-- | :-- | :-- | :-- | :-- | :-- |
| `effect` | `String` | No | `DeployIfNotExists` | `DeployIfNotExists`, `AuditIfNotExists`, `Disabled` | Enable or disable the execution of the policy. |
| `logAnalytics` | `String` | Yes |  |  | Resource ID of the workspace. The portal creates the role assignment on it. |
| `retentionDays` | `Integer` | No | `30` |  | Days to keep the logs. |
| `enabledCategories` | `Array` | No | See below |  | Categories to enable. |
| `metricsEnabled` | `Boolean` | No | `true` |  | Whether metrics are collected \| too. |
| `settings` | `Object` | No | See below |  | Free-form settings. |
| `legacyOption` | `String` | No | `""` |  | No longer used. |

### `logAnalytics`

- Strong type: `omsWorkspace`

- Assign permissions: Yes

### `enabledCategories`

- Default value:

```json
[
  "StorageRead",
  "StorageWrite",
  "StorageDelete"
]
```

### `settings`

- Default value:

```json
{
  "profileName": "setByPolicy",
  "tags": {
    "env": "prod"
  }
}
```

### `legacyOption`

- Deprecated: Yes

## Field aliases

- `Microsoft.Insights/diagnosticSettings/logs.enabled`
- `Microsoft.Insights/diagnosticSettings/workspaceId`
- `Microsoft.Storage/storageAccounts/supportsHttpsTrafficOnly`

## Policy rule

```json
{
  "if": {
    "allOf": [
      {
        "field": "type",
        "equals": "Microsoft.Storage/storageAccounts"
      },
      {
        "field": "kind",
        "in": [
          "StorageV2",
          "BlobStorage"
        ]
      },
      {
        "field": "tags['exclude']",
        "exists": "false"
      },
      {
        "field": "Microsoft.Storage/storageAccounts/supportsHttpsTrafficOnly",
        "equals": "true"
      },
      {
        "anyOf": [
          {
            "field": "type",
            "in": [
              "Microsoft.Storage/storageAccounts/blobServices",
              "[parameters('settings')]"
            ]
          },
          {
            "field": "type",
            "like": "Microsoft.Storage/*"
          }
        ]
      }
    ]
  },
  "then": {
    "effect": "[parameters('effect')]",
    "details": {
      "type": "Microsoft.Insights/diagnosticSettings",
      "roleDefinitionIds": [
        "/providers/Microsoft.Authorization/roleDefinitions/b24988ac-6180-42a0-ab88-20f7382dd24c",
        "/providers/Microsoft.Authorization/roleDefinitions/00000000-1111-2222-3333-444444444444"
      ],
      "existenceCondition": {
        "allOf": [
          {
            "field": "Microsoft.Insights/diagnosticSettings/workspaceId",
            "equals": "[parameters('logAnalytics')]"
          },
          {
            "field": "Microsoft.Insights/diagnosticSettings/logs.enabled",
            "equals": "true"
          }
        ]
      },
      "deployment": {
        "properties": {
          "mode": "incremental",
          "template": {
            "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#",
            "contentVersion": "1.0.0.0",
            "resources": []
          }
        }
      }
    }
  }
}
```
