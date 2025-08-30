local mq                = require('mq')
local base64            = require('lib.base64')

local ButtonUtils       = {}
ButtonUtils.__index     = ButtonUtils
ButtonUtils.enableDebug = false

function ButtonUtils.PCallString(str)
    local func, err = load(str)
    if not func then
        ButtonUtils.Output(err)
        return false, err
    end

    return pcall(func)
end

function ButtonUtils.EvaluateLua(str)
    local runEnv = [[mq = require('mq')
        %s
        ]]
    return ButtonUtils.PCallString(string.format(runEnv, str))
end

function ButtonUtils.serializeTable(val, name, skipnewlines, depth)
    skipnewlines = skipnewlines or false
    depth = depth or 0

    local tmp = string.rep(" ", depth)

    if name then
        if type(name) ~= 'number' then name = '"' .. name .. '"' end
        tmp = tmp .. '[' .. name .. '] = '
    end

    if type(val) == "table" then
        tmp = tmp .. "{" .. (not skipnewlines and "\n" or "")

        for k, v in pairs(val) do
            tmp = tmp ..
                ButtonUtils.serializeTable(v, k, skipnewlines, depth + 1) .. "," .. (not skipnewlines and "\n" or "")
        end

        tmp = tmp .. string.rep(" ", depth) .. "}"
    elseif type(val) == "number" then
        tmp = tmp .. tostring(val)
    elseif type(val) == "string" then
        tmp = tmp .. string.format("%q", val)
    elseif type(val) == "boolean" then
        tmp = tmp .. (val and "true" or "false")
    else
        tmp = tmp .. "\"[inserializeable datatype:" .. type(val) .. "]\""
    end

    return tmp
end

function ButtonUtils.encodeTable(tbl)
    return base64.enc('return ' .. ButtonUtils.serializeTable(tbl))
end

function ButtonUtils.decodeTable(encString)
    local decodedStr = base64.dec(encString)
    local loadedFn, err = load(decodedStr)
    if not loadedFn then
        printf('\arERROR: Failed to import object [load failed]: %s!\ax', err)
        return false, nil
    end
    local success, decodedTable = pcall(loadedFn)
    if not success or not type(decodedTable) == 'table' then
        printf('\arERROR: Failed to import object! [pcall failed]: %s\ax', decodedTable or "Unknown")
        return false, nil
    end
    return true, decodedTable
end

function ButtonUtils.tableContains(t, v)
    if not t then return false end
    for _, tv in pairs(t) do
        if tv == v then return true end
    end
    return false
end

function ButtonUtils.dumpTable(o, depth)
    if not depth then depth = 0 end
    if type(o) == 'table' then
        local s = '{ \n'
        for k, v in pairs(o) do
            if type(k) ~= 'number' then k = '"' .. k .. '"' end
            s = s .. string.rep(" ", depth) .. '\t[' .. k .. '] = ' .. ButtonUtils.dumpTable(v, depth + 1) .. ',\n'
        end
        return s .. string.rep(" ", depth) .. '}'
    else
        return tostring(o)
    end
end

function ButtonUtils.getTableSize(tbl)
    local cnt = 0
    if tbl ~= nil then
        for k, v in pairs(tbl) do cnt = cnt + 1 end
    end
    return cnt
end

---@param time integer # in seconds
---@return string
function ButtonUtils.FormatTime(time)
    local days = math.floor(time / 86400)
    local hours = math.floor((time % 86400) / 3600)
    local minutes = math.floor((time % 3600) / 60)
    local seconds = math.floor((time % 60))
    return string.format("%d:%02d:%02d:%02d", days, hours, minutes, seconds)
end

---@param id string
---@param text string
---@param on boolean
---@return boolean: state
---@return boolean: changed
function ButtonUtils.RenderOptionToggle(id, text, on)
    local toggled = false
    local state   = on
    ImGui.PushID(id .. "_togg_btn")

    ImGui.PushStyleColor(ImGuiCol.ButtonActive, 1.0, 1.0, 1.0, 0)
    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 1.0, 1.0, 1.0, 0)
    ImGui.PushStyleColor(ImGuiCol.Button, 1.0, 1.0, 1.0, 0)

    if on then
        ImGui.PushStyleColor(ImGuiCol.Text, 0.3, 1.0, 0.3, 0.9)
        if ImGui.Button(Icons.FA_TOGGLE_ON) then
            toggled = true
            state   = false
        end
    else
        ImGui.PushStyleColor(ImGuiCol.Text, 1.0, 0.3, 0.3, 0.8)
        if ImGui.Button(Icons.FA_TOGGLE_OFF) then
            toggled = true
            state   = true
        end
    end
    ImGui.PopStyleColor(4)
    ImGui.PopID()
    ImGui.SameLine()
    ImGui.Text(text)

    return state, toggled
end

function ButtonUtils.RenderOptionNumber(id, text, cur, min, max, step)
    ImGui.PushID("##num_spin_" .. id)
    ImGui.PushItemWidth(100)
    local input, changed = ImGui.InputInt(text, cur, step, step * 10)
    ImGui.PopItemWidth()
    ImGui.PopID()

    if input > max then input = max end
    if input < min then input = min end

    changed = cur ~= input
    return input, changed
