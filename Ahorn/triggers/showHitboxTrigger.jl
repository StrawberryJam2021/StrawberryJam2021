module SJ2021ShowHitboxTrigger
using ..Ahorn, Maple

@mapdef Trigger "SJ2021/ShowHitboxTrigger" ShowHitboxTrigger(x::Integer, y::Integer, width::Integer=Maple.defaultTriggerWidth, height::Integer=Maple.defaultTriggerHeight, typeNames::String="")

const placements = Ahorn.PlacementDict(
    "Show Hitbox Trigger (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        ShowHitboxTrigger,
        "rectangle"
    )
)

end
