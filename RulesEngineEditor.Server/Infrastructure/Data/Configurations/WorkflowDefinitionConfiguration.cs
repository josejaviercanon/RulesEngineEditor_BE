using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RulesEngineEditor.Server.Business.Entities.Models;

namespace RulesEngineEditor.Server.Infrastructure.Data.Configurations;

public sealed class WorkflowDefinitionConfiguration : IEntityTypeConfiguration<WorkflowDefinitions>
{
    public void Configure(EntityTypeBuilder<WorkflowDefinitions> builder)
    {
        builder.ToTable("WorkflowDefinitions");

        builder.HasKey(e => e.WorkflowDefinitionId);

        builder.Property(e => e.WorkflowName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(e => e.Version)
            .IsRequired();

        builder.Property(e => e.JsonContent)
            .IsRequired()
            .HasColumnType("json");

        builder.Property(e => e.Status)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("Draft");

        builder.Property(e => e.CreatedAt)
            .HasColumnType("datetime2")
            .HasDefaultValueSql("(sysdatetimeoffset())");

        builder.Property(e => e.CreatedBy)
            .HasMaxLength(100);

        builder.HasIndex(e => new { e.WorkflowName, e.Version })
            .IsUnique()
            .HasDatabaseName("UQ_Workflow_Version");

        builder.HasIndex(e => new { e.WorkflowName, e.Status })
            .HasDatabaseName("IX_WorkflowDefinitions_Name_Status");
    }
}
