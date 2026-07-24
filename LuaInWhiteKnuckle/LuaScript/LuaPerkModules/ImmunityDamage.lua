-- ============================================================
-- ImmunityDamage 模块
-- 支持从 perk.tags 读取免疫配置
-- 格式: ImmunityDamage:type1,type2,...:0.5
-- ============================================================

local M = {
    id = "ImmunityDamage",
    name = "Perk_Module_Immunity_Damage"
}

-- 运行时状态
M.immunityTypes = {}    -- 减免单类型配置
M.immunityAll = false   -- 是否减免所有类型
M.blockPercent = 1.0    -- 减免全类型倍率

-- ============================================================
-- 配置解析
-- ============================================================

-- 单个标签生成减伤配置
local function ParseImmunityTag(tag)
    -- ImmunityDamage:type1,type2,...:percent
    local typesStr, percentStr = tag:match("^ImmunityDamage:([^:]+):?(.-)$")
    if not typesStr then return nil end

    local result = { types = {}, percent = 1.0 }
    -- 生成减免倍率
    if percentStr and percentStr ~= "" then
        result.percent = tonumber(percentStr) or 1.0
    end

    if typesStr == "all" then
        result.all = true
    else
        -- 捕获多个匹配类型
        for typeName in typesStr:gmatch("([^,]+)") do
            typeName = typeName:match("^%s*(.-)%s*$")
            if typeName ~= "" then
                result.types[typeName] = true
            end
        end
    end

    return result
end

-- 处理所有标签
local function BuildImmunityConfig(tags)
    local config = { types = {}, all = false, maxPercent = 0 }

    for i = 0, tags.Count - 1 do
        local tag = tags[i]
        -- 生成匹配组
        local parsed = ParseImmunityTag(tag)

        if parsed then
            if parsed.all then config.all = true end

            for typeName, _ in pairs(parsed.types) do
                config.types[typeName] = true
            end

            -- 默认取减免最大值
            if parsed.percent > config.maxPercent then
                config.maxPercent = parsed.percent
            end
        end
    end

    if config.maxPercent == 0 then config.maxPercent = 1.0 end

    return config
end

-- ============================================================
-- 免疫检查
-- ============================================================

function M.CheckImmunity(damageTags)
    if M.immunityAll then return true, M.blockPercent end

    if not next(M.immunityTypes) then return false, 0 end

    for i = 0, damageTags.Count - 1 do
        local tag = damageTags[i]
        if M.immunityTypes[tag] then
            return true, M.blockPercent
        end
    end

    return false, 0
end

-- ============================================================
-- 生命周期
-- ============================================================

function M.Initialize(perk, firstTime)
    local config = BuildImmunityConfig(perk.tags)
    M.immunityTypes = config.types
    M.immunityAll = config.all
    M.blockPercent = config.maxPercent

    local typeList = {}
    
    for t, _ in pairs(M.immunityTypes) do
        table.insert(typeList, t)
    end

    print(string.format("[ImmunityDamage] types: %s, percent: %.0f%%",
        M.immunityAll and "ALL" or table.concat(typeList, ","),
        M.blockPercent * 100))

    Game.Hooks.Register("OnPlayerDamage", "ImmunityDamage_" .. perk.id,
        function(damageInfo)
            local isImmune, percent = M.CheckImmunity(damageInfo.tags)

            if isImmune then
                if percent >= 1.0 then return damageInfo, true
                else
                    damageInfo.damage = damageInfo.damage * (1 - percent)
                    return damageInfo, false
                end
            end

            return damageInfo, false
        end)
end

function M.OnDestroy(perk)
    Game.Hooks.Unregister("OnPlayerDamage", "ImmunityDamage_" .. perk.id)
    M.immunityTypes = {}
    M.immunityAll = false
end

-- ============================================================
-- 描述支持
-- ============================================================

function M.GetDescriptionFromKey(key)
    if key == "immunityTypes" then
        if M.immunityAll then return "all" end
        local types = {}
        for t, _ in pairs(M.immunityTypes) do
            table.insert(types, t)
        end
        return #types > 0 and table.concat(types, ",") or "none"
    end

    if key == "immunityPercent" then
        return string.format("%.0f", M.blockPercent * 100)
    end

    return ""
end

return M