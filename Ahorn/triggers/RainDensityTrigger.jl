module SJ2021RainDensityTrigger
using ..Ahorn, Maple

@mapdef Trigger "SJ2021/RainDensityTrigger" RainDensityTrigger(x::Integer, y::Integer, width::Integer=Maple.defaultTriggerWidth, height::Integer=Maple.defaultTriggerHeight, density::Number=0.0, duration::Number=5.0)

const placements = Ahorn.PlacementDict(
    "Rain Density Trigger (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        RainDensityTrigger,
        "rectangle"
    )
)

end