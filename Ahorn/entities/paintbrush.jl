module SJ2021Paintbrush

using ..Ahorn, Maple

const default_size = 16
const thickness = 8

const BEAM_THICKNESS = 12
const BRUSH_LENGTH = 16

@mapdef Entity "SJ2021/PaintbrushUp" PaintbrushUp(
    x::Integer, y::Integer, width::Integer=default_size,
    cassetteIndex::Integer=0, killPlayer::Bool=true, collideWithSolids::Bool=true, halfLength::Bool=false
)

@mapdef Entity "SJ2021/PaintbrushDown" PaintbrushDown(
    x::Integer, y::Integer, width::Integer=default_size,
    cassetteIndex::Integer=0, killPlayer::Bool=true, collideWithSolids::Bool=true, halfLength::Bool=false
)

@mapdef Entity "SJ2021/PaintbrushLeft" PaintbrushLeft(
    x::Integer, y::Integer, height::Integer=default_size,
    cassetteIndex::Integer=0, killPlayer::Bool=true, collideWithSolids::Bool=true, halfLength::Bool=false
)

@mapdef Entity "SJ2021/PaintbrushRight" PaintbrushRight(
    x::Integer, y::Integer, height::Integer=default_size,
    cassetteIndex::Integer=0, killPlayer::Bool=true, collideWithSolids::Bool=true, halfLength::Bool=false
)

const directions = Dict{String, String}(
    "SJ2021/PaintbrushUp" => "up",
    "SJ2021/PaintbrushDown" => "down",
    "SJ2021/PaintbrushLeft" => "left",
    "SJ2021/PaintbrushRight" => "right",
)
const brushLaserUnion = Union{PaintbrushUp, PaintbrushDown, PaintbrushLeft, PaintbrushRight}

const placements = Ahorn.PlacementDict(
    "Paintbrush (Up) (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        PaintbrushUp,
        "rectangle",
    ),
    "Paintbrush (Down) (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        PaintbrushDown,
        "rectangle",
    ),
    "Paintbrush (Left) (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        PaintbrushLeft,
        "rectangle",
    ),
    "Paintbrush (Right) (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        PaintbrushRight,
        "rectangle",
    )
)

function Ahorn.selection(entity::PaintbrushUp)
    x, y = Ahorn.position(entity)
    width = get(entity.data, "width", default_size)
    return Ahorn.Rectangle(x, y - thickness, width, thickness)
end

function Ahorn.selection(entity::PaintbrushDown)
    x, y = Ahorn.position(entity)
    width = get(entity.data, "width", default_size)
    return Ahorn.Rectangle(x, y, width, thickness)
end

function Ahorn.selection(entity::PaintbrushLeft)
    x, y = Ahorn.position(entity)
    height = get(entity.data, "height", default_size)
    return Ahorn.Rectangle(x - thickness, y, thickness, height)
end

function Ahorn.selection(entity::PaintbrushRight)
    x, y = Ahorn.position(entity)
    height = get(entity.data, "height", default_size)
    return Ahorn.Rectangle(x, y, thickness, height)
end

Ahorn.resizable(entity::PaintbrushUp) = true, false
Ahorn.resizable(entity::PaintbrushDown) = true, false
Ahorn.resizable(entity::PaintbrushLeft) = false, true
Ahorn.resizable(entity::PaintbrushRight) = false, true
Ahorn.minimumSize(entity::PaintbrushUp) = default_size, thickness
Ahorn.minimumSize(entity::PaintbrushDown) = default_size, thickness
Ahorn.minimumSize(entity::PaintbrushLeft) = thickness, default_size
Ahorn.minimumSize(entity::PaintbrushRight) = thickness, default_size

sprite_path = "objects/StrawberryJam2021/Paintbrush"

function spriteForTexture(entity::brushLaserUnion)
    index = get(entity.data, "cassetteIndex", 0)
    prefix = index == 0 ? "blue" : "pink"
    return "$(sprite_path)/$(prefix)/brush1"
end

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::PaintbrushUp, room::Maple.Room) = Ahorn.drawSprite(ctx, spriteForTexture(entity), -12, 0, jx=0, jy=0, rot=-pi/2)
Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::PaintbrushDown, room::Maple.Room) = Ahorn.drawSprite(ctx, spriteForTexture(entity), -12, 0, sy=-1, jx=0, jy=0, rot=-pi/2)

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::PaintbrushLeft, room::Maple.Room) = Ahorn.drawSprite(ctx, spriteForTexture(entity), 0, 0, sx=-1, jx=0, jy=0.5, rot=0)
Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::PaintbrushRight, room::Maple.Room) = Ahorn.drawSprite(ctx, spriteForTexture(entity), 0, 0, jx=0, jy=0.5, rot=0)

end
