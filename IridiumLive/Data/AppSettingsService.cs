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

using Microsoft.Extensions.Configuration;

namespace IridiumLive.Data
{
    internal class AppSettingsService
    {
        private readonly IConfiguration _configuration;
        public AppSettingsService(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public string GetBaseUrl()
        {
            return _configuration.GetValue<string>("MySettings:BaseUrl");
        }
    }
}