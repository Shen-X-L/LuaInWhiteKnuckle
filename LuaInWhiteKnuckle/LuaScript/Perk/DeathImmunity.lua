-- 死亡免疫计时器
local lastKillImmunityTrigger = 0
-- 免疫冷却冷却cd
local DEATH_IMMUNITY_COOLDOWN = 300
-- 死亡免疫技能
local perk_death_immunity
-- 定义perk描述
local perk_death_immunity_description = ""
-- 定义模块
local perk_module_death_immunity = {}

perk_module_death_immunity.name = "Perk_Module_Death_Immunity"

function perk_module_death_immunity.Initialize(perk, firstTime)
    Game.Hooks.Unregister("OnPlayerKill")
    -- 重置冷却期
    lastKillImmunityTrigger = 0
    Game.Hooks.Register("OnPlayerKill", "Death_Immunity_Hook",
        function(killType, damageInfo)
            -- 在冷却期内 死亡
            if lastKillImmunityTrigger > Game.Time.time then
                return killType, damageInfo, false, 0
            end
            -- 刷新冷却期
            lastKillImmunityTrigger = DEATH_IMMUNITY_COOLDOWN + Game.Time.time
            local newBuff = BuffContainerData("Death_Immunity", "Death_Immunity")
            newBuff.loseRate = 0.5
            newBuff.AddBuff("slowTime", 5)
            newBuff.AddBuff("addSpeed", 3)
            newBuff.AddBuff("addJump", 2)
            Player.AddBuffContainer(newBuff)
            Player.Shake(2)
            return killType, damageInfo, true, 1
        end)
end

function perk_module_death_immunity.OnDestroy(perk)
    Game.Hooks.Unregister("OnPlayerKill")
end

function perk_module_death_immunity.GetCounterString()
    if lastKillImmunityTrigger > Game.Time.time then
        return string.format("%.0f", lastKillImmunityTrigger - Game.Time.time)
    else
        return "ready"
    end
end

perk_death_immunity = Perk.CreateCustomBuffPerk("Perk_Death_Immunity",
    "Death Immunity",
    perk_death_immunity_description,
    false)
Perk.AddLuaModule(perk_death_immunity, perk_module_death_immunity)
perk_death_immunity.canStack = false
perk_death_immunity.useBuff = false

Perk.AddPerk(perk_death_immunity)
