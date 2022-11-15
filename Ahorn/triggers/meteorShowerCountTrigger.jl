module SJ2021MeteorShowerCountTrigger

using ..Ahorn, Maple

@mapdef Trigger "SJ2021/MeteorShowerCountTrigger" MeteorShowerCountTrigger(x::Integer, y::Integer, width::Integer=16, height::Integer=16, NumberOfMeteors::Integer=1, OnlyOnce::Bool=false)

const placements = Ahorn.PlacementDict(
    "Meteor Shower Count Trigger (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        MeteorShowerCountTrigger,
        "rectangle"
    )
)

end