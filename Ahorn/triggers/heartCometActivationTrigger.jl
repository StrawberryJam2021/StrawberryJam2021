module SJ2021HeartCometActivationTrigger

using ..Ahorn, Maple

@mapdef Trigger "SJ2021/HeartCometActivationTrigger" HeartCometActivationTrigger(x::Integer, y::Integer, width::Integer=16, height::Integer=16)

const placements = Ahorn.PlacementDict(
    "Heart Comet Activation Trigger (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        HeartCometActivationTrigger,
        "rectangle"
    )
)

end