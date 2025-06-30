
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Globalization;
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
        public static bool CheckLogTis(string id, DateTime now)
        {
            string logPath = "log/tis_log.json";
            if (!File.Exists(logPath))
            {
                Directory.CreateDirectory("log");
                string json = JsonSerializer.Serialize(new Dictionary<string, string>());
                File.WriteAllText(logPath, json);
            }

            DeleteLogTis(logPath, now);

            string text = File.ReadAllText(logPath);
            Dictionary<string, string> data = JsonSerializer.Deserialize<Dictionary<string, string>>(text);

            string dataDate = data.GetValueOrDefault(id);
            if (dataDate == null)
            {
                data.Add(id, now.ToString());
                File.WriteAllText(logPath, JsonSerializer.Serialize(data));
                return true;
            }
            else
            {
                return false;
            }

        }
        public static void DeleteLogTis(string logPath, DateTime now)
        {
            bool statusDelete = false;
            string text = File.ReadAllText(logPath);
            Dictionary<string, string> data = JsonSerializer.Deserialize<Dictionary<string, string>>(text);
            foreach (var item in data)
            {
                //DateTime date = DateTime.Parse(item.Value);


                DateTime date;

                bool success = DateTime.TryParseExact(
                    item.Value,
                    new[] { "dd/MM/yyyy HH:mm:ss", "MM/dd/yyyy HH:mm:ss" }, // รองรับหลายรูปแบบ
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out date);


                DateTime dateEx = now.AddMinutes(-1);
                if (date < dateEx)
                {
                    data.Remove(item.Key);
                    statusDelete = true;
                }
            }

            if (statusDelete)
                File.WriteAllText(logPath, JsonSerializer.Serialize(data));
        }
        public static bool CheckLogMk(string id, DateTime now)
        {
            string logPath = "log/mk_log.json";
            if (!File.Exists(logPath))
            {
                Directory.CreateDirectory("log");
                string json = JsonSerializer.Serialize(new Dictionary<string, string>());
                File.WriteAllText(logPath, json);
            }

            DeleteLogMk(logPath, now);

            string text = File.ReadAllText(logPath);
            Dictionary<string, string> data = JsonSerializer.Deserialize<Dictionary<string, string>>(text);

            string dataDate = data.GetValueOrDefault(id);
            if (dataDate == null)
            {
                data.Add(id, now.ToString());
                File.WriteAllText(logPath, JsonSerializer.Serialize(data));
                return true;
            }
            else
            {
                return false;
            }

        }
        public static void DeleteLogMk(string logPath, DateTime now)
        {
            bool statusDelete = false;
            string text = File.ReadAllText(logPath);
            Dictionary<string, string> data = JsonSerializer.Deserialize<Dictionary<string, string>>(text);
            foreach (var item in data)
            {
                //DateTime date = DateTime.Parse(item.Value);


                DateTime date;

                bool success = DateTime.TryParseExact(
                    item.Value,
                    new[] { "dd/MM/yyyy HH:mm:ss", "MM/dd/yyyy HH:mm:ss" }, // รองรับหลายรูปแบบ
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out date);


                DateTime dateEx = now.AddMinutes(-1);
                if (date < dateEx)
                {
                    data.Remove(item.Key);
                    statusDelete = true;
                }
            }

            if (statusDelete)
                File.WriteAllText(logPath, JsonSerializer.Serialize(data));
        }
        public static bool CheckLogSendEmail(string id, DateTime now)
        {
            string logPath = "log/send_email_log.json";
            if (!File.Exists(logPath))
            {
                Directory.CreateDirectory("log");
                string json = JsonSerializer.Serialize(new Dictionary<string, string>());
                File.WriteAllText(logPath, json);
            }

            DeleteLogSendEmail(logPath, now);

            string text = File.ReadAllText(logPath);
            Dictionary<string, string> data = JsonSerializer.Deserialize<Dictionary<string, string>>(text);

            string dataDate = data.GetValueOrDefault(id);
            if (dataDate == null)
            {
                data.Add(id, now.ToString());
                File.WriteAllText(logPath, JsonSerializer.Serialize(data));
                return true;
            }
            else
            {
                return false;
            }

        }
        public static void DeleteLogSendEmail(string logPath, DateTime now)
        {
            bool statusDelete = false;
            string text = File.ReadAllText(logPath);
            Dictionary<string, string> data = JsonSerializer.Deserialize<Dictionary<string, string>>(text);
            foreach (var item in data)
            {
                DateTime date = DateTime.Parse(item.Value);
                DateTime dateEx = now.AddSeconds(-10);
                if (date < dateEx)
                {
                    data.Remove(item.Key);
                    statusDelete = true;
                }
            }

            if (statusDelete)
                File.WriteAllText(logPath, JsonSerializer.Serialize(data));
        }
        public static bool CheckLogSendSms(string id, DateTime now)
        {
            string logPath = "log/send_sms_log.json";
            if (!File.Exists(logPath))
            {
                Directory.CreateDirectory("log");
                string json = JsonSerializer.Serialize(new Dictionary<string, string>());
                File.WriteAllText(logPath, json);
            }

            DeleteLogSendSms(logPath, now);

            string text = File.ReadAllText(logPath);
            Dictionary<string, string> data = JsonSerializer.Deserialize<Dictionary<string, string>>(text);

            string dataDate = data.GetValueOrDefault(id);
            if (dataDate == null)
            {
                data.Add(id, now.ToString());
                File.WriteAllText(logPath, JsonSerializer.Serialize(data));
                return true;
            }
            else
            {
                return false;
            }

        }
        public static void DeleteLogSendSms(string logPath, DateTime now)
        {
            bool statusDelete = false;
            string text = File.ReadAllText(logPath);
            Dictionary<string, string> data = JsonSerializer.Deserialize<Dictionary<string, string>>(text);
            foreach (var item in data)
            {
                DateTime date = DateTime.Parse(item.Value);
                DateTime dateEx = now.AddSeconds(-10);
                if (date < dateEx)
                {
                    data.Remove(item.Key);
                    statusDelete = true;
                }
            }

            if (statusDelete)
                File.WriteAllText(logPath, JsonSerializer.Serialize(data));
        }
    }
}
