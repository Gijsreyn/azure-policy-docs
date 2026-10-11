# MDC Workload Protection Enrollment Policy Set

Policy Set enrolls subscriptions in every available Microsoft Defender for Cloud Workload Protection.

| Property | Value |
| :-- | :-- |
| Name | `3fbd9091-1965-4a8f-babb-7c8e97c95d80` |
| Type | Policy set definition |
| Category | Security Center |
| Version | `1.0.0` |
| Policies | 15 |

## Navigation

- [Parameters](#parameters)
- [Policy definitions](#policy-definitions)

## Parameters

| Name | Type | Required | Default | Allowed values | Description |
| :-- | :-- | :-- | :-- | :-- | :-- |
| `workLoadProtectionList` | `Array` | No | See below | `AppServices`, `Arm`, `CloudPosture`, `ContainerRegistry`, `Containers`, `CosmosDbs`, `Dns`, `KeyVaults`, `KubernetesService`, `OpenSourceRelationalDatabases`, `SqlServers`, `SqlServerVirtualMachines`, `VirtualMachines`, `StorageAccounts-Basic`, `StorageAccounts-Full` | List of workloads to enroll in Microsoft Defender for Cloud Workload Protection. Note: StorageAccounts-Basic and StorageAccounts-Full are mutually exclusive. |

### `workLoadProtectionList`

- Default value:

```json
[
  "AppServices",
  "Arm",
  "CloudPosture",
  "ContainerRegistry",
  "Containers",
  "CosmosDbs",
  "Dns",
  "KeyVaults",
  "KubernetesService",
  "OpenSourceRelationalDatabases",
  "SqlServers",
  "SqlServerVirtualMachines",
  "VirtualMachines",
  "StorageAccounts-Basic"
]
```

## Policy definitions

| Reference ID | Policy definition | Built-in | Version | Groups |
| :-- | :-- | :-- | :-- | :-- |
| `AppServices` | `90c1d98c-7ad3-4ea3-89c4-6abd261e437` | No |  |  |
| `Arm` | `90c1d98c-7ad3-4ea3-89c4-6abd261e437` | No |  |  |
| `CloudPosture` | `90c1d98c-7ad3-4ea3-89c4-6abd261e437` | No |  |  |
| `ContainerRegistry` | `90c1d98c-7ad3-4ea3-89c4-6abd261e437` | No |  |  |
| `Containers` | `90c1d98c-7ad3-4ea3-89c4-6abd261e437` | No |  |  |
| `CosmosDbs` | `90c1d98c-7ad3-4ea3-89c4-6abd261e437` | No |  |  |
| `Dns` | `90c1d98c-7ad3-4ea3-89c4-6abd261e437` | No |  |  |
| `KeyVaults` | `90c1d98c-7ad3-4ea3-89c4-6abd261e437` | No |  |  |
| `KubernetesService` | `90c1d98c-7ad3-4ea3-89c4-6abd261e437` | No |  |  |
| `OpenSourceRelationalDatabases` | `90c1d98c-7ad3-4ea3-89c4-6abd261e437` | No |  |  |
| `SqlServers` | `90c1d98c-7ad3-4ea3-89c4-6abd261e437` | No |  |  |
| `SqlServerVirtualMachines` | `90c1d98c-7ad3-4ea3-89c4-6abd261e437` | No |  |  |
| `VirtualMachines` | `90c1d98c-7ad3-4ea3-89c4-6abd261e437` | No |  |  |
| `StorageAccounts-Basic` | `17bc14a7-92e1-4551-8b8c-80f36953e166` | Yes |  |  |
| `StorageAccounts-Full` | `cfdc5972-75b3-4418-8ae1-7f5c36839390` | Yes |  |  |

### `AppServices`

| Parameter | Value |
| :-- | :-- |
| `effect` | `[if(contains(parameters('workLoadProtectionList'),'AppServices'),'DeployIfNotExists','Disabled')]` |
| `workLoadProtection` | `AppServices` |

### `Arm`

| Parameter | Value |
| :-- | :-- |
| `effect` | `[if(contains(parameters('workLoadProtectionList'),'Arm'),'DeployIfNotExists','Disabled')]` |
| `workLoadProtection` | `Arm` |

### `CloudPosture`

| Parameter | Value |
| :-- | :-- |
| `effect` | `[if(contains(parameters('workLoadProtectionList'),'CloudPosture'),'DeployIfNotExists','Disabled')]` |
| `workLoadProtection` | `CloudPosture` |

### `ContainerRegistry`

| Parameter | Value |
| :-- | :-- |
| `effect` | `[if(contains(parameters('workLoadProtectionList'),'ContainerRegistry'),'DeployIfNotExists','Disabled')]` |
| `workLoadProtection` | `ContainerRegistry` |

### `Containers`

| Parameter | Value |
| :-- | :-- |
| `effect` | `[if(contains(parameters('workLoadProtectionList'),'Containers'),'DeployIfNotExists','Disabled')]` |
| `workLoadProtection` | `Containers` |

### `CosmosDbs`

| Parameter | Value |
| :-- | :-- |
| `effect` | `[if(contains(parameters('workLoadProtectionList'),'CosmosDbs'),'DeployIfNotExists','Disabled')]` |
| `workLoadProtection` | `CosmosDbs` |

### `Dns`

| Parameter | Value |
| :-- | :-- |
| `effect` | `[if(contains(parameters('workLoadProtectionList'),'Dns'),'DeployIfNotExists','Disabled')]` |
| `workLoadProtection` | `Dns` |

### `KeyVaults`

| Parameter | Value |
| :-- | :-- |
| `effect` | `[if(contains(parameters('workLoadProtectionList'),'KeyVaults'),'DeployIfNotExists','Disabled')]` |
| `workLoadProtection` | `KeyVaults` |

### `KubernetesService`

| Parameter | Value |
| :-- | :-- |
| `effect` | `[if(contains(parameters('workLoadProtectionList'),'KubernetesService'),'DeployIfNotExists','Disabled')]` |
| `workLoadProtection` | `KubernetesService` |

### `OpenSourceRelationalDatabases`

| Parameter | Value |
| :-- | :-- |
| `effect` | `[if(contains(parameters('workLoadProtectionList'),'OpenSourceRelationalDatabases'),'DeployIfNotExists','Disabled')]` |
| `workLoadProtection` | `OpenSourceRelationalDatabases` |

### `SqlServers`

| Parameter | Value |
| :-- | :-- |
| `effect` | `[if(contains(parameters('workLoadProtectionList'),'SqlServers'),'DeployIfNotExists','Disabled')]` |
| `workLoadProtection` | `VirtualMachines` |

### `SqlServerVirtualMachines`

| Parameter | Value |
| :-- | :-- |
| `effect` | `[if(contains(parameters('workLoadProtectionList'),'SqlServerVirtualMachines'),'DeployIfNotExists','Disabled')]` |
| `workLoadProtection` | `VirtualMachines` |

### `VirtualMachines`

| Parameter | Value |
| :-- | :-- |
| `effect` | `[if(contains(parameters('workLoadProtectionList'),'change'),'DeployIfNotExists','Disabled')]` |
| `workLoadProtection` | `VirtualMachines` |

### `StorageAccounts-Basic`

- Policy definition ID: `/providers/Microsoft.Authorization/policyDefinitions/17bc14a7-92e1-4551-8b8c-80f36953e166`

| Parameter | Value |
| :-- | :-- |
| `effect` | `[if(and(contains(parameters('workLoadProtectionList'),'StorageAccounts-Basic'),not(contains(parameters('workLoadProtectionList'),'StorageAccounts-Full'))),'DeployIfNotExists','Disabled')]` |

### `StorageAccounts-Full`

- Policy definition ID: `/providers/Microsoft.Authorization/policyDefinitions/cfdc5972-75b3-4418-8ae1-7f5c36839390`

| Parameter | Value |
| :-- | :-- |
| `effect` | `[if(contains(parameters('workLoadProtectionList'),'StorageAccounts-Full'),'DeployIfNotExists','Disabled')]` |
