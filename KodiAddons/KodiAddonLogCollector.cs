using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure.Storage.Blobs;
using Telegram.Bot;
using YamlDotNet.Serialization;

namespace KodiAddonLogCollector
{
    public static class KodiAddonLogCollector
    {
        private static readonly string TELEGRAM_BOT_API_KEY = Environment.GetEnvironmentVariable("TELEGRAM_BOT_API_KEY");
        private static readonly string TELEGRAM_BOT_GROUP_ID = Environment.GetEnvironmentVariable("TELEGRAM_BOT_GROUP_ID");

        [FunctionName("KodiAddonLogCollector")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            [Blob("kodi-addon-logs", FileAccess.Write)] BlobContainerClient blobContainerClient,
            ILogger log)
        {

            log.LogInformation($"{new { req.HttpContext.Connection.RemoteIpAddress }}");
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var remoteIPAddress = req.HttpContext.Connection.RemoteIpAddress;

            if (TryDeserializeCrashLogV1(requestBody, out var crashLog))
            {
                crashLog.IPAddress = remoteIPAddress.ToString();
                var filename = $"{crashLog.Addon}/{crashLog.Version}/{DateTime.Now:yyyy-MM-dd_HH-mm-ss_ffff}.yaml";
                var serializer = new Serializer();
                var yaml = serializer.Serialize(crashLog);
                await blobContainerClient.UploadBlobAsync(filename, BinaryData.FromString(yaml));
                await SendNewLogNotification(crashLog, filename);
                return new OkResult();
            }
            else
            {
                await SendWrongParametersNotification(remoteIPAddress, requestBody);
                return new BadRequestResult();
            }
        }

        private static bool TryDeserializeCrashLogV1(string data, out CrashLogV1 result)
        {
            try
            {
                result = JsonConvert.DeserializeObject<CrashLogV1>(data);
                if (result == null)
                    return false;
                if (string.IsNullOrWhiteSpace(result.Addon) || string.IsNullOrWhiteSpace(result.Version))
                    return false;
                return true;
            }
            catch (JsonException)
            {
                result = null;
                return false;
            }
        }

        private static async Task SendNewLogNotification(CrashLogV1 crashLog, string filename)
        {
            var text = crashLog.GetNotificationText() + 
                $"\n*Filename:* _{filename}_";
            await SendNotification(text);
        }

        private static async Task SendWrongParametersNotification(IPAddress remoteIPAddress, string requestBody)
        {
            var text = 
                $"*Invalid function call from:* {remoteIPAddress}\n" +
                $"*Body:* '{requestBody}'";
            await SendNotification(text);
        }

        private static async Task SendNotification(string message)
        {
            var telegramBot = new TelegramBotClient(TELEGRAM_BOT_API_KEY);
            await telegramBot.SendTextMessageAsync(TELEGRAM_BOT_GROUP_ID, message, Telegram.Bot.Types.Enums.ParseMode.Markdown);
        }
    }
}
