module SJ2021OshiroAttackTimeTrigger

using ..Ahorn, Maple

@mapdef Trigger "SJ2021/OshiroAttackTimeTrigger" OshiroAttackTimeTrigger(x::Integer, y::Integer, width::Integer=16, height::Integer=16, Enable::Bool=true)

const placements = Ahorn.PlacementDict(
    "Stable Oshiro Attack Time (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        OshiroAttackTimeTrigger,
        "rectangle"
    )
)

end