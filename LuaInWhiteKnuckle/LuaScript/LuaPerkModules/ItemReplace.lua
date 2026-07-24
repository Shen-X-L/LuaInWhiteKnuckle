-- ============================================================
-- ItemReplace 模块
-- 功能: 当玩家装备物品时,如果物品匹配指定条件,自动替换为其他物品
--
-- 标签格式:
--   ItemReplace:槽位:匹配类型:匹配值:替换物品
--
-- 参数说明:
--   槽位       - hand0 / hand1 / all
--   匹配类型   - tag / name / id
--   匹配值     - 要匹配的具体值,支持逗号分隔的多个值(OR关系)
--   替换物品   - 替换成的物品ID
--
-- 示例:
--   ItemReplace:hand0:tag:explosive:Item_Rebar_Explosive
--     → 左手装备有explosive标签的物品时,替换为爆炸钢筋
--
--   ItemReplace:hand1:prefab:Item_Piton:Item_Auto_Piton
--     → 右手装备旧岩钉时,替换为新岩钉
-- ============================================================

local M = {
    id = "ItemReplace",
    name = "Perk_Module_Item_Replace"
}

M.rules = {}   -- { slot=数字, matchType, matchValues=表, replaceItem }
M.slotAll = -1 -- 常量

-- ============================================================
-- 辅助函数
-- ============================================================

--- 检查物品是否存在
-- @param itemId 物品 ID
-- @return boolean
local function IsItemExist(itemId)
    if Item and Item.isItemExist then
        return Item.isItemExist(itemId)
    end
    return false
end

-- ============================================================
-- 配置解析
-- ============================================================

--- 将字符串槽位转为数字
-- "hand0" → 0, "hand1" → 1, "all" → -1
local function ParseSlot(slotStr)
    if slotStr == "all" then return M.slotAll end
    return tonumber(slotStr:match("hand(%d)"))
end

--- 解析单条规则
local function ParseTag(tag)
    local slotStr, matchType, matchValue, replaceItem = tag:match(
        "^ItemReplace:([^:]+):([^:]+):([^:]+):(.+)$")

    if not slotStr then return nil end

    -- 检查替换目标物品是否存在
    if not IsItemExist(replaceItem) then return nil end

    -- 预解析匹配值，避免每次匹配时分割字符串
    local values = {}
    for v in matchValue:gmatch("([^,]+)") do
        v = v:match("^%s*(.-)%s*$")
        if v ~= "" then values[v] = true end -- 用表做集合，O(1)查找
    end

    return {
        slot = ParseSlot(slotStr), -- 数字: 0, 1, -1
        matchType = matchType,     -- "tag" / "name"
        matchValues = values,      -- 集合: { ["explosive"] = true, ... }
        replaceItem = replaceItem  -- 替换目标
    }
end

-- ============================================================
-- 匹配检查
-- ============================================================

--- 检查物品是否匹配规则
-- @param item 物品对象
-- @param rule 解析后的规则
-- @return boolean
local function MatchItem(item, rule)
    if not item then return false end
    if rule.matchType == "tag" then
        if not item.tags then return false end
        -- 遍历物品的tags，检查是否在规则的集合中
        for i = 0, item.tags.Count - 1 do
            if rule.matchValues[item.tags[i]] then
                return true
            end
        end
    elseif rule.matchType == "name" then
        return rule.matchValues[item.name] or rule.matchValues[item.prefab]
    end

    return false
end

-- ============================================================
-- 事件回调
-- ============================================================

--- C# 回调参数: handIndex(数字), lastItem, currentItem
local function OnHandItemChange(handIndex, lastItem, currentItem)
    for _, rule in ipairs(M.rules) do
        -- 槽位匹配: rule.slot == -1(all) 或相同数字
        if rule.slot == M.slotAll or rule.slot == handIndex then
            if MatchItem(currentItem, rule) then
                -- 执行替换
                Inventory.RemoveHandItem(handIndex)
                Inventory.AddHandItem(rule.replaceItem, handIndex)
                return -- 匹配即返回，不继续检查
            end
        end
    end
end

-- ============================================================
-- 生命周期
-- ============================================================

function M.Initialize(perk, firstTime)
    M.rules = {}

    for i = 0, perk.tags.Count - 1 do
        local rule = ParseTag(perk.tags[i])
        if rule then
            table.insert(M.rules, rule)
            print(string.format("[ItemReplace] 规则生效: %s %s -> %s",
                rule.slot == M.slotAll and "all" or "hand" .. rule.slot,
                rule.matchType,
                rule.replaceItem))
        end
    end

    -- C# 传数字 0/1，Lua 直接用数字比较
    Game.Events.On("OnHandItemChange", "ItemReplace_" .. perk.id, OnHandItemChange)
end

function M.OnDestroy(perk)
    Game.Events.Off("OnHandItemChange", "ItemReplace_" .. perk.id)
    M.rules = {}
end

return M
