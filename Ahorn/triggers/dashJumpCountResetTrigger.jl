module SJ2021DashJumpCountResetTrigger

using ..Ahorn, Maple

@mapdef Trigger "SJ2021/DashJumpCountResetTrigger" DashJumpCountResetTrigger(x::Integer, y::Integer, width::Integer=16, height::Integer=16)

const placements = Ahorn.PlacementDict(
    "Dash/Jump Count Reset Trigger (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        DashJumpCountResetTrigger,
        "rectangle"
    )
)

end