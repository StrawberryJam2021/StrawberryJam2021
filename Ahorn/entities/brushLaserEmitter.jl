module SJ2021BrushLaserEmitter

using ..Ahorn, Maple

const default_size = 16
const thickness = 8

const BEAM_THICKNESS = 12
const BRUSH_LENGTH = 16

@mapdef Entity "SJ2021/BrushLaserEmitterUp" BrushLaserEmitterUp(
    x::Integer, y::Integer, width::Integer=default_size,
    cassetteIndex::Integer=0, killPlayer::Bool=true, collideWithSolids::Bool=true, halfLength::Bool=false
)

@mapdef Entity "SJ2021/BrushLaserEmitterDown" BrushLaserEmitterDown(
    x::Integer, y::Integer, width::Integer=default_size,
    cassetteIndex::Integer=0, killPlayer::Bool=true, collideWithSolids::Bool=true, halfLength::Bool=false
)

@mapdef Entity "SJ2021/BrushLaserEmitterLeft" BrushLaserEmitterLeft(
    x::Integer, y::Integer, height::Integer=default_size,
    cassetteIndex::Integer=0, killPlayer::Bool=true, collideWithSolids::Bool=true, halfLength::Bool=false
)

@mapdef Entity "SJ2021/BrushLaserEmitterRight" BrushLaserEmitterRight(
    x::Integer, y::Integer, height::Integer=default_size,
    cassetteIndex::Integer=0, killPlayer::Bool=true, collideWithSolids::Bool=true, halfLength::Bool=false
)

const directions = Dict{String, String}(
    "SJ2021/BrushLaserEmitterUp" => "up",
    "SJ2021/BrushLaserEmitterDown" => "down",
    "SJ2021/BrushLaserEmitterLeft" => "left",
    "SJ2021/BrushLaserEmitterRight" => "right",
)
const brushLaserUnion = Union{BrushLaserEmitterUp, BrushLaserEmitterDown, BrushLaserEmitterLeft, BrushLaserEmitterRight}

const placements = Ahorn.PlacementDict(
    "Brush Laser Emitter (Up) (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        BrushLaserEmitterUp,
        "rectangle",
    ),
    "Brush Laser Emitter (Down) (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        BrushLaserEmitterDown,
        "rectangle",
    ),
    "Brush Laser Emitter (Left) (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        BrushLaserEmitterLeft,
        "rectangle",
    ),
    "Brush Laser Emitter (Right) (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        BrushLaserEmitterRight,
        "rectangle",
    )
)

function Ahorn.selection(entity::BrushLaserEmitterUp)
    x, y = Ahorn.position(entity)
    width = get(entity.data, "width", default_size)
    return Ahorn.Rectangle(x, y - thickness, width, thickness)
end

function Ahorn.selection(entity::BrushLaserEmitterDown)
    x, y = Ahorn.position(entity)
    width = get(entity.data, "width", default_size)
    return Ahorn.Rectangle(x, y, width, thickness)
end

function Ahorn.selection(entity::BrushLaserEmitterLeft)
    x, y = Ahorn.position(entity)
    height = get(entity.data, "height", default_size)
    return Ahorn.Rectangle(x - thickness, y, thickness, height)
end

function Ahorn.selection(entity::BrushLaserEmitterRight)
    x, y = Ahorn.position(entity)
    height = get(entity.data, "height", default_size)
    return Ahorn.Rectangle(x, y, thickness, height)
end

Ahorn.resizable(entity::BrushLaserEmitterUp) = true, false
Ahorn.resizable(entity::BrushLaserEmitterDown) = true, false
Ahorn.resizable(entity::BrushLaserEmitterLeft) = false, true
Ahorn.resizable(entity::BrushLaserEmitterRight) = false, true
Ahorn.minimumSize(entity::BrushLaserEmitterUp) = default_size, thickness
Ahorn.minimumSize(entity::BrushLaserEmitterDown) = default_size, thickness
Ahorn.minimumSize(entity::BrushLaserEmitterLeft) = thickness, default_size
Ahorn.minimumSize(entity::BrushLaserEmitterRight) = thickness, default_size

sprite_path = "objects/StrawberryJam2021/brushLaserEmitter"

function spriteForTexture(entity::brushLaserUnion)
    index = get(entity.data, "cassetteIndex", 0)
    prefix = index == 0 ? "blue" : "pink"
    return "$(sprite_path)/$(prefix)/brush-a/brush-a-idle00"
end

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::BrushLaserEmitterUp, room::Maple.Room) = Ahorn.drawSprite(ctx, spriteForTexture(entity), -12, 0, jx=0, jy=0, rot=-pi/2)
Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::BrushLaserEmitterDown, room::Maple.Room) = Ahorn.drawSprite(ctx, spriteForTexture(entity), -12, 0, sy=-1, jx=0, jy=0, rot=-pi/2)

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::BrushLaserEmitterLeft, room::Maple.Room) = Ahorn.drawSprite(ctx, spriteForTexture(entity), 0, 0, sx=-1, jx=0, jy=0.5, rot=0)
Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::BrushLaserEmitterRight, room::Maple.Room) = Ahorn.drawSprite(ctx, spriteForTexture(entity), 0, 0, jx=0, jy=0.5, rot=0)

end
