# Sample policy catalog

Generated from the files under samples/ with policydocs.

4 policy definition(s) and 2 policy set definition(s).

## Policy definitions

| Policy | Category | Mode | Effects | Description |
| :-- | :-- | :-- | :-- | :-- |
| [Resources should be deployed to allowed locations](policyDefinitions/allowed-locations.md) | General | `Indexed` | `Audit`, `Deny`, `Disabled` | Audits or denies resources deployed outside the allowed locations. Resource groups and global resources are not evaluated. |
| [MDC Workload Protection Enrollment](policyDefinitions/mdc-workload-protection-enrollment.md) | Security Center | `All` | `DeployIfNotExists`, `AuditIfNotExists`, `Disabled` | Enroll subscriptions in a specific Microsoft Defender for Cloud Workload Protection. |
| [Inherit Tag from Resource Group with dynamic notScope](policyDefinitions/resources-inherit-rg-tag-dynamic-notscope.md) | Tags | `Indexed` | `Disabled`, `Audit`, `Deny`, `Modify` | Resources inherit a tag from the resource group. It allows for resource groups and resource types to be excluded from this. |
| [Resource groups should have an 'environment' tag](policyDefinitions/rg-require-environment-tag.md) | Tags | `All` | `Audit`, `Deny`, `Disabled` | Audits or denies resource groups that are created or updated without the required tag. Use the tagName parameter to require a different tag. |

## Policy set definitions

| Policy set | Category | Policies | Description |
| :-- | :-- | :-- | :-- |
| [Allowed Locations Initiative](policySetDefinitions/allowed-locations-initiative.md) | Org Governance | 2 | This initiative contains the policies necessary to limit Azure region deployments for all resources and resource groups. |
| [MDC Workload Protection Enrollment Policy Set](policySetDefinitions/mdc-workload-protection-enrollment-policy-set.md) | Security Center | 15 | Policy Set enrolls subscriptions in every available Microsoft Defender for Cloud Workload Protection. |
