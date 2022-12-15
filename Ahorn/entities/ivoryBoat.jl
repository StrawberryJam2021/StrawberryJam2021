module SJ2021IvoryBoat

using ..Ahorn, Maple

@mapdef Entity "SJ2021/IvoryBoat" IvoryBoat(x::Integer, y::Integer, flag::String="")

function ivoryBoatFinalizer(entity)
    x, y = Ahorn.position(entity)
    entity.data["nodes"] = [(x + 48, y)]
end
    
const placements = Ahorn.PlacementDict(
    "Ivory Boat (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        IvoryBoat,
        "rectangle",
        Dict{String, Any}(),
        ivoryBoatFinalizer
    )
)

Ahorn.nodeLimits(entity::IvoryBoat) = 1, 1
Ahorn.resizable(entity::IvoryBoat) = false, false

const texture = "objects/StrawberryJam2021/ivoryBoat/boat.png"

function Ahorn.selection(entity::IvoryBoat)
    x, y = Ahorn.position(entity)
    nx, ny = Int.(entity.data["nodes"][1])
    nx = max(nx, x + 48)

    return [Ahorn.Rectangle(x, y, 48, 8), Ahorn.Rectangle(nx, y, 48, 8)]
end

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::IvoryBoat, room::Maple.Room)
    x, y = Ahorn.position(entity)
    Ahorn.drawSprite(ctx, texture, x + 24, y)
end

function Ahorn.renderSelectedAbs(ctx::Ahorn.Cairo.CairoContext, entity::IvoryBoat)
    x, y = Ahorn.position(entity)
    nx, ny = Int.(entity.data["nodes"][1])
    nx = max(nx, x + 48)

    Ahorn.drawSprite(ctx, texture, nx + 24, y)
    Ahorn.drawArrow(ctx, x + 24, y + 4, nx + 24, y + 4, Ahorn.colors.selection_selected_fc, headLength=6)
end

end