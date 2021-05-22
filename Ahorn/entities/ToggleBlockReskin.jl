module SJ2021ToggleSwapBlock

using ..Ahorn, Maple

@mapdef Entity "SJ2021/ToggleSwapBlock" ToggleBlock(x1::Integer, y1::Integer, x2::Integer=x1+16, y2::Integer=y1, width::Integer=16, height::Integer=16)

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
        Dict{String, Any}(
            "travelSpeed" => 5.0,
            "oscillate" => false,
            "stopAtEnd" => true,
            "customTexturePath" => ""
        ),
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
midResource = "objects/StrawberryJam2021/toggleIndicator/stay"

function renderSingleToggleBlock(ctx::Ahorn.Cairo.CairoContext, x::Number, y::Number, width::Number, height::Number)
    midSprite = Ahorn.getSprite(midResource, "Gameplay")
    
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

    Ahorn.drawImage(ctx, midSprite, x + div(width - midSprite.width, 2), y + div(height - midSprite.height, 2))
end

function renderToggleBlock(ctx::Ahorn.Cairo.CairoContext, width::Number, height::Number, entity::ToggleBlock)
    
    nodes = get(entity.data, "nodes", ())

    for node in nodes
        nx, ny = Int.(node)

        renderSingleToggleBlock(ctx, nx, ny, width, height)
    end
end

function Ahorn.renderSelectedAbs(ctx::Ahorn.Cairo.CairoContext, entity::ToggleBlock, room::Maple.Room)
    sprite = get(entity.data, "sprite", "block")

    px, py, width, height = getXYWidthHeight(entity)

    for node in get(entity.data, "nodes", ())
        nx, ny = Int.(node)

        theta = atan(py - ny, px - nx)
        Ahorn.drawArrow(ctx, px + width / 2, py + height / 2, nx + width / 2 + cos(theta) * 8, ny + height / 2 + sin(theta) * 8, Ahorn.colors.selection_selected_fc, headLength=6)

        px, py = nx, ny
    end

    renderToggleBlock(ctx, width, height, entity)
end

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::ToggleBlock, room::Maple.Room)
    customTexture = get(entity.data, "customTexturePath", "")
    if (length(customTexture) > 0 && !occursin('"', customTexture))
        global frame = customTexture
    else
        global frame = "objects/canyon/toggleblock/block1"
    end

    Ahorn.renderSelectedAbs(ctx, entity, room)

    x, y, width, height = getXYWidthHeight(entity)
    renderSingleToggleBlock(ctx, x, y, width, height)
end

end