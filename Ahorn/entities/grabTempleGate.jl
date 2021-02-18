module SJ2021GrabTempleGate

using ..Ahorn, Maple

@mapdef Entity "SJ2021/GrabTempleGate" GrabTempleGate(x::Integer, y::Integer, closed::Bool = false)

const placements = Ahorn.PlacementDict(
    "Grab Temple Gate (Open) (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        GrabTempleGate,
        "point",
        Dict{String, Any}(
            "closed" => false
        )
    ),
    "Grab Temple Gate (Closed) (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        GrabTempleGate,
        "point",
        Dict{String, Any}(
            "closed" => true
        )
    )
)

const texture = "objects/door/TempleDoor00";

function Ahorn.selection(entity::GrabTempleGate)
    x, y = Ahorn.position(entity)

    return Ahorn.Rectangle(x - 4, y, 15, 48)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::GrabTempleGate, room::Maple.Room)
    Ahorn.drawImage(ctx, texture, -4, 0)
end

end