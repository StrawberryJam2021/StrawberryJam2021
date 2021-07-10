module SJ2021BrushLaserEmitter

using ..Ahorn, Maple

const default_size = 16
const thickness = 8

@mapdef Entity "SJ2021/WideBrushLaserEmitterUp" WideBrushLaserEmitterUp(
    x::Integer, y::Integer, width::Integer=default_size,
    cassetteIndex::Integer=0, killPlayer::Bool=true, collideWithSolids::Bool=true, halfLength::Bool=false
)

@mapdef Entity "SJ2021/WideBrushLaserEmitterDown" WideBrushLaserEmitterDown(
    x::Integer, y::Integer, width::Integer=default_size,
    cassetteIndex::Integer=0, killPlayer::Bool=true, collideWithSolids::Bool=true, halfLength::Bool=false
)

@mapdef Entity "SJ2021/WideBrushLaserEmitterLeft" WideBrushLaserEmitterLeft(
    x::Integer, y::Integer, height::Integer=default_size,
    cassetteIndex::Integer=0, killPlayer::Bool=true, collideWithSolids::Bool=true, halfLength::Bool=false
)

@mapdef Entity "SJ2021/WideBrushLaserEmitterRight" WideBrushLaserEmitterRight(
    x::Integer, y::Integer, height::Integer=default_size,
    cassetteIndex::Integer=0, killPlayer::Bool=true, collideWithSolids::Bool=true, halfLength::Bool=false
)

const directions = Dict{String, String}(
    "SJ2021/WideBrushLaserEmitterUp" => "up",
    "SJ2021/WideBrushLaserEmitterDown" => "down",
    "SJ2021/WideBrushLaserEmitterLeft" => "left",
    "SJ2021/WideBrushLaserEmitterRight" => "right",
)
const wideBrushLaserUnion = Union{WideBrushLaserEmitterUp, WideBrushLaserEmitterDown, WideBrushLaserEmitterLeft, WideBrushLaserEmitterRight}

const placements = Ahorn.PlacementDict(
    "Wide Brush Laser Emitter (Up) (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        WideBrushLaserEmitterUp,
        "rectangle",
    ),
    "Wide Brush Laser Emitter (Down) (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        WideBrushLaserEmitterDown,
        "rectangle",
    ),
    "Wide Brush Laser Emitter (Left) (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        WideBrushLaserEmitterLeft,
        "rectangle",
    ),
    "Wide Brush Laser Emitter (Right) (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        WideBrushLaserEmitterRight,
        "rectangle",
    )
)

function Ahorn.selection(entity::WideBrushLaserEmitterUp)
    x, y = Ahorn.position(entity)
    width = get(entity.data, "width", default_size)
    return Ahorn.Rectangle(x, y - thickness, width, thickness)
end

function Ahorn.selection(entity::WideBrushLaserEmitterDown)
    x, y = Ahorn.position(entity)
    width = get(entity.data, "width", default_size)
    return Ahorn.Rectangle(x, y, width, thickness)
end

function Ahorn.selection(entity::WideBrushLaserEmitterLeft)
    x, y = Ahorn.position(entity)
    height = get(entity.data, "height", default_size)
    return Ahorn.Rectangle(x - thickness, y, thickness, height)
end

function Ahorn.selection(entity::WideBrushLaserEmitterRight)
    x, y = Ahorn.position(entity)
    height = get(entity.data, "height", default_size)
    return Ahorn.Rectangle(x, y, thickness, height)
end

Ahorn.resizable(entity::WideBrushLaserEmitterUp) = true, false
Ahorn.resizable(entity::WideBrushLaserEmitterDown) = true, false
Ahorn.resizable(entity::WideBrushLaserEmitterLeft) = false, true
Ahorn.resizable(entity::WideBrushLaserEmitterRight) = false, true
Ahorn.minimumSize(entity::WideBrushLaserEmitterUp) = default_size, thickness
Ahorn.minimumSize(entity::WideBrushLaserEmitterDown) = default_size, thickness
Ahorn.minimumSize(entity::WideBrushLaserEmitterLeft) = thickness, default_size
Ahorn.minimumSize(entity::WideBrushLaserEmitterRight) = thickness, default_size

sprite_path = "objects/StrawberryJam2021/brushLaserEmitter"

function spriteForTexture(entity::wideBrushLaserUnion)
    index = get(entity.data, "cassetteIndex", 0)
    prefix = index == 0 ? "blue" : "pink"
    return "$(sprite_path)/$(prefix)/brush-a/brush-a-idle00"
end

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::WideBrushLaserEmitterUp, room::Maple.Room) = Ahorn.drawSprite(ctx, spriteForTexture(entity), -12, 0, jx=0, jy=0, rot=-pi/2)
Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::WideBrushLaserEmitterDown, room::Maple.Room) = Ahorn.drawSprite(ctx, spriteForTexture(entity), -12, 0, sy=-1, jx=0, jy=0, rot=-pi/2)

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::WideBrushLaserEmitterLeft, room::Maple.Room) = Ahorn.drawSprite(ctx, spriteForTexture(entity), 0, 0, sx=-1, jx=0, jy=0.5, rot=0)
Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::WideBrushLaserEmitterRight, room::Maple.Room) = Ahorn.drawSprite(ctx, spriteForTexture(entity), 0, 0, jx=0, jy=0.5, rot=0)

end
