local utils = {}

function shallowcopy(orig)
    local orig_type = type(orig)
    local copy
    if orig_type == 'table' then
        copy = {}
        for orig_key, orig_value in pairs(orig) do
            copy[orig_key] = orig_value
        end
    else -- number, string, boolean, etc
        copy = orig
    end
    return copy
end

function file_exists(path)
    local f = io.open(path, "r")
    if f ~= nil then io.close(f) return true else return false end
end

function __genOrderedIndex( t )
    local orderedIndex = {}
    for key in pairs(t) do
        table.insert( orderedIndex, key )
    end
    table.sort( orderedIndex )
    return orderedIndex
end

function orderedNext(t, state)
    -- Equivalent of the next function, but returns the keys in the alphabetic
    -- order. We use a temporary ordered key table that is stored in the
    -- table being iterated.

    local key = nil
    --print("orderedNext: state = "..tostring(state) )
    if state == nil then
        -- the first time, generate the index
        t.__orderedIndex = __genOrderedIndex( t )
        key = t.__orderedIndex[1]
    else
        -- fetch the next value
        for i = 1,table.getn(t.__orderedIndex) do
            if t.__orderedIndex[i] == state then
                key = t.__orderedIndex[i+1]
            end
        end
    end

    if key then
        return key, t[key]
    end

    -- no more value to return, cleanup
    t.__orderedIndex = nil
    return
end

function orderedPairs(t)
    -- Equivalent of the pairs() function on tables. Allows to iterate
    -- in order
    return orderedNext, t, nil
end

-- Print contents of `tbl`, with indentation.
-- `indent` sets the initial level of indentation.
function tprint (tbl, indent)
  if tbl ~= nil then
    if not indent then indent = 0 end
    for k, v in orderedPairs(tbl) do
      formatting = string.rep("  ", indent) .. k .. ": "
      if type(v) == "table" then
        print(formatting)
        tprint(v, indent+1)
      else
        print(formatting .. tostring(v))
      end
    end
  end
end

function getTableSize(tbl)
    local cnt = 0
    if tbl ~= nil then
        for k, v in pairs(tbl) do cnt = cnt + 1 end
    end
    return cnt
end

function sortTable(tbl)
    local t = {}
    local keys = {}
    -- get and sort table keys
    for k in pairs(tbl) do table.insert(keys, k) end
    table.sort(keys)
    -- rebuild tbl by key order and return it
    for _, k in ipairs(keys) do t[k] = tbl[k] end
    return t
end

function gsplit(text, pattern, plain)
    local splitStart, length = 1, #text
    return function ()
      if splitStart then
        local sepStart, sepEnd = string.find(text, pattern, splitStart, plain)
        local ret
        if not sepStart then
          ret = string.sub(text, splitStart)
          splitStart = nil
        elseif sepEnd < sepStart then
          -- Empty separator!
          ret = string.sub(text, splitStart, sepStart)
          if sepStart < length then
            splitStart = sepStart + 1
          else
            splitStart = nil
          end
        else
          ret = sepStart > splitStart and string.sub(text, splitStart, sepStart - 1) or ''
          splitStart = sepEnd + 1
        end
        return ret
      end
    end
end

function split(text, pattern, plain)
    local ret = {}
    if text ~= nil then
      for match in gsplit(text, pattern, plain) do
        table.insert(ret, match)
      end
    end
    return ret
end

return utils