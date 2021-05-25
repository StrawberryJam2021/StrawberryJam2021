module SJ2021ToggleSwapBlock

using ..Ahorn, Maple

@mapdef Entity "SJ2021/ToggleSwapBlock" ToggleBlock(width::Integer=16, height::Integer=16, travelSpeed::Number=5.0, oscillate::Bool=false, stopAtEnd::Bool=true, customTexturePath::String="")

function getXYWidthHeight(entity::ToggleBlock)
    x, y = Ahorn.position(entity)
    return x, y, Int(get(entity.data, "width", 8)), Int(get(entity.data, "height", 8))
end

function toggleBlockFinalizer(entity::ToggleBlock)
    x, y, width, height = getXYWidthHeight(entity)
    entity.data["nodes"] = Tuple{Int, Int}[(x + width, y)]
end

const placements = Ahorn.PlacementDict(
    "Toggle Swap Block (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        ToggleBlock,
        "rectangle",
        Dict{String, Any}(),
        toggleBlockFinalizer
    )
)

Ahorn.nodeLimits(entity::ToggleBlock) = 1, -1
Ahorn.minimumSize(entity::ToggleBlock) = 16, 16
Ahorn.resizable(entity::ToggleBlock) = true, true

function Ahorn.selection(entity::ToggleBlock)
    nodes = get(entity.data, "nodes", ())

    x, y, width, height = getXYWidthHeight(entity)

    res = Ahorn.Rectangle[Ahorn.Rectangle(x, y, width, height)]
    
    for node in nodes
        nx, ny = Int.(node)

        push!(res, Ahorn.Rectangle(nx, ny, width, height))
    end

    return res
end

frame = "objects/canyon/toggleblock/block1"
pathPrefix = "objects/StrawberryJam2021/toggleIndicator/"
paths = ["right", "downRight", "down", "downLeft", "left", "upLeft", "up", "upRight", "stay", "done"]
STAY, DONE = 9, 10

function renderSingleToggleBlock(ctx::Ahorn.Cairo.CairoContext, x::Number, y::Number, width::Number, height::Number)
    tilesWidth = div(width, 8)
    tilesHeight = div(height, 8)

    for i in 2:tilesWidth - 1
        Ahorn.drawImage(ctx, frame, x + (i - 1) * 8, y, 8, 0, 8, 8)
        Ahorn.drawImage(ctx, frame, x + (i - 1) * 8, y + height - 8, 8, 16, 8, 8)
    end

    for i in 2:tilesHeight - 1
        Ahorn.drawImage(ctx, frame, x, y + (i - 1) * 8, 0, 8, 8, 8)
        Ahorn.drawImage(ctx, frame, x + width - 8, y + (i - 1) * 8, 16, 8, 8, 8)
    end

    for i in 2:tilesWidth - 1, j in 2:tilesHeight - 1
        Ahorn.drawImage(ctx, frame, x + (i - 1) * 8, y + (j - 1) * 8, 8, 8, 8, 8)
    end

    Ahorn.drawImage(ctx, frame, x, y, 0, 0, 8, 8)
    Ahorn.drawImage(ctx, frame, x + width - 8, y, 16, 0, 8, 8)
    Ahorn.drawImage(ctx, frame, x, y + height - 8, 0, 16, 8, 8)
    Ahorn.drawImage(ctx, frame, x + width - 8, y + height - 8, 16, 16, 8, 8)
end

function getIndex(sx::Number, sy::Number, tx::Number, ty::Number)
    if (sx == tx && sy == ty)
        return STAY
    end
    theta = atan(ty - sy, tx - sx)
    index = Int(round(theta * (4 / pi)))
    if (index < 0)
        index += 8
    end
    return index + 1
end

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::ToggleBlock, room::Maple.Room)
    customTexture = get(entity.data, "customTexturePath", "")
    if (length(customTexture) > 0)
        global frame = customTexture
    else
        global frame = "objects/canyon/toggleblock/block1"
    end

    nodes = get(entity.data, "nodes", ())
    x, y, width, height = getXYWidthHeight(entity)

    half_width, half_height = width / 2, height / 2

    function drawArrow(sx::Number, sy::Number, tx::Number, ty::Number)
        if (sx == tx && sy == ty)
            return
        end
        theta = atan(sy - ty, sx - tx)
        Ahorn.drawArrow(ctx, sx + half_width * (1 - cos(theta)), sy + half_height * (1 - sin(theta)), tx + half_width * (1 + cos(theta)), ty + half_height * (1 + sin(theta)), Ahorn.colors.selection_selected_fc, headLength=6)
    end

    px, py = x, y
    for node in nodes
        nx, ny = Int.(node)
        drawArrow(px, py, nx, ny)
        px, py = nx, ny
    end

    stopAtEnd = get(entity.data, "stopAtEnd", true)
    oscillate = get(entity.data, "oscillate", false)

    if (!stopAtEnd)
        if (!oscillate)
            drawArrow(px, py, x, y)
        else
            px, py = x, y
            for node in nodes
                nx, ny = Int.(node)
                drawArrow(nx, ny, px, py)
                px, py = nx, ny
            end
        end
    end

    function drawSprite(x::Number, y::Number, index::Integer)
        resource = string(pathPrefix, paths[index])
        sprite = Ahorn.getSprite(resource, "Gameplay")
        Ahorn.drawImage(ctx, sprite, x + div(width - sprite.width, 2), y + div(height - sprite.height, 2))
    end

    renderSingleToggleBlock(ctx, x, y, width, height)

    prevStay = false
    px, py, nx, ny = x, y, x, y
    for node in nodes
        px, py = nx, ny
        nx, ny = Int.(node)
        renderSingleToggleBlock(ctx, nx, ny, width, height)
        index = getIndex(px, py, nx, ny)
        currStay = index == STAY
        if (!currStay)
            if (prevStay)
                index = STAY
            end
            drawSprite(px, py, index)
        end
        prevStay = currStay
    end

    if (prevStay)
        drawSprite(nx, ny, STAY)
    else
        index = DONE
        if (oscillate)
            index = getIndex(nx, ny, px, py)
        elseif (!stopAtEnd)
            index = getIndex(nx, ny, x, y)
        end
        drawSprite(nx, ny, index)
    end
end

end