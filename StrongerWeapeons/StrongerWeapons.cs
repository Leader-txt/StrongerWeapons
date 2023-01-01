using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TShockAPI;
using Terraria;
using TerrariaApi.Server;
using Microsoft.Xna.Framework;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Diagnostics;

namespace StrongerWeapons
{
    /*
     需求：
    武器强化：{
    武器：{
    伤害,
    击退，
    使用时间（攻速），
    尺寸
    }
    }
     */
    [ApiVersion(2, 1)]
    public class StrongerWeapons : TerrariaPlugin
    {
        /// <summary>
        /// Gets the author(s) of this plugin
        /// </summary>
        public override string Author => "Leader";

        /// <summary>
        /// Gets the description of this plugin.
        /// A short, one lined description that tells people what your plugin does.
        /// </summary>
        public override string Description => "武器强化";

        /// <summary>
        /// Gets the name of this plugin.
        /// </summary>
        public override string Name => "StrongerWeapons";

        /// <summary>
        /// Gets the version of this plugin.
        /// </summary>
        public override Version Version => new Version(3, 2, 1, 0);
        

        /// <summary>
        /// Initializes a new instance of the StrongerWeapeons class.
        /// This is where you set the plugin's order and perfrom other constructor logic
        /// </summary>
        public StrongerWeapons(Main game) : base(game)
        {
            
        }
        /*Skill需求
         StrongerWeapons-Skill更新
        可自定义等级（当达到指定等级时可进行复制武器以及最高可复制数量）
        锻造到指定等级可根据已有技能书将技能附到武器上
        （特殊技能，回蓝回血，按照人数回血，可自定义回血回蓝数值以及冷却时间）
        技能书可配置自定义弹幕，回血，回蓝，伤害附赠（将指定伤害附赠到武器上）攻速附赠（
        将攻速附赠到指定武器上），对指定boss可设置概率掉落此技能书，可设置商店出售技能书
        （配置表中技能书出售和配置按照1 2 3 4排列，商店可显示技能书添加属性）
        可添加被动技能（有概率触发一定伤害，按照此武器伤害倍数，概率可自定义）
        （有概率触发免伤，可自定义概率）（当血量低于指定百分比时，触发武器提升指定伤害倍数和一定
        概率免伤，可自定义概率）
         */
        #region 全局变量
        Dictionary<int, Task> Delay = new Dictionary<int, Task>();//冷却
        Dictionary<int, Dictionary<int, Stopwatch>> ProjDelay = new Dictionary<int, Dictionary<int, Stopwatch>>();
        public static Dictionary<int, List<Learnt>> Learnts = new Dictionary<int, List<Learnt>>();//习得技能
        #endregion
        /// <summary>
        /// Handles plugin initialization. 
        /// Fired when the server is started and the plugin is being loaded.
        /// You may register hooks, perform loading procedures etc here.
        /// </summary>
        public override void Initialize()
        {
            Commands.ChatCommands.Add(new Command("StrongerWeapons.use", sw, "强化", "sw"));
            Commands.ChatCommands.Add(new Command("StrongerWeapons.admin", sw_admin, "强化管理"));
            Commands.ChatCommands.Add(new Command("StrongerWeapons.use", learn, "学习"));
            Config.GetConfig();
            ServerApi.Hooks.GameInitialize.Register(this, OnInvitatize);
            ServerApi.Hooks.NetGreetPlayer.Register(this, OnGreetPlayer);
            ServerApi.Hooks.NpcKilled.Register(this, OnNPCKilled);
            ServerApi.Hooks.NpcStrike.Register(this, OnNPCStrike);
            GetDataHandlers.PlayerUpdate.Register(OnPlayerUpdate);
            GetDataHandlers.PlayerDamage.Register(OnPlayerDamage);//玩家受伤钩子,被动技能
            //Commands.ChatCommands.Add(new Command("", test, "test"));
        }

        private void test(CommandArgs args)
        {
            Terraria.Projectile.NewProjectile(null, args.Player.TPlayer.position, new Vector2(0,0), 1, 10, 10);
        }

        private void learn(CommandArgs args)
        {
            if(args.Parameters .Count == 0)
            {
                args.Player.SendInfoMessage("[i:4956]技能书学习系统[i:4956]：");
                Commands.HandleCommand(args.Player, Commands.Specifier + "强化 学习");
                return;
            }
            string cmd = TShock.Config.Settings.CommandSpecifier + "强化 学习";
            foreach (var s in args.Parameters)
            {
                cmd += $" \"{s}\"";
            }
            Commands.HandleCommand (args.Player , cmd);
        }

