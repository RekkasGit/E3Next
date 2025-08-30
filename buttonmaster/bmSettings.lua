local mq            = require('mq')
local btnUtils      = require('lib.buttonUtils')

local settings_base = mq.configDir .. '/ButtonMaster'
local settings_path = settings_base .. '.lua '


local BMSettings                 = {}
BMSettings.__index               = BMSettings
BMSettings.settings              = {}
BMSettings.CharConfig            = string.format("%s_%s", mq.TLO.EverQuest.Server(), mq.TLO.Me.DisplayName())
BMSettings.Constants             = {}

BMSettings.Globals               = {}
BMSettings.Globals.Version       = 7
BMSettings.Globals.CustomThemes  = {}

BMSettings.Constants.TimerTypes  = {
    "Seconds Timer",
    "Item",
    "Spell Gem",
    "AA",
    "Ability",
    "Disc",
    "Custom Lua",
}

BMSettings.Constants.UpdateRates = {
    { Display = "Unlimited",     Value = 0, },
    { Display = "1 per second",  Value = 1, },
    { Display = "2 per second",  Value = 0.5, },
    { Display = "4 per second",  Value = 0.25, },
    { Display = "10 per second", Value = 0.1, },
    { Display = "20 per second", Value = 0.05, },
}


function BMSettings.new()
    local newSettings      = setmetatable({}, BMSettings)
    newSettings.CharConfig = string.format("%s_%s", mq.TLO.EverQuest.Server(), mq.TLO.Me.DisplayName())


    local config, err = loadfile(mq.configDir .. '/Button_Master_Theme.lua')
    if not err and config then
        BMSettings.Globals.CustomThemes = config()
    end

    return newSettings
end

function BMSettings:SaveSettings(doBroadcast)
    if doBroadcast == nil then doBroadcast = true end

    if not self.settings.LastBackup or os.time() - self.settings.LastBackup > 3600 * 24 then
        self.settings.LastBackup = os.time()
        mq.pickle(mq.configDir .. "/Buttonmaster-Backups/ButtonMaster-backup-" .. os.date("%m-%d-%y-%H-%M-%S") .. ".lua",
            self.settings)
    end

    mq.pickle(settings_path, self.settings)

    if doBroadcast and mq.TLO.MacroQuest.GameState() == "INGAME" then
        --btnUtils.Output("\aySent Event from(\am%s\ay) event(\at%s\ay)", mq.TLO.Me.DisplayName(), "SaveSettings")
        ButtonActors.send({
            from = mq.TLO.Me.DisplayName(),
            script = "ButtonMaster",
            event = "SaveSettings",
            newSettings =
                self.settings,
        })
    end
end

function BMSettings:NeedUpgrade()
    return (self.settings.Version or 0) < BMSettings.Globals.Version
end

function BMSettings:GetSettings()
    return self.settings
end

function BMSettings:GetSetting(settingKey)
    -- main setting
    if self.settings.Global[settingKey] ~= nil then return self.settings.Global[settingKey] end

    -- character sertting
    if self.settings.Characters[self.CharConfig] ~= nil and self.settings.Characters[self.CharConfig][settingKey] ~= nil then
        return self.settings.Characters[self.CharConfig]
            [settingKey]
    end

    -- not found.
    btnUtils.Debug("Setting not Found: %s", settingKey)
end

function BMSettings:GetCharacterWindow(windowId)
    return self.settings.Characters[self.CharConfig].Windows[windowId]
end

function BMSettings:GetCharacterWindowSets(windowId)
    if not self.settings.Characters or
        not self.settings.Characters[self.CharConfig] or
        not self.settings.Characters[self.CharConfig].Windows or
        not self.settings.Characters[self.CharConfig].Windows[windowId] or
        not self.settings.Characters[self.CharConfig].Windows[windowId].Sets then
        return {}
    end

    return self.settings.Characters[self.CharConfig].Windows[windowId].Sets
end

function BMSettings:GetCharConfig()
    return self.settings.Characters[self.CharConfig]
end

function BMSettings:GetButtonSectionKeyBySetIndex(Set, Index)
    -- somehow an invalid set exists. Just make it empty.
    if not self.settings.Sets[Set] then
        self.settings.Sets[Set] = {}
        btnUtils.Debug("Set: %s does not exist. Creating it.", Set)
    end

    local key = self.settings.Sets[Set][Index]

    -- if the key doesn't exist, get the current button counter and add 1
    if key == nil then
        key = self:GenerateButtonKey()
    end
    return key
end

function BMSettings:GetNextWindowId()
    return #self:GetCharConfig().Windows + 1
end

function BMSettings:GenerateButtonKey()
    local i = 1
    while (true) do
        local buttonKey = string.format("Button_%d", i)
        if self.settings.Buttons[buttonKey] == nil then
            return buttonKey
        end
        i = i + 1
    end
end

function BMSettings:ImportButtonAndSave(button, save)
    local key = self:GenerateButtonKey()
    self.settings.Buttons[key] = button
    if save then
        self:SaveSettings(true)
    end
    return key
end

