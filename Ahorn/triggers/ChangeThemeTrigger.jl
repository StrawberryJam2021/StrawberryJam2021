module SJ2021ChangeThemeTrigger

using ..Ahorn, Maple

@mapdef Trigger "SJ2021/ChangeThemeTrigger" ChangeThemeTrigger(x::Integer, y::Integer,
    width::Integer=Maple.defaultTriggerWidth, height::Integer=Maple.defaultTriggerHeight, enable::Bool=false, toggle::Bool=false)

const placements = Ahorn.PlacementDict(
    "Change Theme Trigger (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        ChangeThemeTrigger,
        "rectangle",
    ),
)

end