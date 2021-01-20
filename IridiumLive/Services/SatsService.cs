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

using IridiumLive.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Globalization;

namespace IridiumLive.Services
{
    public interface ISatsService
    {
        public Task<ICollection<Sat>> GetSatsAsync();
        
        public Task<Sat> GetSatAsync(string id);

        public Task<bool> PostSatAsync(Sat sat);

        public Task<bool> PutSatAsync(string id, Sat sat);

        public Task<bool> AddRxLineAsync(string rxLine);
    }

    public class SatsService : IridiumService, ISatsService
    {
        public SatsService(IConfiguration configuration) : base(configuration)
        {
            //Console.WriteLine("Only IRA gets charted.");
        }

        public async Task<ICollection<Sat>> GetSatsAsync()
        {
            using IridiumLiveDbContext _context = new IridiumLiveDbContext(Options); 
            return await _context.Sats.OrderBy(s => s.SatNo).AsNoTracking().ToListAsync();
        }

        public async Task<Sat> GetSatAsync(string id)
        {
            using IridiumLiveDbContext _context = new IridiumLiveDbContext(Options); 
            return await _context.Sats.FindAsync(id);
        }

        public async Task<bool> PostSatAsync(Sat sat)
        {
            using IridiumLiveDbContext _context = new IridiumLiveDbContext(Options); 
            _context.Sats.Add(sat);
            
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (SatExists(sat.Id))
                {
                    return false;
                }
                else
                {
                    throw;
                }
            }

            return true;
        }

        public async Task<bool> PutSatAsync(string id, Sat sat)
        {
            if (id != sat.Id)
            {
                return false;
            }

            using IridiumLiveDbContext _context = new IridiumLiveDbContext(Options);
            _context.Entry(sat).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SatExists(id))
                {
                    return false;
                }
                else
                {
                    throw;
                }
            }

            return true;
        }

        public async Task<bool> AddRxLineAsync(string rxLine)
        {
            using IridiumLiveDbContext _context = new IridiumLiveDbContext(Options); 
            try
            {
                string newGuid = Guid.NewGuid().ToString();
                string[] words = rxLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                long currentstamp = DateTimeOffset.Now.ToUnixTimeSeconds();
                DateTimeOffset satTime = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(currentstamp, CultureInfo.InvariantCulture)).ToLocalTime();
                long utcTicks = satTime.ToUniversalTime().UtcTicks;
                int quality = Convert.ToInt32(words[4].TrimEnd('%'), CultureInfo.InvariantCulture);
                int satNo;
                //Debug.WriteLine("{0} {1}", words[0], satTime);

                //store everything
                Packet packets = new Packet
                {
                    Id = newGuid,
                    Time = satTime,
                    UtcTicks = utcTicks,
                    Quality = quality,
                    PacketId = words[0].Substring(0, 3)
                };

                _context.Packets.Add(packets);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    return false;
                }

                //store for immediate view
                if (words[0] == "IRA:")
                {
                    Console.WriteLine("{0} {1} {2}", words[0], satTime, utcTicks);

                    Ira ira = new Ira
                    {
                        Id = newGuid,
                        Time = satTime,
                        UtcTicks = utcTicks,
                        Quality = quality
                    };

                    //sat:26 -> [8]
                    satNo = Convert.ToInt32(words[8].Substring(4), CultureInfo.InvariantCulture);
                    ira.SatNo = satNo;

                    //beam:44 -> [9]
                    ira.Beam = Convert.ToInt32(words[9].Substring(5), CultureInfo.InvariantCulture);

                    //pos=(+51.18/-068.82) -> [10]
                    string[] words2 = words[10].Split('(', '/', ')');
                    ira.Lat = Convert.ToDouble(words2[1], CultureInfo.InvariantCulture);
                    ira.Lon = Convert.ToDouble(words2[2], CultureInfo.InvariantCulture);

                    //alt=796 -> [11]
                    ira.Alt = Convert.ToDouble(words[11].Substring(4), CultureInfo.InvariantCulture);

                    _context.Iras.Add(ira);
                    try
                    {
                        await _context.SaveChangesAsync();
                    }
                    catch (DbUpdateException)
                    {
                        return false;
                    }
                }
                else if (words[0] == "IBC:")
                {
                    //Console.WriteLine("{0} {1} {2}", words[0], satTime, utcTicks); 
                    Ibc ibc = new Ibc
                    {
                        Id = newGuid,
                        Time = satTime,
                        UtcTicks = utcTicks,
                        Quality = quality
                    };

                    //sat:26 -> [9]
                    satNo = Convert.ToInt32(words[9].Substring(4), CultureInfo.InvariantCulture);
                    ibc.SatNo = satNo;

                    //cell:31 -> [10]
                    ibc.Cell = Convert.ToInt32(words[10].Substring(5), CultureInfo.InvariantCulture);

                    _context.Ibcs.Add(ibc);
                    try
                    {
                        await _context.SaveChangesAsync();
                    }
                    catch (DbUpdateException)
                    {
                        return false;
                    }
                }
                else
                {
                    //no IRA/IBC so no sat number present
                    return false;
                }

                //store IRA/IBC into the sat table, they have sat numbers
                //store to avoid query, which could be a compounded view
                //as it will save on write operations to db, however the view might be more expensive
                //need to research
                if (!SatExists(satNo))
                {
                    Sat sat = new Sat
                    {
                        //it does not matter we use an existing Guid as long as it is unique
                        Id = newGuid,
                        SatNo = satNo,
                        Name = satNo.ToString()
                    };

                    _context.Sats.Add(sat);
                    try
                    {
                        await _context.SaveChangesAsync();
                    }
                    catch (DbUpdateException ex)
                    {
                        Debug.WriteLine(ex.Message);
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return false;
            }
        }

        private bool SatExists(string id)
        {
            using IridiumLiveDbContext _context = new IridiumLiveDbContext(Options); 
            return _context.Sats.Any(e => e.Id == id);
        }

        private bool SatExists(int rxId)
        {
            using IridiumLiveDbContext _context = new IridiumLiveDbContext(Options); 
            return _context.Sats.Any(e => e.SatNo == rxId);
        }
    }
}
