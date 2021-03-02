module SJ2021TheoKillBoxTrigger

using ..Ahorn, Maple

@mapdef Trigger "SJ2021/TheoKillBoxTrigger" TheoKillBox(x::Integer, y::Integer, width::Integer=16, height::Integer=16)

const placements = Ahorn.PlacementDict(
    "Theo Kill Box (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        TheoKillBox,
        "rectangle"
    )
)

end