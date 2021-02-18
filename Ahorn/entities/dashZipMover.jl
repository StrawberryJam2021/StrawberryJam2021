module SJ2021DashZipMover

using ..Ahorn, Maple

@mapdef Entity "SJ2021/DashZipMover" DashZipMover(x::Integer, y::Integer, nodes::Array{Tuple{Integer, Integer}, 1}=Tuple{Integer, Integer}[])

const placements = Ahorn.PlacementDict(
    "Dash Zip Mover (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        DashZipMover,
        "rectangle",
        Dict{String, Any}(),
        function(entity)
            entity.data["nodes"] = [(Int(entity.data["x"]) + Int(entity.data["width"]) + 8, Int(entity.data["y"]))]
        end
    )
)

Ahorn.nodeLimits(entity::DashZipMover) = 1, 1
Ahorn.minimumSize(entity::DashZipMover) = 16, 16
Ahorn.resizable(entity::DashZipMover) = true, true

const ropeColor = (6, 82, 23, 255) ./ 255

const block = "objects/zipmover/moon/block" 
const light = "objects/zipmover/moon/light01"
const cog = "objects/StrawberryJam2021/dashZipMover/cog"

function Ahorn.selection(entity::DashZipMover)
    x, y = Ahorn.position(entity)
    nx, ny = Int.(entity.data["nodes"][1])

    width = Int(get(entity.data, "width", 8))
    height = Int(get(entity.data, "height", 8))

    return [Ahorn.Rectangle(x, y, width, height), Ahorn.Rectangle(nx + floor(Int, width / 2) - 5, ny + floor(Int, height / 2) - 5, 10, 10)]
end

function renderZipMover(ctx::Ahorn.Cairo.CairoContext, entity::DashZipMover)
    x, y = Ahorn.position(entity)
    nx, ny = Int.(entity.data["nodes"][1])

    width = Int(get(entity.data, "width", 32))
    height = Int(get(entity.data, "height", 32))

    lightSprite = Ahorn.getSprite(light, "Gameplay")

    tilesWidth = div(width, 8)
    tilesHeight = div(height, 8)

    cx, cy = x + width / 2, y + height / 2
    cnx, cny = nx + width / 2, ny + height / 2

    length = sqrt((x - nx)^2 + (y - ny)^2)
    theta = atan(cny - cy, cnx - cx)

    Ahorn.Cairo.save(ctx)

    Ahorn.translate(ctx, cx, cy)
    Ahorn.rotate(ctx, theta)

    Ahorn.setSourceColor(ctx, ropeColor)
    Ahorn.set_antialias(ctx, 1)
    Ahorn.set_line_width(ctx, 1);

    # Offset for rounding errors
    Ahorn.move_to(ctx, 0, 4 + (theta <= 0))
    Ahorn.line_to(ctx, length, 4 + (theta <= 0))

    Ahorn.move_to(ctx, 0, -4 - (theta > 0))
    Ahorn.line_to(ctx, length, -4 - (theta > 0))

    Ahorn.stroke(ctx)

    Ahorn.Cairo.restore(ctx)

    Ahorn.drawRectangle(ctx, x + 2, y + 2, width - 4, height - 4, (0.0, 0.0, 0.0, 1.0))
    Ahorn.drawSprite(ctx, cog, cnx, cny)

    for i in 2:tilesWidth - 1
        Ahorn.drawImage(ctx, block, x + (i - 1) * 8, y, 8, 0, 8, 8)
        Ahorn.drawImage(ctx, block, x + (i - 1) * 8, y + height - 8, 8, 16, 8, 8)
    end

    for i in 2:tilesHeight - 1
        Ahorn.drawImage(ctx, block, x, y + (i - 1) * 8, 0, 8, 8, 8)
        Ahorn.drawImage(ctx, block, x + width - 8, y + (i - 1) * 8, 16, 8, 8, 8)
    end

    Ahorn.drawImage(ctx, block, x, y, 0, 0, 8, 8)
    Ahorn.drawImage(ctx, block, x + width - 8, y, 16, 0, 8, 8)
    Ahorn.drawImage(ctx, block, x, y + height - 8, 0, 16, 8, 8)
    Ahorn.drawImage(ctx, block, x + width - 8, y + height - 8, 16, 16, 8, 8)

    Ahorn.drawImage(ctx, lightSprite, x + floor(Int, (width - lightSprite.width) / 2), y)
end

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::DashZipMover, room::Maple.Room)
    renderZipMover(ctx, entity)
end

end