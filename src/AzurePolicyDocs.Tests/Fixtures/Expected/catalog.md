# Contoso policies

3 policy definition(s) and 2 policy set definition(s).

## Policy definitions

| Policy | Category | Mode | Effects | Description |
| :-- | :-- | :-- | :-- | :-- |
| [Public IP addresses are not allowed](Definitions/hard-coded-effect.md) |  | `All` | `Deny` |  |
| [Deploy diagnostic settings for storage accounts \| preview](Definitions/comprehensive-definition.md) | Monitoring | `Indexed` | `DeployIfNotExists`, `AuditIfNotExists`, `Disabled` | Deploys a diagnostic setting that routes storage account logs and metrics to a Log Analytics workspace. The second line exercises multi-line descriptions. |
| [Add the costCenter tag to resource groups](Definitions/modify-expression-effect.md) | Tags | `All` | `Modify`, `Disabled` | Adds or replaces the costCenter tag on resource groups that lack it. |

## Policy set definitions

| Policy set | Category | Policies | Description |
| :-- | :-- | :-- | :-- |
| [Allowed Locations Initiative](PolicySets/allowed-locations-initiative.md) | Org Governance | 2 | This initiative contains the policies necessary to limit Azure region deployments for all resources and resource groups. |
| [Security baseline](PolicySets/grouped-policy-set.md) | Security | 4 | Groups the controls every subscription must meet. |
