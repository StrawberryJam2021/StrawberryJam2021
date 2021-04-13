module SJ2021CassetteTimedLaserEmitter

using ..Ahorn, Maple

const default_alpha = 0.4
const default_thickness = 6.0

@mapdef Entity "SJ2021/CassetteTimedLaserEmitterUp" CassetteTimedLaserEmitterUp(
    x::Integer, y::Integer,
    cassetteIndex::Integer=0, tickOffset::Integer=0, lengthInTicks::Integer=2,
    alpha::Float64=default_alpha, thickness::Float64=default_thickness, flicker::Bool=true,
    killPlayer::Bool=true, disableLasers::Bool=false, triggerZipMovers::Bool=false, collideWithSolids::Bool=true
)

@mapdef Entity "SJ2021/CassetteTimedLaserEmitterDown" CassetteTimedLaserEmitterDown(
    x::Integer, y::Integer,
    cassetteIndex::Integer=0, tickOffset::Integer=0, lengthInTicks::Integer=2,
    alpha::Float64=default_alpha, thickness::Float64=default_thickness, flicker::Bool=true,
    killPlayer::Bool=true, disableLasers::Bool=false, triggerZipMovers::Bool=false, collideWithSolids::Bool=true
)

@mapdef Entity "SJ2021/CassetteTimedLaserEmitterLeft" CassetteTimedLaserEmitterLeft(
    x::Integer, y::Integer,
    cassetteIndex::Integer=0, tickOffset::Integer=0, lengthInTicks::Integer=2,
    alpha::Float64=default_alpha, thickness::Float64=default_thickness, flicker::Bool=true,
    killPlayer::Bool=true, disableLasers::Bool=false, triggerZipMovers::Bool=false, collideWithSolids::Bool=true
)

@mapdef Entity "SJ2021/CassetteTimedLaserEmitterRight" CassetteTimedLaserEmitterRight(
    x::Integer, y::Integer,
    cassetteIndex::Integer=0, tickOffset::Integer=0, lengthInTicks::Integer=2,
    alpha::Float64=default_alpha, thickness::Float64=default_thickness, flicker::Bool=true,
    killPlayer::Bool=true, disableLasers::Bool=false, triggerZipMovers::Bool=false, collideWithSolids::Bool=true
)

const colorNames = Dict{String, Int}(
    "Blue" => 0,
    "Rose" => 1,
    "Bright Sun" => 2,
    "Malachite" => 3
)

const editingOptions = Dict{String, Any}(
    "cassetteIndex" => colorNames,
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

Ahorn.editingOptions(entity::CassetteTimedLaserEmitterUp) = editingOptions
Ahorn.editingOptions(entity::CassetteTimedLaserEmitterDown) = editingOptions
Ahorn.editingOptions(entity::CassetteTimedLaserEmitterLeft) = editingOptions
Ahorn.editingOptions(entity::CassetteTimedLaserEmitterRight) = editingOptions

sprite = "objects/StrawberryJam2021/laserEmitter/idle00"

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::CassetteTimedLaserEmitterUp, room::Maple.Room) = Ahorn.drawSprite(ctx, sprite, 0, 0, jx=0.5, jy=1, rot=0)
Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::CassetteTimedLaserEmitterDown, room::Maple.Room) = Ahorn.drawSprite(ctx, sprite, 16, 16, jx=0.5, jy=1, rot=pi)
Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::CassetteTimedLaserEmitterLeft, room::Maple.Room) = Ahorn.drawSprite(ctx, sprite, 0, 16, jx=0.5, jy=1, rot=-pi/2)
Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::CassetteTimedLaserEmitterRight, room::Maple.Room) = Ahorn.drawSprite(ctx, sprite, 16, 0, jx=0.5, jy=1, rot=pi/2)

end
