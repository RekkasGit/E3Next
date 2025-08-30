local mq                           = require('mq')
local btnUtils                     = require('lib.buttonUtils')
local BMButtonHandlers             = require('bmButtonHandlers')
local picker                       = require('lib.IconPicker').new()

local BMButtonEditor               = {}
BMButtonEditor.__index             = BMButtonEditor
BMButtonEditor.editButtonPopupOpen = false
BMButtonEditor.editButtonUseCursor = false
BMButtonEditor.editButtonAdvanced  = false
BMButtonEditor.editButtonSet       = ""
BMButtonEditor.editButtonIndex     = 0
BMButtonEditor.editButtonUIChanged = false

BMButtonEditor.tmpButton           = nil

BMButtonEditor.selectedTimerType   = 1
BMButtonEditor.selectedUpdateRate  = 1

function BMButtonEditor:RenderEditButtonPopup()
    if not self.editButtonPopupOpen then
        picker:SetClosed()
        return
    end

    local ButtonKey = BMSettings:GetButtonSectionKeyBySetIndex(self.editButtonSet, self.editButtonIndex)
    local shouldDrawEditPopup = false


    self.editButtonPopupOpen, shouldDrawEditPopup = ImGui.Begin("Edit Button", self.editButtonPopupOpen,
        self.editButtonUIChanged and ImGuiWindowFlags.UnsavedDocument or ImGuiWindowFlags.None)

    ImGui.PushID(string.format("##edit_button_pop"))

    if self.editButtonPopupOpen and shouldDrawEditPopup then
        -- shallow copy original button incase we want to reset (close)
        if self.editButtonUseCursor then
            self.editButtonUseCursor = false
            if mq.TLO.CursorAttachment and mq.TLO.CursorAttachment.Type() then
                local cursorIndex = mq.TLO.CursorAttachment.Index()
                local buttonText = mq.TLO.CursorAttachment.ButtonText():gsub("\n", " ")
                local attachmentType = mq.TLO.CursorAttachment.Type():lower()
                if attachmentType == "item" or attachmentType == "item_link" then
                    self.tmpButton.Label = mq.TLO.CursorAttachment.Item()
                    self.tmpButton.Cmd = string.format("/useitem \"%s\"", mq.TLO.CursorAttachment.Item())
                    self.tmpButton.Icon = tostring((mq.TLO.CursorAttachment.Item.Icon() or 500) - 500)
                    self.tmpButton.IconType = "Item"
                    self.tmpButton.Cooldown = mq.TLO.CursorAttachment.Item()
                    self.tmpButton.TimerType = "Item"
                elseif attachmentType == "spell_gem" then
                    local gem = mq.TLO.Me.Gem(mq.TLO.CursorAttachment.Spell.RankName() or "")() or 0
                    self.tmpButton.Label = mq.TLO.CursorAttachment.Spell.RankName()
                    self.tmpButton.Cmd = string.format("/cast %d", gem)
                    self.tmpButton.Icon = tostring(mq.TLO.CursorAttachment.Spell.SpellIcon())
                    self.tmpButton.IconType = "Spell"
                    self.tmpButton.Cooldown = gem
                    self.tmpButton.TimerType = "Spell Gem"
                elseif attachmentType == "skill" then
                    self.tmpButton.Label = buttonText
                    self.tmpButton.Cmd = string.format("/doability %s", buttonText)
                    self.tmpButton.Icon = nil
                    self.tmpButton.Cooldown = buttonText
                    self.tmpButton.TimerType = "Ability"
                elseif attachmentType == "melee_ability" then
                    self.tmpButton.Label = buttonText
                    self.tmpButton.Cmd = string.format("/disc %s", buttonText)
                    self.tmpButton.Icon = mq.TLO.Spell(buttonText).SpellIcon()
                    self.tmpButton.IconType = "Spell"
                    self.tmpButton.Cooldown = buttonText
                    self.tmpButton.TimerType = "Disc"
                elseif attachmentType == "social" then
                    self.tmpButton.Label = buttonText
                    if cursorIndex + 1 > 120 then
                        self.tmpButton.Cmd = string.format("/alt act %d", cursorIndex - 120)
                        self.tmpButton.Icon = nil
                        self.tmpButton.Cooldown = buttonText
                        self.tmpButton.TimerType = "AA"
                    else
                        if mq.TLO.Social then
                            self.tmpButton.Cmd = ""
                            for i = 0, 4 do
                                local cmd = mq.TLO.Social(cursorIndex + 1).Cmd(i)() or ""
                                if cmd:len() > 0 then
                                    self.tmpButton.Cmd = string.format("%s%s%s", self.tmpButton.Cmd,
                                        self.tmpButton.Cmd:len() > 0 and "\n" or "", cmd)
                                end
                            end
                        end
                    end
                end

                for index, type in ipairs(BMSettings.Constants.TimerTypes) do
                    if type == self.tmpButton.TimerType then
                        self.selectedTimerType = index
                        break
                    end
                end
            end
        end

        self:RenderButtonEditUI(self.tmpButton, true, true)

        -- save button
        if ImGui.Button("Save") or (ImGui.IsWindowHovered(ImGuiHoveredFlags.ChildWindows) and (ImGui.IsKeyPressed(ImGuiMod.Ctrl) and ImGui.IsKeyPressed(ImGuiKey.S))) then
            -- make sure the button label isn't nil/empty/spaces
            if self.tmpButton.Label ~= nil and self.tmpButton.Label:gsub("%s+", ""):len() > 0 then
                BMSettings:GetSettings().Sets[self.editButtonSet][self.editButtonIndex] =
                    ButtonKey                                                                      -- add the button key for this button set index
                BMSettings:GetSettings().Buttons[ButtonKey] = btnUtils.shallowcopy(self.tmpButton) -- store the tmp button into the settings table
                BMSettings:GetSettings().Buttons[ButtonKey].Unassigned = nil                       -- clear the unassigned flag

                BMSettings:SaveSettings(true)
                self.editButtonUIChanged = false
            else
                btnUtils.Output("\arSave failed.  Button Label cannot be empty.")
            end
        end

        ImGui.SameLine()

        -- close button
        local closeClick = ImGui.Button("Close")
        if ImGui.IsItemHovered() then
            ImGui.BeginTooltip()
            ImGui.Text("Close edit dialog without saving")
            ImGui.EndTooltip()
        end
        if closeClick then
            picker:SetClosed()
            self:CloseEditPopup()
        end
    end

    if ImGui.IsWindowHovered(ImGuiHoveredFlags.ChildWindows) then
        if ImGui.IsKeyPressed(ImGuiMod.Ctrl) and ImGui.IsKeyPressed(ImGuiKey.S) then
            if self.tmpButton.Label ~= nil and self.tmpButton.Label:gsub("%s+", ""):len() > 0 then
                BMSettings:GetSettings().Sets[self.editButtonSet][self.editButtonIndex] =
                    ButtonKey                                                                      -- add the button key for this button set index
                BMSettings:GetSettings().Buttons[ButtonKey] = btnUtils.shallowcopy(self.tmpButton) -- store the tmp button into the settings table
                BMSettings:GetSettings().Buttons[ButtonKey].Unassigned = nil                       -- clear the unassigned flag

                BMSettings:SaveSettings(true)
                self.editButtonUIChanged = false
            else
                btnUtils.Output("\arSave failed.  Button Label cannot be empty.")
            end
        end
    end
    ImGui.PopID()
    ImGui.End()
