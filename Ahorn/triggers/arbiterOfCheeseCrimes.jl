module SJ2021ArbiterOfCheeseCrimes

using ..Ahorn, Maple

@mapdef Trigger "SJ2021/ArbiterOfCheeseCrimes" ArbiterOfCheeseCrimes(x::Integer, y::Integer, width::Integer=16, height::Integer=16)

const placements = Ahorn.PlacementDict(
    "Arbiter of Cheese Crimes (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        ArbiterOfCheeseCrimes,
        "rectangle"
    ),
)

end