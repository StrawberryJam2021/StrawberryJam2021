module SJ2021CassetteTimedLaserEmitter

using ..Ahorn, Maple

const DEFAULT_CASSETTE_INDICES = "0"
const DEFAULT_TICKS = "0"
const DEFAULT_LENGTH = 2
const DEFAULT_ALPHA = 0.4
const DEFAULT_THICKNESS = 6.0

@mapdef Entity "SJ2021/CassetteTimedLaserEmitterUp" CassetteTimedLaserEmitterUp(
    x::Integer, y::Integer,
    cassetteIndices::String=DEFAULT_CASSETTE_INDICES, ticks::String=DEFAULT_TICKS, lengthInTicks::Integer=DEFAULT_LENGTH,
    alpha::Real=DEFAULT_ALPHA, thickness::Real=DEFAULT_THICKNESS, flicker::Bool=true,
    killPlayer::Bool=true, disableLasers::Bool=false, triggerZipMovers::Bool=false, collideWithSolids::Bool=true
)

@mapdef Entity "SJ2021/CassetteTimedLaserEmitterDown" CassetteTimedLaserEmitterDown(
    x::Integer, y::Integer,
    cassetteIndices::String=DEFAULT_CASSETTE_INDICES, ticks::String=DEFAULT_TICKS, lengthInTicks::Integer=DEFAULT_LENGTH,
    alpha::Real=DEFAULT_ALPHA, thickness::Real=DEFAULT_THICKNESS, flicker::Bool=true,
    killPlayer::Bool=true, disableLasers::Bool=false, triggerZipMovers::Bool=false, collideWithSolids::Bool=true
)

@mapdef Entity "SJ2021/CassetteTimedLaserEmitterLeft" CassetteTimedLaserEmitterLeft(
    x::Integer, y::Integer,
    cassetteIndices::String=DEFAULT_CASSETTE_INDICES, ticks::String=DEFAULT_TICKS, lengthInTicks::Integer=DEFAULT_LENGTH,
    alpha::Real=DEFAULT_ALPHA, thickness::Real=DEFAULT_THICKNESS, flicker::Bool=true,
    killPlayer::Bool=true, disableLasers::Bool=false, triggerZipMovers::Bool=false, collideWithSolids::Bool=true
)

@mapdef Entity "SJ2021/CassetteTimedLaserEmitterRight" CassetteTimedLaserEmitterRight(
    x::Integer, y::Integer,
    cassetteIndices::String=DEFAULT_CASSETTE_INDICES, ticks::String=DEFAULT_TICKS, lengthInTicks::Integer=DEFAULT_LENGTH,
    alpha::Real=DEFAULT_ALPHA, thickness::Real=DEFAULT_THICKNESS, flicker::Bool=true,
    killPlayer::Bool=true, disableLasers::Bool=false, triggerZipMovers::Bool=false, collideWithSolids::Bool=true
)

const placements = Ahorn.PlacementDict(
    "Cassette Timed Laser Emitter (Up) (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        CassetteTimedLaserEmitterUp,
    ),
    "Cassette Timed Laser Emitter (Down) (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        CassetteTimedLaserEmitterDown,
    ),
    "Cassette Timed Laser Emitter (Left) (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        CassetteTimedLaserEmitterLeft,
    ),
    "Cassette Timed Laser Emitter (Right) (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        CassetteTimedLaserEmitterRight,
    )
)

function Ahorn.selection(entity::CassetteTimedLaserEmitterUp)
    x, y = Ahorn.position(entity)
    return Ahorn.Rectangle(x - 7, y - 8, 14, 8)
end

function Ahorn.selection(entity::CassetteTimedLaserEmitterDown)
    x, y = Ahorn.position(entity)
    return Ahorn.Rectangle(x - 7, y, 14, 8)
end

function Ahorn.selection(entity::CassetteTimedLaserEmitterLeft)
    x, y = Ahorn.position(entity)
    return Ahorn.Rectangle(x - 8, y - 7, 8, 14)
end

function Ahorn.selection(entity::CassetteTimedLaserEmitterRight)
    x, y = Ahorn.position(entity)
    return Ahorn.Rectangle(x, y - 7, 8, 14)
end

sprite = "objects/StrawberryJam2021/laserEmitter/simple00"

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::CassetteTimedLaserEmitterUp, room::Maple.Room) = Ahorn.drawSprite(ctx, sprite, 0, 0, jx=0.5, jy=1, rot=0)
Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::CassetteTimedLaserEmitterDown, room::Maple.Room) = Ahorn.drawSprite(ctx, sprite, 16, 16, jx=0.5, jy=1, rot=pi)
Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::CassetteTimedLaserEmitterLeft, room::Maple.Room) = Ahorn.drawSprite(ctx, sprite, 0, 16, jx=0.5, jy=1, rot=-pi/2)
Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::CassetteTimedLaserEmitterRight, room::Maple.Room) = Ahorn.drawSprite(ctx, sprite, 16, 0, jx=0.5, jy=1, rot=pi/2)

end
