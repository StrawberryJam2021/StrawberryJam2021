module SJ2021ToggleSwapBlock

using ..Ahorn, Maple

@mapdef Entity "SJ2021/ToggleSwapBlock" ToggleBlock(width::Integer=16, height::Integer=16, travelSpeed::Number=5.0, oscillate::Bool=false, stopAtEnd::Bool=false,
    directionIndicator::Bool=false, constantSpeed::Bool=false, customTexturePath::String="", customIndicatorPath::String="")

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

defaultFrame = "objects/canyon/toggleblock/block1"
midResource = "objects/canyon/toggleblock/middleRed00"
defaultIndicatorPath = "objects/StrawberryJam2021/toggleIndicator/plain/"
paths = ["right", "downRight", "down", "downLeft", "left", "upLeft", "up", "upRight", "stay", "done"]
STAY, DONE = 9, 10

function renderSingleToggleBlock(ctx::Ahorn.Cairo.CairoContext, frame::String, x::Number, y::Number, width::Number, height::Number)
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
    if sx == tx && sy == ty
        return STAY
    end
    theta = atan(ty - sy, tx - sx)
    index = Int(round(theta * (4 / pi)))
    if index < 0
        index += 8
    end
    return index + 1
end

function drawArrow(ctx::Ahorn.Cairo.CairoContext, sx::Number, sy::Number, tx::Number, ty::Number, off_x::Number, off_y::Number)
    if sx == tx && sy == ty
        return
    end
    theta = atan(sy - ty, sx - tx)
    Ahorn.drawArrow(ctx, sx + off_x * (1 - cos(theta)), sy + off_y * (1 - sin(theta)), tx + off_x * (1 + cos(theta)), ty + off_y * (1 + sin(theta)), Ahorn.colors.selection_selected_fc, headLength=6)
end

function drawIndicator(ctx::Ahorn.Cairo.CairoContext, x::Number, y::Number, index::Integer, width::Number, height::Number, indicatorPath::String)
    resource = string(indicatorPath, paths[index])
    drawSprite(ctx, x, y, resource, width, height)
end

function drawSprite(ctx::Ahorn.Cairo.CairoContext, x::Number, y::Number, resource::String, width::Number, height::Number)
    sprite = Ahorn.getSprite(resource, "Gameplay")
    Ahorn.drawImage(ctx, sprite, x + div(width - sprite.width, 2), y + div(height - sprite.height, 2))
end

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::ToggleBlock, room::Maple.Room)
    frame = get(entity.data, "customTexturePath", "")
    if length(frame) == 0
        frame = defaultFrame
    end

    nodes = get(entity.data, "nodes", ())
    x, y, width, height = getXYWidthHeight(entity)

    off_x, off_y = width / 2, height / 2

    px, py = x, y
    for node in nodes
        nx, ny = Int.(node)
        drawArrow(ctx, px, py, nx, ny, off_x, off_y)
        px, py = nx, ny
    end

    stopAtEnd = get(entity.data, "stopAtEnd", true)
    oscillate = get(entity.data, "oscillate", false)

    if !stopAtEnd
        if !oscillate
            drawArrow(ctx, px, py, x, y, off_x, off_y)
        else
            px, py = x, y
            for node in nodes
                nx, ny = Int.(node)
                drawArrow(ctx, nx, ny, px, py, off_x, off_y)
                px, py = nx, ny
            end
        end
    end

    renderSingleToggleBlock(ctx, frame, x, y, width, height)

    for node in nodes
        nx, ny = Int.(node)
        renderSingleToggleBlock(ctx, frame, nx, ny, width, height)
    end

    showIndicators = get(entity.data, "directionIndicator", true)

    if !showIndicators
        drawSprite(ctx, x, y, midResource, width, height)
        for node in nodes
            nx, ny = Int.(node)
            drawSprite(ctx, nx, ny, midResource, width, height)
        end
    else
        indicatorPath = get(entity.data, "customIndicatorPath", "")
        if indicatorPath == ""
            indicatorPath = defaultIndicatorPath
        end
        if last(indicatorPath) != '/'
            indicatorPath = string(indicatorPath, '/')
        end

        prevStay = !stopAtEnd && !oscillate && last(nodes) == (x, y)
        px, py, nx, ny = x, y, x, y
        for node in nodes
            px, py = nx, ny
            nx, ny = Int.(node)
            index = getIndex(px, py, nx, ny)
            currStay = index == STAY
            if !currStay
                if prevStay
                    index = STAY
                end
                drawIndicator(ctx, px, py, index, width, height, indicatorPath)
            end
            prevStay = currStay
        end

        if prevStay
            drawIndicator(ctx, nx, ny, STAY, width, height, indicatorPath)
        else
            if stopAtEnd
                index = DONE
            elseif oscillate
                index = getIndex(nx, ny, px, py)
            else
                index = getIndex(nx, ny, x, y)
            end
            drawIndicator(ctx, nx, ny, index, width, height, indicatorPath)
        end
    end
end

end