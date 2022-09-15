module SJ2021CoreModeTriggerNoFlash
using ..Ahorn, Maple

@mapdef Trigger "SJ2021/CoreModeTriggerNoFlash" CoreModeTriggerNoFlash(x::Integer, y::Integer, width::Integer=Maple.defaultTriggerWidth, height::Integer=Maple.defaultTriggerHeight, mode::String="None")

const placements = Ahorn.PlacementDict(
    "Core Mode Trigger (No Flash) (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        CoreModeTriggerNoFlash,
        "rectangle"
    )
)

function Ahorn.editingOptions(trigger::CoreModeTriggerNoFlash)
    return Dict{String, Any}(
        "mode" => sort(Maple.core_modes)
    )
end

end