using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer.Infrastructure.Internal;

namespace ProcessManager.Infrastructure.Models
{
    public partial class ProcesManagerDbContext : DbContext
    {
        private const string _getUtcDate = "GETUTCDATE()";
        public ProcesManagerDbContext(DbContextOptions<ProcesManagerDbContext> options) : base(options)
        {
            var sqlServerOptionsExtension = options.FindExtension<SqlServerOptionsExtension>();
            if (sqlServerOptionsExtension != null)
            {
                var connection = (SqlConnection)Database.GetDbConnection();
                connection.AccessToken =
                    (new Microsoft.Azure.Services.AppAuthentication.AzureServiceTokenProvider())
                    .GetAccessTokenAsync("https://database.windows.net/").Result;
            }
        }

        public virtual DbSet<WorkflowRunDbo> WorkflowRuns { get; set; }
        public virtual DbSet<WorkflowRelationDbo> WorkflowRelations { get; set; }
        public virtual DbSet<RelationDbo> Relations { get; set; }
        public virtual DbSet<ActivityDbo> Activities { get; set; }
        public virtual DbSet<OutboxMessageDbo> OutboxMessages { get; set; }
        public virtual DbSet<UnorchestratedRunDbo> UnorchestratedRuns { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<WorkflowRunDbo>(entity =>
            {
                entity.HasKey(e => e.OperationId);

                entity.Property(e => e.WorkflowRunName)
                    .IsRequired();

                entity.Property(e => e.Status)
                    .IsRequired();

                entity.Property(e => e.CreatedBy)
                    .IsRequired(false);

                entity.Property(e => e.ChangedBy)
                    .IsRequired(false);

                entity.Property(e => e.CreatedDate)
                    .HasDefaultValueSql(_getUtcDate);
                entity.Property(e => e.ChangedDate)
                    .HasDefaultValueSql(_getUtcDate)
                    .ValueGeneratedOnAddOrUpdate();

                entity.HasMany(e => e.WorkflowRelations);

                entity
                    .HasMany(c => c.Activities)
                    .WithOne(e => e.WorkflowRun);

                entity.Ignore(e => e.DomainEvents);
            });

            modelBuilder.Entity<WorkflowRelationDbo>(entity =>
            {
                entity.HasKey(e => new { e.OperationId, e.EntityId });

                entity.Property(e => e.EntityId)
                    .IsRequired();

                entity.Property(e => e.OperationId)
                    .IsRequired();

                entity.Property(e => e.CreatedBy)
                    .IsRequired(false);

                entity.Property(e => e.ChangedBy)
                    .IsRequired(false);

                entity.Property(e => e.CreatedDate)
                    .HasDefaultValueSql(_getUtcDate);
                entity.Property(e => e.ChangedDate)
                    .HasDefaultValueSql(_getUtcDate)
                    .ValueGeneratedOnAddOrUpdate();
            });

            modelBuilder.Entity<RelationDbo>(entity =>
            {
                entity.HasKey(e => e.EntityId);
                entity.Property(e => e.EntityType)
                    .IsRequired(false);
            });

            modelBuilder.Entity<ActivityDbo>(entity =>
            {
                entity.HasKey(e => e.ActivityId);

                entity.Property(e => e.Name)
                    .IsRequired();

                entity.Property(e => e.URI)
                    .IsRequired(false);

                entity.Property(e => e.Status)
                    .IsRequired();

                entity.Property(e => e.StartDate)
                    .IsRequired();

                entity.Property(e => e.OperationId)
                    .IsRequired();

                entity
                    .HasOne(e => e.WorkflowRun)
                    .WithMany(c => c.Activities)
                    .HasForeignKey(e => e.OperationId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                entity.Ignore(e => e.DomainEvents);
            });

            modelBuilder.Entity<OutboxMessageDbo>(entity =>
            {
                entity.HasKey(e => e.OutboxMessageId);

                entity.Property(e => e.CreatedDate)
                    .HasDefaultValueSql(_getUtcDate);

                entity.Property(e => e.Data)
                    .IsRequired(false);

                entity.Property(e => e.ProcessedDate)
                    .IsRequired(false);

                entity.Property(e => e.Type)
                    .IsRequired();

                entity.Ignore(e => e.DomainEvents);
            });

            modelBuilder.Entity<UnorchestratedRunDbo>(entity =>
            {
                entity.HasKey(e => e.UnorchestratedRunId);

                entity.Property(e => e.OperationId)
                    .IsRequired();

                entity.Property(e => e.EntityId)
                    .IsRequired();

                entity.Property(e => e.WorkflowRunName)
                    .IsRequired();

                entity.Property(e => e.WorkflowRunId)
                    .IsRequired();

                entity.Property(e => e.CreatedDate)
                    .HasDefaultValueSql(_getUtcDate);

                entity.Property(e => e.ChangedDate)
                    .HasDefaultValueSql(_getUtcDate)
                    .ValueGeneratedOnAddOrUpdate();

                entity.Ignore(e => e.DomainEvents);
            });
        }
    }
}