end

function BMButtonEditor:CloseEditPopup()
    picker:SetClosed()
    self.editButtonPopupOpen = false
    self.editButtonIndex = 0
    self.editButtonSet = ""
end

function BMButtonEditor:OpenEditPopup(Set, Index)
    self.editButtonPopupOpen = true
    self.editButtonIndex = Index
    self.editButtonSet = Set
    self.selectedTimerType = 1
    self.selectedUpdateRate = 1
    local button = BMSettings:GetButtonBySetIndex(Set, Index)
    self.tmpButton = btnUtils.shallowcopy(button)

    if not button.Unassigned and button.TimerType and button.TimerType:len() > 0 then
        for index, type in ipairs(BMSettings.Constants.TimerTypes) do
            if type == button.TimerType then
                self.selectedTimerType = index
                break
            end
        end
        for index, type in ipairs(BMSettings.Constants.UpdateRates) do
            if type.Value == button.UpdateRate then
                self.selectedUpdateRate = index
                break
            end
        end
    end
end

function BMButtonEditor:CreateButtonFromCursor(Set, Index)
    self.editButtonUseCursor = true
    self:OpenEditPopup(Set, Index)
end

function BMButtonEditor:RenderButtonEditUI(renderButton, enableShare, enableEdit)
    -- Share Buttton
    if enableShare then
        if ImGui.Button(Icons.MD_SHARE) then
            BMButtonHandlers.ExportButtonToClipBoard(renderButton)
        end
        btnUtils.Tooltip("Copy contents of this button to share with friends.")
        ImGui.SameLine()
    end

    picker:RenderIconPicker()

    local colorChanged = false
    -- color pickers
    colorChanged = btnUtils.RenderColorPicker(string.format("##ButtonColorPicker1_%s", renderButton.Label), 'Button',
        renderButton,
        'ButtonColorRGB')
    self.editButtonUIChanged = self.editButtonUIChanged or colorChanged

    ImGui.SameLine()
    colorChanged = btnUtils.RenderColorPicker(string.format("##TextColorPicker1_%s", renderButton.Label), 'Text',
        renderButton, 'TextColorRGB')
    self.editButtonUIChanged = self.editButtonUIChanged or colorChanged

    ImGui.SameLine()
    self:RenderIconPicker(renderButton)

    ImGui.SameLine()
    ImGui.Text("Icon")

    if picker.Selected then
        self.editButtonUIChanged = true
        renderButton.Icon = picker.Selected
        renderButton.IconType = picker.SelectedType
        picker:ClearSelection()
    end
    -- default to show label.
    if renderButton.ShowLabel == nil then renderButton.ShowLabel = true end

    if renderButton.Icon ~= nil then
        ImGui.SameLine()
        renderButton.ShowLabel = ImGui.Checkbox("Show Button Label", renderButton.ShowLabel)
    end

    ImGui.SameLine()

    -- reset
    ImGui.SameLine()
    if ImGui.Button("Reset All") then
        renderButton.ButtonColorRGB = nil
        renderButton.TextColorRGB   = nil
        renderButton.Icon           = nil
        renderButton.IconType       = nil
        renderButton.Timer          = nil
        renderButton.Cooldown       = nil
        renderButton.ToggleCheck    = nil
        renderButton.ShowLabel      = nil
        renderButton.EvaluateLabel  = nil
        self.editButtonUIChanged    = true
    end

    ImGui.SameLine()
    self.editButtonAdvanced, _ = btnUtils.RenderOptionToggle(string.format("advanced_toggle_%s", renderButton.Label),
        "Show Advanced", self.editButtonAdvanced)

    local textChanged
    renderButton.Label, textChanged = ImGui.InputText('Button Label', renderButton.Label or '')
    self.editButtonUIChanged = self.editButtonUIChanged or textChanged

    if self.editButtonAdvanced then
        ImGui.SameLine()
        renderButton.EvaluateLabel, _ = ImGui.Checkbox("Evaluate Label", renderButton.EvaluateLabel or false)
        btnUtils.Tooltip("Treat the Label as a Lua function and evaluate it.")

        renderButton.IconLua, textChanged = ImGui.InputText('Icon Lua', renderButton.IconLua or '')
        btnUtils.Tooltip(
            "Dynamically override the IconID with this Lua function. \nNote: This MUST return number, string : IconId, IconType")

        self.selectedUpdateRate, _ = ImGui.Combo("Update Rate", self.selectedUpdateRate,
            function(idx) return BMSettings.Constants.UpdateRates[idx].Display end,
            #BMSettings.Constants.UpdateRates)
        renderButton.UpdateRate = BMSettings.Constants.UpdateRates[self.selectedUpdateRate].Value
        self.editButtonUIChanged = self.editButtonUIChanged or textChanged
    end

    ImGui.Separator()
    self:RenderTimerPanel(renderButton)

    ImGui.Separator()

    ImGui.Text("Commands:")
    local yPos = ImGui.GetCursorPosY()
    local footerHeight = 35
    local editHeight = ImGui.GetWindowHeight() - yPos - footerHeight
    ImGui.PushFont(ImGui.ConsoleFont)
    renderButton.Cmd, textChanged = ImGui.InputTextMultiline("##_Cmd_Edit", renderButton.Cmd or "",
        ImVec2(ImGui.GetWindowWidth() * 0.98, editHeight), ImGuiInputTextFlags.AllowTabInput)
    ImGui.PopFont()
    self.editButtonUIChanged = self.editButtonUIChanged or textChanged
end

function BMButtonEditor:RenderTimerPanel(renderButton)
    self.selectedTimerType, _ = ImGui.Combo("Timer Type", self.selectedTimerType, BMSettings.Constants.TimerTypes)

    renderButton.TimerType = BMSettings.Constants.TimerTypes[self.selectedTimerType]

    if BMSettings.Constants.TimerTypes[self.selectedTimerType] == "Custom Lua" then
        renderButton.Timer = ImGui.InputText("Custom Timer Lua", renderButton.Timer)
        btnUtils.Tooltip(
            "Lua expression that describes how much longer is left until this button is usable.\ni.e. 'return mq.TLO.Item(\"Potion of Clarity IV\").TimerReady()'")
        renderButton.Cooldown = ImGui.InputText("Custom Cooldown Lua", tostring(renderButton.Cooldown))
        btnUtils.Tooltip(
            "Lua expression that describes how long the timer is in total.\ni.e. 'return mq.TLO.Item(\"Potion of Clarity IV\").Clicky.TimerID()'")
        renderButton.ToggleCheck = ImGui.InputText("Custom Toggle Check Lua",
            renderButton.ToggleCheck and tostring(renderButton.ToggleCheck) or "")
        btnUtils.Tooltip(
            "Lua expression that must result in a bool: true if the button is locked and false if it is unlocked.")
    elseif BMSettings.Constants.TimerTypes[self.selectedTimerType] == "Seconds Timer" then
        renderButton.Cooldown, _ = btnUtils.RenderOptionNumber("##cooldown", "Manual Cooldown",
            tonumber(renderButton.Cooldown) or 0, 0, 3600, 1)
        btnUtils.Tooltip("Amount of time in seconds to display the cooldown overlay.")
    elseif BMSettings.Constants.TimerTypes[self.selectedTimerType] == "Item" then
        renderButton.Cooldown = ImGui.InputText("Item Name", tostring(renderButton.Cooldown))
        btnUtils.Tooltip("Name of the item that you want to track the cooldown of.")
    elseif BMSettings.Constants.TimerTypes[self.selectedTimerType] == "Spell Gem" then
        renderButton.Cooldown = ImGui.InputInt("Spell Gem", tonumber(renderButton.Cooldown) or 1, 1)
        if renderButton.Cooldown < 1 then renderButton.Cooldown = 1 end
        if renderButton.Cooldown > mq.TLO.Me.NumGems() then renderButton.Cooldown = mq.TLO.Me.NumGems() end
        btnUtils.Tooltip("Spell Gem Number that you want to track the cooldown of.")
    elseif BMSettings.Constants.TimerTypes[self.selectedTimerType] == "AA" then
        renderButton.Cooldown = ImGui.InputText("Alt Ability Name or ID", tostring(renderButton.Cooldown))
        btnUtils.Tooltip("Name or ID of the AA that you want to track the cooldown of.")
    elseif BMSettings.Constants.TimerTypes[self.selectedTimerType] == "Ability" then
        renderButton.Cooldown = ImGui.InputText("Ability Name", tostring(renderButton.Cooldown))
        btnUtils.Tooltip("Name of the Ability that you want to track the cooldown of.")
    elseif BMSettings.Constants.TimerTypes[self.selectedTimerType] == "Disc" then
        renderButton.Cooldown = ImGui.InputText("Disc Name", tostring(renderButton.Cooldown))
        btnUtils.Tooltip("Name of the Disc that you want to track the cooldown of.")
    end
end

function BMButtonEditor:RenderIconPicker(renderButton)
    if renderButton.Icon then
        local objectID = string.format("##IconPicker_%s_%d", self.editButtonSet, self.editButtonIndex)
        ImGui.PushID(objectID)
        if BMButtonHandlers.Render(renderButton, 20, false, 1, false) then
            picker:SetOpen()
        end
        ImGui.PopID()
        if ImGui.BeginPopupContextItem(objectID) then
            if ImGui.MenuItem("Clear Icon") then
                renderButton.Icon = nil
                BMSettings:SaveSettings(true)
            end
            ImGui.EndPopup()
        end
    else
        if ImGui.Button('', ImVec2(20, 20)) then
            picker:SetOpen()
        end
    end
end

return BMButtonEditor
