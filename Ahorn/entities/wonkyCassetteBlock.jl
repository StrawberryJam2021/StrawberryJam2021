module SJ2021WonkyCassetteBlock
# Most of this file was copied from the CassetteBlock jl file in Ahorn

using ..Ahorn, Maple

@mapdef Entity "SJ2021/WonkyCassetteBlock" WonkyCassetteBlock(
    x::Integer,
    y::Integer,
    onAtBeats="1, 3",
    color::String="FFFFFF",
    textureDirectory::String="objects/cassetteblock",
    boostFrames::Integer=-1,
    controllerIndex::Integer=0,
)

const placements = Ahorn.PlacementDict(
    "Wonky Cassette Block (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        WonkyCassetteBlock,
        "rectangle",
    )
)

Ahorn.minimumSize(entity::WonkyCassetteBlock) = 16, 16
Ahorn.resizable(entity::WonkyCassetteBlock) = true, true

Ahorn.selection(entity::WonkyCassetteBlock) = Ahorn.getEntityRectangle(entity)

function getCassetteBlockRectangles(room::Maple.Room)
    entities = filter(e -> e.name == "SJ2021/WonkyCassetteBlock", room.entities)
    rects = Dict{String, Array{Ahorn.Rectangle, 1}}()

    for e in entities
        colorHex = String(get(e.data, "color", "FFFFFF"))
        rectList = get!(rects, colorHex) do
            Ahorn.Rectangle[]
        end

        push!(rectList, Ahorn.Rectangle(
            Int(get(e.data, "x", 0)),
            Int(get(e.data, "y", 0)),
            Int(get(e.data, "width", 8)),
            Int(get(e.data, "height", 8))
        ))
    end

    return rects
end

# Is there a casette block we should connect to at the offset?
function notAdjacent(entity::WonkyCassetteBlock, ox, oy, rects)
    x, y = Ahorn.position(entity)
    rect = Ahorn.Rectangle(x + ox + 4, y + oy + 4, 1, 1)

    for r in rects
        if Ahorn.checkCollision(r, rect)
            return false
        end
    end

    return true
end

function drawCassetteBlock(ctx::Ahorn.Cairo.CairoContext, entity::WonkyCassetteBlock, room::Maple.Room)
    cassetteBlockRectangles = getCassetteBlockRectangles(room)

    x, y = Ahorn.position(entity)

    width = Int(get(entity.data, "width", 32))
    height = Int(get(entity.data, "height", 32))

    tileWidth = ceil(Int, width / 8)
    tileHeight = ceil(Int, height / 8)

    colorHex = String(get(entity.data, "color", "FFFFFF"))
    color = tuple(Ahorn.argb32ToRGBATuple(parse(Int, String(get(entity.data, "color", "FFFFFF")), base=16))[1:3] ./ 255..., 1.0)

    frame = String(get(entity.data, "textureDirectory", "objects/cassetteblock")) * "/solid"

    rect = Ahorn.Rectangle(x, y, width, height)
    rects = get(cassetteBlockRectangles, colorHex, Ahorn.Rectangle[])

    if !(rect in rects)
        push!(rects, rect)
    end

    for x in 1:tileWidth, y in 1:tileHeight
        drawX, drawY = (x - 1) * 8, (y - 1) * 8

        closedLeft = !notAdjacent(entity, drawX - 8, drawY, rects)
        closedRight = !notAdjacent(entity, drawX + 8, drawY, rects)
        closedUp = !notAdjacent(entity, drawX, drawY - 8, rects)
        closedDown = !notAdjacent(entity, drawX, drawY + 8, rects)
        completelyClosed = closedLeft && closedRight && closedUp && closedDown

        if completelyClosed
            if notAdjacent(entity, drawX + 8, drawY - 8, rects)
                Ahorn.drawImage(ctx, frame, drawX, drawY, 24, 0, 8, 8, tint=color)

            elseif notAdjacent(entity, drawX - 8, drawY - 8, rects)
                Ahorn.drawImage(ctx, frame, drawX, drawY, 24, 8, 8, 8, tint=color)

            elseif notAdjacent(entity, drawX + 8, drawY + 8, rects)
                Ahorn.drawImage(ctx, frame, drawX, drawY, 24, 16, 8, 8, tint=color)

            elseif notAdjacent(entity, drawX - 8, drawY + 8, rects)
                Ahorn.drawImage(ctx, frame, drawX, drawY, 24, 24, 8, 8, tint=color)

            else
                Ahorn.drawImage(ctx, frame, drawX, drawY, 8, 8, 8, 8, tint=color)
            end

        else
            if closedLeft && closedRight && !closedUp && closedDown
                Ahorn.drawImage(ctx, frame, drawX, drawY, 8, 0, 8, 8, tint=color)

            elseif closedLeft && closedRight && closedUp && !closedDown
                Ahorn.drawImage(ctx, frame, drawX, drawY, 8, 16, 8, 8, tint=color)

            elseif closedLeft && !closedRight && closedUp && closedDown
                Ahorn.drawImage(ctx, frame, drawX, drawY, 16, 8, 8, 8, tint=color)

            elseif !closedLeft && closedRight && closedUp && closedDown
                Ahorn.drawImage(ctx, frame, drawX, drawY, 0, 8, 8, 8, tint=color)

            elseif closedLeft && !closedRight && !closedUp && closedDown
                Ahorn.drawImage(ctx, frame, drawX, drawY, 16, 0, 8, 8, tint=color)

            elseif !closedLeft && closedRight && !closedUp && closedDown
                Ahorn.drawImage(ctx, frame, drawX, drawY, 0, 0, 8, 8, tint=color)

            elseif !closedLeft && closedRight && closedUp && !closedDown
                Ahorn.drawImage(ctx, frame, drawX, drawY, 0, 16, 8, 8, tint=color)

            elseif closedLeft && !closedRight && closedUp && !closedDown
                Ahorn.drawImage(ctx, frame, drawX, drawY, 16, 16, 8, 8, tint=color)
            end
        end
    end
end

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::WonkyCassetteBlock, room::Maple.Room) = drawCassetteBlock(ctx, entity, room)

end