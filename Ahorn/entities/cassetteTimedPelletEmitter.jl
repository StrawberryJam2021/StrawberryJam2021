module SJ2021CassetteTimedPelletEmitter

using ..Ahorn, Maple

const DEFAULT_PELLET_SPEED = 100.0
const DEFAULT_CASSETTE_INDICES = "0"
const DEFAULT_TICKS = "0"

@mapdef Entity "SJ2021/CassetteTimedPelletEmitterUp" CassetteTimedPelletEmitterUp(
    x::Integer, y::Integer,
    cassetteIndices::String=DEFAULT_CASSETTE_INDICES, ticks::String=DEFAULT_TICKS, count::Integer=1,
    pelletSpeed::Real=DEFAULT_PELLET_SPEED,
    collideWithSolids::Bool=true
)

@mapdef Entity "SJ2021/CassetteTimedPelletEmitterDown" CassetteTimedPelletEmitterDown(
    x::Integer, y::Integer,
    cassetteIndices::String=DEFAULT_CASSETTE_INDICES, ticks::String=DEFAULT_TICKS, count::Integer=1,
    pelletSpeed::Real=DEFAULT_PELLET_SPEED,
    collideWithSolids::Bool=true
)

@mapdef Entity "SJ2021/CassetteTimedPelletEmitterLeft" CassetteTimedPelletEmitterLeft(
    x::Integer, y::Integer,
    cassetteIndices::String=DEFAULT_CASSETTE_INDICES, ticks::String=DEFAULT_TICKS, count::Integer=1,
    pelletSpeed::Real=DEFAULT_PELLET_SPEED,
    collideWithSolids::Bool=true
)

@mapdef Entity "SJ2021/CassetteTimedPelletEmitterRight" CassetteTimedPelletEmitterRight(
    x::Integer, y::Integer,
    cassetteIndices::String=DEFAULT_CASSETTE_INDICES, ticks::String=DEFAULT_TICKS, count::Integer=1,
    pelletSpeed::Real=DEFAULT_PELLET_SPEED,
    collideWithSolids::Bool=true
)

const placements = Ahorn.PlacementDict(
    "Cassette Timed Pellet Emitter (Up) (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        CassetteTimedPelletEmitterUp,
    ),
    "Cassette Timed Pellet Emitter (Down) (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        CassetteTimedPelletEmitterDown,
    ),
    "Cassette Timed Pellet Emitter (Left) (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        CassetteTimedPelletEmitterLeft,
    ),
    "Cassette Timed Pellet Emitter (Right) (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        CassetteTimedPelletEmitterRight,
    )
)

function Ahorn.selection(entity::CassetteTimedPelletEmitterUp)
    x, y = Ahorn.position(entity)
    return Ahorn.Rectangle(x - 7, y - 8, 14, 8)
end

function Ahorn.selection(entity::CassetteTimedPelletEmitterDown)
    x, y = Ahorn.position(entity)
    return Ahorn.Rectangle(x - 7, y, 14, 8)
end

function Ahorn.selection(entity::CassetteTimedPelletEmitterLeft)
    x, y = Ahorn.position(entity)
    return Ahorn.Rectangle(x - 8, y - 7, 8, 14)
end

function Ahorn.selection(entity::CassetteTimedPelletEmitterRight)
    x, y = Ahorn.position(entity)
    return Ahorn.Rectangle(x, y - 7, 8, 14)
end

sprite = "objects/StrawberryJam2021/laserEmitter/simple00"

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::CassetteTimedPelletEmitterUp, room::Maple.Room) = Ahorn.drawSprite(ctx, sprite, 0, 0, jx=0.5, jy=1, rot=0)
Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::CassetteTimedPelletEmitterDown, room::Maple.Room) = Ahorn.drawSprite(ctx, sprite, 16, 16, jx=0.5, jy=1, rot=pi)
Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::CassetteTimedPelletEmitterLeft, room::Maple.Room) = Ahorn.drawSprite(ctx, sprite, 0, 16, jx=0.5, jy=1, rot=-pi/2)
Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::CassetteTimedPelletEmitterRight, room::Maple.Room) = Ahorn.drawSprite(ctx, sprite, 16, 0, jx=0.5, jy=1, rot=pi/2)

end