        private void sw_admin(CommandArgs args)
        {
            if(args.Parameters.Count == 0)
            {
                args.Player.SendInfoMessage("[i:4956]管理强化指令[i:4956]:");
                args.Player.SendInfoMessage("/强化 添加 物品ID 伤害 击退 CD(0-100的整数) 大小 强化成功概率(0-100的整数) 强化次数，添加一项武器强化");
                args.Player.SendInfoMessage("/强化 修改 索引 物品id 伤害 击退 CD(0-100的整数) 大小 强化成功概率(0-100的整数) 强化次数，修改一项武器强化");
                args.Player.SendInfoMessage("/强化 删除 索引，删除这项武器强化");
                args.Player.SendInfoMessage("/强化 添加材料 索引 材料ID 材料数量，添加这项强化所需材料，如已存在该材料则修改该材料所需数量");
                args.Player.SendInfoMessage("/强化 删除材料 索引，删除这项强化的其中一种材料");
                args.Player.SendInfoMessage("/强化 重置玩家，重置玩家数据");
                args.Player.SendInfoMessage("/强化 重置强化，重置强化数据");
                args.Player.SendInfoMessage("/强化 重置次数，重置强化次数");
                args.Player.SendInfoMessage("/强化 重置技能，重置已学习的技能");
                return;
            }
            switch (args.Parameters[0])
            {
                case "重置技能":
                    {
                        Data.Command("delete from Learnt");
                        args.Player.SendSuccessMessage("已删除所有已学习技能！");
                    }
                    break;
                case "重置次数":
                    {
                        Data.Command("delete from user");
                        args.Player.SendSuccessMessage("已删除所有强化次数！");
                        break;
                    }
                case "重置强化":
                    {
                        Data.Command("delete from Stronger");
                        args.Player.SendSuccessMessage("已删除所有强化数据！");
                        break;
                    }
                case "重置玩家":
                    {
                        Data.Command("delete from Weapons");
                        args.Player.SendSuccessMessage("已删除所有玩家数据！");
                        break;
                    }
                case "删除材料":
                    {
                        var index = int.Parse(args.Parameters[1]);
                        var str = Stronger.GetStrongerByIndex(index);
                        if (str == null)
                        {
                            args.Player.SendErrorMessage("未找到该项记录！索引：" + index);
                            return;
                        }
                        var id = int.Parse(args.Parameters[2]);
                        var needs = new List<Item>();
                        needs.AddRange(str.Needs);
                        for (int i = 0; i < str.Needs.Length; i++)
                        {
                            if (str.Needs[i].netID == id)
                            {
                                needs.RemoveAt(i);
                                str.Needs = needs.ToArray();
                                str.Save();
                                args.Player.SendSuccessMessage("移除成功！");
                                return;
                            }
                        }
                        args.Player.SendErrorMessage("未找到该项材料！移除失败！");
                        break;
                    }
                case "添加材料":
                    {
                        var index = int.Parse(args.Parameters[1]);
                        var str = Stronger.GetStrongerByIndex(index);
                        if (str == null)
                        {
                            args.Player.SendErrorMessage("未找到该项记录！索引：" + index);
                            return;
                        }
                        var id = int.Parse(args.Parameters[2]);
                        var stack = int.Parse(args.Parameters[3]);
                        foreach (var i in str.Needs)
                        {
                            if (i.netID == id)
                            {
                                i.stack = stack;
                                args.Player.SendSuccessMessage("已有该项材料，数量已更新为:" + stack);
                                str.Save();
                                return;
                            }
                        }
                        var needs = new List<Item>();
                        needs.AddRange(str.Needs);
                        needs.Add(new Item() { netID = id, stack = stack });
                        str.Needs = needs.ToArray();
                        str.Save();
                        args.Player.SendSuccessMessage("添加材料成功！");
                        break;
                    }
                case "删除":
                    {
                        var index = int.Parse(args.Parameters[1]);
                        var str = Stronger.GetStrongerByIndex(index);
                        if (str == null)
                        {
                            args.Player.SendErrorMessage("未找到该项记录！索引：" + index);
                            return;
                        }
                        else
                        {
                            str.Del();
                            Data.Command($"delete from User where id={str.Index}");
                            args.Player.SendSuccessMessage("删除成功！索引：" + index);
                        }
                        break;
                    }
                case "修改":
                    {
                        var index = int.Parse(args.Parameters[1]);
                        var str = Stronger.GetStrongerByIndex(index);
                        if (str == null)
                        {
                            args.Player.SendErrorMessage("未找到该项记录！索引：" + index);
                            return;
                        }
                        var id = int.Parse(args.Parameters[2]);
                        var damage = int.Parse(args.Parameters[3]);
                        var knockback = int.Parse(args.Parameters[4]);
                        var usetime = int.Parse(args.Parameters[5]);
                        var size = int.Parse(args.Parameters[6]);
                        var probablity = int.Parse(args.Parameters[7]);
                        var stack = int.Parse(args.Parameters[8]);
                        if (usetime > 100 || usetime < 0 || damage < 0 || knockback < 0 || size < 0 || probablity > 100 || probablity < 0 || stack < 0)
                        {
                            args.Player.SendErrorMessage("输入的数值不正确！请重新输入！");
                            return;
                        }
                        Stronger.Update(new Stronger(id, damage, knockback, usetime, size, probablity, stack, index, new Item[0]));
                        args.Player.SendSuccessMessage("修改成功！索引：" + index);
                        break;
                    }
                case "添加":
                    {
                        var id = int.Parse(args.Parameters[1]);
                        var damage = int.Parse(args.Parameters[2]);
                        var knockback = int.Parse(args.Parameters[3]);
                        var usetime = int.Parse(args.Parameters[4]);
                        var size = int.Parse(args.Parameters[5]);
                        var probablity = int.Parse(args.Parameters[6]);
                        var stack = int.Parse(args.Parameters[7]);
                        if (usetime > 100 || usetime < 0 || damage < 0 || knockback < 0 || size < 0 || probablity > 100 || probablity < 0 || stack < 0)
                        {
                            args.Player.SendErrorMessage("输入的数值不正确！请重新输入！");
                            return;
                        }
                        Stronger.Update(new Stronger(id, damage, knockback, usetime, size, probablity, stack, -1, new Item[0]));
                        args.Player.SendSuccessMessage("添加成功！");
                        break;
                    }
            }
        }

