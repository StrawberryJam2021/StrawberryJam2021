module SJ2021LaserEmitter

using ..Ahorn, Maple

@mapdef Entity "SJ2021/LaserEmitterUp" LaserEmitterUp(x::Integer, y::Integer)
@mapdef Entity "SJ2021/LaserEmitterDown" LaserEmitterDown(x::Integer, y::Integer)
@mapdef Entity "SJ2021/LaserEmitterLeft" LaserEmitterLeft(x::Integer, y::Integer)
@mapdef Entity "SJ2021/LaserEmitterRight" LaserEmitterRight(x::Integer, y::Integer)

const placements = Ahorn.PlacementDict(
    "Laser Emitter (Up) (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        LaserEmitterUp,
    ),
    "Laser Emitter (Down) (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        LaserEmitterDown,
    ),
    "Laser Emitter (Left) (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        LaserEmitterLeft,
    ),
    "Laser Emitter (Right) (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        LaserEmitterRight,
    )
)

function Ahorn.selection(entity::LaserEmitterUp)
    x, y = Ahorn.position(entity)
    return Ahorn.Rectangle(x - 7, y - 8, 14, 8)
end

function Ahorn.selection(entity::LaserEmitterDown)
    x, y = Ahorn.position(entity)
    return Ahorn.Rectangle(x - 7, y, 14, 8)
end

function Ahorn.selection(entity::LaserEmitterLeft)
    x, y = Ahorn.position(entity)
    return Ahorn.Rectangle(x - 8, y - 7, 8, 14)
end

function Ahorn.selection(entity::LaserEmitterRight)
    x, y = Ahorn.position(entity)
    return Ahorn.Rectangle(x, y - 7, 8, 14)
end

sprite = "objects/StrawberryJam2021/laserEmitter/idle00"

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::LaserEmitterUp, room::Maple.Room) = Ahorn.drawSprite(ctx, sprite, 0, 0, jx=0.5, jy=1, rot=0)
Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::LaserEmitterDown, room::Maple.Room) = Ahorn.drawSprite(ctx, sprite, 16, 16, jx=0.5, jy=1, rot=pi)
Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::LaserEmitterRight, room::Maple.Room) = Ahorn.drawSprite(ctx, sprite, 16, 0, jx=0.5, jy=1, rot=pi/2)
Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::LaserEmitterLeft, room::Maple.Room) = Ahorn.drawSprite(ctx, sprite, 0, 16, jx=0.5, jy=1, rot=-pi/2)

end