---comment
---@param Set string
---@param Index number
---@return table
function BMSettings:GetButtonBySetIndex(Set, Index)
    if self.settings.Sets[Set] and self.settings.Sets[Set][Index] and self.settings.Buttons[self.settings.Sets[Set][Index]] then
        return self.settings.Buttons[self.settings.Sets[Set][Index]]
    end

    return { Unassigned = true, Label = tostring(Index), }
end

function BMSettings:ImportSetAndSave(sharableSet, windowId)
    -- is setname unqiue?
    local setName = sharableSet.Key
    if self.settings.Sets[setName] ~= nil then
        local newSetName = setName .. "_Imported_" .. os.date("%m-%d-%y-%H-%M-%S")
        btnUtils.Output("\ayImport Set Warning: Set name: \at%s\ay already exists renaming it to \at%s\ax", setName,
            newSetName)
        setName = newSetName
    end

    self.settings.Sets[setName] = {}
    for index, btnName in pairs(sharableSet.Set) do
        local newButtonName = self:ImportButtonAndSave(sharableSet.Buttons[btnName], false)
        self.settings.Sets[setName][index] = newButtonName
    end

    -- add set to user
    table.insert(self.settings.Characters[self.CharConfig].Windows[windowId].Sets, setName)

    self:SaveSettings(true)
end

function BMSettings:ConvertToLatestConfigVersion()
    self:LoadSettings()
    local needsSave = false
    local newSettings = {}

    if not self.settings.Version then
        -- version 2
        -- Run through all settings and make sure they are in the new format.
        for key, value in pairs(self.settings or {}) do
            -- TODO: Make buttons a seperate table instead of doing the string compare crap.
            if type(value) == 'table' then
                if key:find("^(Button_)") and value.Cmd1 or value.Cmd2 or value.Cmd3 or value.Cmd4 or value.Cmd5 then
                    btnUtils.Output("Key: %s Needs Converted!", key)
                    value.Cmd  = string.format("%s\n%s\n%s\n%s\n%s\n%s", value.Cmd or '', value.Cmd1 or '',
                        value.Cmd2 or '',
                        value.Cmd3 or '', value.Cmd4 or '', value.Cmd5 or '')
                    value.Cmd  = value.Cmd:gsub("\n+", "\n")
                    value.Cmd  = value.Cmd:gsub("\n$", "")
                    value.Cmd  = value.Cmd:gsub("^\n", "")
                    value.Cmd1 = nil
                    value.Cmd2 = nil
                    value.Cmd3 = nil
                    value.Cmd4 = nil
                    value.Cmd5 = nil
                    needsSave  = true
                    btnUtils.Output("\atUpgraded to \amv2\at!")
                end
            end
        end

        -- version 3
        -- Okay now that a similar but lua-based config is stabalized the next pass is going to be
        -- cleaning up the data model so we aren't doing a ton of string compares all over.
        newSettings.Buttons = {}
        newSettings.Sets = {}
        newSettings.Characters = {}
        newSettings.Global = self.settings.Global
        for key, value in pairs(self.settings) do
            local sStart, sEnd = key:find("^Button_")
            if sStart then
                local newKey = key --key:sub(sEnd + 1)
                btnUtils.Output("Old Key: \am%s\ax, New Key: \at%s\ax", key, newKey)
                newSettings.Buttons[newKey] = newSettings.Buttons[newKey] or {}
                if type(value) == 'table' then
                    for subKey, subValue in pairs(value) do
                        newSettings.Buttons[newKey][subKey] = tostring(subValue)
                    end
                end
                needsSave = true
            end
            sStart, sEnd = key:find("^Set_")
            if sStart then
                local newKey = key:sub(sEnd + 1)
                btnUtils.Output("Old Key: \am%s\ax, New Key: \at%s\ax", key, newKey)
                newSettings.Sets[newKey] = value
                needsSave                = true
            end
            sStart, sEnd = key:find("^Char_(.*)_Config")
            if sStart then
                local newKey = key:sub(sStart + 5, sEnd - 7)
                btnUtils.Output("Old Key: \am%s\ax, New Key: \at%s\ax", key, newKey)
                newSettings.Characters[newKey] = newSettings.Characters[newKey] or {}
                if type(value) == 'table' then
                    for subKey, subValue in pairs(value) do
                        newSettings.Characters[newKey].Sets = newSettings.Characters[newKey].Sets or {}
                        if type(subKey) == "number" then
                            table.insert(newSettings.Characters[newKey].Sets, subValue)
                        else
                            newSettings.Characters[newKey][subKey] = subValue
                        end
                    end
                end

                needsSave = true
            end
        end

        if needsSave then
            -- be nice and make a backup.
            mq.pickle(mq.configDir .. "/ButtonMaster-v3-" .. os.date("%m-%d-%y-%H-%M-%S") .. ".lua", self.settings)
            self.settings = newSettings
            self:SaveSettings(true)
            needsSave = false
            btnUtils.Output("\atUpgraded to \amv3\at!")
        end
    end

    -- version 4 same as 5 but moved the version data around
    -- version 5
    -- Move Character sets to a specific window name
    if (self.settings.Version or 0) < 5 then
        mq.pickle(mq.configDir .. "/ButtonMaster-v4-" .. os.date("%m-%d-%y-%H-%M-%S") .. ".lua", self.settings)

        needsSave = true
        newSettings = self.settings
        newSettings.Version = 5
        for charKey, _ in pairs(self.settings.Characters or {}) do
            if self.settings.Characters[charKey] and self.settings.Characters[charKey].Sets ~= nil then
                newSettings.Characters[charKey].Windows = {}
                table.insert(newSettings.Characters[charKey].Windows,
                    { Sets = newSettings.Characters[charKey].Sets, Visible = true, })
                newSettings.Characters[charKey].Sets = nil
                needsSave = true
            end
        end
        if needsSave then
            self.settings = newSettings
            self:SaveSettings(true)
            btnUtils.Output("\atUpgraded to \amv5\at!")
        end
    end

    -- version 6
    -- Moved TitleBar/Locked into the window settings
    -- Removed Button Count
    -- Removed Defaults for now
    if (self.settings.Version or 0) < 6 then
        mq.pickle(mq.configDir .. "/ButtonMaster-v5-" .. os.date("%m-%d-%y-%H-%M-%S") .. ".lua", self.settings)
        needsSave = true
        newSettings = self.settings
        newSettings.Version = 6
        newSettings.Defaults = nil

        for _, curCharData in pairs(newSettings.Characters or {}) do
            for _, windowData in ipairs(curCharData.Windows or {}) do
                windowData.Locked = curCharData.Locked or false
                windowData.HideTitleBar = curCharData.HideTitleBar or false
            end
            curCharData.HideTitleBar = nil
            curCharData.Locked = nil
        end

        newSettings.Global.ButtonCount = nil

        if needsSave then
            self.settings = newSettings
            self:SaveSettings(true)
            btnUtils.Output("\atUpgraded to \amv6\at!")
        end
    end

    -- version 7
    -- moved ButtonSize and Font to each hotbar
    if (self.settings.Version or 0) < 7 then
        mq.pickle(mq.configDir .. "/ButtonMaster-v6-" .. os.date("%m-%d-%y-%H-%M-%S") .. ".lua", self.settings)
        needsSave = true
        newSettings = self.settings
        newSettings.Version = 7

        for _, curCharData in pairs(newSettings.Characters or {}) do
            for _, windowData in ipairs(curCharData.Windows or {}) do
                windowData.Font = (newSettings.Global.Font or 1) * 10
                windowData.ButtonSize = newSettings.Global.ButtnSize or 6
            end
        end

        newSettings.Global.Font = nil
        newSettings.Global.ButtonSize = nil
        newSettings.Global = nil

        if needsSave then
            self.settings = newSettings
            self:SaveSettings(true)
            btnUtils.Output("\atUpgraded to \amv%d\at!", BMSettings.Globals.Version)
        end
    end
