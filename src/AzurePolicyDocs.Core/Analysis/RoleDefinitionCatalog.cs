using AzurePolicyDocs.Model;

namespace AzurePolicyDocs.Analysis;

/// <summary>
/// Friendly names for the built-in roles that remediation policies reference most often. Unknown GUIDs are
/// still documented, just without a name. The list is a convenience, not a dependency on Azure.
/// </summary>
public static class RoleDefinitionCatalog
{
    private static readonly IReadOnlyDictionary<string, string> Names = new Dictionary<
        string,
        string
    >(StringComparer.OrdinalIgnoreCase)
    {
        ["8e3af657-a8ff-443c-a75c-2fe8c4bcb635"] = "Owner",
        ["b24988ac-6180-42a0-ab88-20f7382dd24c"] = "Contributor",
        ["acdd72a7-3385-48ef-bd42-f606fba81ae7"] = "Reader",
        ["18d7d88d-d35e-4fb5-a5c3-7773c20a72d9"] = "User Access Administrator",
        ["36243c78-bf99-498c-9df9-86d9f8d28608"] = "Resource Policy Contributor",
        ["4a9ae827-6dc8-4573-8ac7-8239d42aa03f"] = "Tag Contributor",
        ["4d97b98b-1d4f-4787-a291-c67834d212e7"] = "Network Contributor",
        ["17d1049b-9a84-46fb-8f53-869881c3d3ab"] = "Storage Account Contributor",
        ["81a9662b-bebf-436f-a333-f67b29880f12"] = "Storage Account Key Operator Service Role",
        ["ba92f5b4-2d11-453d-a403-e96b0029c9fe"] = "Storage Blob Data Contributor",
        ["9980e02c-c2be-4d73-94e8-173b1dc7cf3c"] = "Virtual Machine Contributor",
        ["92aaf0da-9dab-42b6-94a3-d43ce8d16293"] = "Log Analytics Contributor",
        ["73c42c96-874c-492b-b04d-ab87d138a893"] = "Log Analytics Reader",
        ["749f88d5-cbae-40b8-bcfc-e573ddc772fa"] = "Monitoring Contributor",
        ["43d0d8ad-25c7-4714-9337-8ba259a9fe05"] = "Monitoring Reader",
        ["fb1c8493-542b-48eb-b624-b4c8fea62acd"] = "Security Admin",
        ["39bc4728-0917-49c7-9d2c-d95423bc2eb4"] = "Security Reader",
        ["f25e0fa2-a7c8-4377-a976-54943a77a395"] = "Key Vault Contributor",
        ["f1a07417-d97a-45cb-824c-7a7467783830"] = "Managed Identity Operator",
        ["e40ec5ca-96e0-45a2-b4ff-59039f2c2b59"] = "Managed Identity Contributor",
        ["056cd41c-7e88-42e1-933e-88ba6a50c9c3"] = "SQL Security Manager",
        ["9b7fa17d-e63e-47b0-bb0a-15c516ac86ec"] = "SQL DB Contributor",
        ["5e467623-bb1f-42f4-a55d-6e525e11384b"] = "Backup Contributor",
        ["ed7f3fbd-7b88-4dd4-9017-9adb7ce333f8"] = "Azure Kubernetes Service Contributor Role",
        ["de139f84-1756-47ae-9be6-808fbbe84772"] = "Website Contributor",
        ["b12aa53e-6015-4669-85d0-8515ebb3ae7f"] = "Private DNS Zone Contributor",
        ["befefa01-2a29-4197-83a8-272ff33ce314"] = "DNS Zone Contributor",
        ["f353d9bd-d4a6-484e-a77a-8050b599b867"] = "Automation Contributor",
        ["cd570a14-e51a-42ad-bac8-bafd67325302"] = "Azure Connected Machine Resource Administrator",
        ["673868aa-7521-48a0-acc6-0f60742d39f5"] = "Data Factory Contributor",
        ["230815da-be43-4aae-9cb4-875f7bd000aa"] = "Cosmos DB Operator",
    };

    /// <summary>Builds a <see cref="RoleDefinition"/> from a full role definition ID or a bare GUID.</summary>
    public static RoleDefinition Resolve(string roleDefinitionId)
    {
        var trimmed = roleDefinitionId.Trim();
        var guid =
            trimmed.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? trimmed;
        Names.TryGetValue(guid, out var name);

        return new RoleDefinition(trimmed, guid, name);
    }
}
