# StrongerWeapons
TShock plugin--StrongerWeapons

# Commands
sw or '强化'<br>
normal player's permission:StrongerWeapons.use<br>
administrator's permission:StrongerWeapons.admin<br>
You'll get detatied commands list and useages by execute command:'/sw'<br>
# Skill principle explaination
"Skill book" is a special type of items,which can be any existed item whose prefix is '254'
# About learning skills
If you want to learn a skill,you must have matched "skill book" in your inventory,
and your weapon level must higher than the learning level limit of the "skill book".<br>
Each "skill book" can be used only once,and you're allowed to learn different kinds of skills.<br>
Also , you can forget a skill,and the "skill book" will be returned to you inventory.<br>
For administrators,you can set multiple skills for one weapon.<br>
## About passive skills
### About learned multiple skills for one weapon
The effect of mp and hp restore will be stacked and additionally,cooling time will be sumed up. <br>

3.1.2.关于被动技能的触发<br>
每次使用武器时（挥舞武器时）都会触发被动技能（中的回血回蓝部分）若冷却时间未到则不会触发<br>
3.1.2.1.关于低血量下时的被动技能<br>
低血量时的被动技能分为暴击和闪避<br>
当血量低于或等于技能中低血量值时手持相应武器触发<br>
当为同种武器学习多种技能时效果叠加<br>
4.关于购买技能书<br>
使用相应的一个或多个物品兑换技能书，这些物品必须在背包中<br>
技能书id不是技能书的物品id，而是技能书在配置文件中的索引<br>
以下是配置文件详解<br>
```
Permission 学习权限，""为无需权限
Name 技能书名字
WeaponID 武器id,可填写多个
NetID 技能书物品id
Life 回血值
Rate 按人数回血
回血值=Life+Rate x (在线人数 - 1)
Magic 回蓝值
Delay 冷却时间
Damage 增加的伤害
UseTime 减少的时间
LowLife 血量低于此数值触发暴击和闪避被动技能
CriticalRate 暴击概率（0-100的整数）
Critical 暴击数值（暴击时伤害=原来伤害 x Critical）
DodgeRate 闪避概率（0-100的整数）
Level 武器强化等级大于等于此等级方可学习（若小于等于0则为不做限制）
Drop 掉落，可填写多个
 BossID 击杀时有概率掉落的npc id
 Rate 掉落概率（0-100的整数）
Projectile 弹幕设置（可以设置多个）
 Trace 是否跟踪
 ProjID 弹幕id
 Speed 弹幕速度
 Damage 弹幕伤害
 KnockBack 击退
 Delay 弹幕生成冷却时间
 Defined 是否启用自定义位置速度设置
 X 以玩家为原点x轴坐标
 Y 以玩家为原点y坐标
 VX x轴分速度
 VY y轴分速度
Coins 购买此技能书所需的物品
 netID 物品id
 stack 物品数量
 Perfix 前缀，可以不管
 ```