end

function BMSettings:InvalidateButtonCache()
    for _, button in pairs(self.settings.Buttons) do
        button.CachedLabel = nil
    end
end

function BMSettings:LoadSettings()
    local config, err = loadfile(settings_path)
    if err or not config then
        local old_settings_path = settings_path:gsub(".lua", ".ini")
        printf("\ayUnable to load global settings file(%s), creating a new one from legacy ini(%s) file!",
            settings_path, old_settings_path)
        if btnUtils.file_exists(old_settings_path) then
            self.settings = btnUtils.loadINI(old_settings_path)
            self:SaveSettings(true)
        else
            printf("\ayUnable to load legacy settings file(%s), creating a new config!", old_settings_path)
            self.settings = {
                Version = BMSettings.Globals.Version,
                Sets = {
                    ['Primary'] = { 'Button_1', 'Button_2', 'Button_3', },
                    ['Movement'] = { 'Button_4', },
                },
                Buttons = {
                    Button_1 = {
                        Label = 'Burn (all)',
                        Cmd = '/bcaa //burn\n/timed 500 /bcaa //burn',
                    },
                    Button_2 = {
                        Label = 'Pause (all)',
                        Cmd = '/bcaa //multi ; /twist off ; /mqp on',
                    },
                    Button_3 = {
                        Label = 'Unpause (all)',
                        Cmd = '/bcaa //mqp off',
                    },
                    Button_4 = {
                        Label = 'Nav Target (bca)',
                        Cmd = '/bca //nav id ${Target.ID}',
                    },
                },
                Characters = {
                    [self.CharConfig] = {
                        Windows = { [1] = { Visible = true, Pos = { x = 10, y = 10, }, Sets = {}, Locked = false, }, },
                    },
                },
            }
            self:SaveSettings(true)
        end
    else
        self.settings = config()
    end

    -- if we need to upgrade anyway then bail after the load.
    if self:NeedUpgrade() then return false end

    self.settings.Characters[self.CharConfig] = self.settings.Characters[self.CharConfig] or {}
    self.settings.Characters[self.CharConfig].Windows = self.settings.Characters[self.CharConfig].Windows or
        { [1] = { Visible = true, Pos = { x = 10, y = 10, }, Sets = {}, Locked = false, }, }

    self:InvalidateButtonCache()
    return true
end

return BMSettings
