using Microsoft.EntityFrameworkCore;
using MySensorApi.Models;
using System.Collections.Generic;

namespace MySensorApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<ComfortRecommendation> ComfortRecommendations { get; set; }

        public DbSet<SensorData> SensorData { get; set; }

        public DbSet<Room> Rooms { get; set; }
    }
}