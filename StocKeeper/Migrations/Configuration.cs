namespace StocKeeper.Migrations
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;
    using StocKeeper.Models;

    internal sealed class Configuration : DbMigrationsConfiguration<StocKeeper.Models.StocKeeperContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false; // Important: Disable automatic migrations
            AutomaticMigrationDataLossAllowed = false; // Prevent accidental data loss
        }

        protected override void Seed(StocKeeperContext context)
        {
        }
    }
}