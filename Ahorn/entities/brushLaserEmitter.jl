module SJ2021BrushLaserEmitter

using ..Ahorn, Maple

const thickness = 8
# const default_color = "FF0000"
# const default_cassette_indices = "0"
# const default_ticks = "0"
const default_length = 2
# const default_texture = "a"

@mapdef Entity "SJ2021/BrushLaserEmitterUp" BrushLaserEmitterUp(
    x::Integer, y::Integer,
    cassetteIndex::Integer=0, killPlayer::Bool=true, collideWithSolids::Bool=true, lengthInTicks::Integer=default_length
#     texture::String=default_texture, killPlayer::Bool=true, collideWithSolids::Bool=true, color::String=default_color,
#     cassetteIndices::String=default_cassette_indices, ticks::String=default_ticks, lengthInTicks::Integer=default_length
)

@mapdef Entity "SJ2021/BrushLaserEmitterDown" BrushLaserEmitterDown(
    x::Integer, y::Integer,
    cassetteIndex::Integer=0, killPlayer::Bool=true, collideWithSolids::Bool=true, lengthInTicks::Integer=default_length
#     texture::String=default_texture, killPlayer::Bool=true, collideWithSolids::Bool=true, color::String=default_color,
#     cassetteIndices::String=default_cassette_indices, ticks::String=default_ticks, lengthInTicks::Integer=default_length
)

@mapdef Entity "SJ2021/BrushLaserEmitterLeft" BrushLaserEmitterLeft(
    x::Integer, y::Integer,
    cassetteIndex::Integer=0, killPlayer::Bool=true, collideWithSolids::Bool=true, lengthInTicks::Integer=default_length
#     texture::String=default_texture, killPlayer::Bool=true, collideWithSolids::Bool=true, color::String=default_color,
#     cassetteIndices::String=default_cassette_indices, ticks::String=default_ticks, lengthInTicks::Integer=default_length
)

@mapdef Entity "SJ2021/BrushLaserEmitterRight" BrushLaserEmitterRight(
    x::Integer, y::Integer,
    cassetteIndex::Integer=0, killPlayer::Bool=true, collideWithSolids::Bool=true, lengthInTicks::Integer=default_length
#     texture::String=default_texture, killPlayer::Bool=true, collideWithSolids::Bool=true, color::String=default_color,
#     cassetteIndices::String=default_cassette_indices, ticks::String=default_ticks, lengthInTicks::Integer=default_length
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
    ),
    "Brush Laser Emitter (Down) (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        BrushLaserEmitterDown,
    ),
    "Brush Laser Emitter (Left) (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        BrushLaserEmitterLeft,
    ),
    "Brush Laser Emitter (Right) (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        BrushLaserEmitterRight,
    )
)
function Ahorn.selection(entity::BrushLaserEmitterUp)
    x, y = Ahorn.position(entity)
    return Ahorn.Rectangle(x - 7, y - 8, 14, 8)
end

function Ahorn.selection(entity::BrushLaserEmitterDown)
    x, y = Ahorn.position(entity)
    return Ahorn.Rectangle(x - 7, y, 14, 8)
end

function Ahorn.selection(entity::BrushLaserEmitterLeft)
    x, y = Ahorn.position(entity)
    return Ahorn.Rectangle(x - 8, y - 7, 8, 14)
end

function Ahorn.selection(entity::BrushLaserEmitterRight)
    x, y = Ahorn.position(entity)
    return Ahorn.Rectangle(x, y - 7, 8, 14)
end

sprite_path = "objects/StrawberryJam2021/brushLaserEmitter"

function spriteForTexture(entity::brushLaserUnion)
    index = get(entity.data, "cassetteIndex", 0)
    prefix = index == 0 ? "pink" : "blue"
    return "$(sprite_path)/$(prefix)/brush-a/brush-a-idle00"
end

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::BrushLaserEmitterUp, room::Maple.Room) = Ahorn.drawSprite(ctx, spriteForTexture(entity), -12, 0, jx=0, jy=0, rot=-pi/2)
Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::BrushLaserEmitterDown, room::Maple.Room) = Ahorn.drawSprite(ctx, spriteForTexture(entity), -12, 0, sy=-1, jx=0, jy=0, rot=-pi/2)

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::BrushLaserEmitterLeft, room::Maple.Room) = Ahorn.drawSprite(ctx, spriteForTexture(entity), 0, 0, sx=-1, jx=0, jy=0.5, rot=0)
Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::BrushLaserEmitterRight, room::Maple.Room) = Ahorn.drawSprite(ctx, spriteForTexture(entity), 0, 0, jx=0, jy=0.5, rot=0)

end