end

function ButtonUtils.RenderColorPicker(id, buttonTypeName, renderButton, key)
    local btnColor = {}
    local changed = false

    if renderButton[key] ~= nil then
        local tColors = ButtonUtils.split(renderButton[key], ",")
        for i, v in ipairs(tColors) do btnColor[i] = tonumber(v / 255) end
    else
        btnColor[1] = 0
        btnColor[2] = 0
        btnColor[3] = 0
    end

    ImGui.PushID(id)
    local col, used = ImGui.ColorEdit3(string.format("%s Color", buttonTypeName), btnColor, ImGuiColorEditFlags.NoInputs)
    if used then
        changed = true
        btnColor = ButtonUtils.shallowcopy(col)
        renderButton[key] = string.format("%d,%d,%d", math.floor(col[1] * 255),
            math.floor(col[2] * 255), math.floor(col[3] * 255))
    end

    if ImGui.BeginPopupContextItem(id) then
        if ImGui.MenuItem(string.format("Clear %s Color", buttonTypeName)) then
            renderButton[key] = nil
            BMSettings:SaveSettings(true)
        end
        ImGui.EndPopup()
    end
    ImGui.PopID()

    return changed
end

function ButtonUtils.gsplit(text, pattern, plain)
    local splitStart, length = 1, #text
    return function()
        if splitStart > 0 then
            local sepStart, sepEnd = string.find(text, pattern, splitStart, plain)
            local ret
            if not sepStart then
                ret = string.sub(text, splitStart)
                splitStart = 0
            elseif sepEnd < sepStart then
                -- Empty separator!
                ret = string.sub(text, splitStart, sepStart)
                if sepStart < length then
                    splitStart = sepStart + 1
                else
                    splitStart = 0
                end
            else
                ret = sepStart > splitStart and string.sub(text, splitStart, sepStart - 1) or ''
                splitStart = sepEnd + 1
            end
            return ret
        end
    end
end

function ButtonUtils.split(text, pattern, plain)
    local ret = {}
    if text ~= nil then
        for match in ButtonUtils.gsplit(text, pattern, plain) do
            table.insert(ret, match)
        end
    end
    return ret
end

function ButtonUtils.Tooltip(desc)
    if ImGui.IsItemHovered() then
        ImGui.BeginTooltip()
        ImGui.PushTextWrapPos(ImGui.GetFontSize() * 25.0)
        ImGui.Text(desc)
        ImGui.PopTextWrapPos()
        ImGui.EndTooltip()
    end
end

--- Returns a table containing all the data from the INI file.
--@param fileName The name of the INI file to parse. [string]
--@return The table containing all data from the INI file. [table]
function ButtonUtils.loadINI(fileName)
    assert(type(fileName) == 'string', 'Parameter "fileName" must be a string.');
    local file = assert(io.open(fileName, 'r'), 'Error loading file : ' .. fileName);
    local data = {};
    local section;
    for line in file:lines() do
        local tempSection = line:match('^%[([^%[%]]+)%]$');
        if (tempSection) then
            section = tonumber(tempSection) and tonumber(tempSection) or tempSection;
            data[section] = data[section] or {};
        end
        local param, value = line:match("^([%w|_'.%s-]+)=%s-(.+)$");
        if (param and value ~= nil) then
            if (tonumber(value)) then
                value = tonumber(value);
            elseif (value == 'true') then
                value = true;
            elseif (value == 'false') then
                value = false;
            end
            if (tonumber(param)) then
                param = tonumber(param);
            end
            if param then
                data[section][param] = value;
            end
        end
    end
    file:close();
    return data;
end

function ButtonUtils.file_exists(path)
    local f = io.open(path, "r")
    if f ~= nil then
        io.close(f)
        return true
    else
        return false
    end
end

function ButtonUtils.shallowcopy(orig)
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

function ButtonUtils.deepcopy(obj, seen)
    if type(obj) ~= 'table' then return obj end
    if seen and seen[obj] then return seen[obj] end
    local s = seen or {}
    local res = setmetatable({}, getmetatable(obj))
    s[obj] = res
    for k, v in pairs(obj) do res[ButtonUtils.deepcopy(k, s)] = ButtonUtils.deepcopy(v, s) end
    return res
end

local function getCallStack()
    local info = debug.getinfo(4, "Snl")

    local callerTracer = string.format("\ao%s\aw::\ao%s()\aw:\ao%-04d\ax",
        info and info.short_src and info.short_src:match("[^\\^/]*.lua$") or "unknown_file",
        info and info.name or "unknown_func", info and info.currentline or 0)

    return callerTracer
end

function ButtonUtils.Output(msg, ...)
    local callerTracer = getCallStack()

    local formatted = msg
    if ... then
        formatted = string.format(msg, ...)
    end

    printf('\aw[' .. mq.TLO.Time() .. '] <%s> [\aoButton Master\aw] ::\a-t %s', callerTracer, formatted)
end

function ButtonUtils.Debug(msg, ...)
    if not ButtonUtils.enableDebug then return end
    ButtonUtils.Output('\ay<\atDEBUG\ay>\aw ' .. msg, ...)
end

return ButtonUtils
