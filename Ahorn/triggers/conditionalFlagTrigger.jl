module SJ2021ConditionalFlagTrigger
using ..Ahorn, Maple

@mapdef Trigger "SJ2021/ConditionalFlagTrigger" ConditionalFlagTrigger(x::Integer, y::Integer, width::Integer=16, height::Integer=16, flag::String="", flagValue::Bool=true, controllerFlag::String="", resetOnLeave::Bool=true)

const placements = Ahorn.PlacementDict(
    "Conditional Flag Trigger (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        ConditionalFlagTrigger,
        "rectangle"
    )
)

end