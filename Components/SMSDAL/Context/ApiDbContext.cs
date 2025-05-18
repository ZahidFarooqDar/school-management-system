using Microsoft.EntityFrameworkCore;
using SMSDAL.Base;
using SMSDomainModels.AppUser;
using SMSDomainModels.Client;
using SMSDomainModels.Foundation;
using SMSDomainModels.v1.General.License;
using SMSDomainModels.v1.General.ScanCodes;

namespace SMSDAL.Context
{
    public class ApiDbContext : EfCoreContextRoot
    {
        #region Constructor
        public ApiDbContext(DbContextOptions<ApiDbContext> options)
            : base(options)
        {
            Database.EnsureCreated();
        }
        #endregion Constructor

        #region Log Tables
        public DbSet<ErrorLogRoot> ErrorLogRoots { get; set; }

        #endregion Log Tables

        #region App Users
        public DbSet<ApplicationUserDM> ApplicationUsers { get; set; }
        public DbSet<ClientUserDM> ClientUsers { get; set; }
        public DbSet<ClientCompanyDetailDM> ClientCompanyDetails { get; set; }
        public DbSet<ExternalUserDM> ExternalUsers { get; set; }

        #endregion App Users

        #region Barcode
        public DbSet<ScanCodesFormatDM> ScanCodes { get; set; }

        #endregion Barcode

        #region License

        public DbSet<LicenseTypeDM> LicenseTypes { get; set; }
        public DbSet<UserInvoiceDM> UserInvoices { get; set; }
        public DbSet<UserLicenseDetailsDM> UserLicenseDetails { get; set; }
        public DbSet<FeatureDM> Features { get; set; }
        public DbSet<FeatureDM_LicenseTypeDM> LicenseFeatures { get; set; }

        #endregion License

        #region On Model Creating
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Ensure unique constraint on Email and UserName
            /*modelBuilder.Entity<ClientUserDM>()
                .HasIndex(u => u.EmailId)
                .IsUnique();

            modelBuilder.Entity<ClientUserDM>()
                .HasIndex(u => u.LoginId)
                .IsUnique();*/

            

            // Seed database with initial data
            DatabaseSeeder<ApiDbContext> seeder = new DatabaseSeeder<ApiDbContext>();
            seeder.SetupDatabaseWithSeedData(modelBuilder);
        }

        #endregion On Model Creating

    }
}
