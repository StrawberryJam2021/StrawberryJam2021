module SJ2021PocketJellyTrigger

using ..Ahorn, Maple

@mapdef Trigger "SJ2021/PocketJellyTrigger" PocketJellyTrigger(x::Integer, y::Integer, width::Integer=Maple.defaultTriggerWidth,
height::Integer=Maple.defaultTriggerHeight, enabled::Bool=true)

const placements = Ahorn.PlacementDict(
    "PocketJellyTrigger (SJ2021)" => Ahorn.EntityPlacement(
        PocketJellyTrigger,
        "rectangle"
    )
)

end