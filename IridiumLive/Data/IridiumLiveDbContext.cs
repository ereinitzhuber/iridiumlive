﻿/*
 * microp11 2019
 * 
 * This file is part of IridiumLive.
 * 
 * IridiumLive is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * IridiumLive is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with IridiumLive.  If not, see <http://www.gnu.org/licenses/>.
 *
 *
 */

using Microsoft.EntityFrameworkCore;

namespace IridiumLive.Data
{
    public class IridiumLiveDbContext : DbContext
    {
        public DbSet<Sat> Sats { get; set; }
        public DbSet<Ira> Iras { get; set; }
        public DbSet<ViewIra> ViewIras { get; set; }
        public DbSet<Ibc> Ibcs { get; set; }
        public DbSet<Stat> Stats { get; set; }
        public DbSet<Packet> Packets { get; set; }
        public DbSet<PacketCounter> PacketCounters { get; set; }

        public IridiumLiveDbContext(DbContextOptions<IridiumLiveDbContext> options)
           : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //ViewIra is a view although we treat it as a table
            modelBuilder.Entity<ViewIra>(v =>
            {
                v.ToView("V_ViewIras");
            });

            //add an extra index to Packet table
            modelBuilder.Entity<Packet>()
                .HasIndex(u => u.PacketId)
                .HasName("IX_PacketId");
        }
    }
}