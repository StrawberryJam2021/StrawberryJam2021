module SJ2021RequireDashlessTrigger
using ..Ahorn, Maple

@mapdef Trigger "SJ2021/RequireDashlessTrigger" RequireDashlessTrigger(x::Integer, y::Integer, width::Integer=Maple.defaultTriggerWidth, height::Integer=Maple.defaultTriggerHeight, entityNames::String="")

const placements = Ahorn.PlacementDict(
    "Require Dashless Trigger (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        RequireDashlessTrigger,
        "rectangle"
    )
)

end