        private void OnNPCStrike(NpcStrikeEventArgs args)
        {
            try
            {
                var player = TShock.Players[args.Npc.lastInteraction];
                var learnts = Learnts[player.Index].FindAll((Learnt l) => l.NetID == player.SelectedItem.netID);
                new Thread(() =>
                {
                    foreach (var learnt in learnts)
                    {
                        var skill = learnt.Skill;
                        if (player.TPlayer.statLife <= skill.LowLife)
                        {
                            if (Utils.Rate(skill.CriticalRate))
                            {
                                args.Damage *= skill.Critical;
                                player.SendInfoMessage("暴击！");
                            }
                        }
                    }
                })
                { IsBackground = true }.Start();
            }
            catch { }
        }

        private void OnPlayerUpdate(object sender, GetDataHandlers.PlayerUpdateEventArgs e)
        {
            if (e.Control.IsUsingItem)
            {
                try
                {
                    if (e.Player != null && e.Player.Active)
                    {
                        var player = e.Player;
                        new Thread(() =>
                        {
                            try
                            {
                                foreach (var learnt in Learnts[player.Index])
                                {
                                    //player.SendInfoMessage(learnt.NetID +" "+player.SelectedItem.netID);
                                    if (learnt.NetID == player.SelectedItem.netID)
                                    {
                                        var skill = learnt.Skill;
                                        if (!ProjDelay.ContainsKey(e.Player.Index))
                                        {
                                            ProjDelay.Add(e.Player.Index, new Dictionary<int, Stopwatch>());
                                        }
                                        foreach (var p in skill.Projs)
                                        {
                                            if (ProjDelay[e.Player.Index].ContainsKey(p.ProjID))//若有记录
                                            {
                                                if (ProjDelay[e.Player.Index][p.ProjID].ElapsedMilliseconds >= p.Delay)//如果冷却时间已过
                                                {
                                                    //重新计时冷却时间
                                                    ProjDelay[e.Player.Index][p.ProjID].Restart();
                                                }
                                                else//否则跳过生成该弹幕
                                                {
                                                    continue;
                                                }
                                            }
                                            else//若无记录则添加计时器
                                            {
                                                var stopwatch = new Stopwatch();
                                                stopwatch.Start();
                                                ProjDelay[e.Player.Index].Add(p.ProjID, stopwatch);
                                            }
                                            if (p.Defined)
                                            {
                                                var index = Terraria.Projectile.NewProjectile(null, e.Position + new Vector2(p.X, p.Y), new Vector2(p.VX, p.VY) * p.Speed, p.Trace ? 207 : p.ProjID, p.Damage, p.KnockBack);
                                                //Main.projectile[index].friendly = true;
                                                //Main.projectile[index].Update(index);
                                            }
                                            else
                                            {
                                                var vec = e.Player.TPlayer.position - e.Player.TPlayer.oldPosition;
                                                var mod = (float)Math.Sqrt(Math.Pow(vec.X, 2) + Math.Pow(vec.Y, 2));
                                                vec.X /= mod;
                                                vec.Y /= mod;
                                                var index = Terraria.Projectile.NewProjectile(null, e.Position, vec * p.Speed, p.Trace ? 207 : p.ProjID, p.Damage, p.KnockBack);
                                                //Main.projectile[index].friendly = true;
                                                //Main.projectile[index].Update(index);
                                            }
                                        }
                                    }
                                }
                            }
                            catch { }
                        })
                        { IsBackground = true }.Start();
                    }
                }
                catch { }
                //GetDataHandlers.NewProjectile.Register(OnNewProjectile);
                //回血和回蓝
                try
                {
                    if (Delay[e.PlayerId].IsCompleted)
                    {
                        var weapon = e.Player.SelectedItem;
                        int wait = 0;
                        var learnts = Learnts[e.Player.Index].FindAll((Learnt l) => l.NetID == weapon.netID);
                        foreach (var learnt in learnts)
                        {
                            var skill = learnt.Skill;
                            e.Player.TPlayer.ManaEffect(skill.Magic);
                            e.Player.Heal(skill.Life + skill.Rate * (Utils.GetOnlineCount() - 1));
                            wait += skill.Delay;
                        }
                        Delay[e.PlayerId] = new Task(() =>
                        {
                            Thread.Sleep(1000 * wait);
                        });
                        Delay[e.PlayerId].Start();
                    }
                }
                catch
                {
                    var weapon = e.Player.SelectedItem;
                    int wait = 0;
                    var learnts = Learnts[e.Player.Index].FindAll((Learnt l) => l.NetID == weapon.netID);
                    foreach (var learnt in learnts)
                    {
                        var skill = learnt.Skill;
                        e.Player.TPlayer.ManaEffect(skill.Magic);
                        e.Player.Heal(skill.Life + skill.Rate * (Utils.GetOnlineCount() - 1));
                        wait += skill.Delay;
                    }
                    Delay.Add(e.PlayerId, new Task(() =>
                    {
                        Thread.Sleep(1000 * wait);
                    }));
                    Delay[e.PlayerId].Start();
                }
            }
        }

