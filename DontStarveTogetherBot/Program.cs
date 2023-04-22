﻿using DontStarveTogetherBot.Exceptions;
using DontStarveTogetherBot.Models;
using EleCho.GoCqHttpSdk;
using EleCho.GoCqHttpSdk.Message;
using YamlDotNet.Serialization;
using YukariToolBox.LightLog;

Log.LogConfiguration.EnableConsoleOutput().SetLogLevel(LogLevel.Info);
Log.Info("System", "正在加载配置文件...");
var configPath = Path.Combine(AppContext.BaseDirectory, "config.yaml");
if (!File.Exists(configPath))
{
    await File.WriteAllTextAsync(configPath, new Serializer().Serialize(new Config()));
    Log.Error("System", "配置文件不存在，已自动生成，请修改后重启程序。");
    Console.ReadKey();
    return;
}
var config = new Deserializer().Deserialize<Config>(await File.ReadAllTextAsync(configPath));
Log.Info("System", "配置文件加载完成。");

Log.Info("System", "正在连接到 go-cqhttp...");
var session = new CqWsSession(new CqWsSessionOptions()
{
    BaseUri = new Uri(config.CqWsAddress),
    UseApiEndPoint = true,
    UseEventEndPoint = true
});
await session.StartAsync();
Log.Info("System", "连接成功。");

session.UseGroupMessage(async (context, next) =>
{
    if (context.RawMessage == "在线")
    {
        var server = new Server(config.DstServerIp, config.DstServerPort);
        List<Player> players;
        try
        {
            players = (await server.GetServerInfoAsync()).GetPlayers();
        }
        catch (ServerNotFoundException e)
        {
            await session.SendGroupMessageAsync(context.GroupId, new CqMessage("无法连接至服务器。"));
            return;
        }
        if (!players.Any())
        {
            await session.SendGroupMessageAsync(context.GroupId, new CqMessage("当前没有玩家在线。"));
            return;
        }
        await session.SendGroupMessageAsync(context.GroupId, new CqMessage("当前在线玩家：\n" + string.Join(" ", players.Select(x => $"[{x.Name}({x.Prefab})]"))));
        return;
    }
});

await session.WaitForShutdownAsync();