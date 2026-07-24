-- ============================================================
-- ItemInfinite 模块
-- 支持从 perk.tags 读取 ItemInfinite:handN:ItemID 配置
-- 物品不存在时跳过该手配置
-- ============================================================

local M = {
    id = "ItemInfinite",
    name = "Perk_Module_Item_Infinite"
}

-- 运行时状态
-- [handIndex] = itemId, nil 表示该手未启用
M.items = {}

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

--- 从 tags 解析 ItemInfinite:handN:ItemID 配置
-- @param tags C# List<string>
-- @return 表: { [0] = "Item_Hammer", [1] = "Item_Piton" }
local function ParseItems(tags)
    print("A")
    local items = {}

    for i = 0, tags.Count - 1 do
            print("B")
        local tag = tags[i]
        local handIndex, itemId = tag:match("^ItemInfinite:hand(%d+):(.+)$")
            print("C")
        if handIndex then
            handIndex = tonumber(handIndex)
            print("D")
            -- 检查物品是否存在
            if IsItemExist(itemId) and handIndex then
                            print("E")
                items[handIndex] = itemId
            end
        end
    end

    return items
end

-- ============================================================
-- 生命周期
-- ============================================================

function M.Initialize(perk, firstTime)
    -- 解析配置,自动过滤不存在的物品
    M.items = ParseItems(perk.tags)

    if firstTime then
        -- 大口袋配置
        Inventory.PocketPouch(0).allowNonPouchable = true
        Inventory.PocketPouch(1).allowNonPouchable = true

        -- 容量 Buff
        local newBuff = BuffContainerData("addBigCapacity", "addBigCapacity")
        newBuff.loseOverTime = false
        newBuff.AddBuff("addPocketBigItemCapacity", 1)
        Player.AddBuffContainer(newBuff)

        -- 初始化物品
        for handIndex = 0, 1 do
            local itemId = M.items[handIndex]
            if itemId and Inventory.GetHandItem(handIndex) == nil then
                Inventory.AddHandItem(itemId, handIndex)
            end
        end
    end

    -- 注册事件
    Game.Events.On("OnHandItemChange", "ItemInfinite_" .. perk.id,
        function(handIndex, lastItem, currentItem)
            M.OnHandItemChange(handIndex, lastItem, currentItem)
        end)
end

-- ============================================================
-- 手部物品变化处理
-- ============================================================

function M.OnHandItemChange(handIndex, lastItem, currentItem)
    -- 该手未启用,忽略
    local itemId = M.items[handIndex]
    if not itemId then return end

    -- 手空了,补充
    if currentItem == nil then
        local pocket = Inventory.GetPocketItem(handIndex)
        if pocket == nil or pocket[0] == nil then
            Inventory.AddHandItem(itemId, handIndex)
        end
    end
end

-- ============================================================
-- 销毁
-- ============================================================

function M.OnDestroy(perk)
    Game.Events.Off("OnHandItemChange", "ItemInfinite_" .. perk.id)
    M.items = {}
end

-- ============================================================
-- 计数器
-- ============================================================

function M.GetCounterString()
    local parts = {}
    for i = 0, 1 do
        local item = M.items[i]
        if item then
            local shortName = item:match("Item_(.+)") or item
            table.insert(parts, string.format("H%d:%s", i, shortName))
        else
            table.insert(parts, string.format("H%d:-", i))
        end
    end
    return table.concat(parts, " ")
end

M.counter_tick = 100

-- ============================================================
-- 描述支持
-- ============================================================

function M.GetDescriptionFromKey(key)
    if key == "hand0" then
        local item = M.items[0]
        return item and (item:match("Item_(.+)") or item) or "None"
    end
    if key == "hand1" then
        local item = M.items[1]
        return item and (item:match("Item_(.+)") or item) or "None"
    end
    return ""
end

M.description_tick = 100

return M