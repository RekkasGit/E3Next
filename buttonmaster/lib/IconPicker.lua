---@type Mq
local mq = require('mq')
---@type ImGui
require('ImGui')

local animSpellIcons = mq.FindTextureAnimation('A_SpellIcons')
local animItems      = mq.FindTextureAnimation('A_DragItem')
local btnUtils       = require('lib.buttonUtils')

local IconPicker     = {}
IconPicker.__index   = IconPicker

function IconPicker.new()
    local newPicker = {
        Open = false,
        Draw = false,
        maxSpell = 2243,
        maxItem = 12599,
        iconsPerPage = 500,
        Page = 1,
    }
    return setmetatable(newPicker, IconPicker)
end

local IconSize = 40
function IconPicker:renderSpellIcon(id)
    local cursor_x, cursor_y = ImGui.GetCursorPos()
    -- icon
    animSpellIcons:SetTextureCell(id)
    ImGui.DrawTextureAnimation(animSpellIcons, IconSize, IconSize)
    ImGui.SetCursorPos(cursor_x, cursor_y)
    ImGui.PushID(tostring(id) .. "SpellButton")
    if ImGui.InvisibleButton(tostring(id), ImVec2(IconSize, IconSize)) then
        self.Selected = id
    end
    btnUtils.Tooltip(string.format("Icon ID: %d", id))
    ImGui.PopID()
end

function IconPicker:renderItemIcon(id)
    local cursor_x, cursor_y = ImGui.GetCursorPos()
    -- icon
    animItems:SetTextureCell(id)
    ImGui.DrawTextureAnimation(animItems, IconSize, IconSize)
    ImGui.SetCursorPos(cursor_x, cursor_y)
    ImGui.PushID(tostring(id) .. "ItemButton")
    if ImGui.InvisibleButton(tostring(id), ImVec2(IconSize, IconSize)) then
        self.Selected = id
    end
    btnUtils.Tooltip(string.format("Icon ID: %d", id + 500))
    ImGui.PopID()
end

function IconPicker:RenderTab(getterFn, maxIcon)
    local style = ImGui.GetStyle()
    local width = ImGui.GetWindowWidth()
    local cols = math.max(math.floor(width / (IconSize + style.ItemSpacing.x)), 1)
    local maxPage = math.ceil(maxIcon / self.iconsPerPage)
    if self.Page > maxPage then self.Page = maxPage end
    ImGui.BeginChild("Icon##Picker")
    if ImGui.BeginTable("SpellIcons", cols) then
        local startId = math.max(0, ((self.Page - 1) * self.iconsPerPage))
        local endId   = math.min(maxIcon, startId + self.iconsPerPage - 1)
        for iconId = startId, endId do
            ImGui.TableNextColumn()
            getterFn(self, iconId)
        end
        ImGui.EndTable()
        ImGui.EndChild()
    end
end

function IconPicker:RenderIconPicker()
    if not self.Open then return end
    self.Open, self.Draw = ImGui.Begin('Icon Picker', self.Open, ImGuiWindowFlags.None)
    if self.Draw then
        self.Page = ImGui.InputInt("Page", self.Page, 1)
        if self.Page < 1 then self.Page = 1 end

        if ImGui.BeginTabBar("IconTabs") then
            if ImGui.BeginTabItem("Spell Icons") then
                self.SelectedType = "Spell"
                self:RenderTab(self.renderSpellIcon, self.maxSpell)
                ImGui.EndTabItem()
            end
            if ImGui.BeginTabItem("Item Icons") then
                self.SelectedType = "Item"
                self:RenderTab(self.renderItemIcon, self.maxItem)
                ImGui.EndTabItem()
            end
        end
    end
    ImGui.End()
end

function IconPicker:SetOpen()
    self.Open, self.Draw = true, true
end

function IconPicker:SetClosed()
    self.Open, self.Draw = false, false
end

function IconPicker:ClearSelection()
    self.Selected = nil
    self.SelectedType = ""
    self.Page = 1
end

return IconPicker
