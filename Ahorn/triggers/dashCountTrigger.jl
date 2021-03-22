module SJ2021DashCountTrigger

using ..Ahorn, Maple

@mapdef Trigger "SJ2021/DashCountTrigger" DashCountTrigger(x::Integer, y::Integer, width::Integer=16, height::Integer=16, NumberOfDashes::Integer=1, ResetOnDeath ::Bool = false)

const placements = Ahorn.PlacementDict(
    "DashCount Trigger (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        DashCountTrigger,
        "rectangle"
    )
)

end