        private void OnNPCKilled(NpcKilledEventArgs args)
        {
            if (args.npc.boss)
            {
                try
                {
                    var killer = TShock.Players[args.npc.lastInteraction];
                    if (killer == null || !killer.Active)
                        return;
                    var config = Config.GetConfig();
                    foreach (var skill in config.Skills)
                    {
                        var drop = skill.Drop.ToList().Find((Drop d) => d.BossID == args.npc.netID);
                        if (drop != null&&Utils.Rate(drop.Rate ))
                        {
                            skill.DropMe(killer.Index);
                        }
                    }
                }
                catch { }
            }
        }

        /// <summary>
        /// 玩家受伤
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">钩子参数</param>
        private void OnPlayerDamage(object sender, GetDataHandlers.PlayerDamageEventArgs e)
        {
            try
            {
                var learnts = Learnts[e.Player.Index].FindAll((Learnt l) => l.NetID == e.Player.SelectedItem.netID);
                new Thread(() =>
                {
                    foreach (var learnt in learnts)
                    {
                        var skill = learnt.Skill;
                        if (e.Player.TPlayer.statLife <= skill.LowLife)
                        {
                            if (Utils.Rate(skill.DodgeRate))
                            {
                                e.Handled = true;
                                e.Player.SendInfoMessage("闪避！");
                                return;
                            }
                        }
                    }
                })
                { IsBackground = true }.Start();
            }
            catch { }
            if (e.PVP)
            {
                try
                {
                    var hurter = TShock.Players[e.Direction];
                    var learnts = Learnts[e.Player.Index].FindAll((Learnt l) => l.NetID == e.Player.SelectedItem.netID);
                    new Thread(() =>
                    {
                        foreach (var learnt in learnts)
                        {
                            var skill = learnt.Skill;
                            if (hurter.TPlayer.statLife <= skill.LowLife)
                            {
                                if (Utils.Rate(skill.CriticalRate))
                                {
                                    e.Damage *= (short)skill.CriticalRate;
                                    hurter.SendInfoMessage("暴击！");
                                    return;
                                }
                            }
                        }
                    })
                    { IsBackground = true }.Start();
                }
                catch { }
            }
        }

        private void OnGreetPlayer(GreetPlayerEventArgs args)
        {
            Config config = Config.GetConfig();
            if (config.FlashRightNow)
                Utils.UpdateWeas(args.Who);
            try
            {
                Learnts[args.Who] = Learnt.GetLearnts(TShock.Players[args.Who].Name).ToList();
            }
            catch
            {
                Learnts.Add(args.Who ,Learnt.GetLearnts(TShock.Players[args.Who].Name).ToList());
            }
        }

        private void OnInvitatize(EventArgs args)
        {
            Data.Init();
            new Thread(() =>
            {
                while (true)
                {
                    Thread.Sleep(Config.GetConfig().FlashTimer * 1000);
                    foreach (var p in TShock.Players)
                    {
                        if (p != null && p.Active)
                        {
                            try
                            {
                                Utils.UpdateWeas(p.Index);
                            }
                            catch { }
                        }
                    }
                }
            })
            { IsBackground=true}.Start();
        }

