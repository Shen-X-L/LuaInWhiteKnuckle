local function DumpTable(tbl, prefix, visited)
    visited = visited or {}

    if visited[tbl] then
        return
    end
    visited[tbl] = true

    prefix = prefix or ""

    for k, v in pairs(tbl) do
        local name = prefix .. tostring(k)

        print(name, type(v))

        if type(v) == "table" then
            DumpTable(v, name .. ".", visited)
        end
    end
end

DumpTable(_G)