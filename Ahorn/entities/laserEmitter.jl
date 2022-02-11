module SJ2021LaserEmitter

using ..Ahorn, Maple

const DEFAULT_ALPHA = 0.4
const DEFAULT_THICKNESS = 6.0
const DEFAULT_STYLE = "Rounded"

# color is used to render the beam and tint the emitter
# FF2626 (HSV 0,85,100) is the default red as requested by hennyburgr/SchaapMakker
const DEFAULT_COLOR = "FF2626"

# color channel is used for linking lasers with each other and with linkedzipmovers
# defaults to fullbright red as a matter of convenience
const DEFAULT_COLOR_CHANNEL = "FF0000"

const styles = ["Simple", "Rounded", "Large"]

const sprites = Dict{String, String}(
    "Simple" => "objects/StrawberryJam2021/laserEmitter/simple00",
    "Rounded" => "objects/StrawberryJam2021/laserEmitter/rounded_base00",
    "Large" => "objects/StrawberryJam2021/laserEmitter/large_base00",
)

const rounded_tint = "objects/StrawberryJam2021/laserEmitter/rounded_tint00"

const offsets = Dict{String, Array{Tuple{Integer, Integer}}}(
    "Simple" => [(0, 0), (0, 0), (0, 16), (0, 16)],
    "Rounded" => [(0, 0), (0, 0), (0, 18), (0, 18)],
    "Large" => [(0, 0), (0, 0), (-8, 24), (8, 24)]
)

@mapdef Entity "SJ2021/LaserEmitterUp" LaserEmitterUp(
    x::Integer, y::Integer,
    color::String=DEFAULT_COLOR, alpha::Real=DEFAULT_ALPHA, thickness::Real=DEFAULT_THICKNESS, flicker::Bool=true,
    killPlayer::Bool=true, collideWithSolids::Bool=true,
    disableLasers::Bool=false, triggerZipMovers::Bool=false, colorChannel::String=DEFAULT_COLOR_CHANNEL,
    style::String=DEFAULT_STYLE, leniency::Integer=1, beamAboveEmitter::Bool=false,
    emitterColliderWidth::Real=14, emitterColliderHeight::Real=6, emitSparks::Bool=true
)

@mapdef Entity "SJ2021/LaserEmitterDown" LaserEmitterDown(
    x::Integer, y::Integer,
    color::String=DEFAULT_COLOR, alpha::Real=DEFAULT_ALPHA, thickness::Real=DEFAULT_THICKNESS, flicker::Bool=true,
    killPlayer::Bool=true, collideWithSolids::Bool=true,
    disableLasers::Bool=false, triggerZipMovers::Bool=false, colorChannel::String=DEFAULT_COLOR_CHANNEL,
    style::String=DEFAULT_STYLE, leniency::Integer=1, beamAboveEmitter::Bool=false,
    emitterColliderWidth::Real=14, emitterColliderHeight::Real=6, emitSparks::Bool=true
)

@mapdef Entity "SJ2021/LaserEmitterLeft" LaserEmitterLeft(
    x::Integer, y::Integer,
    color::String=DEFAULT_COLOR, alpha::Real=DEFAULT_ALPHA, thickness::Real=DEFAULT_THICKNESS, flicker::Bool=true,
    killPlayer::Bool=true, collideWithSolids::Bool=true,
    disableLasers::Bool=false, triggerZipMovers::Bool=false, colorChannel::String=DEFAULT_COLOR_CHANNEL,
    style::String=DEFAULT_STYLE, leniency::Integer=1, beamAboveEmitter::Bool=false,
    emitterColliderWidth::Real=14, emitterColliderHeight::Real=6, emitSparks::Bool=true
)

@mapdef Entity "SJ2021/LaserEmitterRight" LaserEmitterRight(
    x::Integer, y::Integer,
    color::String=DEFAULT_COLOR, alpha::Real=DEFAULT_ALPHA, thickness::Real=DEFAULT_THICKNESS, flicker::Bool=true,
    killPlayer::Bool=true, collideWithSolids::Bool=true,
    disableLasers::Bool=false, triggerZipMovers::Bool=false, colorChannel::String=DEFAULT_COLOR_CHANNEL,
    style::String=DEFAULT_STYLE, leniency::Integer=1, beamAboveEmitter::Bool=false,
    emitterColliderWidth::Real=14, emitterColliderHeight::Real=6, emitSparks::Bool=true
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

Ahorn.editingOptions(entity::laserEmitterUnion) = Dict{String, Any}( "style" => styles )

function renderSprite(ctx::Ahorn.Cairo.CairoContext, entity::laserEmitterUnion, dir::Integer, sx::Real, sy::Real, rot::Real)
    style = get(entity.data, "style", "Simple")
    sprite = sprites[style]
    offset = offsets[style][dir]
    Ahorn.drawSprite(ctx, sprite, offset[1], offset[2], jx=0.5, jy=1, sx=sx, sy=sy, rot=rot)

    if style == "Rounded"
        tintcolor = parseColor(get(entity.data, "color", DEFAULT_COLOR))
        Ahorn.drawSprite(ctx, rounded_tint, offset[1], offset[2], jx=0.5, jy=1, sx=sx, sy=sy, rot=rot, tint=tintcolor)
    end
end

parseColor(hex::String) = Ahorn.argb32ToRGBATuple(parse(Int, hex, base=16))[1:4] ./ 255

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::LaserEmitterUp, room::Maple.Room) = renderSprite(ctx, entity, 1, 1, 1, 0)
Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::LaserEmitterDown, room::Maple.Room) = renderSprite(ctx, entity, 2, 1, -1, 0)
Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::LaserEmitterLeft, room::Maple.Room) = renderSprite(ctx, entity, 3, 1, 1, -pi/2)
Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::LaserEmitterRight, room::Maple.Room) = renderSprite(ctx, entity, 4, -1, 1, -pi/2)

end
