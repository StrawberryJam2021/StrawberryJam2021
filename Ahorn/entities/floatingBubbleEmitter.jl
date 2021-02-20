module SJ2021FloatingBubbleEmitter

using ..Ahorn,Maple

@mapdef Entity "SJ2021/FloatingBubbleEmitter" FloatingBubbleEmitter(x::Integer, y::Integer, spawnTimer::Number=2.0)

const placements = Ahorn.PlacementDict(
    "Floating Bubble Emitter\n(Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        FloatingBubbleEmitter,
        "point"
    )
)

function Ahorn.selection(entity::FloatingBubbleEmitter)
    x, y = Ahorn.position(entity)

    return Ahorn.Rectangle(x-8, y-8, 16, 16)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::FloatingBubbleEmitter, room::Maple.Room)
    Ahorn.drawRectangle(ctx, -8, -8, 16, 16, (1.0, 1.0, 1.0, 0.4), (1.0, 1.0, 1.0, 1.0))
end

end