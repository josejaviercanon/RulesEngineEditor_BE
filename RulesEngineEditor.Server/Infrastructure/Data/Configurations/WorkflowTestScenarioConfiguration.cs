using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RulesEngineEditor.Server.Business.Entities.Models;

namespace RulesEngineEditor.Server.Infrastructure.Data.Configurations;

public sealed class WorkflowTestScenarioConfiguration : IEntityTypeConfiguration<WorkflowTestScenarios>
{
    public void Configure(EntityTypeBuilder<WorkflowTestScenarios> builder)
    {
        builder.ToTable("WorkflowTestScenarios");

        builder.HasKey(e => e.ScenarioId);

        builder.Property(e => e.ScenarioName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(e => e.MockInputJson)
            .IsRequired()
            .HasColumnType("json");

        builder.Property(e => e.ExpectedOutputJson)
            .HasColumnType("json");

        builder.Property(e => e.CreatedAt)
            .HasColumnType("datetime2")
            .HasDefaultValueSql("(sysdatetimeoffset())");

        builder.HasOne<WorkflowDefinitions>()
            .WithMany()
            .HasForeignKey(e => e.WorkflowDefinitionId)
            .HasConstraintName("FK_WorkflowTestScenarios_WorkflowDefinitions")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.WorkflowDefinitionId)
            .HasDatabaseName("IX_WorkflowTestScenarios_WorkflowId");
    }
}
