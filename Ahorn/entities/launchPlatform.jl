module SJ2021LaunchPlatform

using ..Ahorn, Maple

const DEFAULT_WIDTH = 40
const DEFAULT_OFFSET = -80

@pardef LaunchPlatform(
    x1::Integer, y1::Integer,
    x2::Integer=x1, y2::Integer=y1+DEFAULT_OFFSET,
    width::Integer=DEFAULT_WIDTH
) = Entity("SJ2021/LaunchPlatform",
    x=x1, y=y1,
    nodes=Tuple{Int, Int}[(x2, y2)],
    width=width
)

const placements = Ahorn.PlacementDict(
    "Launch Platform (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        LaunchPlatform,
        "rectangle", 
        Dict{String, Any}(),
        function(entity)
            x, y = Int(entity.data["x"]), Int(entity.data["y"])
            entity.data["nodes"] = [(x, y + DEFAULT_OFFSET)]
        end
    )
)

const sprite = "objects/StrawberryJam2021/launchPlatform/default"

Ahorn.nodeLimits(entity::LaunchPlatform) = 1, 1
Ahorn.resizable(entity::LaunchPlatform) = true, false
Ahorn.minimumSize(entity::LaunchPlatform) = 8, 0

function Ahorn.selection(entity::LaunchPlatform)
    width = Int(get(entity.data, "width", 8))
    startX, startY = Int(entity.data["x"]), Int(entity.data["y"])
    stopX, stopY = Int.(entity.data["nodes"][1])
    return [Ahorn.Rectangle(startX, startY, width, 8), Ahorn.Rectangle(stopX, stopY, width, 8)]
end

function renderPlatform(ctx::Ahorn.Cairo.CairoContext, x::Int, y::Int, width::Int)
    tilesWidth = div(width, 8)

    for i in 2:tilesWidth - 1
        Ahorn.drawImage(ctx, sprite, x + 8 * (i - 1), y, 8, 0, 8, 8)
    end

    Ahorn.drawImage(ctx, sprite, x, y, 0, 0, 8, 8)
    Ahorn.drawImage(ctx, sprite, x + tilesWidth * 8 - 8, y, 24, 0, 8, 8)
    Ahorn.drawImage(ctx, sprite, x + floor(Int, width / 2) - 4, y, 16, 0, 8, 8)
end

outerColor = (30, 14, 25) ./ 255
innerColor = (10, 0, 6) ./ 255

function renderConnection(ctx::Ahorn.Cairo.CairoContext, x::Int, y::Int, nx::Int, ny::Int, width::Int)
    cx, cy = x + floor(Int, width / 2), y + 4
    cnx, cny = nx + floor(Int, width / 2), ny + 4

    length = sqrt((x - nx)^2 + (y - ny)^2)
    theta = atan(cny - cy, cnx - cx)

    Ahorn.Cairo.save(ctx)

    Ahorn.translate(ctx, cx, cy)
    Ahorn.rotate(ctx, theta)

    Ahorn.setSourceColor(ctx, outerColor)
    Ahorn.set_antialias(ctx, 1)
    Ahorn.set_line_width(ctx, 3);

    Ahorn.move_to(ctx, 0, 0)
    Ahorn.line_to(ctx, length, 0)

    Ahorn.stroke(ctx)

    Ahorn.setSourceColor(ctx, innerColor)
    Ahorn.set_antialias(ctx, 1)
    Ahorn.set_line_width(ctx, 1);

    Ahorn.move_to(ctx, 0, 0)
    Ahorn.line_to(ctx, length, 0)

    Ahorn.stroke(ctx)

    Ahorn.Cairo.restore(ctx)
end

function renderPlatform(ctx::Ahorn.Cairo.CairoContext, x::Int, y::Int, width::Int)
    tilesWidth = div(width, 8)

    for i in 2:tilesWidth - 1
        Ahorn.drawImage(ctx, sprite, x + 8 * (i - 1), y, 8, 0, 8, 8)
    end

    Ahorn.drawImage(ctx, sprite, x, y, 0, 0, 8, 8)
    Ahorn.drawImage(ctx, sprite, x + tilesWidth * 8 - 8, y, 24, 0, 8, 8)
    Ahorn.drawImage(ctx, sprite, x + floor(Int, width / 2) - 4, y, 16, 0, 8, 8)
end

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::LaunchPlatform, room::Maple.Room)
    width = Int(get(entity.data, "width", 8))

    x, y = Int(entity.data["x"]), Int(entity.data["y"])
    nx, ny = Int.(entity.data["nodes"][1])

    renderConnection(ctx, x, y, nx, ny, width)
    renderPlatform(ctx, x, y, width)
end

function Ahorn.renderSelectedAbs(ctx::Ahorn.Cairo.CairoContext, entity::LaunchPlatform, room::Maple.Room)
    width = Int(get(entity.data, "width", 8))

    startX, startY = Int(entity.data["x"]), Int(entity.data["y"])
    stopX, stopY = Int.(entity.data["nodes"][1])

    renderPlatform(ctx, startX, startY, width)
    renderPlatform(ctx, stopX, stopY, width)

    Ahorn.drawArrow(ctx, startX + width / 2, startY, stopX + width / 2, stopY, Ahorn.colors.selection_selected_fc, headLength=6)
end

end