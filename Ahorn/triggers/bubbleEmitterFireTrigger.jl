module SJ2021BubbleEmitterFireTrigger

using ..Ahorn, Maple

@mapdef Trigger "SJ2021/bubbleEmitterFireTrigger" BubbleEmitterFireTrigger(x::Integer, y::Integer, width::Integer=16, height::Integer=16, flag::String="")

const placements = Ahorn.PlacementDict(
    "Bubble Emitter Fire Trigger\n(Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        BubbleEmitterFireTrigger,
        "rectangle"
    )
)

end