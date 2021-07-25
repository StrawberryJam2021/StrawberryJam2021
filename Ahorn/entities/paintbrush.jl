module SJ2021Paintbrush

using ..Ahorn, Maple

const default_size = 16
const thickness = 8

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

const scales = Dict{String, Tuple{Integer, Integer}}(
    "up" => (1, -1),
    "down" => (1, 1),
    "left" => (-1, 1),
    "right" => (1, 1),
)

const rotations = Dict{String, Number}(
    "up" => 0,
    "down" => pi,
    "left" => -pi / 2,
    "right" => pi / 2,
)

const smallOffsets = Dict{String, Tuple{Integer, Integer}}(
    "up" => (12, -8),
    "down" => (12, 8),
    "left" => (-8, 4),
    "right" => (8, 4),
)

const largeOffsets = Dict{String, Tuple{Integer, Integer}}(
    "up" => (28, -24),
    "down" => (28, 24),
    "left" => (-16, 12),
    "right" => (16, 12),
)

const paintbrushUnion = Union{PaintbrushUp, PaintbrushDown, PaintbrushLeft, PaintbrushRight}

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

sprite_path = "objects/StrawberryJam2021/paintbrush"

function largeSpriteForTexture(entity::paintbrushUnion)
    index = get(entity.data, "cassetteIndex", 0)
    prefix = index == 0 ? "blue" : "pink"
    return "$(sprite_path)/$(prefix)/brush1"
end

function smallSpriteForTexture(entity::paintbrushUnion)
    index = get(entity.data, "cassetteIndex", 0)
    prefix = index == 0 ? "blue" : "pink"
    return "$(sprite_path)/$(prefix)/backbrush1"
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::paintbrushUnion, room::Maple.Room)
    largeSprite = largeSpriteForTexture(entity)
    smallSprite = smallSpriteForTexture(entity)
    direction = get(directions, entity.name, "up")
    width = get(entity.data, "width", 0)
    height = get(entity.data, "height", 0)
    vertical = direction == "up" || direction == "down"
    size = vertical ? width : height
    tiles = Integer(size / 8)
    theta = vertical ? pi / 2 : 0
    sx, sy = scales[direction]
    sox, soy = smallOffsets[direction]
    lox, loy = largeOffsets[direction]

    # draw small
    for num in 2:(tiles - 1)
        if num % 2 != 0
            continue
        end
        drawX = vertical ? num * 8 : 0
        drawY = vertical ? 0 : num * 8
        Ahorn.drawSprite(ctx, smallSprite, drawX + sox, drawY + soy, jx=0.5, jy=1, sx=sx, sy=sy, rot=theta)
    end

    # draw large
    for num in 1:(tiles - 1)
        if num % 2 == 0
            continue
        end
        drawX = vertical ? num * 8 : 0
        drawY = vertical ? 0 : num * 8
        Ahorn.drawSprite(ctx, largeSprite, drawX + lox, drawY + loy, jx=0.5, jy=1, sx=sx, sy=sy, rot=theta)
    end
end

end
