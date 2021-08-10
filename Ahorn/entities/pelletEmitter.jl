module SJ2021PelletEmitter

using ..Ahorn, Maple

const DEFAULT_PELLET_SPEED = 100.0
const DEFAULT_PELLET_COLOR = "FF0000"
const DEFAULT_FREQUENCY = 2.0

@mapdef Entity "SJ2021/PelletEmitterUp" PelletEmitterUp(
    x::Integer, y::Integer,
    frequency::Real=DEFAULT_FREQUENCY, offset::Real=0, count::Integer=1,
    pelletSpeed::Real=DEFAULT_PELLET_SPEED, pelletColor::String=DEFAULT_PELLET_COLOR,
    collideWithSolids::Bool=true
)

@mapdef Entity "SJ2021/PelletEmitterDown" PelletEmitterDown(
    x::Integer, y::Integer,
    frequency::Real=DEFAULT_FREQUENCY, offset::Real=0, count::Integer=1,
    pelletSpeed::Real=DEFAULT_PELLET_SPEED, pelletColor::String=DEFAULT_PELLET_COLOR,
    collideWithSolids::Bool=true
)

@mapdef Entity "SJ2021/PelletEmitterLeft" PelletEmitterLeft(
    x::Integer, y::Integer,
    frequency::Real=DEFAULT_FREQUENCY, offset::Real=0, count::Integer=1,
    pelletSpeed::Real=DEFAULT_PELLET_SPEED, pelletColor::String=DEFAULT_PELLET_COLOR,
    collideWithSolids::Bool=true
)

@mapdef Entity "SJ2021/PelletEmitterRight" PelletEmitterRight(
    x::Integer, y::Integer,
    frequency::Real=DEFAULT_FREQUENCY, offset::Real=0, count::Integer=1,
    pelletSpeed::Real=DEFAULT_PELLET_SPEED, pelletColor::String=DEFAULT_PELLET_COLOR,
    collideWithSolids::Bool=true
)

const colors = Dict{String, String}(
    "Red" => "FF0000",
    "Green" => "00FF00",
    "Blue" => "0000FF",
    "Cyan" => "00FFFF",
    "Magenta" => "FF00FF",
    "Yellow" => "FFFF00",
    "White" => "FFFFFF"
)

const placements = Ahorn.PlacementDict(
    "Pellet Emitter (Up) (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        PelletEmitterUp,
    ),
    "Pellet Emitter (Down) (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        PelletEmitterDown,
    ),
    "Pellet Emitter (Left) (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        PelletEmitterLeft,
    ),
    "Pellet Emitter (Right) (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        PelletEmitterRight,
    )
)

function Ahorn.selection(entity::PelletEmitterUp)
    x, y = Ahorn.position(entity)
    return Ahorn.Rectangle(x - 7, y - 8, 14, 8)
end

function Ahorn.selection(entity::PelletEmitterDown)
    x, y = Ahorn.position(entity)
    return Ahorn.Rectangle(x - 7, y, 14, 8)
end

function Ahorn.selection(entity::PelletEmitterLeft)
    x, y = Ahorn.position(entity)
    return Ahorn.Rectangle(x - 8, y - 7, 8, 14)
end

function Ahorn.selection(entity::PelletEmitterRight)
    x, y = Ahorn.position(entity)
    return Ahorn.Rectangle(x, y - 7, 8, 14)
end

Ahorn.editingOptions(entity::PelletEmitterUp) = Dict{String, Any}( "pelletColor" => colors )
Ahorn.editingOptions(entity::PelletEmitterDown) = Dict{String, Any}( "pelletColor" => colors )
Ahorn.editingOptions(entity::PelletEmitterLeft) = Dict{String, Any}( "pelletColor" => colors )
Ahorn.editingOptions(entity::PelletEmitterRight) = Dict{String, Any}( "pelletColor" => colors )

sprite = "objects/StrawberryJam2021/laserEmitter/simple00"

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::PelletEmitterUp, room::Maple.Room) = Ahorn.drawSprite(ctx, sprite, 0, 0, jx=0.5, jy=1, rot=0)
Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::PelletEmitterDown, room::Maple.Room) = Ahorn.drawSprite(ctx, sprite, 16, 16, jx=0.5, jy=1, rot=pi)
Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::PelletEmitterLeft, room::Maple.Room) = Ahorn.drawSprite(ctx, sprite, 0, 16, jx=0.5, jy=1, rot=-pi/2)
Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::PelletEmitterRight, room::Maple.Room) = Ahorn.drawSprite(ctx, sprite, 16, 0, jx=0.5, jy=1, rot=pi/2)

end
