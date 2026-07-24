-- ============================================================
-- ItemReplace 模块
-- 功能: 当玩家装备物品时,如果物品匹配指定条件,自动替换为其他物品
--
-- 标签格式:
--   ItemRemove:槽位:匹配类型:匹配值
--
-- 参数说明:
--   槽位       - hand0 / hand1 / all
--   匹配类型   - tag / name / id
--   匹配值     - 要匹配的具体值,支持逗号分隔的多个值(OR关系)
--
-- 示例:
--   ItemRemove:hand0:tag:explosive
--     → 左手装备有explosive标签的物品时删除
--
--   ItemRemove:all:name:Item_Cursed,Item_Corrupted
--     → 任何槽位装备诅咒或腐化物品时删除
--
--   ItemRemove:hand1:id:Item_AutoPiton
--     → 右手装备旧岩钉时删除
-- ============================================================

local M = {
    id = "ItemRemove",
    name = "Perk_Module_Item_Remove"
}

M.rules = {}
M.slotAll = -1

-- ============================================================
-- 配置解析
-- ============================================================

local function ParseSlot(slotStr)
    if slotStr == "all" then return M.slotAll end
    return tonumber(slotStr:match("hand(%d)"))
end

local function ParseTag(tag)
    local slotStr, matchType, matchValue = tag:match(
        "^ItemRemove:([^:]+):([^:]+):(.+)$")
    if not slotStr then return nil end

    local values = {}
    for v in matchValue:gmatch("([^,]+)") do
        v = v:match("^%s*(.-)%s*$")
        if v ~= "" then values[v] = true end
    end

    return {
        slot = ParseSlot(slotStr),
        matchType = matchType,
        matchValues = values
    }
end

-- ============================================================
-- 匹配检查
-- ============================================================

local function MatchItem(item, rule)
    if not item then return false end

    if rule.matchType == "tag" then
        if not item.tags then return false end
        for i = 0, item.tags.Count - 1 do
            if rule.matchValues[item.tags[i]] then return true end
        end

    elseif rule.matchType == "name" then
        return rule.matchValues[item.name] or rule.matchValues[item.prefab]
    end

    return false
end

-- ============================================================
-- 事件回调
-- ============================================================

local function OnHandItemChange(handIndex, lastItem, currentItem)
    for _, rule in ipairs(M.rules) do
        if rule.slot == M.slotAll or rule.slot == handIndex then
            if MatchItem(currentItem, rule) then
                Inventory.RemoveHandItem(handIndex)
                print(string.format("[ItemRemove] hand%d removed", handIndex))
                return
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
        if rule then table.insert(M.rules, rule) end
    end

    Game.Events.On("OnHandItemChange", "ItemRemove_" .. perk.id, OnHandItemChange)
end

function M.OnDestroy(perk)
    Game.Events.Off("OnItemChange", "ItemRemove_" .. perk.id)
    M.rules = {}
end

return M