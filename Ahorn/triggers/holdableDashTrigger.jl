module SJ2021HoldableDashTrigger

using ..Ahorn, Maple

@mapdef Trigger "SJ2021/HoldableDashTrigger" HoldableDashTrigger(x::Integer, y::Integer, width::Integer=16, height::Integer=16,
    mode::String="EnableOnStay")

const modes = String["EnableOnStay", "DisableOnStay", "EnableToggle", "DisableToggle"]

const placements = Ahorn.PlacementDict(
    "Dash With Holdable Trigger (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        HoldableDashTrigger,
        "rectangle"
    )
)

Ahorn.editingOptions(entity::HoldableDashTrigger) = Dict{String, Any}(
    "mode" => modes
)

end