
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace Etax_Api
{
    public static class Log
    {
        public static bool CheckLogTis(string etax_id, DateTime now)
        {
            if (!File.Exists("log/tis_log.json"))
            {
                Directory.CreateDirectory("log");
                string json = JsonSerializer.Serialize(new Dictionary<string, string>());
                File.WriteAllText("log/tis_log.json", json);
            }

            string text = File.ReadAllText("log/tis_log.json");
            Dictionary<string, string> data = JsonSerializer.Deserialize<Dictionary<string, string>>(text);

            string dataDate = data.GetValueOrDefault(etax_id);
            if (dataDate == null)
            {
                data.Add(etax_id, now.ToString());
                File.WriteAllText("log/tis_log.json", JsonSerializer.Serialize(data));
                Delete(now);
                return true;
            }
            else
            {
                return false;
            }

        }

        public static void Delete(DateTime now)
        {
            bool statusDelete = false;
            string text = File.ReadAllText("log/tis_log.json");
            Dictionary<string, string> data = JsonSerializer.Deserialize<Dictionary<string, string>>(text);
            foreach (var item in data)
            {
                DateTime date = DateTime.Parse(item.Value);
                DateTime dateEx = now.AddMinutes(-1);
                if (date < dateEx)
                {
                    data.Remove(item.Key);
                    statusDelete = true;
                }
            }

            if (statusDelete)
                File.WriteAllText("log/tis_log.json", JsonSerializer.Serialize(data));
        }
    }
}
