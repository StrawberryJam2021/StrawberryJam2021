module SJ2021SpriteSwapTrigger
using ..Ahorn, Maple

@mapdef Trigger "SJ2021/SpriteSwapTrigger" SpriteSwapTrigger(x::Integer, y::Integer, width::Integer=Maple.defaultTriggerWidth, height::Integer=Maple.defaultTriggerHeight, fromIds::String="", toIds::String="")

const placements = Ahorn.PlacementDict(
    "Sprite Swap Trigger (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        SpriteSwapTrigger,
        "rectangle"
    )
)

end