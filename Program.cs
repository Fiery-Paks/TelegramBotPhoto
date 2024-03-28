using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using ConsoleApp2.Models;
using System;

internal class Program
{
    private static void Main(string[] args)
    {
        Start();
        Console.ReadLine();
    }
    private static async void Start()
    {
        var botClient = new TelegramBotClient("7019893449:AAGrmtY8rqgW2VtWbjRoHXAQW5bx-CBTTG8");

        using CancellationTokenSource cts = new();

        // StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
        ReceiverOptions receiverOptions = new()
        {
            AllowedUpdates = Array.Empty<UpdateType>() // receive all update types except ChatMember related updates
        };

        botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            pollingErrorHandler: HandlePollingErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: cts.Token
        );

        var me = await botClient.GetMeAsync();

        Console.WriteLine($"Start listening for @{me.Username}");
        Console.ReadLine();

        // Send cancellation request to stop bot
        cts.Cancel();
    }

    private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        await HandlePhotoAsync(botClient, update, cancellationToken);
        await HandleMessageAsync(botClient, update, cancellationToken);
    }
    private static async Task HandlePhotoAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update == null || update.Message == null || update.Message.Photo == null)
        {
            return;
        }
        await DowloadPhoto(botClient, update, cancellationToken);
    }
    private static async Task DowloadPhoto(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        var fileId = update.Message!.Photo!.Last().FileId;
        var fileInfo = await botClient.GetFileAsync(fileId);
        var filePath = fileInfo.FilePath;

        string url = @$"https://api.telegram.org/file/bot7019893449:AAGrmtY8rqgW2VtWbjRoHXAQW5bx-CBTTG8/{filePath}";
        string newnamefile = $@"{Thread.CurrentThread.ManagedThreadId}{Path.GetFileName(filePath!)}";
        string localpath = @$"Images\{newnamefile}";

        using (var client = new HttpClient())
        {
            using (var s = client.GetStreamAsync(url))
            {
                using (var fs = new FileStream(localpath, FileMode.OpenOrCreate))
                {
                    s.Result.CopyTo(fs);
                }
            }
        }

        TestContext testContext = new TestContext();
        InfoUser user = new InfoUser();
        user.Iduser = update.Message.Chat.Id;
        user.Name = update.Message.From!.FirstName;
        user.Image = System.IO.File.ReadAllBytes(localpath);
        testContext.InfoUsers.Add(user);
        await testContext.SaveChangesAsync();

        System.IO.File.Delete(localpath);
    }
    private static async Task HandleMessageAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        // Only process Message updates: https://core.telegram.org/bots/api#message
        if (update.Message is not { } message)
            return;
        // Only process text messages
        if (message.Text is not { } messageText)
            return;

        var chatId = message.Chat.Id;


        if (message.Text == "/start")
        {
            await UpLoadPhoto(botClient, update, cancellationToken);
        }
    }
    private static async Task UpLoadPhoto(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        var chatId = update.Message!.Chat.Id;

        TestContext testContext = new TestContext();
        InfoUser user = testContext.InfoUsers.FirstOrDefault(x => x.Iduser == chatId)!;
        if (user != null)
        {
            Stream stream = new MemoryStream(user.Image);

            await botClient.SendPhotoAsync(
            chatId: chatId,
            photo: InputFile.FromStream(stream),
            caption: @$"<b> {user.Id} {user.Name} </b>",
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken);
        }
    }
    private static Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var ErrorMessage = exception switch
        {
            ApiRequestException apiRequestException
                => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        Console.WriteLine(ErrorMessage);
        return Task.CompletedTask;
    }

}