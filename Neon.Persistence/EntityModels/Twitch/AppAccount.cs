using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Neon.Persistence.EntityModels.Twitch;

public class AppAccount : BaseModel
{
    public Guid? AppAccountId { get; set; }
    public string? AppName { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? AccessToken { get; set; }
    public string? RedirectUri { get; set; }
}

public class AppAccountConfiguration : IEntityTypeConfiguration<AppAccount>
{
    public void Configure(EntityTypeBuilder<AppAccount> builder)
    {
        //schema
        builder.ToTable("app_account", "twitch");
        
        //pk
        builder.HasKey(s => s.AppAccountId);

        //indexes
        builder.HasIndex(s => s.AppName);

        //relationships

        //base model
        builder.Property(s => s.CreatedDate).HasColumnOrder(2).IsRequired();
        builder.Property(s => s.CreatedBy).HasColumnOrder(3).IsRequired().HasMaxLength(50);
        builder.Property(s => s.ModifiedDate).HasColumnOrder(4);
        builder.Property(s => s.ModifiedBy).HasColumnOrder(5).HasMaxLength(50);

        //columns
        builder.Property(s => s.AppAccountId).HasColumnOrder(1).ValueGeneratedOnAdd();
        builder.Property(s => s.AppName).IsRequired().HasMaxLength(100);
        builder.Property(s => s.ClientId).IsRequired().HasMaxLength(255);
        builder.Property(s => s.ClientSecret).IsRequired().HasMaxLength(255);
        builder.Property(s => s.AccessToken).IsRequired().HasMaxLength(255);
        builder.Property(s => s.RedirectUri).IsRequired().HasMaxLength(2000);
    }
}
