module SJ2021InfiniteDashTrigger

using ..Ahorn, Maple

@mapdef Trigger "SJ2021/InfiniteDashTrigger" InfiniteDashTrigger(x::Integer, y::Integer, width::Integer=16, height::Integer=16)

const placements = Ahorn.PlacementDict(
    "Infinite Dash Field(Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        InfiniteDashTrigger,
        "rectangle"
    )
)

end