        public static Random random = new Random();
        const string Permission = "StrongerWeapons.admin";
        private void sw(CommandArgs args)
        {
            if (args.Parameters.Count == 0)
            {
                args.Player.SendInfoMessage("[I:4956]玩家强化指令[I:4956]：");
                args.Player.SendInfoMessage("/强化 手持，强化手持武器");
                args.Player.SendInfoMessage("/强化 列表 [武器id]");
                args.Player.SendInfoMessage("/强化 退回 武器id 伤害 击退 攻速 大小");
                args.Player.SendInfoMessage("/强化 查看 [玩家名]，查看当前所有已强化的武器");
                args.Player.SendInfoMessage("/强化 升级 索引，强化武器，注意材料必须在背包中，强化失败不返还材料（注：CD只能强化到99%超出不返还材料）");
                args.Player.SendInfoMessage("/强化 刷新，刷新背包中的武器");
                args.Player.SendInfoMessage("/强化 学习，学习技能");
                if (args.Player.HasPermission(Permission))
                {
                    args.Player.SendInfoMessage("/强化 添加 物品ID 伤害 击退 CD(0-100的整数) 大小 强化成功概率(0-100的整数) 强化次数，添加一项武器强化");
                    args.Player.SendInfoMessage("/强化 修改 索引 物品id 伤害 击退 CD(0-100的整数) 大小 强化成功概率(0-100的整数) 强化次数，修改一项武器强化");
                    args.Player.SendInfoMessage("/强化 删除 索引，删除这项武器强化");
                    args.Player.SendInfoMessage("/强化 添加材料 索引 材料ID 材料数量，添加这项强化所需材料，如已存在该材料则修改该材料所需数量");
                    args.Player.SendInfoMessage("/强化 删除材料 索引，删除这项强化的其中一种材料");
                    args.Player.SendInfoMessage("/强化 重置玩家，重置玩家数据");
                    args.Player.SendInfoMessage("/强化 重置强化，重置强化数据");
                    args.Player.SendInfoMessage("/强化 重置次数，重置强化次数");
                    args.Player.SendInfoMessage("/强化 重置技能，重置已学习的技能");
                }
                return;
            }
            if(args.Player.HasPermission(Permission))
            {
                switch (args.Parameters[0])
                {
                    case "重置技能":
                        {
                            Data.Command("delete from Learnt");
                            args.Player.SendSuccessMessage("已删除所有已学习技能！");
                        }
                        break;
                    case "重置次数":
                        {
                            Data.Command("delete from user");
                            args.Player.SendSuccessMessage("已删除所有强化次数！");
                            break;
                        }
                    case "重置强化":
                        {
                            Data.Command("delete from Stronger");
                            args.Player.SendSuccessMessage("已删除所有强化数据！");
                            break;
                        }
                    case "重置玩家":
                        {
                            Data.Command("delete from Weapons");
                            args.Player.SendSuccessMessage("已删除所有玩家数据！");
                            break;
                        }
                    case "删除材料":
                        {
                            var index = int.Parse(args.Parameters[1]);
                            var str = Stronger.GetStrongerByIndex(index);
                            if (str == null)
                            {
                                args.Player.SendErrorMessage("未找到该项记录！索引：" + index);
                                return;
                            }
                            var id = int.Parse(args.Parameters[2]);
                            var needs = new List<Item>();
                            needs.AddRange(str.Needs);
                            for(int i = 0; i < str.Needs.Length; i++)
                            {
                                if(str.Needs[i].netID == id)
                                {
                                    needs.RemoveAt(i);
                                    str.Needs = needs.ToArray();
                                    str.Save();
                                    args.Player.SendSuccessMessage("移除成功！");
                                    return;
                                }
                            }
                            args.Player.SendErrorMessage("未找到该项材料！移除失败！");
                            break;
                        }
                    case "添加材料":
                        {
                            var index = int.Parse(args.Parameters[1]);
                            var str = Stronger.GetStrongerByIndex(index);
                            if (str == null)
                            {
                                args.Player.SendErrorMessage("未找到该项记录！索引：" + index);
                                return;
                            }
                            var id = int.Parse(args.Parameters[2]);
                            var stack = int.Parse(args.Parameters[3]);
                            foreach (var i in str.Needs)
                            {
                                if(i.netID==id)
                                {
                                    i.stack = stack;
                                    args.Player.SendSuccessMessage("已有该项材料，数量已更新为:" + stack);
                                    str.Save();
                                    return;
                                }
                            }
                            var needs = new List<Item>();
                            needs.AddRange(str.Needs);
                            needs.Add(new Item() { netID = id, stack = stack });
                            str.Needs = needs.ToArray();
                            str.Save();
                            args.Player.SendSuccessMessage("添加材料成功！");
                            break;
                        }
                    case "删除":
                        {
                            var index = int.Parse(args.Parameters[1]);
                            var str = Stronger.GetStrongerByIndex(index);
                            if(str==null)
                            {
                                args.Player.SendErrorMessage("未找到该项记录！索引：" + index);
                                return;
                            }
                            else
                            {
                                str.Del();
                                Data.Command($"delete from User where id={str.Index}");
                                args.Player.SendSuccessMessage("删除成功！索引：" + index);
                            }
                            break;
                        }
                    case "修改":
                        {
                            var index = int.Parse(args.Parameters[1]);
                            var str = Stronger.GetStrongerByIndex(index);
                            if (str == null)
                            {
                                args.Player.SendErrorMessage("未找到该项记录！索引：" + index);
                                return;
                            }
                            var id = int.Parse(args.Parameters[2]);
                            var damage = int.Parse(args.Parameters[3]);
                            var knockback = int.Parse(args.Parameters[4]);
                            var usetime = int.Parse(args.Parameters[5]);
                            var size = int.Parse(args.Parameters[6]);
                            var probablity = int.Parse(args.Parameters[7]);
                            var stack = int.Parse(args.Parameters[8]);
                            if(usetime >100||usetime<0||damage <0||knockback<0||size<0||probablity>100||probablity < 0||stack <0)
                            {
                                args.Player.SendErrorMessage("输入的数值不正确！请重新输入！");
                                return;
                            }
                            Stronger.Update(new Stronger(id, damage, knockback, usetime, size, probablity,stack, index, new Item[0]));
                            args.Player.SendSuccessMessage("修改成功！索引：" + index);
                            break;
                        }
                    case "添加":
                        {
                            var id = int.Parse(args.Parameters[1]);
                            var damage = int.Parse(args.Parameters[2]);
                            var knockback = int.Parse(args.Parameters[3]);
                            var usetime = int.Parse(args.Parameters[4]);
                            var size = int.Parse(args.Parameters[5]);
                            var probablity = int.Parse(args.Parameters[6]);
                            var stack = int.Parse(args.Parameters[7]);
                            if (usetime > 100 || usetime < 0 || damage < 0 || knockback < 0 || size < 0 || probablity > 100 || probablity < 0||stack<0)
                            {
                                args.Player.SendErrorMessage("输入的数值不正确！请重新输入！");
                                return;
                            }
                            Stronger.Update(new Stronger(id, damage, knockback, usetime, size, probablity,stack, -1, new Item[0]));
                            args.Player.SendSuccessMessage("添加成功！");
                            break;
                        }
                }
            }
            switch (args.Parameters[0])
            {
                case "学习":
                    {
                        void SendInfo()
                        {
                            args.Player.SendInfoMessage("/强化 学习 列表 [页码] [背包/已学习]，查看所有技能书列表，若添加'背包'参数则只显示可学习的技能，若添加'已学习'参数查看已学习的技能");
                            args.Player.SendInfoMessage("/强化 学习 精简列表");
                            args.Player.SendInfoMessage("/强化 学习 武器id，学习指定武器技能");
                            args.Player.SendInfoMessage("/强化 学习 解绑 技能书 武器id，解绑指定技能书");
                            args.Player.SendInfoMessage("/强化 学习 购买 技能书id，购买指定技能书");
                            args.Player.SendInfoMessage("/强化 学习 查看详细信息 技能书id");
                        }
                        if(args.Parameters.Count <= 1)
                        {
                            SendInfo();
                            return;
                        }
                        try
                        {
                            switch (args.Parameters[1])
                            {
                                case "查看详细信息":
                                    {
                                        var index=int.Parse (args.Parameters[2]);
                                        var s=Config.GetConfig().Skills[index];
                                        string drops = "";
                                        foreach (var d in s.Drop)
                                        {
                                            drops += $"\n击杀{Lang.GetNPCNameValue(d.BossID)}有{d.Rate}%的概率掉落此技能书";
                                        }
                                        var str = $"技能书id:{index}\n" + (args.Player.HasPermission(s.Permission) ? "" : "您无权学习此技能书！") + s.Info() + drops;
                                        str += "\n购买此技能书需要：";
                                        foreach (var i in s.Coins)
                                        {
                                            str += "\n" + i.ToString();
                                        }
                                        args.Player.SendInfoMessage(str);
                                    }
                                    break;
                                case "精简列表":
                                    {
                                        List<Skill> list = Config.GetConfig().Skills.ToList();
                                        int index = 0;
                                        foreach (var skill in list)
                                        {
                                            string wea = "适配武器:";
                                            skill.WeaponID.ToList().ForEach(s =>
                                            {
                                                wea += "," + Lang.GetItemName(s);
                                            });
                                            args.Player.SendInfoMessage($"{index}:{skill.Name}" + wea);
                                            index++;
                                        }
                                    }
                                    break;
                                case "购买":
                                    {
                                        int index = int.Parse(args.Parameters[2]);
                                        var skill = Config.GetConfig().Skills[index];
                                        if (!args.Player.HasPermission(skill.Permission))
                                        {
                                            args.Player.SendErrorMessage("您无权购买此技能书");
                                            return;
                                        }
                                        if(!Utils.CanDel(args.Player.Index, skill.Coins))
                                        {
                                            args.Player.SendErrorMessage("背包中兑换物品不足！");
                                            args.Player.SendInfoMessage("您需要");
                                            foreach (var i in skill.Coins)
                                            {
                                                args.Player.SendInfoMessage(i.ToString());
                                            }
                                            args.Player.SendInfoMessage("以兑换该技能书");
                                            return;
                                        }
                                        Utils.DelNeeds(args.Player.Index, skill.Coins);
                                        skill.DropMe(args.Player.Index,false);
                                        args.Player.SendSuccessMessage("兑换成功！");
                                    }
                                    break;
                                case "解绑":
                                    {
                                        int index = int.Parse(args.Parameters[2]);
                                        var skill = Config.GetConfig().Skills[index];
                                        var weapon = int.Parse(args.Parameters[3]);
                                        if (!Learnts.Keys.Contains(args.Player.Index)||
                                            !Learnts[args.Player.Index].Remove
                                            (Learnts[args.Player.Index ].Find ((Learnt l)=>l.Skill.NetID ==skill.NetID&&l.NetID ==weapon)))
                                        {
                                            args.Player.SendErrorMessage("未学习此技能书！");
                                            return;
                                        }
                                        foreach (var i in skill.WeaponID)
                                        {
                                            var Weapon = Weapons.GetWeapon(args.Player.Name, i);
                                            if (Weapon != null)
                                            {
                                                Weapon.Damage -= skill.Damage;
                                                Weapon.UseTime += skill.UseTime;
                                                Utils.UpdateWea(args.Player.Index, Weapon);
                                                Weapon.Save();
                                            }
                                        }
                                        (new Learnt { Name = args.Player.Name, Index = index,NetID=weapon }).Remove();
                                        skill.DropMe(args.Player.Index,false);
                                        args.Player.SendInfoMessage("解绑成功！");
                                    }
                                    break;
                                case "列表":
                                    {
                                        List<Skill> list = Config.GetConfig().Skills.ToList();
                                        try
                                        {
                                            if (args.Parameters.Contains("背包"))
                                            {
                                                list = list.FindAll((Skill s) => s.Has(args.Player.Index));
                                            }
                                            else if (args.Parameters.Contains("已学习"))
                                            {
                                                if (!Learnts.Keys.Contains(args.Player.Index) || Learnts[args.Player.Index].Count == 0)
                                                {
                                                    args.Player.SendErrorMessage("未学习任何技能！");
                                                    return;
                                                }
                                                foreach (var l in Learnts[args.Player.Index ])
                                                {
                                                    list.Add(l.Skill);
                                                }
                                            }
                                        }
                                        catch { }
                                        var info = new List<string>();
                                        int index = 0;
                                        if(args.Parameters.Contains("背包"))
                                        {
                                            args.Player.SendInfoMessage("背包中技能书如下");
                                            foreach (var l in Learnts[args.Player.Index])
                                            {
                                                info.Add((args.Player.HasPermission(l.Skill.Permission) ? "" : "您无权学习此技能书！")+l.Skill.Info());
                                            }
                                        }
                                        if (args.Parameters.Contains("已学习"))
                                        {
                                            args.Player.SendInfoMessage("已学习列表如下");
                                            foreach (var l in Learnts[args.Player.Index])
                                            {
                                                info.Add(l.Skill.Name + $"->[i:{l.NetID}]{Lang.GetItemNameValue(l.NetID)}");
                                            }
                                        }
                                        else
                                            foreach (var s in list)
                                            {
                                                string drops = "";
                                                foreach (var d in s.Drop)
                                                {
                                                    drops += $"\n击杀{Lang.GetNPCNameValue(d.BossID)}有{d.Rate}%的概率掉落此技能书";
                                                }
                                                var str = $"技能书id:{index}\n" + (args.Player.HasPermission(s.Permission)?"":"您无权学习此技能书！")+s.Info() + drops;
                                                str += "\n购买此技能书需要：";
                                                foreach (var i in s.Coins)
                                                {
                                                    str += "\n" + i.ToString();
                                                }
                                                info.Add(str);
                                                index++;
                                            }
                                        int pagenum = -1;
                                        if (args.Parameters.Count == 2)
                                            pagenum = 1;
                                        else
                                            PaginationTools.TryParsePageNumber(args.Parameters, 2, args.Player, out pagenum);
                                        if (pagenum == -1)
                                            return;
                                        PaginationTools.SendPage(args.Player, pagenum, info, new PaginationTools.Settings()
                                        {
                                            HeaderFormat = "技能书列表，第{0}/{1}页",
                                            MaxLinesPerPage = 5
                                        });
                                    }
                                    break;
                                default:
                                    {
                                        var weapon = int.Parse(args.Parameters[1]);
                                        var skill = Config.GetConfig().Skills.ToList().FindIndex((Skill s) => s.WeaponID.Contains(weapon));
                                        if (skill == -1)
                                        {
                                            args.Player.SendInfoMessage($"没有该武器的技能书！");
                                            return;
                                        }
                                        Utils.LearnSkill(args.Player.Name, skill,weapon);
                                    }
                                    break;
                            }
                        }
                        catch(Exception ex)
                        {
                            SendInfo();
                            Console.WriteLine(ex);
                        }
                    }
                    break;
                case "退回":
                    {
                        var id = int.Parse(args.Parameters[1]);
                        var w = Weapons.GetWeapon(args.Player.Name, id);
                        if (w == null)
                        {
                            args.Player.SendErrorMessage("您尚未进行此项武器的强化！");
                        }
                        var damage = int.Parse(args.Parameters[2]);
                        var knockback = int.Parse(args.Parameters[3]);
                        var usetime = int.Parse(args.Parameters[4]);
                        var size = int.Parse(args.Parameters[5]);
                        if(damage <0||knockback < 0 || usetime < 0 || size < 0)
                        {
                            args.Player.SendErrorMessage("数值必须大于等于0！！！");
                            return;
                        }
                        w.Damage -= damage;
                        if (w.Damage < 0)
                            w.Damage = 0;
                        w.Knockback -= knockback;
                        if (w.Knockback < 0)
                            w.Knockback = 0;
                        w.UseTime -= usetime;
                        if (w.UseTime < 0)
                            w.UseTime = 0;
                        w.Size -= size;
                        if (w.Size < 0)
                            w.Size = 0;
                        w.Save();
                        Utils.UpdateWea(args.Player.Index, w);
                        args.Player.SendInfoMessage($"退回属性成功！当前属性:伤害:+{w.Damage }%,击退:+{w.Knockback}%,CD:-{w.UseTime}%,大小:+{w.Size}%");
                        break;
                    }
                case "刷新":
                    {
                        Utils.UpdateWeas(args.Player.Index);
                        args.Player.SendInfoMessage("刷新成功，如果没有反应请确保背包中有相应武器以及是否进行了强化");
                        break;
                    }
                case "升级":
                    {
                        var index = int.Parse(args.Parameters[1]);
                        var stronger = Stronger.GetStrongerByIndex(index);
                        if (stronger != null)
                        {
                            foreach(var i in args.Player.TPlayer.inventory)
                            {
                                if(i.netID ==stronger.NetID)
                                {
                                    Utils.StrWea(args.Player.Index, stronger);
                                    return;
                                }
                            }
                            args.Player.SendErrorMessage("您的背包中没有" + Lang.GetItemNameValue(stronger.NetID) + "无法进行强化！");
                        }
                        else
                        {
                            args.Player.SendErrorMessage("未找到该项强化！索引:" + index);
                        }
                        break;
                    }
                case "手持":
                    {
                        var id = args.Player.SelectedItem.netID;
                        var stronger = Stronger.GetStrongerByID(id);
                        foreach(var s in stronger)
                        {
                            Utils.StrWea(args.Player.Index, s);
                        }
                        if (stronger.Length == 0)
                        {
                            args.Player.SendErrorMessage($"没有手持武器的强化项！");
                        }
                        break;
                    }
                case "查看":
                    {
                        var wes = Weapons.GetWeapons(args.Parameters.Count >1?args.Parameters[1]:args.Player.Name);
                        args.Player.SendInfoMessage("以下为所有已强化武器");
                        foreach (var w in wes)
                        {
                            args.Player.SendInfoMessage($"武器:[i:{w.NetID}]{Lang.GetItemNameValue(w.NetID)}伤害:+{w.Damage}%,击退:+{w.Knockback}%,CD:-{w.UseTime}%,大小:+{w.Size}%,等级:{w.Level}");
                        }
                        break;
                    }
                case "列表":
                    {
                        var strs = args.Parameters.Count == 1 ? Stronger.GetStronger() : Stronger.GetStrongerByID(int.Parse(args.Parameters[1]));
                        var list = new List<string>();
                        args.Player.SendInfoMessage($"以下为所有"+(args.Parameters.Count ==1?"":$"关于{Lang.GetItemNameValue(int.Parse(args.Parameters[1]))}的")+"强化项目");
                        foreach (var s in strs)
                        {
                            if(args.Parameters .Count == 1)
                            {
                                args.Player.SendInfoMessage($"{s.Index }:{Lang.GetItemNameValue(s.NetID)}[i:{s.NetID}] id:{s.NetID}");
                            }
                            else
                            {
                                var user = new User();
                                try
                                {
                                    user = User.GetUser(args.Player.Index, s.Index);
                                }
                                catch
                                {
                                    user.Stack = 0;
                                }
                                args.Player.SendInfoMessage($"索引:{s.Index},{Lang.GetItemNameValue(s.NetID)}[i:{s.NetID}],成功率:{s.Probability}%伤害:+{s.Damage}%,击退:+{s.Knockback}%,CD:-{s.UseTime}%,大小:+{s.Size }%,剩余强化次数:{(user == null ? s.Stack : s.Stack - user.Stack)}");
                                args.Player.SendInfoMessage("以下为所需材料");
                                foreach (var i in s.Needs)
                                {
                                    args.Player.SendInfoMessage($"材料:[i/s{i.stack}:{i.netID}]{Lang.GetItemNameValue(i.netID)}x{i.stack}");
                                }
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
    }
}