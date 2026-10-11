# MDC Workload Protection Enrollment

Enroll subscriptions in a specific Microsoft Defender for Cloud Workload Protection.

| Property | Value |
| :-- | :-- |
| Name | `90c1d98c-7ad3-4ea3-89c4-6abd261e437` |
| Type | Policy definition |
| Mode | `All` |
| Category | Security Center |
| Version | `1.0.2` |

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

Related resource type: `Microsoft.Security/pricings`, checked with an existence condition, deployed from a template when missing.

Remediation needs these role assignments:

| Role | Role definition ID |
| :-- | :-- |
| Owner | `8e3af657-a8ff-443c-a75c-2fe8c4bcb635` |

## Resource types

| Resource type |
| :-- |
| `Microsoft.Resources/subscriptions` |

## Parameters

| Name | Type | Required | Default | Allowed values | Description |
| :-- | :-- | :-- | :-- | :-- | :-- |
| `effect` | `String` | No | `DeployIfNotExists` | `DeployIfNotExists`, `AuditIfNotExists`, `Disabled` | Deploy the execution of the policy |
| `workLoadProtection` | `String` | No | `Arm` |  | Valid choices (om 7/27/2023) are: AppServices, Arm, ContainerRegistry, Containers, CloudPosture, Dns, KeyVaults, KubernetesService, OpenSourceRelationalDatabases, VirtualMachines. Do not use this Policy (use built-in Policies instead) for SQL and Storage |

## Field aliases

- `Microsoft.Security/pricings/pricingTier`

## Policy rule

```json
{
  "if": {
    "field": "type",
    "equals": "Microsoft.Resources/subscriptions"
  },
  "then": {
    "effect": "[parameters('effect')]",
    "details": {
      "type": "Microsoft.Security/pricings",
      "deploymentScope": "Subscription",
      "existenceScope": "Subscription",
      "name": "[parameters('workLoadProtection')]",
      "roleDefinitionIds": [
        "/providers/Microsoft.Authorization/roleDefinitions/8e3af657-a8ff-443c-a75c-2fe8c4bcb635"
      ],
      "existenceCondition": {
        "field": "Microsoft.Security/pricings/pricingTier",
        "equals": "Standard"
      },
      "deployment": {
        "location": "eastus2",
        "properties": {
          "mode": "incremental",
          "parameters": {
            "workLoadProtection": {
              "value": "[parameters('workLoadProtection')]"
            }
          },
          "template": {
            "$schema": "https://schema.management.azure.com/schemas/2018-05-01/subscriptionDeploymentTemplate.json#",
            "contentVersion": "1.0.0.0",
            "parameters": {
              "resourceProvider": {
                "type": "String"
              }
            },
            "variables": {},
            "resources": [
              {
                "type": "Microsoft.Security/pricings",
                "apiVersion": "2023-01-01",
                "name": "[parameters('workLoadProtection')]",
                "properties": {
                  "pricingTier": "Standard"
                }
              }
            ]
          }
        }
      }
    }
  }
}
```
