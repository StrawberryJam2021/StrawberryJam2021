module SJ2021MaskedOutline
using ..Ahorn, Maple

@mapdef Entity "SJ2021/MaskedOutline" MaskedOutline(x::Integer, y::Integer)

const placements = Ahorn.PlacementDict(
    "Masked Outline Controller (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        MaskedOutline,
        "rectangle"
    )
)

function Ahorn.selection(entity::MaskedOutline)
    x, y = Ahorn.position(entity)
    return Ahorn.Rectangle(x, y, 10, 10)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::MaskedOutline)
    x, y = Ahorn.position(entity)
    Ahorn.drawRectangle(ctx, 0, 0, 10, 10, (1.0, 0.0, 0.0, 1.0), (0.0, 0.0, 0.0, 0.0))
end

end