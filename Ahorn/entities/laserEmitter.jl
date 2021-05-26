module SJ2021LaserEmitter

using ..Ahorn, Maple

const default_alpha = 0.4
const default_thickness = 6.0
const default_style = "Rounded"

# color is used to render the beam and tint the emitter
# FF2626 (HSV 0,85,100) is the default red as requested by hennyburgr/SchaapMakker 
const default_color = "FF2626"

# color channel is used for linking lasers with each other and with linkedzipmovers
# defaults to fullbright red as a matter of convenience
const default_color_channel = "FF0000"

const styles = ["Simple", "Rounded"]

@mapdef Entity "SJ2021/LaserEmitterUp" LaserEmitterUp(
    x::Integer, y::Integer,
    color::String=default_color, alpha::Real=default_alpha, thickness::Real=default_thickness, flicker::Bool=true,
    killPlayer::Bool=true, collideWithSolids::Bool=true,
    disableLasers::Bool=false, triggerZipMovers::Bool=false, colorChannel::String=default_color_channel,
    style::String=default_style
)

@mapdef Entity "SJ2021/LaserEmitterDown" LaserEmitterDown(
    x::Integer, y::Integer,
    color::String=default_color, alpha::Real=default_alpha, thickness::Real=default_thickness, flicker::Bool=true,
    killPlayer::Bool=true, collideWithSolids::Bool=true,
    disableLasers::Bool=false, triggerZipMovers::Bool=false, colorChannel::String=default_color_channel,
    style::String=default_style
)

@mapdef Entity "SJ2021/LaserEmitterLeft" LaserEmitterLeft(
    x::Integer, y::Integer,
    color::String=default_color, alpha::Real=default_alpha, thickness::Real=default_thickness, flicker::Bool=true,
    killPlayer::Bool=true, collideWithSolids::Bool=true,
    disableLasers::Bool=false, triggerZipMovers::Bool=false, colorChannel::String=default_color_channel,
    style::String=default_style
)

@mapdef Entity "SJ2021/LaserEmitterRight" LaserEmitterRight(
    x::Integer, y::Integer,
    color::String=default_color, alpha::Real=default_alpha, thickness::Real=default_thickness, flicker::Bool=true,
    killPlayer::Bool=true, collideWithSolids::Bool=true,
    disableLasers::Bool=false, triggerZipMovers::Bool=false, colorChannel::String=default_color_channel,
    style::String=default_style
)

const laserEmitterUnion = Union{LaserEmitterUp, LaserEmitterDown, LaserEmitterLeft, LaserEmitterRight}

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

simple_sprite = "objects/StrawberryJam2021/laserEmitter/simple00"

Ahorn.editingOptions(entity::laserEmitterUnion) = Dict{String, Any}( "style" => styles )

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::LaserEmitterUp, room::Maple.Room) = Ahorn.drawSprite(ctx, simple_sprite, 0, 0, jx=0.5, jy=1, rot=0)
Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::LaserEmitterDown, room::Maple.Room) = Ahorn.drawSprite(ctx, simple_sprite, 16, 16, jx=0.5, jy=1, rot=pi)
Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::LaserEmitterLeft, room::Maple.Room) = Ahorn.drawSprite(ctx, simple_sprite, 0, 16, jx=0.5, jy=1, rot=-pi/2)
Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::LaserEmitterRight, room::Maple.Room) = Ahorn.drawSprite(ctx, simple_sprite, 16, 0, jx=0.5, jy=1, rot=pi/2)

end
