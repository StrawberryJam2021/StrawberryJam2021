module SJ2021FloatingBubbleEmitter

using ..Ahorn,Maple

@mapdef Entity "SJ2021/FloatingBubbleEmitter" FloatingBubbleEmitter(x::Integer, y::Integer, flag::String="")

const placements = Ahorn.PlacementDict(
    "Floating Bubble Emitter\n(Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        FloatingBubbleEmitter,
        "point"
    )
)

function Ahorn.selection(entity::FloatingBubbleEmitter)
    x, y = Ahorn.position(entity)

    return Ahorn.getSpriteRectangle("objects/StrawberryJam2021/bubbleEmitter/idle00.png", x, y)
end

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::FloatingBubbleEmitter, room::Maple.Room)
    x, y = Ahorn.position(entity)
    Ahorn.drawSprite(ctx, "objects/StrawberryJam2021/bubbleEmitter/idle00.png", x, y)
end

end