using CsvHelper;
using log4net;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBotWS
{
    partial class AngelsBotService : ServiceBase
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        Thread Worker;
        public static bool welcomeMessage = false;
        AutoResetEvent StopRequest = new AutoResetEvent(false);
        public static readonly TelegramBotClient Bot = new TelegramBotClient("1417186445:AAGFG-jByzgAEhaZRAKLnnJOigAXbzM8dhU");
        public AngelsBotService()
        {
            log4net.Config.XmlConfigurator.Configure();
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            // Start the worker thread
            Worker = new Thread(DoWork);
            Worker.Start();
        }

        protected override void OnStop()
        {
            // Signal worker to stop and wait until it does
            StopRequest.Set();
            Worker.Join();
        }

        public void DoWork(object arg)
        {
            // Worker thread loop
            while (true)
            {
                log.Info("O robô começou ...");

                //Run this code once every 5 min or stop right away if the service
                // is stopped
                if (StopRequest.WaitOne(300000)) return;
                //if (StopRequest.WaitOne(1)) return;

                // Do work...
                MainWork();
                log.Info("O robô finalizou ...");
            }
        }
        public static void MainWork()
        {
            try
            {
                if (!welcomeMessage)
                {
                    BotMessage("🇧🇷 ANGEL SIGNALS 🇧🇷\n" +
                                        "   🇨🇮 TRADER X 🇨🇮\n" +
                                        "================================\n" +
                                        "Estão preparados? Vamos começar com os sinais.\n" +
                                        "💰 TRADER X está analisando o mercado 🤑\n" +
                                        "SINAIS AO VIVO EM BREVE...\n" +
                                        "AGUARDEM!!!"
                                                            , new List<int> { 1079068893 });
                    welcomeMessage = true;
                }

                var me = Bot.GetMeAsync().Result;
                log.InfoFormat("Conectado com o bot: {0}", me.Username);
                Dictionary<string, string> m5FilesValidation = new Dictionary<string, string>();
                Dictionary<string, string> m15FilesValidation = new Dictionary<string, string>();
                var ListM5 = new List<MxFile>();
                var ListM15 = new List<MxFile>();

                Bot.OnMessage += BotOnMessageReceived;
                Bot.OnMessageEdited += BotOnMessageReceived;
                Bot.OnCallbackQuery += BotOnCallbackQueryReceived;
                Bot.OnReceiveError += BotOnReceiveError;
                Bot.StartReceiving(Array.Empty<UpdateType>());

                var md5files = GetAllFiles(@"E:\Marcelo\Binárias\AngelSignals\M5");
                var md15files = GetAllFiles(@"E:\Marcelo\Binárias\AngelSignals\M15");

                foreach (var item in md5files)
                {
                    var result = ReadCSVFile(item);
                    var date = result[0].Date.ToString();
                    if (date != null)
                    {
                        m5FilesValidation.Add(date, item);
                    }
                    ListM5.Add(new MxFile
                    {
                        ListMx = result,
                        FileName = Path.GetFileName(item)
                    });
                }

                foreach (var item in md15files)
                {
                    var result = ReadCSVFile(item);
                    var date = result[0].Date.ToString();
                    if (date != null)
                    {
                        m15FilesValidation.Add(date, item);
                    }
                    ListM15.Add(new MxFile
                    {
                        ListMx = result,
                        FileName = Path.GetFileName(item)
                    });
                }

                var validateM5File = GetFileOfTheDay(m5FilesValidation);
                var validateM15File = GetFileOfTheDay(m15FilesValidation);

                if (validateM5File != null)
                {
                    var listM5 = ListM5.FirstOrDefault(x => x.FileName.Equals(Path.GetFileName(validateM5File))).ListMx;
                    var listM15 = ListM15.FirstOrDefault(x => x.FileName.Equals(Path.GetFileName(validateM15File))).ListMx;
                    var listLiveSignalsM5 = CheckAvailableLiveSignals(listM5);
                    var listLiveSignalsM15 = CheckAvailableLiveSignals(listM15);

                    if (listLiveSignalsM5.Count > 0 || listLiveSignalsM15.Count > 0)
                    {
                        //Lista M5
                        foreach (var item in listLiveSignalsM5)
                        {
                            var squareColor = item.Signal.Equals("CALL") ? "🟩 "+ item.Signal + "\n\n" 
                                : "🟥 " + item.Signal + "\n\n";

                            BotMessage(string.Format("--- {0} ---\n" +
                                                "🇧🇷 ANGEL SIGNALS 🇧🇷\n" +
                                                "   🇨🇮 TRADER X 🇨🇮\n" +
                                                "================\n" +
                                                "💰 {1}\n" +
                                                "⏰ {2}\n" +
                                                "⏳ {3}\n" +
                                                "{4}" +
                                                "Sinal até gale 1."
                                                , item.Date, item.Currency
                                                , item.Time, item.CurrencyTime.Replace(",", ""), squareColor)
                                                            , new List<int> { 1079068893, 1001493482 });                            
                        }

                        //Lista M15
                        foreach (var item in listLiveSignalsM15)
                        {
                            var squareColor = item.Signal.Equals("CALL") ? "🟩 " + item.Signal + "\n\n"
                                : "🟥 " + item.Signal + "\n\n";

                            BotMessage(string.Format("--- {0} ---\n" +
                                                "🇧🇷 ANGEL SIGNALS 🇧🇷\n" +
                                                "   🇨🇮 TRADER X 🇨🇮\n" +
                                                "================\n" +
                                                "💰 {1}\n" +
                                                "⏰ {2}\n" +
                                                "⏳ {3}\n" +
                                                "{4}" +
                                                "Sinal até gale 1.", item.Date, item.Currency
                                                , item.Time, item.CurrencyTime.Replace(",",""), squareColor)
                                                            , new List<int> { 1079068893, 1001493482 });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.ErrorFormat("Erro: {0}", ex);
                Bot.SendTextMessageAsync(1079068893,"Marcelo estou com problemas. Venha no meu log! :)");
            }
        }

        private static async void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs)
        {
            var message = messageEventArgs.Message;

            if (message == null || message.Type != MessageType.Text) return;

            switch (message.Text)
            {
                case "":
                    {
                        break;
                    }
                default:
                    {
                        await Bot.SendChatActionAsync(message.Chat.Id, ChatAction.Typing);
                        //await Bot.SendChatActionAsync("@MarceliNMARLEY", ChatAction.Typing);


                        var inlineKeyboard = new InlineKeyboardMarkup(new[]
                        {
                            new []
                            {
                                InlineKeyboardButton.WithCallbackData("Teste"),
                            }
                        });

                        //await Bot.SendTextMessageAsync(
                        //    //message.Chat.Id,
                        //    //1079068893,
                        //    1001493482,
                        //    "Recebeu ai Renatão? confirma pra mim la no whatsapp!!!!");
                        await Bot.SendTextMessageAsync(
                            message.Chat.Id,
                            //1079068893,
                            "Marcelo testando enviar messagens através do bot",
                            replyMarkup: inlineKeyboard);
                        break;
                    }
            }
        }

        private static async void BotOnCallbackQueryReceived(object sender, CallbackQueryEventArgs callbackQueryEventArgs)
        {
            var callbackQuery = callbackQueryEventArgs.CallbackQuery;

            await Bot.SendTextMessageAsync(
                callbackQuery.Message.Chat.Id,
                $"Received {callbackQuery.Data}");
        }

        private static void BotOnReceiveError(object sender, ReceiveErrorEventArgs receiveErrorEventArgs)
        {
            Console.WriteLine("Received error: {0} — {1}",
                receiveErrorEventArgs.ApiRequestException.ErrorCode,
                receiveErrorEventArgs.ApiRequestException.Message);
        }

        private static List<string> GetAllFiles(string sDirt)
        {
            List<string> files = new List<string>();

            try
            {
                foreach (string file in Directory.GetFiles(sDirt))
                {
                    //files.Add(Path.GetFileName(file));
                    files.Add(file);
                }
                foreach (string fl in Directory.GetDirectories(sDirt))
                {
                    files.AddRange(GetAllFiles(fl));
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);
            }

            return files;
        }

        private static string GetFileOfTheDay(Dictionary<string, string> file)
        {
            var result = string.Empty;
            foreach (KeyValuePair<string, string> kvp in file)
            {
                if (kvp.Key.Equals(DateTime.Now.Date.ToString("dd/MM/yyyy")))
                {
                    result = kvp.Value;
                }
            }

            return result;
        }
        private static List<ModelFile> CheckAvailableLiveSignals(List<ModelFile> list)
        {
            var result = new List<ModelFile>();

            foreach (var item in list)
            {
                if (item.CurrencyTime.Replace(",","").Equals("5M"))
                {
                    var time = DateTime.Now.AddHours(-3).AddMinutes(10);
                    //var time = new DateTime(2020, 12, 03, 7, 09, 20).AddMinutes(10);
                    var timeRoundedUp = RoundUp(time,TimeSpan.FromMinutes(5)).ToShortTimeString();
                    //if (DateTime.ParseExact(timeRoundedUp, "HH:mm:ss:fff", CultureInfo.InvariantCulture) <=
                    //    DateTime.ParseExact(item.Time, "HH:mm:ss:fff", CultureInfo.InvariantCulture).AddMinutes(-10))
                    if (item.Time.Equals(timeRoundedUp))
                    {
                        result.Add(item);
                    }
                }
                else if (item.CurrencyTime.Replace(",", "").Equals("15M"))
                {
                    var time = DateTime.Now.AddHours(-3).AddMinutes(20);
                    //var time = new DateTime(2020, 12, 03, 13, 09, 20).AddMinutes(20);
                    var timeRoundedUp = RoundUp(time, TimeSpan.FromMinutes(5)).ToShortTimeString();
                    if (item.Time.Equals(timeRoundedUp))
                    {
                        result.Add(item);
                    }
                }
            }

            return result;
        }

        private static List<ModelFile> ReadCSVFile(string file)
        {
            var listModelFile = new List<ModelFile>();
            using (TextReader reader = File.OpenText(file))
            {
                CsvReader csv = new CsvReader(reader, System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                csv.Configuration.Delimiter = ";";
                csv.Configuration.MissingFieldFound = null;
                csv.Configuration.HasHeaderRecord = false;
                while (csv.Read())
                {
                    ModelFile Record = csv.GetRecord<ModelFile>();
                    listModelFile.Add(Record);
                }
            }

            return listModelFile;
        }

        private static void BotMessage(string message, List<int> listNumberOfReceivers)
        {
            foreach (var item in listNumberOfReceivers)
            {
                Bot.SendTextMessageAsync(item, message); 
            }
        }

        public static DateTime RoundUp(DateTime dt, TimeSpan d)
        {
            var modTicks = dt.Ticks % d.Ticks;
            var delta = modTicks != 0 ? d.Ticks - modTicks : 0;
            return new DateTime(dt.Ticks + delta, dt.Kind);
        }

        public static DateTime RoundDown(DateTime dt, TimeSpan d)
        {
            var delta = dt.Ticks % d.Ticks;
            return new DateTime(dt.Ticks - delta, dt.Kind);
        }

        public static DateTime RoundToNearest(DateTime dt, TimeSpan d)
        {
            var delta = dt.Ticks % d.Ticks;
            bool roundUp = delta > d.Ticks / 2;
            var offset = roundUp ? d.Ticks : 0;

            return new DateTime(dt.Ticks + offset - delta, dt.Kind);
        }
    }
}
