using AzurePolicyDocs.Model;
using AzurePolicyDocs.Parsing;
using FluentAssertions;
using Xunit;

namespace AzurePolicyDocs.Tests;

public class PolicyDocumentationModelBuilderTests
{
    private static PolicyDocumentationModel Build(params string[] fixture) =>
        PolicyDocumentationModelBuilder.Build(
            PolicyDocumentParser.ParseFile(Fixtures.Path(fixture))
        );

    [Fact]
    public void Build_Comprehensive_ReadsTopLevelProperties()
    {
        var model = Build("Definitions", "comprehensive-definition.json");

        model.Kind.Should().Be(PolicyDocumentKind.PolicyDefinition);
        model.Name.Should().Be("deploy-storage-diagnostics");
        model.DisplayName.Should().Be("Deploy diagnostic settings for storage accounts | preview");
        model.Description.Should().StartWith("Deploys a diagnostic setting").And.Contain("\n");
        model.PolicyType.Should().Be("Custom");
        model.Mode.Should().Be("Indexed");
        model.Category.Should().Be("Monitoring");
        model.IsPreview.Should().BeTrue();
        model.IsDeprecated.Should().BeFalse();
        model.Metadata.Should().NotBeNull();
    }

    [Fact]
    public void Build_PropertiesVersionWinsOverMetadataVersion()
    {
        Build("Definitions", "comprehensive-definition.json").Version.Should().Be("1.2.0");
        Build("PolicySets", "grouped-policy-set.json").Version.Should().Be("3.0.0");
        Build("PolicySets", "allowed-locations-initiative.jsonc").Version.Should().Be("1.0.0");
    }

    [Fact]
    public void Build_Parameters_KeepDeclarationOrderAndDetails()
    {
        var parameters = Build("Definitions", "comprehensive-definition.json").Parameters;

        parameters
            .Select(parameter => parameter.Name)
            .Should()
            .Equal(
                "effect",
                "logAnalytics",
                "retentionDays",
                "enabledCategories",
                "metricsEnabled",
                "settings",
                "legacyOption"
            );

        var effect = parameters[0];
        effect.IsEffect.Should().BeTrue();
        effect.IsRequired.Should().BeFalse();
        effect.DefaultValue.Should().Be("DeployIfNotExists");
        effect.AllowedValues.Should().Equal("DeployIfNotExists", "AuditIfNotExists", "Disabled");

        var workspace = parameters[1];
        workspace.IsRequired.Should().BeTrue();
        workspace.DefaultValue.Should().BeNull();
        workspace.StrongType.Should().Be("omsWorkspace");
        workspace.AssignPermissions.Should().BeTrue();
        workspace.DisplayName.Should().Be("Log Analytics workspace");

        parameters[2].DefaultValue.Should().Be("30");
        parameters[2].Type.Should().Be("Integer");

        parameters[3]
            .DefaultValue.Should()
            .Be("[\"StorageRead\",\"StorageWrite\",\"StorageDelete\"]");
        parameters[3].DefaultValueJson.Should().Contain("\n");

        parameters[4].DefaultValue.Should().Be("true");

        parameters[5].DefaultValueJson.Should().Contain("\"profileName\": \"setByPolicy\"");

        parameters[6].IsDeprecated.Should().BeTrue();
        parameters[6].DefaultValue.Should().Be("\"\"", "an empty string default must stay visible");
    }

    [Fact]
    public void Build_ParameterizedEffect_ResolvesAllowedAndDefault()
    {
        var effect = Build("Definitions", "comprehensive-definition.json").Effect;

        effect.Should().NotBeNull();
        effect!.Kind.Should().Be(PolicyEffectKind.Parameter);
        effect.ParameterName.Should().Be("effect");
        effect.DefaultEffect.Should().Be("DeployIfNotExists");
        effect.AllowedEffects.Should().Equal("DeployIfNotExists", "AuditIfNotExists", "Disabled");
    }

    [Fact]
    public void Build_LiteralEffect_AndNameFallsBackToFileName()
    {
        var model = Build("Definitions", "hard-coded-effect.json");

        model.Name.Should().Be("hard-coded-effect");
        model.PolicyType.Should().BeNull();
        model.Description.Should().BeNull();
        model.Parameters.Should().BeEmpty();
        model.Effect!.Kind.Should().Be(PolicyEffectKind.Literal);
        model.Effect.Raw.Should().Be("Deny");
        model.Effect.AllowedEffects.Should().Equal("Deny");
        model.ResourceTypes.Should().Equal("Microsoft.Network/publicIPAddresses");
        model.EffectDetails.Should().BeNull();
    }

    [Fact]
    public void Build_ExpressionEffect_ExtractsEffectLiterals()
    {
        var model = Build("Definitions", "modify-expression-effect.jsonc");

        model.Effect!.Kind.Should().Be(PolicyEffectKind.Expression);
        model.Effect.ParameterName.Should().BeNull();
        model.Effect.DefaultEffect.Should().BeNull();
        model.Effect.AllowedEffects.Should().Equal("Modify", "Disabled");
        model.Parameters.Should().OnlyContain(parameter => !parameter.IsEffect);
        model.IsDeprecated.Should().BeFalse("the string \"false\" is not true");
    }

