module SJ2021PocketUmbrellaTrigger

using ..Ahorn, Maple

@mapdef Trigger "SJ2021/PocketUmbrellaTrigger" PocketUmbrellaTrigger(x::Integer, y::Integer, width::Integer=Maple.defaultTriggerWidth,
height::Integer=Maple.defaultTriggerHeight, enabled::Bool=true, revertOnLeave::Bool=false, staminaCost::Number=45.454544, cooldown::Number=0.2)

const placements = Ahorn.PlacementDict(
    "Pocket Umbrella Trigger (SJ2021)" => Ahorn.EntityPlacement(
        PocketUmbrellaTrigger,
        "rectangle"
    )
)

end