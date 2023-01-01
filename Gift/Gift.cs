using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TShockAPI;
using Terraria;
using TerrariaApi.Server;
using Microsoft.Xna.Framework;

namespace Gift
{
    [ApiVersion(2, 1)]
    public class Gift : TerrariaPlugin
    {
        /// <summary>
        /// Gets the author(s) of this plugin
        /// </summary>
        public override string Author => "Leader";

        /// <summary>
        /// Gets the description of this plugin.
        /// A short, one lined description that tells people what your plugin does.
        /// </summary>
        public override string Description => "A sample test plugin";

        /// <summary>
        /// Gets the name of this plugin.
        /// </summary>
        public override string Name => "Test Plugin";

        /// <summary>
        /// Gets the version of this plugin.
        /// </summary>
        public override Version Version => new Version(1, 0, 0, 0);

        /// <summary>
        /// Initializes a new instance of the Gift class.
        /// This is where you set the plugin's order and perfrom other constructor logic
        /// </summary>
        public Gift(Main game) : base(game)
        {

        }
        /// <summary>
        /// Handles plugin initialization. 
        /// Fired when the server is started and the plugin is being loaded.
        /// You may register hooks, perform loading procedures etc here.
        /// </summary>
        public override void Initialize()
        {
            Commands.ChatCommands.Add(new Command("", gift, "抽奖","gift"));
            Config.GetConfig();
        }
        const string Permission = "gift.admin";
        Random random = new Random();
        private void gift(CommandArgs args)
        {
            var config = Config.GetConfig();
            if (args.Parameters.Count == 0)
            {
                args.Player.SendInfoMessage("/抽奖 随机抽取，随机抽取一个礼包");
                args.Player.SendInfoMessage("/抽奖 列表,查看礼包列表");
                if (args.Player.HasPermission(Permission))
                {
                    args.Player.SendInfoMessage("/抽奖 指定 礼包名，获取指定礼包");
                }
                return;
            }
            switch (args.Parameters[0])
            {
                case "指定":
                    {
                        var name = args.Parameters[1];
                        foreach (var i in config.Gifts)
                        {
                            if(i.Name ==name)
                            {
                                foreach (var it in i.Items)
                                {
                                    args.Player.GiveItem(it.NetID, it.Stack, it.Perfix);
                                }
                                args.Player.SendSuccessMessage("恭喜您获得了礼包：" + i.Name);
                                return;
                            }
                        }
                        args.Player.SendErrorMessage("未找到礼包:" + name);
                        break;
                    }
                case "随机":
                    {
                        int total = 0;
                        foreach (var i in config.Gifts)
                        {
                            total += i.Probability;
                        }
                        int index = random.Next(0, total);
                        total = 0;
                        for(int i = 0; i < config.Gifts.Length; i++)
                        {
                            total += config.Gifts[i].Probability;
                            if(index<=total)
                            {
                                foreach (var it in config.Gifts[i].Items)
                                {
                                    args.Player.GiveItem(it.NetID, it.Stack, it.Perfix);
                                }
                                args.Player.SendSuccessMessage("恭喜您获得了礼包：" + config.Gifts[i].Name);
                                return;
                            }
                        }
                        break;
                    }
                case "列表":
                    {
                        foreach (var i in config.Gifts)
                        {
                            args.Player.SendInfoMessage($"礼包{i.Name},概率:{i.Probability }");
                            args.Player.SendInfoMessage("内容物如下：");
                            foreach (var it in i.Items)
                            {
                                args.Player.SendInfoMessage($"{Lang.GetItemNameValue(it.NetID)}[i/s{it.Stack}:{it.NetID}]数量：{it.Stack},前缀:{TShock.Utils.GetPrefixById(it.Perfix)}");
                            }
                        }
                        break;
                    }
            }
        }

        /// <summary>
        /// Handles plugin disposal logic.
        /// *Supposed* to fire when the server shuts down.
        /// You should deregister hooks and free all resources here.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Deregister hooks here
            }
            base.Dispose(disposing);
        }
        public class Item
        {
            public int NetID { get; set; } = 0;
            public int Stack { get; set; } = 0;
            public int Perfix { get; set; } = 0;
        }
    }
}