    [Fact]
    public void Build_EffectDetails_ResolvesRolesConditionsAndOperations()
    {
        var details = Build("Definitions", "comprehensive-definition.json").EffectDetails;

        details.Should().NotBeNull();
        details!.ResourceType.Should().Be("Microsoft.Insights/diagnosticSettings");
        details.HasExistenceCondition.Should().BeTrue();
        details.HasDeployment.Should().BeTrue();
        details.ModifyOperations.Should().BeEmpty();
        details.RoleDefinitions.Should().HaveCount(2);
        details.RoleDefinitions[0].Name.Should().Be("Contributor");
        details.RoleDefinitions[0].Guid.Should().Be("b24988ac-6180-42a0-ab88-20f7382dd24c");
        details.RoleDefinitions[1].Name.Should().BeNull();
        details.RoleDefinitions[1].Guid.Should().Be("00000000-1111-2222-3333-444444444444");

        var modify = Build("Definitions", "modify-expression-effect.jsonc").EffectDetails!;
        modify.ResourceType.Should().BeNull();
        modify.HasDeployment.Should().BeFalse();
        modify.RoleDefinitions.Single().Name.Should().Be("Tag Contributor");
        modify
            .ModifyOperations.Should()
            .ContainSingle()
            .Which.Should()
            .Be(new ModifyOperation("addOrReplace", "tags['costCenter']"));
    }

    [Fact]
    public void Build_ResourceTypes_CollectsPositiveTypeConditionsOnly()
    {
        var model = Build("Definitions", "comprehensive-definition.json");

        model
            .ResourceTypes.Should()
            .Equal(
                "Microsoft.Storage/*",
                "Microsoft.Storage/storageAccounts",
                "Microsoft.Storage/storageAccounts/blobServices"
            );
    }

    [Fact]
    public void Build_FieldAliases_IncludesExistenceConditionAndSkipsTags()
    {
        var model = Build("Definitions", "comprehensive-definition.json");

        model
            .FieldAliases.Should()
            .Equal(
                "Microsoft.Insights/diagnosticSettings/logs.enabled",
                "Microsoft.Insights/diagnosticSettings/workspaceId",
                "Microsoft.Storage/storageAccounts/supportsHttpsTrafficOnly"
            );
    }

    [Fact]
    public void Build_PolicyRuleJson_IsIndentedAndUnescaped()
    {
        var model = Build("Definitions", "comprehensive-definition.json");

        model
            .PolicyRuleJson.Should()
            .Contain("[parameters('effect')]", "quotes must not be escaped as \\u0027");
        model.PolicyRuleJson.Should().StartWith("{\n  \"if\": {");
        model.PolicyDefinitions.Should().BeEmpty();
        model.Groups.Should().BeEmpty();
    }

    [Fact]
    public void Build_PolicySet_ResolvesMembersGroupsAndParameters()
    {
        var model = Build("PolicySets", "grouped-policy-set.json");

        model.Kind.Should().Be(PolicyDocumentKind.PolicySetDefinition);
        model.Mode.Should().BeNull();
        model.Effect.Should().BeNull();
        model.PolicyRuleJson.Should().BeNull();
        model.PolicyDefinitions.Should().HaveCount(4);

        var storage = model.PolicyDefinitions[0];
        storage.ReferenceId.Should().Be("storage-https");
        storage.Name.Should().Be("404c3081-a854-4457-ae30-26a93ef643f9");
        storage.IsBuiltIn.Should().BeTrue();
        storage.DefinitionVersion.Should().Be("1.*.*");
        storage.GroupNames.Should().Equal("Storage");
        storage
            .Parameters.Should()
            .ContainSingle()
            .Which.Should()
            .Be(new PolicySetMemberParameter("effect", "[parameters('storageEffect')]"));

        var custom = model.PolicyDefinitions[2];
        custom.PolicyDefinitionName.Should().Be("rg-require-environment-tag");
        custom.Name.Should().Be("rg-require-environment-tag");
        custom.IsBuiltIn.Should().BeFalse();
        custom
            .Parameters.Select(parameter => parameter.Value)
            .Should()
            .Equal("environment", "Audit");

        var noReference = model.PolicyDefinitions[3];
        noReference.ReferenceId.Should().Be("custom-no-reference-id");
        noReference.IsBuiltIn.Should().BeFalse();

        model
            .Groups.Select(group => (group.Name, group.MemberCount))
            .Should()
            .Equal(("Storage", 2), ("Location", 1), ("Unused", 0));
        model.Groups[0].Category.Should().Be("Data protection");
        model.Parameters[1].StrongType.Should().Be("location");
    }

    [Fact]
    public void Build_CustomValues_AreCarriedOnTheModel()
    {
        var document = PolicyDocumentParser.ParseFile(
            Fixtures.Path("Definitions", "hard-coded-effect.json")
        );
        var model = PolicyDocumentationModelBuilder.Build(
            document,
            new Dictionary<string, string> { ["owner"] = "Team" }
        );

        model.Custom.Should().ContainKey("owner").WhoseValue.Should().Be("Team");
    }

    [Fact]
    public void Catalog_SortsByCategoryThenDisplayName()
    {
        var definition = Build("Definitions", "comprehensive-definition.json");
        var hardCoded = Build("Definitions", "hard-coded-effect.json");
        var set = Build("PolicySets", "grouped-policy-set.json");

        var catalog = PolicyCatalogModel.Create([
            (definition, "a/comprehensive.json", "a/comprehensive.md"),
            (hardCoded, "b/hard.json", null),
            (set, "c/set.json", "c/set.md"),
        ]);

        catalog
            .PolicyDefinitions.Select(entry => entry.Name)
            .Should()
            .Equal("hard-coded-effect", "deploy-storage-diagnostics");
        catalog
            .PolicyDefinitions[1]
            .AllowedEffects.Should()
            .Equal("DeployIfNotExists", "AuditIfNotExists", "Disabled");
        catalog.PolicySetDefinitions.Single().PolicyCount.Should().Be(4);
    }
}
