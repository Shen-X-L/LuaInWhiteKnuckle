-- 定义模块
local M = {
    id = "Death Immunity",
    name = "Perk_Module_Death_Immunity",
    -- 死亡免疫计时器
    lastKillImmunityTrigger = 0,
    -- 免疫冷却冷却cd
    cooldownDuration = 300,
}


--- 从 perk.tags 解析 DeathImmunity:number
-- @param tags C# List<string>
-- @return number 冷却时间
local function ParseCooldown(tags)
    for i = 0, tags.Count - 1 do
        local tag = tags[i]
        
        -- 匹配 DeathImmunity:数字
        local numberStr = tag:match("^DeathImmunity:(%d+)$")
        if numberStr then
            local value = tonumber(numberStr)
            if value and value > 0 then
                print(string.format("[DeathImmunity] cooldown: %d seconds", value))
                return value
            end
        end
    end
    
    -- 没找到，返回默认值
    print("[DeathImmunity] using default cooldown: 300")
    return 300
end

-- 支持小数
local function ParseCooldownFlexible(tags)
    for i = 0, tags.Count - 1 do
        local tag = tags[i]
        local numberStr = tag:match("^DeathImmunity:([%d%.]+)$")
        if numberStr then
            local value = tonumber(numberStr)
            if value and value > 0 then
                return value
            end
        end
    end
    return 300
end

function M.Initialize(perk, firstTime)
    M.cooldownDuration = ParseCooldownFlexible(perk.tags)
    M.buff = perk.baseBuff
    Game.Hooks.Unregister("OnPlayerKill")
    -- 重置冷却期
    M.lastKillImmunityTrigger = 0
    Game.Hooks.Register("OnPlayerKill", "Death_Immunity_Hook",
        function(killType, damageInfo)
            -- 在冷却期内 死亡
            if M.lastKillImmunityTrigger > Game.Time.time then
                return killType, damageInfo, false, 0
            end
            -- 刷新冷却期
            M.lastKillImmunityTrigger = M.cooldownDuration + Game.Time.time
            Player.AddBuffContainer(M.buff)
            Player.Shake(2)
            return killType, damageInfo, true, 1
        end)
end

function M.OnDestroy(perk)
    Game.Hooks.Unregister("OnPlayerKill")
end

function M.GetCounterString()
    if M.lastKillImmunityTrigger > Game.Time.time then
        return string.format("%.0f", M.lastKillImmunityTrigger - Game.Time.time)
    else
        return "ready"
    end
end
-- min value 0.2
M.counter_tick = 1


return M
