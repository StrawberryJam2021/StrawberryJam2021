module SJ2021LaserEmitter

using ..Ahorn, Maple

const default_alpha=0.4
const default_thickness=6.0
const default_color="FF0000"
const default_flicker_frequency=4.0
const default_flicker_intensity=8.0

@mapdef Entity "SJ2021/LaserEmitterUp" LaserEmitterUp(
    x::Integer, y::Integer,
    color::String=default_color, alpha::Float64=default_alpha,
    flickerFrequency::Float64=default_flicker_frequency, flickerIntensity::Float64=default_flicker_intensity,
    thickness::Float64=default_thickness,
    killPlayer::Bool=true
)

@mapdef Entity "SJ2021/LaserEmitterDown" LaserEmitterDown(
    x::Integer, y::Integer,
    color::String=default_color, alpha::Float64=default_alpha,
    flickerFrequency::Float64=default_flicker_frequency, flickerIntensity::Float64=default_flicker_intensity,
    thickness::Float64=default_thickness,
    killPlayer::Bool=true
)

@mapdef Entity "SJ2021/LaserEmitterLeft" LaserEmitterLeft(
    x::Integer, y::Integer,
    color::String=default_color, alpha::Float64=default_alpha,
    flickerFrequency::Float64=default_flicker_frequency, flickerIntensity::Float64=default_flicker_intensity,
    thickness::Float64=default_thickness,
    killPlayer::Bool=true
)

@mapdef Entity "SJ2021/LaserEmitterRight" LaserEmitterRight(
    x::Integer, y::Integer,
    color::String=default_color, alpha::Float64=default_alpha,
    flickerFrequency::Float64=default_flicker_frequency, flickerIntensity::Float64=default_flicker_intensity,
    thickness::Float64=default_thickness,
    killPlayer::Bool=true
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

Ahorn.editingOptions(entity::LaserEmitterUp) = Dict{String, Any}( "color" => colors )
Ahorn.editingOptions(entity::LaserEmitterDown) = Dict{String, Any}( "color" => colors )
Ahorn.editingOptions(entity::LaserEmitterLeft) = Dict{String, Any}( "color" => colors )
Ahorn.editingOptions(entity::LaserEmitterRight) = Dict{String, Any}( "color" => colors )

sprite = "objects/StrawberryJam2021/laserEmitter/idle00"

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::LaserEmitterUp, room::Maple.Room) = Ahorn.drawSprite(ctx, sprite, 0, 0, jx=0.5, jy=1, rot=0)
Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::LaserEmitterDown, room::Maple.Room) = Ahorn.drawSprite(ctx, sprite, 16, 16, jx=0.5, jy=1, rot=pi)
Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::LaserEmitterLeft, room::Maple.Room) = Ahorn.drawSprite(ctx, sprite, 0, 16, jx=0.5, jy=1, rot=-pi/2)
Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::LaserEmitterRight, room::Maple.Room) = Ahorn.drawSprite(ctx, sprite, 16, 0, jx=0.5, jy=1, rot=pi/2)

end
