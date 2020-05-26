using Google.Apis.Auth.OAuth2;
using Google.Apis.Download;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using File = Google.Apis.Drive.v3.Data.File;

namespace TelegramBot
{
    //1191413291:AAFzfzNQ_O-_cryGKLui3wD1VZwuamuAmZ0
    class Program
    {
        private static string UploadingFileType = "";
        private static string UploadingFilePath = "";
        private static string FileName = "";
        private static string FolderName = "";
        private static string FolderId = "";
        private static string FileId = "";
        private static string DownloadFilePath = "";
        private static string[] Scopes = { DriveService.Scope.Drive };
        private static string ApplicationName = "Telegram Bot Google Grive";
        static TelegramBotClient Bot;
        static void Main(string[] args)
        {

            Bot = new TelegramBotClient("1191413291:AAFzfzNQ_O-_cryGKLui3wD1VZwuamuAmZ0");
            Bot.OnMessage += Bot_OnMessage;
            Bot.OnCallbackQuery += Bot_OnCallbackQuery;
            var me = Bot.GetMeAsync().Result;
            Console.WriteLine(me.FirstName);
            Bot.StartReceiving();
            Console.ReadKey();
            Bot.StopReceiving();
        }
        private static UserCredential GetUserCredential()
        {
            using (var stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
            {
                string creadPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                creadPath = Path.Combine(creadPath, "driveApiCredentials", "drive-credentials.json");
                return GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "User",
                    CancellationToken.None,
                    new FileDataStore(creadPath, true)).Result;
            }
        }
        private static DriveService GetDriveService(UserCredential credential)
        {
            return new DriveService(
                new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = ApplicationName
                }
                );
        }
        private static string GetFiles(DriveService service)
        {
            string AllFiles = "";
            IList<File> files = service.Files.List().Execute().Files;
            Console.WriteLine(files.Count);
            for(int i = 0; i < 50; i++)
            {
                AllFiles += "File title: " + files[i].Name + "\n" + "id: " + files[i].Id + "\n" + "\n";
            }
            return AllFiles;
        }
        private static void DownloadFileFromDrive(DriveService service, string fileId, string filePath)
        {
            var request = service.Files.Get(fileId);
            using (var memoryStream = new MemoryStream())
            {
                request.MediaDownloader.ProgressChanged += (IDownloadProgress progress) =>
                {
                    switch (progress.Status)
                    {
                        case DownloadStatus.Downloading:
                            Console.WriteLine(progress.BytesDownloaded);
                            break;
                        case DownloadStatus.Completed:
                            Console.WriteLine("Download comlete");
                            break;
                        case DownloadStatus.Failed:
                            Console.WriteLine("Download failed");
                            break;
                    }
                };
                request.Download(memoryStream);
                using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    fileStream.Write(memoryStream.GetBuffer(), 0, memoryStream.GetBuffer().Length);
                }
            }
        }
        private static string CreateFolder(DriveService service, string folderName)
        {
            var file = new File();
            file.Name = folderName;
            file.MimeType = "application/vnd.google-apps.folder";

            var request = service.Files.Create(file);
            request.Fields = "id";

            var result = request.Execute();

            return result.Id;
        }
        private static void UploadFiletoDrive(DriveService service, string fileName, string filePath, string contentType)
        {
            var fileMetadata = new File();
            fileMetadata.Name = fileName;
            fileMetadata.Parents = new List<string> { FolderId };

            FilesResource.CreateMediaUpload request;
            using (var stream = new FileStream(filePath, FileMode.Open))
            {
                request = service.Files.Create(fileMetadata, stream, contentType);
                request.Upload();
            }
            var file = request.ResponseBody;
        }
        private static async void Bot_OnCallbackQuery(object sender, Telegram.Bot.Args.CallbackQueryEventArgs e)
        {
            string buttonText = e.CallbackQuery.Data;
            string name = $"{e.CallbackQuery.From.FirstName} {e.CallbackQuery.From.LastName}";
            Console.WriteLine($"{name} нажал кнопку '{buttonText}'");
            await Bot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, $"Вы нажали кнопу '{buttonText}'");
        }

        private static async void Bot_OnMessage(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            UserCredential credential = GetUserCredential();
            DriveService service = GetDriveService(credential);
            
            var message = e.Message;
            if (message == null ||message.Type != MessageType.Text)
                return;
            string name = $"{message.From.FirstName}{message.From.LastName}";


            Console.WriteLine($"{name} sent message: '{ message.Text}'");
            switch (message.Text)
            {
                case "/start":
                    {
                        string text =
                    @" Привет! Я твой бот по Google Grive
                    Список команд:
                    /start - запуск бота
                    /getfiles - получение файлов
                    /createfolder - создание папки
                    /downloadfile - скачивание файла
                    /uploadingfile - загрузка файла";
                        await Bot.SendTextMessageAsync(message.From.Id, text);
                        break;
                    }
                case "/getfiles":
                    {
                        string text = GetFiles(service);
                        await Bot.SendTextMessageAsync(message.Chat.Id, text);
                        break;
                    }
                case "/createfolder":
                    {
                        string text = "Чтобы создать папку на Вашем Google Drive, нужно указать название папки. Для этого введите команду /enterNameFolder";
                        await Bot.SendTextMessageAsync(message.Chat.Id, text);
                        break;
                    }
                case "/downloadfile":
                    {
                        string text = "Чтобы скачать файл с Вашего Google Drive Вам нужно указать его Id и путь куда бы вы хотели его установить" + "\n" + "Для начала укажите Id файла, для этого введите команду /enterId";

                        await Bot.SendTextMessageAsync(message.Chat.Id, text);
                        break;
                    }
                case "/uploadingfile":
                    {
                        string text =
                    "Чтобы загрузить файл на Ваш Google Drive Вам нужно указать Id папки, в которую будет помещен файл; указать название файла, который хотите загрузить, его Путь и тип." + "\n" + "Для начала укажите Id папки, для этого введите команду /enterFolderId. Если нужной папки нет, то создайте ее, для этого введите команду /createfolder";

                        await Bot.SendTextMessageAsync(message.Chat.Id, text);
                        break;
                    }
                case "/enterId":
                    {
                        string text = "Введите Id файла";

                        await Bot.SendTextMessageAsync(message.Chat.Id, text, replyMarkup: new ForceReplyMarkup { Selective = true });
                        break;
                    }
                case "/enterNameFolder":
                    {
                        string text = "Введите название папки";

                        await Bot.SendTextMessageAsync(message.Chat.Id, text, replyMarkup: new ForceReplyMarkup { Selective = true });
                        break;
                    }
                case "/enterFolderId":
                    {
                        string text = "Введите Id папки";

                        await Bot.SendTextMessageAsync(message.Chat.Id, text, replyMarkup: new ForceReplyMarkup { Selective = true });
                        break;
                    }
                case "/enterFileName":
                    {
                        string text = "Введите название файла, который хотите загрузить";

                        await Bot.SendTextMessageAsync(message.Chat.Id, text, replyMarkup: new ForceReplyMarkup { Selective = true });
                        break;
                    }
                case "/enterFilePath":
                    {
                        string text = "Введите Путь файла, который хотите загрузить";

                        await Bot.SendTextMessageAsync(message.Chat.Id, text, replyMarkup: new ForceReplyMarkup { Selective = true });
                        break;
                    }
                case "/enterFileType":
                    {
                        string text = "Введите тип файла, который хотите загрузить";

                        await Bot.SendTextMessageAsync(message.Chat.Id, text, replyMarkup: new ForceReplyMarkup { Selective = true });
                        break;
                    }
                case "/enterPath":
                    {
                        string text = "Введите ПУТЬ файла";
                        await Bot.SendTextMessageAsync(message.Chat.Id, text, replyMarkup: new ForceReplyMarkup { Selective = true });
                        break;
                    }
                case "/download":
                    {
                        try
                        {
                            DownloadFileFromDrive(service,FileId, DownloadFilePath);
                            string text = @"Скачивание завершено
                        Список команд:
                        /start - запуск бота
                        /getfiles - получение файлов
                        /createfolder - создание папки
                        /downloadfile - скачивание файла
                        /uploadingfile - загрузка файла";
                            await Bot.SendTextMessageAsync(message.Chat.Id, text); 
                        }
                        catch
                        {
                            await Bot.SendTextMessageAsync(message.Chat.Id, "Ууууупппппсссс, произошла ошибка. Скорее всего вы неверно указали Id или ПУТЬ файла. Повторите ввод данных. Для этого введите команду /enterId");
                        }
                        break;
                    }
                case "/uploading":
                    {
                        try
                        {
                            UploadFiletoDrive(service, FileName, UploadingFilePath, UploadingFileType);
                            string text = @"Загрузка завершена
                        Список команд:
                        /start - запуск бота
                        /getfiles - получение файлов
                        /createfolder - создание папки
                        /downloadfile - скачивание файла
                        /uploadingfile - загрузка файла";
                            await Bot.SendTextMessageAsync(message.Chat.Id, text);
                        }
                        catch
                        {
                            await Bot.SendTextMessageAsync(message.Chat.Id, "Ууууупппппсссс, произошла ошибка. Скорее всего вы неверно указали нужные данные для загрузки файла. Повторите ввод данных. Для этого введите команду /enterId");
                        }
                        break;
                    }
                default:
                        break;
            }
            if (message.ReplyToMessage != null && message.ReplyToMessage.Text.Contains("Введите название папки"))
            {
                try
                {
                    FolderName = message.Text.ToString();
                    FolderId = CreateFolder(service, FolderName);
                    await Bot.SendTextMessageAsync(message.Chat.Id, "Папка создана." + "\n" + "Если вы хотите загрузить файл в эту папку введите команду /enterFileName");
                }
                catch
                {
                    await Bot.SendTextMessageAsync(message.Chat.Id, "Упппссс, произошла ошибка");
                }
            }
            if (message.ReplyToMessage != null && message.ReplyToMessage.Text.Contains("Введите тип файла, который хотите загрузить"))
            {
                try
                {
                    UploadingFileType = "application/" + message.Text.ToString();
                    await Bot.SendTextMessageAsync(message.Chat.Id, "Отлично! Все нужные данные введены. Для того чтобы начать загрузку файла введите команду /uploading");
                }
                catch
                {

                }
            }
            if (message.ReplyToMessage != null && message.ReplyToMessage.Text.Contains("Введите Путь файла, который хотите загрузить"))
            {
                try
                {
                    UploadingFilePath = message.Text.ToString();
                    await Bot.SendTextMessageAsync(message.Chat.Id, "Отлично! Теперь Вам нужно указать тип файла. Для этого введите команду /enterFileType");
                }
                catch
                {

                }
            }
            if (message.ReplyToMessage != null && message.ReplyToMessage.Text.Contains("Введите название файла, который хотите загрузить"))
            {
                try
                {
                    FileName = message.Text.ToString();
                    await Bot.SendTextMessageAsync(message.Chat.Id, "Отлично! Теперь Вам нужно указать Путь файла. Для этого введите команду /enterFilePath");
                }
                catch
                {

                }
            }
            if (message.ReplyToMessage != null && message.ReplyToMessage.Text.Contains("Введите Id папки"))
            {
                try
                {
                    FolderId = message.Text.ToString();
                    await Bot.SendTextMessageAsync(message.Chat.Id, "Отлично! Теперь Вам нужно указать название файла. Для этого введите команду /enterFileName");
                }
                catch
                {

                }
            }
            if (message.ReplyToMessage != null && message.ReplyToMessage.Text.Contains("Введите Id файла"))
            {
                try //кєтч необяз.
                {
                    FileId = message.Text.ToString();
                    await Bot.SendTextMessageAsync(message.Chat.Id, "Отлично! Теперь Вам нужно указать ПУТЬ файла. Для этого введите команду /enterPath");
                }
                catch
                {

                }
            }
            if (message.ReplyToMessage != null && message.ReplyToMessage.Text.Contains("Введите ПУТЬ файла"))
            {
                try
                {
                    DownloadFilePath = message.Text.ToString();
                    await Bot.SendTextMessageAsync(message.Chat.Id, "Отлично! Для запуска скачивания файла введите команду /download");
                }
                catch
                {

                }
            }
        }
    }
}
